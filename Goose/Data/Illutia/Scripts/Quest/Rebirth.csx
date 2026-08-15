using System;
using Goose;
using Goose.Quests;
using Goose.Scripting;

/// <summary>Rebirth: converts banked experience into spirit and resets the character.
///
/// Backs both the Script requirement (the threshold) and the Script reward (the whole
/// transaction) on the quest Dimensions.csx creates. One file serves both roles because
/// IQuestScript covers both.
///
/// All state change is in GiveReward, never in OnTakeRequirement: QuestWindow runs
/// TakeRequirements before GiveRewards (QuestWindow.cs:341-342), so consuming the
/// experience in the requirement would zero the number the reward has to read. The
/// requirement is registered with KeepRequirement = true for that reason.</summary>
public class Rebirth : BaseQuestScript
{
    private const string SpiritCurrencyId = "spirit";

    /// <summary>Read inside the call and never cached in a field - the IQuestScript
    /// contract, because one script instance is shared by every row pointing at it.</summary>
    private static long RateFrom(string scriptParams)
    {
        long rate;
        if (!long.TryParse(scriptParams, out rate) || rate <= 0)
            throw new Exception("Rebirth.csx: ScriptParams must be a positive experience-per-spirit rate.");

        return rate;
    }

    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
    {
        return player.Experience + player.ExperienceSold >= RateFrom(requirement.ScriptParams);
    }

    public override string GetProgressText(QuestRequirement requirement, Player player, GameWorld world)
    {
        long rate = RateFrom(requirement.ScriptParams);
        long total = player.Experience + player.ExperienceSold;

        return string.Format("{0:N0} / {1:N0} experience", total, rate);
    }

    public override string CanComplete(QuestReward reward, Player player, GameWorld world)
    {
        if (world.CurrencyHandler.Get(SpiritCurrencyId) == null)
            return "The void is silent. Rebirth is not possible here.";

        if (player.Experience + player.ExperienceSold < RateFrom(reward.ScriptParams))
            return "You have not earned enough to be worth remaking.";

        return null;
    }

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        var spirit = world.CurrencyHandler.Get(SpiritCurrencyId);
        if (spirit == null) return;     // CanComplete already refused; belt and braces

        long rate = RateFrom(reward.ScriptParams);
        long total = player.Experience + player.ExperienceSold;
        long minted = total / rate;

        // Class 1 level 1 - Commoner. Hardcoded because .csx files compile separately and
        // this one cannot see Dimensions.RebirthDestinationClassId; CreateRebirthQuest
        // preflights the same pair against class_info, so keep the two in step.
        //
        // Explicit 0 loss: rebirth is an exchange, not the 7% penalty quest 60 charges.
        // ChangeClass does the rest - RemoveStats/AddStats, the MaxStats adjustment, the
        // level-1 class row, BaseStats.HP/MP = 0, Spellbook.RemoveNonClassSpells, the bind
        // reset, and the StatusInfo/ExpBar packets (Player.cs:1358-1400).
        player.ChangeClass(1, 1, world, 0d);

        // After ChangeClass, which banks Experience into ExperienceSold. The sub-rate
        // remainder is destroyed, faithful to RebirthEvent.java:47.
        player.Experience = 0;
        player.ExperienceSold = 0;

        spirit.Add(player, minted, world);

        world.Send(player, P.ServerMessage(string.Format(
            "You surrender {0:N0} experience and are remade. You gain {1:N0} spirit.", total, minted)));

        world.LogHandler.Log(Log.Types.Rebirth, player,
            string.Format("Rebirth: {0} experience -> {1} spirit", total, minted));
    }
}

return typeof(Rebirth);
