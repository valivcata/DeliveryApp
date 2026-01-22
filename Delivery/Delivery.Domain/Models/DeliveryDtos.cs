using System.ComponentModel.DataAnnotations;

namespace Delivery.Domain.Models;

public class InvoiceIssuedMessageDto
{
    [Required]
    public string RestaurantId { get; set; } = string.Empty;
    
    [Required]
    public string CustomerPhone { get; set; } = string.Empty;
    
    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [Required]
    [Range(0.00, double.MaxValue)]
    public decimal Tax { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Total { get; set; }
    
    [Required]
    public DateTime IssuedAt { get; set; }
}

public class DeliveryResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DeliveryDetailsDto? DeliveryDetails { get; set; }
}

public class DeliveryDetailsDto
{
    public string InvoiceReference { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}
