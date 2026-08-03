using Goose;
using Xunit;

namespace Goose.Tests
{
    public class UtilsTests
    {
        [Fact]
        public void FormatNumber_FormatsLongMinValue()
        {
            Assert.Equal("-9220000000b", Utils.FormatNumber(long.MinValue));
        }
    }
}
