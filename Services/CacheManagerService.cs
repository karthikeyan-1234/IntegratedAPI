using IntegratedAPI.DTOs;

using Microsoft.Extensions.Caching.Distributed;

using StackExchange.Redis;

using System.Diagnostics;
using System.Text.Json;

namespace IntegratedAPI.Services
{
    public class CacheManagerService : ICacheManagerService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly ILogger<CacheManagerService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public CacheManagerService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer redisConnection,
            ILogger<CacheManagerService> logger)
        {
            _distributedCache = distributedCache;
            _redisConnection = redisConnection;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Get a value from cache by key
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var cachedData = await _distributedCache.GetStringAsync(key);
                stopwatch.Stop();

                if (cachedData == null)
                {
                    _logger.LogDebug("Cache miss for key: {Key} (took {ElapsedMs}ms)", key, stopwatch.ElapsedMilliseconds);
                    return default;
                }

                _logger.LogDebug("Cache hit for key: {Key} (took {ElapsedMs}ms)", key, stopwatch.ElapsedMilliseconds);
                return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache entry for key: {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// Set a value in cache with optional expiration
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var options = new DistributedCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.SetAbsoluteExpiration(expiration.Value);
                }
                else
                {
                    // Default expiration: 1 hour
                    options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                }

                await _distributedCache.SetStringAsync(key, serializedValue, options);
                _logger.LogDebug("Cache set for key: {Key} with expiration: {Expiration}", key, expiration?.ToString() ?? "1 hour");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set cache entry for key: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Remove a cache entry by key
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
                _logger.LogDebug("Cache removed for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove cache entry for key: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Check if a key exists in cache
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var value = await _distributedCache.GetAsync(key);
                return value != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check existence for key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Get keys matching a pattern (using Redis directly)
        /// </summary>
        public async Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());

                var keys = new List<string>();
                await foreach (var key in server.KeysAsync(pattern: pattern))
                {
                    keys.Add(key.ToString());
                }

                return keys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get keys with pattern: {Pattern}", pattern);
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Clear all cache entries (use with caution!)
        /// </summary>
        public async Task ClearAllAsync()
        {
            try
            {
                var keys = await GetKeysAsync();
                var db = _redisConnection.GetDatabase();

                foreach (var key in keys)
                {
                    await db.KeyDeleteAsync(key);
                }

                _logger.LogInformation("Cleared all cache entries. Total: {Count}", keys.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear all cache entries");
                throw;
            }
        }

        /// <summary>
        /// Get value from cache or set it using factory method
        /// </summary>
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }

            return value;
        }

        /// <summary>
        /// Get value from cache or set it using synchronous factory method
        /// </summary>
        public async Task<T?> GetOrSetAsync<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var value = factory();
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }

            return value;
        }

        /// <summary>
        /// Get multiple values from cache
        /// </summary>
        public async Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, T?>();
            foreach (var key in keys)
            {
                result[key] = await GetAsync<T>(key);
            }
            return result;
        }

        /// <summary>
        /// Set multiple values in cache
        /// </summary>
        public async Task SetMultipleAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null)
        {
            foreach (var item in items)
            {
                await SetAsync(item.Key, item.Value, expiration);
            }
        }

        /// <summary>
        /// Get time to live for a key
        /// </summary>
        public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var ttl = await db.KeyTimeToLiveAsync(key);
                return ttl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get TTL for key: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Extend expiration time for a key
        /// </summary>
        public async Task<bool> ExtendExpirationAsync(string key, TimeSpan additionalTime)
        {
            try
            {
                var currentTtl = await GetTimeToLiveAsync(key);
                if (!currentTtl.HasValue)
                {
                    return false;
                }

                var newExpiration = currentTtl.Value + additionalTime;
                var db = _redisConnection.GetDatabase();
                return await db.KeyExpireAsync(key, newExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extend expiration for key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Get Redis statistics
        /// </summary>
        public async Task<RedisStatistics> GetStatisticsAsync()
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var info = await db.ExecuteAsync("INFO", "stats");
                var stats = info.ToString();

                return new RedisStatistics
                {
                    ConnectedClients = ExtractValue(stats, "connected_clients"),
                    UsedMemory = ExtractValue(stats, "used_memory_human"),
                    TotalConnectionsReceived = ExtractValue(stats, "total_connections_received"),
                    TotalCommandsProcessed = ExtractValue(stats, "total_commands_processed"),
                    KeyspaceHits = ExtractValue(stats, "keyspace_hits"),
                    KeyspaceMisses = ExtractValue(stats, "keyspace_misses"),
                    UptimeInSeconds = ExtractValue(stats, "uptime_in_seconds"),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Redis statistics");
                return new RedisStatistics { Error = ex.Message };
            }
        }

        /// <summary>
        /// Get Redis database instance for advanced operations
        /// </summary>
        public Task<IDatabase> GetDatabaseAsync()
        {
            return Task.FromResult(_redisConnection.GetDatabase());
        }

        /// <summary>
        /// Ping Redis server to test connectivity
        /// </summary>
        public async Task<bool> PingAsync()
        {
            try
            {
                var db = _redisConnection.GetDatabase();
                var result = await db.PingAsync();
                return result.TotalMilliseconds > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ping Redis");
                return false;
            }
        }

        /// <summary>
        /// Helper method to extract values from Redis INFO command
        /// </summary>
        private static string ExtractValue(string info, string key)
        {
            var lines = info.Split('\n');
            var line = lines.FirstOrDefault(l => l.StartsWith(key + ":"));
            return line?.Split(':').LastOrDefault()?.Trim() ?? "N/A";
        }
    }
}
