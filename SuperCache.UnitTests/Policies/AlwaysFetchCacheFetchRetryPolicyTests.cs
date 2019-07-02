using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperCache.Policies;

namespace SuperCache.UnitTests.Policies
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
