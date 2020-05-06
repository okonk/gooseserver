using System;

namespace Goose
{
    public class GooseSettings
    {
        public string ServerVersion { get; set; }
        public string ServerType { get; set; }
        public string DatabaseName { get; set; }
        public string DataLink { get; set; }
        public string DataPath { get; set; }
        public string ServerName { get; set; }
        public int StartingMapID { get; set; }
        public int StartingMapX { get; set; }
        public int StartingMapY { get; set; }
        public string StartingItems { get; set; }
        public int EquippedSize { get; set; }
        public int SpellbookSize { get; set; }
        public int StartingBodyState { get; set; }
        public int StartingBankPages { get; set; }
        public int BankSlotsPerPage { get; set; }


        public bool AutoCharacterCreation { get; set; }
        public string GameServerIP { get; set; }
        public int GameServerPort { get; set; }
        public int MaxPlayers { get; set; }
        public int BaseHaste { get; set; }
        public int BaseSpellDamage { get; set; }
        public int BaseSpellCrit { get; set; }
        public int BaseMeleeDamage { get; set; }
        public int BaseMeleeCrit { get; set; }
        public int BaseDamageReduction { get; set; }
        public decimal BaseHPPercentRegen { get; set; }
        public int BaseHPStaticRegen { get; set; }
        public decimal BaseMPPercentRegen { get; set; }
        public int BaseMPStaticRegen { get; set; }
        public int StartingHP { get; set; }
        public int StartingMP { get; set; }
        public int StartingSP { get; set; }
        public int StartingStrength { get; set; }
        public int StartingStamina { get; set; }
        public int StartingDexterity { get; set; }
        public int StartingIntelligence { get; set; }
        public int StartingAC { get; set; }
        public int StartingFireResist { get; set; }
        public int StartingWaterResist { get; set; }
        public int StartingAirResist { get; set; }
        public int StartingSpiritResist { get; set; }
        public int StartingEarthResist { get; set; }
        public int StartingGold { get; set; }
        public int StartingExperience { get; set; }
        public int StartingExperienceSold { get; set; }
        public int StartingLevel { get; set; }
        public int StartingClassID { get; set; }
        public int StartingGuildID { get; set; }
        public int StartingBodyID { get; set; }
        public int StartingFaceID { get; set; }
        public int StartingHairID { get; set; }
        public int StartingHairR { get; set; }
        public int StartingHairG { get; set; }
        public int StartingHairB { get; set; }
        public int StartingHairA { get; set; }
        public string MOTD { get; set; }
        public string StartingTitle { get; set; }
        public string StartingSurname { get; set; }
        public decimal RegenSpeed { get; set; }
        public int GoldItemID { get; set; }
        public int StaminaToHP { get; set; }
        public int IntelligenceToMP { get; set; }
        public decimal DamageModifier { get; set; }
        public decimal ExperienceModifier { get; set; }
        public int PlayerSavePeriod { get; set; }
        public int ItemProtectedTime { get; set; }
        public decimal SpellEffectPeriod { get; set; }
        public int VitaBuyAmount { get; set; }
        public int ManaBuyAmount { get; set; }
        public int IncreaseVitaBuyAmount { get; set; }
        public int IncreaseManaBuyAmount { get; set; }
        public long ExperienceCap { get; set; }
        public bool LockdownModeEnabled { get; set; }
        public int LogoutLagTime { get; set; }
        public int GuildCreationCost { get; set; }
        public string DefaultGuildMOTD { get; set; }
        public int GuildSavePeriod { get; set; }
        public int RankUpdatePeriod { get; set; }
        public int MaxAC { get; set; }
        public int ExperienceModifierLimit { get; set; }
        public decimal DropRateModifier { get; set; }
        public bool SpeedhackDetectionEnabled { get; set; }
        public int ItemGroundExistTime { get; set; }
        public int ItemGroundSweepTime { get; set; }
        public int DefaultAetherThreshold { get; set; }
        public int DefaultToggleSettings { get; set; }
        public int NewCharactersPerDayPerIP { get; set; }
        public int MaxNPCs { get; set; }
        public int PetVitaBuyAmount { get; set; }
        public int IncreasePetVitaBuyCost { get; set; }
        public int PetVitaCost { get; set; }
        public int PetDamageBuyAmount { get; set; }
        public int IncreasePetDamageBuyCost { get; set; }
        public int PetDamageCost { get; set; }
        public int PetCountLimit { get; set; }
        public int IdleTimeout { get; set; }
        public int PlayerCountExperienceModifierInterval { get; set; }
        public decimal PlayerCountExperienceModifier { get; set; }
        public int CreditUpdateInterval { get; set; }
        public decimal BaseMoveSpeedIncrease { get; set; }
        public int StartingMoveSpeed { get; set; }
        public int StartingBodyR { get; set; }
        public int StartingBodyG { get; set; }
        public int StartingBodyB { get; set; }
        public int StartingBodyA { get; set; }
        public int HairdyeCommandCost { get; set; }
        public int CustomTicketId { get; set; }
        public double RespawnTimeBackoff { get; set; }
        public double ChangeClassExperienceLossPercent { get; set; }
        public int BuffBarVisibleSize { get; set; }
        public int VendorSlotSize { get; set; }
        public int InventorySize { get; set; }
        public int PartyWindowMax { get; set; }
        public int CombineBagSize { get; set; }
        public int NumberOfRanks { get; set; }
        public int ItemIDStartpoint { get; set; }
    }
}