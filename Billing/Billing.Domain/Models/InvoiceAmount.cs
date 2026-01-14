using Billing.Domain.Exceptions;

namespace Billing.Domain.Models;

public record InvoiceAmount
{
    public decimal Value { get; }

    private InvoiceAmount(decimal value)
    {
        if (IsValid(value)) Value = value;
        else throw new InvalidInvoiceException("Invalid invoice amount: Amount must be greater than 0.");
    }

    public static InvoiceAmount Create(decimal value) => new(value);

    public static bool TryParse(decimal input, out InvoiceAmount? result)
    {
        result = null;
        if (IsValid(input))
        {
            result = new InvoiceAmount(input);
            return true;
        }
        return false;
    }

    private static bool IsValid(decimal value) => value > 0;

    public override string ToString() => Value.ToString("C");
}
