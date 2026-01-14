using Billing.Domain.Exceptions;

namespace Billing.Domain.Models;

public record TotalAmount
{
    public decimal Value { get; }

    private TotalAmount(decimal value)
    {
        if (IsValid(value)) Value = value;
        else throw new InvalidInvoiceException("Invalid total amount: Total must be greater than 0.");
    }

    public static TotalAmount Create(decimal value) => new(value);

    private static bool IsValid(decimal value) => value > 0;

    public override string ToString() => Value.ToString("C");
}
