namespace ArticleApi.Data;

using ArticleApi.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Content> Contents => Set<Content>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(e =>
        {
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        });
        modelBuilder.Entity<Content>(e =>
       {
           e.Property(c => c.Body).HasColumnName("Content");
           e.Property(c => c.AuthorId).HasColumnName("Author");
           e.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
           e.Property(c => c.Language).HasConversion<string>().HasMaxLength(20);

           e.HasOne(c => c.Author)
            .WithMany(u => u.Contents)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

           e.HasOne(c => c.Article)
            .WithMany(a => a.Contents)
            .HasForeignKey(c => c.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
       });
    }
}

