using System.ComponentModel.DataAnnotations;
using Products.Core.Dto.Product;

namespace Products.UnitTests;

public class ProductDtoValidationTests
{
    [Fact]
    public void ProductCreateDto_InvalidData_ReturnsErrorPerField()
    {
        var dto = new ProductCreateDto { Name = "", Category = "", ImageUrl = "no-es-url", Price = 0, Stock = -1 };

        var invalidFields = Validate(dto);

        Assert.Equivalent(
            new[] { "Name", "Category", "ImageUrl", "Price", "Stock" },
            invalidFields);
    }

    [Fact]
    public void ProductCreateDto_ValidData_HasNoErrors()
    {
        var dto = new ProductCreateDto { Name = "Mouse", Category = "Accesorios", ImageUrl = "https://img.test/a.png", Price = 9.99m, Stock = 0 };

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void ProductFilterDto_MinGreaterThanMax_IsInvalid()
    {
        var filter = new ProductFilterDto { MinPrice = 50, MaxPrice = 10, MinStock = 5, MaxStock = 1, SortBy = "otro" };

        var invalidFields = Validate(filter);

        Assert.Equivalent(new[] { "MaxPrice", "MaxStock", "SortBy" }, invalidFields);
    }

    private static List<string> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames).Distinct().ToList();
    }
}
