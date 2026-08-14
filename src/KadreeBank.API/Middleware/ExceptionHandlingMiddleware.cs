using System.Net;
using FluentValidation;
using KadreeBank.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace KadreeBank.API.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
            InsufficientFundsException => (HttpStatusCode.UnprocessableEntity, exception.Message, null),
            DomainException => (HttpStatusCode.BadRequest, exception.Message, null),
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                "Uno o más campos no son válidos.",
                (IReadOnlyDictionary<string, string[]>)validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            _ => (HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Excepción no controlada procesando {Path}", context.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Type = $"https://httpstatuses.io/{(int)statusCode}"
        };

        if (errors is not null)
            problemDetails.Extensions["errors"] = errors;

        if (statusCode == HttpStatusCode.InternalServerError && environment.IsEnvironment("Testing"))
            problemDetails.Extensions["debugException"] = exception.ToString();

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
