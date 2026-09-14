#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LuminaRift.Editor
{
    public static class PrototypeSmokeValidation
    {
        [MenuItem("Lumina Rift/Validate Prototype 0.0.3 Loop")]
        public static void Run()
        {
            PrototypeGameConfig config = PrototypeContent.LoadOrCreate(); var game = new LuminaRiftGameState(config, 12345);
            Require(game.ActiveCharacter != null && game.ActiveCharacter.IsOwned, "Fresh-player ownership failed.");
            LevelTo(game, 50); Require(game.StandardTickets >= 10, "Milestone tickets failed.");
            List<GachaResult> pulls; Require(game.TrySummon(config.Banners[0], 10, out pulls) && pulls.Count == 10, "10-pull failed.");
            Require(pulls.Exists(result => result.Character.Definition.Rarity >= CharacterRarity.FourStar), "10-pull guarantee failed.");
            AscensionReward reward = game.ProjectedAscensionReward; int owned = Owned(game); int affinity = Affinity(game);
            Require(game.TryAscend(), "Ascension failed."); Require(game.Lumina == reward.Lumina && game.AscensionPower == reward.Power, "Ascension reward failed.");
            Require(Owned(game) >= owned && Affinity(game) == affinity, "Permanent collection progress reset.");
            PlayerSaveData snapshot = game.CreateSave(); var restored = new LuminaRiftGameState(config, 7); restored.Restore(snapshot);
            Require(restored.Lumina == game.Lumina && restored.AscensionCount == game.AscensionCount && Owned(restored) == Owned(game), "Save snapshot restore failed.");
            snapshot.ascensionCount = 5; snapshot.autoLevelEnabled = true; restored.Restore(snapshot);
            Require(restored.LevelTenUnlocked && restored.LevelMaxUnlocked && restored.AutoLevelUnlocked && restored.AutoLevelEnabled, "Automation unlock restore failed.");
            Debug.Log("Lumina Rift Prototype 0.0.3 validation passed.");
        }

        private static void LevelTo(LuminaRiftGameState game, int level)
        {
            int safety = 2000000; while (game.ActiveCharacter.Level < level && safety-- > 0) { if (game.CanLevel) game.BuyLevels(1); else game.EarnClick(); }
            Require(game.ActiveCharacter.Level >= level, "Levelling safety limit reached.");
        }
        private static int Owned(LuminaRiftGameState game) { int value=0; foreach (CharacterRuntimeState c in game.Roster) if(c.IsOwned)value++; return value; }
        private static int Affinity(LuminaRiftGameState game) { int value=0; foreach (CharacterRuntimeState c in game.Roster)value+=c.AffinityXp; return value; }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
