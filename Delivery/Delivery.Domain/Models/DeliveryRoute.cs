using Delivery.Domain.Exceptions;

namespace Delivery.Domain.Models;

public record DeliveryRoute
{
    public string Value { get; }

    private DeliveryRoute(string value)
    {
        if (IsValid(value)) Value = value;
        else throw new InvalidDeliveryException("Invalid delivery route: Route cannot be empty.");
    }

    public static DeliveryRoute Create(string value) => new(value);

    public static DeliveryRoute CreateOptimized(string destination)
    {
        // Simple route generation - in real app would integrate with mapping API
        return new DeliveryRoute($"Route to: {destination}");
    }

    private static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value);

    public override string ToString() => Value;
}
