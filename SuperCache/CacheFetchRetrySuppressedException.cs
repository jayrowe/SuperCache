using System;
using System.Runtime.Serialization;

namespace SuperCache
{
    /// <summary>
    /// Exception thrown when a previous attempt to fetch failed and the fetch retry policy has blocked an
    /// additional attempt to fetch the value.
    /// </summary>
    [Serializable]
    public class CacheFetchRetrySuppressedException : Exception
    {
        public CacheFetchRetrySuppressedException(Exception ex)
            : base ("A previous cache fetch attempt failed, and the fetch retry policy disallowed retry", ex)
        {
        }

        protected CacheFetchRetrySuppressedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
