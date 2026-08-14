using KadreeBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadreeBank.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.AccountNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(a => a.AccountNumber).IsUnique();

        builder.Property(a => a.Balance)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(a => a.OriginCity)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(a => a.CustomerId);
    }
}
