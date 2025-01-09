namespace LoginServer.Models.DTOs;

/// <summary>
/// 서버가 클라이언트로 보내는 로그인 응답 데이터
/// </summary>
public class LoginResponseDto
{
  public int UserId { get; set; } // 사용자 고유 Id
  public string Nickname { get; set; } = string.Empty; // 사용자 닉네임
  public string Token { get; set; } = string.Empty; // JWT 토큰
  public DateTime ExpiresAt { get; set; } // 토큰 만료 시간
  public string LobbyHost { get; set; } = string.Empty; // 로비 호스트
  public int LobbyPort { get; set; } // 로비 포트
  public LoginResult Result { get; set; } // 로그인 결과
}

public enum LoginResult 
{
  Success = 0,
  DuplicateLogin = 1,
}