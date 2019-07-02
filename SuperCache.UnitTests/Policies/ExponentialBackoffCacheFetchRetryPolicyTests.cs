using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperCache.Policies;
using SuperCache.UnitTests;
using System;

namespace SuperCache.UnitTests.Policies
{
    [TestClass]
    public class ExponentialBackoffCacheFetchRetryPolicyTests
    {
        [TestCleanup]
        public void TearDown()
        {
            UnitTestTimeProvider.Reset();
        }

        [TestMethod]
        public void ctor_MaximumIsLessThanInitialInterval()
        {
            Assert.ThrowsException<ArgumentException>(() => new ExponentialBackoffCacheFetchRetryPolicy(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(4.9)));
        }

        [TestMethod]
        public void ctor_InitialIntervalIsLessThanZero()
        {
            Assert.ThrowsException<ArgumentException>(() => new ExponentialBackoffCacheFetchRetryPolicy(TimeSpan.FromSeconds(-0.1), TimeSpan.FromSeconds(4.9)));
        }

        [TestMethod]
        public void ShouldRetry_UsesExponentialSequence()
        {
            UnitTestTimeProvider.Advance(0);

            var policy = new ExponentialBackoffCacheFetchRetryPolicy(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(10.0));
            var slot = new KeyedCacheSlot();
            policy.FetchAttempted(slot, false);

            UnitTestTimeProvider.Advance(0.9);
            Assert.IsFalse(policy.ShouldRetry(slot));

            UnitTestTimeProvider.Advance(0.1);
            Assert.IsTrue(policy.ShouldRetry(slot));
            policy.FetchAttempted(slot, false);

            UnitTestTimeProvider.Advance(1.9);
            Assert.IsFalse(policy.ShouldRetry(slot));

            UnitTestTimeProvider.Advance(0.1);
            Assert.IsTrue(policy.ShouldRetry(slot));
            policy.FetchAttempted(slot, false);

            UnitTestTimeProvider.Advance(3.9);
            Assert.IsFalse(policy.ShouldRetry(slot));

            UnitTestTimeProvider.Advance(0.1);
            Assert.IsTrue(policy.ShouldRetry(slot));
            policy.FetchAttempted(slot, false);

            UnitTestTimeProvider.Advance(7.9);
            Assert.IsFalse(policy.ShouldRetry(slot));

            UnitTestTimeProvider.Advance(0.1);
            Assert.IsTrue(policy.ShouldRetry(slot));
        }

        [TestMethod]
        public void ShouldRetry_HitsMaximumBackoff()
        {
            UnitTestTimeProvider.Advance(0);

            var policy = new ExponentialBackoffCacheFetchRetryPolicy(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(5.0));
            var slot = new KeyedCacheSlot();
            policy.FetchAttempted(slot, false);

            UnitTestTimeProvider.Advance(0.9);
            Assert.IsFalse(policy.ShouldRetry(slot));

            UnitTestTimeProvider.Advance(0.1);
            Assert.IsTrue(policy.ShouldRetry(slot));
            policy.FetchAttempted(slot, false);

            UnitTestTimeProvider.Advance(1.9);
            Assert.IsFalse(policy.ShouldRetry(slot));

            UnitTestTimeProvider.Advance(0.1);
            Assert.IsTrue(policy.ShouldRetry(slot));
            policy.FetchAttempted(slot, false);

            UnitTestTimeProvider.Advance(3.9);
            Assert.IsFalse(policy.ShouldRetry(slot));

            UnitTestTimeProvider.Advance(0.1);
            Assert.IsTrue(policy.ShouldRetry(slot));
            policy.FetchAttempted(slot, false);

            UnitTestTimeProvider.Advance(4.9);
            Assert.IsFalse(policy.ShouldRetry(slot));

            UnitTestTimeProvider.Advance(0.1);
            Assert.IsTrue(policy.ShouldRetry(slot));
        }
    }
}
