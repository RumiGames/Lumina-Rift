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
        public GachaResult(CharacterRuntimeState character, bool duplicate, int xp) { Character = character; WasDuplicate = duplicate; AffinityAwarded = xp; }
    }

    public sealed class GachaManager
    {
        private readonly PrototypeGameConfig config;
        private readonly IReadOnlyList<CharacterRuntimeState> roster;
        private readonly Dictionary<string, int> pity = new Dictionary<string, int>();
        private readonly Random random;

        public GachaManager(PrototypeGameConfig gameConfig, IReadOnlyList<CharacterRuntimeState> characterRoster, int? seed = null)
        { config = gameConfig; roster = characterRoster; random = seed.HasValue ? new Random(seed.Value) : new Random(); }

        public int GetPity(BannerData banner) { int value; return banner != null && pity.TryGetValue(banner.BannerId, out value) ? value : 0; }
        public List<PitySaveRecord> ExportPity() { return pity.Select(pair => new PitySaveRecord { bannerId = pair.Key, pullsSinceFiveStar = pair.Value }).ToList(); }
        public void RestorePity(IEnumerable<PitySaveRecord> records)
        {
            pity.Clear(); if (records == null) return;
            foreach (PitySaveRecord record in records) if (record != null && !string.IsNullOrEmpty(record.bannerId)) pity[record.bannerId] = Math.Max(0, record.pullsSinceFiveStar);
        }

        public List<GachaResult> Pull(BannerData banner, int count)
        {
            var results = new List<GachaResult>(); bool highRarity = false;
            for (int i = 0; i < count; i++)
            {
                CharacterRarity rarity = RollRarity(banner, count == 10 && i == 9 && !highRarity);
                CharacterRuntimeState character = PickCharacter(banner, rarity); if (character == null) continue;
                bool duplicate = character.IsOwned; int xp = 0;
                if (duplicate) { xp = config.DuplicateAffinity(character.Definition.Rarity); character.AddAffinity(xp); }
                else character.Unlock();
                if (rarity >= CharacterRarity.FourStar) highRarity = true;
                results.Add(new GachaResult(character, duplicate, xp));
            }
            return results;
        }

        private CharacterRarity RollRarity(BannerData banner, bool guaranteeFour)
        {
            int current = GetPity(banner); CharacterRarity result;
            if (current + 1 >= banner.HardPity) result = CharacterRarity.FiveStar;
            else if (guaranteeFour)
            {
                double high = banner.FourStarRate + banner.FiveStarRate;
                result = random.NextDouble() * Math.Max(0.001, high) < banner.FiveStarRate ? CharacterRarity.FiveStar : CharacterRarity.FourStar;
            }
            else
            {
                double total = banner.ThreeStarRate + banner.FourStarRate + banner.FiveStarRate;
                double roll = random.NextDouble() * Math.Max(0.001, total);
                result = roll < banner.FiveStarRate ? CharacterRarity.FiveStar : roll < banner.FiveStarRate + banner.FourStarRate ? CharacterRarity.FourStar : CharacterRarity.ThreeStar;
            }
            pity[banner.BannerId] = result == CharacterRarity.FiveStar ? 0 : current + 1;
            return result;
        }

        private CharacterRuntimeState PickCharacter(BannerData banner, CharacterRarity rarity)
        {
            if (rarity == CharacterRarity.FiveStar && banner.RateUpCharacter != null && random.NextDouble() < banner.RateUpShareOfFiveStar)
            {
                CharacterRuntimeState featured = roster.FirstOrDefault(item => item.Definition == banner.RateUpCharacter); if (featured != null) return featured;
            }
            List<CharacterRuntimeState> candidates = roster.Where(item => item.Definition.Rarity == rarity && banner.CharacterPool.Contains(item.Definition)
                && (rarity != CharacterRarity.FiveStar || item.Definition != banner.RateUpCharacter)).ToList();
            if (candidates.Count == 0) candidates = roster.Where(item => banner.CharacterPool.Contains(item.Definition)).ToList();
            return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
        }
    }
}
