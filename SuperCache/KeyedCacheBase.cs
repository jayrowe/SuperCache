using SuperCache.Policies;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SuperCache
{
    public abstract class KeyedCacheBase<TSource, TKey, TResult> : IKeyedCache, IKeyedCache<TResult>
    {
        protected readonly TSource _source;
        private readonly ConcurrentDictionary<TKey, KeyedCacheSlot> _cache = new ConcurrentDictionary<TKey, KeyedCacheSlot>();
        private readonly LinkedList<TKey> _keys = new LinkedList<TKey>();
        protected ICacheExpirationPolicy _expirationPolicy;
        protected ICacheFetchRetryPolicy _fetchRetryPolicy;
        protected ICacheValuePolicy<TResult> _valuePolicy;

        public KeyedCacheBase(TSource source)
        {
            _source = source;
            _expirationPolicy = new NeverExpireCacheExpirationPolicy();
            _fetchRetryPolicy = new AlwaysCacheFetchRetryPolicy();
            _valuePolicy = new IdentityCacheValuePolicy<TResult>();
        }

        protected KeyedCacheSlot GetOrCreateSlot(TKey key)
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

        public TResult Purge(TKey key)
        {
            _cache.TryRemove(key, out var slot);
            return (TResult)slot.CachedObject;
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
