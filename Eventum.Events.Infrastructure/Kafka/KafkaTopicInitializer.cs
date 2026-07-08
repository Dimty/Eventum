using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Eventum.Events.Infrastructure.Options;
using Eventum.Shared.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eventum.Events.Infrastructure.Kafka;

public class KafkaTopicInitializer(
    IOptions<KafkaSettings> options,
    ILogger<KafkaTopicInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new AdminClientConfig { BootstrapServers = options.Value.BootstrapServers };

        try
        {
            using var adminClient = new AdminClientBuilder(config).Build();
            await adminClient.CreateTopicsAsync(
                [new TopicSpecification { Name = BookingTopics.BookingConfirmed, NumPartitions = 1, ReplicationFactor = 1 }],
                new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(10) });

            logger.LogInformation("Kafka topic {Topic} created", BookingTopics.BookingConfirmed);
        }
        catch (CreateTopicsException ex) when (ex.Results.Any(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            logger.LogInformation("Kafka topic {Topic} already exists", BookingTopics.BookingConfirmed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kafka topic {Topic} was not created; subscriber will retry on broker availability", BookingTopics.BookingConfirmed);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
