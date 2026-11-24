namespace Goose
{
    public readonly record struct PetResult(bool Ok, string Message);

    public static class PetOperations
    {
        public const int MaxAffordableBuyCap = 100_000;

        public static bool IsMaxLevel(Pet pet)
        {
            return pet.Class.GetLevel(pet.Level)?.Experience == 0;
        }

        private static long VitaCost(long hp, GooseSettings s)
        {
            double buyrate = (hp / s.IncreasePetVitaBuyCost) * 0.2 + 1;
            return Utils.MultiplyAndTruncate(s.PetVitaCost, buyrate);
        }

        private static long DamageCost(long damage, GooseSettings s)
        {
            double buyrate = (damage / s.IncreasePetDamageBuyCost) * 0.2 + 1;
            return Utils.MultiplyAndTruncate(s.PetDamageCost, buyrate);
        }

        public static long NextVitaCost(Pet pet, GooseSettings s)
        {
            return VitaCost(pet.BaseStats.HP, s);
        }

        public static long NextDamageCost(Pet pet, GooseSettings s)
        {
            return DamageCost(pet.WeaponDamage, s);
        }

        public static long MaxAffordableVita(Pet pet, GooseSettings s)
        {
            if (!IsMaxLevel(pet)) return 0;

            long exp = pet.Experience;
            long hp = pet.BaseStats.HP;
            long count = 0;
            while (count < MaxAffordableBuyCap)
            {
                long cost = VitaCost(hp, s);
                if (exp < cost) break;
                exp -= cost;
                hp += s.PetVitaBuyAmount;
                count++;
            }
            return count;
        }

        public static long MaxAffordableDamage(Pet pet, GooseSettings s)
        {
            if (!IsMaxLevel(pet)) return 0;

            long exp = pet.Experience;
            long damage = pet.WeaponDamage;
            long count = 0;
            while (count < MaxAffordableBuyCap)
            {
                long cost = DamageCost(damage, s);
                if (exp < cost) break;
                exp -= cost;
                damage += s.PetDamageBuyAmount;
                count++;
            }
            return count;
        }

        public static PetResult BuyVita(Pet pet, long buys, GameWorld world)
        {
            if (!IsMaxLevel(pet))
            {
                return new PetResult(false, pet.Name + " must be max level to buy stats.");
            }

            long bought = 0;
            long soldexp = 0;

            pet.RemoveStats(pet.BaseStats, world, false);

            for (long i = 1; i <= buys; i++)
            {
                long expcost = NextVitaCost(pet, world.Settings);

                if (pet.Experience >= expcost)
                {
                    pet.Experience -= expcost;
                    pet.ExperienceSold += expcost;
                    pet.BaseStats.HP += world.Settings.PetVitaBuyAmount;
                    bought += world.Settings.PetVitaBuyAmount;
                    soldexp += expcost;
                }
                else
                {
                    break;
                }
            }

            pet.AddStats(pet.BaseStats, world);

            if (bought == 0)
            {
                return new PetResult(false, pet.Name + " doesn't have enough experience.");
            }

            return new PetResult(true, "Bought " + bought + " hp for " + soldexp + " experience.");
        }

        public static PetResult BuyDamage(Pet pet, long buys, GameWorld world)
        {
            if (!IsMaxLevel(pet))
            {
                return new PetResult(false, pet.Name + " must be max level to buy stats.");
            }

            long bought = 0;
            long soldexp = 0;

            for (long i = 1; i <= buys; i++)
            {
                long expcost = NextDamageCost(pet, world.Settings);

                if (pet.Experience >= expcost)
                {
                    pet.Experience -= expcost;
                    pet.ExperienceSold += expcost;
                    pet.WeaponDamage += world.Settings.PetDamageBuyAmount;
                    bought += world.Settings.PetDamageBuyAmount;
                    soldexp += expcost;
                }
                else
                {
                    break;
                }
            }

            if (bought == 0)
            {
                return new PetResult(false, pet.Name + " doesn't have enough experience.");
            }

            return new PetResult(true, "Bought " + bought + " damage for " + soldexp + " experience.");
        }

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
