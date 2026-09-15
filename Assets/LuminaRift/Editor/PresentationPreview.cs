#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace LuminaRift.Editor
{
    // Renders the production UI with disposable fixtures. Never constructs a LocalSaveSystem.
    public sealed class PresentationPreview : EditorWindow
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly string[] views = { "Home", "Characters", "Summon", "Stats", "Offline", "Reveal", "Results", "Ascension" };
        private GameObject root;
        private LuminaRiftPrototypeUI ui;
        private LuminaRiftGameState game;
        private int view = 2, preparedView = -1;
        private bool funded = true;
        [MenuItem("Lumina Rift/Visual Preview (No Save)")]
        public static void Open()
        {
            var window = GetWindow<PresentationPreview>(true, "Lumina Rift • Visual Preview • No Save");
            window.minSize = new Vector2(960, 580);
            window.position = new Rect(100, 100, 1280, 770);
            window.Show();
        }
        private void OnEnable() { EditorApplication.update += Repaint; }
        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
            if (root != null) DestroyImmediate(root);
        }
        private void Set(string name, object value) { typeof(LuminaRiftPrototypeUI).GetField(name, Private).SetValue(ui, value); }
        private void Draw(string name, params object[] args) { typeof(LuminaRiftPrototypeUI).GetMethod(name, Private).Invoke(ui, args); }
        private void Prepare()
        {
            if (ui != null) return;
            var config = PrototypeContent.LoadOrCreate(); game = new LuminaRiftGameState(config, 42);
            var fixture = game.CreateSave(); fixture.credits = 25000; fixture.standardTickets = 20; fixture.lumina = 1200;
            for (int i = 0; i < fixture.characters.Count; i++)
            { fixture.characters[i].isOwned = i % 2 == 0; fixture.characters[i].level = 50; }
            game.Restore(fixture);
            root = new GameObject("Disposable Visual Preview") { hideFlags = HideFlags.HideAndDontSave };
            ui = root.AddComponent<LuminaRiftPrototypeUI>(); ui.enabled = false;
            Set("config", config); Set("state", game); preparedView = -1;
        }
        private void OnGUI()
        {
            Prepare();
            view = GUILayout.Toolbar(view, views);
            bool wasFunded = funded;
            funded = GUILayout.Toggle(funded, "Preview available currency (sample data only; no save access)");
            if (wasFunded != funded) { var fixture = game.CreateSave(); fixture.lumina = funded ? 1200 : 0; fixture.standardTickets = funded ? 20 : 0; game.Restore(fixture); }
            Draw("EnsureStyles");
            if (preparedView != view)
            {
            preparedView = view;
            Set("showOffline", view == 4); Set("showAscensionConfirm", false);
            Set("revealResults", null); Set("showSummonSummary", view == 6);
            Set("pendingOfflineCredits", 12500d); Set("pendingOfflineSeconds", 7200d);
            Set("screen", Enum.ToObject(typeof(LuminaRiftPrototypeUI).GetNestedType("ScreenView", BindingFlags.NonPublic), view == 7 ? 4 : Math.Min(view, 3)));
            if (view == 5 || view == 6)
            {
                var results = new List<GachaResult>();
                for (int i = 0; i < 10; i++) results.Add(new GachaResult(game.Roster[(i + 4) % game.Roster.Count], i >= 3, i >= 3 ? 20 : 0));
                Set("revealResults", results); Set("revealIndex", 0); Set("revealTimer", 0f);
            }
            }
            float scale = Mathf.Min(position.width / 1280f, (position.height - 48) / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(0, 48, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            try
            {
                Draw("DrawCosmicBackground", 1280f, 720f);
                GUI.enabled = (bool)typeof(LuminaRiftPrototypeUI).GetProperty("InterfaceEnabled", Private).GetValue(ui);
                Draw("DrawHeader", 1280f);
                Draw(view < 4 || view == 7 ? "Draw" + views[view] : "DrawSummon", new Rect(24, 82, 1232, 518));
                Draw("DrawNavigation", 1280f, 720f);
                GUI.enabled = true;
                if (view == 4) Draw("DrawOfflineOverlay", 1280f, 720f);
                if (view == 5 || view == 6) Draw("DrawReveal", 1280f, 720f);
                if ((bool)typeof(LuminaRiftPrototypeUI).GetField("showAscensionConfirm", Private).GetValue(ui)) Draw("DrawAscensionOverlay", 1280f, 720f);
            }
            finally { GUI.matrix = previous; GUI.enabled = true; }
        }
    }
}
#endif
