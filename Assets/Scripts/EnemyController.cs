using UnityEngine;

namespace IdleGame
{
    public sealed class EnemyController : MonoBehaviour
    {
        private const int FirstWave = 1;

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

        public EnemySpawnData CreateSpawnData()
        {
            return CreateSpawnDataForWave(FirstWave);
        }

        public EnemySpawnData CreateSpawnDataForWave(int wave)
        {
            var normalizedWave = Mathf.Max(FirstWave, wave);
            var waveOffset = normalizedWave - FirstWave;
            var scaledStats = new CombatantStats(
                ScaleInt(baseStats.MaxHealth, healthMultiplierPerWave, waveOffset),
                ScaleInt(baseStats.AttackPower, attackMultiplierPerWave, waveOffset),
                ScaleAttackSpeed(baseStats.AttacksPerSecond, waveOffset));

            return new EnemySpawnData(
                normalizedWave,
                enemyId,
                scaledStats,
                ScaleInt(goldReward, goldMultiplierPerWave, waveOffset),
                respawnDelay);
        }

        private static int ScaleInt(int baseValue, float multiplierPerWave, int waveOffset)
        {
            var scaledValue = baseValue * (1f + (multiplierPerWave * Mathf.Max(0, waveOffset)));
            return Mathf.Max(1, Mathf.RoundToInt(scaledValue));
        }

        private float ScaleAttackSpeed(float baseAttackSpeed, int waveOffset)
        {
            var scaledValue = baseAttackSpeed + (attackSpeedPerWave * Mathf.Max(0, waveOffset));
            return Mathf.Max(0.1f, scaledValue);
        }
    }
}
