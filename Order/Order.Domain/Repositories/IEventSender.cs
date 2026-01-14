namespace Order.Domain.Repositories;

public interface IEventSender
{
    Task SendAsync<T>(string topicName, T @event);
}
