using System;
using System.Collections.Generic;
using System.Linq;

namespace LuminaRift
{
    public sealed class LuminaRiftGameState
    {
        private readonly PrototypeGameConfig config;
        private readonly List<CharacterRuntimeState> roster = new List<CharacterRuntimeState>();
        private readonly HashSet<string> claimedRunMilestones = new HashSet<string>();
        private readonly GachaManager gacha;

        public event Action Changed;
        public event Action<string> MessageRaised;

        public IReadOnlyList<CharacterRuntimeState> Roster { get { return roster; } }
        public IReadOnlyList<BannerData> Banners { get { return config.Banners; } }
        public CharacterRuntimeState ActiveCharacter { get; private set; }
        public double Credits { get; private set; }
        public int StandardTickets { get; private set; }
        public int Lumina { get; private set; }
        public int AscensionPower { get; private set; }
        public float AscensionMultiplier { get { return 1f + AscensionPower * config.PermanentIncomeBonusPerPower; } }

        public LuminaRiftGameState(PrototypeGameConfig gameConfig, int? randomSeed = null)
        {
            config = gameConfig;
            foreach (CharacterData definition in config.Characters.Where(item => item != null))
            {
                roster.Add(new CharacterRuntimeState(definition, definition == config.StartingCharacter));
            }
            ActiveCharacter = roster.FirstOrDefault(item => item.IsOwned) ?? roster.FirstOrDefault();
            if (ActiveCharacter != null) ActiveCharacter.Unlock();
            gacha = new GachaManager(config, roster, randomSeed);
        }

        public double ClickIncome { get { return CalculateIncome(ActiveCharacter, true); } }
        public double PassiveIncomePerSecond { get { return CalculateIncome(ActiveCharacter, false); } }
        public AscensionReward ProjectedAscensionReward
        {
            get { return config.GetAscensionReward(ActiveCharacter == null ? 0 : ActiveCharacter.Level); }
        }
        public double NextLevelCost
        {
            get
            {
                if (ActiveCharacter == null || ActiveCharacter.Level >= config.RunLevelCap) return 0d;
                return config.FirstLevelCost * ActiveCharacter.Definition.LevelCostMultiplier *
                    Math.Pow(config.LevelCostGrowth, ActiveCharacter.Level - 1);
            }
        }
        public bool CanLevel { get { return ActiveCharacter != null && ActiveCharacter.Level < config.RunLevelCap && Credits >= NextLevelCost; } }
        public bool CanAscend { get { return ActiveCharacter != null && ActiveCharacter.Level >= config.AscensionMinimumLevel; } }

        public int GetAffinityRank(CharacterRuntimeState character) { return config.GetAffinityRank(character.AffinityXp); }
        public int GetPity(BannerData banner) { return gacha.GetPity(banner); }
        public int CurrencyFor(BannerData banner) { return banner.Currency == BannerCurrency.Lumina ? Lumina : StandardTickets; }

        public void EarnClick()
        {
            Credits += ClickIncome;
            NotifyChanged();
        }

        public void TickPassive(float seconds)
        {
            if (seconds <= 0f || ActiveCharacter == null) return;
            Credits += PassiveIncomePerSecond * seconds;
            NotifyChanged();
        }

        public bool TryLevelActiveCharacter()
        {
            if (!CanLevel) return false;
            Credits -= NextLevelCost;
            ActiveCharacter.GainLevel();
            AwardReachedMilestones(ActiveCharacter);
            RaiseMessage(ActiveCharacter.Definition.DisplayName + " reached level " + ActiveCharacter.Level + ".");
            NotifyChanged();
            return true;
        }

        public bool SetActiveCharacter(CharacterRuntimeState character)
        {
            if (character == null || !character.IsOwned || !roster.Contains(character)) return false;
            ActiveCharacter = character;
            RaiseMessage(character.Definition.DisplayName + " is now active.");
            NotifyChanged();
            return true;
        }

        public bool TrySummon(BannerData banner, int count, out List<GachaResult> results)
        {
            results = null;
            if (banner == null || (count != 1 && count != 10)) return false;
            int cost = count == 10 ? banner.TenPullCost : banner.SinglePullCost;
            if (CurrencyFor(banner) < cost) return false;
            if (banner.Currency == BannerCurrency.Lumina) Lumina -= cost;
            else StandardTickets -= cost;
            results = gacha.Pull(banner, count);
            RaiseMessage(count + " pull(s) opened on " + banner.DisplayName + ".");
            NotifyChanged();
            return true;
        }

        public bool TryAscend()
        {
            if (!CanAscend) return false;
            AscensionReward reward = ProjectedAscensionReward;
            Lumina += reward.Lumina;
            AscensionPower += reward.Power;
            Credits = 0d;
            claimedRunMilestones.Clear();
            foreach (CharacterRuntimeState character in roster) character.ResetLevel();
            RaiseMessage("Ascension complete! +" + reward.Lumina + " Lumina and +" + reward.Power + " Ascension Power.");
            NotifyChanged();
            return true;
        }

        private double CalculateIncome(CharacterRuntimeState character, bool click)
        {
            if (character == null) return 0d;
            CharacterData data = character.Definition;
            double baseIncome = click ? data.BaseClickIncome : data.BasePassiveIncome;
            double exponent = click ? data.ClickLevelExponent : data.PassiveLevelExponent;
            double affinityMultiplier = 1d;
            int rank = config.GetAffinityRank(character.AffinityXp);
            if (click && rank >= 2) affinityMultiplier += config.RankTwoClickBonus;
            if (!click && rank >= 3) affinityMultiplier += config.RankThreePassiveBonus;
            return baseIncome * Math.Pow(character.Level, exponent) * MilestoneMultiplier(character.Level) *
                affinityMultiplier * AscensionMultiplier;
        }

        private float MilestoneMultiplier(int level)
        {
            float multiplier = 1f;
            foreach (LevelMilestone milestone in config.Milestones)
                if (milestone != null && level >= milestone.level) multiplier *= milestone.incomeMultiplier;
            return multiplier;
        }

        private void AwardReachedMilestones(CharacterRuntimeState character)
        {
            foreach (LevelMilestone milestone in config.Milestones)
            {
                if (milestone == null || character.Level < milestone.level) continue;
                string claim = character.Definition.CharacterId + ":" + milestone.level;
                if (!claimedRunMilestones.Add(claim)) continue;
                StandardTickets += milestone.standardTickets;
                RaiseMessage("Level " + milestone.level + " milestone: x" + milestone.incomeMultiplier +
                    " income and +" + milestone.standardTickets + " Standard Tickets.");
            }
        }

        private void RaiseMessage(string message) { if (MessageRaised != null) MessageRaised(message); }
        private void NotifyChanged() { if (Changed != null) Changed(); }
    }
}
