using Goose;
using Xunit;

namespace Goose.Tests
{
    public class NoticePacketTests
    {
        [Fact]
        public void Notice_Quest_BuildsNTF1()
        {
            Assert.Equal("NTF1,a, b", P.Notice(NoticeType.Quest, "a, b"));
        }

        [Fact]
        public void Notice_Info_PinsId0()
        {
            Assert.Equal("NTF0,m", P.Notice(NoticeType.Info, "m"));
        }

        [Fact]
        public void Notice_BuffOn_PinsId2()
        {
            Assert.Equal("NTF2,m", P.Notice(NoticeType.BuffOn, "m"));
        }

        [Fact]
        public void Notice_BuffOff_PinsId3()
        {
            Assert.Equal("NTF3,m", P.Notice(NoticeType.BuffOff, "m"));
        }
    }
}
