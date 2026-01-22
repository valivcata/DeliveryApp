using System.ComponentModel.DataAnnotations;

namespace Billing.Domain.Models;

public class OrderPlacedMessageDto
{
    [Required]
    public string RestaurantId { get; set; } = string.Empty;
    
    [Required]
    public string CustomerPhone { get; set; } = string.Empty;
    
    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal OrderAmount { get; set; }
    
    [Required]
    public DateTime PlacedAt { get; set; }
}

public class InvoiceResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public InvoiceDetailsDto? InvoiceDetails { get; set; }
}

public class InvoiceDetailsDto
{
    public string OrderReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public DateTime IssuedAt { get; set; }
}
