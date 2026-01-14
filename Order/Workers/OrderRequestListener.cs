using Order.Data;
using Order.Domain.Models;
using Order.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Order.Events.ServiceBus;

namespace Order.Workers;

/// <summary>
/// Background service that listens for order requests from console input
/// and processes them through the order workflow
/// </summary>
public class OrderRequestListener : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OrderRequestListener> _logger;
    private readonly string _topicName;
    private static readonly SemaphoreSlim _consoleSemaphore = new(1, 1);

    public OrderRequestListener(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<OrderRequestListener> logger,
        IOptions<ServiceBusSettings> settings)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _topicName = settings.Value.TopicName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Request Listener started. Waiting for user input...");
        
        await Task.Yield(); // Allow the application to start

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("Place a New Order (or 'Esc' to quit):");
                Console.WriteLine(new string('=', 60));
                
                // Get available restaurants from database
                string? restaurantId;
                await using (var restaurantScope = _serviceScopeFactory.CreateAsyncScope())
                {
                    var dbContext = restaurantScope.ServiceProvider.GetRequiredService<OrderDbContext>();
                    var availableRestaurants = await dbContext.Restaurants
                        .Where(r => r.Status == "Active")
                        .OrderBy(r => r.Name)
                        .Select(r => new { r.RestaurantId, r.Name, r.Cuisine, r.MinimumOrder, r.DeliveryFee })
                        .ToListAsync(stoppingToken);

                    if (!availableRestaurants.Any())
                    {
                        Console.WriteLine("No available restaurants at this time.");
                        await Task.Delay(2000, stoppingToken);
                        continue;
                    }

                    // Interactive restaurant selection
                    restaurantId = SelectRestaurantInteractive(availableRestaurants);
                }
                
                if (restaurantId == null)
                {
                    _logger.LogInformation("Exiting order listener...");
                    break;
                }

                Console.Write("Customer Phone: ");
                var customerPhone = Console.ReadLine();

                Console.WriteLine("\n" + new string('-', 60));
                Console.WriteLine("Delivery Details:");
                Console.WriteLine(new string('-', 60));
                
                Console.Write("Delivery Address: ");
                var deliveryAddress = Console.ReadLine();

                Console.Write("Order Amount ($): ");
                var orderAmountStr = Console.ReadLine();
                
                if (!decimal.TryParse(orderAmountStr, out var orderAmount))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid order amount. Please try again.");
                    Console.ResetColor();
                    continue;
                }

                var orderRequest = new PlaceOrderDto
                {
                    RestaurantId = restaurantId?.Trim() ?? "",
                    CustomerPhone = customerPhone?.Trim() ?? "",
                    DeliveryAddress = deliveryAddress?.Trim() ?? "",
                    OrderAmount = orderAmount
                };

                _logger.LogInformation("Processing order request for restaurant {RestaurantId}", orderRequest.RestaurantId);

                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var workflow = scope.ServiceProvider.GetRequiredService<PlaceOrderWorkflow>();
                var result = await workflow.ExecuteAsync(orderRequest);

                Console.WriteLine("\n" + new string('-', 60));
                Console.WriteLine($"Result: {result.GetType().Name}");
                
                if (result is OrderFailedEvent failedEvent)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {failedEvent.Reason}");
                    Console.ResetColor();
                }
                else if (result is OrderPlacedEvent placedEvent)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Success! Order placed at {placedEvent.PlacedAt}");
                    Console.WriteLine($"Restaurant: {placedEvent.RestaurantId}, Amount: {placedEvent.OrderAmount:C}");
                    Console.WriteLine($"Delivery to: {placedEvent.DeliveryAddress}");
                    Console.ResetColor();
                }
                
                Console.WriteLine(new string('-', 60) + "\n");

                await Task.Delay(500, stoppingToken); // Small delay before next iteration
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order request");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private string? SelectRestaurantInteractive<T>(List<T> restaurants) where T : class
    {
        if (!restaurants.Any())
            return null;

        int selectedIndex = 0;
        ConsoleKey key;

        // Get property info for display
        var restaurantIdProp = typeof(T).GetProperty("RestaurantId");
        var nameProp = typeof(T).GetProperty("Name");
        var cuisineProp = typeof(T).GetProperty("Cuisine");
        var minOrderProp = typeof(T).GetProperty("MinimumOrder");
        var deliveryFeeProp = typeof(T).GetProperty("DeliveryFee");

        _consoleSemaphore.Wait();
        try
        {
            Console.WriteLine("\nUse ↑/↓ arrow keys to select, Enter to confirm, Esc to exit:");
            Console.WriteLine(new string('-', 60));

            do
            {
                // Clear previous display and redraw
                Console.SetCursorPosition(0, Console.CursorTop - restaurants.Count - 1);

                for (int i = 0; i < restaurants.Count; i++)
                {
                    var restaurant = restaurants[i];
                    var id = restaurantIdProp?.GetValue(restaurant)?.ToString() ?? "";
                    var name = nameProp?.GetValue(restaurant)?.ToString() ?? "";
                    var cuisine = cuisineProp?.GetValue(restaurant)?.ToString() ?? "";
                    var minOrder = minOrderProp?.GetValue(restaurant);
                    var deliveryFee = deliveryFeeProp?.GetValue(restaurant);

                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.Write("→ ");
                    }
                    else
                    {
                        Console.Write("  ");
                    }

                    Console.Write($"{id,-10} {name,-25} [{cuisine,-15}] Min: ${minOrder,-6} Fee: ${deliveryFee}");
                    
                    if (i == selectedIndex)
                    {
                        Console.ResetColor();
                    }

                    // Clear rest of line and move to next
                    Console.Write(new string(' ', Math.Max(0, Console.BufferWidth - Console.CursorLeft - 1)));
                    Console.WriteLine();
                }

                Console.WriteLine(new string('-', 60));

                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = selectedIndex > 0 ? selectedIndex - 1 : restaurants.Count - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = selectedIndex < restaurants.Count - 1 ? selectedIndex + 1 : 0;
                        break;
                }

            } while (key != ConsoleKey.Enter && key != ConsoleKey.Escape);

            Console.WriteLine(); // Move to next line after selection

            if (key == ConsoleKey.Escape)
                return null;

            return restaurantIdProp?.GetValue(restaurants[selectedIndex])?.ToString();
        }
        finally
        {
            _consoleSemaphore.Release();
        }
    }
}
