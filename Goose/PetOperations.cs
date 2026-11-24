namespace Goose
{
    public readonly record struct PetResult(bool Ok, string Message);

    public static class PetOperations
    {
        public static Pet? Find(Player owner, int petId)
        {
            foreach (var pet in owner.Pets)
            {
                if (pet.PetID == petId) return pet;
            }
            return null;
        }

        public static int ActiveCount(Player owner)
        {
            return owner.Pets.Count(p => p.IsAlive);
        }

        public static PetResult TrySpawn(Player owner, Pet pet, GameWorld world)
        {
            if (!owner.Map.CanSpawnPets)
            {
                return new PetResult(false, "Pets are disabled in this map.");
            }

            if (pet.IsAlive)
            {
                return new PetResult(false, "That pet is already spawned.");
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (pet.NextRespawnTime > now)
            {
                long wait = pet.NextRespawnTime - now;
                return new PetResult(false, "You must wait " + wait + " seconds to spawn this pet.");
            }

            if (ActiveCount(owner) >= world.Settings.ActivePetLimit)
            {
                return new PetResult(false, "You can only have " + world.Settings.ActivePetLimit + " pets out at once.");
            }

            pet.Spawn(world);
            return new PetResult(true, pet.Name + " has been summoned.");
        }

        public static PetResult TryDespawn(Pet pet, GameWorld world)
        {
            if (!pet.IsAlive)
            {
                return new PetResult(false, pet.Name + " isn't out.");
            }

            pet.DestroyWithCooldown(world);
            return new PetResult(true, pet.Name + " has been dismissed.");
        }

        public static PetResult SetMode(Pet pet, Pet.Modes mode)
        {
            if (mode == Pet.Modes.Attack)
            {
                throw new ArgumentOutOfRangeException(nameof(mode), "Attack mode is set by the attack spell, not SetMode.");
            }

            if (!pet.IsAlive)
            {
                return new PetResult(false, pet.Name + " isn't out.");
            }

            pet.Target = null;
            pet.Mode = mode;
            return new PetResult(true, pet.Name + " is now in " + mode + " mode.");
        }
    }
}
