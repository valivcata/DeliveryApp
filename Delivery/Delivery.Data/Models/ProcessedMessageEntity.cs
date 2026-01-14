namespace Delivery.Data.Models;

public class ProcessedMessageEntity
{
    public string MessageId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string ProcessorName { get; set; } = string.Empty;
}
