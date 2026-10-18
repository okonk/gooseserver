using System.Text.Json;

namespace Goose
{
    public sealed record LightingConfig(double StartAlpha, double EndAlpha, uint TintColor, uint FillColor, double CycleDurationSeconds)
    {
        public static readonly LightingConfig None = new(0, 0, 0x090909FFu, 0x001429FFu, 0);

        private sealed class Dto
        {
            public string? Preset { get; set; }
            public double? CycleDuration { get; set; }
            public double? StartAlpha { get; set; }
            public double? EndAlpha { get; set; }
            public string? TintColor { get; set; }
            public string? FillColor { get; set; }
        }

        private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

        public static LightingConfig Parse(string? raw, out string? warning)
        {
            warning = null;
            if (string.IsNullOrWhiteSpace(raw))
                return None;

            string trimmed = raw.Trim();
            if (trimmed[0] != '{')
            {
                if (!TryGetPreset(trimmed, out double s, out double e, out double c))
                {
                    warning = $"unknown lighting preset '{trimmed}'";
                    return None;
                }
                return new LightingConfig(s, e, 0x090909FFu, 0x001429FFu, c);
            }

            Dto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<Dto>(trimmed, Options);
            }
            catch (JsonException)
            {
                warning = "invalid lighting JSON";
                return None;
            }
            if (dto is null)
                return None;

            if (!TryGetPreset(dto.Preset ?? "", out double start, out double end, out double cycle))
            {
                warning = $"unknown lighting preset '{dto.Preset}'";
                return None;
            }

            List<string>? problems = null;
            uint tint = 0x090909FFu;
            uint fill = 0x001429FFu;

            if (dto.CycleDuration is double cycleDuration)
            {
                if (double.IsFinite(cycleDuration) && cycleDuration > 0)
                    cycle = cycleDuration;
                else
                    (problems ??= new()).Add($"invalid cycleDuration {cycleDuration}");
            }
            if (dto.StartAlpha is double startAlpha)
                start = Math.Clamp(startAlpha, 0, 1);
            if (dto.EndAlpha is double endAlpha)
                end = Math.Clamp(endAlpha, 0, 1);
            if (dto.TintColor is string tintRaw)
            {
                if (ColorUtils.TryParseHex(tintRaw, out uint tintValue))
                    tint = tintValue;
                else
                    (problems ??= new()).Add($"invalid tintColor '{tintRaw}'");
            }
            if (dto.FillColor is string fillRaw)
            {
                if (ColorUtils.TryParseHex(fillRaw, out uint fillValue))
                    fill = fillValue;
                else
                    (problems ??= new()).Add($"invalid fillColor '{fillRaw}'");
            }

            warning = problems is null ? null : string.Join(", ", problems);
            return new LightingConfig(start, end, tint, fill, cycle);
        }

        private static bool TryGetPreset(string name, out double start, out double end, out double cycle)
        {
            switch (name.Trim().ToLowerInvariant())
            {
                case "" or "none":
                    (start, end, cycle) = (0, 0, 0);
                    return true;
                case "outside":
                    (start, end, cycle) = (0, 0.70, 1440);
                    return true;
                case "inside":
                    (start, end, cycle) = (0.1, 0.2, 1440);
                    return true;
                case "cave":
                    (start, end, cycle) = (0.70, 0.70, 1440);
                    return true;
                default:
                    (start, end, cycle) = (0, 0, 0);
                    return false;
            }
        }
    }
}
