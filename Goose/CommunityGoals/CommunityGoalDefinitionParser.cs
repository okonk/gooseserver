using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Goose.CommunityGoals
{
    public static class CommunityGoalDefinitionParser
    {
        public static CommunityGoalDefinition Parse(string json)
        {
            JsonNode? node;
            try
            {
                RejectDuplicateProperties(json);
                node = JsonNode.Parse(json);
            }
            catch (JsonException e)
            {
                throw new CommunityGoalDefinitionException($"invalid JSON: {e.Message}", e);
            }
            if (node is not JsonObject root)
                throw new CommunityGoalDefinitionException("definition must be a JSON object");

            var requirements = new List<GoalRequirement>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            if (root.TryGetPropertyValue("requirements", out var reqNode) && reqNode is not null)
            {
                if (reqNode is not JsonArray reqs)
                    throw new CommunityGoalDefinitionException("'requirements' must be an array");
                foreach (var entry in reqs)
                {
                    if (entry is not JsonObject req)
                        throw new CommunityGoalDefinitionException("requirement entries must be objects");
                    if (!req.TryGetPropertyValue("key", out var keyNode) || keyNode is not JsonValue keyVal || keyVal.GetValueKind() != JsonValueKind.String)
                        throw new CommunityGoalDefinitionException("requirement is missing a string 'key'");
                    var key = keyVal.GetValue<string>()!;
                    if (!seenKeys.Add(key))
                        throw new CommunityGoalDefinitionException($"duplicate requirement key '{key}'");
                    if (!req.TryGetPropertyValue("type", out var typeNode) || typeNode is not JsonValue typeVal || typeVal.GetValueKind() != JsonValueKind.String)
                        throw new CommunityGoalDefinitionException($"requirement '{key}' is missing a string 'type'");
                    var typeName = typeVal.GetValue<string>()!;
                    var kind = typeName switch
                    {
                        "Item" => RequirementKind.Item,
                        "Gold" => RequirementKind.Gold,
                        "Experience" => RequirementKind.Experience,
                        "Kill" => RequirementKind.Kill,
                        "Custom" => RequirementKind.Custom,
                        _ => throw new CommunityGoalDefinitionException($"unknown requirement type '{typeName}'")
                    };
                    requirements.Add(new GoalRequirement
                    {
                        Key = key,
                        Kind = kind,
                        Target = GetLong(req, "target"),
                        ItemTemplateId = GetInt(req, "item"),
                        NpcTemplateId = GetInt(req, "npc"),
                        Label = GetString(req, "label")
                    });
                }
            }

            var scoreWeights = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var req in requirements)
                scoreWeights[req.Key] = 1;
            if (root.TryGetPropertyValue("score", out var scoreNode) && scoreNode is not null)
            {
                if (scoreNode is not JsonObject score)
                    throw new CommunityGoalDefinitionException("'score' must be an object");
                foreach (var (key, value) in score)
                    scoreWeights[key] = GetDouble(value, $"score entry '{key}'");
            }

            var ranking = RankingKind.Score;
            if (root.TryGetPropertyValue("ranking", out var rankingNode) && rankingNode is not null)
            {
                if (rankingNode is not JsonValue rankingVal || rankingVal.GetValueKind() != JsonValueKind.String)
                    throw new CommunityGoalDefinitionException("'ranking' must be a string");
                var rankingText = rankingVal.GetValue<string>()!;
                ranking = rankingText switch
                {
                    "score" => RankingKind.Score,
                    "lottery" => RankingKind.Lottery,
                    _ => throw new CommunityGoalDefinitionException($"unknown ranking '{rankingText}'")
                };
            }

            Dictionary<string, long>? entryCost = null;
            if (root.TryGetPropertyValue("entryCost", out var entryCostNode) && entryCostNode is not null)
            {
                if (entryCostNode is not JsonObject entryCostObj)
                    throw new CommunityGoalDefinitionException("'entryCost' must be an object");
                entryCost = new Dictionary<string, long>(StringComparer.Ordinal);
                foreach (var (key, value) in entryCostObj)
                    entryCost[key] = AsLong(value, $"entryCost entry '{key}'");
            }

            var duration = GetDuration(root, "duration");
            var claimWindow = GetDuration(root, "claimWindow");

            DateTime? closesAt = null;
            if (root.TryGetPropertyValue("closesAt", out var closesAtNode) && closesAtNode is not null)
            {
                if (closesAtNode is not JsonValue closesAtVal || closesAtVal.GetValueKind() != JsonValueKind.String)
                    throw new CommunityGoalDefinitionException("'closesAt' must be an ISO-8601 date-time string");
                var closesAtText = closesAtVal.GetValue<string>()!;
                try
                {
                    closesAt = DateTime.Parse(closesAtText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                }
                catch (FormatException e)
                {
                    throw new CommunityGoalDefinitionException($"invalid closesAt '{closesAtText}'", e);
                }
            }

            RepeatRule? repeat = null;
            if (root.TryGetPropertyValue("repeat", out var repeatNode) && repeatNode is not null)
            {
                if (repeatNode is JsonValue repeatVal && repeatVal.GetValueKind() == JsonValueKind.String)
                {
                    var repeatText = repeatVal.GetValue<string>()!;
                    if (repeatText != "onClose")
                        throw new CommunityGoalDefinitionException($"invalid repeat '{repeatText}'");
                    repeat = Repeat.OnClose;
                }
                else if (repeatNode is JsonObject repeatObj)
                {
                    if (!repeatObj.TryGetPropertyValue("every", out var everyNode) || everyNode is not JsonValue everyVal || everyVal.GetValueKind() != JsonValueKind.String)
                        throw new CommunityGoalDefinitionException("repeat object requires a string 'every'");
                    repeat = Repeat.Every(ParseDuration(everyVal.GetValue<string>()!));
                }
                else
                {
                    throw new CommunityGoalDefinitionException("repeat must be \"onClose\" or { \"every\": <duration> }");
                }
            }

            var brackets = new List<GoalBracket>();
            if (root.TryGetPropertyValue("brackets", out var bracketsNode) && bracketsNode is not null)
            {
                if (bracketsNode is not JsonArray bracketsArr)
                    throw new CommunityGoalDefinitionException("'brackets' must be an array");
                foreach (var entry in bracketsArr)
                {
                    if (entry is not JsonObject bracket)
                        throw new CommunityGoalDefinitionException("bracket entries must be objects");
                    int? rank = null;
                    if (bracket.TryGetPropertyValue("rank", out var rankNode) && rankNode is not null)
                    {
                        var rankValue = AsLong(rankNode, "bracket 'rank'");
                        if (rankValue is < int.MinValue or > int.MaxValue)
                            throw new CommunityGoalDefinitionException("bracket 'rank' must be an integer");
                        rank = (int)rankValue;
                    }
                    double? minScore = null;
                    if (bracket.TryGetPropertyValue("minScore", out var minScoreNode) && minScoreNode is not null)
                        minScore = GetDouble(minScoreNode, "bracket 'minScore'");
                    brackets.Add(new GoalBracket
                    {
                        Rank = rank,
                        MinScore = minScore,
                        Measure = GetString(bracket, "measure"),
                        Rewards = ParseRewards(bracket, "bracket")
                    });
                }
            }

            var communityRewards = new List<RewardSpec>();
            if (root.TryGetPropertyValue("communityRewards", out var communityRewardsNode) && communityRewardsNode is not null)
            {
                if (communityRewardsNode is not JsonArray communityRewardsArr)
                    throw new CommunityGoalDefinitionException("'communityRewards' must be an array");
                foreach (var entry in communityRewardsArr)
                    communityRewards.Add(ParseReward(entry, "communityRewards"));
            }

            return new CommunityGoalDefinition
            {
                Requirements = requirements,
                ScoreWeights = scoreWeights,
                Ranking = ranking,
                EntryCost = entryCost,
                Duration = duration,
                ClosesAt = closesAt,
                Repeat = repeat,
                ClaimWindow = claimWindow,
                Brackets = brackets,
                CommunityRewards = communityRewards
            };
        }

        // JsonNode.Parse keeps duplicate property names and only throws a raw ArgumentException
        // when the object is first enumerated, which would escape the goal-load catch in Task 5.
        private static void RejectDuplicateProperties(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            var scopes = new Stack<HashSet<string>>();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        scopes.Push(new HashSet<string>(StringComparer.Ordinal));
                        break;
                    case JsonTokenType.PropertyName:
                        var name = reader.GetString()!;
                        if (!scopes.Peek().Add(name))
                            throw new CommunityGoalDefinitionException($"duplicate property '{name}' in JSON object");
                        break;
                    case JsonTokenType.EndObject:
                        scopes.Pop();
                        break;
                }
            }
        }

        public static IReadOnlyList<string> Validate(CommunityGoalDefinition def, bool customScript)
        {
            var errors = new List<string>();
            var keys = def.Requirements.Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var req in def.Requirements)
            {
                if (!Enum.IsDefined(typeof(RequirementKind), req.Kind))
                    errors.Add($"unknown requirement kind '{req.Kind}'");
                if (!seenKeys.Add(req.Key))
                    errors.Add($"duplicate requirement key '{req.Key}'");
                if (req.Kind == RequirementKind.Item && req.ItemTemplateId is null)
                    errors.Add($"requirement '{req.Key}' of type Item is missing 'item'");
                if (req.Kind == RequirementKind.Kill && req.NpcTemplateId is null)
                    errors.Add($"requirement '{req.Key}' of type Kill is missing 'npc'");
            }

            foreach (var key in def.ScoreWeights.Keys.Where(k => !keys.Contains(k)))
                errors.Add($"score references unknown requirement key '{key}'");

            for (var i = 0; i < def.Brackets.Count; i++)
            {
                var bracket = def.Brackets[i];
                if (bracket.Rank is null && bracket.MinScore is null)
                    errors.Add($"bracket {i} has neither 'rank' nor 'minScore'");
                if (bracket.Rank is not null && bracket.MinScore is not null)
                    errors.Add($"bracket {i} has both 'rank' and 'minScore'");
                if (bracket.Measure is not null && !keys.Contains(bracket.Measure))
                    errors.Add($"bracket {i} measure references unknown requirement key '{bracket.Measure}'");
            }

            if (def.Ranking == RankingKind.Lottery)
            {
                if (def.EntryCost is null)
                    errors.Add("lottery ranking requires 'entryCost'");
                else
                {
                    foreach (var key in def.EntryCost.Keys.Where(k => !keys.Contains(k)))
                        errors.Add($"entryCost references unknown requirement key '{key}'");
                }
            }

            if (!def.HasTargets && def.Duration is null && def.ClosesAt is null && !customScript)
                errors.Add("no termination: the goal has no targets and no duration or closesAt");

            return errors;
        }

        private static long? GetLong(JsonObject obj, string field)
        {
            if (!obj.TryGetPropertyValue(field, out var node) || node is null)
                return null;
            return AsLong(node, $"'{field}'");
        }

        private static int? GetInt(JsonObject obj, string field)
        {
            if (!obj.TryGetPropertyValue(field, out var node) || node is null)
                return null;
            var value = AsLong(node, $"'{field}'");
            if (value is < int.MinValue or > int.MaxValue)
                throw new CommunityGoalDefinitionException($"'{field}' must be an integer");
            return (int)value;
        }

        private static long AsLong(JsonNode? node, string context)
        {
            if (node is null)
                throw new CommunityGoalDefinitionException($"{context} must be an integer");
            try
            {
                return node.GetValue<long>();
            }
            catch (Exception e) when (e is InvalidOperationException or FormatException)
            {
                throw new CommunityGoalDefinitionException($"{context} must be an integer");
            }
        }

        private static string? GetString(JsonObject obj, string field)
        {
            if (!obj.TryGetPropertyValue(field, out var node) || node is null)
                return null;
            if (node is not JsonValue value || value.GetValueKind() != JsonValueKind.String)
                throw new CommunityGoalDefinitionException($"'{field}' must be a string");
            return value.GetValue<string>();
        }

        private static double GetDouble(JsonNode? node, string context)
        {
            if (node is null)
                throw new CommunityGoalDefinitionException($"{context} must be a number");
            try
            {
                return node.GetValue<double>();
            }
            catch (Exception e) when (e is InvalidOperationException or FormatException)
            {
                throw new CommunityGoalDefinitionException($"{context} must be a number");
            }
        }

        private static TimeSpan? GetDuration(JsonObject root, string field)
        {
            if (!root.TryGetPropertyValue(field, out var node) || node is null)
                return null;
            if (node is not JsonValue value || value.GetValueKind() != JsonValueKind.String)
                throw new CommunityGoalDefinitionException($"'{field}' must be a duration string");
            return ParseDuration(value.GetValue<string>()!);
        }

        private static TimeSpan ParseDuration(string value)
        {
            if (value.Length < 2)
                throw new CommunityGoalDefinitionException($"invalid duration '{value}'");
            if (!int.TryParse(value.AsSpan(0, value.Length - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
                throw new CommunityGoalDefinitionException($"invalid duration '{value}'");
            return value[^1] switch
            {
                'd' => TimeSpan.FromDays(amount),
                'h' => TimeSpan.FromHours(amount),
                'm' => TimeSpan.FromMinutes(amount),
                _ => throw new CommunityGoalDefinitionException($"invalid duration '{value}'")
            };
        }

        private static IReadOnlyList<RewardSpec> ParseRewards(JsonObject parent, string context)
        {
            var rewards = new List<RewardSpec>();
            if (!parent.TryGetPropertyValue("rewards", out var node) || node is null)
                return rewards;
            if (node is not JsonArray arr)
                throw new CommunityGoalDefinitionException($"{context} 'rewards' must be an array");
            foreach (var entry in arr)
                rewards.Add(ParseReward(entry, context));
            return rewards;
        }

        private static RewardSpec ParseReward(JsonNode? entry, string context)
        {
            if (entry is not JsonObject obj)
                throw new CommunityGoalDefinitionException($"{context} reward entries must be objects");
            if (!obj.TryGetPropertyValue("type", out var typeNode) || typeNode is not JsonValue typeVal || typeVal.GetValueKind() != JsonValueKind.String)
                throw new CommunityGoalDefinitionException($"{context} reward is missing a string 'type'");
            return new RewardSpec
            {
                Type = typeVal.GetValue<string>()!,
                Params = (JsonObject)obj
            };
        }
    }
}
