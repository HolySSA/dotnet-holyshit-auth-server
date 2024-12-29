using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using LoginServer.Constants;
using LoginServer.Services.Interfaces;
using LoginServer.Data;
using LoginServer.Models.DTOs;
using LoginServer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using static BCrypt.Net.BCrypt;

namespace LoginServer.Services.Implementations;

public class AuthService : IAuthService
{
  private readonly ApplicationDbContext _context; // DB 컨텍스트
  private readonly ICacheService _cacheService; // Redis 캐시 서비스
  private readonly IConfiguration _configuration; // 설정 정보

  public AuthService(ApplicationDbContext context, ICacheService cacheService, IConfiguration configuration)
  {
    _context = context;
    _cacheService = cacheService;
    _configuration = configuration;
  }

  public async Task<bool> RegisterAsync(RegisterRequestDto request)
  {
    // 이메일 중복 확인
    if (await _context.Users.AnyAsync(u => u.Email == request.Email))
      throw new Exception("이미 존재하는 이메일입니다.");

    // 닉네임 중복 확인
    if (await _context.Users.AnyAsync(u => u.Nickname == request.Nickname))
      throw new Exception("이미 존재하는 닉네임입니다.");

    // 비밀번호 해시
    var passwordHash = HashPassword(request.Password);

    // 유저 생성
    var user = new User
    {
      Email = request.Email,
      Nickname = request.Nickname,
      PasswordHash = passwordHash,
      IsActive = true,
      CreatedAt = DateTime.UtcNow
    };

    // DB 유저 저장
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();

    return true;
  }

  public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
  {
    // 로그인 시도 횟수 확인
    var loginAttemptKey = $"login_attempt:{request.Email}";
    var attempts = await _cacheService.GetAsync<int?>(loginAttemptKey);

    if ((attempts ?? 0) >= SecurityConstants.MAX_LOGIN_ATTEMPTS)
    {
      throw new Exception("너무 많은 로그인 시도가 있습니다. 나중에 다시 시도해주세요.");
    }

    // DB에서 유저 확인
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    // 유저 X or 비밀번호 일치 X
    if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
    {
      // 실패한 로그인 시도 횟수 증가(시도 횟수 유지하면서)
      await _cacheService.SetAsync(loginAttemptKey, (attempts ?? 0) + 1,
        TimeSpan.FromMinutes(SecurityConstants.LOGIN_LOCKOUT_MINUTES));
      
      throw new Exception("이메일 또는 비밀번호가 올바르지 않습니다.");
    }

    // 로그인 성공 시 Redis에서 시도 횟수 삭제
    await _cacheService.RemoveAsync(loginAttemptKey);

    // 마지막 로그인 시간 업데이트
    user.LastLoginAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    // JWT 토큰 생성
    var token = GenerateJwtToken(user);

    // Redis에 유저 세션 저장
    await _cacheService.SetAsync(
      $"session:{user.Email}", 
      new { Token = token, LastActivity = DateTime.UtcNow },
      TimeSpan.FromHours(SecurityConstants.JWT_EXPIRATION_HOURS)
    );

    // 클라이언트에 응답
    return new LoginResponseDto
    {
      Token = token,
      Email = user.Email,
      Nickname = user.Nickname,
      ExpiresAt = DateTime.UtcNow.AddHours(SecurityConstants.JWT_EXPIRATION_HOURS)
    };
  }

  public async Task<bool> ValidateTokenAsync(string token)
  {
    // 토큰 블랙리스트 확인
    var isBlacklisted = await _cacheService.ExistsAsync($"blacklist:{token}");
    if (isBlacklisted)
      return false;

    try
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(SecurityConstants.JWT_SECRET_KEY);

      tokenHandler.ValidateToken(token, new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = SecurityConstants.JWT_ISSUER,
        ValidAudience = SecurityConstants.JWT_AUDIENCE,
        ClockSkew = TimeSpan.Zero
      }, out _);

      return true;
    }
    catch
    {
      return false;
    }
  }

  public async Task LogoutAsync(int userId)
  {
    // 사용자 조회
    var user = await _context.Users.FindAsync(userId)
        ?? throw new Exception("User not found");

    // Redis에서 유저 세션 삭제
    await _cacheService.RemoveAsync($"session:{user.Email}");
    
    // 현재 토큰을 블랙리스트에 추가
    var sessionKey = $"session:{user.Email}";
    var session = await _cacheService.GetAsync<dynamic>(sessionKey);
    if (session != null)
    {
      await _cacheService.SetAsync(
        $"blacklist:{session.Token}", 
        true,
        TimeSpan.FromHours(SecurityConstants.JWT_EXPIRATION_HOURS)
      );
    }
  }

  private string GenerateJwtToken(User user)
  {
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(SecurityConstants.JWT_SECRET_KEY);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity(new[]
      {
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Nickname),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
      }),
      Expires = DateTime.UtcNow.AddHours(SecurityConstants.JWT_EXPIRATION_HOURS),
      SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
      Issuer = SecurityConstants.JWT_ISSUER,
      Audience = SecurityConstants.JWT_AUDIENCE
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
  }

  private bool VerifyPassword(string password, string passwordHash)
  {
    try
    {
      // 비밀번호와 해시된 비밀번호를 비교
      return Verify(password, passwordHash);
    }
    catch (Exception)
    {
      // 해시 형식이 잘못되었거나 다른 오류가 발생한 경우
      return false;
    }
  }
}