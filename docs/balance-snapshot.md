# Balance Snapshot

Scope: documentation-only snapshot of the current main-branch balance state. No runtime code, gameplay code, scene, prefab, package, or Unity setting changes are included.

Base reference: `318e5c3a22ba21e4abea900347afeb7abb19c6c0`.

## Source State

Enemy values are derived from `Assets/Scripts/EnemyController.cs`.

- `waveOffset = wave - 1`
- Base HP: `RoundToInt(12 * (1 + 0.24 * waveOffset))`
- Base ATK: `RoundToInt(1 * (1 + 0.10 * waveOffset))`
- Base SPD: fixed `0.5` attacks/sec before family and boss shaping.
- Base gold: `RoundToInt(5 * (1 + 0.18 * waveOffset))`
- Wave 10 and every 10th wave after it is a boss wave.
- Boss waves use `archetypeWave = wave - 1`, so each block boss uses the preceding family.
- The 100-wave family table repeats after W100. W101 starts Slime again, and W200/W500/W1000/W2000 are Drake bosses.
- Enemy SPD is clamped after shaping: normal enemies max at 8.0 APS, bosses max at 3.5 APS.
- Defense is flat damage reduction. Armor is percent mitigation stored as a ratio.
- Shared damage formula: `afterFlat = max(1, incomingDamage - targetDefense)`, then `finalDamage = max(1, RoundToInt(afterFlat * (1 - targetArmor)))`. Incoming damage <= 0 stays 0.

Player values are derived from `Assets/Scripts/GameManager.cs`, `Assets/Scripts/UpgradeDefinition.cs`, `Assets/Scripts/AutoBattleSystem.cs`, `Assets/Scripts/CombatantStats.cs`, and `Assets/Scripts/BossCombatMechanics.cs`.

- Base player stats: 30 HP, 2 ATK, 1.0 APS, 0 DEF, 0 Armor, 0 regen.
- Attack: +1 ATK/level, cost `Ceil(10 * 1.28^level)`.
- Health: +12 HP/level, cost `Ceil(14 * 1.32^level)`.
- Regen: +0.9/s per effective level, cost `Ceil(16 * 1.38^level)`. Levels after 8 count at 65% effect.
- Defense: +1 flat damage reduction/level, cost `Ceil(15 * 1.60^level)`.
- Armor: +2 percentage points/level, cost `Ceil(18 * 1.55^level)`, max level 20, hard cap 40%.
- Speed: +0.18 APS/level, cost `Ceil(16 * 1.34^level)`.
- Bounty: +18% gold gain/level, cost `Ceil(18 * 1.38^level)`.
- Milestones every 5 waves grant +1 permanent ATK and `30 + 15 * (milestoneIndex - 1)` gold.
- Player attacks use a 0.25s minimum interval, or 0.20s while Frenzy is active.

## Requested Wave Enemy Snapshot

All requested exact waves are boss waves. Normal rows therefore use the preceding non-boss wave as the same-family reference. Values below are spawned stats after wave scaling, family shaping, boss shaping where applicable, Armor/DEF/regen additions, and SPD clamps.

| Tier | Type | Wave | Enemy | HP | ATK | SPD | DEF | Armor | Regen/s | Gold | Mechanic notes |
|---:|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---|
| W50 | Normal ref | 49 | Golem | 322 | 6 | 0.45 | 1 | 20% | 0.75 | 91 | Defensive family; high Armor; slow attacks. |
| W50 | Boss | 50 | Boss_Golem [Spined] | 1,135 | 7 | 0.50 | 2 | 25% | 1.85 | 531 | ReflectWindow: 4.6s cooldown, 2.2s active, damage taken x0.6, retaliation x0.85 + 1 flat. |
| W100 | Normal ref | 99 | Drake | 706 | 20 | 0.60 | 1 | 15% | 0.50 | 260 | Capstone family; high HP and ATK, medium SPD. |
| W100 | Boss | 100 | Boss_Drake [Ashen] | 2,602 | 31 | 0.70 | 2 | 20% | 1.70 | 1,790 | FrenzyWindow: 4.5s cooldown, 2.4s active, ATK x1.25, SPD x1.7. |
| W200 | Normal ref | 199 | Drake | 1,397 | 38 | 0.60 | 1 | 15% | 0.50 | 512 | Repeated W91-W100 Drake family with W199 base scaling. |
| W200 | Boss | 200 | Boss_Drake [Ashen] | 5,125 | 59 | 0.70 | 2 | 20% | 1.70 | 3,503 | Same Drake boss profile; higher base HP/ATK/gold from wave number. |
| W500 | Normal ref | 499 | Drake | 3,470 | 92 | 0.60 | 1 | 15% | 0.50 | 1,268 | SPD remains fixed by family, not wave-scaled. |
| W500 | Boss | 500 | Boss_Drake [Ashen] | 12,695 | 143 | 0.70 | 2 | 20% | 1.70 | 8,644 | Frenzy raises active boss pressure to about 1.19 APS and 179 raw ATK. |
| W1000 | Normal ref | 999 | Drake | 6,926 | 182 | 0.60 | 1 | 15% | 0.50 | 2,528 | Linear HP/ATK/gold growth with fixed Drake SPD. |
| W1000 | Boss | 1000 | Boss_Drake [Ashen] | 25,309 | 282 | 0.70 | 2 | 20% | 1.70 | 17,212 | Current high-wave threat is mostly large single-hit ATK, not runaway APS. |
| W2000 | Normal ref | 1999 | Drake | 13,838 | 362 | 0.60 | 1 | 15% | 0.50 | 5,048 | High raw ATK with low frequency. |
| W2000 | Boss | 2000 | Boss_Drake [Ashen] | 50,538 | 561 | 0.70 | 2 | 20% | 1.70 | 34,348 | Boss Frenzy can make hits very large while still below the boss SPD clamp. |

## Plausible Player Growth Tracks

This is not an optimal purchase simulation. It is a bounded proxy that spends all no-bounty gold available before the target wave evenly across the seven upgrade tracks: ATK, HP, Regen, DEF, Armor, SPD, and Bounty. Milestone ATK through the target wave is included because reaching W50/W100/etc. grants the milestone after defeating the prior wave.

| Tier | Enemy gold before target | Milestone gold through target | Total no-bounty gold | Proxy levels ATK/HP/Regen/DEF/Armor/SPD/Bounty | Player ATK | HP | DEF | Armor | Regen/s | SPD |
|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|
| W50 | 2,500 | 975 | 3,475 | 10 / 9 / 7 / 6 / 6 / 8 / 7 | 22 | 138 | 6 | 12% | 6.30 | 2.44 |
| W100 | 13,977 | 3,450 | 17,427 | 17 / 14 / 12 / 9 / 9 / 13 / 12 | 39 | 198 | 9 | 18% | 9.54 | 3.34 |
| W200 | 51,768 | 12,900 | 64,668 | 22 / 19 / 16 / 12 / 12 / 18 / 16 | 64 | 258 | 12 | 24% | 11.88 | 4.24 |
| W500 | 307,528 | 77,250 | 384,778 | 29 / 25 / 22 / 16 / 16 / 24 / 21 | 131 | 330 | 16 | 32% | 15.39 | 5.32 |
| W1000 | 1,208,463 | 304,500 | 1,512,963 | 35 / 30 / 26 / 19 / 20 / 28 / 26 | 237 | 390 | 19 | 40% | 17.73 | 6.04 |
| W2000 | 4,790,339 | 1,209,000 | 5,999,339 | 40 / 35 / 30 / 22 / 20 / 33 / 30 | 442 | 450 | 22 | 40% | 20.07 | 6.94 |

## Effective Damage Snapshot

Effective player DPS is calculated as `CombatDamage(player ATK, enemy DEF, enemy Armor) * player SPD`. It does not include enemy regen, boss guard/reflect damage-taken windows, player Burst, or player Frenzy.

Effective incoming damage is calculated as `CombatDamage(enemy ATK, player DEF, player Armor) * enemy SPD`. It applies player Defense first and Player Armor after Defense, matching the current shared combat formula. It does not include player Guard, Last Stand, player regen, boss Frenzy windows, or boss WindUp/Enrage modifiers.

| Tier | Target | Player applied hit | Effective player DPS | Enemy applied hit to player | Effective incoming damage/s | Read |
|---:|---|---:|---:|---:|---:|---|
| W50 | W49 Golem | 17 | 41.5 | 1 | 0.5 | Player proxy strongly outscales normal reference survival. |
| W50 | W50 Boss_Golem | 15 | 36.6 | 1 | 0.5 | Boss reflect can punish Burst timing, but base incoming damage is low. |
| W100 | W99 Drake | 32 | 106.9 | 9 | 5.4 | Stable under the proxy build. |
| W100 | W100 Boss_Drake | 30 | 100.2 | 18 | 12.6 | Boss Frenzy spikes this above the baseline row during active windows. |
| W200 | W199 Drake | 54 | 229.0 | 20 | 12.0 | Normal pressure remains controlled. |
| W200 | W200 Boss_Drake | 50 | 212.0 | 36 | 25.2 | Boss is mostly a moderate HP check under this proxy. |
| W500 | W499 Drake | 110 | 585.2 | 52 | 31.2 | Normal enemy damage is low relative to proxy HP and regen. |
| W500 | W500 Boss_Drake | 103 | 548.0 | 86 | 60.2 | Boss TTK is long enough for Frenzy windows to matter. |
| W1000 | W999 Drake | 201 | 1,214.0 | 98 | 58.8 | Normal survival remains plausible, but HP growth is flattening. |
| W1000 | W1000 Boss_Drake | 188 | 1,135.5 | 158 | 110.6 | Boss can remove a large share of player HP per hit. |
| W2000 | W1999 Drake | 375 | 2,602.5 | 204 | 122.4 | Normal reference is survivable by baseline DPS math, but only a few hits matter. |
| W2000 | W2000 Boss_Drake | 352 | 2,442.9 | 323 | 226.1 | Two baseline boss hits threaten the proxy player; Frenzy-modified hits are more severe. |

## Player Skill Effects

| Skill | Current effect | Balance relevance |
|---|---|---|
| Guard | Manual GuardRecovery. 8s cooldown, 3s active, incoming damage x0.68 before Defense/Armor, recovery 2% max HP/s, does not block attacks. | Smooths high single-hit damage. Since it modifies incoming damage before `CombatDamage`, it also improves the value of flat Defense and Armor on the reduced hit. |
| Last Stand | Manual/auto survival trigger when incoming damage would defeat the player. Restores to 25% max HP, lasts 4s, incoming damage x0.75, 60s cooldown. | Prevents one lethal event, but the long cooldown means it cannot solve repeated high-wave boss hits by itself. |
| Burst | Manual PlayerBurst. 12s cooldown, armed for up to 4s, next attack ATK x2.5, then enters cooldown. | Strong against high-Armor bosses only when the post-DEF/post-Armor hit remains high. On ReflectWindow bosses, Burst timing can increase retaliation risk. |
| Frenzy | Manual FrenzyWindow. 18s cooldown, 5s active, player attack speed x1.6, uses 0.20s minimum attack interval while active. | Raises short-window DPS and helps convert large ATK into faster boss kills. It does not directly improve survival. |

## Risk Areas

- The old runaway high-wave risk from wave-scaled enemy SPD is no longer present in current code. Enemy SPD is fixed from base 0.5, then shaped by family/boss multipliers and clamped.
- High-wave Drake bosses are still dangerous because ATK continues to scale linearly while player HP, DEF, regen, and Armor grow through exponentially priced upgrades.
- The Armor cap is reached by the even-spend proxy around W1000. After that, player mitigation only improves through flat DEF, which is expensive and weak against W1000+ boss hit sizes.
- W1000 and W2000 boss fights can become brittle because each hit is a large chunk of player HP. Current baseline incoming DPS looks manageable, but discrete hit damage plus boss Frenzy windows can create sudden deaths.
- Boss mechanics are unevenly represented in the requested high-wave points because the requested exact waves all resolve to Drake bosses. Golem Reflect is visible at W50 only.
- Bounty can change real progression substantially, but it competes with direct survival and DPS tracks. This snapshot does not assume optimized bounty-first routing.
- Regen is modest relative to high-wave boss hit size. It helps between slow hits but does not prevent burst deaths.
- Effective DPS rows omit enemy regen and boss defensive windows. Actual TTK is worse against Golem Reflect and GuardRecovery bosses than the baseline DPS column suggests.

## Candidate Adjustment Options For Later PRs

These are candidates only. This snapshot does not tune numbers.

| Option | Candidate change | Why consider it | Main risk |
|---|---|---|---|
| Preserve fixed enemy SPD | Keep current fixed base SPD and clamps as the high-wave baseline. | It removes the previous quadratic enemy DPS curve and makes high-wave pressure easier to reason about. | Bosses may feel too slow if HP climbs without enough mechanic pressure. |
| Smooth boss hit size | Add era-based boss ATK multipliers or soft caps after defined wave bands. | Reduces two-hit deaths while keeping family SPD identity intact. | May make Armor/Last Stand less valuable if reduced too far. |
| Add Armor-era progression | Add a later-game mitigation track, Armor cap extension, or milestone Armor reward. | Current Armor caps at 40%, leaving W1000+ survival mostly dependent on HP and DEF. | Too much percent mitigation can flatten all high-wave danger. |
| Rebudget Drake boss Frenzy | Tune Drake Frenzy active duration, cooldown, ATK multiplier, or SPD multiplier. | Requested high-wave boss points are all Drake, so this mechanic dominates late snapshot behavior. | Could weaken W100 capstone identity if applied globally. |
| Improve player survival milestones | Add HP, DEF, Armor, or regen milestone rewards in addition to ATK. | Milestone ATK scales linearly forever, but survival milestones do not. | Adds permanent power that may trivialize early waves unless era-gated. |
| Add automated balance export | Add an editor/test-only exporter that prints target wave stats, TTK, TTD, and skill-window variants. | Prevents docs from drifting and makes future tuning PRs reviewable. | Needs care to keep runtime behavior unchanged if implemented as tooling. |

## PR Body Draft

Documentation-only balance snapshot for the current main branch.

- Added `docs/balance-snapshot.md`.
- Captured current enemy formulas, 100-wave family repeat behavior, Enemy Armor, Player Armor, fixed enemy SPD, player growth proxies, skill effects, and requested W50/W100/W200/W500/W1000/W2000 snapshots.
- Included effective player DPS and effective incoming damage using the current `CombatDamage` order: Defense first, Player Armor after Defense.
- Listed risk areas and concrete candidate adjustment options for later PRs.

No runtime, code, scene, prefab, package, setting, or gameplay changes were made.
