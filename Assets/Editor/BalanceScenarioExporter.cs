using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using IdleGame;
using UnityEditor;
using UnityEngine;

namespace IdleGame.EditorTools
{
    public static class BalanceScenarioExporter
    {
        private const string OutputPath = "docs/balance-scenario-export.md";
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        private static readonly int[] Waves = { 10, 30, 50, 80, 100, 200, 500, 1000, 2000 };

        private static readonly UpgradeModel[] Upgrades =
        {
            new(UpgradeTrack.AttackPower, "ATK", 10, 1.28f, atk: 1),
            new(UpgradeTrack.MaxHealth, "HP", 14, 1.32f, hp: 12),
            new(UpgradeTrack.HealthRegen, "Regen", 16, 1.38f, regen: 0.9f, full: 8, post: 0.65f),
            new(UpgradeTrack.Defense, "DEF", 15, 1.60f, def: 1),
            new(UpgradeTrack.Armor, "Armor", 18, 1.55f, armor: 0.02f, armorCap: 0.40f, max: 20),
            new(UpgradeTrack.AttackSpeed, "SPD", 16, 1.34f, spd: 0.18f),
            new(UpgradeTrack.GoldGain, "Bounty", 18, 1.38f, bounty: 0.18f),
        };

        private static readonly Scenario[] Scenarios =
        {
            new("Balanced", 1f, 1f, 1f, 1f, 1f, 1f, 1f),
            new("Glass Cannon", 2.6f, 0.55f, 0.35f, 0.35f, 0.35f, 2.1f, 0.8f),
            new("Tank", 0.65f, 2.25f, 1.35f, 2.2f, 2.05f, 0.55f, 0.35f),
            new("Sustain", 0.9f, 1.95f, 2.45f, 1.25f, 1.45f, 0.7f, 0.35f),
            new("Speed/Frenzy", 1.85f, 0.85f, 0.55f, 0.7f, 0.7f, 2.75f, 0.55f),
            new("Economy First", 1.3f, 1.15f, 0.4f, 0.45f, 0.45f, 1f, 3.2f),
            new("Boss Killer", 2.25f, 1.35f, 0.65f, 0.85f, 1.2f, 1.1f, 0.35f),
        };

        [MenuItem("Idle Game/Export Balance Scenarios")]
        public static void Export()
        {
            var rows = BuildRows();
            AddComparisonFlags(rows);
            WriteMarkdown(rows);
            AssetDatabase.Refresh();
            Debug.Log($"Balance scenario export written to {OutputPath}");
        }

        private static List<Row> BuildRows()
        {
            var go = new GameObject("BalanceScenarioExporter_EnemyController")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var enemyController = go.AddComponent<EnemyController>();

            try
            {
                var rows = new List<Row>();
                foreach (var scenario in Scenarios)
                {
                    foreach (var wave in Waves)
                    {
                        var route = BuildRoute(scenario, wave, enemyController);
                        rows.Add(BuildRow(scenario, wave, route, enemyController.CreateSpawnDataForWave(wave)));
                    }
                }

                return rows;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static Route BuildRoute(Scenario scenario, int targetWave, EnemyController enemyController)
        {
            var route = new Route();
            Spend(route, scenario);

            for (var defeatedWave = 1; defeatedWave < targetWave; defeatedWave++)
            {
                var spawn = enemyController.CreateSpawnDataForWave(defeatedWave);
                route.BaseEnemyGold += spawn.GoldReward;

                var goldReward = Mathf.Max(1, Mathf.RoundToInt(spawn.GoldReward * route.GoldMultiplier));
                route.BountyGoldDelta += goldReward - spawn.GoldReward;
                route.GoldEarned += goldReward;
                route.Gold += goldReward;

                var reachedWave = defeatedWave + 1;
                if (reachedWave % 5 == 0)
                {
                    var milestoneIndex = reachedWave / 5;
                    var milestoneGold = 30 + (15 * Mathf.Max(0, milestoneIndex - 1));
                    route.MilestoneGold += milestoneGold;
                    route.GoldEarned += milestoneGold;
                    route.Gold += milestoneGold;
                    route.MilestoneAttack++;
                }

                Spend(route, scenario);
            }

            route.PlayerStats = BuildPlayerStats(route);
            return route;
        }

        private static void Spend(Route route, Scenario scenario)
        {
            for (var guard = 0; guard < 20000; guard++)
            {
                var pick = PickUpgrade(route, scenario);
                if (pick == null)
                {
                    return;
                }

                var cost = pick.Cost(route.Level(pick.Track));
                route.Gold -= cost;
                route.GoldSpent += cost;
                route.Levels[pick.Track] = route.Level(pick.Track) + 1;
            }
        }

        private static UpgradeModel PickUpgrade(Route route, Scenario scenario)
        {
            UpgradeModel best = null;
            var bestScore = float.MinValue;
            var median = route.MedianLevel();

            foreach (var upgrade in Upgrades)
            {
                var level = route.Level(upgrade.Track);
                if (upgrade.MaxLevel > 0 && level >= upgrade.MaxLevel)
                {
                    continue;
                }

                if (upgrade.Cost(level) > route.Gold)
                {
                    continue;
                }

                var score = scenario.Weight(upgrade.Track) / Mathf.Pow(level + 1f, 1.08f);
                if (scenario.Name == "Balanced")
                {
                    score -= Mathf.Max(0, level - median - 1) * 0.45f;
                }
                else if (scenario.Name == "Economy First"
                    && upgrade.Track == UpgradeTrack.GoldGain
                    && level < Mathf.Max(4, median + 3))
                {
                    score *= 1.45f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = upgrade;
                }
            }

            return best;
        }

        private static CombatantStats BuildPlayerStats(Route route)
        {
            var stats = new CombatantStats(30, 2, 1f);
            foreach (var upgrade in Upgrades)
            {
                stats = upgrade.Apply(stats, route.Level(upgrade.Track));
            }

            return stats.Add(attackPower: route.MilestoneAttack);
        }

        private static Row BuildRow(Scenario scenario, int wave, Route route, EnemySpawnData enemy)
        {
            var player = route.PlayerStats;
            var enemyStats = enemy.Stats;

            var playerHit = CombatDamage.CalculateAppliedDamage(
                player.AttackPower,
                enemyStats.FlatDamageReduction,
                enemyStats.ArmorPercent);
            var incomingHit = CombatDamage.CalculateAppliedDamage(
                enemyStats.AttackPower,
                player.FlatDamageReduction,
                player.ArmorPercent);

            var rawPlayerDps = playerHit * player.AttacksPerSecond;
            var enemyDamageTakenMultiplier = EnemyDamageTakenMultiplier(enemy.BossMechanic);
            var enemyRecovery = enemyStats.HealthRegenPerSecond
                + EnemyMechanicRecovery(enemy.BossMechanic, enemyStats.MaxHealth);
            var effectivePlayerDps = Mathf.Max(0.01f, (rawPlayerDps * enemyDamageTakenMultiplier) - enemyRecovery);

            var enemyOutgoingMultiplier = EnemyOutgoingMultiplier(enemy.BossMechanic);
            var incomingDps = incomingHit * enemyStats.AttacksPerSecond * enemyOutgoingMultiplier;

            var reflectDps = enemy.BossMechanic.Type == CombatMechanicType.ReflectWindow
                ? ReflectDps(enemy.BossMechanic, playerHit, player.AttacksPerSecond, player.FlatDamageReduction, player.ArmorPercent)
                : 0f;
            var effectiveIncomingDps = incomingDps + reflectDps;

            var noSkillTtk = enemyStats.MaxHealth / effectivePlayerDps;
            var noSkillTtd = Ttd(player.MaxHealth, effectiveIncomingDps, player.HealthRegenPerSecond);

            var burstHit = CombatDamage.CalculateAppliedDamage(
                Mathf.RoundToInt(player.AttackPower * 2.5f),
                enemyStats.FlatDamageReduction,
                enemyStats.ArmorPercent);
            var burstAddedDps = Mathf.Max(0, burstHit - playerHit) / 12f;
            var frenzyUptime = 5f / 23f;
            var frenzyDpsMultiplier = 1f + ((1.6f - 1f) * frenzyUptime);
            var skillDps = Mathf.Max(
                0.01f,
                (((rawPlayerDps * frenzyDpsMultiplier) + burstAddedDps) * enemyDamageTakenMultiplier) - enemyRecovery);
            var skillTtk = enemyStats.MaxHealth / skillDps;

            var guardUptime = 3f / 11f;
            var guardedIncoming = incomingDps * ((guardUptime * 0.68f) + (1f - guardUptime)) + reflectDps;
            var guardRecovery = player.MaxHealth * 0.02f * guardUptime;
            var lastStandHealth = player.MaxHealth * 0.25f;
            var skillTtd = Ttd(player.MaxHealth + lastStandHealth, guardedIncoming, player.HealthRegenPerSecond + guardRecovery);

            var row = new Row
            {
                ScenarioName = scenario.Name,
                Wave = wave,
                EnemyName = enemy.EnemyId,
                BehaviorLabel = enemy.BehaviorLabel,
                Mechanic = enemy.BossMechanic,
                Route = route,
                Player = player,
                Enemy = enemyStats,
                PlayerHit = playerHit,
                IncomingHit = incomingHit,
                EffectivePlayerDps = effectivePlayerDps,
                EffectiveIncomingDps = effectiveIncomingDps,
                NoSkillTtk = noSkillTtk,
                NoSkillTtd = noSkillTtd,
                SkillPlayerDps = skillDps,
                SkillTtk = skillTtk,
                SkillTtd = skillTtd,
                BurstHit = burstHit,
                ReflectDps = reflectDps,
                EnemyRecovery = enemyRecovery,
            };

            row.Flags = BuildBaseFlags(row);
            return row;
        }

        private static string BuildBaseFlags(Row row)
        {
            var flags = new List<string>();
            if (row.NoSkillTtd < row.NoSkillTtk)
            {
                flags.Add("No-skill death-before-kill");
            }

            if (row.SkillTtd < row.SkillTtk)
            {
                flags.Add("Skill-est death-before-kill");
            }

            if (row.ReflectDps > 0f)
            {
                flags.Add("Reflect retaliation visible");
            }

            if (row.PlayerHit <= 1
                && (row.EnemyName.Contains("Golem")
                    || row.EnemyName.Contains("Knight")
                    || row.EnemyName.Contains("Drake")))
            {
                flags.Add("Applied hit at floor");
            }

            var baseDps = Mathf.Max(0.01f, row.PlayerHit * row.Player.AttacksPerSecond);
            if (row.EnemyRecovery / (row.EnemyRecovery + baseDps) > 0.35f)
            {
                flags.Add("Enemy regen share >35%");
            }

            return flags.Count == 0 ? "Review" : string.Join("; ", flags);
        }

        private static float EnemyDamageTakenMultiplier(CombatMechanicDefinition mechanic)
        {
            return mechanic.Type switch
            {
                CombatMechanicType.GuardRecovery => WindowAverage(mechanic, mechanic.DamageTakenMultiplier),
                CombatMechanicType.ReflectWindow => WindowAverage(mechanic, mechanic.DamageTakenMultiplier),
                CombatMechanicType.WindUpBurst => WindowAverage(mechanic, mechanic.DamageTakenMultiplier),
                _ => 1f,
            };
        }

        private static float EnemyOutgoingMultiplier(CombatMechanicDefinition mechanic)
        {
            return mechanic.Type switch
            {
                CombatMechanicType.FrenzyWindow => WindowAverage(
                    mechanic,
                    mechanic.AttackPowerMultiplier * mechanic.AttackSpeedMultiplier),
                CombatMechanicType.EnrageThreshold => mechanic.ThresholdHealthRatio
                    + ((1f - mechanic.ThresholdHealthRatio) * mechanic.AttackPowerMultiplier * mechanic.AttackSpeedMultiplier),
                CombatMechanicType.WindUpBurst => Mathf.Max(1f, mechanic.AttackPowerMultiplier),
                _ => 1f,
            };
        }

        private static float EnemyMechanicRecovery(CombatMechanicDefinition mechanic, int maxHealth)
        {
            if (mechanic.Type != CombatMechanicType.GuardRecovery)
            {
                return 0f;
            }

            var uptime = mechanic.ActiveDuration / Mathf.Max(0.01f, mechanic.CooldownDuration + mechanic.ActiveDuration);
            return maxHealth * mechanic.RecoveryPercentPerSecond * uptime;
        }

        private static float ReflectDps(
            CombatMechanicDefinition mechanic,
            int playerHit,
            float playerAttacksPerSecond,
            int playerDefense,
            float playerArmor)
        {
            var uptime = mechanic.ActiveDuration / Mathf.Max(0.01f, mechanic.CooldownDuration + mechanic.ActiveDuration);
            var reflected = Mathf.RoundToInt((playerHit * mechanic.RetaliationDamageMultiplier) + mechanic.RetaliationFlatDamage);
            var applied = CombatDamage.CalculateAppliedDamage(reflected, playerDefense, playerArmor);
            return applied * playerAttacksPerSecond * uptime;
        }

        private static float WindowAverage(CombatMechanicDefinition mechanic, float activeMultiplier)
        {
            var uptime = mechanic.ActiveDuration / Mathf.Max(0.01f, mechanic.CooldownDuration + mechanic.ActiveDuration);
            return (uptime * activeMultiplier) + (1f - uptime);
        }

        private static float Ttd(float health, float incomingDps, float regen)
        {
            var netIncoming = incomingDps - regen;
            return netIncoming <= 0.01f ? float.PositiveInfinity : health / netIncoming;
        }

        private static void AddComparisonFlags(List<Row> rows)
        {
            var balanced = rows
                .Where(row => row.ScenarioName == "Balanced")
                .ToDictionary(row => row.Wave);
            var glass = rows
                .Where(row => row.ScenarioName == "Glass Cannon")
                .ToDictionary(row => row.Wave);

            foreach (var row in rows)
            {
                if (!balanced.TryGetValue(row.Wave, out var balancedRow))
                {
                    continue;
                }

                row.TtkVsBalanced = row.NoSkillTtk / Mathf.Max(0.01f, balancedRow.NoSkillTtk);
                row.TtdVsBalanced = Ratio(row.NoSkillTtd, balancedRow.NoSkillTtd);
                row.SkillTtkDeltaPercent = PercentDelta(row.NoSkillTtk, row.SkillTtk);
                row.SkillTtdDeltaPercent = PercentDelta(row.SkillTtd, row.NoSkillTtd);

                var flags = new List<string>();
                if (!string.IsNullOrWhiteSpace(row.Flags) && row.Flags != "Review")
                {
                    flags.Add(row.Flags);
                }

                if (row.NoSkillTtd < row.NoSkillTtk * 0.8f)
                {
                    flags.Add("Boss TTD < 80% TTK");
                }

                if (row.ScenarioName == "Glass Cannon"
                    && IsAny(row.Wave, 100, 200, 500)
                    && row.NoSkillTtk > balancedRow.NoSkillTtk * 0.8f)
                {
                    flags.Add("Glass Cannon boss TTK not 20% below Balanced");
                }

                if (row.ScenarioName == "Tank"
                    && IsAny(row.Wave, 100, 500)
                    && row.NoSkillTtd < balancedRow.NoSkillTtd * 1.5f)
                {
                    flags.Add("Tank boss TTD not 50% above Balanced");
                }

                if (row.ScenarioName == "Tank" && row.NoSkillTtk > balancedRow.NoSkillTtk * 3f)
                {
                    flags.Add("Tank TTK > 3x Balanced");
                }

                if (row.ScenarioName == "Speed/Frenzy"
                    && IsAny(row.Wave, 100, 500)
                    && row.SkillTtkDeltaPercent < 10f)
                {
                    flags.Add("Frenzy/Burst TTK lift < 10%");
                }

                if (row.ScenarioName == "Economy First" && row.Wave == 200)
                {
                    var goldAdvantage = (row.Route.GoldEarned - balancedRow.Route.GoldEarned)
                        / Mathf.Max(1f, balancedRow.Route.GoldEarned);
                    if (goldAdvantage > 0.35f
                        && row.NoSkillTtk <= balancedRow.NoSkillTtk
                        && row.NoSkillTtd >= balancedRow.NoSkillTtd)
                    {
                        flags.Add("Economy snowball: >35% gold and parity+ combat");
                    }
                }

                if (row.ScenarioName == "Economy First"
                    && row.Wave == 500
                    && row.NoSkillTtk > balancedRow.NoSkillTtk
                    && row.NoSkillTtd < balancedRow.NoSkillTtd)
                {
                    flags.Add("Economy gold not converting by W500");
                }

                if (row.ScenarioName == "Boss Killer"
                    && IsAny(row.Wave, 50, 100, 500)
                    && row.NoSkillTtk > balancedRow.NoSkillTtk * 0.85f)
                {
                    flags.Add("Boss Killer TTK not 15% below Balanced");
                }

                if (row.ScenarioName == "Speed/Frenzy"
                    && glass.TryGetValue(row.Wave, out var glassRow)
                    && row.NoSkillTtd < glassRow.NoSkillTtd
                    && row.NoSkillTtk > glassRow.NoSkillTtk)
                {
                    flags.Add("Worse survival and TTK than Glass Cannon");
                }

                row.Flags = flags.Count == 0 ? "Review" : string.Join("; ", flags.Distinct());
            }
        }

        private static bool IsAny(int value, params int[] options)
        {
            return Array.IndexOf(options, value) >= 0;
        }

        private static float Ratio(float value, float baseline)
        {
            if (float.IsPositiveInfinity(value) && float.IsPositiveInfinity(baseline))
            {
                return 1f;
            }

            if (float.IsPositiveInfinity(value))
            {
                return 999f;
            }

            return float.IsPositiveInfinity(baseline) ? 0f : value / Mathf.Max(0.01f, baseline);
        }

        private static float PercentDelta(float before, float after)
        {
            if (float.IsPositiveInfinity(before) || Mathf.Approximately(before, 0f))
            {
                return 0f;
            }

            return ((before - after) / before) * 100f;
        }

        private static void WriteMarkdown(List<Row> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Balance Scenario Export");
            builder.AppendLine();
            builder.AppendLine("Generated by `IdleGame.EditorTools.BalanceScenarioExporter.Export`.");
            builder.AppendLine();
            builder.AppendLine("Scope: Unity Editor-only tooling output. This report duplicates current formulas where runtime APIs are private, reuses `EnemyController.CreateSpawnDataForWave` and `CombatDamage.CalculateAppliedDamage` where practical, and does not tune any numbers.");
            builder.AppendLine();
            builder.AppendLine("Skill handling is a first-pass estimate. No-skill baseline and skill-adjusted estimates are kept separate so manual skill value remains visible.");
            builder.AppendLine();
            builder.AppendLine("## PR Body Note");
            builder.AppendLine();
            builder.AppendLine("This is tooling/docs only. No runtime gameplay changes were made.");
            builder.AppendLine();
            builder.AppendLine("## Assumptions");
            builder.AppendLine();
            builder.AppendLine("- Scenario upgrade routing is a deterministic proxy from `docs/balance-scenarios.md`; it is not an optimizer.");
            builder.AppendLine("- Gold comes from defeated waves before each checkpoint, including Bounty-modified enemy rewards and milestone rewards.");
            builder.AppendLine("- TTK includes enemy Defense, Armor, regen, and averaged boss damage-taken windows.");
            builder.AppendLine("- TTD includes player Defense, Armor, regen, averaged boss outgoing windows, Guard, and Last Stand estimates.");
            builder.AppendLine("- Boss mechanics and manual skill windows are averaged for reviewability, not frame-accurate simulation.");
            builder.AppendLine();

            foreach (var group in rows.GroupBy(row => row.ScenarioName))
            {
                builder.AppendLine($"## {group.Key}");
                builder.AppendLine();
                builder.AppendLine("| Wave | Enemy | Mechanic | Levels ATK/HP/Regen/DEF/Armor/SPD/Bounty | Gold earned | Bounty delta | Unspent | Player HP/ATK/DEF/Armor/Regen/SPD | Enemy HP/ATK/DEF/Armor/Regen/SPD | Hit out/in | Eff DPS out | Eff DPS in | TTK no-skill | TTD no-skill | Skill DPS out | TTK skill-est | TTD skill-est | Skill TTK delta | Skill TTD delta | TTK vs Balanced | TTD vs Balanced | Burst hit | Reflect DPS | Max hit %HP | Flags |");
                builder.AppendLine("|---:|---|---|---|---:|---:|---:|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");

                foreach (var row in group.OrderBy(row => row.Wave))
                {
                    builder.AppendLine(row.ToMarkdown());
                }

                builder.AppendLine();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, builder.ToString().Replace("\r\n", "\n").TrimEnd() + "\n", Encoding.UTF8);
        }

        private sealed class Scenario
        {
            private readonly Dictionary<UpgradeTrack, float> weights;

            public Scenario(
                string name,
                float attack,
                float health,
                float regen,
                float defense,
                float armor,
                float speed,
                float bounty)
            {
                Name = name;
                weights = new Dictionary<UpgradeTrack, float>
                {
                    { UpgradeTrack.AttackPower, attack },
                    { UpgradeTrack.MaxHealth, health },
                    { UpgradeTrack.HealthRegen, regen },
                    { UpgradeTrack.Defense, defense },
                    { UpgradeTrack.Armor, armor },
                    { UpgradeTrack.AttackSpeed, speed },
                    { UpgradeTrack.GoldGain, bounty },
                };
            }

            public string Name { get; }

            public float Weight(UpgradeTrack track)
            {
                return weights.TryGetValue(track, out var weight) ? weight : 0f;
            }
        }

        private sealed class Route
        {
            public readonly Dictionary<UpgradeTrack, int> Levels = new();
            public int Gold;
            public int GoldSpent;
            public int BaseEnemyGold;
            public int BountyGoldDelta;
            public int MilestoneGold;
            public int GoldEarned;
            public int MilestoneAttack;
            public CombatantStats PlayerStats;

            public float GoldMultiplier => 1f + (0.18f * Level(UpgradeTrack.GoldGain));

            public int Level(UpgradeTrack track)
            {
                return Levels.TryGetValue(track, out var level) ? level : 0;
            }

            public float MedianLevel()
            {
                var ordered = Upgrades.Select(upgrade => Level(upgrade.Track)).OrderBy(level => level).ToArray();
                return ordered[ordered.Length / 2];
            }

            public string FormatLevels()
            {
                return string.Join("/", Upgrades.Select(upgrade => Level(upgrade.Track).ToString(Culture)));
            }
        }

        private sealed class Row
        {
            public string ScenarioName;
            public int Wave;
            public string EnemyName;
            public string BehaviorLabel;
            public CombatMechanicDefinition Mechanic;
            public Route Route;
            public CombatantStats Player;
            public CombatantStats Enemy;
            public int PlayerHit;
            public int IncomingHit;
            public float EffectivePlayerDps;
            public float EffectiveIncomingDps;
            public float NoSkillTtk;
            public float NoSkillTtd;
            public float SkillPlayerDps;
            public float SkillTtk;
            public float SkillTtd;
            public int BurstHit;
            public float ReflectDps;
            public float EnemyRecovery;
            public float TtkVsBalanced = 1f;
            public float TtdVsBalanced = 1f;
            public float SkillTtkDeltaPercent;
            public float SkillTtdDeltaPercent;
            public string Flags;

            public string ToMarkdown()
            {
                var enemyLabel = string.IsNullOrWhiteSpace(BehaviorLabel)
                    ? EnemyName
                    : $"{EnemyName} [{BehaviorLabel}]";
                var mechanicLabel = Mechanic.IsDefined
                    ? $"{Mechanic.Type}:{Mechanic.DisplayName}"
                    : "None";
                var playerStats = $"{Player.MaxHealth}/{Player.AttackPower}/{Player.FlatDamageReduction}/{Pct(Player.ArmorPercent)}/{Fmt(Player.HealthRegenPerSecond)}/{Fmt(Player.AttacksPerSecond)}";
                var enemyStats = $"{Enemy.MaxHealth}/{Enemy.AttackPower}/{Enemy.FlatDamageReduction}/{Pct(Enemy.ArmorPercent)}/{Fmt(Enemy.HealthRegenPerSecond)}/{Fmt(Enemy.AttacksPerSecond)}";
                var maxHitPercent = IncomingHit / Mathf.Max(1f, Player.MaxHealth) * 100f;

                return string.Join(" | ", new[]
                {
                    $"| W{Wave}",
                    Escape(enemyLabel),
                    Escape(mechanicLabel),
                    Route.FormatLevels(),
                    Route.GoldEarned.ToString("N0", Culture),
                    Route.BountyGoldDelta.ToString("N0", Culture),
                    Route.Gold.ToString("N0", Culture),
                    playerStats,
                    enemyStats,
                    $"{PlayerHit}/{IncomingHit}",
                    Fmt(EffectivePlayerDps),
                    Fmt(EffectiveIncomingDps),
                    Seconds(NoSkillTtk),
                    Seconds(NoSkillTtd),
                    Fmt(SkillPlayerDps),
                    Seconds(SkillTtk),
                    Seconds(SkillTtd),
                    $"{Fmt(SkillTtkDeltaPercent)}%",
                    $"{Fmt(SkillTtdDeltaPercent)}%",
                    Fmt(TtkVsBalanced),
                    Fmt(TtdVsBalanced),
                    BurstHit.ToString(Culture),
                    Fmt(ReflectDps),
                    $"{Fmt(maxHitPercent)}%",
                    Escape(Flags) + " |",
                });
            }
        }

        private sealed class UpgradeModel
        {
            private readonly int attackPower;
            private readonly int health;
            private readonly float speed;
            private readonly int defense;
            private readonly float regen;
            private readonly float armor;
            private readonly float armorCap;
            private readonly int fullEffectLevels;
            private readonly float postSoftCapMultiplier;

            public UpgradeModel(
                UpgradeTrack track,
                string label,
                int startingCost,
                float costMultiplier,
                int atk = 0,
                int hp = 0,
                float spd = 0f,
                int def = 0,
                float bounty = 0f,
                float regen = 0f,
                float armor = 0f,
                float armorCap = 0f,
                int max = 0,
                int full = 0,
                float post = 1f)
            {
                Track = track;
                Label = label;
                StartingCost = startingCost;
                CostMultiplier = costMultiplier;
                attackPower = atk;
                health = hp;
                speed = spd;
                defense = def;
                GoldGainPerLevel = bounty;
                this.regen = regen;
                this.armor = armor;
                this.armorCap = armorCap;
                MaxLevel = max;
                fullEffectLevels = full;
                postSoftCapMultiplier = post;
            }

            public UpgradeTrack Track { get; }
            public string Label { get; }
            public int StartingCost { get; }
            public float CostMultiplier { get; }
            public float GoldGainPerLevel { get; }
            public int MaxLevel { get; }

            public int Cost(int level)
            {
                if (MaxLevel > 0 && level >= MaxLevel)
                {
                    return 0;
                }

                return Mathf.Max(1, Mathf.CeilToInt(StartingCost * Mathf.Pow(CostMultiplier, Mathf.Max(0, level))));
            }

            public CombatantStats Apply(CombatantStats stats, int level)
            {
                if (level <= 0)
                {
                    return stats;
                }

                return Track switch
                {
                    UpgradeTrack.AttackPower => stats.Add(attackPower: attackPower * level),
                    UpgradeTrack.MaxHealth => stats.Add(maxHealth: health * level),
                    UpgradeTrack.Defense => stats.Add(flatDamageReduction: Mathf.RoundToInt(defense * EffectiveLevel(level))),
                    UpgradeTrack.AttackSpeed => stats.Add(attacksPerSecond: speed * level),
                    UpgradeTrack.HealthRegen => stats.Add(healthRegenPerSecond: regen * EffectiveLevel(level)),
                    UpgradeTrack.Armor => stats.Add(armorPercent: armorCap > 0f ? Mathf.Min(armorCap, armor * level) : armor * level),
                    _ => stats,
                };
            }

            private float EffectiveLevel(int level)
            {
                if (fullEffectLevels <= 0 || level <= fullEffectLevels)
                {
                    return Mathf.Max(0, level);
                }

                return fullEffectLevels + ((level - fullEffectLevels) * postSoftCapMultiplier);
            }
        }

        private static string Fmt(float value)
        {
            return float.IsPositiveInfinity(value) ? "Inf" : value.ToString("0.##", Culture);
        }

        private static string Seconds(float value)
        {
            return float.IsPositiveInfinity(value) ? "Inf" : value.ToString("0.##", Culture);
        }

        private static string Pct(float value)
        {
            return $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private static string Escape(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("|", "\\|");
        }
    }
}
