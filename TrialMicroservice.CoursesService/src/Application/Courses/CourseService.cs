using TrialMicroservice.CoursesService.Application.Abstractions;
using TrialMicroservice.CoursesService.Application.Events;
using TrialMicroservice.CoursesService.Domain;

namespace TrialMicroservice.CoursesService.Application.Courses;

/// <summary>
/// Use cases for the course catalog. No EF, no Kafka, no HTTP: only the two ports.
/// </summary>
public class CourseService(ICourseStore store, IEventPublisher publisher)
{
    public const string CourseEventsTopic = "course-events";

    public async Task<IReadOnlyList<CourseListItem>> ListAsync(CancellationToken ct)
    {
        var courses = await store.ListAsync(ct);
        return courses.Select(c => new CourseListItem(c.Id, c.Title, c.Description, c.Lessons.Count)).ToList();
    }

    public async Task<CourseDetails?> FindAsync(int id, CancellationToken ct)
    {
        var course = await store.FindWithLessonsAsync(id, ct);
        return course is null ? null : ToDetails(course);
    }

    public async Task<CourseDetails> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            Lessons = [.. (request.Lessons ?? []).Select(l => new Lesson { Title = l.Title, Content = l.Content, Order = l.Order })]
        };

        await store.AddAsync(course, ct);

        await publisher.PublishAsync(
            CourseEventsTopic,
            course.Id.ToString(),
            new CourseCreatedEvent(course.Id, course.Title, course.Description, DateTime.UtcNow),
            ct);

        return ToDetails(course);
    }

    private static CourseDetails ToDetails(Course course) => new(
        course.Id,
        course.Title,
        course.Description,
        [.. course.Lessons.OrderBy(l => l.Order).Select(l => new LessonItem(l.Id, l.Title, l.Content, l.Order))]);
}