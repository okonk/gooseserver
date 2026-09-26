using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    public class LogFormatterTests
    {
        private static readonly IReadOnlyDictionary<int, string> Players = new Dictionary<int, string>
        {
            [1] = "Alice", [2] = "Bob", [7] = "GM", [42] = "Carol",
        }.AsReadOnly();

        private static readonly IReadOnlyDictionary<int, string> Guilds = new Dictionary<int, string>
        {
            [5] = "Knights",
        }.AsReadOnly();

        private static readonly IReadOnlyDictionary<int, string> Npcs = new Dictionary<int, string>
        {
            [100] = "Merchant",
        }.AsReadOnly();

        private static readonly IReadOnlyDictionary<int, string> Maps = new Dictionary<int, string>
        {
            [3] = "Town Square",
        }.AsReadOnly();

        private static LogFormatContext Row(long type, long? playerId = 1, long? otherId = 0,
            string text = "", long? mapId = 0, long? mapX = 10, long? mapY = 20,
            IReadOnlyDictionary<int, string>? playerNames = null,
            IReadOnlyDictionary<int, string>? guildNames = null,
            IReadOnlyDictionary<int, string>? npcTemplateNames = null,
            IReadOnlyDictionary<int, string>? mapNames = null)
            => new(1, 1_700_000_000_000_000, type, playerId, otherId, mapId, mapX, mapY, text,
                playerNames ?? Players, guildNames ?? Guilds,
                npcTemplateNames ?? Npcs, mapNames ?? Maps);

        private static void AssertRelated(string label, LogEntityKind kind, long? id, string? name,
            bool quick, LogFormattedEvent e)
        {
            Assert.NotNull(e.Related);
            Assert.Equal(label, e.Related!.Label);
            Assert.Equal(kind, e.Related.Kind);
            Assert.Equal(id, e.Related.Id);
            Assert.Equal(name, e.Related.Name);
            Assert.Equal(quick, e.Related.CanQuickFilter);
        }

        [Fact]
        public void PickupItem_projects_item_related_from_stored_text()
        {
            var e = LogFormatter.Project(Row(11, 1, 0, "77 12 Iron Sword 3"));

            Assert.Equal("Alice picked up Iron Sword ×3", e.Summary);
            AssertRelated("Item", LogEntityKind.Item, 77, "Iron Sword", true, e);
        }

        [Fact]
        public void PickupItem_gold_projects_gold_related_without_filterable_id()
        {
            var e = LogFormatter.Project(Row(11, 1, 0, "500 gold"));

            Assert.Equal("Alice picked up 500 gold", e.Summary);
            AssertRelated("Gold", LogEntityKind.Gold, null, null, false, e);
        }

        [Fact]
        public void PlayerDropItem_projects_item_related_from_stored_text()
        {
            var e = LogFormatter.Project(Row(12, 1, 0, "77 12 Iron Sword 3"));

            Assert.Equal("Alice dropped Iron Sword ×3", e.Summary);
            AssertRelated("Item", LogEntityKind.Item, 77, "Iron Sword", true, e);
        }

        [Fact]
        public void GetItem_projects_item_related_parsed_from_the_right()
        {
            var e = LogFormatter.Project(Row(10001, 7, 0, "Iron Sword 77 3"));

            Assert.Equal("GM gave Iron Sword ×3", e.Summary);
            AssertRelated("Item", LogEntityKind.Item, 77, "Iron Sword", true, e);
        }

        [Fact]
        public void CreatedCustom_projects_item_related_from_otherid_with_name_from_text()
        {
            var e = LogFormatter.Project(Row(17, 7, 77, "Custom Sword (12) 34|255,0,0,255"));

            Assert.Equal("GM created custom item Custom Sword (template 12)", e.Summary);
            AssertRelated("Item", LogEntityKind.Item, 77, "Custom Sword", true, e);
        }

        [Fact]
        public void CreatedCustom_oversized_otherid_keeps_raw_id_and_disables_quick_filter()
        {
            var e = LogFormatter.Project(Row(17, 7, long.MaxValue, "Custom Sword (12) 34|255,0,0,255"));

            Assert.Equal("GM created custom item Custom Sword (template 12)", e.Summary);
            AssertRelated("Item", LogEntityKind.Item, long.MaxValue, "Custom Sword", false, e);
        }

        [Fact]
        public void ResetItem_projects_item_related_from_otherid_without_invented_name()
        {
            var e = LogFormatter.Project(Row(24, 1, 77,
                "ResetItem: template 12 dim 3 cost 500 spirit balance 1000 -> 500"));

            Assert.Equal("Alice reset an item (template 12, dimension 3) for 500 spirit", e.Summary);
            AssertRelated("Item", LogEntityKind.Item, 77, null, true, e);
        }

        [Fact]
        public void RespawnMap_projects_map_related_from_mapid_not_otherid()
        {
            var e = LogFormatter.Project(Row(10005, 7, 999, "", mapId: 3));

            Assert.Equal("GM respawned Town Square", e.Summary);
            AssertRelated("Map", LogEntityKind.Map, 3, "Town Square", true, e);
        }

        [Fact]
        public void BuyFromVendor_projects_merchant_related_from_otherid()
        {
            var e = LogFormatter.Project(Row(18, 1, 100, "Health Potion (12) x3 (900 gold)"));

            Assert.Equal("Alice bought 3 Health Potion from Merchant", e.Summary);
            AssertRelated("Merchant", LogEntityKind.NpcTemplate, 100, "Merchant", true, e);
        }

        [Fact]
        public void SellToVendor_projects_merchant_related_from_otherid()
        {
            var e = LogFormatter.Project(Row(19, 1, 100, "Health Potion (12) x3 (900 gold)"));

            Assert.Equal("Alice sold 3 Health Potion to Merchant", e.Summary);
            AssertRelated("Merchant", LogEntityKind.NpcTemplate, 100, "Merchant", true, e);
        }

        [Fact]
        public void Unknown_type_with_nonzero_otherid_projects_stored_value_without_lookup()
        {
            var e = LogFormatter.Project(Row(999, 1, 2, "mystery"));

            Assert.Equal("Unknown event #999 (player 1, other 2, map 0 at 10, 20): mystery", e.Summary);
            AssertRelated("Stored other ID", LogEntityKind.StoredValue, 2, null, false, e);
        }

        [Fact]
        public void Unknown_type_with_oversized_otherid_keeps_raw_signed_id()
        {
            var e = LogFormatter.Project(Row(long.MaxValue, long.MinValue, long.MaxValue, "",
                mapId: long.MinValue, mapX: long.MaxValue, mapY: long.MinValue));

            Assert.Equal("Unknown event #9223372036854775807 (player -9223372036854775808, "
                + "other 9223372036854775807, map -9223372036854775808 at 9223372036854775807, -9223372036854775808)",
                e.Summary);
            AssertRelated("Stored other ID", LogEntityKind.StoredValue, long.MaxValue, null, false, e);
        }

        [Fact]
        public void Tell_projects_recipient_from_player_valued_otherid()
        {
            var e = LogFormatter.Project(Row(13, 1, 2, "Meet me in town"));

            Assert.Equal("Alice told Bob \"Meet me in town\"", e.Summary);
            AssertRelated("Recipient", LogEntityKind.Player, 2, "Bob", true, e);
        }

        [Fact]
        public void GuildChat_projects_guild_from_guild_valued_otherid()
        {
            var e = LogFormatter.Project(Row(7, 1, 5, "hello"));

            Assert.Equal("Alice said in Knights \"hello\"", e.Summary);
            AssertRelated("Guild", LogEntityKind.Guild, 5, "Knights", true, e);
        }

        [Fact]
        public void JoinGuild_projects_inviter_from_otherid()
        {
            var e = LogFormatter.Project(Row(5, 42, 2, "5"));

            Assert.Equal("Carol joined Knights (invited by Bob)", e.Summary);
            AssertRelated("Invited by", LogEntityKind.Player, 2, "Bob", true, e);
        }

        [Fact]
        public void LeaveGuild_projects_remover_and_is_null_when_voluntary()
        {
            var removed = LogFormatter.Project(Row(6, 42, 2, "5"));
            Assert.Equal("Carol left Knights, removed by Bob", removed.Summary);
            AssertRelated("Removed by", LogEntityKind.Player, 2, "Bob", true, removed);

            var voluntary = LogFormatter.Project(Row(6, 42, 0, "5"));
            Assert.Equal("Carol left Knights", voluntary.Summary);
            Assert.Null(voluntary.Related);
        }

        [Fact]
        public void JoinGroup_projects_invited_member_from_otherid()
        {
            var e = LogFormatter.Project(Row(8, 1, 42));

            Assert.Equal("Alice invited Carol to a group", e.Summary);
            AssertRelated("Member", LogEntityKind.Player, 42, "Carol", true, e);
        }

        [Fact]
        public void LeaveGroup_projects_remover_and_is_null_when_voluntary()
        {
            var removed = LogFormatter.Project(Row(9, 42, 1));
            Assert.Equal("Carol left a group, removed by Alice", removed.Summary);
            AssertRelated("Removed by", LogEntityKind.Player, 1, "Alice", true, removed);

            var voluntary = LogFormatter.Project(Row(9, 42, 0));
            Assert.Equal("Carol left a group", voluntary.Summary);
            Assert.Null(voluntary.Related);
        }

        [Fact]
        public void GaveCredits_projects_recipient_from_otherid()
        {
            var e = LogFormatter.Project(Row(15, 1, 2, "100"));

            Assert.Equal("Alice gave 100 credits to Bob", e.Summary);
            AssertRelated("Recipient", LogEntityKind.Player, 2, "Bob", true, e);
        }

        [Fact]
        public void GiveSpirit_sent_projects_recipient_and_received_projects_sender()
        {
            var sent = LogFormatter.Project(Row(23, 1, 2,
                "GiveSpirit: sent 50 spirit to Bob, balance 200 -> 150"));
            Assert.Equal("Alice sent 50 spirit to Bob", sent.Summary);
            AssertRelated("Recipient", LogEntityKind.Player, 2, "Bob", true, sent);

            var received = LogFormatter.Project(Row(23, 2, 1,
                "GiveSpirit: received 50 spirit from Alice, balance 0 -> 50"));
            Assert.Equal("Bob received 50 spirit from Alice", received.Summary);
            AssertRelated("Sender", LogEntityKind.Player, 1, "Alice", true, received);
        }

        [Theory]
        [InlineData(10002)]
        [InlineData(10003)]
        [InlineData(10004)]
        [InlineData(10007)]
        [InlineData(10010)]
        [InlineData(10011)]
        [InlineData(10012)]
        public void Gm_commands_project_target_from_player_valued_otherid(long type)
        {
            var e = LogFormatter.Project(Row(type, 7, 2));

            AssertRelated("Target", LogEntityKind.Player, 2, "Bob", true, e);
        }

        [Fact]
        public void SpawnedNPC_projects_npc_template_from_otherid()
        {
            var e = LogFormatter.Project(Row(10006, 7, 100, "Merchant"));

            Assert.Equal("GM spawned Merchant", e.Summary);
            AssertRelated("NPC", LogEntityKind.NpcTemplate, 100, "Merchant", true, e);
        }

        [Theory]
        [InlineData(0, "hello")]
        [InlineData(1, "hello")]
        [InlineData(2, "hello")]
        [InlineData(10, "hello")]
        [InlineData(3, "1.2.3.4")]
        [InlineData(4, "")]
        [InlineData(14, "100")]
        [InlineData(16, "1.2.3.4")]
        [InlineData(20, "Rebirth: 100000 experience -> 500 spirit")]
        [InlineData(21, "BuyGold: 100 spirit -> 1000000 gold, spirit 500 -> 400")]
        [InlineData(22, "BuyExperience: 100 spirit -> 100000 exp, spirit 500 -> 400")]
        [InlineData(10005, "")]
        [InlineData(10008, "")]
        [InlineData(10009, "")]
        [InlineData(10013, "")]
        [InlineData(999, "")]
        public void Null_related_where_no_semantic_relation_exists(long type, string text)
        {
            Assert.Null(LogFormatter.Project(Row(type, 1, 0, text)).Related);
        }

        [Theory]
        [InlineData(0, 1, 0, "hello", 0, "Alice said \"hello\"")]
        [InlineData(1, 1, 0, "hello world", 0, "Alice shouted \"hello world\"")]
        [InlineData(2, 1, 0, "hello world", 0, "Alice auctioned \"hello world\"")]
        [InlineData(10, 1, 0, "hello", 0, "Alice said in group \"hello\"")]
        [InlineData(7, 1, 5, "hello", 0, "Alice said in Knights \"hello\"")]
        [InlineData(7, 1, 6, "hello", 0, "Alice said in Guild #6 \"hello\"")]
        [InlineData(13, 1, 2, "Meet me in town", 0, "Alice told Bob \"Meet me in town\"")]
        [InlineData(13, 1, 3, "hi", 0, "Alice told Player #3 \"hi\"")]
        public void Communication_summaries(long type, long player, long other, string text, long map, string expected)
        {
            Assert.Equal(expected, LogFormatter.Project(Row(type, player, other, text, map)).Summary);
        }

        [Theory]
        [InlineData(3, 1, 0, "1.2.3.4", 0, "Alice joined from 1.2.3.4")]
        [InlineData(3, 1, 0, "", 0, "Alice joined")]
        [InlineData(4, 1, 0, "", 0, "Alice left")]
        [InlineData(16, 1, 0, "1.2.3.4", 0, "Alice failed login from 1.2.3.4")]
        [InlineData(16, 1, 0, "", 0, "Alice failed login")]
        public void Session_and_security_summaries(long type, long player, long other, string text, long map, string expected)
        {
            Assert.Equal(expected, LogFormatter.Project(Row(type, player, other, text, map)).Summary);
        }

        [Theory]
        [InlineData(5, 42, 2, "5", 0, "Carol joined Knights (invited by Bob)")]
        [InlineData(5, 42, 0, "5", 0, "Carol joined Knights")]
        [InlineData(5, 42, 2, "junk", 0, "Carol joined Guild #? (invited by Bob)")]
        [InlineData(6, 42, 2, "5", 0, "Carol left Knights, removed by Bob")]
        [InlineData(6, 42, 0, "5", 0, "Carol left Knights")]
        [InlineData(8, 1, 42, "", 0, "Alice invited Carol to a group")]
        [InlineData(9, 42, 1, "", 0, "Carol left a group, removed by Alice")]
        [InlineData(9, 42, 0, "", 0, "Carol left a group")]
        public void Social_membership_summaries(long type, long player, long other, string text, long map, string expected)
        {
            Assert.Equal(expected, LogFormatter.Project(Row(type, player, other, text, map)).Summary);
        }

        [Theory]
        [InlineData(14, 1, 0, "100", 0, "Alice received 100 credits")]
        [InlineData(14, 1, 0, "", 0, "Alice received credits")]
        [InlineData(15, 1, 2, "100", 0, "Alice gave 100 credits to Bob")]
        [InlineData(15, 1, 2, "", 0, "Alice gave credits to Bob")]
        [InlineData(15, 1, 0, "100", 0, "Alice gave 100 credits")]
        public void Credit_summaries(long type, long player, long other, string text, long map, string expected)
        {
            Assert.Equal(expected, LogFormatter.Project(Row(type, player, other, text, map)).Summary);
        }

        [Theory]
        [InlineData(11, 1, 0, "77 12 Iron Sword 3", 0, "Alice picked up Iron Sword ×3")]
        [InlineData(11, 1, 0, "500 gold", 0, "Alice picked up 500 gold")]
        [InlineData(12, 1, 0, "77 12 Iron Sword 3", 0, "Alice dropped Iron Sword ×3")]
        [InlineData(12, 1, 0, "500 gold", 0, "Alice dropped 500 gold")]
        [InlineData(17, 7, 77, "Custom Sword (12) 34|255,0,0,255", 0, "GM created custom item Custom Sword (template 12)")]
        [InlineData(18, 1, 100, "Health Potion (12) x3 (900 gold)", 0, "Alice bought 3 Health Potion from Merchant")]
        [InlineData(19, 1, 100, "Health Potion (12) x3 (900 gold)", 0, "Alice sold 3 Health Potion to Merchant")]
        [InlineData(18, 1, 101, "Health Potion (12) x3 (900 gold)", 0, "Alice bought 3 Health Potion from NPC #101")]
        public void Custom_and_vendor_summaries(long type, long player, long other, string text, long map, string expected)
        {
            Assert.Equal(expected, LogFormatter.Project(Row(type, player, other, text, map)).Summary);
        }

        [Theory]
        [InlineData(20, 1, 0, "Rebirth: 100000 experience -> 500 spirit", 0, "Alice rebirthed: 100000 experience -> 500 spirit")]
        [InlineData(21, 1, 0, "BuyGold: 100 spirit -> 1000000 gold, spirit 500 -> 400", 0, "Alice bought 1000000 gold for 100 spirit")]
        [InlineData(22, 1, 0, "BuyExperience: 100 spirit -> 100000 exp, spirit 500 -> 400", 0, "Alice bought 100000 experience for 100 spirit")]
        [InlineData(23, 1, 2, "GiveSpirit: sent 50 spirit to Bob, balance 200 -> 150", 0, "Alice sent 50 spirit to Bob")]
        [InlineData(23, 2, 1, "GiveSpirit: received 50 spirit from Alice, balance 0 -> 50", 0, "Bob received 50 spirit from Alice")]
        [InlineData(24, 1, 77, "ResetItem: template 12 dim 3 cost 500 spirit balance 1000 -> 500", 0, "Alice reset an item (template 12, dimension 3) for 500 spirit")]
        public void Dimension_summaries(long type, long player, long other, string text, long map, string expected)
        {
            Assert.Equal(expected, LogFormatter.Project(Row(type, player, other, text, map)).Summary);
        }

        [Theory]
        [InlineData(10001, 7, 0, "Iron Sword 77 3", 0, "GM gave Iron Sword ×3")]
        [InlineData(10002, 7, 2, "2 3 2.5", 0, "GM changed Bob's class to 3 (rate 2.5)")]
        [InlineData(10003, 7, 2, "5000 to 2", 0, "GM gave Bob 5000 experience")]
        [InlineData(10004, 7, 2, "5000 to 2", 0, "GM gave Bob 5000 gold")]
        [InlineData(10005, 7, 0, "", 3, "GM respawned Town Square")]
        [InlineData(10005, 7, 0, "", 4, "GM respawned Map #4")]
        [InlineData(10006, 7, 100, "Merchant", 0, "GM spawned Merchant")]
        [InlineData(10006, 7, 0, "Wandering Goblin", 0, "GM spawned Wandering Goblin")]
        [InlineData(10007, 7, 2, "", 0, "GM macro-checked Bob")]
        [InlineData(10008, 1, 0, "", 0, "Alice passed a macro check")]
        [InlineData(10009, 1, 0, "", 0, "Alice failed a macro check")]
        [InlineData(10010, 7, 2, "", 0, "GM banned Bob")]
        [InlineData(10011, 7, 2, "", 0, "GM kicked Bob")]
        [InlineData(10012, 7, 2, "Set password of Bob", 0, "GM set the password for Bob")]
        [InlineData(10013, 7, 0, "", 0, "GM viewed logs")]
        [InlineData(10013, 7, 0, "types 0,1; map 3", 0, "GM viewed logs: types 0,1; map 3")]
        public void Gm_action_summaries(long type, long player, long other, string text, long map, string expected)
        {
            Assert.Equal(expected, LogFormatter.Project(Row(type, player, other, text, map)).Summary);
        }

        [Theory]
        [InlineData(11, "", "PickupItem")]
        [InlineData(11, "77", "PickupItem: 77")]
        [InlineData(11, "77 12", "PickupItem: 77 12")]
        [InlineData(11, "77 12 Iron Sword", "PickupItem: 77 12 Iron Sword")]
        [InlineData(11, "77 12 Iron Sword x", "PickupItem: 77 12 Iron Sword x")]
        [InlineData(11, "99999999999999999999 12 Sword 1", "PickupItem: 99999999999999999999 12 Sword 1")]
        [InlineData(11, "gold 500", "PickupItem: gold 500")]
        [InlineData(11, "500 Gold", "PickupItem: 500 Gold")]
        [InlineData(12, "77 12 Iron Sword x", "PlayerDropItem: 77 12 Iron Sword x")]
        [InlineData(10001, "", "GetItem")]
        [InlineData(10001, "Sword", "GetItem: Sword")]
        [InlineData(10001, "Sword 77", "GetItem: Sword 77")]
        [InlineData(10001, "Sword x 3", "GetItem: Sword x 3")]
        [InlineData(10001, "Sword 77 99999999999999999999", "GetItem: Sword 77 99999999999999999999")]
        [InlineData(17, "", "CreatedCustom")]
        [InlineData(17, "(12) 34|255,0,0,255", "CreatedCustom: (12) 34|255,0,0,255")]
        [InlineData(17, "Sword (12", "CreatedCustom: Sword (12")]
        [InlineData(17, "Sword (abc) 34|1,2,3,4", "CreatedCustom: Sword (abc) 34|1,2,3,4")]
        [InlineData(18, "", "BuyFromVendor")]
        [InlineData(18, "Health Potion (12) x3 (900 gold", "BuyFromVendor: Health Potion (12) x3 (900 gold")]
        [InlineData(18, "Health Potion (12) x3 (900 gold) extra)", "BuyFromVendor: Health Potion (12) x3 (900 gold) extra)")]
        [InlineData(18, "Health Potion (12) x3 (900)", "BuyFromVendor: Health Potion (12) x3 (900)")]
        [InlineData(18, "Health Potion (abc) x3 (900 gold)", "BuyFromVendor: Health Potion (abc) x3 (900 gold)")]
        [InlineData(18, "Health Potion (12) x3 (99999999999999999999 gold)", "BuyFromVendor: Health Potion (12) x3 (99999999999999999999 gold)")]
        [InlineData(19, "Health Potion (12) x3 (900 gold", "SellToVendor: Health Potion (12) x3 (900 gold")]
        [InlineData(10002, "", "ClassChange")]
        [InlineData(10002, "2", "ClassChange: 2")]
        [InlineData(10002, "2 3", "ClassChange: 2 3")]
        [InlineData(10003, "", "GiveExperience")]
        [InlineData(10003, "garbage", "GiveExperience: garbage")]
        [InlineData(10003, " to 2", "GiveExperience:  to 2")]
        [InlineData(10003, "abc to 2", "GiveExperience: abc to 2")]
        [InlineData(10003, "99999999999999999999 to 2", "GiveExperience: 99999999999999999999 to 2")]
        [InlineData(10004, "abc to 2", "GiveGold: abc to 2")]
        [InlineData(20, "", "Rebirth")]
        [InlineData(20, "junk", "Rebirth: junk")]
        [InlineData(20, "Rebirth: ", "Rebirth: Rebirth: ")]
        [InlineData(20, "Rebirth: abc experience -> 5 spirit", "Rebirth: Rebirth: abc experience -> 5 spirit")]
        [InlineData(20, "Rebirth: 10 experience -> abc spirit", "Rebirth: Rebirth: 10 experience -> abc spirit")]
        [InlineData(20, "Rebirth: 10 experience -> 5", "Rebirth: Rebirth: 10 experience -> 5")]
        [InlineData(21, "", "BuyGold")]
        [InlineData(21, "junk", "BuyGold: junk")]
        [InlineData(21, "BuyGold: 10 spirit -> gold, spirit 1 -> 0", "BuyGold: BuyGold: 10 spirit -> gold, spirit 1 -> 0")]
        [InlineData(22, "BuyExperience: abc spirit -> 100 exp, spirit 1 -> 0", "BuyExperience: BuyExperience: abc spirit -> 100 exp, spirit 1 -> 0")]
        [InlineData(23, "", "GiveSpirit")]
        [InlineData(23, "junk", "GiveSpirit: junk")]
        [InlineData(23, "GiveSpirit: sent 50 spirit to Bob", "GiveSpirit: GiveSpirit: sent 50 spirit to Bob")]
        [InlineData(24, "", "ResetItem")]
        [InlineData(24, "junk", "ResetItem: junk")]
        [InlineData(24, "ResetItem: template 12", "ResetItem: ResetItem: template 12")]
        public void Malformed_text_falls_back_to_labeled_text(long type, string text, string expected)
        {
            var e = LogFormatter.Project(Row(type, 1, 0, text));

            Assert.Equal(expected, e.Summary);
        }

        [Fact]
        public void Malformed_text_fallback_keeps_otherid_based_related()
        {
            var vendor = LogFormatter.Project(Row(18, 1, 100, "Health Potion (12) x3 (900 gold"));
            Assert.Equal("BuyFromVendor: Health Potion (12) x3 (900 gold", vendor.Summary);
            AssertRelated("Merchant", LogEntityKind.NpcTemplate, 100, "Merchant", true, vendor);

            var custom = LogFormatter.Project(Row(17, 7, 77, "Sword (12"));
            Assert.Equal("CreatedCustom: Sword (12", custom.Summary);
            AssertRelated("Item", LogEntityKind.Item, 77, null, true, custom);

            var classChange = LogFormatter.Project(Row(10002, 7, 2, "2 3"));
            Assert.Equal("ClassChange: 2 3", classChange.Summary);
            AssertRelated("Target", LogEntityKind.Player, 2, "Bob", true, classChange);
        }

        [Fact]
        public void Malformed_pickup_and_getitem_have_no_related()
        {
            Assert.Null(LogFormatter.Project(Row(11, 1, 0, "77 12 Iron Sword x")).Related);
            Assert.Null(LogFormatter.Project(Row(11, 1, 0, "")).Related);
            Assert.Null(LogFormatter.Project(Row(10001, 7, 0, "Sword x 3")).Related);
        }

        [Fact]
        public void Extreme_raw_fields_never_throw_and_retain_raw_values()
        {
            var e = LogFormatter.Project(Row(0, long.MinValue, long.MaxValue, "hi",
                mapId: long.MinValue, mapX: long.MaxValue, mapY: long.MinValue));

            Assert.Equal("Player #-9223372036854775808 said \"hi\"", e.Summary);
            Assert.Null(e.Related);
        }

        [Fact]
        public void Null_integer_fields_render_as_placeholders_in_unknown_fallback()
        {
            var e = LogFormatter.Project(Row(999, null, null, "", mapId: null, mapX: null, mapY: null));

            Assert.Equal("Unknown event #999 (player -, other -, map - at -, -)", e.Summary);
            Assert.Null(e.Related);
        }

        [Fact]
        public void Empty_lookup_name_falls_back_to_stable_id()
        {
            var names = new Dictionary<int, string> { [1] = "Alice", [2] = "" }.AsReadOnly();
            var e = LogFormatter.Project(Row(13, 1, 2, "hi", playerNames: names));

            Assert.Equal("Alice told Player #2 \"hi\"", e.Summary);
            AssertRelated("Recipient", LogEntityKind.Player, 2, null, true, e);
        }
    }
}
