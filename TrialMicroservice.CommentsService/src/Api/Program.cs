using TrialMicroservice.CommentsService.Application.Comments;
using TrialMicroservice.CommentsService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<CommentService>();
builder.Services.AddCommentsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();

// The service owns its schema; nobody else migrates CommentsDb.
await app.Services.MigrateCommentsDbAsync();

app.Run();