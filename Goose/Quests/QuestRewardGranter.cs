namespace Goose.Quests
{
    public static class QuestRewardGranter
    {
        public static void Give(QuestReward reward, NPC npc, Player player, GameWorld world)
        {
            string prefix = "[Quest Reward]: ";
            string? rewardMessage = null;
            switch (reward.Type)
            {
                case RewardType.Gold:
                    player.AddGold(reward.LongValue, world);
                    rewardMessage = $"{prefix}{reward.LongValue} gold";
                    break;
                case RewardType.Item:
                    int id = (int)reward.LongValue;
                    int stack = (int)reward.LongValue2;

                    ItemTemplate? template = world.ItemHandler.GetTemplate(id);
                    if (template is null) return;

                    Item item = new Item();
                    if (!item.LoadFromTemplate(template)) return;
                    world.ItemHandler.RollTitleAndSurname(item, world);
                    world.ItemHandler.AddAndAssignId(item, world);

                    player.Inventory.AddItem(item, stack, world);

                    rewardMessage = $"{prefix}Item: {item.Name} ({stack})";
                    break;
                case RewardType.Title:
                    player.GrantTitle(reward.StringValue, world);
                    rewardMessage = $"{prefix}Title: {reward.StringValue}";
                    break;
                case RewardType.Surname:
                    player.GrantSurname(reward.StringValue, world);
                    rewardMessage = $"{prefix}Surname: {reward.StringValue}";
                    break;
                case RewardType.Teleport:
                    string[] m = reward.StringValue.Split(',');
                    int mapId = 0;
                    int x = 0;
                    int y = 0;
                    if (!int.TryParse(m[0], out mapId))
                        return;
                    if (!int.TryParse(m[1], out x))
                        return;
                    if (!int.TryParse(m[2], out y))
                        return;

                    Map? map = world.MapHandler.GetMap(mapId);
                    if (map is null)
                        return;

                    player.WarpTo(world, map, x, y);
                    rewardMessage = $"{prefix}Teleport to {map.Name}";
                    break;
                case RewardType.Experience:
                    player.AddExperience(reward.LongValue, world, Player.ExperienceMessage.None);
                    rewardMessage = $"{prefix}{reward.LongValue} experience";
                    break;
                case RewardType.FaceGraphic:
                    player.FaceID = (int)reward.LongValue;
                    player.SendCHPString(world);
                    rewardMessage = $"{prefix}changed face";
                    break;
                case RewardType.BodyGraphic:
                    player.BodyID = (int)reward.LongValue;
                    player.SendCHPString(world);
                    rewardMessage = $"{prefix}changed body";
                    break;
                case RewardType.HairGraphic:
                    player.HairID = (int)reward.LongValue;
                    player.SendCHPString(world);
                    rewardMessage = $"{prefix}changed hair";
                    break;
                case RewardType.HairColour:
                    int[] rgba = reward.StringValue.Split(',').Select(s => int.Parse(s)).ToArray();
                    player.HairR = rgba[0];
                    player.HairG = rgba[1];
                    player.HairB = rgba[2];
                    player.HairA = rgba[3];
                    player.SendCHPString(world);
                    rewardMessage = $"{prefix}changed hair colour";
                    break;
                case RewardType.BodyColour:
                    int[] brgba = reward.StringValue.Split(',').Select(s => int.Parse(s)).ToArray();
                    player.BodyR = brgba[0];
                    player.BodyG = brgba[1];
                    player.BodyB = brgba[2];
                    player.BodyA = brgba[3];
                    player.SendCHPString(world);
                    rewardMessage = $"{prefix}changed body colour";
                    break;
                case RewardType.ClassChange:
                    player.ChangeClass((int)reward.LongValue, 5, world);
                    rewardMessage = $"{prefix}Class changed to {player.Class.ClassName}";
                    break;
                case RewardType.HP:
                    AddPlayerStats(new AttributeSet() { HP = reward.LongValue }, player, world);
                    rewardMessage = $"{prefix}{reward.LongValue} HP";
                    break;
                case RewardType.MP:
                    AddPlayerStats(new AttributeSet() { MP = reward.LongValue }, player, world);
                    rewardMessage = $"{prefix}{reward.LongValue} MP";
                    break;
                case RewardType.AC:
                    AddPlayerStats(new AttributeSet() { AC = (int)reward.LongValue }, player, world);
                    rewardMessage = $"{prefix}{reward.LongValue} AC";
                    break;
                case RewardType.Stamina:
                    AddPlayerStats(new AttributeSet() { Stamina = (int)reward.LongValue }, player, world);
                    rewardMessage = $"{prefix}{reward.LongValue} Stamina";
                    break;
                case RewardType.Strength:
                    AddPlayerStats(new AttributeSet() { Strength = (int)reward.LongValue }, player, world);
                    rewardMessage = $"{prefix}{reward.LongValue} Strength";
                    break;
                case RewardType.Dexterity:
                    AddPlayerStats(new AttributeSet() { Dexterity = (int)reward.LongValue }, player, world);
                    rewardMessage = $"{prefix}{reward.LongValue} Dexterity";
                    break;
                case RewardType.Intelligence:
                    AddPlayerStats(new AttributeSet() { Intelligence = (int)reward.LongValue }, player, world);
                    rewardMessage = $"{prefix}{reward.LongValue} Intelligence";
                    break;
                case RewardType.SpellBuff:
                    var spellEffect = world.SpellHandler.GetSpellEffect((int)reward.LongValue);
                    if (spellEffect is null)
                        return;

                    spellEffect.Cast(npc, player, world);
                    rewardMessage = $"{prefix}Spell Effect: {spellEffect.Name}";
                    break;
                case RewardType.LearnSpell:
                    player.LearnSpell((int)reward.LongValue, world);
                    break;
                case RewardType.Script:
                    reward.Script!.Object.GiveReward(reward, npc, player, world);
                    break;
            }

            if (rewardMessage is not null)
                world.Send(player, P.ServerMessage(rewardMessage));
        }

        public static int RequiredInventorySlots(IEnumerable<QuestReward> rewards, Player player, GameWorld world)
        {
            // assume 1 slot per reward item
            var requiredSlots = rewards.Count(r => r.Type == RewardType.Item);

            // A script reporting 0 or less cannot loosen the gate: AddItem returns false when it
            // finds no room and GiveRewards ignores the result, so the reward would be dropped
            // after the quest was already marked complete.
            foreach (var reward in rewards.Where(r => r.Type == RewardType.Script))
            {
                var required = reward.Script?.Object.GetRequiredInventorySpace(reward, player, world) ?? 0;
                if (required > 0) requiredSlots += required;
            }

            return requiredSlots;
        }

        private static void AddPlayerStats(AttributeSet stats, Player player, GameWorld world)
        {
            player.RemoveStats(player.BaseStats, world);
            player.BaseStats += stats;
            player.AddStats(player.BaseStats, world);
        }
    }
}
