# High-Wave Scaling Analysis

Scope: numeric analysis only. No gameplay balance, runtime code, save schema, UI, skill, boss, upgrade, wave travel, or reset-save behavior was changed.

## Source Formulas

Enemy values come from `Assets/Scripts/EnemyController.cs`.

- `waveOffset = wave - 1`
- Base HP: `RoundToInt(12 * (1 + 0.24 * waveOffset))`
- Base ATK: `RoundToInt(1 * (1 + 0.10 * waveOffset))`
- Base attacks/sec: `0.5 + (0.02 * waveOffset)`
- Base gold: `RoundToInt(5 * (1 + 0.18 * waveOffset))`
- Wave 10 and every 10th wave after it is a boss wave.
- Boss waves use `archetypeWave = wave - 1` for family selection.
- Wave 41+ uses the Golem normal archetype:
  - HP x2.15, ATK x0.95, attacks/sec x0.55, gold x1.9
  - Flat damage reduction +2, regen +0.75/s
- Golem bosses use the Spined profile:
  - HP x3.45, ATK x1.15, attacks/sec x0.62, gold x5.7
  - Flat damage reduction +3, regen +1.1/s
  - Reflect mechanic: 4.6s cooldown, 2.2s active, damage taken x0.6, retaliation x0.85 + 1 flat

Player values come from `Assets/Scripts/GameManager.cs`, `Assets/Scripts/UpgradeDefinition.cs`, and `Assets/Scripts/AutoBattleSystem.cs`.

- Base player stats: 30 HP, 2 ATK, 1.0 attacks/sec, 0 DEF, 0 regen.
- Attack upgrade: +1 ATK/level, cost `Ceil(10 * 1.28^level)`.
- Health upgrade: +12 HP/level, cost `Ceil(14 * 1.32^level)`.
- Regen upgrade: +0.9/s per effective level, cost `Ceil(16 * 1.38^level)`.
  - Effective regen level is full through level 8, then 65% per level after that.
- Defense upgrade: +1 flat damage reduction/level, cost `Ceil(15 * 1.60^level)`.
- Speed upgrade: +0.18 attacks/sec/level, cost `Ceil(16 * 1.34^level)`.
- Bounty upgrade: +18% gold gain/level, cost `Ceil(18 * 1.38^level)`.
- Milestone rewards every 5 waves: +1 permanent ATK and `30 + 15 * (milestoneIndex - 1)` gold.
- Player attacks have a normal minimum interval of 0.25s and a Frenzy minimum interval of 0.20s.
- Frenzy is 1.6x attack speed for 5s on an 18s cooldown; the recent Frenzy cap fix is assumed verified.

## Enemy Stat Tables

Waves 100, 500, 1000, and 2000 are all boss waves. The normal enemy row below is therefore the same-tier reference normal from the previous non-boss wave, because no normal enemy actually spawns on the exact listed wave.

| Tier | Normal reference | Normal HP | Normal ATK | Normal APS | Normal gold | Boss enemy | Boss HP | Boss ATK | Boss APS | Boss gold |
|---:|---|---:|---:|---:|---:|---|---:|---:|---:|---:|
| W100 | W99 Golem | 632 | 10 | 1.35 | 177 | W100 Boss_Golem [Spined] | 2,205 | 12 | 0.85 | 1,018 |
| W500 | W499 Golem | 3,109 | 48 | 5.75 | 861 | W500 Boss_Golem [Spined] | 10,747 | 55 | 3.57 | 4,917 |
| W1000 | W999 Golem | 6,205 | 96 | 11.25 | 1,716 | W1000 Boss_Golem [Spined] | 21,428 | 110 | 6.98 | 9,790 |
| W2000 | W1999 Golem | 12,397 | 191 | 22.25 | 3,426 | W2000 Boss_Golem [Spined] | 42,790 | 220 | 13.80 | 19,541 |

Approximate enemy outgoing DPS before player DEF and skills:

| Tier | Normal reference DPS | Boss DPS |
|---:|---:|---:|
| W100 | 13.5/s | 10.2/s |
| W500 | 276.0/s | 196.4/s |
| W1000 | 1,080.0/s | 767.8/s |
| W2000 | 4,249.8/s | 3,036.0/s |

Gold progression if each prior wave is defeated once and no bounty multiplier is purchased:

| Target wave | Enemy gold before target | Milestone gold through target | Total before bounty |
|---:|---:|---:|---:|
| W100 | 12,251 | 3,450 | 15,701 |
| W500 | 316,485 | 77,250 | 393,735 |
| W1000 | 1,262,360 | 304,500 | 1,566,860 |
| W2000 | 5,039,387 | 1,209,000 | 6,248,387 |

## Player-Side Comparison

Exact purchase simulation is not practical from the current formulas alone because it depends on player upgrade order, farming behavior, death loops, bounty timing, skill usage, and whether the editor jump tools were used. The estimates below use two bounded assumptions:

- Editor jump / no purchases: the player receives milestone attack through the jumped wave but has no upgrade levels.
- Even spending proxy: total no-bounty gold before the target is split evenly across the six upgrade tracks. This is not an optimal build; it is a rough middle case for comparing growth shapes.

| Tier | Scenario | HP | ATK | DEF | Regen/s | APS | Player DPS | Survival read |
|---:|---|---:|---:|---:|---:|---:|---:|---|
| W100 | Jump/no purchases | 30 | 22 | 0 | 0.0 | 1.00 | 22.0 | Boss deals about 10.2 DPS; playable only because early boss ATK is low. |
| W100 | Even spending proxy | 198 | 39 | 9 | 9.5 | 3.34 | 130.3 | Player can likely survive W100 boss outside reflect mistakes. |
| W500 | Jump/no purchases | 30 | 102 | 0 | 0.0 | 1.00 | 102.0 | Boss deals about 196 DPS; player dies almost immediately. |
| W500 | Even spending proxy | 342 | 132 | 16 | 15.4 | 5.32 | 702.2 | Boss post-DEF damage is about 139 DPS, survivable for a short window with skills. |
| W1000 | Jump/no purchases | 30 | 202 | 0 | 0.0 | 1.00 | 202.0 | Boss deals about 768 DPS; impossible survival. |
| W1000 | Even spending proxy | 402 | 238 | 19 | 18.3 | 6.22 | 1,480.4 | Boss post-DEF damage is about 635 DPS; time-to-death is below 1s without strong skill coverage. |
| W2000 | Jump/no purchases | 30 | 402 | 0 | 0.0 | 1.00 | 402.0 | Boss deals about 3,036 DPS; one or two hits end the fight. |
| W2000 | Even spending proxy | 462 | 443 | 22 | 20.7 | 7.12 | 3,154.2 | Player DPS can kill the boss in about 14s, but boss post-DEF damage is about 2,732 DPS. Survival fails first. |

Maximum single-track levels affordable with the same no-bounty budget show why high-wave player stats remain modest. Spending all W2000 no-bounty gold on one track buys only about Attack L48, Health L42, Regen L36, Defense L26, Speed L40, or Bounty L36. Since real builds must split spending across several tracks, HP/DEF/regen cannot keep up with enemy attack throughput.

## Scaling Diagnosis

Enemy HP and enemy ATK are linear in wave, with piecewise multipliers from the active archetype and boss family. Enemy attacks/sec is also linear in wave. That means enemy outgoing damage per second is approximately:

`enemyDps ~= linearATK(wave) * linearAPS(wave)`

So the dangerous combat pressure is effectively quadratic, not linear. The Golem boss multipliers do not create the curve by themselves; they multiply the already high base values. At W2000, the Golem boss is `220 ATK * 13.80 APS = 3,036 DPS` before DEF and skill effects.

Player growth is structurally different:

- Combat stats gained from upgrades are additive per level.
- Upgrade costs are exponential.
- Milestone ATK is linear at +1 per 5 waves.
- Bounty can improve gold income, but bounty itself also has exponential costs and does not directly improve survival.

The wave 2000 spike is therefore caused primarily by `attackSpeedPerWave = 0.02` stacking with `attackMultiplierPerWave = 0.10`. The boss attack-speed multiplier of `0.62` reduces Golem boss speed relative to normal Golems, but not enough once the base speed reaches `0.5 + 0.02 * 1999 = 40.48 APS` before archetype and boss shaping.

The excessive point appears before W1000 for survival. By W500, a balanced build can plausibly survive briefly. By W1000, boss post-DEF DPS is already higher than plausible player max HP per second, and by W2000 the fight is decided by incoming damage long before player DPS can matter.

## Adjustment Options

### 1. Conservative near-term playability tweak

Keep HP, ATK, gold, archetype identity, and boss mechanics mostly intact, but reduce or cap high-wave enemy attack speed.

Options to evaluate:

- Lower `attackSpeedPerWave` from `0.02` to a much smaller value such as `0.004` to `0.006`.
- Add a soft cap after a defined wave, for example full speed scaling through W300, then 25% speed scaling after W300.
- Add an enemy APS clamp for prototype testing, for example a normal cap around 6-8 APS and boss cap around 4-6 APS.

Why this is conservative: it targets the quadratic incoming-DPS source without immediately nerfing enemy HP, rewards, wave travel, upgrades, skills, or boss definitions. It also keeps W100 behavior close to current values while reducing the W1000/W2000 survival wall.

Main risk: if only enemy APS is reduced, high-wave bosses may become DPS checks rather than survival checks. That is acceptable for a short-term smoke target, but it should be measured.

### 2. Structural longer-term scaling pass

Define separate target curves for time-to-kill and time-to-death instead of deriving both from raw wave multipliers.

Recommended structure:

- Split enemy scaling into eras, for example W1-100, W101-500, W501-1000, W1001+.
- Use a curve asset or table-backed formula for HP, ATK, APS, gold, and boss multipliers.
- Keep enemy HP growth tied to expected player DPS and enemy ATK/APS tied to expected player effective health.
- Convert defense/regen pressure into an effective incoming-DPS budget, not independent flat numbers.
- Add automated balance snapshots for target waves that print player estimates, enemy stats, TTK, and TTD.

Why this is structural: it stops high-wave balance from depending on several independent linear terms that multiply into a quadratic combat result. It also gives future boss mechanics a stable budget instead of stacking them on top of unbounded base stats.

Main risk: this requires choosing progression goals first, such as desired boss time-to-kill, expected farming count, and whether wave travel should represent earned progression or debug traversal.

## Recommended Next Implementation Step

Implement a non-runtime editor or test-only balance snapshot that reproduces `EnemyController.CreateSpawnDataForWave` and player stat estimates for W100, W500, W1000, and W2000, then use it to validate one conservative enemy APS soft-cap proposal. Do not change live balance values until the snapshot shows target time-to-kill and time-to-death ranges for at least W500, W1000, and W2000.
