using System.Text.Json;
using Confluent.Kafka;
using Eventum.Events.Application.Interfaces;
using Eventum.Events.Infrastructure.Options;
using Eventum.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eventum.Events.Infrastructure.Kafka;

public class BookingConfirmedConsumer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<KafkaSettings> options,
    ILogger<BookingConfirmedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(() => ConsumeAsync(stoppingToken), CancellationToken.None);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(BookingTopics.BookingConfirmed);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var message = JsonSerializer.Deserialize<BookingConfirmed>(result.Message.Value);
                if (message is null)
                {
                    logger.LogWarning("Skipping empty BookingConfirmed message at {Offset}", result.Offset);
                    consumer.Commit(result);
                    continue;
                }

                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
                var applied = await eventService.ApplyBookingConfirmedAsync(message.EventId, message.Seats, stoppingToken);

                if (!applied)
                {
                    logger.LogWarning(
                        "BookingConfirmed skipped. BookingId={BookingId}, EventId={EventId}, Seats={Seats}",
                        message.BookingId,
                        message.EventId,
                        message.Seats);
                }

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Invalid BookingConfirmed JSON");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BookingConfirmed processing failed");
            }
        }

        consumer.Close();
    }
}
