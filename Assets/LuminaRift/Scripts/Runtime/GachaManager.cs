using System;
using System.Collections.Generic;
using System.Linq;

namespace LuminaRift
{
    public sealed class GachaResult
    {
        public CharacterRuntimeState Character { get; private set; }
        public bool WasDuplicate { get; private set; }
        public int AffinityAwarded { get; private set; }

        public GachaResult(CharacterRuntimeState character, bool duplicate, int affinityAwarded)
        {
            Character = character;
            WasDuplicate = duplicate;
            AffinityAwarded = affinityAwarded;
        }
    }

    public sealed class GachaManager
    {
        private readonly PrototypeGameConfig config;
        private readonly IReadOnlyList<CharacterRuntimeState> roster;
        private readonly Dictionary<string, int> pityByBanner = new Dictionary<string, int>();
        private readonly Random random;

        public GachaManager(PrototypeGameConfig gameConfig, IReadOnlyList<CharacterRuntimeState> characterRoster, int? seed = null)
        {
            config = gameConfig;
            roster = characterRoster;
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int GetPity(BannerData banner)
        {
            int value;
            return banner != null && pityByBanner.TryGetValue(banner.BannerId, out value) ? value : 0;
        }

        public List<GachaResult> Pull(BannerData banner, int count)
        {
            var results = new List<GachaResult>();
            bool hasHighRarity = false;
            for (int i = 0; i < count; i++)
            {
                bool guaranteeFourPlus = count == 10 && i == count - 1 && !hasHighRarity;
                CharacterRarity rarity = RollRarity(banner, guaranteeFourPlus);
                CharacterRuntimeState pulled = PickCharacter(banner, rarity);
                if (pulled == null) continue;

                bool duplicate = pulled.IsOwned;
                int affinity = 0;
                if (duplicate)
                {
                    affinity = config.DuplicateAffinity(pulled.Definition.Rarity);
                    pulled.AddAffinity(affinity);
                }
                else
                {
                    pulled.Unlock();
                }

                if (rarity >= CharacterRarity.FourStar) hasHighRarity = true;
                results.Add(new GachaResult(pulled, duplicate, affinity));
            }
            return results;
        }

        private CharacterRarity RollRarity(BannerData banner, bool guaranteeFourPlus)
        {
            int pity = GetPity(banner);
            CharacterRarity rarity;
            if (pity + 1 >= banner.HardPity)
            {
                rarity = CharacterRarity.FiveStar;
            }
            else if (guaranteeFourPlus)
            {
                double highTotal = banner.FourStarRate + banner.FiveStarRate;
                rarity = highTotal > 0d && random.NextDouble() * highTotal < banner.FiveStarRate
                    ? CharacterRarity.FiveStar
                    : CharacterRarity.FourStar;
            }
            else
            {
                double total = banner.ThreeStarRate + banner.FourStarRate + banner.FiveStarRate;
                double roll = random.NextDouble() * Math.Max(0.0001d, total);
                if (roll < banner.FiveStarRate) rarity = CharacterRarity.FiveStar;
                else if (roll < banner.FiveStarRate + banner.FourStarRate) rarity = CharacterRarity.FourStar;
                else rarity = CharacterRarity.ThreeStar;
            }

            pityByBanner[banner.BannerId] = rarity == CharacterRarity.FiveStar ? 0 : pity + 1;
            return rarity;
        }

        private CharacterRuntimeState PickCharacter(BannerData banner, CharacterRarity rarity)
        {
            if (rarity == CharacterRarity.FiveStar && banner.RateUpCharacter != null &&
                random.NextDouble() < banner.RateUpShareOfFiveStar)
            {
                CharacterRuntimeState rateUp = roster.FirstOrDefault(item => item.Definition == banner.RateUpCharacter);
                if (rateUp != null) return rateUp;
            }

            var candidates = roster.Where(item =>
                item.Definition.Rarity == rarity && banner.CharacterPool.Contains(item.Definition)).ToList();
            if (candidates.Count == 0)
            {
                candidates = roster.Where(item => banner.CharacterPool.Contains(item.Definition)).ToList();
            }
            return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
        }
    }
}
