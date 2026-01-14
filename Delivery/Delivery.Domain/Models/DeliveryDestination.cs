using Delivery.Domain.Exceptions;

namespace Delivery.Domain.Models;

public record DeliveryDestination
{
    public string Value { get; }

    private DeliveryDestination(string value)
    {
        if (IsValid(value)) Value = value;
        else throw new InvalidDeliveryException("Invalid delivery destination: Address cannot be empty.");
    }

    public static DeliveryDestination Create(string value) => new(value);

    private static bool IsValid(string value) => 
        !string.IsNullOrWhiteSpace(value) && value.Length >= 10;

    public override string ToString() => Value;
}
