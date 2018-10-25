namespace SuperCache.Policies
{
    public class NeverExpireCacheExpirationPolicy : ICacheExpirationPolicy
    {
        public bool AllowExpiredResult(KeyedCacheSlot slot)
        {
            return true;
        }

        public void Expire(KeyedCacheSlot slot)
        {
        }

        public void ExpireAll()
        {
        }

        public void Fetched(KeyedCacheSlot slot)
        {
        }

        public bool IsExpired(KeyedCacheSlot slot)
        {
            return false;
        }
    }
}
