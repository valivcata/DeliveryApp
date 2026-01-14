namespace Delivery.Domain.Repositories;

public interface IEventSender
{
    Task SendAsync<T>(string topicName, T @event);
}
