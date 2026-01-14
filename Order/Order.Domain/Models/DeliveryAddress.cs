using Order.Domain.Exceptions;

namespace Order.Domain.Models;

public record DeliveryAddress
{
    public string Value { get; }

    private DeliveryAddress(string value)
    {
        if (IsValid(value)) Value = value;
        else throw new InvalidOrderException("Invalid delivery address: Address cannot be empty.");
    }

    public static DeliveryAddress Create(string value) => new(value);

    public static bool TryParse(string input, out DeliveryAddress? result)
    {
        result = null;
        if (IsValid(input))
        {
            result = new DeliveryAddress(input);
            return true;
        }
        return false;
    }

    private static bool IsValid(string value) => 
        !string.IsNullOrWhiteSpace(value) && value.Length >= 10;

    public override string ToString() => Value;
}
