namespace LoginServer.Models.DTOs;

/// <summary>
/// 서버가 클라이언트로 보내는 로그인 응답 데이터
/// </summary>
public class LoginResponseDto
{
  public string Token { get; set; } = string.Empty; // JWT 토큰
  public string Email { get; set; } = string.Empty; // 사용자 이메일
  public string Nickname { get; set; } = string.Empty; // 사용자 닉네임
  public DateTime ExpiresAt { get; set; } // 토큰 만료 시간
}