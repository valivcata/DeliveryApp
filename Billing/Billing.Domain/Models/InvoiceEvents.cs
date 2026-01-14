namespace Billing.Domain.Models;

public interface IInvoiceEvent { }

public record InvoiceIssuedEvent(
    string RestaurantId,
    string CustomerPhone,
    decimal Amount,
    decimal Tax,
    decimal Total,
    DateTime IssuedAt
) : IInvoiceEvent;

public record InvoiceFailedEvent(
    string Reason,
    DateTime FailedAt
) : IInvoiceEvent;
