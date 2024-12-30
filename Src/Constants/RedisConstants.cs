namespace LoginServer.Constants;

public static class RedisConstants
{
    // Redis 관련 상수
    public const string REDIS_INSTANCE_NAME = "LoginServer";

    // Redis 키 형식
    public const string LOGIN_ATTEMPT_KEY_FORMAT = "login_attempt:{0}";
    public const string SESSION_KEY_FORMAT = "session:{0}";
    public const string BLACKLIST_KEY_FORMAT = "blacklist:{0}";
    
    // 기타 보안 관련 상수
    public const int MAX_LOGIN_ATTEMPTS = 5;
    public const int LOGIN_LOCKOUT_MINUTES = 30;
}