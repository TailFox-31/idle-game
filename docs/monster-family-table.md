# 100-Wave Enemy Family Table

Scope: enemy family progression, Armor behavior, and numeric spawn analysis only. This change does not add new mechanics, art, or HP/ATK/gold wave-scaling formula changes.

## Source Formulas

Enemy values come from `Assets/Scripts/EnemyController.cs`.

- `waveOffset = wave - 1`
- Base HP: `RoundToInt(12 * (1 + 0.24 * waveOffset))`
- Base ATK: `RoundToInt(1 * (1 + 0.10 * waveOffset))`
- Base attacks/sec: `0.5`
- Base gold: `RoundToInt(5 * (1 + 0.18 * waveOffset))`
- Wave 10 and every 10th wave after it is a boss wave.
- Boss waves use `archetypeWave = wave - 1` for family selection.
- Enemy attacks/sec is shaped by family and boss multipliers, then clamped to 8 APS for normal enemies and 3.5 APS for bosses.
- W101+ repeats the same 100-wave family table while HP, ATK, and gold continue to use the existing wave number.
- Defense is flat damage reduction.
- Armor is percent damage reduction stored as a ratio. For example, `0.25` means 25%.
- Damage mitigation uses the shared formula `afterFlat = max(1, incomingDamage - defense)`, then `finalDamage = max(1, RoundToInt(afterFlat * (1 - armorPercent)))`. Incoming damage less than or equal to 0 applies 0 damage.
- Player and enemy damage both use the same damage application path with target Defense first, then target Armor.
- Enemy Armor is family identity only. It does not grow by wave.
- Player Armor comes from the Armor upgrade track: base cost 18g, 1.55x cost multiplier, +2 percentage points per level, and a hard cap of 40% at level 20. No softcap is used.

## Damage Formula

The shared damage calculation is centralized in `CombatDamage.CalculateAppliedDamage`.

1. Clamp incoming damage to 0 or higher.
2. If incoming damage is 0, apply 0 damage.
3. Apply flat Defense first: `afterFlat = max(1, incomingDamage - defense)`.
4. Apply Armor percent mitigation: `finalDamage = max(1, RoundToInt(afterFlat * (1 - armorPercent)))`.

Current behavior parity:

| Incoming | Defense | Armor | Before | After | Notes |
|---:|---:|---:|---:|---:|---|
| 20 | 0 | 0% | 20 | 20 | Baseline hit. |
| 20 | 5 | 0% | 15 | 15 | Flat Defense only. |
| 20 | 5 | 25% | 15 | 11 | Defense applies first, then Armor: `RoundToInt(15 * 0.75) = 11`. |
| 3 | 5 | 30% | 1 | 1 | Positive incoming damage keeps the minimum 1 damage after mitigation. |
| 0 | 5 | 30% | 0 | 0 | Zero incoming damage stays 0. |
| -5 | 5 | 30% | 0 | 0 | Negative incoming damage stays 0. |

Player Armor parity examples:

| Incoming | Player Defense | Player Armor | Applied damage | Notes |
|---:|---:|---:|---:|---|
| 20 | 5 | 0% | 15 | Same result as the previous player damage-taken path. |
| 20 | 5 | 10% | 14 | Defense first: `20 - 5 = 15`, then `RoundToInt(15 * 0.90) = 14`. |
| 40 | 8 | 40% | 19 | Capped Armor remains useful against high damage and boss hits. |

Future modifiers should enter the pipeline by role:

- Critical damage and outgoing damage multipliers should modify raw outgoing damage before target mitigation.
- Armor penetration should adjust the target Armor value passed into the shared formula.
- Defense penetration or shred should adjust the target Defense value passed into the shared formula.
- Player Armor and enemy Armor both enter as `target.Stats.ArmorPercent`.

## Player Defense And Armor

Defense and Armor now both live on `CombatantStats`, but they are intentionally different defensive tracks:

| Track | Cost | Effect | Primary strength |
|---|---:|---|---|
| Defense | 15g base, 1.60x multiplier | Flat -1 damage per level | Strong against small early hits. |
| Armor | 18g base, 1.55x multiplier | +2 percentage points per level, hard-capped at 40% on level 20 | Stronger for high damage and boss hits. |

Existing saves remain compatible because upgrade levels are loaded by `UpgradeTrack`; saves without an Armor entry initialize Armor level to 0. Reset Save clears Armor with the rest of the upgrade states.

## Wave Table

| Waves | Normal family | Boss |
|---:|---|---|
| W1-W10 | Slime | Boss_Slime |
| W11-W20 | Boar | Boss_Boar |
| W21-W30 | Wisp | Boss_Wisp |
| W31-W40 | Bandit | Boss_Bandit |
| W41-W50 | Golem | Boss_Golem |
| W51-W60 | Ghost | Boss_Ghost |
| W61-W70 | Knight | Boss_Knight |
| W71-W80 | Shaman | Boss_Shaman |
| W81-W90 | Assassin | Boss_Assassin |
| W91-W100 | Drake | Boss_Drake |

The table repeats from W101: W101-W110 is Slime/Boss_Slime, W111-W120 is Boar/Boss_Boar, and so on.

## Family Roles And Future Hooks

| Family | Current role | Future hook |
|---|---|---|
| Slime | Starter baseline with guard-style boss recovery. | Tutorial-safe durability baseline. |
| Boar | Heavy early attacker with low explicit DEF and burst boss pressure. | More readable wind-up/counterplay tuning. |
| Wisp | Fast low-durability enemy using existing threshold boss pressure. | Speed-focused magic identity. |
| Bandit | Fast tempo enemy with frenzy boss pressure. | Later burst/crit-adjacent tuning if needed. |
| Golem | High-HP defensive enemy with high Armor and reflect boss pressure. | Armor and reflect counterplay. |
| Ghost | Fast low-HP enemy. | Evasion/accuracy later. |
| Knight | Defensive enemy with high Armor, low explicit DEF, and low SPD. | Armor penetration later. |
| Shaman | Regeneration enemy with explicit `healthRegenPerSecond`. | Buff system later, regen now. |
| Assassin | Burst danger enemy with high ATK and low HP. | Burst danger. |
| Drake | Aggressive tank with high HP, high ATK, and medium SPD. | 100-wave capstone feel. |

## Spawned Stat Comparison

Normal rows use the final non-boss wave before each block boss. Boss rows use the boss at the end of that same 10-wave block. These are actual spawned stats after the existing wave scaling, family multipliers, boss multipliers, additive DEF/regen, Armor assignment, and APS clamps.

| Family | Normal wave | Normal HP | Normal ATK | Normal APS | Normal DEF | Normal Armor | Normal Regen/s | Normal Gold | Boss wave | Boss HP | Boss ATK | Boss APS | Boss DEF | Boss Armor | Boss Regen/s | Boss Gold | Boss mechanic |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| Slime | W9 | 35 | 2 | 0.70 | 0 | 5% | 0.00 | 12 | W10 | 97 | 2 | 0.85 | 0 | 10% | 0.00 | 52 | GuardRecovery |
| Boar | W19 | 99 | 4 | 0.50 | 1 | 10% | 0.15 | 25 | W20 | 302 | 7 | 0.60 | 2 | 15% | 0.35 | 114 | WindUpBurst |
| Wisp | W29 | 73 | 3 | 1.20 | 0 | 0% | 0.00 | 37 | W30 | 146 | 3 | 1.60 | 0 | 5% | 0.00 | 179 | EnrageThreshold |
| Bandit | W39 | 115 | 5 | 1.05 | 0 | 5% | 0.10 | 62 | W40 | 254 | 7 | 1.40 | 0 | 10% | 0.25 | 328 | FrenzyWindow |
| Golem | W49 | 322 | 6 | 0.45 | 1 | 20% | 0.75 | 91 | W50 | 1,135 | 7 | 0.50 | 2 | 25% | 1.85 | 530 | ReflectWindow |
| Ghost | W59 | 63 | 6 | 1.40 | 0 | 0% | 0.00 | 98 | W60 | 144 | 6 | 1.60 | 0 | 5% | 0.00 | 510 | EnrageThreshold |
| Knight | W69 | 374 | 8 | 0.38 | 1 | 25% | 0.25 | 135 | W70 | 1,216 | 9 | 0.40 | 2 | 30% | 0.75 | 740 | GuardRecovery |
| Shaman | W79 | 284 | 10 | 0.55 | 1 | 10% | 2.50 | 165 | W80 | 734 | 10 | 0.60 | 2 | 15% | 5.90 | 969 | GuardRecovery |
| Assassin | W89 | 146 | 20 | 0.90 | 0 | 0% | 0.00 | 206 | W90 | 309 | 27 | 1.05 | 0 | 5% | 0.10 | 1,290 | WindUpBurst |
| Drake | W99 | 706 | 20 | 0.60 | 1 | 15% | 0.50 | 260 | W100 | 2,602 | 31 | 0.70 | 2 | 20% | 1.70 | 1,788 | FrenzyWindow |

## Armor Table

| Family | Normal Armor ratio | Normal Armor display | Boss Armor ratio | Boss Armor display |
|---|---:|---:|---:|---:|
| Ghost | 0.00 | 0% | 0.05 | 5% |
| Wisp | 0.00 | 0% | 0.05 | 5% |
| Assassin | 0.00 | 0% | 0.05 | 5% |
| Slime | 0.05 | 5% | 0.10 | 10% |
| Bandit | 0.05 | 5% | 0.10 | 10% |
| Boar | 0.10 | 10% | 0.15 | 15% |
| Shaman | 0.10 | 10% | 0.15 | 15% |
| Drake | 0.15 | 15% | 0.20 | 20% |
| Golem | 0.20 | 20% | 0.25 | 25% |
| Knight | 0.25 | 25% | 0.30 | 30% |

## Identity Checks

- Ghost is faster than Wisp in actual final normal stats: W59 Ghost is 1.40 APS and W29 Wisp is 1.20 APS. Ghost HP is also low: W59 Ghost has 63 HP, below W29 Wisp at 73 HP and far below the surrounding late-table families.
- Flat DEF is no longer the main defensive identity lever. Golem, Knight, and Drake keep only small flat DEF values while Armor carries their family identity.
- Knight has explicit Armor and low SPD: W69 Knight has 25% Armor and 0.38 APS; W70 Boss_Knight has 30% Armor and 0.40 APS.
- Shaman has explicit `healthRegenPerSecond`: W79 Shaman has 2.50 regen/s; W80 Boss_Shaman has 5.90 regen/s and reuses GuardRecovery.
- Assassin has high ATK and low HP: W89 Assassin has 20 ATK and 146 HP; W90 Boss_Assassin has 27 ATK and 309 HP.
- Drake has high HP and high ATK with medium SPD: W99 Drake has 706 HP, 20 ATK, and 0.60 APS; W100 Boss_Drake has 2,602 HP, 31 ATK, and 0.70 APS.

## Repeat And Regression Checks

- W1-W50 mapping remains unchanged: Slime, Boar, Wisp, Bandit, and Golem still occupy the same 10-wave blocks and boss IDs.
- W51-W100 extends the table with Ghost, Knight, Shaman, Assassin, and Drake.
- W101 repeats the family table back to Slime; W110 is Boss_Slime, W111 starts Boar, and W120 is Boss_Boar.
- Existing mechanics only are used for new bosses: EnrageThreshold, GuardRecovery, WindUpBurst, and FrenzyWindow.
- HP, ATK, gold, speed, regen, boss mechanics, wave travel, and player skill values are unchanged by Armor.
- The enemy HUD displays non-zero enemy Armor as `Armor N%`. It omits 0% Armor to avoid clutter in the existing compact enemy stat line.
- The player HUD always displays player Armor as `Armor N%`, including 0%, so the Defense/Armor stat line stays explicit.

## Non-Goals

- No evasion/accuracy system yet.
- No buff system yet.
- No new boss mechanic type.
- No new art/assets.
- No player skill changes.
- No Armor softcap.
- No wave travel or Reset Save behavior changes beyond Armor being cleared consistently with other upgrades.
- No HP/ATK/gold wave-scaling formula changes.
