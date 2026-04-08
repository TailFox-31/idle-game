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

        [SerializeField]
        private Button goldGainButton;

        [SerializeField]
        private TMP_Text goldGainButtonText;

        [Header("Meta")]
        [SerializeField]
        private Button resetSaveButton;

        [Header("Wave Travel")]
        [SerializeField]
        private TMP_Text startWaveText;

        [SerializeField]
        private Button previousWaveButton;

        [SerializeField]
        private Button nextWaveButton;

        [SerializeField]
        private Button travelButton;

        [SerializeField]
        private TMP_Text travelButtonText;

        private bool ownsRuntimeResetButton;
        private bool ownsRuntimeWaveTravelControls;
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

        public void RequestGoldGainUpgrade()
        {
            gameManager?.TryPurchaseUpgrade(UpgradeTrack.GoldGain);
        }

        public void RequestResetSave()
        {
            gameManager?.ResetSavedProgress();
        }

        public void RequestPreviousStartWave()
        {
            gameManager?.SelectPreviousStartWave();
        }

        public void RequestNextStartWave()
        {
            gameManager?.SelectNextStartWave();
        }

        public void RequestTravelToSelectedWave()
        {
            gameManager?.TravelToSelectedWave();
        }

        private void OnEnable()
        {
            EnsureResetSaveButton();
            EnsureWaveTravelControls();
            RegisterButtons();
        }

        private void OnDisable()
        {
            UnregisterButtons();
        }

        private void OnDestroy()
        {
            DestroyRuntimeResetButton();
            DestroyRuntimeWaveTravelControls();
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
            var lootMultiplier = GetGoldGainMultiplier(snapshot);
            var lootBonusPercent = Mathf.Max(0, Mathf.RoundToInt((lootMultiplier - 1f) * 100f));

            if (goldText != null)
            {
                goldText.text = snapshot.LastMilestoneWave > 0
                    ? $"G {snapshot.Gold} | Loot +{lootBonusPercent}% | W{snapshot.Wave}{bossTag} | N{snapshot.NextMilestoneWave} | L{snapshot.LastMilestoneWave} +{snapshot.LastMilestoneGoldReward}g/+{snapshot.LastMilestoneAttackReward}A"
                    : $"G {snapshot.Gold} | Loot +{lootBonusPercent}% | W{snapshot.Wave}{bossTag} | N{snapshot.NextMilestoneWave}";
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

            if (startWaveText != null)
            {
                startWaveText.text = BuildWaveTravelReadout(snapshot);
            }

            RefreshUpgradeButton(snapshot, UpgradeTrack.AttackPower, attackPowerButton, attackPowerButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.AttackSpeed, attackSpeedButton, attackSpeedButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.Defense, defenseButton, defenseButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.MaxHealth, maxHealthButton, maxHealthButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.GoldGain, goldGainButton, goldGainButtonText);

            if (previousWaveButton != null)
            {
                previousWaveButton.interactable = snapshot.SelectedStartWave > 1;
            }

            if (nextWaveButton != null)
            {
                nextWaveButton.interactable = snapshot.SelectedStartWave < snapshot.HighestWaveReached;
            }

            if (travelButton != null)
            {
                var canTravel = snapshot.SelectedStartWave != snapshot.Wave;
                travelButton.interactable = canTravel;
                if (travelButtonText != null)
                {
                    travelButtonText.text = canTravel
                        ? $"Travel to W{snapshot.SelectedStartWave}"
                        : $"At W{snapshot.Wave}";
                }
            }
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
                buttonText.text = track == UpgradeTrack.GoldGain
                    ? $"{GetUpgradeLabel(track)} +{Mathf.RoundToInt((data.Value.GoldGainMultiplier - 1f) * 100f)}% ({data.Value.NextCost}g)"
                    : $"{GetUpgradeLabel(track)} Lv.{data.Value.Level} ({data.Value.NextCost}g)";
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

            if (goldGainButton != null)
            {
                goldGainButton.onClick.AddListener(RequestGoldGainUpgrade);
            }

            if (resetSaveButton != null)
            {
                resetSaveButton.onClick.AddListener(RequestResetSave);
            }

            if (previousWaveButton != null)
            {
                previousWaveButton.onClick.AddListener(RequestPreviousStartWave);
            }

            if (nextWaveButton != null)
            {
                nextWaveButton.onClick.AddListener(RequestNextStartWave);
            }

            if (travelButton != null)
            {
                travelButton.onClick.AddListener(RequestTravelToSelectedWave);
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

            if (goldGainButton != null)
            {
                goldGainButton.onClick.RemoveListener(RequestGoldGainUpgrade);
            }

            if (resetSaveButton != null)
            {
                resetSaveButton.onClick.RemoveListener(RequestResetSave);
            }

            if (previousWaveButton != null)
            {
                previousWaveButton.onClick.RemoveListener(RequestPreviousStartWave);
            }

            if (nextWaveButton != null)
            {
                nextWaveButton.onClick.RemoveListener(RequestNextStartWave);
            }

            if (travelButton != null)
            {
                travelButton.onClick.RemoveListener(RequestTravelToSelectedWave);
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

        private void EnsureWaveTravelControls()
        {
            if (travelButtonText == null && travelButton != null)
            {
                travelButtonText = GetButtonLabel(travelButton);
            }

            if ((startWaveText != null && previousWaveButton != null && nextWaveButton != null && travelButton != null) || enemyText == null)
            {
                return;
            }

            var parent = enemyText.rectTransform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var panelObject = new GameObject("RuntimeWaveTravelPanel", typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(360f, 96f);
            panelRect.anchoredPosition = new Vector2(-20f, -122f);

            startWaveText = CreateTravelLabel(panelRect, "StartWaveLabel", "Current W1 | Start W1 | Max W1");
            previousWaveButton = CreateTravelButton(panelRect, "PrevWaveButton", new Vector2(0f, -42f), new Vector2(84f, 44f), "Prev");
            nextWaveButton = CreateTravelButton(panelRect, "NextWaveButton", new Vector2(92f, -42f), new Vector2(84f, 44f), "Next");
            travelButton = CreateTravelButton(panelRect, "TravelWaveButton", new Vector2(184f, -42f), new Vector2(176f, 44f), "Travel to W1");
            travelButtonText = GetButtonLabel(travelButton);

            ownsRuntimeWaveTravelControls = true;
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

        private void DestroyRuntimeWaveTravelControls()
        {
            if (!ownsRuntimeWaveTravelControls)
            {
                return;
            }

            var panel = startWaveText != null ? startWaveText.transform.parent : null;
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }

            startWaveText = null;
            previousWaveButton = null;
            nextWaveButton = null;
            travelButton = null;
            travelButtonText = null;
            ownsRuntimeWaveTravelControls = false;
        }

        private static TMP_Text CreateTravelLabel(RectTransform parent, string name, string text)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);

            var rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = new Vector2(0f, -30f);
            rectTransform.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.enableWordWrapping = false;
            label.color = Color.white;
            label.richText = false;

            if (label.font == null && TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            return label;
        }

        private static Button CreateTravelButton(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, string labelText)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color32(50, 72, 96, 220);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 20f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.color = Color.white;
            label.richText = false;

            if (label.font == null && TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            return button;
        }

        private static TMP_Text GetButtonLabel(Button button)
        {
            if (button == null)
            {
                return null;
            }

            var labelTransform = button.transform.Find("Label");
            return labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        }

        private static string BuildWaveTravelReadout(GameSnapshot snapshot)
        {
            return $"Current W{snapshot.Wave} | Start W{snapshot.SelectedStartWave} | Max W{snapshot.HighestWaveReached}";
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
                UpgradeTrack.GoldGain => "Bounty",
                _ => track.ToString(),
            };
        }

        private static float GetGoldGainMultiplier(GameSnapshot snapshot)
        {
            var data = GetUpgradeViewData(snapshot, UpgradeTrack.GoldGain);
            return data.HasValue ? Mathf.Max(1f, data.Value.GoldGainMultiplier) : 1f;
        }
    }
}
