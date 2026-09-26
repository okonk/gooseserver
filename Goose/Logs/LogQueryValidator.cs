using System.Collections.Immutable;
using System.Data;
using System.Data.SQLite;
using System.Globalization;

namespace Goose.Logs
{
    public static class LogQueryValidator
    {
        private static readonly TimeSpan MaxFreshRange = TimeSpan.FromDays(31);
        private static readonly TimeSpan MaxTextRange = TimeSpan.FromDays(7);

        public static LogValidationResult ValidateFresh(SQLiteConnection connection, LogFreshSearchInput input)
        {
            string text = input.Text ?? string.Empty;
            string participant = input.Participant ?? string.Empty;

            if (text.Contains('\0'))
                return LogValidationResult.Failure(LogValidationError.InvalidText, "Text contains a NUL character.");
            if (participant.Contains('\0'))
                return LogValidationResult.Failure(LogValidationError.InvalidParticipant, "Participant contains a NUL character.");

            if (!TryToUtcTicks(input.StartUtcMilliseconds, out long startTicks)
                || !TryToUtcTicks(input.EndUtcMilliseconds, out long endTicks))
                return LogValidationResult.Failure(LogValidationError.InvalidBounds, "Unix millisecond value is out of range.");
            if (input.StartUtcMilliseconds >= input.EndUtcMilliseconds)
                return LogValidationResult.Failure(LogValidationError.InvalidBounds, "Start must be before end.");

            TimeSpan range = TimeSpan.FromMilliseconds(input.EndUtcMilliseconds - input.StartUtcMilliseconds);
            if (range > MaxFreshRange)
                return LogValidationResult.Failure(LogValidationError.RangeTooWide, "Range exceeds 31 days.");
            if (text.Length > 0 && range > MaxTextRange)
                return LogValidationResult.Failure(LogValidationError.TextRangeTooWide, "Range exceeds 7 days for text search.");

            int? mapId;
            if (input.MapId == 0)
                mapId = null;
            else if (input.MapId > 0)
                mapId = input.MapId;
            else
                return LogValidationResult.Failure(LogValidationError.InvalidMap, "Map id must be zero or positive.");

            var eventIds = new List<int>();
            if (input.EventTypeIds is not null)
            {
                foreach (int id in input.EventTypeIds)
                {
                    if (!LogEventRegistry.TryGetKnown(id, out _))
                        return LogValidationResult.Failure(LogValidationError.InvalidEventType, "Unknown event type id " + id + ".");
                    eventIds.Add(id);
                }
            }
            eventIds.Sort();
            eventIds = eventIds.Distinct().ToList();
            ImmutableArray<int> eventTypeIds = eventIds.ToImmutableArray();

            int? participantId;
            if (participant.Trim().Length == 0)
                participantId = null;
            else if (participant.StartsWith('#'))
            {
                string digits = participant[1..];
                if (digits.Length == 0 || !digits.All(char.IsAsciiDigit)
                    || !long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out long idValue)
                    || idValue < 1 || idValue > int.MaxValue)
                    return LogValidationResult.Failure(LogValidationError.InvalidParticipant, "Malformed participant id.");
                participantId = (int)idValue;
            }
            else
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT player_id FROM players WHERE player_name = @name COLLATE NOCASE ORDER BY player_id LIMIT 2;";
                command.Parameters.Add(new SQLiteParameter("@name", DbType.String) { Value = participant });
                int matches = 0;
                int resolved = 0;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        matches++;
                        resolved = reader.GetInt32(0);
                    }
                }
                if (matches == 0)
                    return LogValidationResult.Failure(LogValidationError.ParticipantNotFound, "No player matches that name.");
                if (matches > 1)
                    return LogValidationResult.Failure(LogValidationError.ParticipantAmbiguous, "Multiple players match that name.");
                participantId = resolved;
            }

            return LogValidationResult.Success(new LogSearchQuery(startTicks, endTicks, participantId, mapId, eventTypeIds, text, null));
        }

        private static bool TryToUtcTicks(long unixMilliseconds, out long utcTicks)
        {
            try
            {
                utcTicks = checked(DateTime.UnixEpoch.Ticks + unixMilliseconds * TimeSpan.TicksPerMillisecond);
            }
            catch (OverflowException)
            {
                utcTicks = 0;
                return false;
            }
            return utcTicks >= 0 && utcTicks <= DateTime.MaxValue.Ticks;
        }
    }
}
