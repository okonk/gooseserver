using System.Text.Json;
using System.Text.Json.Nodes;
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
                CommunityGoalRewards = CommunityGoalRewardScriptPaths(conn),
            });

            foreach (var path in paths.SpellEffects) world.ScriptHandler.GetScript<ISpellEffectScript>(path);
            foreach (var path in paths.Items) world.ScriptHandler.GetScript<IItemScript>(path);
            foreach (var path in paths.Npcs) world.ScriptHandler.GetScript<INPCScript>(path);
            foreach (var path in paths.Quests) world.ScriptHandler.GetScript<IQuestScript>(path);
            foreach (var path in paths.CommunityGoals) world.ScriptHandler.GetScript<ICommunityGoalScript>(path);
            foreach (var path in paths.CommunityGoalRewards) world.ScriptHandler.GetScript<ICommunityRewardScript>(path);
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

        private static List<string> CommunityGoalRewardScriptPaths(System.Data.SQLite.SQLiteConnection conn)
        {
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT properties FROM community_goals WHERE properties IS NOT NULL AND properties <> ''";
            using var reader = command.ExecuteReader();

            var paths = new HashSet<string>(StringComparer.Ordinal);
            while (reader.Read())
                CollectRewardScriptPaths(reader.GetString(0), paths);
            return paths.ToList();
        }

        // properties is author-written JSON that can be invalid; a bad row must not fail the reload.
        // Duplicate properties parse fine but throw a raw ArgumentException when the object is
        // first enumerated, so the whole walk is guarded, not just the parse.
        private static void CollectRewardScriptPaths(string json, HashSet<string> paths)
        {
            try
            {
                var root = JsonNode.Parse(json);
                if (root is not JsonObject def) return;
                if (def["brackets"] is JsonArray brackets)
                    foreach (var bracket in brackets)
                        if (bracket is JsonObject bracketObj && bracketObj["rewards"] is JsonArray rewards)
                            CollectFromRewards(rewards, paths);
                if (def["communityRewards"] is JsonArray communityRewards)
                    CollectFromRewards(communityRewards, paths);
            }
            catch (Exception e) when (e is JsonException or ArgumentException)
            {
                log.Warn(e, "Skipping malformed community_goals properties row");
            }
        }

        private static void CollectFromRewards(JsonArray rewards, HashSet<string> paths)
        {
            foreach (var reward in rewards)
            {
                if (reward is not JsonObject rewardObj) continue;
                if (rewardObj["type"] is not JsonValue type || type.GetValueKind() != JsonValueKind.String || type.GetValue<string>() != "Script")
                    continue;
                if (rewardObj["script"] is JsonValue script && script.GetValueKind() == JsonValueKind.String)
                    paths.Add(script.GetValue<string>()!);
            }
        }

        private static void Fail(GameWorld world, Player player, Exception e)
        {
            log.Error(e, "Failed reloading sql data");
            world.Send(player, "$7Failed reloading sql: " + e.Message);
        }
    }
}
