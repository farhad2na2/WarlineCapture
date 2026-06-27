#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BattleScenarioLabSceneBuilder
{
    public const string ScenePath = "Assets/Game/Scenes/ScenarioLab/BattleScenarioLab.unity";
    public const string BakedPrefabSubScenePath = "Assets/Game/Scenes/ScenarioLab/BattleScenarioLabBakedPrefabs.unity";
    public const string PrefabRegistryConfigPath = "Assets/Game/Configs/ScenarioLab/BattleScenarioLab_UnitPrefabRegistry.asset";
    private const string AirLauncherPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Air.prefab";
    private const string GroundLauncherPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Ground.prefab";
    private const string RadarTankPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Radar_Tank.prefab";

    [MenuItem("Warline Capture/Scenario Lab/Create Manual Scene Shell")]
    public static void CreateManualSceneShell()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Game/Scenes/ScenarioLab");

        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad001DefinitionPath);
        if (definition == null)
            BattleScenarioLabValidationRunner.CreateOrUpdateAd001DefinitionAsset();
        definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad001DefinitionPath);
        CreateOrUpdatePrefabRegistryConfig();
        CreateOrUpdateBakedPrefabSubScene();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BattleScenarioLab";
        definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad001DefinitionPath);
        if (definition == null)
            throw new InvalidOperationException($"Missing AD-001 scenario definition: {BattleScenarioLabValidationRunner.Ad001DefinitionPath}");

        GameObject root = new("BattleScenarioLabRoot");
        BattleScenarioLabSceneReferences references = root.AddComponent<BattleScenarioLabSceneReferences>();
        BattleScenarioLabPlayBootstrap bootstrap = root.AddComponent<BattleScenarioLabPlayBootstrap>();
        BattleScenarioLabVisualPlayback visualPlayback = root.AddComponent<BattleScenarioLabVisualPlayback>();

        Camera camera = CreateCamera();
        GameObject ground = CreateGround();
        ScenarioLabVisuals visuals = CreateSceneMarkers(root.transform);

        ground.transform.SetParent(root.transform);
        CreateSubSceneReference(root.transform);
        BattleScenarioLabOverlayView overlay = CreateOverlay(root.transform, definition, bootstrap);
        CreateEventSystem();

        SerializedObject serialized = new(references);
        serialized.FindProperty("scenarioDefinition").objectReferenceValue = definition;
        serialized.FindProperty("scenarioCamera").objectReferenceValue = camera;
        serialized.FindProperty("launcherMarker").objectReferenceValue = visuals.AirLauncherVisual;
        serialized.FindProperty("radarMarker").objectReferenceValue = visuals.RadarVisual;
        serialized.FindProperty("incomingThreatStartMarker").objectReferenceValue = visuals.GroundLauncherVisual;
        serialized.FindProperty("defendedTargetMarker").objectReferenceValue = visuals.DefendedTargetVisual;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject visualSerialized = new(visualPlayback);
        visualSerialized.FindProperty("scenarioCamera").objectReferenceValue = camera;
        visualSerialized.FindProperty("groundLauncherRoot").objectReferenceValue = visuals.GroundLauncherVisual;
        visualSerialized.FindProperty("airLauncherRoot").objectReferenceValue = visuals.AirLauncherVisual;
        visualSerialized.FindProperty("radarRoot").objectReferenceValue = visuals.RadarVisual;
        visualSerialized.FindProperty("defendedTargetVisual").objectReferenceValue = visuals.DefendedTargetVisual;
        visualSerialized.FindProperty("entityWaitTimeoutSeconds").floatValue = 30f;
        visualSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject bootstrapSerialized = new(bootstrap);
        bootstrapSerialized.FindProperty("scenarioDefinition").objectReferenceValue = definition;
        bootstrapSerialized.FindProperty("overlayView").objectReferenceValue = overlay;
        bootstrapSerialized.FindProperty("visualPlayback").objectReferenceValue = visualPlayback;
        bootstrapSerialized.FindProperty("runOnStart").boolValue = true;
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] Manual scene shell saved: {ScenePath}");
    }

    private static UnitPrefabRegistryAuthoringConfig CreateOrUpdatePrefabRegistryConfig()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabRegistryConfigPath) ?? "Assets/Game/Configs/ScenarioLab");

        UnitPrefabRegistryAuthoringConfig config =
            AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(PrefabRegistryConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();
            AssetDatabase.CreateAsset(config, PrefabRegistryConfigPath);
        }

        SerializedObject serialized = new(config);
        SerializedProperty prefabs = serialized.FindProperty("unitSpawnPrefabs");
        prefabs.arraySize = 3;
        prefabs.GetArrayElementAtIndex(0).objectReferenceValue = RequirePrefab(GroundLauncherPrefabPath);
        prefabs.GetArrayElementAtIndex(1).objectReferenceValue = RequirePrefab(AirLauncherPrefabPath);
        prefabs.GetArrayElementAtIndex(2).objectReferenceValue = RequirePrefab(RadarTankPrefabPath);
        serialized.FindProperty("unitSelectionMarkerPrefab").objectReferenceValue = null;
        serialized.FindProperty("unitHealthBarPrefab").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        return config;
    }

    private static void CreateOrUpdateBakedPrefabSubScene()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BakedPrefabSubScenePath) ?? "Assets/Game/Scenes/ScenarioLab");

        Scene subScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        subScene.name = "BattleScenarioLabBakedPrefabs";

        GameObject registryObject = new("ScenarioLabUnitPrefabRegistry");
        BattleScenarioLabUnitPrefabRegistryAuthoring registry =
            registryObject.AddComponent<BattleScenarioLabUnitPrefabRegistryAuthoring>();
        GameObject[] scenarioPrefabs =
        {
            RequirePrefab(GroundLauncherPrefabPath),
            RequirePrefab(AirLauncherPrefabPath),
            RequirePrefab(RadarTankPrefabPath)
        };

        SerializedObject serialized = new(registry);
        SerializedProperty unitSpawnPrefabs = serialized.FindProperty("unitSpawnPrefabs");
        unitSpawnPrefabs.arraySize = scenarioPrefabs.Length;
        for (int i = 0; i < scenarioPrefabs.Length; i++)
            unitSpawnPrefabs.GetArrayElementAtIndex(i).objectReferenceValue = scenarioPrefabs[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
        EditorUtility.SetDirty(registryObject);

        EditorSceneManager.SaveScene(subScene, BakedPrefabSubScenePath);
        AssetDatabase.ImportAsset(BakedPrefabSubScenePath, ImportAssetOptions.ForceUpdate);
    }

    private static void CreateSubSceneReference(Transform parent)
    {
        UnityEngine.Object subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BakedPrefabSubScenePath);
        if (subSceneAsset == null)
            throw new InvalidOperationException($"Missing Scenario Lab baked prefab subscene: {BakedPrefabSubScenePath}");

        Type subSceneType = Type.GetType("Unity.Scenes.SubScene, Unity.Scenes");
        if (subSceneType == null)
            throw new InvalidOperationException("Unity.Scenes.SubScene type is not available.");

        GameObject subSceneObject = new("BattleScenarioLabBakedPrefabsSubScene");
        subSceneObject.transform.SetParent(parent);
        Component subScene = subSceneObject.AddComponent(subSceneType);
        SerializedObject serialized = new(subScene);
        serialized.FindProperty("_SceneAsset").objectReferenceValue = subSceneAsset;
        SerializedProperty autoLoad = serialized.FindProperty("AutoLoadScene");
        if (autoLoad != null)
            autoLoad.boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("ScenarioLabCamera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(72f, 62f, -118f);
        cameraObject.transform.rotation = Quaternion.Euler(58f, -31f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.055f, 0.07f, 1f);
        camera.fieldOfView = 38f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 500f;
        return camera;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
            eventSystemObject.AddComponent(inputSystemModuleType);
        else
            eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "NeutralGroundPlane";
        ground.transform.position = new Vector3(70f, -0.05f, 0f);
        ground.transform.localScale = new Vector3(280f, 0.1f, 180f);
        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreatePreviewMaterial("ScenarioLab_GroundPreview", new Color(0.12f, 0.14f, 0.12f));
        return ground;
    }

    private sealed class ScenarioLabVisuals
    {
        public Transform GroundLauncherVisual;
        public Transform AirLauncherVisual;
        public Transform RadarVisual;
        public Transform DefendedTargetVisual;
    }

    private static ScenarioLabVisuals CreateSceneMarkers(Transform parent)
    {
        GameObject visualRoot = new("AD001ScenarioMarkers");
        visualRoot.transform.SetParent(parent);

        var visuals = new ScenarioLabVisuals
        {
            GroundLauncherVisual = CreateMarker("GroundMissileLauncherSpawnMarker", new Vector3(210f, 0f, 0f), visualRoot.transform),
            AirLauncherVisual = CreateMarker("AirMissileLauncherSpawnMarker", Vector3.zero, visualRoot.transform),
            RadarVisual = CreateMarker("RadarSupportSpawnMarker", new Vector3(8f, 0f, -12f), visualRoot.transform),
            DefendedTargetVisual = CreateTargetVisual(
                "DefendedTargetVisual",
                new Vector3(-40f, 1.2f, 0f),
                visualRoot.transform)
        };

        return visuals;
    }

    private static Transform CreateMarker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static GameObject RequirePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException($"Missing production prefab: {path}");
        return prefab;
    }

    private static Transform CreateTargetVisual(string name, Vector3 position, Transform parent)
    {
        GameObject root = new(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        CreatePrimitiveChild("Core", PrimitiveType.Cube, root.transform, Vector3.zero, new Vector3(5.2f, 2.4f, 5.2f), new Color(0.82f, 0.72f, 0.22f));
        CreatePrimitiveChild("Beacon", PrimitiveType.Sphere, root.transform, new Vector3(0f, 2.4f, 0f), new Vector3(1.6f, 1.6f, 1.6f), new Color(1f, 0.92f, 0.28f));
        return root.transform;
    }

    private static Transform CreatePrimitiveChild(
        string name,
        PrimitiveType primitive,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        GameObject child = GameObject.CreatePrimitive(primitive);
        child.name = name;
        child.transform.SetParent(parent);
        child.transform.localPosition = localPosition;
        child.transform.localScale = localScale;
        Renderer renderer = child.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreatePreviewMaterial(parent.name + "_" + name + "_Preview", color);
        return child.transform;
    }

    private static Material CreatePreviewMaterial(string name, Color color)
    {
        Material material = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
        {
            name = name,
            color = color
        };
        return material;
    }

    private static BattleScenarioLabOverlayView CreateOverlay(
        Transform parent,
        BattleScenarioDefinition definition,
        BattleScenarioLabPlayBootstrap bootstrap)
    {
        GameObject canvasObject = new("ScenarioLabOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BattleScenarioLabOverlayView overlay = canvasObject.AddComponent<BattleScenarioLabOverlayView>();
        GameObject panel = CreateOverlayPanel(canvasObject.transform);
        Text title = CreateOverlayText("Title", panel.transform, definition != null ? definition.DisplayName : "Battle Scenario Lab", 24, TextAnchor.MiddleLeft);
        Text status = CreateOverlayText("Status", panel.transform, "Waiting for scenario run.", 20, TextAnchor.MiddleLeft);
        Text variants = CreateOverlayText("Variants", panel.transform, string.Empty, 15, TextAnchor.UpperLeft);
        Text comparisons = CreateOverlayText("Comparisons", panel.transform, string.Empty, 15, TextAnchor.UpperLeft);
        Dropdown variantDropdown = CreateOverlayDropdown("VariantSelector", panel.transform, definition, 14);
        Button restartButton = CreateOverlayButton("RestartScenarioButton", panel.transform, "RUN AGAIN", 15);

        SetRect(title.rectTransform, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);
        SetRect(status.rectTransform, new Vector2(0.04f, 0.68f), new Vector2(0.66f, 0.78f), Vector2.zero, Vector2.zero);
        SetRect(variantDropdown.GetComponent<RectTransform>(), new Vector2(0.04f, 0.59f), new Vector2(0.66f, 0.67f), Vector2.zero, Vector2.zero);
        SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.59f), new Vector2(0.96f, 0.77f), Vector2.zero, Vector2.zero);
        SetRect(variants.rectTransform, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.64f), Vector2.zero, Vector2.zero);
        SetRect(comparisons.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.22f), Vector2.zero, Vector2.zero);
        UnityEventTools.AddPersistentListener(restartButton.onClick, bootstrap.RunScenario);

        SerializedObject serialized = new(overlay);
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("statusText").objectReferenceValue = status;
        serialized.FindProperty("variantsText").objectReferenceValue = variants;
        serialized.FindProperty("comparisonsText").objectReferenceValue = comparisons;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject bootstrapSerialized = new(bootstrap);
        bootstrapSerialized.FindProperty("variantDropdown").objectReferenceValue = variantDropdown;
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();
        return overlay;
    }

    private static GameObject CreateOverlayPanel(Transform parent)
    {
        GameObject panel = new("MetricsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0.015f, 0.66f), new Vector2(0.37f, 0.985f), Vector2.zero, Vector2.zero);
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.02f, 0.03f, 0.035f, 0.74f);
        return panel;
    }

    private static Text CreateOverlayText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        TextAnchor alignment)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent);
        Text label = textObject.GetComponent<Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.color = new Color(0.92f, 0.96f, 0.92f, 1f);
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return label;
    }

    private static Button CreateOverlayButton(string name, Transform parent, string text, int fontSize)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.24f, 0.25f, 0.92f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.25f, 0.48f, 0.48f, 0.96f);
        colors.pressedColor = new Color(0.08f, 0.18f, 0.2f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.5f);
        button.colors = colors;

        Text label = CreateOverlayText("Label", buttonObject.transform, text, fontSize, TextAnchor.MiddleCenter);
        label.color = new Color(0.96f, 1f, 0.96f, 1f);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 2f), new Vector2(-8f, -2f));
        return button;
    }

    private static Dropdown CreateOverlayDropdown(
        string name,
        Transform parent,
        BattleScenarioDefinition definition,
        int fontSize)
    {
        GameObject dropdownObject = new(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
        dropdownObject.transform.SetParent(parent);

        Image image = dropdownObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.13f, 0.14f, 0.94f);

        Text label = CreateOverlayText("Label", dropdownObject.transform, string.Empty, fontSize, TextAnchor.MiddleLeft);
        label.color = new Color(0.92f, 1f, 0.96f, 1f);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 2f), new Vector2(-34f, -2f));

        Text arrow = CreateOverlayText("Arrow", dropdownObject.transform, "v", fontSize, TextAnchor.MiddleCenter);
        arrow.color = new Color(0.76f, 1f, 0.92f, 1f);
        SetRect(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-30f, 0f), new Vector2(-6f, 0f));

        Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
        dropdown.targetGraphic = image;
        dropdown.captionText = label;
        dropdown.options.Clear();
        dropdown.options.Add(new Dropdown.OptionData("All AD-001 variants"));
        BattleScenarioVariant[] variants = definition != null
            ? definition.ScenarioVariants
            : BattleScenarioAd001Runner.CreateDefaultVariants();
        for (int i = 0; i < variants.Length; i++)
        {
            BattleScenarioVariant variant = variants[i];
            dropdown.options.Add(new Dropdown.OptionData(!string.IsNullOrWhiteSpace(variant.Label) ? variant.Label : variant.VariantId));
        }

        dropdown.value = 0;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
#endif
