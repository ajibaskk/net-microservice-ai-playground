using Microsoft.EntityFrameworkCore;
using TrialMicroservice.CommentsService.Application.Abstractions;
using TrialMicroservice.CommentsService.Domain;

namespace TrialMicroservice.CommentsService.Infrastructure.Persistence;

public class CommentStore(CommentDbContext db) : ICommentStore
{
    public async Task<IReadOnlyList<Comment>> ListByCourseAsync(int courseId, CancellationToken ct) =>
        await db.Comments.AsNoTracking()
            .Where(c => c.CourseId == courseId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task AddAsync(Comment comment, CancellationToken ct)
    {
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
    }
}