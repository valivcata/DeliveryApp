using Delivery.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace Delivery.Domain.Models;

public record DriverId
{
    public string Value { get; }

    private DriverId(string value)
    {
        if (IsValid(value)) Value = value.ToUpperInvariant();
        else throw new InvalidDeliveryException("Invalid driver ID: Must be in format DRV-XXXX where X is a digit.");
    }

    public static DriverId Create(string value) => new(value);

    public static DriverId CreateRandom()
    {
        var random = new Random();
        return new DriverId($"DRV-{random.Next(1000, 9999)}");
    }

    private static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Regex.IsMatch(value, @"^DRV-\d{4}$", RegexOptions.IgnoreCase);
    }

    public override string ToString() => Value;
}
