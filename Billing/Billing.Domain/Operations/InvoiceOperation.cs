using Billing.Domain.Exceptions;
using static Billing.Domain.Models.Invoice;

namespace Billing.Domain.Operations;

internal abstract class InvoiceOperation<TState> : DomainOperation<IInvoice, TState, IInvoice>
    where TState : class
{
    public override IInvoice Transform(IInvoice invoice, TState? state) => invoice switch
    {
        UnprocessedInvoice unprocessed => OnUnprocessed(unprocessed, state),
        CalculatedInvoice calculated => OnCalculated(calculated, state),
        InvoiceIssued issued => OnIssued(issued, state),
        InvalidInvoice invalid => OnInvalid(invalid, state),
        _ => throw new InvalidInvoiceException($"Invalid invoice state: {invoice.GetType().Name}")
    };

    protected virtual IInvoice OnUnprocessed(UnprocessedInvoice invoice, TState? state) => invoice;
    protected virtual IInvoice OnCalculated(CalculatedInvoice invoice, TState? state) => invoice;
    protected virtual IInvoice OnIssued(InvoiceIssued invoice, TState? state) => invoice;
    protected virtual IInvoice OnInvalid(InvalidInvoice invoice, TState? state) => invoice;
}

internal abstract class InvoiceOperation : InvoiceOperation<object>
{
    internal IInvoice Transform(IInvoice invoice) => Transform(invoice, null);

    protected sealed override IInvoice OnUnprocessed(UnprocessedInvoice invoice, object? state) => OnUnprocessed(invoice);
    protected virtual IInvoice OnUnprocessed(UnprocessedInvoice invoice) => invoice;

    protected sealed override IInvoice OnCalculated(CalculatedInvoice invoice, object? state) => OnCalculated(invoice);
    protected virtual IInvoice OnCalculated(CalculatedInvoice invoice) => invoice;

    protected sealed override IInvoice OnIssued(InvoiceIssued invoice, object? state) => OnIssued(invoice);
    protected virtual IInvoice OnIssued(InvoiceIssued invoice) => invoice;

    protected sealed override IInvoice OnInvalid(InvalidInvoice invoice, object? state) => OnInvalid(invoice);
    protected virtual IInvoice OnInvalid(InvalidInvoice invoice) => invoice;
}
