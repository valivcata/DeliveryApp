namespace Delivery.Domain.Models;

public static class Delivery
{
    public interface IDelivery { }

    public record RequestedDelivery(
        string RestaurantId,
        string CustomerPhone,
        string DeliveryAddress,
        decimal InvoiceTotal
    ) : IDelivery;

    public record AssignedDelivery(
        InvoiceReference InvoiceRef,
        DeliveryDestination Destination,
        DriverId Driver,
        DeliveryRoute Route
    ) : IDelivery;

    public record DeliveryStarted(
        InvoiceReference InvoiceRef,
        DeliveryDestination Destination,
        DriverId Driver,
        DeliveryRoute Route,
        DateTime StartedAt
    ) : IDelivery;

    public record FailedDelivery(string Reason) : IDelivery;
}
