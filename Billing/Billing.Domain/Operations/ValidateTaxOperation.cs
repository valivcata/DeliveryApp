using Billing.Domain.Models;
using static Billing.Domain.Models.Invoice;

namespace Billing.Domain.Operations;

/// <summary>
/// Operation to validate tax calculations and compliance
/// </summary>
internal class ValidateTaxOperation : InvoiceOperation
{
    protected override IInvoice OnCalculated(CalculatedInvoice invoice)
    {
        // Validate that tax is within expected ranges
        // In real scenario, this could check tax jurisdiction rules, exemptions, etc.
        
        decimal taxRate = invoice.Tax.Value / invoice.Amount.Value;
        
        // Ensure tax rate is reasonable (between 0% and 30%)
        if (taxRate < 0 || taxRate > 0.30m)
        {
            return new FailedInvoice($"Invalid tax rate: {taxRate:P2}. Expected between 0% and 30%.");
        }

        // Return as validated invoice ready for issuing
        return new ValidatedInvoice(
            invoice.OrderRef,
            invoice.Amount,
            invoice.Tax,
            invoice.Total
        );
    }
}
