namespace Eventum.Events.Infrastructure.Options;

public class KafkaSettings
{
    public string BootstrapServers { get; init; } = "localhost:9092";

    public string ConsumerGroup { get; init; } = "eventum-events";
}
