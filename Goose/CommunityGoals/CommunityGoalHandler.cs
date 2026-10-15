using System.Data.SQLite;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Goose.Events;
using Goose.Scripting;

namespace Goose.CommunityGoals
{
    public readonly record struct ContributeResult(long Accepted, string? Refusal);

    public class CommunityGoalHandler
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly BaseCommunityGoalScript baseScript = new();
        private readonly HashSet<int> orphanGoalIds = new();
        private Dictionary<int, CommunityGoal> goals = new();
        private Dictionary<int, List<(CommunityGoal, GoalRequirement)>> killIndex = new();
        private Dictionary<(int GoalId, int Run), CommunityGoalRun> runs = new();
        private Dictionary<string, WorldFlag> flags = new();
        private readonly HashSet<(int GoalId, int Run)> dirtyRuns = new();
        private readonly HashSet<(int GoalId, int Run, int PlayerId)> dirtyContributions = new();
        private readonly HashSet<(int GoalId, int Run, int PlayerId)> pendingContributionDeletes = new();
        private readonly HashSet<string> dirtyFlags = new();
        private readonly List<SavePlan> inFlightPlans = new();
        private readonly HashSet<int> missingPrerequisiteGoalIds = new();

        public IReadOnlyDictionary<int, CommunityGoal> Goals => this.goals;
        internal IReadOnlyDictionary<int, List<(CommunityGoal, GoalRequirement)>> KillIndex => this.killIndex;
        internal IReadOnlyCollection<(int GoalId, int Run, int PlayerId)> PendingContributionDeletes => this.pendingContributionDeletes;
        internal Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;
        public event Action<CommunityGoal, CommunityGoalRun, GameWorld>? RunCompleted;

        public void Load(GameWorld world)
        {
            this.RunCompleted += this.GiveCommunityRewards;
            this.RunCompleted += this.AnnounceCompletion;
            this.LoadDefinitions(world);

            var (runRows, contributionRows, flagRows) = world.Database.Execute(conn =>
                (ReadRuns(conn), ReadContributions(conn), ReadFlags(conn)));
            this.LoadStateRows(runRows, contributionRows, flagRows);
            this.AddTickEvent(world);
            this.AddSaveEvent(world);
        }

        public void LoadDefinitions(GameWorld world)
        {
            var rows = world.Database.Execute<List<CommunityGoal>>(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, name, description, npc_template_id, prerequisite_goals, script_path, properties FROM community_goals";
                using var reader = cmd.ExecuteReader();
                var result = new List<CommunityGoal>();
                while (reader.Read())
                {
                    var goal = new CommunityGoal
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        NpcTemplateId = reader.GetInt32(3),
                        PrerequisiteGoalsRaw = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        ScriptPath = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        PropertiesJson = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    };
                    result.Add(goal);
                }
                return result;
            });
            this.LoadDefinitionRows(rows, world);
        }

        internal void LoadDefinitionRows(IEnumerable<CommunityGoal> rows, GameWorld world)
        {
            var newGoals = new Dictionary<int, CommunityGoal>();
            foreach (var row in rows)
            {
                try
                {
                    if (row.PrerequisiteGoalsRaw is not null)
                        row.PrerequisiteGoals.AddRange(ParsePrerequisites(row.PrerequisiteGoalsRaw));
                    row.Script = string.IsNullOrEmpty(row.ScriptPath)
                        ? null
                        : world.ScriptHandler.GetScript<ICommunityGoalScript>(row.ScriptPath);
                    var script = row.Script?.Object ?? this.baseScript;
                    var definition = script.Define(row, world);
                    var errors = CommunityGoalDefinitionParser.Validate(definition, row.Script is not null);
                    if (errors.Count > 0)
                        throw new CommunityGoalDefinitionException(string.Join("; ", errors));
                    ResolveRewardScripts(definition, world);
                    row.Definition = definition;
                    row.ResolvedScript = script;
                    newGoals[row.Id] = row;
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed to load community goal {goalId}, skipping", row.Id);
                }
            }

            this.goals = newGoals;
            this.killIndex = this.BuildKillIndex(newGoals);
        }

        public void Redefine(GameWorld world)
        {
            var newKillIndex = new Dictionary<int, List<(CommunityGoal, GoalRequirement)>>();
            foreach (var goal in this.goals.Values)
            {
                try
                {
                    var script = goal.Script?.Object ?? this.baseScript;
                    var definition = script.Define(goal, world);
                    var errors = CommunityGoalDefinitionParser.Validate(definition, goal.Script is not null);
                    if (errors.Count > 0)
                        throw new CommunityGoalDefinitionException(string.Join("; ", errors));
                    ResolveRewardScripts(definition, world);
                    goal.Definition = definition;
                    goal.ResolvedScript = script;
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed to redefine community goal {goalId}, keeping previous definition", goal.Id);
                }
                this.AddKills(newKillIndex, goal, goal.Definition!);
            }
            this.killIndex = newKillIndex;
        }

        public CommunityGoalRun? CurrentRun(int goalId)
        {
            CommunityGoalRun? best = null;
            foreach (var run in this.runs.Values)
            {
                if (run.GoalId != goalId) continue;
                if (best is null || run.Run > best.Run) best = run;
            }
            return best;
        }

        public IEnumerable<CommunityGoal> GoalsForNpc(int npcTemplateId)
        {
            foreach (var goal in this.goals.Values)
                if (goal.NpcTemplateId == npcTemplateId)
                    yield return goal;
        }

        public int GetFlag(string name)
        {
            return this.flags.TryGetValue(name, out var flag) ? flag.Value : 0;
        }

        public void SetFlag(string name, int value, int? sourceGoalId)
        {
            if (!this.flags.TryGetValue(name, out var flag))
                this.flags[name] = flag = new WorldFlag { Name = name };
            flag.Value = value;
            flag.SourceGoalId = sourceGoalId;
            flag.SetAt = this.UtcNow();
            this.dirtyFlags.Add(name);
        }

        public int AddToFlag(string name, int delta, int? sourceGoalId)
        {
            this.SetFlag(name, this.GetFlag(name) + delta, sourceGoalId);
            return this.GetFlag(name);
        }

        public void Tick(GameWorld world)
        {
            try
            {
                var now = this.UtcNow();
                foreach (var goal in this.goals.Values)
                    this.CheckCompletion(goal, this.CurrentRun(goal.Id), world, now);
                foreach (var goal in this.goals.Values)
                    this.StartPendingRuns(goal, world, now);
                foreach (var goal in this.goals.Values)
                    this.ExpireContributions(goal, now);
            }
            finally
            {
                this.AddTickEvent(world);
            }
        }

        internal void AddTickEvent(GameWorld world)
        {
            var ev = new CommunityGoalTickEvent();
            ev.Ticks += (long)(60 * world.TimerFrequency);
            world.EventHandler.AddEvent(ev);
        }

        private void StartPendingRuns(CommunityGoal goal, GameWorld world, DateTime now)
        {
            var definition = goal.Definition;
            if (definition is null) return;
            var current = this.CurrentRun(goal.Id);
            if (current is null)
            {
                if (!this.PrerequisitesMet(goal))
                    return;
                // A goal whose absolute closesAt has already passed never runs Active; record a
                // terminal run so dependent goals can still see run 1 as Completed/Failed.
                if (definition.ClosesAt is DateTime closesAt && closesAt <= now)
                    this.StartTerminalRun(goal, world, now);
                else
                    this.StartRun(goal, now);
                return;
            }
            if (current.Status == RunStatus.Active || definition.Repeat is null) return;
            if (definition.Repeat.Every is TimeSpan span)
            {
                if (current.StartedAt + span <= now)
                    this.StartRun(goal, now);
            }
            else
            {
                this.StartRun(goal, now);
            }
        }

        private bool PrerequisitesMet(CommunityGoal goal)
        {
            foreach (var prereqId in goal.PrerequisiteGoals)
            {
                if (!this.goals.ContainsKey(prereqId))
                {
                    if (this.missingPrerequisiteGoalIds.Add(goal.Id))
                        log.Warn("Community goal {goalId} has missing prerequisite goal {prereqId}, not started", goal.Id, prereqId);
                    return false;
                }
                if (!this.runs.TryGetValue((prereqId, 1), out var prereqRun) || prereqRun.Status != RunStatus.Completed)
                    return false;
            }
            return true;
        }

        private void StartRun(CommunityGoal goal, DateTime now)
        {
            // A run whose absolute closesAt has already passed would be Active but already
            // expired; never start it. Duration-only goals are unaffected (their duration is
            // relative to their own StartedAt).
            if (goal.Definition?.ClosesAt is DateTime closesAt && closesAt <= now)
                return;
            var run = new CommunityGoalRun
            {
                GoalId = goal.Id,
                Run = (this.CurrentRun(goal.Id)?.Run ?? 0) + 1,
                StartedAt = now,
            };
            this.runs[(run.GoalId, run.Run)] = run;
            this.dirtyRuns.Add((run.GoalId, run.Run));
        }

        private void StartTerminalRun(CommunityGoal goal, GameWorld world, DateTime now)
        {
            var run = new CommunityGoalRun
            {
                GoalId = goal.Id,
                Run = 1,
                StartedAt = now,
            };
            this.runs[(run.GoalId, run.Run)] = run;
            this.dirtyRuns.Add((run.GoalId, run.Run));
            this.CheckCompletion(goal, run, world, now);
        }

        private void RaiseRunCompleted(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
        {
            try
            {
                this.GetScript(goal).OnCompleted(goal, run, world);
            }
            catch (Exception e)
            {
                log.Error(e, "Community goal {goalId} OnCompleted Exception", goal.Id);
            }
            foreach (var subscriber in this.RunCompleted?.GetInvocationList() ?? Array.Empty<Delegate>())
            {
                try
                {
                    ((Action<CommunityGoal, CommunityGoalRun, GameWorld>)subscriber)(goal, run, world);
                }
                catch (Exception e)
                {
                    log.Error(e, "Community goal {goalId} run {run} RunCompleted subscriber Exception", goal.Id, run.Run);
                }
            }
        }

        private void ExpireContributions(CommunityGoal goal, DateTime now)
        {
            var definition = goal.Definition;
            if (definition is null || definition.EffectiveClaimWindow is not TimeSpan window)
                return;
            foreach (var run in this.runs.Values)
            {
                if (run.GoalId != goal.Id || run.CompletedAt is not DateTime completedAt)
                    continue;
                if (completedAt + window > now || run.Contributions.Count == 0)
                    continue;
                foreach (var playerId in run.Contributions.Keys)
                    this.pendingContributionDeletes.Add((goal.Id, run.Run, playerId));
                run.Contributions.Clear();
            }
        }

        public ContributeResult Contribute(CommunityGoal goal, Player? player, string key, long amount, GameWorld world)
        {
            var run = this.CurrentRun(goal.Id);
            if (run is null || run.Status != RunStatus.Active || goal.Definition is null)
                return new ContributeResult(0, "This goal has ended.");
            var script = this.GetScript(goal);
            if (player is not null)
            {
                var refusal = this.CanContribute(goal, run, player, key, amount, world);
                if (refusal is not null)
                    return new ContributeResult(0, refusal);
            }
            var remaining = this.Remaining(goal, key);
            if (remaining < long.MaxValue)
                amount = Math.Min(amount, remaining);
            if (amount <= 0)
                return new ContributeResult(0, "That's no longer needed.");
            run.Totals[key] = run.Totals.GetValueOrDefault(key) + amount;
            if (player is not null)
            {
                if (!run.Contributions.TryGetValue(player.PlayerID, out var contribution))
                {
                    contribution = new Contribution { PlayerId = player.PlayerID };
                    run.Contributions[player.PlayerID] = contribution;
                }
                contribution.Amounts[key] = contribution.Amounts.GetValueOrDefault(key) + amount;
                contribution.LastAt = this.UtcNow();
                this.dirtyContributions.Add((goal.Id, run.Run, player.PlayerID));
            }
            this.dirtyRuns.Add((goal.Id, run.Run));
            try
            {
                script.OnContributed(goal, run, player, key, amount, world);
            }
            catch (Exception e)
            {
                log.Error(e, "Community goal {goalId} OnContributed Exception", goal.Id);
            }
            this.CheckCompletion(goal, run, world, this.UtcNow());
            return new ContributeResult(amount, null);
        }

        internal string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world)
        {
            var script = this.GetScript(goal);
            try
            {
                return script.CanContribute(goal, run, player, key, amount, world);
            }
            catch (Exception e)
            {
                // Fail closed: a throwing gate script refuses rather than admits (mirrors PickupItemEvent).
                log.Error(e, "Community goal {goalId} CanContribute Exception", goal.Id);
                return "You cannot contribute to this goal right now.";
            }
        }

        public void OnNpcKilled(NPC npc, IReadOnlyList<Player> credited, GameWorld world)
        {
            if (!this.killIndex.TryGetValue(npc.NPCTemplateID, out var entries)) return;
            foreach (var (goal, requirement) in entries)
            {
                var run = this.CurrentRun(goal.Id);
                if (run is null || run.Status != RunStatus.Active) continue;
                // Credit players before the total: the total increment can complete the run,
                // which freezes Results from the contributions recorded so far.
                this.Contribute(run, credited, requirement.Key, 1);
                this.Contribute(goal, null, requirement.Key, 1, world);
            }
        }

        // Per-player kill credit: records each credited player's amount only; the run's total
        // is incremented separately (and may complete the run, freezing Results).
        internal ContributeResult Contribute(CommunityGoalRun run, IReadOnlyList<Player> players, string key, long amount)
        {
            if (amount <= 0)
                return new ContributeResult(0, "That's no longer needed.");
            foreach (var player in players)
            {
                if (!run.Contributions.TryGetValue(player.PlayerID, out var contribution))
                {
                    contribution = new Contribution { PlayerId = player.PlayerID };
                    run.Contributions[player.PlayerID] = contribution;
                }
                contribution.Amounts[key] = contribution.Amounts.GetValueOrDefault(key) + amount;
                contribution.LastAt = this.UtcNow();
                this.dirtyContributions.Add((run.GoalId, run.Run, player.PlayerID));
            }
            this.dirtyRuns.Add((run.GoalId, run.Run));
            return new ContributeResult(amount, null);
        }

        public long Remaining(CommunityGoal goal, string key)
        {
            var run = this.CurrentRun(goal.Id);
            if (run is null || run.Status != RunStatus.Active || goal.Definition is null)
                return 0;
            foreach (var requirement in goal.Definition.Requirements)
            {
                if (requirement.Key != key) continue;
                return requirement.Target is null ? long.MaxValue : requirement.Target.Value - run.Totals.GetValueOrDefault(key);
            }
            return 0;
        }

        public void CheckCompletion(CommunityGoal goal, GameWorld world) =>
            this.CheckCompletion(goal, this.CurrentRun(goal.Id), world, this.UtcNow());

        private void CheckCompletion(CommunityGoal goal, CommunityGoalRun? run, GameWorld world, DateTime now)
        {
            if (run is null || run.Status != RunStatus.Active || goal.Definition is null)
                return;
            var definition = goal.Definition;
            var targetsMet = definition.HasTargets && definition.Requirements
                .Where(r => r.Target.HasValue)
                .All(r => run.Totals.GetValueOrDefault(r.Key) >= r.Target!.Value);
            var timedOut = (definition.ClosesAt is DateTime closesAt && closesAt <= now)
                || (definition.Duration is TimeSpan duration && run.StartedAt + duration <= now);
            var scriptDone = false;
            try
            {
                scriptDone = this.GetScript(goal).IsComplete(goal, run, world);
            }
            catch (Exception e)
            {
                log.Error(e, "Community goal {goalId} IsComplete Exception", goal.Id);
            }
            if (!targetsMet && !scriptDone)
            {
                if (!timedOut)
                    return;
                if (definition.HasTargets)
                {
                    run.Status = RunStatus.Failed;
                    run.CompletedAt = now;
                    this.dirtyRuns.Add((goal.Id, run.Run));
                    log.Info("Community goal {goalId} run {run} failed with unmet targets", goal.Id, run.Run);
                    return;
                }
            }
            run.Status = RunStatus.Completed;
            run.CompletedAt = now;
            run.Results = BuildResults(definition, run, world.Random);
            this.dirtyRuns.Add((goal.Id, run.Run));
            this.RaiseRunCompleted(goal, run, world);
            if (definition.Repeat is { Every: null })
                this.StartRun(goal, now);
        }

        private static JsonObject BuildResults(CommunityGoalDefinition definition, CommunityGoalRun run, Random random)
        {
            var places = Ranking.Freeze(definition, run, random);
            var top = new JsonArray();
            foreach (var contribution in run.Contributions.Values
                         .Where(c => Ranking.Score(definition, c) > 0)
                         .OrderByDescending(c => Ranking.Score(definition, c))
                         .ThenBy(c => c.LastAt)
                         .ThenBy(c => c.PlayerId)
                         .Take(10))
            {
                top.Add(new JsonObject
                {
                    ["player"] = contribution.PlayerId,
                    ["score"] = Ranking.Score(definition, contribution),
                });
            }
            var totals = new JsonObject();
            foreach (var (key, value) in run.Totals)
                totals[key] = value;
            return new JsonObject
            {
                ["places"] = new JsonArray(places.Select(id => (JsonNode)id).ToArray()),
                ["top"] = top,
                ["totals"] = totals,
            };
        }

        internal bool IsRunDirty(int goalId, int run) => this.dirtyRuns.Contains((goalId, run));
        internal bool IsContributionDirty(int goalId, int run, int playerId) => this.dirtyContributions.Contains((goalId, run, playerId));
        internal void MarkFlagDirty(string name) => this.dirtyFlags.Add(name);

        public void Save(GameWorld world)
        {
            try
            {
                var plan = PlanSave();
                if (plan.IsEmpty) return;
                world.Database.EnqueueTransaction(
                    conn => ExecutePlan(conn, plan),
                    onSettled: ex =>
                    {
                        world.EnqueueCompletion(() =>
                        {
                            this.inFlightPlans.Remove(plan);
                            if (ex is not null)
                                ReDirty(plan);
                        });
                    });
                this.inFlightPlans.Add(plan);
            }
            finally
            {
                this.AddSaveEvent(world);
            }
        }

        public void SaveSync(GameWorld world)
        {
            // A failed in-flight save's re-dirty may have been dropped by a shutdown in
            // progress, so re-dirty in-flight plans explicitly or SaveSync would no-op
            // and lose their data.
            if (this.inFlightPlans.Count > 0)
            {
                foreach (var inFlight in this.inFlightPlans)
                    ReDirty(inFlight);
                this.inFlightPlans.Clear();
            }
            var plan = PlanSave();
            if (plan.IsEmpty) return;
            world.Database.ExecuteTransaction(conn => ExecutePlan(conn, plan));
        }

        internal void AddSaveEvent(GameWorld world)
        {
            var ev = new CommunityGoalSaveEvent();
            ev.Ticks += (long)(Math.Max(1, world.Settings.WorldSavePeriod) * world.TimerFrequency);
            world.EventHandler.AddEvent(ev);
        }

        internal SavePlan PlanSave()
        {
            var runs = new List<RunRow>();
            foreach (var key in this.dirtyRuns)
            {
                if (!this.runs.TryGetValue(key, out var run)) continue;
                runs.Add(new RunRow(
                    run.GoalId, run.Run, run.Status,
                    run.StartedAt.ToString("o", CultureInfo.InvariantCulture),
                    run.CompletedAt?.ToString("o", CultureInfo.InvariantCulture),
                    JsonHelper.Serialize(run.Totals),
                    run.Results is null ? null : JsonHelper.Serialize(run.Results)));
            }

            var contributions = new List<ContributionRow>();
            foreach (var (goalId, run, playerId) in this.dirtyContributions)
            {
                if (!this.runs.TryGetValue((goalId, run), out var runObj)
                    || !runObj.Contributions.TryGetValue(playerId, out var contribution))
                    continue;
                contributions.Add(new ContributionRow(
                    goalId, run, playerId,
                    JsonHelper.Serialize(contribution.Amounts),
                    contribution.LastAt.ToString("o", CultureInfo.InvariantCulture),
                    contribution.ClaimedBracket,
                    contribution.ClaimedAt?.ToString("o", CultureInfo.InvariantCulture)));
            }

            var flags = new List<FlagRow>();
            foreach (var name in this.dirtyFlags)
            {
                if (!this.flags.TryGetValue(name, out var flag)) continue;
                flags.Add(new FlagRow(
                    flag.Name, flag.Value,
                    flag.SetAt.ToString("o", CultureInfo.InvariantCulture),
                    flag.SourceGoalId));
            }

            var deletes = this.pendingContributionDeletes.ToList();

            this.dirtyRuns.Clear();
            this.dirtyContributions.Clear();
            this.pendingContributionDeletes.Clear();
            this.dirtyFlags.Clear();

            return new SavePlan(runs, contributions, flags, deletes);
        }

        private void ReDirty(SavePlan plan)
        {
            foreach (var row in plan.Runs)
                this.dirtyRuns.Add((row.GoalId, row.Run));
            foreach (var row in plan.Contributions)
                this.dirtyContributions.Add((row.GoalId, row.Run, row.PlayerId));
            foreach (var (goalId, run, playerId) in plan.Deletes)
                this.pendingContributionDeletes.Add((goalId, run, playerId));
            foreach (var row in plan.Flags)
                this.dirtyFlags.Add(row.Name);
        }

        private static void ExecutePlan(SQLiteConnection conn, SavePlan plan)
        {
            // Runs on the DB thread; executes only the captured plan, never live state.
            foreach (var (goalId, run, playerId) in plan.Deletes)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM community_goal_contributions WHERE goal_id=@g AND run=@r AND player_id=@p";
                cmd.Parameters.AddWithValue("@g", goalId);
                cmd.Parameters.AddWithValue("@r", run);
                cmd.Parameters.AddWithValue("@p", playerId);
                cmd.ExecuteNonQuery();
            }
            foreach (var row in plan.Runs)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO community_goal_runs (goal_id, run, status, started_at, completed_at, totals, results) " +
                    "VALUES (@g, @r, @status, @start, @end, @totals, @results) " +
                    "ON CONFLICT(goal_id, run) DO UPDATE SET status=@status, started_at=@start, completed_at=@end, totals=@totals, results=@results";
                cmd.Parameters.AddWithValue("@g", row.GoalId);
                cmd.Parameters.AddWithValue("@r", row.Run);
                cmd.Parameters.AddWithValue("@status", row.Status.ToString());
                cmd.Parameters.AddWithValue("@start", row.StartedAt);
                cmd.Parameters.AddWithValue("@end", (object?)row.CompletedAt ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@totals", row.TotalsJson);
                cmd.Parameters.AddWithValue("@results", (object?)row.ResultsJson ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            foreach (var row in plan.Contributions)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO community_goal_contributions (goal_id, run, player_id, amounts, last_at, claimed_bracket, claimed_at) " +
                    "VALUES (@g, @r, @p, @amounts, @last, @bracket, @claimed) " +
                    "ON CONFLICT(goal_id, run, player_id) DO UPDATE SET amounts=@amounts, last_at=@last, claimed_bracket=@bracket, claimed_at=@claimed";
                cmd.Parameters.AddWithValue("@g", row.GoalId);
                cmd.Parameters.AddWithValue("@r", row.Run);
                cmd.Parameters.AddWithValue("@p", row.PlayerId);
                cmd.Parameters.AddWithValue("@amounts", row.AmountsJson);
                cmd.Parameters.AddWithValue("@last", row.LastAt);
                cmd.Parameters.AddWithValue("@bracket", (object?)row.ClaimedBracket ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@claimed", (object?)row.ClaimedAt ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            foreach (var row in plan.Flags)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO world_flags (name, value, set_at, source_goal_id) " +
                    "VALUES (@name, @value, @set, @goal) " +
                    "ON CONFLICT(name) DO UPDATE SET value=@value, set_at=@set, source_goal_id=@goal";
                cmd.Parameters.AddWithValue("@name", row.Name);
                cmd.Parameters.AddWithValue("@value", row.Value);
                cmd.Parameters.AddWithValue("@set", row.SetAt);
                cmd.Parameters.AddWithValue("@goal", (object?)row.SourceGoalId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        internal sealed record SavePlan(
            List<RunRow> Runs,
            List<ContributionRow> Contributions,
            List<FlagRow> Flags,
            List<(int GoalId, int Run, int PlayerId)> Deletes)
        {
            public bool IsEmpty => Runs.Count == 0 && Contributions.Count == 0 && Flags.Count == 0 && Deletes.Count == 0;
        }

        internal sealed record RunRow(int GoalId, int Run, RunStatus Status, string StartedAt,
                                      string? CompletedAt, string TotalsJson, string? ResultsJson);

        internal sealed record ContributionRow(int GoalId, int Run, int PlayerId, string AmountsJson,
                                               string LastAt, int? ClaimedBracket, string? ClaimedAt);

        internal sealed record FlagRow(string Name, int Value, string SetAt, int? SourceGoalId);

        internal void LoadStateRows(IEnumerable<CommunityGoalRun> runs,
                                   IEnumerable<(int GoalId, int Run, Contribution C)> contributions,
                                   IEnumerable<WorldFlag> flags)
        {
            this.orphanGoalIds.Clear();

            var newRuns = new Dictionary<(int, int), CommunityGoalRun>();
            foreach (var run in runs)
            {
                newRuns[(run.GoalId, run.Run)] = run;
                this.LogOrphan(run.GoalId);
            }

            foreach (var (goalId, run, contribution) in contributions)
            {
                this.LogOrphan(goalId);
                if (!newRuns.TryGetValue((goalId, run), out var existing))
                {
                    existing = new CommunityGoalRun
                    {
                        GoalId = goalId,
                        Run = run,
                        StartedAt = contribution.LastAt,
                    };
                    newRuns[(goalId, run)] = existing;
                }
                existing.Contributions[contribution.PlayerId] = contribution;
            }

            this.runs = newRuns;
            this.flags = flags.ToDictionary(f => f.Name);
        }

        private ICommunityGoalScript GetScript(CommunityGoal goal)
        {
            return goal.ResolvedScript ?? this.baseScript;
        }

        internal string GetSummaryText(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
        {
            try
            {
                return this.GetScript(goal).GetSummaryText(goal, run, world);
            }
            catch (Exception e)
            {
                log.Error(e, "Community goal {goalId} GetSummaryText Exception", goal.Id);
                return this.baseScript.GetSummaryText(goal, run, world);
            }
        }

        public int? BracketIndex(CommunityGoal goal, CommunityGoalRun run, Player player)
        {
            // The claim flow (the only caller) has no world; the base GetBracket ignores it.
            var bracket = this.GetScript(goal).GetBracket(goal, run, player, null!);
            var brackets = goal.Definition?.Brackets;
            if (bracket is null || brackets is null) return null;
            for (var i = 0; i < brackets.Count; i++)
                if (ReferenceEquals(brackets[i], bracket)) return i;
            return null;
        }

        public IReadOnlyList<PendingPrize> PendingPrizes(Player player, int npcTemplateId)
        {
            var prizes = new List<PendingPrize>();
            foreach (var goal in this.GoalsForNpc(npcTemplateId))
            {
                foreach (var run in this.runs.Values)
                {
                    if (run.GoalId != goal.Id || run.Status != RunStatus.Completed)
                        continue;
                    if (!run.Contributions.TryGetValue(player.PlayerID, out var contribution))
                        continue;
                    if (contribution.ClaimedBracket is not null || this.IsClaimExpired(goal, run))
                        continue;
                    int? bracketIndex;
                    try
                    {
                        bracketIndex = this.BracketIndex(goal, run, player);
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "Community goal {goalId} GetBracket Exception", goal.Id);
                        continue;
                    }
                    if (bracketIndex is not int index)
                        continue;
                    prizes.Add(new PendingPrize(goal, run, index));
                }
            }
            return prizes;
        }

        public ClaimResult Claim(CommunityGoal goal, CommunityGoalRun run, Player player, NPC npc, GameWorld world)
        {
            if (run is null || run.GoalId != goal.Id || run.Status != RunStatus.Completed)
                return ClaimResult.Refuse("This goal hasn't finished yet.");
            if (!run.Contributions.TryGetValue(player.PlayerID, out var contribution))
                return ClaimResult.Refuse("You have nothing to claim.");
            int bracketIndex;
            try
            {
                if (this.BracketIndex(goal, run, player) is not int index)
                    return ClaimResult.Refuse("You have nothing to claim.");
                bracketIndex = index;
            }
            catch (Exception e)
            {
                log.Error(e, "Community goal {goalId} GetBracket Exception", goal.Id);
                return ClaimResult.Refuse("You cannot claim this reward right now.");
            }
            if (contribution.ClaimedBracket is not null)
                return ClaimResult.Refuse("You've already claimed this reward.");
            if (!this.goals.ContainsKey(goal.Id) || this.IsClaimExpired(goal, run))
                return ClaimResult.Refuse("This reward has expired.");
            var bracket = goal.Definition!.Brackets[bracketIndex];
            var ctx = new CommunityRewardContext
            {
                Goal = goal,
                Run = run,
                Player = player,
                Npc = npc,
                Bracket = bracket,
                World = world,
            };
            string? refusal;
            try
            {
                refusal = CommunityRewardRegistry.CanGive(bracket.Rewards, ctx);
            }
            catch (Exception e)
            {
                // Fail closed: a throwing gate script refuses rather than admits (mirrors CanContribute).
                log.Error(e, "Community goal {goalId} CanGive Exception", goal.Id);
                return ClaimResult.Refuse("You cannot claim this reward right now.");
            }
            if (refusal is not null)
                return ClaimResult.Refuse(refusal);
            int required;
            try
            {
                required = CommunityRewardRegistry.RequiredSlots(bracket.Rewards, ctx);
            }
            catch (Exception e)
            {
                log.Error(e, "Community goal {goalId} RequiredSlots Exception", goal.Id);
                return ClaimResult.Refuse("You cannot claim this reward right now.");
            }
            if (player.Inventory.GetNumberOfFreeSlots() < required)
                return ClaimResult.Refuse($"You need {required} free inventory slots to claim this.");
            contribution.ClaimedBracket = bracketIndex;
            contribution.ClaimedAt = this.UtcNow();
            this.dirtyContributions.Add((goal.Id, run.Run, player.PlayerID));
            try
            {
                CommunityRewardRegistry.Give(bracket.Rewards, ctx);
            }
            catch (Exception e)
            {
                // The claim is recorded before Give so a throwing reward cannot be re-claimed
                // and re-grant a partial bracket.
                log.Error(e, "Community goal {goalId} Give Exception", goal.Id);
                return ClaimResult.Refuse("You cannot claim this reward right now.");
            }
            return ClaimResult.Ok();
        }

        private bool IsClaimExpired(CommunityGoal goal, CommunityGoalRun run)
        {
            return goal.Definition?.EffectiveClaimWindow is TimeSpan window
                && run.CompletedAt is DateTime completedAt
                && completedAt + window < this.UtcNow();
        }

        private void AnnounceCompletion(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
        {
            if (run.Status != RunStatus.Completed) return;
            var message = $"[Community] {goal.Name} has been completed!";
            if (goal.Definition?.Brackets.Any(b => b.Rank.HasValue) ?? false)
            {
                var places = run.Results?["places"] as JsonArray;
                if (places is { Count: > 0 } && places[0] is JsonValue first)
                    message += " Winner: " + (world.PlayerHandler.GetPlayerName(first.GetValue<int>()) ?? "Unknown");
                else if (places is null || places.Count == 0)
                    message += " No winners.";
            }
            world.SendToAll(P.ServerMessage(message));
        }

        private void GiveCommunityRewards(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
        {
            if (goal.Definition is null) return;
            CommunityRewardRegistry.Give(goal.Definition.CommunityRewards, new CommunityRewardContext
            {
                Goal = goal,
                Run = run,
                World = world,
            });
        }

        // A missing or broken reward script skips the whole goal, same rule as goal scripts.
        private static void ResolveRewardScripts(CommunityGoalDefinition definition, GameWorld world)
        {
            foreach (var bracket in definition.Brackets)
                foreach (var reward in bracket.Rewards)
                    ResolveRewardScript(reward, world);
            foreach (var reward in definition.CommunityRewards)
                ResolveRewardScript(reward, world);
        }

        private static void ResolveRewardScript(RewardSpec reward, GameWorld world)
        {
            if (reward.Type != "Script") return;
            var path = reward.Params["script"] is JsonValue value && value.GetValueKind() == JsonValueKind.String
                ? value.GetValue<string>()!
                : "";
            if (path.Length == 0)
                throw new CommunityGoalDefinitionException("reward of type Script is missing 'script'");
            world.ScriptHandler.GetScript<ICommunityRewardScript>(path);
        }

        private void LogOrphan(int goalId)
        {
            if (!this.goals.ContainsKey(goalId) && this.orphanGoalIds.Add(goalId))
                log.Warn("Community goal state found for unknown goal {goalId}", goalId);
        }

        private Dictionary<int, List<(CommunityGoal, GoalRequirement)>> BuildKillIndex(
            IReadOnlyDictionary<int, CommunityGoal> goals)
        {
            var index = new Dictionary<int, List<(CommunityGoal, GoalRequirement)>>();
            foreach (var goal in goals.Values)
                this.AddKills(index, goal, goal.Definition!);
            return index;
        }

        private void AddKills(Dictionary<int, List<(CommunityGoal, GoalRequirement)>> index,
                              CommunityGoal goal, CommunityGoalDefinition definition)
        {
            foreach (var requirement in definition.Requirements)
            {
                if (requirement.Kind != RequirementKind.Kill || requirement.NpcTemplateId is not int npcId)
                    continue;
                if (!index.TryGetValue(npcId, out var list))
                    index[npcId] = list = new List<(CommunityGoal, GoalRequirement)>();
                list.Add((goal, requirement));
            }
        }

        private static List<int> ParsePrerequisites(string csv)
        {
            var ids = new List<int>();
            foreach (var part in csv.Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.Length == 0) continue;
                if (!int.TryParse(trimmed, out var id))
                    throw new FormatException($"invalid prerequisite goal id '{trimmed}'");
                ids.Add(id);
            }
            return ids;
        }

        private static List<CommunityGoalRun> ReadRuns(SQLiteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT goal_id, run, status, started_at, completed_at, totals, results FROM community_goal_runs";
            using var reader = cmd.ExecuteReader();
            var rows = new List<CommunityGoalRun>();
            while (reader.Read())
            {
                var run = new CommunityGoalRun
                {
                    GoalId = reader.GetInt32(0),
                    Run = reader.GetInt32(1),
                    Status = ParseStatus(reader.GetString(2)),
                    StartedAt = ParseTime(reader.GetString(3)),
                    CompletedAt = reader.IsDBNull(4) ? null : ParseTime(reader.GetString(4)),
                    Results = reader.IsDBNull(6) ? null : JsonNode.Parse(reader.GetString(6)) as JsonObject,
                };
                CopyInto(run.Totals, JsonHelper.Deserialize<Dictionary<string, long>>(reader.GetString(5)) ?? new());
                rows.Add(run);
            }
            return rows;
        }

        private static List<(int GoalId, int Run, Contribution C)> ReadContributions(SQLiteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT goal_id, run, player_id, amounts, last_at, claimed_bracket, claimed_at FROM community_goal_contributions";
            using var reader = cmd.ExecuteReader();
            var rows = new List<(int, int, Contribution)>();
            while (reader.Read())
            {
                var contribution = new Contribution
                {
                    PlayerId = reader.GetInt32(2),
                    LastAt = ParseTime(reader.GetString(4)),
                    ClaimedBracket = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ClaimedAt = reader.IsDBNull(6) ? null : ParseTime(reader.GetString(6)),
                };
                CopyInto(contribution.Amounts, JsonHelper.Deserialize<Dictionary<string, long>>(reader.GetString(3)) ?? new());
                rows.Add((reader.GetInt32(0), reader.GetInt32(1), contribution));
            }
            return rows;
        }

        private static List<WorldFlag> ReadFlags(SQLiteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name, value, set_at, source_goal_id FROM world_flags";
            using var reader = cmd.ExecuteReader();
            var rows = new List<WorldFlag>();
            while (reader.Read())
            {
                rows.Add(new WorldFlag
                {
                    Name = reader.GetString(0),
                    Value = reader.GetInt32(1),
                    SetAt = ParseTime(reader.GetString(2)),
                    SourceGoalId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                });
            }
            return rows;
        }

        private static void CopyInto(Dictionary<string, long> target, Dictionary<string, long> source)
        {
            foreach (var (key, value) in source)
                target[key] = value;
        }

        private static RunStatus ParseStatus(string status)
        {
            if (Enum.TryParse<RunStatus>(status, true, out var parsed))
                return parsed;
            throw new FormatException($"unknown community goal run status '{status}'");
        }

        private static DateTime ParseTime(string value)
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}
