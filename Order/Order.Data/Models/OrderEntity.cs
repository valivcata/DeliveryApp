namespace Order.Data.Models;

public class OrderEntity
{
    public Guid Id { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PlacedAt { get; set; }
    public string? ErrorReason { get; set; }
}
