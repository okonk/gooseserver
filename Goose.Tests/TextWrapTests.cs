using Goose;
using Xunit;

namespace Goose.Tests
{
    public class TextWrapTests
    {
        [Fact]
        public void LineAtExactWidth_FitsUnsplit()
        {
            var line = new string('a', 50);
            Assert.Equal([line], TextWrap.Lines(line));
        }

        [Fact]
        public void WordsFillingExactWidth_StayOnOneLine()
        {
            Assert.Equal(
                ["aaaaaaaaaaaaaaaaaaaaaaaaa bbbbbbbbbbbbbbbbbbbbbbbb"],
                TextWrap.Lines("aaaaaaaaaaaaaaaaaaaaaaaaa bbbbbbbbbbbbbbbbbbbbbbbb"));
        }

        [Fact]
        public void WordsExceedingWidth_WrapToNextLine()
        {
            Assert.Equal(
                ["aaaaaaaaaaaaaaaaaaaaaaaaa", "bbbbbbbbbbbbbbbbbbbbbbbbbbb"],
                TextWrap.Lines("aaaaaaaaaaaaaaaaaaaaaaaaa bbbbbbbbbbbbbbbbbbbbbbbbbbb"));
        }

        [Fact]
        public void WordLongerThanWidth_IsHardSplit()
        {
            Assert.Equal(
                [new string('x', 50), "x"],
                TextWrap.Lines(new string('x', 51)));
        }

        [Fact]
        public void WordLongerThanWidth_AfterShortWord_FlushesShortWordFirst()
        {
            Assert.Equal(
                ["hello", new string('x', 50), new string('x', 10)],
                TextWrap.Lines("hello " + new string('x', 60)));
        }

        [Fact]
        public void LiteralBackslashN_SeparatesParagraphs()
        {
            Assert.Equal(
                ["first", "second"],
                TextWrap.Lines("first\\nsecond"));
        }

        [Fact]
        public void RealNewline_SeparatesParagraphs()
        {
            Assert.Equal(
                ["first", "second"],
                TextWrap.Lines("first\nsecond"));
        }

        [Fact]
        public void CrlfNewline_SeparatesParagraphs()
        {
            Assert.Equal(
                ["first", "second"],
                TextWrap.Lines("first\r\nsecond"));
        }

        [Fact]
        public void ParagraphsWrapIndependently()
        {
            var p1 = new string('a', 60);
            var p2 = new string('b', 60);
            Assert.Equal(
                [new string('a', 50), new string('a', 10), new string('b', 50), new string('b', 10)],
                TextWrap.Lines($"{p1}\\n{p2}"));
        }

        [Fact]
        public void EmptyString_ReturnsOneEmptyLine()
        {
            Assert.Equal([""], TextWrap.Lines(""));
        }

        [Fact]
        public void BlankParagraphs_ArePreserved()
        {
            Assert.Equal(
                ["first", "", "second"],
                TextWrap.Lines("first\n\nsecond"));
        }

        [Fact]
        public void BlankParagraphFromLiteralBackslashN_IsPreserved()
        {
            Assert.Equal(
                ["first", "", "second"],
                TextWrap.Lines("first\\n\\nsecond"));
        }

        [Fact]
        public void NonPositiveWidth_IsClampedToOne()
        {
            Assert.Equal(TextWrap.Lines("hello", width: 1), TextWrap.Lines("hello", width: 0));
            Assert.Equal(["h", "e", "l", "l", "o"], TextWrap.Lines("hello", width: 0));
            Assert.Equal(["h", "e", "l", "l", "o"], TextWrap.Lines("hello", width: -5));
        }

        [Fact]
        public void CustomWidth_IsRespected()
        {
            Assert.Equal(
                ["aaaa", "bbbb"],
                TextWrap.Lines("aaaa bbbb", width: 4));
        }
    }
}
