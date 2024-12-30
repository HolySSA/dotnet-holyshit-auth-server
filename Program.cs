using LoginServer.Middleware;
using LoginServer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 서비스 등록
builder.Services
    .AddDatabase(builder.Configuration)
    .AddRedisCache(builder.Configuration)
    .AddAuthenticationServices()
    .AddValidation()
    .AddSwaggerServices();

var app = builder.Build();

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