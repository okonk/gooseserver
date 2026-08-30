namespace Goose.Commands
{
    [Command("/changeclass ", AccessPrivilege.ClassChange, Section = "GM", Help = "Change a player's class.")]
    public sealed class ChangeClassCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, string cl, decimal? modifier = null)
        {
            var world = ctx.World;

            decimal rate = modifier ?? 1m;

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is null)
            {
                ctx.Send("Player " + name + " doesn't exist.");
                return;
            }

            var newClass = world.ClassHandler.Classes.FirstOrDefault(c => c.ClassName.ToLowerInvariant() == cl.ToLowerInvariant());
            if (newClass is null)
            {
                ctx.Send("Invalid class name.");
                return;
            }

            if (player.Class.GetLevel(player.Level) is null || newClass.GetLevel(player.Level) is null)
            {
                ctx.Send("Cannot change class: level data missing.");
                return;
            }

            player.ClassID = newClass.ClassID;

            player.RemoveStats(player.BaseStats, world);

            player.MaxStats -= player.Class.GetLevel(player.Level)!.BaseStats;
            player.Experience += player.ExperienceSold;
            player.Experience = (long)(player.Experience * rate);
            player.ExperienceSold = 0;
            player.BaseStats.HP = 0;
            player.BaseStats.MP = 0;

            player.AddStats(player.BaseStats, world);

            player.Class = newClass;

            player.AddStats(player.Class.GetLevel(player.Level)!.BaseStats, world);

            world.Send(player, P.StatusInfo(player));
            world.Send(player, P.ExpBar(player));

            player.Spellbook.RemoveNonClassSpells(world);

            for (int level = 1; level <= player.Level; level++)
            {
                if (level > player.Class.MaxLevel) break;

                foreach (var spell in player.Class.GetLevel(level)!.Spells)
                {
                    player.LearnSpell(spell.ID, world);
                }
            }

            ctx.Send("Changed class successfully.");

            if (player.State != Player.States.NotLoggedIn)
            {
                world.Send(player, P.StatusInfo(player));
            }
            else
            {
                player.SaveToDatabase(world);
            }

            world.LogHandler.Log(Log.Types.ClassChange,
                ctx.Player.PlayerID, player.PlayerID + " " + cl + " " + rate,
                0, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
