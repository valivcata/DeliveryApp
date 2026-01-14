namespace Delivery.Events.ServiceBus;

public class ServiceBusSettings
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "billing-topic";
    public string SubscriptionName { get; set; } = "delivery-subscription";
    public string OutputTopicName { get; set; } = "delivery-topic";
}
