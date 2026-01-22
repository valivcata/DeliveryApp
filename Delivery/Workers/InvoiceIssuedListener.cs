using Delivery.Domain.Models;
using Delivery.Domain.Workflows;
using Delivery.Events.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Delivery.Workers;

public class InvoiceIssuedListener : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ServiceBusSettings _settings;
    private readonly ILogger<InvoiceIssuedListener> _logger;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public InvoiceIssuedListener(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<ServiceBusSettings> settings,
        ILogger<InvoiceIssuedListener> logger)
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
        
        try
        {
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

            _logger.LogInformation("Starting to listen for invoice events on topic: {TopicName}, subscription: {Subscription}",
                _settings.TopicName, _settings.SubscriptionName);

            await _processor.StartProcessingAsync(stoppingToken);

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown, ignore
            _logger.LogInformation("Service Bus listener shutting down gracefully...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FATAL ERROR starting Service Bus listener");
            throw;
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("  DELIVERY SERVICE - Processing Invoice");
        Console.WriteLine(new string('=', 60));
        Console.ResetColor();
        
        _logger.LogInformation("[STEP 1/7] Received message: {MessageId}", messageId);
        
        try
        {
            // Check for idempotency - skip if already processed
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Delivery.Data.DeliveryDbContext>();
            
            _logger.LogInformation("[STEP 2/7] Checking idempotency for message {MessageId}...", messageId);
            
            var alreadyProcessed = await dbContext.ProcessedMessages
                .AnyAsync(m => m.MessageId == messageId);
            
            if (alreadyProcessed)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠ Message already processed, skipping");
                Console.ResetColor();
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            var body = args.Message.Body.ToString();
            var invoiceData = JsonSerializer.Deserialize<InvoiceIssuedMessageDto>(body);
            
            if (invoiceData == null)
            {
                _logger.LogError("Failed to deserialize invoice message {MessageId}", messageId);
                await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed", "Unable to deserialize message body");
                return;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  Restaurant: {invoiceData.RestaurantId}");
            Console.WriteLine($"  Customer:   {invoiceData.CustomerPhone}");
            Console.WriteLine($"  Address:    {invoiceData.DeliveryAddress}");
            Console.WriteLine($"  Total:      ${invoiceData.Total:F2}");
            Console.ResetColor();
            
            _logger.LogInformation("[STEP 3/7] Assigning delivery driver...");
            _logger.LogInformation("[STEP 4/7] Validating destination...");
            _logger.LogInformation("[STEP 5/7] Optimizing delivery route...");
            
            var workflow = scope.ServiceProvider.GetRequiredService<StartDeliveryWorkflow>();
            var result = await workflow.ExecuteAsync(invoiceData);
            
            _logger.LogInformation("[STEP 6/7] Starting delivery...");

            // Record as processed
            dbContext.ProcessedMessages.Add(new Delivery.Data.Models.ProcessedMessageEntity
            {
                MessageId = messageId,
                ProcessedAt = DateTime.UtcNow,
                ProcessorName = nameof(InvoiceIssuedListener)
            });
            await dbContext.SaveChangesAsync();
            
            _logger.LogInformation("[STEP 7/7] Publishing DeliveryStarted event to delivery-topic...");

            if (result is DeliveryStartedEvent started)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n  ✓ Delivery started successfully!");
                Console.WriteLine($"    Driver: {started.DriverId}");
                Console.WriteLine($"    Route:  {started.Route}");
                Console.WriteLine(new string('=', 60) + "\n");
                Console.ResetColor();
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for message {MessageId}", messageId);
            await args.DeadLetterMessageAsync(args.Message, "JsonError", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error processing message {MessageId}", messageId);
            await args.DeadLetterMessageAsync(args.Message, "BusinessLogicError", ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error processing message {MessageId}, will retry", messageId);
            await args.AbandonMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message {MessageId}", messageId);
            
            // Check delivery count to avoid infinite retries
            if (args.Message.DeliveryCount >= 3)
            {
                _logger.LogError("Message {MessageId} exceeded max delivery attempts, moving to DLQ", messageId);
                await args.DeadLetterMessageAsync(args.Message, "MaxRetriesExceeded", ex.Message);
            }
            else
            {
                await args.AbandonMessageAsync(args.Message);
            }
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
