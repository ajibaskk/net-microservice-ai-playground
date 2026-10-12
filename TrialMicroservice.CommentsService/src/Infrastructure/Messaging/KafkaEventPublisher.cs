using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrialMicroservice.CommentsService.Application.Abstractions;

namespace TrialMicroservice.CommentsService.Infrastructure.Messaging;

/// <summary>Producer for the comment-events topic.</summary>
public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T payload, CancellationToken ct = default)
    {
        var message = new Message<string, string>
        {
            Key = key,
            Value = JsonSerializer.Serialize(payload, Json)
        };

        var result = await _producer.ProduceAsync(topic, message, ct);
        _logger.LogInformation("Published to {Topic} key={Key} offset={Offset}", topic, key, result.Offset.Value);
    }

    public void Dispose() => _producer.Dispose();
}