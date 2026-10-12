using TrialMicroservice.CommentsService.Domain;

namespace TrialMicroservice.CommentsService.Application.Abstractions;

public interface ICommentStore
{
    Task<IReadOnlyList<Comment>> ListByCourseAsync(int courseId, CancellationToken ct);
    Task AddAsync(Comment comment, CancellationToken ct);
}