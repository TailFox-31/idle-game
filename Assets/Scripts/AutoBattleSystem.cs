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

    public enum BattleCombatantSide
    {
        None = 0,
        Player = 1,
        Enemy = 2,
    }

    public enum BattleFlowHookPoint
    {
        TickStarted = 0,
        BeforePlayerAttackResolved = 1,
        AfterPlayerAttackResolved = 2,
        BeforeEnemyAttackResolved = 3,
        AfterEnemyAttackResolved = 4,
        PlayerDefeated = 5,
        EnemyDefeated = 6,
        PlayerRespawned = 7,
        EnemyRespawned = 8,
        TickCompleted = 9,
    }

    public readonly struct BattleFlowContext
    {
        public BattleFlowContext(
            BattleFlowHookPoint hookPoint,
            int wave,
            float deltaTime,
            BattleCombatantSide actor,
            BattleCombatantSide target,
            int attemptedDamage,
            int appliedDamage)
        {
            HookPoint = hookPoint;
            Wave = Mathf.Max(0, wave);
            DeltaTime = Mathf.Max(0f, deltaTime);
            Actor = actor;
            Target = target;
            AttemptedDamage = Mathf.Max(0, attemptedDamage);
            AppliedDamage = Mathf.Max(0, appliedDamage);
        }

        public BattleFlowHookPoint HookPoint { get; }

        public int Wave { get; }

        public float DeltaTime { get; }

        public BattleCombatantSide Actor { get; }

        public BattleCombatantSide Target { get; }

        public int AttemptedDamage { get; }

        public int AppliedDamage { get; }
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
            float enemyArmorPercent,
            string enemyBehaviorLabel,
            string enemyStateLabel,
            bool enemyAlive,
            float enemyRespawnRemaining)
            : this(
                wave,
                enemyId,
                playerHealth,
                playerMaxHealth,
                playerAlive,
                playerRespawnRemaining,
                playerStateLabel,
                Array.Empty<CombatRuntimeStatus>(),
                guardSkill,
                lastStandSkill,
                burstSkill,
                frenzySkill,
                enemyHealth,
                enemyMaxHealth,
                enemyAttackPower,
                enemyAttacksPerSecond,
                enemyGoldReward,
                enemyArmorPercent,
                enemyBehaviorLabel,
                enemyStateLabel,
                Array.Empty<CombatRuntimeStatus>(),
                enemyAlive,
                enemyRespawnRemaining)
        {
        }

        public BattleSnapshot(
            int wave,
            string enemyId,
            int playerHealth,
            int playerMaxHealth,
            bool playerAlive,
            float playerRespawnRemaining,
            string playerStateLabel,
            CombatRuntimeStatus[] playerStatuses,
            SkillSnapshot guardSkill,
            SkillSnapshot lastStandSkill,
            SkillSnapshot burstSkill,
            SkillSnapshot frenzySkill,
            int enemyHealth,
            int enemyMaxHealth,
            int enemyAttackPower,
            float enemyAttacksPerSecond,
            int enemyGoldReward,
            float enemyArmorPercent,
            string enemyBehaviorLabel,
            string enemyStateLabel,
            CombatRuntimeStatus[] enemyStatuses,
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
            PlayerStatuses = playerStatuses ?? Array.Empty<CombatRuntimeStatus>();
            GuardSkill = guardSkill;
            LastStandSkill = lastStandSkill;
            BurstSkill = burstSkill;
            FrenzySkill = frenzySkill;
            EnemyHealth = enemyHealth;
            EnemyMaxHealth = enemyMaxHealth;
            EnemyAttackPower = enemyAttackPower;
            EnemyAttacksPerSecond = enemyAttacksPerSecond;
            EnemyGoldReward = enemyGoldReward;
            EnemyArmorPercent = Mathf.Clamp01(enemyArmorPercent);
            EnemyBehaviorLabel = string.IsNullOrWhiteSpace(enemyBehaviorLabel) ? string.Empty : enemyBehaviorLabel.Trim();
            EnemyStateLabel = string.IsNullOrWhiteSpace(enemyStateLabel) ? string.Empty : enemyStateLabel.Trim();
            EnemyStatuses = enemyStatuses ?? Array.Empty<CombatRuntimeStatus>();
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

        public CombatRuntimeStatus[] PlayerStatuses { get; }

        public SkillSnapshot GuardSkill { get; }

        public SkillSnapshot LastStandSkill { get; }

        public SkillSnapshot BurstSkill { get; }

        public SkillSnapshot FrenzySkill { get; }

        public int EnemyHealth { get; }

        public int EnemyMaxHealth { get; }

        public int EnemyAttackPower { get; }

        public float EnemyAttacksPerSecond { get; }

        public int EnemyGoldReward { get; }

        public float EnemyArmorPercent { get; }

        public string EnemyBehaviorLabel { get; }

        public string EnemyStateLabel { get; }

        public CombatRuntimeStatus[] EnemyStatuses { get; }

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

        public event Action<BattleFlowContext> BattleFlowReached;

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

            NotifyBattleFlow(BattleFlowHookPoint.TickStarted, deltaTime);
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
                    NotifyBattleFlow(BattleFlowHookPoint.PlayerRespawned, deltaTime);
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
                    NotifyBattleFlow(BattleFlowHookPoint.EnemyRespawned, deltaTime);
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
                    NotifyBattleFlow(BattleFlowHookPoint.TickCompleted, deltaTime);
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
                    NotifyBattleFlow(
                        BattleFlowHookPoint.BeforePlayerAttackResolved,
                        deltaTime,
                        BattleCombatantSide.Player,
                        BattleCombatantSide.Enemy,
                        outgoingPlayerDamage);
                    var appliedDamage = ApplyIncomingDamage(enemy, outgoingPlayerDamage);
                    changed |= playerBurstMechanic.NotifyAttackResolved();
                    var hitReaction = enemyCombatMechanic.NotifyIncomingHitResolved(appliedDamage, enemy, player, ApplyIncomingDamageToPlayer);
                    changed |= hitReaction.StateChanged;
                    NotifyBattleFlow(
                        BattleFlowHookPoint.AfterPlayerAttackResolved,
                        deltaTime,
                        BattleCombatantSide.Player,
                        BattleCombatantSide.Enemy,
                        outgoingPlayerDamage,
                        appliedDamage);
                    changed = true;

                    if (hitReaction.RetaliationDamage > 0 && !player.IsAlive)
                    {
                        playerRespawnRemaining = playerRespawnDelay;
                        ResetPlayerRuntimeState();
                        NotifyBattleFlow(BattleFlowHookPoint.PlayerDefeated, deltaTime, BattleCombatantSide.Enemy, BattleCombatantSide.Player, hitReaction.RetaliationDamage, hitReaction.RetaliationDamage);
                    }

                    if (!enemy.IsAlive)
                    {
                        enemyRespawnRemaining = enemySpawnData.RespawnDelay;
                        ResetPlayerRuntimeState();
                        NotifyBattleFlow(BattleFlowHookPoint.EnemyDefeated, deltaTime, BattleCombatantSide.Player, BattleCombatantSide.Enemy, outgoingPlayerDamage, appliedDamage);
                        GoldAwarded?.Invoke(enemySpawnData.GoldReward);
                        EnemyDefeated?.Invoke(enemySpawnData);
                    }
                }

                if (enemyAttacks && enemy.IsAlive && player.IsAlive)
                {
                    var outgoingEnemyDamage = enemyCombatMechanic.ModifyOutgoingDamage(enemy.Stats.AttackPower);
                    NotifyBattleFlow(
                        BattleFlowHookPoint.BeforeEnemyAttackResolved,
                        deltaTime,
                        BattleCombatantSide.Enemy,
                        BattleCombatantSide.Player,
                        outgoingEnemyDamage);
                    var appliedDamage = ApplyIncomingDamageToPlayer(outgoingEnemyDamage);
                    changed |= enemyCombatMechanic.NotifyAttackResolved();
                    NotifyBattleFlow(
                        BattleFlowHookPoint.AfterEnemyAttackResolved,
                        deltaTime,
                        BattleCombatantSide.Enemy,
                        BattleCombatantSide.Player,
                        outgoingEnemyDamage,
                        appliedDamage);
                    changed = true;

                    if (!player.IsAlive)
                    {
                        playerRespawnRemaining = playerRespawnDelay;
                        ResetPlayerRuntimeState();
                        NotifyBattleFlow(BattleFlowHookPoint.PlayerDefeated, deltaTime, BattleCombatantSide.Enemy, BattleCombatantSide.Player, outgoingEnemyDamage, appliedDamage);
                    }
                }
            }

            NotifyBattleFlow(BattleFlowHookPoint.TickCompleted, deltaTime);
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
            var playerStatuses = BuildPlayerStatuses();
            var enemyStatuses = BuildEnemyStatuses();

            return new BattleSnapshot(
                displayedEnemy.Wave,
                displayedEnemy.EnemyId,
                player.CurrentHealth,
                player.Stats.MaxHealth,
                player.IsAlive,
                playerRespawnRemaining,
                FormatStatusLabel(playerStatuses),
                playerStatuses,
                BuildGuardSnapshot(),
                BuildLastStandSnapshot(),
                BuildBurstSnapshot(),
                BuildFrenzySnapshot(),
                enemy.CurrentHealth,
                displayedEnemy.Stats.MaxHealth,
                displayedEnemy.Stats.AttackPower,
                displayedEnemy.Stats.AttacksPerSecond,
                displayedEnemy.GoldReward,
                displayedEnemy.Stats.ArmorPercent,
                displayedEnemy.BehaviorLabel,
                FormatStatusLabel(enemyStatuses),
                enemyStatuses,
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

        private CombatRuntimeStatus[] BuildPlayerStatuses()
        {
            if (!player.IsAlive)
            {
                return Array.Empty<CombatRuntimeStatus>();
            }

            var statuses = new System.Collections.Generic.List<CombatRuntimeStatus>(4);
            TryAddMechanicStatus(statuses, "player.guard", playerGuardMechanic, true);
            TryAddMechanicStatus(statuses, "player.last-stand", playerLastStandMechanic, true);
            TryAddMechanicStatus(statuses, "player.burst", playerBurstMechanic, true);
            TryAddMechanicStatus(statuses, "player.frenzy", playerFrenzyMechanic, true);
            return statuses.Count > 0 ? statuses.ToArray() : Array.Empty<CombatRuntimeStatus>();
        }

        private CombatRuntimeStatus[] BuildEnemyStatuses()
        {
            if (!enemy.IsAlive)
            {
                return Array.Empty<CombatRuntimeStatus>();
            }

            var statuses = new System.Collections.Generic.List<CombatRuntimeStatus>(1);
            TryAddMechanicStatus(statuses, "enemy.combat-mechanic", enemyCombatMechanic, true);
            return statuses.Count > 0 ? statuses.ToArray() : Array.Empty<CombatRuntimeStatus>();
        }

        private static void TryAddMechanicStatus(System.Collections.Generic.List<CombatRuntimeStatus> statuses, string statusId, CombatMechanicRuntime runtime, bool isBeneficial)
        {
            if (statuses == null || runtime == null)
            {
                return;
            }

            var statusText = runtime.StateText;
            if (string.IsNullOrWhiteSpace(statusText))
            {
                return;
            }

            statuses.Add(new CombatRuntimeStatus(
                statusId,
                runtime.Definition.DisplayName,
                GetStatusRemainingDuration(runtime),
                1,
                isBeneficial,
                false,
                statusText));
        }

        private static float GetStatusRemainingDuration(CombatMechanicRuntime runtime)
        {
            if (runtime == null)
            {
                return 0f;
            }

            return runtime.IsActive ? runtime.ActiveRemaining : 0f;
        }

        private static string FormatStatusLabel(CombatRuntimeStatus[] statuses)
        {
            if (statuses == null || statuses.Length == 0)
            {
                return string.Empty;
            }

            var stateLabel = string.Empty;
            for (var i = 0; i < statuses.Length; i++)
            {
                var statusText = statuses[i].StatusText;
                if (string.IsNullOrWhiteSpace(statusText))
                {
                    continue;
                }

                stateLabel = string.IsNullOrWhiteSpace(stateLabel)
                    ? statusText
                    : $"{stateLabel} | {statusText}";
            }

            return stateLabel;
        }

        private int ApplyIncomingDamageToPlayer(int incomingDamage)
        {
            var guardedDamage = playerGuardMechanic.ModifyIncomingDamage(incomingDamage);
            var mitigatedDamage = playerLastStandMechanic.ModifyIncomingDamage(guardedDamage);
            var appliedDamage = CalculateIncomingDamage(player, mitigatedDamage);
            if (appliedDamage >= player.CurrentHealth && playerLastStandMechanic.TryTriggerLastStand(player))
            {
                return 0;
            }

            return ApplyIncomingDamage(player, mitigatedDamage);
        }

        private static int ApplyIncomingDamage(CombatantRuntime target, int incomingDamage)
        {
            if (target == null || !target.IsAlive)
            {
                return 0;
            }

            return target.ReceiveAppliedDamage(CalculateIncomingDamage(target, incomingDamage));
        }

        private static int CalculateIncomingDamage(CombatantRuntime target, int incomingDamage)
        {
            if (target == null || !target.IsAlive)
            {
                return 0;
            }

            return CombatDamage.CalculateAppliedDamage(
                incomingDamage,
                target.Stats.FlatDamageReduction,
                target.Stats.ArmorPercent);
        }

        private void PublishState()
        {
            BattleStateChanged?.Invoke(BuildSnapshot());
        }

        private void NotifyBattleFlow(
            BattleFlowHookPoint hookPoint,
            float deltaTime,
            BattleCombatantSide actor = BattleCombatantSide.None,
            BattleCombatantSide target = BattleCombatantSide.None,
            int attemptedDamage = 0,
            int appliedDamage = 0)
        {
            BattleFlowReached?.Invoke(new BattleFlowContext(
                hookPoint,
                enemySpawnData.Wave,
                deltaTime,
                actor,
                target,
                attemptedDamage,
                appliedDamage));
        }
    }
}
