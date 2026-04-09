using UnityEngine;

namespace IdleGame
{
    public enum BossMechanicType
    {
        None = 0,
        WindUpBurst = 1,
        FrenzyWindow = 2,
        GuardRecovery = 3,
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
            float recoveryPercentPerSecond = 0f)
        {
            Type = type;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
            CooldownDuration = Mathf.Max(0f, cooldownDuration);
            ActiveDuration = Mathf.Max(0f, activeDuration);
            AttackPowerMultiplier = Mathf.Max(0f, attackPowerMultiplier);
            AttackSpeedMultiplier = Mathf.Max(0f, attackSpeedMultiplier);
            DamageTakenMultiplier = Mathf.Clamp01(damageTakenMultiplier);
            RecoveryPercentPerSecond = Mathf.Max(0f, recoveryPercentPerSecond);
        }

        public BossMechanicType Type { get; }

        public string DisplayName { get; }

        public float CooldownDuration { get; }

        public float ActiveDuration { get; }

        public float AttackPowerMultiplier { get; }

        public float AttackSpeedMultiplier { get; }

        public float DamageTakenMultiplier { get; }

        public float RecoveryPercentPerSecond { get; }

        public bool IsDefined => Type != BossMechanicType.None;

        public static BossMechanicDefinition None => default;
    }

    public sealed class BossMechanicRuntime
    {
        private BossMechanicDefinition definition;
        private float timer;
        private float pendingRecovery;
        private int maxHealth;
        private WindUpBurstState burstState;
        private TimedWindowState windowState;

        private enum WindUpBurstState
        {
            Inactive = 0,
            Charging = 1,
            BurstReady = 2,
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

                    break;
                case BossMechanicType.FrenzyWindow:
                case BossMechanicType.GuardRecovery:
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
            _ => true,
        };

        public float GetAttackSpeedMultiplier()
        {
            return definition.Type == BossMechanicType.FrenzyWindow && IsWindowActive
                ? Mathf.Max(0.1f, definition.AttackSpeedMultiplier)
                : 1f;
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

            return baseDamage;
        }

        public bool NotifyAttackResolved()
        {
            if (definition.Type != BossMechanicType.WindUpBurst || burstState != WindUpBurstState.BurstReady)
            {
                return false;
            }

            burstState = WindUpBurstState.Charging;
            timer = GetBurstChargeDuration();
            return true;
        }

        private bool IsWindowActive => windowState == TimedWindowState.Active;

        private bool IsGuarding => definition.Type == BossMechanicType.GuardRecovery && IsWindowActive;

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
                    : $"{definition.DisplayName} {timer:0.0}s",
                BossMechanicType.FrenzyWindow => IsWindowActive
                    ? $"{definition.DisplayName} {timer:0.0}s"
                    : string.Empty,
                BossMechanicType.GuardRecovery => IsGuarding
                    ? $"{definition.DisplayName} +{GetRecoveryPerSecond():0}/s {timer:0.0}s"
                    : string.Empty,
                _ => string.Empty,
            };
        }
    }
}
