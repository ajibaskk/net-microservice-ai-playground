using TrialMicroservice.CoursesService.Application.Courses;
using TrialMicroservice.CoursesService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<CourseService>();
builder.Services.AddCoursesInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();

// The service owns its schema; nobody else migrates CoursesDb.
await app.Services.MigrateCoursesDbAsync();

app.Run();