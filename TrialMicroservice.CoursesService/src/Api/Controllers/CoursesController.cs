using Microsoft.AspNetCore.Mvc;
using TrialMicroservice.CoursesService.Application.Courses;

namespace TrialMicroservice.CoursesService.Api.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(CourseService courses) : ControllerBase
{
    /// <summary>Read by CommentsService (Redis miss fallback) and by clients.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await courses.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var c = await courses.FindAsync(id, ct);
        return course is null ? NotFound() : Ok(course);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request, CancellationToken ct)
    {
        var course = await courses.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = course.Id }, course);
    }
}