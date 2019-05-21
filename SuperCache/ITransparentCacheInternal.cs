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


}
