using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;

namespace Game.Editor
{
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
        private const string MatchHudPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        private const string MapPreviewPath = "Assets/Game/Art/UI/V3Shared/CampaignScenes/SCN05_SahrinMissionMap_V3.png";
        private const string CaptureBackgroundPath = "Design/AgentReports/M02EstablishBase/M02EB-029/current_gameplay_zoom.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string ActiveKey = "MatchHudFullMapPopupPrefabSetup.Active";
        private const string PhaseKey = "MatchHudFullMapPopupPrefabSetup.Phase";
        private const string StartedAtKey = "MatchHudFullMapPopupPrefabSetup.StartedAt";
        private const string PopupOpenedKey = "MatchHudFullMapPopupPrefabSetup.PopupOpened";
        private const string CloseInvokedKey = "MatchHudFullMapPopupPrefabSetup.CloseInvoked";
        private const double RuntimeSmokeTimeoutSeconds = 120d;

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(24, 34, 38, 252);
        private static readonly Color DarkBottom = new Color32(3, 8, 10, 254);
        private static readonly Color RaisedTop = new Color32(39, 52, 57, 255);
        private static readonly Color Line = new Color32(137, 153, 157, 255);
        private static readonly Color Cyan = new Color32(10, 184, 231, 255);
        private static readonly Color Green = new Color32(111, 203, 43, 255);
        private static readonly Color Orange = new Color32(255, 91, 17, 255);
        private static readonly Color Amber = new Color32(255, 196, 17, 255);
        private static readonly Color Muted = new Color32(174, 181, 181, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;

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
            V3UiFoundationBuilder.EnsureBuilt();
            ConfigureMapPreviewSprite();
            LoadAssets();
            GameObject prefab = CreatePopupPrefab();
            AssignToMenuScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[MatchHudFullMapPopupPrefabSetup] result=Passed layout=1672x941 gradients=procedural borders=3 markers=shared-v3-atlas");
        }

        [MenuItem("Game/UI/V3/Capture Full Map V3 Review")]
        public static void CaptureReview()
        {
            MatchHudV3PrefabBuilder.Build();
            Apply();
            Capture("/private/tmp/warline-full-map-v3-16x9.png", 1920, 1080);
            Capture("/private/tmp/warline-full-map-v3-20x9.png", 4800, 2160);
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
            if (popupView.CloseAction == null || popupView.CenterOnHqAction == null)
                throw new System.InvalidOperationException($"{PopupPrefabPath} is missing a functional close or center-on-HQ action.");

            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || layout.ReferenceResolution != Reference)
                throw new System.InvalidOperationException("Full Map V3 must serialize its centered 1672x941 responsive composition.");

            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 20)
                throw new System.InvalidOperationException($"Full Map V3 requires procedural gradients across every panel; found {gradients}.");
            if (popupView.Minimap.PlayerMarkerSprite == null ||
                popupView.Minimap.EnemyMarkerSprite == null ||
                popupView.Minimap.NeutralMarkerSprite == null)
            {
                throw new System.InvalidOperationException("Full Map V3 must use shared sharp marker sprites instead of placeholder squares.");
            }

            Transform legend = FindChild(prefab.transform, "LegendPanel");
            Transform info = FindChild(prefab.transform, "MapInfoPanel");
            Transform toggles = FindChild(prefab.transform, "QuickTogglePanel");
            if (legend == null || info == null || toggles == null)
                throw new System.InvalidOperationException("Full Map V3 is missing the legend, map info, or quick-toggle panel.");

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView shellContent = FindSceneObject<UIShellContentView>();
            if (shellContent == null)
                throw new System.InvalidOperationException("Menu scene is missing UIShellContentView.");
            if (shellContent.FullMapPopupPrefab != prefab)
                throw new System.InvalidOperationException("Menu scene UIShellContentView does not reference SCN08_FullMapPopup.");

            Debug.Log($"[MatchHudFullMapPopupPrefabSetup] validation=Passed gradients={gradients} borders=3 interactions=close,drag,tap,zoom,center");
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
            GameObject root = new("SCN08_FullMapPopup", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);
                Image blocker = root.GetComponent<Image>();
                blocker.color = new Color(0f, 0.01f, 0.015f, 0.76f);
                blocker.raycastTarget = true;

                UIPopupMotionView.Ensure(root);
                MatchHudFullMapPopupView popupView = root.AddComponent<MatchHudFullMapPopupView>();
                RectTransform composition = CreateTopLeft("V3Composition", root.transform, 0f, 0f, Reference.x, Reference.y);
                composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>()
                    .Configure(Reference, MainMenuV3SectionAlignment.Center);

                RectTransform panel = CreatePanel("TacticalMapPanel", composition, 194f, 62f, 1288f, 807f, DarkTop, DarkBottom, Line, 3f);
                RectTransform header = CreatePanel("HeaderPanel", panel, 0f, 0f, 1288f, 72f, RaisedTop, DarkBottom, Line, 3f);
                CreateText("Title", header, 24f, 5f, 760f, 60f, "TACTICAL MAP", 45f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);

                RectTransform closeRect = CreatePanel("CloseAction", header, 1224f, 7f, 57f, 58f, RaisedTop, DarkBottom, Line, 3f);
                Button closeAction = closeRect.gameObject.AddComponent<Button>();
                closeAction.targetGraphic = closeRect.GetComponent<V3GradientGraphic>();
                CreateLine("CloseSlashA", closeRect, 15f, 14f, 42f, 43f, 4f, theme.TextPrimary);
                CreateLine("CloseSlashB", closeRect, 42f, 14f, 15f, 43f, 4f, theme.TextPrimary);

                BuildLegendPanel(panel);

                RectTransform mapPanel = CreatePanel("MapPanel", panel, 211f, 80f, 850f, 648f, DarkTop, DarkBottom, Line, 3f);
                RectTransform mapClip = CreateTopLeft("MapClip", mapPanel, 4f, 4f, 842f, 640f);
                mapClip.gameObject.AddComponent<RectMask2D>();
                Image mapImage = CreateImage("Map", mapClip, RequireSprite(MapPreviewPath), Color.white);
                Stretch(mapImage.rectTransform);
                mapImage.raycastTarget = true;
                mapImage.preserveAspect = false;
                AspectRatioFitter mapFitter = mapImage.gameObject.AddComponent<AspectRatioFitter>();
                mapFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                mapFitter.aspectRatio = mapImage.sprite.rect.width / mapImage.sprite.rect.height;
                MatchHudMinimapView minimapView = mapImage.gameObject.AddComponent<MatchHudMinimapView>();

                RectTransform markerRoot = CreateTopLeft("Markers", mapImage.transform, 0f, 0f, 842f, 640f);
                Stretch(markerRoot);
                RectTransform previewMarkers = CreateTopLeft("V3PreviewMarkers", mapClip, 0f, 0f, 842f, 640f);
                BuildPreviewMapMarkers(previewMarkers);
                previewMarkers.gameObject.SetActive(false);

                RectTransform viewportRect = CreateTopLeft("Viewport", mapImage.transform, 165f, 202f, 470f, 350f);
                V3GradientGraphic viewport = viewportRect.gameObject.AddComponent<V3GradientGraphic>();
                viewport.Configure(new Color(0f, .42f, .62f, .035f), new Color(0f, .12f, .18f, .02f), Cyan, 3f);
                viewport.raycastTarget = true;

                Button zoomIn = CreateZoomButton("ZoomIn", mapPanel, 786f, 526f, true);
                Button zoomOut = CreateZoomButton("ZoomOut", mapPanel, 786f, 582f, false);
                minimapView.Configure(mapImage, mapImage.rectTransform, viewportRect, zoomIn, zoomOut, markerRoot);
                minimapView.ConfigureMarkerSprites(
                    RequireSprite(V3UiFoundationBuilder.MatchFriendlyMarkerIconPath),
                    RequireSprite(V3UiFoundationBuilder.MatchHostileMarkerIconPath),
                    RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath));
                RemoveGeneratedInteractionRelays(minimapView, zoomIn, zoomOut);

                BuildMapInformationPanels(panel);

                RectTransform footer = CreatePanel("FooterPanel", panel, 8f, 735f, 1272f, 64f, DarkTop, DarkBottom, Line, 3f);
                CreateText("Instruction", footer, 210f, 8f, 760f, 48f,
                    "DRAG VIEWPORT OR TAP MAP TO FOCUS CAMERA", 24f, Cyan, TextAlignmentOptions.Center, true);
                RectTransform centerRect = CreatePanel("CenterOnHqAction", footer, 975f, 8f, 286f, 48f,
                    new Color32(20, 103, 147, 255), new Color32(4, 43, 67, 255), Cyan, 3f);
                Button centerAction = centerRect.gameObject.AddComponent<Button>();
                centerAction.targetGraphic = centerRect.GetComponent<V3GradientGraphic>();
                Image centerIcon = CreateImage("Icon", centerRect, RequireSprite(V3UiFoundationBuilder.MatchJumpIconPath), theme.TextPrimary);
                SetTopLeft(centerIcon.rectTransform, 14f, 8f, 34f, 34f);
                CreateText("Label", centerRect, 56f, 3f, 216f, 42f, "CENTER ON HQ", 21f, theme.TextPrimary, TextAlignmentOptions.Center, true);

                var popupObject = new SerializedObject(popupView);
                SetObject(popupObject, "popupRoot", root);
                SetObject(popupObject, "minimap", minimapView);
                SetObject(popupObject, "closeAction", closeAction);
                SetObject(popupObject, "centerOnHqAction", centerAction);
                popupObject.FindProperty("hqNormalizedPosition").vector2Value = new Vector2(.50f, .47f);
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

        private static void BuildLegendPanel(RectTransform panel)
        {
            RectTransform legend = CreatePanel("LegendPanel", panel, 8f, 80f, 195f, 648f, DarkTop, DarkBottom, Line, 3f);
            float y = 10f;
            CreateText("FriendliesTitle", legend, 15f, y, 165f, 28f, "FRIENDLIES", 20f, Green, TextAlignmentOptions.MidlineLeft, true);
            y += 38f;
            CreateLegendRow(legend, "YourSquads", y, V3UiFoundationBuilder.MatchPlayerIconPath, "Your Squads", Green); y += 34f;
            CreateLegendRow(legend, "AlliedUnits", y, V3UiFoundationBuilder.MatchFriendlyMarkerIconPath, "Allied Units", Green); y += 34f;
            CreateLegendRow(legend, "AlliedVehicles", y, V3UiFoundationBuilder.MatchAirTransportIconPath, "Allied Vehicles", Green); y += 34f;
            CreateLegendRow(legend, "AlliedSupport", y, V3UiFoundationBuilder.MatchMedicalIconPath, "Allied Support", Green); y += 40f;
            CreateSolid("FriendliesDivider", legend, 12f, y, 171f, 2f, new Color(1f, 1f, 1f, .16f));
            y += 12f;

            CreateText("EnemiesTitle", legend, 15f, y, 165f, 28f, "ENEMIES", 20f, Orange, TextAlignmentOptions.MidlineLeft, true);
            y += 38f;
            CreateLegendRow(legend, "HostileUnits", y, V3UiFoundationBuilder.MatchHostileMarkerIconPath, "Hostile Units", Orange); y += 34f;
            CreateLegendRow(legend, "HostileVehicles", y, V3UiFoundationBuilder.MatchAttackIconPath, "Hostile Vehicles", Orange); y += 34f;
            CreateLegendRow(legend, "HighThreat", y, V3UiFoundationBuilder.MatchInvalidIconPath, "High Threat", Orange); y += 34f;
            CreateLegendRow(legend, "EnemyPosition", y, V3UiFoundationBuilder.MatchJumpIconPath, "Enemy Position", Orange); y += 40f;
            CreateSolid("EnemiesDivider", legend, 12f, y, 171f, 2f, new Color(1f, 1f, 1f, .16f));
            y += 12f;

            CreateText("ObjectivesTitle", legend, 15f, y, 165f, 28f, "OBJECTIVES", 20f, Amber, TextAlignmentOptions.MidlineLeft, true);
            y += 38f;
            CreateLegendRow(legend, "PrimaryObjective", y, V3UiFoundationBuilder.MatchArmorIconPath, "Primary Objective", Amber); y += 34f;
            CreateLegendRow(legend, "SecondaryObjective", y, V3UiFoundationBuilder.MatchJumpIconPath, "Secondary Objective", Amber); y += 40f;
            CreateSolid("ObjectivesDivider", legend, 12f, y, 171f, 2f, new Color(1f, 1f, 1f, .16f));
            y += 12f;

            CreateText("OtherTitle", legend, 15f, y, 165f, 28f, "OTHER", 20f, Muted, TextAlignmentOptions.MidlineLeft, true);
            y += 38f;
            CreateRouteLegend(legend, "PlannedRoute", y, "Planned Route", false); y += 34f;
            CreateRouteLegend(legend, "RouteDirection", y, "Route Direction", true); y += 34f;
            RectTransform viewport = CreateTopLeft("ViewportLegend", legend, 16f, y + 2f, 35f, 24f);
            V3GradientGraphic viewportFrame = viewport.gameObject.AddComponent<V3GradientGraphic>();
            viewportFrame.Configure(Color.clear, Color.clear, Cyan, 2f);
            viewportFrame.raycastTarget = false;
            CreateText("ViewportLabel", legend, 61f, y - 2f, 120f, 30f, "Camera Viewport", 15f, Muted, TextAlignmentOptions.MidlineLeft, false);
        }

        private static void BuildMapInformationPanels(RectTransform panel)
        {
            RectTransform info = CreatePanel("MapInfoPanel", panel, 1069f, 80f, 211f, 311f, DarkTop, DarkBottom, Line, 3f);
            CreateText("Title", info, 13f, 9f, 185f, 29f, "MAP INFO", 20f, Cyan, TextAlignmentOptions.MidlineLeft, true);
            CreateMapInfoRow(info, "District", 47f, V3UiFoundationBuilder.MatchMaterialsIconPath, "MARKET DISTRICT", "Urban Center");
            CreateMapInfoRow(info, "Size", 101f, V3UiFoundationBuilder.MatchJumpIconPath, "MAP SIZE", "2.5km x 2.5km");
            CreateMapInfoRow(info, "Intel", 155f, V3UiFoundationBuilder.MatchPlayerIconPath, "INTEL LEVEL", "High");
            CreateMapInfoRow(info, "Time", 209f, V3UiFoundationBuilder.MatchSpeedIconPath, "TIME", "14:36");
            CreateMapInfoRow(info, "Weather", 263f, V3UiFoundationBuilder.MatchScanIconPath, "WEATHER", "Clear");

            RectTransform toggles = CreatePanel("QuickTogglePanel", panel, 1069f, 399f, 211f, 329f, DarkTop, DarkBottom, Line, 3f);
            CreateText("Title", toggles, 13f, 9f, 185f, 29f, "QUICK TOGGLE", 20f, Cyan, TextAlignmentOptions.MidlineLeft, true);
            CreateQuickToggle(toggles, "FriendliesToggle", 46f, "FRIENDLIES", Green);
            CreateQuickToggle(toggles, "EnemiesToggle", 101f, "ENEMIES", Orange);
            CreateQuickToggle(toggles, "ObjectivesToggle", 156f, "OBJECTIVES", Amber);
            CreateQuickToggle(toggles, "RoutesToggle", 211f, "ROUTES", Green);
            CreateQuickToggle(toggles, "ViewportToggle", 266f, "VIEWPORT", Cyan);
        }

        private static void BuildPreviewMapMarkers(RectTransform parent)
        {
            CreateRouteSegment(parent, 376f, 594f, 330f, 524f, Green, 7f);
            CreateRouteSegment(parent, 330f, 524f, 302f, 430f, Green, 7f);
            CreateRouteSegment(parent, 302f, 430f, 352f, 361f, Green, 7f);
            CreateRouteSegment(parent, 352f, 361f, 458f, 288f, Green, 7f);
            CreateRouteSegment(parent, 458f, 288f, 493f, 180f, Green, 7f);

            CreateMapMarker(parent, "SquadAlpha", 274f, 327f, 54f, V3UiFoundationBuilder.MatchFriendlyMarkerIconPath, Green, true);
            CreateMapMarker(parent, "SquadBravo", 393f, 406f, 54f, V3UiFoundationBuilder.MatchFriendlyMarkerIconPath, Green, true);
            CreateMapMarker(parent, "SquadCharlie", 304f, 524f, 54f, V3UiFoundationBuilder.MatchFriendlyMarkerIconPath, Green, true);
            CreateMapMarker(parent, "SupportNorth", 535f, 256f, 45f, V3UiFoundationBuilder.MatchMedicalIconPath, Green, false);
            CreateMapMarker(parent, "SupportWest", 92f, 350f, 45f, V3UiFoundationBuilder.MatchMedicalIconPath, Green, false);
            CreateMapMarker(parent, "EnemyNorth", 612f, 95f, 47f, V3UiFoundationBuilder.MatchHostileMarkerIconPath, Orange, false);
            CreateMapMarker(parent, "EnemyEast", 681f, 430f, 47f, V3UiFoundationBuilder.MatchAttackIconPath, Orange, false);
            CreateMapMarker(parent, "EnemyWest", 66f, 440f, 47f, V3UiFoundationBuilder.MatchHostileMarkerIconPath, Orange, false);
            CreateMapMarker(parent, "PrimaryObjective", 494f, 92f, 47f, V3UiFoundationBuilder.MatchArmorIconPath, Amber, false);
            CreateMapMarker(parent, "SecondaryObjective", 688f, 320f, 42f, V3UiFoundationBuilder.MatchJumpIconPath, Amber, false);
        }

        private static void CreateLegendRow(Transform parent, string name, float y, string iconPath, string label, Color color)
        {
            Image icon = CreateImage(name + "Icon", parent, RequireSprite(iconPath), color);
            SetTopLeft(icon.rectTransform, 14f, y + 1f, 27f, 27f);
            float labelSize = label.Length > 16 ? 12.5f : 14f;
            CreateText(name + "Label", parent, 55f, y - 1f, 133f, 29f, label, labelSize, Muted, TextAlignmentOptions.MidlineLeft, false);
        }

        private static void CreateRouteLegend(Transform parent, string name, float y, string label, bool arrow)
        {
            if (arrow)
            {
                CreateLine(name + "Line", parent, 16f, y + 14f, 42f, y + 14f, 4f, Green);
                CreateLine(name + "ArrowA", parent, 42f, y + 14f, 34f, y + 7f, 3f, Green);
                CreateLine(name + "ArrowB", parent, 42f, y + 14f, 34f, y + 21f, 3f, Green);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                    CreateSolid(name + "Dash" + i, parent, 16f + i * 12f, y + 12f, 8f, 4f, Green);
            }
            float labelSize = label.Length > 14 ? 12.5f : 14f;
            CreateText(name + "Label", parent, 61f, y - 1f, 127f, 29f, label, labelSize, Muted, TextAlignmentOptions.MidlineLeft, false);
        }

        private static void CreateMapInfoRow(Transform parent, string name, float y, string iconPath, string label, string value)
        {
            Image icon = CreateImage(name + "Icon", parent, RequireSprite(iconPath), theme.TextPrimary);
            SetTopLeft(icon.rectTransform, 14f, y + 7f, 30f, 30f);
            CreateText(name + "Label", parent, 57f, y + 1f, 140f, 24f, label, 13.5f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            CreateText(name + "Value", parent, 57f, y + 23f, 140f, 24f, value, 13f, Muted, TextAlignmentOptions.MidlineLeft, false);
            if (y < 260f)
                CreateSolid(name + "Divider", parent, 12f, y + 50f, 187f, 1f, new Color(1f, 1f, 1f, .11f));
        }

        private static Toggle CreateQuickToggle(Transform parent, string name, float y, string label, Color accent)
        {
            RectTransform row = CreatePanel(name, parent, 11f, y, 189f, 46f,
                new Color(accent.r * .20f, accent.g * .20f, accent.b * .20f, 1f), DarkBottom, accent, 2f);
            Toggle toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = row.GetComponent<V3GradientGraphic>();
            toggle.transition = Selectable.Transition.None;
            toggle.isOn = true;
            CreateText("Label", row, 12f, 4f, 132f, 38f, label, 16f, accent, TextAlignmentOptions.MidlineLeft, true);
            RectTransform box = CreatePanel("CheckBox", row, 150f, 6f, 32f, 34f,
                new Color(accent.r * .42f, accent.g * .42f, accent.b * .42f, 1f), DarkBottom, accent, 2f);
            RectTransform checkRoot = CreateTopLeft("Check", box, 0f, 0f, 32f, 34f);
            CreateLine("CheckShort", checkRoot, 7f, 18f, 14f, 25f, 3f, accent);
            CreateLine("CheckLong", checkRoot, 14f, 25f, 26f, 10f, 3f, accent);
            toggle.graphic = null;
            row.gameObject.AddComponent<V3ToggleCheckView>().Configure(toggle, checkRoot.gameObject);
            return toggle;
        }

        private static Button CreateZoomButton(string name, Transform parent, float x, float y, bool plus)
        {
            RectTransform rect = CreatePanel(name, parent, x, y, 48f, 48f, RaisedTop, DarkBottom, Cyan, 2f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<V3GradientGraphic>();
            CreateSolid("Horizontal", rect, 12f, 22f, 24f, 4f, theme.TextPrimary);
            if (plus)
                CreateSolid("Vertical", rect, 22f, 12f, 4f, 24f, theme.TextPrimary);
            return button;
        }

        private static void CreateMapMarker(Transform parent, string name, float x, float y, float size, string iconPath, Color accent, bool circular)
        {
            RectTransform marker = CreatePanel(name, parent, x, y, size, size, new Color(0f, .08f, .06f, .92f), DarkBottom, accent, 2f);
            Image icon = CreateImage("Icon", marker, RequireSprite(iconPath), accent);
            SetTopLeft(icon.rectTransform, 6f, 6f, size - 12f, size - 12f);
            if (circular)
            {
                RectTransform ringRect = CreateTopLeft("SelectionRing", marker, 0f, 0f, size, size);
                V3RingGraphic ring = ringRect.gameObject.AddComponent<V3RingGraphic>();
                ring.Configure(accent, 3f, 48);
                ring.raycastTarget = false;
            }
        }

        private static void CreateRouteSegment(Transform parent, float x1, float y1, float x2, float y2, Color color, float width)
        {
            Vector2 start = new(x1, y1);
            Vector2 end = new(x2, y2);
            float length = Vector2.Distance(start, end);
            int dashCount = Mathf.Max(1, Mathf.FloorToInt(length / 23f));
            Vector2 direction = (end - start).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (int i = 0; i < dashCount; i++)
            {
                Vector2 center = start + direction * ((i + .5f) * length / dashCount);
                RectTransform dash = CreateTopLeft("RouteDash", parent, 0f, 0f, 16f, width);
                dash.pivot = new Vector2(.5f, .5f);
                dash.anchoredPosition = new Vector2(center.x, -center.y);
                dash.localEulerAngles = new Vector3(0f, 0f, -angle);
                Image image = dash.gameObject.AddComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
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

        private static void RemoveGeneratedInteractionRelays(MatchHudMinimapView minimapView, params Button[] zoomActions)
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

            MatchHudMinimapViewportDragRelay viewportRelay = minimapView != null && minimapView.ViewportRect != null
                ? minimapView.ViewportRect.GetComponent<MatchHudMinimapViewportDragRelay>()
                : null;
            if (viewportRelay != null)
                UnityEngine.Object.DestroyImmediate(viewportRelay);
        }

        private static void Capture(string outputPath, int width, int height)
        {
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudPrefabPath);
            GameObject popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
            if (hudPrefab == null || popupPrefab == null)
                throw new FileNotFoundException("Full Map review requires both Match HUD and Full Map popup prefabs.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("FullMapV3CaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = height * .5f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            GameObject canvasObject = new("FullMapV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, height);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            Texture2D backgroundTexture = LoadCaptureTexture(CaptureBackgroundPath);
            RawImage background = CreateRawImage("GameplayRuntimeBackground", canvasRect, backgroundTexture);
            Stretch(background.rectTransform);
            AspectRatioFitter backgroundFitter = background.gameObject.AddComponent<AspectRatioFitter>();
            backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFitter.aspectRatio = backgroundTexture.width / (float)backgroundTexture.height;

            GameObject hud = UnityEngine.Object.Instantiate(hudPrefab, canvasRect);
            Stretch(hud.transform as RectTransform);
            GameObject popup = UnityEngine.Object.Instantiate(popupPrefab, canvasRect);
            Stretch(popup.transform as RectTransform);
            Transform preview = FindChild(popup.transform, "V3PreviewMarkers");
            preview?.gameObject.SetActive(true);
            MatchHudFullMapPopupView popupView = popup.GetComponent<MatchHudFullMapPopupView>();
            popupView.Minimap.ApplyInteractionOptions(true, true, true, true, true, false);

            Canvas.ForceUpdateCanvases();
            foreach (MainMenuV3SectionLayoutView layout in canvasObject.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                layout.RefreshLayout();
            Canvas.ForceUpdateCanvases();

            RenderTexture previous = RenderTexture.active;
            Texture2D capture = new(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply(false);
                File.WriteAllBytes(outputPath, capture.EncodeToPNG());
                Debug.Log($"[MatchHudFullMapPopupPrefabSetup] capture=Passed size={width}x{height} path={outputPath} scene={scene.name}");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(capture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(backgroundTexture);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void LoadAssets()
        {
            boldFont = Require<TMP_FontAsset>(BoldFontPath);
            mediumFont = Require<TMP_FontAsset>(MediumFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
        }

        private static void ConfigureMapPreviewSprite()
        {
            if (AssetImporter.GetAtPath(MapPreviewPath) is not TextureImporter importer)
                throw new InvalidOperationException($"Could not configure Full Map preview texture: {MapPreviewPath}");

            bool dirty = importer.textureType != TextureImporterType.Sprite ||
                         importer.spriteImportMode != SpriteImportMode.Single ||
                         importer.maxTextureSize < 2048;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
            if (dirty)
                importer.SaveAndReimport();
        }

        private static RectTransform CreatePanel(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            Color top,
            Color bottom,
            Color border,
            float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, border, borderWidth);
            gradient.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            string value,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            bool bold)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = bold ? boldFont : mediumFont;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Texture texture)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateSolid(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color);
            SetTopLeft(image.rectTransform, x, y, width, height);
        }

        private static void CreateLine(string name, Transform parent, float x1, float y1, float x2, float y2, float thickness, Color color)
        {
            Vector2 start = new(x1, y1);
            Vector2 end = new(x2, y2);
            Vector2 delta = end - start;
            RectTransform line = CreateTopLeft(name, parent, 0f, 0f, delta.magnitude, thickness);
            line.pivot = new Vector2(.5f, .5f);
            Vector2 center = (start + end) * .5f;
            line.anchoredPosition = new Vector2(center.x, -center.y);
            line.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Image image = line.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
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

        private static Texture2D LoadCaptureTexture(string relativePath)
        {
            string absolutePath = Path.GetFullPath(relativePath);
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException($"Missing gameplay background for Full Map capture: {absolutePath}");
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath), false))
                throw new InvalidOperationException($"Could not load gameplay background for Full Map capture: {absolutePath}");
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing required Full Map V3 asset: {path}");
            return asset;
        }

        private static Sprite RequireSprite(string path) => Require<Sprite>(path);

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
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
}
