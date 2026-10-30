using System.ComponentModel.DataAnnotations;
using Transactions.Core.Dto.Transaction;
using Transactions.Core.Enums;

namespace Transactions.UnitTests;

public class TransactionDtoValidationTests
{
    [Fact]
    public void TransactionCreateDto_MissingRequiredFields_IsInvalid()
    {
        var dto = new TransactionCreateDto { Quantity = 0, UnitPrice = 0 };

        var invalidFields = Validate(dto);

        Assert.Equivalent(new[] { "Type", "ProductId", "Quantity", "UnitPrice" }, invalidFields);
    }

    [Fact]
    public void TransactionCreateDto_FutureDate_IsInvalid()
    {
        var dto = new TransactionCreateDto
        {
            Date = DateTime.UtcNow.AddDays(5),
            Type = TransactionType.Sale,
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };

        Assert.Equal(["Date"], Validate(dto));
    }

    [Fact]
    public void TransactionCreateDto_UndefinedType_IsInvalid()
    {
        var dto = new TransactionCreateDto { Type = (TransactionType)9, ProductId = Guid.NewGuid(), Quantity = 1 };

        Assert.Equal(["Type"], Validate(dto));
    }

    [Fact]
    public void TransactionFilterDto_DateFromAfterDateTo_IsInvalid()
    {
        var filter = new TransactionFilterDto { DateFrom = new DateTime(2026, 10, 10), DateTo = new DateTime(2026, 10, 1) };

        Assert.Equal(["DateTo"], Validate(filter));
    }

    private static List<string> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames).Distinct().ToList();
    }
}
