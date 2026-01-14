namespace Order.Domain.Models;

public static class Order
{
    public interface IOrder { }

    public record UnvalidatedOrder(
        string RestaurantId,
        string CustomerPhone,
        string DeliveryAddress,
        decimal OrderAmount
    ) : IOrder;

    public record ValidatedOrder(
        RestaurantId Restaurant,
        CustomerPhone Phone,
        DeliveryAddress Address,
        OrderAmount Amount
    ) : IOrder;

    public record OrderPlaced(
        RestaurantId Restaurant,
        CustomerPhone Phone,
        DeliveryAddress Address,
        OrderAmount Amount,
        DateTime PlacedAt
    ) : IOrder;

    public record InvalidOrder(string Reason) : IOrder;
}
