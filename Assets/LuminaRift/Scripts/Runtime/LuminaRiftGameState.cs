using System;
using System.Collections.Generic;
using System.Linq;

namespace LuminaRift
{
    public sealed class LuminaRiftGameState
    {
        private readonly PrototypeGameConfig config;
        private readonly List<CharacterRuntimeState> roster = new List<CharacterRuntimeState>();
        private readonly HashSet<string> claimedMilestones = new HashSet<string>();
        private GachaManager gacha;

        public event Action Changed;
        public event Action<string> MessageRaised;
        public IReadOnlyList<CharacterRuntimeState> Roster { get { return roster; } }
        public IReadOnlyList<BannerData> Banners { get { return config.Banners; } }
        public CharacterRuntimeState ActiveCharacter { get; private set; }
        public double Credits { get; private set; }
        public int StandardTickets { get; private set; }
        public int Lumina { get; private set; }
        public int AscensionPower { get; private set; }
        public int AscensionCount { get; private set; }
        public int RiftRank { get { return AscensionPower; } }
        public bool AutoLevelEnabled { get; private set; }
        public TelemetrySaveData Telemetry { get; private set; } = new TelemetrySaveData();
        public bool LevelTenUnlocked { get { return AscensionCount >= config.LevelTenUnlockAscensions; } }
        public bool LevelMaxUnlocked { get { return AscensionCount >= config.LevelMaxUnlockAscensions; } }
        public bool AutoLevelUnlocked { get { return AscensionCount >= config.AutoLevelUnlockAscensions; } }
        public bool StartingLevelUnlocked { get { return AscensionCount >= config.StartingLevelUnlockAscensions; } }
        public float AscensionMultiplier { get { return 1f + AscensionPower * config.PermanentIncomeBonusPerPower; } }
        public double ClickIncome { get { return CalculateIncome(true); } }
        public double PassiveIncomePerSecond { get { return CalculateIncome(false); } }
        public bool CanAscend { get { return ActiveCharacter != null && ActiveCharacter.Level >= config.AscensionMinimumLevel; } }
        public AscensionReward ProjectedAscensionReward { get { return config.GetAscensionReward(ActiveCharacter == null ? 0 : ActiveCharacter.Level); } }

        public double NextLevelCost
        {
            get
            {
                if (ActiveCharacter == null || ActiveCharacter.Level >= config.RunLevelCap) return 0;
                return config.FirstLevelCost * ActiveCharacter.Definition.LevelCostMultiplier * Math.Pow(config.LevelCostGrowth, ActiveCharacter.Level - 1);
            }
        }
        public bool CanLevel { get { return ActiveCharacter != null && ActiveCharacter.Level < config.RunLevelCap && Credits >= NextLevelCost; } }

        public LuminaRiftGameState(PrototypeGameConfig gameConfig, int? seed = null)
        { config = gameConfig; InitializeFresh(seed); }

        public void EarnClick()
        {
            double amount = ClickIncome; Credits += amount; Telemetry.lifetimeCreditsEarned += amount; Telemetry.totalClicks++; Notify();
        }

        public void Tick(float seconds)
        {
            if (seconds <= 0) return;
            Telemetry.lifetimePlaySeconds += seconds; Telemetry.currentRunSeconds += seconds;
            double amount = PassiveIncomePerSecond * seconds; Credits += amount; Telemetry.lifetimeCreditsEarned += amount;
            if (AutoLevelEnabled && AutoLevelUnlocked)
            {
                if (BuyLevels(int.MaxValue) == 0) Notify();
            }
            else Notify();
        }

        public void CollectOfflineCredits(double amount)
        {
            amount = Math.Max(0, amount); Credits += amount; Telemetry.lifetimeCreditsEarned += amount;
            Raise("Offline earnings collected: " + amount.ToString("0.##") + " Credits."); Notify();
        }

        public int BuyLevels(int requested)
        {
            int bought = 0;
            while (bought < requested && CanLevel)
            {
                Credits -= NextLevelCost; ActiveCharacter.GainLevel(); bought++; AwardMilestones();
                Telemetry.highestLevelReached = Math.Max(Telemetry.highestLevelReached, ActiveCharacter.Level);
            }
            if (bought > 0) { Raise(ActiveCharacter.Definition.DisplayName + " gained " + bought + " level" + (bought == 1 ? "." : "s.")); Notify(); }
            return bought;
        }

        public void SetAutoLevel(bool enabled)
        { AutoLevelEnabled = enabled && AutoLevelUnlocked; Raise("Auto-Level " + (AutoLevelEnabled ? "enabled." : "disabled.")); Notify(); }

        public bool SetActiveCharacter(CharacterRuntimeState character)
        {
            if (character == null || !character.IsOwned || !roster.Contains(character)) return false;
            ActiveCharacter = character; Raise(character.Definition.DisplayName + " is now active."); Notify(); return true;
        }

        public bool TrySummon(BannerData banner, int count, out List<GachaResult> results)
        {
            results = null; if (banner == null || (count != 1 && count != 10)) return false;
            int cost = count == 10 ? banner.TenPullCost : banner.SinglePullCost;
            if (CurrencyFor(banner) < cost) return false;
            if (banner.Currency == BannerCurrency.Lumina) Lumina -= cost; else StandardTickets -= cost;
            results = gacha.Pull(banner, count); Telemetry.totalPulls += results.Count;
            Telemetry.fiveStarPulls += results.Count(item => item.Character.Definition.Rarity == CharacterRarity.FiveStar);
            Raise(count + " pull(s) opened on " + banner.DisplayName + "."); Notify(); return true;
        }

        public bool TryAscend()
        {
            if (!CanAscend) return false;
            AscensionReward reward = ProjectedAscensionReward; Lumina += reward.Lumina; AscensionPower += reward.Power; AscensionCount++;
            Telemetry.lastAscensionRunSeconds = Telemetry.currentRunSeconds; Telemetry.currentRunSeconds = 0;
            Credits = 0; claimedMilestones.Clear();
            int startingLevel = StartingLevelUnlocked ? config.PermanentStartingLevel : 1;
            foreach (CharacterRuntimeState character in roster) character.ResetLevel(startingLevel);
            Raise("Ascension " + AscensionCount + " complete: +" + reward.Lumina + " Lumina, +" + reward.Power + " Power."); Notify(); return true;
        }

        public int CurrencyFor(BannerData banner) { return banner.Currency == BannerCurrency.Lumina ? Lumina : StandardTickets; }
        public int GetPity(BannerData banner) { return gacha.GetPity(banner); }
        public int GetAffinityRank(CharacterRuntimeState character) { return config.GetAffinityRank(character.AffinityXp); }

        public PlayerSaveData CreateSave(double pendingOfflineCredits = 0, double pendingOfflineSeconds = 0)
        {
            var save = new PlayerSaveData
            {
                credits = Credits, standardTickets = StandardTickets, lumina = Lumina, ascensionPower = AscensionPower,
                ascensionCount = AscensionCount, autoLevelEnabled = AutoLevelEnabled, activeCharacterId = ActiveCharacter == null ? "" : ActiveCharacter.Definition.CharacterId,
                pity = gacha.ExportPity(), claimedRunMilestones = claimedMilestones.ToList(), telemetry = Telemetry,
                pendingOfflineCredits = pendingOfflineCredits, pendingOfflineSeconds = pendingOfflineSeconds
            };
            foreach (CharacterRuntimeState character in roster)
                save.characters.Add(new CharacterSaveRecord { characterId = character.Definition.CharacterId, isOwned = character.IsOwned, level = character.Level, affinityXp = character.AffinityXp });
            return save;
        }

        public void Restore(PlayerSaveData save)
        {
            if (save == null) return;
            Credits = Math.Max(0, save.credits); StandardTickets = Math.Max(0, save.standardTickets); Lumina = Math.Max(0, save.lumina);
            AscensionPower = Math.Max(0, save.ascensionPower); AscensionCount = Math.Max(0, save.ascensionCount);
            foreach (CharacterSaveRecord record in save.characters ?? new List<CharacterSaveRecord>())
            {
                CharacterRuntimeState character = roster.FirstOrDefault(item => item.Definition.CharacterId == record.characterId);
                if (character != null) character.Restore(record.isOwned, Math.Min(config.RunLevelCap, record.level), record.affinityXp);
            }
            ActiveCharacter = roster.FirstOrDefault(item => item.IsOwned && item.Definition.CharacterId == save.activeCharacterId) ?? roster.FirstOrDefault(item => item.IsOwned);
            if (ActiveCharacter == null && roster.Count > 0) { ActiveCharacter = roster[0]; ActiveCharacter.Unlock(); }
            claimedMilestones.Clear(); foreach (string claim in save.claimedRunMilestones ?? new List<string>()) claimedMilestones.Add(claim);
            gacha.RestorePity(save.pity); Telemetry = save.telemetry ?? new TelemetrySaveData();
            AutoLevelEnabled = save.autoLevelEnabled && AutoLevelUnlocked; Notify();
        }

        public void ResetAllProgress()
        { InitializeFresh(null); Raise("Save reset. A fresh Rift has opened."); Notify(); }

        private void InitializeFresh(int? seed)
        {
            roster.Clear(); claimedMilestones.Clear(); Credits = 0; StandardTickets = 0; Lumina = 0; AscensionPower = 0; AscensionCount = 0; AutoLevelEnabled = false;
            Telemetry = new TelemetrySaveData();
            foreach (CharacterData data in config.Characters.Where(item => item != null)) roster.Add(new CharacterRuntimeState(data, data == config.StartingCharacter));
            ActiveCharacter = roster.FirstOrDefault(item => item.IsOwned) ?? roster.FirstOrDefault(); if (ActiveCharacter != null) ActiveCharacter.Unlock();
            gacha = new GachaManager(config, roster, seed);
        }

        private double CalculateIncome(bool click)
        {
            if (ActiveCharacter == null) return 0;
            CharacterData data = ActiveCharacter.Definition; int rank = config.GetAffinityRank(ActiveCharacter.AffinityXp);
            double affinity = 1 + (click && rank >= 2 ? config.RankTwoClickBonus : !click && rank >= 3 ? config.RankThreePassiveBonus : 0);
            double milestone = 1; foreach (LevelMilestone item in config.Milestones) if (ActiveCharacter.Level >= item.level) milestone *= item.incomeMultiplier;
            return (click ? data.BaseClickIncome : data.BasePassiveIncome) * Math.Pow(ActiveCharacter.Level, click ? data.ClickLevelExponent : data.PassiveLevelExponent) * milestone * affinity * AscensionMultiplier;
        }

        private void AwardMilestones()
        {
            foreach (LevelMilestone milestone in config.Milestones)
            {
                if (ActiveCharacter.Level < milestone.level) continue;
                string id = ActiveCharacter.Definition.CharacterId + ":" + milestone.level;
                if (claimedMilestones.Add(id)) { StandardTickets += milestone.standardTickets; Raise("Level " + milestone.level + ": +" + milestone.standardTickets + " Tickets."); }
            }
        }

        private void Raise(string message) { if (MessageRaised != null) MessageRaised(message); }
        private void Notify() { if (Changed != null) Changed(); }
    }
}
