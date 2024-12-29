namespace LoginServer.Models.DTOs;

public class RegisterRequestDto
{
  public required string Email { get; set; }
  public required string Password { get; set; }
  public required string Nickname { get; set; }
}