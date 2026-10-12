namespace TrialMicroservice.CommentsService.Domain;

/// <summary>
/// CourseId is a raw primitive, and CommentsDb has no foreign key to CoursesDb. The two
/// services agree on an identifier and nothing else - no shared schema, no shared context.
/// </summary>
public class Comment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}