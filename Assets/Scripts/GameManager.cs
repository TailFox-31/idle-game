using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame
{
    public readonly struct UpgradeViewData
    {
        public UpgradeViewData(UpgradeTrack track, int level, int nextCost)
        {
            Track = track;
            Level = level;
            NextCost = nextCost;
        }

        public UpgradeTrack Track { get; }

        public int Level { get; }

        public int NextCost { get; }
    }

    public readonly struct GameSnapshot
    {
        public GameSnapshot(
            int gold,
            int wave,
            CombatantStats playerStats,
            BattleSnapshot battle,
            UpgradeViewData[] upgrades,
            int nextMilestoneWave,
            int milestoneAttackBonus,
            int lastMilestoneWave,
            int lastMilestoneGoldReward,
            int lastMilestoneAttackReward)
        {
            Gold = gold;
            Wave = wave;
            PlayerStats = playerStats;
            Battle = battle;
            Upgrades = upgrades;
            NextMilestoneWave = nextMilestoneWave;
            MilestoneAttackBonus = milestoneAttackBonus;
            LastMilestoneWave = lastMilestoneWave;
            LastMilestoneGoldReward = lastMilestoneGoldReward;
            LastMilestoneAttackReward = lastMilestoneAttackReward;
        }

        public int Gold { get; }

        public int Wave { get; }

        public CombatantStats PlayerStats { get; }

        public BattleSnapshot Battle { get; }

        public UpgradeViewData[] Upgrades { get; }

        public int NextMilestoneWave { get; }

        public int MilestoneAttackBonus { get; }

        public int LastMilestoneWave { get; }

        public int LastMilestoneGoldReward { get; }

        public int LastMilestoneAttackReward { get; }
    }

    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField]
        private CombatantStats playerBaseStats = new CombatantStats(30, 2, 1f);

        [SerializeField, Min(0)]
        private int startingGold;

        [SerializeField]
        private EnemyController enemyController;

        [SerializeField]
        private UIBinder uiBinder;

        [SerializeField, Min(0f)]
        private float playerRespawnDelay = 2f;

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
        private List<UpgradeDefinition> upgrades = new()
        {
            new UpgradeDefinition(UpgradeTrack.AttackPower, 10, 1.28f, attackPowerPerLevel: 1),
            new UpgradeDefinition(UpgradeTrack.MaxHealth, 14, 1.34f, maxHealthPerLevel: 10),
            new UpgradeDefinition(UpgradeTrack.Defense, 16, 1.38f, flatDamageReductionPerLevel: 1),
            new UpgradeDefinition(UpgradeTrack.AttackSpeed, 20, 1.42f, attackSpeedPerLevel: 0.12f),
        };

        private readonly Dictionary<UpgradeTrack, UpgradeState> upgradeStates = new();
        private AutoBattleSystem battleSystem;
        private int gold;
        private int currentWave = 1;
        private int lastClaimedMilestoneWave;
        private int lastMilestoneGoldReward;
        private int lastMilestoneAttackReward;
        private int milestoneAttackBonus;

        public event Action<GameSnapshot> StateChanged;

        public GameSnapshot CurrentSnapshot => BuildSnapshot();

        private void Awake()
        {
            gold = Mathf.Max(0, startingGold);
            EnsureUpgradeDefinitions();
            InitializeUpgradeStates();
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

            battleSystem?.SetPlayerStats(BuildPlayerStats());
            PublishState();
            return true;
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

        private void EnsureUpgradeDefinitions()
        {
            if (HasExpectedUpgradeTracks(upgrades))
            {
                return;
            }

            upgrades = BuildDefaultUpgradeDefinitions();
        }

        private static bool HasExpectedUpgradeTracks(List<UpgradeDefinition> definitions)
        {
            if (definitions == null || definitions.Count != 4)
            {
                return false;
            }

            var seenAttackPower = false;
            var seenMaxHealth = false;
            var seenDefense = false;
            var seenAttackSpeed = false;

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    return false;
                }

                switch (definition.Track)
                {
                    case UpgradeTrack.AttackPower:
                        if (seenAttackPower)
                        {
                            return false;
                        }

                        seenAttackPower = true;
                        break;
                    case UpgradeTrack.MaxHealth:
                        if (seenMaxHealth)
                        {
                            return false;
                        }

                        seenMaxHealth = true;
                        break;
                    case UpgradeTrack.Defense:
                        if (seenDefense)
                        {
                            return false;
                        }

                        seenDefense = true;
                        break;
                    case UpgradeTrack.AttackSpeed:
                        if (seenAttackSpeed)
                        {
                            return false;
                        }

                        seenAttackSpeed = true;
                        break;
                    default:
                        return false;
                }
            }

            return seenAttackPower && seenMaxHealth && seenDefense && seenAttackSpeed;
        }

        private static List<UpgradeDefinition> BuildDefaultUpgradeDefinitions()
        {
            return new List<UpgradeDefinition>
            {
                new UpgradeDefinition(UpgradeTrack.AttackPower, 10, 1.28f, attackPowerPerLevel: 1),
                new UpgradeDefinition(UpgradeTrack.MaxHealth, 14, 1.34f, maxHealthPerLevel: 10),
                new UpgradeDefinition(UpgradeTrack.Defense, 16, 1.38f, flatDamageReductionPerLevel: 1),
                new UpgradeDefinition(UpgradeTrack.AttackSpeed, 20, 1.42f, attackSpeedPerLevel: 0.12f),
            };
        }

        private void InitializeBattleSystem()
        {
            if (enemyController == null)
            {
                return;
            }

            battleSystem = new AutoBattleSystem(BuildPlayerStats(), enemyController.CreateSpawnDataForWave(currentWave), playerRespawnDelay);
            battleSystem.GoldAwarded += HandleGoldAwarded;
            battleSystem.EnemyDefeated += HandleEnemyDefeated;
            battleSystem.BattleStateChanged += _ => PublishState();
        }

        private CombatantStats BuildPlayerStats()
        {
            var stats = playerBaseStats;
            foreach (var state in upgradeStates.Values)
            {
                stats = state.Definition.Apply(stats, state.Level);
            }

            return stats.Add(attackPower: milestoneAttackBonus);
        }

        private GameSnapshot BuildSnapshot()
        {
            var upgradeData = new UpgradeViewData[upgradeStates.Count];
            var index = 0;
            foreach (var state in upgradeStates.Values)
            {
                upgradeData[index++] = new UpgradeViewData(state.Definition.Track, state.Level, state.CurrentCost);
            }

            var battle = battleSystem != null
                ? battleSystem.Snapshot
                : new BattleSnapshot(0, string.Empty, 0, 0, false, 0f, 0, 0, 0, 0f, 0, string.Empty, false, 0f);

            return new GameSnapshot(
                gold,
                currentWave,
                BuildPlayerStats(),
                battle,
                upgradeData,
                GetNextMilestoneWave(currentWave),
                milestoneAttackBonus,
                lastClaimedMilestoneWave,
                lastMilestoneGoldReward,
                lastMilestoneAttackReward);
        }

        private void HandleGoldAwarded(int amount)
        {
            gold += Mathf.Max(0, amount);
            PublishState();
        }

        private void HandleEnemyDefeated(EnemySpawnData defeatedEnemy)
        {
            if (enemyController == null)
            {
                return;
            }

            currentWave = Mathf.Max(currentWave, defeatedEnemy.Wave) + 1;
            TryGrantMilestoneReward(currentWave);
            battleSystem?.QueueEnemy(enemyController.CreateSpawnDataForWave(currentWave));
            PublishState();
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
                battleSystem?.SetPlayerStats(BuildPlayerStats());
            }
        }

#if UNITY_EDITOR
        public void EditorGrantGold(int amount)
        {
            gold += Mathf.Max(0, amount);
            PublishState();
        }

        public void EditorJumpToNextMilestone()
        {
            EditorJumpToWave(GetNextMilestoneWave(currentWave + 1));
        }

        public void EditorJumpToWave(int targetWave)
        {
            if (enemyController == null || battleSystem == null)
            {
                return;
            }

            currentWave = Mathf.Max(1, targetWave);
            GrantMilestonesUpToWave(currentWave);
            battleSystem.SetEnemy(enemyController.CreateSpawnDataForWave(currentWave));
            PublishState();
        }
#endif

        private void PublishState()
        {
            StateChanged?.Invoke(BuildSnapshot());
        }
    }
}
