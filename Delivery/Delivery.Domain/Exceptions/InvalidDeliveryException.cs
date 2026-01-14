namespace Delivery.Domain.Exceptions;

public class InvalidDeliveryException : Exception
{
    public InvalidDeliveryException(string message) : base(message) { }
}
