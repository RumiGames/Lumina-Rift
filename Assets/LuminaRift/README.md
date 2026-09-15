# Lumina Rift — Prototype 0.0.4: Visual Identity

Open `Assets/Scenes/Main.unity` and press Play. On first import, the editor creates the scene and all character, banner, and balance ScriptableObjects automatically.

## Added in 0.0.4

- An art-independent astral-fantasy presentation built from cloud blue, pearl ivory, soft lavender, champagne gold, frosted panels, procedural clouds, orbital geometry, and crystalline rift light.
- A character-first Home screen with click glow, floating income, rarity treatment, compact currencies, and a unified income/level/milestone rail.
- A coherent Character Collection with names visible for every character, explicit collected/not-yet-collected status, rarity treatment, Affinity progress, and active-character framing.
- A centrepiece Summon screen with a code-driven celestial banner canvas, a calm text-safe information panel, selectable banners, pity visualization, featured-character presentation, and staged rarity reveals.
- Koikatsu-ready presentation slots on every character: card portrait, Home full-body render, and Summon promotional pose. Missing art falls back cleanly without baking placeholder images into the visual identity.
- A Unity 6 / URP 17 foundation. On first import, `Lumina Rift > Configure URP Foundation` is run automatically and creates the pipeline assets under `Assets/LuminaRift/Rendering`.

## Preserved from 0.0.3

- Versioned JSON save/load for the current run, collection, Affinity, active Echo, currencies, Ascension progression, banner pity, automation settings, milestone claims, and telemetry.
- Autosave every 10 seconds and saves on pause, quit, summons, Ascension, offline collection, and save reset.
- Offline passive Credits based on the saved active Echo, capped at 8 hours, with a Welcome Back collection panel.
- Rift Rank, Ascension Count, and visible convenience milestones.
- Level x10/x25 at Ascension 2, Level MAX at Ascension 3, Auto-Level at Ascension 5, and starting level 5 at Ascension 10.
- Local Rift Record statistics: run/lifetime time, last Ascension duration, highest level, pulls, 5-star pulls, clicks, and lifetime Credits.
- A confirmed developer Reset Save button.

The Standard and Featured banners use separate 40-pull pity counters, a 10-pull 4★ guarantee, Affinity ranks, a 21-Echo roster, and scalable Ascension rewards.

## Contrast and visual hierarchy

- Midnight-blue background, opaque navy panels, and lighter slate cards establish distinct layers.
- Bright primary text, larger supporting labels, and gold actions improve readability. Disabled actions keep readable labels without accepting input.
- Status messages have dedicated space above navigation. Missing character art uses an orbital monogram sigil.

## Presentation polish

- Passive text and panels have fixed states; only buttons react to hover, with no font or layout changes. Banner tabs are wider and banner titles fit on one line.
- Offline earnings use separate heading, duration, reward, and collection rows with a readable Credit total.
- Summons build from a charging rift into rarity-coloured light and a character reveal. Five-star arrivals have a longer build and a fuller burst.
- Reveals wait for **Reveal next**. **Skip to results** works throughout the sequence. Both single and ten-pulls finish with a retained summary showing each Echo, rarity, new ownership, and duplicate Affinity rewards.
- Modal screens consistently block background actions. Summon results are awarded and saved before the presentation starts; advancing or skipping never charges again.
- Banner base rates and remaining hard pity read from banner data.

## Tuning

After Unity imports the project:

- `Assets/LuminaRift/Resources/PrototypeGameConfig.asset` controls offline cap, automation milestones, level economy, Affinity, and Ascension rewards.
- `Assets/LuminaRift/Resources/Characters/*.asset` controls per-character cost and income scaling.
- `Assets/LuminaRift/Resources/Banners/*.asset` controls costs, pools, rates, rate-up, and pity.

The local save is named `lumina-rift-save.json` under `Application.persistentDataPath`. Saving is local-only; no telemetry leaves the device.

Run **Lumina Rift > Validate Prototype 0.0.4 Loop** for the deterministic in-editor smoke test, or **Lumina Rift > Validate Presentation Flow** to include modal blocking, summon progression, result retention, and offline reward checks.

## Known limits

- No cloud save, save migration beyond version 1, or tamper protection.
- Offline income uses only the active Echo; support teams do not exist yet.
- Runtime visuals still use IMGUI for rapid iteration and target a 16:9 landscape layout.
- Final character renders are intentionally unassigned; characters use lightweight luminous silhouettes until Koikatsu exports are added to their presentation slots.
- Summon reveals can be skipped but do not yet have a persistent pull-history screen.

## Recommended 0.0.5

Import one representative Koikatsu character as a transparent card portrait, Home full-body render, and Summon pose; evaluate all three crops before carrying that export pipeline to the remaining characters. Once the presentation and retention pacing are sound, build Active + Support teams, abilities, tags, and synergy.

Open **Lumina Rift > Visual Preview (No Save)** to inspect Home, Collection, Summon, Records, Offline, Reveal, Results, and Ascension at the 1280x720 reference layout. This editor-only preview uses disposable sample data and never creates a save service. The currency toggle previews enabled and disabled summon buttons.

## Collection Ascension

Ascension has its own bottom navigation page. Every owned Echo at level 50 or above contributes its configured reward; switching the active Echo does not change the total. Each contribution appears alongside the total Lumina, permanent Power, and resulting income multiplier. The current level cap is 100, so levels beyond 50 increase the reward. Ascension resets Credits, all run levels, and milestone claims together while keeping collection, Affinity, currencies, pity, and permanent progress.

The Home page keeps upgrades and next milestones together. Banner artwork and orbital effects share the centre of the right-hand region. Echo ring pulses use opacity with stable geometry; reveal cards keep text stationary, and reusable label/button styles avoid repeated style allocation during animation.

## Character and banner pool

The Standard Rift contains nine 3★ Echoes, seven 4★ Echoes, and four 5★ Echoes. Frozen Resonance draws its 3★ and 4★ results from that collection and adds Weiss Schnee as its exclusive featured 5★. When a 5★ appears on Frozen Resonance, Weiss has a 50% chance; the remaining 50% is shared by the four standard 5★ Echoes.

Both banners use base rates of 82% for 3★, 16% for 4★, and 2% for 5★, with a guaranteed 5★ by summon 40. Ten-pulls still guarantee at least one 4★ or higher.
