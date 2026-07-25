using System;

namespace Goose.ConsoleCommands
{
    /**
     * ShutdownCommand, /shutdown
     *
     * Same mechanism as the in game /shutdown and as GameServer.RequestShutdown:
     * clearing Running exits the game loop, which then saves players and drains the
     * database queue before returning.
     *
     */
    public static class ShutdownCommand
    {
        public const string Usage = "/shutdown";
        public const string Description = "Shut the server down, saving players first.";

        public static void Run(GameWorld world, string[] args)
        {
            Console.WriteLine("Shutting down.");
            world.Running = false;
        }
    }
}
