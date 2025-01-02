namespace LoginServer.Models.Entities;

public class User
{
  public int Id { get; set; }
  public required string Email { get; set; }
  public required string Nickname { get; set; }
  public required string PasswordHash { get; set; }
  public bool IsActive { get; set; }
  public int Mmr { get; set; }
  public CharacterType LastSelectedCharacter { get; set; }
  public DateTime? LastLoginAt { get; set; }
  public DateTime CreatedAt { get; set; }

  // Navigation property
  public ICollection<UserCharacter> Characters { get; set; }

  public User()
  {
    IsActive = true;
    Mmr = 1000;
    CreatedAt = DateTime.UtcNow;
    Characters = new List<UserCharacter>();
  }
}