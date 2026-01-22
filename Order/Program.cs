using Order.Data;
using Order.Data.Repositories;
using Order.Domain.Repositories;
using Order.Domain.Workflows;
using Order.Events.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("OrderDb"),
        b => b.MigrationsAssembly("Order.Data")));

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

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Order API", 
        Version = "v1",
        Description = "API for managing food delivery orders"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
