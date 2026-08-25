
namespace Goose.ConsoleCommands
{
    /**
     * ParsedCommand, a console line split into its command name and arguments
     *
     */
    public sealed class ParsedCommand
    {
        public string Name;
        public string[] Args;
    }

    /**
     * ConsoleCommandParser, generic console line tokenizing
     *
     * Only the parts every command shares live here. Per command argument
     * validation belongs next to that command's handler, so this does not become a
     * dumping ground as commands are added.
     *
     */
    public static class ConsoleCommandParser
    {
        /**
         * Parse, splits a console line into command name and arguments
         *
         * Returns null for a blank line. The leading slash is optional so that both
         * "/who" and "who" work, and the name is lowercased for dispatch. Argument
         * case is preserved, since player names are arguments.
         *
         */
        public static ParsedCommand Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            string[] tokens = line.Trim().Split(
                (char[])null, StringSplitOptions.RemoveEmptyEntries);

            return new ParsedCommand
            {
                Name = tokens[0].TrimStart('/').ToLowerInvariant(),
                Args = tokens.Skip(1).ToArray()
            };
        }
    }
}
