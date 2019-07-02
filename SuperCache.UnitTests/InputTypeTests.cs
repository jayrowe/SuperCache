using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SuperCache.Policies;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SuperCache.UnitTests
{
    [TestClass]
    public class InputTypeTests
    {
        [TestMethod]
        public async Task DateTimeInput()
        {
            await RunInput(
                DateTime.MinValue,
                DateTime.UtcNow,
                DateTime.MaxValue);
        }

        [TestMethod]
        public async Task StringInput()
        {
            await RunInput(
                "",
                null,
                "    ",
                "string");
        }

        [TestMethod]
        public async Task NullableInt()
        {
            await RunInput(
                (int?)null,
                int.MinValue,
                int.MaxValue,
                0);
        }

        [TestMethod]
        public async Task NullableDateTime()
        {
            await RunInput(
                (DateTime?)null,
                DateTime.MinValue,
                DateTime.UtcNow,
                DateTime.MaxValue);
        }

        [TestMethod]
        public async Task Enum()
        {
            await RunInput(System.IO.FileMode.OpenOrCreate);
        }

        private async Task RunInput<T>(params T[] values)
        {
            var mock = new Mock<IAsyncGet<T, object>>();

            var expected = new object[values.Length];
            var indices = new int[values.Length];

            for (int index = 0; index < values.Length; index++)
            {
                expected[index] = new object();
                indices[index] = index;

                mock.Setup(m => m.GetAsync(values[index])).ReturnsAsync(expected[index]);
            }


            var cache = new TransparentCacheBuilder<IAsyncGet<T, object>>(mock.Object).
                    SetDefaultPolicy(new DurationSinceFetchCacheExpirationPolicy(TimeSpan.FromMinutes(30.0))).
                    Create();

            for (int iterations = 0; iterations < 10; iterations++)
            {
                var random = new Random();
                var keys = new byte[indices.Length];

                random.NextBytes(keys);

                Array.Sort(
                    keys,
                    indices);

                for (int index = 0; index < indices.Length; index++)
                {
                    Assert.AreSame(expected[indices[index]], await cache.Cached.GetAsync(values[indices[index]]));
                }
            }

            for (int index = 0; index < values.Length; index++)
            {
                mock.Verify(m => m.GetAsync(values[index]), Times.Once);
            }
        }

        public interface IAsyncGet<TInput, TOutput>
        {
            Task<TOutput> GetAsync(TInput startDate);
        }
    }
}
