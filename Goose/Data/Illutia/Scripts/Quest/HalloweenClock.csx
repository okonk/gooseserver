using System;
using System.Globalization;
using Goose;
using Goose.Quests;
using Goose.Scripting;

public class HalloweenClock : BaseQuestScript
{
    public override string? CanComplete(QuestReward reward, Player player, GameWorld world)
        => Reading(DateTime.Now);

    public string Reading(DateTime now)
    {
        string reading = "The grandfather clock reads " + now.ToString("h:mm tt", CultureInfo.InvariantCulture) + ".\\n";
        if (now.Minute == 0) return reading + "It is striking the hour right now!";
        int minutes = 60 - now.Minute;
        return reading + $"It strikes the hour in {minutes} minute{(minutes == 1 ? "" : "s")}.";
    }
}

return typeof(HalloweenClock);
