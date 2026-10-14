using Goose.CommunityGoals;
using Xunit;

namespace Goose.Tests
{
    public class WorldFlagTests
    {
        private static CommunityGoalHandler Handler(out DateTime now)
        {
            var handler = new CommunityGoalHandler();
            var nowValue = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
            now = nowValue;
            handler.UtcNow = () => nowValue;
            return handler;
        }

        [Fact]
        public void SetFlag_SetsValueSourceAndMarksDirty()
        {
            var handler = Handler(out var now);

            handler.SetFlag("chest_pages:77", 2, 5);

            Assert.Equal(2, handler.GetFlag("chest_pages:77"));
            var row = Assert.Single(handler.PlanSave().Flags);
            Assert.Equal("chest_pages:77", row.Name);
            Assert.Equal(2, row.Value);
            Assert.Equal(now.ToString("o"), row.SetAt);
            Assert.Equal(5, row.SourceGoalId);
        }

        [Fact]
        public void SetFlag_OverwritesExistingValueAndSource()
        {
            var handler = Handler(out _);

            handler.SetFlag("f", 1, 5);
            handler.SetFlag("f", 3, 9);

            Assert.Equal(3, handler.GetFlag("f"));
            var row = Assert.Single(handler.PlanSave().Flags);
            Assert.Equal(3, row.Value);
            Assert.Equal(9, row.SourceGoalId);
        }

        [Fact]
        public void AddToFlag_AccumulatesFromZeroAndMarksDirty()
        {
            var handler = Handler(out var now);

            Assert.Equal(1, handler.AddToFlag("chest_pages:77", 1, null));
            Assert.Equal(2, handler.AddToFlag("chest_pages:77", 1, null));

            Assert.Equal(2, handler.GetFlag("chest_pages:77"));
            var row = Assert.Single(handler.PlanSave().Flags);
            Assert.Equal(2, row.Value);
            Assert.Equal(now.ToString("o"), row.SetAt);
            Assert.Null(row.SourceGoalId);
        }

        [Fact]
        public void AddToFlag_NegativeDelta_CanGoNegative()
        {
            var handler = Handler(out _);

            Assert.Equal(-1, handler.AddToFlag("f", -1, null));
            Assert.Equal(-1, handler.GetFlag("f"));
        }

        [Fact]
        public void UnchangedFlags_PlanHasNoFlagRows()
        {
            var handler = Handler(out _);

            Assert.Empty(handler.PlanSave().Flags);

            handler.SetFlag("f", 1, null);
            handler.PlanSave();
            Assert.Empty(handler.PlanSave().Flags);
        }
    }
}
