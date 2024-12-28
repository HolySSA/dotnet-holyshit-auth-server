using Microsoft.EntityFrameworkCore;
using LoginServer.Models.Entities;

namespace LoginServer.Data;

public class ApplicationDbContext : DbContext
{
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
      : base(options)
  {
  }

  public required DbSet<User> Users { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<User>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Nickname).IsRequired().HasMaxLength(50);
      entity.Property(e => e.PasswordHash).IsRequired();
      entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
      entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
      entity.Property(e => e.IsActive).HasDefaultValue(true);

      // Nickname 유니크
      entity.HasIndex(e => e.Nickname).IsUnique();
      // Email 유니크
      entity.HasIndex(e => e.Email).IsUnique();
    });
  }
}