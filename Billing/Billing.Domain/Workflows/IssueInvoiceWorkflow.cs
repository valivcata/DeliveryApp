using Billing.Domain.Extensions;
using Billing.Domain.Models;
using Billing.Domain.Operations;
using Billing.Domain.Repositories;
using static Billing.Domain.Models.Invoice;

namespace Billing.Domain.Workflows;

public class IssueInvoiceWorkflow
{
    private readonly IEventSender _eventSender;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly string _topicName;

    public IssueInvoiceWorkflow(
        IEventSender eventSender,
        IInvoiceRepository invoiceRepository,
        string topicName = "billing-topic")
    {
        _eventSender = eventSender;
        _invoiceRepository = invoiceRepository;
        _topicName = topicName;
    }

    public async Task<IInvoiceEvent> ExecuteAsync(OrderPlacedMessageDto command)
    {
        try
        {
            IInvoice invoice = ExecuteBusinessLogic(command);

            if (invoice is InvoiceIssued issued)
            {
                Console.WriteLine($"           ✓ Saving invoice to database...");
                await _invoiceRepository.SaveAsync(invoice);
                Console.WriteLine($"           ✓ Invoice saved successfully");
                
                Console.WriteLine($"[Step 5/6] 📤 Publishing to Service Bus topic: {_topicName}");
                await PublishToServiceBusAsync(issued, command.DeliveryAddress);
                Console.WriteLine($"           ✓ Event published to {_topicName}");
            }

            return invoice.ToEvent();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"           ❌ Workflow error: {ex.Message}");
            Console.ResetColor();
            return new InvoiceFailedEvent($"Unexpected error: {ex.Message}", DateTime.UtcNow);
        }
    }

    private static IInvoice ExecuteBusinessLogic(OrderPlacedMessageDto command)
    {
        IInvoice invoice = new UnprocessedInvoice(
            command.RestaurantId,
            command.CustomerPhone,
            command.OrderAmount
        );

        Console.WriteLine($"           → State: UnprocessedInvoice");
        
        Console.WriteLine($"           → Running CalculateInvoiceOperation...");
        invoice = new CalculateInvoiceOperation().Transform(invoice);
        Console.WriteLine($"           → State: {invoice.GetType().Name}");
        
        if (invoice is InvalidInvoice invalid)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"           ❌ Calculation failed: {invalid.Reason}");
            Console.ResetColor();
            return invoice;
        }
        
        Console.WriteLine($"           → Running ValidateTaxOperation...");
        invoice = new ValidateTaxOperation().Transform(invoice);
        Console.WriteLine($"           → State: {invoice.GetType().Name}");
        
        if (invoice is InvalidInvoice invalidTax)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"           ❌ Tax validation failed: {invalidTax.Reason}");
            Console.ResetColor();
            return invoice;
        }
        
        Console.WriteLine($"           → Running IssueInvoiceOperation...");
        invoice = new IssueInvoiceOperation().Transform(invoice);
        Console.WriteLine($"           → State: {invoice.GetType().Name}");

        return invoice;
    }

    private async Task PublishToServiceBusAsync(InvoiceIssued issued, string deliveryAddress)
    {
        var invoiceMessage = new
        {
            RestaurantId = issued.OrderRef.RestaurantId,
            CustomerPhone = issued.OrderRef.CustomerPhone,
            DeliveryAddress = deliveryAddress,
            Amount = issued.Amount.Value,
            Tax = issued.Tax.Value,
            Total = issued.Total.Value,
            IssuedAt = issued.IssuedAt
        };

        await _eventSender.SendAsync(_topicName, invoiceMessage);
    }
}
