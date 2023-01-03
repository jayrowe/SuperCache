using SuperCache.CodeGen;
using SuperCache.Policies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperCache
{
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

        public TransparentCacheBuilder<T> SetPolicy<TResult>(ICacheValuePolicy<TResult> valuePolicy, Func<T, TResult> selector)
        {
            _specific.Add(
                () =>
                {
                    IKeyedCache<TResult> cache = (IKeyedCache<TResult>)GetCache(selector);
                    cache.ValuePolicy = valuePolicy;
                });

            return this;
        }

        public TransparentCacheBuilder<T> SetPolicy<TResult>(ICacheValuePolicy<TResult> valuePolicy, Func<T, Task<TResult>> selector)
        {
            _specific.Add(
                () =>
                {
                    IKeyedCache<TResult> cache = (IKeyedCache<TResult>)GetCache(selector);
                    cache.ValuePolicy = valuePolicy;
                });

            return this;
        }

        public TransparentCacheBuilder<T> SetPolicy<TResult>(ICacheValuePolicy valuePolicy, Func<T, TResult> selector)
        {
            _specific.Add(
                () =>
                {
                    IKeyedCache cache = GetCache(selector);
                    cache.ValuePolicy = valuePolicy;
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

        public TransparentCacheBuilder<T> SetDefaultPolicy(ICacheValuePolicy outputPolicy)
        {
            _defaults.Add(
                () =>
                {
                    foreach (var collection in _source.GetAllCollections())
                    {
                        collection.ValuePolicy = outputPolicy;
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
