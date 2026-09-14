using System.Collections.Generic;
using UnityEngine;

namespace LuminaRift
{
    public enum BannerCurrency { StandardTickets, Lumina }

    [CreateAssetMenu(fileName = "Banner", menuName = "Lumina Rift/Banner")]
    public sealed class BannerData : ScriptableObject
    {
        [SerializeField] private string bannerId = "banner_id";
        [SerializeField] private string displayName = "Rift Banner";
        [SerializeField, TextArea] private string description = "Echoes wait beyond the Rift.";
        [SerializeField] private BannerCurrency currency = BannerCurrency.StandardTickets;
        [SerializeField, Min(1)] private int singlePullCost = 1;
        [SerializeField, Min(1)] private int tenPullCost = 10;
        [SerializeField, Min(0f)] private float threeStarRate = 75f;
        [SerializeField, Min(0f)] private float fourStarRate = 20f;
        [SerializeField, Min(0f)] private float fiveStarRate = 5f;
        [SerializeField, Min(1)] private int hardPity = 30;
        [SerializeField] private List<CharacterData> characterPool = new List<CharacterData>();
        [SerializeField] private CharacterData rateUpCharacter;
        [SerializeField, Range(0f, 1f)] private float rateUpShareOfFiveStar = 0.5f;
        [SerializeField] private Color accentColor = new Color(0.45f, 0.3f, 0.85f);

        public string BannerId { get { return bannerId; } }
        public string DisplayName { get { return displayName; } }
        public string Description { get { return description; } }
        public BannerCurrency Currency { get { return currency; } }
        public int SinglePullCost { get { return singlePullCost; } }
        public int TenPullCost { get { return tenPullCost; } }
        public float ThreeStarRate { get { return threeStarRate; } }
        public float FourStarRate { get { return fourStarRate; } }
        public float FiveStarRate { get { return fiveStarRate; } }
        public int HardPity { get { return hardPity; } }
        public IReadOnlyList<CharacterData> CharacterPool { get { return characterPool; } }
        public CharacterData RateUpCharacter { get { return rateUpCharacter; } }
        public float RateUpShareOfFiveStar { get { return rateUpShareOfFiveStar; } }
        public Color AccentColor { get { return accentColor; } }

        public void ConfigurePrototype(string id, string name, string bannerDescription, BannerCurrency bannerCurrency,
            int singleCost, int multiCost, IList<CharacterData> pool, CharacterData rateUp, Color accent)
        {
            bannerId = id;
            displayName = name;
            description = bannerDescription;
            currency = bannerCurrency;
            singlePullCost = singleCost;
            tenPullCost = multiCost;
            characterPool = new List<CharacterData>(pool);
            rateUpCharacter = rateUp;
            accentColor = accent;
        }
    }
}
