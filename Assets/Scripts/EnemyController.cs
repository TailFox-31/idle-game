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

            public EnemyArchetypeStage()
            {
            }

            public EnemyArchetypeStage(
                int startWave,
                string enemyId,
                float healthMultiplier,
                float attackMultiplier,
                float attackSpeedMultiplier,
                float goldMultiplier)
            {
                this.startWave = Mathf.Max(FirstWave, startWave);
                this.enemyId = string.IsNullOrWhiteSpace(enemyId) ? "Enemy" : enemyId.Trim();
                this.healthMultiplier = Mathf.Max(0.1f, healthMultiplier);
                this.attackMultiplier = Mathf.Max(0.1f, attackMultiplier);
                this.attackSpeedMultiplier = Mathf.Max(0.1f, attackSpeedMultiplier);
                this.goldMultiplier = Mathf.Max(0.1f, goldMultiplier);
            }

            public int StartWave => Mathf.Max(FirstWave, startWave);

            public string EnemyId => string.IsNullOrWhiteSpace(enemyId) ? "Enemy" : enemyId.Trim();

            public float HealthMultiplier => Mathf.Max(0.1f, healthMultiplier);

            public float AttackMultiplier => Mathf.Max(0.1f, attackMultiplier);

            public float AttackSpeedMultiplier => Mathf.Max(0.1f, attackSpeedMultiplier);

            public float GoldMultiplier => Mathf.Max(0.1f, goldMultiplier);
        }

        private sealed class BossFamilyProfile
        {
            public BossFamilyProfile(string familyId, string behaviorLabel, float healthMultiplier, float attackMultiplier, float attackSpeedMultiplier, float goldMultiplier)
            {
                FamilyId = string.IsNullOrWhiteSpace(familyId) ? "Enemy" : familyId.Trim();
                BehaviorLabel = string.IsNullOrWhiteSpace(behaviorLabel) ? "Boss" : behaviorLabel.Trim();
                HealthMultiplier = Mathf.Max(0.1f, healthMultiplier);
                AttackMultiplier = Mathf.Max(0.1f, attackMultiplier);
                AttackSpeedMultiplier = Mathf.Max(0.1f, attackSpeedMultiplier);
                GoldMultiplier = Mathf.Max(0.1f, goldMultiplier);
            }

            public string FamilyId { get; }

            public string BehaviorLabel { get; }

            public float HealthMultiplier { get; }

            public float AttackMultiplier { get; }

            public float AttackSpeedMultiplier { get; }

            public float GoldMultiplier { get; }
        }

        private const int FirstWave = 1;
        private const int BossWaveInterval = 10;
        private static readonly BossFamilyProfile DefaultBossProfile = new("Enemy", "Elite", 2.8f, 1.55f, 1.1f, 4f);
        private static readonly BossFamilyProfile[] DefaultBossProfiles =
        {
            new("Slime", "Crown", 2.7f, 1.35f, 1f, 4f),
            new("Boar", "Heavy", 2.6f, 1.45f, 0.8f, 4f),
            new("Wisp", "Frenzy", 2.15f, 1.2f, 1.5f, 4.6f),
            new("Bandit", "Executioner", 2.35f, 1.95f, 0.65f, 5.1f),
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
            var archetypeWave = isBossWave ? normalizedWave - 1 : normalizedWave;
            var scaledBaseStats = new CombatantStats(
                ScaleInt(baseStats.MaxHealth, healthMultiplierPerWave, waveOffset),
                ScaleInt(baseStats.AttackPower, attackMultiplierPerWave, waveOffset),
                ScaleAttackSpeed(baseStats.AttacksPerSecond, waveOffset));
            var archetype = GetArchetypeForWave(archetypeWave);
            var shapedStats = scaledBaseStats.Multiply(
                archetype.HealthMultiplier,
                archetype.AttackMultiplier,
                archetype.AttackSpeedMultiplier);
            var bossProfile = isBossWave ? GetBossProfile(archetype.EnemyId) : DefaultBossProfile;
            var behaviorLabel = string.Empty;
            if (isBossWave)
            {
                shapedStats = shapedStats.Multiply(
                    bossProfile.HealthMultiplier,
                    bossProfile.AttackMultiplier,
                    bossProfile.AttackSpeedMultiplier);
                behaviorLabel = bossProfile.BehaviorLabel;
            }

            var enemyName = isBossWave
                ? BuildBossEnemyId(archetype.EnemyId)
                : archetype.EnemyId;
            var reward = Mathf.Max(
                1,
                Mathf.RoundToInt(ScaleInt(goldReward, goldMultiplierPerWave, waveOffset) * archetype.GoldMultiplier * (isBossWave ? bossProfile.GoldMultiplier : 1f)));

            return new EnemySpawnData(
                normalizedWave,
                enemyName,
                shapedStats,
                reward,
                respawnDelay,
                behaviorLabel);
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
                new EnemyArchetypeStage(FirstWave, enemyId, 1f, 1f, 1f, 1f),
                new EnemyArchetypeStage(11, "Boar", 1.3f, 1.05f, 0.85f, 1.2f),
                new EnemyArchetypeStage(21, "Wisp", 1.1f, 1.25f, 1.3f, 1.45f),
                new EnemyArchetypeStage(31, "Bandit", 1.25f, 1.55f, 1.05f, 1.7f),
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
