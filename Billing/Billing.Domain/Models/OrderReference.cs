using Billing.Domain.Exceptions;

namespace Billing.Domain.Models;

public record OrderReference
{
    public string RestaurantId { get; }
    public string CustomerPhone { get; }

    private OrderReference(string restaurantId, string customerPhone)
    {
        if (IsValid(restaurantId, customerPhone))
        {
            RestaurantId = restaurantId;
            CustomerPhone = customerPhone;
        }
        else throw new InvalidInvoiceException("Invalid order reference: Restaurant ID and customer phone cannot be empty.");
    }

    public static OrderReference Create(string restaurantId, string customerPhone) => 
        new(restaurantId, customerPhone);

    private static bool IsValid(string restaurantId, string customerPhone) =>
        !string.IsNullOrWhiteSpace(restaurantId) && !string.IsNullOrWhiteSpace(customerPhone);

    public override string ToString() => $"{RestaurantId}/{CustomerPhone}";
}
