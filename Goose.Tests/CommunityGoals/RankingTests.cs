using System.Text.Json.Nodes;
using Goose.CommunityGoals;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class RankingTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private DateTime now;

        public RankingTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            handler = world.CommunityGoals;
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private static CommunityGoalDefinition ScoreDef(double goldWeight = 1, params GoalBracket[] brackets) =>
            new()
            {
                Requirements = new[] { Requirement.Gold("gold", 100) },
                ScoreWeights = new Dictionary<string, double>(StringComparer.Ordinal) { ["gold"] = goldWeight },
                Ranking = RankingKind.Score,
                Brackets = brackets,
            };

        private static CommunityGoalDefinition LotteryDef(long entryCost, params GoalBracket[] brackets) =>
            new()
            {
                Requirements = new[] { Requirement.Gold("gold", 100) },
                Ranking = RankingKind.Lottery,
                EntryCost = new Dictionary<string, long>(StringComparer.Ordinal) { ["gold"] = entryCost },
                Brackets = brackets,
            };

        private static GoalBracket[] Ranks(int count)
        {
            var brackets = new GoalBracket[count];
            for (var i = 0; i < count; i++)
                brackets[i] = new GoalBracket { Rank = i + 1 };
            return brackets;
        }

        private static CommunityGoalRun MakeRun(params (int Id, long Gold, DateTime LastAt)[] contributors)
        {
            var run = new CommunityGoalRun
            {
                GoalId = 1, Run = 1, StartedAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            };
            foreach (var (id, gold, lastAt) in contributors)
            {
                var contribution = new Contribution { PlayerId = id, LastAt = lastAt };
                contribution.Amounts["gold"] = gold;
                run.Contributions[id] = contribution;
            }
            return run;
        }

        private static Contribution MakeContribution(int id, long gold, DateTime lastAt)
        {
            var contribution = new Contribution { PlayerId = id, LastAt = lastAt };
            contribution.Amounts["gold"] = gold;
            return contribution;
        }

        [Fact]
        public void Score_SumsWeightTimesAmount_OverCurrentRequirementKeys()
        {
            var definition = ScoreDef(goldWeight: 2);
            var contribution = MakeContribution(5, 10, started);
            contribution.Amounts["removed"] = 1000;

            Assert.Equal(20, Ranking.Score(definition, contribution));
        }

        [Fact]
        public void Score_DefaultWeightIsOne_WhenKeyMissingFromScoreWeights()
        {
            var definition = new CommunityGoalDefinition
            {
                Requirements = new[] { Requirement.Gold("gold", 100) },
                Ranking = RankingKind.Score,
            };

            Assert.Equal(42, Ranking.Score(definition, MakeContribution(5, 42, started)));
        }

        [Fact]
        public void Entries_FloorsAmountByCostAndSums()
        {
            var definition = LotteryDef(10);
            var contribution = MakeContribution(5, 25, started);

            Assert.Equal(2, Ranking.Entries(definition, contribution));
        }

        [Fact]
        public void Freeze_Score_TieBrokenByEarlierLastAt()
        {
            var definition = ScoreDef(brackets: Ranks(2));
            var run = MakeRun(
                (5, 40, started),
                (7, 40, started.AddHours(1)),
                (6, 10, started));

            Assert.Equal(new[] { 5, 7 }, Ranking.Freeze(definition, run, new Random(1)));
        }

        [Fact]
        public void Freeze_Score_TieOnLastAt_BrokenByPlayerId()
        {
            var definition = ScoreDef(brackets: Ranks(2));
            var run = MakeRun(
                (7, 40, started),
                (5, 40, started));

            Assert.Equal(new[] { 5, 7 }, Ranking.Freeze(definition, run, new Random(1)));
        }

        [Fact]
        public void Freeze_Score_ExcludesZeroScorePlayers()
        {
            var definition = ScoreDef(brackets: Ranks(3));
            var zero = MakeContribution(9, 0, started);
            zero.Amounts["removed"] = 500;
            var run = MakeRun((5, 40, started), (9, 0, started));
            run.Contributions[9] = zero;

            Assert.Equal(new[] { 5 }, Ranking.Freeze(definition, run, new Random(1)));
        }

        [Fact]
        public void Freeze_Score_FewerContributorsThanPlaces_ShorterPlaces()
        {
            var definition = ScoreDef(brackets: Ranks(5));
            var run = MakeRun((5, 40, started));

            Assert.Equal(new[] { 5 }, Ranking.Freeze(definition, run, new Random(1)));
        }

        [Fact]
        public void Freeze_ZeroContributors_EmptyPlaces()
        {
            var definition = ScoreDef(brackets: Ranks(3));

            Assert.Empty(Ranking.Freeze(definition, MakeRun(), new Random(1)));
        }

        [Fact]
        public void Freeze_NoRankedBrackets_EmptyPlaces()
        {
            var definition = ScoreDef(brackets: []);
            var run = MakeRun((5, 40, started));

            Assert.Empty(Ranking.Freeze(definition, run, new Random(1)));
        }

        [Fact]
        public void Freeze_Lottery_SeededRandom_DeterministicAndNeverRepeats()
        {
            var definition = LotteryDef(10, Ranks(6));
            var run = MakeRun(
                (1, 10, started),
                (2, 20, started),
                (3, 30, started),
                (4, 40, started),
                (5, 50, started),
                (6, 60, started));

            var first = Ranking.Freeze(definition, run, new Random(42));
            var second = Ranking.Freeze(definition, run, new Random(42));
            var third = Ranking.Freeze(definition, run, new Random(43));

            Assert.Equal(first, second);
            Assert.NotEqual(first, third);
            Assert.Equal(6, first.Count);
            Assert.Equal(6, first.Distinct().Count());
            Assert.Subset(new HashSet<int>(first), new HashSet<int> { 1, 2, 3, 4, 5, 6 });
        }

        [Fact]
        public void Freeze_Lottery_ZeroEntriesPlayer_NeverWins()
        {
            var definition = LotteryDef(100, Ranks(4));
            var run = MakeRun(
                (5, 250, started),
                (6, 99, started),
                (7, 500, started));

            var places = Ranking.Freeze(definition, run, new Random(7));

            Assert.Equal(2, places.Count);
            Assert.DoesNotContain(6, places);
            Assert.Equal(2, places.Distinct().Count());
            Assert.Subset(new HashSet<int>(places), new HashSet<int> { 5, 7 });
        }

        [Fact]
        public void Freeze_Lottery_AllZeroEntries_EmptyPlaces()
        {
            var definition = LotteryDef(100, Ranks(2));
            var run = MakeRun((5, 50, started));

            Assert.Empty(Ranking.Freeze(definition, run, new Random(7)));
        }

        private CommunityGoal LoadGoal(string properties)
        {
            var goal = new CommunityGoal
            {
                Id = 1,
                Name = "Goal 1",
                PropertiesJson = properties,
            };
            handler.LoadDefinitionRows(new[] { goal }, world);
            return handler.Goals[1];
        }

        [Fact]
        public void Contribute_Completion_WritesResultsBeforeRunCompleted()
        {
            LoadGoal("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                  "score": { "gold": 2 },
                  "brackets": [ { "rank": 1 }, { "rank": 2 }, { "rank": 3 } ] }
                """);
            var run = MakeRun(
                (5, 40, started),
                (7, 40, started.AddHours(1)));
            run.Totals["gold"] = 80;
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());

            JsonObject? resultsAtEvent = null;
            handler.RunCompleted += (g, r, w) => resultsAtEvent = r.Results;

            handler.Contribute(handler.Goals[1], new Player(0) { PlayerID = 6, Name = "P6" }, "gold", 20, world);

            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.NotNull(run.Results);
            Assert.Same(run.Results, resultsAtEvent);
            Assert.Equal(new[] { 5, 7, 6 }, resultsAtEvent!["places"]!.AsArray().Select(n => n!.GetValue<int>()).ToArray());
            var top = resultsAtEvent["top"]!.AsArray();
            Assert.Equal(3, top.Count);
            Assert.Equal(5, top[0]["player"]!.GetValue<int>());
            Assert.Equal(80, top[0]["score"]!.GetValue<double>());
            Assert.Equal(7, top[1]["player"]!.GetValue<int>());
            Assert.Equal(80, top[1]["score"]!.GetValue<double>());
            Assert.Equal(6, top[2]["player"]!.GetValue<int>());
            Assert.Equal(40, top[2]["score"]!.GetValue<double>());
            Assert.Equal(100, resultsAtEvent["totals"]!["gold"]!.GetValue<long>());
            Assert.True(handler.IsRunDirty(1, 1));
        }

        [Fact]
        public void Contribute_Completion_TopCappedAtTen()
        {
            LoadGoal("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 10000 } ] }
                """);
            var contributors = Enumerable.Range(1, 11)
                .Select(i => (i, 900L, started.AddMinutes(i)))
                .Append((12, 99L, started.AddMinutes(12)))
                .ToArray();
            var run = MakeRun(contributors);
            foreach (var (id, gold, _) in contributors)
                run.Totals["gold"] = run.Totals.GetValueOrDefault("gold") + gold;
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());

            handler.Contribute(handler.Goals[1], new Player(0) { PlayerID = 0, Name = "P0" }, "gold", 1, world);

            Assert.Equal(RunStatus.Completed, run.Status);
            var top = run.Results!["top"]!.AsArray();
            Assert.Equal(10, top.Count);
            Assert.Equal(1, top[0]["player"]!.GetValue<int>());
            Assert.Equal(900, top[0]["score"]!.GetValue<double>());
            Assert.Equal(10, top[9]["player"]!.GetValue<int>());
        }

        [Fact]
        public void Tick_FailedRun_DoesNotWriteResults()
        {
            LoadGoal("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                  "duration": "1h" }
                """);
            var run = MakeRun((5, 40, started));
            run.Totals["gold"] = 40;
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());

            now = started.AddHours(2);
            handler.Tick(world);

            Assert.Equal(RunStatus.Failed, run.Status);
            Assert.Null(run.Results);
        }

        [Fact]
        public void LoadStateRows_CompletedRun_ReloadKeepsStoredPlacesUnchanged()
        {
            LoadGoal("""
                { "requirements": [ { "key": "gold", "type": "Gold", "target": 100 } ],
                  "brackets": [ { "rank": 1 }, { "rank": 2 } ] }
                """);
            // Stored places deliberately disagree with a fresh score ranking (player 5
            // contributed more), so any recomputation would be visible.
            var stored = JsonNode.Parse("""{ "places": [7, 5], "top": [], "totals": { "gold": 100 } }""")!.AsObject();
            var run = MakeRun(
                (5, 60, started),
                (7, 40, started.AddHours(1)));
            run.Status = RunStatus.Completed;
            run.CompletedAt = started.AddHours(1);
            run.Totals["gold"] = 100;
            run.Results = stored;
            handler.LoadStateRows(new[] { run }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());

            handler.Tick(world);
            handler.Tick(world);

            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Same(stored, run.Results);
            Assert.Equal(new[] { 7, 5 }, run.Results!["places"]!.AsArray().Select(n => n!.GetValue<int>()).ToArray());
        }
    }
}
