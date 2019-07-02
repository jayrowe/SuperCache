using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperCache.Policies;
using SuperCache.UnitTests;
using System;

namespace SuperCache.UnitTests.Policies
{
    [TestClass]
    public class ConstantIntervalCacheFetchRetryPolicyTests
    {
        [TestCleanup]
        public void TearDown()
        {
            UnitTestTimeProvider.Reset();
        }

        [TestMethod]
        public void ctor_IntervalIsNegative()
        {
            Assert.ThrowsException<ArgumentException>(() => new ConstantIntervalCacheFetchRetryPolicy(TimeSpan.FromSeconds(-1.0)));
        }

        [TestMethod]
        public void ShouldRetry_UnderConstantInterval()
        {
            var policy = new ConstantIntervalCacheFetchRetryPolicy(TimeSpan.FromSeconds(5.0));

            UnitTestTimeProvider.Advance(0.0);

            var slot = new KeyedCacheSlot();
            slot.LastFetchAttempt = TimeProvider.UtcNow;

            UnitTestTimeProvider.Advance(4.9);
            Assert.IsFalse(policy.ShouldRetry(slot));
        }

        [TestMethod]
        public void ShouldRetry_PastConstantInterval()
        {
            var policy = new ConstantIntervalCacheFetchRetryPolicy(TimeSpan.FromSeconds(5.0));

            UnitTestTimeProvider.Advance(0.0);

            var slot = new KeyedCacheSlot();
            slot.LastFetchAttempt = TimeProvider.UtcNow;

            UnitTestTimeProvider.Advance(5.0);
            Assert.IsTrue(policy.ShouldRetry(slot));
        }
    }
}
