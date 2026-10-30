using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Products.Core.Models;

namespace Products.Infrastructure.DataBase.ConfigShema;

/// <summary>Mapeo de la tabla dbo.Products creada por database.sql.</summary>
public class ProductConfig : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(x => x.Id)
            .HasName("PK_Products");

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Stock)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_Products_Name");

        builder.HasIndex(x => x.Category)
            .HasDatabaseName("IX_Products_Category");

        // Los productos eliminados se ocultan de las consultas, pero se conservan para el historial.
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
