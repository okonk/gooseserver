namespace Goose.Commands
{
    public sealed class CommandContext
    {
        public Player Player { get; }
        public GameWorld World { get; }
        public CommandRegistry Registry { get; }
        public string[] Args { get; }
        public string Remainder { get; }
        public string Usage { get; internal set; } = "";

        internal CommandContext(Player player, GameWorld world, CommandRegistry registry, string[] args, string remainder)
        {
            Player = player;
            World = world;
            Registry = registry;
            Args = args;
            Remainder = remainder;
        }

        public void Send(string message) => World.Send(Player, P.ServerMessage(message));
    }
}
