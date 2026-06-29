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
    private const string JetPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Jet_01.prefab";
    private const string HelicopterPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack.prefab";
    private const string DronePrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Drone.prefab";
    private const string SoldierPrefabPath = "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab";
    private const string GroundVehicleTransportPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab";
    private const string HelicopterTransportPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab";
    private const string PlaneTransportPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab";
    private const string VehicleCargoPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab";

    [MenuItem("Warline Capture/Scenario Lab/Create Manual Scene Shell")]
    public static void CreateManualSceneShell()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Game/Scenes/ScenarioLab");

        EnsureScenarioDefinitionsExist();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        CreateOrUpdatePrefabRegistryConfig();
        CreateOrUpdateBakedPrefabSubScene();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BattleScenarioLab";
        BattleScenarioDefinition[] definitions = LoadScenarioDefinitions();
        BattleScenarioDefinition definition = definitions.Length > 0 ? definitions[0] : null;
        if (definition == null)
        {
            BattleScenarioLabValidationRunner.CreateOrUpdateAd001DefinitionAsset();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            definitions = LoadScenarioDefinitions();
            definition = definitions.Length > 0 ? definitions[0] : null;
        }

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
        BattleScenarioLabOverlayView overlay = CreateOverlay(root.transform, definition, definitions, bootstrap);
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
        SerializedProperty scenarioDefinitions = bootstrapSerialized.FindProperty("scenarioDefinitions");
        scenarioDefinitions.arraySize = definitions.Length;
        for (int i = 0; i < definitions.Length; i++)
            scenarioDefinitions.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
        bootstrapSerialized.FindProperty("overlayView").objectReferenceValue = overlay;
        bootstrapSerialized.FindProperty("visualPlayback").objectReferenceValue = visualPlayback;
        bootstrapSerialized.FindProperty("runOnStart").boolValue = true;
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[BattleScenarioLab] Manual scene shell saved: {ScenePath}");
    }

    private static void EnsureScenarioDefinitionsExist()
    {
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad001DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd001DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad002DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd002DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad003DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd003DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad004DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd004DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad005DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd005DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad006DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd006DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad007DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd007DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad008DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd008DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad009DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd009DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad010DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd010DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Ad011DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateAd011DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Gm001DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateGm001DefinitionAsset);
        EnsureScenarioDefinitionExists(BattleScenarioLabValidationRunner.Dr001DefinitionPath, BattleScenarioLabValidationRunner.CreateOrUpdateDr001DefinitionAsset);
        BattleScenarioLabValidationRunner.CreateOrUpdateTransportBoardingDefinitionAssets();
    }

    private static void EnsureScenarioDefinitionExists(string path, Action createOrUpdate)
    {
        if (File.Exists(path))
            return;

        createOrUpdate();
    }

    private static BattleScenarioDefinition[] LoadScenarioDefinitions()
    {
        string[] basePaths =
        {
            BattleScenarioLabValidationRunner.Ad001DefinitionPath,
            BattleScenarioLabValidationRunner.Ad002DefinitionPath,
            BattleScenarioLabValidationRunner.Ad003DefinitionPath,
            BattleScenarioLabValidationRunner.Ad004DefinitionPath,
            BattleScenarioLabValidationRunner.Ad005DefinitionPath,
            BattleScenarioLabValidationRunner.Ad006DefinitionPath,
            BattleScenarioLabValidationRunner.Ad007DefinitionPath,
            BattleScenarioLabValidationRunner.Ad008DefinitionPath,
            BattleScenarioLabValidationRunner.Ad009DefinitionPath,
            BattleScenarioLabValidationRunner.Ad010DefinitionPath,
            BattleScenarioLabValidationRunner.Ad011DefinitionPath,
            BattleScenarioLabValidationRunner.Gm001DefinitionPath,
            BattleScenarioLabValidationRunner.Dr001DefinitionPath
        };
        var paths = new System.Collections.Generic.List<string>(basePaths);
        for (int i = 0; i < TransportBoardingScenarioCatalog.All.Count; i++)
            paths.Add(BattleScenarioLabValidationRunner.GetTransportBoardingDefinitionPath(TransportBoardingScenarioCatalog.All[i]));

        var definitions = new System.Collections.Generic.List<BattleScenarioDefinition>(paths.Count);
        for (int i = 0; i < paths.Count; i++)
        {
            string path = paths[i];
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(path);
            if (definition != null)
            {
                definitions.Add(definition);
                continue;
            }

            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            string assetType = mainAsset != null ? mainAsset.GetType().FullName : "null";
            Debug.LogWarning($"[BattleScenarioLab] Scenario definition did not load as BattleScenarioDefinition: {path} (main asset type: {assetType})");
        }

        return definitions.ToArray();
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
        GameObject[] scenarioPrefabs = LoadScenarioPrefabRegistryPrefabs();
        prefabs.arraySize = scenarioPrefabs.Length;
        for (int i = 0; i < scenarioPrefabs.Length; i++)
            prefabs.GetArrayElementAtIndex(i).objectReferenceValue = scenarioPrefabs[i];
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
        GameObject[] scenarioPrefabs = LoadScenarioPrefabRegistryPrefabs();

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

    private static GameObject[] LoadScenarioPrefabRegistryPrefabs()
    {
        return new[]
        {
            RequirePrefab(GroundLauncherPrefabPath),
            RequirePrefab(AirLauncherPrefabPath),
            RequirePrefab(RadarTankPrefabPath),
            RequirePrefab(JetPrefabPath),
            RequirePrefab(HelicopterPrefabPath),
            RequirePrefab(DronePrefabPath),
            RequirePrefab(SoldierPrefabPath),
            RequirePrefab(GroundVehicleTransportPrefabPath),
            RequirePrefab(HelicopterTransportPrefabPath),
            RequirePrefab(PlaneTransportPrefabPath),
            RequirePrefab(VehicleCargoPrefabPath)
        };
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
        BattleScenarioDefinition[] definitions,
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
        Dropdown scenarioDropdown = CreateScenarioDropdown("ScenarioSelector", panel.transform, definitions, 13);
        Dropdown variantDropdown = CreateOverlayDropdown("VariantSelector", panel.transform, definition, 14);
        Button previousButton = CreateOverlayButton("PreviousScenarioButton", panel.transform, "PREV", 14);
        Button nextButton = CreateOverlayButton("NextScenarioButton", panel.transform, "NEXT", 14);
        Button restartButton = CreateOverlayButton("RestartScenarioButton", panel.transform, "RUN AGAIN", 15);

        SetRect(title.rectTransform, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);
        SetRect(status.rectTransform, new Vector2(0.04f, 0.68f), new Vector2(0.66f, 0.78f), Vector2.zero, Vector2.zero);
        SetRect(scenarioDropdown.GetComponent<RectTransform>(), new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.67f), Vector2.zero, Vector2.zero);
        SetRect(variantDropdown.GetComponent<RectTransform>(), new Vector2(0.04f, 0.49f), new Vector2(0.66f, 0.57f), Vector2.zero, Vector2.zero);
        SetRect(previousButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.49f), new Vector2(0.77f, 0.57f), Vector2.zero, Vector2.zero);
        SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.49f), new Vector2(0.87f, 0.57f), Vector2.zero, Vector2.zero);
        SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.88f, 0.49f), new Vector2(0.96f, 0.57f), Vector2.zero, Vector2.zero);
        SetRect(variants.rectTransform, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.47f), Vector2.zero, Vector2.zero);
        SetRect(comparisons.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.20f), Vector2.zero, Vector2.zero);
        UnityEventTools.AddPersistentListener(previousButton.onClick, bootstrap.SelectPreviousScenario);
        UnityEventTools.AddPersistentListener(nextButton.onClick, bootstrap.SelectNextScenario);
        UnityEventTools.AddPersistentListener(restartButton.onClick, bootstrap.RunScenario);

        SerializedObject serialized = new(overlay);
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("statusText").objectReferenceValue = status;
        serialized.FindProperty("variantsText").objectReferenceValue = variants;
        serialized.FindProperty("comparisonsText").objectReferenceValue = comparisons;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject bootstrapSerialized = new(bootstrap);
        bootstrapSerialized.FindProperty("scenarioDropdown").objectReferenceValue = scenarioDropdown;
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
        ConfigureDropdownTemplate(dropdown, dropdownObject.transform, fontSize);
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

    private static Dropdown CreateScenarioDropdown(
        string name,
        Transform parent,
        BattleScenarioDefinition[] definitions,
        int fontSize)
    {
        GameObject dropdownObject = new(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
        dropdownObject.transform.SetParent(parent);

        Image image = dropdownObject.GetComponent<Image>();
        image.color = new Color(0.07f, 0.12f, 0.14f, 0.96f);

        Text label = CreateOverlayText("Label", dropdownObject.transform, string.Empty, fontSize, TextAnchor.MiddleLeft);
        label.color = new Color(0.94f, 1f, 0.96f, 1f);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 2f), new Vector2(-34f, -2f));

        Text arrow = CreateOverlayText("Arrow", dropdownObject.transform, "v", fontSize, TextAnchor.MiddleCenter);
        arrow.color = new Color(0.76f, 1f, 0.92f, 1f);
        SetRect(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-30f, 0f), new Vector2(-6f, 0f));

        Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
        dropdown.targetGraphic = image;
        dropdown.captionText = label;
        ConfigureDropdownTemplate(dropdown, dropdownObject.transform, fontSize);
        dropdown.options.Clear();
        for (int i = 0; i < definitions.Length; i++)
        {
            BattleScenarioDefinition definition = definitions[i];
            string option = definition != null && !string.IsNullOrWhiteSpace(definition.DisplayName)
                ? definition.DisplayName
                : definition != null && !string.IsNullOrWhiteSpace(definition.ScenarioId)
                    ? definition.ScenarioId
                    : $"Scenario {i + 1}";
            dropdown.options.Add(new Dropdown.OptionData(option));
        }

        dropdown.value = 0;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static void ConfigureDropdownTemplate(Dropdown dropdown, Transform parent, int fontSize)
    {
        GameObject templateObject = new("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        templateObject.transform.SetParent(parent);
        RectTransform templateRect = templateObject.GetComponent<RectTransform>();
        SetRect(templateRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -156f), new Vector2(0f, -2f));

        Image templateImage = templateObject.GetComponent<Image>();
        templateImage.color = new Color(0.025f, 0.045f, 0.05f, 0.98f);

        GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(templateObject.transform);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        SetRect(viewportRect, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new("Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewportObject.transform);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        SetRect(contentRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), Vector2.zero);
        contentRect.pivot = new Vector2(0.5f, 1f);

        GameObject itemObject = new("Item", typeof(RectTransform), typeof(Toggle));
        itemObject.transform.SetParent(contentObject.transform);
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        SetRect(itemRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), Vector2.zero);
        itemRect.pivot = new Vector2(0.5f, 1f);

        GameObject itemBackgroundObject = new("Item Background", typeof(RectTransform), typeof(Image));
        itemBackgroundObject.transform.SetParent(itemObject.transform);
        RectTransform itemBackgroundRect = itemBackgroundObject.GetComponent<RectTransform>();
        SetRect(itemBackgroundRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image itemBackground = itemBackgroundObject.GetComponent<Image>();
        itemBackground.color = new Color(0.08f, 0.14f, 0.15f, 0.92f);

        GameObject checkmarkObject = new("Item Checkmark", typeof(RectTransform), typeof(Image));
        checkmarkObject.transform.SetParent(itemObject.transform);
        RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        SetRect(checkmarkRect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(8f, 7f), new Vector2(22f, -7f));
        Image checkmark = checkmarkObject.GetComponent<Image>();
        checkmark.color = new Color(0.78f, 1f, 0.92f, 1f);

        Text itemLabel = CreateOverlayText("Item Label", itemObject.transform, string.Empty, fontSize, TextAnchor.MiddleLeft);
        itemLabel.color = new Color(0.94f, 1f, 0.96f, 1f);
        itemLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        SetRect(itemLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(30f, 2f), new Vector2(-8f, -2f));

        Toggle toggle = itemObject.GetComponent<Toggle>();
        toggle.targetGraphic = itemBackground;
        toggle.graphic = checkmark;

        ScrollRect scrollRect = templateObject.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 18f;

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        dropdown.itemImage = itemBackground;
        templateObject.SetActive(false);
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
