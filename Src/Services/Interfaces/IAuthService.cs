using LoginServer.Models.DTOs;

namespace LoginServer.Services.Interfaces;

public interface IAuthService
{
  Task<bool> RegisterAsync(RegisterRequestDto request);
  Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
  Task<bool> ValidateTokenAsync(string token);
  Task LogoutAsync(int userId);
}