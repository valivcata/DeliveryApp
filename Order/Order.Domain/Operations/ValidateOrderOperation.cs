using Order.Domain.Models;
using Order.Domain.Exceptions;
using static Order.Domain.Models.Order;

namespace Order.Domain.Operations;

internal class ValidateOrderOperation : OrderOperation
{
    protected override IOrder OnUnvalidated(UnvalidatedOrder order)
    {
        List<string> validationErrors = [];
        
        RestaurantId? restaurantId = ValidateRestaurantId(order.RestaurantId, validationErrors);
        CustomerPhone? phone = ValidateCustomerPhone(order.CustomerPhone, validationErrors);
        DeliveryAddress? address = ValidateDeliveryAddress(order.DeliveryAddress, validationErrors);
        OrderAmount? amount = ValidateOrderAmount(order.OrderAmount, validationErrors);

        if (validationErrors.Count > 0)
        {
            return new InvalidOrder(string.Join("; ", validationErrors));
        }

        return new ValidatedOrder(restaurantId!, phone!, address!, amount!);
    }

    private static RestaurantId? ValidateRestaurantId(string restaurantId, List<string> validationErrors)
    {
        try
        {
            return RestaurantId.Create(restaurantId);
        }
        catch (InvalidOrderException ex)
        {
            validationErrors.Add(ex.Message);
            return null;
        }
    }

    private static CustomerPhone? ValidateCustomerPhone(string phone, List<string> validationErrors)
    {
        try
        {
            return CustomerPhone.Create(phone);
        }
        catch (InvalidOrderException ex)
        {
            validationErrors.Add(ex.Message);
            return null;
        }
    }

    private static DeliveryAddress? ValidateDeliveryAddress(string address, List<string> validationErrors)
    {
        try
        {
            return DeliveryAddress.Create(address);
        }
        catch (InvalidOrderException ex)
        {
            validationErrors.Add(ex.Message);
            return null;
        }
    }

    private static OrderAmount? ValidateOrderAmount(decimal amount, List<string> validationErrors)
    {
        try
        {
            return OrderAmount.Create(amount);
        }
        catch (InvalidOrderException ex)
        {
            validationErrors.Add(ex.Message);
            return null;
        }
    }
}
