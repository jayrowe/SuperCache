using SuperCache.CodeGen;
using System;

namespace SuperCache
{
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
            _cached = (ITransparentCacheInternal<T>)Activator.CreateInstance(_generator.CacheType, source);
        }

        public T Cached { get { return _cached.GetCachedInterface(); } }

        public T Purge { get { return _cached.GetPurgeInterface(); } }

        public T Expire { get { return _cached.GetExpireInterface(); } }

        public void Insert<CachedObject>(CachedObject result, Func<T, CachedObject> selector)
        {
            selector(_cached.GetInsertInterface(result));
        }

        public void PurgeAll()
        {
            foreach (var cache in _cached.GetAllCollections())
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
            foreach (var cache in _cached.GetAllCollections())
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


}
