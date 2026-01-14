using Delivery.Domain.Models;
using static Delivery.Domain.Models.Delivery;

namespace Delivery.Domain.Extensions;

public static class DeliveryExtensions
{
    public static IDeliveryEvent ToEvent(this IDelivery delivery)
    {
        return delivery switch
        {
            DeliveryStarted started => new DeliveryStartedEvent(
                started.InvoiceRef.RestaurantId,
                started.InvoiceRef.CustomerPhone,
                started.Driver.Value,
                started.Route.Value,
                started.StartedAt
            ),
            FailedDelivery failed => new DeliveryFailedEvent(
                failed.Reason,
                DateTime.UtcNow
            ),
            _ => new DeliveryFailedEvent(
                $"Delivery ended in unexpected state: {delivery.GetType().Name}",
                DateTime.UtcNow
            )
        };
    }
}
