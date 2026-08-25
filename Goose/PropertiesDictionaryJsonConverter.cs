#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goose;

/// <summary>
/// Custom JSON converter for <see cref="PropertiesDictionary"/> that properly deserializes
/// primitive types (instead of leaving them as JsonElement), plus nested dictionaries and lists.
/// </summary>
public class PropertiesDictionaryJsonConverter : JsonConverter<PropertiesDictionary>
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    public override PropertiesDictionary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject token, got {reader.TokenType}");
        }

        var dictionary = new PropertiesDictionary();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");
            }

            var key = reader.GetString() ?? throw new JsonException("Property name was null");

            reader.Read();

            var value = ReadValue(ref reader, key, options);
            dictionary[key] = value!;
        }

        throw new JsonException("Unexpected end of JSON");
    }

    private object? ReadValue(ref Utf8JsonReader reader, string key, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.Number:
                // Try to get as long first (for integers), fall back to double
                if (reader.TryGetInt64(out var longValue))
                {
                    return longValue;
                }
                return reader.GetDouble();

            case JsonTokenType.StartArray:
                return ReadArray(ref reader, key, options);

            case JsonTokenType.StartObject:
                return ReadObject(ref reader, key, options);

            default:
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }
    }

    private object ReadArray(ref Utf8JsonReader reader, string key, JsonSerializerOptions options)
    {
        var list = new List<object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return list;
            }

            var value = ReadValue(ref reader, string.Empty, options);
            list.Add(value);
        }

        throw new JsonException("Unexpected end of JSON while reading array");
    }

    private object? ReadObject(ref Utf8JsonReader reader, string key, JsonSerializerOptions options)
    {
        // No registered type - read as a nested dictionary
        var nestedDict = new Dictionary<string, object?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return nestedDict;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");
            }

            var nestedKey = reader.GetString() ?? throw new JsonException("Property name was null");
            reader.Read();

            var value = ReadValue(ref reader, nestedKey, options);
            nestedDict[nestedKey] = value;
        }

        throw new JsonException("Unexpected end of JSON while reading object");
    }

    public override void Write(Utf8JsonWriter writer, PropertiesDictionary value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, kvp.Value?.GetType() ?? typeof(object), CamelCaseOptions);
        }

        writer.WriteEndObject();
    }
}
