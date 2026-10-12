namespace TrialMicroservice.CommentsService.Application.Events;

/// <summary>
/// Consumer-side copy of CoursesService's contract. Independently deployable services each
/// own their copy of the event schema; sharing an assembly would re-couple them.
/// </summary>
public sealed record CourseCreatedEvent(int CourseId, string Title, string? Description, DateTime CreatedAtUtc);