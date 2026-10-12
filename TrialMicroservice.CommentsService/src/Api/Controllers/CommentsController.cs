using Microsoft.AspNetCore.Mvc;
using TrialMicroservice.CommentsService.Application.Comments;

namespace TrialMicroservice.CommentsService.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController(CommentService comments) : ControllerBase
{
    [HttpGet("course/{courseId:int}")]
    public async Task<IActionResult> ListForCourse(int courseId, CancellationToken ct)
    {
        var result = await comments.ListForCourseAsync(courseId, ct);
        return result is null ? CourseNotFound(courseId) : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var created = await comments.AddAsync(request, ct);
        return created is null
            ? CourseNotFound(request.CourseId)
            : Created($"/api/comments/{created.Id}", created);
    }

    private IActionResult CourseNotFound(int courseId) =>
        NotFound(new ProblemDetails { Title = $"Course {courseId} does not exist.", Status = StatusCodes.Status404NotFound });
}