using SuperCache.Policies;
using System;

namespace SuperCache
{
    public class KeyedCacheSync<TSource, TKey, TResult> : KeyedCacheBase<TSource, TKey, TResult>
        where TKey : IFetchable<TSource, TResult>
    {
        public KeyedCacheSync(TSource source)
            :base(source)
        {
        }

        public TResult Get(TKey key)
        {
            var slot = GetOrCreateSlot(key);

            if (!slot.HasCachedValue || _expirationPolicy.IsExpired(slot))
            {
                // TODO: this is probably right in most instances, but may not be in some cases
                // TODO: change this so it can be modified by setting a policy
                lock (slot.Lock)
                {
                    if (!slot.HasCachedValue || _expirationPolicy.IsExpired(slot))
                    {
                        AttemptFetch(key, slot);
                    }
                }
            }

            slot.LastAccess = TimeProvider.UtcNow;

            return _valuePolicy.Retrieve(slot.CachedObject);
        }

        private void AttemptFetch(TKey key, KeyedCacheSlot slot)
        {
            if (slot.LastFetchException != null && !_fetchRetryPolicy.ShouldRetry(slot))
            {
                throw new CacheFetchRetrySuppressedException(slot.LastFetchException);
            }

            bool success = false;
            slot.LastFetchAttempt = TimeProvider.UtcNow;

            try
            {
                slot.CachedObject = _valuePolicy.Store(key.Fetch(_source));
                slot.HasCachedValue = true;
                slot.LastFetchSuccess = TimeProvider.UtcNow;
                slot.LastFetchException = null;
                _expirationPolicy.Fetched(slot);
                success = true;
            }
            catch (Exception ex)
            {
                slot.LastFetchException = ex;

                if (slot.HasCachedValue && _expirationPolicy.AllowExpiredResult(slot))
                {
                    return;
                }

                slot.HasCachedValue = false;

                throw;
            }
            finally
            {
                _fetchRetryPolicy.FetchAttempted(slot, success);
            }
        }
    }
}
