using Delivery.Domain.Extensions;
using Delivery.Domain.Models;
using Delivery.Domain.Operations;
using Delivery.Domain.Repositories;
using static Delivery.Domain.Models.Delivery;

namespace Delivery.Domain.Workflows;

public class StartDeliveryWorkflow
{
    private readonly IEventSender _eventSender;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly string _topicName;

    public StartDeliveryWorkflow(
        IEventSender eventSender,
        IDeliveryRepository deliveryRepository,
        string topicName = "delivery-topic")
    {
        _eventSender = eventSender;
        _deliveryRepository = deliveryRepository;
        _topicName = topicName;
    }

    public async Task<IDeliveryEvent> ExecuteAsync(InvoiceIssuedMessageDto command)
    {
        try
        {
            IDelivery delivery = ExecuteBusinessLogic(command);

            if (delivery is DeliveryStarted started)
            {
                Console.WriteLine($"           ✓ Saving delivery to database...");
                await _deliveryRepository.SaveAsync(delivery);
                Console.WriteLine($"           ✓ Delivery saved successfully");
                
                Console.WriteLine($"[Step 6/7] 📤 Publishing to Service Bus topic: {_topicName}");
                await PublishToServiceBusAsync(started);
                Console.WriteLine($"           ✓ Event published to {_topicName}");
            }

            return delivery.ToEvent();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"           ❌ Workflow error: {ex.Message}");
            Console.ResetColor();
            return new DeliveryFailedEvent($"Unexpected error: {ex.Message}", DateTime.UtcNow);
        }
    }

    private static IDelivery ExecuteBusinessLogic(InvoiceIssuedMessageDto command)
    {
        IDelivery delivery = new RequestedDelivery(
            command.RestaurantId,
            command.CustomerPhone,
            command.DeliveryAddress,
            command.Total
        );

        Console.WriteLine($"           → State: RequestedDelivery");
        
        Console.WriteLine($"           → Running AssignDeliveryOperation...");
        delivery = new AssignDeliveryOperation().Transform(delivery);
        Console.WriteLine($"           → State: {delivery.GetType().Name}");
        
        if (delivery is FailedDelivery failed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"           ❌ Assignment failed: {failed.Reason}");
            Console.ResetColor();
            return delivery;
        }
        
        Console.WriteLine($"           → Running OptimizeRouteOperation...");
        delivery = new OptimizeRouteOperation().Transform(delivery);
        Console.WriteLine($"           → State: {delivery.GetType().Name}");
        
        if (delivery is FailedDelivery failedRoute)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"           ❌ Route optimization failed: {failedRoute.Reason}");
            Console.ResetColor();
            return delivery;
        }
        
        Console.WriteLine($"           → Running StartDeliveryOperation...");
        delivery = new StartDeliveryOperation().Transform(delivery);
        Console.WriteLine($"           → State: {delivery.GetType().Name}");

        return delivery;
    }

    private async Task PublishToServiceBusAsync(DeliveryStarted started)
    {
        var deliveryMessage = new
        {
            RestaurantId = started.InvoiceRef.RestaurantId,
            CustomerPhone = started.InvoiceRef.CustomerPhone,
            DriverId = started.Driver.Value,
            Route = started.Route.Value,
            StartedAt = started.StartedAt
        };

        await _eventSender.SendAsync(_topicName, deliveryMessage);
    }
}
