using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using SettingsService = Game.UI.Runtime.SettingsService;

public sealed class SettingsPopupValidationTests
{
    private const string ApprovedSharedSpriteFolder = "Assets/Game/Art/UI/V3Shared";
    private const string ApprovedSettingsIcon = "Assets/Game/Art/UI/V3Shared/Sprites/Icons/Settings/ui_icon_settings_gear.png";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string SharedSettingsPopupPath = "Assets/Game/Prefabs/UI/Shell/Popups/SCN_SettingsPopup.prefab";
    private const string LegacyMenuSettingsPopupPath = "Assets/Game/Prefabs/UI/Shell/Popups/SCN02_MenuSettingsPopup.prefab";
    private const string LegacyMatchSettingsPopupPath = "Assets/Game/Prefabs/UI/Shell/Popups/SCN08_MatchSettingsPopup.prefab";
    private const string MainMenuContentPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
    private const string MatchHudContentPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string PauseMenuPopupPath = "Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(SettingsService_DefaultsAndPersistsAssistantLevel),
                test => test.SettingsService_DefaultsAndPersistsAssistantLevel(),
                ref passed);
            RunValidationStep(
                nameof(SettingsPopupPrefabs_UseOnlyApprovedSprites),
                test => test.SettingsPopupPrefabs_UseOnlyApprovedSprites(),
                ref passed);
            RunValidationStep(
                nameof(SettingsPopupPrefabs_ExposeRequiredControls),
                test => test.SettingsPopupPrefabs_ExposeRequiredControls(),
                ref passed);
            RunValidationStep(
                nameof(SettingsPopupPrefabs_MatchV3VerticalTabStructure),
                test => test.SettingsPopupPrefabs_MatchV3VerticalTabStructure(),
                ref passed);
            RunValidationStep(
                nameof(SettingsPopupPrefabs_AuthorReadableNonOverlappingLayout),
                test => test.SettingsPopupPrefabs_AuthorReadableNonOverlappingLayout(),
                ref passed);
            RunValidationStep(
                nameof(SettingsPopupPrefabs_FrameImagesUseSlicedPpuTwo),
                test => test.SettingsPopupPrefabs_FrameImagesUseSlicedPpuTwo(),
                ref passed);
            RunValidationStep(
                nameof(SettingsButtons_EnqueueOpenSettingsAction),
                test => test.SettingsButtons_EnqueueOpenSettingsAction(),
                ref passed);
            RunValidationStep(
                nameof(UiActionRequestSystem_OpenSettingsProducesSettingsPopupRequest),
                test => test.UiActionRequestSystem_OpenSettingsProducesSettingsPopupRequest(),
                ref passed);
            RunValidationStep(
                nameof(MenuSceneShell_InstallsMenuAndMatchSettingsPopups),
                test => test.MenuSceneShell_InstallsMenuAndMatchSettingsPopups(),
                ref passed);
            RunValidationStep(
                nameof(MenuDiagnosticsLogPanel_IsSuppressedDuringMatch),
                test => test.MenuDiagnosticsLogPanel_IsSuppressedDuringMatch(),
                ref passed);

            Debug.Log($"[SettingsPopupValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SettingsPopupValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void MenuDiagnosticsLogPanel_IsSuppressedDuringMatch()
    {
        GameObject logPanel = new("MenuDiagnosticsLogPanelTest");
        try
        {
            logPanel.SetActive(true);
            Assert.IsTrue(
                MenuDiagnosticsUiSystemHelper.SuppressRuntimeLogPanelForRoute(logPanel, UIRoute.Match),
                "The legacy menu diagnostics log must be suppressed while the Match route owns the screen.");
            Assert.IsFalse(
                logPanel.activeSelf,
                "The legacy full-screen log panel must not cover Match HUD controls or Android lifecycle input.");

            logPanel.SetActive(true);
            Assert.IsFalse(
                MenuDiagnosticsUiSystemHelper.SuppressRuntimeLogPanelForRoute(logPanel, UIRoute.MainMenu),
                "The menu route may retain its explicit diagnostics toggle.");
            Assert.IsTrue(logPanel.activeSelf, "Menu diagnostics should remain available on menu routes.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(logPanel);
        }
    }

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    }

    [Test]
    public void SettingsService_DefaultsAndPersistsAssistantLevel()
    {
        UISettingsModel previous = SettingsService.Load();

        try
        {
            UISettingsModel defaults = SettingsService.ResetToDefaults();
            Assert.AreEqual(
                UIAssistanceLevel.FullGuidance,
                defaults.Assistant.AssistanceLevel,
                "Settings defaults must include the documented full-guidance assistant level.");
            Assert.AreEqual(
                UIAssistantNarrationMode.Important,
                defaults.Assistant.NarrationMode,
                "Settings defaults must match the ECS important-only narration default.");
            Assert.IsTrue(
                defaults.Assistant.AllowTakeover,
                "Settings defaults must allow bounded ARIA takeover.");
            Assert.IsTrue(
                defaults.Assistant.SubtitlesEnabled,
                "Settings defaults must keep ARIA text subtitles visible.");

            UISettingsModel model = defaults;
            model.Assistant.AssistanceLevel = UIAssistanceLevel.HintsOnly;
            model.Assistant.NarrationMode = UIAssistantNarrationMode.CriticalOnly;
            model.Assistant.AllowTakeover = false;
            model.Assistant.SubtitlesEnabled = false;
            SettingsService.Save(model);

            UISettingsModel loaded = SettingsService.Load();
            Assert.AreEqual(
                UIAssistanceLevel.HintsOnly,
                loaded.Assistant.AssistanceLevel,
                "Assistant assistance level must round-trip through SettingsService.");
            Assert.AreEqual(
                UIAssistantNarrationMode.CriticalOnly,
                loaded.Assistant.NarrationMode,
                "Assistant narration mode must round-trip through SettingsService.");
            Assert.IsFalse(
                loaded.Assistant.AllowTakeover,
                "Assistant takeover permission must round-trip through SettingsService.");
            Assert.IsFalse(
                loaded.Assistant.SubtitlesEnabled,
                "Assistant subtitle visibility must round-trip through SettingsService.");
        }
        finally
        {
            SettingsService.Save(previous);
        }
    }

    [Test]
    public void SettingsPopupPrefabs_UseOnlyApprovedSprites()
    {
        AssertLegacyDuplicatePopupRemoved(LegacyMenuSettingsPopupPath);
        AssertLegacyDuplicatePopupRemoved(LegacyMatchSettingsPopupPath);
        AssertPrefabUsesOnlyApprovedSprites(SharedSettingsPopupPath);
    }

    [Test]
    public void SettingsPopupPrefabs_ExposeRequiredControls()
    {
        AssertSettingsPopupPrefab(SharedSettingsPopupPath);
    }

    [Test]
    public void SettingsPopupPrefabs_MatchV3VerticalTabStructure()
    {
        GameObject prefab = LoadPrefab(SharedSettingsPopupPath);
        V3SettingsTabView tabView = prefab.GetComponentInChildren<V3SettingsTabView>(true);
        Assert.NotNull(tabView, "V3 Settings must own the approved vertical tab controller.");
        Assert.AreEqual(4, tabView.TabButtons.Length, "V3 Settings must expose four large category tabs.");
        Assert.AreEqual(4, tabView.Pages.Length, "V3 Settings must expose four single-page content surfaces.");

        string[] tabs = { "AUDIOTab", "GAMEPLAYTab", "VIDEOTab", "ACCESSIBILITYTab" };
        string[] pages = { "AudioPage", "GameplayPage", "VideoPage", "AccessibilityPage" };
        int activePages = 0;
        for (int i = 0; i < tabs.Length; i++)
        {
            RectTransform tab = FindChild(prefab.transform, tabs[i]) as RectTransform;
            Transform page = FindChild(prefab.transform, pages[i]);
            Assert.NotNull(tab, $"Missing V3 Settings tab {tabs[i]}.");
            Assert.GreaterOrEqual(tab.rect.height, 130f, $"{tabs[i]} must retain the large touch target from the V3 lock.");
            Assert.NotNull(page, $"Missing V3 Settings page {pages[i]}.");
            if (page.gameObject.activeSelf)
                activePages++;
        }

        Assert.AreEqual(1, activePages, "Only one V3 Settings page may be visible at a time.");
        Assert.IsTrue(FindChild(prefab.transform, "AudioPage").gameObject.activeSelf, "Audio must be the authored default page.");
        Assert.IsNull(FindChild(prefab.transform, "AudioSection"), "Legacy four-panel Settings layout must not return.");
        Assert.IsNull(FindChild(prefab.transform, "ControlSection"), "Legacy four-panel Settings layout must not return.");
        Assert.IsNull(FindChild(prefab.transform, "DisplaySection"), "Legacy four-panel Settings layout must not return.");
        Assert.IsNull(FindChild(prefab.transform, "AccessibilitySection"), "Legacy four-panel Settings layout must not return.");
    }

    [Test]
    public void SettingsPopupPrefabs_AuthorReadableNonOverlappingLayout()
    {
        AssertReadablePopupLayout(SharedSettingsPopupPath);
    }

    [Test]
    public void SettingsPopupPrefabs_FrameImagesUseSlicedPpuTwo()
    {
        AssertFrameImagesUseSlicedPpuTwo(SharedSettingsPopupPath);
    }

    [Test]
    public void SettingsButtons_EnqueueOpenSettingsAction()
    {
        AssertSettingsButtonUsesOpenSettingsAction(MainMenuContentPath);
        AssertSettingsButtonUsesOpenSettingsAction(MatchHudContentPath);
        AssertPauseMenuButtonAction("ResumeButton", UiActionKind.ClosePause);
        AssertPauseMenuButtonAction("SettingsButton", UiActionKind.OpenSettings);
        AssertPauseMenuButtonAction("ExitButton", UiActionKind.MatchMenu);
    }

    [Test]
    public void UiActionRequestSystem_OpenSettingsProducesSettingsPopupRequest()
    {
        using World world = new("SettingsPopupValidation");
        EntityManager entityManager = world.EntityManager;
        Entity boundary = entityManager.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent),
            typeof(UiDiagnosticsOverlayComponent),
            typeof(UiMatchHudPassengerDrawerStateComponent),
            typeof(UiMatchHudSquadTrayStateComponent),
            typeof(UiBuildDrawerStateComponent));
        entityManager.AddBuffer<UiActionRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellPopupRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellRouteHistoryComponent>(boundary);
        entityManager.AddBuffer<UiShellPresentationCommandComponent>(boundary);
        entityManager.AddBuffer<UiShellTransitionCompleteComponent>(boundary);
        entityManager.AddComponentData(boundary, new UiShellLoadingProgressComponent());
        entityManager.AddComponentData(boundary, new MatchIntroTransitionComponent());
        entityManager.AddComponentData(boundary, new UiShellActivePopupComponent());

        Entity selectionInput = entityManager.CreateEntity(
            typeof(RtsSelectionInputStateComponent),
            typeof(RtsSelectionInputRequestQueueComponent));
        entityManager.AddBuffer<RtsSelectionPointerRequestElement>(selectionInput);
        entityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(selectionInput);
        entityManager.AddBuffer<RtsSelectionCommandResultElement>(selectionInput);

        DynamicBuffer<UiActionRequestComponent> actionRequests =
            entityManager.GetBuffer<UiActionRequestComponent>(boundary);
        actionRequests.Add(new UiActionRequestComponent
        {
            Kind = UiActionKind.OpenSettings,
            PayloadId = 77
        });

        SystemHandle system = world.CreateSystem<UiActionRequestSystem>();
        system.Update(world.Unmanaged);

        actionRequests = entityManager.GetBuffer<UiActionRequestComponent>(boundary);
        DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
            entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);

        Assert.AreEqual(0, actionRequests.Length, "OpenSettings action should be consumed by UiActionRequestSystem.");
        Assert.AreEqual(0, routeRequests.Length, "OpenSettings must not enqueue a full-screen settings route.");
        Assert.AreEqual(1, popupRequests.Length, "OpenSettings must enqueue exactly one popup request.");
        Assert.AreEqual(UiShellPopupKind.Settings, popupRequests[0].PopupKind);
        Assert.AreEqual(UiShellPopupIntent.Show, popupRequests[0].Intent);
        Assert.AreEqual(77, popupRequests[0].PayloadId);

        popupRequests.Clear();
        actionRequests.Add(new UiActionRequestComponent
        {
            Kind = UiActionKind.ClosePause,
            PayloadId = 88
        });
        system.Update(world.Unmanaged);

        Assert.AreEqual(0, actionRequests.Length, "ClosePause action should be consumed by UiActionRequestSystem.");
        Assert.AreEqual(1, popupRequests.Length, "ClosePause must enqueue exactly one popup request.");
        Assert.AreEqual(UiShellPopupKind.Pause, popupRequests[0].PopupKind);
        Assert.AreEqual(UiShellPopupIntent.Hide, popupRequests[0].Intent);
        Assert.AreEqual(88, popupRequests[0].PayloadId);

        popupRequests.Clear();
        actionRequests.Add(new UiActionRequestComponent
        {
            Kind = UiActionKind.MatchMenu,
            PayloadId = 99
        });
        system.Update(world.Unmanaged);

        Assert.AreEqual(0, actionRequests.Length, "MatchMenu action should be consumed by UiActionRequestSystem.");
        Assert.AreEqual(1, popupRequests.Length, "MatchMenu must close the pause popup before changing routes.");
        Assert.AreEqual(UiShellPopupKind.Pause, popupRequests[0].PopupKind);
        Assert.AreEqual(UiShellPopupIntent.Hide, popupRequests[0].Intent);
        Assert.AreEqual(99, popupRequests[0].PayloadId);
        Assert.AreEqual(1, routeRequests.Length, "MatchMenu must retain exactly one main-menu route request.");
        Assert.AreEqual(UIRoute.MainMenu, routeRequests[0].Route);
        Assert.AreEqual(UiShellRouteIntent.ReturnToMainMenu, routeRequests[0].Intent);
        Assert.AreEqual(0, routeRequests[0].PushHistory);

        entityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.MatchHud,
            ActiveRoute = UIRoute.Match,
            Phase = UiShellTransitionPhase.PopupVisible,
            TransitionSequenceId = 20,
            IsTransitionRunning = 0
        });
        entityManager.SetComponentData(boundary, new UiShellActivePopupComponent
        {
            PopupKind = UiShellPopupKind.Pause,
            Visible = 1
        });

        SystemHandle flow = world.CreateSystem<UiShellFlowSystem>();
        flow.Update(world.Unmanaged);

        UiShellStateComponent shell = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        UiShellActivePopupComponent activePopup = entityManager.GetComponentData<UiShellActivePopupComponent>(boundary);
        DynamicBuffer<UiShellPresentationCommandComponent> commands =
            entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
        Assert.AreEqual(21, shell.TransitionSequenceId);
        Assert.AreEqual(1, shell.IsTransitionRunning);
        Assert.AreEqual(UiShellTransitionPhase.HidingPopup, shell.Phase);
        Assert.AreEqual(0, activePopup.Visible);
        Assert.AreEqual(1, commands.Length);
        Assert.AreEqual(UiShellCommandKind.HidePopup, commands[0].Kind);
        Assert.AreEqual(1, routeRequests.Length, "The route must remain queued until pause-hide completion.");

        entityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary).Add(
            new UiShellTransitionCompleteComponent
            {
                Kind = UiShellCommandKind.HidePopup,
                Region = UiShellRegionId.PopupLayer,
                SequenceId = shell.TransitionSequenceId
            });
        flow.Update(world.Unmanaged);

        shell = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        commands = entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
        Assert.AreEqual(22, shell.TransitionSequenceId);
        Assert.AreEqual(1, shell.IsTransitionRunning);
        Assert.AreEqual(UiShellMode.Loading, shell.CurrentMode);
        Assert.AreEqual(UIRoute.MainMenu, shell.ActiveRoute);
        Assert.AreEqual(UiShellTransitionPhase.ShowingLoading, shell.Phase);
        Assert.AreEqual(1, commands.Length, "Return-to-menu must not couple teardown to Match HUD exit tweens.");
        Assert.AreEqual(UiShellCommandKind.ShowLoading, commands[0].Kind);
        Assert.AreEqual(0, routeRequests.Length);
    }

    [Test]
    public void MenuSceneShell_InstallsMenuAndMatchSettingsPopups()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");
        Assert.NotNull(content.SettingsPopupPrefab, "Shared settings popup prefab must be assigned on the shell content binder.");
        Assert.AreEqual("SCN_SettingsPopup", content.SettingsPopupPrefab.name);
        Assert.NotNull(content.PauseMenuPopupPrefab, "Pause menu popup prefab must be assigned on the shell content binder.");
        Assert.AreEqual("PauseMenuPopup", content.PauseMenuPopupPrefab.name);

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(
                UiShellCommandKind.EnterMenu,
                UiShellRegionId.None,
                UIRoute.MainMenu,
                UiShellMode.MainMenu,
                1),
            new UiShellPresentationCommandModel(
                UiShellCommandKind.ShowPopup,
                UiShellRegionId.PopupLayer,
                UIRoute.MainMenu,
                UiShellMode.PopupOnly,
                2,
                UiShellPopupKind.Settings)
        });

        GameObject menuPopup = AssertRegionHasChild(content.ShellView, UIShellRegionId.PopupLayer);
        Assert.AreEqual("SCN_SettingsPopup", menuPopup.name);
        AssertSettingsPopupInstance(menuPopup, SettingsPopupContext.Menu);

        content.CloseSettingsPopup();
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.PopupLayer);

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(
                UiShellCommandKind.EnterMatchHud,
                UiShellRegionId.None,
                UIRoute.Match,
                UiShellMode.MatchHud,
                3),
            new UiShellPresentationCommandModel(
                UiShellCommandKind.ShowPopup,
                UiShellRegionId.PopupLayer,
                UIRoute.Match,
                UiShellMode.PopupOnly,
                4,
                UiShellPopupKind.Pause)
        });

        GameObject pausePopup = AssertRegionHasChild(content.ShellView, UIShellRegionId.PopupLayer);
        Assert.AreEqual("PauseMenuPopup", pausePopup.name);
        content.ClosePauseMenuPopup();
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.PopupLayer);

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(
                UiShellCommandKind.ShowPopup,
                UiShellRegionId.PopupLayer,
                UIRoute.Match,
                UiShellMode.PopupOnly,
                5,
                UiShellPopupKind.Settings)
        });

        GameObject matchPopup = AssertRegionHasChild(content.ShellView, UIShellRegionId.PopupLayer);
        Assert.AreEqual("SCN_SettingsPopup", matchPopup.name);
        AssertSettingsPopupInstance(matchPopup, SettingsPopupContext.Match);
    }

    private static void AssertPauseMenuButtonAction(string buttonName, UiActionKind expectedAction)
    {
        GameObject prefab = LoadPrefab(PauseMenuPopupPath);
        Transform buttonTransform = FindChild(prefab.transform, buttonName);
        Assert.NotNull(buttonTransform, $"Pause menu must include {buttonName}.");
        UIShellActionButtonView actionButton = buttonTransform.GetComponent<UIShellActionButtonView>();
        Assert.NotNull(actionButton, $"Pause menu {buttonName} must use UIShellActionButtonView.");
        Assert.AreEqual(expectedAction, actionButton.ActionKind);
        Assert.AreEqual(0, actionButton.PayloadId);
    }

    private static void AssertLegacyDuplicatePopupRemoved(string prefabPath)
    {
        Assert.IsNull(
            AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),
            $"Duplicate settings popup should be removed: {prefabPath}");
    }

    private static void AssertPrefabUsesOnlyApprovedSprites(string prefabPath)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        Image[] images = prefab.GetComponentsInChildren<Image>(true);
        Assert.Greater(images.Length, 0, $"{prefabPath} must contain Image components for the command UI frame.");

        for (int i = 0; i < images.Length; i++)
        {
            Sprite sprite = images[i].sprite;
            if (sprite == null)
                continue;

            string spritePath = AssetDatabase.GetAssetPath(sprite);
            bool approved = spritePath.StartsWith(ApprovedSharedSpriteFolder + "/", StringComparison.Ordinal) ||
                            string.Equals(spritePath, ApprovedSettingsIcon, StringComparison.Ordinal);
            Assert.IsTrue(approved, $"{prefabPath} image '{images[i].name}' uses non-canonical art: {spritePath}");
        }
    }

    private static void AssertSettingsPopupPrefab(string prefabPath)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        SettingsPopupView popupView = prefab.GetComponent<SettingsPopupView>();
        RectTransform settingsRoot = FindChild(prefab.transform, "SettingsRoot") as RectTransform;
        AssertSettingsPopupInstance(prefab, SettingsPopupContext.Menu);
        Assert.NotNull(settingsRoot, $"{prefabPath} must include a SettingsRoot panel.");
        Assert.GreaterOrEqual(settingsRoot.sizeDelta.x, 1500f, $"{prefabPath} SettingsRoot must be large enough for the command UI scale.");
        Assert.GreaterOrEqual(settingsRoot.sizeDelta.y, 900f, $"{prefabPath} SettingsRoot must be tall enough for readable command UI controls.");
        Assert.GreaterOrEqual(settingsRoot.sizeDelta.x * settingsRoot.localScale.x, 3150f, $"{prefabPath} SettingsRoot effective width must match the 4800x2160 menu canvas scale.");
        Assert.GreaterOrEqual(settingsRoot.sizeDelta.y * settingsRoot.localScale.y, 1900f, $"{prefabPath} SettingsRoot effective height must match the 4800x2160 menu canvas scale.");
        Assert.NotNull(popupView.CloseButton, $"{prefabPath} must serialize a close button.");
        Assert.NotNull(popupView.ResetButton, $"{prefabPath} must serialize a reset button.");
        Assert.NotNull(popupView.ApplyButton, $"{prefabPath} must serialize an apply button.");
        AssertButtonHasInteractiveGraphic(popupView.CloseButton, $"{prefabPath} close button must have a usable graphic hit target.");
        AssertButtonHasInteractiveGraphic(popupView.ResetButton, $"{prefabPath} reset button must have a usable graphic hit target.");
        AssertButtonHasInteractiveGraphic(popupView.ApplyButton, $"{prefabPath} apply button must have a usable graphic hit target.");

        SettingsPanelView panel = popupView.SettingsPanel;
        Assert.GreaterOrEqual(panel.GetComponentsInChildren<UISliderRowView>(true).Length, 4, $"{prefabPath} must expose audio and camera sliders.");
        Assert.GreaterOrEqual(panel.GetComponentsInChildren<UIToggleRowView>(true).Length, 8, $"{prefabPath} must expose audio, notification, accessibility, and ARIA toggles.");
        Assert.GreaterOrEqual(panel.GetComponentsInChildren<UISegmentedControlView>(true).Length, 6, $"{prefabPath} must expose graphics, framerate, ARIA, color, and language segments.");
        Assert.NotNull(FindChild(prefab.transform, "MusicEnabledRow"), $"{prefabPath} must expose a music enable toggle row.");
        Assert.NotNull(FindChild(prefab.transform, "SoundEnabledRow"), $"{prefabPath} must expose a sound enable toggle row.");
        Assert.NotNull(FindChild(prefab.transform, "VoiceEnabledRow"), $"{prefabPath} must expose an ARIA voice enable toggle row.");
        AssertSerializedReference(panel, "musicEnabledRow", $"{prefabPath} must serialize the music enable row.");
        AssertSerializedReference(panel, "soundEnabledRow", $"{prefabPath} must serialize the sound enable row.");
        AssertSerializedReference(panel, "voiceEnabledRow", $"{prefabPath} must serialize the ARIA voice enable row.");
        Assert.NotNull(FindChild(prefab.transform, "NarrationModeControl"), $"{prefabPath} must expose ARIA narration mode control.");
        Assert.NotNull(FindChild(prefab.transform, "AssistantTakeoverRow"), $"{prefabPath} must expose ARIA takeover toggle.");
        Assert.NotNull(FindChild(prefab.transform, "AssistantSubtitlesRow"), $"{prefabPath} must expose ARIA subtitle toggle.");
    }

    private static void AssertSettingsPopupInstance(GameObject popup, SettingsPopupContext expectedContext)
    {
        SettingsPopupView popupView = popup.GetComponent<SettingsPopupView>();
        Assert.NotNull(popupView, $"{popup.name} must own SettingsPopupView.");
        Assert.AreEqual(expectedContext, popupView.Context, $"{popup.name} must declare the expected settings context.");
        Assert.NotNull(popupView.SettingsPanel, $"{popup.name} must serialize its SettingsPanelView.");
    }

    private static void AssertSerializedReference(UnityEngine.Object target, string fieldName, string message)
    {
        SerializedObject serialized = new(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        Assert.NotNull(property, $"{target.name} must expose serialized field {fieldName}.");
        Assert.NotNull(property.objectReferenceValue, message);
    }

    private static void AssertReadablePopupLayout(string prefabPath)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        TMP_Text[] textComponents = prefab.GetComponentsInChildren<TMP_Text>(true);
        Assert.Greater(textComponents.Length, 0, $"{prefabPath} must contain authored TMP labels.");
        for (int i = 0; i < textComponents.Length; i++)
        {
            string text = textComponents[i].text;
            Assert.IsFalse(string.Equals(text, "SETTING", StringComparison.Ordinal), $"{prefabPath} contains placeholder label on {textComponents[i].name}.");
            Assert.IsFalse(text.Contains("ARIA", StringComparison.OrdinalIgnoreCase), $"{prefabPath} contains the old assistant typo on {textComponents[i].name}: {text}");
            Assert.IsFalse(text.Contains("DQUALITY", StringComparison.OrdinalIgnoreCase), $"{prefabPath} contains overlapped graphics label text: {text}");
            Assert.IsFalse(text.Contains("P%", StringComparison.Ordinal), $"{prefabPath} contains overlapped frame-rate/value text: {text}");
        }

        AssertTextPairDoesNotOverlap(prefab.transform, "MasterVolumeRow", "Label", "Value", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "MusicVolumeRow", "Label", "Value", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "SfxVolumeRow", "Label", "Value", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "CameraSensitivityRow", "Label", "Value", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "MusicEnabledRow", "Label", "State", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "SoundEnabledRow", "Label", "State", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "VoiceEnabledRow", "Label", "State", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "ThreatWarningsRow", "Label", "State", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "AssistantTakeoverRow", "Label", "State", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "HighContrastRow", "Label", "State", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "LargeTextRow", "Label", "State", prefabPath);
        AssertTextPairDoesNotOverlap(prefab.transform, "AssistantSubtitlesRow", "Label", "State", prefabPath);
        AssertSegmentLabelsFit(prefab.transform, "GraphicsQualityControl", prefabPath);
        AssertSegmentLabelsFit(prefab.transform, "FrameRateControl", prefabPath);
        AssertSegmentLabelsFit(prefab.transform, "AssistanceLevelControl", prefabPath);
        AssertSegmentLabelsFit(prefab.transform, "NarrationModeControl", prefabPath);
        AssertSegmentLabelsFit(prefab.transform, "ColorblindModeControl", prefabPath);
        AssertSegmentLabelsFit(prefab.transform, "LanguageControl", prefabPath);
    }

    private static void AssertFrameImagesUseSlicedPpuTwo(string prefabPath)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        Image[] images = prefab.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image.sprite == null)
                continue;

            string spriteName = image.sprite.name;
            if (spriteName.Contains("icon", StringComparison.OrdinalIgnoreCase) ||
                spriteName.Contains("gear", StringComparison.OrdinalIgnoreCase))
                continue;

            Assert.AreEqual(Image.Type.Sliced, image.type, $"{prefabPath} image {image.name} using {spriteName} must be Sliced.");
            Assert.IsTrue(image.fillCenter, $"{prefabPath} image {image.name} using {spriteName} must fill center.");
            Assert.AreEqual(2f, image.pixelsPerUnitMultiplier, 0.001f, $"{prefabPath} image {image.name} using {spriteName} must use PPU multiplier 2.");
        }
    }

    private static void AssertTextPairDoesNotOverlap(Transform root, string rowName, string firstName, string secondName, string prefabPath)
    {
        Transform row = FindChild(root, rowName);
        Assert.NotNull(row, $"{prefabPath} missing row {rowName}.");
        TMP_Text first = FindDirectChildText(row, firstName);
        TMP_Text second = FindDirectChildText(row, secondName);
        Assert.NotNull(first, $"{prefabPath} missing {rowName}/{firstName}.");
        Assert.NotNull(second, $"{prefabPath} missing {rowName}/{secondName}.");
        Rect firstRect = GetLocalRect(first.rectTransform, row as RectTransform);
        Rect secondRect = GetLocalRect(second.rectTransform, row as RectTransform);
        Assert.IsFalse(firstRect.Overlaps(secondRect), $"{prefabPath} has overlapping text in {rowName}: {firstName} and {secondName}.");
    }

    private static void AssertSegmentLabelsFit(Transform root, string rowName, string prefabPath)
    {
        Transform row = FindChild(root, rowName);
        Assert.NotNull(row, $"{prefabPath} missing row {rowName}.");
        Transform segments = FindChild(row, "Segments");
        Assert.NotNull(segments, $"{prefabPath} missing {rowName}/Segments.");
        for (int i = 0; i < segments.childCount; i++)
        {
            RectTransform buttonRect = segments.GetChild(i) as RectTransform;
            TMP_Text label = segments.GetChild(i).GetComponentInChildren<TMP_Text>(true);
            Assert.NotNull(label, $"{prefabPath} missing segment label in {rowName}/{segments.GetChild(i).name}.");
            Assert.GreaterOrEqual(buttonRect.rect.width, 92f, $"{prefabPath} segment button too narrow in {rowName}.");
            Assert.LessOrEqual(label.fontSizeMax, 14f, $"{prefabPath} segment label font too large for compact button in {rowName}.");
        }
    }

    private static TMP_Text FindDirectChildText(Transform row, string name)
    {
        Transform child = row.Find(name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Rect GetLocalRect(RectTransform rectTransform, RectTransform relativeTo)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        for (int i = 0; i < corners.Length; i++)
            corners[i] = relativeTo.InverseTransformPoint(corners[i]);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private static void AssertSettingsButtonUsesOpenSettingsAction(string prefabPath)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        Transform settingsButtonTransform = FindChild(prefab.transform, "SettingsButton");
        Assert.NotNull(settingsButtonTransform, $"{prefabPath} must include a SettingsButton.");

        UIShellActionButtonView actionButton = settingsButtonTransform.GetComponent<UIShellActionButtonView>();
        Assert.NotNull(actionButton, $"{prefabPath} SettingsButton must use UIShellActionButtonView.");
        Assert.AreEqual(UiActionKind.OpenSettings, actionButton.ActionKind, $"{prefabPath} SettingsButton must enqueue OpenSettings.");
        Assert.AreEqual(0, actionButton.PayloadId, $"{prefabPath} SettingsButton should not require a payload.");
        Assert.IsNull(
            settingsButtonTransform.GetComponent<UIShellRouteButtonView>(),
            $"{prefabPath} SettingsButton must not route to the full-screen Settings route.");
    }

    private static void AssertButtonHasInteractiveGraphic(Button button, string message)
    {
        Assert.NotNull(button, message);
        Assert.NotNull(button.targetGraphic, message);
        Assert.IsTrue(button.targetGraphic.raycastTarget, message);
        RectTransform rectTransform = button.transform as RectTransform;
        Assert.NotNull(rectTransform, message);
        Assert.Greater(rectTransform.rect.width, 1f, message);
        Assert.Greater(rectTransform.rect.height, 1f, message);
    }

    private static GameObject AssertRegionHasChild(UIShellView shell, UIShellRegionId regionId)
    {
        Assert.IsTrue(shell.TryGetRegion(regionId, out UIShellRegionView region), $"{regionId} must be registered.");
        Assert.NotNull(region.ContentRoot, $"{regionId} must have a content root.");
        Assert.Greater(region.ContentRoot.childCount, 0, $"{regionId} should contain installed content.");
        return region.ContentRoot.GetChild(0).gameObject;
    }

    private static void AssertRegionIsEmpty(UIShellView shell, UIShellRegionId regionId)
    {
        Assert.IsTrue(shell.TryGetRegion(regionId, out UIShellRegionView region), $"{regionId} must be registered.");
        Assert.NotNull(region.ContentRoot, $"{regionId} must have a content root.");
        Assert.AreEqual(0, region.ContentRoot.childCount, $"{regionId} should be empty after closing settings.");
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.NotNull(prefab, $"Prefab must exist: {path}");
        return prefab;
    }

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

    private static T FindInScene<T>(Scene scene) where T : UnityEngine.Object
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static void RunValidationStep(
        string name,
        Action<SettingsPopupValidationTests> step,
        ref int passed)
    {
        var test = new SettingsPopupValidationTests();
        try
        {
            step(test);
            passed++;
            Debug.Log($"[SettingsPopupValidation] passed={name}");
        }
        finally
        {
            test.TearDown();
        }
    }
}
