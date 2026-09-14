# Lumina Rift — Prototype 0.0.3: Long-Term Progression

Open `Assets/Scenes/Main.unity` and press Play. On first import, the editor creates the scene and all character, banner, and balance ScriptableObjects automatically.

## Added in 0.0.3

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
- Runtime visuals remain prototype IMGUI and target a 16:9 landscape layout.
- Summon reveals have no skip/history controls.

## Recommended 0.0.4

Playtest multiple sessions first and use the Rift Record to tune time-to-Ascend, Featured-pull cadence, and automation thresholds. Once retention pacing is sound, build Active + Support teams, character abilities, tags, and synergy so collection choices affect more than income curves.
