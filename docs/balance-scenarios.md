# Balance Simulation Scenarios

Scope: documentation-only scenario definitions for later balance simulation work. This file does not tune numbers, change gameplay/runtime code, add exporter tooling, or change Unity assets.

Current baseline: use `docs/balance-snapshot.md` as the reference for formulas, enemy family behavior, player upgrade tracks, skill effects, and the current risk areas. Exporter and balance tooling come later; these scenarios define what future exporter output should make reviewable.

## Shared Evaluation Notes

- Use the current baseline formulas from `docs/balance-snapshot.md` until a later tuning PR explicitly changes them.
- Capture both normal-wave and boss-wave results where listed, because high-wave Drake bosses and defensive Golem/Knight bosses stress different parts of the model.
- Track manual skill effects separately from no-skill baseline results so skill value does not disappear behind aggregate DPS or survival averages.
- Record economy routing separately from combat routing. Bounty-first runs can look weak at an early checkpoint and still snowball too hard later.

## Metrics To Capture In Every Scenario

| Metric | Purpose |
|---|---|
| Time to kill (TTK) | Measures whether the build clears each target enemy in a reasonable window. |
| Time to defeat player (TTD) | Measures whether survival is stable or brittle. |
| Net survival margin | Remaining HP, regen contribution, and Last Stand usage at kill time. |
| Damage per second dealt | Separates raw clear speed from fight duration effects. |
| Damage per second taken | Shows incoming pressure after Defense, Armor, Guard, and boss windows. |
| Upgrade levels and gold spent | Makes investment routing comparable across scenarios. |
| Unspent gold | Flags stalled purchase curves or runaway economy. |
| Skill casts, uptime, and prevented damage | Keeps Guard, Last Stand, Burst, and Frenzy visible in exporter output. |
| Boss mechanic windows encountered | Explains TTK/TTD changes caused by GuardRecovery, WindUpBurst, EnrageThreshold, FrenzyWindow, and ReflectWindow. |

## Target Waves

Use these checkpoints unless a later exporter adds a more detailed sweep:

| Checkpoint | Why it matters |
|---:|---|
| W10 | First boss and starter-baseline validation. |
| W30 | Early speed enemy and EnrageThreshold check. |
| W50 | Golem ReflectWindow and defensive enemy check. |
| W80 | Shaman regen and GuardRecovery check. |
| W100 | First Drake capstone boss. |
| W200 | Repeated-table midgame check. |
| W500 | Long-run growth check before Armor cap dominates. |
| W1000 | Late boss-hit-size check. |
| W2000 | Extreme high-wave brittleness check. |

## Scenario 1: Balanced

| Field | Definition |
|---|---|
| Investment priority | Keep ATK, HP, Regen, DEF, Armor, SPD, and Bounty within a narrow band. Prefer no upgrade track more than 2 levels ahead of the median before W100, and no more than 4 levels ahead after W100. |
| Intended play pattern | Generalist route that should clear normal waves smoothly, survive boss mechanics without perfect skill timing, and preserve visible value from all four player skills. |
| Expected strengths | Stable across mixed enemy families, low risk of one-stat dependency, and readable comparisons against the `docs/balance-snapshot.md` even-spend proxy. |
| Expected weaknesses | Should not be the fastest boss killer, the best economy route, or the strongest survival specialist. |
| Target waves to evaluate | W10, W30, W50, W80, W100, W200, W500, W1000, W2000. |
| Metrics to capture | All shared metrics, plus upgrade-level spread and median upgrade level at each checkpoint. |
| Validation questions / failure conditions | Does Balanced remain a credible baseline against every family? Fail if normal-wave TTK is more than 2.0x the previous same-era normal checkpoint without a family-specific explanation. Fail if boss TTD is below boss TTK by more than 20% without Last Stand or Guard recovering the fight. Fail if any skill changes final TTK or TTD by less than 3% across three consecutive boss checkpoints, because that suggests skill value is disappearing from the model. |

## Scenario 2: Glass Cannon

| Field | Definition |
|---|---|
| Investment priority | Prioritize ATK and SPD first, then Burst/Frenzy value through combat stats. Keep HP, Regen, DEF, and Armor deliberately behind Balanced. Bounty is secondary after damage purchases. |
| Intended play pattern | Kill enemies before incoming damage matters. Use Burst and Frenzy to compress boss TTK, especially against high-HP targets. |
| Expected strengths | Fast normal clears, strong boss burst windows, and clear value from ATK and SPD purchases. |
| Expected weaknesses | Vulnerable to high single-hit damage, boss Frenzy windows, WindUpBurst, and ReflectWindow retaliation. |
| Target waves to evaluate | W30, W50, W90, W100, W200, W500, W1000, W2000. |
| Metrics to capture | All shared metrics, plus Burst hit value, Frenzy DPS lift, number of enemy hits taken before kill, and reflected damage on Golem bosses. |
| Validation questions / failure conditions | Does damage investment meaningfully shorten fights? Fail if Glass Cannon boss TTK is not at least 20% lower than Balanced at W100, W200, and W500. Fail if TTD is so low that the player dies before one full skill cycle on two consecutive boss checkpoints. Fail if ReflectWindow retaliation at W50 or W150 makes Burst a net negative with no exporter-visible warning. Fail if high Armor enemies cause applied hit damage to collapse near the 1-damage floor despite heavy ATK investment. |

## Scenario 3: Tank

| Field | Definition |
|---|---|
| Investment priority | Prioritize HP, DEF, and Armor. Add enough Regen to matter between slow boss hits. Keep ATK and SPD below Balanced, with Bounty delayed. |
| Intended play pattern | Absorb boss mechanics, make Guard and Last Stand valuable, and win through survival margin rather than fast kills. |
| Expected strengths | Strong against high incoming damage, boss Frenzy/WindUpBurst spikes, and late Drake hit sizes. |
| Expected weaknesses | Slow clears, possible TTK stalls against regen or high-Armor enemies, and weak economy tempo. |
| Target waves to evaluate | W20, W50, W70, W80, W100, W500, W1000, W2000. |
| Metrics to capture | All shared metrics, plus max hit as percent of HP, mitigation from DEF vs Armor, Guard prevented damage, and Last Stand trigger timing. |
| Validation questions / failure conditions | Does survival investment buy enough time without making the run impossible to finish? Fail if Tank boss TTD is not at least 50% higher than Balanced at W100 and W500. Fail if Tank boss TTK exceeds 3.0x Balanced at the same checkpoint. Fail if Shaman or GuardRecovery enemies heal more than 35% of damage dealt during the fight, causing practical stalemates. Fail if Armor cap causes W1000+ TTD to collapse to within 10% of Glass Cannon despite heavy defensive investment. |

## Scenario 4: Sustain

| Field | Definition |
|---|---|
| Investment priority | Prioritize Regen and HP, then Armor and DEF. Keep ATK moderate and SPD below Balanced unless needed to prevent regen stalemates. Bounty is late. |
| Intended play pattern | Outlast sustained incoming damage through recovery, with Guard smoothing damage and Last Stand acting as backup rather than the main survival plan. |
| Expected strengths | Long normal-wave stability, recovery between slow boss hits, and clear value from Regen levels. |
| Expected weaknesses | Weak against burst damage that exceeds recovery windows, high-wave Drake hits, and enemy regen races. |
| Target waves to evaluate | W20, W40, W70, W80, W100, W200, W1000, W2000. |
| Metrics to capture | All shared metrics, plus total HP regenerated, overheal/wasted regen time, incoming burst size, and time spent below 25% HP. |
| Validation questions / failure conditions | Does Regen remain visible without replacing mitigation? Fail if Regen contributes less than 5% of max HP over three consecutive boss fights when the player survives. Fail if Regen alone makes normal-wave TTD effectively infinite while ATK remains under-invested. Fail if W1000+ bosses kill the player from above 50% HP in one boss mechanic window despite Guard availability, because sustain has no time to function. Fail if Shaman enemy regen causes TTK to exceed 2.5x Balanced. |

## Scenario 5: Speed/Frenzy

| Field | Definition |
|---|---|
| Investment priority | Prioritize SPD first, then ATK. Use Frenzy as the primary skill-value test. Keep HP, DEF, Armor, and Regen near but below Balanced. Bounty is optional after speed breakpoints. |
| Intended play pattern | Convert frequent attacks into smoother DPS, faster Burst consumption, and high Frenzy uptime value without relying on one-shot damage. |
| Expected strengths | Strong against low-Armor and low-HP families, responsive skill windows, and lower variance than pure Glass Cannon. |
| Expected weaknesses | Weak if per-hit damage is too low after enemy Defense/Armor, and vulnerable to high incoming damage because survival lags behind Balanced. |
| Target waves to evaluate | W30, W40, W50, W60, W90, W100, W500, W1000. |
| Metrics to capture | All shared metrics, plus attacks made per fight, effective APS after minimum interval clamps, Frenzy uptime, Frenzy-added attacks, and applied damage per hit. |
| Validation questions / failure conditions | Does speed investment produce real damage instead of hitting caps or mitigation floors? Fail if Frenzy reduces boss TTK by less than 10% at W100 and W500 compared with no-skill Speed/Frenzy output. Fail if effective APS stops increasing while more than two planned SPD purchases are still being made. Fail if applied damage per hit falls to 1 against Golem, Knight, or Drake for more than 25% of attacks. Fail if survival is worse than Glass Cannon while TTK is also worse than Glass Cannon at the same checkpoint. |

## Scenario 6: Economy First

| Field | Definition |
|---|---|
| Investment priority | Buy Bounty aggressively before combat parity, then catch up with ATK, HP, and SPD. Delay DEF, Armor, and Regen unless the route fails survival checks. |
| Intended play pattern | Accept slower and riskier early clears to generate a later gold advantage, then verify the advantage does not overwhelm combat balance. |
| Expected strengths | Higher total gold, faster late upgrade catch-up, and strong long-run scaling if early waves remain survivable. |
| Expected weaknesses | Early boss failures, weak skill value when combat stats lag, and risk of runaway snowball after Bounty compounds. |
| Target waves to evaluate | W10, W30, W50, W100, W200, W500, W1000, W2000. |
| Metrics to capture | All shared metrics, plus cumulative gold earned, Bounty gold delta, time to recover combat parity with Balanced, and upgrade-level lead after W200. |
| Validation questions / failure conditions | Is Bounty useful without becoming mandatory? Fail if Economy First cannot clear W30 or W50 without relying on perfect skill timing. Fail if Economy First has more than 35% total gold advantage over Balanced by W200 and also has equal or better TTK and TTD, because the snowball is too large. Fail if Economy First remains behind Balanced in both TTK and TTD at W500 despite the gold advantage, because Bounty value is not converting into power. Fail if the exporter cannot show Bounty gold delta separately from base gold. |

## Scenario 7: Boss Killer

| Field | Definition |
|---|---|
| Investment priority | Prioritize ATK enough for Burst value, then HP/Armor for boss hit survival, then SPD for Frenzy windows. Keep Bounty low and Regen secondary. |
| Intended play pattern | Optimize for boss checkpoints rather than normal-wave efficiency. Use Burst and Frenzy intentionally, while Guard and Last Stand cover dangerous boss windows. |
| Expected strengths | Strong W50/W100/W200+ boss performance, clear skill-window decision points, and controlled high-wave Drake boss TTK. |
| Expected weaknesses | Less efficient normal-wave farming, possible weakness into ReflectWindow timing, and poor economy compared with Economy First. |
| Target waves to evaluate | W10, W20, W30, W40, W50, W70, W80, W90, W100, W200, W500, W1000, W2000. |
| Metrics to capture | All shared metrics, plus boss TTK rank against Balanced and Glass Cannon, skill-window alignment, boss mechanic damage prevented, and normal-wave TTK penalty. |
| Validation questions / failure conditions | Does boss-focused routing actually solve bosses without trivializing them? Fail if Boss Killer boss TTK is not at least 15% lower than Balanced at W50, W100, and W500. Fail if Boss Killer normal-wave TTK is more than 2.5x Balanced at three consecutive non-boss checkpoints. Fail if W1000 or W2000 Drake boss TTD remains below TTK even with Guard and Last Stand modeled. Fail if Burst value disappears against high-Armor bosses, defined as Burst reducing fight duration by less than 5% at W50, W70, or W100. |

## Future Exporter Requirements

The exporter/tooling is intentionally out of scope for this documentation-only change. A later tooling PR should emit enough structured data to answer the validation questions above without requiring manual combat-log inspection.

Minimum future output:

- Scenario name and target wave.
- Enemy family, boss flag, boss mechanic, and spawned stats.
- Upgrade levels, gold spent, cumulative gold earned, and Bounty gold delta.
- TTK, TTD, winner, and remaining HP.
- Player DPS dealt, incoming DPS taken, and per-hit applied damage.
- Skill casts, skill uptime, damage prevented, damage added, and Last Stand triggers.
- Failure-condition flags with enough context to review why they fired.

## Non-Goals

- No tuning changes.
- No runtime code changes.
- No gameplay code changes.
- No scene, prefab, package, or Unity setting changes.
- No exporter/tooling implementation in this PR.
- No changes to `docs/balance-snapshot.md` baseline formulas.

## PR Body Draft

Documentation-only balance scenario definitions.

- Added `docs/balance-scenarios.md`.
- Defined seven future simulation scenarios: Balanced, Glass Cannon, Tank, Sustain, Speed/Frenzy, Economy First, and Boss Killer.
- Referenced `docs/balance-snapshot.md` as the current baseline.
- Captured investment priorities, intended play patterns, strengths, weaknesses, target waves, metrics, validation questions, and concrete failure conditions for later exporter output.
- Stated that exporter/tooling comes later.

No runtime, gameplay, scene, prefab, package, setting, tuning, or exporter/tooling changes were made.
