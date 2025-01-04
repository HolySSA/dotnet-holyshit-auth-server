namespace LoginServer.Services.Interfaces;

public interface ICacheService
{
  Task<T?> GetAsync<T>(string key);
  Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null);
  Task RemoveAsync(string key);
  Task<bool> ExistsAsync(string key);

  // Hash
  Task<bool> HashSetAsync(string key, string field, string value);
  Task<string?> HashGetAsync(string key, string field);
  Task<Dictionary<string, string>> HashGetAllAsync(string key);
  Task HashDeleteAsync(string key, string field);
  Task SetHashExpirationAsync(string key, TimeSpan expirationTime);
}