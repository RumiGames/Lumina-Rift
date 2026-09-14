using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LuminaRift
{
    public sealed class LuminaRiftPrototypeUI : MonoBehaviour
    {
        private sealed class BannerView
        {
            public BannerData Banner;
            public Text Currency;
            public Text Pity;
            public Button SingleButton;
            public Button TenButton;
        }

        private static readonly Color Background = new Color(0.025f, 0.032f, 0.07f);
        private static readonly Color Panel = new Color(0.075f, 0.09f, 0.16f, 0.98f);
        private static readonly Color Soft = new Color(0.13f, 0.15f, 0.25f, 0.98f);
        private static readonly Color Blue = new Color(0.25f, 0.8f, 1f);
        private static readonly Color Gold = new Color(1f, 0.72f, 0.25f);

        private PrototypeGameConfig config;
        private LuminaRiftGameState state;
        private Font font;
        private Text creditsText;
        private Text currencyText;
        private Text messageText;
        private RectTransform mainScreen;
        private RectTransform rosterScreen;
        private RectTransform summonScreen;
        private Text mainCharacterName;
        private Text mainCharacterStats;
        private Image mainPortrait;
        private Button levelButton;
        private Text levelButtonText;
        private Button ascendButton;
        private Text ascendButtonText;
        private readonly List<RectTransform> rosterCards = new List<RectTransform>();
        private readonly List<BannerView> bannerViews = new List<BannerView>();
        private RectTransform revealOverlay;
        private Image revealGlow;
        private Text revealRarity;
        private Text revealName;
        private Text revealStatus;
        private RectTransform ascensionOverlay;
        private Text ascensionConfirmText;
        private bool presentingSummons;
        private float passiveTickAccumulator;

        public void Initialize(PrototypeGameConfig gameConfig)
        {
            config = gameConfig;
            state = new LuminaRiftGameState(config);
            state.Changed += Refresh;
            state.MessageRaised += ShowMessage;
            BuildInterface();
            ShowScreen(mainScreen);
            ShowMessage("Prototype 0.0.2 ready. Build the run, collect Echoes, and push beyond level 50.");
            Refresh();
        }

        private void OnDestroy()
        {
            if (state == null) return;
            state.Changed -= Refresh;
            state.MessageRaised -= ShowMessage;
        }

        private void Update()
        {
            if (state == null) return;
            passiveTickAccumulator += Time.unscaledDeltaTime;
            if (passiveTickAccumulator < 0.1f) return;
            state.TickPassive(passiveTickAccumulator);
            passiveTickAccumulator = 0f;
        }

        private void BuildInterface()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            Image background = CreateImage("Background", transform, Background);
            Stretch(background.rectTransform, 0, 0, 0, 0);
            BuildHeader();
            BuildNavigation();
            BuildMainScreen();
            BuildRosterScreen();
            BuildSummonScreen();
            BuildRevealOverlay();
            BuildAscensionOverlay();
        }

        private void BuildHeader()
        {
            RectTransform header = CreatePanel("Header", transform, Panel);
            SetRect(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -78), new Vector2(-20, -12));
            Text title = CreateText("Title", header, "LUMINA RIFT  <size=15>PROTOTYPE 0.0.2</size>", 28, FontStyle.Bold, Blue, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0.02f, 0), new Vector2(0.4f, 1), Vector2.zero, Vector2.zero);
            creditsText = CreateText("Credits", header, string.Empty, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            SetRect(creditsText.rectTransform, new Vector2(0.38f, 0), new Vector2(0.68f, 1), Vector2.zero, Vector2.zero);
            currencyText = CreateText("Currency", header, string.Empty, 17, FontStyle.Normal, Gold, TextAnchor.MiddleRight);
            SetRect(currencyText.rectTransform, new Vector2(0.66f, 0), new Vector2(0.98f, 1), Vector2.zero, Vector2.zero);
        }

        private void BuildNavigation()
        {
            RectTransform navigation = CreatePanel("Navigation", transform, Panel);
            SetRect(navigation, new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 12), new Vector2(-20, 69));
            AddNavButton(navigation, "MAIN", 0.02f, delegate { ShowScreen(mainScreen); });
            AddNavButton(navigation, "CHARACTERS", 0.35f, delegate { ShowScreen(rosterScreen); });
            AddNavButton(navigation, "SUMMON", 0.68f, delegate { ShowScreen(summonScreen); });
            messageText = CreateText("Message", navigation, string.Empty, 13, FontStyle.Italic, new Color(0.75f, 0.8f, 0.9f), TextAnchor.MiddleCenter);
            SetRect(messageText.rectTransform, new Vector2(0, 0.02f), new Vector2(1, 0.32f), Vector2.zero, Vector2.zero);
        }

        private void AddNavButton(Transform parent, string label, float left, UnityEngine.Events.UnityAction action)
        {
            Text text;
            Button button = CreateButton(label, parent, Soft, action, out text);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(left, 0.36f), new Vector2(left + 0.3f, 0.94f), Vector2.zero, Vector2.zero);
        }

        private void BuildMainScreen()
        {
            mainScreen = CreatePanel("MainScreen", transform, Color.clear);
            SetRect(mainScreen, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 82), new Vector2(-20, -90));
            RectTransform hero = CreatePanel("ActiveEcho", mainScreen, Panel);
            SetRect(hero, new Vector2(0, 0), new Vector2(0.62f, 1), Vector2.zero, new Vector2(-8, 0));
            mainPortrait = CreateImage("Portrait", hero, Blue);
            SetRect(mainPortrait.rectTransform, new Vector2(0.045f, 0.30f), new Vector2(0.43f, 0.91f), Vector2.zero, Vector2.zero);
            Button tap = mainPortrait.gameObject.AddComponent<Button>();
            tap.targetGraphic = mainPortrait;
            tap.onClick.AddListener(state.EarnClick);
            Text tapText = CreateText("Tap", mainPortrait.transform, "TAP ECHO\n+ CREDITS", 27, FontStyle.Bold, Background, TextAnchor.MiddleCenter);
            Stretch(tapText.rectTransform, 8, 8, 8, 8);
            mainCharacterName = CreateText("Name", hero, string.Empty, 29, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            SetRect(mainCharacterName.rectTransform, new Vector2(0.47f, 0.75f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);
            mainCharacterStats = CreateText("Stats", hero, string.Empty, 17, FontStyle.Normal, new Color(0.82f, 0.85f, 0.94f), TextAnchor.UpperLeft);
            SetRect(mainCharacterStats.rectTransform, new Vector2(0.47f, 0.28f), new Vector2(0.96f, 0.75f), Vector2.zero, Vector2.zero);
            levelButton = CreateButton("Level", hero, Soft, OnLevel, out levelButtonText);
            SetRect(levelButton.GetComponent<RectTransform>(), new Vector2(0.045f, 0.07f), new Vector2(0.61f, 0.24f), Vector2.zero, Vector2.zero);
            ascendButton = CreateButton("Ascend", hero, Gold, OpenAscensionConfirmation, out ascendButtonText);
            SetRect(ascendButton.GetComponent<RectTransform>(), new Vector2(0.65f, 0.07f), new Vector2(0.955f, 0.24f), Vector2.zero, Vector2.zero);

            RectTransform guide = CreatePanel("RunGuide", mainScreen, Panel);
            SetRect(guide, new Vector2(0.62f, 0), new Vector2(1, 1), new Vector2(8, 0), Vector2.zero);
            Text guideTitle = CreateText("GuideTitle", guide, "RUN PROGRESSION", 24, FontStyle.Bold, Gold, TextAnchor.MiddleCenter);
            SetRect(guideTitle.rectTransform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
            Text guideBody = CreateText("Guide", guide,
                "MILESTONES\nLv.10  x2 income  +2 tickets\nLv.25  x2 income  +4 tickets\nLv.50  x3 income  +10 tickets\nLv.75  x1.75 income  +5 tickets\nLv.100  x2 income  +10 tickets\n\nASCENSION\nAvailable at Lv.50. Every extra level grants +20 Lumina. Every 10 extra levels grants +1 additional Power.",
                17, FontStyle.Normal, new Color(0.78f, 0.83f, 0.94f), TextAnchor.UpperLeft);
            SetRect(guideBody.rectTransform, new Vector2(0.09f, 0.12f), new Vector2(0.91f, 0.81f), Vector2.zero, Vector2.zero);
        }

        private void BuildRosterScreen()
        {
            rosterScreen = CreatePanel("RosterScreen", transform, Color.clear);
            SetRect(rosterScreen, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 82), new Vector2(-20, -90));
            Text heading = CreateText("Heading", rosterScreen, "CHARACTER COLLECTION", 25, FontStyle.Bold, Blue, TextAnchor.MiddleLeft);
            SetRect(heading.rectTransform, new Vector2(0.015f, 0.88f), new Vector2(0.6f, 1), Vector2.zero, Vector2.zero);
            Text hint = CreateText("Hint", rosterScreen, "Select any unlocked Echo to make them active. Summoned Echoes are never auto-equipped.", 15, FontStyle.Normal, Color.white, TextAnchor.MiddleRight);
            SetRect(hint.rectTransform, new Vector2(0.45f, 0.88f), new Vector2(0.985f, 1), Vector2.zero, Vector2.zero);
            for (int i = 0; i < state.Roster.Count; i++)
            {
                int column = i % 3;
                int row = i / 3;
                float left = 0.015f + column * 0.33f;
                float right = left + 0.31f;
                float top = 0.85f - row * 0.42f;
                RectTransform card = CreatePanel("RosterCard" + i, rosterScreen, Soft);
                SetRect(card, new Vector2(left, top - 0.37f), new Vector2(right, top), Vector2.zero, Vector2.zero);
                rosterCards.Add(card);
                CharacterRuntimeState captured = state.Roster[i];
                Button select = card.gameObject.AddComponent<Button>();
                select.targetGraphic = card.GetComponent<Image>();
                select.onClick.AddListener(delegate { state.SetActiveCharacter(captured); });
                CreateRosterCardContents(card);
            }
        }

        private void CreateRosterCardContents(RectTransform card)
        {
            Image portrait = CreateImage("CardPortrait", card, Blue);
            SetRect(portrait.rectTransform, new Vector2(0.05f, 0.18f), new Vector2(0.34f, 0.84f), Vector2.zero, Vector2.zero);
            Text lockText = CreateText("Lock", portrait.transform, "", 32, FontStyle.Bold, Background, TextAnchor.MiddleCenter);
            Stretch(lockText.rectTransform, 2, 2, 2, 2);
            Text name = CreateText("CardName", card, string.Empty, 20, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            SetRect(name.rectTransform, new Vector2(0.39f, 0.66f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);
            Text info = CreateText("CardInfo", card, string.Empty, 15, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
            SetRect(info.rectTransform, new Vector2(0.39f, 0.19f), new Vector2(0.95f, 0.67f), Vector2.zero, Vector2.zero);
            Text active = CreateText("Active", card, "", 13, FontStyle.Bold, Gold, TextAnchor.MiddleCenter);
            SetRect(active.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.17f), Vector2.zero, Vector2.zero);
        }

        private void BuildSummonScreen()
        {
            summonScreen = CreatePanel("SummonScreen", transform, Color.clear);
            SetRect(summonScreen, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 82), new Vector2(-20, -90));
            for (int i = 0; i < state.Banners.Count; i++)
            {
                BannerData banner = state.Banners[i];
                float left = i == 0 ? 0f : 0.505f;
                float right = i == 0 ? 0.495f : 1f;
                RectTransform card = CreatePanel("Banner" + i, summonScreen, Panel);
                SetRect(card, new Vector2(left, 0), new Vector2(right, 1), Vector2.zero, Vector2.zero);
                Image accent = CreateImage("Accent", card, banner.AccentColor);
                SetRect(accent.rectTransform, new Vector2(0, 0.91f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                Text name = CreateText("BannerName", card, banner.DisplayName, 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                SetRect(name.rectTransform, new Vector2(0.05f, 0.77f), new Vector2(0.95f, 0.91f), Vector2.zero, Vector2.zero);
                Text description = CreateText("Description", card, banner.Description, 16, FontStyle.Normal, new Color(0.82f, 0.85f, 0.95f), TextAnchor.MiddleCenter);
                SetRect(description.rectTransform, new Vector2(0.08f, 0.63f), new Vector2(0.92f, 0.77f), Vector2.zero, Vector2.zero);
                Text rates = CreateText("Rates", card, "3★ 75%   •   4★ 20%   •   5★ 5%\n10-pull: guaranteed 4★+   •   Hard pity: 30", 15, FontStyle.Normal, Gold, TextAnchor.MiddleCenter);
                SetRect(rates.rectTransform, new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.63f), Vector2.zero, Vector2.zero);
                string pool = string.Join("  •  ", banner.CharacterPool.Select(item => item.DisplayName + " " + StarText(item.Rarity)));
                Text poolText = CreateText("Pool", card, "AVAILABLE ECHOES\n" + pool, 14, FontStyle.Normal, new Color(0.7f, 0.78f, 0.9f), TextAnchor.UpperCenter);
                SetRect(poolText.rectTransform, new Vector2(0.07f, 0.33f), new Vector2(0.93f, 0.49f), Vector2.zero, Vector2.zero);
                BannerView view = new BannerView { Banner = banner };
                view.Currency = CreateText("Owned", card, string.Empty, 17, FontStyle.Bold, Blue, TextAnchor.MiddleCenter);
                SetRect(view.Currency.rectTransform, new Vector2(0.08f, 0.24f), new Vector2(0.55f, 0.33f), Vector2.zero, Vector2.zero);
                view.Pity = CreateText("Pity", card, string.Empty, 15, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
                SetRect(view.Pity.rectTransform, new Vector2(0.55f, 0.24f), new Vector2(0.92f, 0.33f), Vector2.zero, Vector2.zero);
                Text singleLabel;
                view.SingleButton = CreateButton("Single", card, banner.AccentColor, delegate { StartSummon(banner, 1); }, out singleLabel);
                SetRect(view.SingleButton.GetComponent<RectTransform>(), new Vector2(0.07f, 0.06f), new Vector2(0.47f, 0.21f), Vector2.zero, Vector2.zero);
                singleLabel.text = "SUMMON x1\n" + banner.SinglePullCost + " " + CurrencyName(banner);
                Text tenLabel;
                view.TenButton = CreateButton("Ten", card, banner.AccentColor, delegate { StartSummon(banner, 10); }, out tenLabel);
                SetRect(view.TenButton.GetComponent<RectTransform>(), new Vector2(0.53f, 0.06f), new Vector2(0.93f, 0.21f), Vector2.zero, Vector2.zero);
                tenLabel.text = "SUMMON x10\n" + banner.TenPullCost + " " + CurrencyName(banner);
                bannerViews.Add(view);
            }
        }

        private void BuildRevealOverlay()
        {
            revealOverlay = CreatePanel("SummonReveal", transform, new Color(0.01f, 0.015f, 0.04f, 0.985f));
            Stretch(revealOverlay, 0, 0, 0, 0);
            revealGlow = CreateImage("RiftGlow", revealOverlay, Blue);
            SetRect(revealGlow.rectTransform, new Vector2(0.31f, 0.17f), new Vector2(0.69f, 0.83f), Vector2.zero, Vector2.zero);
            revealRarity = CreateText("Rarity", revealGlow.transform, string.Empty, 38, FontStyle.Bold, Background, TextAnchor.MiddleCenter);
            SetRect(revealRarity.rectTransform, new Vector2(0, 0.63f), new Vector2(1, 0.88f), Vector2.zero, Vector2.zero);
            revealName = CreateText("Name", revealGlow.transform, string.Empty, 34, FontStyle.Bold, Background, TextAnchor.MiddleCenter);
            SetRect(revealName.rectTransform, new Vector2(0, 0.35f), new Vector2(1, 0.64f), Vector2.zero, Vector2.zero);
            revealStatus = CreateText("Status", revealGlow.transform, string.Empty, 21, FontStyle.Bold, Background, TextAnchor.MiddleCenter);
            SetRect(revealStatus.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.35f), Vector2.zero, Vector2.zero);
            revealOverlay.gameObject.SetActive(false);
        }

        private void BuildAscensionOverlay()
        {
            ascensionOverlay = CreatePanel("AscensionConfirmation", transform, new Color(0.01f, 0.015f, 0.04f, 0.96f));
            Stretch(ascensionOverlay, 0, 0, 0, 0);
            RectTransform dialog = CreatePanel("Dialog", ascensionOverlay, Panel);
            SetRect(dialog, new Vector2(0.29f, 0.25f), new Vector2(0.71f, 0.75f), Vector2.zero, Vector2.zero);
            Text title = CreateText("Title", dialog, "ASCEND THIS RUN?", 28, FontStyle.Bold, Gold, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
            ascensionConfirmText = CreateText("Reward", dialog, string.Empty, 18, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
            SetRect(ascensionConfirmText.rectTransform, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.76f), Vector2.zero, Vector2.zero);
            Text cancelText;
            Button cancel = CreateButton("Cancel", dialog, Soft, delegate { ascensionOverlay.gameObject.SetActive(false); }, out cancelText);
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.08f, 0.07f), new Vector2(0.46f, 0.24f), Vector2.zero, Vector2.zero);
            Text confirmText;
            Button confirm = CreateButton("Confirm", dialog, Gold, ConfirmAscension, out confirmText);
            SetRect(confirm.GetComponent<RectTransform>(), new Vector2(0.54f, 0.07f), new Vector2(0.92f, 0.24f), Vector2.zero, Vector2.zero);
            ascensionOverlay.gameObject.SetActive(false);
        }

        private void OnLevel()
        {
            if (!state.TryLevelActiveCharacter()) ShowMessage("Earn more Credits before levelling.");
        }

        private void OpenAscensionConfirmation()
        {
            if (!state.CanAscend) return;
            AscensionReward reward = state.ProjectedAscensionReward;
            ascensionConfirmText.text = "PROJECTED REWARD\n\n+" + reward.Lumina + " Lumina\n+" + reward.Power + " Ascension Power\n\nResets Credits, run levels, and milestone claims.\nKeeps collection, Affinity, currencies, and banner pity.";
            ascensionOverlay.gameObject.SetActive(true);
        }

        private void ConfirmAscension()
        {
            ascensionOverlay.gameObject.SetActive(false);
            state.TryAscend();
            ShowScreen(mainScreen);
        }

        private void StartSummon(BannerData banner, int count)
        {
            if (presentingSummons) return;
            List<GachaResult> results;
            if (!state.TrySummon(banner, count, out results))
            {
                ShowMessage("Not enough " + CurrencyName(banner) + " for that summon.");
                return;
            }
            StartCoroutine(PresentResults(results));
        }

        private IEnumerator PresentResults(IReadOnlyList<GachaResult> results)
        {
            presentingSummons = true;
            revealOverlay.gameObject.SetActive(true);
            for (int i = 0; i < results.Count; i++)
            {
                GachaResult result = results[i];
                int stars = (int)result.Character.Definition.Rarity;
                Color rarityColor = stars == 5 ? Gold : stars == 4 ? new Color(0.72f, 0.4f, 1f) : new Color(0.32f, 0.7f, 1f);
                revealGlow.color = Color.white;
                revealGlow.rectTransform.localScale = Vector3.one * 0.15f;
                revealRarity.text = "RIFT OPENING";
                revealName.text = string.Empty;
                revealStatus.text = results.Count > 1 ? (i + 1) + " / " + results.Count : string.Empty;
                float duration = stars == 5 ? 0.7f : 0.35f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    revealGlow.rectTransform.localScale = Vector3.one * Mathf.SmoothStep(0.15f, stars == 5 ? 1.08f : 1f, t);
                    revealGlow.color = Color.Lerp(Color.white, rarityColor, t);
                    yield return null;
                }
                revealGlow.rectTransform.localScale = Vector3.one;
                revealRarity.text = StarText(result.Character.Definition.Rarity);
                revealName.text = result.Character.Definition.DisplayName;
                revealStatus.text = (result.WasDuplicate ? "DUPLICATE  •  +" + result.AffinityAwarded + " AFFINITY XP" : "NEW!") +
                    (results.Count > 1 ? "\n" + (i + 1) + " / " + results.Count : string.Empty);
                yield return new WaitForSecondsRealtime(stars == 5 ? 1.05f : 0.65f);
            }
            revealOverlay.gameObject.SetActive(false);
            presentingSummons = false;
            Refresh();
        }

        private void ShowScreen(RectTransform screen)
        {
            if (mainScreen == null) return;
            mainScreen.gameObject.SetActive(screen == mainScreen);
            rosterScreen.gameObject.SetActive(screen == rosterScreen);
            summonScreen.gameObject.SetActive(screen == summonScreen);
            Refresh();
        }

        private void Refresh()
        {
            if (state == null || state.ActiveCharacter == null || creditsText == null) return;
            CharacterRuntimeState active = state.ActiveCharacter;
            CharacterData data = active.Definition;
            creditsText.text = "CREDITS  " + FormatNumber(state.Credits);
            currencyText.text = "TICKETS  " + state.StandardTickets + "     LUMINA  " + state.Lumina + "     POWER x" + state.AscensionMultiplier.ToString("0.00");
            mainCharacterName.text = StarText(data.Rarity) + "  " + data.DisplayName;
            mainCharacterName.color = data.AccentColor;
            mainPortrait.color = data.AccentColor;
            mainPortrait.sprite = data.Portrait;
            int rank = state.GetAffinityRank(active);
            string affinityProgress = rank >= 3 ? "MAX" : active.AffinityXp + " / " + (rank == 1 ? config.AffinityRankTwoXp : config.AffinityRankThreeXp);
            mainCharacterStats.text = "LEVEL " + active.Level + " / " + config.RunLevelCap + "\nAffinity " + Roman(rank) + "  (" + affinityProgress + ")\n\n+" + FormatNumber(state.ClickIncome) + " per tap\n+" + FormatNumber(state.PassiveIncomePerSecond) + " Credits / sec\n\n" + data.Description;
            levelButtonText.text = active.Level >= config.RunLevelCap ? "MAX RUN LEVEL" : "LEVEL UP\n" + FormatNumber(state.NextLevelCost) + " CREDITS";
            levelButton.interactable = state.CanLevel;
            AscensionReward reward = state.ProjectedAscensionReward;
            ascendButtonText.text = state.CanAscend ? "ASCEND\n+" + reward.Lumina + " L / +" + reward.Power + " POWER" : "ASCEND\nLV." + config.AscensionMinimumLevel + " REQUIRED";
            ascendButton.interactable = state.CanAscend;
            RefreshRoster();
            foreach (BannerView view in bannerViews)
            {
                int owned = state.CurrencyFor(view.Banner);
                view.Currency.text = "OWNED  " + owned + " " + CurrencyName(view.Banner);
                view.Pity.text = "5★ PITY  " + state.GetPity(view.Banner) + " / " + view.Banner.HardPity;
                view.SingleButton.interactable = !presentingSummons && owned >= view.Banner.SinglePullCost;
                view.TenButton.interactable = !presentingSummons && owned >= view.Banner.TenPullCost;
            }
        }

        private void RefreshRoster()
        {
            for (int i = 0; i < rosterCards.Count; i++)
            {
                CharacterRuntimeState character = state.Roster[i];
                RectTransform card = rosterCards[i];
                Image portrait = card.Find("CardPortrait").GetComponent<Image>();
                Text lockText = portrait.transform.Find("Lock").GetComponent<Text>();
                Text name = card.Find("CardName").GetComponent<Text>();
                Text info = card.Find("CardInfo").GetComponent<Text>();
                Text active = card.Find("Active").GetComponent<Text>();
                Button button = card.GetComponent<Button>();
                portrait.color = character.IsOwned ? character.Definition.AccentColor : new Color(0.075f, 0.08f, 0.1f);
                lockText.text = character.IsOwned ? StarText(character.Definition.Rarity) : "LOCKED";
                name.text = character.IsOwned ? character.Definition.DisplayName : "UNKNOWN ECHO";
                name.color = character.IsOwned ? character.Definition.AccentColor : Color.gray;
                int rank = state.GetAffinityRank(character);
                info.text = character.IsOwned ? "Level " + character.Level + "\nAffinity " + Roman(rank) + "  •  " + character.AffinityXp + " XP\n" + RarityRole(character.Definition) : "Summon this Echo to unlock.";
                active.text = character == state.ActiveCharacter ? "● ACTIVE ECHO" : character.IsOwned ? "SELECT" : string.Empty;
                button.interactable = character.IsOwned && character != state.ActiveCharacter;
                card.GetComponent<Image>().color = character == state.ActiveCharacter ? Color.Lerp(character.Definition.AccentColor, Soft, 0.55f) : Soft;
            }
        }

        private static string RarityRole(CharacterData data)
        {
            if (data.Rarity == CharacterRarity.ThreeStar) return "Low cost • early efficiency";
            if (data.Rarity == CharacterRarity.FiveStar) return "Higher cost • late scaling";
            return "Balanced growth";
        }

        private void ShowMessage(string message) { if (messageText != null) messageText.text = message; }
        private static string StarText(CharacterRarity rarity) { return new string('★', (int)rarity); }
        private static string Roman(int value) { return value == 3 ? "III" : value == 2 ? "II" : "I"; }
        private static string CurrencyName(BannerData banner) { return banner.Currency == BannerCurrency.Lumina ? "LUMINA" : "TICKETS"; }
        private static string FormatNumber(double value)
        {
            if (value >= 1e12) return (value / 1e12).ToString("0.##") + "T";
            if (value >= 1e9) return (value / 1e9).ToString("0.##") + "B";
            if (value >= 1e6) return (value / 1e6).ToString("0.##") + "M";
            if (value >= 1e3) return (value / 1e3).ToString("0.##") + "K";
            return value.ToString(value < 100 ? "0.0" : "0");
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventObject);
        }

        private RectTransform CreatePanel(string name, Transform parent, Color color) { return CreateImage(name, parent, color).rectTransform; }
        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            Text text = item.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Button CreateButton(string name, Transform parent, Color color, UnityEngine.Events.UnityAction action, out Text label)
        {
            Image image = CreateImage(name, parent, color);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.disabledColor = new Color(0.12f, 0.13f, 0.17f, 0.85f);
            button.colors = colors;
            button.onClick.AddListener(action);
            label = CreateText("Label", image.transform, name, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 6, 6, 5, 5);
            return button;
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
