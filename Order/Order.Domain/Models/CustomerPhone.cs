using Order.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace Order.Domain.Models;

public record CustomerPhone
{
    public string Value { get; }

    private CustomerPhone(string value)
    {
        if (IsValid(value)) Value = value;
        else throw new InvalidOrderException("Invalid phone number: Must be 10 digits.");
    }

    public static CustomerPhone Create(string value) => new(value);

    public static bool TryParse(string input, out CustomerPhone? result)
    {
        result = null;
        if (IsValid(input))
        {
            result = new CustomerPhone(input);
            return true;
        }
        return false;
    }

    private static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        // Remove common formatting characters
        var cleaned = Regex.Replace(value, @"[\s\-\(\)]", "");
        return Regex.IsMatch(cleaned, @"^\d{10}$");
    }

    public override string ToString() => Value;
}
