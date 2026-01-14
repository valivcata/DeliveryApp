using Delivery.Domain.Models;
using Delivery.Domain.Exceptions;
using static Delivery.Domain.Models.Delivery;

namespace Delivery.Domain.Operations;

internal class AssignDeliveryOperation : DeliveryOperation
{
    protected override IDelivery OnRequested(RequestedDelivery delivery)
    {
        List<string> validationErrors = [];

        InvoiceReference? invoiceRef = ValidateInvoiceReference(
            delivery.RestaurantId, 
            delivery.CustomerPhone, 
            validationErrors);
        
        DeliveryDestination? destination = ValidateDestination(delivery.DeliveryAddress, validationErrors);

        if (validationErrors.Count > 0)
        {
            return new FailedDelivery(string.Join("; ", validationErrors));
        }

        // Assign driver and calculate route
        var driver = DriverId.CreateRandom();
        var route = DeliveryRoute.CreateOptimized(destination!.Value);

        return new AssignedDelivery(invoiceRef!, destination!, driver, route);
    }

    private static InvoiceReference? ValidateInvoiceReference(
        string restaurantId, 
        string customerPhone, 
        List<string> validationErrors)
    {
        try
        {
            return InvoiceReference.Create(restaurantId, customerPhone);
        }
        catch (InvalidDeliveryException ex)
        {
            validationErrors.Add(ex.Message);
            return null;
        }
    }

    private static DeliveryDestination? ValidateDestination(string address, List<string> validationErrors)
    {
        try
        {
            return DeliveryDestination.Create(address);
        }
        catch (InvalidDeliveryException ex)
        {
            validationErrors.Add(ex.Message);
            return null;
        }
    }
}
