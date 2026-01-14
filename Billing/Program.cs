using Billing.Data;
using Billing.Data.Repositories;
using Billing.Domain.Repositories;
using Billing.Domain.Workflows;
using Billing.Events.ServiceBus;
using Billing.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BillingDb")));

builder.Services.Configure<ServiceBusSettings>(
    builder.Configuration.GetSection("ServiceBus"));

builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

builder.Services.AddSingleton<IEventSender, ServiceBusTopicEventSender>();

builder.Services.AddScoped<IssueInvoiceWorkflow>(sp => 
{
    var eventSender = sp.GetRequiredService<IEventSender>();
    var invoiceRepo = sp.GetRequiredService<IInvoiceRepository>();
    var settings = sp.GetRequiredService<IOptions<ServiceBusSettings>>();
    
    return new IssueInvoiceWorkflow(
        eventSender, 
        invoiceRepo, 
        settings.Value.OutputTopicName ?? "billing-topic");
});

builder.Services.AddHostedService<OrderPlacedListener>();

var host = builder.Build();
host.Run();
