#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LuminaRift.Editor
{
    [InitializeOnLoad]
    public static class PrototypeAssetSetup
    {
        private const string Root = "Assets/LuminaRift/Resources";
        private const string ConfigPath = Root + "/PrototypeGameConfig.asset";

        static PrototypeAssetSetup() { EditorApplication.delayCall += EnsurePrototypeAssets; }

        [MenuItem("Lumina Rift/Rebuild Prototype 0.0.2 Data")]
        public static void RebuildPrototypeAssets()
        {
            AssetDatabase.DeleteAsset(Root);
            BuildAssets();
        }

        public static void EnsurePrototypeAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            PrototypeGameConfig existing = AssetDatabase.LoadAssetAtPath<PrototypeGameConfig>(ConfigPath);
            if (existing != null && existing.PrototypeVersion >= 2) return;
            if (existing != null) AssetDatabase.DeleteAsset(Root);
            BuildAssets();
        }

        private static void BuildAssets()
        {
            EnsureFolder("Assets", "LuminaRift");
            EnsureFolder("Assets/LuminaRift", "Resources");
            EnsureFolder(Root, "Characters");
            EnsureFolder(Root, "Banners");

            List<CharacterData> runtimeCharacters = PrototypeContent.CreateCharacters();
            var characters = new List<CharacterData>();
            foreach (CharacterData character in runtimeCharacters)
            {
                string path = Root + "/Characters/" + character.name + ".asset";
                AssetDatabase.CreateAsset(character, path);
                characters.Add(character);
            }

            List<BannerData> runtimeBanners = PrototypeContent.CreateBanners(characters);
            var banners = new List<BannerData>();
            foreach (BannerData banner in runtimeBanners)
            {
                string safeName = banner.name.Replace(" - ", "_").Replace(" ", "_");
                AssetDatabase.CreateAsset(banner, Root + "/Banners/" + safeName + ".asset");
                banners.Add(banner);
            }

            PrototypeGameConfig config = ScriptableObject.CreateInstance<PrototypeGameConfig>();
            config.ConfigurePrototype(characters, banners);
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Lumina Rift Prototype 0.0.2 data generated.");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
