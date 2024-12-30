namespace LoginServer.Models.DTOs;

/// <summary>
/// 서버가 클라이언트로 보내는 회원가입 응답 데이터
/// </summary>
public class RegisterResponseDto
{
  public bool Success { get; set; }
  public string Message { get; set; } = string.Empty;
}