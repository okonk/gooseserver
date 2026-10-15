using System.Globalization;

namespace Goose.Tests
{
    internal sealed class ProcessEnvironmentScope : IDisposable
    {
        private readonly string? previousTz;
        private readonly CultureInfo previousCulture;

        public ProcessEnvironmentScope(string tz, CultureInfo culture)
        {
            this.previousTz = Environment.GetEnvironmentVariable("TZ");
            this.previousCulture = CultureInfo.CurrentCulture;
            Environment.SetEnvironmentVariable("TZ", tz);
            CultureInfo.CurrentCulture = culture;
            TimeZoneInfo.ClearCachedData();
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("TZ", this.previousTz);
            CultureInfo.CurrentCulture = this.previousCulture;
            TimeZoneInfo.ClearCachedData();
        }
    }
}
