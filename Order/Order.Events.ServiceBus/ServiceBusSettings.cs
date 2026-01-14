namespace Order.Events.ServiceBus;

public class ServiceBusSettings
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "order-topic";
}
