namespace Goose
{
    public static class PetMenu
    {
        public static void Open(Player owner, GameWorld world)
        {
            foreach (var w in owner.Windows.Where(w => w.Type == Window.WindowTypes.OptionList && w.NPC is null).ToList())
                w.Close(owner, world);

            string title = $"Pets ({owner.Pets.Count}/{owner.MaxPets(world.Settings)})";

            if (owner.Pets.Count == 0)
            {
                new OptionListWindow(owner, world, title, ["You have no pets. Tame one!"], (_, _, _) => { });
                return;
            }

            // Snapshot ids and re-resolve on click: the window closes before the callback runs,
            // so a pet released in between can't leave a dangling reference.
            var ids = owner.Pets.Select(p => p.PetID).ToList();
            var lines = owner.Pets.Select(PetLine).ToList();
            new OptionListWindow(owner, world, title, lines, (index, p, w) => OpenPet(p, ids[index], w));
        }

        public static string PetLine(Pet pet)
        {
            string status = "";
            if (pet.IsAlive)
            {
                status = pet.Mode == Pet.Modes.Attack ? "  [Out - Attacking]" : $"  [Out - {pet.Mode}]";
            }
            else
            {
                long remaining = pet.NextRespawnTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (remaining > 0)
                    status = $"  [Cooldown {remaining}s]";
            }
            return $"{pet.Name}  Lv {pet.Level}{status}";
        }

        internal static void OpenPet(Player owner, int petId, GameWorld world)
        {
        }
    }
}
