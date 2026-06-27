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

    [MenuItem("Warline Capture/Scenario Lab/Create Manual Scene Shell")]
    public static void CreateManualSceneShell()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Game/Scenes/ScenarioLab");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BattleScenarioLab";

        BattleScenarioDefinition definition =
            AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad001DefinitionPath);
        if (definition == null)
            BattleScenarioLabValidationRunner.CreateOrUpdateAd001DefinitionAsset();
        definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad001DefinitionPath);

        GameObject root = new("BattleScenarioLabRoot");
        BattleScenarioLabSceneReferences references = root.AddComponent<BattleScenarioLabSceneReferences>();
        BattleScenarioLabPlayBootstrap bootstrap = root.AddComponent<BattleScenarioLabPlayBootstrap>();
        BattleScenarioLabVisualPlayback visualPlayback = root.AddComponent<BattleScenarioLabVisualPlayback>();

        Camera camera = CreateCamera();
        GameObject ground = CreateGround();
        ScenarioLabVisuals visuals = CreateVisuals(root.transform);

        ground.transform.SetParent(root.transform);
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
        visualSerialized.FindProperty("groundLauncherVisual").objectReferenceValue = visuals.GroundLauncherVisual;
        visualSerialized.FindProperty("airLauncherVisual").objectReferenceValue = visuals.AirLauncherVisual;
        visualSerialized.FindProperty("radarVisual").objectReferenceValue = visuals.RadarVisual;
        visualSerialized.FindProperty("defendedTargetVisual").objectReferenceValue = visuals.DefendedTargetVisual;
        visualSerialized.FindProperty("incomingMissileVisual").objectReferenceValue = visuals.IncomingMissileVisual;
        visualSerialized.FindProperty("interceptorVisual").objectReferenceValue = visuals.InterceptorVisual;
        visualSerialized.FindProperty("incomingTrail").objectReferenceValue = visuals.IncomingTrail;
        visualSerialized.FindProperty("interceptorTrail").objectReferenceValue = visuals.InterceptorTrail;
        visualSerialized.FindProperty("groundLaunchFlash").objectReferenceValue = visuals.GroundLaunchFlash;
        visualSerialized.FindProperty("airLaunchFlash").objectReferenceValue = visuals.AirLaunchFlash;
        visualSerialized.FindProperty("interceptExplosion").objectReferenceValue = visuals.InterceptExplosion;
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
        public Transform IncomingMissileVisual;
        public Transform InterceptorVisual;
        public LineRenderer IncomingTrail;
        public LineRenderer InterceptorTrail;
        public ParticleSystem GroundLaunchFlash;
        public ParticleSystem AirLaunchFlash;
        public ParticleSystem InterceptExplosion;
    }

    private static ScenarioLabVisuals CreateVisuals(Transform parent)
    {
        GameObject visualRoot = new("AD001VisualPlayback");
        visualRoot.transform.SetParent(parent);

        var visuals = new ScenarioLabVisuals
        {
            GroundLauncherVisual = CreateLauncherVisual(
                "GroundMissileLauncherVisual",
                new Vector3(210f, 1.2f, 0f),
                new Color(0.38f, 0.18f, 0.12f),
                new Color(0.95f, 0.28f, 0.12f),
                visualRoot.transform),
            AirLauncherVisual = CreateLauncherVisual(
                "AirMissileLauncherVisual",
                new Vector3(0f, 1.2f, 0f),
                new Color(0.10f, 0.35f, 0.42f),
                new Color(0.25f, 0.96f, 1f),
                visualRoot.transform),
            RadarVisual = CreateRadarVisual(
                "RadarSupportVisual",
                new Vector3(8f, 1.1f, -12f),
                visualRoot.transform),
            DefendedTargetVisual = CreateTargetVisual(
                "DefendedTargetVisual",
                new Vector3(-40f, 1.2f, 0f),
                visualRoot.transform),
            IncomingMissileVisual = CreateMissileVisual(
                "IncomingGroundMissileVisual",
                new Color(1f, 0.28f, 0.10f),
                visualRoot.transform),
            InterceptorVisual = CreateMissileVisual(
                "AirDefenseInterceptorVisual",
                new Color(0.22f, 0.95f, 1f),
                visualRoot.transform)
        };

        visuals.IncomingTrail = CreateTrail("IncomingGroundMissileTrail", new Color(1f, 0.36f, 0.08f, 0.92f), 0.65f, visualRoot.transform);
        visuals.InterceptorTrail = CreateTrail("AirDefenseInterceptorTrail", new Color(0.25f, 0.92f, 1f, 0.92f), 0.52f, visualRoot.transform);
        visuals.GroundLaunchFlash = CreateBurst("GroundLaunchFlash", visuals.GroundLauncherVisual, new Color(1f, 0.38f, 0.05f));
        visuals.AirLaunchFlash = CreateBurst("AirLaunchFlash", visuals.AirLauncherVisual, new Color(0.35f, 0.95f, 1f));
        visuals.InterceptExplosion = CreateBurst("InterceptExplosion", null, new Color(1f, 0.72f, 0.15f), 5f, 44);

        visuals.IncomingMissileVisual.position = new Vector3(130f, 8f, 0f);
        visuals.InterceptorVisual.position = new Vector3(0f, 3.7f, 0f);
        visuals.IncomingTrail.enabled = false;
        visuals.InterceptorTrail.enabled = false;
        return visuals;
    }

    private static Transform CreateLauncherVisual(
        string name,
        Vector3 position,
        Color bodyColor,
        Color accentColor,
        Transform parent)
    {
        GameObject root = new(name);
        root.transform.SetParent(parent);
        root.transform.position = position;

        CreatePrimitiveChild("Body", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0f), new Vector3(7f, 1.4f, 3.2f), bodyColor);
        CreatePrimitiveChild("Turret", PrimitiveType.Cube, root.transform, new Vector3(0.4f, 1.2f, 0f), new Vector3(4.2f, 0.8f, 2.2f), bodyColor * 1.18f);
        Transform tube = CreatePrimitiveChild("LaunchTube", PrimitiveType.Cylinder, root.transform, new Vector3(2.6f, 2.1f, 0f), new Vector3(0.65f, 3.8f, 0.65f), accentColor);
        tube.rotation = Quaternion.Euler(0f, 0f, 82f);
        return root.transform;
    }

    private static Transform CreateRadarVisual(string name, Vector3 position, Transform parent)
    {
        GameObject root = new(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        CreatePrimitiveChild("Base", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(2.2f, 0.7f, 2.2f), new Color(0.12f, 0.42f, 0.2f));
        CreatePrimitiveChild("Mast", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 2.1f, 0f), new Vector3(0.35f, 2.6f, 0.35f), new Color(0.18f, 0.65f, 0.34f));
        Transform dish = CreatePrimitiveChild("Dish", PrimitiveType.Sphere, root.transform, new Vector3(0f, 3.7f, 0f), new Vector3(3.2f, 1.2f, 0.28f), new Color(0.22f, 1f, 0.48f));
        dish.rotation = Quaternion.Euler(0f, 0f, -18f);
        return root.transform;
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

    private static Transform CreateMissileVisual(string name, Color color, Transform parent)
    {
        GameObject root = new(name);
        root.transform.SetParent(parent);
        root.transform.localScale = Vector3.one;
        Transform body = CreatePrimitiveChild("Body", PrimitiveType.Capsule, root.transform, Vector3.zero, new Vector3(0.8f, 2.8f, 0.8f), color);
        body.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Transform nose = CreatePrimitiveChild("Nose", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0f, 1.55f), new Vector3(0.75f, 0.75f, 0.75f), color * 1.25f);
        nose.localScale = new Vector3(0.75f, 0.55f, 0.75f);
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

    private static LineRenderer CreateTrail(string name, Color color, float width, Transform parent)
    {
        GameObject trailObject = new(name, typeof(LineRenderer));
        trailObject.transform.SetParent(parent);
        LineRenderer line = trailObject.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width * 0.22f;
        line.numCapVertices = 4;
        line.material = CreatePreviewMaterial(name + "_Preview", color);
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, 0.05f);
        return line;
    }

    private static ParticleSystem CreateBurst(
        string name,
        Transform parent,
        Color color,
        float size = 2.6f,
        short count = 26)
    {
        GameObject burstObject = new(name, typeof(ParticleSystem));
        if (parent != null)
        {
            burstObject.transform.SetParent(parent);
            burstObject.transform.localPosition = new Vector3(2.8f, 2.1f, 0f);
        }

        ParticleSystem particles = burstObject.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.3f;
        main.startLifetime = 0.55f;
        main.startSpeed = 9f;
        main.startSize = size;
        main.startColor = color;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        ParticleSystemRenderer renderer = burstObject.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreatePreviewMaterial(name + "_Preview", color);

        return particles;
    }

    private static GameObject CreateMarker(string name, Vector3 position, Color color, PrimitiveType primitive)
    {
        GameObject marker = GameObject.CreatePrimitive(primitive);
        marker.name = name;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 2.5f;
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreatePreviewMaterial(name + "_Preview", color);
        return marker;
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
