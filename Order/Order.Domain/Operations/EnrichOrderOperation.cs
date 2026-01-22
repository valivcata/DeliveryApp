using Order.Domain.Models;
using static Order.Domain.Models.Order;

namespace Order.Domain.Operations;

/// <summary>
/// Operation to enrich validated order with additional metadata
/// </summary>
internal class EnrichOrderOperation : OrderOperation
{
    protected override IOrder OnValidated(ValidatedOrder order)
    {
        // Enrich the order with additional information
        // In real scenario, this could fetch restaurant info, calculate delivery time, etc.
        return new EnrichedOrder(
            order.Restaurant,
            order.Phone,
            order.Address,
            order.Amount,
            DateTime.UtcNow, // OrderDate
            CalculateEstimatedDeliveryTime(), // EstimatedDeliveryTime
            GenerateOrderReference() // OrderReference
        );
    }

    private static DateTime CalculateEstimatedDeliveryTime()
    {
        // Simple calculation: 30-60 minutes from now
        var random = new Random();
        return DateTime.UtcNow.AddMinutes(random.Next(30, 61));
    }

    private static string GenerateOrderReference()
    {
        // Generate a unique order reference
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
