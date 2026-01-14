using Delivery.Data;
using Delivery.Data.Repositories;
using Delivery.Domain.Repositories;
using Delivery.Domain.Workflows;
using Delivery.Events.ServiceBus;
using Delivery.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<DeliveryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DeliveryDb")));

builder.Services.Configure<ServiceBusSettings>(
    builder.Configuration.GetSection("ServiceBus"));

builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();

builder.Services.AddSingleton<IEventSender, ServiceBusTopicEventSender>();

builder.Services.AddScoped<StartDeliveryWorkflow>(sp => 
{
    var eventSender = sp.GetRequiredService<IEventSender>();
    var deliveryRepo = sp.GetRequiredService<IDeliveryRepository>();
    var settings = sp.GetRequiredService<IOptions<ServiceBusSettings>>();
    
    return new StartDeliveryWorkflow(
        eventSender, 
        deliveryRepo, 
        settings.Value.OutputTopicName ?? "delivery-topic");
});

builder.Services.AddHostedService<InvoiceIssuedListener>();

var host = builder.Build();
host.Run();
