using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using LoginServer.Services.Interfaces;

namespace LoginServer.Services.Implementations;

public class RedisCacheService : ICacheService
{
  private readonly IDistributedCache _cache;

  public RedisCacheService(IDistributedCache cache)
  {
    _cache = cache;
  }

  /// <summary>
  /// 데이터 조회
  /// </summary>
  public async Task<T?> GetAsync<T>(string key)
  {
    var value = await _cache.GetStringAsync(key);
    return value == null ? default : JsonSerializer.Deserialize<T>(value);
  }

  /// <summary>
  /// 데이터 저장
  /// </summary>
  public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
  {
    var options = new DistributedCacheEntryOptions();
    if (expirationTime.HasValue)
    {
      options.AbsoluteExpirationRelativeToNow = expirationTime;
    }

    var jsonValue = JsonSerializer.Serialize(value);
    await _cache.SetStringAsync(key, jsonValue, options);
  }

  /// <summary>
  /// 데이터 삭제
  /// </summary>
  public async Task RemoveAsync(string key)
  {
    await _cache.RemoveAsync(key);
  }

  /// <summary>
  /// 해당 키-데이터 존재 여부
  /// </summary>
  public async Task<bool> ExistsAsync(string key)
  {
    return await GetAsync<string>(key) != null;
  }
}