using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transactions.Core.Models;

namespace Transactions.Infrastructure.DataBase.ConfigShema;

/// <summary>Mapeo de la tabla dbo.Transactions creada por database.sql.</summary>
public class TransactionConfig : IEntityTypeConfiguration<TransactionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionEntity> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(x => x.Id)
            .HasName("PK_Transactions");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Date)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TotalPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Detail)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2(3)");

        // Propiedad calculada en memoria, no es columna.
        builder.Ignore(x => x.StockEffect);

        builder.HasIndex(x => new { x.ProductId, x.Date })
            .HasDatabaseName("IX_Transactions_ProductId_Date");

        builder.HasIndex(x => new { x.Date, x.Type })
            .HasDatabaseName("IX_Transactions_Date_Type");
    }
}
