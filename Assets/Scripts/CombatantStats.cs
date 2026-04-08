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

        public int MaxHealth => maxHealth;

        public int AttackPower => attackPower;

        public float AttacksPerSecond => attacksPerSecond;

        public int FlatDamageReduction => flatDamageReduction;

        public CombatantStats(int maxHealth, int attackPower, float attacksPerSecond, int flatDamageReduction = 0)
        {
            this.maxHealth = Mathf.Max(1, maxHealth);
            this.attackPower = Mathf.Max(1, attackPower);
            this.attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond);
            this.flatDamageReduction = Mathf.Max(0, flatDamageReduction);
        }

        public float AttackInterval => 1f / AttacksPerSecond;

        public CombatantStats With(int? maxHealth = null, int? attackPower = null, float? attacksPerSecond = null, int? flatDamageReduction = null)
        {
            return new CombatantStats(
                maxHealth ?? MaxHealth,
                attackPower ?? AttackPower,
                attacksPerSecond ?? AttacksPerSecond,
                flatDamageReduction ?? FlatDamageReduction);
        }

        public CombatantStats Add(int maxHealth = 0, int attackPower = 0, float attacksPerSecond = 0f, int flatDamageReduction = 0)
        {
            return new CombatantStats(
                MaxHealth + maxHealth,
                AttackPower + attackPower,
                AttacksPerSecond + attacksPerSecond,
                FlatDamageReduction + flatDamageReduction);
        }

        public CombatantStats Multiply(float healthMultiplier = 1f, float attackPowerMultiplier = 1f, float attacksPerSecondMultiplier = 1f, float flatDamageReductionMultiplier = 1f)
        {
            return new CombatantStats(
                Mathf.Max(1, Mathf.RoundToInt(MaxHealth * Mathf.Max(0f, healthMultiplier))),
                Mathf.Max(1, Mathf.RoundToInt(AttackPower * Mathf.Max(0f, attackPowerMultiplier))),
                Mathf.Max(0.1f, AttacksPerSecond * Mathf.Max(0f, attacksPerSecondMultiplier)),
                Mathf.Max(0, Mathf.RoundToInt(FlatDamageReduction * Mathf.Max(0f, flatDamageReductionMultiplier))));
        }
    }

    public sealed class CombatantRuntime
    {
        private float attackCooldown;

        public CombatantRuntime(CombatantStats stats)
        {
            Stats = stats;
            CurrentHealth = stats.MaxHealth;
            attackCooldown = 0f;
        }

        public CombatantStats Stats { get; private set; }

        public int CurrentHealth { get; private set; }

        public bool IsAlive => CurrentHealth > 0;

        public void Reset(CombatantStats stats)
        {
            Stats = stats;
            CurrentHealth = stats.MaxHealth;
            attackCooldown = 0f;
        }

        public void SetStats(CombatantStats stats)
        {
            Stats = stats;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, stats.MaxHealth);
            attackCooldown = 0f;
        }

        public void SetCurrentHealth(int currentHealth)
        {
            CurrentHealth = Mathf.Clamp(currentHealth, 0, Stats.MaxHealth);
        }

        public bool TryAttack(float deltaTime)
        {
            if (!IsAlive)
            {
                return false;
            }

            attackCooldown -= deltaTime;
            if (attackCooldown > 0f)
            {
                return false;
            }

            attackCooldown += Stats.AttackInterval;
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
    }
}
