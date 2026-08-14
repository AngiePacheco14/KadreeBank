using KadreeBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadreeBank.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(t => t.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.BalanceAfter)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        // Soporta movimientos recientes y extractos mensuales (rango de fechas por cuenta).
        builder.HasIndex(t => new { t.AccountId, t.Timestamp });

        // Soporta el reporte de retiros fuera de ciudad (filtra por tipo antes de agrupar).
        builder.HasIndex(t => new { t.AccountId, t.Type });
    }
}
