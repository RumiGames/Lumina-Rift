using UnityEngine;

namespace LuminaRift
{
    public static class LuminaRiftBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartPrototype()
        {
            if (Object.FindAnyObjectByType<LuminaRiftPrototypeUI>() != null) return;
            GameObject root = new GameObject("Lumina Rift Prototype");
            Object.DontDestroyOnLoad(root);
            LuminaRiftPrototypeUI ui = root.AddComponent<LuminaRiftPrototypeUI>();
            ui.Initialize(PrototypeContent.LoadOrCreate());
        }
    }
}
