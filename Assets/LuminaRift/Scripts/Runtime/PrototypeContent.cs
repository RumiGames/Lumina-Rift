using System.Collections.Generic;
using UnityEngine;

namespace LuminaRift
{
    public static class PrototypeContent
    {
        public static PrototypeGameConfig LoadOrCreate()
        {
            PrototypeGameConfig saved = Resources.Load<PrototypeGameConfig>("PrototypeGameConfig");
            if (saved != null && saved.PrototypeVersion >= 3) return saved;
            List<CharacterData> characters = CreateCharacters(); List<BannerData> banners = CreateBanners(characters);
            PrototypeGameConfig config = ScriptableObject.CreateInstance<PrototypeGameConfig>(); config.Configure(characters, banners); return config;
        }

        public static List<CharacterData> CreateCharacters()
        {
            return new List<CharacterData>
            {
                Character("lyra", "Lyra", "An efficient early-run Rift scout.", CharacterRarity.ThreeStar, 1f, .32f, .78f, 1.02f, .92f, new Color(.25f,.8f,1f)),
                Character("bramble", "Bramble", "A steady guardian with dependable passive income.", CharacterRarity.ThreeStar, .9f, .42f, .82f, .96f, 1.02f, new Color(.3f,.85f,.45f)),
                Character("nyx", "Nyx", "A balanced duelist who scales smoothly.", CharacterRarity.FourStar, 1.25f, .48f, 1f, 1.1f, 1.1f, new Color(.75f,.35f,1f)),
                Character("kael", "Kael", "A disciplined vanguard from the Ember Meridian.", CharacterRarity.FourStar, 1.45f, .38f, 1.05f, 1.15f, 1.05f, new Color(1f,.32f,.22f)),
                Character("solara", "Solara", "A radiant champion whose power blooms late.", CharacterRarity.FiveStar, 1.6f, .6f, 1.32f, 1.28f, 1.3f, new Color(1f,.65f,.2f)),
                Character("vesper", "Vesper", "A rare oracle with exceptional passive scaling.", CharacterRarity.FiveStar, 1.35f, .78f, 1.38f, 1.18f, 1.38f, new Color(1f,.35f,.75f))
            };
        }

        public static List<BannerData> CreateBanners(IList<CharacterData> characters)
        {
            BannerData standard = ScriptableObject.CreateInstance<BannerData>();
            standard.Configure("standard", "Standard Rift", "The full prototype roster.", BannerCurrency.StandardTickets, 1, 10, characters, null, new Color(.3f,.55f,.95f));
            BannerData featured = ScriptableObject.CreateInstance<BannerData>();
            var pool = new List<CharacterData> { characters[0], characters[2], characters[3], characters[4] };
            featured.Configure("featured_solara", "Radiant Convergence", "Solara receives 50% of 5-star results.", BannerCurrency.Lumina, 100, 1000, pool, characters[4], new Color(1f,.55f,.18f));
            return new List<BannerData> { standard, featured };
        }

        private static CharacterData Character(string id, string name, string bio, CharacterRarity rarity, float click, float passive, float cost, float clickExp, float passiveExp, Color color)
        { CharacterData value = ScriptableObject.CreateInstance<CharacterData>(); value.Configure(id, name, bio, rarity, click, passive, cost, clickExp, passiveExp, color); value.name = name; return value; }
    }
}
