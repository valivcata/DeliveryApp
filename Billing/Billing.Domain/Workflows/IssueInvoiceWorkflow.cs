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
                await _invoiceRepository.SaveAsync(invoice);
                await PublishToServiceBusAsync(issued, command.DeliveryAddress);
            }

            return invoice.ToEvent();
        }
        catch (Exception ex)
        {
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

        invoice = new CalculateInvoiceOperation().Transform(invoice);
        invoice = new IssueInvoiceOperation().Transform(invoice);

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
