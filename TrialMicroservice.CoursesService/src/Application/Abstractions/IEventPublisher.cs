namespace TrialMicroservice.CoursesService.Application.Abstractions;

/// <summary>Messaging port. Keeps Confluent.Kafka out of the Application layer.</summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, string key, T payload, CancellationToken ct = default);
}