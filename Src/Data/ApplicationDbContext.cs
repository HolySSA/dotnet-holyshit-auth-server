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
  public required DbSet<UserCharacter> UserCharacters { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<User>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Nickname).IsRequired().HasMaxLength(50);
      entity.Property(e => e.PasswordHash).IsRequired();
      entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
      entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
      entity.Property(e => e.Mmr).IsRequired().HasDefaultValue(1000);
      entity.Property(e => e.LastSelectedCharacter).HasConversion<int>().HasDefaultValue(CharacterType.NONE_CHARACTER);
      entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
      entity.Property(e => e.LastLoginAt).IsRequired(false);

      // Nickname, Email 유니크
      entity.HasIndex(e => e.Nickname).IsUnique();
      entity.HasIndex(e => e.Email).IsUnique();

      // User와 UserCharacter 간의 1:N 관계 설정
      entity.HasMany(u => u.Characters)
        .WithOne(uc => uc.User)
        .HasForeignKey(uc => uc.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<UserCharacter>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.CharacterType).HasConversion<int>();
      entity.Property(e => e.PlayCount).HasDefaultValue(0);
      entity.Property(e => e.WinCount).HasDefaultValue(0);
      entity.Property(e => e.PurchasedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
      entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    });
  }
}