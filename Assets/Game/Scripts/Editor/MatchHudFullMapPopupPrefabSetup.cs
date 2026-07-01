#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TMPro;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MatchHudFullMapPopupPrefabSetup
{
    private const string PopupPrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/SCN08_FullMapPopup.prefab";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string OuterFramePath = "Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/chrome_01_drawer_outer_frame.png";
    private const string InnerFramePath = "Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/chrome_02_detail_panel_frame.png";
    private const string CloseFramePath = "Assets/Game/Art/UI/Panels/scn09_panel_close_button_bg.png";
    private const string CloseIconPath = "Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/icon_20_icon_close.png";
    private const string ZoomFramePath = "Assets/Game/Art/UI/Panels/scn08_minimap_zoom_button_frame.png";
    private const string ZoomInIconPath = "Assets/Game/Art/UI/Icons/scn08_minimap_zoom_plus_icon.png";
    private const string ZoomOutIconPath = "Assets/Game/Art/UI/Icons/scn08_minimap_zoom_minus_icon.png";
    private const string FontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
    private const string ActiveKey = "MatchHudFullMapPopupPrefabSetup.Active";
    private const string PhaseKey = "MatchHudFullMapPopupPrefabSetup.Phase";
    private const string StartedAtKey = "MatchHudFullMapPopupPrefabSetup.StartedAt";
    private const string PopupOpenedKey = "MatchHudFullMapPopupPrefabSetup.PopupOpened";
    private const string CloseInvokedKey = "MatchHudFullMapPopupPrefabSetup.CloseInvoked";
    private const double RuntimeSmokeTimeoutSeconds = 120d;

    private enum RuntimeSmokePhase
    {
        Idle = 0,
        WaitingForPlayMode = 1,
        WaitingForMainMenu = 2,
        WaitingForMatch = 3,
        OpeningPopup = 4,
        ClosingPopup = 5
    }

    [InitializeOnLoadMethod]
    private static void ResumeRuntimeSmoke()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        RegisterRuntimeSmokeCallbacks();
    }

    [MenuItem("Game/UI/Setup Match HUD Full Map Popup")]
    public static void Apply()
    {
        GameObject prefab = CreatePopupPrefab();
        AssignToMenuScene(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void Validate()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
        if (prefab == null)
            throw new System.InvalidOperationException($"Missing full-map popup prefab at {PopupPrefabPath}.");

        MatchHudFullMapPopupView popupView = prefab.GetComponent<MatchHudFullMapPopupView>();
        if (popupView == null)
            throw new System.InvalidOperationException($"{PopupPrefabPath} is missing MatchHudFullMapPopupView.");

        ValidateMinimapView(popupView.Minimap, PopupPrefabPath);

        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView shellContent = FindSceneObject<UIShellContentView>();
        if (shellContent == null)
            throw new System.InvalidOperationException("Menu scene is missing UIShellContentView.");
        if (shellContent.FullMapPopupPrefab != prefab)
            throw new System.InvalidOperationException("Menu scene UIShellContentView does not reference SCN08_FullMapPopup.");

        Debug.Log("[MatchHudFullMapPopupPrefabSetup] Validation passed.");
    }

    public static void RunRuntimeSmokeValidation()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(PhaseKey, (int)RuntimeSmokePhase.WaitingForPlayMode);
            SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(PopupOpenedKey, false);
            SessionState.SetBool(CloseInvokedKey, false);
            RegisterRuntimeSmokeCallbacks();
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MatchHudFullMapPopupRuntimeSmoke] result=Failed\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    private static void RegisterRuntimeSmokeCallbacks()
    {
        EditorApplication.update -= UpdateRuntimeSmoke;
        EditorApplication.update += UpdateRuntimeSmoke;
        EditorApplication.playModeStateChanged -= OnRuntimeSmokePlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnRuntimeSmokePlayModeStateChanged;
    }

    private static void OnRuntimeSmokePlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
            SessionState.SetInt(PhaseKey, (int)RuntimeSmokePhase.WaitingForMainMenu);
    }

    private static void UpdateRuntimeSmoke()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedAtKey, 0f) > RuntimeSmokeTimeoutSeconds)
            {
                FinishRuntimeSmoke(false, "Timed out waiting for full-map popup smoke validation.");
                return;
            }

            RuntimeSmokePhase phase = (RuntimeSmokePhase)SessionState.GetInt(PhaseKey, (int)RuntimeSmokePhase.Idle);
            if (phase == RuntimeSmokePhase.WaitingForPlayMode)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.EnterPlaymode();
                return;
            }

            if (!EditorApplication.isPlaying)
                return;

            if (phase == RuntimeSmokePhase.WaitingForMainMenu)
            {
                if (!TryGetShellState(out UiShellStateComponent shellState) ||
                    shellState.CurrentMode != UiShellMode.MainMenu ||
                    shellState.ActiveRoute != UIRoute.MainMenu ||
                    shellState.IsTransitionRunning != 0)
                {
                    return;
                }

                if (!TryEnqueueMatchRoute(out string routeError))
                {
                    FinishRuntimeSmoke(false, routeError);
                    return;
                }

                SessionState.SetInt(PhaseKey, (int)RuntimeSmokePhase.WaitingForMatch);
                return;
            }

            if (phase == RuntimeSmokePhase.WaitingForMatch)
            {
                if (!IsMatchReady())
                    return;

                SessionState.SetInt(PhaseKey, (int)RuntimeSmokePhase.OpeningPopup);
                return;
            }

            if (phase == RuntimeSmokePhase.OpeningPopup)
            {
                MatchHudMinimapView compactMinimap = FindCompactMinimap();
                if (compactMinimap == null)
                    return;

                InvokeFullMapOpenRequest(compactMinimap);
                SessionState.SetBool(PopupOpenedKey, true);
                SessionState.SetInt(PhaseKey, (int)RuntimeSmokePhase.ClosingPopup);
                return;
            }

            if (phase == RuntimeSmokePhase.ClosingPopup)
            {
                if (SessionState.GetBool(CloseInvokedKey, false))
                {
                    MatchHudFullMapPopupView openPopup = FindActiveObject<MatchHudFullMapPopupView>();
                    if (openPopup == null || !openPopup.IsOpen)
                        FinishRuntimeSmoke(true, "Compact minimap opened full-screen tactical map and close action dismissed it.");
                    return;
                }

                MatchHudFullMapPopupView popupView = FindActiveObject<MatchHudFullMapPopupView>();
                if (popupView == null || !popupView.IsOpen)
                    return;

                if (popupView.Minimap == null ||
                    !popupView.Minimap.UseFullMapProjection ||
                    !popupView.Minimap.ShowsViewport ||
                    popupView.Minimap.ZoomInButton == null ||
                    !popupView.Minimap.ZoomInButton.gameObject.activeInHierarchy ||
                    popupView.Minimap.ZoomOutButton == null ||
                    !popupView.Minimap.ZoomOutButton.gameObject.activeInHierarchy)
                {
                    FinishRuntimeSmoke(false, "Full-map popup opened without full-map projection, viewport, or zoom controls.");
                    return;
                }

                if (popupView.Minimap.MapImage == null || popupView.Minimap.MapImage.sprite == null)
                    return;

                InvokeCloseAction(popupView);
                SessionState.SetBool(CloseInvokedKey, true);
                return;
            }
        }
        catch (Exception exception)
        {
            FinishRuntimeSmoke(false, exception.ToString());
        }
    }

    private static bool TryEnqueueMatchRoute(out string error)
    {
        error = string.Empty;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            error = "Default ECS world is missing.";
            return false;
        }

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UiShellRootComponent>(),
            ComponentType.ReadWrite<UiShellRouteRequestComponent>());
        if (query.IsEmptyIgnoreFilter)
        {
            error = "UI shell root is missing.";
            return false;
        }

        Entity boundary = query.GetSingletonEntity();
        DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        routeRequests.Add(new UiShellRouteRequestComponent
        {
            Intent = UiShellRouteIntent.EnterMatch,
            Route = UIRoute.Match,
            PushHistory = 0
        });
        return true;
    }

    private static bool IsMatchReady()
    {
        if (!TryGetShellState(out UiShellStateComponent shellState))
            return false;
        if (!TryGetRuntimeGameplayState(out RuntimeGameplayStateComponent runtimeState))
            return false;
        if (!TryGetMatchIntroState(out MatchIntroTransitionComponent matchIntro))
            return false;

        return shellState.CurrentMode == UiShellMode.MatchHud &&
               shellState.ActiveRoute == UIRoute.Match &&
               shellState.IsTransitionRunning == 0 &&
               runtimeState.PlayRequested != 0 &&
               matchIntro.State == MatchIntroTransitionStateKind.Complete &&
               matchIntro.InputLocked == 0 &&
               SceneManager.GetSceneByName("Match").isLoaded;
    }

    private static bool TryGetShellState(out UiShellStateComponent shellState)
    {
        shellState = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UiShellRootComponent>(),
            ComponentType.ReadOnly<UiShellStateComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        shellState = entityManager.GetComponentData<UiShellStateComponent>(query.GetSingletonEntity());
        return true;
    }

    private static bool TryGetRuntimeGameplayState(out RuntimeGameplayStateComponent runtimeState)
    {
        runtimeState = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        runtimeState = entityManager.GetComponentData<RuntimeGameplayStateComponent>(query.GetSingletonEntity());
        return true;
    }

    private static bool TryGetMatchIntroState(out MatchIntroTransitionComponent matchIntro)
    {
        matchIntro = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MatchIntroTransitionComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        matchIntro = entityManager.GetComponentData<MatchIntroTransitionComponent>(query.GetSingletonEntity());
        return true;
    }

    private static MatchHudMinimapView FindCompactMinimap()
    {
        MatchHudMinimapView[] views = Resources.FindObjectsOfTypeAll<MatchHudMinimapView>();
        for (int i = 0; i < views.Length; i++)
        {
            MatchHudMinimapView view = views[i];
            if (view == null ||
                EditorUtility.IsPersistent(view) ||
                !view.isActiveAndEnabled ||
                view.UseFullMapProjection ||
                view.ShowsViewport)
            {
                continue;
            }

            return view;
        }

        return null;
    }

    private static T FindActiveObject<T>() where T : UnityEngine.Object
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < objects.Length; i++)
        {
            T obj = objects[i];
            if (obj == null || EditorUtility.IsPersistent(obj))
                continue;

            Component component = obj as Component;
            if (component != null && !component.gameObject.activeInHierarchy)
                continue;

            return obj;
        }

        return null;
    }

    private static void InvokeFullMapOpenRequest(MatchHudMinimapView compactMinimap)
    {
        FieldInfo field = typeof(MatchHudMinimapView).GetField(
            "FullMapOpenRequested",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(compactMinimap) is not Action action)
            throw new InvalidOperationException("Compact minimap has no full-map open listener.");

        action.Invoke();
    }

    private static void InvokeCloseAction(MatchHudFullMapPopupView popupView)
    {
        FieldInfo field = typeof(MatchHudFullMapPopupView).GetField(
            "closeAction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(popupView) is not Button closeAction)
            throw new InvalidOperationException("Full-map popup has no close action.");

        closeAction.onClick.Invoke();
    }

    private static void FinishRuntimeSmoke(bool passed, string message)
    {
        EditorApplication.update -= UpdateRuntimeSmoke;
        EditorApplication.playModeStateChanged -= OnRuntimeSmokePlayModeStateChanged;
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseInt(PhaseKey);
        SessionState.EraseFloat(StartedAtKey);
        SessionState.EraseBool(PopupOpenedKey);
        SessionState.EraseBool(CloseInvokedKey);

        if (passed)
            Debug.Log($"[MatchHudFullMapPopupRuntimeSmoke] result=Passed {message}");
        else
            Debug.LogError($"[MatchHudFullMapPopupRuntimeSmoke] result=Failed {message}");

        EditorApplication.Exit(passed ? 0 : 1);
    }

    private static GameObject CreatePopupPrefab()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PopupPrefabPath));

        Sprite outerFrame = LoadSprite(OuterFramePath);
        Sprite innerFrame = LoadSprite(InnerFramePath);
        Sprite closeFrame = LoadSprite(CloseFramePath);
        Sprite closeIcon = LoadSprite(CloseIconPath);
        Sprite zoomFrame = LoadSprite(ZoomFramePath);
        Sprite zoomInIcon = LoadSprite(ZoomInIconPath);
        Sprite zoomOutIcon = LoadSprite(ZoomOutIconPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        GameObject root = new("SCN08_FullMapPopup", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.58f);
            blocker.raycastTarget = true;

            UIPopupMotionView.Ensure(root);
            MatchHudFullMapPopupView popupView = root.AddComponent<MatchHudFullMapPopupView>();

            GameObject panel = CreateImageObject("TacticalMapPanel", root.transform, outerFrame, Color.white, true, Image.Type.Sliced);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            ConfigureRect(panelRect, new Vector2(0.025f, 0.035f), new Vector2(0.975f, 0.965f), Vector2.zero, Vector2.zero);

            TMP_Text title = CreateText("Title", panel.transform, "TACTICAL MAP", font, 44f, TextAlignmentOptions.Left);
            ConfigureRect(title.rectTransform, new Vector2(0.045f, 0.915f), new Vector2(0.55f, 0.975f), Vector2.zero, Vector2.zero);

            Button closeAction = CreateIconAction(
                "CloseAction",
                panel.transform,
                closeFrame,
                closeIcon,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-104f, -76f),
                new Vector2(152f, 132f));

            GameObject mapFrame = CreateImageObject("MapFrame", panel.transform, innerFrame, Color.white, true, Image.Type.Sliced);
            RectTransform mapFrameRect = mapFrame.GetComponent<RectTransform>();
            ConfigureRect(mapFrameRect, new Vector2(0.018f, 0.048f), new Vector2(0.982f, 0.91f), Vector2.zero, Vector2.zero);

            GameObject map = CreateImageObject("Map", mapFrame.transform, null, new Color(0.48f, 0.43f, 0.34f, 1f), true, Image.Type.Simple);
            RectTransform mapRect = map.GetComponent<RectTransform>();
            ConfigureRect(mapRect, new Vector2(0.006f, 0.009f), new Vector2(0.994f, 0.991f), Vector2.zero, Vector2.zero);
            MatchHudMinimapView minimapView = map.AddComponent<MatchHudMinimapView>();

            GameObject markerRoot = CreateUiObject("Markers", map.transform);
            RectTransform markerRect = markerRoot.GetComponent<RectTransform>();
            ConfigureRect(markerRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject viewport = CreateImageObject("Viewport", map.transform, null, new Color(0.65f, 0.95f, 0.30f, 0.12f), false, Image.Type.Simple);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            ConfigureRect(viewportRect, new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.65f), Vector2.zero, Vector2.zero);
            Outline viewportOutline = viewport.AddComponent<Outline>();
            viewportOutline.effectColor = new Color(0.62f, 0.98f, 0.28f, 0.95f);
            viewportOutline.effectDistance = new Vector2(3f, -3f);

            Button zoomIn = CreateIconAction(
                "ZoomIn",
                panel.transform,
                zoomFrame,
                zoomInIcon,
                new Vector2(0.925f, 0.245f),
                new Vector2(0.975f, 0.365f),
                Vector2.zero,
                Vector2.zero);
            Button zoomOut = CreateIconAction(
                "ZoomOut",
                panel.transform,
                zoomFrame,
                zoomOutIcon,
                new Vector2(0.925f, 0.105f),
                new Vector2(0.975f, 0.225f),
                Vector2.zero,
                Vector2.zero);

            TMP_Text instruction = CreateText("Instruction", panel.transform, "DRAG VIEWPORT OR TAP MAP TO FOCUS CAMERA", font, 25f, TextAlignmentOptions.Center);
            ConfigureRect(instruction.rectTransform, new Vector2(0.22f, 0.015f), new Vector2(0.78f, 0.07f), Vector2.zero, Vector2.zero);

            minimapView.Configure(map.GetComponent<Image>(), mapRect, viewportRect, zoomIn, zoomOut, markerRect);
            RemoveGeneratedZoomRelays(zoomIn, zoomOut);
            var popupObject = new SerializedObject(popupView);
            SetObject(popupObject, "popupRoot", root);
            SetObject(popupObject, "minimap", minimapView);
            SetObject(popupObject, "closeAction", closeAction);
            popupObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PopupPrefabPath);
            if (prefab == null)
                throw new System.InvalidOperationException($"Failed to save {PopupPrefabPath}.");

            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AssignToMenuScene(GameObject popupPrefab)
    {
        if (popupPrefab == null)
            return;

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView shellContent = FindSceneObject<UIShellContentView>();
        if (shellContent == null)
            throw new System.InvalidOperationException("Menu scene is missing UIShellContentView.");

        var serialized = new SerializedObject(shellContent);
        SetObject(serialized, "fullMapPopupPrefab", popupPrefab);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static T FindSceneObject<T>() where T : UnityEngine.Object
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < objects.Length; i++)
        {
            UnityEngine.Object obj = objects[i];
            if (obj == null || EditorUtility.IsPersistent(obj))
                continue;

            return objects[i];
        }

        return null;
    }

    private static void ValidateMinimapView(MatchHudMinimapView minimapView, string context)
    {
        if (minimapView == null)
            throw new System.InvalidOperationException($"{context} is missing its full-map MatchHudMinimapView reference.");
        if (minimapView.MapImage == null)
            throw new System.InvalidOperationException($"{context} minimap is missing MapImage.");
        if (minimapView.MapRect == null)
            throw new System.InvalidOperationException($"{context} minimap is missing MapRect.");
        if (minimapView.ViewportRect == null)
            throw new System.InvalidOperationException($"{context} minimap is missing ViewportRect.");
        if (minimapView.ZoomInButton == null)
            throw new System.InvalidOperationException($"{context} minimap is missing ZoomIn.");
        if (minimapView.ZoomOutButton == null)
            throw new System.InvalidOperationException($"{context} minimap is missing ZoomOut.");
        if (minimapView.MarkerRoot == null)
            throw new System.InvalidOperationException($"{context} minimap is missing MarkerRoot.");
    }

    private static void RemoveGeneratedZoomRelays(params Button[] zoomActions)
    {
        for (int i = 0; i < zoomActions.Length; i++)
        {
            Button zoomAction = zoomActions[i];
            MatchHudMinimapZoomPressRelay relay = zoomAction != null
                ? zoomAction.GetComponent<MatchHudMinimapZoomPressRelay>()
                : null;
            if (relay != null)
                UnityEngine.Object.DestroyImmediate(relay);
        }
    }

    private static Button CreateIconAction(
        string name,
        Transform parent,
        Sprite frame,
        Sprite icon,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject action = CreateImageObject(name, parent, frame, Color.white, true, Image.Type.Sliced);
        RectTransform rect = action.GetComponent<RectTransform>();
        ConfigureRect(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        if (size.sqrMagnitude > 0.001f)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        Button button = action.AddComponent<Button>();
        button.targetGraphic = action.GetComponent<Image>();

        if (icon != null)
        {
            GameObject iconObject = CreateImageObject("Icon", action.transform, icon, Color.white, false, Image.Type.Simple);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            ConfigureRect(iconRect, new Vector2(0.20f, 0.20f), new Vector2(0.80f, 0.80f), Vector2.zero, Vector2.zero);
            iconObject.GetComponent<Image>().preserveAspect = true;
        }

        return button;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string text,
        TMP_FontAsset font,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject obj = CreateUiObject(name, parent);
        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = font;
        label.fontSize = fontSize;
        label.fontSizeMin = Mathf.Max(14f, fontSize * 0.55f);
        label.fontSizeMax = fontSize;
        label.enableAutoSizing = true;
        label.alignment = alignment;
        label.color = new Color(0.96f, 0.90f, 0.70f, 1f);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static GameObject CreateImageObject(
        string name,
        Transform parent,
        Sprite sprite,
        Color color,
        bool raycastTarget,
        Image.Type type)
    {
        GameObject obj = CreateUiObject(name, parent);
        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? type : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = raycastTarget;
        image.preserveAspect = false;
        return obj;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject obj = new(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return obj;
    }

    private static void ConfigureRect(
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
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void Stretch(RectTransform rect)
    {
        ConfigureRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[MatchHudFullMapPopupPrefabSetup] Missing sprite at {path}");
        return sprite;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new System.InvalidOperationException($"Missing serialized property {propertyName} on {serializedObject.targetObject}.");

        property.objectReferenceValue = value;
    }
}
#endif
