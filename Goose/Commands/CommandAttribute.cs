using System;

namespace Goose.Commands
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CommandAttribute : Attribute
    {
        public string[] Keys { get; }
        public string PrimaryKey => this.Keys[0];
        public AccessPrivilege? Privilege { get; }
        public string Section { get; set; } = "General";
        public string Help { get; set; } = null!;
        public string? Usage { get; set; }

        // Attribute constructor parameters cannot be nullable value types, so the
        // no-privilege overloads get their own bodies instead of `this(..., null)`.
        public CommandAttribute(string key) { Keys = [key]; }
        public CommandAttribute(string key, AccessPrivilege privilege) { Keys = [key]; Privilege = privilege; }
        public CommandAttribute(string firstKey, string secondKey, params string[] additionalKeys)
            { Keys = [firstKey, secondKey, .. additionalKeys]; }
        public CommandAttribute(string firstKey, string secondKey, AccessPrivilege privilege, params string[] additionalKeys)
            { Keys = [firstKey, secondKey, .. additionalKeys]; Privilege = privilege; }
    }
}
