using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrialMicroservice.CoursesService.Application.Abstractions;
using TrialMicroservice.CoursesService.Infrastructure.Messaging;
using TrialMicroservice.CoursesService.Infrastructure.Persistence;

namespace TrialMicroservice.CoursesService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoursesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        services.AddDbContext<CourseDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CoursesDb")));

        services.AddScoped<ICourseStore, CourseStore>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        return services;
    }

    public static async Task MigrateCoursesDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CourseDbContext>().Database.MigrateAsync();
    }
}