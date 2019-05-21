namespace SuperCache.Policies
{
    public interface ICacheValuePolicy
    {
        object Store<TResult>(TResult value);
        TResult Retrieve<TResult>(object value);
    }

    public interface ICacheValuePolicy<TResult>
    {
        object Store(TResult value);
        TResult Retrieve(object value);
    }
}