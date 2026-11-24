using System.Globalization;

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
            var pet = PetOperations.Find(owner, petId);
            if (pet is null)
            {
                world.Send(owner, P.Notice(NoticeType.Info, "That pet is no longer yours."));
                Open(owner, world);
                return;
            }

            string ModeLine(Pet.Modes mode) =>
                pet.IsAlive && pet.Mode == mode ? $"* Mode: {mode}" : $"Mode: {mode}";

            var lines = new List<string>
            {
                "Details",
                pet.IsAlive ? "Despawn" : "Spawn",
                ModeLine(Pet.Modes.Neutral),
                ModeLine(Pet.Modes.Follow),
                ModeLine(Pet.Modes.Defend),
                PetOperations.IsMaxLevel(pet)
                    ? $"Buy HP (next: {PetOperations.NextVitaCost(pet, world.Settings).ToString("N0", CultureInfo.InvariantCulture)} exp)"
                    : "Buy HP (max level only)",
                PetOperations.IsMaxLevel(pet)
                    ? $"Buy Damage (next: {PetOperations.NextDamageCost(pet, world.Settings).ToString("N0", CultureInfo.InvariantCulture)} exp)"
                    : "Buy Damage (max level only)",
                "Release",
            };

            new OptionListWindow(owner, world, pet.Name, lines, (index, p, w) =>
            {
                var target = PetOperations.Find(p, petId);
                if (target is null)
                {
                    w.Send(p, P.Notice(NoticeType.Info, "That pet is no longer yours."));
                    Open(p, w);
                    return;
                }

                switch (index)
                {
                    case 0:
                        PetOperations.ShowInfo(p, target, w);
                        OpenPet(p, petId, w);
                        break;
                    case 1:
                    {
                        var r = target.IsAlive
                            ? PetOperations.TryDespawn(target, w)
                            : PetOperations.TrySpawn(p, target, w);
                        w.Send(p, P.Notice(NoticeType.Info, r.Message));
                        OpenPet(p, petId, w);
                        break;
                    }
                    case 2:
                    case 3:
                    case 4:
                    {
                        var mode = (Pet.Modes)(index - 2);
                        var r = PetOperations.SetMode(target, mode);
                        w.Send(p, P.Notice(NoticeType.Info, r.Message));
                        OpenPet(p, petId, w);
                        break;
                    }
                    case 5:
                    case 6:
                    {
                        long max = index == 5
                            ? PetOperations.MaxAffordableVita(target, w.Settings)
                            : PetOperations.MaxAffordableDamage(target, w.Settings);
                        if (max == 0)
                        {
                            string msg = PetOperations.IsMaxLevel(target)
                                ? target.Name + " doesn't have enough experience."
                                : target.Name + " must be max level to buy stats.";
                            w.Send(p, P.Notice(NoticeType.Info, msg));
                            OpenPet(p, petId, w);
                            break;
                        }
                        string stat = index == 5 ? "HP" : "damage";
                        new AmountWindow(p, w, index == 5 ? "Buy HP" : "Buy Damage",
                            $"How many {stat} buys for {target.Name}? (max {max.ToString(CultureInfo.InvariantCulture)})",
                            (0, 0, 0, 0, 0, 0), 1, max, 1, (n, pl, wd) =>
                            {
                                var bought = PetOperations.Find(pl, petId);
                                if (bought is null)
                                {
                                    wd.Send(pl, P.Notice(NoticeType.Info, "That pet is no longer yours."));
                                    Open(pl, wd);
                                    return;
                                }
                                var r = index == 5
                                    ? PetOperations.BuyVita(bought, n, wd)
                                    : PetOperations.BuyDamage(bought, n, wd);
                                wd.Send(pl, P.Notice(NoticeType.Info, r.Message));
                                OpenPet(pl, petId, wd);
                            });
                        break;
                    }
                }
            }, onBack: (p, w) => Open(p, w));
        }
    }
}
