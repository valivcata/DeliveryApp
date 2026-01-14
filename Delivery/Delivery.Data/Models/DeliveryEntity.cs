namespace Delivery.Data.Models;

public class DeliveryEntity
{
    public Guid Id { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? DriverId { get; set; }
    public string? Route { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public string? ErrorReason { get; set; }
}
