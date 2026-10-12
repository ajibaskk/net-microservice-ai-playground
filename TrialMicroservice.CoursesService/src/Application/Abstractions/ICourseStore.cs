using TrialMicroservice.CoursesService.Domain;

namespace TrialMicroservice.CoursesService.Application.Abstractions;

/// <summary>
/// Persistence port. Application cannot see CourseDbContext, so this is the seam that keeps
/// the dependency arrow pointing inwards. Implemented once, by Infrastructure.
/// </summary>
public interface ICourseStore
{
    Task<IReadOnlyList<Course>> ListAsync(CancellationToken ct);
    Task<Course?> FindWithLessonsAsync(int id, CancellationToken ct);
    Task AddAsync(Course course, CancellationToken ct);
}