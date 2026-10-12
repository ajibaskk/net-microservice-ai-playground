using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TrialMicroservice.CommentsService.Application.Abstractions;
using TrialMicroservice.CommentsService.Application.Events;
using TrialMicroservice.CommentsService.Infrastructure.Caching;

namespace TrialMicroservice.CommentsService.Infrastructure.Messaging;

/// <summary>
/// Consumes course-events and pre-populates the Redis cache, so the first comment on a newly
/// published course never pays for the HTTP round trip.
/// </summary>
public class KafkaCourseConsumerService(
    IOptions<KafkaOptions> options,
    IConnectionMultiplexer redis,
    ILogger<KafkaCourseConsumerService> logger) : BackgroundService
{
    public const string CourseEventsTopic = "course-events";
    private const string GroupId = "comments-service";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield first so the consumer handshake never delays host startup.
        await Task.Yield();

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // Commit only after the cache write succeeds: at-least-once beats a lost warm-up.
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        }).Build();

        consumer.Subscribe(CourseEventsTopic);
        logger.LogInformation("Consuming {Topic} as group {GroupId}", CourseEventsTopic, GroupId);

        var database = redis.GetDatabase();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                if (result?.Message is null)
                {
                    continue;
                }

                var created = JsonSerializer.Deserialize<CourseCreatedEvent>(result.Message.Value, Json);
                if (created is null)
                {
                    logger.LogWarning("Skipped malformed message at offset {Offset}", result.Offset.Value);
                    continue;
                }

                await RedisCourseCatalog.WarmAsync(
                    database,
                    new CourseSnapshot(created.CourseId, created.Title, created.Description));

                consumer.Commit(result);
                logger.LogInformation(
                    "Warmed course:{CourseId} ({Title}) from {Topic}",
                    created.CourseId,
                    created.Title,
                    CourseEventsTopic);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (ConsumeException ex)
        {
            logger.LogError(ex, "Kafka consume failed for {Topic}", CourseEventsTopic);
            throw;
        }
        finally
        {
            consumer.Close();
        }
    }
}