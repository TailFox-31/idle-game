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

        [SerializeField]
        private Button defenseButton;

        [SerializeField]
        private TMP_Text defenseButtonText;

        [SerializeField]
        private Button maxHealthButton;

        [SerializeField]
        private TMP_Text maxHealthButtonText;

        [Header("Meta")]
        [SerializeField]
        private Button resetSaveButton;

        private bool ownsRuntimeResetButton;
        private GameManager gameManager;

#if UNITY_EDITOR
        private string editorJumpWaveText = "20";
#endif

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

        public void RequestDefenseUpgrade()
        {
            gameManager?.TryPurchaseUpgrade(UpgradeTrack.Defense);
        }

        public void RequestMaxHealthUpgrade()
        {
            gameManager?.TryPurchaseUpgrade(UpgradeTrack.MaxHealth);
        }

        public void RequestResetSave()
        {
            gameManager?.ResetSavedProgress();
        }

        private void OnEnable()
        {
            EnsureResetSaveButton();
            RegisterButtons();
        }

        private void OnDisable()
        {
            UnregisterButtons();
        }

        private void OnDestroy()
        {
            DestroyRuntimeResetButton();
            Unbind();
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (gameManager == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 340f, 230f, 180f), "EDITOR DEBUG", GUI.skin.window);
            GUILayout.Label("Prototype-only wave tools");

            if (GUILayout.Button("+100 Gold"))
            {
                gameManager.EditorGrantGold(100);
            }

            if (GUILayout.Button("Next Milestone"))
            {
                gameManager.EditorJumpToNextMilestone();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Wave", GUILayout.Width(42f));
            editorJumpWaveText = GUILayout.TextField(editorJumpWaveText, GUILayout.Width(52f));
            if (GUILayout.Button("Jump"))
            {
                if (int.TryParse(editorJumpWaveText, out var targetWave))
                {
                    gameManager.EditorJumpToWave(targetWave);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
#endif

        private void Refresh(GameSnapshot snapshot)
        {
            var bossTag = IsBossEnemy(snapshot.Battle.EnemyId) ? " BOSS" : string.Empty;

            if (goldText != null)
            {
                goldText.text = snapshot.LastMilestoneWave > 0
                    ? $"G {snapshot.Gold} | W{snapshot.Wave}{bossTag} | N{snapshot.NextMilestoneWave} | L{snapshot.LastMilestoneWave} +{snapshot.LastMilestoneGoldReward}g/+{snapshot.LastMilestoneAttackReward}A"
                    : $"G {snapshot.Gold} | W{snapshot.Wave}{bossTag} | N{snapshot.NextMilestoneWave}";
            }

            if (playerStatsText != null)
            {
                playerStatsText.text = snapshot.Battle.PlayerAlive
                    ? $"HP {snapshot.Battle.PlayerHealth}/{snapshot.Battle.PlayerMaxHealth} | ATK {snapshot.PlayerStats.AttackPower} | SPD {snapshot.PlayerStats.AttacksPerSecond:0.00} | DEF {snapshot.PlayerStats.FlatDamageReduction} | M+{snapshot.MilestoneAttackBonus}"
                    : $"HP 0/{snapshot.Battle.PlayerMaxHealth} | Down {snapshot.Battle.PlayerRespawnRemaining:0.0}s | ATK {snapshot.PlayerStats.AttackPower} | DEF {snapshot.PlayerStats.FlatDamageReduction}";
            }

            if (enemyText != null)
            {
                var enemyPrefix = IsBossEnemy(snapshot.Battle.EnemyId) ? "Boss " : string.Empty;
                var behaviorSuffix = string.IsNullOrWhiteSpace(snapshot.Battle.EnemyBehaviorLabel)
                    ? string.Empty
                    : $" {snapshot.Battle.EnemyBehaviorLabel}";
                enemyText.text = snapshot.Battle.EnemyAlive
                    ? $"{enemyPrefix}W{snapshot.Battle.Wave} {snapshot.Battle.EnemyId}{behaviorSuffix} {snapshot.Battle.EnemyHealth}/{snapshot.Battle.EnemyMaxHealth} | {snapshot.Battle.EnemyAttackPower}ATK {snapshot.Battle.EnemyAttacksPerSecond:0.00}SPD | {snapshot.Battle.EnemyGoldReward}g"
                    : $"{enemyPrefix}W{snapshot.Battle.Wave} {snapshot.Battle.EnemyId}{behaviorSuffix} re {snapshot.Battle.EnemyRespawnRemaining:0.0}s | {snapshot.Battle.EnemyAttackPower}ATK {snapshot.Battle.EnemyAttacksPerSecond:0.00}SPD | {snapshot.Battle.EnemyGoldReward}g";
            }

            RefreshUpgradeButton(snapshot, UpgradeTrack.AttackPower, attackPowerButton, attackPowerButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.AttackSpeed, attackSpeedButton, attackSpeedButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.Defense, defenseButton, defenseButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.MaxHealth, maxHealthButton, maxHealthButtonText);
        }

        private static UpgradeViewData? GetUpgradeViewData(GameSnapshot snapshot, UpgradeTrack track)
        {
            foreach (var entry in snapshot.Upgrades)
            {
                if (entry.Track == track)
                {
                    return entry;
                }
            }

            return null;
        }

        private void RefreshUpgradeButton(GameSnapshot snapshot, UpgradeTrack track, Button button, TMP_Text buttonText)
        {
            var data = GetUpgradeViewData(snapshot, track);

            if (buttonText != null && data.HasValue)
            {
                buttonText.text = $"{GetUpgradeLabel(track)} Lv.{data.Value.Level} ({data.Value.NextCost}g)";
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

            if (defenseButton != null)
            {
                defenseButton.onClick.AddListener(RequestDefenseUpgrade);
            }

            if (maxHealthButton != null)
            {
                maxHealthButton.onClick.AddListener(RequestMaxHealthUpgrade);
            }

            if (resetSaveButton != null)
            {
                resetSaveButton.onClick.AddListener(RequestResetSave);
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

            if (defenseButton != null)
            {
                defenseButton.onClick.RemoveListener(RequestDefenseUpgrade);
            }

            if (maxHealthButton != null)
            {
                maxHealthButton.onClick.RemoveListener(RequestMaxHealthUpgrade);
            }

            if (resetSaveButton != null)
            {
                resetSaveButton.onClick.RemoveListener(RequestResetSave);
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

        private void EnsureResetSaveButton()
        {
            if (resetSaveButton != null || enemyText == null)
            {
                return;
            }

            var parent = enemyText.rectTransform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var buttonObject = new GameObject("RuntimeResetSaveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.sizeDelta = new Vector2(180f, 44f);
            rectTransform.anchoredPosition = new Vector2(-20f, -66f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color32(122, 54, 54, 220);

            resetSaveButton = buttonObject.GetComponent<Button>();
            resetSaveButton.targetGraphic = image;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(14f, 8f);
            labelRect.offsetMax = new Vector2(-14f, -8f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Reset Save";
            label.fontSize = 20f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.color = Color.white;
            label.richText = false;

            if (label.font == null && TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            ownsRuntimeResetButton = true;
        }

        private void DestroyRuntimeResetButton()
        {
            if (!ownsRuntimeResetButton || resetSaveButton == null)
            {
                return;
            }

            Destroy(resetSaveButton.gameObject);
            resetSaveButton = null;
            ownsRuntimeResetButton = false;
        }

        private static bool IsBossEnemy(string enemyId)
        {
            return !string.IsNullOrWhiteSpace(enemyId) && enemyId.StartsWith("Boss_", System.StringComparison.Ordinal);
        }

        private static string GetUpgradeLabel(UpgradeTrack track)
        {
            return track switch
            {
                UpgradeTrack.AttackPower => "Attack",
                UpgradeTrack.MaxHealth => "Health",
                UpgradeTrack.Defense => "Defense",
                UpgradeTrack.AttackSpeed => "Speed",
                _ => track.ToString(),
            };
        }
    }
}
