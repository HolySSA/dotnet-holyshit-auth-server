using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using LoginServer.Services.Interfaces;
using StackExchange.Redis;

namespace LoginServer.Services.Implementations;

public class RedisCacheService : ICacheService
{
  private readonly IConnectionMultiplexer _redis;
  private readonly IDatabase _db;

  public RedisCacheService(IConnectionMultiplexer redis)
  {
    _redis = redis;
    _db = redis.GetDatabase();
  }

  /// <summary>
  /// 데이터 조회
  /// </summary>
  public async Task<T?> GetAsync<T>(string key)
  {
    var value = await _db.StringGetAsync(key);
    return value.IsNull ? default : JsonSerializer.Deserialize<T>(value!);
  }

  /// <summary>
  /// 데이터 저장
  /// </summary>
  public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
  {
    var jsonValue = JsonSerializer.Serialize(value);
    await _db.StringSetAsync(key, jsonValue, expirationTime);
  }

  /// <summary>
  /// 데이터 삭제
  /// </summary>
  public async Task RemoveAsync(string key)
  {
    await _db.KeyDeleteAsync(key);
  }

  /// <summary>
  /// 패턴으로 데이터 삭제
  /// </summary>
  public async Task RemoveByPatternAsync(string pattern)
  {
    var server = _redis.GetServer(_redis.GetEndPoints().First());
    var keys = server.Keys(pattern: pattern);
    
    foreach (var key in keys)
    {
      await _db.KeyDeleteAsync(key);
    }
  }

  /// <summary>
  /// 해당 키-데이터 존재 여부
  /// </summary>
  public async Task<bool> ExistsAsync(string key)
  {
    return await _db.KeyExistsAsync(key);
  }

  public async Task<bool> HashSetAsync(string key, string field, string value)
  {
    return await _db.HashSetAsync(key, field, value);
  }

  public async Task<string?> HashGetAsync(string key, string field)
  {
    var value = await _db.HashGetAsync(key, field);
    return value.HasValue ? value.ToString() : null;
  }

  public async Task<Dictionary<string, string>> HashGetAllAsync(string key)
  {
    var hashEntries = await _db.HashGetAllAsync(key);
    return hashEntries.ToDictionary(
      he => he.Name.ToString(),
      he => he.Value.ToString()
    );
  }

  public async Task HashDeleteAsync(string key, string field)
  {
    await _db.HashDeleteAsync(key, field);
  }

  public async Task SetHashExpirationAsync(string key, TimeSpan expirationTime)
  {
    await _db.KeyExpireAsync(key, expirationTime);
  }
}