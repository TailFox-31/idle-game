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
        public GameSnapshot(int gold, CombatantStats playerStats, BattleSnapshot battle, UpgradeViewData[] upgrades)
        {
            Gold = gold;
            PlayerStats = playerStats;
            Battle = battle;
            Upgrades = upgrades;
        }

        public int Gold { get; }

        public CombatantStats PlayerStats { get; }

        public BattleSnapshot Battle { get; }

        public UpgradeViewData[] Upgrades { get; }
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

        [SerializeField]
        private List<UpgradeDefinition> upgrades = new()
        {
            new UpgradeDefinition(UpgradeTrack.AttackPower, 10, 1.5f, attackPowerPerLevel: 1),
            new UpgradeDefinition(UpgradeTrack.AttackSpeed, 15, 1.5f, attackSpeedPerLevel: 0.15f),
        };

        private readonly Dictionary<UpgradeTrack, UpgradeState> upgradeStates = new();
        private AutoBattleSystem battleSystem;
        private int gold;

        public event Action<GameSnapshot> StateChanged;

        public GameSnapshot CurrentSnapshot => BuildSnapshot();

        private void Awake()
        {
            gold = Mathf.Max(0, startingGold);
            InitializeUpgradeStates();
            InitializeBattleSystem();

            if (uiBinder != null)
            {
                uiBinder.Bind(this);
            }

            PublishState();
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

        private void InitializeBattleSystem()
        {
            if (enemyController == null)
            {
                return;
            }

            battleSystem = new AutoBattleSystem(BuildPlayerStats(), enemyController.CreateSpawnData(), playerRespawnDelay);
            battleSystem.GoldAwarded += HandleGoldAwarded;
            battleSystem.BattleStateChanged += _ => PublishState();
        }

        private CombatantStats BuildPlayerStats()
        {
            var stats = playerBaseStats;
            foreach (var state in upgradeStates.Values)
            {
                stats = state.Definition.Apply(stats, state.Level);
            }

            return stats;
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
                : new BattleSnapshot(string.Empty, 0, 0, false, 0f, 0, 0, false, 0f);

            return new GameSnapshot(gold, BuildPlayerStats(), battle, upgradeData);
        }

        private void HandleGoldAwarded(int amount)
        {
            gold += Mathf.Max(0, amount);
            PublishState();
        }

        private void PublishState()
        {
            StateChanged?.Invoke(BuildSnapshot());
        }
    }
}
