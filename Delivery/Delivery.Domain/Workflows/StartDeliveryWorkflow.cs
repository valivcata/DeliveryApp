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
                await _deliveryRepository.SaveAsync(delivery);
                await PublishToServiceBusAsync(started);
            }

            return delivery.ToEvent();
        }
        catch (Exception ex)
        {
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

        delivery = new AssignDeliveryOperation().Transform(delivery);
        delivery = new StartDeliveryOperation().Transform(delivery);

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
