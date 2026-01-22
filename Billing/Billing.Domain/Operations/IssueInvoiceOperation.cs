using static Billing.Domain.Models.Invoice;

namespace Billing.Domain.Operations;

internal class IssueInvoiceOperation : InvoiceOperation
{
    protected override IInvoice OnValidated(ValidatedInvoice invoice)
    {
        return new InvoiceIssued(
            invoice.OrderRef,
            invoice.Amount,
            invoice.Tax,
            invoice.Total,
            DateTime.UtcNow
        );
    }
}
