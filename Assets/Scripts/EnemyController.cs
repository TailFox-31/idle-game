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

        private const int FirstWave = 1;
        private const int BossWaveInterval = 10;
        private const float BossHealthMultiplier = 3.5f;
        private const float BossAttackMultiplier = 1.75f;
        private const float BossAttackSpeedMultiplier = 1.15f;
        private const float BossGoldMultiplier = 4f;

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
        private float healthMultiplierPerWave = 0.35f;

        [SerializeField, Min(0f)]
        private float attackMultiplierPerWave = 0.18f;

        [SerializeField, Min(0f)]
        private float attackSpeedPerWave = 0.03f;

        [SerializeField, Min(0f)]
        private float goldMultiplierPerWave = 0.12f;

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
            var scaledBaseStats = new CombatantStats(
                ScaleInt(baseStats.MaxHealth, healthMultiplierPerWave, waveOffset),
                ScaleInt(baseStats.AttackPower, attackMultiplierPerWave, waveOffset),
                ScaleAttackSpeed(baseStats.AttacksPerSecond, waveOffset));
            var archetype = GetArchetypeForWave(normalizedWave);
            var shapedStats = scaledBaseStats.Multiply(
                archetype.HealthMultiplier,
                archetype.AttackMultiplier,
                archetype.AttackSpeedMultiplier);
            var isBossWave = IsBossWave(normalizedWave);
            if (isBossWave)
            {
                shapedStats = shapedStats.Multiply(
                    BossHealthMultiplier,
                    BossAttackMultiplier,
                    BossAttackSpeedMultiplier);
            }

            var enemyName = isBossWave
                ? BuildBossEnemyId(archetype.EnemyId)
                : archetype.EnemyId;
            var reward = Mathf.Max(
                1,
                Mathf.RoundToInt(ScaleInt(goldReward, goldMultiplierPerWave, waveOffset) * archetype.GoldMultiplier * (isBossWave ? BossGoldMultiplier : 1f)));

            return new EnemySpawnData(
                normalizedWave,
                enemyName,
                shapedStats,
                reward,
                respawnDelay);
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

            validStages.Sort((left, right) => left.StartWave.CompareTo(right.StartWave));
            return validStages;
        }

        private List<EnemyArchetypeStage> BuildDefaultArchetypes()
        {
            return new List<EnemyArchetypeStage>
            {
                new EnemyArchetypeStage(FirstWave, enemyId, 1f, 1f, 1f, 1f),
                new EnemyArchetypeStage(10, "Boar", 1.6f, 1.15f, 0.8f, 1.15f),
                new EnemyArchetypeStage(20, "Wisp", 1.15f, 1.45f, 1.45f, 1.35f),
            };
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

        private float ScaleAttackSpeed(float baseAttackSpeed, int waveOffset)
        {
            var scaledValue = baseAttackSpeed + (attackSpeedPerWave * Mathf.Max(0, waveOffset));
            return Mathf.Max(0.1f, scaledValue);
        }
    }
}
