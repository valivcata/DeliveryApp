using Order.Domain.Exceptions;
using static Order.Domain.Models.Order;

namespace Order.Domain.Operations;

internal abstract class OrderOperation<TState> : DomainOperation<IOrder, TState, IOrder>
    where TState : class
{
    public override IOrder Transform(IOrder order, TState? state) => order switch
    {
        UnvalidatedOrder unvalidated => OnUnvalidated(unvalidated, state),
        ValidatedOrder validated => OnValidated(validated, state),
        OrderPlaced placed => OnPlaced(placed, state),
        InvalidOrder invalid => OnInvalid(invalid, state),
        _ => throw new InvalidOrderException($"Invalid order state: {order.GetType().Name}")
    };

    protected virtual IOrder OnUnvalidated(UnvalidatedOrder order, TState? state) => order;

    protected virtual IOrder OnValidated(ValidatedOrder order, TState? state) => order;

    protected virtual IOrder OnPlaced(OrderPlaced order, TState? state) => order;

    protected virtual IOrder OnInvalid(InvalidOrder order, TState? state) => order;
}

internal abstract class OrderOperation : OrderOperation<object>
{
    internal IOrder Transform(IOrder order) => Transform(order, null);

    protected sealed override IOrder OnUnvalidated(UnvalidatedOrder order, object? state) => OnUnvalidated(order);

    protected virtual IOrder OnUnvalidated(UnvalidatedOrder order) => order;

    protected sealed override IOrder OnValidated(ValidatedOrder order, object? state) => OnValidated(order);

    protected virtual IOrder OnValidated(ValidatedOrder order) => order;

    protected sealed override IOrder OnPlaced(OrderPlaced order, object? state) => OnPlaced(order);

    protected virtual IOrder OnPlaced(OrderPlaced order) => order;

    protected sealed override IOrder OnInvalid(InvalidOrder order, object? state) => OnInvalid(order);

    protected virtual IOrder OnInvalid(InvalidOrder order) => order;
}
