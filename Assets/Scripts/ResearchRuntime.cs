using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame
{
    public readonly struct ResearchViewData
    {
        public ResearchViewData(
            string researchId,
            ResearchAxis axis,
            string displayName,
            string description,
            int level,
            int maxLevel,
            int nextLevelCost,
            bool canInvest,
            bool isMaxed)
        {
            ResearchId = string.IsNullOrWhiteSpace(researchId) ? string.Empty : researchId.Trim();
            Axis = axis;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
            Level = Mathf.Max(0, level);
            MaxLevel = Mathf.Max(1, maxLevel);
            NextLevelCost = Mathf.Max(0, nextLevelCost);
            CanInvest = canInvest;
            IsMaxed = isMaxed;
        }

        public string ResearchId { get; }

        public ResearchAxis Axis { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public int Level { get; }

        public int MaxLevel { get; }

        public int NextLevelCost { get; }

        public bool CanInvest { get; }

        public bool IsMaxed { get; }
    }

    public sealed class ResearchRuntime
    {
        private readonly Dictionary<string, int> levelsById = new(StringComparer.Ordinal);
        private readonly List<ResearchDefinition> definitions = new();
        private readonly Dictionary<string, ResearchDefinition> definitionsById = new(StringComparer.Ordinal);
        private BattleFlowContext lastBattleFlowContext;

        public BattleFlowContext LastBattleFlowContext => lastBattleFlowContext;

        public void SetDatabase(ResearchDatabase database)
        {
            definitions.Clear();
            definitionsById.Clear();

            if (database == null)
            {
                ResetLevels();
                return;
            }

            var sourceDefinitions = database.Definitions;
            for (var i = 0; i < sourceDefinitions.Count; i++)
            {
                var definition = sourceDefinitions[i];
                if (definition == null || !definition.IsValid || definitionsById.ContainsKey(definition.ResearchId))
                {
                    continue;
                }

                definitions.Add(definition);
                definitionsById.Add(definition.ResearchId, definition);
            }

            var staleIds = new List<string>();
            foreach (var entry in levelsById)
            {
                if (!definitionsById.ContainsKey(entry.Key))
                {
                    staleIds.Add(entry.Key);
                }
            }

            for (var i = 0; i < staleIds.Count; i++)
            {
                levelsById.Remove(staleIds[i]);
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (!levelsById.ContainsKey(definition.ResearchId))
                {
                    levelsById.Add(definition.ResearchId, 0);
                }
            }
        }

        public void ResetLevels()
        {
            levelsById.Clear();

            for (var i = 0; i < definitions.Count; i++)
            {
                levelsById[definitions[i].ResearchId] = 0;
            }
        }

        public void LoadLevels(IEnumerable<(string researchId, int level)> savedLevels)
        {
            ResetLevels();
            if (savedLevels == null)
            {
                return;
            }

            foreach (var savedLevel in savedLevels)
            {
                if (string.IsNullOrWhiteSpace(savedLevel.researchId)
                    || !definitionsById.TryGetValue(savedLevel.researchId.Trim(), out var definition))
                {
                    continue;
                }

                levelsById[definition.ResearchId] = Mathf.Clamp(savedLevel.level, 0, definition.MaxLevel);
            }
        }

        public void HandleBattleFlow(BattleFlowContext battleFlowContext)
        {
            lastBattleFlowContext = battleFlowContext;
        }

        public bool TryInvest(string researchId, ref int researchPoints)
        {
            if (string.IsNullOrWhiteSpace(researchId)
                || !definitionsById.TryGetValue(researchId.Trim(), out var definition))
            {
                return false;
            }

            var currentLevel = GetLevel(definition.ResearchId);
            if (currentLevel >= definition.MaxLevel)
            {
                return false;
            }

            var nextCost = definition.CostPerLevel;
            if (researchPoints < nextCost)
            {
                return false;
            }

            researchPoints -= nextCost;
            levelsById[definition.ResearchId] = currentLevel + 1;
            return true;
        }

        public CombatantStats ApplyToPlayerStats(CombatantStats stats)
        {
            var modifiedStats = stats;
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var level = GetLevel(definition.ResearchId);
                if (level <= 0)
                {
                    continue;
                }

                switch (definition.EffectType)
                {
                    case ResearchEffectType.HealthRegenFlat:
                        modifiedStats = modifiedStats.Add(healthRegenPerSecond: definition.EffectValueA * level);
                        break;
                    case ResearchEffectType.AttackSpeedFlat:
                        modifiedStats = modifiedStats.Add(attacksPerSecond: definition.EffectValueA * level);
                        break;
                }
            }

            return modifiedStats;
        }

        public CombatMechanicDefinition ApplyToGuardDefinition(CombatMechanicDefinition definition)
        {
            var damageTakenMultiplier = definition.DamageTakenMultiplier;
            var recoveryPercentPerSecond = definition.RecoveryPercentPerSecond;

            for (var i = 0; i < definitions.Count; i++)
            {
                var researchDefinition = definitions[i];
                var level = GetLevel(researchDefinition.ResearchId);
                if (level <= 0 || researchDefinition.EffectType != ResearchEffectType.GuardTuning)
                {
                    continue;
                }

                damageTakenMultiplier = Mathf.Max(0.1f, damageTakenMultiplier - (researchDefinition.EffectValueA * level));
                recoveryPercentPerSecond += researchDefinition.EffectValueB * level;
            }

            return new CombatMechanicDefinition(
                definition.Type,
                definition.DisplayName,
                definition.CooldownDuration,
                definition.ActiveDuration,
                definition.AttackPowerMultiplier,
                definition.AttackSpeedMultiplier,
                damageTakenMultiplier,
                recoveryPercentPerSecond,
                definition.ThresholdHealthRatio,
                definition.RetaliationDamageMultiplier,
                definition.RetaliationFlatDamage,
                definition.RestoreHealthRatio,
                definition.TriggerMode,
                definition.BlocksAttacksWhileActive);
        }

        public CombatMechanicDefinition ApplyToBurstDefinition(CombatMechanicDefinition definition)
        {
            var attackPowerMultiplier = definition.AttackPowerMultiplier;
            var activeDuration = definition.ActiveDuration;

            for (var i = 0; i < definitions.Count; i++)
            {
                var researchDefinition = definitions[i];
                var level = GetLevel(researchDefinition.ResearchId);
                if (level <= 0 || researchDefinition.EffectType != ResearchEffectType.BurstTuning)
                {
                    continue;
                }

                attackPowerMultiplier += researchDefinition.EffectValueA * level;
                activeDuration += researchDefinition.EffectValueB * level;
            }

            return new CombatMechanicDefinition(
                definition.Type,
                definition.DisplayName,
                definition.CooldownDuration,
                activeDuration,
                attackPowerMultiplier,
                definition.AttackSpeedMultiplier,
                definition.DamageTakenMultiplier,
                definition.RecoveryPercentPerSecond,
                definition.ThresholdHealthRatio,
                definition.RetaliationDamageMultiplier,
                definition.RetaliationFlatDamage,
                definition.RestoreHealthRatio,
                definition.TriggerMode,
                definition.BlocksAttacksWhileActive);
        }

        public ResearchViewData[] BuildViewData(int researchPoints)
        {
            var viewData = new ResearchViewData[definitions.Count];
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var level = GetLevel(definition.ResearchId);
                var isMaxed = level >= definition.MaxLevel;
                var nextCost = isMaxed ? 0 : definition.CostPerLevel;
                viewData[i] = new ResearchViewData(
                    definition.ResearchId,
                    definition.Axis,
                    definition.DisplayName,
                    definition.Description,
                    level,
                    definition.MaxLevel,
                    nextCost,
                    !isMaxed && researchPoints >= nextCost,
                    isMaxed);
            }

            return viewData;
        }

        public IEnumerable<(string researchId, int level)> EnumerateLevels()
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var level = GetLevel(definition.ResearchId);
                if (level <= 0)
                {
                    continue;
                }

                yield return (definition.ResearchId, level);
            }
        }

        private int GetLevel(string researchId)
        {
            return levelsById.TryGetValue(researchId, out var level) ? Mathf.Max(0, level) : 0;
        }
    }
}
