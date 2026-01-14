using Billing.Domain.Models;
using Billing.Domain.Exceptions;
using static Billing.Domain.Models.Invoice;

namespace Billing.Domain.Operations;

internal class CalculateInvoiceOperation : InvoiceOperation
{
    private const decimal TaxRate = 0.10m; // 10% tax

    protected override IInvoice OnUnprocessed(UnprocessedInvoice invoice)
    {
        List<string> validationErrors = [];

        OrderReference? orderRef = ValidateOrderReference(
            invoice.OrderRestaurantId, 
            invoice.OrderCustomerPhone, 
            validationErrors);
        
        InvoiceAmount? amount = ValidateAmount(invoice.OrderAmount, validationErrors);

        if (validationErrors.Count > 0)
        {
            return new InvalidInvoice(string.Join("; ", validationErrors));
        }

        var tax = TaxAmount.Create(amount!.Value * TaxRate);
        var total = TotalAmount.Create(amount!.Value + tax.Value);

        return new CalculatedInvoice(orderRef!, amount!, tax, total);
    }

    private static OrderReference? ValidateOrderReference(
        string restaurantId, 
        string customerPhone, 
        List<string> validationErrors)
    {
        try
        {
            return OrderReference.Create(restaurantId, customerPhone);
        }
        catch (InvalidInvoiceException ex)
        {
            validationErrors.Add(ex.Message);
            return null;
        }
    }

    private static InvoiceAmount? ValidateAmount(decimal amount, List<string> validationErrors)
    {
        try
        {
            return InvoiceAmount.Create(amount);
        }
        catch (InvalidInvoiceException ex)
        {
            validationErrors.Add(ex.Message);
            return null;
        }
    }
}
