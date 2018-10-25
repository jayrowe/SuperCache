namespace SuperCache.Policies
{
    public class AlwaysCacheFetchRetryPolicy : ICacheFetchRetryPolicy
    {
        public void FetchAttempted(KeyedCacheSlot slot, bool success)
        {
        }

        public bool ShouldRetry(KeyedCacheSlot slot)
        {
            return true;
        }
    }
}
