# Lumina Rift — Prototype 0.0.2

Open `Assets/Scenes/SampleScene.unity` and press Play. The prototype creates its UI automatically and requires no scene wiring.

## Complete prototype loop

1. Use the **Main** screen to tap the active Echo, earn Credits, and buy run levels.
2. Claim Standard Tickets from level milestones at levels 10, 25, 50, 75, and 100.
3. Use **Summon** for Standard Ticket or Featured Lumina pulls. Both banners support single and 10-pulls.
4. A 10-pull guarantees at least one 4-star or higher result. Each banner tracks separate hard pity at 30 pulls.
5. New Echoes enter the collection without replacing the active Echo. Duplicates award permanent Affinity XP.
6. Use **Characters** to inspect all unlocked and locked cards and manually select the active Echo.
7. At level 50 or beyond, preview and confirm Ascension. Extra levels grant more Lumina and additional Ascension Power.
8. Ascension resets Credits, run levels, and milestone claims. It keeps ownership, Affinity, tickets, Lumina, pity, the active selection, and permanent power.

## Architecture

- `CharacterData`: static identity, rarity, base income, cost multiplier, and click/passive level exponents.
- `CharacterRuntimeState`: mutable ownership, run level, and permanent Affinity XP.
- `BannerData`: currency, costs, rates, pool, rate-up, hard pity, and visual accent.
- `GachaManager`: rarity rolls, per-banner pity, 10-pull guarantee, rate-up selection, unlocks, and duplicates.
- `LuminaRiftGameState`: run economy, active character, milestones, income, currencies, and Ascension resets.
- `LuminaRiftPrototypeUI`: Main, Characters, and Summon screens plus replaceable summon/Ascension overlays.
- `PrototypeAssetSetup`: creates the default ScriptableObject assets and offers a rebuild menu command.

## Major tuning locations

- `Resources/PrototypeGameConfig.asset`: level cap/cost curve, milestones, Affinity thresholds and bonuses, Ascension formula, permanent power scaling.
- `Resources/Characters/*.asset`: each Echo's income bases, level cost multiplier, and early/late scaling exponents.
- `Resources/Banners/*.asset`: banner currency/cost, 75/20/5 rates, 30-pull pity, pool, and featured rate-up.

Default Affinity progression is:

- Affinity I: unlocked.
- Affinity II at 25 XP: +10% base click income.
- Affinity III at 75 XP: +10% base passive income.

Default Ascension progression is 100 Lumina and +1 Power at level 50, +20 Lumina per extra level, and +1 additional Power per 10 extra levels. Each Power adds +25% global income.

## Validation

Run **Lumina Rift > Validate Prototype 0.0.2 Loop** to smoke-test milestone tickets, the 10-pull guarantee, collection persistence, Ascension rewards/resets, and a post-Ascension Featured summon.

## Known prototype limits

- No save data or offline income; state lasts for the current play session only.
- Summon presentation uses placeholder UI flashes and sequential cards, with no skip control yet.
- The six-character roster and balance values are for loop testing, not final balance.
- Pity is kept through in-session Ascensions but cannot persist between launches until saving is added.
- Runtime UI is optimized for a 16:9 landscape reference resolution.

## Recommended Prototype 0.0.3 direction

Add versioned local save data first, then offline income and basic economy analytics. After persistence is trustworthy, improve summon pacing with skip/history controls and use playtest data to tune character efficiency, Affinity thresholds, and Ascension timing. Avoid expanding the roster until the collection loop has measurable retention and choice value.
