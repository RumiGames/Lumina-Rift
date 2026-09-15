#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LuminaRift.Editor
{
    public static class PrototypeSmokeValidation
    {
        [MenuItem("Lumina Rift/Validate Prototype 0.0.5 Loop")]
        public static void Run()
        {
            PrototypeGameConfig config = PrototypeContent.LoadOrCreate(); var game = new LuminaRiftGameState(config, 12345);
            ValidateBannerPools(config);
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
            ValidateCollectionAscension(config);
            ValidateEchoTeams(config);
            Debug.Log("Lumina Rift Prototype 0.0.5 validation passed.");
        }

        private static void ValidateBannerPools(PrototypeGameConfig config)
        {
            Require(config.PrototypeVersion == 5 && config.Characters.Count == 21 && config.Banners.Count == 2, "0.0.5 roster setup failed.");
            Require(config.Characters.All(character => character.Tags.Count > 0 && character.SupportEffectValue > 0), "Echo tags or Support effects were not configured.");
            int three = 0, four = 0, five = 0;
            foreach (CharacterData character in config.Characters)
            { if (character.Rarity == CharacterRarity.ThreeStar) three++; else if (character.Rarity == CharacterRarity.FourStar) four++; else five++; }
            Require(three == 9 && four == 7 && five == 5, "Rarity roster counts failed.");
            BannerData standard = config.Banners[0], featured = config.Banners[1];
            Require(standard.CharacterPool.Count == 20 && featured.CharacterPool.Count == 21, "Banner pool sizes failed.");
            Require(featured.RateUpCharacter != null && featured.RateUpCharacter.CharacterId == "weiss_schnee" && !standard.CharacterPool.Contains(featured.RateUpCharacter), "Weiss banner exclusivity failed.");
            foreach (BannerData banner in config.Banners)
                Require(banner.ThreeStarRate == 82f && banner.FourStarRate == 16f && banner.FiveStarRate == 2f && banner.HardPity == 40, "Banner rates or pity failed.");
            var roster = new LuminaRiftGameState(config, 99);
            var manager = new GachaManager(config, roster.Roster, 99);
            manager.RestorePity(new[] { new PitySaveRecord { bannerId = featured.BannerId, pullsSinceFiveStar = 39 } });
            List<GachaResult> pityPull = manager.Pull(featured, 1);
            Require(pityPull.Count == 1 && pityPull[0].Character.Definition.Rarity == CharacterRarity.FiveStar, "40-pull hard pity failed.");
        }

        private static void ValidateCollectionAscension(PrototypeGameConfig config)
        {
            var game = new LuminaRiftGameState(config, 21);
            var snapshot = game.CreateSave();
            for (int i = 0; i < snapshot.characters.Count; i++)
            { snapshot.characters[i].isOwned = i < 3; snapshot.characters[i].level = i == 0 ? 1 : i == 1 ? config.AscensionMinimumLevel : config.AscensionMinimumLevel + 10; }
            snapshot.activeCharacterId = snapshot.characters[0].characterId;
            snapshot.credits = 500; snapshot.lumina = 17; snapshot.standardTickets = 9;
            snapshot.characters[1].affinityXp = 25;
            game.Restore(snapshot);
            var first = config.GetAscensionReward(config.AscensionMinimumLevel);
            var second = config.GetAscensionReward(config.AscensionMinimumLevel + 10);
            var total = game.ProjectedAscensionReward;
            Require(game.CanAscend && game.ActiveCharacter.Level == 1, "Inactive eligible Echo must unlock Ascension.");
            Require(total.Lumina == first.Lumina + second.Lumina && total.Power == first.Power + second.Power, "Ascension must sum eligible owned Echoes only.");
            game.SetActiveCharacter(game.Roster[1]);
            Require(game.ProjectedAscensionReward.Lumina == total.Lumina && game.ProjectedAscensionReward.Power == total.Power, "Active selection changed collection reward.");
            Require(game.GetAscensionContribution(game.Roster[0]).Power == 0 && game.GetAscensionContribution(game.Roster[3]).Power == 0, "Ineligible or unowned Echo contributed.");
            Require(game.TryAscend(), "Collection Ascension failed.");
            Require(game.Lumina == 17 + total.Lumina && game.AscensionPower == total.Power && game.AscensionCount == 1, "Collection rewards were not paid exactly once.");
            Require(game.Credits == 0 && game.StandardTickets == 9 && game.Roster[1].AffinityXp == 25 && Owned(game) == 3, "Ascension persistence/reset rules failed.");
            foreach (var character in game.Roster) Require(character.Level == 1, "All run levels must reset together.");
            Require(!game.TryAscend(), "Repeated Ascension must not pay again.");
            var restored = new LuminaRiftGameState(config, 22); restored.Restore(game.CreateSave());
            Require(restored.Lumina == game.Lumina && restored.AscensionPower == game.AscensionPower && !restored.CanAscend, "Collection Ascension save restore failed.");
        }

        private static void ValidateEchoTeams(PrototypeGameConfig config)
        {
            var game = new LuminaRiftGameState(config, 31); var snapshot = game.CreateSave();
            for (int i = 0; i < 5; i++) snapshot.characters[i].isOwned = true;
            game.Restore(snapshot);
            double clickBefore = game.ClickIncome, passiveBefore = game.PassiveIncomePerSecond;
            Require(game.ToggleSupportCharacter(game.Roster[1]) && game.ToggleSupportCharacter(game.Roster[2]), "Support assignment failed.");
            Require(game.SupportCharacters.Count == LuminaRiftGameState.MaxSupportCharacters, "Support slot limit failed.");
            Require(!game.ToggleSupportCharacter(game.Roster[3]), "A third Support Echo was accepted.");
            Require(game.ClickIncome > clickBefore && game.PassiveIncomePerSecond > passiveBefore, "Support income or effects failed.");
            double expectedOffline = game.PassiveIncomePerSecond * config.OfflineEarningsRate * (1d + game.Roster[1].Definition.SupportEffectValue);
            Require(Math.Abs(game.OfflineIncomePerSecond - expectedOffline) < .0001 && game.OfflineIncomePerSecond < game.PassiveIncomePerSecond, "Reduced offline income or its Support bonus failed.");

            var restored = new LuminaRiftGameState(config, 32); restored.Restore(game.CreateSave());
            Require(restored.SupportCharacters.Count == 2 && restored.SupportCharacters[0].Definition.CharacterId == game.Roster[1].Definition.CharacterId, "Support order/save restore failed.");
            Require(restored.SetActiveCharacter(restored.SupportCharacters[0]) && restored.SupportCharacters.Count == 1, "Promoting Support to Active did not remove the duplicate slot.");

            PlayerSaveData invalid = game.CreateSave();
            invalid.supportCharacterIds = new List<string> { invalid.activeCharacterId, invalid.supportCharacterIds[0], invalid.supportCharacterIds[0], "deleted_echo" };
            var sanitized = new LuminaRiftGameState(config, 33); sanitized.Restore(invalid);
            Require(sanitized.SupportCharacters.Count == 1 && sanitized.SupportCharacters[0] != sanitized.ActiveCharacter, "Invalid or duplicate Support entries were not sanitized.");
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
