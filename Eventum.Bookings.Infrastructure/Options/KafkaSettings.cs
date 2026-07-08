namespace Eventum.Bookings.Infrastructure.Options;

public class KafkaSettings
{
    public string BootstrapServers { get; init; } = "localhost:9092";
}
