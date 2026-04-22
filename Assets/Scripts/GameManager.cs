using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame
{
    public readonly struct UpgradeViewData
    {
        public UpgradeViewData(
            UpgradeTrack track,
            int level,
            int nextCost,
            int attackPowerBonus,
            int maxHealthBonus,
            float attackSpeedBonus,
            int flatDamageReduction,
            float goldGainMultiplier,
            float healthRegenPerSecond,
            float armorPercent,
            bool isMaxed)
        {
            Track = track;
            Level = level;
            NextCost = nextCost;
            AttackPowerBonus = attackPowerBonus;
            MaxHealthBonus = maxHealthBonus;
            AttackSpeedBonus = attackSpeedBonus;
            FlatDamageReduction = flatDamageReduction;
            GoldGainMultiplier = goldGainMultiplier;
            HealthRegenPerSecond = healthRegenPerSecond;
            ArmorPercent = armorPercent;
            IsMaxed = isMaxed;
        }

        public UpgradeTrack Track { get; }

        public int Level { get; }

        public int NextCost { get; }

        public int AttackPowerBonus { get; }

        public int MaxHealthBonus { get; }

        public float AttackSpeedBonus { get; }

        public int FlatDamageReduction { get; }

        public float GoldGainMultiplier { get; }

        public float HealthRegenPerSecond { get; }

        public float ArmorPercent { get; }

        public bool IsMaxed { get; }
    }

    public readonly struct GameSnapshot
    {
        public GameSnapshot(
            int gold,
            int wave,
            int highestWaveReached,
            int selectedStartWave,
            CombatantStats playerStats,
            BattleSnapshot battle,
            UpgradeViewData[] upgrades,
            int researchPoints,
            ResearchViewData[] researches,
            int nextMilestoneWave,
            int milestoneAttackBonus,
            int lastMilestoneWave,
            int lastMilestoneGoldReward,
            int lastMilestoneAttackReward)
        {
            Gold = gold;
            Wave = wave;
            HighestWaveReached = highestWaveReached;
            SelectedStartWave = selectedStartWave;
            PlayerStats = playerStats;
            Battle = battle;
            Upgrades = upgrades;
            ResearchPoints = researchPoints;
            Researches = researches;
            NextMilestoneWave = nextMilestoneWave;
            MilestoneAttackBonus = milestoneAttackBonus;
            LastMilestoneWave = lastMilestoneWave;
            LastMilestoneGoldReward = lastMilestoneGoldReward;
            LastMilestoneAttackReward = lastMilestoneAttackReward;
        }

        public int Gold { get; }

        public int Wave { get; }

        public int HighestWaveReached { get; }

        public int SelectedStartWave { get; }

        public CombatantStats PlayerStats { get; }

        public BattleSnapshot Battle { get; }

        public UpgradeViewData[] Upgrades { get; }

        public int ResearchPoints { get; }

        public ResearchViewData[] Researches { get; }

        public int NextMilestoneWave { get; }

        public int MilestoneAttackBonus { get; }

        public int LastMilestoneWave { get; }

        public int LastMilestoneGoldReward { get; }

        public int LastMilestoneAttackReward { get; }
    }

    public sealed class GameManager : MonoBehaviour
    {
        [Serializable]
        private sealed class SaveData
        {
            public int gold;
            public int wave = 1;
            public int highestWaveReached;
            public int lastClaimedMilestoneWave;
            public int lastMilestoneGoldReward;
            public int lastMilestoneAttackReward;
            public int milestoneAttackBonus;
            public int researchPoints;
            public List<UpgradeSaveData> upgrades = new();
            public List<ResearchLevelSaveData> researchLevels = new();
        }

        [Serializable]
        private sealed class UpgradeSaveData
        {
            public UpgradeTrack track;
            public int level;
        }

        [Serializable]
        private sealed class ResearchLevelSaveData
        {
            public string researchId;
            public int level;
        }

        private const string SaveKey = "IdleGame.PrototypeSave";
        private const string DefaultResearchDatabaseResourcePath = "Research/DefaultResearchDatabase";

        [SerializeField]
        private CombatantStats playerBaseStats = new CombatantStats(30, 2, 1f);

        [SerializeField, Min(0)]
        private int startingGold;

        [SerializeField]
        private EnemyController enemyController;

        [SerializeField]
        private UIBinder uiBinder;

        [Header("Research")]
        [SerializeField]
        private ResearchDatabase researchDatabase;

        [SerializeField, Min(0f)]
        private float playerRespawnDelay = 2f;

        [Header("Player Skill MVP")]
        [SerializeField, Min(0f)]
        private float guardCooldownDuration = 8f;

        [SerializeField, Min(0.1f)]
        private float guardActiveDuration = 3f;

        [SerializeField, Range(0.1f, 1f)]
        private float guardDamageTakenMultiplier = 0.68f;

        [SerializeField, Min(0f)]
        private float guardRecoveryPercentPerSecond = 0.02f;

        [SerializeField, Range(0.01f, 1f)]
        private float lastStandRestoreHealthRatio = 0.25f;

        [SerializeField, Range(0.1f, 1f)]
        private float lastStandDamageTakenMultiplier = 0.75f;

        [SerializeField, Min(0.1f)]
        private float lastStandActiveDuration = 4f;

        [SerializeField, Min(0f)]
        private float lastStandCooldownDuration = 60f;

        [SerializeField, Min(1f)]
        private float burstAttackPowerMultiplier = 2.5f;

        [SerializeField, Min(0.1f)]
        private float burstArmedDuration = 4f;

        [SerializeField, Min(0f)]
        private float burstCooldownDuration = 12f;

        [SerializeField, Min(1f)]
        private float frenzyAttackSpeedMultiplier = 1.6f;

        [SerializeField, Min(0.1f)]
        private float frenzyActiveDuration = 5f;

        [SerializeField, Min(0f)]
        private float frenzyCooldownDuration = 18f;

        [Header("Milestone Rewards")]
        [SerializeField, Min(2)]
        private int milestoneWaveInterval = 5;

        [SerializeField, Min(0)]
        private int milestoneGoldReward = 30;

        [SerializeField, Min(0)]
        private int milestoneGoldRewardPerMilestone = 15;

        [SerializeField, Min(0)]
        private int milestoneAttackPowerPerMilestone = 1;

        [SerializeField]
        private List<UpgradeDefinition> upgrades = BuildDefaultUpgradeDefinitions();

        private readonly Dictionary<UpgradeTrack, UpgradeState> upgradeStates = new();
        private readonly ResearchRuntime researchRuntime = new();
        private AutoBattleSystem battleSystem;
        private int gold;
        private int currentWave = 1;
        private int highestWaveReached = 1;
        private int selectedStartWave = 1;
        private int lastClaimedMilestoneWave;
        private int lastMilestoneGoldReward;
        private int lastMilestoneAttackReward;
        private int milestoneAttackBonus;
        private int researchPoints;

        public event Action<GameSnapshot> StateChanged;

        public GameSnapshot CurrentSnapshot => BuildSnapshot();

        private void Awake()
        {
            EnsureUpgradeDefinitions();
            InitializeUpgradeStates();
            InitializeResearchRuntime();
            LoadProgressOrInitializeDefaults();
            InitializeBattleSystem();

            if (uiBinder != null)
            {
                uiBinder.Bind(this);
            }

            PublishState();
        }

        private void OnValidate()
        {
            EnsureUpgradeDefinitions();
        }

        private void Update()
        {
            battleSystem?.Tick(Time.deltaTime);
        }

        public bool TryPurchaseUpgrade(UpgradeTrack track)
        {
            if (!upgradeStates.TryGetValue(track, out var state))
            {
                return false;
            }

            if (!state.TryPurchase(ref gold))
            {
                return false;
            }

            ApplyRuntimeCombatConfiguration();
            SaveProgress();
            PublishState();
            return true;
        }

        public bool TryInvestResearch(string researchId)
        {
            if (!researchRuntime.TryInvest(researchId, ref researchPoints))
            {
                return false;
            }

            ApplyRuntimeCombatConfiguration();
            SaveProgress();
            PublishState();
            return true;
        }

        public void ResetSavedProgress()
        {
            DeleteSavedProgress();
            ApplyFreshState();
            RebuildBattleState();
            PublishState();
        }

        public void SelectPreviousStartWave()
        {
            SetSelectedStartWave(selectedStartWave - 1);
        }

        public void SelectNextStartWave()
        {
            SetSelectedStartWave(selectedStartWave + 1);
        }

        public void TravelToSelectedWave()
        {
            TravelToWave(selectedStartWave);
        }

        public bool TryActivateGuard()
        {
            return battleSystem != null && battleSystem.TryActivateGuard();
        }

        public bool TryActivateBurst()
        {
            return battleSystem != null && battleSystem.TryActivateBurst();
        }

        public bool TryActivateFrenzy()
        {
            return battleSystem != null && battleSystem.TryActivateFrenzy();
        }

        private void InitializeUpgradeStates()
        {
            upgradeStates.Clear();

            foreach (var definition in upgrades)
            {
                if (definition == null || upgradeStates.ContainsKey(definition.Track))
                {
                    continue;
                }

                upgradeStates.Add(definition.Track, new UpgradeState(definition));
            }
        }

        private void LoadProgressOrInitializeDefaults()
        {
            ApplyFreshState();

            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return;
            }

            var raw = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            var saveData = JsonUtility.FromJson<SaveData>(raw);
            if (saveData == null)
            {
                return;
            }

            gold = Mathf.Max(0, saveData.gold);
            currentWave = Mathf.Max(1, saveData.wave);
            highestWaveReached = Mathf.Max(currentWave, saveData.highestWaveReached);
            selectedStartWave = Mathf.Clamp(currentWave, 1, highestWaveReached);
            lastClaimedMilestoneWave = Mathf.Max(0, saveData.lastClaimedMilestoneWave);
            lastMilestoneGoldReward = Mathf.Max(0, saveData.lastMilestoneGoldReward);
            lastMilestoneAttackReward = Mathf.Max(0, saveData.lastMilestoneAttackReward);
            milestoneAttackBonus = Mathf.Max(0, saveData.milestoneAttackBonus);
            researchPoints = Mathf.Max(0, saveData.researchPoints);

            if (saveData.upgrades != null)
            {
                LoadUpgradeLevels(saveData.upgrades);
            }

            LoadResearchLevels(saveData.researchLevels);
        }

        private void ApplyFreshState()
        {
            gold = Mathf.Max(0, startingGold);
            currentWave = 1;
            highestWaveReached = 1;
            selectedStartWave = 1;
            lastClaimedMilestoneWave = 0;
            lastMilestoneGoldReward = 0;
            lastMilestoneAttackReward = 0;
            milestoneAttackBonus = 0;
            researchPoints = 0;

            foreach (var state in upgradeStates.Values)
            {
                state.SetLevel(0);
            }

            researchRuntime.ResetLevels();
        }

        private void InitializeResearchRuntime()
        {
            if (researchDatabase == null)
            {
                researchDatabase = Resources.Load<ResearchDatabase>(DefaultResearchDatabaseResourcePath);
            }

            researchRuntime.SetDatabase(researchDatabase);
        }

        private void EnsureUpgradeDefinitions()
        {
            upgrades = BuildNormalizedUpgradeDefinitions(upgrades);
        }

        private static List<UpgradeDefinition> BuildNormalizedUpgradeDefinitions(List<UpgradeDefinition> definitions)
        {
            var normalized = BuildDefaultUpgradeDefinitions();
            if (definitions == null)
            {
                return normalized;
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                var candidate = definitions[i];
                if (candidate == null)
                {
                    continue;
                }

                var normalizedIndex = GetDefinitionIndex(normalized, candidate.Track);
                if (normalizedIndex < 0)
                {
                    continue;
                }

                if (candidate.UsesLegacyDefaultBalanceValues())
                {
                    continue;
                }

                normalized[normalizedIndex] = candidate;
            }

            return normalized;
        }

        private static int GetDefinitionIndex(List<UpgradeDefinition> definitions, UpgradeTrack track)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].Track == track)
                {
                    return i;
                }
            }

            return -1;
        }

        private static List<UpgradeDefinition> BuildDefaultUpgradeDefinitions()
        {
            return new List<UpgradeDefinition>
            {
                new UpgradeDefinition(UpgradeTrack.AttackPower, 10, 1.28f, attackPowerPerLevel: 1),
                new UpgradeDefinition(UpgradeTrack.MaxHealth, 14, 1.32f, maxHealthPerLevel: 12),
                new UpgradeDefinition(
                    UpgradeTrack.HealthRegen,
                    16,
                    1.38f,
                    healthRegenPerSecondPerLevel: 0.9f,
                    fullEffectLevels: 8,
                    postSoftCapEffectMultiplier: 0.65f),
                new UpgradeDefinition(
                    UpgradeTrack.Defense,
                    15,
                    1.60f,
                    flatDamageReductionPerLevel: 1),
                new UpgradeDefinition(
                    UpgradeTrack.Armor,
                    18,
                    1.55f,
                    armorPercentPerLevel: 0.02f,
                    maxArmorPercent: 0.40f,
                    maxLevel: 20),
                new UpgradeDefinition(UpgradeTrack.AttackSpeed, 16, 1.34f, attackSpeedPerLevel: 0.18f),
                new UpgradeDefinition(UpgradeTrack.GoldGain, 18, 1.38f, goldGainMultiplierPerLevel: 0.18f),
            };
        }

        private void InitializeBattleSystem()
        {
            if (enemyController == null)
            {
                return;
            }

            battleSystem = new AutoBattleSystem(
                BuildPlayerStats(),
                enemyController.CreateSpawnDataForWave(currentWave),
                playerRespawnDelay,
                BuildGuardDefinition(),
                BuildLastStandDefinition(),
                BuildBurstDefinition(),
                BuildFrenzyDefinition());
            battleSystem.GoldAwarded += HandleGoldAwarded;
            battleSystem.EnemyDefeated += HandleEnemyDefeated;
            battleSystem.BattleFlowReached += HandleBattleFlowReached;
            battleSystem.BattleStateChanged += _ => PublishState();
        }

        private void RebuildBattleState()
        {
            if (enemyController == null)
            {
                return;
            }

            if (battleSystem == null)
            {
                InitializeBattleSystem();
                return;
            }

            ApplyRuntimeCombatConfiguration();
            battleSystem.SetEnemy(enemyController.CreateSpawnDataForWave(currentWave));
        }

        private void ApplyRuntimeCombatConfiguration()
        {
            if (battleSystem == null)
            {
                return;
            }

            battleSystem.SetPlayerStats(BuildPlayerStats());
            battleSystem.SetPlayerGuardDefinition(BuildGuardDefinition());
            battleSystem.SetPlayerLastStandDefinition(BuildLastStandDefinition());
            battleSystem.SetPlayerBurstDefinition(BuildBurstDefinition());
            battleSystem.SetPlayerFrenzyDefinition(BuildFrenzyDefinition());
        }

        private void SetSelectedStartWave(int targetWave)
        {
            var clampedWave = Mathf.Clamp(targetWave, 1, Mathf.Max(1, highestWaveReached));
            if (clampedWave == selectedStartWave)
            {
                return;
            }

            selectedStartWave = clampedWave;
            PublishState();
        }

        private void TravelToWave(int targetWave)
        {
            currentWave = Mathf.Clamp(targetWave, 1, Mathf.Max(1, highestWaveReached));
            selectedStartWave = currentWave;
            RebuildBattleState();
            SaveProgress();
            PublishState();
        }

        private void UpdateHighestWaveReached(int reachedWave)
        {
            highestWaveReached = Mathf.Max(highestWaveReached, Mathf.Max(1, reachedWave));
        }

        private CombatantStats BuildPlayerStats()
        {
            var stats = playerBaseStats;
            foreach (var state in upgradeStates.Values)
            {
                stats = state.Definition.Apply(stats, state.Level);
            }

            stats = stats.Add(attackPower: milestoneAttackBonus);
            return researchRuntime.ApplyToPlayerStats(stats);
        }

        private CombatMechanicDefinition BuildGuardDefinition()
        {
            return researchRuntime.ApplyToGuardDefinition(new CombatMechanicDefinition(
                CombatMechanicType.GuardRecovery,
                "Guard",
                guardCooldownDuration,
                guardActiveDuration,
                damageTakenMultiplier: guardDamageTakenMultiplier,
                recoveryPercentPerSecond: guardRecoveryPercentPerSecond,
                triggerMode: CombatMechanicTriggerMode.Manual,
                blocksAttacksWhileActive: false));
        }

        private CombatMechanicDefinition BuildLastStandDefinition()
        {
            return new CombatMechanicDefinition(
                CombatMechanicType.LastStand,
                "Last Stand AUTO",
                lastStandCooldownDuration,
                lastStandActiveDuration,
                damageTakenMultiplier: lastStandDamageTakenMultiplier,
                restoreHealthRatio: lastStandRestoreHealthRatio,
                triggerMode: CombatMechanicTriggerMode.Manual,
                blocksAttacksWhileActive: false);
        }

        private CombatMechanicDefinition BuildBurstDefinition()
        {
            return researchRuntime.ApplyToBurstDefinition(new CombatMechanicDefinition(
                CombatMechanicType.PlayerBurst,
                "Burst",
                burstCooldownDuration,
                burstArmedDuration,
                attackPowerMultiplier: burstAttackPowerMultiplier,
                triggerMode: CombatMechanicTriggerMode.Manual,
                blocksAttacksWhileActive: false));
        }

        private CombatMechanicDefinition BuildFrenzyDefinition()
        {
            return new CombatMechanicDefinition(
                CombatMechanicType.FrenzyWindow,
                "Frenzy",
                frenzyCooldownDuration,
                frenzyActiveDuration,
                attackSpeedMultiplier: frenzyAttackSpeedMultiplier,
                triggerMode: CombatMechanicTriggerMode.Manual,
                blocksAttacksWhileActive: false);
        }

        private GameSnapshot BuildSnapshot()
        {
            var upgradeData = new UpgradeViewData[upgradeStates.Count];
            var index = 0;
            foreach (var state in upgradeStates.Values)
            {
                upgradeData[index++] = new UpgradeViewData(
                    state.Definition.Track,
                    state.Level,
                    state.CurrentCost,
                    state.Definition.GetAttackPowerBonus(state.Level),
                    state.Definition.GetMaxHealthBonus(state.Level),
                    state.Definition.GetAttackSpeedBonus(state.Level),
                    state.Definition.GetFlatDamageReduction(state.Level),
                    state.Definition.GetGoldGainMultiplier(state.Level),
                    state.Definition.GetHealthRegenPerSecond(state.Level),
                    state.Definition.GetArmorPercent(state.Level),
                    state.Definition.HasMaxLevel && state.Level >= state.Definition.MaxLevel);
            }

            var battle = battleSystem != null
                ? battleSystem.Snapshot
                : new BattleSnapshot(0, string.Empty, 0, 0, false, 0f, string.Empty, default, default, default, default, 0, 0, 0, 0f, 0, 0f, string.Empty, string.Empty, false, 0f);
            var researchData = researchRuntime.BuildViewData(researchPoints);

            return new GameSnapshot(
                gold,
                currentWave,
                highestWaveReached,
                selectedStartWave,
                BuildPlayerStats(),
                battle,
                upgradeData,
                researchPoints,
                researchData,
                GetNextMilestoneWave(currentWave),
                milestoneAttackBonus,
                lastClaimedMilestoneWave,
                lastMilestoneGoldReward,
                lastMilestoneAttackReward);
        }

        private void HandleGoldAwarded(int amount)
        {
            gold += GetModifiedGoldReward(amount);
            SaveProgress();
            PublishState();
        }

        private void LoadUpgradeLevels(List<UpgradeSaveData> savedUpgrades)
        {
            foreach (var state in upgradeStates.Values)
            {
                state.SetLevel(0);
            }

            foreach (var entry in savedUpgrades)
            {
                if (entry == null || !upgradeStates.TryGetValue(entry.track, out var state))
                {
                    continue;
                }

                state.SetLevel(entry.level);
            }
        }

        private void LoadResearchLevels(List<ResearchLevelSaveData> savedResearchLevels)
        {
            if (savedResearchLevels == null)
            {
                researchRuntime.ResetLevels();
                return;
            }

            var levels = new List<(string researchId, int level)>(savedResearchLevels.Count);
            for (var i = 0; i < savedResearchLevels.Count; i++)
            {
                var entry = savedResearchLevels[i];
                if (entry == null)
                {
                    continue;
                }

                levels.Add((entry.researchId, entry.level));
            }

            researchRuntime.LoadLevels(levels);
        }

        private int GetModifiedGoldReward(int baseAmount)
        {
            var normalizedAmount = Mathf.Max(0, baseAmount);
            if (normalizedAmount <= 0)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(normalizedAmount * GetGoldGainMultiplier()));
        }

        private float GetGoldGainMultiplier()
        {
            if (!upgradeStates.TryGetValue(UpgradeTrack.GoldGain, out var state))
            {
                return 1f;
            }

            return state.Definition.GetGoldGainMultiplier(state.Level);
        }

        private void HandleEnemyDefeated(EnemySpawnData defeatedEnemy)
        {
            if (enemyController == null)
            {
                return;
            }

            currentWave = Mathf.Max(currentWave, defeatedEnemy.Wave) + 1;
            UpdateHighestWaveReached(currentWave);
            TryGrantMilestoneReward(currentWave);
            battleSystem?.QueueEnemy(enemyController.CreateSpawnDataForWave(currentWave));
            SaveProgress();
            PublishState();
        }

        private void HandleBattleFlowReached(BattleFlowContext battleFlowContext)
        {
            researchRuntime.HandleBattleFlow(battleFlowContext);
            if (battleFlowContext.HookPoint == BattleFlowHookPoint.EnemyDefeated
                && IsResearchBossWave(battleFlowContext.Wave))
            {
                AwardResearchPoints(1);
            }
        }

        private void AwardResearchPoints(int amount)
        {
            var normalizedAmount = Mathf.Max(0, amount);
            if (normalizedAmount <= 0)
            {
                return;
            }

            researchPoints += normalizedAmount;
            SaveProgress();
            PublishState();
        }

        private static bool IsResearchBossWave(int wave)
        {
            return wave >= 10 && wave % 10 == 0;
        }

        private void TryGrantMilestoneReward(int reachedWave)
        {
            GrantMilestonesUpToWave(reachedWave);
        }

        private int GetReachedMilestoneWave(int wave)
        {
            var interval = Mathf.Max(2, milestoneWaveInterval);
            if (wave < interval)
            {
                return 0;
            }

            return wave - (wave % interval);
        }

        private int GetNextMilestoneWave(int wave)
        {
            var interval = Mathf.Max(2, milestoneWaveInterval);
            var normalizedWave = Mathf.Max(1, wave);
            var remainder = normalizedWave % interval;
            return remainder == 0 ? normalizedWave : normalizedWave + (interval - remainder);
        }

        private void GrantMilestonesUpToWave(int reachedWave)
        {
            var milestoneWave = GetReachedMilestoneWave(reachedWave);
            if (milestoneWave <= lastClaimedMilestoneWave)
            {
                return;
            }

            var interval = Mathf.Max(2, milestoneWaveInterval);
            var attackRewardGranted = false;

            for (var wave = lastClaimedMilestoneWave + interval; wave <= milestoneWave; wave += interval)
            {
                var milestoneIndex = wave / interval;
                var goldReward = milestoneGoldReward + (milestoneGoldRewardPerMilestone * Mathf.Max(0, milestoneIndex - 1));
                var attackReward = milestoneAttackPowerPerMilestone;

                lastClaimedMilestoneWave = wave;
                lastMilestoneGoldReward = Mathf.Max(0, goldReward);
                lastMilestoneAttackReward = Mathf.Max(0, attackReward);
                gold += lastMilestoneGoldReward;
                milestoneAttackBonus += lastMilestoneAttackReward;
                attackRewardGranted |= lastMilestoneAttackReward > 0;
            }

            if (attackRewardGranted)
            {
                ApplyRuntimeCombatConfiguration();
            }
        }

        private void SaveProgress()
        {
            var saveData = new SaveData
            {
                gold = gold,
                wave = currentWave,
                highestWaveReached = highestWaveReached,
                lastClaimedMilestoneWave = lastClaimedMilestoneWave,
                lastMilestoneGoldReward = lastMilestoneGoldReward,
                lastMilestoneAttackReward = lastMilestoneAttackReward,
                milestoneAttackBonus = milestoneAttackBonus,
                researchPoints = researchPoints,
            };

            foreach (var state in upgradeStates.Values)
            {
                saveData.upgrades.Add(new UpgradeSaveData
                {
                    track = state.Definition.Track,
                    level = state.Level,
                });
            }

            foreach (var entry in researchRuntime.EnumerateLevels())
            {
                saveData.researchLevels.Add(new ResearchLevelSaveData
                {
                    researchId = entry.researchId,
                    level = entry.level,
                });
            }

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        private static void DeleteSavedProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        public void EditorGrantGold(int amount)
        {
            gold += Mathf.Max(0, amount);
            SaveProgress();
            PublishState();
        }

        public void EditorJumpToNextMilestone()
        {
            EditorJumpToWave(GetNextMilestoneWave(currentWave + 1));
        }

        public void EditorJumpToWave(int targetWave)
        {
            if (enemyController == null)
            {
                return;
            }

            currentWave = Mathf.Max(1, targetWave);
            UpdateHighestWaveReached(currentWave);
            selectedStartWave = currentWave;
            GrantMilestonesUpToWave(currentWave);
            RebuildBattleState();
            SaveProgress();
            PublishState();
        }
#endif

        private void PublishState()
        {
            StateChanged?.Invoke(BuildSnapshot());
        }
    }
}
