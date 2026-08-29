using System.Reflection;

namespace Goose.Commands
{
    internal sealed class CommandDefinition
    {
        public string[] Keys { get; }
        public string PrimaryKey => this.Keys[0];
        public AccessPrivilege? Privilege { get; }
        public string? Section { get; }
        public string Help { get; }
        public string? UsageOverride { get; }
        public object? Instance { get; }
        public Delegate? Handler { get; }
        public MethodInfo? ExecuteMethod { get; }
        public List<SubcommandInfo> Subcommands { get; }
        public Delegate? LegacyFactory { get; }
        public Type? LegacyType { get; }

        internal CommandDefinition(string[] keys, AccessPrivilege? privilege, string? section, string help,
            string? usageOverride, object? instance, Delegate? handler, MethodInfo? executeMethod,
            List<SubcommandInfo> subcommands, Delegate? legacyFactory, Type? legacyType)
        {
            this.Keys = keys;
            this.Privilege = privilege;
            this.Section = section;
            this.Help = help;
            this.UsageOverride = usageOverride;
            this.Instance = instance;
            this.Handler = handler;
            this.ExecuteMethod = executeMethod;
            this.Subcommands = subcommands;
            this.LegacyFactory = legacyFactory;
            this.LegacyType = legacyType;
        }
    }

    internal sealed record SubcommandInfo(
        string PrimaryName, string[] Names, MethodInfo Method,
        ParameterInfo[] Parameters, string Help, AccessPrivilege? Privilege, string? UsageOverride);
}
