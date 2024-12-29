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
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL 설정
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetSection("PostgresSettings:ConnectionString").Value));

// Redis 설정
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetSection("RedisSettings:ConnectionString").Value;
    options.InstanceName = builder.Configuration.GetSection("RedisSettings:InstanceName").Value;
});

builder.Services.AddControllers(); // 컨트롤러 등록
builder.Services.AddEndpointsApiExplorer(); // Swagger 등록
builder.Services.AddScoped<ICacheService, RedisCacheService>(); // Redis 캐시 서비스 등록
builder.Services.AddScoped<IAuthService, AuthService>(); // 인증(로그인) 서비스 등록

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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

builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// Swagger 설정
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Login API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 미들웨어 설정
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // 인증 미들웨어
app.UseAuthorization(); // 인가 미들웨어

app.MapControllers();

app.Run();