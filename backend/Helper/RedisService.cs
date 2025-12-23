using System.Text.Json;
using Backend.Const;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Backend.Helper
{
    /// <summary>
    /// Light wrapper around StackExchange.Redis for simple get/set patterns with optional key prefixing.
    /// Registered as a singleton in DI (see Startup).
    /// </summary>
    public class RedisService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisOptions _options;
        private readonly ILogger<RedisService> _logger;

        public RedisService(
            IConnectionMultiplexer connectionMultiplexer,
            IOptions<RedisOptions> options,
            ILogger<RedisService> logger)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is not configured.");
            }
        }

        private IDatabase Database => _connectionMultiplexer.GetDatabase(_options.Database ?? -1);

        private string BuildKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }

            return string.IsNullOrWhiteSpace(_options.KeyPrefix)
                ? key
                : $"{_options.KeyPrefix}:{key}";
        }

        public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            var namespacedKey = BuildKey(key);
            return await Database.StringSetAsync(namespacedKey, value, expiry);
        }

        public async Task<string?> GetStringAsync(string key)
        {
            var namespacedKey = BuildKey(key);
            var value = await Database.StringGetAsync(namespacedKey);
            return value.IsNull ? null : value.ToString();
        }

        public async Task<bool> RemoveAsync(string key)
        {
            var namespacedKey = BuildKey(key);
            return await Database.KeyDeleteAsync(namespacedKey);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var namespacedKey = BuildKey(key);
            return await Database.KeyExistsAsync(namespacedKey);
        }

        public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var payload = JsonSerializer.Serialize(value);
            return await SetStringAsync(key, payload, expiry);
        }

        public async Task<T?> GetObjectAsync<T>(string key)
        {
            var json = await GetStringAsync(key);

            if (json is null)
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize value for key {RedisKey}", key);
                return default;
            }
        }

        /// <summary>
        /// Remove all keys that start with the provided prefix (after namespacing). Useful to invalidate caches.
        /// </summary>
        public async Task<long> RemoveByPrefixAsync(string prefix)
        {
            var namespacedPrefix = BuildKey(prefix);
            var endpoints = _connectionMultiplexer.GetEndPoints();
            long removed = 0;

            foreach (var endpoint in endpoints)
            {
                var server = _connectionMultiplexer.GetServer(endpoint);
                if (!server.IsConnected)
                {
                    continue;
                }

                var keys = server.Keys(database: _options.Database ?? -1, pattern: $"{namespacedPrefix}*");
                foreach (var key in keys)
                {
                    if (await Database.KeyDeleteAsync(key))
                    {
                        removed++;
                    }
                }
            }

            return removed;
        }
    }
}
