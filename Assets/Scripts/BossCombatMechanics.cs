using UnityEngine;

namespace IdleGame
{
    public enum BossMechanicType
    {
        None = 0,
        WindUpBurst = 1,
        FrenzyWindow = 2,
        GuardRecovery = 3,
        EnrageThreshold = 4,
        ReflectWindow = 5,
    }

    public readonly struct BossMechanicDefinition
    {
        public BossMechanicDefinition(
            BossMechanicType type,
            string displayName,
            float cooldownDuration,
            float activeDuration,
            float attackPowerMultiplier = 1f,
            float attackSpeedMultiplier = 1f,
            float damageTakenMultiplier = 1f,
            float recoveryPercentPerSecond = 0f,
            float thresholdHealthRatio = 0f,
            float retaliationDamageMultiplier = 0f,
            int retaliationFlatDamage = 0)
        {
            Type = type;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
            CooldownDuration = Mathf.Max(0f, cooldownDuration);
            ActiveDuration = Mathf.Max(0f, activeDuration);
            AttackPowerMultiplier = Mathf.Max(0f, attackPowerMultiplier);
            AttackSpeedMultiplier = Mathf.Max(0f, attackSpeedMultiplier);
            DamageTakenMultiplier = Mathf.Max(0.1f, damageTakenMultiplier);
            RecoveryPercentPerSecond = Mathf.Max(0f, recoveryPercentPerSecond);
            ThresholdHealthRatio = Mathf.Clamp01(thresholdHealthRatio);
            RetaliationDamageMultiplier = Mathf.Max(0f, retaliationDamageMultiplier);
            RetaliationFlatDamage = Mathf.Max(0, retaliationFlatDamage);
        }

        public BossMechanicType Type { get; }

        public string DisplayName { get; }

        public float CooldownDuration { get; }

        public float ActiveDuration { get; }

        public float AttackPowerMultiplier { get; }

        public float AttackSpeedMultiplier { get; }

        public float DamageTakenMultiplier { get; }

        public float RecoveryPercentPerSecond { get; }

        public float ThresholdHealthRatio { get; }

        public float RetaliationDamageMultiplier { get; }

        public int RetaliationFlatDamage { get; }

        public bool IsDefined => Type != BossMechanicType.None;

        public static BossMechanicDefinition None => default;
    }

    public readonly struct BossHitReaction
    {
        public BossHitReaction(bool stateChanged, int retaliationDamage)
        {
            StateChanged = stateChanged;
            RetaliationDamage = Mathf.Max(0, retaliationDamage);
        }

        public bool StateChanged { get; }

        public int RetaliationDamage { get; }

        public static BossHitReaction None => default;
    }

    public sealed class BossMechanicRuntime
    {
        private BossMechanicDefinition definition;
        private float timer;
        private float pendingRecovery;
        private int maxHealth;
        private bool enrageTriggered;
        private WindUpBurstState burstState;
        private TimedWindowState windowState;

        private enum WindUpBurstState
        {
            Inactive = 0,
            Charging = 1,
            BurstReady = 2,
            Recovering = 3,
        }

        private enum TimedWindowState
        {
            Cooldown = 0,
            Active = 1,
        }

        public BossMechanicDefinition Definition => definition;

        public string StateText => BuildStateText();

        public void Reset(BossMechanicDefinition newDefinition, int newMaxHealth)
        {
            definition = newDefinition;
            maxHealth = Mathf.Max(1, newMaxHealth);
            pendingRecovery = 0f;
            enrageTriggered = false;
            burstState = WindUpBurstState.Inactive;
            windowState = TimedWindowState.Cooldown;
            timer = 0f;

            switch (definition.Type)
            {
                case BossMechanicType.WindUpBurst:
                    burstState = WindUpBurstState.Charging;
                    timer = GetBurstChargeDuration();
                    break;
                case BossMechanicType.FrenzyWindow:
                case BossMechanicType.GuardRecovery:
                case BossMechanicType.ReflectWindow:
                    windowState = TimedWindowState.Cooldown;
                    timer = GetCooldownDuration();
                    break;
            }
        }

        public bool Tick(float deltaTime, CombatantRuntime enemy)
        {
            if (!definition.IsDefined || deltaTime <= 0f)
            {
                return false;
            }

            var changed = false;
            if (definition.Type == BossMechanicType.GuardRecovery && IsGuarding)
            {
                pendingRecovery += GetRecoveryPerSecond() * deltaTime;
                var healAmount = Mathf.FloorToInt(pendingRecovery);
                if (healAmount > 0)
                {
                    pendingRecovery -= healAmount;
                    changed |= enemy.RecoverHealth(healAmount);
                }
            }

            if (timer <= 0f)
            {
                return changed;
            }

            var previousTimer = timer;
            timer = Mathf.Max(0f, timer - deltaTime);
            if (timer == previousTimer)
            {
                return changed;
            }

            changed = true;
            if (timer > 0f)
            {
                return changed;
            }

            switch (definition.Type)
            {
                case BossMechanicType.WindUpBurst:
                    if (burstState == WindUpBurstState.Charging)
                    {
                        burstState = WindUpBurstState.BurstReady;
                        enemy.SetAttackCooldown(0f);
                    }
                    else if (burstState == WindUpBurstState.Recovering)
                    {
                        burstState = WindUpBurstState.Charging;
                        timer = GetBurstChargeDuration();
                    }

                    break;
                case BossMechanicType.FrenzyWindow:
                case BossMechanicType.GuardRecovery:
                case BossMechanicType.ReflectWindow:
                    windowState = windowState == TimedWindowState.Cooldown
                        ? TimedWindowState.Active
                        : TimedWindowState.Cooldown;
                    timer = windowState == TimedWindowState.Active
                        ? GetActiveDuration()
                        : GetCooldownDuration();
                    break;
            }

            return true;
        }

        public bool CanAttack => definition.Type switch
        {
            BossMechanicType.WindUpBurst => burstState == WindUpBurstState.BurstReady,
            BossMechanicType.GuardRecovery => !IsGuarding,
            BossMechanicType.ReflectWindow => !IsReflecting,
            _ => true,
        };

        public float GetAttackSpeedMultiplier()
        {
            return definition.Type switch
            {
                BossMechanicType.FrenzyWindow when IsWindowActive => Mathf.Max(0.1f, definition.AttackSpeedMultiplier),
                BossMechanicType.EnrageThreshold when enrageTriggered => Mathf.Max(0.1f, definition.AttackSpeedMultiplier),
                _ => 1f,
            };
        }

        public int ModifyOutgoingDamage(int baseDamage)
        {
            if (baseDamage <= 0)
            {
                return 0;
            }

            if (definition.Type == BossMechanicType.WindUpBurst && burstState == WindUpBurstState.BurstReady)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(1f, definition.AttackPowerMultiplier)));
            }

            if (definition.Type == BossMechanicType.FrenzyWindow && IsWindowActive)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, definition.AttackPowerMultiplier)));
            }

            if (definition.Type == BossMechanicType.EnrageThreshold && enrageTriggered)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, definition.AttackPowerMultiplier)));
            }

            return baseDamage;
        }

        public int ModifyIncomingDamage(int baseDamage)
        {
            if (baseDamage <= 0)
            {
                return 0;
            }

            if (definition.Type == BossMechanicType.GuardRecovery && IsGuarding)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * definition.DamageTakenMultiplier));
            }

            if (definition.Type == BossMechanicType.WindUpBurst && burstState == WindUpBurstState.Recovering)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * definition.DamageTakenMultiplier));
            }

            if (definition.Type == BossMechanicType.ReflectWindow && IsReflecting)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * definition.DamageTakenMultiplier));
            }

            return baseDamage;
        }

        public bool NotifyAttackResolved()
        {
            if (definition.Type != BossMechanicType.WindUpBurst || burstState != WindUpBurstState.BurstReady)
            {
                return false;
            }

            burstState = WindUpBurstState.Recovering;
            timer = GetCooldownDuration();
            return true;
        }

        public BossHitReaction NotifyIncomingHitResolved(int appliedDamage, CombatantRuntime enemy, CombatantRuntime player)
        {
            if (!definition.IsDefined)
            {
                return BossHitReaction.None;
            }

            var changed = false;
            var retaliationDamage = 0;

            if (definition.Type == BossMechanicType.ReflectWindow && IsReflecting && appliedDamage > 0 && player != null)
            {
                var retaliationAttempt = Mathf.Max(
                    0,
                    Mathf.RoundToInt((appliedDamage * definition.RetaliationDamageMultiplier) + definition.RetaliationFlatDamage));
                retaliationDamage = retaliationAttempt > 0
                    ? player.ReceiveDamage(retaliationAttempt)
                    : 0;
                changed |= retaliationDamage > 0;
            }

            if (definition.Type == BossMechanicType.EnrageThreshold
                && !enrageTriggered
                && enemy != null
                && enemy.IsAlive
                && enemy.CurrentHealth <= GetThresholdHealth())
            {
                enrageTriggered = true;
                enemy.SetAttackCooldown(0f);
                changed = true;
            }

            return changed || retaliationDamage > 0
                ? new BossHitReaction(changed, retaliationDamage)
                : BossHitReaction.None;
        }

        private bool IsWindowActive => windowState == TimedWindowState.Active;

        private bool IsGuarding => definition.Type == BossMechanicType.GuardRecovery && IsWindowActive;

        private bool IsReflecting => definition.Type == BossMechanicType.ReflectWindow && IsWindowActive;

        private float GetCooldownDuration()
        {
            return Mathf.Max(0f, definition.CooldownDuration);
        }

        private float GetActiveDuration()
        {
            return Mathf.Max(0.1f, definition.ActiveDuration);
        }

        private float GetBurstChargeDuration()
        {
            return Mathf.Max(0.25f, definition.ActiveDuration);
        }

        private float GetRecoveryPerSecond()
        {
            return Mathf.Max(0f, maxHealth * definition.RecoveryPercentPerSecond);
        }

        private int GetThresholdHealth()
        {
            return Mathf.Max(1, Mathf.CeilToInt(maxHealth * definition.ThresholdHealthRatio));
        }

        private string BuildStateText()
        {
            if (!definition.IsDefined)
            {
                return string.Empty;
            }

            return definition.Type switch
            {
                BossMechanicType.WindUpBurst => burstState == WindUpBurstState.BurstReady
                    ? $"{definition.DisplayName} ready"
                    : burstState == WindUpBurstState.Recovering
                        ? $"{definition.DisplayName} stagger {timer:0.0}s"
                        : $"{definition.DisplayName} {timer:0.0}s",
                BossMechanicType.FrenzyWindow => IsWindowActive
                    ? $"{definition.DisplayName} {timer:0.0}s"
                    : string.Empty,
                BossMechanicType.GuardRecovery => IsGuarding
                    ? $"{definition.DisplayName} +{GetRecoveryPerSecond():0}/s {timer:0.0}s"
                    : string.Empty,
                BossMechanicType.EnrageThreshold => enrageTriggered
                    ? $"{definition.DisplayName} unleashed"
                    : $"{definition.DisplayName} at {definition.ThresholdHealthRatio * 100f:0}%",
                BossMechanicType.ReflectWindow => IsReflecting
                    ? $"{definition.DisplayName} reflect {timer:0.0}s"
                    : string.Empty,
                _ => string.Empty,
            };
        }
    }
}
