FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY KadreeBank.sln .
COPY src/KadreeBank.Domain/KadreeBank.Domain.csproj src/KadreeBank.Domain/
COPY src/KadreeBank.Application/KadreeBank.Application.csproj src/KadreeBank.Application/
COPY src/KadreeBank.Infrastructure/KadreeBank.Infrastructure.csproj src/KadreeBank.Infrastructure/
COPY src/KadreeBank.API/KadreeBank.API.csproj src/KadreeBank.API/
RUN dotnet restore src/KadreeBank.API/KadreeBank.API.csproj

COPY src/ src/
RUN dotnet publish src/KadreeBank.API/KadreeBank.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "KadreeBank.API.dll"]
