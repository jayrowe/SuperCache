using SuperCache.Policies;
using System;

namespace SuperCache.UnitTests
{
    public static class UnitTestTimeProvider
    {
        public static void Reset()
        {
            TimeProvider._staticTime = null;
        }

        public static void Advance(TimeSpan span)
        {
            TimeProvider._staticTime = TimeProvider._staticTime.GetValueOrDefault(DateTime.UtcNow) + span;
        }

        public static void Advance(double seconds)
        {
            Advance(TimeSpan.FromSeconds(seconds));
        }
    }
}
