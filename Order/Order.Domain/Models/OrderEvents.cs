namespace Order.Domain.Models;

public interface IOrderEvent { }

public record OrderPlacedEvent(
    string RestaurantId,
    string CustomerPhone,
    string DeliveryAddress,
    decimal OrderAmount,
    DateTime PlacedAt
) : IOrderEvent;

public record OrderFailedEvent(
    string Reason,
    DateTime FailedAt
) : IOrderEvent;
