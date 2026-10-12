namespace TrialMicroservice.CommentsService.Application.Abstractions;

/// <summary>
/// Answers "does this course exist?" without any reference to CourseDbContext, Course
/// entities, or the CoursesService assembly. The implementation decides how: cache first,
/// then a network call.
/// </summary>
public interface ICourseCatalog
{
    Task<CourseSnapshot?> FindAsync(int courseId, CancellationToken ct);
}