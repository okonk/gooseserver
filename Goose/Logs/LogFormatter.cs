using System.Globalization;
using System.Text.RegularExpressions;

namespace Goose.Logs
{
    public sealed class LogFormatContext
    {
        private static readonly IReadOnlyDictionary<int, string> Empty = new Dictionary<int, string>();

        public long RowId { get; }
        public long? DateTicks { get; }
        public long Type { get; }
        public long? PlayerId { get; }
        public long? OtherId { get; }
        public long? MapId { get; }
        public long? MapX { get; }
        public long? MapY { get; }
        public string Text { get; }
        public IReadOnlyDictionary<int, string> PlayerNames { get; }
        public IReadOnlyDictionary<int, string> GuildNames { get; }
        public IReadOnlyDictionary<int, string> NpcTemplateNames { get; }
        public IReadOnlyDictionary<int, string> MapNames { get; }

        public LogFormatContext(long rowId, long? dateTicks, long type, long? playerId, long? otherId,
            long? mapId, long? mapX, long? mapY, string text,
            IReadOnlyDictionary<int, string>? playerNames = null,
            IReadOnlyDictionary<int, string>? guildNames = null,
            IReadOnlyDictionary<int, string>? npcTemplateNames = null,
            IReadOnlyDictionary<int, string>? mapNames = null)
        {
            this.RowId = rowId;
            this.DateTicks = dateTicks;
            this.Type = type;
            this.PlayerId = playerId;
            this.OtherId = otherId;
            this.MapId = mapId;
            this.MapX = mapX;
            this.MapY = mapY;
            this.Text = text ?? "";
            this.PlayerNames = playerNames ?? Empty;
            this.GuildNames = guildNames ?? Empty;
            this.NpcTemplateNames = npcTemplateNames ?? Empty;
            this.MapNames = mapNames ?? Empty;
        }
    }

    public static class LogFormatter
    {
        private static readonly Regex VendorText = new(
            @"^(?<name>.*) \((?<template>\d+)\) x(?<stack>\d+) \((?<cost>\d+) (?<currency>[^)]+)\)$",
            RegexOptions.Compiled);

        public static LogFormattedEvent Project(LogFormatContext c)
        {
            return new LogFormattedEvent(Summary(c), Related(c));
        }

        private static string Summary(LogFormatContext c) => c.Type switch
        {
            0 => Say(c, "said"),
            1 => Say(c, "shouted"),
            2 => Say(c, "auctioned"),
            10 => Say(c, "said in group"),
            7 => $"{Player(c)} said in {GuildRef(c, c.OtherId)} \"{c.Text}\"",
            13 => $"{Player(c)} told {Target(c)} \"{c.Text}\"",
            3 => c.Text.Length == 0 ? $"{Player(c)} joined" : $"{Player(c)} joined from {c.Text}",
            4 => $"{Player(c)} left",
            5 => c.OtherId is > 0
                ? $"{Player(c)} joined {GuildRef(c, TextId(c.Text))} (invited by {Target(c)})"
                : $"{Player(c)} joined {GuildRef(c, TextId(c.Text))}",
            6 => c.OtherId is > 0
                ? $"{Player(c)} left {GuildRef(c, TextId(c.Text))}, removed by {Target(c)}"
                : $"{Player(c)} left {GuildRef(c, TextId(c.Text))}",
            // playerid is the inviter and otherid the invited member (GroupAddCommand).
            8 => c.OtherId is > 0 ? $"{Player(c)} invited {Target(c)} to a group" : Fallback(c),
            9 => c.OtherId is > 0
                ? $"{Player(c)} left a group, removed by {Target(c)}"
                : $"{Player(c)} left a group",
            11 => ItemAction(c, "picked up"),
            12 => ItemAction(c, "dropped"),
            14 => c.Text.Length == 0 ? $"{Player(c)} received credits" : $"{Player(c)} received {c.Text} credits",
            15 => c.OtherId is > 0
                ? (c.Text.Length == 0 ? $"{Player(c)} gave credits to {Target(c)}" : $"{Player(c)} gave {c.Text} credits to {Target(c)}")
                : (c.Text.Length == 0 ? $"{Player(c)} gave credits" : $"{Player(c)} gave {c.Text} credits"),
            16 => c.Text.Length == 0 ? $"{Player(c)} failed login" : $"{Player(c)} failed login from {c.Text}",
            17 => CustomSummary(c),
            18 => VendorSummary(c, "bought", "from"),
            19 => VendorSummary(c, "sold", "to"),
            20 => RebirthSummary(c),
            21 => SpiritPurchase(c, "BuyGold: ", "gold", "gold"),
            22 => SpiritPurchase(c, "BuyExperience: ", "exp", "experience"),
            23 => GiveSpirit(c),
            24 => ResetItem(c),
            10001 => GetItemSummary(c),
            10002 => ClassChange(c),
            10003 => GiveAmount(c, "experience"),
            10004 => GiveAmount(c, "gold"),
            10005 => $"{Player(c)} respawned {MapRef(c)}",
            10006 => SpawnedNpc(c),
            10007 => $"{Player(c)} macro-checked {Target(c)}",
            10008 => $"{Player(c)} passed a macro check",
            10009 => $"{Player(c)} failed a macro check",
            10010 => $"{Player(c)} banned {Target(c)}",
            10011 => $"{Player(c)} kicked {Target(c)}",
            10012 => $"{Player(c)} set the password for {Target(c)}",
            10013 => c.Text.Length == 0 ? $"{Player(c)} viewed logs" : $"{Player(c)} viewed logs: {c.Text}",
            _ => UnknownSummary(c),
        };

        private static LogRelatedEntity? Related(LogFormatContext c)
        {
            if (!LogEventRegistry.TryGetKnown(c.Type, out _))
            {
                return c.OtherId is null or 0
                    ? null
                    : new LogRelatedEntity("Stored other ID", LogEntityKind.StoredValue, c.OtherId, null, false);
            }

            return c.Type switch
            {
                5 => PlayerRelated(c, "Invited by"),
                6 => PlayerRelated(c, "Removed by"),
                7 => EntityRelated(c.OtherId, "Guild", LogEntityKind.Guild, c.GuildNames),
                8 => PlayerRelated(c, "Member"),
                9 => PlayerRelated(c, "Removed by"),
                11 or 12 => ItemFromText(c),
                13 => PlayerRelated(c, "Recipient"),
                15 => PlayerRelated(c, "Recipient"),
                17 => ItemFromOtherId(c, CustomName(c)),
                18 or 19 => EntityRelated(c.OtherId, "Merchant", LogEntityKind.NpcTemplate, c.NpcTemplateNames),
                23 => PlayerRelated(c, SpiritDirection(c)),
                24 => ItemFromOtherId(c, null),
                10001 => GetItemRelated(c),
                10002 or 10003 or 10004 or 10007 or 10010 or 10011 or 10012 => PlayerRelated(c, "Target"),
                10005 => MapRelated(c),
                10006 => EntityRelated(c.OtherId, "NPC", LogEntityKind.NpcTemplate, c.NpcTemplateNames),
                _ => null,
            };
        }

        private static string Say(LogFormatContext c, string verb) => $"{Player(c)} {verb} \"{c.Text}\"";

        private static string ItemAction(LogFormatContext c, string verb)
        {
            var (isItem, isGold, id, stack, name) = ParseItemText(c.Text);
            if (isItem) return $"{Player(c)} {verb} {name} ×{stack}";
            if (isGold) return $"{Player(c)} {verb} {stack} gold";
            return Fallback(c);
        }

        private static LogRelatedEntity? ItemFromText(LogFormatContext c)
        {
            var (isItem, isGold, id, stack, name) = ParseItemText(c.Text);
            if (isItem) return new LogRelatedEntity("Item", LogEntityKind.Item, id, name, Quick(id));
            if (isGold) return new LogRelatedEntity("Gold", LogEntityKind.Gold, null, null, false);
            return null;
        }

        private static (bool Item, bool Gold, long Id, long Stack, string Name) ParseItemText(string text)
        {
            var tokens = text.Split(' ');
            if (tokens.Length == 2 && tokens[1] == "gold" && TryLong(tokens[0], out long gold))
            {
                return (false, true, 0, gold, "");
            }

            if (tokens.Length >= 4 && TryLong(tokens[0], out long id) && TryLong(tokens[1], out _)
                && TryLong(tokens[^1], out long stack))
            {
                return (true, false, id, stack, string.Join(" ", tokens[2..^1]));
            }

            return (false, false, 0, 0, "");
        }

        private static string CustomSummary(LogFormatContext c)
        {
            var (ok, name, template) = ParseCustomText(c.Text);
            return ok ? $"{Player(c)} created custom item {name} (template {template})" : Fallback(c);
        }

        private static string? CustomName(LogFormatContext c)
        {
            var (ok, name, _) = ParseCustomText(c.Text);
            return ok ? name : null;
        }

        private static (bool Ok, string Name, long Template) ParseCustomText(string text)
        {
            int open = text.LastIndexOf(" (", StringComparison.Ordinal);
            if (open <= 0) return (false, "", 0);
            int close = text.IndexOf(')', open);
            if (close < 0) return (false, "", 0);
            if (!TryLong(text[(open + 2)..close], out long template)) return (false, "", 0);
            return (true, text[..open], template);
        }

        private static LogRelatedEntity? ItemFromOtherId(LogFormatContext c, string? name)
        {
            if (c.OtherId is null or 0) return null;
            return new LogRelatedEntity("Item", LogEntityKind.Item, c.OtherId, name, Quick(c.OtherId));
        }

        private static string VendorSummary(LogFormatContext c, string verb, string prep)
        {
            var (ok, name, stack) = ParseVendorText(c.Text);
            return ok ? $"{Player(c)} {verb} {stack} {name} {prep} {NpcRef(c)}" : Fallback(c);
        }

        private static (bool Ok, string Name, long Stack) ParseVendorText(string text)
        {
            var m = VendorText.Match(text);
            if (m.Success && TryLong(m.Groups["stack"].Value, out long stack)
                && TryLong(m.Groups["cost"].Value, out _))
            {
                return (true, m.Groups["name"].Value, stack);
            }

            return (false, "", 0);
        }

        private static string RebirthSummary(LogFormatContext c)
        {
            const string prefix = "Rebirth: ";
            if (!c.Text.StartsWith(prefix, StringComparison.Ordinal)) return Fallback(c);
            string rest = c.Text[prefix.Length..];
            const string mid = " experience -> ";
            int i = rest.IndexOf(mid, StringComparison.Ordinal);
            if (i < 0) return Fallback(c);
            string total = rest[..i];
            string tail = rest[(i + mid.Length)..];
            const string suffix = " spirit";
            if (!tail.EndsWith(suffix, StringComparison.Ordinal)) return Fallback(c);
            string minted = tail[..^suffix.Length];
            if (!TryLong(total, out _) || !TryLong(minted, out _)) return Fallback(c);
            return $"{Player(c)} rebirthed: {total} experience -> {minted} spirit";
        }

        private static string SpiritPurchase(LogFormatContext c, string prefix, string textUnit, string summaryUnit)
        {
            if (!c.Text.StartsWith(prefix, StringComparison.Ordinal)) return Fallback(c);
            string rest = c.Text[prefix.Length..];
            int arrow = rest.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrow < 0) return Fallback(c);
            string head = rest[..arrow];
            string tail = rest[(arrow + 4)..];
            int unit = tail.IndexOf($" {textUnit},", StringComparison.Ordinal);
            if (unit < 0) return Fallback(c);
            string granted = tail[..unit];
            var headTokens = head.Split(' ');
            if (headTokens.Length < 2) return Fallback(c);
            string amount = headTokens[0];
            string spirit = string.Join(" ", headTokens[1..]);
            if (!TryLong(amount, out _) || !TryLong(granted, out _) || spirit.Length == 0) return Fallback(c);
            return $"{Player(c)} bought {granted} {summaryUnit} for {amount} {spirit}";
        }

        private static string GiveSpirit(LogFormatContext c)
        {
            const string sentPrefix = "GiveSpirit: sent ";
            const string receivedPrefix = "GiveSpirit: received ";
            (string Verb, string Prep)? dir =
                c.Text.StartsWith(sentPrefix, StringComparison.Ordinal) ? ("sent", "to") :
                c.Text.StartsWith(receivedPrefix, StringComparison.Ordinal) ? ("received", "from") : null;
            if (dir is null) return Fallback(c);
            string rest = c.Text.StartsWith(sentPrefix, StringComparison.Ordinal)
                ? c.Text[sentPrefix.Length..]
                : c.Text[receivedPrefix.Length..];
            int bal = rest.IndexOf(", balance ", StringComparison.Ordinal);
            if (bal < 0) return Fallback(c);
            string head = rest[..bal];
            int sep = head.LastIndexOf($" {dir.Value.Prep} ", StringComparison.Ordinal);
            if (sep < 0) return Fallback(c);
            var amountSpirit = head[..sep].Split(' ');
            if (amountSpirit.Length < 2) return Fallback(c);
            string amount = amountSpirit[0];
            string spirit = string.Join(" ", amountSpirit[1..]);
            if (!TryLong(amount, out _)) return Fallback(c);
            return $"{Player(c)} {dir.Value.Verb} {amount} {spirit} {dir.Value.Prep} {Target(c)}";
        }

        private static string SpiritDirection(LogFormatContext c)
        {
            if (c.Text.StartsWith("GiveSpirit: sent ", StringComparison.Ordinal)) return "Recipient";
            if (c.Text.StartsWith("GiveSpirit: received ", StringComparison.Ordinal)) return "Sender";
            return "Player";
        }

        private static string ResetItem(LogFormatContext c)
        {
            const string prefix = "ResetItem: template ";
            if (!c.Text.StartsWith(prefix, StringComparison.Ordinal)) return Fallback(c);
            var t = c.Text[prefix.Length..].Split(' ');
            if (t.Length < 7 || t[1] != "dim" || t[3] != "cost" || t[6] != "balance" ||
                !TryLong(t[0], out _) || !TryLong(t[2], out _) || !TryLong(t[4], out _))
            {
                return Fallback(c);
            }

            return $"{Player(c)} reset an item (template {t[0]}, dimension {t[2]}) for {t[4]} {t[5]}";
        }

        private static string GetItemSummary(LogFormatContext c)
        {
            var (ok, id, stack, name) = ParseGetItemText(c.Text);
            return ok ? $"{Player(c)} gave {name} ×{stack}" : Fallback(c);
        }

        private static LogRelatedEntity? GetItemRelated(LogFormatContext c)
        {
            var (ok, id, stack, name) = ParseGetItemText(c.Text);
            return ok ? new LogRelatedEntity("Item", LogEntityKind.Item, id, name, Quick(id)) : null;
        }

        private static (bool Ok, long Id, long Stack, string Name) ParseGetItemText(string text)
        {
            var tokens = text.Split(' ');
            if (tokens.Length >= 3 && TryLong(tokens[^2], out long id) && TryLong(tokens[^1], out long stack))
            {
                return (true, id, stack, string.Join(" ", tokens[..^2]));
            }

            return (false, 0, 0, "");
        }

        private static string ClassChange(LogFormatContext c)
        {
            var t = c.Text.Split(' ');
            if (t.Length < 3) return Fallback(c);
            return $"{Player(c)} changed {Target(c)}'s class to {t[1]} (rate {t[2]})";
        }

        private static string GiveAmount(LogFormatContext c, string unit)
        {
            int sep = c.Text.LastIndexOf(" to ", StringComparison.Ordinal);
            if (sep < 0) return Fallback(c);
            string amount = c.Text[..sep];
            if (!TryLong(amount, out _)) return Fallback(c);
            return $"{Player(c)} gave {Target(c)} {amount} {unit}";
        }

        private static string SpawnedNpc(LogFormatContext c)
        {
            string npc = c.OtherId is > 0 ? NpcRef(c) : c.Text.Length > 0 ? c.Text : NpcRef(c);
            return $"{Player(c)} spawned {npc}";
        }

        private static LogRelatedEntity? PlayerRelated(LogFormatContext c, string label)
        {
            if (c.OtherId is null or 0) return null;
            return new LogRelatedEntity(label, LogEntityKind.Player, c.OtherId,
                Lookup(c.OtherId, c.PlayerNames), Quick(c.OtherId));
        }

        private static LogRelatedEntity? EntityRelated(long? id, string label, LogEntityKind kind,
            IReadOnlyDictionary<int, string> names)
        {
            if (id is null or 0) return null;
            return new LogRelatedEntity(label, kind, id, Lookup(id, names), Quick(id));
        }

        private static LogRelatedEntity? MapRelated(LogFormatContext c)
        {
            if (c.MapId is null or 0) return null;
            return new LogRelatedEntity("Map", LogEntityKind.Map, c.MapId,
                Lookup(c.MapId, c.MapNames), Quick(c.MapId));
        }

        private static string? Lookup(long? id, IReadOnlyDictionary<int, string> names)
        {
            if (id is > 0 and <= int.MaxValue && names.TryGetValue((int)id.Value, out string? n) && n.Length > 0)
            {
                return n;
            }

            return null;
        }

        private static string Fallback(LogFormatContext c)
        {
            string label = LogEventRegistry.Get(c.Type).Label;
            return c.Text.Length == 0 ? label : $"{label}: {c.Text}";
        }

        private static string UnknownSummary(LogFormatContext c)
        {
            string s = $"Unknown event #{c.Type} (player {Raw(c.PlayerId)}, other {Raw(c.OtherId)}, "
                + $"map {Raw(c.MapId)} at {Raw(c.MapX)}, {Raw(c.MapY)})";
            if (c.Text.Length > 0) s += $": {c.Text}";
            return s;
        }

        private static string Raw(long? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "-";

        private static string Player(LogFormatContext c) => Display(c.PlayerId, c.PlayerNames, "Player");
        private static string Target(LogFormatContext c) => Display(c.OtherId, c.PlayerNames, "Player");
        private static string NpcRef(LogFormatContext c) => Display(c.OtherId, c.NpcTemplateNames, "NPC");
        private static string MapRef(LogFormatContext c) => Display(c.MapId, c.MapNames, "Map");
        private static string GuildRef(LogFormatContext c, long? id) => Display(id, c.GuildNames, "Guild");

        private static string Display(long? id, IReadOnlyDictionary<int, string> names, string prefix)
        {
            if (id is > 0 and <= int.MaxValue && names.TryGetValue((int)id.Value, out string? n) && n.Length > 0)
            {
                return n;
            }

            return id is null ? $"{prefix} #?" : $"{prefix} #{id}";
        }

        private static long? TextId(string text) => TryLong(text, out long v) ? v : null;

        private static bool Quick(long? id) => id is > 0 and <= int.MaxValue;

        private static bool TryLong(string s, out long v) =>
            long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
    }
}
