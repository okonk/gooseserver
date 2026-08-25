#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Goose;

/// <summary>
/// A dictionary that provides typed access to properties stored as objects.
/// </summary>
[JsonConverter(typeof(PropertiesDictionaryJsonConverter))]
public class PropertiesDictionary : Dictionary<string, object>
{
    /// <summary>
    /// Gets a property value by key, converting it to the specified type.
    /// Handles numeric type conversions (e.g., long to int, double to float).
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <returns>The property value converted to type T.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be converted to type T.</exception>
    public T GetProperty<T>(string key)
    {
        if (!TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"Property '{key}' not found.");
        }

        return ConvertValue<T>(value);
    }

    /// <summary>
    /// Gets a property value by key, converting it to the specified type.
    /// Returns the specified default value if the key does not exist.
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <returns>The property value converted to type T, or the default value if the key doesn't exist.</returns>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be converted to type T.</exception>
    public T GetProperty<T>(string key, T defaultValue)
    {
        if (!TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return ConvertValue<T>(value);
    }

    /// <summary>
    /// Attempts to get a property value by key, converting it to the specified type.
    /// Handles numeric type conversions (e.g., long to int, double to float).
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="value">When this method returns, contains the property value if found; otherwise, the default value.</param>
    /// <returns>true if the property exists; otherwise, false.</returns>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be converted to type T.</exception>
    public bool TryGetProperty<T>(string key, [MaybeNullWhen(false)] out T value)
    {
        if (!TryGetValue(key, out var obj))
        {
            value = default;
            return false;
        }

        value = ConvertValue<T>(obj);
        return true;
    }

    /// <summary>
    /// Converts a value to the specified type, handling numeric type conversions.
    /// </summary>
    private static T ConvertValue<T>(object? value)
    {
        if (value is null)
        {
            if (default(T) is null)
            {
                return default!;
            }
            throw new InvalidCastException($"Cannot convert null to non-nullable type {typeof(T).Name}.");
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        // Handle numeric conversions (JSON deserializes integers as long, decimals as double)
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        
        if (IsNumericType(targetType) && IsNumericType(value.GetType()))
        {
            try
            {
                var converted = Convert.ChangeType(value, targetType);
                return (T)converted;
            }
            catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
            {
                throw new InvalidCastException($"Cannot convert value '{value}' of type {value.GetType().Name} to {typeof(T).Name}.", ex);
            }
        }

        // Handle enum conversions (numeric value to enum)
        if (targetType.IsEnum && IsNumericType(value.GetType()))
        {
            try
            {
                var enumValue = Enum.ToObject(targetType, value);
                return (T)enumValue;
            }
            catch (Exception ex) when (ex is InvalidCastException or ArgumentException)
            {
                throw new InvalidCastException($"Cannot convert value '{value}' of type {value.GetType().Name} to {typeof(T).Name}.", ex);
            }
        }

        throw new InvalidCastException($"Cannot convert value of type {value.GetType().Name} to {typeof(T).Name}.");
    }

    /// <summary>
    /// Determines if a type is a numeric type.
    /// </summary>
    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    /// <summary>
    /// Shallow copy: new dictionary, same value references. Snapshots the entry set so
    /// serialization can't race a concurrent key add/remove; values are replaced (never
    /// mutated in place) by convention, so sharing them is safe.
    /// </summary>
    public PropertiesDictionary Clone()
    {
        var copy = new PropertiesDictionary();
        foreach (var (key, value) in this)
            copy[key] = value;
        return copy;
    }
}
