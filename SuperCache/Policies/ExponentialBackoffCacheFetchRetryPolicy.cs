
using System;

namespace SuperCache.Policies
{
    public class ExponentialBackoffCacheFetchRetryPolicy : ICacheFetchRetryPolicy
    {
        private static readonly object _contextKey = new object();
        private readonly long _initialRetryIntervalTicks;
        private readonly long _maxRetryIntervalTicks;

        public ExponentialBackoffCacheFetchRetryPolicy(TimeSpan initialRetryInterval, TimeSpan maxRetryInterval)
        {
            if (initialRetryInterval < TimeSpan.Zero)
            {
                throw new ArgumentException(
                    "initialRetryInterval cannot be negative",
                    "initialRetryInterval");
            }

            if (maxRetryInterval < initialRetryInterval)
            {
                throw new ArgumentException(
                    "maxRetryInterval cannot be less than initialRetryInterval",
                    "maxRetryInterval");
            }

            _initialRetryIntervalTicks = initialRetryInterval.Ticks;
            _maxRetryIntervalTicks = maxRetryInterval.Ticks;
        }

        public void FetchAttempted(KeyedCacheSlot slot, bool success)
        {
            if (success)
            {
                slot.Context[_contextKey] = null;
            }
            else
            {
                FetchFailed(slot);
            }
        }

        public bool ShouldRetry(KeyedCacheSlot slot)
        {
            var context = ExtractPolicyState(slot);

            if (context == null)
            {
                return true;
            }

            return context.NextRetry <= TimeProvider.UtcNow;
        }

        private void FetchFailed(KeyedCacheSlot slot)
        {
            PolicyState context = ExtractPolicyState(slot);

            if (context == null)
            { 
                context = new PolicyState
                {
                    NextRetry = TimeProvider.UtcNow.AddTicks(_initialRetryIntervalTicks),
                    NextInterval = _initialRetryIntervalTicks * 2
                };

                slot.Context[_contextKey] = context;
            }
            else
            {
                context.NextRetry = TimeProvider.UtcNow.AddTicks(context.NextInterval);
                context.NextInterval = Math.Min(context.NextInterval * 2, _maxRetryIntervalTicks);
            }
        }

        private static PolicyState ExtractPolicyState(KeyedCacheSlot slot)
        {
            if (slot.Context.TryGetValue(_contextKey, out var contextObject))
            {
                return contextObject as PolicyState;
            }

            return null;
        }

        private class PolicyState
        {
            public DateTime NextRetry;
            public long NextInterval;
        }
    }
}
