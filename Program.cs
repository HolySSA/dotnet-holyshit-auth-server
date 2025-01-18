using LoginServer.Middleware;
using LoginServer.Extensions;
using LoginServer.Models.Settings;
using LoginServer.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 서비스 등록
builder.Services
    .AddDatabase(builder.Configuration)
    .AddRedisCache(builder.Configuration)
    .AddAuthenticationServices(builder.Configuration)
    .AddValidation()
    .AddSwaggerServices();

var app = builder.Build();

// 애플리케이션 종료 시 Redis 정리
app.Lifetime.ApplicationStopping.Register(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        // Redis 키 패턴으로 삭제
        await cacheService.RemoveByPatternAsync("gameserver:*");  // 게임 서버
        await cacheService.RemoveAsync("game_servers");
        await cacheService.RemoveByPatternAsync("user:*"); // 유저
        await cacheService.RemoveByPatternAsync("session:*"); // 세션
        
        app.Logger.LogInformation("Redis cache cleaned up successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error cleaning up Redis cache");
    }
});

// 미들웨어 설정
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>(); // 예외처리 미들웨어
app.UseHttpsRedirection(); // HTTPS 리디렉션 미들웨어
app.UseAuthentication(); // 인증 미들웨어
app.UseAuthorization(); // 인가 미들웨어
app.MapControllers(); // 컨트롤러 매핑

app.Run();