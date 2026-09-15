#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using LuminaRift;
public static class PresentationValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Set(object target, string field, object value) { target.GetType().GetField(field, Private).SetValue(target, value); }
    static object Get(object target, string field) { return target.GetType().GetField(field, Private).GetValue(target); }
    static object Call(object target, string method, params object[] args) { return target.GetType().GetMethod(method, Private).Invoke(target, args); }
    static bool Enabled(object target) { return (bool)target.GetType().GetProperty("InterfaceEnabled", Private).GetValue(target); }
    [MenuItem("Lumina Rift/Validate Presentation Flow")]
    public static void Run()
    {
        GameObject root = null;
        try
        {
            LuminaRift.Editor.PrototypeSmokeValidation.Run();
            var config = PrototypeContent.LoadOrCreate();
            var state = new LuminaRiftGameState(config, 12345);
            var snapshot = state.CreateSave(); snapshot.standardTickets = 20; state.Restore(snapshot);
            root = new GameObject("Presentation validation") { hideFlags = HideFlags.HideAndDontSave };
            var ui = root.AddComponent<LuminaRiftPrototypeUI>();
            Set(ui, "config", config); Set(ui, "state", state);
            Require(Enabled(ui), "Fresh interface should accept input");
            foreach (string flag in new[] { "showOffline", "showAscensionConfirm", "showResetConfirm" })
            { Set(ui, flag, true); Require(!Enabled(ui), flag + " did not block background input"); Set(ui, flag, false); }
            Call(ui, "BeginSummon", config.Banners[0], 10);
            var results = (List<GachaResult>)Get(ui, "revealResults");
            Require(results.Count == 10 && state.StandardTickets == 10, "Ten pull should charge once and retain all results");
            Require(!Enabled(ui), "Reveal did not block background input");
            Set(ui, "revealTimer", 0f); Call(ui, "Update");
            Require((int)Get(ui, "revealIndex") == 0 && Get(ui, "revealResults") != null, "Reveal should wait for player input");
            for (int i = 0; i < 10; i++) Call(ui, "AdvanceReveal");
            Require((bool)Get(ui, "showSummonSummary") && ReferenceEquals(results, Get(ui, "revealResults")), "Final reveal must retain results for review");
            Require(state.StandardTickets == 10, "Advancing reveals charged currency again");
            Set(ui, "revealResults", null); Set(ui, "showSummonSummary", false);
            Call(ui, "BeginSummon", config.Banners[0], 1); Call(ui, "AdvanceReveal");
            Require((bool)Get(ui, "showSummonSummary") && ((List<GachaResult>)Get(ui, "revealResults")).Count == 1, "Single pull summary failed");
            var saved = state.CreateSave(1234.5, 7200);
            Require(saved.pendingOfflineCredits == 1234.5 && saved.pendingOfflineSeconds == 7200, "Pending offline reward persistence failed");
            double before = state.Credits; state.CollectOfflineCredits(1234.5);
            Require(Math.Abs(state.Credits - before - 1234.5) < .001, "Offline credit collection failed");

            Debug.Log("PRESENTATION VALIDATION PASSED: modal input, single/ten pulls, manual reveal progression, retained summaries, currency charging, offline rewards, and existing progression smoke tests.");

        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
    }
}

#endif
