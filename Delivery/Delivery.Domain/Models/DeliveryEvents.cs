namespace Delivery.Domain.Models;

public interface IDeliveryEvent { }

public record DeliveryStartedEvent(
    string RestaurantId,
    string CustomerPhone,
    string DriverId,
    string Route,
    DateTime StartedAt
) : IDeliveryEvent;

public record DeliveryFailedEvent(
    string Reason,
    DateTime FailedAt
) : IDeliveryEvent;
