using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TrialMicroservice.CommentsService.Application.Abstractions;
using TrialMicroservice.CommentsService.Infrastructure.Caching;
using TrialMicroservice.CommentsService.Infrastructure.Messaging;
using TrialMicroservice.CommentsService.Infrastructure.Persistence;

namespace TrialMicroservice.CommentsService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommentsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        services.AddDbContext<CommentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CommentsDb")));

        services.AddScoped<ICommentStore, CommentStore>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

        // Named client: the catalog resolves it through IHttpClientFactory, so the factory still
        // owns the handler pool and the base address stays in configuration.
        services.AddHttpClient(RedisCourseCatalog.HttpClientName, client =>
            client.BaseAddress = new Uri(configuration["CoursesService:BaseUrl"]!));

        services.AddScoped<ICourseCatalog, RedisCourseCatalog>();

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddHostedService<KafkaCourseConsumerService>();

        return services;
    }

    public static async Task MigrateCommentsDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CommentDbContext>().Database.MigrateAsync();
    }
}