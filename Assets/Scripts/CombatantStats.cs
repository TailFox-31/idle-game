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

        public int MaxHealth => maxHealth;

        public int AttackPower => attackPower;

        public float AttacksPerSecond => attacksPerSecond;

        public CombatantStats(int maxHealth, int attackPower, float attacksPerSecond)
        {
            this.maxHealth = Mathf.Max(1, maxHealth);
            this.attackPower = Mathf.Max(1, attackPower);
            this.attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond);
        }

        public float AttackInterval => 1f / AttacksPerSecond;

        public CombatantStats With(int? maxHealth = null, int? attackPower = null, float? attacksPerSecond = null)
        {
            return new CombatantStats(
                maxHealth ?? MaxHealth,
                attackPower ?? AttackPower,
                attacksPerSecond ?? AttacksPerSecond);
        }

        public CombatantStats Add(int maxHealth = 0, int attackPower = 0, float attacksPerSecond = 0f)
        {
            return new CombatantStats(
                MaxHealth + maxHealth,
                AttackPower + attackPower,
                AttacksPerSecond + attacksPerSecond);
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

            var appliedDamage = Mathf.Max(0, damage);
            CurrentHealth = Mathf.Max(0, CurrentHealth - appliedDamage);
            return appliedDamage;
        }
    }
}
