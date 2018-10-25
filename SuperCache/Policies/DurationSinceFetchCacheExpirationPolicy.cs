using System;

namespace SuperCache.Policies
{
    public class DurationSinceFetchCacheExpirationPolicy : ICacheExpirationPolicy
    {
        private static readonly object _expirationStateKey = new object();
        private readonly TimeSpan _maximumDuration;
        private readonly bool _allowExpiredResult;
        private DateTime _minimumInsertTime = DateTime.MinValue;

        public DurationSinceFetchCacheExpirationPolicy(TimeSpan maximumDuration)
            : this(maximumDuration, true)
        {
        }

        public DurationSinceFetchCacheExpirationPolicy(TimeSpan maximumDuration, bool allowExpiredResult)
        {
            _maximumDuration = maximumDuration;
            _allowExpiredResult = allowExpiredResult;
        }

        public bool AllowExpiredResult(KeyedCacheSlot slot)
        {
            return _allowExpiredResult;
        }

        public void Expire(KeyedCacheSlot slot)
        {
            ((ExpirationState)slot.Context[_expirationStateKey]).ExpirationTime = TimeProvider.UtcNow;
        }

        public void ExpireAll()
        {
            _minimumInsertTime = TimeProvider.UtcNow;
        }

        public void Fetched(KeyedCacheSlot slot)
        {
            var now = TimeProvider.UtcNow;

            slot.Context[_expirationStateKey] = new ExpirationState
            {
                InsertTime = now,
                ExpirationTime = now + _maximumDuration
            };
        }

        public bool IsExpired(KeyedCacheSlot slot)
        {
            var state = slot.Context[_expirationStateKey] as ExpirationState;
            var now = TimeProvider.UtcNow;
            return state == null || now >= state.ExpirationTime || _minimumInsertTime >= state.InsertTime;
        }

        private class ExpirationState
        {
            public DateTime ExpirationTime;
            public DateTime InsertTime;
        }
    }
}
