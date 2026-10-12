using TrialMicroservice.CommentsService.Application.Abstractions;
using TrialMicroservice.CommentsService.Application.Events;
using TrialMicroservice.CommentsService.Domain;

namespace TrialMicroservice.CommentsService.Application.Comments;

public class CommentService(ICommentStore store, ICourseCatalog catalog, IEventPublisher publisher)
{
    public const string CommentEventsTopic = "comment-events";

    /// <summary>Returns null when the course is unknown locally and upstream.</summary>
    public async Task<CourseComments?> ListForCourseAsync(int courseId, CancellationToken ct)
    {
        var course = await catalog.FindAsync(courseId, ct);
        if (course is null)
        {
            return null;
        }

        var comments = await store.ListByCourseAsync(courseId, ct);
        return new CourseComments(courseId, course.Title, [.. comments.Select(ToItem)]);
    }

    /// <summary>Returns null when the course does not exist, so the API can answer 404.</summary>
    public async Task<CommentItem?> AddAsync(CreateCommentRequest request, CancellationToken ct)
    {
        // Cross-service integrity lives here: we cannot use a foreign key across databases,
        // so existence is proven through the catalog instead.
        if (await catalog.FindAsync(request.CourseId, ct) is null)
        {
            return null;
        }

        var comment = new Comment
        {
            CourseId = request.CourseId,
            Author = request.Author,
            Body = request.Body,
            CreatedAtUtc = DateTime.UtcNow
        };

        await store.AddAsync(comment, ct);

        await publisher.PublishAsync(
            CommentEventsTopic,
            comment.CourseId.ToString(),
            new CommentCreatedEvent(comment.Id, comment.CourseId, comment.Author, comment.CreatedAtUtc),
            ct);

        return ToItem(comment);
    }

    private static CommentItem ToItem(Comment c) => new(c.Id, c.CourseId, c.Author, c.Body, c.CreatedAtUtc);
}