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
                goldText.text = snapshot.LastMilestoneWave > 0
                    ? $"Gold {snapshot.Gold} | W{snapshot.Wave} | Next {snapshot.NextMilestoneWave} | Last {snapshot.LastMilestoneWave} +{snapshot.LastMilestoneGoldReward}g/+{snapshot.LastMilestoneAttackReward}ATK"
                    : $"Gold {snapshot.Gold} | W{snapshot.Wave} | Next {snapshot.NextMilestoneWave}";
            }

            if (playerStatsText != null)
            {
                playerStatsText.text = snapshot.Battle.PlayerAlive
                    ? $"HP {snapshot.Battle.PlayerHealth}/{snapshot.Battle.PlayerMaxHealth} | ATK {snapshot.PlayerStats.AttackPower} | SPD {snapshot.PlayerStats.AttacksPerSecond:0.00} | M+{snapshot.MilestoneAttackBonus}"
                    : $"HP 0/{snapshot.Battle.PlayerMaxHealth} | Down {snapshot.Battle.PlayerRespawnRemaining:0.0}s | ATK {snapshot.PlayerStats.AttackPower} | M+{snapshot.MilestoneAttackBonus}";
            }

            if (enemyText != null)
            {
                enemyText.text = snapshot.Battle.EnemyAlive
                    ? $"W{snapshot.Battle.Wave} {snapshot.Battle.EnemyId} {snapshot.Battle.EnemyHealth}/{snapshot.Battle.EnemyMaxHealth} | {snapshot.Battle.EnemyAttackPower}ATK {snapshot.Battle.EnemyAttacksPerSecond:0.00}SPD | {snapshot.Battle.EnemyGoldReward}g"
                    : $"W{snapshot.Battle.Wave} {snapshot.Battle.EnemyId} re {snapshot.Battle.EnemyRespawnRemaining:0.0}s | {snapshot.Battle.EnemyAttackPower}ATK {snapshot.Battle.EnemyAttacksPerSecond:0.00}SPD | {snapshot.Battle.EnemyGoldReward}g";
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
