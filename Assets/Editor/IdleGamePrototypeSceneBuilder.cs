using IdleGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class IdleGamePrototypeSceneBuilder
{
    private const string MenuItemPath = "Tools/Idle Game/Build Or Refresh Prototype Scene Setup";
    private const string PrototypeCanvasName = "PrototypeCanvas";
    private const string UiRootName = "PrototypeUI";
    private const string SystemsRootName = "PrototypeSystems";
    private const string HeaderPanelName = "HeaderPanel";
    private const string UpgradesPanelName = "UpgradesPanel";
    private const string GoldReadoutName = "GoldReadout";
    private const string PlayerStatsReadoutName = "PlayerStatsReadout";
    private const string EnemyStatusReadoutName = "EnemyStatusReadout";
    private const string AttackPowerButtonName = "AttackPowerUpgradeButton";
    private const string AttackSpeedButtonName = "AttackSpeedUpgradeButton";
    private const string DefenseButtonName = "DefenseUpgradeButton";
    private const string MaxHealthButtonName = "MaxHealthUpgradeButton";
    private const string GoldGainButtonName = "GoldGainUpgradeButton";
    private const string ResetSaveButtonName = "ResetSaveButton";
    private const string LabelChildName = "Label";
    private const string GameManagerName = "GameManager";
    private const string EnemyControllerName = "EnemyController";
    private const string UIBinderName = "UIBinder";

    [MenuItem(MenuItemPath)]
    private static void BuildOrRefreshPrototypeSceneSetup()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Idle Game prototype scene builder requires an open, loaded scene.");
            return;
        }

        if (!string.Equals(scene.name, "SampleScene", System.StringComparison.Ordinal))
        {
            Debug.LogWarning($"Idle Game prototype scene builder is intended for SampleScene, but the current scene is '{scene.name}'.");
        }

        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Idle Game Prototype Scene Setup");

        var canvas = EnsureCanvas(scene);
        var uiRoot = EnsureChildRectTransform(canvas.transform, UiRootName);
        StretchToParent(uiRoot);

        var headerPanel = EnsureChildRectTransform(uiRoot, HeaderPanelName);
        ConfigureHeaderPanel(headerPanel);

        var goldReadout = EnsureReadout(headerPanel, GoldReadoutName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), "G 0 | Loot +0% | W1 | N5");
        var playerStatsReadout = EnsureReadout(headerPanel, PlayerStatsReadoutName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -66f), "ATK 0 | SPD 0.00 | DEF 0");
        var enemyStatusReadout = EnsureReadout(headerPanel, EnemyStatusReadoutName, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), "Enemy HP 0/0");
        enemyStatusReadout.alignment = TextAlignmentOptions.TopRight;
        var resetSaveButton = EnsureButton(headerPanel, ResetSaveButtonName, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -66f), "Reset Save", new Vector2(180f, 44f), 20f);
        resetSaveButton.GetComponent<Image>().color = new Color32(122, 54, 54, 220);

        var upgradesPanel = EnsureChildRectTransform(uiRoot, UpgradesPanelName);
        ConfigureUpgradePanel(upgradesPanel);

        var attackPowerButton = EnsureButton(upgradesPanel, AttackPowerButtonName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), "Attack Lv.0 (10g)");
        var maxHealthButton = EnsureButton(upgradesPanel, MaxHealthButtonName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -74f), "Health Lv.0 (16g)");
        var defenseButton = EnsureButton(upgradesPanel, DefenseButtonName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -148f), "Defense Lv.0 (18g)");
        var attackSpeedButton = EnsureButton(upgradesPanel, AttackSpeedButtonName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -222f), "Speed Lv.0 (24g)");
        var goldGainButton = EnsureButton(upgradesPanel, GoldGainButtonName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -296f), "Bounty +0% (24g)");

        EnsureEventSystem(scene);

        var systemsRoot = EnsureSceneObject(scene, SystemsRootName, parent: null);
        var gameManagerObject = EnsureSceneObject(scene, GameManagerName, systemsRoot.transform);
        var enemyControllerObject = EnsureSceneObject(scene, EnemyControllerName, systemsRoot.transform);
        var uiBinderObject = EnsureSceneObject(scene, UIBinderName, systemsRoot.transform);

        var gameManager = EnsureComponent<GameManager>(gameManagerObject);
        var enemyController = EnsureComponent<EnemyController>(enemyControllerObject);
        var uiBinder = EnsureComponent<UIBinder>(uiBinderObject);

        WireGameManager(gameManager, enemyController, uiBinder);
        WireUiBinder(uiBinder, goldReadout, playerStatsReadout, enemyStatusReadout, attackPowerButton, maxHealthButton, defenseButton, attackSpeedButton, goldGainButton, resetSaveButton);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = systemsRoot;
        Undo.CollapseUndoOperations(undoGroup);

        var tmpFont = TMP_Settings.defaultFontAsset;
        if (tmpFont == null)
        {
            Debug.LogWarning("Idle Game prototype scene builder created TMP readouts without a default TMP font asset. Import TMP Essentials if text appears missing.");
        }

        Debug.Log("Idle Game prototype scene setup refreshed for the currently open scene. Review the generated hierarchy, then save the scene manually if the layout looks correct.");
    }

    private static Canvas EnsureCanvas(Scene scene)
    {
        var canvasObject = FindSceneObject(scene, PrototypeCanvasName);
        if (canvasObject == null)
        {
            canvasObject = new GameObject(PrototypeCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create PrototypeCanvas");
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
        }

        var canvas = EnsureComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = EnsureComponent<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        EnsureComponent<GraphicRaycaster>(canvasObject);

        var rectTransform = canvasObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return canvas;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        var eventSystemObject = FindSceneObject(scene, "EventSystem");
        if (eventSystemObject == null)
        {
            eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            return;
        }

        EnsureComponent<EventSystem>(eventSystemObject);
        var inputModule = eventSystemObject.GetComponent<BaseInputModule>();
        if (inputModule == null)
        {
            EnsureComponent<InputSystemUIInputModule>(eventSystemObject);
        }
        else if (inputModule is not InputSystemUIInputModule)
        {
            Debug.LogWarning($"EventSystem already uses {inputModule.GetType().Name}. Verify UI clicks still work with the project's input handling.");
        }
    }

    private static void WireGameManager(GameManager gameManager, EnemyController enemyController, UIBinder uiBinder)
    {
        var serializedObject = new SerializedObject(gameManager);
        SetObjectReference(serializedObject, "enemyController", enemyController);
        SetObjectReference(serializedObject, "uiBinder", uiBinder);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);
    }

    private static void WireUiBinder(
        UIBinder uiBinder,
        TMP_Text goldReadout,
        TMP_Text playerStatsReadout,
        TMP_Text enemyStatusReadout,
        Button attackPowerButton,
        Button maxHealthButton,
        Button defenseButton,
        Button attackSpeedButton,
        Button goldGainButton,
        Button resetSaveButton)
    {
        var serializedObject = new SerializedObject(uiBinder);
        SetObjectReference(serializedObject, "goldText", goldReadout);
        SetObjectReference(serializedObject, "playerStatsText", playerStatsReadout);
        SetObjectReference(serializedObject, "enemyText", enemyStatusReadout);
        SetObjectReference(serializedObject, "attackPowerButton", attackPowerButton);
        SetObjectReference(serializedObject, "attackPowerButtonText", GetButtonLabel(attackPowerButton));
        SetObjectReference(serializedObject, "maxHealthButton", maxHealthButton);
        SetObjectReference(serializedObject, "maxHealthButtonText", GetButtonLabel(maxHealthButton));
        SetObjectReference(serializedObject, "defenseButton", defenseButton);
        SetObjectReference(serializedObject, "defenseButtonText", GetButtonLabel(defenseButton));
        SetObjectReference(serializedObject, "attackSpeedButton", attackSpeedButton);
        SetObjectReference(serializedObject, "attackSpeedButtonText", GetButtonLabel(attackSpeedButton));
        SetObjectReference(serializedObject, "goldGainButton", goldGainButton);
        SetObjectReference(serializedObject, "goldGainButtonText", GetButtonLabel(goldGainButton));
        SetObjectReference(serializedObject, "resetSaveButton", resetSaveButton);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiBinder);
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"{serializedObject.targetObject.GetType().Name} no longer exposes serialized field '{propertyName}'. Manual hookup may be required.");
            return;
        }

        property.objectReferenceValue = value;
    }

    private static RectTransform EnsureChildRectTransform(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            var childObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {name}");
            child = childObject.transform;
            child.SetParent(parent, false);
        }

        return child.GetComponent<RectTransform>();
    }

    private static TMP_Text EnsureReadout(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        string defaultText)
    {
        var readoutTransform = EnsureChildRectTransform(parent, name);
        readoutTransform.anchorMin = anchorMin;
        readoutTransform.anchorMax = anchorMax;
        readoutTransform.pivot = new Vector2(anchorMin.x, anchorMax.y);
        readoutTransform.sizeDelta = new Vector2(620f, 32f);
        readoutTransform.anchoredPosition = anchoredPosition;

        var label = EnsureComponent<TextMeshProUGUI>(readoutTransform.gameObject);
        label.fontSize = 28f;
        label.text = defaultText;
        label.color = new Color32(245, 245, 245, 255);
        label.enableWordWrapping = false;
        label.richText = false;
        label.alignment = TextAlignmentOptions.TopLeft;

        if (label.font == null && TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        return label;
    }

    private static Button EnsureButton(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        string labelText,
        Vector2? sizeDelta = null,
        float fontSize = 24f)
    {
        var buttonTransform = EnsureChildRectTransform(parent, name);
        buttonTransform.anchorMin = anchorMin;
        buttonTransform.anchorMax = anchorMax;
        buttonTransform.pivot = new Vector2(anchorMax.x, anchorMax.y);
        buttonTransform.sizeDelta = sizeDelta ?? new Vector2(320f, 58f);
        buttonTransform.anchoredPosition = anchoredPosition;

        var image = EnsureComponent<Image>(buttonTransform.gameObject);
        image.color = new Color32(50, 72, 96, 220);

        var button = EnsureComponent<Button>(buttonTransform.gameObject);
        button.targetGraphic = image;

        var labelTransform = EnsureChildRectTransform(buttonTransform, LabelChildName);
        StretchToParent(labelTransform, new Vector2(14f, 8f), new Vector2(-14f, -8f));

        var label = EnsureComponent<TextMeshProUGUI>(labelTransform.gameObject);
        label.text = labelText;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.color = new Color32(255, 255, 255, 255);
        label.richText = false;

        if (label.font == null && TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        return button;
    }

    private static TMP_Text GetButtonLabel(Button button)
    {
        var labelTransform = button.transform.Find(LabelChildName);
        return labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
    }

    private static void ConfigureHeaderPanel(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void ConfigureUpgradePanel(RectTransform rectTransform)
    {
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0f, 0f);
        rectTransform.anchoredPosition = new Vector2(20f, 20f);
        rectTransform.sizeDelta = new Vector2(340f, 374f);
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        StretchToParent(rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void StretchToParent(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private static GameObject EnsureSceneObject(Scene scene, string name, Transform parent)
    {
        var existing = FindSceneObject(scene, name);
        if (existing != null)
        {
            if (parent != null && existing.transform.parent != parent)
            {
                Undo.SetTransformParent(existing.transform, parent, $"Reparent {name}");
                existing.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                existing.transform.localScale = Vector3.one;
            }

            return existing;
        }

        var created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        SceneManager.MoveGameObjectToScene(created, scene);

        if (parent != null)
        {
            created.transform.SetParent(parent, false);
        }

        return created;
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        foreach (var rootGameObject in scene.GetRootGameObjects())
        {
            var found = FindInHierarchy(rootGameObject.transform, name);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindInHierarchy(Transform current, string name)
    {
        if (current.name == name)
        {
            return current;
        }

        for (var i = 0; i < current.childCount; i++)
        {
            var match = FindInHierarchy(current.GetChild(i), name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        return Undo.AddComponent<T>(gameObject);
    }
}
