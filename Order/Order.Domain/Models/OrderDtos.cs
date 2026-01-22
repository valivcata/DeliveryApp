using System.ComponentModel.DataAnnotations;

namespace Order.Domain.Models;

public class PlaceOrderDto
{
    [Required(ErrorMessage = "Restaurant ID is required")]
    [RegularExpression(@"^REST-\d{4}$", ErrorMessage = "Restaurant ID must be in format REST-XXXX (e.g., REST-0001)")]
    public string RestaurantId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Customer phone is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 20 characters")]
    public string CustomerPhone { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Delivery address is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Delivery address must be between 10 and 500 characters")]
    public string DeliveryAddress { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, 10000.00, ErrorMessage = "Order amount must be between 0.01 and 10,000.00")]
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
