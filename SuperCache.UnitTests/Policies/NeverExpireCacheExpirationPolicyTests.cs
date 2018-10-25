using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SuperCache.Policies.UnitTests
{
    [TestClass]
    public class NeverExpireCacheExpirationPolicyTests
    {
        [TestMethod]
        public void Fetched_NoOp()
        {
            new NeverExpireCacheExpirationPolicy().Fetched(null);
        }

        [TestMethod]
        public void Expire_NoOp()
        {
            new NeverExpireCacheExpirationPolicy().Expire(null);
        }

        [TestMethod]
        public void ExpireAll_NoOp()
        {
            new NeverExpireCacheExpirationPolicy().ExpireAll();
        }

        [TestMethod]
        public void IsExpired_ReturnsFalseAlways()
        {
            Assert.IsFalse(new NeverExpireCacheExpirationPolicy().IsExpired(null));
        }

        [TestMethod]
        public void AllowExpiredResult_ReturnsTrueAlways()
        {
            Assert.IsTrue(new NeverExpireCacheExpirationPolicy().AllowExpiredResult(null));
        }
    }
}
