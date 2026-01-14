using Order.Domain.Exceptions;

namespace Order.Domain.Models;

public record OrderAmount
{
    public decimal Value { get; }

    private OrderAmount(decimal value)
    {
        if (IsValid(value)) Value = value;
        else throw new InvalidOrderException("Invalid order amount: Amount must be greater than 0.");
    }

    public static OrderAmount Create(decimal value) => new(value);

    public static bool TryParse(decimal input, out OrderAmount? result)
    {
        result = null;
        if (IsValid(input))
        {
            result = new OrderAmount(input);
            return true;
        }
        return false;
    }

    public static bool TryParse(string input, out OrderAmount? result)
    {
        result = null;
        if (decimal.TryParse(input, out var decimalValue) && IsValid(decimalValue))
        {
            result = new OrderAmount(decimalValue);
            return true;
        }
        return false;
    }

    private static bool IsValid(decimal value) => value > 0;

    public override string ToString() => Value.ToString("C");
}
