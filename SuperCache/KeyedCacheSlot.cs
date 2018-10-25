using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SuperCache
{
    public class KeyedCacheSlot
    {
        public bool HasCachedValue { get; internal set; }
        public DateTime LastFetchSuccess { get; internal set; }
        public DateTime LastFetchAttempt { get; internal set; }
        public Exception LastFetchException { get; internal set; }
        public DateTime LastAccess { get; internal set; }
        public IDictionary<object, object> Context { get; } = new ConcurrentDictionary<object, object>();
        public object CachedObject { get; internal set; }
    }

    public interface IFetchable<TSource, TResult>
    {
        TResult Fetch(TSource source);
    }
}
