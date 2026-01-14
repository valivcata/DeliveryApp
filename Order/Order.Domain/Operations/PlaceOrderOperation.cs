using static Order.Domain.Models.Order;

namespace Order.Domain.Operations;

internal class PlaceOrderOperation : OrderOperation
{
    protected override IOrder OnValidated(ValidatedOrder order)
    {
        return new OrderPlaced(
            order.Restaurant,
            order.Phone,
            order.Address,
            order.Amount,
            DateTime.UtcNow
        );
    }
}
