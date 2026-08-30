namespace Goose.Commands
{
    [Command("/petinfo ", Section = "Pets", Help = "Show the status window for one of your pets.")]
    public sealed class PetInfoCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] id)
        {
            var world = ctx.World;

            string data = string.Join(" ", id);
            int petId = 0;

            try
            {
                petId = Convert.ToInt32(data);
            }
            catch (Exception)
            {
                petId = 0;
            }

            if (petId <= 0)
            {
                world.Send(ctx.Player, P.ServerMessage("Invalid pet ID."));
                return;
            }

            Pet? match = null;
            foreach (var pet in ctx.Player.Pets)
            {
                if (pet.PetID == petId)
                {
                    match = pet;
                    break;
                }
            }

            if (match is null)
            {
                world.Send(ctx.Player, P.ServerMessage("Couldn't find pet matching ID."));
                return;
            }

            foreach (var window in ctx.Player.Windows)
            {
                if (window.Type == Window.WindowTypes.PetInfo && window.Data == match)
                {
                    window.Refresh(ctx.Player, world);
                    return;
                }
            }

            Window w = new Window();
            w.Title = "Pet Info For ID " + match.PetID;
            w.Type = Window.WindowTypes.PetInfo;
            w.Buttons = "0,0,0,0,0";
            w.Data = match;

            ctx.Player.Windows.Add(w);
            w.Create(ctx.Player, world);
        }
    }
}
