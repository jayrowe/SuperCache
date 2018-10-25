namespace SuperCache.Policies
{
    public interface ICacheFetchRetryPolicy
    {
        bool ShouldRetry(KeyedCacheSlot slot);
        void FetchAttempted(KeyedCacheSlot slot, bool success);
    }
}
