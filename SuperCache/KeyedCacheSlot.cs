using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        internal Task Task { get; set; }
        internal object Lock { get; } = new object();
    }
}
