using UnityEngine;
using System.Collections.Generic;

namespace LuminaRift
{
    public enum CharacterRarity { ThreeStar = 3, FourStar = 4, FiveStar = 5 }
    public enum EchoTag { Precision, Swift, Guardian, Mystic, Royal, Scholar, Water, Frost }
    public enum SupportEffectType { PassiveIncome, ClickIncome, OfflineIncome, TagIncome }

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
        [Header("Echo Team Identity")]
        [SerializeField] private List<EchoTag> tags = new List<EchoTag>();
        [SerializeField] private SupportEffectType supportEffect = SupportEffectType.PassiveIncome;
        [SerializeField, Min(0f), Tooltip("Team bonus as a decimal. 0.10 means +10%.")]
        private float supportEffectValue = 0.05f;
        [SerializeField, Tooltip("Used only by Tag Income effects.")]
        private EchoTag supportEffectTag = EchoTag.Precision;
        [Header("Koikatsu Presentation")]
        [SerializeField, Tooltip("Transparent bust or close crop used on collection cards.")]
        private Sprite portrait = null;
        [SerializeField, Tooltip("Transparent full-body render used on the Home screen.")]
        private Sprite homeArtwork = null;
        [SerializeField, Tooltip("Transparent promotional pose used on banners and summon reveals.")]
        private Sprite summonArtwork = null;

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
        public IReadOnlyList<EchoTag> Tags { get { return tags; } }
        public SupportEffectType SupportEffect { get { return supportEffect; } }
        public float SupportEffectValue { get { return supportEffectValue; } }
        public EchoTag SupportEffectTag { get { return supportEffectTag; } }
        public Sprite Portrait { get { return portrait; } }
        public Sprite HomeArtwork { get { return homeArtwork != null ? homeArtwork : portrait; } }
        public Sprite SummonArtwork { get { return summonArtwork != null ? summonArtwork : homeArtwork != null ? homeArtwork : portrait; } }

        public void Configure(string id, string name, string bio, CharacterRarity starRarity, float click,
            float passive, float cost, float clickExponent, float passiveExponent, Color color)
        {
            characterId = id; displayName = name; description = bio; rarity = starRarity;
            baseClickIncome = click; basePassiveIncome = passive; levelCostMultiplier = cost;
            clickLevelExponent = clickExponent; passiveLevelExponent = passiveExponent; accentColor = color;
        }

        public void ConfigureTeamIdentity(SupportEffectType effect, float effectValue, EchoTag effectTag, params EchoTag[] echoTags)
        {
            supportEffect = effect; supportEffectValue = Mathf.Max(0, effectValue); supportEffectTag = effectTag;
            tags = echoTags == null ? new List<EchoTag>() : new List<EchoTag>(echoTags);
        }
    }
}
