namespace LoginServer.Models.Entities;

public class User
{
  public int Id { get; set; }
  public required string Email { get; set; }
  public required string Nickname { get; set; }
  public required string PasswordHash { get; set; }
  public bool IsActive { get; set; } = false;
  public DateTime? LastLoginAt { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}