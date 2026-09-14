using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuminaRift
{
    [Serializable]
    public sealed class LevelMilestone
    {
        [Min(2)] public int level = 10;
        [Min(1f)] public float incomeMultiplier = 2f;
        [Min(0)] public int standardTickets = 1;
    }

    public struct AscensionReward
    {
        public int Lumina;
        public int Power;

        public AscensionReward(int lumina, int power)
        {
            Lumina = lumina;
            Power = power;
        }
    }

    [CreateAssetMenu(fileName = "PrototypeGameConfig", menuName = "Lumina Rift/Prototype Game Config")]
    public sealed class PrototypeGameConfig : ScriptableObject
    {
        [SerializeField, HideInInspector] private int prototypeVersion = 2;
        [Header("Content")]
        [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
        [SerializeField] private CharacterData startingCharacter;
        [SerializeField] private List<BannerData> banners = new List<BannerData>();
        [Header("Levelling")]
        [SerializeField, Min(51)] private int runLevelCap = 100;
        [SerializeField, Min(0.01f)] private float firstLevelCost = 8f;
        [SerializeField, Min(1f)] private float levelCostGrowth = 1.14f;
        [SerializeField] private List<LevelMilestone> milestones = new List<LevelMilestone>();
        [Header("Affinity")]
        [SerializeField, Min(1)] private int affinityRankTwoXp = 25;
        [SerializeField, Min(2)] private int affinityRankThreeXp = 75;
        [SerializeField, Min(0f)] private float rankTwoClickBonus = 0.10f;
        [SerializeField, Min(0f)] private float rankThreePassiveBonus = 0.10f;
        [SerializeField, Min(0)] private int threeStarDuplicateAffinity = 10;
        [SerializeField, Min(0)] private int fourStarDuplicateAffinity = 25;
        [SerializeField, Min(0)] private int fiveStarDuplicateAffinity = 75;
        [Header("Ascension")]
        [SerializeField, Min(2)] private int ascensionMinimumLevel = 50;
        [SerializeField, Min(0)] private int baseAscensionLumina = 100;
        [SerializeField, Min(0)] private int bonusLuminaPerExtraLevel = 20;
        [SerializeField, Min(1)] private int extraLevelsPerBonusPower = 10;
        [SerializeField, Min(0f)] private float permanentIncomeBonusPerPower = 0.25f;

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

        public int DuplicateAffinity(CharacterRarity rarity)
        {
            if (rarity == CharacterRarity.FiveStar) return fiveStarDuplicateAffinity;
            if (rarity == CharacterRarity.FourStar) return fourStarDuplicateAffinity;
            return threeStarDuplicateAffinity;
        }

        public AscensionReward GetAscensionReward(int level)
        {
            if (level < ascensionMinimumLevel) return new AscensionReward(0, 0);
            int extraLevels = level - ascensionMinimumLevel;
            return new AscensionReward(
                baseAscensionLumina + extraLevels * bonusLuminaPerExtraLevel,
                1 + extraLevels / Math.Max(1, extraLevelsPerBonusPower));
        }

        public int GetAffinityRank(int xp)
        {
            if (xp >= affinityRankThreeXp) return 3;
            if (xp >= affinityRankTwoXp) return 2;
            return 1;
        }

        public void ConfigurePrototype(IList<CharacterData> roster, IList<BannerData> bannerList)
        {
            prototypeVersion = 2;
            characters = new List<CharacterData>(roster);
            startingCharacter = characters.Count > 0 ? characters[0] : null;
            banners = new List<BannerData>(bannerList);
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
