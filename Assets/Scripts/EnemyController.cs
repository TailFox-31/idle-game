using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame
{
    public sealed class EnemyController : MonoBehaviour
    {
        [Serializable]
        private sealed class EnemyArchetypeStage
        {
            [SerializeField, Min(1)]
            private int startWave = EnemyController.FirstWave;

            [SerializeField]
            private string enemyId = "Slime";

            [SerializeField, Min(0.1f)]
            private float healthMultiplier = 1f;

            [SerializeField, Min(0.1f)]
            private float attackMultiplier = 1f;

            [SerializeField, Min(0.1f)]
            private float attackSpeedMultiplier = 1f;

            [SerializeField, Min(0.1f)]
            private float goldMultiplier = 1f;

            [SerializeField, Min(0)]
            private int flatDamageReduction;

            [SerializeField, Min(0f)]
            private float healthRegenPerSecond;

            [SerializeField, Min(0.1f)]
            private float respawnDelayMultiplier = 1f;

            [SerializeField, Min(0f)]
            private float openingAttackDelayMultiplier = 1f;

            public EnemyArchetypeStage()
            {
            }

            public EnemyArchetypeStage(
                int startWave,
                string enemyId,
                float healthMultiplier,
                float attackMultiplier,
                float attackSpeedMultiplier,
                float goldMultiplier,
                int flatDamageReduction,
                float healthRegenPerSecond,
                float respawnDelayMultiplier,
                float openingAttackDelayMultiplier)
            {
                this.startWave = Mathf.Max(FirstWave, startWave);
                this.enemyId = string.IsNullOrWhiteSpace(enemyId) ? "Enemy" : enemyId.Trim();
                this.healthMultiplier = Mathf.Max(0.1f, healthMultiplier);
                this.attackMultiplier = Mathf.Max(0.1f, attackMultiplier);
                this.attackSpeedMultiplier = Mathf.Max(0.1f, attackSpeedMultiplier);
                this.goldMultiplier = Mathf.Max(0.1f, goldMultiplier);
                this.flatDamageReduction = Mathf.Max(0, flatDamageReduction);
                this.healthRegenPerSecond = Mathf.Max(0f, healthRegenPerSecond);
                this.respawnDelayMultiplier = Mathf.Max(0.1f, respawnDelayMultiplier);
                this.openingAttackDelayMultiplier = Mathf.Max(0f, openingAttackDelayMultiplier);
            }

            public int StartWave => Mathf.Max(FirstWave, startWave);

            public string EnemyId => string.IsNullOrWhiteSpace(enemyId) ? "Enemy" : enemyId.Trim();

            public float HealthMultiplier => Mathf.Max(0.1f, healthMultiplier);

            public float AttackMultiplier => Mathf.Max(0.1f, attackMultiplier);

            public float AttackSpeedMultiplier => Mathf.Max(0.1f, attackSpeedMultiplier);

            public float GoldMultiplier => Mathf.Max(0.1f, goldMultiplier);

            public int FlatDamageReduction => Mathf.Max(0, flatDamageReduction);

            public float HealthRegenPerSecond => Mathf.Max(0f, healthRegenPerSecond);

            public float RespawnDelayMultiplier => Mathf.Max(0.1f, respawnDelayMultiplier);

            public float OpeningAttackDelayMultiplier => Mathf.Max(0f, openingAttackDelayMultiplier);
        }

        private sealed class BossFamilyProfile
        {
            public BossFamilyProfile(
                string familyId,
                string behaviorLabel,
                float healthMultiplier,
                float attackMultiplier,
                float attackSpeedMultiplier,
                int flatDamageReduction,
                float healthRegenPerSecond,
                float goldMultiplier,
                float respawnDelayMultiplier,
                float openingAttackDelayMultiplier,
                CombatMechanicDefinition bossMechanic)
            {
                FamilyId = string.IsNullOrWhiteSpace(familyId) ? "Enemy" : familyId.Trim();
                BehaviorLabel = string.IsNullOrWhiteSpace(behaviorLabel) ? "Boss" : behaviorLabel.Trim();
                HealthMultiplier = Mathf.Max(0.1f, healthMultiplier);
                AttackMultiplier = Mathf.Max(0.1f, attackMultiplier);
                AttackSpeedMultiplier = Mathf.Max(0.1f, attackSpeedMultiplier);
                FlatDamageReduction = Mathf.Max(0, flatDamageReduction);
                HealthRegenPerSecond = Mathf.Max(0f, healthRegenPerSecond);
                GoldMultiplier = Mathf.Max(0.1f, goldMultiplier);
                RespawnDelayMultiplier = Mathf.Max(0.1f, respawnDelayMultiplier);
                OpeningAttackDelayMultiplier = Mathf.Max(0f, openingAttackDelayMultiplier);
                BossMechanic = bossMechanic;
            }

            public string FamilyId { get; }

            public string BehaviorLabel { get; }

            public float HealthMultiplier { get; }

            public float AttackMultiplier { get; }

            public float AttackSpeedMultiplier { get; }

            public int FlatDamageReduction { get; }

            public float HealthRegenPerSecond { get; }

            public float GoldMultiplier { get; }

            public float RespawnDelayMultiplier { get; }

            public float OpeningAttackDelayMultiplier { get; }

            public CombatMechanicDefinition BossMechanic { get; }
        }

        private const int FirstWave = 1;
        private const int BossWaveInterval = 10;
        private const int DefaultFamilyCycleWaveLength = 50;
        private const float MaxNormalAttacksPerSecond = 8f;
        private const float MaxBossAttacksPerSecond = 3.5f;
        private static readonly BossFamilyProfile DefaultBossProfile = new("Enemy", "Elite", 2.8f, 1.55f, 1.1f, 0, 0f, 4f, 1f, 1f, CombatMechanicDefinition.None);
        private static readonly BossFamilyProfile[] DefaultBossProfiles =
        {
            new("Slime", "Crown", 2.55f, 1.2f, 0.96f, 0, 0f, 4f, 0.95f, 1f, new CombatMechanicDefinition(CombatMechanicType.GuardRecovery, "Jelly guard", 4f, 2.4f, damageTakenMultiplier: 0.4f, recoveryPercentPerSecond: 0.07f)),
            new("Boar", "Heavy", 2.9f, 1.8f, 0.68f, 1, 0.2f, 4.4f, 1.05f, 1.35f, new CombatMechanicDefinition(CombatMechanicType.WindUpBurst, "Crushing slam", 2.4f, 3.2f, attackPowerMultiplier: 2.6f, damageTakenMultiplier: 1.75f)),
            new("Wisp", "Stormbound", 1.95f, 1f, 1.7f, 0, 0f, 4.7f, 0.75f, 0.35f, new CombatMechanicDefinition(CombatMechanicType.EnrageThreshold, "Storm surge", 0f, 0f, attackPowerMultiplier: 1.4f, attackSpeedMultiplier: 2.1f, thresholdHealthRatio: 0.55f, triggerMode: CombatMechanicTriggerMode.Threshold)),
            new("Bandit", "Cutthroat", 2.15f, 1.35f, 1.55f, 0, 0.15f, 5.2f, 0.65f, 0.5f, new CombatMechanicDefinition(CombatMechanicType.FrenzyWindow, "Flurry", 3.8f, 1.7f, attackPowerMultiplier: 0.95f, attackSpeedMultiplier: 2.6f)),
            new("Golem", "Spined", 3.45f, 1.15f, 0.62f, 3, 1.1f, 5.7f, 1.15f, 1.45f, new CombatMechanicDefinition(CombatMechanicType.ReflectWindow, "Granite spikes", 4.6f, 2.2f, damageTakenMultiplier: 0.6f, retaliationDamageMultiplier: 0.85f, retaliationFlatDamage: 1)),
        };

        [SerializeField]
        private string enemyId = "Slime";

        [SerializeField]
        private CombatantStats baseStats = new CombatantStats(12, 1, 0.5f);

        [SerializeField, Min(0)]
        private int goldReward = 5;

        [SerializeField, Min(0f)]
        private float respawnDelay = 1.25f;

        [Header("Wave Scaling")]
        [SerializeField, Min(0f)]
        private float healthMultiplierPerWave = 0.24f;

        [SerializeField, Min(0f)]
        private float attackMultiplierPerWave = 0.10f;

        [SerializeField, Min(0f)]
        private float attackSpeedPerWave = 0.02f;

        [SerializeField, Min(0f)]
        private float goldMultiplierPerWave = 0.18f;

        [Header("Archetype Progression")]
        [SerializeField]
        private List<EnemyArchetypeStage> archetypeStages = new();

        public EnemySpawnData CreateSpawnData()
        {
            return CreateSpawnDataForWave(FirstWave);
        }

        public EnemySpawnData CreateSpawnDataForWave(int wave)
        {
            var normalizedWave = Mathf.Max(FirstWave, wave);
            var waveOffset = normalizedWave - FirstWave;
            var isBossWave = IsBossWave(normalizedWave);
            var archetypeWave = ResolveRepeatingFamilyWave(isBossWave ? normalizedWave - 1 : normalizedWave);
            var scaledBaseStats = new CombatantStats(
                ScaleInt(baseStats.MaxHealth, healthMultiplierPerWave, waveOffset),
                ScaleInt(baseStats.AttackPower, attackMultiplierPerWave, waveOffset),
                ScaleAttackSpeed(baseStats.AttacksPerSecond, waveOffset));
            var archetype = GetArchetypeForWave(archetypeWave);
            var shapedStats = scaledBaseStats.Multiply(
                archetype.HealthMultiplier,
                archetype.AttackMultiplier,
                archetype.AttackSpeedMultiplier)
                .Add(
                    flatDamageReduction: archetype.FlatDamageReduction,
                    healthRegenPerSecond: archetype.HealthRegenPerSecond);
            var bossProfile = isBossWave ? GetBossProfile(archetype.EnemyId) : DefaultBossProfile;
            var behaviorLabel = string.Empty;
            if (isBossWave)
            {
                shapedStats = shapedStats.Multiply(
                    bossProfile.HealthMultiplier,
                    bossProfile.AttackMultiplier,
                    bossProfile.AttackSpeedMultiplier)
                    .Add(
                        flatDamageReduction: bossProfile.FlatDamageReduction,
                        healthRegenPerSecond: bossProfile.HealthRegenPerSecond);
                behaviorLabel = bossProfile.BehaviorLabel;
            }

            shapedStats = ClampEnemyAttackSpeed(shapedStats, isBossWave);

            var enemyName = isBossWave
                ? BuildBossEnemyId(archetype.EnemyId)
                : archetype.EnemyId;
            var resolvedRespawnDelay = Mathf.Max(
                0f,
                respawnDelay * archetype.RespawnDelayMultiplier * (isBossWave ? bossProfile.RespawnDelayMultiplier : 1f));
            var openingAttackDelay = Mathf.Max(
                0f,
                shapedStats.AttackInterval * archetype.OpeningAttackDelayMultiplier * (isBossWave ? bossProfile.OpeningAttackDelayMultiplier : 1f));
            var reward = Mathf.Max(
                1,
                Mathf.RoundToInt(ScaleInt(goldReward, goldMultiplierPerWave, waveOffset) * archetype.GoldMultiplier * (isBossWave ? bossProfile.GoldMultiplier : 1f)));

            return new EnemySpawnData(
                normalizedWave,
                enemyName,
                shapedStats,
                reward,
                resolvedRespawnDelay,
                behaviorLabel,
                isBossWave ? bossProfile.BossMechanic : CombatMechanicDefinition.None,
                openingAttackDelay);
        }

        private EnemyArchetypeStage GetArchetypeForWave(int wave)
        {
            var stages = GetOrderedArchetypes();
            var selected = stages[0];

            for (var index = 1; index < stages.Count; index++)
            {
                if (wave < stages[index].StartWave)
                {
                    break;
                }

                selected = stages[index];
            }

            return selected;
        }

        private List<EnemyArchetypeStage> GetOrderedArchetypes()
        {
            if (archetypeStages == null || archetypeStages.Count == 0)
            {
                return BuildDefaultArchetypes();
            }

            var validStages = new List<EnemyArchetypeStage>();
            foreach (var stage in archetypeStages)
            {
                if (stage != null)
                {
                    validStages.Add(stage);
                }
            }

            if (validStages.Count == 0)
            {
                return BuildDefaultArchetypes();
            }

            MergeMissingDefaultArchetypes(validStages);
            validStages.Sort((left, right) => left.StartWave.CompareTo(right.StartWave));
            return validStages;
        }

        private List<EnemyArchetypeStage> BuildDefaultArchetypes()
        {
            return new List<EnemyArchetypeStage>
            {
                new EnemyArchetypeStage(FirstWave, enemyId, 1f, 1f, 1f, 1f, 0, 0f, 1f, 0.95f),
                new EnemyArchetypeStage(11, "Boar", 1.55f, 1.45f, 0.72f, 1.18f, 1, 0.15f, 1.08f, 1.3f),
                new EnemyArchetypeStage(21, "Wisp", 0.78f, 0.78f, 1.85f, 1.22f, 0, 0f, 0.82f, 0.42f),
                new EnemyArchetypeStage(31, "Bandit", 0.95f, 1.08f, 1.45f, 1.58f, 0, 0.1f, 0.68f, 0.5f),
                new EnemyArchetypeStage(41, "Golem", 2.15f, 0.95f, 0.55f, 1.9f, 2, 0.75f, 1.15f, 1.4f),
            };
        }

        private void MergeMissingDefaultArchetypes(List<EnemyArchetypeStage> validStages)
        {
            var defaults = BuildDefaultArchetypes();
            foreach (var defaultStage in defaults)
            {
                if (ContainsArchetype(validStages, defaultStage.StartWave, defaultStage.EnemyId))
                {
                    continue;
                }

                validStages.Add(defaultStage);
            }
        }

        private static bool ContainsArchetype(List<EnemyArchetypeStage> stages, int startWave, string enemyArchetypeId)
        {
            foreach (var stage in stages)
            {
                if (stage.StartWave != startWave)
                {
                    continue;
                }

                if (string.Equals(stage.EnemyId, enemyArchetypeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int ScaleInt(int baseValue, float multiplierPerWave, int waveOffset)
        {
            var scaledValue = baseValue * (1f + (multiplierPerWave * Mathf.Max(0, waveOffset)));
            return Mathf.Max(1, Mathf.RoundToInt(scaledValue));
        }

        private static bool IsBossWave(int wave)
        {
            return wave >= BossWaveInterval && wave % BossWaveInterval == 0;
        }

        private static int ResolveRepeatingFamilyWave(int wave)
        {
            var normalizedWave = Mathf.Max(FirstWave, wave);
            var cycleOffset = (normalizedWave - FirstWave) % DefaultFamilyCycleWaveLength;
            return FirstWave + cycleOffset;
        }

        private static CombatantStats ClampEnemyAttackSpeed(CombatantStats stats, bool isBoss)
        {
            var maxAttacksPerSecond = isBoss ? MaxBossAttacksPerSecond : MaxNormalAttacksPerSecond;
            return stats.With(attacksPerSecond: Mathf.Min(stats.AttacksPerSecond, maxAttacksPerSecond));
        }

        private static string BuildBossEnemyId(string enemyArchetypeId)
        {
            var familyId = string.IsNullOrWhiteSpace(enemyArchetypeId) ? "Enemy" : enemyArchetypeId.Trim();
            if (familyId.StartsWith("Boss_", StringComparison.Ordinal))
            {
                return familyId;
            }

            return $"Boss_{familyId}";
        }

        private static BossFamilyProfile GetBossProfile(string enemyArchetypeId)
        {
            var familyId = string.IsNullOrWhiteSpace(enemyArchetypeId)
                ? DefaultBossProfile.FamilyId
                : enemyArchetypeId.Trim();

            foreach (var profile in DefaultBossProfiles)
            {
                if (string.Equals(profile.FamilyId, familyId, StringComparison.Ordinal))
                {
                    return profile;
                }
            }

            return DefaultBossProfile;
        }

        private float ScaleAttackSpeed(float baseAttackSpeed, int waveOffset)
        {
            var scaledValue = baseAttackSpeed + (attackSpeedPerWave * Mathf.Max(0, waveOffset));
            return Mathf.Max(0.1f, scaledValue);
        }
    }
}
