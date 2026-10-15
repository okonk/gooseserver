using Goose;
using Goose.CommunityGoals;
using Goose.Scripting;
using System.Text.Json.Nodes;

public class PotShare : BaseCommunityRewardScript
{
    public override void Give(CommunityRewardContext ctx)
    {
        if (ctx.Player is null)
        {
            LogError(ctx, "no player to pay");
            return;
        }
        if (!ctx.Params.TryGetPropertyValue("key", out var keyNode) || keyNode is not JsonValue keyVal || keyVal.GetValueKind() != JsonValueKind.String)
        {
            LogError(ctx, "missing string 'key' param");
            return;
        }
        if (!ctx.Params.TryGetPropertyValue("percent", out var percentNode)
            || percentNode is not JsonValue percentVal
            || percentVal.GetValueKind() != JsonValueKind.Number
            || !TryGetInt(percentVal, out var percent)
            || percent is < 1 or > 100)
        {
            LogError(ctx, "missing or out-of-range 'percent' param (integer 1-100)");
            return;
        }

        var key = keyVal.GetValue<string>()!;
        long pot = ctx.Run.Totals.GetValueOrDefault(key);
        long share = pot * percent / 100;
        ctx.Player.AddGold(share, ctx.World);
        ctx.World.Send(ctx.Player, P.ServerMessage($"[Community Reward] {share} gold from the {key} pot"));
    }

    private static bool TryGetInt(JsonValue value, out int result)
    {
        var boxed = value.GetValue<object>();
        if (boxed is JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Number) { result = 0; return false; }
            try
            {
                long parsed = element.GetInt64();
                if (parsed is < int.MinValue or > int.MaxValue) { result = 0; return false; }
                result = (int)parsed;
                return true;
            }
            catch (FormatException)
            {
                result = 0;
                return false;
            }
        }
        if (boxed is int i) { result = i; return true; }
        if (boxed is long l && l is >= int.MinValue and <= int.MaxValue) { result = (int)l; return true; }
        if (boxed is double d && d == Math.Truncate(d) && d is >= int.MinValue and <= int.MaxValue) { result = (int)d; return true; }
        result = 0;
        return false;
    }

    private static void LogError(CommunityRewardContext ctx, string what)
    {
        ctx.World.LogHandler.Log(Log.Types.GiveGold, ctx.Player?.PlayerID ?? 0,
            "PotShare reward on community goal " + ctx.Goal.Id + " gave nothing: " + what);
    }
}

return typeof(PotShare);
