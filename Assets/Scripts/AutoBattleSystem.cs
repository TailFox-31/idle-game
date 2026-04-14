using System;
using UnityEngine;

namespace IdleGame
{
    public readonly struct EnemySpawnData
    {
        public EnemySpawnData(int wave, string enemyId, CombatantStats stats, int goldReward, float respawnDelay, string behaviorLabel, CombatMechanicDefinition bossMechanic, float openingAttackDelay = 0f)
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

        public CombatMechanicDefinition BossMechanic { get; }

        public float OpeningAttackDelay { get; }
    }

    public readonly struct SkillSnapshot
    {
        public SkillSnapshot(string displayName, bool isReady, bool isActive, float cooldownRemaining, float activeRemaining, string statusText)
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
            IsReady = isReady;
            IsActive = isActive;
            CooldownRemaining = Mathf.Max(0f, cooldownRemaining);
            ActiveRemaining = Mathf.Max(0f, activeRemaining);
            StatusText = string.IsNullOrWhiteSpace(statusText) ? string.Empty : statusText.Trim();
        }

        public string DisplayName { get; }

        public bool IsReady { get; }

        public bool IsActive { get; }

        public float CooldownRemaining { get; }

        public float ActiveRemaining { get; }

        public string StatusText { get; }
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
            string playerStateLabel,
            SkillSnapshot guardSkill,
            SkillSnapshot lastStandSkill,
            SkillSnapshot burstSkill,
            SkillSnapshot frenzySkill,
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
            PlayerStateLabel = string.IsNullOrWhiteSpace(playerStateLabel) ? string.Empty : playerStateLabel.Trim();
            GuardSkill = guardSkill;
            LastStandSkill = lastStandSkill;
            BurstSkill = burstSkill;
            FrenzySkill = frenzySkill;
            EnemyHealth = enemyHealth;
            EnemyMaxHealth = enemyMaxHealth;
            EnemyAttackPower = enemyAttackPower;
            EnemyAttacksPerSecond = enemyAttacksPerSecond;
            EnemyGoldReward = enemyGoldReward;
            EnemyBehaviorLabel = string.IsNullOrWhiteSpace(enemyBehaviorLabel) ? string.Empty : enemyBehaviorLabel.Trim();
            EnemyStateLabel = string.IsNullOrWhiteSpace(enemyStateLabel) ? string.Empty : enemyStateLabel.Trim();
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

        public string PlayerStateLabel { get; }

        public SkillSnapshot GuardSkill { get; }

        public SkillSnapshot LastStandSkill { get; }

        public SkillSnapshot BurstSkill { get; }

        public SkillSnapshot FrenzySkill { get; }

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
        private const float MinimumPlayerAttackInterval = 0.25f;
        private const float MinimumPlayerFrenzyAttackInterval = 0.20f;

        private readonly CombatantRuntime player;
        private readonly CombatantRuntime enemy;
        private readonly CombatMechanicRuntime enemyCombatMechanic = new();
        private readonly CombatMechanicRuntime playerGuardMechanic = new();
        private readonly CombatMechanicRuntime playerLastStandMechanic = new();
        private readonly CombatMechanicRuntime playerBurstMechanic = new();
        private readonly CombatMechanicRuntime playerFrenzyMechanic = new();
        private readonly float playerRespawnDelay;
        private CombatMechanicDefinition playerGuardDefinition;
        private CombatMechanicDefinition playerLastStandDefinition;
        private CombatMechanicDefinition playerBurstDefinition;
        private CombatMechanicDefinition playerFrenzyDefinition;
        private EnemySpawnData enemySpawnData;
        private EnemySpawnData queuedEnemySpawnData;
        private float enemyRespawnRemaining;
        private bool hasQueuedEnemy;
        private float playerRespawnRemaining;

        public AutoBattleSystem(
            CombatantStats playerStats,
            EnemySpawnData initialEnemy,
            float playerRespawnDelay,
            CombatMechanicDefinition playerGuardDefinition,
            CombatMechanicDefinition playerLastStandDefinition,
            CombatMechanicDefinition playerBurstDefinition,
            CombatMechanicDefinition playerFrenzyDefinition)
        {
            player = new CombatantRuntime(playerStats);
            enemySpawnData = initialEnemy;
            enemy = new CombatantRuntime(initialEnemy.Stats, initialEnemy.OpeningAttackDelay);
            enemyCombatMechanic.Reset(initialEnemy.BossMechanic, initialEnemy.Stats.MaxHealth);
            this.playerRespawnDelay = Mathf.Max(0f, playerRespawnDelay);
            this.playerGuardDefinition = playerGuardDefinition;
            this.playerLastStandDefinition = playerLastStandDefinition;
            this.playerBurstDefinition = playerBurstDefinition;
            this.playerFrenzyDefinition = playerFrenzyDefinition;
            ResetPlayerRuntimeState();
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

            RefreshPlayerMechanicMaxHealth();
            PublishState();
        }

        public void SetPlayerGuardDefinition(CombatMechanicDefinition definition)
        {
            playerGuardDefinition = definition;
            ResetPlayerGuardRuntimeState();
            PublishState();
        }

        public void SetPlayerLastStandDefinition(CombatMechanicDefinition definition)
        {
            playerLastStandDefinition = definition;
            ResetPlayerLastStandRuntimeState();
            PublishState();
        }

        public void SetPlayerBurstDefinition(CombatMechanicDefinition definition)
        {
            playerBurstDefinition = definition;
            ResetPlayerBurstRuntimeState();
            PublishState();
        }

        public void SetPlayerFrenzyDefinition(CombatMechanicDefinition definition)
        {
            playerFrenzyDefinition = definition;
            ResetPlayerFrenzyRuntimeState();
            PublishState();
        }

        public bool TryActivateGuard()
        {
            if (!player.IsAlive || !enemy.IsAlive)
            {
                return false;
            }

            var activated = playerGuardMechanic.TryTrigger();
            if (activated)
            {
                PublishState();
            }

            return activated;
        }

        public bool TryActivateBurst()
        {
            if (!player.IsAlive || !enemy.IsAlive)
            {
                return false;
            }

            var activated = playerBurstMechanic.TryTrigger();
            if (activated)
            {
                PublishState();
            }

            return activated;
        }

        public bool TryActivateFrenzy()
        {
            if (!player.IsAlive || !enemy.IsAlive)
            {
                return false;
            }

            var activated = playerFrenzyMechanic.TryTrigger();
            if (activated)
            {
                PublishState();
            }

            return activated;
        }

        public void SetEnemy(EnemySpawnData spawnData)
        {
            enemySpawnData = spawnData;
            queuedEnemySpawnData = default;
            hasQueuedEnemy = false;
            enemyRespawnRemaining = 0f;
            enemy.Reset(spawnData.Stats, spawnData.OpeningAttackDelay);
            enemyCombatMechanic.Reset(spawnData.BossMechanic, spawnData.Stats.MaxHealth);
            ResetPlayerRuntimeState();
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
                    enemyCombatMechanic.Reset(enemySpawnData.BossMechanic, enemySpawnData.Stats.MaxHealth);
                    enemyRespawnRemaining = 0f;
                    ResetPlayerRuntimeState();
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
                    enemyCombatMechanic.Reset(enemySpawnData.BossMechanic, enemySpawnData.Stats.MaxHealth);
                    ResetPlayerRuntimeState();
                    changed = true;
                }
            }
            else
            {
                changed |= player.TryRegenerate(deltaTime);
                changed |= playerGuardMechanic.Tick(deltaTime, player);
                changed |= playerLastStandMechanic.Tick(deltaTime, player);
                changed |= playerBurstMechanic.Tick(deltaTime, player);
                changed |= playerFrenzyMechanic.Tick(deltaTime, player);
                changed |= enemyCombatMechanic.Tick(deltaTime, enemy);

                var minimumPlayerAttackInterval = playerFrenzyMechanic.IsActive
                    ? MinimumPlayerFrenzyAttackInterval
                    : MinimumPlayerAttackInterval;
                var playerAttacks = player.TryAttack(deltaTime, playerFrenzyMechanic.GetAttackSpeedMultiplier(), minimumPlayerAttackInterval);
                var enemyAttacks = enemyCombatMechanic.CanAttack && enemy.TryAttack(deltaTime, enemyCombatMechanic.GetAttackSpeedMultiplier());
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
                    var burstPlayerDamage = playerBurstMechanic.ModifyOutgoingDamage(player.Stats.AttackPower);
                    var outgoingPlayerDamage = enemyCombatMechanic.ModifyIncomingDamage(burstPlayerDamage);
                    var appliedDamage = enemy.ReceiveDamage(outgoingPlayerDamage);
                    changed |= playerBurstMechanic.NotifyAttackResolved();
                    var hitReaction = enemyCombatMechanic.NotifyIncomingHitResolved(appliedDamage, enemy, player, ApplyIncomingDamageToPlayer);
                    changed |= hitReaction.StateChanged;
                    changed = true;

                    if (hitReaction.RetaliationDamage > 0 && !player.IsAlive)
                    {
                        playerRespawnRemaining = playerRespawnDelay;
                        ResetPlayerRuntimeState();
                    }

                    if (!enemy.IsAlive)
                    {
                        enemyRespawnRemaining = enemySpawnData.RespawnDelay;
                        ResetPlayerRuntimeState();
                        GoldAwarded?.Invoke(enemySpawnData.GoldReward);
                        EnemyDefeated?.Invoke(enemySpawnData);
                    }
                }

                if (enemyAttacks && enemy.IsAlive && player.IsAlive)
                {
                    var outgoingEnemyDamage = enemyCombatMechanic.ModifyOutgoingDamage(enemy.Stats.AttackPower);
                    ApplyIncomingDamageToPlayer(outgoingEnemyDamage);
                    changed |= enemyCombatMechanic.NotifyAttackResolved();
                    changed = true;

                    if (!player.IsAlive)
                    {
                        playerRespawnRemaining = playerRespawnDelay;
                        ResetPlayerRuntimeState();
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
                BuildPlayerStateLabel(),
                BuildGuardSnapshot(),
                BuildLastStandSnapshot(),
                BuildBurstSnapshot(),
                BuildFrenzySnapshot(),
                enemy.CurrentHealth,
                displayedEnemy.Stats.MaxHealth,
                displayedEnemy.Stats.AttackPower,
                displayedEnemy.Stats.AttacksPerSecond,
                displayedEnemy.GoldReward,
                displayedEnemy.BehaviorLabel,
                enemy.IsAlive ? enemyCombatMechanic.StateText : string.Empty,
                enemy.IsAlive,
                enemyRespawnRemaining);
        }

        private void ResetPlayerRuntimeState()
        {
            ResetPlayerGuardRuntimeState();
            ResetPlayerLastStandRuntimeState();
            ResetPlayerBurstRuntimeState();
            ResetPlayerFrenzyRuntimeState();
        }

        private void ResetPlayerGuardRuntimeState()
        {
            playerGuardMechanic.Reset(playerGuardDefinition, player.Stats.MaxHealth);
        }

        private void ResetPlayerLastStandRuntimeState()
        {
            playerLastStandMechanic.Reset(playerLastStandDefinition, player.Stats.MaxHealth);
        }

        private void ResetPlayerBurstRuntimeState()
        {
            playerBurstMechanic.Reset(playerBurstDefinition, player.Stats.MaxHealth);
        }

        private void ResetPlayerFrenzyRuntimeState()
        {
            playerFrenzyMechanic.Reset(playerFrenzyDefinition, player.Stats.MaxHealth);
        }

        private void RefreshPlayerMechanicMaxHealth()
        {
            playerGuardMechanic.SetMaxHealth(player.Stats.MaxHealth);
            playerLastStandMechanic.SetMaxHealth(player.Stats.MaxHealth);
            playerBurstMechanic.SetMaxHealth(player.Stats.MaxHealth);
            playerFrenzyMechanic.SetMaxHealth(player.Stats.MaxHealth);
        }

        private SkillSnapshot BuildGuardSnapshot()
        {
            if (!playerGuardDefinition.IsDefined)
            {
                return default;
            }

            var isActive = player.IsAlive && enemy.IsAlive && playerGuardMechanic.IsActive;
            var cooldownRemaining = player.IsAlive ? playerGuardMechanic.CooldownRemaining : 0f;
            var isReady = player.IsAlive && enemy.IsAlive && playerGuardMechanic.CanTriggerManually;
            var statusText = !player.IsAlive
                ? "Down"
                : !enemy.IsAlive
                    ? "Waiting"
                : isActive
                    ? $"Active {playerGuardMechanic.ActiveRemaining:0.0}s"
                    : cooldownRemaining > 0f
                        ? $"Cooldown {cooldownRemaining:0.0}s"
                        : "Ready";

            return new SkillSnapshot(
                playerGuardDefinition.DisplayName,
                isReady,
                isActive,
                cooldownRemaining,
                playerGuardMechanic.ActiveRemaining,
                statusText);
        }

        private SkillSnapshot BuildLastStandSnapshot()
        {
            if (!playerLastStandDefinition.IsDefined)
            {
                return default;
            }

            var isActive = player.IsAlive && enemy.IsAlive && playerLastStandMechanic.IsActive;
            var cooldownRemaining = player.IsAlive ? playerLastStandMechanic.CooldownRemaining : 0f;
            var isReady = player.IsAlive && enemy.IsAlive && playerLastStandMechanic.CanTriggerLastStand;
            var statusText = !player.IsAlive
                ? "Down"
                : !enemy.IsAlive
                    ? "Waiting"
                    : isActive
                        ? $"Active {playerLastStandMechanic.ActiveRemaining:0.0}s"
                        : cooldownRemaining > 0f
                            ? $"Cooldown {cooldownRemaining:0.0}s"
                            : "Ready";

            return new SkillSnapshot(
                playerLastStandDefinition.DisplayName,
                isReady,
                isActive,
                cooldownRemaining,
                playerLastStandMechanic.ActiveRemaining,
                statusText);
        }

        private SkillSnapshot BuildBurstSnapshot()
        {
            if (!playerBurstDefinition.IsDefined)
            {
                return default;
            }

            var isActive = player.IsAlive && enemy.IsAlive && playerBurstMechanic.IsActive;
            var cooldownRemaining = player.IsAlive ? playerBurstMechanic.CooldownRemaining : 0f;
            var isReady = player.IsAlive && enemy.IsAlive && playerBurstMechanic.CanTriggerManually;
            var statusText = !player.IsAlive
                ? "Down"
                : !enemy.IsAlive
                    ? "Waiting"
                    : isActive
                        ? $"Armed {playerBurstMechanic.ActiveRemaining:0.0}s"
                        : cooldownRemaining > 0f
                            ? $"Cooldown {cooldownRemaining:0.0}s"
                            : "Ready";

            return new SkillSnapshot(
                playerBurstDefinition.DisplayName,
                isReady,
                isActive,
                cooldownRemaining,
                playerBurstMechanic.ActiveRemaining,
                statusText);
        }

        private SkillSnapshot BuildFrenzySnapshot()
        {
            if (!playerFrenzyDefinition.IsDefined)
            {
                return default;
            }

            var isActive = player.IsAlive && enemy.IsAlive && playerFrenzyMechanic.IsActive;
            var cooldownRemaining = player.IsAlive ? playerFrenzyMechanic.CooldownRemaining : 0f;
            var isReady = player.IsAlive && enemy.IsAlive && playerFrenzyMechanic.CanTriggerManually;
            var statusText = !player.IsAlive
                ? "Down"
                : !enemy.IsAlive
                    ? "Waiting"
                    : isActive
                        ? $"Active {playerFrenzyMechanic.ActiveRemaining:0.0}s"
                        : cooldownRemaining > 0f
                            ? $"Cooldown {cooldownRemaining:0.0}s"
                            : "Ready";

            return new SkillSnapshot(
                playerFrenzyDefinition.DisplayName,
                isReady,
                isActive,
                cooldownRemaining,
                playerFrenzyMechanic.ActiveRemaining,
                statusText);
        }

        private string BuildPlayerStateLabel()
        {
            if (!player.IsAlive)
            {
                return string.Empty;
            }

            var guardState = playerGuardMechanic.StateText;
            var lastStandState = playerLastStandMechanic.StateText;
            var burstState = playerBurstMechanic.StateText;
            var frenzyState = playerFrenzyMechanic.StateText;
            var stateLabel = string.Empty;

            if (!string.IsNullOrWhiteSpace(guardState))
            {
                stateLabel = guardState;
            }

            if (!string.IsNullOrWhiteSpace(lastStandState))
            {
                stateLabel = string.IsNullOrWhiteSpace(stateLabel)
                    ? lastStandState
                    : $"{stateLabel} | {lastStandState}";
            }

            if (!string.IsNullOrWhiteSpace(burstState))
            {
                stateLabel = string.IsNullOrWhiteSpace(stateLabel)
                    ? burstState
                    : $"{stateLabel} | {burstState}";
            }

            if (!string.IsNullOrWhiteSpace(frenzyState))
            {
                stateLabel = string.IsNullOrWhiteSpace(stateLabel)
                    ? frenzyState
                    : $"{stateLabel} | {frenzyState}";
            }

            return stateLabel;
        }

        private int ApplyIncomingDamageToPlayer(int incomingDamage)
        {
            var guardedDamage = playerGuardMechanic.ModifyIncomingDamage(incomingDamage);
            var mitigatedDamage = playerLastStandMechanic.ModifyIncomingDamage(guardedDamage);
            var appliedDamage = GetAppliedDamage(player, mitigatedDamage);
            if (appliedDamage >= player.CurrentHealth && playerLastStandMechanic.TryTriggerLastStand(player))
            {
                return 0;
            }

            return player.ReceiveDamage(mitigatedDamage);
        }

        private static int GetAppliedDamage(CombatantRuntime target, int incomingDamage)
        {
            if (target == null || !target.IsAlive)
            {
                return 0;
            }

            var normalizedDamage = Mathf.Max(0, incomingDamage);
            return normalizedDamage <= 0
                ? 0
                : Mathf.Max(1, normalizedDamage - target.Stats.FlatDamageReduction);
        }

        private void PublishState()
        {
            BattleStateChanged?.Invoke(BuildSnapshot());
        }
    }
}
