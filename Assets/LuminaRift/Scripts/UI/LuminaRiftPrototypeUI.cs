using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LuminaRift
{
    public sealed class LuminaRiftPrototypeUI : MonoBehaviour
    {
        private enum ScreenView { Home, Characters, Summon, Stats }
        private sealed class ClickEffect { public Vector2 Position; public float Born; public string Text; public Color Color; public float Drift; }

        private static readonly Color Ink = Hex("080B14"), Navy = Hex("101526"), Panel = Hex("171D36"),
            DeepBlue = Hex("222B55"), Cyan = Hex("66E4FF"), Violet = Hex("A67CFF"),
            Gold = Hex("F6C85F"), Muted = Hex("8792B8");

        private PrototypeGameConfig config;
        private LuminaRiftGameState state;
        private LocalSaveSystem saves;
        private ScreenView screen;
        private string message = "The Rift is open.";
        private float autosaveTimer, characterPunch, levelFlash;
        private double pendingOfflineCredits, pendingOfflineSeconds;
        private bool showOffline, showAscensionConfirm, showResetConfirm;
        private List<GachaResult> revealResults;
        private int revealIndex, selectedBanner = 1;
        private float revealTimer;
        private Texture2D solaraArt, pixel, softCircle, ring;
        private readonly List<ClickEffect> clickEffects = new List<ClickEffect>();
        private GUIStyle display, title, centeredDisplay, centeredTitle, eyebrow, heading, body, centered, small, currency, nav, navActive,
            primaryButton, secondaryButton, ghostButton, dangerButton, panelStyle, cardStyle;
        private bool stylesReady;

        public void Initialize(PrototypeGameConfig gameConfig)
        {
            config = gameConfig; state = new LuminaRiftGameState(config); saves = new LocalSaveSystem();
            state.MessageRaised += value => message = value;
            solaraArt = Resources.Load<Texture2D>("Art/Solara");
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
                    pendingOfflineSeconds += capped; pendingOfflineCredits += state.PassiveIncomePerSecond * capped;
                }
                showOffline = pendingOfflineCredits > 0.01;
                message = "Signal restored. Welcome back, Resonator.";
            }
            SaveNow();
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime; state.Tick(delta); autosaveTimer += delta;
            characterPunch = Mathf.MoveTowards(characterPunch, 0, delta * 5.5f);
            levelFlash = Mathf.MoveTowards(levelFlash, 0, delta * 2.8f);
            clickEffects.RemoveAll(item => Time.unscaledTime - item.Born > 1.05f);
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
        private void SaveNow() { if (saves != null && state != null) saves.Save(state.CreateSave(pendingOfflineCredits, pendingOfflineSeconds)); }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            bool hasBlockingOverlay = showOffline || showAscensionConfirm || showResetConfirm || revealResults != null;
            DrawCosmicBackground(width, height);
            GUI.enabled = !hasBlockingOverlay;
            DrawHeader(width);
            Rect content = new Rect(24, 82, width - 48, height - 170);
            if (screen == ScreenView.Home) DrawHome(content);
            else if (screen == ScreenView.Characters) DrawCharacters(content);
            else if (screen == ScreenView.Summon) DrawSummon(content);
            else DrawStats(content);
            DrawNavigation(width, height); GUI.Label(new Rect(width / 2 - 310, height - 103, 620, 22), message, small);
            GUI.enabled = true;
            if (showOffline) DrawOfflineOverlay(width, height);
            else if (showAscensionConfirm) DrawAscensionOverlay(width, height);
            else if (showResetConfirm) DrawResetOverlay(width, height);
            else if (revealResults != null) DrawReveal(width, height);
            GUI.matrix = previous;
        }

        private void DrawCosmicBackground(float width, float height)
        {
            DrawRect(new Rect(0, 0, width, height), Ink);
            DrawRect(new Rect(0, 0, width, height * .58f), new Color(.04f, .055f, .13f, .72f));
            DrawGlow(new Vector2(width * .67f, height * .38f), 510, new Color(.22f, .12f, .55f, .17f));
            DrawGlow(new Vector2(width * .36f, height * .46f), 420, new Color(.05f, .5f, .72f, .12f));
            for (int i = 0; i < 46; i++)
            {
                float x = Mathf.Repeat(i * 173.17f, width), y = Mathf.Repeat(i * 91.73f + 17, height - 100);
                float twinkle = .25f + .5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * (.25f + i % 5 * .07f) + i));
                float size = i % 11 == 0 ? 3 : 1.4f;
                DrawRect(new Rect(x, y, size, size), new Color(.55f, .82f, 1f, twinkle));
            }
            DrawLine(new Vector2(width * .04f, 88), new Vector2(width * .24f, height - 118), new Color(.35f, .45f, 1f, .08f), 1);
            DrawLine(new Vector2(width * .86f, 76), new Vector2(width * .98f, height - 144), new Color(.55f, .3f, 1f, .1f), 1);
        }

        private void DrawHeader(float width)
        {
            DrawRect(new Rect(0, 0, width, 66), new Color(.035f, .045f, .095f, .96f));
            DrawRect(new Rect(0, 65, width, 1), new Color(.35f, .75f, 1f, .22f));
            GUI.Label(new Rect(26, 10, 52, 44), "◇", display);
            GUI.Label(new Rect(70, 9, 255, 26), "LUMINA RIFT", title);
            GUI.Label(new Rect(72, 34, 220, 18), "DIMENSIONAL RESONANCE", eyebrow);
            DrawCurrency(new Rect(width - 665, 9, 190, 45), "◇", Number(state.Credits), "CREDITS", Cyan);
            DrawCurrency(new Rect(width - 468, 9, 130, 45), "▱", state.StandardTickets.ToString(), "TICKETS", Violet);
            DrawCurrency(new Rect(width - 330, 9, 130, 45), "✦", state.Lumina.ToString(), "LUMINA", Gold);
            DrawCurrency(new Rect(width - 192, 9, 112, 45), "◈", state.RiftRank.ToString("00"), "RANK", Cyan);
            if (GUI.Button(new Rect(width - 68, 13, 48, 38), "LOG", ghostButton)) screen = ScreenView.Stats;
        }

        private void DrawCurrency(Rect rect, string icon, string value, string label, Color accent)
        {
            DrawRect(rect, new Color(.08f, .105f, .2f, .78f)); DrawRect(new Rect(rect.x, rect.y, 2, rect.height), accent);
            Color old = GUI.color; GUI.color = accent; GUI.Label(new Rect(rect.x + 8, rect.y + 2, 32, rect.height - 4), icon, heading); GUI.color = old;
            GUI.Label(new Rect(rect.x + 40, rect.y + 4, rect.width - 45, 22), value, currency);
            GUI.Label(new Rect(rect.x + 41, rect.y + 25, rect.width - 45, 14), label, eyebrow);
        }

        private void DrawNavigation(float width, float height)
        {
            float y = height - 72; DrawRect(new Rect(0, y - 8, width, 80), new Color(.035f, .045f, .095f, .98f));
            DrawRect(new Rect(0, y - 8, width, 1), new Color(.45f, .5f, 1f, .2f));
            float navWidth = Mathf.Min(760, width - 48) / 4f, start = (width - navWidth * 4) / 2;
            NavButton(new Rect(start, y, navWidth, 55), ScreenView.Home, "⌂", "HOME");
            NavButton(new Rect(start + navWidth, y, navWidth, 55), ScreenView.Characters, "◇", "ECHOES");
            NavButton(new Rect(start + navWidth * 2, y, navWidth, 55), ScreenView.Summon, "✦", "SUMMON");
            if (GUI.Button(new Rect(start + navWidth * 3, y, navWidth, 55), "◈\nASCEND", showAscensionConfirm ? navActive : nav))
            { screen = ScreenView.Home; if (state.CanAscend) showAscensionConfirm = true; else message = "Reach level " + config.AscensionMinimumLevel + " to stabilize the Ascension Rift."; }
        }

        private void NavButton(Rect rect, ScreenView target, string icon, string label)
        {
            bool active = screen == target;
            if (GUI.Button(rect, icon + "\n" + label, active ? navActive : nav)) screen = target;
            if (active) DrawRect(new Rect(rect.x + 30, rect.y + 53, rect.width - 60, 2), Cyan);
        }

        private void DrawHome(Rect area)
        {
            CharacterRuntimeState active = state.ActiveCharacter; CharacterData data = active.Definition;
            Rect stage = new Rect(area.x, area.y, area.width * .69f, area.height);
            Rect rail = new Rect(stage.xMax + 16, area.y, area.xMax - stage.xMax - 16, area.height);
            GUI.Box(stage, GUIContent.none, panelStyle); DrawFrame(stage, new Color(Cyan.r, Cyan.g, Cyan.b, .34f));
            DrawRect(new Rect(stage.x, stage.y, 3, stage.height), new Color(Cyan.r, Cyan.g, Cyan.b, .75f));
            GUI.Label(new Rect(stage.x + 25, stage.y + 18, 250, 18), "ACTIVE RESONANCE", eyebrow);
            GUI.Label(new Rect(stage.x + 24, stage.y + 42, stage.width - 48, 43), data.DisplayName.ToUpperInvariant(), display);
            Color rarity = RarityColor(data.Rarity), old = GUI.color; GUI.color = rarity;
            GUI.Label(new Rect(stage.x + 27, stage.y + 84, 220, 24), Stars(data.Rarity), heading); GUI.color = old;

            Rect artRect = new Rect(stage.x + 150, stage.y + 15, stage.width - 250, stage.height - 70);
            DrawCharacter(active, ScaleAround(artRect, 1f + characterPunch * .045f), true);
            Rect clickZone = new Rect(stage.x + 90, stage.y + 36, stage.width - 180, stage.height - 105);
            if (GUI.Button(clickZone, GUIContent.none, GUIStyle.none))
            {
                state.EarnClick(); characterPunch = 1; Vector2 point = Event.current.mousePosition;
                clickEffects.Add(new ClickEffect { Position = point, Born = Time.unscaledTime, Text = "+" + Number(state.ClickIncome), Color = Cyan, Drift = UnityEngine.Random.Range(-18f, 18f) });
            }
            DrawClickEffects();

            DrawRect(new Rect(stage.x + 24, stage.yMax - 98, stage.width - 48, 74), new Color(.025f, .035f, .09f, .9f));
            GUI.Label(new Rect(stage.x + 42, stage.yMax - 90, 145, 20), "LV. " + active.Level.ToString("00"), title);
            GUI.Label(new Rect(stage.x + 42, stage.yMax - 66, 210, 18), "AFFINITY " + Roman(state.GetAffinityRank(active)), eyebrow);
            GUI.Label(new Rect(stage.x + 250, stage.yMax - 86, 210, 22), "+" + Number(state.ClickIncome) + " / CLICK", heading);
            GUI.Label(new Rect(stage.x + 250, stage.yMax - 60, 210, 22), "+" + Number(state.PassiveIncomePerSecond) + " / SEC", heading);
            GUI.Label(new Rect(stage.xMax - 255, stage.yMax - 78, 220, 28), "TAP TO RESONATE", centered);

            GUI.Box(rail, GUIContent.none, panelStyle); DrawFrame(rail, new Color(Violet.r, Violet.g, Violet.b, .28f));
            GUI.Label(new Rect(rail.x + 24, rail.y + 18, rail.width - 48, 18), "RIFT RANK " + state.RiftRank.ToString("00"), eyebrow);
            GUI.Label(new Rect(rail.x + 24, rail.y + 44, rail.width - 48, 34), "RESONATOR", title);
            DrawStatLine(rail.x + 24, rail.y + 93, rail.width - 48, "INCOME POWER", "×" + state.AscensionMultiplier.ToString("0.00"));
            DrawStatLine(rail.x + 24, rail.y + 133, rail.width - 48, "ASCENSIONS", state.AscensionCount.ToString("00"));
            DrawStatLine(rail.x + 24, rail.y + 173, rail.width - 48, "NEXT LEVEL", "◇ " + Number(state.NextLevelCost));
            GUI.Label(new Rect(rail.x + 24, rail.y + 224, rail.width - 48, 18), "LEVEL MATRIX", eyebrow);
            int buyAmount = state.LevelTenUnlocked ? 10 : 1; GUI.enabled = state.CanLevel;
            Color previousColor = GUI.backgroundColor; GUI.backgroundColor = Color.Lerp(Cyan, Color.white, levelFlash * .25f);
            if (GUI.Button(new Rect(rail.x + 24, rail.y + 250, rail.width - 48, 58), "LEVEL UP ×" + buyAmount + "\n◇ " + Number(CostPreview(buyAmount)), primaryButton))
            { if (state.BuyLevels(buyAmount) > 0) levelFlash = 1; }
            GUI.backgroundColor = previousColor; GUI.enabled = true;
            if (state.LevelMaxUnlocked && GUI.Button(new Rect(rail.x + 24, rail.y + 316, (rail.width - 56) / 2, 38), "LEVEL MAX", secondaryButton)) state.BuyLevels(int.MaxValue);
            if (state.AutoLevelUnlocked && GUI.Button(new Rect(rail.x + 32 + (rail.width - 56) / 2, rail.y + 316, (rail.width - 56) / 2, 38), state.AutoLevelEnabled ? "AUTO • ON" : "AUTO • OFF", secondaryButton)) state.SetAutoLevel(!state.AutoLevelEnabled);
            GUI.Label(new Rect(rail.x + 24, rail.yMax - 115, rail.width - 48, 18), "ASCENSION SIGNAL", eyebrow);
            GUI.enabled = state.CanAscend; AscensionReward reward = state.ProjectedAscensionReward;
            if (GUI.Button(new Rect(rail.x + 24, rail.yMax - 90, rail.width - 48, 58), state.CanAscend ? "OPEN THE RIFT\n+" + reward.Lumina + " ✦   +" + reward.Power + " ◈" : "LOCKED • LV. " + config.AscensionMinimumLevel, secondaryButton)) showAscensionConfirm = true;
            GUI.enabled = true;
        }

        private void DrawCharacters(Rect area)
        {
            GUI.Label(new Rect(area.x + 8, area.y, 420, 34), "ECHO ARCHIVE", title);
            GUI.Label(new Rect(area.x + 8, area.y + 36, 500, 18), state.Roster.Count(item => item.IsOwned) + " / " + state.Roster.Count + " RESONANCES DISCOVERED", eyebrow);
            float gap = 14, cardWidth = (area.width - gap * 2) / 3f, cardHeight = (area.height - 76 - gap) / 2f;
            for (int i = 0; i < state.Roster.Count; i++)
            {
                int col = i % 3, row = i / 3;
                DrawCharacterCard(new Rect(area.x + col * (cardWidth + gap), area.y + 70 + row * (cardHeight + gap), cardWidth, cardHeight), state.Roster[i]);
            }
        }

        private void DrawCharacterCard(Rect card, CharacterRuntimeState character)
        {
            CharacterData data = character.Definition; Color rarity = RarityColor(data.Rarity);
            GUI.Box(card, GUIContent.none, cardStyle); DrawFrame(card, new Color(rarity.r, rarity.g, rarity.b, character == state.ActiveCharacter ? .48f : .24f));
            DrawRect(new Rect(card.x, card.y, 3, card.height), character == state.ActiveCharacter ? Cyan : new Color(rarity.r, rarity.g, rarity.b, .7f));
            Rect portrait = new Rect(card.x + 4, card.y + 4, card.width * .45f, card.height - 8);
            if (character.IsOwned) DrawCharacter(character, portrait, false);
            else { DrawGlow(portrait.center, portrait.height * .65f, new Color(rarity.r, rarity.g, rarity.b, .12f)); GUI.Label(new Rect(portrait.x, portrait.y + portrait.height * .3f, portrait.width, 60), "?", centeredDisplay); }
            Rect info = new Rect(card.x + card.width * .43f, card.y, card.width * .57f, card.height);
            Color old = GUI.color; GUI.color = rarity; GUI.Label(new Rect(info.x + 8, info.y + 14, info.width - 16, 22), Stars(data.Rarity), small); GUI.color = old;
            GUI.Label(new Rect(info.x + 8, info.y + 42, info.width - 16, 28), character.IsOwned ? data.DisplayName.ToUpperInvariant() : "UNDISCOVERED", heading);
            if (character.IsOwned)
            {
                GUI.Label(new Rect(info.x + 8, info.y + 74, info.width - 16, 46), "LV. " + character.Level + "   •   AFFINITY " + Roman(state.GetAffinityRank(character)) + "\n" + RarityRole(data.Rarity), small);
                float progress = AffinityProgress(character.AffinityXp);
                DrawRect(new Rect(info.x + 8, info.y + 124, info.width - 25, 4), new Color(.12f, .15f, .28f));
                DrawRect(new Rect(info.x + 8, info.y + 124, (info.width - 25) * progress, 4), rarity);
                GUI.enabled = character != state.ActiveCharacter;
                if (GUI.Button(new Rect(info.x + 8, info.yMax - 46, info.width - 24, 30), character == state.ActiveCharacter ? "ACTIVE" : "SET ACTIVE", character == state.ActiveCharacter ? primaryButton : ghostButton)) state.SetActiveCharacter(character);
                GUI.enabled = true;
            }
            else GUI.Label(new Rect(info.x + 8, info.yMax - 55, info.width - 20, 34), "SUMMON TO REVEAL", eyebrow);
        }

        private void DrawSummon(Rect area)
        {
            if (state.Banners.Count == 0) return;
            selectedBanner = Mathf.Clamp(selectedBanner, 0, state.Banners.Count - 1); float tabWidth = 190;
            for (int i = 0; i < state.Banners.Count; i++)
                if (GUI.Button(new Rect(area.x + i * (tabWidth + 8), area.y, tabWidth, 34), state.Banners[i].DisplayName.ToUpperInvariant(), i == selectedBanner ? primaryButton : ghostButton)) selectedBanner = i;
            BannerData banner = state.Banners[selectedBanner]; Rect bannerRect = new Rect(area.x, area.y + 46, area.width, area.height - 46);
            GUI.Box(bannerRect, GUIContent.none, panelStyle); Color accent = banner.Currency == BannerCurrency.Lumina ? Gold : Cyan;
            DrawFrame(bannerRect, new Color(accent.r, accent.g, accent.b, .34f));
            DrawGlow(new Vector2(bannerRect.x + bannerRect.width * .69f, bannerRect.center.y), 510, new Color(banner.AccentColor.r, banner.AccentColor.g, banner.AccentColor.b, .25f));
            DrawPortal(new Vector2(bannerRect.x + bannerRect.width * .72f, bannerRect.center.y), 380, accent);
            if (banner.RateUpCharacter != null && banner.RateUpCharacter.CharacterId == "solara" && solaraArt != null)
                DrawArt(new Rect(bannerRect.x + bannerRect.width * .47f, bannerRect.y + 4, bannerRect.width * .47f, bannerRect.height - 8), solaraArt, .96f);
            else if (banner.RateUpCharacter == null)
            {
                GUI.Label(new Rect(bannerRect.x + bannerRect.width * .54f, bannerRect.center.y - 34, bannerRect.width * .36f, 68), "◇\nARCHIVE SIGNAL", centeredTitle);
            }
            GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 30, 360, 18), banner.RateUpCharacter != null ? "FEATURED RESONANCE" : "STANDARD RESONANCE", eyebrow);
            GUI.Label(new Rect(bannerRect.x + 36, bannerRect.y + 58, 500, 45), banner.DisplayName.ToUpperInvariant(), display);
            if (banner.RateUpCharacter != null)
            {
                GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 112, 420, 34), banner.RateUpCharacter.DisplayName.ToUpperInvariant(), title);
                Color old = GUI.color; GUI.color = Gold; GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 148, 250, 24), Stars(banner.RateUpCharacter.Rarity), heading); GUI.color = old;
                GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 180, 390, 50), "RATE UP • " + Mathf.RoundToInt(banner.RateUpShareOfFiveStar * 100) + "% OF 5★ SIGNALS\n" + banner.Description, body);
            }
            else GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 120, 390, 90), banner.Description + "\n\nEvery tenfold resonance guarantees 4★ or higher.", body);
            int pity = state.GetPity(banner);
            GUI.Label(new Rect(bannerRect.x + 38, bannerRect.yMax - 168, 360, 18), "FIVE-STAR CONVERGENCE", eyebrow);
            DrawRect(new Rect(bannerRect.x + 38, bannerRect.yMax - 143, 330, 6), new Color(.13f, .16f, .3f));
            DrawRect(new Rect(bannerRect.x + 38, bannerRect.yMax - 143, 330 * Mathf.Clamp01((float)pity / banner.HardPity), 6), accent);
            GUI.Label(new Rect(bannerRect.x + 376, bannerRect.yMax - 153, 100, 22), pity + " / " + banner.HardPity, small);
            string symbol = banner.Currency == BannerCurrency.Lumina ? "✦" : "▱";
            GUI.Label(new Rect(bannerRect.x + 38, bannerRect.yMax - 115, 330, 22), "AVAILABLE   " + symbol + " " + state.CurrencyFor(banner), heading);
            GUI.enabled = revealResults == null && state.CurrencyFor(banner) >= banner.SinglePullCost;
            if (GUI.Button(new Rect(bannerRect.x + 38, bannerRect.yMax - 78, 190, 54), "RESONATE ×1\n" + symbol + " " + banner.SinglePullCost, secondaryButton)) BeginSummon(banner, 1);
            GUI.enabled = revealResults == null && state.CurrencyFor(banner) >= banner.TenPullCost;
            if (GUI.Button(new Rect(bannerRect.x + 240, bannerRect.yMax - 78, 220, 54), "RESONATE ×10\n" + symbol + " " + banner.TenPullCost, primaryButton)) BeginSummon(banner, 10);
            GUI.enabled = true; GUI.Label(new Rect(bannerRect.xMax - 325, bannerRect.yMax - 44, 290, 18), "3★ 75%   •   4★ 20%   •   5★ 5%", eyebrow);
        }

        private void DrawStats(Rect area)
        {
            TelemetrySaveData t = state.Telemetry; GUI.Box(area, GUIContent.none, panelStyle); DrawFrame(area, new Color(Cyan.r, Cyan.g, Cyan.b, .24f));
            GUI.Label(new Rect(area.x + 34, area.y + 25, area.width - 68, 38), "RIFT RECORD", title);
            GUI.Label(new Rect(area.x + 34, area.y + 63, area.width - 68, 18), "LOCAL RESONATOR TELEMETRY • NOTHING LEAVES THIS DEVICE", eyebrow);
            string[] labels = { "CURRENT RUN", "LIFETIME SIGNAL", "LAST ASCENSION", "HIGHEST LEVEL", "TOTAL PULLS", "FIVE-STAR PULLS", "TOTAL CLICKS", "LIFETIME CREDITS" };
            string[] values = { Duration(t.currentRunSeconds), Duration(t.lifetimePlaySeconds), Duration(t.lastAscensionRunSeconds), t.highestLevelReached.ToString(), t.totalPulls.ToString(), t.fiveStarPulls.ToString(), t.totalClicks.ToString(), Number(t.lifetimeCreditsEarned) };
            float cellW = (area.width - 100) / 4;
            for (int i = 0; i < labels.Length; i++)
            {
                int col = i % 4, row = i / 4; Rect cell = new Rect(area.x + 38 + col * (cellW + 8), area.y + 115 + row * 130, cellW, 110);
                DrawRect(cell, new Color(.055f, .07f, .14f, .9f)); GUI.Label(new Rect(cell.x + 12, cell.y + 16, cell.width - 24, 18), labels[i], eyebrow);
                GUI.Label(new Rect(cell.x + 12, cell.y + 45, cell.width - 24, 38), values[i], title);
            }
            if (GUI.Button(new Rect(area.xMax - 225, area.yMax - 54, 190, 34), "DEVELOPER • RESET SAVE", dangerButton)) showResetConfirm = true;
        }

        private void DrawCharacter(CharacterRuntimeState character, Rect rect, bool large)
        {
            if (character.Definition.CharacterId == "solara" && solaraArt != null)
            { DrawGlow(rect.center, rect.height * .75f, new Color(.35f, .45f, 1f, large ? .2f : .1f)); DrawArt(rect, solaraArt, large ? 1f : .9f); return; }
            Color accent = character.Definition.AccentColor; DrawGlow(rect.center, rect.height * .72f, new Color(accent.r, accent.g, accent.b, .16f));
            float head = Mathf.Min(rect.width, rect.height) * .18f;
            DrawTexture(new Rect(rect.center.x - head / 2, rect.y + rect.height * .18f, head, head), softCircle, new Color(accent.r, accent.g, accent.b, .76f));
            DrawTexture(new Rect(rect.center.x - rect.width * .18f, rect.y + rect.height * .35f, rect.width * .36f, rect.height * .54f), softCircle, new Color(accent.r * .5f, accent.g * .5f, accent.b * .65f, .72f));
            for (int i = 0; i < 3; i++)
            {
                float angle = Time.unscaledTime * (10 + i * 3) + i * 120;
                Vector2 p = rect.center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * (rect.width * (.24f + i * .035f));
                DrawTexture(new Rect(p.x - 4, p.y - 4, 8, 8), softCircle, new Color(accent.r, accent.g, accent.b, .8f));
            }
        }

        private void DrawClickEffects()
        {
            foreach (ClickEffect effect in clickEffects)
            {
                float age = Time.unscaledTime - effect.Born, alpha = 1 - Mathf.Clamp01(age / 1.05f);
                Vector2 p = effect.Position + new Vector2(effect.Drift * age, -62 * age);
                Color old = GUI.color; GUI.color = new Color(effect.Color.r, effect.Color.g, effect.Color.b, alpha);
                GUI.Label(new Rect(p.x - 70, p.y - 22, 140, 32), effect.Text, title); GUI.color = old;
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 1.257f + effect.Born * 4; Vector2 spark = effect.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * age * 55;
                    DrawTexture(new Rect(spark.x - 3, spark.y - 3, 6, 6), softCircle, new Color(effect.Color.r, effect.Color.g, effect.Color.b, alpha));
                }
            }
        }

        private void DrawReveal(float width, float height)
        {
            DrawModalShade(width, height); GachaResult result = revealResults[revealIndex]; CharacterData data = result.Character.Definition;
            Color rarity = RarityColor(data.Rarity); float total = RevealDuration(result), progress = 1 - Mathf.Clamp01(revealTimer / total);
            Vector2 center = new Vector2(width / 2, height / 2);
            DrawGlow(center, Mathf.Lerp(120, 720, EaseOut(progress)), new Color(rarity.r, rarity.g, rarity.b, .35f * (1 - progress * .4f)));
            DrawPortal(center, Mathf.Lerp(80, 490, EaseOut(progress)), rarity);
            if (progress < .35f) { GUI.Label(new Rect(center.x - 240, center.y - 35, 480, 70), "RESONANCE DETECTED", centeredTitle); return; }
            Rect card = ScaleAround(new Rect(center.x - 245, center.y - 280, 490, 555), Mathf.Lerp(.82f, 1f, EaseOut((progress - .35f) / .65f)));
            GUI.Box(card, GUIContent.none, panelStyle); DrawFrame(card, new Color(rarity.r, rarity.g, rarity.b, .55f)); DrawRect(new Rect(card.x, card.y, 4, card.height), rarity);
            if (data.CharacterId == "solara" && solaraArt != null) DrawArt(new Rect(card.x + 5, card.y + 5, card.width * .57f, card.height - 10), solaraArt, .95f);
            else DrawCharacter(result.Character, new Rect(card.x + 15, card.y + 40, card.width * .5f, card.height - 80), true);
            Rect info = new Rect(card.x + card.width * .5f, card.y + 30, card.width * .47f, card.height - 60);
            GUI.Label(new Rect(info.x, info.y, info.width, 18), result.WasDuplicate ? "RESONANCE DEEPENED" : "NEW ECHO", eyebrow);
            Color old = GUI.color; GUI.color = rarity; GUI.Label(new Rect(info.x, info.y + 35, info.width, 30), Stars(data.Rarity), heading); GUI.color = old;
            GUI.Label(new Rect(info.x, info.y + 82, info.width, 44), data.DisplayName.ToUpperInvariant(), title);
            GUI.Label(new Rect(info.x, info.y + 140, info.width - 8, 110), data.Description, body);
            GUI.Label(new Rect(info.x, info.y + 285, info.width, 55), result.WasDuplicate ? "+" + result.AffinityAwarded + " AFFINITY XP" : "ARCHIVE UPDATED", centered);
            GUI.Label(new Rect(info.x, info.yMax - 35, info.width, 22), (revealIndex + 1) + " / " + revealResults.Count, small);
            if (GUI.Button(new Rect(width - 116, height - 62, 90, 34), "SKIP", ghostButton)) revealResults = null;
        }

        private void DrawOfflineOverlay(float width, float height)
        {
            DrawModalShade(width, height); Rect box = ModalBox(width, height, 580, 360); GUI.Box(box, GUIContent.none, panelStyle);
            DrawPortal(new Vector2(box.center.x, box.y + 80), 130, Cyan);
            DrawFrame(box, new Color(Cyan.r, Cyan.g, Cyan.b, .45f));
            GUI.Label(new Rect(box.x + 35, box.y + 30, box.width - 70, 42), "SIGNAL RESTORED", centeredTitle);
            GUI.Label(new Rect(box.x + 48, box.y + 105, box.width - 96, 115), "The Rift resonated for " + Duration(pendingOfflineSeconds) + ".\n\n<size=30><color=#66E4FF>◇ " + Number(pendingOfflineCredits) + "</color></size>\n\nOffline resonance is capped at " + config.OfflineEarningsCapHours + " hours.", centered);
            if (GUI.Button(new Rect(box.x + 130, box.yMax - 72, box.width - 260, 46), "COLLECT CREDITS", primaryButton))
            { state.CollectOfflineCredits(pendingOfflineCredits); pendingOfflineCredits = 0; pendingOfflineSeconds = 0; showOffline = false; SaveNow(); }
        }

        private void DrawAscensionOverlay(float width, float height)
        {
            DrawModalShade(width, height); Rect box = ModalBox(width, height, 620, 365); GUI.Box(box, GUIContent.none, panelStyle);
            AscensionReward reward = state.ProjectedAscensionReward;
            DrawFrame(box, new Color(Violet.r, Violet.g, Violet.b, .45f));
            GUI.Label(new Rect(box.x + 35, box.y + 30, box.width - 70, 42), "OPEN THE ASCENSION RIFT?", centeredTitle);
            GUI.Label(new Rect(box.x + 55, box.y + 90, box.width - 110, 145), "<size=27><color=#F6C85F>+" + reward.Lumina + " ✦</color>    <color=#66E4FF>+" + reward.Power + " ◈</color></size>\n\nCredits, run levels, and milestone claims will dissolve.\nYour archive, Affinity, currencies, pity, and records remain.", centered);
            if (GUI.Button(new Rect(box.x + 55, box.yMax - 70, 235, 42), "CANCEL", ghostButton)) showAscensionConfirm = false;
            if (GUI.Button(new Rect(box.xMax - 290, box.yMax - 70, 235, 42), "ASCEND", primaryButton)) { showAscensionConfirm = false; state.TryAscend(); characterPunch = 1; SaveNow(); }
        }

        private void DrawResetOverlay(float width, float height)
        {
            DrawModalShade(width, height); Rect box = ModalBox(width, height, 560, 285); GUI.Box(box, GUIContent.none, panelStyle);
            DrawFrame(box, new Color(1f, .28f, .38f, .4f));
            GUI.Label(new Rect(box.x + 35, box.y + 30, box.width - 70, 42), "RESET LOCAL SAVE?", centeredTitle);
            GUI.Label(new Rect(box.x + 48, box.y + 90, box.width - 96, 70), "This clears the current run, archive, pity, Rift Rank, and local record. This cannot be undone.", centered);
            if (GUI.Button(new Rect(box.x + 45, box.yMax - 66, 220, 40), "CANCEL", ghostButton)) showResetConfirm = false;
            if (GUI.Button(new Rect(box.xMax - 265, box.yMax - 66, 220, 40), "RESET SAVE", dangerButton))
            { showResetConfirm = false; saves.Delete(); pendingOfflineCredits = 0; pendingOfflineSeconds = 0; state.ResetAllProgress(); SaveNow(); }
        }

        private void DrawStatLine(float x, float y, float width, string label, string value)
        { GUI.Label(new Rect(x, y, width * .55f, 22), label, eyebrow); GUI.Label(new Rect(x + width * .48f, y - 2, width * .52f, 26), value, heading); DrawRect(new Rect(x, y + 29, width, 1), new Color(.35f, .45f, .8f, .15f)); }

        private double CostPreview(int amount)
        {
            if (amount <= 1) return state.NextLevelCost;
            double result = 0; int level = state.ActiveCharacter.Level;
            for (int i = 0; i < amount && level + i < config.RunLevelCap; i++)
                result += config.FirstLevelCost * state.ActiveCharacter.Definition.LevelCostMultiplier * Math.Pow(config.LevelCostGrowth, level + i - 1);
            return result;
        }

        private float AffinityProgress(int xp)
        {
            if (xp >= config.AffinityRankThreeXp) return 1f;
            if (xp >= config.AffinityRankTwoXp)
                return Mathf.InverseLerp(config.AffinityRankTwoXp, config.AffinityRankThreeXp, xp);
            return Mathf.InverseLerp(0, config.AffinityRankTwoXp, xp);
        }

        private void BeginSummon(BannerData banner, int count)
        { List<GachaResult> results; if (!state.TrySummon(banner, count, out results)) return; revealResults = results; revealIndex = 0; revealTimer = RevealDuration(results[0]); SaveNow(); }

        private void DrawPortal(Vector2 center, float size, Color color)
        {
            float pulse = 1 + Mathf.Sin(Time.unscaledTime * 1.4f) * .025f;
            DrawTexture(new Rect(center.x - size * pulse / 2, center.y - size * pulse / 2, size * pulse, size * pulse), ring, new Color(color.r, color.g, color.b, .34f));
            DrawTexture(new Rect(center.x - size * .36f, center.y - size * .36f, size * .72f, size * .72f), softCircle, new Color(.18f, .12f, .5f, .16f));
            for (int i = 0; i < 7; i++)
            {
                float angle = Time.unscaledTime * (12 + i) + i * 51.4f;
                Vector2 p = center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * size * .43f;
                DrawTexture(new Rect(p.x - 3, p.y - 3, 6, 6), softCircle, new Color(color.r, color.g, color.b, .7f));
            }
        }

        private void DrawGlow(Vector2 center, float size, Color color) { DrawTexture(new Rect(center.x - size / 2, center.y - size / 2, size, size), softCircle, color); }
        private static void DrawArt(Rect rect, Texture texture, float alpha) { Color old = GUI.color; GUI.color = new Color(1, 1, 1, alpha); GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true); GUI.color = old; }
        private static Rect ScaleAround(Rect rect, float scale) { return new Rect(rect.center.x - rect.width * scale / 2, rect.center.y - rect.height * scale / 2, rect.width * scale, rect.height * scale); }
        private static Rect ModalBox(float width, float height, float w, float h) { return new Rect(width / 2 - w / 2, height / 2 - h / 2, w, h); }
        private static float EaseOut(float value) { value = Mathf.Clamp01(value); return 1 - (1 - value) * (1 - value); }
        private static float RevealDuration(GachaResult result) { return result.Character.Definition.Rarity == CharacterRarity.FiveStar ? 2.6f : result.Character.Definition.Rarity == CharacterRarity.FourStar ? 1.9f : 1.45f; }
        private static string Stars(CharacterRarity rarity) { return new string('★', (int)rarity); }
        private static string Roman(int rank) { return rank == 3 ? "III" : rank == 2 ? "II" : "I"; }
        private static string RarityRole(CharacterRarity rarity) { return rarity == CharacterRarity.ThreeStar ? "EARLY EFFICIENCY" : rarity == CharacterRarity.FiveStar ? "LATE-RUN SCALING" : "BALANCED GROWTH"; }
        private static Color RarityColor(CharacterRarity rarity) { return rarity == CharacterRarity.FiveStar ? Gold : rarity == CharacterRarity.FourStar ? Violet : Cyan; }
        private static string Number(double value) { if (value >= 1e15) return (value / 1e15).ToString("0.##") + "Qa"; if (value >= 1e12) return (value / 1e12).ToString("0.##") + "T"; if (value >= 1e9) return (value / 1e9).ToString("0.##") + "B"; if (value >= 1e6) return (value / 1e6).ToString("0.##") + "M"; if (value >= 1e3) return (value / 1e3).ToString("0.##") + "K"; return value.ToString(value < 100 ? "0.0" : "0"); }
        private static string Duration(double seconds) { TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, seconds)); return time.TotalHours >= 1 ? ((int)time.TotalHours) + "h " + time.Minutes + "m" : time.Minutes > 0 ? time.Minutes + "m " + time.Seconds + "s" : time.Seconds + "s"; }
        private static Color Hex(string hex) { Color value; ColorUtility.TryParseHtmlString("#" + hex, out value); return value; }

        private void DrawRect(Rect rect, Color color) { DrawTexture(rect, pixel, color); }
        private static void DrawTexture(Rect rect, Texture texture, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, texture); GUI.color = old; }
        private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Matrix4x4 oldMatrix = GUI.matrix; Color old = GUI.color; GUI.color = color;
            float angle = Vector3.Angle(b - a, Vector2.right); if (a.y > b.y) angle = -angle;
            GUIUtility.RotateAroundPivot(angle, a); GUI.DrawTexture(new Rect(a.x, a.y, (b - a).magnitude, width), pixel);
            GUI.matrix = oldMatrix; GUI.color = old;
        }
        private void DrawModalShade(float width, float height) { DrawRect(new Rect(0, 0, width, height), new Color(.01f, .015f, .04f, .94f)); }

        private void DrawFrame(Rect rect, Color color)
        {
            const float hairline = 1f, corner = 18f;
            DrawRect(new Rect(rect.x, rect.y, rect.width, hairline), new Color(color.r, color.g, color.b, color.a * .55f));
            DrawRect(new Rect(rect.x, rect.yMax - hairline, rect.width, hairline), new Color(color.r, color.g, color.b, color.a * .35f));
            DrawRect(new Rect(rect.x, rect.y, hairline, rect.height), new Color(color.r, color.g, color.b, color.a * .55f));
            DrawRect(new Rect(rect.xMax - hairline, rect.y, hairline, rect.height), new Color(color.r, color.g, color.b, color.a * .35f));
            DrawRect(new Rect(rect.x, rect.y, corner, 2), color); DrawRect(new Rect(rect.x, rect.y, 2, corner), color);
            DrawRect(new Rect(rect.xMax - corner, rect.yMax - 2, corner, 2), color); DrawRect(new Rect(rect.xMax - 2, rect.yMax - corner, 2, corner), color);
        }

        private void EnsureStyles()
        {
            if (stylesReady) return; stylesReady = true;
            pixel = MakeTexture(2, (x, y) => Color.white);
            softCircle = MakeTexture(128, (x, y) => { float distance = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 63.5f; return new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - distance), 2)); });
            ring = MakeTexture(192, (x, y) => { float d = Vector2.Distance(new Vector2(x, y), new Vector2(95.5f, 95.5f)) / 95.5f; float alpha = Mathf.Clamp01(1 - Mathf.Abs(d - .82f) * 35) * .9f + Mathf.Clamp01(1 - Mathf.Abs(d - .65f) * 80) * .35f; return new Color(1, 1, 1, alpha); });
            display = Label(31, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white); title = Label(24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            centeredDisplay = new GUIStyle(display) { alignment = TextAnchor.MiddleCenter };
            centeredTitle = new GUIStyle(title) { alignment = TextAnchor.MiddleCenter };
            eyebrow = Label(11, FontStyle.Bold, TextAnchor.MiddleLeft, Muted); heading = Label(16, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            body = Label(14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(.82f, .85f, .95f)); body.wordWrap = true;
            centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, richText = true };
            small = Label(12, FontStyle.Normal, TextAnchor.MiddleCenter, Muted); currency = Label(16, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeSolid(new Color(.055f, .07f, .14f, .92f)) }, border = new RectOffset(1, 1, 1, 1) };
            cardStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeSolid(new Color(.065f, .08f, .155f, .96f)) }, border = new RectOffset(1, 1, 1, 1) };
            primaryButton = Button(14, Color.white, MakeSolid(DeepBlue), MakeSolid(new Color(.25f, .32f, .68f)), MakeSolid(new Color(.15f, .7f, .85f)));
            secondaryButton = Button(13, Color.white, MakeSolid(new Color(.11f, .13f, .25f)), MakeSolid(new Color(.19f, .2f, .39f)), MakeSolid(new Color(.22f, .24f, .48f)));
            ghostButton = Button(12, new Color(.82f, .86f, .98f), MakeSolid(new Color(.06f, .075f, .14f, .72f)), MakeSolid(new Color(.12f, .14f, .27f)), MakeSolid(new Color(.18f, .22f, .42f)));
            dangerButton = Button(12, new Color(1f, .72f, .76f), MakeSolid(new Color(.18f, .06f, .1f)), MakeSolid(new Color(.3f, .08f, .14f)), MakeSolid(new Color(.4f, .08f, .15f)));
            nav = Button(11, Muted, MakeSolid(new Color(.035f, .045f, .095f)), MakeSolid(new Color(.07f, .09f, .18f)), MakeSolid(new Color(.09f, .11f, .22f)));
            navActive = new GUIStyle(nav); navActive.normal.textColor = Cyan; navActive.hover.textColor = Cyan; navActive.active.textColor = Color.white;
        }

        private static GUIStyle Label(int size, FontStyle style, TextAnchor alignment, Color color)
        { return new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = style, alignment = alignment, richText = true, normal = { textColor = color } }; }

        private static GUIStyle Button(int size, Color color, Texture2D normal, Texture2D hover, Texture2D active)
        {
            return new GUIStyle(GUI.skin.button) { fontSize = size, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true,
                normal = { textColor = color, background = normal }, hover = { textColor = Color.white, background = hover }, active = { textColor = Color.white, background = active },
                focused = { textColor = color, background = normal }, border = new RectOffset(2, 2, 2, 2), padding = new RectOffset(8, 8, 5, 5) };
        }

        private static Texture2D MakeSolid(Color color) { return MakeTexture(4, (x, y) => color); }
        private static Texture2D MakeTexture(int size, Func<int, int, Color> color)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color[] pixels = new Color[size * size]; for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) pixels[y * size + x] = color(x, y);
            texture.SetPixels(pixels); texture.Apply(); return texture;
        }
    }
}
