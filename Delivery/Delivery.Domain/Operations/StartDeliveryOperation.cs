using static Delivery.Domain.Models.Delivery;

namespace Delivery.Domain.Operations;

internal class StartDeliveryOperation : DeliveryOperation
{
    protected override IDelivery OnAssigned(AssignedDelivery delivery)
    {
        return new DeliveryStarted(
            delivery.InvoiceRef,
            delivery.Destination,
            delivery.Driver,
            delivery.Route,
            DateTime.UtcNow
        );
    }
}
