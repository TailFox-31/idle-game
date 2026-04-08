using System;
using UnityEngine;

namespace IdleGame
{
    public readonly struct EnemySpawnData
    {
        public EnemySpawnData(int wave, string enemyId, CombatantStats stats, int goldReward, float respawnDelay, string behaviorLabel)
        {
            Wave = Mathf.Max(1, wave);
            EnemyId = enemyId;
            Stats = stats;
            GoldReward = Mathf.Max(0, goldReward);
            RespawnDelay = Mathf.Max(0f, respawnDelay);
            BehaviorLabel = string.IsNullOrWhiteSpace(behaviorLabel) ? string.Empty : behaviorLabel.Trim();
        }

        public int Wave { get; }

        public string EnemyId { get; }

        public CombatantStats Stats { get; }

        public int GoldReward { get; }

        public float RespawnDelay { get; }

        public string BehaviorLabel { get; }
    }

    public readonly struct BattleSnapshot
    {
        public BattleSnapshot(
            int wave,
            string enemyId,
            int playerHealth,
            int playerMaxHealth,
            bool playerAlive,
            float playerRespawnRemaining,
            int enemyHealth,
            int enemyMaxHealth,
            int enemyAttackPower,
            float enemyAttacksPerSecond,
            int enemyGoldReward,
            string enemyBehaviorLabel,
            bool enemyAlive,
            float enemyRespawnRemaining)
        {
            Wave = wave;
            EnemyId = enemyId;
            PlayerHealth = playerHealth;
            PlayerMaxHealth = playerMaxHealth;
            PlayerAlive = playerAlive;
            PlayerRespawnRemaining = playerRespawnRemaining;
            EnemyHealth = enemyHealth;
            EnemyMaxHealth = enemyMaxHealth;
            EnemyAttackPower = enemyAttackPower;
            EnemyAttacksPerSecond = enemyAttacksPerSecond;
            EnemyGoldReward = enemyGoldReward;
            EnemyBehaviorLabel = enemyBehaviorLabel;
            EnemyAlive = enemyAlive;
            EnemyRespawnRemaining = enemyRespawnRemaining;
        }

        public int Wave { get; }

        public string EnemyId { get; }

        public int PlayerHealth { get; }

        public int PlayerMaxHealth { get; }

        public bool PlayerAlive { get; }

        public bool PlayerDefeated => !PlayerAlive;

        public float PlayerRespawnRemaining { get; }

        public int EnemyHealth { get; }

        public int EnemyMaxHealth { get; }

        public int EnemyAttackPower { get; }

        public float EnemyAttacksPerSecond { get; }

        public int EnemyGoldReward { get; }

        public string EnemyBehaviorLabel { get; }

        public bool EnemyAlive { get; }

        public float EnemyRespawnRemaining { get; }
    }

    public sealed class AutoBattleSystem
    {
        private readonly CombatantRuntime player;
        private readonly CombatantRuntime enemy;
        private readonly float playerRespawnDelay;
        private EnemySpawnData enemySpawnData;
        private EnemySpawnData queuedEnemySpawnData;
        private float enemyRespawnRemaining;
        private bool hasQueuedEnemy;
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

        public event Action<EnemySpawnData> EnemyDefeated;

        public BattleSnapshot Snapshot => BuildSnapshot();

        public void SetPlayerStats(CombatantStats playerStats)
        {
            if (player.IsAlive)
            {
                var missingHealth = Mathf.Max(0, player.Stats.MaxHealth - player.CurrentHealth);
                player.SetStats(playerStats);
                player.SetCurrentHealth(player.Stats.MaxHealth - missingHealth);
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
            queuedEnemySpawnData = default;
            hasQueuedEnemy = false;
            enemyRespawnRemaining = 0f;
            enemy.Reset(spawnData.Stats);
            PublishState();
        }

        public void QueueEnemy(EnemySpawnData spawnData)
        {
            queuedEnemySpawnData = spawnData;
            hasQueuedEnemy = true;
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
                    if (hasQueuedEnemy)
                    {
                        enemySpawnData = queuedEnemySpawnData;
                        queuedEnemySpawnData = default;
                        hasQueuedEnemy = false;
                    }

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
                    if (hasQueuedEnemy)
                    {
                        enemySpawnData = queuedEnemySpawnData;
                        queuedEnemySpawnData = default;
                        hasQueuedEnemy = false;
                    }

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
                        EnemyDefeated?.Invoke(enemySpawnData);
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
            var displayedEnemy = !enemy.IsAlive && hasQueuedEnemy
                ? queuedEnemySpawnData
                : enemySpawnData;

            return new BattleSnapshot(
                displayedEnemy.Wave,
                displayedEnemy.EnemyId,
                player.CurrentHealth,
                player.Stats.MaxHealth,
                player.IsAlive,
                playerRespawnRemaining,
                enemy.CurrentHealth,
                displayedEnemy.Stats.MaxHealth,
                displayedEnemy.Stats.AttackPower,
                displayedEnemy.Stats.AttacksPerSecond,
                displayedEnemy.GoldReward,
                displayedEnemy.BehaviorLabel,
                enemy.IsAlive,
                enemyRespawnRemaining);
        }

        private void PublishState()
        {
            BattleStateChanged?.Invoke(BuildSnapshot());
        }
    }
}
