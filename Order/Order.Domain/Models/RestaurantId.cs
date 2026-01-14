using Order.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace Order.Domain.Models;

public record RestaurantId
{
    public string Value { get; }

    private RestaurantId(string value)
    {
        if (IsValid(value)) Value = value.ToUpperInvariant();
        else throw new InvalidOrderException("Invalid restaurant ID: Must be in format REST-XXXX where X is a digit.");
    }

    public static RestaurantId Create(string value) => new(value);

    public static bool TryParse(string input, out RestaurantId? result)
    {
        result = null;
        if (IsValid(input))
        {
            result = new RestaurantId(input);
            return true;
        }
        return false;
    }

    private static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Regex.IsMatch(value, @"^REST-\d{4}$", RegexOptions.IgnoreCase);
    }

    public override string ToString() => Value;
}
