using System;
using UnityEngine;

namespace IdleGame
{
    public enum UpgradeTrack
    {
        AttackPower = 0,
        MaxHealth = 1,
        Defense = 2,
        AttackSpeed = 3,
        GoldGain = 4,
    }

    [Serializable]
    public sealed class UpgradeDefinition
    {
        [SerializeField]
        private UpgradeTrack track = UpgradeTrack.AttackPower;

        [SerializeField, Min(1)]
        private int startingCost = 10;

        [SerializeField, Min(1f)]
        private float costMultiplier = 1.5f;

        [SerializeField, Min(0)]
        private int attackPowerPerLevel = 1;

        [SerializeField, Min(0)]
        private int maxHealthPerLevel = 5;

        [SerializeField, Min(0f)]
        private float attackSpeedPerLevel = 0.1f;

        [SerializeField, Min(0)]
        private int flatDamageReductionPerLevel = 1;

        [SerializeField, Min(0f)]
        private float goldGainMultiplierPerLevel = 0f;

        public UpgradeDefinition()
        {
        }

        public UpgradeDefinition(
            UpgradeTrack track,
            int startingCost,
            float costMultiplier,
            int attackPowerPerLevel = 0,
            int maxHealthPerLevel = 0,
            float attackSpeedPerLevel = 0f,
            int flatDamageReductionPerLevel = 0,
            float goldGainMultiplierPerLevel = 0f)
        {
            this.track = track;
            this.startingCost = Mathf.Max(1, startingCost);
            this.costMultiplier = Mathf.Max(1f, costMultiplier);
            this.attackPowerPerLevel = Mathf.Max(0, attackPowerPerLevel);
            this.maxHealthPerLevel = Mathf.Max(0, maxHealthPerLevel);
            this.attackSpeedPerLevel = Mathf.Max(0f, attackSpeedPerLevel);
            this.flatDamageReductionPerLevel = Mathf.Max(0, flatDamageReductionPerLevel);
            this.goldGainMultiplierPerLevel = Mathf.Max(0f, goldGainMultiplierPerLevel);
        }

        public UpgradeTrack Track => track;

        public int GetCost(int currentLevel)
        {
            var scaled = startingCost * Mathf.Pow(costMultiplier, Mathf.Max(0, currentLevel));
            return Mathf.Max(1, Mathf.CeilToInt(scaled));
        }

        public float GetGoldGainMultiplier(int level)
        {
            if (level <= 0 || track != UpgradeTrack.GoldGain)
            {
                return 1f;
            }

            return Mathf.Max(1f, 1f + (goldGainMultiplierPerLevel * level));
        }

        public int GetGoldGainBonusPercent(int level)
        {
            return Mathf.Max(0, Mathf.RoundToInt((GetGoldGainMultiplier(level) - 1f) * 100f));
        }

        public CombatantStats Apply(CombatantStats stats, int level)
        {
            if (level <= 0)
            {
                return stats;
            }

            return track switch
            {
                UpgradeTrack.AttackPower => stats.Add(attackPower: attackPowerPerLevel * level),
                UpgradeTrack.MaxHealth => stats.Add(maxHealth: maxHealthPerLevel * level),
                UpgradeTrack.Defense => stats.Add(flatDamageReduction: flatDamageReductionPerLevel * level),
                UpgradeTrack.AttackSpeed => stats.Add(attacksPerSecond: attackSpeedPerLevel * level),
                UpgradeTrack.GoldGain => stats,
                _ => stats,
            };
        }
    }

    public sealed class UpgradeState
    {
        public UpgradeState(UpgradeDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public UpgradeDefinition Definition { get; }

        public int Level { get; private set; }

        public int CurrentCost => Definition.GetCost(Level);

        public bool TryPurchase(ref int gold)
        {
            var cost = CurrentCost;
            if (gold < cost)
            {
                return false;
            }

            gold -= cost;
            Level++;
            return true;
        }

        public void SetLevel(int level)
        {
            Level = Mathf.Max(0, level);
        }
    }
}
