using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LuminaRift
{
    public sealed class LuminaRiftPrototypeUI : MonoBehaviour
    {
        private enum ScreenView { Main, Characters, Summon, Stats }
        private PrototypeGameConfig config;
        private LuminaRiftGameState state;
        private LocalSaveSystem saves;
        private ScreenView screen;
        private string message = "The Rift is open.";
        private float autosaveTimer;
        private double pendingOfflineCredits;
        private double pendingOfflineSeconds;
        private bool showOffline;
        private bool showAscensionConfirm;
        private bool showResetConfirm;
        private List<GachaResult> revealResults;
        private int revealIndex;
        private float revealTimer;
        private GUIStyle titleStyle, headingStyle, bodyStyle, centeredStyle, cardStyle, bigButtonStyle, smallStyle;
        private bool stylesReady;

        public void Initialize(PrototypeGameConfig gameConfig)
        {
            config = gameConfig; state = new LuminaRiftGameState(config); saves = new LocalSaveSystem();
            state.MessageRaised += value => message = value;
            PlayerSaveData loaded = saves.Load();
            if (loaded != null)
            {
                state.Restore(loaded);
                pendingOfflineCredits = Math.Max(0, loaded.pendingOfflineCredits);
                pendingOfflineSeconds = Math.Max(0, loaded.pendingOfflineSeconds);
                if (loaded.lastSaveUtcTicks > 0 && loaded.lastSaveUtcTicks <= DateTime.UtcNow.Ticks)
                {
                    double elapsed = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - loaded.lastSaveUtcTicks).TotalSeconds;
                    double remainingCap = Math.Max(0, config.OfflineEarningsCapHours * 3600d - pendingOfflineSeconds);
                    double capped = Math.Min(elapsed, remainingCap);
                    pendingOfflineSeconds += capped;
                    pendingOfflineCredits += state.PassiveIncomePerSecond * capped;
                }
                showOffline = pendingOfflineCredits > 0.01;
                message = "Save loaded. Welcome back to the Rift.";
            }
            SaveNow();
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime; state.Tick(delta); autosaveTimer += delta;
            if (autosaveTimer >= 10f) { SaveNow(); autosaveTimer = 0; }
            if (revealResults != null)
            {
                revealTimer -= delta;
                if (revealTimer <= 0)
                {
                    revealIndex++;
                    if (revealIndex >= revealResults.Count) revealResults = null;
                    else revealTimer = RevealDuration(revealResults[revealIndex]);
                }
            }
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        private void OnApplicationQuit() { SaveNow(); }
        private void OnDestroy() { if (saves != null && state != null) SaveNow(); }

        private void SaveNow()
        {
            if (saves != null && state != null) saves.Save(state.CreateSave(pendingOfflineCredits, pendingOfflineSeconds));
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale; float height = Screen.height / scale;
            DrawBackground(new Rect(0, 0, width, height), new Color(.025f, .032f, .07f));
            DrawHeader(width); DrawNavigation(width, height);
            Rect content = new Rect(24, 92, width - 48, height - 190);
            if (screen == ScreenView.Main) DrawMain(content);
            else if (screen == ScreenView.Characters) DrawCharacters(content);
            else if (screen == ScreenView.Summon) DrawSummon(content);
            else DrawStats(content);
            GUI.Label(new Rect(30, height - 46, width - 60, 22), message, smallStyle);
            if (showOffline) DrawOfflineOverlay(width, height);
            else if (showAscensionConfirm) DrawAscensionOverlay(width, height);
            else if (showResetConfirm) DrawResetOverlay(width, height);
            else if (revealResults != null) DrawReveal(width, height);
            GUI.matrix = previous;
        }

        private void DrawHeader(float width)
        {
            DrawBackground(new Rect(18, 12, width - 36, 64), new Color(.075f, .09f, .16f));
            GUI.Label(new Rect(38, 20, 420, 46), "LUMINA RIFT  <size=15>PROTOTYPE 0.0.3</size>", titleStyle);
            GUI.Label(new Rect(450, 20, 300, 42), "CREDITS  " + Number(state.Credits), headingStyle);
            GUI.Label(new Rect(width - 485, 20, 445, 42), "TICKETS " + state.StandardTickets + "   LUMINA " + state.Lumina + "   RANK " + state.RiftRank, headingStyle);
        }

        private void DrawNavigation(float width, float height)
        {
            float y = height - 91; float buttonWidth = (width - 70) / 4f;
            if (GUI.Button(new Rect(22, y, buttonWidth, 38), "MAIN", bigButtonStyle)) screen = ScreenView.Main;
            if (GUI.Button(new Rect(30 + buttonWidth, y, buttonWidth, 38), "CHARACTERS", bigButtonStyle)) screen = ScreenView.Characters;
            if (GUI.Button(new Rect(38 + buttonWidth * 2, y, buttonWidth, 38), "SUMMON", bigButtonStyle)) screen = ScreenView.Summon;
            if (GUI.Button(new Rect(46 + buttonWidth * 3, y, buttonWidth, 38), "RIFT RECORD", bigButtonStyle)) screen = ScreenView.Stats;
        }

        private void DrawMain(Rect area)
        {
            CharacterRuntimeState active = state.ActiveCharacter; CharacterData data = active.Definition;
            Rect left = new Rect(area.x, area.y, area.width * .61f, area.height); Rect right = new Rect(left.xMax + 14, area.y, area.width - left.width - 14, area.height);
            GUI.Box(left, GUIContent.none, cardStyle); GUI.Box(right, GUIContent.none, cardStyle);
            GUI.backgroundColor = data.AccentColor;
            if (GUI.Button(new Rect(left.x + 28, left.y + 66, 280, 285), "<size=34>TAP ECHO</size>\n\n+" + Number(state.ClickIncome) + " CREDITS", bigButtonStyle)) state.EarnClick();
            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(left.x + 340, left.y + 34, left.width - 370, 50), Stars(data.Rarity) + "  " + data.DisplayName, headingStyle);
            int affinityRank = state.GetAffinityRank(active);
            GUI.Label(new Rect(left.x + 340, left.y + 92, left.width - 370, 210),
                "LEVEL " + active.Level + " / " + config.RunLevelCap + "\nAffinity " + Roman(affinityRank) + "  •  " + active.AffinityXp + " XP\n\n+" + Number(state.PassiveIncomePerSecond) + " Credits / sec\nIncome Power ×" + state.AscensionMultiplier.ToString("0.00") + "\n\n" + data.Description, bodyStyle);
            float by = left.yMax - 92; float bw = (left.width - 70) / 4;
            if (GUI.Button(new Rect(left.x + 20, by, bw, 60), "LEVEL x1\n" + Number(state.NextLevelCost), bigButtonStyle)) state.BuyLevels(1);
            GUI.enabled = state.LevelTenUnlocked;
            if (GUI.Button(new Rect(left.x + 30 + bw, by, bw, 60), state.LevelTenUnlocked ? "LEVEL x10" : "x10\nASCENSION 2", bigButtonStyle)) state.BuyLevels(10);
            if (GUI.Button(new Rect(left.x + 40 + bw * 2, by, bw, 60), state.LevelTenUnlocked ? "LEVEL x25" : "x25\nASCENSION 2", bigButtonStyle)) state.BuyLevels(25);
            GUI.enabled = state.LevelMaxUnlocked;
            if (GUI.Button(new Rect(left.x + 50 + bw * 3, by, bw, 60), state.LevelMaxUnlocked ? "LEVEL MAX" : "MAX\nASCENSION 3", bigButtonStyle)) state.BuyLevels(int.MaxValue);
            GUI.enabled = true;

            GUI.Label(new Rect(right.x + 26, right.y + 22, right.width - 52, 42), "RIFT RANK " + state.RiftRank, titleStyle);
            GUI.Label(new Rect(right.x + 26, right.y + 72, right.width - 52, 205),
                "Ascensions: " + state.AscensionCount + "\nPermanent Income: ×" + state.AscensionMultiplier.ToString("0.00") + "\n\n" + UnlockLine(state.LevelTenUnlocked, "Level x10 / x25") + "\n" + UnlockLine(state.LevelMaxUnlocked, "Level MAX") + "\n" + UnlockLine(state.AutoLevelUnlocked, "Auto-Level") + "\n" + UnlockLine(state.StartingLevelUnlocked, "Runs start at Lv." + config.PermanentStartingLevel), bodyStyle);
            GUI.enabled = state.AutoLevelUnlocked;
            if (GUI.Button(new Rect(right.x + 28, right.y + 280, right.width - 56, 54), state.AutoLevelEnabled ? "AUTO-LEVEL: ON" : state.AutoLevelUnlocked ? "AUTO-LEVEL: OFF" : "AUTO-LEVEL • ASCENSION 5", bigButtonStyle)) state.SetAutoLevel(!state.AutoLevelEnabled);
            GUI.enabled = state.CanAscend;
            AscensionReward reward = state.ProjectedAscensionReward;
            if (GUI.Button(new Rect(right.x + 28, right.y + 350, right.width - 56, 68), state.CanAscend ? "ASCEND\n+" + reward.Lumina + " LUMINA  •  +" + reward.Power + " POWER" : "ASCEND AT LEVEL " + config.AscensionMinimumLevel, bigButtonStyle)) showAscensionConfirm = true;
            GUI.enabled = true;
            if (GUI.Button(new Rect(right.x + 28, right.yMax - 58, right.width - 56, 34), "DEVELOPER: RESET SAVE", bigButtonStyle)) showResetConfirm = true;
        }

        private void DrawCharacters(Rect area)
        {
            GUI.Label(new Rect(area.x + 8, area.y, 500, 38), "CHARACTER COLLECTION", titleStyle);
            for (int i = 0; i < state.Roster.Count; i++)
            {
                CharacterRuntimeState character = state.Roster[i]; int col = i % 3; int row = i / 3;
                Rect card = new Rect(area.x + col * (area.width / 3), area.y + 55 + row * 220, area.width / 3 - 12, 202);
                GUI.backgroundColor = character == state.ActiveCharacter ? character.Definition.AccentColor : Color.white;
                GUI.Box(card, GUIContent.none, cardStyle); GUI.backgroundColor = Color.white;
                string name = character.IsOwned ? character.Definition.DisplayName : "LOCKED ECHO";
                string details = character.IsOwned ? Stars(character.Definition.Rarity) + "\nRun Level " + character.Level + "\nAffinity " + Roman(state.GetAffinityRank(character)) + "  •  " + character.AffinityXp + " XP\n" + RarityRole(character.Definition.Rarity) : "? ? ?\nSummon to unlock";
                GUI.Label(new Rect(card.x + 18, card.y + 15, card.width - 36, 34), name, headingStyle);
                GUI.Label(new Rect(card.x + 18, card.y + 52, card.width - 36, 100), details, bodyStyle);
                GUI.enabled = character.IsOwned && character != state.ActiveCharacter;
                if (GUI.Button(new Rect(card.x + 18, card.yMax - 44, card.width - 36, 30), character == state.ActiveCharacter ? "ACTIVE ECHO" : "SET ACTIVE", bigButtonStyle)) state.SetActiveCharacter(character);
                GUI.enabled = true;
            }
        }

        private void DrawSummon(Rect area)
        {
            for (int i = 0; i < state.Banners.Count; i++)
            {
                BannerData banner = state.Banners[i]; Rect card = new Rect(area.x + i * (area.width / 2), area.y, area.width / 2 - 10, area.height);
                GUI.backgroundColor = banner.AccentColor; GUI.Box(card, GUIContent.none, cardStyle); GUI.backgroundColor = Color.white;
                GUI.Label(new Rect(card.x + 24, card.y + 22, card.width - 48, 42), banner.DisplayName, titleStyle);
                GUI.Label(new Rect(card.x + 30, card.y + 72, card.width - 60, 62), banner.Description, centeredStyle);
                GUI.Label(new Rect(card.x + 30, card.y + 140, card.width - 60, 100), "3★ 75%   •   4★ 20%   •   5★ 5%\n10-pull guarantees 4★+\n5★ Pity: " + state.GetPity(banner) + " / " + banner.HardPity, centeredStyle);
                GUI.Label(new Rect(card.x + 28, card.y + 245, card.width - 56, 95), "AVAILABLE\n" + string.Join("  •  ", banner.CharacterPool.Select(value => value.DisplayName)), centeredStyle);
                string currency = banner.Currency == BannerCurrency.Lumina ? "Lumina" : "Tickets";
                GUI.Label(new Rect(card.x + 28, card.y + 347, card.width - 56, 35), "OWNED: " + state.CurrencyFor(banner) + " " + currency, headingStyle);
                GUI.enabled = revealResults == null && state.CurrencyFor(banner) >= banner.SinglePullCost;
                if (GUI.Button(new Rect(card.x + 30, card.yMax - 80, card.width / 2 - 40, 52), "SUMMON x1\n" + banner.SinglePullCost + " " + currency, bigButtonStyle)) BeginSummon(banner, 1);
                GUI.enabled = revealResults == null && state.CurrencyFor(banner) >= banner.TenPullCost;
                if (GUI.Button(new Rect(card.x + card.width / 2 + 10, card.yMax - 80, card.width / 2 - 40, 52), "SUMMON x10\n" + banner.TenPullCost + " " + currency, bigButtonStyle)) BeginSummon(banner, 10);
                GUI.enabled = true;
            }
        }

        private void DrawStats(Rect area)
        {
            TelemetrySaveData t = state.Telemetry; GUI.Box(area, GUIContent.none, cardStyle);
            GUI.Label(new Rect(area.x + 30, area.y + 24, area.width - 60, 45), "RIFT RECORD • LOCAL PROTOTYPE TELEMETRY", titleStyle);
            GUI.Label(new Rect(area.x + 55, area.y + 90, area.width * .45f, area.height - 120),
                "Current Run Time\n" + Duration(t.currentRunSeconds) + "\n\nLifetime Play Time\n" + Duration(t.lifetimePlaySeconds) + "\n\nLast Ascension Run\n" + Duration(t.lastAscensionRunSeconds) + "\n\nAscensions\n" + state.AscensionCount, headingStyle);
            GUI.Label(new Rect(area.x + area.width * .52f, area.y + 90, area.width * .42f, area.height - 120),
                "Highest Level Reached\n" + t.highestLevelReached + "\n\nTotal Pulls / 5★ Pulls\n" + t.totalPulls + " / " + t.fiveStarPulls + "\n\nTotal Clicks\n" + t.totalClicks + "\n\nLifetime Credits\n" + Number(t.lifetimeCreditsEarned), headingStyle);
        }

        private void DrawOfflineOverlay(float width, float height)
        {
            DrawModalShade(width, height); Rect box = new Rect(width / 2 - 280, height / 2 - 190, 560, 380); GUI.Box(box, GUIContent.none, cardStyle);
            GUI.Label(new Rect(box.x + 30, box.y + 32, box.width - 60, 50), "WELCOME BACK", titleStyle);
            GUI.Label(new Rect(box.x + 45, box.y + 100, box.width - 90, 130), "You were away for " + Duration(pendingOfflineSeconds) + ".\n\nEarned\n<size=30>" + Number(pendingOfflineCredits) + " Credits</size>\n\nOffline earnings are capped at " + config.OfflineEarningsCapHours + " hours.", centeredStyle);
            if (GUI.Button(new Rect(box.x + 95, box.yMax - 82, box.width - 190, 52), "COLLECT", bigButtonStyle))
            { state.CollectOfflineCredits(pendingOfflineCredits); pendingOfflineCredits = 0; pendingOfflineSeconds = 0; showOffline = false; SaveNow(); }
        }

        private void DrawAscensionOverlay(float width, float height)
        {
            DrawModalShade(width, height); Rect box = new Rect(width / 2 - 300, height / 2 - 175, 600, 350); GUI.Box(box, GUIContent.none, cardStyle);
            AscensionReward reward = state.ProjectedAscensionReward;
            GUI.Label(new Rect(box.x + 30, box.y + 30, box.width - 60, 45), "ASCEND THIS RUN?", titleStyle);
            GUI.Label(new Rect(box.x + 45, box.y + 90, box.width - 90, 125), "+" + reward.Lumina + " Lumina   •   +" + reward.Power + " Rift Power\n\nResets Credits, run levels, and milestone claims.\nKeeps collection, Affinity, currencies, pity, and telemetry.", centeredStyle);
            if (GUI.Button(new Rect(box.x + 55, box.yMax - 75, 220, 44), "CANCEL", bigButtonStyle)) showAscensionConfirm = false;
            if (GUI.Button(new Rect(box.xMax - 275, box.yMax - 75, 220, 44), "ASCEND", bigButtonStyle)) { showAscensionConfirm = false; state.TryAscend(); SaveNow(); }
        }

        private void DrawResetOverlay(float width, float height)
        {
            DrawModalShade(width, height); Rect box = new Rect(width / 2 - 285, height / 2 - 150, 570, 300); GUI.Box(box, GUIContent.none, cardStyle);
            GUI.Label(new Rect(box.x + 30, box.y + 28, box.width - 60, 42), "RESET LOCAL SAVE?", titleStyle);
            GUI.Label(new Rect(box.x + 45, box.y + 90, box.width - 90, 70), "This clears the current run, collection, pity, Rift Rank, and local telemetry.", centeredStyle);
            if (GUI.Button(new Rect(box.x + 45, box.yMax - 72, 220, 42), "CANCEL", bigButtonStyle)) showResetConfirm = false;
            if (GUI.Button(new Rect(box.xMax - 265, box.yMax - 72, 220, 42), "RESET SAVE", bigButtonStyle))
            { showResetConfirm = false; saves.Delete(); pendingOfflineCredits = 0; pendingOfflineSeconds = 0; state.ResetAllProgress(); SaveNow(); }
        }

        private void DrawReveal(float width, float height)
        {
            DrawModalShade(width, height); GachaResult result = revealResults[revealIndex]; int stars = (int)result.Character.Definition.Rarity;
            float pulse = .85f + Mathf.PingPong(Time.unscaledTime * .7f, .15f); float cardWidth = 400 * pulse; float cardHeight = 430 * pulse;
            GUI.backgroundColor = stars == 5 ? new Color(1f,.72f,.2f) : stars == 4 ? new Color(.7f,.4f,1f) : new Color(.3f,.65f,1f);
            Rect card = new Rect(width / 2 - cardWidth / 2, height / 2 - cardHeight / 2, cardWidth, cardHeight); GUI.Box(card, GUIContent.none, cardStyle); GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(card.x + 20, card.y + 55, card.width - 40, 55), Stars(result.Character.Definition.Rarity), titleStyle);
            GUI.Label(new Rect(card.x + 20, card.y + 140, card.width - 40, 55), result.Character.Definition.DisplayName, titleStyle);
            GUI.Label(new Rect(card.x + 25, card.y + 230, card.width - 50, 95), result.WasDuplicate ? "DUPLICATE\n+" + result.AffinityAwarded + " AFFINITY XP" : "NEW!", centeredStyle);
            GUI.Label(new Rect(card.x + 20, card.yMax - 45, card.width - 40, 25), (revealIndex + 1) + " / " + revealResults.Count, centeredStyle);
        }

        private void BeginSummon(BannerData banner, int count)
        {
            List<GachaResult> results; if (!state.TrySummon(banner, count, out results)) return;
            revealResults = results; revealIndex = 0; revealTimer = RevealDuration(results[0]); SaveNow();
        }

        private static float RevealDuration(GachaResult result) { return result.Character.Definition.Rarity == CharacterRarity.FiveStar ? 1.5f : .9f; }
        private static string UnlockLine(bool unlocked, string text) { return (unlocked ? "✓ " : "□ ") + text; }
        private static string Stars(CharacterRarity rarity) { return new string('★', (int)rarity); }
        private static string Roman(int rank) { return rank == 3 ? "III" : rank == 2 ? "II" : "I"; }
        private static string RarityRole(CharacterRarity rarity) { return rarity == CharacterRarity.ThreeStar ? "Early efficiency" : rarity == CharacterRarity.FiveStar ? "Late-run scaling" : "Balanced growth"; }
        private static string Number(double value) { if (value >= 1e12) return (value/1e12).ToString("0.##")+"T"; if (value >= 1e9) return (value/1e9).ToString("0.##")+"B"; if (value >= 1e6) return (value/1e6).ToString("0.##")+"M"; if (value >= 1e3) return (value/1e3).ToString("0.##")+"K"; return value.ToString(value < 100 ? "0.0" : "0"); }
        private static string Duration(double seconds) { TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, seconds)); return time.TotalHours >= 1 ? ((int)time.TotalHours) + "h " + time.Minutes + "m" : time.Minutes > 0 ? time.Minutes + "m " + time.Seconds + "s" : time.Seconds + "s"; }
        private static void DrawBackground(Rect rect, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        private static void DrawModalShade(float width, float height) { DrawBackground(new Rect(0,0,width,height), new Color(0,0,0,.86f)); }

        private void EnsureStyles()
        {
            if (stylesReady) return; stylesReady = true;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true, normal = { textColor = new Color(.3f,.82f,1f) } };
            headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true, normal = { textColor = Color.white } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperLeft, wordWrap = true, richText = true, normal = { textColor = new Color(.84f,.87f,.95f) } };
            centeredStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.75f,.8f,.9f) } };
            cardStyle = new GUIStyle(GUI.skin.box); cardStyle.normal.background = MakeTexture(new Color(.075f,.09f,.16f,.98f));
            bigButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true, normal = { textColor = Color.white }, hover = { textColor = Color.white }, active = { textColor = Color.white } };
        }

        private static Texture2D MakeTexture(Color color) { Texture2D texture = new Texture2D(1,1); texture.SetPixel(0,0,color); texture.Apply(); return texture; }
    }
}
