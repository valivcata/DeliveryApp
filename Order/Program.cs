using Order.Data;
using Order.Data.Repositories;
using Order.Domain.Repositories;
using Order.Domain.Workflows;
using Order.Events.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderDb")));

builder.Services.Configure<ServiceBusSettings>(
    builder.Configuration.GetSection("ServiceBus"));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddSingleton<IEventSender, ServiceBusTopicEventSender>();

builder.Services.AddScoped<PlaceOrderWorkflow>(sp => 
{
    var eventSender = sp.GetRequiredService<IEventSender>();
    var orderRepo = sp.GetRequiredService<IOrderRepository>();
    var settings = sp.GetRequiredService<IOptions<ServiceBusSettings>>();
    
    return new PlaceOrderWorkflow(
        eventSender, 
        orderRepo, 
        settings.Value.TopicName ?? "order-topic");
});

var host = builder.Build();
host.Run();
