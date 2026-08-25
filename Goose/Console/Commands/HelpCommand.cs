
namespace Goose.ConsoleCommands
{
    /**
     * HelpCommand, /help
     *
     * */
    public static class HelpCommand
    {
        public const string Usage = "/help";
        public const string Description = "Show this list.";

        public static void Run(IEnumerable<ConsoleCommand> commands)
        {
            Console.WriteLine("Console commands:");

            foreach (ConsoleCommand command in commands)
            {
                Console.WriteLine("  " + command.Usage.PadRight(34) + command.Description);
            }
        }
    }
}
