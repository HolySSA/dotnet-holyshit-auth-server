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
using LoginServer.Models.Settings;
using Microsoft.Extensions.Options;

namespace LoginServer.Services.Implementations;

public class AuthService : IAuthService
{
  private readonly ApplicationDbContext _context; // DB 컨텍스트
  private readonly ICacheService _cacheService; // Redis 캐시 서비스
  private readonly JwtSettings _jwtSettings; // JWT 설정
  private readonly LobbyServerSettings _lobbyServerSettings; // 로비 서버 설정

  public AuthService(
    ApplicationDbContext context,
    ICacheService cacheService,
    IOptions<JwtSettings> jwtSettings,
    IOptions<LobbyServerSettings> lobbyServerSettings)
  {
    _context = context;
    _cacheService = cacheService;
    _jwtSettings = jwtSettings.Value;
    _lobbyServerSettings = lobbyServerSettings.Value;
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

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
      // 유저 생성
      var user = new User
      {
        Email = request.Email,
        Nickname = request.Nickname,
        PasswordHash = passwordHash
      };
      // DB 유저 저장
      await _context.Users.AddAsync(user);
      await _context.SaveChangesAsync();

      // 기본 캐릭터 부여 (RED-빨갱이)
      var defaultCharacter = new UserCharacter
      {
        UserId = user.Id,
        CharacterType = CharacterType.RED,
        PlayCount = 0,
        WinCount = 0,
        PurchasedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      await _context.UserCharacters.AddAsync(defaultCharacter);
      await _context.SaveChangesAsync();

      await transaction.CommitAsync();
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"회원가입 에러: {ex.Message}");

      await transaction.RollbackAsync();
      return false;
    }
  }

  public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
  {
    // 로그인 시도 횟수 확인 - Redis
    var loginAttemptKey = string.Format(RedisConstants.LOGIN_ATTEMPT_KEY_FORMAT, request.Email);
    var attempts = await _cacheService.GetAsync<int?>(loginAttemptKey);

    if ((attempts ?? 0) >= RedisConstants.MAX_LOGIN_ATTEMPTS)
      throw new Exception("너무 많은 로그인 시도가 있습니다. 나중에 다시 시도해주세요.");

    // 유저 검증 - PostgreSQL
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
    {
      // 실패 시 로그인 시도 횟수 증가 - Redis
      await _cacheService.SetAsync(loginAttemptKey, (attempts ?? 0) + 1, TimeSpan.FromMinutes(RedisConstants.LOGIN_LOCKOUT_MINUTES));
      throw new Exception("이메일 또는 비밀번호가 올바르지 않습니다.");
    }

    // 중복 로그인 체크
    var sessionKey = string.Format(RedisConstants.SESSION_KEY_FORMAT, user.Email);
    var existingSession = await _cacheService.HashGetAllAsync(sessionKey);
    if (existingSession.Count > 0)
    {
      // 중복 로그인 응답 반환
      return new LoginResponseDto
      {
        UserId = user.Id,
        Nickname = user.Nickname,
        Token = string.Empty,
        ExpiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
        LobbyHost = _lobbyServerSettings.Host,
        LobbyPort = _lobbyServerSettings.Port,
        Result = LoginResult.DuplicateLogin
      };
    }

    // 로그인 성공 시 시도 횟수 제거 - Redis
    await _cacheService.RemoveAsync(loginAttemptKey);

    // 마지막 로그인 시간 업데이트
    user.LastLoginAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    // JWT 토큰 생성
    var token = GenerateJwtToken(user);

    // 유저 세션 저장 - Redis
    await _cacheService.HashSetAsync(sessionKey, "Token", token);
    await _cacheService.HashSetAsync(sessionKey, "LastActivity", DateTime.UtcNow.ToString("O"));
    await _cacheService.SetHashExpirationAsync(sessionKey, TimeSpan.FromHours(_jwtSettings.ExpirationHours));

    // 클라이언트에 응답
    return new LoginResponseDto
    {
      UserId = user.Id,
      Nickname = user.Nickname,
      Token = token,
      ExpiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
      LobbyHost = _lobbyServerSettings.Host,
      LobbyPort = _lobbyServerSettings.Port,
      Result = LoginResult.Success
    };
  }

  public async Task<bool> ValidateTokenAsync(string token)
  {
    // 토큰 블랙리스트 확인
    var isBlacklisted = await _cacheService.ExistsAsync(string.Format(RedisConstants.BLACKLIST_KEY_FORMAT, token));
    if (isBlacklisted)
      return false;

    try
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

      tokenHandler.ValidateToken(token, new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = _jwtSettings.Issuer,
        ValidAudience = _jwtSettings.Audience,
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
    var user = await _context.Users.FindAsync(userId) ?? throw new Exception("User not found");

    // 캐릭터 통계 DB 업데이트
    await UpdateCharacterStatsToDbAsync(userId);

    // Redis에서 유저 세션 삭제
    await _cacheService.RemoveAsync(string.Format(RedisConstants.SESSION_KEY_FORMAT, user.Email));
    await _cacheService.RemoveAsync($"user:{userId}");
    await _cacheService.RemoveAsync($"user:{userId}:characters");

    // 현재 토큰을 블랙리스트에 추가
    var sessionKey = string.Format(RedisConstants.SESSION_KEY_FORMAT, user.Email);
    var session = await _cacheService.GetAsync<dynamic>(sessionKey);
    if (session != null)
    {
      await _cacheService.SetAsync(
        string.Format(RedisConstants.BLACKLIST_KEY_FORMAT, session.Token),
        true,
        TimeSpan.FromHours(_jwtSettings.ExpirationHours)
      );
    }
  }

  private string GenerateJwtToken(User user)
  {
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity(new[]
      {
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Nickname),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
      }),
      Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
      SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
      Issuer = _jwtSettings.Issuer,
      Audience = _jwtSettings.Audience
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

  private async Task UpdateCharacterStatsToDbAsync(int userId)
  {
    // Redis에서 캐릭터 통계 데이터 가져오기
    var characterStatsKey = $"user:{userId}:characters";
    var characterStats = await _cacheService.HashGetAllAsync(characterStatsKey);

    if (characterStats.Count == 0) return;

    // DB에서 유저의 캐릭터 데이터 가져오기
    var userCharacters = await _context.UserCharacters.Where(uc => uc.UserId == userId).ToListAsync();

    foreach (var stat in characterStats)
    {
      // Redis 데이터 파싱
      var characterType = int.Parse(stat.Key);
      var values = stat.Value.Split(':');
      var playCount = int.Parse(values[0]);
      var winCount = int.Parse(values[1]);

      // DB의 해당 캐릭터 찾기
      var character = userCharacters.FirstOrDefault(uc => uc.UserId == userId && (int)uc.CharacterType == characterType);

      if (character != null)
      {
        // 통계 업데이트
        character.PlayCount = playCount;
        character.WinCount = winCount;
        character.UpdatedAt = DateTime.UtcNow;
      }
    }

    // DB 변경사항 저장
    await _context.SaveChangesAsync();
  }
}