using SuperCache.CodeGen;
using SuperCache.Policies;
using System;
using System.Collections.Generic;

namespace SuperCache
{
    public interface ITransparentCacheInternal<T>
    {
        T GetCachedInterface();
        T GetPurgeInterface();
        T GetExpireInterface();
        T GetCollectionFinderInterface(KeyedCacheFetcher fetcher);
        T GetInsertInterface(object result);
        IKeyedCache[] GetAllCollections();
    }

    public class KeyedCacheFetcher
    {
        public IKeyedCache KeyedCache;
    }

    public class TransparentCache<T>
    {
        private readonly ITransparentCacheInternal<T> _cached;
        public static readonly CacheGenerator _generator = new CacheGenerator(typeof(T));

        public TransparentCache(ITransparentCacheInternal<T> cache)
        {
            _cached = cache;
        }

        public TransparentCache(T source)
        {
            _cached = (ITransparentCacheInternal<T>) Activator.CreateInstance(_generator.CacheType, source);
        }

        public T Cached {  get { return _cached.GetCachedInterface(); } }

        public T Purge { get { return _cached.GetPurgeInterface(); } }

        public T Expire { get { return _cached.GetExpireInterface(); } }

        public void Insert<CachedObject>(CachedObject result, Func<T, CachedObject> selector)
        {
            selector(_cached.GetInsertInterface(result));
        }

        public void PurgeAll()
        {
            foreach(var cache in _cached.GetAllCollections())
            {
                cache.PurgeAll();
            }
        }

        public void PurgeAll<TResult>(Func<T, TResult> selector)
        {
            var cache = GetTargetCollection(selector);

            if (cache != null)
            {
                cache.PurgeAll();
            }
        }

        public void ExpireAll()
        {
            foreach(var cache in _cached.GetAllCollections())
            {
                cache.ExpireAll();
            }
        }

        public void ExpireAll<TResult>(Func<T, TResult> selector)
        {
            var cache = GetTargetCollection(selector);

            if (cache != null)
            {
                cache.ExpireAll();
            }
        }

        private IKeyedCache GetTargetCollection<TResult>(Func<T, TResult> selector)
        {
            var fetcher = new KeyedCacheFetcher();
            var iface = _cached.GetCollectionFinderInterface(fetcher);
            selector(iface);

            return fetcher.KeyedCache;
        }
    }

    public class TransparentCacheBuilder<T>
    {
        internal static readonly CacheGenerator _generator = new CacheGenerator(typeof(T));
        private List<Action> _defaults = new List<Action>();
        private List<Action> _specific = new List<Action>();
        private ITransparentCacheInternal<T> _source;

        public TransparentCacheBuilder(T source)
        {
            _source = (ITransparentCacheInternal<T>)Activator.CreateInstance(_generator.CacheType, source);
        }

        public TransparentCacheBuilder<T> SetPolicy<TResult>(ICacheExpirationPolicy expirationPolicy, Func<T, TResult> selector)
        {
            _specific.Add(
                () =>
                {
                    IKeyedCache cache = GetCache(selector);
                    cache.ExpirationPolicy = expirationPolicy;
                });
            return this;
        }

        public TransparentCacheBuilder<T> SetPolicy<TResult>(ICacheFetchRetryPolicy fetchRetryPolicy, Func<T, TResult> selector)
        {
            _specific.Add(
                () =>
                {
                    IKeyedCache cache = GetCache(selector);
                    cache.FetchRetryPolicy = fetchRetryPolicy;
                });

            return this;
        }

        public TransparentCacheBuilder<T> SetDefaultPolicy(ICacheFetchRetryPolicy fetchRetryPolicy)
        {
            _defaults.Add(
                () =>
                {
                    foreach (var collection in _source.GetAllCollections())
                    {
                        collection.FetchRetryPolicy = fetchRetryPolicy;
                    }
                });

            return this;
        }

        public TransparentCacheBuilder<T> SetDefaultPolicy(ICacheExpirationPolicy expirationPolicy)
        {
            _defaults.Add(
                () =>
                {
                    foreach (var collection in _source.GetAllCollections())
                    {
                        collection.ExpirationPolicy = expirationPolicy;
                    }
                });

            return this;
        }

        public TransparentCache<T> Create()
        {
            _defaults.ForEach(a => a());
            _specific.ForEach(a => a());

            var cache = new TransparentCache<T>(_source);
            _source = null;
            return cache;
        }

        private IKeyedCache GetCache<TResult>(Func<T, TResult> selector)
        {
            if (_source == null)
            {
                throw new InvalidOperationException("cannot modify a cache after a call to Create()");
            }

            var fetcher = new KeyedCacheFetcher();
            var finder = _source.GetCollectionFinderInterface(fetcher);
            selector(finder);

            if (fetcher.KeyedCache == null)
            {
                throw new InvalidOperationException("selector does not reference a call that can be cached");
            }

            return fetcher.KeyedCache;
        }
    }
}
