# Lumina Rift — Prototype 0.0.4: Visual Identity

Open `Assets/Scenes/Main.unity` and press Play. On first import, the editor creates the scene and all character, banner, and balance ScriptableObjects automatically.

## Added in 0.0.4

- A complete sci-fi/fantasy presentation pass built around the Lumina/Rift identity: near-black navy, luminous cyan, violet, restrained gold, glass-like panels, dimensional geometry, and animated star fields.
- A character-first Home screen with click punch, floating income, Lumina sparks, rarity treatment, compact currency iconography, and a redesigned level/Ascension rail.
- A new Echo Archive with rarity-visible undiscovered cards, restrained information density, Affinity progress, and active-character framing.
- A centrepiece Summon screen with selectable banners, animated portal treatment, pity visualization, featured-character presentation, and staged rarity reveals.
- Original production-style Solara key art used across her Home, archive, Featured banner, and summon reveal appearances.
- A Unity 6 / URP 17 foundation. On first import, `Lumina Rift > Configure URP Foundation` is run automatically and creates the pipeline assets under `Assets/LuminaRift/Rendering`.

## Preserved from 0.0.3

- Versioned JSON save/load for the current run, collection, Affinity, active Echo, currencies, Ascension progression, banner pity, automation settings, milestone claims, and telemetry.
- Autosave every 10 seconds and saves on pause, quit, summons, Ascension, offline collection, and save reset.
- Offline passive Credits based on the saved active Echo, capped at 8 hours, with a Welcome Back collection panel.
- Rift Rank, Ascension Count, and visible convenience milestones.
- Level x10/x25 at Ascension 2, Level MAX at Ascension 3, Auto-Level at Ascension 5, and starting level 5 at Ascension 10.
- Local Rift Record statistics: run/lifetime time, last Ascension duration, highest level, pulls, 5-star pulls, clicks, and lifetime Credits.
- A confirmed developer Reset Save button.

The existing Standard/Featured banners, separate 30-pull pity, 10-pull guarantee, Affinity ranks, six-Echo roster, and scalable Ascension rewards are preserved.

## Tuning

After Unity imports the project:

- `Assets/LuminaRift/Resources/PrototypeGameConfig.asset` controls offline cap, automation milestones, level economy, Affinity, and Ascension rewards.
- `Assets/LuminaRift/Resources/Characters/*.asset` controls per-character cost and income scaling.
- `Assets/LuminaRift/Resources/Banners/*.asset` controls costs, pools, rates, rate-up, and pity.

The local save is named `lumina-rift-save.json` under `Application.persistentDataPath`. Saving is local-only; no telemetry leaves the device.

Run **Lumina Rift > Validate Prototype 0.0.3 Loop** for the deterministic in-editor smoke test.

## Known limits

- No cloud save, save migration beyond version 1, or tamper protection.
- Offline income uses only the active Echo; support teams do not exist yet.
- Runtime visuals still use IMGUI for rapid iteration and target a 16:9 landscape layout.
- Solara is the single visual-quality benchmark; the other Echoes intentionally retain luminous silhouette treatment until their final art direction is approved.
- Summon reveals can be skipped but do not yet have a persistent pull-history screen.

## Recommended 0.0.5

Evaluate the new identity in motion before expanding its asset count. Replace Solara only after a representative Koikatsu render has been tested in the same Home/card/banner crops; then carry the approved art pipeline to the remaining Echoes. Once the presentation and retention pacing are sound, build Active + Support teams, abilities, tags, and synergy.
