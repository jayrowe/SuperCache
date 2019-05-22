namespace SuperCache.Policies
{
    internal class ShimCacheValuePolicy<TResult> : ICacheValuePolicy<TResult>
    {
        private readonly ICacheValuePolicy _policy;

        public ShimCacheValuePolicy(ICacheValuePolicy policy)
        {
            _policy = policy;
        }

        public TResult Retrieve(object value) => _policy.Retrieve<TResult>(value);

        public object Store(TResult value) => _policy.Store(value);
    }
}
