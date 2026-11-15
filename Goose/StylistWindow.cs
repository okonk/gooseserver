namespace Goose
{
    public class StylistWindow : Window
    {
        public const int MaleBodyID = 1;
        public const int FemaleBodyID = 11;

        public StylistWindow(GameWorld world, Player player, NPC npc)
        {
            this.ID = ++player.LastWindowID;
            this.Frame = WindowFrames.Stylist;
            this.Type = WindowTypes.Stylist;
            this.Title = "Stylist";
            this.Buttons = "0,1,0,0,1";
            this.NPC = npc;

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public static void Open(GameWorld world, Player player, NPC npc)
        {
            FindOpen(player)?.Close(player, world);
            _ = new StylistWindow(world, player, npc);
        }

        public static StylistWindow? FindOpen(Player player)
        {
            return player.Windows.OfType<StylistWindow>().FirstOrDefault();
        }

        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case ButtonTypes.Exit:
                case ButtonTypes.Close:
                    player.Windows.Remove(this);
                    break;
            }
        }

        public override void Populate(Player player, GameWorld world)
        {
        }

        public static void Change(GameWorld world, Player player, int hair, int face, int body)
        {
            StylistWindow? window = FindOpen(player);
            if (window is null) return;

            if (!Map.InRange(player, window.NPC!))
            {
                world.Send(player, P.Notice(NoticeType.Info, "You are too far from the stylist."));
                return;
            }

            GooseSettings settings = world.Settings;
            if (hair < 1 || hair > settings.StylistMaxHairID
                || face < 1 || face > settings.StylistMaxFaceID
                || (body != player.BodyID && body != MaleBodyID && body != FemaleBodyID))
            {
                return;
            }

            long cost = (long)settings.StylistChangeCost
                * ((hair != player.HairID ? 1 : 0) + (face != player.FaceID ? 1 : 0) + (body != player.BodyID ? 1 : 0));
            if (cost < 0) return;
            if (cost == 0)
            {
                world.Send(player, P.Notice(NoticeType.Info, "Nothing changed."));
                return;
            }

            if (player.Gold < cost)
            {
                world.Send(player, P.Notice(NoticeType.Info, "You need " + cost + " gold."));
                return;
            }

            int oldHair = player.HairID;
            int oldFace = player.FaceID;
            int oldBody = player.BodyID;
            player.RemoveGold(cost, world);
            player.HairID = hair;
            player.FaceID = face;
            if (player.CurrentBodyID == player.BodyID) player.CurrentBodyID = body;
            player.BodyID = body;

            string packet = P.UpdateCharacter(player);
            world.Send(player, packet);
            foreach (var p in player.Map.GetPlayersInRange(player))
            {
                world.Send(p, packet);
            }

            world.Send(player, P.Notice(NoticeType.Info, "Your new look cost " + cost + " gold."));
            world.LogHandler.Log(Log.Types.Stylist, player,
                "Stylist: hair " + oldHair + " -> " + hair + " face " + oldFace + " -> " + face + " body " + oldBody + " -> " + body + " cost " + cost);
        }
    }
}
