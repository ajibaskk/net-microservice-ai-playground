namespace TrialMicroservice.CommentsService.Application.Events;

public sealed record CommentCreatedEvent(int CommentId, int CourseId, string Author, DateTime CreatedAtUtc);