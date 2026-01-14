namespace Billing.Data.Models;

public class InvoiceEntity
{
    public Guid Id { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? ErrorReason { get; set; }
}
