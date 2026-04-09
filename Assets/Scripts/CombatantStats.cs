using System;
using UnityEngine;

namespace IdleGame
{
    [Serializable]
    public struct CombatantStats
    {
        [SerializeField, Min(1)]
        private int maxHealth;

        [SerializeField, Min(1)]
        private int attackPower;

        [SerializeField, Min(0.1f)]
        private float attacksPerSecond;

        [SerializeField, Min(0)]
        private int flatDamageReduction;

        [SerializeField, Min(0f)]
        private float healthRegenPerSecond;

        public int MaxHealth => maxHealth;

        public int AttackPower => attackPower;

        public float AttacksPerSecond => attacksPerSecond;

        public int FlatDamageReduction => flatDamageReduction;

        public float HealthRegenPerSecond => healthRegenPerSecond;

        public CombatantStats(int maxHealth, int attackPower, float attacksPerSecond, int flatDamageReduction = 0, float healthRegenPerSecond = 0f)
        {
            this.maxHealth = Mathf.Max(1, maxHealth);
            this.attackPower = Mathf.Max(1, attackPower);
            this.attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond);
            this.flatDamageReduction = Mathf.Max(0, flatDamageReduction);
            this.healthRegenPerSecond = Mathf.Max(0f, healthRegenPerSecond);
        }

        public float AttackInterval => 1f / AttacksPerSecond;

        public CombatantStats With(int? maxHealth = null, int? attackPower = null, float? attacksPerSecond = null, int? flatDamageReduction = null, float? healthRegenPerSecond = null)
        {
            return new CombatantStats(
                maxHealth ?? MaxHealth,
                attackPower ?? AttackPower,
                attacksPerSecond ?? AttacksPerSecond,
                flatDamageReduction ?? FlatDamageReduction,
                healthRegenPerSecond ?? HealthRegenPerSecond);
        }

        public CombatantStats Add(int maxHealth = 0, int attackPower = 0, float attacksPerSecond = 0f, int flatDamageReduction = 0, float healthRegenPerSecond = 0f)
        {
            return new CombatantStats(
                MaxHealth + maxHealth,
                AttackPower + attackPower,
                AttacksPerSecond + attacksPerSecond,
                FlatDamageReduction + flatDamageReduction,
                HealthRegenPerSecond + healthRegenPerSecond);
        }

        public CombatantStats Multiply(float healthMultiplier = 1f, float attackPowerMultiplier = 1f, float attacksPerSecondMultiplier = 1f, float flatDamageReductionMultiplier = 1f, float healthRegenPerSecondMultiplier = 1f)
        {
            return new CombatantStats(
                Mathf.Max(1, Mathf.RoundToInt(MaxHealth * Mathf.Max(0f, healthMultiplier))),
                Mathf.Max(1, Mathf.RoundToInt(AttackPower * Mathf.Max(0f, attackPowerMultiplier))),
                Mathf.Max(0.1f, AttacksPerSecond * Mathf.Max(0f, attacksPerSecondMultiplier)),
                Mathf.Max(0, Mathf.RoundToInt(FlatDamageReduction * Mathf.Max(0f, flatDamageReductionMultiplier))),
                Mathf.Max(0f, HealthRegenPerSecond * Mathf.Max(0f, healthRegenPerSecondMultiplier)));
        }
    }

    public sealed class CombatantRuntime
    {
        private float attackCooldown;
        private float pendingRegen;

        public CombatantRuntime(CombatantStats stats, float openingAttackDelay = 0f)
        {
            Reset(stats, openingAttackDelay);
        }

        public CombatantStats Stats { get; private set; }

        public int CurrentHealth { get; private set; }

        public bool IsAlive => CurrentHealth > 0;

        public void Reset(CombatantStats stats, float openingAttackDelay = 0f)
        {
            Stats = stats;
            CurrentHealth = stats.MaxHealth;
            attackCooldown = Mathf.Max(0f, openingAttackDelay);
            pendingRegen = 0f;
        }

        public void SetStats(CombatantStats stats, float openingAttackDelay = 0f)
        {
            Stats = stats;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, stats.MaxHealth);
            attackCooldown = Mathf.Max(0f, openingAttackDelay);
            pendingRegen = 0f;
        }

        public void SetCurrentHealth(int currentHealth)
        {
            CurrentHealth = Mathf.Clamp(currentHealth, 0, Stats.MaxHealth);
        }

        public void SetAttackCooldown(float cooldown)
        {
            attackCooldown = Mathf.Max(0f, cooldown);
        }

        public bool TryAttack(float deltaTime, float attackSpeedMultiplier = 1f)
        {
            if (!IsAlive)
            {
                return false;
            }

            attackCooldown -= Mathf.Max(0f, deltaTime) * Mathf.Max(0f, attackSpeedMultiplier);
            if (attackCooldown > 0f)
            {
                return false;
            }

            attackCooldown += Stats.AttackInterval;
            return true;
        }

        public bool TryRegenerate(float deltaTime)
        {
            if (!IsAlive)
            {
                pendingRegen = 0f;
                return false;
            }

            if (deltaTime <= 0f || Stats.HealthRegenPerSecond <= 0f)
            {
                return false;
            }

            if (CurrentHealth >= Stats.MaxHealth)
            {
                pendingRegen = 0f;
                return false;
            }

            pendingRegen += Stats.HealthRegenPerSecond * deltaTime;
            var appliedHealing = Mathf.FloorToInt(pendingRegen);
            if (appliedHealing <= 0)
            {
                return false;
            }

            pendingRegen -= appliedHealing;
            var healedHealth = Mathf.Min(Stats.MaxHealth, CurrentHealth + appliedHealing);
            if (healedHealth == CurrentHealth)
            {
                return false;
            }

            CurrentHealth = healedHealth;
            return true;
        }

        public int ReceiveDamage(int damage)
        {
            if (!IsAlive)
            {
                return 0;
            }

            var incomingDamage = Mathf.Max(0, damage);
            var appliedDamage = incomingDamage <= 0
                ? 0
                : Mathf.Max(1, incomingDamage - Stats.FlatDamageReduction);
            CurrentHealth = Mathf.Max(0, CurrentHealth - appliedDamage);
            return appliedDamage;
        }

        public bool RecoverHealth(int amount)
        {
            if (!IsAlive || amount <= 0 || CurrentHealth >= Stats.MaxHealth)
            {
                return false;
            }

            var recoveredHealth = Mathf.Min(Stats.MaxHealth, CurrentHealth + amount);
            if (recoveredHealth == CurrentHealth)
            {
                return false;
            }

            CurrentHealth = recoveredHealth;
            return true;
        }
    }
}
