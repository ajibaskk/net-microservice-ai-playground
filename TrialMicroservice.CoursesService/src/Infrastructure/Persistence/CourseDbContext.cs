using Microsoft.EntityFrameworkCore;
using TrialMicroservice.CoursesService.Domain;

namespace TrialMicroservice.CoursesService.Infrastructure.Persistence;

/// <summary>Owns the CoursesDb schema. No other service connects to this database.</summary>
public class CourseDbContext(DbContextOptions<CourseDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(e =>
        {
            e.Property(c => c.Title).HasMaxLength(200).IsRequired();
            e.Property(c => c.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Lesson>(e =>
        {
            e.Property(l => l.Title).HasMaxLength(200).IsRequired();
            e.Property(l => l.Content).HasMaxLength(4000);
        });

        modelBuilder.Entity<Course>()
            .HasMany(c => c.Lessons)
            .WithOne(l => l.Course!)
            .HasForeignKey(l => l.CourseId);
    }
}