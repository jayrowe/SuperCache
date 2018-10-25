namespace SuperCache.Policies
{
    public interface ICacheExpirationPolicy
    {
        void Fetched(KeyedCacheSlot slot);
        bool IsExpired(KeyedCacheSlot slot);
        bool AllowExpiredResult(KeyedCacheSlot slot);
        void Expire(KeyedCacheSlot slot);
        void ExpireAll();
    }
}
