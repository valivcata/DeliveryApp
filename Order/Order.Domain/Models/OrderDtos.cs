namespace Order.Domain.Models;

public class PlaceOrderDto
{
    public string RestaurantId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
}

public class OrderResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public OrderDetailsDto? OrderDetails { get; set; }
}

public class OrderDetailsDto
{
    public string RestaurantId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public DateTime PlacedAt { get; set; }
}
