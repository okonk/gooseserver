using Goose;
using Xunit;

namespace Goose.Tests
{
    public class MapWarpPacketTests
    {
        [Fact]
        public void MapWarp_ProducesExpectedPacket()
        {
            Assert.Equal("MWP3,7", P.MapWarp(3, 7));
        }
    }
}
