namespace Billing.Events.ServiceBus;

public class ServiceBusSettings
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "order-topic";
    public string SubscriptionName { get; set; } = "billing-subscription";
    public string OutputTopicName { get; set; } = "billing-topic";
}
