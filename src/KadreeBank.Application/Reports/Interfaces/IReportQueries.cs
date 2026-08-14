using KadreeBank.Application.Reports.Dtos;

namespace KadreeBank.Application.Reports.Interfaces;

/// <summary>
/// Consultas de solo lectura que agregan datos entre Customer/Account/Transaction.
/// Se resuelven directamente contra el motor de base de datos (sin pasar por los
/// repositorios de agregados) porque son proyecciones de reporte, no operaciones
/// sobre un aggregate root.
/// </summary>
public interface IReportQueries
{
    Task<IReadOnlyList<CustomerTransactionCountDto>> GetCustomerTransactionCountsAsync(
        int year, int month, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutOfCityWithdrawalDto>> GetOutOfCityWithdrawalsAsync(
        decimal minAmount, CancellationToken cancellationToken = default);
}
