using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuminaRift
{
    [Serializable]
    public sealed class LevelMilestone
    {
        public int level;
        public float incomeMultiplier;
        public int standardTickets;
    }

    public struct AscensionReward
    {
        public int Lumina;
        public int Power;
        public AscensionReward(int lumina, int power) { Lumina = lumina; Power = power; }
    }

    [CreateAssetMenu(fileName = "PrototypeGameConfig", menuName = "Lumina Rift/Game Config")]
    public sealed class PrototypeGameConfig : ScriptableObject
    {
        [SerializeField, HideInInspector] private int prototypeVersion = 4;
        [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
        [SerializeField] private CharacterData startingCharacter;
        [SerializeField] private List<BannerData> banners = new List<BannerData>();
        [SerializeField, Min(51)] private int runLevelCap = 100;
        [SerializeField, Min(0.01f)] private float firstLevelCost = 8f;
        [SerializeField, Min(1f)] private float levelCostGrowth = 1.14f;
        [SerializeField] private List<LevelMilestone> milestones = new List<LevelMilestone>();
        [SerializeField] private int affinityRankTwoXp = 25;
        [SerializeField] private int affinityRankThreeXp = 75;
        [SerializeField] private float rankTwoClickBonus = 0.1f;
        [SerializeField] private float rankThreePassiveBonus = 0.1f;
        [SerializeField] private int threeStarDuplicateAffinity = 10;
        [SerializeField] private int fourStarDuplicateAffinity = 25;
        [SerializeField] private int fiveStarDuplicateAffinity = 75;
        [SerializeField] private int ascensionMinimumLevel = 50;
        [SerializeField] private int baseAscensionLumina = 100;
        [SerializeField] private int bonusLuminaPerExtraLevel = 20;
        [SerializeField] private int extraLevelsPerBonusPower = 10;
        [SerializeField] private float permanentIncomeBonusPerPower = 0.25f;
        [SerializeField] private float offlineEarningsCapHours = 8f;
        [SerializeField] private int levelTenUnlockAscensions = 2;
        [SerializeField] private int levelMaxUnlockAscensions = 3;
        [SerializeField] private int autoLevelUnlockAscensions = 5;
        [SerializeField] private int startingLevelUnlockAscensions = 10;
        [SerializeField] private int permanentStartingLevel = 5;

        public int PrototypeVersion { get { return prototypeVersion; } }
        public IReadOnlyList<CharacterData> Characters { get { return characters; } }
        public CharacterData StartingCharacter { get { return startingCharacter; } }
        public IReadOnlyList<BannerData> Banners { get { return banners; } }
        public int RunLevelCap { get { return runLevelCap; } }
        public float FirstLevelCost { get { return firstLevelCost; } }
        public float LevelCostGrowth { get { return levelCostGrowth; } }
        public IReadOnlyList<LevelMilestone> Milestones { get { return milestones; } }
        public int AffinityRankTwoXp { get { return affinityRankTwoXp; } }
        public int AffinityRankThreeXp { get { return affinityRankThreeXp; } }
        public float RankTwoClickBonus { get { return rankTwoClickBonus; } }
        public float RankThreePassiveBonus { get { return rankThreePassiveBonus; } }
        public int AscensionMinimumLevel { get { return ascensionMinimumLevel; } }
        public float PermanentIncomeBonusPerPower { get { return permanentIncomeBonusPerPower; } }
        public float OfflineEarningsCapHours { get { return offlineEarningsCapHours; } }
        public int LevelTenUnlockAscensions { get { return levelTenUnlockAscensions; } }
        public int LevelMaxUnlockAscensions { get { return levelMaxUnlockAscensions; } }
        public int AutoLevelUnlockAscensions { get { return autoLevelUnlockAscensions; } }
        public int StartingLevelUnlockAscensions { get { return startingLevelUnlockAscensions; } }
        public int PermanentStartingLevel { get { return permanentStartingLevel; } }

        public int DuplicateAffinity(CharacterRarity rarity) { return rarity == CharacterRarity.FiveStar ? fiveStarDuplicateAffinity : rarity == CharacterRarity.FourStar ? fourStarDuplicateAffinity : threeStarDuplicateAffinity; }
        public int GetAffinityRank(int xp) { return xp >= affinityRankThreeXp ? 3 : xp >= affinityRankTwoXp ? 2 : 1; }
        public AscensionReward GetAscensionReward(int level)
        {
            if (level < ascensionMinimumLevel) return new AscensionReward();
            int extra = level - ascensionMinimumLevel;
            return new AscensionReward(baseAscensionLumina + extra * bonusLuminaPerExtraLevel, 1 + extra / Math.Max(1, extraLevelsPerBonusPower));
        }

        public void Configure(IList<CharacterData> roster, IList<BannerData> bannerList)
        {
            prototypeVersion = 4; characters = new List<CharacterData>(roster); startingCharacter = characters[0]; banners = new List<BannerData>(bannerList);
            milestones = new List<LevelMilestone>
            {
                new LevelMilestone { level = 10, incomeMultiplier = 2f, standardTickets = 2 },
                new LevelMilestone { level = 25, incomeMultiplier = 2f, standardTickets = 4 },
                new LevelMilestone { level = 50, incomeMultiplier = 3f, standardTickets = 10 },
                new LevelMilestone { level = 75, incomeMultiplier = 1.75f, standardTickets = 5 },
                new LevelMilestone { level = 100, incomeMultiplier = 2f, standardTickets = 10 }
            };
        }
    }
}
