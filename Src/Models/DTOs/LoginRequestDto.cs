using System.ComponentModel.DataAnnotations;

namespace LoginServer.Models.DTOs;

/// <summary>
/// DTO - Data Transfer Object(데이터 전송 객체)
/// 클라이언트가 서버로 보내는 로그인 요청 데이터
/// </summary>
public class LoginRequestDto
{
  [Required(ErrorMessage = "이메일은 필수입니다")]
  [EmailAddress(ErrorMessage = "올바른 이메일 형식이 아닙니다")]
  public string Email { get; set; } = string.Empty;

  [Required(ErrorMessage = "비밀번호는 필수입니다")]
  public string Password { get; set; } = string.Empty;
}