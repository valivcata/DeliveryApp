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
                await _orderRepository.SaveAsync(order);
                await PublishToServiceBusAsync(placed);
            }

            return order.ToEvent();
        }
        catch (Exception ex)
        {
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

        order = new ValidateOrderOperation().Transform(order);
        order = new PlaceOrderOperation().Transform(order);

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
