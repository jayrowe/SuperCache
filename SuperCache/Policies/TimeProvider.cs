using System;

namespace SuperCache.Policies
{
    public static class TimeProvider
    {
        internal static DateTime? _staticTime;

        public static DateTime UtcNow
        {
            get
            {
                return _staticTime.GetValueOrDefault(DateTime.UtcNow);
            }
        }
    }
}
