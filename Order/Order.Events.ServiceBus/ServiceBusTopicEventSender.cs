using Order.Domain.Repositories;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Order.Events.ServiceBus;

public class ServiceBusTopicEventSender : IEventSender, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusTopicEventSender> _logger;

    public ServiceBusTopicEventSender(
        IOptions<ServiceBusSettings> settings,
        ILogger<ServiceBusTopicEventSender> logger)
    {
        _client = new ServiceBusClient(settings.Value.ConnectionString);
        _logger = logger;
    }

    public async Task SendAsync<T>(string topicName, T @event)
    {
        var sender = _client.CreateSender(topicName);
        
        try
        {
            var messageBody = JsonSerializer.Serialize(@event);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                Subject = typeof(T).Name
            };

            await sender.SendMessageAsync(serviceBusMessage);
            _logger.LogInformation("Published message to topic {TopicName}: {MessageType}", 
                topicName, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to topic {TopicName}", topicName);
            throw;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}
