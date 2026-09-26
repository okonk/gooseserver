using System.Collections.Immutable;

namespace Goose.Logs
{
    public static class LogEventRegistry
    {
        private static readonly LogEventDescriptor[] _known =
        {
            new(0, "Chat", LogEventGroup.Communication, LogOtherIdKind.Unused),
            new(1, "Shout", LogEventGroup.Communication, LogOtherIdKind.Unused),
            new(2, "Auction", LogEventGroup.Communication, LogOtherIdKind.Unused),
            new(3, "JoinGame", LogEventGroup.SessionsSecurity, LogOtherIdKind.Unused),
            new(4, "LeaveGame", LogEventGroup.SessionsSecurity, LogOtherIdKind.Unused),
            new(5, "JoinGuild", LogEventGroup.Social, LogOtherIdKind.Player),
            new(6, "LeaveGuild", LogEventGroup.Social, LogOtherIdKind.Player),
            new(7, "GuildChat", LogEventGroup.Communication, LogOtherIdKind.Guild),
            new(8, "JoinGroup", LogEventGroup.Social, LogOtherIdKind.Player),
            new(9, "LeaveGroup", LogEventGroup.Social, LogOtherIdKind.Player),
            new(10, "GroupChat", LogEventGroup.Communication, LogOtherIdKind.Unused),
            new(11, "PickupItem", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused),
            new(12, "PlayerDropItem", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused),
            new(13, "Tell", LogEventGroup.Communication, LogOtherIdKind.Player),
            new(14, "Received Credits (Retired)", LogEventGroup.OtherRetired, LogOtherIdKind.Unused),
            new(15, "GaveCredits", LogEventGroup.ItemsEconomy, LogOtherIdKind.Player),
            new(16, "InvalidPassword", LogEventGroup.SessionsSecurity, LogOtherIdKind.Unused),
            new(17, "CreatedCustom", LogEventGroup.ItemsEconomy, LogOtherIdKind.Item),
            new(18, "BuyFromVendor", LogEventGroup.ItemsEconomy, LogOtherIdKind.NpcTemplate),
            new(19, "SellToVendor", LogEventGroup.ItemsEconomy, LogOtherIdKind.NpcTemplate),
            new(20, "Rebirth", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused),
            new(21, "BuyGold", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused),
            new(22, "BuyExperience", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused),
            new(23, "GiveSpirit", LogEventGroup.ItemsEconomy, LogOtherIdKind.Player),
            new(24, "ResetItem", LogEventGroup.ItemsEconomy, LogOtherIdKind.Item),
            new(10001, "GetItem", LogEventGroup.GmActions, LogOtherIdKind.Unused),
            new(10002, "ClassChange", LogEventGroup.GmActions, LogOtherIdKind.Player),
            new(10003, "GiveExperience", LogEventGroup.GmActions, LogOtherIdKind.Player),
            new(10004, "GiveGold", LogEventGroup.GmActions, LogOtherIdKind.Player),
            new(10005, "RespawnMap", LogEventGroup.GmActions, LogOtherIdKind.Unused),
            new(10006, "SpawnedNPC", LogEventGroup.GmActions, LogOtherIdKind.NpcTemplate),
            new(10007, "MacroCheck", LogEventGroup.GmActions, LogOtherIdKind.Player),
            new(10008, "MacroCheckConfirm", LogEventGroup.GmActions, LogOtherIdKind.Unused),
            new(10009, "MacroCheckFailed", LogEventGroup.GmActions, LogOtherIdKind.Unused),
            new(10010, "Ban", LogEventGroup.GmActions, LogOtherIdKind.Player),
            new(10011, "Kick", LogEventGroup.GmActions, LogOtherIdKind.Player),
            new(10012, "SetPassword", LogEventGroup.GmActions, LogOtherIdKind.Player),
            new(10013, "ViewLogs", LogEventGroup.GmActions, LogOtherIdKind.Unused),
        };

        private static readonly Dictionary<int, LogEventDescriptor> KnownById =
            _known.ToDictionary(d => d.Id);

        public static IReadOnlyList<LogEventDescriptor> Known { get; } = _known;

        public static LogEventDescriptor Unknown { get; } =
            new(int.MinValue, "Unknown event", LogEventGroup.OtherRetired, LogOtherIdKind.Unused);

        public static IReadOnlyCollection<int> PlayerValuedOtherTypeIds { get; } =
            _known
                .Where(d => d.OtherIdKind == LogOtherIdKind.Player)
                .Select(d => d.Id)
                .ToImmutableHashSet();

        public static bool TryGetKnown(long id, out LogEventDescriptor descriptor)
        {
            if (id is >= int.MinValue and <= int.MaxValue
                && KnownById.TryGetValue((int)id, out LogEventDescriptor? found) && found is not null)
            {
                descriptor = found;
                return true;
            }

            descriptor = Unknown;
            return false;
        }

        public static LogEventDescriptor Get(long id)
        {
            return TryGetKnown(id, out LogEventDescriptor descriptor) ? descriptor : Unknown;
        }
    }
}
