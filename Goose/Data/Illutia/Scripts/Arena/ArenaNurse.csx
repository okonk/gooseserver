using System;
using Goose;
using Goose.Scripting;

public class ArenaNurse : BaseNPCScript
{
    private const string StepKey = "nurse.step";
    private const string UntilKey = "nurse.until";

    private enum Kind { Walk, Face, Wait, Punch }

    private readonly record struct Step(Kind Kind, int X = 0, int Y = 0, double Min = 0, double Max = 0, Direction Facing = Direction.Right);

    private static Step Walk(int x, int y) => new(Kind.Walk, X: x, Y: y);
    private static Step Face(Direction facing) => new(Kind.Face, Facing: facing);
    private static Step Wait(double min, double max) => new(Kind.Wait, Min: min, Max: max);
    private static readonly Step Punch = new(Kind.Punch);

    private static readonly Step[] Patrol =
    {
        Walk(38, 16), Face(Direction.Right), Wait(2, 4), Punch, Wait(2, 4),
        Walk(42, 16), Face(Direction.Right), Wait(2, 4), Punch, Wait(2, 4),
        Walk(44, 16), Face(Direction.Right), Wait(2, 4), Punch, Wait(2, 4),
        Walk(40, 12), Face(Direction.Down), Wait(60, 60),
        Walk(37, 15), Wait(10, 10),
    };

    public override void OnSpawnEvent(NPC npc, GameWorld world)
    {
        npc.Properties[StepKey] = 0;
        npc.Properties[UntilKey] = 0L;
    }

    public override void OnMoveEvent(NPC npc, GameWorld world)
    {
        try
        {
            Advance(npc, world);
        }
        finally
        {
            npc.AddMoveEvent(world);
        }
    }

    private static void Advance(NPC npc, GameWorld world)
    {
        if (npc.Hidden || npc.Map is null) return;
        for (int i = 0; i < Patrol.Length; i++)
        {
            if (world.TimeNow < npc.Properties.GetProperty<long>(UntilKey, 0)) return;
            int index = npc.Properties.GetProperty<int>(StepKey, 0) % Patrol.Length;
            var step = Patrol[index];
            if (step.Kind == Kind.Walk && (npc.MapX != step.X || npc.MapY != step.Y))
            {
                var map = npc.Map;
                if (npc.PathStepTo(step.X, step.Y, world, (x, y) => map.GetCharacterAt(x, y) is not null) is Direction d)
                    npc.StepTo(d, world);
                return;
            }
            if (step.Kind == Kind.Face)
                npc.FaceTo(step.Facing, world);
            else if (step.Kind == Kind.Punch)
            {
                string packet = P.Attack(npc);
                foreach (var player in npc.Map.GetPlayersInRange(npc))
                    world.Send(player, packet);
            }
            else if (step.Kind == Kind.Wait)
            {
                double seconds = step.Min + world.Random.NextDouble() * (step.Max - step.Min);
                npc.Properties[UntilKey] = world.TimeNow + (long)(seconds * world.TimerFrequency);
            }
            npc.Properties[StepKey] = (index + 1) % Patrol.Length;
        }
    }
}

return typeof(ArenaNurse);
