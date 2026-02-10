using IntegratedAPI.DTOs;

using StackExchange.Redis;

namespace IntegratedAPI.Services
{
    public interface ICacheManagerService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<IEnumerable<string>> GetKeysAsync(string pattern = "*");
        Task ClearAllAsync();
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task<T?> GetOrSetAsync<T>(string key, Func<T> factory, TimeSpan? expiration = null);
        Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys);
        Task SetMultipleAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null);
        Task<TimeSpan?> GetTimeToLiveAsync(string key);
        Task<bool> ExtendExpirationAsync(string key, TimeSpan additionalTime);
        Task<RedisStatistics> GetStatisticsAsync();
        Task<IDatabase> GetDatabaseAsync();
        Task<bool> PingAsync();
    }

}
