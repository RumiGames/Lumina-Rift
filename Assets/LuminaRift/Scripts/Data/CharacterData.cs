using UnityEngine;

namespace LuminaRift
{
    public enum CharacterRarity { ThreeStar = 3, FourStar = 4, FiveStar = 5 }

    [CreateAssetMenu(fileName = "Character", menuName = "Lumina Rift/Character")]
    public sealed class CharacterData : ScriptableObject
    {
        [SerializeField] private string characterId = "echo";
        [SerializeField] private string displayName = "New Echo";
        [SerializeField, TextArea] private string description = "A traveller drawn through the Rift.";
        [SerializeField] private CharacterRarity rarity = CharacterRarity.ThreeStar;
        [SerializeField, Min(0.01f)] private float baseClickIncome = 1f;
        [SerializeField, Min(0f)] private float basePassiveIncome = 0.25f;
        [SerializeField, Min(0.1f)] private float levelCostMultiplier = 1f;
        [SerializeField, Range(0.5f, 2f)] private float clickLevelExponent = 1f;
        [SerializeField, Range(0.5f, 2f)] private float passiveLevelExponent = 1f;
        [SerializeField] private Color accentColor = new Color(0.35f, 0.8f, 1f);
        [SerializeField] private Sprite portrait = null;

        public string CharacterId { get { return characterId; } }
        public string DisplayName { get { return displayName; } }
        public string Description { get { return description; } }
        public CharacterRarity Rarity { get { return rarity; } }
        public float BaseClickIncome { get { return baseClickIncome; } }
        public float BasePassiveIncome { get { return basePassiveIncome; } }
        public float LevelCostMultiplier { get { return levelCostMultiplier; } }
        public float ClickLevelExponent { get { return clickLevelExponent; } }
        public float PassiveLevelExponent { get { return passiveLevelExponent; } }
        public Color AccentColor { get { return accentColor; } }
        public Sprite Portrait { get { return portrait; } }

        public void Configure(string id, string name, string bio, CharacterRarity starRarity, float click,
            float passive, float cost, float clickExponent, float passiveExponent, Color color)
        {
            characterId = id; displayName = name; description = bio; rarity = starRarity;
            baseClickIncome = click; basePassiveIncome = passive; levelCostMultiplier = cost;
            clickLevelExponent = clickExponent; passiveLevelExponent = passiveExponent; accentColor = color;
        }
    }
}
