using System.Text.Json;

namespace Goose
{
    public sealed class WeatherDistribution : Dictionary<WeatherType, float>
    {
        public static WeatherDistribution Parse(string? raw, out string? warning)
        {
            warning = null;
            if (string.IsNullOrWhiteSpace(raw))
                return new WeatherDistribution();

            Dictionary<string, float?>? weights;
            try
            {
                weights = JsonSerializer.Deserialize<Dictionary<string, float?>>(raw);
            }
            catch (JsonException)
            {
                warning = "invalid weather distribution JSON";
                return new WeatherDistribution();
            }
            if (weights is null)
                return new WeatherDistribution();

            var distribution = new WeatherDistribution();
            List<string>? problems = null;
            foreach ((string name, float? weight) in weights)
            {
                string trimmedName = name.Trim();
                bool numeric = trimmedName.Length > 0 && trimmedName.All(char.IsDigit);
                if (numeric || !Enum.TryParse<WeatherType>(name, true, out WeatherType type) || !Enum.IsDefined(type))
                {
                    (problems ??= new()).Add($"unknown weather type '{name}'");
                    continue;
                }
                if (weight is null)
                {
                    (problems ??= new()).Add($"missing weight for '{name}'");
                    continue;
                }
                if (!float.IsFinite(weight.Value) || weight.Value <= 0)
                    continue;
                distribution[type] = weight.Value;
            }

            warning = problems is null ? null : string.Join(", ", problems);
            return distribution;
        }
    }
}
