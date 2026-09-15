#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LuminaRift.Editor
{
    [InitializeOnLoad]
    public static class PrototypeAssetSetup
    {
        private const string Root = "Assets/LuminaRift/Resources";
        private const string ConfigPath = Root + "/PrototypeGameConfig.asset";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        static PrototypeAssetSetup() { EditorApplication.delayCall += EnsureAssets; }

        [MenuItem("Lumina Rift/Rebuild Prototype 0.0.5 Data")]
        public static void Rebuild() { DeleteGeneratedData(); BuildData(); }

        public static void EnsureAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            PrototypeGameConfig config = AssetDatabase.LoadAssetAtPath<PrototypeGameConfig>(ConfigPath);
            if (config == null) { BuildData(); }
            else if (config.PrototypeVersion < 5 || config.Characters.Count > 0 && config.Characters[0].Tags.Count == 0) UpgradeTeamData(config);
            EnsureScene();
        }

        private static void UpgradeTeamData(PrototypeGameConfig config)
        {
            foreach (CharacterData character in config.Characters)
            {
                if (character == null) continue;
                PrototypeContent.ConfigureTeamIdentity(character, character.CharacterId, character.Rarity);
                EditorUtility.SetDirty(character);
            }
            // Configure advances the data version while retaining the existing character assets and artwork references.
            config.Configure(new List<CharacterData>(config.Characters), new List<BannerData>(config.Banners));
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
        }

        private static void BuildData()
        {
            Folder("Assets", "LuminaRift"); Folder("Assets/LuminaRift", "Resources"); Folder(Root, "Characters"); Folder(Root, "Banners");
            List<CharacterData> characters = PrototypeContent.CreateCharacters();
            foreach (CharacterData character in characters) AssetDatabase.CreateAsset(character, Root + "/Characters/" + character.name + ".asset");
            List<BannerData> banners = PrototypeContent.CreateBanners(characters);
            for (int i = 0; i < banners.Count; i++) AssetDatabase.CreateAsset(banners[i], Root + "/Banners/Banner" + i + ".asset");
            PrototypeGameConfig config = ScriptableObject.CreateInstance<PrototypeGameConfig>(); config.Configure(characters, banners);
            AssetDatabase.CreateAsset(config, ConfigPath); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }

        private static void DeleteGeneratedData()
        {
            AssetDatabase.DeleteAsset(ConfigPath);
            AssetDatabase.DeleteAsset(Root + "/Characters");
            AssetDatabase.DeleteAsset(Root + "/Banners");
        }

        private static void EnsureScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Folder("Assets", "Scenes"); Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Main Camera", typeof(Camera)); EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void Folder(string parent, string child) { string path = parent + "/" + child; if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child); }
    }
}
#endif
