namespace TrialMicroservice.CommentsService.Application.Abstractions;

/// <summary>
/// The only shape this service knows about a foreign course. Both cache warm-up paths
/// (Kafka consumer and HTTP fallback) normalise into this record.
/// </summary>
public sealed record CourseSnapshot(int CourseId, string Title, string? Description);