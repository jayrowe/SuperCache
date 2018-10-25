using System;

namespace SuperCache.Policies
{
    public class ConstantIntervalCacheFetchRetryPolicy : ICacheFetchRetryPolicy
    {
        private static readonly object _lastFetchAttemptKey = new object();
        private readonly TimeSpan _interval;

        public ConstantIntervalCacheFetchRetryPolicy(TimeSpan interval)
        {
            if (interval < TimeSpan.Zero)
            {
                throw new ArgumentException(
                    "interval cannot be negative",
                    "interval");
            }

            _interval = interval;
        }

        public void FetchAttempted(KeyedCacheSlot slot, bool success)
        {
        }

        public bool ShouldRetry(KeyedCacheSlot slot)
        {
            return (slot.LastFetchAttempt + _interval) <= TimeProvider.UtcNow;
        }
    }
}
