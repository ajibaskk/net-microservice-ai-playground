using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TrialMicroservice.CommentsService.Application.Abstractions;

namespace TrialMicroservice.CommentsService.Infrastructure.Caching;

/// <summary>
/// The lookup ladder from the spec: Redis -> HTTP to CoursesService -> cache the answer.
/// Nothing here can reach CoursesDb; the HTTP call is the only source of truth.
/// </summary>
public class RedisCourseCatalog : ICourseCatalog
{
    /// <summary>Client name used for the typed HttpClient registered in DependencyInjection.</summary>
    public const string HttpClientName = "courses-service";

    public const int CacheTtlSeconds = 600;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<RedisCourseCatalog> _logger;

    public RedisCourseCatalog(
        IConnectionMultiplexer redis,
        IHttpClientFactory httpFactory,
        ILogger<RedisCourseCatalog> logger)
    {
        _redis = redis;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public static string KeyFor(int courseId) => $"course:{courseId}";

    /// <summary>Shape of GET http://localhost:5001/api/courses/{id}. Owned here, not shared.</summary>
    private sealed record CourseResponse(int Id, string Title, string? Description);

    /// <summary>Shared with the Kafka consumer so both warm the same key with the same shape.</summary>
    public static async Task WarmAsync(IDatabase redis, CourseSnapshot snapshot)
    {
        await redis.StringSetAsync(
            KeyFor(snapshot.CourseId),
            JsonSerializer.Serialize(snapshot, Json),
            TimeSpan.FromSeconds(CacheTtlSeconds));
    }

    public async Task<CourseSnapshot?> FindAsync(int courseId, CancellationToken ct)
    {
        var redis = _redis.GetDatabase();

        if (await redis.StringGetAsync(KeyFor(courseId)) is { HasValue: true } hit)
        {
            _logger.LogDebug("Cache hit for course {CourseId}", courseId);
            return JsonSerializer.Deserialize<CourseSnapshot>(hit.ToString(), Json);
        }

        _logger.LogInformation("Cache miss for course {CourseId}; falling back to CoursesService", courseId);

        var client = _httpFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync($"/api/courses/{courseId}", ct);

        // 404 and upstream failures both mean "we cannot vouch for this course".
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CoursesService returned {Status} for course {CourseId}", (int)response.StatusCode, courseId);
            return null;
        }

        var course = await response.Content.ReadFromJsonAsync<CourseResponse>(Json, ct);
        if (course is null)
        {
            return null;
        }

        var snapshot = new CourseSnapshot(course.Id, course.Title, course.Description);
        await WarmAsync(redis, snapshot);
        return snapshot;
    }
}