using System;
using UnityEngine;

namespace IdleGame
{
    public readonly struct EnemySpawnData
    {
        public EnemySpawnData(int wave, string enemyId, CombatantStats stats, int goldReward, float respawnDelay, string behaviorLabel, BossMechanicDefinition bossMechanic, float openingAttackDelay = 0f)
        {
            Wave = Mathf.Max(1, wave);
            EnemyId = enemyId;
            Stats = stats;
            GoldReward = Mathf.Max(0, goldReward);
            RespawnDelay = Mathf.Max(0f, respawnDelay);
            BehaviorLabel = string.IsNullOrWhiteSpace(behaviorLabel) ? string.Empty : behaviorLabel.Trim();
            BossMechanic = bossMechanic;
            OpeningAttackDelay = Mathf.Max(0f, openingAttackDelay);
        }

        public int Wave { get; }

        public string EnemyId { get; }

        public CombatantStats Stats { get; }

        public int GoldReward { get; }

        public float RespawnDelay { get; }

        public string BehaviorLabel { get; }

        public BossMechanicDefinition BossMechanic { get; }

        public float OpeningAttackDelay { get; }
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
            string enemyStateLabel,
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
            EnemyStateLabel = enemyStateLabel;
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

        public string EnemyStateLabel { get; }

        public bool EnemyAlive { get; }

        public float EnemyRespawnRemaining { get; }
    }

    public sealed class AutoBattleSystem
    {
        private readonly CombatantRuntime player;
        private readonly CombatantRuntime enemy;
        private readonly BossMechanicRuntime enemyBossMechanic = new();
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
            enemy = new CombatantRuntime(initialEnemy.Stats, initialEnemy.OpeningAttackDelay);
            enemyBossMechanic.Reset(initialEnemy.BossMechanic, initialEnemy.Stats.MaxHealth);
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
            enemy.Reset(spawnData.Stats, spawnData.OpeningAttackDelay);
            enemyBossMechanic.Reset(spawnData.BossMechanic, spawnData.Stats.MaxHealth);
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
                    enemy.Reset(enemySpawnData.Stats, enemySpawnData.OpeningAttackDelay);
                    enemyBossMechanic.Reset(enemySpawnData.BossMechanic, enemySpawnData.Stats.MaxHealth);
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

                    enemy.Reset(enemySpawnData.Stats, enemySpawnData.OpeningAttackDelay);
                    enemyBossMechanic.Reset(enemySpawnData.BossMechanic, enemySpawnData.Stats.MaxHealth);
                    changed = true;
                }
            }
            else
            {
                changed |= player.TryRegenerate(deltaTime);
                changed |= enemyBossMechanic.Tick(deltaTime, enemy);
                var playerAttacks = player.TryAttack(deltaTime);
                var enemyAttacks = enemyBossMechanic.CanAttack && enemy.TryAttack(deltaTime, enemyBossMechanic.GetAttackSpeedMultiplier());
                if (!playerAttacks && !enemyAttacks)
                {
                    if (changed)
                    {
                        PublishState();
                    }

                    return;
                }

                if (playerAttacks)
                {
                    var outgoingPlayerDamage = enemyBossMechanic.ModifyIncomingDamage(player.Stats.AttackPower);
                    var appliedDamage = enemy.ReceiveDamage(outgoingPlayerDamage);
                    var hitReaction = enemyBossMechanic.NotifyIncomingHitResolved(appliedDamage, enemy, player);
                    changed |= hitReaction.StateChanged;
                    changed = true;

                    if (hitReaction.RetaliationDamage > 0 && !player.IsAlive)
                    {
                        playerRespawnRemaining = playerRespawnDelay;
                    }

                    if (!enemy.IsAlive)
                    {
                        enemyRespawnRemaining = enemySpawnData.RespawnDelay;
                        GoldAwarded?.Invoke(enemySpawnData.GoldReward);
                        EnemyDefeated?.Invoke(enemySpawnData);
                    }
                }

                if (enemyAttacks && enemy.IsAlive && player.IsAlive)
                {
                    var outgoingEnemyDamage = enemyBossMechanic.ModifyOutgoingDamage(enemy.Stats.AttackPower);
                    player.ReceiveDamage(outgoingEnemyDamage);
                    changed |= enemyBossMechanic.NotifyAttackResolved();
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
                enemy.IsAlive ? enemyBossMechanic.StateText : string.Empty,
                enemy.IsAlive,
                enemyRespawnRemaining);
        }

        private void PublishState()
        {
            BattleStateChanged?.Invoke(BuildSnapshot());
        }
    }
}
