using System.Collections.Generic;
using UnityEngine;

namespace LuminaRift
{
    public static class PrototypeContent
    {
        public static PrototypeGameConfig LoadOrCreate()
        {
            PrototypeGameConfig saved = Resources.Load<PrototypeGameConfig>("PrototypeGameConfig");
            if (saved != null && saved.PrototypeVersion >= 4) return saved;
            List<CharacterData> characters = CreateCharacters();
            PrototypeGameConfig config = ScriptableObject.CreateInstance<PrototypeGameConfig>();
            config.Configure(characters, CreateBanners(characters)); return config;
        }

        public static List<CharacterData> CreateCharacters()
        {
            return new List<CharacterData>
            {
                Character("nanao_ise", "Nanao Ise", "A precise support Echo with efficient early-run growth.", CharacterRarity.ThreeStar, 1.00f, .34f, .80f, 1.02f, .96f, new Color(.38f,.72f,1f)),
                Character("cyan_sung_sun", "Cyan Sung-Sun", "A swift Echo who balances active and passive income.", CharacterRarity.ThreeStar, .96f, .38f, .82f, 1.00f, 1.00f, new Color(.25f,.82f,.88f)),
                Character("ikumi_unagiya", "Ikumi Unagiya", "A dependable Echo with strong click income.", CharacterRarity.ThreeStar, 1.08f, .30f, .80f, 1.04f, .94f, new Color(.92f,.48f,.32f)),
                Character("doll", "Doll", "A composed Echo tuned for steady passive gains.", CharacterRarity.ThreeStar, .92f, .42f, .84f, .98f, 1.03f, new Color(.55f,.76f,.92f)),
                Character("camie", "Camie", "A lively Echo with accessible all-round growth.", CharacterRarity.ThreeStar, 1.02f, .35f, .79f, 1.01f, .99f, new Color(.35f,.86f,.78f)),
                Character("kalifa", "Kalifa", "A technical Echo who rewards active play.", CharacterRarity.ThreeStar, 1.10f, .29f, .83f, 1.05f, .93f, new Color(.92f,.70f,.32f)),
                Character("juvia_lockser", "Juvia Lockser", "A flowing Echo with dependable passive scaling.", CharacterRarity.ThreeStar, .94f, .43f, .85f, .97f, 1.04f, new Color(.28f,.62f,1f)),
                Character("mirajane_strauss", "Mirajane Strauss", "A versatile Echo with balanced income growth.", CharacterRarity.ThreeStar, 1.03f, .36f, .82f, 1.01f, 1.00f, new Color(.70f,.58f,.92f)),
                Character("irene_belserion", "Irene Belserion", "A potent Echo whose active income grows quickly.", CharacterRarity.ThreeStar, 1.12f, .30f, .86f, 1.06f, .94f, new Color(.78f,.34f,.62f)),
                Character("orihime_inoue", "Orihime Inoue", "A radiant support Echo with excellent passive growth.", CharacterRarity.FourStar, 1.22f, .52f, 1.00f, 1.08f, 1.13f, new Color(1f,.58f,.28f)),
                Character("soi_fon", "Soi Fon", "A rapid-strike Echo built around click income.", CharacterRarity.FourStar, 1.48f, .38f, 1.05f, 1.16f, 1.04f, new Color(.78f,.72f,.24f)),
                Character("nefertari_vivi", "Nefertari Vivi", "A balanced support Echo with reliable scaling.", CharacterRarity.FourStar, 1.28f, .48f, 1.01f, 1.10f, 1.10f, new Color(.38f,.72f,1f)),
                Character("ginny", "Ginny", "An energetic Echo with strong active growth.", CharacterRarity.FourStar, 1.42f, .40f, 1.04f, 1.14f, 1.06f, new Color(.95f,.42f,.48f)),
                Character("ruri", "Ruri", "A focused Echo who steadily amplifies passive income.", CharacterRarity.FourStar, 1.20f, .56f, 1.05f, 1.06f, 1.16f, new Color(.58f,.42f,.92f)),
                Character("yuzuriha", "Yuzuriha", "A nimble Echo with smooth all-round progression.", CharacterRarity.FourStar, 1.36f, .45f, 1.03f, 1.12f, 1.09f, new Color(.88f,.48f,.72f)),
                Character("levy_mcgarden", "Levy McGarden", "A clever support Echo with efficient level costs.", CharacterRarity.FourStar, 1.25f, .50f, .96f, 1.09f, 1.12f, new Color(.36f,.68f,.94f)),
                Character("yoruichi_shihoin", "Yoruichi Shihoin", "An elite Echo with exceptional active scaling.", CharacterRarity.FiveStar, 1.72f, .62f, 1.30f, 1.30f, 1.22f, new Color(.72f,.38f,1f)),
                Character("nami", "Nami", "An elite strategist Echo with powerful passive income.", CharacterRarity.FiveStar, 1.48f, .82f, 1.32f, 1.20f, 1.38f, new Color(1f,.55f,.22f)),
                Character("kohaku", "Kohaku", "An elite Echo with fast, balanced progression.", CharacterRarity.FiveStar, 1.62f, .70f, 1.28f, 1.26f, 1.30f, new Color(.35f,.82f,.68f)),
                Character("lucy_heartfilia", "Lucy Heartfilia", "An elite summoner Echo with broad income scaling.", CharacterRarity.FiveStar, 1.58f, .76f, 1.33f, 1.24f, 1.34f, new Color(1f,.76f,.30f)),
                Character("weiss_schnee", "Weiss Schnee", "The exclusive featured Echo, combining precision with exceptional scaling.", CharacterRarity.FiveStar, 1.68f, .80f, 1.35f, 1.27f, 1.36f, new Color(.55f,.82f,1f))
            };
        }

        public static List<BannerData> CreateBanners(IList<CharacterData> characters)
        {
            var standardPool = new List<CharacterData>(characters); standardPool.RemoveAt(standardPool.Count - 1);
            BannerData standard = ScriptableObject.CreateInstance<BannerData>();
            standard.Configure("standard", "Standard Rift", "The permanent 20-Echo collection.", BannerCurrency.StandardTickets, 1, 10, standardPool, null, new Color(.3f,.55f,.95f));
            BannerData featured = ScriptableObject.CreateInstance<BannerData>();
            CharacterData weiss = characters[characters.Count - 1];
            featured.Configure("featured_weiss", "Frozen Resonance", "Otherwise, a standard 5★ Echo appears.", BannerCurrency.Lumina, 100, 1000, characters, weiss, new Color(.55f,.82f,1f));
            return new List<BannerData> { standard, featured };
        }

        private static CharacterData Character(string id, string name, string bio, CharacterRarity rarity, float click, float passive, float cost, float clickExp, float passiveExp, Color color)
        { CharacterData value = ScriptableObject.CreateInstance<CharacterData>(); value.Configure(id, name, bio, rarity, click, passive, cost, clickExp, passiveExp, color); value.name = name; return value; }
    }
}
