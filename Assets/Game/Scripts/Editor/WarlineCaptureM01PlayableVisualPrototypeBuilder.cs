#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureM01PlayableVisualPrototypeBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/Chapter01/Chapter01_M01_PlayableVisualPrototype.unity";
    private const string DefinitionPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset";
    private const string EntityRoot = "Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites";

    [MenuItem("WarlineCapture/Design/Build Chapter01 M01 Playable Visual Prototype")]
    public static void Build()
    {
        WarlineCaptureM01TacticalValidationBuilder.Build();
        AssetDatabase.Refresh();

        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        Chapter01TacticalScaleContract scale = WarlineCaptureChapter01TacticalScaleContractUtility.LoadOrCreate();
        if (definition == null || definition.GroundSprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_M01_PLAYABLE_VISUAL_MISSING_DEFINITION path={DefinitionPath}");
            return;
        }

        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(ScenePath)));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(definition, scale);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_M01_PLAYABLE_VISUAL_BUILT scene={ScenePath}");
    }

    private static void BuildScene(TacticalMapDefinition definition, Chapter01TacticalScaleContract scale)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("Chapter01_M01_PlayableVisualPrototype");
        root.AddComponent<TacticalMapDefinitionReference>().Configure(definition);

        GameObject ground = CreateSprite("Ground_M01_ApprovedScaleProxy", definition.GroundSprite, 0);
        ground.transform.SetParent(root.transform, false);

        Transform player = CreateEntity("M01_Player_RifleSquad_ClickToSelect", EntityRoot + "/infantry_squad.png", definition, "player_spawn.command_squad", scale.GetScale(TacticalVisualScaleRole.InfantrySquad), 30, Color.white, root.transform);
        Transform enemy = CreateEntity("M01_Enemy_Patrol_ClickToAttack", EntityRoot + "/infantry_squad.png", definition, "enemy_spawn.patrol_start", scale.GetScale(TacticalVisualScaleRole.InfantrySquad), 31, new Color(1f, 0.58f, 0.48f, 1f), root.transform);
        CreateEntity("M01_Decor_CommandPoint_OffRoad", EntityRoot + "/command_building.png", definition, "decor.command_point", scale.GetScale(TacticalVisualScaleRole.CommandBuilding), 22, Color.white, root.transform);
        CreateEntity("M01_Decor_TentCluster_OffRoad", EntityRoot + "/tent_cluster.png", definition, "decor.tent_cluster_01", scale.GetScale(TacticalVisualScaleRole.TentCluster), 23, Color.white, root.transform);

        Transform selectionRing = CreateOverlayQuad("SelectionRing_Runtime", new Vector2(0.17f, 0.055f), new Color(0.10f, 0.88f, 1f, 0.62f), 80, root.transform);
        Transform moveMarker = CreateOverlayQuad("MoveMarker_Runtime", new Vector2(0.075f, 0.075f), new Color(0.20f, 1f, 0.35f, 0.72f), 81, root.transform);
        Transform attackMarker = CreateOverlayQuad("AttackMarker_Runtime", new Vector2(0.09f, 0.09f), new Color(1f, 0.22f, 0.12f, 0.76f), 82, root.transform);

        Camera camera = CreateCamera(definition);
        var ui = CreateHud(camera);

        M01PlayableVisualPrototypeController controller = root.AddComponent<M01PlayableVisualPrototypeController>();
        SerializedObject so = new(controller);
        so.FindProperty("gameplayCamera").objectReferenceValue = camera;
        so.FindProperty("playerSquad").objectReferenceValue = player;
        so.FindProperty("enemyPatrol").objectReferenceValue = enemy;
        so.FindProperty("selectionRing").objectReferenceValue = selectionRing;
        so.FindProperty("moveMarker").objectReferenceValue = moveMarker;
        so.FindProperty("attackMarker").objectReferenceValue = attackMarker;
        so.FindProperty("selectedText").objectReferenceValue = ui.Selected;
        so.FindProperty("commandText").objectReferenceValue = ui.Command;
        so.FindProperty("objectiveText").objectReferenceValue = ui.Objective;
        so.FindProperty("enemyHealthText").objectReferenceValue = ui.EnemyHealth;
        so.FindProperty("enemyHealthFill").objectReferenceValue = ui.EnemyHealthFill;
        so.FindProperty("toastText").objectReferenceValue = ui.Toast;
        so.FindProperty("resultPanel").objectReferenceValue = ui.ResultPanel;
        so.FindProperty("playableBounds").rectValue = definition.CameraBounds;
        so.ApplyModifiedPropertiesWithoutUndo();

        WireButton(ui.MoveButton, controller, nameof(M01PlayableVisualPrototypeController.SetMoveMode));
        WireButton(ui.AttackButton, controller, nameof(M01PlayableVisualPrototypeController.SetAttackMode));
        WireButton(ui.StopButton, controller, nameof(M01PlayableVisualPrototypeController.StopOrder));
        Selection.activeObject = root;
    }

    private static Transform CreateEntity(string name, string path, TacticalMapDefinition definition, string anchorId, float scale, int sortingOrder, Color color, Transform parent)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null || !definition.TryGetAnchor(anchorId, out TacticalMapAnchor anchor))
        {
            Debug.LogError($"WARLINECAPTURE_M01_PLAYABLE_VISUAL_ENTITY_FAILED name={name} path={path} anchor={anchorId}");
            return new GameObject(name).transform;
        }

        Vector2 world = definition.NormalizedToWorld(anchor.NormalizedPosition);
        GameObject entity = CreateSprite(name, sprite, sortingOrder);
        entity.transform.SetParent(parent, false);
        entity.transform.localPosition = new Vector3(world.x, world.y, -0.05f);
        entity.transform.localScale = new Vector3(scale, scale, 1f);
        entity.GetComponent<SpriteRenderer>().color = color;
        entity.AddComponent<BoxCollider2D>();
        return entity.transform;
    }

    private static GameObject CreateSprite(string name, Sprite sprite, int sortingOrder)
    {
        GameObject obj = new(name);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return obj;
    }

    private static Transform CreateOverlayQuad(string name, Vector2 size, Color color, int sortingOrder, Transform parent)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);
        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = color };
        renderer.sortingOrder = sortingOrder;
        quad.SetActive(false);
        return quad.transform;
    }

    private static Camera CreateCamera(TacticalMapDefinition definition)
    {
        GameObject obj = new("M01_Playable_CloseGameplayCamera");
        Camera camera = obj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.039f, 0.040f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = definition.DefaultOrthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(definition.CameraDefaultCenter.x, definition.CameraDefaultCenter.y, -10f);
        return camera;
    }

    private readonly struct HudRefs
    {
        public readonly Text Selected;
        public readonly Text Command;
        public readonly Text Objective;
        public readonly Text EnemyHealth;
        public readonly Image EnemyHealthFill;
        public readonly Text Toast;
        public readonly GameObject ResultPanel;
        public readonly Button MoveButton;
        public readonly Button AttackButton;
        public readonly Button StopButton;

        public HudRefs(Text selected, Text command, Text objective, Text enemyHealth, Image enemyHealthFill, Text toast, GameObject resultPanel, Button moveButton, Button attackButton, Button stopButton)
        {
            Selected = selected;
            Command = command;
            Objective = objective;
            EnemyHealth = enemyHealth;
            EnemyHealthFill = enemyHealthFill;
            Toast = toast;
            ResultPanel = resultPanel;
            MoveButton = moveButton;
            AttackButton = attackButton;
            StopButton = stopButton;
        }
    }

    private static HudRefs CreateHud(Camera camera)
    {
        GameObject canvasObject = new("M01_Playable_HUD_Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        CreateEventSystem();

        Text objective = CreatePanelText("ObjectivePanel", canvasObject.transform, "OBJECTIVE: Destroy hostile patrol", new Vector2(28f, -28f), new Vector2(520f, 74f), TextAnchor.MiddleLeft, 26, new Color(0.04f, 0.08f, 0.085f, 0.92f));
        Text enemy = CreatePanelText("EnemyStatusPanel", canvasObject.transform, "ENEMY PATROL: 100/100", new Vector2(28f, -112f), new Vector2(480f, 82f), TextAnchor.UpperLeft, 25, new Color(0.10f, 0.035f, 0.030f, 0.90f));
        Image enemyHealthFill = CreateHealthFill(enemy.transform.parent, new Vector2(0f, -24f), new Vector2(420f, 12f), new Color(1f, 0.24f, 0.12f, 1f));
        Text selected = CreatePanelText("SelectedEntityPanel", canvasObject.transform, "SELECTED: none", new Vector2(28f, 28f), new Vector2(430f, 70f), TextAnchor.MiddleLeft, 24, new Color(0.035f, 0.07f, 0.085f, 0.92f), false);
        Text command = CreatePanelText("CommandModeBanner", canvasObject.transform, "ORDER: DIRECT COMMAND", new Vector2(474f, 28f), new Vector2(430f, 70f), TextAnchor.MiddleLeft, 24, new Color(0.075f, 0.060f, 0.020f, 0.92f), false);
        Text toast = CreatePanelText("FeedbackToast", canvasObject.transform, "Tap the rifle squad to select it.", new Vector2(0f, 118f), new Vector2(620f, 58f), TextAnchor.MiddleCenter, 22, new Color(0.02f, 0.09f, 0.10f, 0.90f), false, true);

        Button move = CreateButton("MoveButton", canvasObject.transform, "MOVE", new Vector2(-388f, 28f), new Vector2(154f, 70f), new Color(0.05f, 0.28f, 0.34f, 0.96f));
        Button attack = CreateButton("AttackButton", canvasObject.transform, "ATTACK", new Vector2(-204f, 28f), new Vector2(172f, 70f), new Color(0.42f, 0.06f, 0.04f, 0.96f));
        Button stop = CreateButton("StopButton", canvasObject.transform, "STOP", new Vector2(-28f, 28f), new Vector2(138f, 70f), new Color(0.18f, 0.18f, 0.16f, 0.96f));
        CreateMinimap(canvasObject.transform);
        GameObject result = CreateResultPanel(canvasObject.transform);
        return new HudRefs(selected, command, objective, enemy, enemyHealthFill, toast, result, move, attack, stop);
    }

    private static Image CreateHealthFill(Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject track = CreateRect("HealthTrack", parent, anchoredPosition, size, new Vector2(0.5f, 0.5f));
        Image trackImage = track.AddComponent<Image>();
        trackImage.color = new Color(0.02f, 0.010f, 0.008f, 0.95f);
        GameObject fill = CreateRect("HealthFill", track.transform, Vector2.zero, size, new Vector2(0.5f, 0.5f));
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = color;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        return fillImage;
    }

    private static Text CreatePanelText(string name, Transform parent, string text, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor, int fontSize, Color background, bool top = true, bool center = false)
    {
        GameObject panel = CreateRect(name, parent, anchoredPosition, size, top ? new Vector2(0f, 1f) : center ? new Vector2(0.5f, 0f) : new Vector2(0f, 0f));
        Image image = panel.AddComponent<Image>();
        image.color = background;
        GameObject textObj = CreateRect("Text", panel.transform, Vector2.zero, size - new Vector2(28f, 12f), new Vector2(0.5f, 0.5f));
        Text label = textObj.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = anchor;
        label.color = new Color(0.92f, 0.98f, 1f, 1f);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = fontSize;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject buttonObject = CreateRect(name, parent, anchoredPosition, size, new Vector2(1f, 0f));
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Button button = buttonObject.AddComponent<Button>();
        GameObject textObj = CreateRect("Text", buttonObject.transform, Vector2.zero, size, new Vector2(0.5f, 0.5f));
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return button;
    }

    private static void CreateMinimap(Transform parent)
    {
        Text text = CreatePanelText("MinimapPrototypePanel", parent, "MINIMAP\nviewport + objective", new Vector2(-28f, -28f), new Vector2(250f, 180f), TextAnchor.MiddleCenter, 20, new Color(0.018f, 0.07f, 0.085f, 0.92f));
        Image image = text.transform.parent.GetComponent<Image>();
        image.color = new Color(0.018f, 0.07f, 0.085f, 0.92f);
    }

    private static GameObject CreateResultPanel(Transform parent)
    {
        GameObject panel = CreateRect("MissionResultPrototypePanel", parent, Vector2.zero, new Vector2(820f, 470f), new Vector2(0.5f, 0.5f));
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.025f, 0.045f, 0.052f, 0.97f);
        Text title = CreatePanelText("ResultTitle", panel.transform, "VICTORY", new Vector2(0f, -40f), new Vector2(720f, 82f), TextAnchor.MiddleCenter, 44, new Color(0f, 0f, 0f, 0f));
        title.color = new Color(1f, 0.78f, 0.18f, 1f);
        CreatePanelText("ResultBody", panel.transform, "Hostile patrol destroyed\nStars: Complete mission | No own losses | Under 4:00\nRewards: Commander XP + Credits", new Vector2(0f, -150f), new Vector2(700f, 210f), TextAnchor.MiddleCenter, 27, new Color(0f, 0f, 0f, 0f));
        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
    {
        GameObject obj = new(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return obj;
    }

    private static void WireButton(Button button, Object target, string method)
    {
        UnityAction action = System.Delegate.CreateDelegate(typeof(UnityAction), target, method) as UnityAction;
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    private static void CreateEventSystem()
    {
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            return;
        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }
}
#endif
