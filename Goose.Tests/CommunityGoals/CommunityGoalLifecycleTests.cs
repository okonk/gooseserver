using Goose.CommunityGoals;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CommunityGoalLifecycleTests : IDisposable
    {
        private readonly TestWorldFixture fixture;
        private readonly GameWorld world;
        private readonly CommunityGoalHandler handler;
        private readonly DateTime started = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private DateTime now;

        public CommunityGoalLifecycleTests()
        {
            fixture = new TestWorldFixture();
            world = fixture.World;
            handler = world.CommunityGoals;
            now = started;
            handler.UtcNow = () => now;
        }

        public void Dispose() => fixture.Dispose();

        private static string GoldProps(long? target, string? extra = null)
        {
            var req = target.HasValue
                ? """{ "key": "gold", "type": "Gold", "target": """ + target.Value + " }"
                : """{ "key": "gold", "type": "Gold", "label": "gold" }""";
            return "{ \"requirements\": [ " + req + " ]" + (extra is null ? "" : ", " + extra) + " }";
        }

        private CommunityGoal LoadGoal(int id, string properties, params int[] prerequisites)
        {
            var goal = new CommunityGoal
            {
                Id = id,
                Name = "Goal " + id,
                PropertiesJson = properties,
            };
            goal.PrerequisiteGoals.AddRange(prerequisites);
            handler.LoadDefinitionRows(new[] { goal }, world);
            return handler.Goals[id];
        }

        private CommunityGoalRun LoadRun(int goalId, int run, DateTime startedAt,
                                         RunStatus status = RunStatus.Active,
                                         DateTime? completedAt = null,
                                         params (int PlayerId, long Gold)[] contributions)
        {
            var runObj = new CommunityGoalRun
            {
                GoalId = goalId,
                Run = run,
                Status = status,
                StartedAt = startedAt,
                CompletedAt = completedAt,
            };
            foreach (var (playerId, gold) in contributions)
            {
                var contribution = new Contribution { PlayerId = playerId, LastAt = startedAt };
                contribution.Amounts["gold"] = gold;
                runObj.Contributions[playerId] = contribution;
            }
            handler.LoadStateRows(new[] { runObj }, Array.Empty<(int, int, Contribution)>(), Array.Empty<WorldFlag>());
            return runObj;
        }

        [Fact]
        public void Tick_GoalWithoutPrerequisites_StartsRun1()
        {
            var goal = LoadGoal(1, GoldProps(1000));

            handler.Tick(world);

            var run = handler.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(1, run!.Run);
            Assert.Equal(RunStatus.Active, run.Status);
            Assert.Equal(now, run.StartedAt);
            Assert.Empty(run.Totals);
            Assert.True(handler.IsRunDirty(1, 1));
        }

        [Fact]
        public void Tick_PrerequisiteMetButClosesAtPast_StartsFailedTerminalRun()
        {
            var goal1 = new CommunityGoal { Id = 1, Name = "Goal 1", PropertiesJson = GoldProps(1000) };
            var goal2 = new CommunityGoal { Id = 2, Name = "Goal 2", PropertiesJson = GoldProps(2000, """ "closesAt": "2025-05-01T00:00:00Z" """) };
            goal2.PrerequisiteGoals.Add(1);
            handler.LoadDefinitionRows(new[] { goal1, goal2 }, world);
            var run1 = LoadRun(1, 1, started, status: RunStatus.Completed, completedAt: now);

            handler.Tick(world);

            Assert.Equal(RunStatus.Completed, run1.Status);
            var run2 = handler.CurrentRun(2);
            Assert.NotNull(run2);
            Assert.Equal(1, run2!.Run);
            Assert.Equal(RunStatus.Failed, run2.Status);
            Assert.Equal(now, run2.CompletedAt);
        }

        [Fact]
        public void Tick_FutureClosesAt_StartsRun()
        {
            LoadGoal(1, GoldProps(1000, """ "closesAt": "2025-07-01T00:00:00Z" """));

            handler.Tick(world);

            var run = handler.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(RunStatus.Active, run!.Status);
            Assert.Equal(now, run.StartedAt);
        }

        [Fact]
        public void Tick_NoRunTargetlessClosesAtPast_StartsCompletedTerminalRun()
        {
            var goal = LoadGoal(1, GoldProps(null, """ "closesAt": "2025-05-01T00:00:00Z" """));
            var events = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            handler.RunCompleted += (g, r, w) => events.Add((g, r));

            handler.Tick(world);

            var run = handler.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(1, run!.Run);
            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Equal(now, run.StartedAt);
            Assert.Equal(now, run.CompletedAt);
            Assert.True(handler.IsRunDirty(1, 1));
            Assert.Same(goal, events[0].Goal);
            Assert.Same(run, events[0].Run);
            Assert.Equal(1, events.Count);
        }

        [Fact]
        public void Tick_NoRunTargetedClosesAtPast_StartsFailedTerminalRun()
        {
            LoadGoal(1, GoldProps(1000, """ "closesAt": "2025-05-01T00:00:00Z" """));
            var events = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            handler.RunCompleted += (g, r, w) => events.Add((g, r));

            handler.Tick(world);

            var run = handler.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(1, run!.Run);
            Assert.Equal(RunStatus.Failed, run.Status);
            Assert.Equal(now, run.CompletedAt);
            Assert.True(handler.IsRunDirty(1, 1));
            Assert.Empty(events);
        }

        [Fact]
        public void Tick_NoRunTargetedClosesAtPast_ScriptIsComplete_StartsCompletedTerminalRun()
        {
            fixture.CompileCommunityGoalScript(@"
public class G : BaseCommunityGoalScript
{
    public override CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world)
        => new()
        {
            Requirements = new[] { Requirement.Gold(""gold"", 1000) },
            ClosesAt = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    public override bool IsComplete(CommunityGoal goal, CommunityGoalRun run, GameWorld world) => true;
}
return typeof(G);
");
            var goal = new CommunityGoal
            {
                Id = 1,
                Name = "Goal 1",
                ScriptPath = "Scripts/CommunityGoal/G.csx",
                PropertiesJson = "{}",
            };
            handler.LoadDefinitionRows(new[] { goal }, world);
            var events = new List<(CommunityGoal Goal, CommunityGoalRun Run)>();
            handler.RunCompleted += (g, r, w) => events.Add((g, r));

            handler.Tick(world);

            var run = handler.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(1, run!.Run);
            Assert.Equal(RunStatus.Completed, run.Status);
            Assert.Equal(now, run.CompletedAt);
            Assert.Same(goal, events[0].Goal);
            Assert.Same(run, events[0].Run);
            Assert.Equal(1, events.Count);
        }

        [Fact]
        public void Tick_DependentGoal_StartsAfterTerminalCompletedPrereq()
        {
            var goal1 = new CommunityGoal { Id = 1, Name = "Goal 1", PropertiesJson = GoldProps(null, """ "closesAt": "2025-05-01T00:00:00Z" """) };
            var goal2 = new CommunityGoal { Id = 2, Name = "Goal 2", PropertiesJson = GoldProps(2000) };
            goal2.PrerequisiteGoals.Add(1);
            handler.LoadDefinitionRows(new[] { goal1, goal2 }, world);

            handler.Tick(world);
            handler.Tick(world);

            var prereqRun = handler.CurrentRun(1);
            Assert.NotNull(prereqRun);
            Assert.Equal(RunStatus.Completed, prereqRun!.Status);
            var run2 = handler.CurrentRun(2);
            Assert.NotNull(run2);
            Assert.Equal(1, run2!.Run);
            Assert.Equal(RunStatus.Active, run2.Status);
        }

        [Fact]
        public void Tick_RepeatingClosesAtPast_DoesNotStartNewRun()
        {
            LoadGoal(1, GoldProps(100, """ "closesAt": "2025-05-01T00:00:00Z", "repeat": "onClose" """));
            LoadRun(1, 1, started, status: RunStatus.Completed, completedAt: started.AddHours(1));

            handler.Tick(world);

            var run = handler.CurrentRun(1);
            Assert.NotNull(run);
            Assert.Equal(1, run!.Run);
            Assert.Equal(RunStatus.Completed, run.Status);
        }

        [Fact]
        public void Tick_PrerequisiteRun1NotCompleted_DoesNotStartUntilItCompletes()
        {
            var goal1 = new CommunityGoal { Id = 1, Name = "Goal 1", PropertiesJson = GoldProps(1000) };
            var goal2 = new CommunityGoal { Id = 2, Name = "Goal 2", PropertiesJson = GoldProps(2000) };
            goal2.PrerequisiteGoals.Add(1);
            handler.LoadDefinitionRows(new[] { goal1, goal2 }, world);
            var run1 = LoadRun(1, 1, started);

            handler.Tick(world);
            Assert.Null(handler.CurrentRun(2));

            run1.Status = RunStatus.Completed;
            run1.CompletedAt = now;
            handler.Tick(world);

            var run2 = handler.CurrentRun(2);
            Assert.NotNull(run2);
            Assert.Equal(1, run2!.Run);
            Assert.Equal(RunStatus.Active, run2.Status);
            Assert.Equal(now, run2.StartedAt);
        }

        [Fact]
        public void Tick_PrerequisiteRun1Failed_BlocksStart()
        {
            var goal1 = new CommunityGoal { Id = 1, Name = "Goal 1", PropertiesJson = GoldProps(1000) };
            var goal2 = new CommunityGoal { Id = 2, Name = "Goal 2", PropertiesJson = GoldProps(2000) };
            goal2.PrerequisiteGoals.Add(1);
            handler.LoadDefinitionRows(new[] { goal1, goal2 }, world);
            LoadRun(1, 1, started, status: RunStatus.Failed, completedAt: started.AddHours(1));

            using var log = new CapturingLog();
            handler.Tick(world);

            Assert.Null(handler.CurrentRun(2));
            Assert.Empty(log.Messages.Where(m => m.Contains("missing prerequisite goal")));
        }

        [Fact]
        public void Tick_MissingPrerequisiteGoal_NotStartedAndLogsOnce()
        {
            LoadGoal(2, GoldProps(2000), prerequisites: 99);

            using var log = new CapturingLog();
            handler.Tick(world);
            handler.Tick(world);

            Assert.Null(handler.CurrentRun(2));
            Assert.Equal(1, log.Messages.Count(m => m.Contains("missing prerequisite goal 99")));
        }

        [Fact]
        public void OnClose_CompletionViaContribute_StartsNextRunImmediately()
        {
            LoadGoal(1, GoldProps(100, """ "repeat": "onClose" """));
            var run1 = LoadRun(1, 1, started);
            run1.Totals["gold"] = 99;

            handler.Contribute(handler.Goals[1], new Player(0), "gold", 1, world);

            Assert.Equal(RunStatus.Completed, run1.Status);
            var run2 = handler.CurrentRun(1);
            Assert.Equal(2, run2!.Run);
            Assert.Equal(RunStatus.Active, run2.Status);
            Assert.Equal(now, run2.StartedAt);
            Assert.Empty(run2.Totals);
            Assert.True(handler.IsRunDirty(1, 2));
        }

        [Fact]
        public void OnClose_TickBackstop_StartsNextRunAfterReload()
        {
            LoadGoal(1, GoldProps(100, """ "repeat": "onClose" """));
            LoadRun(1, 1, started, status: RunStatus.Completed, completedAt: started.AddHours(1));

            handler.Tick(world);

            var run2 = handler.CurrentRun(1);
            Assert.Equal(2, run2!.Run);
            Assert.Equal(RunStatus.Active, run2.Status);
            Assert.Equal(now, run2.StartedAt);
        }

        [Fact]
        public void Every_7d_StartsNextRunOnlyAfterSpanFromRun1Start()
        {
            LoadGoal(1, GoldProps(100, """ "repeat": { "every": "7d" } """));
            LoadRun(1, 1, started, status: RunStatus.Completed, completedAt: started.AddHours(1));

            now = started.AddDays(6);
            handler.Tick(world);
            Assert.Equal(1, handler.CurrentRun(1)!.Run);

            now = started.AddDays(7);
            handler.Tick(world);

            var run2 = handler.CurrentRun(1);
            Assert.Equal(2, run2!.Run);
            Assert.Equal(RunStatus.Active, run2.Status);
            Assert.Equal(now, run2.StartedAt);
        }

        [Fact]
        public void Expiry_RemovesContributionsAfterClaimWindow_ForRepeatingGoal()
        {
            LoadGoal(1, GoldProps(100, """ "repeat": "onClose" """));
            var run1 = LoadRun(1, 1, started, status: RunStatus.Completed,
                completedAt: started, contributions: new[] { (5, 42L), (6, 10L) });

            now = started.AddDays(29);
            handler.Tick(world);
            Assert.Equal(2, run1.Contributions.Count);

            now = started.AddDays(30);
            handler.Tick(world);

            Assert.Empty(run1.Contributions);
            Assert.Contains((1, 1, 5), handler.PendingContributionDeletes);
            Assert.Contains((1, 1, 6), handler.PendingContributionDeletes);
            Assert.False(handler.IsRunDirty(1, 1));
            Assert.Equal(RunStatus.Completed, run1.Status);
            Assert.Equal(started, run1.CompletedAt);
        }

        [Fact]
        public void Expiry_NeverRemovesContributions_ForOneRunGoal()
        {
            LoadGoal(1, GoldProps(100));
            var run1 = LoadRun(1, 1, started, status: RunStatus.Completed,
                completedAt: started, contributions: new[] { (5, 42L) });

            now = started.AddDays(60);
            handler.Tick(world);

            Assert.Single(run1.Contributions);
            Assert.Empty(handler.PendingContributionDeletes);
        }

        [Fact]
        public void FailedRun_RepeatingGoal_StartsNextRunOnNextTick()
        {
            LoadGoal(1, GoldProps(100, """ "repeat": { "every": "7d" } """));
            var run1 = LoadRun(1, 1, started, status: RunStatus.Failed, completedAt: started.AddHours(1));

            now = started.AddDays(6);
            handler.Tick(world);
            Assert.Equal(1, handler.CurrentRun(1)!.Run);

            now = started.AddDays(7);
            handler.Tick(world);

            var run2 = handler.CurrentRun(1);
            Assert.Equal(2, run2!.Run);
            Assert.Equal(RunStatus.Active, run2.Status);
            Assert.Equal(now, run2.StartedAt);
            Assert.Equal(RunStatus.Failed, run1.Status);
            Assert.Equal(started.AddHours(1), run1.CompletedAt);
            Assert.Equal(started, run1.StartedAt);
        }

        [Fact]
        public void FailedRun_OnCloseRepeatingGoal_StartsNextRunImmediatelyOnTick()
        {
            LoadGoal(1, GoldProps(100, """ "repeat": "onClose" """));
            LoadRun(1, 1, started, status: RunStatus.Failed, completedAt: started.AddHours(1));

            handler.Tick(world);

            var run2 = handler.CurrentRun(1);
            Assert.Equal(2, run2!.Run);
            Assert.Equal(RunStatus.Active, run2.Status);
            Assert.Equal(now, run2.StartedAt);
        }

        [Fact]
        public void Tick_TwiceAtSameNow_StartsOneRun()
        {
            LoadGoal(1, GoldProps(1000));

            handler.Tick(world);
            handler.Tick(world);

            Assert.Equal(1, handler.CurrentRun(1)!.Run);
            Assert.Equal(now, handler.CurrentRun(1)!.StartedAt);
        }

        [Fact]
        public void Tick_CompletesTimedOutRunAndStartsNextForOnClose()
        {
            LoadGoal(1, GoldProps(null, """ "duration": "1h", "repeat": "onClose" """));
            var run1 = LoadRun(1, 1, started);

            now = started.AddHours(2);
            handler.Tick(world);

            Assert.Equal(RunStatus.Completed, run1.Status);
            Assert.Equal(now, run1.CompletedAt);
            var run2 = handler.CurrentRun(1);
            Assert.Equal(2, run2!.Run);
            Assert.Equal(RunStatus.Active, run2.Status);
            Assert.Equal(now, run2.StartedAt);
        }

        [Fact]
        public void AddTickEvent_SchedulesCommunityGoalTickEvent()
        {
            var before = world.EventHandler.Count;

            handler.AddTickEvent(world);

            Assert.Equal(before + 1, world.EventHandler.Count);
            Assert.IsType<CommunityGoalTickEvent>(world.EventHandler.Peek());
        }

        [Fact]
        public void Tick_RearmsTickEvent()
        {
            LoadGoal(1, GoldProps(1000));
            var before = world.EventHandler.Count;

            handler.Tick(world);

            Assert.Equal(before + 1, world.EventHandler.Count);
            Assert.IsType<CommunityGoalTickEvent>(world.EventHandler.Peek());
        }

        [Fact]
        public void Load_AddsTickEvent()
        {
            world.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
            world.Database.Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE community_goals (id INTEGER PRIMARY KEY, name TEXT, description TEXT, npc_template_id INTEGER, prerequisite_goals TEXT, script_path TEXT, properties TEXT);
                    CREATE TABLE community_goal_runs (goal_id INTEGER, run INTEGER, status TEXT, started_at TEXT, completed_at TEXT, totals TEXT, results TEXT);
                    CREATE TABLE community_goal_contributions (goal_id INTEGER, run INTEGER, player_id INTEGER, amounts TEXT, last_at TEXT, claimed_bracket INTEGER, claimed_at TEXT);
                    CREATE TABLE world_flags (name TEXT PRIMARY KEY, value INTEGER, set_at TEXT, source_goal_id INTEGER);
                    INSERT INTO community_goals (id, name, description, npc_template_id, prerequisite_goals, script_path, properties)
                    VALUES (1, 'Goal 1', 'desc', 0, '', '', '{ "requirements": [ { "key": "gold", "type": "Gold", "target": 1000 } ] }');
                    """;
                cmd.ExecuteNonQuery();
            });
            var before = world.EventHandler.Count;

            handler.Load(world);

            Assert.Equal(1, handler.Goals.Count);
            Assert.Equal(before + 2, world.EventHandler.Count);
            Assert.IsType<CommunityGoalTickEvent>(world.EventHandler.Peek());
        }
    }
}
