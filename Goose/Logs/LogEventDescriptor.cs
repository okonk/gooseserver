namespace Goose.Logs
{
    public enum LogEventGroup
    {
        Communication,
        SessionsSecurity,
        Social,
        ItemsEconomy,
        GmActions,
        OtherRetired,
    }

    public enum LogOtherIdKind
    {
        Unused,
        Player,
        Guild,
        NpcTemplate,
        Item,
        Map,
    }

    public enum LogEntityKind
    {
        Player,
        Guild,
        Item,
        Gold,
        NpcTemplate,
        Map,
        StoredValue,
    }

    public sealed class LogEventDescriptor
    {
        public int Id { get; }
        public string Label { get; }
        public LogEventGroup Group { get; }
        public LogOtherIdKind OtherIdKind { get; }

        public LogEventDescriptor(int id, string label, LogEventGroup group, LogOtherIdKind otherIdKind)
        {
            this.Id = id;
            this.Label = label;
            this.Group = group;
            this.OtherIdKind = otherIdKind;
        }
    }

    public sealed class LogRelatedEntity
    {
        public string Label { get; }
        public LogEntityKind Kind { get; }
        public long? Id { get; }
        public string? Name { get; }
        public bool CanQuickFilter { get; }

        public LogRelatedEntity(string label, LogEntityKind kind, long? id, string? name, bool canQuickFilter)
        {
            this.Label = label;
            this.Kind = kind;
            this.Id = id;
            this.Name = name;
            this.CanQuickFilter = canQuickFilter;
        }
    }

    public sealed class LogFormattedEvent
    {
        public string Summary { get; }
        public LogRelatedEntity? Related { get; }

        public LogFormattedEvent(string summary, LogRelatedEntity? related)
        {
            this.Summary = summary;
            this.Related = related;
        }
    }
}
