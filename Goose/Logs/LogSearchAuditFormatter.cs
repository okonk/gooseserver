using System.Globalization;
using System.Text;

namespace Goose.Logs
{
    internal static class LogSearchAuditFormatter
    {
        public static string Format(LogFreshSearchInput input)
        {
            var builder = new StringBuilder();
            builder.Append("{\"startUtcMilliseconds\":");
            builder.Append(input.StartUtcMilliseconds.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"endUtcMilliseconds\":");
            builder.Append(input.EndUtcMilliseconds.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"participant\":");
            WriteString(builder, input.Participant ?? string.Empty);
            builder.Append(",\"mapId\":");
            builder.Append(input.MapId.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\"eventIds\":[");
            var ids = (input.EventTypeIds ?? Array.Empty<int>()).Distinct().OrderBy(id => id).ToList();
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(ids[i].ToString(CultureInfo.InvariantCulture));
            }
            builder.Append("],\"groups\":[");
            var groups = GroupLabels(ids);
            for (int i = 0; i < groups.Count; i++)
            {
                if (i > 0) builder.Append(',');
                WriteString(builder, groups[i]);
            }
            builder.Append("],\"text\":");
            WriteString(builder, input.Text ?? string.Empty);
            builder.Append('}');
            return builder.ToString();
        }

        private static List<string> GroupLabels(IReadOnlyList<int> sortedIds)
        {
            var labels = new List<string>();
            foreach (var descriptor in LogEventRegistry.Known)
            {
                if (!sortedIds.Contains(descriptor.Id)) continue;
                string label = Display(descriptor.Group);
                if (!labels.Contains(label)) labels.Add(label);
            }
            if (labels.Count == 0) labels.Add("All");
            return labels;
        }

        private static string Display(LogEventGroup group) => group switch
        {
            LogEventGroup.Communication => "Communication",
            LogEventGroup.SessionsSecurity => "Sessions/Security",
            LogEventGroup.Social => "Social",
            LogEventGroup.ItemsEconomy => "Items/Economy",
            LogEventGroup.GmActions => "GM Actions",
            _ => "Other/Retired",
        };

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(c);
                        break;
                }
            }
            builder.Append('"');
        }
    }
}
