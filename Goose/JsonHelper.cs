using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goose
{
    public static class JsonHelper
    {
        /// <summary>
        /// Options for inventory/bank/quest/spellbook blobs stored in SQLite.
        /// Must remain compatible with historical Newtonsoft output (short names, omitted defaults/nulls).
        /// </summary>
        public static JsonSerializerOptions DatabaseOptions { get; } = CreateDatabaseOptions();

        /// <summary>
        /// Options for GooseSettings.json (// comments, trailing commas allowed).
        /// </summary>
        public static JsonSerializerOptions SettingsOptions { get; } = CreateSettingsOptions();

        private static JsonSerializerOptions CreateDatabaseOptions()
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = false,
                // Do not rename properties; [JsonPropertyName] supplies short names.
                PropertyNamingPolicy = null,
                WriteIndented = false,
                // Dictionary<ItemProperty, object> and similar
                Converters = { new JsonStringEnumConverter() },
            };
            return options;
        }

        private static JsonSerializerOptions CreateSettingsOptions()
        {
            return new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };
        }

        public static string Serialize<T>(T value) =>
            JsonSerializer.Serialize(value, DatabaseOptions);

        public static T Deserialize<T>(string json) =>
            JsonSerializer.Deserialize<T>(json, DatabaseOptions);
    }
}
