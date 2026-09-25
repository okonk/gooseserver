using Goose;
using Xunit;

namespace Goose.Tests
{
    public class BodyClassificationTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(99)]
        [InlineData(10000)]
        [InlineData(10050)]
        [InlineData(10099)]
        public void IsLayered_native_and_imported_bodies_are_layered(int bodyId)
        {
            Assert.True(BodyClassification.IsLayered(bodyId));
        }

        [Theory]
        [InlineData(100)]
        [InlineData(9999)]
        [InlineData(10100)]
        [InlineData(109999)]
        public void IsLayered_illusions_and_beyond_import_range_are_compact(int bodyId)
        {
            Assert.False(BodyClassification.IsLayered(bodyId));
        }
    }
}
