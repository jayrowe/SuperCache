using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language.Flow;
using SuperCache.Policies;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SuperCache.UnitTests
{
    [TestClass]
    public class TransparentCacheTests
    {
        #region Caching/proxying
        [TestMethod]
        public void VoidReturnType_Uncached()
        {
            var mock = new Mock<IReturnsVoid>();
            mock.Setup(i => i.ReturnsVoid("a")).Verifiable();
            mock.Setup(i => i.ReturnsVoid("a")).Verifiable();

            var cache = new TransparentCache<IReturnsVoid>(mock.Object);

            cache.Cached.ReturnsVoid("a");
            cache.Cached.ReturnsVoid("a");

            mock.VerifyAll();
        }

        [TestMethod]
        public void HasSingleParameter_Uncached()
        {
            var result0 = new object();
            var result1 = new object();

            var mock = new Mock<IHasSingleParameter>();
            mock.Setup(i => i.HasSingleParameter(10)).Returns(result0).Verifiable();
            mock.Setup(i => i.HasSingleParameter(11)).Returns(result1).Verifiable();

            var cache = new TransparentCache<IHasSingleParameter>(mock.Object);

            Assert.AreSame(result0, cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1, cache.Cached.HasSingleParameter(11));

            mock.VerifyAll();
        }

        [TestMethod]
        public void HasSingleParameter_Cached()
        {
            object[] result0 = { new object(), new object(), new object() };
            object[] result1 = { new object(), new object(), new object() };

            var mock = new Mock<IHasSingleParameter>();
            mock.Setup(i => i.HasSingleParameter(10)).ReturnsSequence(result0).Verifiable();
            mock.Setup(i => i.HasSingleParameter(11)).ReturnsSequence(result1).Verifiable();

            var cache = new TransparentCache<IHasSingleParameter>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[0], cache.Cached.HasSingleParameter(11));
            Assert.AreSame(result0[0], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[0], cache.Cached.HasSingleParameter(11));

            mock.VerifyAll();
        }

        [TestMethod]
        public void HasMultipleParameters_Uncached()
        {
            var result0 = new object();
            var result1 = new object();

            var mock = new Mock<IHasMultipleParameters>();
            mock.Setup(i => i.HasMultipleParameters(10, "hi")).Returns(result0).Verifiable();
            mock.Setup(i => i.HasMultipleParameters(11, "hi")).Returns(result1).Verifiable();

            var cache = new TransparentCache<IHasMultipleParameters>(mock.Object);

            Assert.AreSame(result0, cache.Cached.HasMultipleParameters(10, "hi"));
            Assert.AreSame(result1, cache.Cached.HasMultipleParameters(11, "hi"));

            mock.VerifyAll();
        }

        [TestMethod]
        public void HasMultipleParameters_Cached()
        {
            object[] result0 = { new object(), new object(), new object() };
            object[] result1 = { new object(), new object(), new object() };

            var mock = new Mock<IHasMultipleParameters>();
            mock.Setup(i => i.HasMultipleParameters(10, "hi")).ReturnsSequence(result0).Verifiable();
            mock.Setup(i => i.HasMultipleParameters(11, "hi")).ReturnsSequence(result1).Verifiable();

            var cache = new TransparentCache<IHasMultipleParameters>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.HasMultipleParameters(10, "hi"));
            Assert.AreSame(result1[0], cache.Cached.HasMultipleParameters(11, "hi"));
            Assert.AreSame(result0[0], cache.Cached.HasMultipleParameters(10, "hi"));
            Assert.AreSame(result1[0], cache.Cached.HasMultipleParameters(11, "hi"));

            mock.VerifyAll();
        }

        [TestMethod]
        public void Overloaded_Cached()
        {
            object[] result0 = { new object(), new object(), new object() };
            object[] result1 = { new object(), new object(), new object() };

            var mock = new Mock<IOverloaded>();
            mock.Setup(i => i.Overloaded("first")).ReturnsSequence(result0).Verifiable();
            mock.Setup(i => i.Overloaded("first", "second")).ReturnsSequence(result1).Verifiable();

            var cache = new TransparentCache<IOverloaded>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.Overloaded("first"));
            Assert.AreSame(result1[0], cache.Cached.Overloaded("first", "second"));
            Assert.AreSame(result0[0], cache.Cached.Overloaded("first"));
            Assert.AreSame(result1[0], cache.Cached.Overloaded("first", "second"));

            mock.VerifyAll();
        }

        [TestMethod]
        public void HasRefParameter_Uncached()
        {
            object[] results = { new object(), new object() };

            var value = "a";

            var mock = new Mock<IHasRefParameter>();
            mock.Setup(i => i.HasRefParameter(ref value)).ReturnsSequence(results).Verifiable();

            var cache = new TransparentCache<IHasRefParameter>(mock.Object);

            Assert.AreSame(results[0], cache.Cached.HasRefParameter(ref value));
            Assert.AreSame(results[1], cache.Cached.HasRefParameter(ref value));

            mock.VerifyAll();
        }

        [TestMethod]
        public void HasOutParameter_Uncached()
        {
            object[] results = { new object(), new object() };

            var value = "a";

            var mock = new Mock<IHasOutParameter>();
            mock.Setup(i => i.HasOutParameter(out value)).ReturnsSequence(results).Verifiable();

            var cache = new TransparentCache<IHasOutParameter>(mock.Object);

            Assert.AreSame(results[0], cache.Cached.HasOutParameter(out value));
            Assert.AreSame(results[1], cache.Cached.HasOutParameter(out value));

            mock.VerifyAll();
        }
        #endregion Caching/proxying

        #region Purge
        [TestMethod]
        public void Purge_ReturnsVoid()
        {
            var mock = new Mock<IReturnsVoid>();
            var cache = new TransparentCache<IReturnsVoid>(mock.Object);

            cache.Purge.ReturnsVoid("a");
        }

        [TestMethod]
        public void Purge_HasRefParameter_ReferenceType()
        {
            var mock = new Mock<IHasRefParameter>();
            var cache = new TransparentCache<IHasRefParameter>(mock.Object);

            string value = "a";

            cache.Purge.HasRefParameter(ref value);
        }

        [TestMethod]
        public void Purge_HasRefParameter_ValueType()
        {
            var mock = new Mock<IHasRefParameter>();
            var cache = new TransparentCache<IHasRefParameter>(mock.Object);

            int value = 0;

            cache.Purge.HasRefParameter(ref value);
        }

        [TestMethod]
        public void Purge_HasOutParameter_ReferenceType()
        {
            var mock = new Mock<IHasOutParameter>();
            var cache = new TransparentCache<IHasOutParameter>(mock.Object);

            var value = "a";

            cache.Purge.HasOutParameter(out value);
        }

        [TestMethod]
        public void Purge_HasOutParameter_ValueType()
        {
            var mock = new Mock<IHasOutParameter>();
            var cache = new TransparentCache<IHasOutParameter>(mock.Object);

            int value = 0;

            cache.Purge.HasOutParameter(out value);
        }

        [TestMethod]
        public void Purge_HasSingleParameter()
        {
            object[] result0 = { new object(), new object() };
            object[] result1 = { new object(), new object() };

            var mock = new Mock<IHasSingleParameter>();
            mock.Setup(i => i.HasSingleParameter(10)).ReturnsSequence(result0).Verifiable();
            mock.Setup(i => i.HasSingleParameter(11)).ReturnsSequence(result1).Verifiable();

            var cache = new TransparentCache<IHasSingleParameter>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[0], cache.Cached.HasSingleParameter(11));
            cache.Purge.HasSingleParameter(11);
            Assert.AreSame(result0[0], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[1], cache.Cached.HasSingleParameter(11));

            mock.VerifyAll();
        }

        [TestMethod]
        public void Purge_HasMultipleParameters()
        {
            object[] result0 = { new object(), new object() };
            object[] result1 = { new object(), new object() };

            var mock = new Mock<IHasMultipleParameters>();
            mock.Setup(i => i.HasMultipleParameters(10, "10")).ReturnsSequence(result0).Verifiable();
            mock.Setup(i => i.HasMultipleParameters(10, "11")).ReturnsSequence(result1).Verifiable();

            var cache = new TransparentCache<IHasMultipleParameters>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.HasMultipleParameters(10, "10"));
            Assert.AreSame(result1[0], cache.Cached.HasMultipleParameters(10, "11"));
            cache.Purge.HasMultipleParameters(10, "11");
            Assert.AreSame(result0[0], cache.Cached.HasMultipleParameters(10, "10"));
            Assert.AreSame(result1[1], cache.Cached.HasMultipleParameters(10, "11"));

            mock.VerifyAll();
        }

        [TestMethod]
        public void Purge_Overloaded()
        {
            object[] result0 = { new object(), new object() };
            object[] result1 = { new object(), new object() };

            var mock = new Mock<IOverloaded>();
            mock.Setup(i => i.Overloaded("first")).ReturnsSequence(result0).Verifiable();
            mock.Setup(i => i.Overloaded("first", "second")).ReturnsSequence(result1).Verifiable();

            var cache = new TransparentCache<IOverloaded>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.Overloaded("first"));
            Assert.AreSame(result1[0], cache.Cached.Overloaded("first", "second"));
            cache.Purge.Overloaded("first", "second");
            Assert.AreSame(result0[0], cache.Cached.Overloaded("first"));
            Assert.AreSame(result1[1], cache.Cached.Overloaded("first", "second"));

            mock.VerifyAll();
        }
        #endregion Purge

        #region PurgeAll (All caches)
        [TestMethod]
        public void PurgeAll_AllCaches()
        {
            object[] result0 = { new object(), new object() };
            object[] result1 = { new object(), new object() };

            var mock = new Mock<IOverloaded>();
            mock.Setup(s => s.Overloaded("first")).ReturnsSequence(result0);
            mock.Setup(s => s.Overloaded("first", "second")).ReturnsSequence(result1);

            var cache = new TransparentCache<IOverloaded>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.Overloaded("first"));
            Assert.AreSame(result1[0], cache.Cached.Overloaded("first", "second"));

            cache.PurgeAll();

            Assert.AreSame(result0[1], cache.Cached.Overloaded("first"));
            Assert.AreSame(result1[1], cache.Cached.Overloaded("first", "second"));

            mock.Verify(s => s.Overloaded("first"), Times.Exactly(2));
            mock.Verify(s => s.Overloaded("first", "second"), Times.Exactly(2));
        }
        #endregion

        #region PurgeAll (Single signature)
        [TestMethod]
        public void PurgeAll_HasRefParameter_ReferenceType()
        {
            var mock = new Mock<IHasRefParameter>();
            var cache = new TransparentCache<IHasRefParameter>(mock.Object);

            string value = "a";

            cache.PurgeAll(i => i.HasRefParameter(ref value));
        }

        [TestMethod]
        public void PurgeAll_HasRefParameter_ValueType()
        {
            var mock = new Mock<IHasRefParameter>();
            var cache = new TransparentCache<IHasRefParameter>(mock.Object);

            int value = 0;

            cache.PurgeAll(i => i.HasRefParameter(ref value));
        }

        [TestMethod]
        public void PurgeAll_HasRefParameter_ValueTypeReturn()
        {
            var mock = new Mock<IHasOutParameter>();
            var cache = new TransparentCache<IHasOutParameter>(mock.Object);

            int value = 0;

            cache.PurgeAll(i => i.HasOutParameterValueTypeReturn(out value));
        }

        [TestMethod]
        public void PurgeAll_HasOutParameter_ReferenceType()
        {
            var mock = new Mock<IHasOutParameter>();
            var cache = new TransparentCache<IHasOutParameter>(mock.Object);

            var value = "a";

            cache.PurgeAll(i => i.HasOutParameter(out value));
        }

        [TestMethod]
        public void PurgeAll_HasOutParameter_ValueType()
        {
            var mock = new Mock<IHasOutParameter>();
            var cache = new TransparentCache<IHasOutParameter>(mock.Object);

            int value = 0;

            cache.PurgeAll(i => i.HasOutParameter(out value));
        }

        [TestMethod]
        public void PurgeAll_HasOutParameter_ValueTypeReturn()
        {
            var mock = new Mock<IHasOutParameter>();
            var cache = new TransparentCache<IHasOutParameter>(mock.Object);

            int value = 0;

            cache.PurgeAll(i => i.HasOutParameterValueTypeReturn(out value));
        }

        [TestMethod]
        public void PurgeAll_HasSingleParameter()
        {
            object[] result0 = { new object(), new object() };
            object[] result1 = { new object(), new object() };

            var mock = new Mock<IHasSingleParameter>();
            mock.Setup(i => i.HasSingleParameter(10)).ReturnsSequence(result0).Verifiable();
            mock.Setup(i => i.HasSingleParameter(11)).ReturnsSequence(result1).Verifiable();

            var cache = new TransparentCache<IHasSingleParameter>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[0], cache.Cached.HasSingleParameter(11));
            cache.PurgeAll(i => i.HasSingleParameter(11));
            Assert.AreSame(result0[1], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[1], cache.Cached.HasSingleParameter(11));

            mock.VerifyAll();
        }

        [TestMethod]
        public void PurgeAll_HasMultipleParameters()
        {
            object[] result0 = { new object(), new object() };
            object[] result1 = { new object(), new object() };

            var mock = new Mock<IHasMultipleParameters>();
            mock.Setup(i => i.HasMultipleParameters(10, "10")).ReturnsSequence(result0).Verifiable();
            mock.Setup(i => i.HasMultipleParameters(10, "11")).ReturnsSequence(result1).Verifiable();

            var cache = new TransparentCache<IHasMultipleParameters>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.HasMultipleParameters(10, "10"));
            Assert.AreSame(result1[0], cache.Cached.HasMultipleParameters(10, "11"));
            cache.PurgeAll(i => i.HasMultipleParameters(10, "11"));
            Assert.AreSame(result0[1], cache.Cached.HasMultipleParameters(10, "10"));
            Assert.AreSame(result1[1], cache.Cached.HasMultipleParameters(10, "11"));

            mock.VerifyAll();
        }

        [TestMethod]
        public void PurgeAll_Overloaded()
        {
            object[] result0 = { new object(), new object() };
            object[] result1 = { new object(), new object() };

            var mock = new Mock<IOverloaded>();
            mock.Setup(i => i.Overloaded("first")).ReturnsSequence(result0).Verifiable();
            mock.Setup(i => i.Overloaded("first", "second")).ReturnsSequence(result1).Verifiable();

            var cache = new TransparentCache<IOverloaded>(mock.Object);

            Assert.AreSame(result0[0], cache.Cached.Overloaded("first"));
            Assert.AreSame(result1[0], cache.Cached.Overloaded("first", "second"));
            cache.PurgeAll(i => i.Overloaded("first", "second"));
            Assert.AreSame(result0[0], cache.Cached.Overloaded("first"));
            Assert.AreSame(result1[1], cache.Cached.Overloaded("first", "second"));

            mock.VerifyAll();
        }
        #endregion PurgeAll

        #region ICacheFetchRetryPolicy integration
        [TestMethod]
        public void FetchRetry_Allowed()
        {
            var result = new object();

            var sourceMock = new Mock<IHasSingleParameter>();
            sourceMock.Setup(i => i.HasSingleParameter(0)).ReturnsSequence(
                () => { throw new MissingMethodException("failed to load"); },
                () => result);

            var retryPolicyMock = new Mock<ICacheFetchRetryPolicy>();
            retryPolicyMock.Setup(p => p.ShouldRetry(It.IsAny<KeyedCacheSlot>())).Returns(true);
            retryPolicyMock.Setup(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), It.IsAny<bool>()));

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(retryPolicyMock.Object, i => i.HasSingleParameter(0));

            var cache = builder.Create();

            Assert.ThrowsException<MissingMethodException>(() => cache.Cached.HasSingleParameter(0));
            Assert.AreSame(result, cache.Cached.HasSingleParameter(0));
        }

        [TestMethod]
        public void FetchRetry_Disallowed()
        {
            var result = new object();

            var sourceMock = new Mock<IHasSingleParameter>();
            sourceMock.Setup(i => i.HasSingleParameter(0)).ReturnsSequence(
                () => { throw new MissingMethodException("failed to load"); },
                () => result);

            var retryPolicyMock = new Mock<ICacheFetchRetryPolicy>();
            retryPolicyMock.Setup(p => p.ShouldRetry(It.IsAny<KeyedCacheSlot>())).Returns(false);
            retryPolicyMock.Setup(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), It.IsAny<bool>()));

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(retryPolicyMock.Object, i => i.HasSingleParameter(0));

            var cache = builder.Create();

            Assert.ThrowsException<MissingMethodException>(() => cache.Cached.HasSingleParameter(0));
            Assert.ThrowsException<CacheFetchRetrySuppressedException>(() => cache.Cached.HasSingleParameter(0));
        }

        [TestMethod]
        public void FetchAttempted_Failed()
        {
            var result = new object();

            var sourceMock = new Mock<IHasSingleParameter>();
            sourceMock.Setup(i => i.HasSingleParameter(0)).ReturnsSequence(
                () => { throw new MissingMethodException("failed to load"); });

            var retryPolicyMock = new Mock<ICacheFetchRetryPolicy>();
            retryPolicyMock.Setup(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), false)).Verifiable();

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(retryPolicyMock.Object, i => i.HasSingleParameter(0));

            var cache = builder.Create();

            Assert.ThrowsException<MissingMethodException>(() => cache.Cached.HasSingleParameter(0));
            retryPolicyMock.Verify(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), false), Times.Once);
        }

        [TestMethod]
        public void FetchAttempted_Success()
        {
            var result = new object();

            var sourceMock = new Mock<IHasSingleParameter>();
            sourceMock.Setup(i => i.HasSingleParameter(0)).Returns(result);

            var retryPolicyMock = new Mock<ICacheFetchRetryPolicy>();
            retryPolicyMock.Setup(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), true)).Verifiable();

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(retryPolicyMock.Object, i => i.HasSingleParameter(0));

            var cache = builder.Create();

            Assert.AreSame(result, cache.Cached.HasSingleParameter(0));
            retryPolicyMock.Verify(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), true), Times.Once);
        }

        [TestMethod]
        public void SetDefaultFetchRetryPolicy_UsedByAllCachedMethods()
        {
            var result = new object();

            var sourceMock = new Mock<IOverloaded>();
            sourceMock.Setup(i => i.Overloaded("first")).Returns(result);
            sourceMock.Setup(i => i.Overloaded("first", "second")).Returns(result);

            var retryPolicyMock = new Mock<ICacheFetchRetryPolicy>();
            retryPolicyMock.Setup(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), true)).Verifiable();

            var builder = new TransparentCacheBuilder<IOverloaded>(sourceMock.Object);
            builder.SetDefaultPolicy(retryPolicyMock.Object);

            var cache = builder.Create();

            Assert.AreSame(result, cache.Cached.Overloaded("first"));
            Assert.AreSame(result, cache.Cached.Overloaded("first", "second"));
            retryPolicyMock.Verify(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), true), Times.Exactly(2));

        }

        [TestMethod]
        public void SetDefaultFetchRetryPolicy_Overridden()
        {
            var result = new object();

            var sourceMock = new Mock<IOverloaded>();
            sourceMock.Setup(i => i.Overloaded("first")).Returns(result);
            sourceMock.Setup(i => i.Overloaded("first", "second")).Returns(result);

            var defaultRetryPolicyMock = new Mock<ICacheFetchRetryPolicy>();
            defaultRetryPolicyMock.Setup(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), true)).Verifiable();

            var retryPolicyMock = new Mock<ICacheFetchRetryPolicy>();
            retryPolicyMock.Setup(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), true)).Verifiable();

            var builder = new TransparentCacheBuilder<IOverloaded>(sourceMock.Object);
            builder.SetPolicy(retryPolicyMock.Object, s => s.Overloaded(String.Empty, String.Empty));
            builder.SetDefaultPolicy(defaultRetryPolicyMock.Object);

            var cache = builder.Create();

            Assert.AreSame(result, cache.Cached.Overloaded("first"));
            Assert.AreSame(result, cache.Cached.Overloaded("first", "second"));
            defaultRetryPolicyMock.Verify(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), true), Times.Once);
            retryPolicyMock.Verify(p => p.FetchAttempted(It.IsAny<KeyedCacheSlot>(), true), Times.Once);
        }

        [TestMethod]
        public async Task Async_RetriesOnException()
        {
            var result = new object();

            var sourceMock = new Mock<IAsync>();
            sourceMock.Setup(i => i.WithReturnValueAsync("param")).ReturnsSequence(
                () => Task.FromException<object>(new MissingMethodException("failed to load")),
                () => Task.FromResult(result));

            var builder = new TransparentCacheBuilder<IAsync>(sourceMock.Object);
            builder.SetDefaultPolicy(new AlwaysCacheFetchRetryPolicy());

            var cache = builder.Create();

            await Assert.ThrowsExceptionAsync<MissingMethodException>(() => cache.Cached.WithReturnValueAsync("param"));
            Assert.AreSame(result, await cache.Cached.WithReturnValueAsync("param"));
        }
        #endregion ICacheFetchRetryPolicy integration

        #region ICacheExpirationPolicy integration
        [TestMethod]
        public void IsExpired_False()
        {
            object[] result0 = { new object(), new object(), new object() };
            object[] result1 = { new object(), new object(), new object() };

            var sourceMock = new Mock<IHasSingleParameter>();
            sourceMock.Setup(i => i.HasSingleParameter(10)).ReturnsSequence(result0).Verifiable();
            sourceMock.Setup(i => i.HasSingleParameter(11)).ReturnsSequence(result1).Verifiable();

            var expirationPolicyMock = new Mock<ICacheExpirationPolicy>(MockBehavior.Strict);
            expirationPolicyMock.Setup(
                p => p.Fetched(
                    It.Is<KeyedCacheSlot>(s => object.ReferenceEquals(result0[0], s.CachedObject))
                    )).Verifiable();
            expirationPolicyMock.Setup(
                p => p.Fetched(
                    It.Is<KeyedCacheSlot>(s => object.ReferenceEquals(result1[0], s.CachedObject))
                    )).Verifiable();
            expirationPolicyMock.Setup(p => p.IsExpired(It.IsAny<KeyedCacheSlot>())).Returns(false);

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(expirationPolicyMock.Object, i => i.HasSingleParameter(0));
            var cache = builder.Create();

            Assert.AreSame(result0[0], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[0], cache.Cached.HasSingleParameter(11));
            Assert.AreSame(result0[0], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[0], cache.Cached.HasSingleParameter(11));
            Assert.AreSame(result0[0], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[0], cache.Cached.HasSingleParameter(11));

            sourceMock.VerifyAll();
        }

        [TestMethod]
        public void IsExpired_True()
        {
            object[] result0 = { new object(), new object(), new object() };
            object[] result1 = { new object(), new object(), new object() };

            var sourceMock = new Mock<IHasSingleParameter>();
            sourceMock.Setup(i => i.HasSingleParameter(10)).ReturnsSequence(result0).Verifiable();
            sourceMock.Setup(i => i.HasSingleParameter(11)).ReturnsSequence(result1).Verifiable();

            var expirationPolicyMock = new Mock<ICacheExpirationPolicy>(MockBehavior.Strict);
            for (int index = 0; index < result0.Length; index++)
            {
                int currentIndex = index;
                expirationPolicyMock.Setup(
                    p => p.Fetched(
                        It.Is<KeyedCacheSlot>(s => object.ReferenceEquals(result0[currentIndex], s.CachedObject))
                        )).Verifiable();
                expirationPolicyMock.Setup(
                    p => p.Fetched(
                        It.Is<KeyedCacheSlot>(s => object.ReferenceEquals(result1[currentIndex], s.CachedObject))
                        )).Verifiable();
            }
            expirationPolicyMock.Setup(p => p.IsExpired(It.IsAny<KeyedCacheSlot>())).Returns(true).Verifiable();

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(expirationPolicyMock.Object, i => i.HasSingleParameter(0));
            var cache = builder.Create();

            Assert.AreSame(result0[0], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[0], cache.Cached.HasSingleParameter(11));
            Assert.AreSame(result0[1], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[1], cache.Cached.HasSingleParameter(11));
            Assert.AreSame(result0[2], cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1[2], cache.Cached.HasSingleParameter(11));

            sourceMock.VerifyAll();
        }

        [TestMethod]
        public void AllowExpiredResult_False()
        {
            object result = new object();

            var sourceMock = new Mock<IHasSingleParameter>();
            sourceMock.Setup(i => i.HasSingleParameter(10)).ReturnsSequence(
                () => result,
                () => { throw new InvalidOperationException("Failed to load"); }).Verifiable();

            var expirationPolicyMock = new Mock<ICacheExpirationPolicy>(MockBehavior.Strict);
            expirationPolicyMock.Setup(p => p.Fetched(It.IsAny<KeyedCacheSlot>()));
            expirationPolicyMock.Setup(p => p.IsExpired(It.IsAny<KeyedCacheSlot>())).Returns(true);
            expirationPolicyMock.Setup(p => p.AllowExpiredResult(It.IsAny<KeyedCacheSlot>())).Returns(false);

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(expirationPolicyMock.Object, i => i.HasSingleParameter(0));
            var cache = builder.Create();

            Assert.AreSame(result, cache.Cached.HasSingleParameter(10));
            Assert.ThrowsException<InvalidOperationException>(() => cache.Cached.HasSingleParameter(10));
        }

        [TestMethod]
        public void AllowExpiredResult_True()
        {
            object result = new object();

            var sourceMock = new Mock<IHasSingleParameter>();
            sourceMock.Setup(i => i.HasSingleParameter(10)).ReturnsSequence(
                () => result,
                () => { throw new InvalidOperationException("Failed to load"); }).Verifiable();

            var expirationPolicyMock = new Mock<ICacheExpirationPolicy>(MockBehavior.Strict);
            expirationPolicyMock.Setup(p => p.Fetched(It.IsAny<KeyedCacheSlot>()));
            expirationPolicyMock.Setup(p => p.IsExpired(It.IsAny<KeyedCacheSlot>())).Returns(true);
            expirationPolicyMock.Setup(p => p.AllowExpiredResult(It.IsAny<KeyedCacheSlot>())).Returns(true);

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(expirationPolicyMock.Object, i => i.HasSingleParameter(0));
            var cache = builder.Create();

            Assert.AreSame(result, cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result, cache.Cached.HasSingleParameter(10));
        }

        [TestMethod]
        public void Expire_InvokesPolicy()
        {
            object result = new object();

            var sourceMock = new Mock<IHasSingleParameter>();
            sourceMock.Setup(i => i.HasSingleParameter(10)).ReturnsSequence(() => result).Verifiable();

            var expirationPolicyMock = new Mock<ICacheExpirationPolicy>(MockBehavior.Strict);
            expirationPolicyMock.Setup(p => p.Fetched(It.IsAny<KeyedCacheSlot>()));
            expirationPolicyMock.Setup(p => p.Expire(It.Is<KeyedCacheSlot>(s => object.ReferenceEquals(s.CachedObject, result))));

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(expirationPolicyMock.Object, i => i.HasSingleParameter(0));
            var cache = builder.Create();

            Assert.AreSame(result, cache.Cached.HasSingleParameter(10));
            cache.Expire.HasSingleParameter(10);

            expirationPolicyMock.Verify(p => p.Fetched(It.IsAny<KeyedCacheSlot>()), Times.Once);
            expirationPolicyMock.Verify(p => p.Expire(It.IsAny<KeyedCacheSlot>()), Times.Once);
        }

        [TestMethod]
        public void ExpireAll_InvokesPolicy()
        {
            var sourceMock = new Mock<IHasSingleParameter>();

            var expirationPolicyMock = new Mock<ICacheExpirationPolicy>(MockBehavior.Strict);
            expirationPolicyMock.Setup(p => p.ExpireAll());

            var builder = new TransparentCacheBuilder<IHasSingleParameter>(sourceMock.Object);
            builder.SetPolicy(expirationPolicyMock.Object, i => i.HasSingleParameter(0));
            var cache = builder.Create();

            cache.ExpireAll(c => c.HasSingleParameter(0));

            expirationPolicyMock.Verify(p => p.ExpireAll(), Times.Once);
        }

        [TestMethod]
        public void SetDefaultExpirationPolicy_UsedByAllCachedMethods()
        {
            var result = new object();

            var sourceMock = new Mock<IOverloaded>();
            sourceMock.Setup(i => i.Overloaded("first")).Returns(result);
            sourceMock.Setup(i => i.Overloaded("first", "second")).Returns(result);

            var expirationPolicyMock = new Mock<ICacheExpirationPolicy>();
            expirationPolicyMock.Setup(p => p.Fetched(It.IsAny<KeyedCacheSlot>())).Verifiable();

            var builder = new TransparentCacheBuilder<IOverloaded>(sourceMock.Object);
            builder.SetDefaultPolicy(expirationPolicyMock.Object);

            var cache = builder.Create();

            Assert.AreSame(result, cache.Cached.Overloaded("first"));
            Assert.AreSame(result, cache.Cached.Overloaded("first", "second"));
            expirationPolicyMock.Verify(p => p.Fetched(It.IsAny<KeyedCacheSlot>()), Times.Exactly(2));

        }

        [TestMethod]
        public void SetDefaultExpirationPolicy_Overridden()
        {
            var result = new object();

            var sourceMock = new Mock<IOverloaded>();
            sourceMock.Setup(i => i.Overloaded("first")).Returns(result);
            sourceMock.Setup(i => i.Overloaded("first", "second")).Returns(result);

            var defaultExpirationPolicyMock = new Mock<ICacheExpirationPolicy>();
            defaultExpirationPolicyMock.Setup(p => p.Fetched(It.IsAny<KeyedCacheSlot>())).Verifiable();

            var expirationPolicyMock = new Mock<ICacheExpirationPolicy>();
            expirationPolicyMock.Setup(p => p.Fetched(It.IsAny<KeyedCacheSlot>())).Verifiable();

            var builder = new TransparentCacheBuilder<IOverloaded>(sourceMock.Object);
            builder.SetPolicy(expirationPolicyMock.Object, s => s.Overloaded(String.Empty, String.Empty));
            builder.SetDefaultPolicy(defaultExpirationPolicyMock.Object);

            var cache = builder.Create();

            Assert.AreSame(result, cache.Cached.Overloaded("first"));
            Assert.AreSame(result, cache.Cached.Overloaded("first", "second"));
            defaultExpirationPolicyMock.Verify(p => p.Fetched(It.IsAny<KeyedCacheSlot>()), Times.Once);
            expirationPolicyMock.Verify(p => p.Fetched(It.IsAny<KeyedCacheSlot>()), Times.Once);
        }
        #endregion

        #region ICacheValuePolicy integration
        [TestMethod]
        public void SetSpecificPolicy()
        {
            // we return these from the source
            var source = new object();

            // we store these in the cache
            var cached = new object();

            // we translate to these on retrieval
            var retrieved = new object();

            var sourceMock = new Mock<IOverloaded>();
            sourceMock.Setup(i => i.Overloaded("first")).Returns(source);

            var valuePolicyMock = new Mock<ICacheValuePolicy<object>>();
            valuePolicyMock.Setup(p => p.Store(source)).Returns(cached).Verifiable();
            valuePolicyMock.Setup(p => p.Retrieve(cached)).Returns(retrieved).Verifiable();

            var builder = new TransparentCacheBuilder<IOverloaded>(sourceMock.Object);
            builder.SetPolicy(valuePolicyMock.Object, i => i.Overloaded("first"));

            var cache = builder.Create();

            Assert.AreSame(retrieved, cache.Cached.Overloaded("first"));
            Assert.AreSame(retrieved, cache.Cached.Overloaded("first"));

            valuePolicyMock.Verify(p => p.Store(source), Times.Once);
            valuePolicyMock.Verify(p => p.Retrieve(cached), Times.Exactly(2));
        }

        [TestMethod]
        public void SetDefaultValuePolicy()
        {
            // we return these from the source
            var first = new object();
            var second = new object();

            // we store these in the cache
            var firstCached = new object();
            var secondCached = new object();

            // we translate to these on retrieval
            var firstRetrieved = new object();
            var secondRetrieved = new object();

            var sourceMock = new Mock<IOverloaded>();
            sourceMock.Setup(i => i.Overloaded("first")).Returns(first);
            sourceMock.Setup(i => i.Overloaded("first", "second")).Returns(second);

            var valuePolicyMock = new Mock<ICacheValuePolicy>();
            valuePolicyMock.Setup(p => p.Store(first)).Returns(firstCached).Verifiable();
            valuePolicyMock.Setup(p => p.Store(second)).Returns(secondCached).Verifiable();
            valuePolicyMock.Setup(p => p.Retrieve<object>(firstCached)).Returns(firstRetrieved).Verifiable();
            valuePolicyMock.Setup(p => p.Retrieve<object>(secondCached)).Returns(secondRetrieved).Verifiable();

            var builder = new TransparentCacheBuilder<IOverloaded>(sourceMock.Object);
            builder.SetDefaultPolicy(valuePolicyMock.Object);

            var cache = builder.Create();

            Assert.AreSame(firstRetrieved, cache.Cached.Overloaded("first"));
            Assert.AreSame(firstRetrieved, cache.Cached.Overloaded("first"));
            Assert.AreSame(secondRetrieved, cache.Cached.Overloaded("first", "second"));
            Assert.AreSame(secondRetrieved, cache.Cached.Overloaded("first", "second"));

            valuePolicyMock.Verify(p => p.Store(first), Times.Once);
            valuePolicyMock.Verify(p => p.Store(second), Times.Once);
            valuePolicyMock.Verify(p => p.Retrieve<object>(firstCached), Times.Exactly(2));
            valuePolicyMock.Verify(p => p.Retrieve<object>(secondCached), Times.Exactly(2));
        }

        [TestMethod]
        public void SetDefaultValuePolicy_Overridden()
        {
            // we return these from the source
            var first = new object();
            var second = new object();

            // we store these in the cache
            var secondCached = new object();

            // we translate to these on retrieval
            var secondRetrieved = new object();

            var sourceMock = new Mock<IOverloaded>();
            sourceMock.Setup(i => i.Overloaded("first")).Returns(first);
            sourceMock.Setup(i => i.Overloaded("first", "second")).Returns(second);

            var valuePolicyMock = new Mock<ICacheValuePolicy>();
            valuePolicyMock.Setup(p => p.Store(second)).Returns(secondCached).Verifiable();
            valuePolicyMock.Setup(p => p.Retrieve<object>(secondCached)).Returns(secondRetrieved).Verifiable();

            var builder = new TransparentCacheBuilder<IOverloaded>(sourceMock.Object);
            builder.SetPolicy(new IdentityCacheValuePolicy<object>(), i => i.Overloaded("first"));
            builder.SetDefaultPolicy(valuePolicyMock.Object);

            var cache = builder.Create();

            Assert.AreSame(first, cache.Cached.Overloaded("first"));
            Assert.AreSame(secondRetrieved, cache.Cached.Overloaded("first", "second"));
            Assert.AreSame(secondRetrieved, cache.Cached.Overloaded("first", "second"));

            valuePolicyMock.Verify(p => p.Store(first), Times.Never);
            valuePolicyMock.Verify(p => p.Store(second), Times.Once);
            valuePolicyMock.Verify(p => p.Retrieve<object>(secondCached), Times.Exactly(2));
        }
        #endregion

        #region ExpireAll (All caches)
        [TestMethod]
        public void ExpireAll_AllCaches()
        {
            object[] result0 = { new object(), new object() };
            object[] result1 = { new object(), new object() };

            var mock = new Mock<IOverloaded>();
            mock.Setup(s => s.Overloaded("first")).ReturnsSequence(result0);
            mock.Setup(s => s.Overloaded("first", "second")).ReturnsSequence(result1);

            var firstPolicy = new Mock<ICacheExpirationPolicy>(MockBehavior.Strict);
            firstPolicy.Setup(p => p.ExpireAll());

            var secondPolicy = new Mock<ICacheExpirationPolicy>(MockBehavior.Strict);
            secondPolicy.Setup(p => p.ExpireAll());

            var builder = new TransparentCacheBuilder<IOverloaded>(mock.Object);
            builder.SetPolicy(firstPolicy.Object, c => c.Overloaded("first"));
            builder.SetPolicy(secondPolicy.Object, c => c.Overloaded("first", "second"));

            var cache = builder.Create();

            cache.ExpireAll();

            firstPolicy.Verify(p => p.ExpireAll(), Times.Once);
            secondPolicy.Verify(p => p.ExpireAll(), Times.Once);
        }
        #endregion

        #region Insert
        [TestMethod]
        public void HasSingleParameter_Insert()
        {
            object result0 = new object();
            object result1 = new object();

            var mock = new Mock<IHasSingleParameter>();

            var cache = new TransparentCache<IHasSingleParameter>(mock.Object);

            cache.Insert(result0, c => c.HasSingleParameter(10));
            cache.Insert(result1, c => c.HasSingleParameter(11));

            Assert.AreSame(result0, cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1, cache.Cached.HasSingleParameter(11));
            Assert.AreSame(result0, cache.Cached.HasSingleParameter(10));
            Assert.AreSame(result1, cache.Cached.HasSingleParameter(11));

            mock.Verify(i => i.HasSingleParameter(10), Times.Never);
            mock.Verify(i => i.HasSingleParameter(11), Times.Never);
        }

        [TestMethod]
        public void HasMultipleParameters_Insert()
        {
            object result0 = new object();
            object result1 = new object();

            var mock = new Mock<IHasMultipleParameters>();

            var cache = new TransparentCache<IHasMultipleParameters>(mock.Object);

            cache.Insert(result0, i => i.HasMultipleParameters(10, "hi"));
            cache.Insert(result1, i => i.HasMultipleParameters(11, "hi"));

            Assert.AreSame(result0, cache.Cached.HasMultipleParameters(10, "hi"));
            Assert.AreSame(result1, cache.Cached.HasMultipleParameters(11, "hi"));
            Assert.AreSame(result0, cache.Cached.HasMultipleParameters(10, "hi"));
            Assert.AreSame(result1, cache.Cached.HasMultipleParameters(11, "hi"));

            mock.Verify(i => i.HasMultipleParameters(10, "hi"), Times.Never);
            mock.Verify(i => i.HasMultipleParameters(11, "hi"), Times.Never);
        }

        [TestMethod]
        public void Overloaded_Insert()
        {
            object result0 = new object();
            object result1 = new object();

            var mock = new Mock<IOverloaded>();

            var cache = new TransparentCache<IOverloaded>(mock.Object);

            cache.Insert(result0, i => i.Overloaded("first"));
            cache.Insert(result1, i => i.Overloaded("first", "second"));

            Assert.AreSame(result0, cache.Cached.Overloaded("first"));
            Assert.AreSame(result1, cache.Cached.Overloaded("first", "second"));
            Assert.AreSame(result0, cache.Cached.Overloaded("first"));
            Assert.AreSame(result1, cache.Cached.Overloaded("first", "second"));

            mock.Verify(i => i.Overloaded("first"), Times.Never);
            mock.Verify(i => i.Overloaded("first", "second"), Times.Never);
        }

        [TestMethod]
        public void HasRefParameter_Insert()
        {
            object inserted = new object();
            object[] results = { new object(), new object() };

            var value = "a";

            var mock = new Mock<IHasRefParameter>();
            mock.Setup(i => i.HasRefParameter(ref value)).ReturnsSequence(results).Verifiable();

            var cache = new TransparentCache<IHasRefParameter>(mock.Object);

            cache.Insert(inserted, i => i.HasRefParameter(ref value));

            Assert.AreSame(results[0], cache.Cached.HasRefParameter(ref value));
            Assert.AreSame(results[1], cache.Cached.HasRefParameter(ref value));

            mock.VerifyAll();
        }

        [TestMethod]
        public void HasOutParameter_Insert()
        {
            object inserted = new object();
            object[] results = { new object(), new object() };

            var value = "a";

            var mock = new Mock<IHasOutParameter>();
            mock.Setup(i => i.HasOutParameter(out value)).ReturnsSequence(results).Verifiable();

            var cache = new TransparentCache<IHasOutParameter>(mock.Object);

            cache.Insert(inserted, i => i.HasOutParameter(out value));

            Assert.AreSame(results[0], cache.Cached.HasOutParameter(out value));
            Assert.AreSame(results[1], cache.Cached.HasOutParameter(out value));

            mock.VerifyAll();
        }
        #endregion Caching/proxying
    }

    #region Test interfaces
    public interface IReturnsVoid
    {
        void ReturnsVoid(string param);
        void ReturnsVoid(int param);
    }

    public interface IHasSingleParameter
    {
        object HasSingleParameter(int param);
    }

    public interface IHasMultipleParameters
    {
        object HasMultipleParameters(int first, string second);
    }

    public interface IOverloaded
    {
        object Overloaded(string first);
        object Overloaded(string first, string second);
    }

    public interface IHasRefParameter
    {
        object HasRefParameter(ref string param);
        object HasRefParameter(ref int param);
        int HasRefParameterValueTypeReturn(out int param);
    }

    public interface IHasOutParameter
    {
        object HasOutParameter(out string param);
        object HasOutParameter(out int param);
        int HasOutParameterValueTypeReturn(out int param);
    }

    public interface IAsync
    {
        Task NoReturnValueAsync(string param);
        Task<object> WithReturnValueAsync(string param);
    }

    public static class ISetupExtensions
    {
        public static IReturnsResult<TSource> ReturnsSequence<TSource, TResult>(
            this ISetup<TSource, TResult> setup,
            params Func<TResult>[] results)
            where TSource : class
        {
            var enumerator = results.Cast<Func<TResult>>().GetEnumerator();

            return setup.Returns(
                () =>
                {
                    enumerator.MoveNext();
                    return enumerator.Current();
                });
        }

        public static IReturnsResult<TSource> ReturnsSequence<TSource, TResult>(
            this ISetup<TSource, TResult> setup,
            params TResult[] results)
            where TSource : class
        {
            var enumerator = results.Cast<TResult>().GetEnumerator();

            return setup.Returns(
                () =>
                {
                    enumerator.MoveNext();
                    return enumerator.Current;
                });
        }
    }
    #endregion Test interfaces
}
