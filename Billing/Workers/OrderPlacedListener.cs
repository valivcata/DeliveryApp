using Billing.Domain.Models;
using Billing.Domain.Workflows;
using Billing.Events.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Billing.Workers;

public class OrderPlacedListener : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ServiceBusSettings _settings;
    private readonly ILogger<OrderPlacedListener> _logger;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public OrderPlacedListener(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<ServiceBusSettings> settings,
        ILogger<OrderPlacedListener> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _settings.ConnectionString;
        
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Service Bus connection string not configured. Listener will not start.");
            return;
        }
        
        _client = new ServiceBusClient(connectionString);
        
        _processor = _client.CreateProcessor(
            _settings.TopicName,
            _settings.SubscriptionName,
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _logger.LogInformation("Starting to listen for order events on topic: {TopicName}, subscription: {Subscription}",
            _settings.TopicName, _settings.SubscriptionName);

        await _processor.StartProcessingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToString();
            var orderData = JsonSerializer.Deserialize<OrderPlacedMessageDto>(body);
            
            if (orderData == null)
            {
                _logger.LogError("Failed to deserialize order message");
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var workflow = scope.ServiceProvider.GetRequiredService<IssueInvoiceWorkflow>();
            var result = await workflow.ExecuteAsync(orderData);

            _logger.LogInformation("Invoice processed for order from restaurant {RestaurantId} - Status: {Status}",
                orderData.RestaurantId, result.GetType().Name);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order message");
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error in Service Bus processor: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        if (_processor != null)
        {
            _logger.LogInformation("Stopping Service Bus processor...");
            await _processor.StopProcessingAsync(stoppingToken);
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(stoppingToken);
    }
}
