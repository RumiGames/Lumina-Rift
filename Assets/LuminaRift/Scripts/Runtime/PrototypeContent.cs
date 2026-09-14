using System.Collections.Generic;
using UnityEngine;

namespace LuminaRift
{
    public static class PrototypeContent
    {
        public static PrototypeGameConfig LoadOrCreate()
        {
            PrototypeGameConfig saved = Resources.Load<PrototypeGameConfig>("PrototypeGameConfig");
            if (saved != null && saved.PrototypeVersion >= 2) return saved;

            var characters = CreateCharacters();
            var banners = CreateBanners(characters);
            PrototypeGameConfig config = ScriptableObject.CreateInstance<PrototypeGameConfig>();
            config.name = "Runtime Prototype 0.0.2 Config";
            config.ConfigurePrototype(characters, banners);
            return config;
        }

        public static List<CharacterData> CreateCharacters()
        {
            return new List<CharacterData>
            {
                CreateCharacter("lyra", "Lyra", "A bright-eyed Rift scout. Efficient in the opening stretch.", CharacterRarity.ThreeStar, 1f, 0.32f, 0.78f, 1.02f, 0.92f, new Color(0.25f, 0.8f, 1f)),
                CreateCharacter("bramble", "Bramble", "A steady guardian with dependable passive income.", CharacterRarity.ThreeStar, 0.9f, 0.42f, 0.82f, 0.96f, 1.02f, new Color(0.3f, 0.85f, 0.45f)),
                CreateCharacter("nyx", "Nyx", "A balanced duelist who scales smoothly through a run.", CharacterRarity.FourStar, 1.25f, 0.48f, 1f, 1.10f, 1.10f, new Color(0.75f, 0.35f, 1f)),
                CreateCharacter("kael", "Kael", "A disciplined vanguard from the Ember Meridian.", CharacterRarity.FourStar, 1.45f, 0.38f, 1.05f, 1.15f, 1.05f, new Color(1f, 0.32f, 0.22f)),
                CreateCharacter("solara", "Solara", "A radiant champion whose power blooms late in a run.", CharacterRarity.FiveStar, 1.6f, 0.6f, 1.32f, 1.28f, 1.30f, new Color(1f, 0.65f, 0.2f)),
                CreateCharacter("vesper", "Vesper", "A rare astral oracle with exceptional passive scaling.", CharacterRarity.FiveStar, 1.35f, 0.78f, 1.38f, 1.18f, 1.38f, new Color(1f, 0.35f, 0.75f))
            };
        }

        public static List<BannerData> CreateBanners(IList<CharacterData> characters)
        {
            BannerData standard = ScriptableObject.CreateInstance<BannerData>();
            standard.name = "Standard Rift";
            standard.ConfigurePrototype("standard", "Standard Rift", "A stable Rift containing the full prototype roster.",
                BannerCurrency.StandardTickets, 1, 10, characters, null, new Color(0.3f, 0.55f, 0.95f));

            var featuredPool = new List<CharacterData> { characters[0], characters[2], characters[3], characters[4] };
            BannerData featured = ScriptableObject.CreateInstance<BannerData>();
            featured.name = "Featured Rift - Solara";
            featured.ConfigurePrototype("featured_solara", "Radiant Convergence", "Solara is featured and receives 50% of 5★ results.",
                BannerCurrency.Lumina, 100, 1000, featuredPool, characters[4], new Color(1f, 0.55f, 0.18f));
            return new List<BannerData> { standard, featured };
        }

        private static CharacterData CreateCharacter(string id, string name, string description, CharacterRarity rarity,
            float click, float passive, float cost, float clickExponent, float passiveExponent, Color accent)
        {
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            character.name = name;
            character.ConfigurePrototype(id, name, description, rarity, click, passive, cost, clickExponent, passiveExponent, accent);
            return character;
        }
    }
}
