using System;
using UnityEngine;

namespace IdleGame
{
    public readonly struct EnemySpawnData
    {
        public EnemySpawnData(string enemyId, CombatantStats stats, int goldReward, float respawnDelay)
        {
            EnemyId = enemyId;
            Stats = stats;
            GoldReward = Mathf.Max(0, goldReward);
            RespawnDelay = Mathf.Max(0f, respawnDelay);
        }

        public string EnemyId { get; }

        public CombatantStats Stats { get; }

        public int GoldReward { get; }

        public float RespawnDelay { get; }
    }

    public readonly struct BattleSnapshot
    {
        public BattleSnapshot(string enemyId, int playerHealth, int enemyHealth, int enemyMaxHealth, bool enemyAlive, float respawnRemaining)
        {
            EnemyId = enemyId;
            PlayerHealth = playerHealth;
            EnemyHealth = enemyHealth;
            EnemyMaxHealth = enemyMaxHealth;
            EnemyAlive = enemyAlive;
            RespawnRemaining = respawnRemaining;
        }

        public string EnemyId { get; }

        public int PlayerHealth { get; }

        public int EnemyHealth { get; }

        public int EnemyMaxHealth { get; }

        public bool EnemyAlive { get; }

        public float RespawnRemaining { get; }
    }

    public sealed class AutoBattleSystem
    {
        private readonly CombatantRuntime player;
        private readonly CombatantRuntime enemy;
        private EnemySpawnData enemySpawnData;
        private float respawnRemaining;

        public AutoBattleSystem(CombatantStats playerStats, EnemySpawnData initialEnemy)
        {
            player = new CombatantRuntime(playerStats);
            enemySpawnData = initialEnemy;
            enemy = new CombatantRuntime(initialEnemy.Stats);
        }

        public event Action<BattleSnapshot> BattleStateChanged;

        public event Action<int> GoldAwarded;

        public BattleSnapshot Snapshot => BuildSnapshot();

        public void SetPlayerStats(CombatantStats playerStats)
        {
            var currentHealth = Mathf.Clamp(player.CurrentHealth, 0, playerStats.MaxHealth);
            player.Reset(playerStats);
            if (currentHealth < playerStats.MaxHealth)
            {
                player.ReceiveDamage(playerStats.MaxHealth - currentHealth);
            }

            PublishState();
        }

        public void SetEnemy(EnemySpawnData spawnData)
        {
            enemySpawnData = spawnData;
            respawnRemaining = 0f;
            enemy.Reset(spawnData.Stats);
            PublishState();
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var changed = false;
            if (!enemy.IsAlive)
            {
                respawnRemaining = Mathf.Max(0f, respawnRemaining - deltaTime);
                if (respawnRemaining <= 0f)
                {
                    enemy.Reset(enemySpawnData.Stats);
                    changed = true;
                }
            }
            else if (player.TryAttack(deltaTime))
            {
                enemy.ReceiveDamage(player.Stats.AttackPower);
                changed = true;

                if (!enemy.IsAlive)
                {
                    respawnRemaining = enemySpawnData.RespawnDelay;
                    GoldAwarded?.Invoke(enemySpawnData.GoldReward);
                }
            }

            if (changed)
            {
                PublishState();
            }
        }

        private BattleSnapshot BuildSnapshot()
        {
            return new BattleSnapshot(
                enemySpawnData.EnemyId,
                player.CurrentHealth,
                enemy.CurrentHealth,
                enemy.Stats.MaxHealth,
                enemy.IsAlive,
                respawnRemaining);
        }

        private void PublishState()
        {
            BattleStateChanged?.Invoke(BuildSnapshot());
        }
    }
}
