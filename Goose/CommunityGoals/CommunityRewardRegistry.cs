using System.Text.Json;
using System.Text.Json.Nodes;
using Goose.Quests;
using Goose.Scripting;

namespace Goose.CommunityGoals
{
    public static class CommunityRewardRegistry
    {
        private static readonly HashSet<RewardType> StringValueTypes =
        [
            RewardType.Title, RewardType.Surname, RewardType.Teleport,
            RewardType.HairColour, RewardType.BodyColour,
        ];

        public static IReadOnlyList<string> Validate(RewardSpec spec, bool community)
        {
            var errors = new List<string>();
            if (spec.Type == nameof(RewardType.Script))
            {
                if (!IsNonEmptyString(spec.Params, "script"))
                    errors.Add("reward 'Script' is missing a string 'script'");
                return errors;
            }

            if (community)
            {
                switch (spec.Type)
                {
                    case "ChestPages":
                        if (!IsInt(spec.Params, "npc")) errors.Add("reward 'ChestPages' is missing an integer 'npc'");
                        if (!IsInt(spec.Params, "pages")) errors.Add("reward 'ChestPages' is missing an integer 'pages'");
                        break;
                    case "WorldFlag":
                        if (!IsNonEmptyString(spec.Params, "name")) errors.Add("reward 'WorldFlag' is missing a string 'name'");
                        break;
                    default:
                        errors.Add(IsPersonalType(spec.Type)
                            ? $"reward type '{spec.Type}' is personal and cannot be a community reward"
                            : $"unknown reward type '{spec.Type}'");
                        break;
                }
            }
            else
            {
                switch (spec.Type)
                {
                    case "ChestPages":
                    case "WorldFlag":
                        errors.Add($"reward type '{spec.Type}' is a community reward and cannot be a personal reward");
                        break;
                    default:
                        if (!IsPersonalType(spec.Type))
                            errors.Add($"unknown reward type '{spec.Type}'");
                        else
                        {
                            var wantsString = StringValueTypes.Contains(Enum.Parse<RewardType>(spec.Type));
                            if (!HasValue(spec.Params))
                                errors.Add($"reward type '{spec.Type}' is missing 'value'");
                            else if (!ValueKindMatches(spec.Params["value"], wantsString))
                                errors.Add(wantsString
                                    ? $"reward type '{spec.Type}' requires a string 'value'"
                                    : $"reward type '{spec.Type}' requires an integer 'value'");
                            if (spec.Params["value2"] is JsonValue value2 && AsLongOrNull(value2) is null)
                                errors.Add($"reward type '{spec.Type}' requires an integer 'value2'");
                        }
                        break;
                }
            }
            return errors;
        }

        public static int RequiredSlots(IEnumerable<RewardSpec> specs, CommunityRewardContext ctx)
        {
            var slots = 0;
            foreach (var spec in specs)
            {
                if (spec.Type == nameof(RewardType.Item))
                    slots++;
                else if (spec.Type == nameof(RewardType.Script))
                {
                    var required = GetScript(spec, ctx.World).GetRequiredInventorySpace(ScriptContext(spec, ctx));
                    if (required > 0) slots += required;
                }
            }
            return slots;
        }

        public static string? CanGive(IEnumerable<RewardSpec> specs, CommunityRewardContext ctx)
        {
            foreach (var spec in specs)
            {
                if (spec.Type != nameof(RewardType.Script)) continue;
                var refusal = GetScript(spec, ctx.World).CanGive(ScriptContext(spec, ctx));
                if (refusal is not null) return refusal;
            }
            return null;
        }

        public static void Give(IEnumerable<RewardSpec> specs, CommunityRewardContext ctx)
        {
            foreach (var spec in specs)
            {
                switch (spec.Type)
                {
                    case nameof(RewardType.Script):
                        GetScript(spec, ctx.World).Give(ScriptContext(spec, ctx));
                        break;
                    case "ChestPages":
                        if (spec.Params["npc"] is not JsonValue npcValue || spec.Params["pages"] is not JsonValue pagesValue)
                            throw new CommunityGoalDefinitionException("reward 'ChestPages' is missing 'npc' or 'pages'");
                        var npc = (int)AsLong(npcValue);
                        ctx.World.CommunityGoals.AddToFlag("chest_pages:" + npc, (int)AsLong(pagesValue), ctx.Goal.Id);
                        ctx.World.ChestHandler.ApplyBonusPages(ctx.World, npc);
                        break;
                    case "WorldFlag":
                        if (spec.Params["name"] is not JsonValue nameValue || nameValue.GetValueKind() != JsonValueKind.String)
                            throw new CommunityGoalDefinitionException("reward 'WorldFlag' is missing a string 'name'");
                        ctx.World.CommunityGoals.SetFlag(nameValue.GetValue<string>()!, 1, ctx.Goal.Id);
                        break;
                    default:
                        if (IsPersonalType(spec.Type))
                            QuestRewardGranter.Give(ToQuestReward(spec), ctx.Npc!, ctx.Player!, ctx.World);
                        break;
                }
            }
        }

        private static bool IsPersonalType(string type) =>
            Enum.TryParse<RewardType>(type, false, out var parsed) && parsed != RewardType.Script;

        private static bool HasValue(JsonObject params_)
        {
            if (!params_.TryGetPropertyValue("value", out var value) || value is not JsonValue jsonValue)
                return false;
            return jsonValue.GetValueKind() is JsonValueKind.Number or JsonValueKind.String;
        }

        private static bool ValueKindMatches(JsonNode? node, bool wantsString)
        {
            if (node is not JsonValue value)
                return false;
            return wantsString
                ? value.GetValueKind() == JsonValueKind.String
                : AsLongOrNull(value) is not null;
        }

        private static bool IsInt(JsonObject params_, string field) =>
            params_[field] is JsonValue value
            && value.GetValueKind() == JsonValueKind.Number
            && AsLongOrNull(value) is long l && l is >= int.MinValue and <= int.MaxValue;

        private static bool IsNonEmptyString(JsonObject params_, string field) =>
            params_[field] is JsonValue value && value.GetValueKind() == JsonValueKind.String
            && !string.IsNullOrEmpty(value.GetValue<string>());

        // Same field vocabulary as the Quest Rewards sheet: value -> LongValue (number) or
        // StringValue (string), value2 -> LongValue2, Item stack defaulting to 1.
        private static QuestReward ToQuestReward(RewardSpec spec)
        {
            var reward = new QuestReward
            {
                Type = Enum.Parse<RewardType>(spec.Type),
                StringValue = "",
            };
            if (spec.Params["value"] is JsonValue value)
            {
                if (value.GetValueKind() == JsonValueKind.String)
                    reward.StringValue = value.GetValue<string>()!;
                else
                    reward.LongValue = AsLong(value);
            }
            if (spec.Params["value2"] is JsonValue value2)
                reward.LongValue2 = AsLong(value2);
            if (reward.Type == RewardType.Item && reward.LongValue2 == 0)
                reward.LongValue2 = 1;
            return reward;
        }

        private static ICommunityRewardScript GetScript(RewardSpec spec, GameWorld world) =>
            world.ScriptHandler.GetScript<ICommunityRewardScript>(spec.Params["script"]!.GetValue<string>()!).Object;

        private static long AsLong(JsonValue value) =>
            AsLongOrNull(value) ?? throw new CommunityGoalDefinitionException("reward value must be an integer");

        // JsonNode.Parse yields JsonValueOfElement, whose GetValue<object> is a JsonElement;
        // hand-built JsonValue<T> nodes box the primitive instead.
        private static long? AsLongOrNull(JsonValue value)
        {
            var boxed = value.GetValue<object>();
            if (boxed is JsonElement element)
            {
                if (element.ValueKind != JsonValueKind.Number) return null;
                try
                {
                    return element.GetInt64();
                }
                catch (FormatException)
                {
                    return null;
                }
            }
            return boxed switch
            {
                int i => i,
                long l => l,
                double d when d == Math.Truncate(d) && d is >= long.MinValue and <= long.MaxValue => (long)d,
                _ => null,
            };
        }

        private static CommunityRewardContext ScriptContext(RewardSpec spec, CommunityRewardContext ctx) => new()
        {
            Goal = ctx.Goal,
            Run = ctx.Run,
            Player = ctx.Player,
            Npc = ctx.Npc,
            Bracket = ctx.Bracket,
            Params = spec.Params["params"] is JsonObject params_ ? params_ : new JsonObject(),
            World = ctx.World,
        };
    }
}
