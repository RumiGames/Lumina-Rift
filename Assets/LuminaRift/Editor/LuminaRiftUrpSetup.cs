#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LuminaRift.Editor
{
    [InitializeOnLoad]
    public static class LuminaRiftUrpSetup
    {
        private const string Folder = "Assets/LuminaRift/Rendering";
        private const string RendererPath = Folder + "/LuminaRiftRenderer.asset";
        private const string PipelinePath = Folder + "/LuminaRiftUniversalRenderPipeline.asset";

        static LuminaRiftUrpSetup()
        {
            EditorApplication.delayCall += EnsureConfigured;
        }

        [MenuItem("Lumina Rift/Configure URP Foundation")]
        public static void EnsureConfigured()
        {
            if (Application.isPlaying) return;
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/LuminaRift", "Rendering");
                UniversalRendererData renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "Lumina Rift URP";
                pipeline.renderScale = 1f;
                pipeline.msaaSampleCount = 4;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                AssetDatabase.SaveAssets();
            }

            if (GraphicsSettings.defaultRenderPipeline != pipeline)
                GraphicsSettings.defaultRenderPipeline = pipeline;
            if (QualitySettings.renderPipeline != pipeline)
                QualitySettings.renderPipeline = pipeline;
        }
    }
}
#endif
