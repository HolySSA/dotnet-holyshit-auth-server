using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using LoginServer.Services.Interfaces;
using LoginServer.Models.DTOs;

namespace LoginServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
  private readonly IAuthService _authService;
  private readonly ILogger<AuthController> _logger;

  public AuthController(IAuthService authService, ILogger<AuthController> logger)
  {
    _authService = authService;
    _logger = logger;
  }

  /// <summary>
  /// 회원가입 API
  /// POST: api/auth/register
  /// </summary>
  [HttpPost("register")]
  [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
  public async Task<ActionResult<bool>> Register([FromBody] RegisterRequestDto request)
  {
    try
    {
      _logger.LogInformation("Register attempt for user: {Email}", request.Email);
      
      await _authService.RegisterAsync(request);
      
      _logger.LogInformation("Register successful for user: {Email}", request.Email);
      return Ok(true);
    }
    catch (Exception ex)
    {
      _logger.LogWarning("Register failed for user: {Email}, Reason: {Message}", request.Email, ex.Message);
          
      return BadRequest(new ErrorResponseDto { Message = ex.Message });
    }
  }

  /// <summary>
  /// 로그인 API
  /// POST: api/auth/login
  /// </summary>
  [HttpPost("login")]
  [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
  public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
  {
    try
    {
      _logger.LogInformation("Login attempt for user: {Email}", request.Email);
      
      var result = await _authService.LoginAsync(request);
      
      _logger.LogInformation("Login successful for user: {Email}", request.Email);
      return Ok(result);
    }
    catch (Exception ex)
    {
      _logger.LogWarning("Login failed for user: {Email}, Reason: {Message}", 
          request.Email, ex.Message);
          
      return BadRequest(new ErrorResponseDto { Message = ex.Message });
    }
  }

  /// <summary>
  /// 로그아웃 API
  /// POST: api/auth/logout
  /// </summary>
  [HttpPost("logout")]
  [Authorize] // JWT 토큰이 필요한 엔드포인트
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> Logout()
  {
    try
    {
      // 토큰에서 사용자 ID 추출
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
          ?? throw new Exception("User ID not found in token");
          
      var userId = int.Parse(userIdClaim);
      
      _logger.LogInformation("Logout attempt for user ID: {UserId}", userId);
      
      await _authService.LogoutAsync(userId);
      
      _logger.LogInformation("Logout successful for user ID: {UserId}", userId);
      return Ok(new { message = "Logged out successfully" });
    }
    catch (Exception ex)
    {
      _logger.LogWarning("Logout failed for user, Reason: {Message}", ex.Message);
      return BadRequest(new { message = ex.Message });
    }
  }

  /// <summary>
  /// 토큰 검증 API
  /// POST: api/auth/validate-token
  /// </summary>
  [HttpPost("validate-token")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<ActionResult<bool>> ValidateToken([FromBody] string token)
  {
    try
    {
      var isValid = await _authService.ValidateTokenAsync(token);
      return Ok(isValid);
    }
    catch (Exception ex)
    {
      _logger.LogWarning("Token validation failed, Reason: {Message}", ex.Message);
      return BadRequest(new { message = ex.Message });
    }
  }
}