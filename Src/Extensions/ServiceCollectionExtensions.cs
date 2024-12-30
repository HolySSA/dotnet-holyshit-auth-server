using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using LoginServer.Constants;
using LoginServer.Data;
using LoginServer.Services.Implementations;
using LoginServer.Services.Interfaces;
using LoginServer.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
    services.AddStackExchangeRedisCache(options =>
    {
      options.Configuration = configuration.GetSection("RedisSettings:ConnectionString").Value;
      options.InstanceName = configuration.GetSection("RedisSettings:InstanceName").Value;
    });
    
    services.AddScoped<ICacheService, RedisCacheService>(); // Redis 캐시 서비스 등록
    
    return services;
  }

  public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
  {
    services.AddScoped<IAuthService, AuthService>(); // 인증(회원가입/로그인) 서비스 등록
    
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(SecurityConstants.JWT_SECRET_KEY)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = SecurityConstants.JWT_ISSUER,
        ValidAudience = SecurityConstants.JWT_AUDIENCE,
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
    services.AddControllers(options => {
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