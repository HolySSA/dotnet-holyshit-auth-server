using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using LoginServer.Constants;
using LoginServer.Data;
using LoginServer.Models.Settings;
using LoginServer.Services.Implementations;
using LoginServer.Services.Interfaces;
using LoginServer.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace LoginServer.Extensions;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// PostgreSQL 설정
  /// </summary>
  public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<ApplicationDbContext>(options =>
      options.UseNpgsql(configuration.GetSection("PostgresSettings:ConnectionString").Value));

    return services;
  }

  /// <summary>
  /// Redis 설정
  /// </summary>
  public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
  {
    // Redis 연결 설정
    services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
      var redisConfig = ConfigurationOptions.Parse(configuration.GetSection("RedisSettings:ConnectionString").Value!);
      return ConnectionMultiplexer.Connect(redisConfig);
    });

    // Redis 캐시 서비스 등록
    services.AddScoped<ICacheService, RedisCacheService>();
    return services;
  }

  public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
  {
    // JWT 설정 등록
    services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
    var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
      ?? throw new InvalidOperationException("JwtSettings configuration is missing");
    // 로비 서버 설정 등록
    services.Configure<LobbyServerSettings>(configuration.GetSection("LobbyServerSettings"));
    // 인증(회원가입/로그인) 서비스 등록
    services.AddScoped<IAuthService, AuthService>();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        ClockSkew = TimeSpan.Zero
      };
    });

    return services;
  }

  /// <summary>
  /// 컨트롤러 / FluentValidation 설정
  /// </summary>
  public static IServiceCollection AddValidation(this IServiceCollection services)
  {
    services.AddControllers(options =>
    {
      options.Filters.Add<FluentValidationFilter>(); // FluentValidation 필터 등록
    }).ConfigureApiBehaviorOptions(options =>
    {
      options.SuppressModelStateInvalidFilter = true; // 기본 모델 검증 동작 비활성화
    });

    services.AddFluentValidationAutoValidation(config =>
    {
      config.DisableDataAnnotationsValidation = true; // 기본 ASP.NET Core의 모델 검증을 비활성화
    });

    services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>(); // FluentValidation 검증기 등록

    return services;
  }
}