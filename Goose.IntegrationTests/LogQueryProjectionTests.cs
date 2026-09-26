using Goose.Logs;
using Xunit;

namespace Goose.IntegrationTests
{
    public class LogQueryProjectionTests
    {
        private static readonly long StartTicks = DateTime.UnixEpoch.Ticks + 10_000_000_000L;
        private static readonly long EndTicks = StartTicks + TimeSpan.TicksPerDay;
        private const long SnapshotCeiling = 1_000_000_000L;

        private static long TicksAfterStart(long seconds) => StartTicks + seconds * TimeSpan.TicksPerSecond;

        private static LogQueryFixture CreateFixture()
        {
            var fixture = new LogQueryFixture();
            fixture.InsertPlayer(1, "Alice");
            fixture.InsertPlayer(2, "Bob");
            fixture.InsertPlayer(7, "GM");
            fixture.InsertPlayer(42, "Carol");
            fixture.InsertGuild(5, "Knights");
            fixture.InsertNpcTemplate(100, "Merchant");
            fixture.InsertMap(3, "Town Square");
            return fixture;
        }

        private static LogQueryRow SingleRow(LogQueryFixture fixture, long rowId)
        {
            var rows = fixture.Query(LogQueryFixture.SearchQuery(StartTicks, EndTicks), SnapshotCeiling);
            return rows.Single(r => r.RowId == rowId);
        }

        [Fact]
        public void Current_primary_and_related_names_resolve_with_stable_ids()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 13, 1, 2, 0, 0, 0, "Meet me in town");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.Equal("Tell", row.TypeLabel);
            Assert.Equal("Communication", row.GroupLabel);
            Assert.Equal("Alice told Bob \"Meet me in town\"", row.Summary);
            Assert.NotNull(row.Primary);
            Assert.Equal("Player", row.Primary!.Label);
            Assert.Equal(LogEntityKind.Player, row.Primary.Kind);
            Assert.Equal(1, row.Primary.Id);
            Assert.Equal("Alice", row.Primary.Name);
            Assert.True(row.Primary.CanQuickFilter);
            Assert.NotNull(row.Related);
            Assert.Equal("Recipient", row.Related!.Label);
            Assert.Equal(LogEntityKind.Player, row.Related.Kind);
            Assert.Equal(2, row.Related.Id);
            Assert.Equal("Bob", row.Related.Name);
            Assert.True(row.Related.CanQuickFilter);
        }

        [Fact]
        public void Renamed_player_reflects_new_name_while_id_stays_stable()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 13, 1, 2, 0, 0, 0, "hi");

            LogQueryRow before = SingleRow(fixture, rowId);
            Assert.Equal("Alice", before.Primary!.Name);

            fixture.RenamePlayer(1, "Alice Renamed");

            LogQueryRow after = SingleRow(fixture, rowId);
            Assert.Equal(1, after.Primary!.Id);
            Assert.Equal("Alice Renamed", after.Primary.Name);
            Assert.Equal("Alice Renamed told Bob \"hi\"", after.Summary);
        }

        [Fact]
        public void Deleted_player_falls_back_to_numeric_id()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 13, 1, 2, 0, 0, 0, "hi");

            fixture.DeletePlayer(1);

            LogQueryRow row = SingleRow(fixture, rowId);
            Assert.Equal(1, row.Primary!.Id);
            Assert.Null(row.Primary.Name);
            Assert.True(row.Primary.CanQuickFilter);
            Assert.Equal("Player #1 told Bob \"hi\"", row.Summary);
        }

        [Fact]
        public void Missing_and_out_of_domain_ids_fall_back_numerically()
        {
            using var fixture = CreateFixture();
            long missing = fixture.InsertLog(TicksAfterStart(10), 0, 999_999, 0, 0, 0, 0, "hi");
            long outOfDomain = fixture.InsertLog(TicksAfterStart(20), 0, 5_000_000_000L, 0, 0, 0, 0, "hi");

            LogQueryRow missingRow = SingleRow(fixture, missing);
            Assert.Equal(999_999, missingRow.Primary!.Id);
            Assert.Null(missingRow.Primary.Name);
            Assert.True(missingRow.Primary.CanQuickFilter);

            LogQueryRow outOfDomainRow = SingleRow(fixture, outOfDomain);
            Assert.Equal(5_000_000_000L, outOfDomainRow.Primary!.Id);
            Assert.Null(outOfDomainRow.Primary.Name);
            Assert.False(outOfDomainRow.Primary.CanQuickFilter);
        }

        [Fact]
        public void GuildChat_resolves_the_guild_from_otherid()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 7, 1, 5, 0, 0, 0, "hello");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.Equal("Alice said in Knights \"hello\"", row.Summary);
            Assert.Equal("Guild", row.Related!.Label);
            Assert.Equal(LogEntityKind.Guild, row.Related.Kind);
            Assert.Equal(5, row.Related.Id);
            Assert.Equal("Knights", row.Related.Name);
            Assert.True(row.Related.CanQuickFilter);
        }

        [Fact]
        public void Vendor_and_spawn_events_resolve_the_npc_template()
        {
            using var fixture = CreateFixture();
            long buy = fixture.InsertLog(TicksAfterStart(10), 18, 1, 100, 0, 0, 0, "Health Potion (12) x3 (900 gold)");
            long spawn = fixture.InsertLog(TicksAfterStart(20), 10006, 7, 100, 0, 0, 0, "");

            LogQueryRow buyRow = SingleRow(fixture, buy);
            Assert.Equal("Alice bought 3 Health Potion from Merchant", buyRow.Summary);
            Assert.Equal("Merchant", buyRow.Related!.Label);
            Assert.Equal(LogEntityKind.NpcTemplate, buyRow.Related.Kind);
            Assert.Equal(100, buyRow.Related.Id);
            Assert.Equal("Merchant", buyRow.Related.Name);
            Assert.True(buyRow.Related.CanQuickFilter);

            LogQueryRow spawnRow = SingleRow(fixture, spawn);
            Assert.Equal("GM spawned Merchant", spawnRow.Summary);
            Assert.Equal("NPC", spawnRow.Related!.Label);
            Assert.Equal(100, spawnRow.Related.Id);
            Assert.Equal("Merchant", spawnRow.Related.Name);
        }

        [Fact]
        public void Map_reference_resolves_independently_of_otherid()
        {
            using var fixture = CreateFixture();
            long chat = fixture.InsertLog(TicksAfterStart(10), 0, 1, 0, 3, 10, 20, "chat on map");
            long respawn = fixture.InsertLog(TicksAfterStart(20), 10005, 7, 999, 3, 0, 0, "");

            LogQueryRow chatRow = SingleRow(fixture, chat);
            Assert.Equal("Map", chatRow.Map!.Label);
            Assert.Equal(LogEntityKind.Map, chatRow.Map.Kind);
            Assert.Equal(3, chatRow.Map.Id);
            Assert.Equal("Town Square", chatRow.Map.Name);
            Assert.True(chatRow.Map.CanQuickFilter);

            LogQueryRow respawnRow = SingleRow(fixture, respawn);
            Assert.Equal(3, respawnRow.Map!.Id);
            Assert.Equal("Town Square", respawnRow.Map.Name);
            Assert.Equal(999, respawnRow.OtherId);
        }

        [Fact]
        public void PickupItem_projects_exact_structured_event()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 11, 1, 0, 0, 0, 0, "77 12 Iron Sword 3");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.Equal("ItemsEconomy", row.GroupLabel);
            Assert.Equal("Alice picked up Iron Sword ×3", row.Summary);
            Assert.Equal("Item", row.Related!.Label);
            Assert.Equal(LogEntityKind.Item, row.Related.Kind);
            Assert.Equal(77, row.Related.Id);
            Assert.Equal("Iron Sword", row.Related.Name);
            Assert.True(row.Related.CanQuickFilter);
        }

        [Fact]
        public void PlayerDropItem_projects_exact_structured_event()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 12, 1, 0, 0, 0, 0, "77 12 Iron Sword 3");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.Equal("Alice dropped Iron Sword ×3", row.Summary);
            Assert.Equal("Item", row.Related!.Label);
            Assert.Equal(77, row.Related.Id);
            Assert.Equal("Iron Sword", row.Related.Name);
            Assert.True(row.Related.CanQuickFilter);
        }

        [Fact]
        public void GetItem_projects_exact_structured_event()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 10001, 7, 0, 0, 0, 0, "Iron Sword 77 3");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.Equal("GM gave Iron Sword ×3", row.Summary);
            Assert.Equal("Item", row.Related!.Label);
            Assert.Equal(77, row.Related.Id);
            Assert.Equal("Iron Sword", row.Related.Name);
            Assert.True(row.Related.CanQuickFilter);
        }

        [Fact]
        public void CreatedCustom_projects_exact_structured_event()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 17, 7, 77, 0, 0, 0, "Custom Sword (12) 34|255,0,0,255");
            long oversized = fixture.InsertLog(TicksAfterStart(20), 17, 7, long.MaxValue, 0, 0, 0,
                "Custom Sword (12) 34|255,0,0,255");

            LogQueryRow row = SingleRow(fixture, rowId);
            Assert.Equal("GM created custom item Custom Sword (template 12)", row.Summary);
            Assert.Equal("Item", row.Related!.Label);
            Assert.Equal(77, row.Related.Id);
            Assert.Equal("Custom Sword", row.Related.Name);
            Assert.True(row.Related.CanQuickFilter);

            LogQueryRow oversizedRow = SingleRow(fixture, oversized);
            Assert.Equal(long.MaxValue, oversizedRow.OtherId);
            Assert.Equal(long.MaxValue, oversizedRow.Related!.Id);
            Assert.False(oversizedRow.Related.CanQuickFilter);
        }

        [Fact]
        public void ResetItem_projects_exact_structured_event()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 24, 1, 77, 0, 0, 0,
                "ResetItem: template 12 dim 3 cost 500 spirit balance 1000 -> 500");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.Equal("Alice reset an item (template 12, dimension 3) for 500 spirit", row.Summary);
            Assert.Equal("Item", row.Related!.Label);
            Assert.Equal(77, row.Related.Id);
            Assert.Null(row.Related.Name);
            Assert.True(row.Related.CanQuickFilter);
        }

        [Fact]
        public void RespawnMap_projects_exact_structured_event()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 10005, 7, 999, 3, 0, 0, "");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.Equal("GM respawned Town Square", row.Summary);
            Assert.Equal("Map", row.Related!.Label);
            Assert.Equal(LogEntityKind.Map, row.Related.Kind);
            Assert.Equal(3, row.Related.Id);
            Assert.Equal("Town Square", row.Related.Name);
            Assert.True(row.Related.CanQuickFilter);
        }

        [Fact]
        public void Vendor_events_project_exact_structured_events()
        {
            using var fixture = CreateFixture();
            long buy = fixture.InsertLog(TicksAfterStart(10), 18, 1, 100, 0, 0, 0, "Health Potion (12) x3 (900 gold)");
            long sell = fixture.InsertLog(TicksAfterStart(20), 19, 1, 100, 0, 0, 0, "Health Potion (12) x3 (900 gold)");

            LogQueryRow buyRow = SingleRow(fixture, buy);
            Assert.Equal("Alice bought 3 Health Potion from Merchant", buyRow.Summary);
            Assert.Equal("Merchant", buyRow.Related!.Label);
            Assert.Equal(LogEntityKind.NpcTemplate, buyRow.Related.Kind);
            Assert.Equal(100, buyRow.Related.Id);
            Assert.Equal("Merchant", buyRow.Related.Name);
            Assert.True(buyRow.Related.CanQuickFilter);

            LogQueryRow sellRow = SingleRow(fixture, sell);
            Assert.Equal("Alice sold 3 Health Potion to Merchant", sellRow.Summary);
            Assert.Equal("Merchant", sellRow.Related!.Label);
            Assert.Equal(100, sellRow.Related.Id);
        }

        [Fact]
        public void Unknown_row_projects_fallback_summary_and_stored_other_id()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), 999, 1, 2, 0, 10, 20, "mystery");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.Equal("Unknown event", row.TypeLabel);
            Assert.Equal("OtherRetired", row.GroupLabel);
            Assert.Equal("Unknown event #999 (player 1, other 2, map 0 at 10, 20): mystery", row.Summary);
            Assert.Equal("Stored other ID", row.Related!.Label);
            Assert.Equal(LogEntityKind.StoredValue, row.Related.Kind);
            Assert.Equal(2, row.Related.Id);
            Assert.Null(row.Related.Name);
            Assert.False(row.Related.CanQuickFilter);
        }

        [Fact]
        public void Raw_int64_fields_survive_unchanged_for_integer_storage()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertLog(TicksAfterStart(10), long.MaxValue, long.MinValue, long.MaxValue,
                70_000, -70_000, long.MaxValue, "extremes");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.True(row.TypeIsInteger);
            Assert.Equal(long.MaxValue, row.Type);
            Assert.True(row.PlayerIdIsInteger);
            Assert.Equal(long.MinValue, row.PlayerId);
            Assert.True(row.OtherIdIsInteger);
            Assert.Equal(long.MaxValue, row.OtherId);
            Assert.True(row.MapIdIsInteger);
            Assert.Equal(70_000, row.MapId);
            Assert.True(row.MapXIsInteger);
            Assert.Equal(-70_000, row.MapX);
            Assert.True(row.MapYIsInteger);
            Assert.Equal(long.MaxValue, row.MapY);
            Assert.Equal("Unknown event", row.TypeLabel);
            Assert.Null(row.Primary);
            Assert.Equal(long.MaxValue, row.Related!.Id);
            Assert.False(row.Related.CanQuickFilter);
            Assert.Equal(70_000, row.Map!.Id);
            Assert.Null(row.Map.Name);
            Assert.True(row.Map.CanQuickFilter);
        }

        [Fact]
        public void Malformed_numeric_text_uses_fallback_without_losing_original_text()
        {
            using var fixture = CreateFixture();
            long rowId = fixture.InsertRawLog(TicksAfterStart(10), 0, "abc", 0L, 0L, 0L, 0L, "hello there");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.False(row.PlayerIdIsInteger);
            Assert.Equal(0, row.PlayerId);
            Assert.Null(row.Primary);
            Assert.Equal("Player #? said \"hello there\"", row.Summary);
            Assert.Equal("hello there", row.Text);
        }

        [Fact]
        public void OtherIdKind_exposes_descriptor_kind_independently_of_related_entity()
        {
            using var fixture = CreateFixture();
            long ban = fixture.InsertLog(TicksAfterStart(10), 10010, 7, 1, 0, 0, 0, "");
            long guildChat = fixture.InsertLog(TicksAfterStart(20), 7, 1, 5, 0, 0, 0, "hello");
            long spawn = fixture.InsertLog(TicksAfterStart(30), 10006, 7, 100, 0, 0, 0, "");
            long created = fixture.InsertLog(TicksAfterStart(40), 17, 7, 77, 0, 0, 0, "Custom Sword (12) 34|255,0,0,255");
            long chat = fixture.InsertLog(TicksAfterStart(50), 0, 1, 0, 0, 0, 0, "hi");
            long unknown = fixture.InsertLog(TicksAfterStart(60), 999, 1, 2, 0, 10, 20, "mystery");
            long nonIntegerType = fixture.InsertRawLog(TicksAfterStart(70), "abc", 0L, 0L, 0L, 0L, 0L, "hello there");

            Assert.Equal(LogOtherIdKind.Player, SingleRow(fixture, ban).OtherIdKind);
            Assert.Equal(LogOtherIdKind.Guild, SingleRow(fixture, guildChat).OtherIdKind);
            Assert.Equal(LogOtherIdKind.NpcTemplate, SingleRow(fixture, spawn).OtherIdKind);
            Assert.Equal(LogOtherIdKind.Item, SingleRow(fixture, created).OtherIdKind);
            Assert.Equal(LogOtherIdKind.Unused, SingleRow(fixture, chat).OtherIdKind);
            Assert.Equal(LogOtherIdKind.Unused, SingleRow(fixture, unknown).OtherIdKind);
            Assert.Equal(LogOtherIdKind.Unused, SingleRow(fixture, nonIntegerType).OtherIdKind);
        }

        [Fact]
        public void Row_exposes_utc_ticks_and_original_text()
        {
            using var fixture = CreateFixture();
            long ticks = TicksAfterStart(123);
            long rowId = fixture.InsertLog(ticks, 0, 1, 0, 0, 0, 0, "original text");

            LogQueryRow row = SingleRow(fixture, rowId);

            Assert.Equal(ticks, row.UtcTicks);
            Assert.Equal("original text", row.Text);
        }
    }
}
