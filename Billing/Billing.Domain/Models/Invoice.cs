namespace Billing.Domain.Models;

public static class Invoice
{
    public interface IInvoice { }

    public record UnprocessedInvoice(
        string OrderRestaurantId,
        string OrderCustomerPhone,
        decimal OrderAmount
    ) : IInvoice;

    public record CalculatedInvoice(
        OrderReference OrderRef,
        InvoiceAmount Amount,
        TaxAmount Tax,
        TotalAmount Total
    ) : IInvoice;

    public record ValidatedInvoice(
        OrderReference OrderRef,
        InvoiceAmount Amount,
        TaxAmount Tax,
        TotalAmount Total
    ) : IInvoice;

    public record InvoiceIssued(
        OrderReference OrderRef,
        InvoiceAmount Amount,
        TaxAmount Tax,
        TotalAmount Total,
        DateTime IssuedAt
    ) : IInvoice;

    public record FailedInvoice(string Reason) : IInvoice;

    public record InvalidInvoice(string Reason) : IInvoice;
}
