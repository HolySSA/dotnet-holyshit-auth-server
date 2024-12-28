using LoginServer.Models.DTOs;

namespace LoginServer.Services.Interfaces;

public interface IAuthService
{
  Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
  Task<bool> ValidateTokenAsync(string token);
  Task LogoutAsync(int userId);
}