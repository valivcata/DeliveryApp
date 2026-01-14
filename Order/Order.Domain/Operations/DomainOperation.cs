namespace Order.Domain.Operations;

public abstract class DomainOperation<TEntity, TState, TResult>
    where TState : class
{
    public abstract TResult Transform(TEntity entity, TState? state);
}
