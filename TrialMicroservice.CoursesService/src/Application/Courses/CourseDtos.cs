namespace TrialMicroservice.CoursesService.Application.Courses;

public sealed record LessonItem(int Id, string Title, string? Content, int Order);

public sealed record CourseListItem(int Id, string Title, string? Description, int LessonCount);

public sealed record CourseDetails(int Id, string Title, string? Description, IReadOnlyList<LessonItem> Lessons);

public sealed record CreateLessonRequest(string Title, string? Content, int Order);

public sealed record CreateCourseRequest(string Title, string? Description, IReadOnlyList<CreateLessonRequest>? Lessons);