namespace LoginServer.Models.Entities;

public class UserCharacter
{
  public int Id { get; set; }
  public int UserId { get; set; }  // FK
  public CharacterType CharacterType { get; set; }
  public int PlayCount { get; set; }
  public int WinCount { get; set; }
  public DateTime PurchasedAt { get; set; }
  public DateTime UpdatedAt { get; set; }

  // Navigation property (User 엔티티와 연결)
  public User User { get; set; } = null!;
}

public enum CharacterType
{
  NONE_CHARACTER = 0,
  RED = 1,
  SHARK = 3,
  MALANG = 5,
  FROGGY = 7,
  PINK = 8,
  SWIM_GLASSES = 9,
  MASK = 10,
  DINOSAUR = 12,
  PINK_SLIME = 13
}