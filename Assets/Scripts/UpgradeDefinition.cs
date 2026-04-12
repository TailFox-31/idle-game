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
        HealthRegen = 5,
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

        [SerializeField, Min(0f)]
        private float healthRegenPerSecondPerLevel = 0f;

        [SerializeField, Min(0)]
        private int fullEffectLevels = 0;

        [SerializeField, Range(0f, 1f)]
        private float postSoftCapEffectMultiplier = 1f;

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
            float goldGainMultiplierPerLevel = 0f,
            float healthRegenPerSecondPerLevel = 0f,
            int fullEffectLevels = 0,
            float postSoftCapEffectMultiplier = 1f)
        {
            this.track = track;
            this.startingCost = Mathf.Max(1, startingCost);
            this.costMultiplier = Mathf.Max(1f, costMultiplier);
            this.attackPowerPerLevel = Mathf.Max(0, attackPowerPerLevel);
            this.maxHealthPerLevel = Mathf.Max(0, maxHealthPerLevel);
            this.attackSpeedPerLevel = Mathf.Max(0f, attackSpeedPerLevel);
            this.flatDamageReductionPerLevel = Mathf.Max(0, flatDamageReductionPerLevel);
            this.goldGainMultiplierPerLevel = Mathf.Max(0f, goldGainMultiplierPerLevel);
            this.healthRegenPerSecondPerLevel = Mathf.Max(0f, healthRegenPerSecondPerLevel);
            this.fullEffectLevels = Mathf.Max(0, fullEffectLevels);
            this.postSoftCapEffectMultiplier = Mathf.Clamp01(postSoftCapEffectMultiplier);
        }

        public UpgradeTrack Track => track;

        public bool UsesLegacyDefaultBalanceValues()
        {
            return track switch
            {
                UpgradeTrack.Defense => IsKnownDefaultDefenseBalance(),
                UpgradeTrack.HealthRegen => startingCost == 15
                    && Mathf.Approximately(costMultiplier, 1.34f)
                    && Mathf.Approximately(healthRegenPerSecondPerLevel, 1f)
                    && fullEffectLevels == 0
                    && Mathf.Approximately(postSoftCapEffectMultiplier, 1f),
                _ => false,
            };
        }

        private bool IsKnownDefaultDefenseBalance()
        {
            if (flatDamageReductionPerLevel != 1)
            {
                return false;
            }

            return (startingCost == 14
                    && Mathf.Approximately(costMultiplier, 1.46f)
                    && fullEffectLevels == 0
                    && Mathf.Approximately(postSoftCapEffectMultiplier, 1f))
                || (startingCost == 15
                    && Mathf.Approximately(costMultiplier, 1.50f)
                    && fullEffectLevels == 8
                    && Mathf.Approximately(postSoftCapEffectMultiplier, 0.6f));
        }

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

        public float GetHealthRegenPerSecond(int level)
        {
            if (level <= 0 || track != UpgradeTrack.HealthRegen)
            {
                return 0f;
            }

            return Mathf.Max(0f, healthRegenPerSecondPerLevel * GetEffectiveLevel(level));
        }

        public int GetAttackPowerBonus(int level)
        {
            return track == UpgradeTrack.AttackPower
                ? Mathf.Max(0, attackPowerPerLevel * Mathf.Max(0, level))
                : 0;
        }

        public int GetMaxHealthBonus(int level)
        {
            return track == UpgradeTrack.MaxHealth
                ? Mathf.Max(0, maxHealthPerLevel * Mathf.Max(0, level))
                : 0;
        }

        public float GetAttackSpeedBonus(int level)
        {
            return track == UpgradeTrack.AttackSpeed
                ? Mathf.Max(0f, attackSpeedPerLevel * Mathf.Max(0, level))
                : 0f;
        }

        public int GetFlatDamageReduction(int level)
        {
            return track == UpgradeTrack.Defense
                ? Mathf.Max(0, Mathf.RoundToInt(flatDamageReductionPerLevel * GetEffectiveLevel(level)))
                : 0;
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
                UpgradeTrack.Defense => stats.Add(flatDamageReduction: GetFlatDamageReduction(level)),
                UpgradeTrack.AttackSpeed => stats.Add(attacksPerSecond: attackSpeedPerLevel * level),
                UpgradeTrack.GoldGain => stats,
                UpgradeTrack.HealthRegen => stats.Add(healthRegenPerSecond: GetHealthRegenPerSecond(level)),
                _ => stats,
            };
        }

        private float GetEffectiveLevel(int level)
        {
            var clampedLevel = Mathf.Max(0, level);
            if (fullEffectLevels <= 0 || clampedLevel <= fullEffectLevels)
            {
                return clampedLevel;
            }

            return fullEffectLevels + ((clampedLevel - fullEffectLevels) * postSoftCapEffectMultiplier);
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
