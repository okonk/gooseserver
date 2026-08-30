using System;

namespace Goose.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubcommandAttribute : Attribute
    {
        public string[] Names { get; }
        public string PrimaryName => this.Names[0];
        public AccessPrivilege? Privilege { get; }
        public string Help { get; set; } = null!;
        public string? Usage { get; set; }

        // Attribute constructor parameters cannot be nullable value types, so the
        // no-privilege overloads get their own bodies instead of `this(..., null)`.
        public SubcommandAttribute(string name) { Names = [name]; }
        public SubcommandAttribute(string name, AccessPrivilege privilege) { Names = [name]; Privilege = privilege; }
        public SubcommandAttribute(string firstName, string secondName, params string[] additionalNames)
            { Names = [firstName, secondName, .. additionalNames]; }
        public SubcommandAttribute(string firstName, string secondName, AccessPrivilege privilege, params string[] additionalNames)
            { Names = [firstName, secondName, .. additionalNames]; Privilege = privilege; }
    }
}
