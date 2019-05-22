using SuperCache.Policies;

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

    public interface IKeyedCache<TResult> : IKeyedCache
    {
        new ICacheValuePolicy<TResult> ValuePolicy { get; set; }
    }
}
