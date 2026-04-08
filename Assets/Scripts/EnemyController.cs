using UnityEngine;

namespace IdleGame
{
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField]
        private string enemyId = "Slime";

        [SerializeField]
        private CombatantStats baseStats = new CombatantStats(12, 1, 0.5f);

        [SerializeField, Min(0)]
        private int goldReward = 5;

        [SerializeField, Min(0f)]
        private float respawnDelay = 1.25f;

        public EnemySpawnData CreateSpawnData()
        {
            return new EnemySpawnData(enemyId, baseStats, goldReward, respawnDelay);
        }
    }
}
