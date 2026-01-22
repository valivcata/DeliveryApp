using Order.Domain.Extensions;
using Order.Domain.Models;
using Order.Domain.Operations;
using Order.Domain.Repositories;
using static Order.Domain.Models.Order;

namespace Order.Domain.Workflows;

public class PlaceOrderWorkflow
{
    private readonly IEventSender _eventSender;
    private readonly IOrderRepository _orderRepository;
    private readonly string _topicName;

    public PlaceOrderWorkflow(
        IEventSender eventSender,
        IOrderRepository orderRepository,
        string topicName = "order-topic")
    {
        _eventSender = eventSender;
        _orderRepository = orderRepository;
        _topicName = topicName;
    }

    public async Task<IOrderEvent> ExecuteAsync(PlaceOrderDto command)
    {
        try
        {
            IOrder order = ExecuteBusinessLogic(command);

            if (order is OrderPlaced placed)
            {
                Console.WriteLine($"           ✓ Saving order to database...");
                await _orderRepository.SaveAsync(order);
                Console.WriteLine($"           ✓ Order saved successfully");
                
                Console.WriteLine($"[Step 3/4] 📤 Publishing to Service Bus topic: {_topicName}");
                await PublishToServiceBusAsync(placed);
                Console.WriteLine($"           ✓ Event published to {_topicName}");
            }

            return order.ToEvent();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"           ❌ Workflow error: {ex.Message}");
            Console.ResetColor();
            return new OrderFailedEvent($"Unexpected error: {ex.Message}", DateTime.UtcNow);
        }
    }

    private static IOrder ExecuteBusinessLogic(PlaceOrderDto command)
    {
        IOrder order = new UnvalidatedOrder(
            command.RestaurantId,
            command.CustomerPhone,
            command.DeliveryAddress,
            command.OrderAmount
        );

        Console.WriteLine($"           → State: UnvalidatedOrder");
        
        Console.WriteLine($"           → Running ValidateOrderOperation...");
        order = new ValidateOrderOperation().Transform(order);
        Console.WriteLine($"           → State: {order.GetType().Name}");
        
        if (order is InvalidOrder invalid)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"           ❌ Validation failed: {invalid.Reason}");
            Console.ResetColor();
            return order;
        }
        
        Console.WriteLine($"           → Running EnrichOrderOperation...");
        order = new EnrichOrderOperation().Transform(order);
        Console.WriteLine($"           → State: {order.GetType().Name}");
        
        Console.WriteLine($"           → Running PlaceOrderOperation...");
        order = new PlaceOrderOperation().Transform(order);
        Console.WriteLine($"           → State: {order.GetType().Name}");

        return order;
    }

    private async Task PublishToServiceBusAsync(OrderPlaced placed)
    {
        var orderMessage = new
        {
            RestaurantId = placed.Restaurant.Value,
            CustomerPhone = placed.Phone.Value,
            DeliveryAddress = placed.Address.Value,
            OrderAmount = placed.Amount.Value,
            PlacedAt = placed.PlacedAt
        };

        await _eventSender.SendAsync(_topicName, orderMessage);
    }
}
