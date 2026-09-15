using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace LuminaRift
{
    public sealed partial class LuminaRiftPrototypeUI
    {
        private void DrawSummon(Rect area)
        {
            if (state.Banners.Count == 0) return;
            selectedBanner = Mathf.Clamp(selectedBanner, 0, state.Banners.Count - 1);
            for (int i = 0; i < state.Banners.Count; i++)
                if (ActionButton(new Rect(area.x + i * 268, area.y, 260, 34), state.Banners[i].DisplayName.ToUpperInvariant(), i == selectedBanner ? primaryButton : ghostButton)) selectedBanner = i;
            BannerData banner = state.Banners[selectedBanner];
            bool featured = banner.RateUpCharacter != null;
            Color accent = featured ? Hex("BCE6FF") : Gold;
            Rect canvas = new Rect(area.x, area.y + 46, area.width, area.height - 46);
            float heroHeight = canvas.height - 192;
            GUI.BeginGroup(canvas);
            DrawBannerScene(new Rect(0, 0, canvas.width, canvas.height), banner, featured, accent);
            // Continue the title column's alignment and background into the controls.
            const float left = 38, columnWidth = 480;
            DrawBannerLine(new Vector2(left, heroHeight), new Vector2(left + columnWidth, heroHeight), new Color(accent.r, accent.g, accent.b, .35f), 1);
            int pity = state.GetPity(banner);
            float total = Mathf.Max(.001f, banner.ThreeStarRate + banner.FourStarRate + banner.FiveStarRate);
            GUI.Label(new Rect(left, heroHeight + 10, 400, 30), "5★ guaranteed within " + Math.Max(1, banner.HardPity - pity) + " summons", heading);
            GUI.Label(new Rect(left + 400, heroHeight + 10, 80, 30), pity + " / " + banner.HardPity,
                AccentLabel(13, FontStyle.Normal, TextAnchor.MiddleRight, Muted));
            DrawRect(new Rect(left, heroHeight + 43, columnWidth, 3), Edge);
            DrawRect(new Rect(left, heroHeight + 43, columnWidth * Mathf.Clamp01((float)pity / Mathf.Max(1, banner.HardPity)), 3), accent);
            GUI.Label(new Rect(left, heroHeight + 53, 100, 26), "BASE RATES", eyebrow);
            GUI.Label(new Rect(left + 112, heroHeight + 53, 112, 26), "3★  " + (100 * banner.ThreeStarRate / total).ToString("0.#") + "%", eyebrow);
            GUI.Label(new Rect(left + 238, heroHeight + 53, 112, 26), "4★  " + (100 * banner.FourStarRate / total).ToString("0.#") + "%", eyebrow);
            GUI.Label(new Rect(left + 364, heroHeight + 53, 116, 26), "5★  " + (100 * banner.FiveStarRate / total).ToString("0.#") + "%", eyebrow);
            GUI.Label(new Rect(left, heroHeight + 79, columnWidth, 26), "Every ten-pull guarantees a 4★ Echo or higher.",
                AccentLabel(13, FontStyle.Normal, TextAnchor.MiddleLeft, Muted));
            string symbol = banner.Currency == BannerCurrency.Lumina ? "✦" : "▱";
            GUI.Label(new Rect(left, heroHeight + 105, columnWidth, 26), "AVAILABLE  " + symbol + " " + state.CurrencyFor(banner),
                AccentLabel(12, FontStyle.Bold, TextAnchor.MiddleRight, accent));
            GUI.enabled = InterfaceEnabled && state.CurrencyFor(banner) >= banner.SinglePullCost;
            if (ActionButton(new Rect(left, heroHeight + 132, 234, 48), "SUMMON ×1\n" + symbol + " " + banner.SinglePullCost, secondaryButton)) BeginSummon(banner, 1);
            GUI.enabled = InterfaceEnabled && state.CurrencyFor(banner) >= banner.TenPullCost;
            if (ActionButton(new Rect(left + 246, heroHeight + 132, 234, 48), "SUMMON ×10\n" + symbol + " " + banner.TenPullCost, primaryButton)) BeginSummon(banner, 10);
            GUI.enabled = InterfaceEnabled;
            GUI.EndGroup();
            DrawFrame(canvas, new Color(accent.r, accent.g, accent.b, .6f));
        }

        private void DrawBannerScene(Rect rect, BannerData banner, bool featured, Color accent)
        {
            // The artwork and all ornament share an inset area inside the hero.
            GUI.BeginGroup(rect);
            DrawTexture(new Rect(0, 0, rect.width, rect.height), summonGradient, featured ? new Color(.8f, .95f, 1) : new Color(1, .86f, 1));
            Rect artwork = new Rect(568, 16, rect.width - 592, rect.height - 32);
            PrepareBannerGeometry(artwork.size);
            Vector2 focus = artwork.center;
            float effectScale = Mathf.Min(artwork.width / 600f, artwork.height / 460f);
            DrawGlow(focus, Mathf.Min(artwork.width, artwork.height), featured ? new Color(.46f, .73f, 1, .5f) : new Color(.56f, .42f, .94f, .38f));
            for (int i = 0; i < 85; i++)
            {
                float x = artwork.x + 16 + Mathf.Repeat(i * 137.37f, Mathf.Max(1, artwork.width - 32));
                float y = artwork.y + 16 + Mathf.Repeat(i * 73.13f, Mathf.Max(1, artwork.height - 32));
                float alpha = .18f + .16f * (1 + Mathf.Sin(Time.realtimeSinceStartup * .7f + i));
                if (i % 11 == 0) DrawBannerStar(new Vector2(x, y), 7, new Color(accent.r, accent.g, accent.b, alpha));
                else DrawRect(new Rect(x, y, 1.5f, 1.5f), new Color(.8f, .9f, 1, alpha));
            }
            DrawBannerOrbit(focus, 218 * effectScale, 218 * effectScale, -.4f, accent, .3f);
            DrawBannerOrbit(focus, 202 * effectScale, 202 * effectScale, -.4f, accent, .16f);
            DrawBannerOrbit(focus, 286 * effectScale, 87 * effectScale, -.32f, accent, .34f);
            DrawBannerOrbit(focus, 257 * effectScale, 108 * effectScale, .52f, accent, .15f);
            if (featured)
            {
                // Move the ice sigils around the stable rings without rotating GUI coordinates.
                float phase = Mathf.Repeat(Time.realtimeSinceStartup * .12f, Mathf.PI * 2);
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4 + phase;
                    DrawBannerSnowflake(focus + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (200 * effectScale), (i % 2 == 0 ? 19 : 10) * effectScale, new Color(.74f, .88f, 1, .65f));
                }
                var character = state.Roster.FirstOrDefault(item => item.Definition == banner.RateUpCharacter);
                if (character != null)
                {
                    Sprite art = character.Definition.SummonArtwork;
                    if (art != null)
                    {
                        Rect pose = artwork;
                        // Weiss's sword pulls the image bounds left of her body. Keep the
                        // full pose and scale, but optically center her torso on the rings.
                        if (character.Definition.CharacterId == "weiss_schnee")
                        {
                            float fittedWidth = Mathf.Min(artwork.width, artwork.height * art.rect.width / art.rect.height);
                            float offset = Mathf.Min(fittedWidth * .10f, (artwork.width - fittedWidth) * .5f);
                            pose.x -= offset;
                        }
                        DrawSprite(pose, art, 1);
                    }
                    else DrawSummonCharacter(character, artwork);
                }
            }
            else
            {
                // A deliberate celestial centerpiece until the standard roster has finished artwork.
                DrawBannerStar(focus, 42, Hex("EEE0B8"));
                DrawBannerDiamond(focus, 24, Violet);
                int count = 0;
                foreach (var character in banner.CharacterPool)
                {
                    if (character == null || character.Rarity != CharacterRarity.FiveStar) continue;
                    float angle = (-135 + count * 90) * Mathf.Deg2Rad;
                    Vector2 point = focus + new Vector2(Mathf.Cos(angle) * (artwork.width * .32f), Mathf.Sin(angle) * (artwork.height * .30f));
                    DrawBannerLine(focus, point, new Color(accent.r, accent.g, accent.b, .22f), 1);
                    DrawTexture(new Rect(point.x - 34, point.y - 34, 68, 68), ring, new Color(accent.r, accent.g, accent.b, .8f));
                    DrawBannerStar(point, 13, accent);
                    DrawFittedLabel(new Rect(point.x - 99, point.y + 32, 198, 36), character.DisplayName, centeredTitle);
                    if (++count == 4) break;
                }
                GUI.Label(new Rect(focus.x - 170, focus.y + 50, 340, 22), "T H E   C O R E   C O L L E C T I O N", AccentLabel(11, FontStyle.Bold, TextAnchor.MiddleCenter, Gold));
            }
            DrawBannerStar(new Vector2(42, 37), 13, accent);
            GUI.Label(new Rect(66, 26, 470, 24), featured ? "C H A R A C T E R   E V E N T   R I F T" : "S T A N D A R D   R I F T", AccentLabel(12, FontStyle.Bold, TextAnchor.MiddleLeft, accent));
            DrawBannerLine(new Vector2(38, 66), new Vector2(460, 66), new Color(accent.r, accent.g, accent.b, .35f), 1);
            string headline = featured ? banner.DisplayName.Replace(" ", "\n") : "Echoes\nBeyond the Veil";
            GUI.Label(new Rect(34, 78, 530, 140), headline, AccentLabel(featured ? 56 : 49, FontStyle.Bold, TextAnchor.UpperLeft, Hex("F2EDDF")));
            GUI.Label(new Rect(38, 209, 480, 34), featured ? "In stillness, strength awakens." : "Different paths. A brighter tomorrow.", AccentLabel(20, FontStyle.Italic, TextAnchor.MiddleLeft, accent));
            GUI.Label(new Rect(38, 246, 480, 46), featured
                ? banner.RateUpCharacter.DisplayName + "   " + Stars(banner.RateUpCharacter.Rarity) + "   •   " + Mathf.RoundToInt(banner.RateUpShareOfFiveStar * 100) + "% of 5★ pulls"
                : "Summon from the core collection of Lumina Rift.", body);
            GUI.EndGroup();
        }

        private readonly Dictionary<Vector3, Texture2D> bannerLines = new Dictionary<Vector3, Texture2D>();
        private Texture2D bannerDiamond;
        private Vector2 bannerGeometrySize;

        private void PrepareBannerGeometry(Vector2 size)
        {
            if (bannerGeometrySize == size) return;
            ReleaseBannerGeometry();
            bannerGeometrySize = size;
        }

        private void ReleaseBannerGeometry()
        {
            foreach (var texture in bannerLines.Values)
            {
                if (Application.isPlaying) Destroy(texture);
                else DestroyImmediate(texture);
            }
            bannerLines.Clear();
            if (bannerDiamond != null)
            {
                if (Application.isPlaying) Destroy(bannerDiamond);
                else DestroyImmediate(bannerDiamond);
                bannerDiamond = null;
            }
        }

        // Rasterize ornament once; drawing never changes GUI.matrix or the group's clip.
        private void DrawBannerLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 delta = b - a;
            if (Mathf.Abs(delta.y) < .001f)
            {
                DrawRect(new Rect(Mathf.Min(a.x, b.x), a.y - width / 2, Mathf.Abs(delta.x), width), color);
                return;
            }
            if (Mathf.Abs(delta.x) < .001f)
            {
                DrawRect(new Rect(a.x - width / 2, Mathf.Min(a.y, b.y), width, Mathf.Abs(delta.y)), color);
                return;
            }
            var key = new Vector3(delta.x, delta.y, width);
            const float padding = 2;
            float w = Mathf.Abs(delta.x) + padding * 2, h = Mathf.Abs(delta.y) + padding * 2;
            if (!bannerLines.TryGetValue(key, out Texture2D texture))
            {
                int tw = Mathf.CeilToInt(w * 2), th = Mathf.CeilToInt(h * 2);
                texture = new Texture2D(tw, th, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var pixels = new Color[tw * th];
                Vector2 start = new Vector2(delta.x < 0 ? -delta.x : 0, delta.y < 0 ? -delta.y : 0) + Vector2.one * padding;
                for (int y = 0; y < th; y++)
                    for (int x = 0; x < tw; x++)
                    {
                        Vector2 p = new Vector2((x + .5f) * w / tw, (th - y - .5f) * h / th);
                        float t = Mathf.Clamp01(Vector2.Dot(p - start, delta) / delta.sqrMagnitude);
                        float distance = Vector2.Distance(p, start + t * delta);
                        pixels[y * tw + x] = new Color(1, 1, 1, Mathf.Clamp01((width / 2 + .5f - distance) * 2));
                    }
                texture.SetPixels(pixels); texture.Apply(false, true);
                bannerLines.Add(key, texture);
            }
            DrawTexture(new Rect(Mathf.Min(a.x, b.x) - padding, Mathf.Min(a.y, b.y) - padding, w, h), texture, color);
        }

        private void DrawBannerDiamond(Vector2 center, float size, Color color)
        {
            if (bannerDiamond == null)
                bannerDiamond = MakeTexture(64, (x, y) => new Color(1, 1, 1,
                    Mathf.Clamp01(32 - Mathf.Abs(x - 31.5f) - Mathf.Abs(y - 31.5f))));
            float extent = size * 1.414214f;
            DrawTexture(new Rect(center.x - extent / 2, center.y - extent / 2, extent, extent), bannerDiamond, color);
        }
        private void DrawBannerOrbit(Vector2 center, float rx, float ry, float tilt, Color color, float alpha)
        {
            Vector2 previous = Vector2.zero;
            for (int i = 0; i <= 96; i++)
            {
                float a = i * Mathf.PI * 2 / 96;
                float x = Mathf.Cos(a) * rx, y = Mathf.Sin(a) * ry;
                Vector2 point = center + new Vector2(x * Mathf.Cos(tilt) - y * Mathf.Sin(tilt), x * Mathf.Sin(tilt) + y * Mathf.Cos(tilt));
                if (i > 0) DrawBannerLine(previous, point, new Color(color.r, color.g, color.b, alpha), 1);
                previous = point;
            }
        }

        private void DrawBannerStar(Vector2 center, float size, Color color)
        {
            DrawBannerLine(center - Vector2.up * size, center + Vector2.up * size, color, 1);
            DrawBannerLine(center - Vector2.right * size, center + Vector2.right * size, color, 1);
            DrawBannerDiamond(center, size * .36f, color);
            DrawGlow(center, size * 4, new Color(color.r, color.g, color.b, color.a * .2f));
        }

        private void DrawBannerSnowflake(Vector2 center, float size, Color color)
        {
            // Local endpoints stay identical while the group moves, keeping the
            // cached line textures bounded throughout an arbitrarily long animation.
            GUI.BeginGroup(new Rect(center.x - size - 2, center.y - size - 2, size * 2 + 4, size * 2 + 4));
            center = new Vector2(size + 2, size + 2);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3;
                Vector2 direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 side = new Vector2(-direction.y, direction.x);
                DrawBannerLine(center, center + direction * size, color, 1);
                Vector2 branch = center + direction * size * .58f;
                DrawBannerLine(branch, branch + (direction + side) * size * .25f, color, 1);
                DrawBannerLine(branch, branch + (direction - side) * size * .25f, color, 1);
            }
            GUI.EndGroup();
        }
    }
}
