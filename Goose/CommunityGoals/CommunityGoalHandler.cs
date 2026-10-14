using System.Data.SQLite;
using System.Globalization;
using System.Text.Json.Nodes;
using Goose.Scripting;

namespace Goose.CommunityGoals
{
    public class CommunityGoalHandler
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly BaseCommunityGoalScript baseScript = new();
        private readonly HashSet<int> orphanGoalIds = new();
        private Dictionary<int, CommunityGoal> goals = new();
        private Dictionary<int, List<(CommunityGoal, GoalRequirement)>> killIndex = new();
        private Dictionary<(int GoalId, int Run), CommunityGoalRun> runs = new();
        private Dictionary<string, WorldFlag> flags = new();

        public IReadOnlyDictionary<int, CommunityGoal> Goals => this.goals;
        internal IReadOnlyDictionary<int, List<(CommunityGoal, GoalRequirement)>> KillIndex => this.killIndex;
        internal Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        public void Load(GameWorld world)
        {
            this.LoadDefinitions(world);

            var (runRows, contributionRows, flagRows) = world.Database.Execute(conn =>
                (ReadRuns(conn), ReadContributions(conn), ReadFlags(conn)));
            this.LoadStateRows(runRows, contributionRows, flagRows);
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
                        ScriptPath = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        PropertiesJson = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    };
                    goal.PrerequisiteGoals.AddRange(ParsePrerequisites(reader.IsDBNull(4) ? "" : reader.GetString(4)));
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
                    row.Script = string.IsNullOrEmpty(row.ScriptPath)
                        ? null
                        : world.ScriptHandler.GetScript<ICommunityGoalScript>(row.ScriptPath);
                    var definition = this.GetScript(row).Define(row, world);
                    var errors = CommunityGoalDefinitionParser.Validate(definition, row.Script is not null);
                    if (errors.Count > 0)
                        throw new CommunityGoalDefinitionException(string.Join("; ", errors));
                    row.Definition = definition;
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
                    var definition = this.GetScript(goal).Define(goal, world);
                    var errors = CommunityGoalDefinitionParser.Validate(definition, goal.Script is not null);
                    if (errors.Count > 0)
                        throw new CommunityGoalDefinitionException(string.Join("; ", errors));
                    goal.Definition = definition;
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
            return goal.Script?.Object ?? this.baseScript;
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
