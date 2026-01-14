using Order.Domain.Models;
using static Order.Domain.Models.Order;

namespace Order.Domain.Extensions;

public static class OrderExtensions
{
    public static IOrderEvent ToEvent(this IOrder order)
    {
        return order switch
        {
            OrderPlaced placed => new OrderPlacedEvent(
                placed.Restaurant.Value,
                placed.Phone.Value,
                placed.Address.Value,
                placed.Amount.Value,
                placed.PlacedAt
            ),
            InvalidOrder invalid => new OrderFailedEvent(
                invalid.Reason,
                DateTime.UtcNow
            ),
            _ => new OrderFailedEvent(
                $"Order ended in unexpected state: {order.GetType().Name}",
                DateTime.UtcNow
            )
        };
    }
}
