using Billing.Domain.Models;
using static Billing.Domain.Models.Invoice;

namespace Billing.Domain.Extensions;

public static class InvoiceExtensions
{
    public static IInvoiceEvent ToEvent(this IInvoice invoice)
    {
        return invoice switch
        {
            InvoiceIssued issued => new InvoiceIssuedEvent(
                issued.OrderRef.RestaurantId,
                issued.OrderRef.CustomerPhone,
                issued.Amount.Value,
                issued.Tax.Value,
                issued.Total.Value,
                issued.IssuedAt
            ),
            InvalidInvoice invalid => new InvoiceFailedEvent(
                invalid.Reason,
                DateTime.UtcNow
            ),
            _ => new InvoiceFailedEvent(
                $"Invoice ended in unexpected state: {invoice.GetType().Name}",
                DateTime.UtcNow
            )
        };
    }
}
