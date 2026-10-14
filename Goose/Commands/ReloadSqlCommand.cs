using Goose.Scripting;

namespace Goose.Commands
{
    [Command("/reloadsql", AccessPrivilege.ReloadSQL, Section = "Admin", Help = "Reload SQL data.")]
    public sealed class ReloadSqlCommand : BaseCommand
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;
            var player = ctx.Player;

            world.Send(player, "$7Reloading sql...");

            Task.Run(() =>
            {
                try
                {
                    CompileReferencedScripts(world);
                }
                catch (Exception e)
                {
                    world.EnqueueCompletion(() => Fail(world, player, e));
                    return;
                }

                world.EnqueueCompletion(() =>
                {
                    try
                    {
                        world.SpellHandler.LoadSpellEffects(world);
                        world.SpellHandler.LoadSpells(world);
                        world.ItemHandler.LoadTemplates(world);
                        world.ItemHandler.RefreshItemStats(world);
                        world.QuestHandler.LoadQuests(world);
                        world.CommunityGoals.LoadDefinitions(world);
                        //world.MapHandler.LoadMaps(world);
                        //world.ClassHandler.LoadClasses(world);
                        world.NPCHandler.LoadNPCTemplates(world);
                        //world.NPCHandler.LoadNPCs(world);
                        //world.CombinationHandler.LoadCombinations(world);

                        world.Send(player, "$7Reloaded sql data.");
                        log.Info("Reloaded sql data");
                    }
                    catch (Exception e)
                    {
                        Fail(world, player, e);
                    }
                });
            });
        }

        // The loaders run on the game thread and call GetScript; compiling here keeps Roslyn off it.
        private static void CompileReferencedScripts(GameWorld world)
        {
            var paths = world.Database.Execute(conn => new
            {
                SpellEffects = ScriptPaths(conn, "spell_effects"),
                Items = ScriptPaths(conn, "item_templates"),
                Npcs = ScriptPaths(conn, "npc_templates"),
                Quests = ScriptPaths(conn, "quest_requirements").Concat(ScriptPaths(conn, "quest_rewards")).Distinct().ToList(),
                CommunityGoals = ScriptPaths(conn, "community_goals"),
            });

            foreach (var path in paths.SpellEffects) world.ScriptHandler.GetScript<ISpellEffectScript>(path);
            foreach (var path in paths.Items) world.ScriptHandler.GetScript<IItemScript>(path);
            foreach (var path in paths.Npcs) world.ScriptHandler.GetScript<INPCScript>(path);
            foreach (var path in paths.Quests) world.ScriptHandler.GetScript<IQuestScript>(path);
            foreach (var path in paths.CommunityGoals) world.ScriptHandler.GetScript<ICommunityGoalScript>(path);
        }

        private static List<string> ScriptPaths(System.Data.SQLite.SQLiteConnection conn, string table)
        {
            using var command = conn.CreateCommand();
            command.CommandText = $"SELECT DISTINCT script_path FROM {table} WHERE script_path IS NOT NULL AND script_path <> ''";
            using var reader = command.ExecuteReader();

            var paths = new List<string>();
            while (reader.Read()) paths.Add(reader.GetString(0));
            return paths;
        }

        private static void Fail(GameWorld world, Player player, Exception e)
        {
            log.Error(e, "Failed reloading sql data");
            world.Send(player, "$7Failed reloading sql: " + e.Message);
        }
    }
}
