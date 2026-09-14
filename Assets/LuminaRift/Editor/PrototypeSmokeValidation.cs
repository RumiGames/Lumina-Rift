#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LuminaRift.Editor
{
    public static class PrototypeSmokeValidation
    {
        [MenuItem("Lumina Rift/Validate Prototype 0.0.2 Loop")]
        public static void Run()
        {
            PrototypeGameConfig config = PrototypeContent.LoadOrCreate();
            var game = new LuminaRiftGameState(config, 12345);
            Require(game.ActiveCharacter != null && game.ActiveCharacter.IsOwned, "Starting Echo was not owned.");

            LevelTo(game, config.AscensionMinimumLevel);
            Require(game.StandardTickets >= 10, "Level milestones did not award enough tickets for a 10-pull.");
            int rosterBefore = CountOwned(game.Roster);
            List<GachaResult> standardResults;
            Require(game.TrySummon(config.Banners[0], 10, out standardResults), "Standard 10-pull failed.");
            Require(standardResults.Count == 10, "Standard 10-pull returned the wrong result count.");
            bool hasFourPlus = standardResults.Exists(item => item.Character.Definition.Rarity >= CharacterRarity.FourStar);
            Require(hasFourPlus, "10-pull guarantee did not produce a 4-star or higher result.");

            int affinityBeforeAscension = TotalAffinity(game.Roster);
            AscensionReward projected = game.ProjectedAscensionReward;
            Require(projected.Lumina >= 100 && projected.Power >= 1, "Projected Ascension reward was invalid.");
            Require(game.TryAscend(), "Ascension failed at the minimum level.");
            Require(game.Lumina == projected.Lumina && game.AscensionPower == projected.Power, "Ascension rewards were not applied.");
            Require(TotalAffinity(game.Roster) == affinityBeforeAscension, "Affinity reset during Ascension.");
            Require(CountOwned(game.Roster) >= rosterBefore, "Ownership reset during Ascension.");
            foreach (CharacterRuntimeState character in game.Roster) Require(character.Level == 1, "A run level survived Ascension.");

            List<GachaResult> featuredResults;
            Require(game.TrySummon(config.Banners[1], 1, out featuredResults), "Featured Lumina summon failed after Ascension.");
            Require(featuredResults.Count == 1, "Featured summon returned the wrong result count.");
            Debug.Log("Lumina Rift Prototype 0.0.2 smoke validation passed.");
        }

        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void LevelTo(LuminaRiftGameState game, int target)
        {
            int safety = 2000000;
            while (game.ActiveCharacter.Level < target && safety-- > 0)
            {
                if (game.CanLevel) game.TryLevelActiveCharacter();
                else game.EarnClick();
            }
            Require(game.ActiveCharacter.Level >= target, "Could not reach the target level within the smoke-test safety limit.");
        }

        private static int CountOwned(IReadOnlyList<CharacterRuntimeState> roster)
        {
            int count = 0;
            foreach (CharacterRuntimeState character in roster) if (character.IsOwned) count++;
            return count;
        }

        private static int TotalAffinity(IReadOnlyList<CharacterRuntimeState> roster)
        {
            int total = 0;
            foreach (CharacterRuntimeState character in roster) total += character.AffinityXp;
            return total;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
