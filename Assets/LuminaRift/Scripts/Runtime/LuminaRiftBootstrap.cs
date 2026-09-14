using UnityEngine;

namespace LuminaRift
{
    public static class LuminaRiftBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartPrototype()
        {
            if (Object.FindAnyObjectByType<LuminaRiftPrototypeUI>() != null) return;
            GameObject root = new GameObject("Lumina Rift Prototype 0.0.3"); Object.DontDestroyOnLoad(root);
            root.AddComponent<LuminaRiftPrototypeUI>().Initialize(PrototypeContent.LoadOrCreate());
        }
    }
}
