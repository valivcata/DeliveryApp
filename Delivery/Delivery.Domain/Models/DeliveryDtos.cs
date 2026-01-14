namespace Delivery.Domain.Models;

public class InvoiceIssuedMessageDto
{
    public string RestaurantId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
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
