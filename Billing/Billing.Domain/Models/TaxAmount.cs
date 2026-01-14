using Billing.Domain.Exceptions;

namespace Billing.Domain.Models;

public record TaxAmount
{
    public decimal Value { get; }

    private TaxAmount(decimal value)
    {
        if (IsValid(value)) Value = value;
        else throw new InvalidInvoiceException("Invalid tax amount: Tax must be 0 or greater.");
    }

    public static TaxAmount Create(decimal value) => new(value);

    private static bool IsValid(decimal value) => value >= 0;

    public override string ToString() => Value.ToString("C");
}
