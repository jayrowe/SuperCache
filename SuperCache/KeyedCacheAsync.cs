using SuperCache.Policies;
using System;
using System.Threading.Tasks;

namespace SuperCache
{
    public class KeyedCacheAsync<TSource, TKey, TResult> : KeyedCacheBase<TSource, TKey, TResult>
        where TKey : IFetchableAsync<TSource, TResult>
    {
        public KeyedCacheAsync(TSource source)
            : base(source)
        {
        }

        public async Task<TResult> GetAsync(TKey key)
        {
            var slot = GetOrCreateSlot(key);

            if (!slot.HasCachedValue || _expirationPolicy.IsExpired(slot))
            {
                // TODO: this is probably right in most instances, but may not be in some cases
                // TODO: change this so it can be modified by setting a policy
                lock (slot.Lock)
                {
                    if (!slot.HasCachedValue || slot.Task == null || _expirationPolicy.IsExpired(slot))
                    {
                        AttemptFetchAsync(key, slot);
                    }
                }

                await slot.Task;
            }

            slot.LastAccess = TimeProvider.UtcNow;

            return _valuePolicy.Retrieve(slot.CachedObject);
        }

        private void AttemptFetchAsync(TKey key, KeyedCacheSlot slot)
        {
            if (slot.LastFetchException != null && !_fetchRetryPolicy.ShouldRetry(slot))
            {
                throw new CacheFetchRetrySuppressedException(slot.LastFetchException);
            }

            slot.LastFetchAttempt = TimeProvider.UtcNow;

            try
            {
                slot.Task = FetchAsync(key, slot);
            }
            catch (Exception ex)
            {
                if (!HandleFetchException(ex, slot))
                {
                    throw;
                }
            }
        }

        private async Task FetchAsync(TKey key, KeyedCacheSlot slot)
        {
            try
            {
                slot.CachedObject = _valuePolicy.Store(await key.FetchAsync(_source));
                slot.HasCachedValue = true;
                slot.LastFetchSuccess = TimeProvider.UtcNow;
                slot.LastFetchException = null;
                _fetchRetryPolicy.FetchAttempted(slot, true);
                _expirationPolicy.Fetched(slot);
            }
            catch (Exception ex)
            {
                if (!HandleFetchException(ex, slot))
                {
                    throw;
                }
            }
        }

        private bool HandleFetchException(Exception ex, KeyedCacheSlot slot)
        {
            _fetchRetryPolicy.FetchAttempted(slot, false);

            slot.LastFetchException = ex;

            if (slot.HasCachedValue && _expirationPolicy.AllowExpiredResult(slot))
            {
                return true;
            }

            slot.HasCachedValue = false;

            return false;
        }
    }
}
