using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class Part3GmAdminTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer gm, Map map) WorldAndGm()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var gm = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            gm.Access = Player.AccessStatus.GameMaster;
            return (fixture, gm, map);
        }

        private static NPCTemplate AddNpcTemplate(TestWorldFixture fixture, int id, string name)
        {
            var template = new NPCTemplate { NPCTemplateID = id, Name = name, Level = 50, BaseStats = new AttributeSet() };
            fixture.World.NPCHandler.AddTemplate(template);
            return template;
        }

        [Fact]
        public void Warp_full_args_moves_player_to_target_map()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                fixture.AddBaseMap(2, "Two", 10, 10);

                Assert.True(fixture.RunCommand(gm, "/warp 2 5 5"));

                Assert.Equal(2, gm.MapID);
                Assert.Equal(5, gm.MapX);
                Assert.Equal(5, gm.MapY);
            }
        }

        [Fact]
        public void Warp_partial_args_is_silent_noop()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                fixture.AddBaseMap(2, "Two", 10, 10);

                Assert.True(fixture.RunCommand(gm, "/warp 2"));

                Assert.Equal(1, gm.MapID);
                Assert.Equal(1, gm.MapX);
                Assert.Equal(2, gm.MapY);
                Assert.Empty(gm.Sent);
            }
        }

        [Fact]
        public void Warp_bare_key_never_matches()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.False(fixture.RunCommand(gm, "/warp"));
                Assert.Empty(gm.Sent);
            }
        }

        [Fact]
        public void GetItem_with_stack_adds_item_with_that_stack()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                fixture.AddBaseItemTemplate(5, "Sword", ItemTemplate.UseTypes.NoUse);

                Assert.True(fixture.RunCommand(gm, "/getitem 5 2"));

                var slot = gm.Inventory.GetInventorySlots().FirstOrDefault(s => s?.Item.Name == "Sword");
                Assert.NotNull(slot);
                Assert.Equal(2, slot!.Stack);
            }
        }

        [Fact]
        public void GetItem_unparseable_stack_replies_with_usage()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                fixture.AddBaseItemTemplate(5, "Sword", ItemTemplate.UseTypes.NoUse);

                Assert.True(fixture.RunCommand(gm, "/getitem 5 powerful"));

                Assert.DoesNotContain(gm.Inventory.GetInventorySlots(), s => s?.Item.Name == "Sword");
                Assert.Contains(gm.Sent, s => s.Contains("Usage: /getitem <id> [stack]"));
            }
        }

        [Fact]
        public void GetItem_extra_tokens_are_ignored()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                fixture.AddBaseItemTemplate(5, "Sword", ItemTemplate.UseTypes.NoUse);

                Assert.True(fixture.RunCommand(gm, "/getitem 5 2 powerful"));

                var slot = gm.Inventory.GetInventorySlots().FirstOrDefault(s => s?.Item.Name == "Sword");
                Assert.NotNull(slot);
                Assert.Equal(2, slot!.Stack);
            }
        }

        [Fact]
        public void Hax_echoes_raw_remainder_unmodified()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/hax M1,5,5"));

                Assert.Contains(gm.Sent, s => s == "M1,5,5\x1");
            }
        }

        [Fact]
        public void Hax_preserves_doubled_space_in_remainder()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/hax  M1,5,5"));

                Assert.Contains(gm.Sent, s => s == " M1,5,5\x1");
            }
        }

        [Fact]
        public void SetConfig_joins_value_tokens()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                fixture.AddOnlinePlayer(gm);

                Assert.True(fixture.RunCommand(gm, "/setconfig ServerName bar baz"));

                Assert.Equal("bar baz", fixture.Settings.ServerName);
                Assert.Contains(gm.Sent, s => s.Contains("[GM] Set Game Setting ServerName to: bar baz"));
            }
        }

        [Fact]
        public void SetConfig_missing_value_sends_usage()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/setconfig ServerName"));

                Assert.Null(fixture.Settings.ServerName);
                Assert.Contains(gm.Sent, s => s.Contains("Usage: /setconfig <setting> <value...>"));
            }
        }

        [Fact]
        public void Search_item_matches_templates_by_regex()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                fixture.AddBaseItemTemplate(5, "Sword of Fire", ItemTemplate.UseTypes.NoUse);
                fixture.AddBaseItemTemplate(6, "Round Shield", ItemTemplate.UseTypes.NoUse);

                Assert.True(fixture.RunCommand(gm, "/search item sword"));

                Assert.Contains(gm.Sent, s => s.Contains("5 - Sword of Fire"));
                Assert.DoesNotContain(gm.Sent, s => s.Contains("Round Shield"));
                Assert.Contains(gm.Sent, s => s.Contains("[Matched 1 items]"));
            }
        }

        [Fact]
        public void MuteMap_toggles_muted_and_broadcasts()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                map.Players.Add(gm);

                Assert.True(fixture.RunCommand(gm, "/mutemap"));
                Assert.True(map.Muted);
                Assert.Contains(gm.Sent, s => s.Contains("Chat is now muted."));

                Assert.True(fixture.RunCommand(gm, "/mutemap"));
                Assert.False(map.Muted);
                Assert.Contains(gm.Sent, s => s.Contains("Chat is now unmuted."));
            }
        }

        [Fact]
        public void Shutdown_normal_player_is_swallowed()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                gm.Access = Player.AccessStatus.Normal;
                fixture.World.Running = true;

                Assert.True(fixture.RunCommand(gm, "/shutdown"));

                Assert.True(fixture.World.Running);
                Assert.Empty(gm.Sent);
            }
        }

        [Fact]
        public void Shutdown_gm_stops_the_world()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                fixture.World.Running = true;

                Assert.True(fixture.RunCommand(gm, "/shutdown"));

                Assert.False(fixture.World.Running);
            }
        }

        [Fact]
        public void SpawnNpc_spawns_registered_template_at_player()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                AddNpcTemplate(fixture, 7, "Slime");

                Assert.True(fixture.RunCommand(gm, "/spawnnpc 7"));

                Assert.Equal(1, fixture.World.NPCHandler.NPCCount);
            }
        }

        [Fact]
        public void PlaceSpawn_drops_gold_item_at_player()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                AddNpcTemplate(fixture, 7, "Slime");
                fixture.AddBaseItemTemplate(fixture.Settings.GoldItemID, "Gold", ItemTemplate.UseTypes.NoUse);

                Assert.True(fixture.RunCommand(gm, "/placespawn 7"));

                Assert.IsType<ItemTile>(map.GetTile(gm.MapX, gm.MapY));
            }
        }

        [Fact]
        public void PlaceSpawn_missing_or_bad_id_sends_usage()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                AddNpcTemplate(fixture, 7, "Slime");
                fixture.AddBaseItemTemplate(fixture.Settings.GoldItemID, "Gold", ItemTemplate.UseTypes.NoUse);

                Assert.True(fixture.RunCommand(gm, "/placespawn"));
                Assert.True(fixture.RunCommand(gm, "/placespawn 0"));

                Assert.Equal(2, gm.Sent.Count(s => s.Contains("Usage: /placespawn <npcId>")));
                Assert.Null(map.GetTile(gm.MapX, gm.MapY));
            }
        }

        [Fact]
        public void SetAccess_changes_online_players_access()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var bob = fixture.CommandPlayerOn(map, 3, 4, "Bob");
                bob.Access = Player.AccessStatus.Normal;
                fixture.RegisterDatabasePlayer(bob);

                Assert.True(fixture.RunCommand(gm, "/setaccess Bob guide"));

                Assert.Equal(Player.AccessStatus.Guide, bob.Access);
                Assert.Contains(gm.Sent, s => s.Contains("Set AccessStatus for Bob to Guide."));
            }
        }

        [Fact]
        public void SetAccess_unknown_player_reports_not_found()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/setaccess Nobody guide"));

                Assert.Contains(gm.Sent, s => s.Contains("Couldn't find player."));
            }
        }

        [Fact]
        public void SaveConfig_is_a_silent_noop()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/saveconfig"));
                Assert.Empty(gm.Sent);
            }
        }

        [Fact]
        public void RespawnMap_revives_dead_npcs_and_broadcasts()
        {
            var (fixture, gm, map) = WorldAndGm();
            using (fixture)
            {
                var template = AddNpcTemplate(fixture, 7, "Slime");
                var npc = fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 3, 3, template, shouldRespawn: false)!;
                npc.State = NPC.States.Dead;
                map.Players.Add(gm);

                Assert.True(fixture.RunCommand(gm, "/respawnmap"));

                Assert.Equal(NPC.States.Alive, npc.State);
                Assert.Contains(gm.Sent, s => s.Contains("Respawned all NPCs."));
            }
        }

        [Fact]
        public void GmHax_embeds_raw_remainder_in_chp_packet()
        {
            var (fixture, gm, _) = WorldAndGm();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/gmhax 3,4,5"));

                Assert.Contains(gm.Sent, s => s.StartsWith("CHP") && s.Contains("3,4,5,"));
            }
        }
    }
}
