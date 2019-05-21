namespace SuperCache.Policies
{
    public class IdentityCacheValuePolicy : ICacheValuePolicy
    {
        public object Store<TResult>(TResult value) => value;
        public TResult Retrieve<TResult>(object value) => (TResult)value;
    }

    public class IdentityCacheValuePolicy<TResult> : ICacheValuePolicy<TResult>
    {
        public object Store(TResult value) => value;
        public TResult Retrieve(object value) => (TResult)value;
    }
}
