using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SuperCache.Policies.UnitTests
{
    [TestClass]
    public class AlwaysFetchCacheFetchRetryPolicyTests
    {
        [TestMethod]
        public void ShouldRetry_ReturnsTrueAlways()
        {
            Assert.IsTrue(new AlwaysCacheFetchRetryPolicy().ShouldRetry(null));
        }

        [TestMethod]
        public void FetchAttempted_NoOp()
        {
            new AlwaysCacheFetchRetryPolicy().FetchAttempted(null, false);
        }
    }
}
