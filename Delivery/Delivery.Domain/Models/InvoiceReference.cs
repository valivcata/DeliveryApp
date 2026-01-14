using Delivery.Domain.Exceptions;

namespace Delivery.Domain.Models;

public record InvoiceReference
{
    public string RestaurantId { get; }
    public string CustomerPhone { get; }

    private InvoiceReference(string restaurantId, string customerPhone)
    {
        if (IsValid(restaurantId, customerPhone))
        {
            RestaurantId = restaurantId;
            CustomerPhone = customerPhone;
        }
        else throw new InvalidDeliveryException("Invalid invoice reference: Restaurant ID and customer phone cannot be empty.");
    }

    public static InvoiceReference Create(string restaurantId, string customerPhone) => 
        new(restaurantId, customerPhone);

    private static bool IsValid(string restaurantId, string customerPhone) =>
        !string.IsNullOrWhiteSpace(restaurantId) && !string.IsNullOrWhiteSpace(customerPhone);

    public override string ToString() => $"{RestaurantId}/{CustomerPhone}";
}
