namespace RAG.Services
{
    public interface IRateLimitService
    {
        Task<bool> IsAllowedAsync(string key, int maxRequests, TimeSpan window);
        Task<int> GetRemainingRequestsAsync(string key, int maxRequests, TimeSpan window);
    }

    public class RateLimitService : IRateLimitService
    {
        private readonly Dictionary<string, List<DateTime>> _requestHistory = new();
        private readonly object _lockObject = new();

        public Task<bool> IsAllowedAsync(string key, int maxRequests, TimeSpan window)
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                var cutoff = now.Subtract(window);

                if (!_requestHistory.ContainsKey(key))
                {
                    _requestHistory[key] = new List<DateTime>();
                }

                _requestHistory[key] = _requestHistory[key]
                    .Where(timestamp => timestamp > cutoff)
                    .ToList();

                if (_requestHistory[key].Count >= maxRequests)
                {
                    return Task.FromResult(false);
                }

                _requestHistory[key].Add(now);
                return Task.FromResult(true);
            }
        }

        public Task<int> GetRemainingRequestsAsync(string key, int maxRequests, TimeSpan window)
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                var cutoff = now.Subtract(window);

                if (!_requestHistory.ContainsKey(key))
                {
                    return Task.FromResult(maxRequests);
                }

                _requestHistory[key] = _requestHistory[key]
                    .Where(timestamp => timestamp > cutoff)
                    .ToList();

                var remaining = maxRequests - _requestHistory[key].Count;
                return Task.FromResult(Math.Max(0, remaining));
            }
        }
    }
} 