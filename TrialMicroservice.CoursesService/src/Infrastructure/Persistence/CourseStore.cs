using Microsoft.EntityFrameworkCore;
using TrialMicroservice.CoursesService.Application.Abstractions;
using TrialMicroservice.CoursesService.Domain;

namespace TrialMicroservice.CoursesService.Infrastructure.Persistence;

public class CourseStore(CourseDbContext db) : ICourseStore
{
    public async Task<IReadOnlyList<Course>> ListAsync(CancellationToken ct) =>
        await db.Courses.AsNoTracking().Include(c => c.Lessons).OrderBy(c => c.Id).ToListAsync(ct);

    public Task<Course?> FindWithLessonsAsync(int id, CancellationToken ct) =>
        db.Courses.AsNoTracking().Include(c => c.Lessons).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Course course, CancellationToken ct)
    {
        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);
    }
}