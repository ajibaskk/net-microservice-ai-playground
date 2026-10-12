using Microsoft.EntityFrameworkCore;
using TrialMicroservice.CommentsService.Domain;

namespace TrialMicroservice.CommentsService.Infrastructure.Persistence;

/// <summary>Owns CommentsDb. It contains no course table and no foreign key to one.</summary>
public class CommentDbContext(DbContextOptions<CommentDbContext> options) : DbContext(options)
{
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>(e =>
        {
            e.Property(c => c.Author).HasMaxLength(200).IsRequired();
            e.Property(c => c.Body).HasMaxLength(4000).IsRequired();
            // Not a foreign key - CourseId points across a service boundary on purpose.
            e.HasIndex(c => c.CourseId);
        });
    }
}