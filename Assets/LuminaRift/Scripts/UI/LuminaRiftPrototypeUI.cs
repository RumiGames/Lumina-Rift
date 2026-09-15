using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LuminaRift
{
    public sealed class LuminaRiftPrototypeUI : MonoBehaviour
    {
        private enum ScreenView { Home, Characters, Summon, Stats, Ascension }
        private sealed class ClickEffect { public Vector2 Position; public float Born; public string Text; public Color Color; public float Drift; }
        private sealed class ScrollDragState
        {
            public bool PendingContent, DraggingContent, DraggingBar;
            public Vector2 PointerStart, ScrollStart;
        }

        private static readonly Color TextPrimary = Hex("EDF2FA"),
            PressedSurface = Hex("22344D"), Cyan = Hex("79D8E8"), Violet = Hex("C0AAEF"),
            Gold = Hex("E8BE76"), Muted = Hex("ADBDD1"), Background = Hex("101B2C"),
            Panel = Hex("18283D"), Raised = Hex("21354C"), Edge = Hex("3B526C");

        private PrototypeGameConfig config;
        private LuminaRiftGameState state;
        private LocalSaveSystem saves;
        private ScreenView screen;
        private string message = "A bright new adventure is waiting.";
        private float autosaveTimer, characterPunch, levelFlash;
        private double pendingOfflineCredits, pendingOfflineSeconds;
        private bool showOffline, showAscensionConfirm, showResetConfirm;
        private List<GachaResult> revealResults;
        private int revealIndex, selectedBanner = 1;
        private Vector2 collectionScroll, ascensionScroll;
        private readonly ScrollDragState collectionDrag = new ScrollDragState(), ascensionDrag = new ScrollDragState();
        private float revealTimer;
        private bool showSummonSummary;
        private bool InterfaceEnabled => !showOffline && !showAscensionConfirm && !showResetConfirm && revealResults == null;
        private Texture2D disabledSurface, pixel, softCircle, ring, skyGradient, summonGradient;
        private readonly List<ClickEffect> clickEffects = new List<ClickEffect>();
        private GUIStyle display, title, centeredDisplay, centeredTitle, eyebrow, heading, body, centered, small, currency, nav, navActive,
            primaryButton, secondaryButton, ghostButton, dangerButton, panelStyle, cardStyle;
        private bool stylesReady;
        private readonly Dictionary<(int, FontStyle, TextAnchor, Color), GUIStyle> accentLabels = new Dictionary<(int, FontStyle, TextAnchor, Color), GUIStyle>();
        private readonly Dictionary<GUIStyle, GUIStyle> disabledStyles = new Dictionary<GUIStyle, GUIStyle>();
        private readonly Dictionary<(GUIStyle, string, int), GUIStyle> fittedLabels = new Dictionary<(GUIStyle, string, int), GUIStyle>();

        public void Initialize(PrototypeGameConfig gameConfig)
        {
            config = gameConfig; state = new LuminaRiftGameState(config); saves = new LocalSaveSystem();
            state.MessageRaised += value => message = value;
            PlayerSaveData loaded = saves.Load();
            if (loaded != null)
            {
                if (!loaded.offlineEarningsRateApplied)
                {
                    loaded.pendingOfflineCredits *= config.OfflineEarningsRate;
                    loaded.offlineEarningsRateApplied = true;
                }
                state.Restore(loaded);
                pendingOfflineCredits = Math.Max(0, loaded.pendingOfflineCredits);
                pendingOfflineSeconds = Math.Max(0, loaded.pendingOfflineSeconds);
                if (loaded.lastSaveUtcTicks > 0 && loaded.lastSaveUtcTicks <= DateTime.UtcNow.Ticks)
                {
                    double elapsed = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - loaded.lastSaveUtcTicks).TotalSeconds;
                    double remainingCap = Math.Max(0, config.OfflineEarningsCapHours * 3600d - pendingOfflineSeconds);
                    double capped = Math.Min(elapsed, remainingCap);
                    pendingOfflineSeconds += capped; pendingOfflineCredits += state.OfflineIncomePerSecond * capped;
                }
                showOffline = pendingOfflineCredits > 0.01;
                message = "Welcome back! Your adventure is ready to continue.";
            }
            SaveNow();
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime; state.Tick(delta); autosaveTimer += delta;
            characterPunch = Mathf.MoveTowards(characterPunch, 0, delta * 5.5f);
            levelFlash = Mathf.MoveTowards(levelFlash, 0, delta * 2.8f);
            clickEffects.RemoveAll(item => Time.realtimeSinceStartup - item.Born > 1.05f);
            if (autosaveTimer >= 10f) { SaveNow(); autosaveTimer = 0; }
            if (revealResults != null && !showSummonSummary)
                revealTimer = Mathf.Max(0, revealTimer - delta);
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
            Rect content = new Rect(24, 82, width - 48, height - 202);
            if (screen == ScreenView.Home) DrawHome(content);
            else if (screen == ScreenView.Characters) DrawCharacters(content);
            else if (screen == ScreenView.Summon) DrawSummon(content);
            else if (screen == ScreenView.Ascension) DrawAscension(content);
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
            DrawTexture(new Rect(0, 0, width, height), skyGradient, Color.white);
            DrawGlow(new Vector2(width * .5f, height * .12f), 520, new Color(.4f, .55f, .8f, .09f));
            DrawGlow(new Vector2(width * .83f, height * .38f), 450, new Color(.45f, .35f, .8f, .09f));
            for (int i = 0; i < 12; i++)
            {
                float drift = Mathf.Sin(Time.realtimeSinceStartup * .05f + i) * 12f;
                float size = 190 + i % 4 * 48;
                float x = Mathf.Repeat(i * 157f + drift, width + 220) - 110;
                float y = height * (.62f + (i % 3) * .09f);
                DrawTexture(new Rect(x - size / 2, y - size / 2, size, size * .56f), softCircle, new Color(.3f, .45f, .7f, .07f));
            }
            for (int i = 0; i < 24; i++)
            {
                float x = Mathf.Repeat(i * 173.17f, width), y = Mathf.Repeat(i * 91.73f + 17, height - 100);
                float twinkle = .25f + .5f * Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * (.25f + i % 5 * .07f) + i));
                float size = i % 11 == 0 ? 3 : 1.4f;
                DrawRect(new Rect(x, y, size, size), new Color(.7f, .8f, 1f, twinkle * .3f));
            }
            DrawTexture(new Rect(width * .34f, height * .07f, width * .32f, width * .32f), ring, new Color(1f, .94f, .7f, .08f));
        }

        private void DrawHeader(float width)
        {
            DrawRect(new Rect(0, 0, width, 66), Background);
            DrawRect(new Rect(0, 65, width, 1), new Color(Gold.r, Gold.g, Gold.b, .52f));
            GUI.Label(new Rect(26, 10, 52, 44), "◇", display);
            GUI.Label(new Rect(70, 9, 255, 26), "LUMINA RIFT", title);
            GUI.Label(new Rect(72, 34, 220, 18), "CLICK. COLLECT. ASCEND.", eyebrow);
            DrawCurrency(new Rect(width - 665, 9, 190, 45), "◇", Number(state.Credits), "CREDITS", Cyan);
            DrawCurrency(new Rect(width - 468, 9, 130, 45), "▱", state.StandardTickets.ToString(), "TICKETS", Violet);
            DrawCurrency(new Rect(width - 330, 9, 130, 45), "✦", state.Lumina.ToString(), "LUMINA", Gold);
            DrawCurrency(new Rect(width - 192, 9, 112, 45), "◈", state.RiftRank.ToString("00"), "RANK", Cyan);
            if (ActionButton(new Rect(width - 68, 13, 48, 38), "LOG", ghostButton)) screen = ScreenView.Stats;
        }

        private void DrawCurrency(Rect rect, string icon, string value, string label, Color accent)
        {
            DrawRect(rect, Panel); DrawRect(new Rect(rect.x, rect.y, 3, rect.height), accent);
            DrawFrame(rect, new Color(Gold.r, Gold.g, Gold.b, .22f));
            Color old = GUI.color; GUI.color = accent; GUI.Label(new Rect(rect.x + 8, rect.y + 2, 32, rect.height - 4), icon, heading); GUI.color = old;
            GUI.Label(new Rect(rect.x + 40, rect.y + 4, rect.width - 45, 22), value, currency);
            GUI.Label(new Rect(rect.x + 41, rect.y + 25, rect.width - 45, 14), label, eyebrow);
        }

        private void DrawNavigation(float width, float height)
        {
            float y = height - 72; DrawRect(new Rect(0, y - 8, width, 80), Background);
            DrawRect(new Rect(0, y - 8, width, 1), new Color(Gold.r, Gold.g, Gold.b, .48f));
            float navWidth = Mathf.Min(760, width - 48) / 4f, start = (width - navWidth * 4) / 2;
            NavButton(new Rect(start, y, navWidth, 55), ScreenView.Home, "⌂", "HOME");
            NavButton(new Rect(start + navWidth, y, navWidth, 55), ScreenView.Characters, "◇", "CHARACTERS");
            NavButton(new Rect(start + navWidth * 2, y, navWidth, 55), ScreenView.Summon, "✦", "SUMMON");
            NavButton(new Rect(start + navWidth * 3, y, navWidth, 55), ScreenView.Ascension, "◈", "ASCENSION");
        }

        private void NavButton(Rect rect, ScreenView target, string icon, string label)
        {
            bool active = screen == target;
            if (ActionButton(rect, icon + "\n" + label, active ? navActive : nav)) screen = target;
            if (active) DrawRect(new Rect(rect.x + 30, rect.y + 53, rect.width - 60, 2), Cyan);
        }

        private void DrawHome(Rect area)
        {
            CharacterRuntimeState active = state.ActiveCharacter; CharacterData data = active.Definition;
            Rect stage = new Rect(area.x, area.y, area.width * .69f, area.height);
            Rect rail = new Rect(stage.xMax + 16, area.y, area.xMax - stage.xMax - 16, area.height);
            GUI.Box(stage, GUIContent.none, panelStyle); DrawFrame(stage, new Color(Cyan.r, Cyan.g, Cyan.b, .34f));
            DrawRect(new Rect(stage.x, stage.y, 3, stage.height), new Color(Cyan.r, Cyan.g, Cyan.b, .75f));
            GUI.Label(new Rect(stage.x + 25, stage.y + 18, 250, 18), "YOUR ACTIVE CHARACTER", eyebrow);
            GUI.Label(new Rect(stage.x + 24, stage.y + 42, stage.width - 48, 43), data.DisplayName.ToUpperInvariant(), display);
            Color rarity = RarityColor(data.Rarity), old = GUI.color; GUI.color = rarity;
            GUI.Label(new Rect(stage.x + 27, stage.y + 84, 220, 24), Stars(data.Rarity), heading); GUI.color = old;

            Rect artRect = new Rect(stage.x + 150, stage.y + 15, stage.width - 250, stage.height - 70);
            DrawCharacter(active, artRect, true);
            DrawGlow(artRect.center, 280, new Color(Cyan.r, Cyan.g, Cyan.b, characterPunch * .18f));
            Rect clickZone = new Rect(stage.x + 90, stage.y + 36, stage.width - 180, stage.height - 105);
            if (ActionButton(clickZone, GUIContent.none, GUIStyle.none))
            {
                state.EarnClick(); characterPunch = 1; Vector2 point = Event.current.mousePosition;
                clickEffects.Add(new ClickEffect { Position = point, Born = Time.realtimeSinceStartup, Text = "+" + Number(state.ClickIncome), Color = Cyan, Drift = UnityEngine.Random.Range(-18f, 18f) });
            }
            DrawClickEffects();

            DrawRect(new Rect(stage.x + 24, stage.yMax - 98, stage.width - 48, 74), Background);
            DrawFrame(new Rect(stage.x + 24, stage.yMax - 98, stage.width - 48, 74), new Color(Gold.r, Gold.g, Gold.b, .28f));
            GUI.Label(new Rect(stage.x + 42, stage.yMax - 90, 145, 20), "LV. " + active.Level.ToString("00"), title);
            GUI.Label(new Rect(stage.x + 42, stage.yMax - 66, 210, 18), "AFFINITY " + Roman(state.GetAffinityRank(active)), eyebrow);
            GUI.Label(new Rect(stage.x + 250, stage.yMax - 86, 210, 22), "+" + Number(state.ClickIncome) + " / CLICK", heading);
            GUI.Label(new Rect(stage.x + 250, stage.yMax - 60, 210, 22), "+" + Number(state.PassiveIncomePerSecond) + " TEAM / SEC", heading);
            string supportNames = state.SupportCharacters.Count == 0 ? "No Support Echoes assigned" : string.Join("  •  ", state.SupportCharacters.Select(item => item.Definition.DisplayName));
            GUI.Label(new Rect(stage.xMax - 255, stage.yMax - 91, 220, 18), "TAP • SUPPORT " + state.SupportCharacters.Count + " / " + LuminaRiftGameState.MaxSupportCharacters, eyebrow);
            DrawFittedLabel(new Rect(stage.xMax - 255, stage.yMax - 70, 220, 28), supportNames, small);

            GUI.Box(rail, GUIContent.none, panelStyle); DrawFrame(rail, new Color(Violet.r, Violet.g, Violet.b, .28f));
            GUI.Label(new Rect(rail.x + 24, rail.y + 18, rail.width - 48, 18), "ECHO TEAM • PROGRESSION", eyebrow);
            GUI.Label(new Rect(rail.x + 24, rail.y + 44, rail.width - 48, 34), "LEVEL " + active.Level + " / " + config.RunLevelCap, title);
            DrawStatLine(rail.x + 24, rail.y + 93, rail.width - 48, "TEAM PASSIVE / SEC", Number(state.PassiveIncomePerSecond));
            DrawStatLine(rail.x + 24, rail.y + 133, rail.width - 48, "SUPPORT / SEC", Number(state.SupportPassiveIncomePerSecond));
            DrawStatLine(rail.x + 24, rail.y + 173, rail.width - 48, "NEXT LEVEL", "◇ " + Number(state.NextLevelCost));
            GUI.Label(new Rect(rail.x + 24, rail.y + 224, rail.width - 48, 18), "LEVEL UP", eyebrow);
            int buyAmount = state.LevelTenUnlocked ? 10 : 1; GUI.enabled = InterfaceEnabled && state.CanLevel;
            Color previousColor = GUI.backgroundColor; GUI.backgroundColor = Color.Lerp(Color.white, new Color(1f, .94f, .8f), levelFlash * .25f);
            if (ActionButton(new Rect(rail.x + 24, rail.y + 250, rail.width - 48, 58), "LEVEL UP ×" + buyAmount + "\n◇ " + Number(CostPreview(buyAmount)), primaryButton))
            { if (state.BuyLevels(buyAmount) > 0) levelFlash = 1; }
            GUI.backgroundColor = previousColor; GUI.enabled = InterfaceEnabled;
            if (state.LevelMaxUnlocked && ActionButton(new Rect(rail.x + 24, rail.y + 316, (rail.width - 56) / 2, 38), "LEVEL MAX", secondaryButton)) state.BuyLevels(int.MaxValue);
            if (state.AutoLevelUnlocked && ActionButton(new Rect(rail.x + 32 + (rail.width - 56) / 2, rail.y + 316, (rail.width - 56) / 2, 38), state.AutoLevelEnabled ? "AUTO • ON" : "AUTO • OFF", secondaryButton)) state.SetAutoLevel(!state.AutoLevelEnabled);
            LevelMilestone next = config.Milestones.FirstOrDefault(item => item.level > active.Level);
            GUI.Label(new Rect(rail.x + 24, rail.yMax - 140, rail.width - 48, 18), "NEXT MILESTONE", eyebrow);
            GUI.Label(new Rect(rail.x + 24, rail.yMax - 115, rail.width - 48, 45), next == null ? "ALL MILESTONES REACHED" : "LEVEL " + next.level + "  •  ×" + next.incomeMultiplier.ToString("0.##") + " INCOME\n+" + next.standardTickets + " summon tickets", body);
            if (next != null)
            {
                DrawRect(new Rect(rail.x + 24, rail.yMax - 62, rail.width - 48, 4), Edge);
                DrawRect(new Rect(rail.x + 24, rail.yMax - 62, (rail.width - 48) * active.Level / next.level, 4), Cyan);
            }
            if (ActionButton(new Rect(rail.x + 24, rail.yMax - 49, rail.width - 48, 30), "CHANGE ACTIVE ECHO →", ghostButton)) screen = ScreenView.Characters;
            GUI.enabled = InterfaceEnabled;
        }

        private void DrawAscension(Rect area)
        {
            GUI.Label(new Rect(area.x + 8, area.y, 700, 36), "ASCENSION", title);
            GUI.Label(new Rect(area.x + 8, area.y + 40, area.width - 16, 24), "Reach LV. " + config.AscensionMinimumLevel + " to make an Echo ready. Every ready Echo adds to the reward.", body);
            Rect roster = new Rect(area.x, area.y + 82, area.width - 396, area.height - 82);
            Rect summary = new Rect(roster.xMax + 16, roster.y, 380, roster.height);
            GUI.Box(roster, GUIContent.none, panelStyle); DrawFrame(roster, Edge);
            GUI.Label(new Rect(roster.x + 24, roster.y + 16, roster.width - 48, 20), "COLLECTED ECHOES", eyebrow);
            GUI.Label(new Rect(roster.x + 24, roster.y + 36, roster.width - 48, 18), "Only collected Echoes are shown. Scroll to review everyone.", small);
            List<CharacterRuntimeState> owned = state.Roster.Where(character => character.IsOwned).ToList();
            const float rowHeight = 66;
            Rect ascensionViewport = new Rect(roster.x + 16, roster.y + 62, roster.width - 42, roster.height - 76);
            Rect ascensionContent = new Rect(0, 0, ascensionViewport.width, Mathf.Max(ascensionViewport.height, owned.Count * rowHeight));
            HandleScrollInput(ascensionViewport, ascensionContent.height, ref ascensionScroll, ascensionDrag);
            ascensionScroll = GUI.BeginScrollView(ascensionViewport, ascensionScroll, ascensionContent, false, false, GUIStyle.none, GUIStyle.none);
            for (int i = 0; i < owned.Count; i++)
            {
                CharacterRuntimeState character = owned[i];
                AscensionReward part = state.GetAscensionContribution(character);
                float y = i * rowHeight;
                DrawRect(new Rect(0, y, ascensionContent.width, rowHeight - 8), i % 2 == 0 ? Raised : Panel);
                GUI.Label(new Rect(14, y + 8, ascensionContent.width * .46f, 22), character.Definition.DisplayName, heading);
                GUI.Label(new Rect(14, y + 31, 90, 18), "LV. " + character.Level, eyebrow);
                string contribution = part.Power > 0 ? "+" + part.Lumina + " Lumina   •   +" + part.Power + " Power" : (config.AscensionMinimumLevel - character.Level) + " levels to ready";
                GUI.Label(new Rect(ascensionContent.width * .51f, y + 17, ascensionContent.width * .46f, 24), contribution, part.Power > 0 ? heading : small);
                if (part.Power == 0)
                {
                    float progress = Mathf.Clamp01(character.Level / (float)config.AscensionMinimumLevel);
                    DrawRect(new Rect(108, y + 38, ascensionContent.width * .32f, 3), Edge);
                    DrawRect(new Rect(108, y + 38, ascensionContent.width * .32f * progress, 3), Cyan);
                }
            }
            GUI.EndScrollView();
            DrawScrollIndicator(ascensionViewport, ascensionContent.height, ascensionScroll.y);
            AscensionReward total = state.ProjectedAscensionReward;
            GUI.Box(summary, GUIContent.none, panelStyle); DrawFrame(summary, Violet);
            GUI.Label(new Rect(summary.x + 24, summary.y + 18, 332, 20), "TOTAL ASCENSION REWARD", eyebrow);
            GUI.Label(new Rect(summary.x + 24, summary.y + 52, 332, 42), "+" + total.Lumina + " LUMINA", title);
            GUI.Label(new Rect(summary.x + 24, summary.y + 100, 332, 28), "+" + total.Power + " PERMANENT POWER", heading);
            GUI.Label(new Rect(summary.x + 24, summary.y + 140, 332, 24), "Income ×" + state.AscensionMultiplier.ToString("0.00") + " → ×" + (state.AscensionMultiplier + total.Power * config.PermanentIncomeBonusPerPower).ToString("0.00"), body);
            GUI.Label(new Rect(summary.x + 24, summary.y + 190, 332, 95), "RESET: Credits, every Echo's run level, and milestone claims.\n\nKEEP: Echoes, Affinity, tickets, Lumina, pity, and permanent progress.", body);
            GUI.Label(new Rect(summary.x + 24, summary.yMax - 110, 332, 28), state.CanAscend ? "Your collection is ready for a new run." : "Raise any owned Echo to LV. " + config.AscensionMinimumLevel + ".", small);
            GUI.enabled = InterfaceEnabled && state.CanAscend;
            if (ActionButton(new Rect(summary.x + 24, summary.yMax - 72, 332, 48), "REVIEW ASCENSION", primaryButton)) showAscensionConfirm = true;
            GUI.enabled = InterfaceEnabled;
        }

        private void DrawCharacters(Rect area)
        {
            GUI.Label(new Rect(area.x + 8, area.y, 420, 34), "CHARACTER COLLECTION", title);
            GUI.Label(new Rect(area.x + 8, area.y + 36, 500, 18), state.Roster.Count(item => item.IsOwned) + " / " + state.Roster.Count + " CHARACTERS COLLECTED", eyebrow);
            GUI.Label(new Rect(area.x + 430, area.y + 4, area.width - 438, 22), "Some Support bonuses boost Echoes that share a trait. Otherwise, traits can be ignored.", small);
            const float gap = 14, cardHeight = 205;
            float scrollTop = area.y + 64, scrollHeight = area.height - 64;
            Rect viewport = new Rect(area.x, scrollTop, area.width - 10, scrollHeight);
            float contentWidth = viewport.width, cardWidth = (contentWidth - gap * 2) / 3f;
            int rows = Mathf.CeilToInt(state.Roster.Count / 3f);
            Rect content = new Rect(0, 0, contentWidth, rows * (cardHeight + gap) - gap + 4);
            HandleScrollInput(viewport, content.height, ref collectionScroll, collectionDrag);
            collectionScroll = GUI.BeginScrollView(viewport, collectionScroll, content, false, false, GUIStyle.none, GUIStyle.none);
            for (int i = 0; i < state.Roster.Count; i++)
            {
                int col = i % 3, row = i / 3;
                DrawCharacterCard(new Rect(col * (cardWidth + gap), row * (cardHeight + gap), cardWidth, cardHeight), state.Roster[i]);
            }
            GUI.EndScrollView();
            DrawScrollIndicator(viewport, content.height, collectionScroll.y);
        }

        private void DrawCharacterCard(Rect card, CharacterRuntimeState character)
        {
            CharacterData data = character.Definition; Color rarity = RarityColor(data.Rarity);
            GUI.Box(card, GUIContent.none, cardStyle); DrawFrame(card, new Color(rarity.r, rarity.g, rarity.b, character == state.ActiveCharacter ? .48f : .24f));
            DrawRect(new Rect(card.x, card.y, 3, card.height), character == state.ActiveCharacter ? Cyan : new Color(rarity.r, rarity.g, rarity.b, .7f));
            Rect portrait = new Rect(card.x + 4, card.y + 4, card.width * .45f, card.height - 8);
            if (character.IsOwned) DrawCharacter(character, portrait, false);
            else { DrawGlow(portrait.center, portrait.height * .65f, new Color(rarity.r, rarity.g, rarity.b, .18f)); GUI.Label(new Rect(portrait.x, portrait.y + portrait.height * .3f, portrait.width, 60), data.DisplayName.Substring(0, 1), centeredDisplay); }
            Rect info = new Rect(card.x + card.width * .43f, card.y, card.width * .57f, card.height);
            GUI.Label(new Rect(info.x + 8, info.y + 14, info.width - 16, 22), Stars(data.Rarity), AccentLabel(13, FontStyle.Bold, TextAnchor.MiddleCenter, rarity));
            DrawFittedLabel(new Rect(info.x + 8, info.y + 42, info.width - 16, 28), data.DisplayName.ToUpperInvariant(), heading);
            if (character.IsOwned)
            {
                GUI.Label(new Rect(info.x + 8, info.y + 72, info.width - 16, 20), "LV. " + character.Level + "   •   AFFINITY " + Roman(state.GetAffinityRank(character)), small);
                DrawFittedLabel(new Rect(info.x + 8, info.y + 94, info.width - 16, 18), Tags(data), small);
                DrawFittedLabel(new Rect(info.x + 8, info.y + 114, info.width - 16, 18), SupportEffect(data), small);
                float progress = AffinityProgress(character.AffinityXp);
                DrawRect(new Rect(info.x + 8, info.y + 136, info.width - 25, 4), Edge);
                DrawRect(new Rect(info.x + 8, info.y + 136, (info.width - 25) * progress, 4), rarity);
                GUI.enabled = InterfaceEnabled && character != state.ActiveCharacter;
                float buttonWidth = (info.width - 30) / 2f;
                if (ActionButton(new Rect(info.x + 8, info.yMax - 46, buttonWidth, 30), character == state.ActiveCharacter ? "ACTIVE" : "SET ACTIVE", character == state.ActiveCharacter ? primaryButton : ghostButton)) state.SetActiveCharacter(character);
                bool isSupport = state.SupportCharacters.Contains(character);
                GUI.enabled = InterfaceEnabled && character != state.ActiveCharacter && (isSupport || state.SupportCharacters.Count < LuminaRiftGameState.MaxSupportCharacters);
                if (ActionButton(new Rect(info.x + 14 + buttonWidth, info.yMax - 46, buttonWidth, 30), isSupport ? "REMOVE" : "SUPPORT", isSupport ? secondaryButton : ghostButton)) state.ToggleSupportCharacter(character);
                GUI.enabled = InterfaceEnabled;
            }
            else
            {
                GUI.Label(new Rect(info.x + 8, info.y + 78, info.width - 20, 34), "NOT YET COLLECTED", eyebrow);
                GUI.Label(new Rect(info.x + 8, info.yMax - 55, info.width - 20, 34), "FIND IN SUMMON", eyebrow);
            }
        }

        private void DrawSummon(Rect area)
        {
            if (state.Banners.Count == 0) return;
            selectedBanner = Mathf.Clamp(selectedBanner, 0, state.Banners.Count - 1); float tabWidth = 260;
            for (int i = 0; i < state.Banners.Count; i++)
                if (ActionButton(new Rect(area.x + i * (tabWidth + 8), area.y, tabWidth, 34), state.Banners[i].DisplayName.ToUpperInvariant(), i == selectedBanner ? primaryButton : ghostButton)) selectedBanner = i;
            BannerData banner = state.Banners[selectedBanner]; Rect bannerRect = new Rect(area.x, area.y + 46, area.width, area.height - 46);
            Color accent = banner.Currency == BannerCurrency.Lumina ? Gold : Cyan;
            DrawTexture(bannerRect, summonGradient, Color.white);
            Rect artwork = new Rect(bannerRect.x + 506, bannerRect.y + 18, bannerRect.width - 524, bannerRect.height - 36);
            DrawGlow(artwork.center, 520, new Color(Violet.r, Violet.g, Violet.b, .2f));
            DrawPortal(artwork.center, 390, accent);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * 51f + Time.realtimeSinceStartup * 2f;
                Vector2 shard = artwork.center +
                    new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * (145 + i % 2 * 34);
                DrawDiamond(shard, 8 + i % 3 * 4, new Color(Violet.r, Violet.g, Violet.b, .34f));
            }
            DrawRect(new Rect(bannerRect.x + 18, bannerRect.y + 18, 470, bannerRect.height - 36), Background);
            DrawFrame(bannerRect, new Color(accent.r, accent.g, accent.b, .62f));
            DrawFrame(new Rect(bannerRect.x + 18, bannerRect.y + 18, 470, bannerRect.height - 36), new Color(Gold.r, Gold.g, Gold.b, .3f));
            CharacterRuntimeState featuredCharacter = banner.RateUpCharacter == null ? null : state.Roster.FirstOrDefault(item => item.Definition == banner.RateUpCharacter);
            if (featuredCharacter != null)
                DrawSummonCharacter(featuredCharacter, artwork);
            else if (banner.RateUpCharacter == null)
            {
                GUI.Label(new Rect(artwork.x, artwork.center.y - 34, artwork.width, 68), "◇\nMYSTERY CHARACTER", centeredTitle);
            }
            GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 32, 360, 18), banner.RateUpCharacter != null ? "FEATURED CHARACTER EVENT" : "STANDARD SUMMON", eyebrow);
            DrawFittedLabel(new Rect(bannerRect.x + 36, bannerRect.y + 58, 420, 45), banner.DisplayName.ToUpperInvariant(), title);
            if (banner.RateUpCharacter != null)
            {
                GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 112, 420, 34), banner.RateUpCharacter.DisplayName.ToUpperInvariant(), title);
                Color old = GUI.color; GUI.color = Gold; GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 148, 250, 24), Stars(banner.RateUpCharacter.Rarity), heading); GUI.color = old;
                GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 180, 390, 72), "WHEN YOU PULL A 5★ • " + Mathf.RoundToInt(banner.RateUpShareOfFiveStar * 100) + "% " + banner.RateUpCharacter.DisplayName.ToUpperInvariant() + "\n" + banner.Description, body);
            }
            else GUI.Label(new Rect(bannerRect.x + 38, bannerRect.y + 120, 390, 90), banner.Description + "\n\nEvery ten-pull guarantees a 4★ character or higher.", body);
            int pity = state.GetPity(banner);
            float rateTotal = Mathf.Max(.001f, banner.ThreeStarRate + banner.FourStarRate + banner.FiveStarRate);
            float rateY = banner.RateUpCharacter == null ? bannerRect.y + 245 : bannerRect.yMax - 190;
            GUI.Label(new Rect(bannerRect.x + 38, rateY, 410, 18),
                "BASE RATES   3★ " + (100 * banner.ThreeStarRate / rateTotal).ToString("0.#") + "%   •   4★ " + (100 * banner.FourStarRate / rateTotal).ToString("0.#") + "%   •   5★ " + (100 * banner.FiveStarRate / rateTotal).ToString("0.#") + "%", eyebrow);
            GUI.Label(new Rect(bannerRect.x + 38, rateY + 25, 390, 18), "5★ GUARANTEED WITHIN " + Math.Max(1, banner.HardPity - pity) + " SUMMONS", eyebrow);
            DrawRect(new Rect(bannerRect.x + 38, rateY + 50, 330, 6), Edge);
            DrawRect(new Rect(bannerRect.x + 38, rateY + 50, 330 * Mathf.Clamp01((float)pity / banner.HardPity), 6), accent);
            GUI.Label(new Rect(bannerRect.x + 376, rateY + 40, 82, 22), pity + " / " + banner.HardPity, small);
            string symbol = banner.Currency == BannerCurrency.Lumina ? "✦" : "▱";
            GUI.Label(new Rect(bannerRect.x + 38, bannerRect.yMax - 112, 330, 22), "AVAILABLE   " + symbol + " " + state.CurrencyFor(banner), heading);
            GUI.enabled = InterfaceEnabled && state.CurrencyFor(banner) >= banner.SinglePullCost;
            if (ActionButton(new Rect(bannerRect.x + 38, bannerRect.yMax - 76, 190, 48), "SUMMON ×1\n" + symbol + " " + banner.SinglePullCost, secondaryButton)) BeginSummon(banner, 1);
            GUI.enabled = InterfaceEnabled && state.CurrencyFor(banner) >= banner.TenPullCost;
            if (ActionButton(new Rect(bannerRect.x + 240, bannerRect.yMax - 76, 220, 48), "SUMMON ×10\n" + symbol + " " + banner.TenPullCost, primaryButton)) BeginSummon(banner, 10);
            GUI.enabled = InterfaceEnabled;
        }

        private void DrawStats(Rect area)
        {
            TelemetrySaveData t = state.Telemetry; GUI.Box(area, GUIContent.none, panelStyle); DrawFrame(area, new Color(Cyan.r, Cyan.g, Cyan.b, .24f));
            GUI.Label(new Rect(area.x + 34, area.y + 25, area.width - 68, 38), "RIFT RECORD", title);
            GUI.Label(new Rect(area.x + 34, area.y + 63, area.width - 68, 18), "YOUR LOCAL ADVENTURE STATS • NOTHING LEAVES THIS DEVICE", eyebrow);
            string[] labels = { "CURRENT RUN", "LIFETIME SIGNAL", "LAST ASCENSION", "HIGHEST LEVEL", "TOTAL PULLS", "FIVE-STAR PULLS", "TOTAL CLICKS", "LIFETIME CREDITS" };
            string[] values = { Duration(t.currentRunSeconds), Duration(t.lifetimePlaySeconds), Duration(t.lastAscensionRunSeconds), t.highestLevelReached.ToString(), t.totalPulls.ToString(), t.fiveStarPulls.ToString(), t.totalClicks.ToString(), Number(t.lifetimeCreditsEarned) };
            float cellW = (area.width - 100) / 4;
            for (int i = 0; i < labels.Length; i++)
            {
                int col = i % 4, row = i / 4; Rect cell = new Rect(area.x + 38 + col * (cellW + 8), area.y + 115 + row * 130, cellW, 110);
                DrawRect(cell, Raised); DrawFrame(cell, new Color(Gold.r, Gold.g, Gold.b, .2f));
                GUI.Label(new Rect(cell.x + 12, cell.y + 16, cell.width - 24, 18), labels[i], eyebrow);
                GUI.Label(new Rect(cell.x + 12, cell.y + 45, cell.width - 24, 38), values[i], title);
            }
            if (ActionButton(new Rect(area.xMax - 225, area.yMax - 54, 190, 34), "DEVELOPER • RESET SAVE", dangerButton)) showResetConfirm = true;
        }

        private void DrawCharacter(CharacterRuntimeState character, Rect rect, bool large)
        {
            Sprite artwork = large ? character.Definition.HomeArtwork : character.Definition.Portrait;
            Color accent = character.Definition.AccentColor;
            if (artwork != null)
            {
                float artworkPortalSize = Mathf.Min(rect.width * .82f, rect.height * .68f);
                DrawGlow(rect.center, artworkPortalSize * 1.65f, new Color(accent.r, accent.g, accent.b, large ? .2f : .12f));
                DrawPortal(rect.center, artworkPortalSize, accent);
                DrawSprite(rect, artwork, large ? 1f : .96f);
                return;
            }
            // A deliberate Echo sigil keeps unillustrated characters legible at every size.
            float size = Mathf.Min(rect.width * .8f, rect.height * .65f);
            Vector2 center = rect.center;
            DrawGlow(center, size * 1.65f, new Color(accent.r, accent.g, accent.b, .25f));
            DrawPortal(center, size, accent);
            GUI.Label(new Rect(center.x - size / 2, center.y - size * .22f, size, size * .44f),
                character.Definition.DisplayName.Substring(0, 1), AccentLabel(large ? 48 : 28, FontStyle.Bold, TextAnchor.MiddleCenter, TextPrimary));

        }

        private void DrawSummonCharacter(CharacterRuntimeState character, Rect rect)
        {
            Sprite artwork = character.Definition.SummonArtwork;
            if (artwork != null)
            {
                Color accent = character.Definition.AccentColor;
                DrawGlow(rect.center, rect.height * .8f, new Color(accent.r, accent.g, accent.b, .2f));
                DrawSprite(rect, artwork, 1f);
            }
            else DrawCharacter(character, rect, true);
        }

        private void DrawClickEffects()
        {
            foreach (ClickEffect effect in clickEffects)
            {
                float age = Time.realtimeSinceStartup - effect.Born, alpha = 1 - Mathf.Clamp01(age / 1.05f);
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
            DrawModalShade(width, height);
            if (showSummonSummary) { DrawSummonSummary(width, height); return; }
            GachaResult result = revealResults[revealIndex];
            CharacterData data = result.Character.Definition;
            Color rarity = RarityColor(data.Rarity);
            float elapsed = RevealDuration(result) - revealTimer;
            float charge = Mathf.Clamp01(elapsed / (RevealDuration(result) * .6f));
            Vector2 center = new Vector2(width / 2, height / 2 - 20);
            Color light = Color.Lerp(Cyan, rarity, EaseOut(charge));
            DrawGlow(center, Mathf.Lerp(180, 760, EaseOut(charge)), new Color(light.r, light.g, light.b, .5f));
            DrawPortal(center, Mathf.Lerp(160, 550, EaseOut(charge)), light);
            if (charge < 1)
            {
                DrawDiamond(center, Mathf.Lerp(28, 62, charge), new Color(light.r, light.g, light.b, .85f));
                GUI.Label(new Rect(center.x - 250, center.y + 145, 500, 36), "THE RIFT IS RESONATING", centeredTitle);
                GUI.Label(new Rect(center.x - 200, center.y + 185, 400, 24), "An Echo is answering your call", small);
            }
            else
            {
                float burst = Mathf.Clamp01((elapsed - RevealDuration(result) * .6f) / .85f);
                int rays = data.Rarity == CharacterRarity.FiveStar ? 24 : 12;
                for (int i = 0; i < rays; i++)
                {
                    float angle = i * Mathf.PI * 2 / rays;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    DrawLine(center + direction * (240 + burst * 120), center + direction * (280 + burst * 230),
                        new Color(rarity.r, rarity.g, rarity.b, (1 - burst) * .7f), 2);
                }
                Rect card = ModalBox(width, height, 760, 490);
                GUI.Box(card, GUIContent.none, panelStyle);
                DrawFrame(card, rarity);
                DrawRect(new Rect(card.x, card.y, card.width, 3), rarity);
                DrawGlow(new Vector2(card.x + 180, card.center.y), 400, new Color(rarity.r, rarity.g, rarity.b, .22f));
                DrawSummonCharacter(result.Character, new Rect(card.x + 20, card.y + 32, 320, 400));
                Rect info = new Rect(card.x + 370, card.y + 46, 350, 380);
                GUI.Label(new Rect(info.x, info.y, info.width, 22), result.WasDuplicate ? "ECHO REUNITED" : "NEW ECHO DISCOVERED", eyebrow);
                GUI.Label(new Rect(info.x, info.y + 40, info.width, 30), Stars(data.Rarity), AccentLabel(22, FontStyle.Bold, TextAnchor.MiddleLeft, rarity));
                DrawFittedLabel(new Rect(info.x, info.y + 87, info.width, 48), data.DisplayName.ToUpperInvariant(), display);
                GUI.Label(new Rect(info.x, info.y + 154, info.width, 95), data.Description, body);
                DrawRect(new Rect(info.x, info.y + 265, info.width, 64), new Color(rarity.r, rarity.g, rarity.b, .1f));
                GUI.Label(new Rect(info.x + 12, info.y + 273, info.width - 24, 48), result.WasDuplicate ? "+" + result.AffinityAwarded + " AFFINITY XP\nYour bond grows stronger" : "ADDED TO YOUR COLLECTION\nReady to join your adventure", centered);
                GUI.Label(new Rect(card.x + 28, card.yMax - 40, 280, 24), "ECHO " + (revealIndex + 1) + " OF " + revealResults.Count, small);
                if (ActionButton(new Rect(info.x, card.yMax - 62, info.width, 42), revealIndex + 1 == revealResults.Count ? "VIEW RESULTS" : "REVEAL NEXT", primaryButton))
                    AdvanceReveal();
            }
            if (ActionButton(new Rect(width - 204, height - 66, 180, 40), "SKIP TO RESULTS", ghostButton)) showSummonSummary = true;
        }

        private void AdvanceReveal()
        {
            if (revealIndex + 1 >= revealResults.Count) { showSummonSummary = true; return; }
            revealIndex++;
            revealTimer = RevealDuration(revealResults[revealIndex]);
        }

        private void DrawSummonSummary(float width, float height)
        {
            bool single = revealResults.Count == 1;
            Rect box = ModalBox(width, height, single ? 580 : 1080, single ? 390 : 590);
            GUI.Box(box, GUIContent.none, panelStyle); DrawFrame(box, Gold);
            GUI.Label(new Rect(box.x + 30, box.y + 20, box.width - 60, 40), "ECHOES OF THE RIFT", centeredTitle);
            int discoveries = revealResults.Count(item => !item.WasDuplicate);
            GUI.Label(new Rect(box.x + 30, box.y + 62, box.width - 60, 24), revealResults.Count + " SUMMONED  •  " + discoveries + " NEW  •  " + (revealResults.Count - discoveries) + " REUNITED", small);
            const float cardWidth = 192, cardHeight = 184, gap = 14;
            int columns = Math.Min(5, revealResults.Count);
            float startX = box.center.x - (columns * cardWidth + (columns - 1) * gap) / 2;
            for (int i = 0; i < revealResults.Count; i++)
            {
                GachaResult result = revealResults[i]; CharacterData data = result.Character.Definition;
                Color rarity = RarityColor(data.Rarity);
                Rect card = new Rect(startX + i % 5 * (cardWidth + gap), box.y + 103 + i / 5 * (cardHeight + gap), cardWidth, cardHeight);
                GUI.Box(card, GUIContent.none, cardStyle); DrawFrame(card, rarity);
                DrawRect(new Rect(card.x, card.y, card.width, 3), rarity);
                DrawCharacter(result.Character, new Rect(card.x + 46, card.y + 17, 100, 88), false);
                GUI.Label(new Rect(card.x + 10, card.y + 8, card.width - 20, 18), result.WasDuplicate ? "REUNITED" : "NEW", eyebrow);
                GUI.Label(new Rect(card.x + 12, card.y + 99, card.width - 24, 22), Stars(data.Rarity), AccentLabel(15, FontStyle.Bold, TextAnchor.MiddleCenter, rarity));
                DrawFittedLabel(new Rect(card.x + 12, card.y + 126, card.width - 24, 25), data.DisplayName, centeredTitle);
                GUI.Label(new Rect(card.x + 10, card.y + 155, card.width - 20, 20), result.WasDuplicate ? "+" + result.AffinityAwarded + " Affinity XP" : "Added to collection", small);
            }
            if (ActionButton(new Rect(box.center.x - 150, box.yMax - 60, 300, 40), "CONTINUE", primaryButton))
            { revealResults = null; showSummonSummary = false; }
        }

        private void DrawOfflineOverlay(float width, float height)
        {
            DrawModalShade(width, height);
            Rect box = ModalBox(width, height, 580, 410);
            GUI.Box(box, GUIContent.none, panelStyle); DrawFrame(box, Cyan);
            DrawRect(new Rect(box.x, box.y, box.width, 3), Cyan);
            GUI.Label(new Rect(box.x + 40, box.y + 28, box.width - 80, 20), "YOUR ADVENTURE CONTINUED", small);
            GUI.Label(new Rect(box.x + 40, box.y + 57, box.width - 80, 42), "WELCOME BACK", centeredTitle);
            GUI.Label(new Rect(box.x + 40, box.y + 108, box.width - 80, 36), "Your Echo team earned Credits for " + Duration(pendingOfflineSeconds), centered);
            Rect reward = new Rect(box.x + 48, box.y + 164, box.width - 96, 104);
            DrawRect(reward, new Color(Cyan.r, Cyan.g, Cyan.b, .09f));
            GUI.Label(new Rect(reward.x + 16, reward.y + 15, reward.width - 32, 46), "◇ " + Number(pendingOfflineCredits), centeredDisplay);
            GUI.Label(new Rect(reward.x, reward.y + 66, reward.width, 20), "CREDITS READY TO COLLECT", small);
            GUI.Label(new Rect(box.x + 40, box.y + 282, box.width - 80, 22), Mathf.RoundToInt(config.OfflineEarningsRate * 100) + "% of normal team income • up to " + config.OfflineEarningsCapHours + " hours", small);
            if (ActionButton(new Rect(box.x + 90, box.yMax - 70, box.width - 180, 46), "COLLECT " + Number(pendingOfflineCredits) + " CREDITS", primaryButton))
            {
                double collected = pendingOfflineCredits;
                state.CollectOfflineCredits(collected); pendingOfflineCredits = 0; pendingOfflineSeconds = 0;
                showOffline = false; message = Number(collected) + " offline Credits collected. Welcome home."; SaveNow();
            }
        }
        private void DrawAscensionOverlay(float width, float height)
        {
            DrawModalShade(width, height); Rect box = ModalBox(width, height, 620, 365); GUI.Box(box, GUIContent.none, panelStyle);
            AscensionReward reward = state.ProjectedAscensionReward;
            DrawFrame(box, new Color(Violet.r, Violet.g, Violet.b, .45f));
            GUI.Label(new Rect(box.x + 35, box.y + 30, box.width - 70, 42), "READY TO ASCEND?", centeredTitle);
            GUI.Label(new Rect(box.x + 55, box.y + 90, box.width - 110, 145), "<size=27><color=#FFD66F>+" + reward.Lumina + " ✦</color>    <color=#82E8FF>+" + reward.Power + " ◈</color></size>\n\nRewards include all eligible Echoes.\nCredits, every Echo’s run level, and milestone claims reset.\nYour collection, Affinity, currencies, pity, and records stay with you.", centered);
            if (ActionButton(new Rect(box.x + 55, box.yMax - 70, 235, 42), "CANCEL", ghostButton)) showAscensionConfirm = false;
            if (ActionButton(new Rect(box.xMax - 290, box.yMax - 70, 235, 42), "ASCEND", primaryButton)) { showAscensionConfirm = false; state.TryAscend(); characterPunch = 1; SaveNow(); }
        }

        private void DrawResetOverlay(float width, float height)
        {
            DrawModalShade(width, height); Rect box = ModalBox(width, height, 560, 285); GUI.Box(box, GUIContent.none, panelStyle);
            DrawFrame(box, new Color(1f, .28f, .38f, .4f));
            GUI.Label(new Rect(box.x + 35, box.y + 30, box.width - 70, 42), "RESET LOCAL SAVE?", centeredTitle);
            GUI.Label(new Rect(box.x + 48, box.y + 90, box.width - 96, 70), "This clears the current run, archive, pity, Rift Rank, and local record. This cannot be undone.", centered);
            if (ActionButton(new Rect(box.x + 45, box.yMax - 66, 220, 40), "CANCEL", ghostButton)) showResetConfirm = false;
            if (ActionButton(new Rect(box.xMax - 265, box.yMax - 66, 220, 40), "RESET SAVE", dangerButton))
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
        { List<GachaResult> results; if (!state.TrySummon(banner, count, out results)) return; revealResults = results; showSummonSummary = false; revealIndex = 0; revealTimer = RevealDuration(results[0]); SaveNow(); }

        private void DrawPortal(Vector2 center, float size, Color color)
        {
            float pulse = .34f + Mathf.Sin(Time.realtimeSinceStartup * 1.4f) * .06f;
            DrawTexture(new Rect(center.x - size / 2, center.y - size / 2, size, size), ring, new Color(color.r, color.g, color.b, pulse));
            DrawTexture(new Rect(center.x - size * .36f, center.y - size * .36f, size * .72f, size * .72f), softCircle, new Color(.42f, .28f, .82f, .22f));
            for (int i = 0; i < 7; i++)
            {
                float angle = Time.realtimeSinceStartup * (12 + i) + i * 51.4f;
                Vector2 p = center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * size * .43f;
                DrawTexture(new Rect(p.x - 3, p.y - 3, 6, 6), softCircle, new Color(color.r, color.g, color.b, .7f));
            }
        }

        private void DrawGlow(Vector2 center, float size, Color color) { DrawTexture(new Rect(center.x - size / 2, center.y - size / 2, size, size), softCircle, color); }
        private static void DrawSprite(Rect rect, Sprite sprite, float alpha)
        {
            if (sprite == null || sprite.texture == null) return;
            Rect source = sprite.textureRect;
            float aspect = source.width / Mathf.Max(1f, source.height);
            Rect fitted = rect;
            if (rect.width / rect.height > aspect)
            {
                fitted.width = rect.height * aspect;
                fitted.x = rect.center.x - fitted.width / 2;
            }
            else
            {
                fitted.height = rect.width / aspect;
                fitted.y = rect.center.y - fitted.height / 2;
            }
            Rect uv = new Rect(source.x / sprite.texture.width, source.y / sprite.texture.height, source.width / sprite.texture.width, source.height / sprite.texture.height);
            Color old = GUI.color; GUI.color = new Color(1, 1, 1, alpha);
            GUI.DrawTextureWithTexCoords(fitted, sprite.texture, uv, true);
            GUI.color = old;
        }

        private void DrawDiamond(Vector2 center, float size, Color color)
        {
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = oldMatrix * Matrix4x4.TRS(center, Quaternion.Euler(0, 0, 45), Vector3.one);
            DrawRect(new Rect(-size / 2, -size / 2, size, size), color);
            GUI.matrix = oldMatrix;
        }
        private static Rect ScaleAround(Rect rect, float scale) { return new Rect(rect.center.x - rect.width * scale / 2, rect.center.y - rect.height * scale / 2, rect.width * scale, rect.height * scale); }
        private static Rect ModalBox(float width, float height, float w, float h) { return new Rect(width / 2 - w / 2, height / 2 - h / 2, w, h); }
        private static float EaseOut(float value) { value = Mathf.Clamp01(value); return 1 - (1 - value) * (1 - value); }
        private static float RevealDuration(GachaResult result) { return result.Character.Definition.Rarity == CharacterRarity.FiveStar ? 2.6f : result.Character.Definition.Rarity == CharacterRarity.FourStar ? 1.9f : 1.45f; }
        private static string Stars(CharacterRarity rarity) { return new string('★', (int)rarity); }
        private static string Roman(int rank) { return rank == 3 ? "III" : rank == 2 ? "II" : "I"; }
        private static string Tags(CharacterData data) { return data.Tags.Count == 0 ? "No traits" : string.Join("  •  ", data.Tags.Select(tag => tag.ToString())); }
        private static string SupportEffect(CharacterData data)
        {
            string amount = Mathf.RoundToInt(data.SupportEffectValue * 100) + "%";
            if (data.SupportEffect == SupportEffectType.ClickIncome) return "As Support: clicks earn " + amount + " more";
            if (data.SupportEffect == SupportEffectType.OfflineIncome) return "As Support: away earnings +" + amount;
            if (data.SupportEffect == SupportEffectType.TagIncome) return "As Support: " + data.SupportEffectTag + " Echoes earn +" + amount;
            return "As Support: team passive +" + amount;
        }
        private static Color RarityColor(CharacterRarity rarity) { return rarity == CharacterRarity.FiveStar ? Gold : rarity == CharacterRarity.FourStar ? Violet : Cyan; }
        private static string Number(double value) { if (value >= 1e15) return (value / 1e15).ToString("0.##") + "Qa"; if (value >= 1e12) return (value / 1e12).ToString("0.##") + "T"; if (value >= 1e9) return (value / 1e9).ToString("0.##") + "B"; if (value >= 1e6) return (value / 1e6).ToString("0.##") + "M"; if (value >= 1e3) return (value / 1e3).ToString("0.##") + "K"; return value.ToString(value < 100 ? "0.0" : "0"); }
        private static string Duration(double seconds) { TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, seconds)); return time.TotalHours >= 1 ? ((int)time.TotalHours) + "h " + time.Minutes + "m" : time.Minutes > 0 ? time.Minutes + "m " + time.Seconds + "s" : time.Seconds + "s"; }
        private static Color Hex(string hex) { Color value; ColorUtility.TryParseHtmlString("#" + hex, out value); return value; }

        private void DrawRect(Rect rect, Color color) { DrawTexture(rect, pixel, color); }
        private void HandleScrollInput(Rect viewport, float contentHeight, ref Vector2 scroll, ScrollDragState drag)
        {
            float maxScroll = Mathf.Max(0, contentHeight - viewport.height);
            scroll.y = Mathf.Clamp(scroll.y, 0, maxScroll);
            if (maxScroll <= 0) return;

            float thumbHeight = Mathf.Max(34, viewport.height * viewport.height / contentHeight);
            float travel = viewport.height - thumbHeight;
            float thumbY = viewport.y + travel * (scroll.y / maxScroll);
            Rect barHitArea = new Rect(viewport.xMax, viewport.y, 10, viewport.height);
            Rect thumbHitArea = new Rect(viewport.xMax, thumbY, 10, thumbHeight);
            Event current = Event.current;

            if (current.type == EventType.ScrollWheel && viewport.Contains(current.mousePosition))
            {
                scroll.y = Mathf.Clamp(scroll.y + current.delta.y * 34f, 0, maxScroll);
                current.Use();
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && thumbHitArea.Contains(current.mousePosition))
            {
                drag.DraggingBar = true; drag.PendingContent = false;
                drag.PointerStart = current.mousePosition; drag.ScrollStart = scroll; current.Use();
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && barHitArea.Contains(current.mousePosition))
            {
                scroll.y = Mathf.Clamp((current.mousePosition.y - viewport.y - thumbHeight * .5f) / travel * maxScroll, 0, maxScroll);
                drag.DraggingBar = true; drag.PendingContent = false;
                drag.PointerStart = current.mousePosition; drag.ScrollStart = scroll; current.Use();
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && viewport.Contains(current.mousePosition))
            {
                drag.PendingContent = true; drag.PointerStart = current.mousePosition; drag.ScrollStart = scroll;
            }
            else if (current.type == EventType.MouseDrag && current.button == 0 && drag.DraggingBar)
            {
                scroll.y = Mathf.Clamp(drag.ScrollStart.y + (current.mousePosition.y - drag.PointerStart.y) / travel * maxScroll, 0, maxScroll);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && current.button == 0 && (drag.PendingContent || drag.DraggingContent))
            {
                if (drag.DraggingContent || Vector2.Distance(current.mousePosition, drag.PointerStart) >= 5f)
                {
                    drag.DraggingContent = true; drag.PendingContent = false;
                    GUIUtility.hotControl = 0;
                    scroll.y = Mathf.Clamp(drag.ScrollStart.y - (current.mousePosition.y - drag.PointerStart.y), 0, maxScroll);
                    current.Use();
                }
            }
            else if (current.rawType == EventType.MouseUp && current.button == 0)
            {
                if (drag.DraggingBar || drag.DraggingContent) { GUIUtility.hotControl = 0; current.Use(); }
                drag.PendingContent = false; drag.DraggingContent = false; drag.DraggingBar = false;
            }
        }
        private void DrawScrollIndicator(Rect viewport, float contentHeight, float scrollY)
        {
            if (contentHeight <= viewport.height + 1) return;
            Rect track = new Rect(viewport.xMax + 3, viewport.y, 3, viewport.height);
            float thumbHeight = Mathf.Max(34, viewport.height * viewport.height / contentHeight);
            float travel = viewport.height - thumbHeight;
            float maxScroll = contentHeight - viewport.height;
            float thumbY = viewport.y + travel * Mathf.Clamp01(scrollY / maxScroll);
            DrawRect(track, new Color(Edge.r, Edge.g, Edge.b, .45f));
            DrawRect(new Rect(track.x, thumbY, track.width, thumbHeight), Cyan);
        }
        private static void DrawTexture(Rect rect, Texture texture, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, texture); GUI.color = old; }
        private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Matrix4x4 oldMatrix = GUI.matrix; Color old = GUI.color; GUI.color = color;
            float angle = Vector3.Angle(b - a, Vector2.right); if (a.y > b.y) angle = -angle;
            GUI.matrix = oldMatrix * Matrix4x4.TRS(a, Quaternion.Euler(0, 0, angle), Vector3.one);
            GUI.DrawTexture(new Rect(0, 0, (b - a).magnitude, width), pixel);
            GUI.matrix = oldMatrix; GUI.color = old;
        }
        private void DrawModalShade(float width, float height) { DrawRect(new Rect(0, 0, width, height), new Color(.02f, .04f, .08f, .84f)); }

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
            disabledSurface = MakeSolid(PressedSurface);
            pixel = MakeTexture(2, (x, y) => Color.white);
            softCircle = MakeTexture(128, (x, y) => { float distance = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 63.5f; return new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - distance), 2)); });
            ring = MakeTexture(192, (x, y) => { float d = Vector2.Distance(new Vector2(x, y), new Vector2(95.5f, 95.5f)) / 95.5f; float alpha = Mathf.Clamp01(1 - Mathf.Abs(d - .82f) * 35) * .9f + Mathf.Clamp01(1 - Mathf.Abs(d - .65f) * 80) * .35f; return new Color(1, 1, 1, alpha); });
            skyGradient = MakeTexture(256, (x, y) => Color.Lerp(Hex("0B1423"), Hex("162840"), y / 255f));
            summonGradient = MakeTexture(256, (x, y) =>
            {
                float horizontal = x / 255f;
                Color baseColor = Color.Lerp(Hex("142236"), Hex("2B3554"), horizontal);
                return Color.Lerp(baseColor, Hex("49365A"), Mathf.Clamp01((horizontal - .48f) * .75f));
            });
            display = Label(31, FontStyle.Bold, TextAnchor.MiddleLeft, TextPrimary); title = Label(24, FontStyle.Bold, TextAnchor.MiddleLeft, TextPrimary);
            centeredDisplay = new GUIStyle(display) { alignment = TextAnchor.MiddleCenter };
            centeredTitle = new GUIStyle(title) { alignment = TextAnchor.MiddleCenter };
            eyebrow = Label(12, FontStyle.Bold, TextAnchor.MiddleLeft, Muted); heading = Label(16, FontStyle.Bold, TextAnchor.MiddleLeft, TextPrimary);
            body = Label(15, FontStyle.Normal, TextAnchor.UpperLeft, TextPrimary); body.wordWrap = true;
            centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, richText = true };
            small = Label(13, FontStyle.Normal, TextAnchor.MiddleCenter, Muted); currency = Label(16, FontStyle.Bold, TextAnchor.MiddleLeft, TextPrimary);
            panelStyle = Surface(Panel);
            cardStyle = Surface(Raised);
            primaryButton = Button(14, Background, MakeSolid(Gold), MakeSolid(Hex("F5D49C")), MakeSolid(Hex("CCA062")));
            secondaryButton = Button(13, TextPrimary, MakeSolid(Hex("304962")), MakeSolid(Hex("3B5875")), MakeSolid(Hex("263D55")));
            ghostButton = Button(12, TextPrimary, MakeSolid(Panel), MakeSolid(Raised), MakeSolid(PressedSurface));
            dangerButton = Button(12, Hex("FFD4D8"), MakeSolid(Hex("502E40")), MakeSolid(Hex("66384A")), MakeSolid(Hex("402333")));
            nav = Button(12, Muted, MakeSolid(Background), MakeSolid(Panel), MakeSolid(Raised));
            navActive = Button(12, Cyan, MakeSolid(Panel), MakeSolid(Raised), MakeSolid(PressedSurface));
        }

        private bool ActionButton(Rect rect, string text, GUIStyle style) { return ActionButton(rect, new GUIContent(text), style); }
        private bool ActionButton(Rect rect, GUIContent content, GUIStyle style)
        {
            if (GUI.enabled) return GUI.Button(rect, content, style);
            // Keep disabled actions readable without accepting input or hover changes.
            GUI.Button(rect, GUIContent.none, GUIStyle.none);
            if (style == GUIStyle.none) return false;
            if (!disabledStyles.TryGetValue(style, out GUIStyle disabled))
            {
                disabled = new GUIStyle(style); SetPassiveStates(disabled, Muted, disabledSurface);
                disabledStyles.Add(style, disabled);
            }
            GUI.enabled = true;
            GUI.Box(rect, content, disabled);
            GUI.enabled = false;
            return false;
        }

        private GUIStyle AccentLabel(int size, FontStyle style, TextAnchor alignment, Color color)
        {
            var key = (size, style, alignment, color);
            if (!accentLabels.TryGetValue(key, out GUIStyle result))
            { result = Label(size, style, alignment, color); accentLabels.Add(key, result); }
            return result;
        }

        private static GUIStyle Label(int size, FontStyle style, TextAnchor alignment, Color color)
        {
            // Start clean: the host skin's hover/focus states must never restyle passive text.
            GUIStyle result = new GUIStyle { font = GUI.skin.font, fontSize = size, fontStyle = style,
                alignment = alignment, richText = true, wordWrap = false, clipping = TextClipping.Clip };
            SetPassiveStates(result, color, null);
            return result;
        }

        private static void SetPassiveStates(GUIStyle style, Color color, Texture2D background)
        {
            foreach (GUIStyleState state in new[] { style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused })
            { state.textColor = color; state.background = background; }
        }

        private static GUIStyle Surface(Color color)
        {
            GUIStyle result = new GUIStyle { border = new RectOffset(1, 1, 1, 1) };
            SetPassiveStates(result, TextPrimary, MakeSolid(color));
            return result;
        }

        private void DrawFittedLabel(Rect rect, string text, GUIStyle style)
        {
            var key = (style, text, Mathf.FloorToInt(rect.width));
            GUIContent content = new GUIContent(text);
            if (!fittedLabels.TryGetValue(key, out GUIStyle fitted))
            {
                fitted = new GUIStyle(style) { wordWrap = false };
                while (fitted.fontSize > 10 && fitted.CalcSize(content).x > rect.width) fitted.fontSize--;
                if (fittedLabels.Count > 256) fittedLabels.Clear();
                fittedLabels.Add(key, fitted);
            }
            GUI.Label(rect, content, fitted);
        }

        private static GUIStyle Button(int size, Color color, Texture2D normal, Texture2D hover, Texture2D active)
        {
            return new GUIStyle { font = GUI.skin.font, fontSize = size, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = false, clipping = TextClipping.Clip,
                normal = { textColor = color, background = normal }, hover = { textColor = color, background = hover }, active = { textColor = color, background = active },
                focused = { textColor = color, background = hover },
                onNormal = { textColor = color, background = normal }, onHover = { textColor = color, background = hover },
                onActive = { textColor = color, background = active }, onFocused = { textColor = color, background = hover },
                border = new RectOffset(2, 2, 2, 2), padding = new RectOffset(8, 8, 5, 5) };
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
