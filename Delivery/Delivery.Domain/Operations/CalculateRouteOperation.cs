using Delivery.Domain.Models;
using static Delivery.Domain.Models.Delivery;

namespace Delivery.Domain.Operations;

/// <summary>
/// Operation to optimize delivery route with time and distance estimates
/// </summary>
internal class OptimizeRouteOperation : DeliveryOperation
{
    protected override IDelivery OnAssigned(AssignedDelivery delivery)
    {
        // Calculate estimates for the assigned route
        decimal estimatedDistance = CalculateDistance(delivery.Destination.Value);
        TimeSpan estimatedTime = CalculateEstimatedTime(estimatedDistance);

        return new OptimizedDelivery(
            delivery.InvoiceRef,
            delivery.Destination,
            delivery.Driver,
            delivery.Route,
            estimatedDistance,
            estimatedTime
        );
    }

    private static decimal CalculateDistance(string address)
    {
        // Simplified distance calculation (in km)
        var random = new Random();
        return Math.Round((decimal)(random.NextDouble() * 15 + 2), 2); // 2-17 km
    }

    private static TimeSpan CalculateEstimatedTime(decimal distance)
    {
        // Assuming average speed of 30 km/h in city
        double hours = (double)distance / 30.0;
        return TimeSpan.FromHours(hours);
    }
}
