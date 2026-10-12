namespace TrialMicroservice.CommentsService.Application.Comments;

public sealed record CommentItem(int Id, int CourseId, string Author, string Body, DateTime CreatedAtUtc);

public sealed record CreateCommentRequest(int CourseId, string Author, string Body);

public sealed record CourseComments(int CourseId, string CourseTitle, IReadOnlyList<CommentItem> Comments);