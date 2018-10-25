using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperCache.UnitTests;
using System;

namespace SuperCache.Policies.UnitTests
{
    [TestClass]
    public class DurationSinceFetchCacheExpirationPolicyTests
    {
        [TestCleanup]
        public void TearDown()
        {
            UnitTestTimeProvider.Reset();
        }

        [TestMethod]
        public void AllowExpiredResult_ReturnsTrueConstructorValue()
        {
            var policy = new DurationSinceFetchCacheExpirationPolicy(TimeSpan.FromSeconds(5.0), true);
            Assert.IsTrue(policy.AllowExpiredResult(null));
        }

        [TestMethod]
        public void AllowExpiredResult_ReturnsFalseConstructorValue()
        {
            var policy = new DurationSinceFetchCacheExpirationPolicy(TimeSpan.FromSeconds(5.0), false);
            Assert.IsFalse(policy.AllowExpiredResult(null));
        }

        [TestMethod]
        public void AllowExpiredResult_DefaultIsTrue()
        {
            var policy = new DurationSinceFetchCacheExpirationPolicy(TimeSpan.FromSeconds(5.0));
            Assert.IsTrue(policy.AllowExpiredResult(null));
        }

        [TestMethod]
        public void IsExpired_ExpirationTimeHasPassed_ReturnsTrue()
        {
            var policy = new DurationSinceFetchCacheExpirationPolicy(TimeSpan.FromSeconds(5.0));

            UnitTestTimeProvider.Advance(0.0);

            var slot = new KeyedCacheSlot();
            policy.Fetched(slot);

            UnitTestTimeProvider.Advance(5.0);

            Assert.IsTrue(policy.IsExpired(slot));
        }

        [TestMethod]
        public void IsExpired_ExpirationTimeHasNotPassed_ReturnsFalse()
        {
            var policy = new DurationSinceFetchCacheExpirationPolicy(TimeSpan.FromSeconds(5.0));

            UnitTestTimeProvider.Advance(0.0);

            var slot = new KeyedCacheSlot();
            policy.Fetched(slot);

            UnitTestTimeProvider.Advance(4.9);

            Assert.IsFalse(policy.IsExpired(slot));
        }

        [TestMethod]
        public void Expire_CausesIsExpiredToReturnTrue()
        {
            var policy = new DurationSinceFetchCacheExpirationPolicy(TimeSpan.FromSeconds(5.0));

            UnitTestTimeProvider.Advance(0.0);

            var slot = new KeyedCacheSlot();
            policy.Fetched(slot);
            Assert.IsFalse(policy.IsExpired(slot));

            policy.Expire(slot);
            Assert.IsTrue(policy.IsExpired(slot));
        }

        [TestMethod]
        public void ExpireAll_ItemInsertedBeforeExpireAllIsExpired()
        {
            var policy = new DurationSinceFetchCacheExpirationPolicy(TimeSpan.FromSeconds(5.0));

            UnitTestTimeProvider.Advance(0.0);

            var slot = new KeyedCacheSlot();
            policy.Fetched(slot);
            Assert.IsFalse(policy.IsExpired(slot));

            policy.ExpireAll();
            Assert.IsTrue(policy.IsExpired(slot));
        }

        [TestMethod]
        public void ExpireAll_ItemInsertedAfterExpireAllIsNotExpired()
        {
            var policy = new DurationSinceFetchCacheExpirationPolicy(TimeSpan.FromSeconds(5.0));

            UnitTestTimeProvider.Advance(0.0);

            var slot = new KeyedCacheSlot();
            policy.Fetched(slot);
            Assert.IsFalse(policy.IsExpired(slot));

            UnitTestTimeProvider.Advance(0.002);

            policy.ExpireAll();
            Assert.IsTrue(policy.IsExpired(slot));
        }
    }
}
