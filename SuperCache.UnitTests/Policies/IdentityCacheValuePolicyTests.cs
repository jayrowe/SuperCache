using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperCache.Policies;

namespace SuperCache.UnitTests.Policies
{
    [TestClass]
    public class IdentityCacheValuePolicyTests
    {
        [TestMethod]
        public void Store_ReturnsOriginalObject()
        {
            var expected = new object();

            var policy = new IdentityCacheValuePolicy();
            Assert.AreSame(expected, policy.Store(expected));
        }

        [TestMethod]
        public void Return_CastsObject()
        {
            var expected = new TestClass();

            var policy = new IdentityCacheValuePolicy();
            Assert.AreSame(expected, policy.Retrieve<TestClass>(expected));
        }

        [TestMethod]
        public void Store_Generic_ReturnsOriginalObject()
        {
            var expected = new TestClass();

            var policy = new IdentityCacheValuePolicy<TestClass>();
            Assert.AreSame(expected, policy.Store(expected));
        }

        [TestMethod]
        public void Return_Generic_CastsObject()
        {
            var expected = new TestClass();

            var policy = new IdentityCacheValuePolicy<TestClass>();
            Assert.AreSame(expected, policy.Retrieve(expected));
        }

        private class TestClass
        {
            public object Value { get; set; }
        }
    }
}
