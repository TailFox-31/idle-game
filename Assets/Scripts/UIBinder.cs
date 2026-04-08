using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleGame
{
    public sealed class UIBinder : MonoBehaviour
    {
        [Header("Readouts")]
        [SerializeField]
        private TMP_Text goldText;

        [SerializeField]
        private TMP_Text playerStatsText;

        [SerializeField]
        private TMP_Text enemyText;

        [Header("Upgrade Buttons")]
        [SerializeField]
        private Button attackPowerButton;

        [SerializeField]
        private TMP_Text attackPowerButtonText;

        [SerializeField]
        private Button attackSpeedButton;

        [SerializeField]
        private TMP_Text attackSpeedButtonText;

        private GameManager gameManager;

        public void Bind(GameManager target)
        {
            Unbind();

            gameManager = target;
            if (gameManager == null)
            {
                return;
            }

            gameManager.StateChanged += Refresh;
            Refresh(gameManager.CurrentSnapshot);
        }

        public void RequestAttackPowerUpgrade()
        {
            gameManager?.TryPurchaseUpgrade(UpgradeTrack.AttackPower);
        }

        public void RequestAttackSpeedUpgrade()
        {
            gameManager?.TryPurchaseUpgrade(UpgradeTrack.AttackSpeed);
        }

        private void OnEnable()
        {
            RegisterButtons();
        }

        private void OnDisable()
        {
            UnregisterButtons();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Refresh(GameSnapshot snapshot)
        {
            if (goldText != null)
            {
                goldText.text = $"Gold: {snapshot.Gold} | Wave {snapshot.Wave}";
            }

            if (playerStatsText != null)
            {
                playerStatsText.text = snapshot.Battle.PlayerAlive
                    ? $"HP {snapshot.Battle.PlayerHealth}/{snapshot.Battle.PlayerMaxHealth} | ATK {snapshot.PlayerStats.AttackPower} | SPD {snapshot.PlayerStats.AttacksPerSecond:0.00}"
                    : $"HP 0/{snapshot.Battle.PlayerMaxHealth} | Defeated ({snapshot.Battle.PlayerRespawnRemaining:0.0}s) | ATK {snapshot.PlayerStats.AttackPower} | SPD {snapshot.PlayerStats.AttacksPerSecond:0.00}";
            }

            if (enemyText != null)
            {
                enemyText.text = snapshot.Battle.EnemyAlive
                    ? $"Wave {snapshot.Battle.Wave} {snapshot.Battle.EnemyId} HP {snapshot.Battle.EnemyHealth}/{snapshot.Battle.EnemyMaxHealth} | ATK {snapshot.Battle.EnemyAttackPower} | SPD {snapshot.Battle.EnemyAttacksPerSecond:0.00} | Gold {snapshot.Battle.EnemyGoldReward}"
                    : $"Wave {snapshot.Battle.Wave} {snapshot.Battle.EnemyId} in {snapshot.Battle.EnemyRespawnRemaining:0.0}s | HP {snapshot.Battle.EnemyMaxHealth} | ATK {snapshot.Battle.EnemyAttackPower} | SPD {snapshot.Battle.EnemyAttacksPerSecond:0.00} | Gold {snapshot.Battle.EnemyGoldReward}";
            }

            RefreshUpgradeButton(snapshot, UpgradeTrack.AttackPower, attackPowerButton, attackPowerButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.AttackSpeed, attackSpeedButton, attackSpeedButtonText);
        }

        private void RefreshUpgradeButton(GameSnapshot snapshot, UpgradeTrack track, Button button, TMP_Text buttonText)
        {
            UpgradeViewData? data = null;
            foreach (var entry in snapshot.Upgrades)
            {
                if (entry.Track == track)
                {
                    data = entry;
                    break;
                }
            }

            if (buttonText != null && data.HasValue)
            {
                buttonText.text = $"{track} Lv.{data.Value.Level} ({data.Value.NextCost}g)";
            }

            if (button != null && data.HasValue)
            {
                button.interactable = snapshot.Gold >= data.Value.NextCost;
            }
        }

        private void RegisterButtons()
        {
            if (attackPowerButton != null)
            {
                attackPowerButton.onClick.AddListener(RequestAttackPowerUpgrade);
            }

            if (attackSpeedButton != null)
            {
                attackSpeedButton.onClick.AddListener(RequestAttackSpeedUpgrade);
            }
        }

        private void UnregisterButtons()
        {
            if (attackPowerButton != null)
            {
                attackPowerButton.onClick.RemoveListener(RequestAttackPowerUpgrade);
            }

            if (attackSpeedButton != null)
            {
                attackSpeedButton.onClick.RemoveListener(RequestAttackSpeedUpgrade);
            }
        }

        private void Unbind()
        {
            if (gameManager == null)
            {
                return;
            }

            gameManager.StateChanged -= Refresh;
            gameManager = null;
        }
    }
}
