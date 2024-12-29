using FluentValidation;
using LoginServer.Models.DTOs;

namespace LoginServer.Utils;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
  public RegisterRequestValidator()
  {
    // Stop 모드: 첫 번째 실패하는 규칙에서 검증을 중단
    ClassLevelCascadeMode = CascadeMode.Stop;

    RuleFor(x => x.Email).Cascade(CascadeMode.Stop)
      .NotEmpty().WithMessage("이메일을 입력해주세요")
      .EmailAddress().WithMessage("올바른 이메일 형식이 아닙니다");

    RuleFor(x => x.Nickname).Cascade(CascadeMode.Stop)
      .NotEmpty().WithMessage("닉네임을 입력해주세요")
      .Length(2, 20).WithMessage("닉네임은 2~20자 사이여야 합니다");

    RuleFor(x => x.Password).Cascade(CascadeMode.Stop)
      .NotEmpty().WithMessage("비밀번호를 입력해주세요")
      .MinimumLength(8).WithMessage("비밀번호는 최소 8자 이상이어야 합니다")
      .Matches(@"[a-z]").WithMessage("소문자를 포함해야 합니다")
      .Matches(@"[0-9]").WithMessage("숫자를 포함해야 합니다")
      .Matches(@"[!@#$%^&*]").WithMessage("특수문자를 포함해야 합니다");
  }
}