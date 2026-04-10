using UnityEngine;

namespace IdleGame
{
    public enum CombatMechanicType
    {
        None = 0,
        WindUpBurst = 1,
        FrenzyWindow = 2,
        GuardRecovery = 3,
        EnrageThreshold = 4,
        ReflectWindow = 5,
    }

    public enum CombatMechanicTriggerMode
    {
        AutomaticCycle = 0,
        Manual = 1,
        Threshold = 2,
    }

    public readonly struct CombatMechanicDefinition
    {
        public CombatMechanicDefinition(
            CombatMechanicType type,
            string displayName,
            float cooldownDuration,
            float activeDuration,
            float attackPowerMultiplier = 1f,
            float attackSpeedMultiplier = 1f,
            float damageTakenMultiplier = 1f,
            float recoveryPercentPerSecond = 0f,
            float thresholdHealthRatio = 0f,
            float retaliationDamageMultiplier = 0f,
            int retaliationFlatDamage = 0,
            CombatMechanicTriggerMode triggerMode = CombatMechanicTriggerMode.AutomaticCycle,
            bool blocksAttacksWhileActive = true)
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
            TriggerMode = triggerMode;
            BlocksAttacksWhileActive = blocksAttacksWhileActive;
        }

        public CombatMechanicType Type { get; }

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

        public CombatMechanicTriggerMode TriggerMode { get; }

        public bool BlocksAttacksWhileActive { get; }

        public bool IsDefined => Type != CombatMechanicType.None;

        public static CombatMechanicDefinition None => default;
    }

    public readonly struct CombatHitReaction
    {
        public CombatHitReaction(bool stateChanged, int retaliationDamage)
        {
            StateChanged = stateChanged;
            RetaliationDamage = Mathf.Max(0, retaliationDamage);
        }

        public bool StateChanged { get; }

        public int RetaliationDamage { get; }

        public static CombatHitReaction None => default;
    }

    public sealed class CombatMechanicRuntime
    {
        private CombatMechanicDefinition definition;
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

        public CombatMechanicDefinition Definition => definition;

        public string StateText => BuildStateText();

        public bool IsActive => definition.Type switch
        {
            CombatMechanicType.FrenzyWindow => IsWindowActive,
            CombatMechanicType.GuardRecovery => IsWindowActive,
            CombatMechanicType.ReflectWindow => IsWindowActive,
            CombatMechanicType.WindUpBurst => burstState == WindUpBurstState.BurstReady,
            CombatMechanicType.EnrageThreshold => enrageTriggered,
            _ => false,
        };

        public bool CanTriggerManually => definition.IsDefined
            && definition.TriggerMode == CombatMechanicTriggerMode.Manual
            && SupportsTimedWindow()
            && !IsWindowActive
            && timer <= 0f;

        public float CooldownRemaining => definition.TriggerMode == CombatMechanicTriggerMode.Manual && !IsWindowActive
            ? Mathf.Max(0f, timer)
            : 0f;

        public float ActiveRemaining => IsWindowActive ? Mathf.Max(0f, timer) : 0f;

        public void Reset(CombatMechanicDefinition newDefinition, int newMaxHealth)
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
                case CombatMechanicType.WindUpBurst:
                    burstState = WindUpBurstState.Charging;
                    timer = GetBurstChargeDuration();
                    break;
                case CombatMechanicType.FrenzyWindow:
                case CombatMechanicType.GuardRecovery:
                case CombatMechanicType.ReflectWindow:
                    if (definition.TriggerMode == CombatMechanicTriggerMode.Manual)
                    {
                        timer = 0f;
                    }
                    else
                    {
                        timer = GetCooldownDuration();
                    }

                    break;
            }
        }

        public bool Tick(float deltaTime, CombatantRuntime actor)
        {
            if (!definition.IsDefined || deltaTime <= 0f)
            {
                return false;
            }

            var changed = false;
            if (definition.Type == CombatMechanicType.GuardRecovery && IsGuarding)
            {
                pendingRecovery += GetRecoveryPerSecond() * deltaTime;
                var healAmount = Mathf.FloorToInt(pendingRecovery);
                if (healAmount > 0)
                {
                    pendingRecovery -= healAmount;
                    changed |= actor.RecoverHealth(healAmount);
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
                case CombatMechanicType.WindUpBurst:
                    if (burstState == WindUpBurstState.Charging)
                    {
                        burstState = WindUpBurstState.BurstReady;
                        actor.SetAttackCooldown(0f);
                    }
                    else if (burstState == WindUpBurstState.Recovering)
                    {
                        burstState = WindUpBurstState.Charging;
                        timer = GetBurstChargeDuration();
                    }

                    break;
                case CombatMechanicType.FrenzyWindow:
                case CombatMechanicType.GuardRecovery:
                case CombatMechanicType.ReflectWindow:
                    if (definition.TriggerMode == CombatMechanicTriggerMode.Manual)
                    {
                        if (windowState == TimedWindowState.Active)
                        {
                            windowState = TimedWindowState.Cooldown;
                            timer = GetCooldownDuration();
                        }
                    }
                    else
                    {
                        windowState = windowState == TimedWindowState.Cooldown
                            ? TimedWindowState.Active
                            : TimedWindowState.Cooldown;
                        timer = windowState == TimedWindowState.Active
                            ? GetActiveDuration()
                            : GetCooldownDuration();
                    }

                    break;
            }

            return true;
        }

        public bool TryTrigger()
        {
            if (!CanTriggerManually)
            {
                return false;
            }

            windowState = TimedWindowState.Active;
            timer = GetActiveDuration();
            pendingRecovery = 0f;
            return true;
        }

        public bool CanAttack => definition.Type switch
        {
            CombatMechanicType.WindUpBurst => burstState == WindUpBurstState.BurstReady,
            CombatMechanicType.GuardRecovery when definition.BlocksAttacksWhileActive => !IsGuarding,
            CombatMechanicType.ReflectWindow when definition.BlocksAttacksWhileActive => !IsReflecting,
            _ => true,
        };

        public float GetAttackSpeedMultiplier()
        {
            return definition.Type switch
            {
                CombatMechanicType.FrenzyWindow when IsWindowActive => Mathf.Max(0.1f, definition.AttackSpeedMultiplier),
                CombatMechanicType.EnrageThreshold when enrageTriggered => Mathf.Max(0.1f, definition.AttackSpeedMultiplier),
                _ => 1f,
            };
        }

        public int ModifyOutgoingDamage(int baseDamage)
        {
            if (baseDamage <= 0)
            {
                return 0;
            }

            if (definition.Type == CombatMechanicType.WindUpBurst && burstState == WindUpBurstState.BurstReady)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(1f, definition.AttackPowerMultiplier)));
            }

            if (definition.Type == CombatMechanicType.FrenzyWindow && IsWindowActive)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, definition.AttackPowerMultiplier)));
            }

            if (definition.Type == CombatMechanicType.EnrageThreshold && enrageTriggered)
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

            if (definition.Type == CombatMechanicType.GuardRecovery && IsGuarding)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * definition.DamageTakenMultiplier));
            }

            if (definition.Type == CombatMechanicType.WindUpBurst && burstState == WindUpBurstState.Recovering)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * definition.DamageTakenMultiplier));
            }

            if (definition.Type == CombatMechanicType.ReflectWindow && IsReflecting)
            {
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * definition.DamageTakenMultiplier));
            }

            return baseDamage;
        }

        public bool NotifyAttackResolved()
        {
            if (definition.Type != CombatMechanicType.WindUpBurst || burstState != WindUpBurstState.BurstReady)
            {
                return false;
            }

            burstState = WindUpBurstState.Recovering;
            timer = GetCooldownDuration();
            return true;
        }

        public CombatHitReaction NotifyIncomingHitResolved(int appliedDamage, CombatantRuntime actor, CombatantRuntime opposingActor)
        {
            if (!definition.IsDefined)
            {
                return CombatHitReaction.None;
            }

            var changed = false;
            var retaliationDamage = 0;

            if (definition.Type == CombatMechanicType.ReflectWindow && IsReflecting && appliedDamage > 0 && opposingActor != null)
            {
                var retaliationAttempt = Mathf.Max(
                    0,
                    Mathf.RoundToInt((appliedDamage * definition.RetaliationDamageMultiplier) + definition.RetaliationFlatDamage));
                retaliationDamage = retaliationAttempt > 0
                    ? opposingActor.ReceiveDamage(retaliationAttempt)
                    : 0;
                changed |= retaliationDamage > 0;
            }

            if (definition.Type == CombatMechanicType.EnrageThreshold
                && !enrageTriggered
                && actor != null
                && actor.IsAlive
                && actor.CurrentHealth <= GetThresholdHealth())
            {
                enrageTriggered = true;
                actor.SetAttackCooldown(0f);
                changed = true;
            }

            return changed || retaliationDamage > 0
                ? new CombatHitReaction(changed, retaliationDamage)
                : CombatHitReaction.None;
        }

        private bool SupportsTimedWindow()
        {
            return definition.Type == CombatMechanicType.FrenzyWindow
                || definition.Type == CombatMechanicType.GuardRecovery
                || definition.Type == CombatMechanicType.ReflectWindow;
        }

        private bool IsWindowActive => windowState == TimedWindowState.Active;

        private bool IsGuarding => definition.Type == CombatMechanicType.GuardRecovery && IsWindowActive;

        private bool IsReflecting => definition.Type == CombatMechanicType.ReflectWindow && IsWindowActive;

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
                CombatMechanicType.WindUpBurst => burstState == WindUpBurstState.BurstReady
                    ? $"{definition.DisplayName} ready"
                    : burstState == WindUpBurstState.Recovering
                        ? $"{definition.DisplayName} stagger {timer:0.0}s"
                        : $"{definition.DisplayName} {timer:0.0}s",
                CombatMechanicType.FrenzyWindow => IsWindowActive
                    ? $"{definition.DisplayName} {timer:0.0}s"
                    : string.Empty,
                CombatMechanicType.GuardRecovery => IsGuarding
                    ? $"{definition.DisplayName} +{GetRecoveryPerSecond():0}/s {timer:0.0}s"
                    : string.Empty,
                CombatMechanicType.EnrageThreshold => enrageTriggered
                    ? $"{definition.DisplayName} unleashed"
                    : $"{definition.DisplayName} at {definition.ThresholdHealthRatio * 100f:0}%",
                CombatMechanicType.ReflectWindow => IsReflecting
                    ? $"{definition.DisplayName} reflect {timer:0.0}s"
                    : string.Empty,
                _ => string.Empty,
            };
        }
    }
}
