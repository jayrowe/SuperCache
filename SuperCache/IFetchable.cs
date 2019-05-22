using System.Threading.Tasks;

namespace SuperCache
{
    public interface IFetchable<TSource, TResult>
    {
        TResult Fetch(TSource source);
    }

    public interface IFetchableAsync<TSource, TResult>
    {
        Task<TResult> FetchAsync(TSource source);
    }
}
