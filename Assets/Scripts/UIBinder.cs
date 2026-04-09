using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace IdleGame
{
    public sealed class UIBinder : MonoBehaviour
    {
        private static readonly Vector2 WaveTravelPanelAnchor = new(1f, 1f);
        private static readonly Vector2 WaveTravelPanelPivot = new(1f, 1f);
        private static readonly Vector2 WaveTravelPanelSize = new(360f, 118f);
        private static readonly Vector2 WaveTravelPanelPosition = new(-20f, -122f);
        private static readonly Vector2 WaveTravelReadoutSize = new(360f, 52f);
        private static readonly Vector2 WaveTravelReadoutTopOffset = new(0f, -52f);
        private static readonly Vector2 WaveTravelButtonSize = new(84f, 44f);
        private static readonly Vector2 TravelButtonSize = new(176f, 44f);
        private static readonly Vector2 PreviousWaveButtonPosition = new(0f, -64f);
        private static readonly Vector2 NextWaveButtonPosition = new(92f, -64f);
        private static readonly Vector2 TravelButtonPosition = new(184f, -64f);
        private static readonly Vector2 RuntimeUpgradePanelAnchor = new(0f, 0f);
        private static readonly Vector2 RuntimeUpgradePanelPivot = new(0f, 0f);
        private static readonly Vector2 RuntimeUpgradePanelPosition = new(20f, 20f);
        private static readonly Vector2 RuntimeUpgradePanelSize = new(340f, 448f);
        private static readonly Vector2 RuntimeUpgradeButtonSize = new(320f, 58f);
        private static readonly Vector2 AttackPowerUpgradeButtonPosition = new(0f, 0f);
        private static readonly Vector2 MaxHealthUpgradeButtonPosition = new(0f, -74f);
        private static readonly Vector2 HealthRegenUpgradeButtonPosition = new(0f, -148f);
        private static readonly Vector2 DefenseUpgradeButtonPosition = new(0f, -222f);
        private static readonly Vector2 AttackSpeedUpgradeButtonPosition = new(0f, -296f);
        private static readonly Vector2 GoldGainUpgradeButtonPosition = new(0f, -370f);

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
        private Button healthRegenButton;

        [SerializeField]
        private TMP_Text healthRegenButtonText;

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
        private bool ownsRuntimeUpgradeControls;
        private readonly List<GameObject> runtimeUpgradeObjects = new();
        private GameManager gameManager;
        private static readonly Color32 TravelTargetHighlightColor = new(255, 214, 102, 255);

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

        public void RequestHealthRegenUpgrade()
        {
            gameManager?.TryPurchaseUpgrade(UpgradeTrack.HealthRegen);
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
            EnsureUpgradeButtons();
            ConfigureWaveTravelLayout();
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
            DestroyRuntimeUpgradeControls();
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
                var regenPerSecond = GetHealthRegenPerSecond(snapshot);
                playerStatsText.text = snapshot.Battle.PlayerAlive
                    ? $"HP {snapshot.Battle.PlayerHealth}/{snapshot.Battle.PlayerMaxHealth} | ATK {snapshot.PlayerStats.AttackPower} | SPD {snapshot.PlayerStats.AttacksPerSecond:0.00} | DEF {snapshot.PlayerStats.FlatDamageReduction} | REG {regenPerSecond:0.0}/s | M+{snapshot.MilestoneAttackBonus}"
                    : $"HP 0/{snapshot.Battle.PlayerMaxHealth} | Down {snapshot.Battle.PlayerRespawnRemaining:0.0}s | ATK {snapshot.PlayerStats.AttackPower} | DEF {snapshot.PlayerStats.FlatDamageReduction} | REG {regenPerSecond:0.0}/s";
            }

            if (enemyText != null)
            {
                var enemyPrefix = IsBossEnemy(snapshot.Battle.EnemyId) ? "Boss " : string.Empty;
                var behaviorSuffix = string.IsNullOrWhiteSpace(snapshot.Battle.EnemyBehaviorLabel)
                    ? string.Empty
                    : $" {snapshot.Battle.EnemyBehaviorLabel}";
                var stateSuffix = string.IsNullOrWhiteSpace(snapshot.Battle.EnemyStateLabel)
                    ? string.Empty
                    : $" | {snapshot.Battle.EnemyStateLabel}";
                enemyText.text = snapshot.Battle.EnemyAlive
                    ? $"{enemyPrefix}W{snapshot.Battle.Wave} {snapshot.Battle.EnemyId}{behaviorSuffix} {snapshot.Battle.EnemyHealth}/{snapshot.Battle.EnemyMaxHealth} | {snapshot.Battle.EnemyAttackPower}ATK {snapshot.Battle.EnemyAttacksPerSecond:0.00}SPD | {snapshot.Battle.EnemyGoldReward}g{stateSuffix}"
                    : $"{enemyPrefix}W{snapshot.Battle.Wave} {snapshot.Battle.EnemyId}{behaviorSuffix} re {snapshot.Battle.EnemyRespawnRemaining:0.0}s | {snapshot.Battle.EnemyAttackPower}ATK {snapshot.Battle.EnemyAttacksPerSecond:0.00}SPD | {snapshot.Battle.EnemyGoldReward}g";
            }

            if (startWaveText != null)
            {
                ConfigureWaveTravelReadout();
                startWaveText.text = BuildWaveTravelReadout(snapshot);
            }

            RefreshUpgradeButton(snapshot, UpgradeTrack.AttackPower, attackPowerButton, attackPowerButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.MaxHealth, maxHealthButton, maxHealthButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.HealthRegen, healthRegenButton, healthRegenButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.Defense, defenseButton, defenseButtonText);
            RefreshUpgradeButton(snapshot, UpgradeTrack.AttackSpeed, attackSpeedButton, attackSpeedButtonText);
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
                        : $"Already at W{snapshot.Wave}";
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
                buttonText.text = BuildUpgradeButtonText(track, data.Value);
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

            if (maxHealthButton != null)
            {
                maxHealthButton.onClick.AddListener(RequestMaxHealthUpgrade);
            }

            if (healthRegenButton != null)
            {
                healthRegenButton.onClick.AddListener(RequestHealthRegenUpgrade);
            }

            if (defenseButton != null)
            {
                defenseButton.onClick.AddListener(RequestDefenseUpgrade);
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

            if (maxHealthButton != null)
            {
                maxHealthButton.onClick.RemoveListener(RequestMaxHealthUpgrade);
            }

            if (healthRegenButton != null)
            {
                healthRegenButton.onClick.RemoveListener(RequestHealthRegenUpgrade);
            }

            if (defenseButton != null)
            {
                defenseButton.onClick.RemoveListener(RequestDefenseUpgrade);
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

        private void EnsureUpgradeButtons()
        {
            if (HasAllUpgradeButtons())
            {
                EnsureUpgradeButtonLabels();
                return;
            }

            var parent = GetUpgradeParent();
            if (parent == null)
            {
                return;
            }

            ConfigureUpgradePanelRect(parent);

            attackPowerButton = EnsureUpgradeButton(parent, attackPowerButton, "AttackPowerUpgradeButton", AttackPowerUpgradeButtonPosition, "Attack +0 (10g)", ref ownsRuntimeUpgradeControls);
            attackPowerButtonText = GetButtonLabel(attackPowerButton);
            maxHealthButton = EnsureUpgradeButton(parent, maxHealthButton, "MaxHealthUpgradeButton", MaxHealthUpgradeButtonPosition, "Health +0 (14g)", ref ownsRuntimeUpgradeControls);
            maxHealthButtonText = GetButtonLabel(maxHealthButton);
            healthRegenButton = EnsureUpgradeButton(parent, healthRegenButton, "HealthRegenUpgradeButton", HealthRegenUpgradeButtonPosition, "Regen +0.0/s (15g)", ref ownsRuntimeUpgradeControls);
            healthRegenButtonText = GetButtonLabel(healthRegenButton);
            defenseButton = EnsureUpgradeButton(parent, defenseButton, "DefenseUpgradeButton", DefenseUpgradeButtonPosition, "Defense -0 dmg (14g)", ref ownsRuntimeUpgradeControls);
            defenseButtonText = GetButtonLabel(defenseButton);
            attackSpeedButton = EnsureUpgradeButton(parent, attackSpeedButton, "AttackSpeedUpgradeButton", AttackSpeedUpgradeButtonPosition, "Speed +0.00/s (16g)", ref ownsRuntimeUpgradeControls);
            attackSpeedButtonText = GetButtonLabel(attackSpeedButton);
            goldGainButton = EnsureUpgradeButton(parent, goldGainButton, "GoldGainUpgradeButton", GoldGainUpgradeButtonPosition, "Bounty +0% (18g)", ref ownsRuntimeUpgradeControls);
            goldGainButtonText = GetButtonLabel(goldGainButton);
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
            ConfigureWaveTravelPanelRect(panelRect);

            startWaveText = CreateTravelLabel(panelRect, "StartWaveLabel", "Now W1\nTarget W1 | Best W1");
            previousWaveButton = CreateTravelButton(panelRect, "PrevWaveButton", PreviousWaveButtonPosition, WaveTravelButtonSize, "Prev");
            nextWaveButton = CreateTravelButton(panelRect, "NextWaveButton", NextWaveButtonPosition, WaveTravelButtonSize, "Next");
            travelButton = CreateTravelButton(panelRect, "TravelWaveButton", TravelButtonPosition, TravelButtonSize, "Travel to W1");
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

        private void DestroyRuntimeUpgradeControls()
        {
            if (!ownsRuntimeUpgradeControls)
            {
                return;
            }

            for (var index = 0; index < runtimeUpgradeObjects.Count; index++)
            {
                if (runtimeUpgradeObjects[index] != null)
                {
                    Destroy(runtimeUpgradeObjects[index]);
                }
            }

            runtimeUpgradeObjects.Clear();
            attackPowerButton = null;
            attackPowerButtonText = null;
            maxHealthButton = null;
            maxHealthButtonText = null;
            healthRegenButton = null;
            healthRegenButtonText = null;
            defenseButton = null;
            defenseButtonText = null;
            attackSpeedButton = null;
            attackSpeedButtonText = null;
            goldGainButton = null;
            goldGainButtonText = null;
            ownsRuntimeUpgradeControls = false;
        }

        private static TMP_Text CreateTravelLabel(RectTransform parent, string name, string text)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);

            var rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = new Vector2(0f, -52f);
            rectTransform.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 20f;
            label.alignment = TextAlignmentOptions.TopRight;
            label.enableWordWrapping = false;
            label.color = Color.white;
            label.richText = true;

            if (label.font == null && TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            return label;
        }

        private void ConfigureWaveTravelLayout()
        {
            RectTransform panelRect = null;
            if (startWaveText != null)
            {
                panelRect = startWaveText.rectTransform.parent as RectTransform;
            }
            else if (travelButton != null)
            {
                panelRect = travelButton.transform.parent as RectTransform;
            }
            else if (previousWaveButton != null)
            {
                panelRect = previousWaveButton.transform.parent as RectTransform;
            }
            else if (nextWaveButton != null)
            {
                panelRect = nextWaveButton.transform.parent as RectTransform;
            }

            if (panelRect != null)
            {
                ConfigureWaveTravelPanelRect(panelRect);
            }

            ConfigureWaveTravelReadout();
            ConfigureWaveTravelButton(previousWaveButton, PreviousWaveButtonPosition, WaveTravelButtonSize);
            ConfigureWaveTravelButton(nextWaveButton, NextWaveButtonPosition, WaveTravelButtonSize);
            ConfigureWaveTravelButton(travelButton, TravelButtonPosition, TravelButtonSize);
        }

        private void ConfigureWaveTravelReadout()
        {
            if (startWaveText == null)
            {
                return;
            }

            startWaveText.richText = true;
            startWaveText.enableWordWrapping = false;
            startWaveText.alignment = TextAlignmentOptions.TopRight;

            var rectTransform = startWaveText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.sizeDelta = WaveTravelReadoutSize;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = new Vector2(0f, WaveTravelReadoutTopOffset.y);
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void ConfigureWaveTravelPanelRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = WaveTravelPanelAnchor;
            rectTransform.anchorMax = WaveTravelPanelAnchor;
            rectTransform.pivot = WaveTravelPanelPivot;
            rectTransform.sizeDelta = WaveTravelPanelSize;
            rectTransform.anchoredPosition = WaveTravelPanelPosition;
        }

        private static void ConfigureWaveTravelButton(Button button, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (button == null)
            {
                return;
            }

            var rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
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

        private static Button CreateRuntimeUpgradeButton(RectTransform parent, string name, Vector2 anchoredPosition, string labelText)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.sizeDelta = RuntimeUpgradeButtonSize;
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
            labelRect.offsetMin = new Vector2(14f, 8f);
            labelRect.offsetMax = new Vector2(-14f, -8f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 24f;
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

        private static void ConfigureUpgradePanelRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = RuntimeUpgradePanelAnchor;
            rectTransform.anchorMax = RuntimeUpgradePanelAnchor;
            rectTransform.pivot = RuntimeUpgradePanelPivot;
            rectTransform.anchoredPosition = RuntimeUpgradePanelPosition;
            rectTransform.sizeDelta = RuntimeUpgradePanelSize;
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
            var nowText = $"Now W{snapshot.Wave}";
            var bestText = $"Best W{snapshot.HighestWaveReached}";
            var targetText = $"Target W{snapshot.SelectedStartWave}";

            if (snapshot.SelectedStartWave == snapshot.Wave)
            {
                return $"{nowText}\n{targetText} | {bestText}";
            }

            var targetColor = ColorUtility.ToHtmlStringRGB(TravelTargetHighlightColor);
            return $"{nowText}\n<color=#{targetColor}>{targetText}</color> | {bestText}";
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
                UpgradeTrack.HealthRegen => "Regen",
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

        private static float GetHealthRegenPerSecond(GameSnapshot snapshot)
        {
            var data = GetUpgradeViewData(snapshot, UpgradeTrack.HealthRegen);
            return data.HasValue ? Mathf.Max(0f, data.Value.HealthRegenPerSecond) : 0f;
        }

        private static string BuildUpgradeButtonText(UpgradeTrack track, UpgradeViewData data)
        {
            return track switch
            {
                UpgradeTrack.AttackPower => $"{GetUpgradeLabel(track)} +{data.AttackPowerBonus} ({data.NextCost}g)",
                UpgradeTrack.MaxHealth => $"{GetUpgradeLabel(track)} +{data.MaxHealthBonus} ({data.NextCost}g)",
                UpgradeTrack.Defense => $"{GetUpgradeLabel(track)} -{data.FlatDamageReduction} dmg ({data.NextCost}g)",
                UpgradeTrack.AttackSpeed => $"{GetUpgradeLabel(track)} +{data.AttackSpeedBonus:0.00}/s ({data.NextCost}g)",
                UpgradeTrack.GoldGain => $"{GetUpgradeLabel(track)} +{Mathf.RoundToInt((data.GoldGainMultiplier - 1f) * 100f)}% ({data.NextCost}g)",
                UpgradeTrack.HealthRegen => $"{GetUpgradeLabel(track)} +{data.HealthRegenPerSecond:0.0}/s ({data.NextCost}g)",
                _ => $"{GetUpgradeLabel(track)} Lv.{data.Level} ({data.NextCost}g)",
            };
        }

        private void EnsureUpgradeButtonLabels()
        {
            if (attackPowerButtonText == null && attackPowerButton != null)
            {
                attackPowerButtonText = GetButtonLabel(attackPowerButton);
            }

            if (maxHealthButtonText == null && maxHealthButton != null)
            {
                maxHealthButtonText = GetButtonLabel(maxHealthButton);
            }

            if (healthRegenButtonText == null && healthRegenButton != null)
            {
                healthRegenButtonText = GetButtonLabel(healthRegenButton);
            }

            if (defenseButtonText == null && defenseButton != null)
            {
                defenseButtonText = GetButtonLabel(defenseButton);
            }

            if (attackSpeedButtonText == null && attackSpeedButton != null)
            {
                attackSpeedButtonText = GetButtonLabel(attackSpeedButton);
            }

            if (goldGainButtonText == null && goldGainButton != null)
            {
                goldGainButtonText = GetButtonLabel(goldGainButton);
            }
        }

        private RectTransform GetUpgradeRootParent()
        {
            if (attackPowerButton != null && attackPowerButton.transform.parent is RectTransform existingParent)
            {
                return existingParent;
            }

            if (maxHealthButton != null && maxHealthButton.transform.parent is RectTransform healthParent)
            {
                return healthParent;
            }

            if (playerStatsText != null && playerStatsText.rectTransform.parent is RectTransform statsParent)
            {
                return statsParent;
            }

            if (goldText != null && goldText.rectTransform.parent is RectTransform goldParent)
            {
                return goldParent;
            }

            return transform as RectTransform;
        }

        private bool HasAllUpgradeButtons()
        {
            return attackPowerButton != null
                && maxHealthButton != null
                && healthRegenButton != null
                && defenseButton != null
                && attackSpeedButton != null
                && goldGainButton != null;
        }

        private RectTransform GetUpgradeParent()
        {
            var existingParent = GetUpgradeRootParent();
            if (existingParent == null)
            {
                return null;
            }

            if (HasAnyUpgradeButton())
            {
                return existingParent;
            }

            var panelObject = new GameObject("RuntimeUpgradesPanel", typeof(RectTransform));
            panelObject.transform.SetParent(existingParent, false);
            runtimeUpgradeObjects.Add(panelObject);

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = RuntimeUpgradePanelAnchor;
            panelRect.anchorMax = RuntimeUpgradePanelAnchor;
            panelRect.pivot = RuntimeUpgradePanelPivot;
            panelRect.anchoredPosition = RuntimeUpgradePanelPosition;
            panelRect.sizeDelta = RuntimeUpgradePanelSize;
            ownsRuntimeUpgradeControls = true;
            return panelRect;
        }

        private bool HasAnyUpgradeButton()
        {
            return attackPowerButton != null
                || maxHealthButton != null
                || healthRegenButton != null
                || defenseButton != null
                || attackSpeedButton != null
                || goldGainButton != null;
        }

        private Button EnsureUpgradeButton(RectTransform parent, Button existingButton, string name, Vector2 position, string labelText, ref bool ownsRuntimeControls)
        {
            if (existingButton != null)
            {
                return existingButton;
            }

            ownsRuntimeControls = true;
            var createdButton = CreateRuntimeUpgradeButton(parent, name, position, labelText);
            runtimeUpgradeObjects.Add(createdButton.gameObject);
            return createdButton;
        }
    }
}
