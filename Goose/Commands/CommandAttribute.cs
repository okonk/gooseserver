using System;

namespace Goose.Commands
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CommandAttribute : Attribute
    {
        public string Key { get; }
        public AccessPrivilege? Privilege { get; }
        public string Section { get; set; } = "General";
        public string Help { get; set; } = null!;
        public string? Usage { get; set; }

        // Attribute constructor parameters cannot be nullable value types, so the
        // no-privilege overload gets its own body instead of `this(key, null)`.
        public CommandAttribute(string key) { Key = key; }
        public CommandAttribute(string key, AccessPrivilege privilege) { Key = key; Privilege = privilege; }
    }
}
