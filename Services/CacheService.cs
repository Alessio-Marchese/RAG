namespace RAG.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
    }

    public class CacheService : ICacheService
    {
        private readonly Dictionary<string, CacheItem> _cache = new();
        private readonly object _lockObject = new();

        public Task<T?> GetAsync<T>(string key)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var item) && !item.IsExpired)
                {
                    return Task.FromResult((T?)item.Value);
                }

                if (item?.IsExpired == true)
                {
                    _cache.Remove(key);
                }

                return Task.FromResult<T?>(default);
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            lock (_lockObject)
            {
                DateTime? expirationTime = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null;
                _cache[key] = new CacheItem(value, expirationTime);
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            lock (_lockObject)
            {
                _cache.Remove(key);
            }
            return Task.CompletedTask;
        }

        private class CacheItem
        {
            public object Value { get; }
            public DateTime? ExpirationTime { get; }

            public CacheItem(object value, DateTime? expirationTime)
            {
                Value = value;
                ExpirationTime = expirationTime;
            }

            public bool IsExpired => ExpirationTime.HasValue && DateTime.UtcNow > ExpirationTime.Value;
        }
    }
} 