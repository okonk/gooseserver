using NLog;
using NLog.Config;
using NLog.Targets;

namespace Goose.Tests;

public sealed class CapturingLog : IDisposable
{
    public MemoryTarget Target { get; } = new();
    private readonly LoggingConfiguration? previous;

    public CapturingLog()
    {
        this.previous = LogManager.Configuration;
        var config = new LoggingConfiguration();
        config.AddTarget("mem", this.Target);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, this.Target);
        LogManager.Configuration = config;
    }

    public IEnumerable<string> Messages => this.Target.Logs;

    public void Dispose() => LogManager.Configuration = this.previous;
}
