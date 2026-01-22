using Delivery.Domain.Exceptions;
using static Delivery.Domain.Models.Delivery;

namespace Delivery.Domain.Operations;

internal abstract class DeliveryOperation<TState> : DomainOperation<IDelivery, TState, IDelivery>
    where TState : class
{
    public override IDelivery Transform(IDelivery delivery, TState? state) => delivery switch
    {
        RequestedDelivery requested => OnRequested(requested, state),
        AssignedDelivery assigned => OnAssigned(assigned, state),
        OptimizedDelivery optimized => OnOptimized(optimized, state),
        DeliveryStarted started => OnStarted(started, state),
        FailedDelivery failed => OnFailed(failed, state),
        _ => throw new InvalidDeliveryException($"Invalid delivery state: {delivery.GetType().Name}")
    };

    protected virtual IDelivery OnRequested(RequestedDelivery delivery, TState? state) => delivery;
    protected virtual IDelivery OnAssigned(AssignedDelivery delivery, TState? state) => delivery;
    protected virtual IDelivery OnOptimized(OptimizedDelivery delivery, TState? state) => delivery;
    protected virtual IDelivery OnStarted(DeliveryStarted delivery, TState? state) => delivery;
    protected virtual IDelivery OnFailed(FailedDelivery delivery, TState? state) => delivery;
}

internal abstract class DeliveryOperation : DeliveryOperation<object>
{
    internal IDelivery Transform(IDelivery delivery) => Transform(delivery, null);

    protected sealed override IDelivery OnRequested(RequestedDelivery delivery, object? state) => OnRequested(delivery);
    protected virtual IDelivery OnRequested(RequestedDelivery delivery) => delivery;

    protected sealed override IDelivery OnAssigned(AssignedDelivery delivery, object? state) => OnAssigned(delivery);
    protected virtual IDelivery OnAssigned(AssignedDelivery delivery) => delivery;

    protected sealed override IDelivery OnOptimized(OptimizedDelivery delivery, object? state) => OnOptimized(delivery);
    protected virtual IDelivery OnOptimized(OptimizedDelivery delivery) => delivery;

    protected sealed override IDelivery OnStarted(DeliveryStarted delivery, object? state) => OnStarted(delivery);
    protected virtual IDelivery OnStarted(DeliveryStarted delivery) => delivery;

    protected sealed override IDelivery OnFailed(FailedDelivery delivery, object? state) => OnFailed(delivery);
    protected virtual IDelivery OnFailed(FailedDelivery delivery) => delivery;
}
