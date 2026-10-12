namespace TrialMicroservice.CoursesService.Application.Events;

/// <summary>
/// Published to the course-events topic. Consumers bind to this shape; they never see our entities.
/// Bump this record only additively, or consumers break.
/// </summary>
public sealed record CourseCreatedEvent(int CourseId, string Title, string? Description, DateTime CreatedAtUtc);