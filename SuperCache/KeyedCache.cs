using SuperCache.Policies;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SuperCache
{
    public interface IKeyedCache
    {
        void PurgeAll();
        void ExpireAll();
        ICacheExpirationPolicy ExpirationPolicy { get; set; }
        ICacheFetchRetryPolicy FetchRetryPolicy { get; set; }
        ICacheValuePolicy ValuePolicy { set; }
    }

    public interface IKeyedCache<TResult>
    {
        ICacheValuePolicy<TResult> ValuePolicy { get; set; }
    }

    public class KeyedCache<TSource, TKey, TResult> : IKeyedCache, IKeyedCache<TResult>
        where TKey : IFetchable<TSource, TResult>
    {
        private readonly TSource _source;
        private readonly ConcurrentDictionary<TKey, KeyedCacheSlot> _cache = new ConcurrentDictionary<TKey, KeyedCacheSlot>();
        private readonly LinkedList<TKey> _keys = new LinkedList<TKey>();
        private ICacheExpirationPolicy _expirationPolicy;
        private ICacheFetchRetryPolicy _fetchRetryPolicy;
        private ICacheValuePolicy<TResult> _valuePolicy;

        public KeyedCache(TSource source)
        {
            _source = source;
            _expirationPolicy = new NeverExpireCacheExpirationPolicy();
            _fetchRetryPolicy = new AlwaysCacheFetchRetryPolicy();
            _valuePolicy = new IdentityCacheValuePolicy<TResult>();
        }

        private KeyedCacheSlot GetOrCreateSlot(TKey key)
        {
            if (!_cache.TryGetValue(key, out var slot))
            {
                var newSlot = new KeyedCacheSlot();
                slot = _cache.GetOrAdd(key, newSlot);

                // track the keys we've inserted separately
                // only need to do this if we actually ended up
                // inserting
                if (ReferenceEquals(slot, newSlot))
                {
                    lock (_keys)
                    {
                        _keys.AddLast(key);
                    }
                }
            }

            return slot;
        }

        public TResult Get(TKey key)
        {
            var slot = GetOrCreateSlot(key);

            if (!slot.HasCachedValue || _expirationPolicy.IsExpired(slot))
            {
                // TODO: this is probably right in most instances, but may not be in some cases
                // TODO: change this so it can be modified by setting a policy
                lock (slot)
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
                slot.CachedObject = default(TResult);

                throw;
            }
            finally
            {
                _fetchRetryPolicy.FetchAttempted(slot, success);
            }
        }

        public TResult Purge(TKey key)
        {
            _cache.TryRemove(key, out _);
            return default;
        }

        public TResult Expire(TKey key)
        {
            if (_cache.TryGetValue(key, out var slot))
            {
                lock (slot)
                {
                    _expirationPolicy.Expire(slot);
                    return (TResult)slot.CachedObject;
                }
            }
            return default;
        }

        public TResult NoOp(TKey ignored)
        {
            return default;
        }

        public TResult Insert(TResult result, TKey key)
        {
            var slot = GetOrCreateSlot(key);

            lock (slot)
            {
                slot.LastFetchAttempt = TimeProvider.UtcNow;
                slot.CachedObject = result;
                slot.HasCachedValue = true;
                slot.LastFetchSuccess = TimeProvider.UtcNow;
                slot.LastFetchException = null;
                _expirationPolicy.Fetched(slot);
                slot.LastAccess = TimeProvider.UtcNow;
            }

            return result;
        }

        #region IKeyedCache members
        ICacheExpirationPolicy IKeyedCache.ExpirationPolicy
        {
            get { return _expirationPolicy; }
            set { _expirationPolicy = value ?? new NeverExpireCacheExpirationPolicy(); }
        }

        ICacheFetchRetryPolicy IKeyedCache.FetchRetryPolicy
        {
            get { return _fetchRetryPolicy; }
            set { _fetchRetryPolicy = value ?? new AlwaysCacheFetchRetryPolicy(); }
        }

        ICacheValuePolicy<TResult> IKeyedCache<TResult>.ValuePolicy
        {
            get { return _valuePolicy; }
            set { _valuePolicy = value ?? new IdentityCacheValuePolicy<TResult>(); }
        }

        ICacheValuePolicy IKeyedCache.ValuePolicy
        {
            set
            {
                switch (value)
                {
                    case null:
                        _valuePolicy = new IdentityCacheValuePolicy<TResult>();
                        break;
                    case ICacheValuePolicy<TResult> specific:
                        _valuePolicy = specific;
                        break;
                    default:
                        _valuePolicy = new ShimCacheValuePolicy<TResult>(value);
                        break;
                }
            }
        }

        void IKeyedCache.PurgeAll()
        {
            _keys.Clear();
            _cache.Clear();
        }

        void IKeyedCache.ExpireAll()
        {
            _expirationPolicy.ExpireAll();
        }
        #endregion IKeyedCache members
    }
}
