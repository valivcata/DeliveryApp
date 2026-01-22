using Microsoft.AspNetCore.Mvc;
using Order.Domain.Models;
using Order.Domain.Workflows;
using static Order.Domain.Models.Order;

namespace Order.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly PlaceOrderWorkflow _placeOrderWorkflow;

    public OrdersController(
        ILogger<OrdersController> logger,
        PlaceOrderWorkflow placeOrderWorkflow)
    {
        _logger = logger;
        _placeOrderWorkflow = placeOrderWorkflow;
    }

    /// <summary>
    /// Place a new order
    /// </summary>
    /// <param name="orderDto">Order details</param>
    /// <returns>Order placement result</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderPlacedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto orderDto)
    {
        try
        {
            // Step 1: Log incoming request
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n========== ORDER SERVICE - NEW ORDER RECEIVED ==========");
            Console.ResetColor();
            
            Console.WriteLine($"[Step 1/4] 📥 Incoming order request:");
            Console.WriteLine($"           Restaurant ID: {orderDto.RestaurantId}");
            Console.WriteLine($"           Customer Phone: {orderDto.CustomerPhone}");
            Console.WriteLine($"           Delivery Address: {orderDto.DeliveryAddress}");
            Console.WriteLine($"           Order Amount: {orderDto.OrderAmount:C}");
            
            _logger.LogInformation("Received order request for restaurant {RestaurantId}", orderDto.RestaurantId);

            // Step 2: Execute workflow
            Console.WriteLine($"[Step 2/4] ⚙️  Executing PlaceOrderWorkflow...");
            
            IOrderEvent result = await _placeOrderWorkflow.ExecuteAsync(orderDto);

            return result switch
            {
                OrderPlacedEvent success => HandleSuccess(success),
                OrderFailedEvent failure => HandleFailure(failure),
                _ => HandleUnknownError()
            };
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] ❌ Exception: {ex.Message}");
            Console.ResetColor();
            
            _logger.LogError(ex, "Error placing order");
            return StatusCode(500, new ErrorResponse
            {
                Success = false,
                Message = "Internal server error",
                Error = ex.Message
            });
        }
    }

    private IActionResult HandleSuccess(OrderPlacedEvent success)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Step 4/4] ✅ ORDER PLACED SUCCESSFULLY!");
        Console.WriteLine($"           Order Amount: {success.OrderAmount:C}");
        Console.WriteLine($"           Placed At: {success.PlacedAt:HH:mm:ss}");
        Console.WriteLine("========== ORDER SERVICE - COMPLETE ==========\n");
        Console.ResetColor();
        
        return Ok(new OrderPlacedResponse
        {
            Success = true,
            Message = "Order placed successfully",
            RestaurantId = success.RestaurantId,
            CustomerPhone = success.CustomerPhone,
            DeliveryAddress = success.DeliveryAddress,
            OrderAmount = success.OrderAmount,
            PlacedAt = success.PlacedAt
        });
    }

    private IActionResult HandleFailure(OrderFailedEvent failure)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Step 4/4] ❌ ORDER FAILED: {failure.Reason}");
        Console.WriteLine("========== ORDER SERVICE - FAILED ==========\n");
        Console.ResetColor();
        
        return BadRequest(new ErrorResponse
        {
            Success = false,
            Message = "Order placement failed",
            Error = failure.Reason
        });
    }

    private IActionResult HandleUnknownError()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Step 4/4] ❌ UNKNOWN ERROR OCCURRED");
        Console.WriteLine("========== ORDER SERVICE - ERROR ==========\n");
        Console.ResetColor();
        
        return StatusCode(500, new ErrorResponse
        {
            Success = false,
            Message = "Unknown error occurred",
            Error = "Unexpected result type"
        });
    }

    /// <summary>
    /// Get API health status
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Order API", Timestamp = DateTime.UtcNow });
    }
}

public class OrderPlacedResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RestaurantId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
