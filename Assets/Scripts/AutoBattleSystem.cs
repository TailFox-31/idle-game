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
        public BattleSnapshot(
            string enemyId,
            int playerHealth,
            int playerMaxHealth,
            bool playerAlive,
            float playerRespawnRemaining,
            int enemyHealth,
            int enemyMaxHealth,
            bool enemyAlive,
            float enemyRespawnRemaining)
        {
            EnemyId = enemyId;
            PlayerHealth = playerHealth;
            PlayerMaxHealth = playerMaxHealth;
            PlayerAlive = playerAlive;
            PlayerRespawnRemaining = playerRespawnRemaining;
            EnemyHealth = enemyHealth;
            EnemyMaxHealth = enemyMaxHealth;
            EnemyAlive = enemyAlive;
            EnemyRespawnRemaining = enemyRespawnRemaining;
        }

        public string EnemyId { get; }

        public int PlayerHealth { get; }

        public int PlayerMaxHealth { get; }

        public bool PlayerAlive { get; }

        public bool PlayerDefeated => !PlayerAlive;

        public float PlayerRespawnRemaining { get; }

        public int EnemyHealth { get; }

        public int EnemyMaxHealth { get; }

        public bool EnemyAlive { get; }

        public float EnemyRespawnRemaining { get; }
    }

    public sealed class AutoBattleSystem
    {
        private readonly CombatantRuntime player;
        private readonly CombatantRuntime enemy;
        private readonly float playerRespawnDelay;
        private EnemySpawnData enemySpawnData;
        private float enemyRespawnRemaining;
        private float playerRespawnRemaining;

        public AutoBattleSystem(CombatantStats playerStats, EnemySpawnData initialEnemy, float playerRespawnDelay)
        {
            player = new CombatantRuntime(playerStats);
            enemySpawnData = initialEnemy;
            enemy = new CombatantRuntime(initialEnemy.Stats);
            this.playerRespawnDelay = Mathf.Max(0f, playerRespawnDelay);
        }

        public event Action<BattleSnapshot> BattleStateChanged;

        public event Action<int> GoldAwarded;

        public BattleSnapshot Snapshot => BuildSnapshot();

        public void SetPlayerStats(CombatantStats playerStats)
        {
            if (player.IsAlive)
            {
                var missingHealth = Mathf.Max(0, player.Stats.MaxHealth - player.CurrentHealth);
                player.SetStats(playerStats);
                if (missingHealth > 0)
                {
                    player.ReceiveDamage(missingHealth);
                }
            }
            else
            {
                player.SetStats(playerStats);
            }

            PublishState();
        }

        public void SetEnemy(EnemySpawnData spawnData)
        {
            enemySpawnData = spawnData;
            enemyRespawnRemaining = 0f;
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
            if (!player.IsAlive)
            {
                playerRespawnRemaining = Mathf.Max(0f, playerRespawnRemaining - deltaTime);
                if (playerRespawnRemaining <= 0f)
                {
                    player.Reset(player.Stats);
                    enemy.Reset(enemySpawnData.Stats);
                    enemyRespawnRemaining = 0f;
                    changed = true;
                }
            }
            else if (!enemy.IsAlive)
            {
                enemyRespawnRemaining = Mathf.Max(0f, enemyRespawnRemaining - deltaTime);
                if (enemyRespawnRemaining <= 0f)
                {
                    enemy.Reset(enemySpawnData.Stats);
                    changed = true;
                }
            }
            else
            {
                var playerAttacks = player.TryAttack(deltaTime);
                var enemyAttacks = enemy.TryAttack(deltaTime);
                if (!playerAttacks && !enemyAttacks)
                {
                    return;
                }

                if (playerAttacks)
                {
                    enemy.ReceiveDamage(player.Stats.AttackPower);
                    changed = true;

                    if (!enemy.IsAlive)
                    {
                        enemyRespawnRemaining = enemySpawnData.RespawnDelay;
                        GoldAwarded?.Invoke(enemySpawnData.GoldReward);
                    }
                }

                if (enemyAttacks)
                {
                    player.ReceiveDamage(enemy.Stats.AttackPower);
                    changed = true;

                    if (!player.IsAlive)
                    {
                        playerRespawnRemaining = playerRespawnDelay;
                    }
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
                player.Stats.MaxHealth,
                player.IsAlive,
                playerRespawnRemaining,
                enemy.CurrentHealth,
                enemy.Stats.MaxHealth,
                enemy.IsAlive,
                enemyRespawnRemaining);
        }

        private void PublishState()
        {
            BattleStateChanged?.Invoke(BuildSnapshot());
        }
    }
}
