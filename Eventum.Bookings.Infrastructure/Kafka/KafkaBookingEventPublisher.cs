using System.Text.Json;
using Confluent.Kafka;
using Eventum.Bookings.Application.Interfaces;
using Eventum.Bookings.Infrastructure.Options;
using Eventum.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace Eventum.Bookings.Infrastructure.Kafka;

public class KafkaBookingEventPublisher : IBookingEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaBookingEventPublisher(IOptions<KafkaSettings> options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(BookingConfirmed message, CancellationToken token = default)
    {
        var payload = JsonSerializer.Serialize(message);
        await _producer.ProduceAsync(
            BookingTopics.BookingConfirmed,
            new Message<string, string>
            {
                Key = message.EventId.ToString(),
                Value = payload
            },
            token);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
