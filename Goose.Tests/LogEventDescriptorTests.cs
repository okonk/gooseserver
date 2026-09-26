using Goose;
using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    public class LogEventDescriptorTests
    {
        [Theory]
        [InlineData(0, "Chat", LogEventGroup.Communication, LogOtherIdKind.Unused)]
        [InlineData(1, "Shout", LogEventGroup.Communication, LogOtherIdKind.Unused)]
        [InlineData(2, "Auction", LogEventGroup.Communication, LogOtherIdKind.Unused)]
        [InlineData(3, "JoinGame", LogEventGroup.SessionsSecurity, LogOtherIdKind.Unused)]
        [InlineData(4, "LeaveGame", LogEventGroup.SessionsSecurity, LogOtherIdKind.Unused)]
        [InlineData(5, "JoinGuild", LogEventGroup.Social, LogOtherIdKind.Player)]
        [InlineData(6, "LeaveGuild", LogEventGroup.Social, LogOtherIdKind.Player)]
        [InlineData(7, "GuildChat", LogEventGroup.Communication, LogOtherIdKind.Guild)]
        [InlineData(8, "JoinGroup", LogEventGroup.Social, LogOtherIdKind.Player)]
        [InlineData(9, "LeaveGroup", LogEventGroup.Social, LogOtherIdKind.Player)]
        [InlineData(10, "GroupChat", LogEventGroup.Communication, LogOtherIdKind.Unused)]
        [InlineData(11, "PickupItem", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused)]
        [InlineData(12, "PlayerDropItem", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused)]
        [InlineData(13, "Tell", LogEventGroup.Communication, LogOtherIdKind.Player)]
        [InlineData(14, "Received Credits (Retired)", LogEventGroup.OtherRetired, LogOtherIdKind.Unused)]
        [InlineData(15, "GaveCredits", LogEventGroup.ItemsEconomy, LogOtherIdKind.Player)]
        [InlineData(16, "InvalidPassword", LogEventGroup.SessionsSecurity, LogOtherIdKind.Unused)]
        [InlineData(17, "CreatedCustom", LogEventGroup.ItemsEconomy, LogOtherIdKind.Item)]
        [InlineData(18, "BuyFromVendor", LogEventGroup.ItemsEconomy, LogOtherIdKind.NpcTemplate)]
        [InlineData(19, "SellToVendor", LogEventGroup.ItemsEconomy, LogOtherIdKind.NpcTemplate)]
        [InlineData(20, "Rebirth", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused)]
        [InlineData(21, "BuyGold", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused)]
        [InlineData(22, "BuyExperience", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused)]
        [InlineData(23, "GiveSpirit", LogEventGroup.ItemsEconomy, LogOtherIdKind.Player)]
        [InlineData(24, "ResetItem", LogEventGroup.ItemsEconomy, LogOtherIdKind.Item)]
        [InlineData(10001, "GetItem", LogEventGroup.GmActions, LogOtherIdKind.Unused)]
        [InlineData(10002, "ClassChange", LogEventGroup.GmActions, LogOtherIdKind.Player)]
        [InlineData(10003, "GiveExperience", LogEventGroup.GmActions, LogOtherIdKind.Player)]
        [InlineData(10004, "GiveGold", LogEventGroup.GmActions, LogOtherIdKind.Player)]
        [InlineData(10005, "RespawnMap", LogEventGroup.GmActions, LogOtherIdKind.Unused)]
        [InlineData(10006, "SpawnedNPC", LogEventGroup.GmActions, LogOtherIdKind.NpcTemplate)]
        [InlineData(10007, "MacroCheck", LogEventGroup.GmActions, LogOtherIdKind.Player)]
        [InlineData(10008, "MacroCheckConfirm", LogEventGroup.GmActions, LogOtherIdKind.Unused)]
        [InlineData(10009, "MacroCheckFailed", LogEventGroup.GmActions, LogOtherIdKind.Unused)]
        [InlineData(10010, "Ban", LogEventGroup.GmActions, LogOtherIdKind.Player)]
        [InlineData(10011, "Kick", LogEventGroup.GmActions, LogOtherIdKind.Player)]
        [InlineData(10012, "SetPassword", LogEventGroup.GmActions, LogOtherIdKind.Player)]
        [InlineData(10013, "ViewLogs", LogEventGroup.GmActions, LogOtherIdKind.Unused)]
        public void Descriptor_matrix_pins_group_label_and_other_id_kind(int id, string label,
            LogEventGroup group, LogOtherIdKind otherIdKind)
        {
            Assert.True(LogEventRegistry.TryGetKnown(id, out var descriptor));
            Assert.Equal(id, descriptor.Id);
            Assert.Equal(label, descriptor.Label);
            Assert.Equal(group, descriptor.Group);
            Assert.Equal(otherIdKind, descriptor.OtherIdKind);
        }

        [Fact]
        public void Every_Log_Types_value_and_retired_14_has_exactly_one_descriptor()
        {
            var enumIds = Enum.GetValues<Log.Types>().Select(t => (int)t).ToList();

            Assert.All(enumIds, id => Assert.True(LogEventRegistry.TryGetKnown(id, out _)));
            Assert.Contains(14, LogEventRegistry.Known.Select(d => d.Id));
            Assert.Equal(enumIds.Count + 1, LogEventRegistry.Known.Count);
            Assert.Equal(LogEventRegistry.Known.Count,
                LogEventRegistry.Known.Select(d => d.Id).Distinct().Count());
        }

        [Fact]
        public void Known_is_id_ordered_with_unique_ids_and_nonempty_labels()
        {
            var ids = LogEventRegistry.Known.Select(d => d.Id).ToList();

            for (int i = 1; i < ids.Count; i++)
            {
                Assert.True(ids[i - 1] < ids[i]);
            }

            Assert.All(LogEventRegistry.Known, d => Assert.False(string.IsNullOrEmpty(d.Label)));
        }

        [Fact]
        public void PlayerValuedOtherTypeIds_equals_the_fixed_player_valued_set()
        {
            Assert.Equal(
                new[] { 5, 6, 8, 9, 13, 15, 23, 10002, 10003, 10004, 10007, 10010, 10011, 10012 },
                LogEventRegistry.PlayerValuedOtherTypeIds.OrderBy(id => id).ToArray());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(25)]
        [InlineData(10000)]
        [InlineData(10014)]
        [InlineData(2147483648L)]
        [InlineData(9223372036854775807L)]
        public void Unknown_raw_type_values_return_the_generic_other_retired_descriptor(long id)
        {
            Assert.False(LogEventRegistry.TryGetKnown(id, out _));

            var descriptor = LogEventRegistry.Get(id);

            Assert.Equal(LogEventGroup.OtherRetired, descriptor.Group);
            Assert.Equal(LogOtherIdKind.Unused, descriptor.OtherIdKind);
        }

        [Fact]
        public void Get_agrees_with_TryGetKnown_for_every_known_id()
        {
            foreach (var descriptor in LogEventRegistry.Known)
            {
                Assert.Same(descriptor, LogEventRegistry.Get(descriptor.Id));
            }
        }

        [Fact]
        public void Retired_14_is_not_a_Log_Types_enum_value()
        {
            Assert.DoesNotContain(typeof(Log.Types).GetFields(),
                f => f.Name == "ReceivedCredits" && (int)f.GetRawConstantValue()! == 14);
        }
    }
}
