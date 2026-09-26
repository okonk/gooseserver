using System.Collections.Immutable;

namespace Goose.Logs
{
    public sealed record LogFreshSearchInput
    {
        public long StartUtcMilliseconds { get; init; }
        public long EndUtcMilliseconds { get; init; }
        public string? Participant { get; init; }
        public int MapId { get; init; }
        public IReadOnlyCollection<int> EventTypeIds { get; init; } = Array.Empty<int>();
        public string? Text { get; init; }
    }

    internal sealed record LogPageCursor(long SnapshotCeiling, long? BeforeUtcTicks, long? BeforeRowId)
    {
        public static LogPageCursor ForFirstPage(long snapshotCeiling) => new(snapshotCeiling, null, null);
    }

    public sealed class LogSearchQuery
    {
        public long StartUtcTicks { get; }
        public long EndUtcTicks { get; }
        public int? ParticipantId { get; }
        public int? MapId { get; }
        public ImmutableArray<int> EventTypeIds { get; }
        public string Text { get; }
        internal LogPageCursor? Cursor { get; }

        internal LogSearchQuery(
            long startUtcTicks,
            long endUtcTicks,
            int? participantId,
            int? mapId,
            ImmutableArray<int> eventTypeIds,
            string text,
            LogPageCursor? cursor)
        {
            StartUtcTicks = startUtcTicks;
            EndUtcTicks = endUtcTicks;
            ParticipantId = participantId;
            MapId = mapId;
            EventTypeIds = eventTypeIds;
            Text = text;
            Cursor = cursor;
        }

        internal LogSearchQuery WithCursor(LogPageCursor cursor)
        {
            return new LogSearchQuery(StartUtcTicks, EndUtcTicks, ParticipantId, MapId, EventTypeIds, Text, cursor);
        }
    }

    public enum LogValidationError
    {
        None,
        InvalidBounds,
        RangeTooWide,
        TextRangeTooWide,
        InvalidText,
        InvalidParticipant,
        ParticipantNotFound,
        ParticipantAmbiguous,
        InvalidMap,
        InvalidEventType,
    }

    public sealed class LogValidationResult
    {
        public LogSearchQuery? Query { get; }
        public LogValidationError Code { get; }
        public string Message { get; }

        private LogValidationResult(LogSearchQuery? query, LogValidationError code, string message)
        {
            Query = query;
            Code = code;
            Message = message;
        }

        public static LogValidationResult Success(LogSearchQuery query)
            => new(query, LogValidationError.None, string.Empty);

        public static LogValidationResult Failure(LogValidationError code, string message)
            => new(null, code, message);

        public bool IsSuccess => Query is not null;
    }
}
