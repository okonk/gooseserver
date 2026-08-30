using System;
using System.Globalization;
using System.Reflection;

namespace Goose.Commands
{
    internal static class CommandBinder
    {
        public static (object?[]? args, string? error) Bind(
            GameWorld world, Player player,
            ParameterInfo[] parameters, string[] tokens, string usage)
        {
            var tokenIndex = 0;
            var startIndex = 0;

            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(CommandContext))
                startIndex = 1;

            // The invoker composes [ctx, .. args]; the context slot is not part of args.
            var args = new object?[parameters.Length - startIndex];

            for (var i = startIndex; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.ParameterType == typeof(string[]))
                {
                    var rest = new string[tokens.Length - tokenIndex];
                    Array.Copy(tokens, tokenIndex, rest, 0, rest.Length);
                    args[i - startIndex] = rest;
                    tokenIndex = tokens.Length;
                    continue;
                }

                if (tokenIndex < tokens.Length)
                {
                    var token = tokens[tokenIndex];
                    tokenIndex++;

                    var underlying = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

                    if (underlying == typeof(Player))
                    {
                        var target = world.PlayerHandler.GetPlayer(token);
                        if (target is null)
                            return (null, $"Couldn't find player {token}.");
                        args[i - startIndex] = target;
                        continue;
                    }

                    if (underlying == typeof(bool))
                    {
                        if (!TryParseBool(token, out var value))
                            return (null, usage);
                        args[i - startIndex] = value;
                        continue;
                    }

                    // Invariant culture, no thousands separators: tokens are wire input, not locale-formatted.
                    const NumberStyles numeric = NumberStyles.Float & ~NumberStyles.AllowThousands;
                    if (underlying == typeof(int) && int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                    { args[i - startIndex] = intValue; continue; }
                    if (underlying == typeof(long) && long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                    { args[i - startIndex] = longValue; continue; }
                    // float/double TryParse saturates to ±Infinity on overflow instead of failing.
                    if (underlying == typeof(float) && float.TryParse(token, numeric, CultureInfo.InvariantCulture, out var floatValue) && float.IsFinite(floatValue))
                    { args[i - startIndex] = floatValue; continue; }
                    if (underlying == typeof(double) && double.TryParse(token, numeric, CultureInfo.InvariantCulture, out var doubleValue) && double.IsFinite(doubleValue))
                    { args[i - startIndex] = doubleValue; continue; }
                    if (underlying == typeof(decimal) && decimal.TryParse(token, numeric, CultureInfo.InvariantCulture, out var decimalValue))
                    { args[i - startIndex] = decimalValue; continue; }
                    if (underlying == typeof(string))
                    { args[i - startIndex] = token; continue; }

                    return (null, usage);
                }

                if (parameter.HasDefaultValue)
                {
                    args[i - startIndex] = parameter.DefaultValue;
                    continue;
                }

                return (null, usage);
            }

            return (args, null);
        }

        public static ParameterInfo[] InvocationParameters(Delegate handler)
            => handler.GetType().GetMethod("Invoke")!.GetParameters();

        public static ParameterInfo[] UsageParameters(Delegate handler)
        {
            var invoke = InvocationParameters(handler);
            var method = handler.Method.GetParameters();
            if (SameTypes(method, invoke))
                return method;
            // A closed static delegate's Method still carries the pre-bound leading parameter.
            if (method.Length == invoke.Length + 1 && SameTypes(method.AsSpan(1), invoke))
                return method.AsSpan(1).ToArray();
            return invoke;
        }

        private static bool SameTypes(ReadOnlySpan<ParameterInfo> a, ReadOnlySpan<ParameterInfo> b)
        {
            if (a.Length != b.Length)
                return false;
            for (var i = 0; i < a.Length; i++)
                if (a[i].ParameterType != b[i].ParameterType)
                    return false;
            return true;
        }

        public static bool IsValidKey(string key)
        {
            if (key.Length == 0 || key[0] != '/')
                return false;
            if (key.Length > 1 && key[^1] == ' ')
                key = key[..^1];
            return !key.Contains(' ');
        }

        public static bool IsValidTarget(ParameterInfo[] parameters, out string? error)
        {
            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(CommandContext))
            {
                error = "first parameter must be CommandContext";
                return false;
            }

            for (var i = 1; i < parameters.Length; i++)
            {
                var type = parameters[i].ParameterType;

                if (type == typeof(string[]))
                {
                    if (i != parameters.Length - 1)
                    {
                        error = "string[] may only be the final parameter";
                        return false;
                    }
                    continue;
                }

                var underlying = Nullable.GetUnderlyingType(type) ?? type;
                if (underlying != typeof(string) && underlying != typeof(int) && underlying != typeof(long)
                    && underlying != typeof(float) && underlying != typeof(double) && underlying != typeof(decimal)
                    && underlying != typeof(bool) && underlying != typeof(Player))
                {
                    error = $"unsupported parameter type {type.Name}";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public static string Usage(string key, ParameterInfo[] parameters, string? usageOverride = null)
        {
            if (usageOverride is not null)
                return $"Usage: {usageOverride}";

            var segments = new System.Text.StringBuilder("Usage: ").Append(key.TrimEnd());
            var startIndex = 0;
            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(CommandContext))
                startIndex = 1;

            for (var i = startIndex; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.ParameterType == typeof(string[]))
                    segments.Append(" [").Append(parameter.Name).Append("...]");
                else if (parameter.HasDefaultValue)
                    segments.Append(" [").Append(parameter.Name).Append(']');
                else
                    segments.Append(" <").Append(parameter.Name).Append('>');
            }

            return segments.ToString();
        }

        private static bool TryParseBool(string token, out bool value)
        {
            switch (token.ToLowerInvariant())
            {
                case "on":
                case "true":
                case "1":
                    value = true;
                    return true;
                case "off":
                case "false":
                case "0":
                    value = false;
                    return true;
                default:
                    value = false;
                    return false;
            }
        }
    }
}
