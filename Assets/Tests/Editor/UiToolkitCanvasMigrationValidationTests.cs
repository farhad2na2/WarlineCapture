#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class UiToolkitCanvasMigrationValidationTests
{
    private const string UiToolkitRoot = "Assets/Game/UI Toolkit";
    private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
    private const string ShellUxmlPath = "Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml";
    private const string ShellUssPath = "Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uss";
    private const string LoadingUxmlPath = "Assets/Game/UI Toolkit/SCN01_LoadingContent/SCN01_LoadingContent.uxml";
    private const string LoadingUssPath = "Assets/Game/UI Toolkit/SCN01_LoadingContent/SCN01_LoadingContent.uss";
    private const string MainMenuUxmlPath = "Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uxml";
    private const string MainMenuUssPath = "Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uss";
    private const string MatchHudUxmlPath = "Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uxml";
    private const string MatchHudUssPath = "Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uss";
    private const string MatchHudPassengerItemUxmlPath = "Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_PassengerItemView.uxml";
    private const string BuildPlacementConfirmationBarUxmlPath = "Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/SCN08_BuildPlacementConfirmationBar.uxml";
    private const string BuildPlacementConfirmationBarUssPath = "Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/SCN08_BuildPlacementConfirmationBar.uss";
    private const string BuildDrawerUxmlPath = "Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildDrawerPopup.uxml";
    private const string BuildDrawerCatalogItemUxmlPath = "Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildCatalogItemView.uxml";
    private const string BuildDrawerProductionQueueItemUxmlPath = "Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_ProductionQueueItemView.uxml";
    private const string BuildDrawerProductionActiveItemUxmlPath = "Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_ProductionActiveItemView.uxml";
    private const string ArmoryUxmlPath = "Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uxml";
    private const string ArmoryUssPath = "Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uss";
    private const string ArmoryItemUxmlPath = "Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryItemView.uxml";
    private const string CommanderProfileUxmlPath = "Assets/Game/UI Toolkit/SCN03_CommanderProfileContent/SCN03_CommanderProfileContent.uxml";
    private const string CommanderProfileUssPath = "Assets/Game/UI Toolkit/SCN03_CommanderProfileContent/SCN03_CommanderProfileContent.uss";
    private const string MissionResultPopupUxmlPath = "Assets/Game/UI Toolkit/POP05_MissionResultPopup/POP05_MissionResultPopup.uxml";
    private const string MissionResultPopupUssPath = "Assets/Game/UI Toolkit/POP05_MissionResultPopup/POP05_MissionResultPopup.uss";
    private const string SettingsPopupUxmlPath = "Assets/Game/UI Toolkit/POP06_SettingsPopup/POP06_SettingsPopup.uxml";
    private const string SettingsPopupUssPath = "Assets/Game/UI Toolkit/POP06_SettingsPopup/POP06_SettingsPopup.uss";
    private const string InboxPopupUxmlPath = "Assets/Game/UI Toolkit/POP07_InboxPopup/POP07_InboxPopup.uxml";
    private const string InboxPopupUssPath = "Assets/Game/UI Toolkit/POP07_InboxPopup/POP07_InboxPopup.uss";
    private const string UiToolkitAsmdefPath = "Assets/Game/Scripts/UI/Toolkit/Game.UI.Toolkit.asmdef";
    private const string ShellApplySystemPath = "Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs";
    private const string MenuBootstrapSystemPath = "Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs";
    private const string MenuBootstrapViewPath = "Assets/Game/Scripts/Composition/MenuBootstrapView.cs";
    private const string UiShellFlowSystemPath = "Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs";
    private const string UiActionRequestSystemPath = "Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs";

    private static readonly Regex UrlRegex = new(
        @"url\(\s*[""']?(?<path>[^""')]+)[""']?\s*\)",
        RegexOptions.CultureInvariant);

    private static readonly string[] OldArtMarkers =
    {
        "Generated/MainMenu/LayeredOneGo",
        "Generated/MainMenuAlt",
        "Art/UI/Final",
        "VisualLockLayered/SCN-02_MainMenu",
        "TargetLockV01",
        "LegacyVisualLock",
        "Generated/MatchHUD/TargetLockV01"
    };

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new UiToolkitCanvasMigrationValidationTests();
            tests.UiToolkitUxmlFilesImport();
            tests.UiToolkitUssFilesImport();
            tests.UiToolkitUssUrlReferencesResolve();
            tests.UiToolkitFilesDoNotReferenceOldArtDirection();
            tests.UiToolkitRuntimeCodeDoesNotDependOnCanvasRuntimeTypes();
            tests.CanvasFallbackReferencesStayInApprovedLegacySurface();
            tests.MigratedUiToolkitRuntimeDoesNotReferenceLegacyCanvasViews();
            tests.RuntimeUiConfigExistsAndDefaultsToUiToolkit();
            tests.MenuBootstrapViewKeepsCanvasFallbackEnabledInCanvasMode();
            tests.MenuBootstrapViewCanEnableIsolatedUiToolkitShellMode();
            tests.MenuBootstrapViewDisablesCanvasPresentationStackInUiToolkitMode();
            tests.UiToolkitShellViewMountsShellUxmlThroughUidocument();
            tests.UiToolkitShellViewBindsRequiredShellRegionsByName();
            tests.UiToolkitShellViewBindsRequiredScreenSlotsByName();
            tests.UiToolkitShellUsesFluidFullscreenScaffold();
            tests.UiToolkitShellAspectSmokeCoversSixteenNineAndTwentyNine();
            tests.UiToolkitShellMotionStyleClassesExist();
            tests.UiToolkitShellViewAppliesMotionStateClasses();
            tests.UiToolkitShellLayersRenderAboveNormalContent();
            tests.UiToolkitShellPointerGateBlocksOnlyConcreteUiAndVisibleOverlays();
            tests.UiToolkitShellApplySystemIsManagedPresentationEdge();
            tests.UiShellRuntimeUsesOnlyIntentionalManagedSystemBaseEdge();
            tests.UiToolkitRuntimeClassNamesFollowArchitectureContract();
            tests.UiToolkitRuntimeAssembliesStayLayered();
            tests.UiToolkitHotPathsAvoidKnownAllocationPatterns();
            tests.UiToolkitManagedApplyEdgeIsClassifiedAndDocumented();
            tests.MenuBootstrapViewWiresUiToolkitShellReferencePath();
            tests.EcsSystemsDoNotTouchUiToolkitObjectsDirectly();
            tests.UiToolkitViewsDoNotOwnFramePolling();
            tests.LoadingUxmlExposesCanvasParityBindings();
            tests.UiToolkitShellViewMountsLoadingUxmlIntoLoadingSlot();
            tests.UiToolkitShellViewAppliesLoadingProgressReadModel();
            tests.UiToolkitShellViewAppliesLoadingPresentationCommands();
            tests.MenuBootstrapSystemKeepsLoadingProgressActiveInUiToolkitMode();
            tests.UiToolkitLoadingMountDoesNotShowInitialLoadingOrBlockByDefault();
            tests.UiToolkitLoadingLayerStaysAboveVisiblePopupWhenShown();
            tests.UiToolkitLoadingLayerOverridesMountedPopupDuringTransition();
            tests.UiToolkitLoadingTextBindingsStayVisibleWhenShown();
            tests.UiToolkitLoadingProgressCompletionCanExitLoadingLayer();
            tests.MainMenuUxmlExposesCanvasParityBindings();
            tests.MainMenuHeaderIconsUseCenteredSafeRects();
            tests.MainMenuSettingsAndMailHitAreasMatchVisibleFrames();
            tests.MainMenuModeCardsKeepTextAndPortraitsInsideSafePadding();
            tests.MainMenuCommanderProfileUsesReadModel();
            tests.MainMenuResourceValuesUseReadModel();
            tests.UiToolkitShellViewMountsMainMenuUxmlIntoMainMenuSlot();
            tests.UiToolkitShellViewAppliesMainMenuPresentationCommands();
            tests.UiToolkitMainMenuActionsEnqueueShellRouteRequests();
            tests.UiToolkitShellApplySystemAppliesMainMenuSelectedStateFromShellReadModel();
            tests.MatchHudUxmlExposesCanvasParityBindings();
            tests.UiToolkitShellViewMountsMatchHudUxmlIntoMatchSlot();
            tests.UiToolkitMatchHudActionsEnqueueUiActionRequests();
            tests.UiActionRequestSystemProcessesMatchHudActionRequests();
            tests.UiToolkitShellApplySystemAppliesMatchHudSelectionReadModel();
            tests.UiToolkitShellApplySystemAppliesMatchHudCommandStateReadModel();
            tests.UiToolkitShellApplySystemAppliesMatchHudHeaderReadModel();
            tests.UiToolkitShellApplySystemAppliesMatchHudStatusSurfacesReadModel();
            tests.UiToolkitShellApplySystemAppliesMatchHudMinimapReadModel();
            tests.UiToolkitShellApplySystemAppliesMatchHudPassengerDrawerReadModel();
            tests.UiToolkitShellApplySystemAppliesMatchHudSquadTrayReadModel();
            tests.BuildDrawerUxmlExposesCanvasParityBindings();
            tests.UiToolkitShellViewMountsBuildDrawerUxmlIntoPopupSlot();
            tests.UiToolkitShellViewRefreshesBuildDrawerRetainedTemplates();
            tests.UiToolkitBuildDrawerCloseActionOnlyHidesBuildDrawerPopup();
            tests.UiToolkitBuildDrawerCatalogActionsEnqueueEcsBuildRequests();
            tests.UiToolkitBuildDrawerProductionActionsEnqueueEcsProductionRequests();
            tests.UiToolkitBuildDrawerPopupCommandsPreserveSelectedBuildState();
            tests.UiToolkitBuildDrawerPrimaryBuildEnqueuesEcsBuildRequest();
            tests.UiToolkitShellApplySystemAppliesBuildDrawerReadModel();
            tests.BuildPlacementConfirmationBarUxmlExposesCanvasParityBindings();
            tests.UiToolkitShellViewMountsBuildPlacementConfirmationBarIntoMatchSlot();
            tests.UiToolkitShellApplySystemAppliesBuildPlacementConfirmationBarReadModelAndActions();
            tests.UiBuildPlacementReadModelSystemMirrorsActivePlacementState();
            tests.ArmoryUxmlExposesCanvasParityBindingsAndRetainedItemTemplate();
            tests.UiToolkitShellViewMountsArmoryUxmlIntoArmorySlot();
            tests.UiToolkitShellViewBindsArmoryRetainedItemsAndActions();
            tests.UiToolkitShellApplySystemAppliesArmoryCategoryReadModel();
            tests.ArmoryItemTemplateSupportsFullRetainedStateBindings();
            tests.UiToolkitArmoryScrollUsesRetainedRowsWithoutPerOperationReplacement();
            tests.UiToolkitArmoryItemSelectionSwapsGeneratedFrameStateClasses();
            tests.UiToolkitArmoryLockedRowsKeepLockedStateAndRejectUnavailableActions();
            tests.CommanderProfileUxmlExposesCanvasParityBindingsAndNewArtAssets();
            tests.CommanderProfileLayoutKeepsRegionsAndTextInsideSafeRects();
            tests.UiToolkitShellViewMountsCommanderProfileUxmlIntoCommanderProfileSlot();
            tests.MissionResultPopupUxmlExposesCanvasParityBindingsAndNewArtAssets();
            tests.UiToolkitShellViewMountsMissionResultPopupIntoPopupSlot();
            tests.UiToolkitShellApplySystemAppliesMissionResultReadModelVariants();
            tests.MissionResultUsesSingleUiToolkitPopupForVictoryAndLossVariants();
            tests.UiToolkitShellDiagnosticsOverlayExposesFpsAndRuntimeLogActions();
            tests.SettingsPopupUxmlExposesShellBindingsAndRoutesThroughGateway();
            tests.InboxPopupUxmlExposesShellBindingsAndRoutesThroughGateway();
            UnityEngine.Debug.Log("[UiToolkitCanvasMigrationValidation] result=Passed tests=91");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[UiToolkitCanvasMigrationValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void UiToolkitUxmlFilesImport()
    {
        foreach (string path in EnumerateAssets("*.uxml"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.IsNotNull(asset, $"UI Toolkit UXML must import as VisualTreeAsset: {path}");
        }
    }

    [Test]
    public void UiToolkitUssFilesImport()
    {
        foreach (string path in EnumerateAssets("*.uss"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            Assert.IsNotNull(asset, $"UI Toolkit USS must import as StyleSheet: {path}");
        }
    }

    [Test]
    public void UiToolkitUssUrlReferencesResolve()
    {
        var missing = new List<string>();

        foreach (string ussPath in EnumerateAssets("*.uss"))
        {
            string source = File.ReadAllText(ussPath);
            foreach (Match match in UrlRegex.Matches(source))
            {
                string value = match.Groups["path"].Value.Trim();
                if (ShouldSkipUrl(value))
                    continue;

                string assetPath = ResolveAssetPath(ussPath, value);
                if (string.IsNullOrWhiteSpace(assetPath) || !File.Exists(assetPath))
                    missing.Add($"{ussPath} -> {value}");
            }
        }

        AssertNoViolations(missing, "Every UI Toolkit USS url(...) reference must resolve to an existing asset.");
    }

    [Test]
    public void UiToolkitFilesDoNotReferenceOldArtDirection()
    {
        var violations = new List<string>();

        foreach (string path in EnumerateAssets("*.uxml", "*.uss"))
        {
            string source = File.ReadAllText(path);
            for (int i = 0; i < OldArtMarkers.Length; i++)
            {
                string marker = OldArtMarkers[i];
                if (source.Contains(marker, StringComparison.Ordinal))
                    violations.Add($"{path} -> {marker}");
            }
        }

        AssertNoViolations(violations, "UI Toolkit migration files must not reference old-art-direction assets.");
    }

    [Test]
    public void UiToolkitRuntimeCodeDoesNotDependOnCanvasRuntimeTypes()
    {
        var violations = new List<string>();
        string[] roots =
        {
            "Assets/Game/Scripts/UI/Toolkit",
            "Assets/Game/Scripts/UI/Shell/Ecs",
            "Assets/Game/Scripts/UI/Contracts"
        };

        (string Label, Regex Pattern)[] forbidden =
        {
            ("UnityEngine.UI namespace", new Regex(@"using\s+UnityEngine\.UI\s*;", RegexOptions.CultureInvariant)),
            ("TMPro namespace", new Regex(@"using\s+TMPro\s*;", RegexOptions.CultureInvariant)),
            ("Canvas type", new Regex(@"\bCanvas\b", RegexOptions.CultureInvariant)),
            ("CanvasGroup type", new Regex(@"\bCanvasGroup\b", RegexOptions.CultureInvariant)),
            ("RectTransform type", new Regex(@"\bRectTransform\b", RegexOptions.CultureInvariant)),
            ("TextMeshPro type", new Regex(@"\bTextMeshPro\w*\b", RegexOptions.CultureInvariant)),
            ("TMP type", new Regex(@"\bTMP_\w+\b", RegexOptions.CultureInvariant))
        };

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            string root = roots[rootIndex];
            if (!Directory.Exists(root))
                continue;

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string normalizedPath = NormalizeAssetPath(path);
                string source = File.ReadAllText(normalizedPath);

                for (int forbiddenIndex = 0; forbiddenIndex < forbidden.Length; forbiddenIndex++)
                {
                    (string label, Regex pattern) = forbidden[forbiddenIndex];
                    if (pattern.IsMatch(source))
                        violations.Add($"{normalizedPath} -> {label}");
                }
            }
        }

        AssertNoViolations(violations, "UI Toolkit runtime code must not depend on legacy Canvas/TMPro runtime types.");
    }

    [Test]
    public void CanvasFallbackReferencesStayInApprovedLegacySurface()
    {
        var violations = new List<string>();
        (string Label, Regex Pattern)[] legacyUiTokens =
        {
            ("UnityEngine.UI namespace", new Regex(@"using\s+UnityEngine\.UI\s*;", RegexOptions.CultureInvariant)),
            ("TMPro namespace", new Regex(@"using\s+TMPro\s*;", RegexOptions.CultureInvariant)),
            ("Canvas type", new Regex(@"\bCanvas\b", RegexOptions.CultureInvariant)),
            ("CanvasGroup type", new Regex(@"\bCanvasGroup\b", RegexOptions.CultureInvariant)),
            ("RectTransform type", new Regex(@"\bRectTransform\b", RegexOptions.CultureInvariant)),
            ("GraphicRaycaster type", new Regex(@"\bGraphicRaycaster\b", RegexOptions.CultureInvariant)),
            ("ScrollRect type", new Regex(@"\bScrollRect\b", RegexOptions.CultureInvariant)),
            ("TMP type", new Regex(@"\bTMP_\w+\b", RegexOptions.CultureInvariant))
        };

        foreach (string path in Directory.GetFiles("Assets/Game/Scripts", "*.cs", SearchOption.AllDirectories))
        {
            string normalizedPath = NormalizeAssetPath(path);
            if (!normalizedPath.StartsWith("Assets/Game/Scripts/UI/", StringComparison.Ordinal) &&
                !normalizedPath.StartsWith("Assets/Game/Scripts/Composition/", StringComparison.Ordinal))
            {
                continue;
            }

            string source = File.ReadAllText(normalizedPath);
            bool containsLegacyUiToken = false;
            for (int tokenIndex = 0; tokenIndex < legacyUiTokens.Length; tokenIndex++)
            {
                if (!legacyUiTokens[tokenIndex].Pattern.IsMatch(source))
                    continue;

                containsLegacyUiToken = true;
                break;
            }

            if (!containsLegacyUiToken || IsApprovedLegacyCanvasFallbackSource(normalizedPath))
                continue;

            violations.Add(normalizedPath);
        }

        AssertNoViolations(violations, "Legacy Canvas/TMPro references must stay inside the approved Canvas fallback surface or bootstrap boundary.");
    }

    [Test]
    public void MigratedUiToolkitRuntimeDoesNotReferenceLegacyCanvasViews()
    {
        var violations = new List<string>();
        string[] migratedRuntimeRoots =
        {
            "Assets/Game/Scripts/UI/Toolkit",
            "Assets/Game/Scripts/UI/Shell/Ecs",
            "Assets/Game/Scripts/UI/Contracts"
        };
        string[] legacyViewTypes =
        {
            "UIShellLoadingProgressView",
            "UIGameStartButtonView",
            "UIModeCardView",
            "MainMenuNavigationView",
            "MainMenuNavigationTabView",
            "MatchHudSelectionPanelView",
            "MatchHudSquadTrayView",
            "MatchHudTransportPassengerDrawerView",
            "MatchHudTransportPassengerItemView",
            "BattleHudRuntimeFeedbackView",
            "MatchHudMinimapView",
            "MatchHudRightQuickRailView",
            "BuildDrawerView",
            "BuildDrawerCatalogRuntimeView",
            "BuildDrawerItemView",
            "BuildDrawerQueueItemView",
            "BuildPlacementConfirmationBarView",
            "ArmoryCatalogItemView",
            "ArmoryContentListView",
            "ArmoryInspectionPanelView",
            "SettingsScreenView",
            "MenuDiagnosticsView",
            "UIPopupFrameView",
            "UIPopupCloseView",
            "UIModalView"
        };

        for (int rootIndex = 0; rootIndex < migratedRuntimeRoots.Length; rootIndex++)
        {
            string root = migratedRuntimeRoots[rootIndex];
            if (!Directory.Exists(root))
                continue;

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string normalizedPath = NormalizeAssetPath(path);
                string source = File.ReadAllText(normalizedPath);
                for (int typeIndex = 0; typeIndex < legacyViewTypes.Length; typeIndex++)
                {
                    string legacyViewType = legacyViewTypes[typeIndex];
                    if (Regex.IsMatch(source, $@"\b{Regex.Escape(legacyViewType)}\b", RegexOptions.CultureInvariant))
                        violations.Add($"{normalizedPath} -> {legacyViewType}");
                }
            }
        }

        AssertNoViolations(violations, "Migrated UI Toolkit runtime must use ECS gateway/read-model contracts, not direct legacy Canvas view classes.");
    }

    [Test]
    public void RuntimeUiConfigExistsAndDefaultsToUiToolkit()
    {
        RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
        Assert.IsNotNull(config, $"Missing runtime UI config: {RuntimeUiConfigPath}");
        Assert.AreEqual(RuntimeUiMode.UiToolkit, config.Mode, "The default runtime UI config must boot the new UI Toolkit shell.");
        Assert.IsTrue(config.UseUiToolkit, "The default runtime UI config must enable UI Toolkit.");
    }

    [Test]
    public void MenuBootstrapViewKeepsCanvasFallbackEnabledInCanvasMode()
    {
        using var scope = new RuntimeUiModeSmokeScope(RuntimeUiMode.Canvas);

        scope.Canvas.enabled = false;
        scope.UiDocument.enabled = true;
        scope.UiToolkitRoot.SetActive(true);

        scope.BootstrapView.ApplyRuntimeUiMode();

        Assert.IsTrue(scope.Canvas.enabled, "Canvas mode must keep the Canvas fallback enabled.");
        Assert.IsFalse(scope.UiDocument.enabled, "Canvas mode must keep the isolated UI Toolkit document disabled.");
        Assert.IsFalse(scope.UiToolkitRoot.activeSelf, "Canvas mode must keep the isolated UI Toolkit shell root inactive.");
    }

    [Test]
    public void MenuBootstrapViewCanEnableIsolatedUiToolkitShellMode()
    {
        using var scope = new RuntimeUiModeSmokeScope(RuntimeUiMode.UiToolkit);

        scope.Canvas.enabled = true;
        scope.UiDocument.enabled = false;
        scope.UiToolkitRoot.SetActive(false);

        scope.BootstrapView.ApplyRuntimeUiMode();

        Assert.IsFalse(scope.Canvas.enabled, "UI Toolkit mode must disable the Canvas fallback without destroying it.");
        Assert.IsTrue(scope.UiDocument.enabled, "UI Toolkit mode must enable the isolated UI Toolkit document.");
        Assert.IsTrue(scope.UiToolkitRoot.activeSelf, "UI Toolkit mode must enable the isolated UI Toolkit shell root.");
    }

    [Test]
    public void MenuBootstrapViewDisablesCanvasPresentationStackInUiToolkitMode()
    {
        using var scope = new RuntimeUiModeSmokeScope(RuntimeUiMode.UiToolkit);

        scope.Canvas.enabled = true;
        scope.LegacyShellPresentation.enabled = true;
        scope.LegacyContentSystem.enabled = true;
        scope.LegacyRouter.enabled = true;
        scope.UiDocument.enabled = false;
        scope.UiToolkitRoot.SetActive(false);

        scope.BootstrapView.ApplyRuntimeUiMode();

        Assert.IsFalse(scope.Canvas.enabled, "UI Toolkit mode must disable the Canvas renderer path.");
        Assert.IsFalse(scope.LegacyShellPresentation.enabled, "UI Toolkit mode must disable the legacy Canvas ECS presentation component.");
        Assert.IsFalse(scope.LegacyContentSystem.enabled, "UI Toolkit mode must disable the legacy Canvas content component.");
        Assert.IsFalse(scope.LegacyRouter.enabled, "UI Toolkit mode must disable the legacy Canvas router component.");
        Assert.IsTrue(scope.UiDocument.enabled, "UI Toolkit mode must enable the UI Toolkit document.");
        Assert.IsTrue(scope.UiToolkitRoot.activeSelf, "UI Toolkit mode must enable the UI Toolkit shell root.");
    }

    [Test]
    public void UiToolkitShellViewMountsShellUxmlThroughUidocument()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellMountSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "UiToolkitShellView must mount the shell VisualTreeAsset through UIDocument.");
            Assert.AreSame(shellAsset, document.visualTreeAsset, "UIDocument must use the UI Toolkit shell UXML.");
            Assert.IsNotNull(shellView.Root, "Mounted shell root must be cached.");
            Assert.IsNotNull(shellView.Root.Q<VisualElement>("SafeAreaRoot"), "Mounted shell must expose SafeAreaRoot.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewBindsRequiredShellRegionsByName()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellRegionSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed only when required regions are bound.");
            Assert.IsTrue(shellView.HasRequiredRegions, "Shell view must cache every required shell region.");
            Assert.IsNotNull(shellView.SafeAreaRoot, "Missing SafeAreaRoot region.");
            Assert.IsNotNull(shellView.HeaderBar, "Missing HeaderBar region.");
            Assert.IsNotNull(shellView.ContentRoot, "Missing ContentRoot region.");
            Assert.IsNotNull(shellView.FooterBar, "Missing FooterBar region.");
            Assert.IsNotNull(shellView.ModalOverlay, "Missing ModalOverlay region.");
            Assert.IsNotNull(shellView.TooltipLayer, "Missing TooltipLayer region.");
            Assert.IsNotNull(shellView.LoadingLayer, "Missing LoadingLayer region.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasRequiredRegions, "ClearCache must clear required-region state.");
            Assert.IsNull(shellView.Root, "ClearCache must clear Root.");
            Assert.IsNull(shellView.SafeAreaRoot, "ClearCache must clear SafeAreaRoot.");
            Assert.IsNull(shellView.ModalOverlay, "ClearCache must clear ModalOverlay.");
            Assert.IsNull(shellView.LoadingLayer, "ClearCache must clear LoadingLayer.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellLayersRenderAboveNormalContent()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellLayerOrderSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating overlay order.");
            Assert.AreSame(shellView.SafeAreaRoot, shellView.ContentRoot.parent, "ContentRoot must be a top-level safe-area child.");
            Assert.AreSame(shellView.SafeAreaRoot, shellView.ModalOverlay.parent, "ModalOverlay must be a top-level safe-area child.");
            Assert.AreSame(shellView.SafeAreaRoot, shellView.LoadingLayer.parent, "LoadingLayer must be a top-level safe-area child.");

            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.ModalOverlay, shellView.ContentRoot, "Popup overlay must draw above normal content.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.ModalOverlay, shellView.FooterBar, "Popup overlay must draw above footer content.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.ContentRoot, "Loading layer must draw above normal content.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.FooterBar, "Loading layer must draw above footer content.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.ModalOverlay, "Loading layer must draw above popups.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.TooltipLayer, "Loading layer must draw above tooltip overlays.");

            Assert.IsTrue(shellView.ModalOverlay.ClassListContains("shell-hidden"), "ModalOverlay must stay hidden by default.");
            Assert.IsTrue(shellView.LoadingLayer.ClassListContains("shell-hidden"), "LoadingLayer must stay hidden by default.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellPointerGateBlocksOnlyConcreteUiAndVisibleOverlays()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellPointerGateSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating pointer-block propagation.");

            Assert.IsFalse(shellView.IsElementBlockingUi(shellView.ContentRoot, out _), "ContentRoot is structural and must not block the whole screen.");
            Assert.IsFalse(shellView.IsElementBlockingUi(shellView.MatchScreenSlot, out _), "Empty screen slots are structural and must not block the whole screen.");
            Assert.IsFalse(shellView.IsElementBlockingUi(shellView.PopupScreenSlot, out _), "Hidden popup slot must not block input.");
            Assert.IsFalse(shellView.IsElementBlockingUi(shellView.LoadingLayer, out _), "Hidden loading layer must not block input.");

            VisualElement matchContent = new()
            {
                name = "SyntheticMatchContent"
            };
            shellView.MatchScreenSlot.Add(matchContent);

            Assert.IsTrue(shellView.IsElementBlockingUi(matchContent, out string matchSource), "Concrete screen content must block world selection and placement clicks.");
            Assert.AreEqual("SyntheticMatchContent", matchSource, "Pointer-block source should identify the concrete UI element when available.");

            shellView.ModalOverlay.RemoveFromClassList("shell-hidden");
            Assert.IsTrue(shellView.IsElementBlockingUi(shellView.PopupScreenSlot, out string popupSource), "Visible popup layer must block world clicks even when the pointer hits the popup slot.");
            Assert.AreEqual("PopupScreenSlot", popupSource);

            shellView.LoadingLayer.RemoveFromClassList("shell-hidden");
            Assert.IsTrue(shellView.IsElementBlockingUi(shellView.LoadingLayer, out string loadingSource), "Visible loading layer must block all world clicks.");
            Assert.AreEqual("LoadingLayer", loadingSource);

            shellView.HeaderBar.RemoveFromClassList("shell-hidden");
            Assert.IsTrue(shellView.IsElementBlockingUi(shellView.HeaderBar, out string headerSource), "Visible persistent header must block world clicks over the header.");
            Assert.AreEqual("HeaderBar", headerSource);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemIsManagedPresentationEdge()
    {
        Assert.IsTrue(File.Exists(ShellApplySystemPath), $"Missing shell apply system: {ShellApplySystemPath}");
        string source = File.ReadAllText(ShellApplySystemPath);
        string asmdef = File.ReadAllText(UiToolkitAsmdefPath);

        StringAssert.Contains("[UpdateInGroup(typeof(PresentationSystemGroup))]", source);
        StringAssert.Contains("public sealed partial class UiToolkitShellApplySystem : SystemBase", source);
        StringAssert.Contains("protected override void OnUpdate()", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadShellState", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadCommanderProfile", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadMainMenuResources", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadLoadingProgress", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadMatchHudSelection", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadMatchHudCommandState", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadMatchHudHeader", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadMatchHudStatusSurfaces", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadMatchHudMinimap", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadMatchHudPassengerDrawer", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadMatchHudSquadTray", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadBuildDrawer", source);
        StringAssert.Contains("public void ConfigureShellView(UiToolkitShellView view)", source);
        StringAssert.Contains("public void ClearShellView(UiToolkitShellView view = null)", source);
        StringAssert.Contains("public bool HasMountedShellView", source);
        StringAssert.Contains("shellView.ApplyLoadingProgress(lastLoadingProgress)", source);
        StringAssert.Contains("shellView.ApplyMainMenuCommanderProfile(lastCommanderProfile)", source);
        StringAssert.Contains("shellView.ApplyMainMenuResources(lastMainMenuResources)", source);
        StringAssert.Contains("shellView.ApplyMatchHudSelection(lastMatchHudSelection)", source);
        StringAssert.Contains("shellView.ApplyMatchHudCommandState(lastMatchHudCommandState)", source);
        StringAssert.Contains("shellView.ApplyMatchHudHeader(lastMatchHudHeader)", source);
        StringAssert.Contains("shellView.ApplyMatchHudStatusSurfaces(lastMatchHudStatusSurfaces)", source);
        StringAssert.Contains("shellView.ApplyMatchHudMinimap(lastMatchHudMinimap)", source);
        StringAssert.Contains("shellView.ApplyMatchHudPassengerDrawer(lastMatchHudPassengerDrawer)", source);
        StringAssert.Contains("shellView.ApplyMatchHudSquadTray(lastMatchHudSquadTray)", source);
        StringAssert.Contains("shellView.ApplyBuildDrawer(lastBuildDrawer)", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryConsumePresentationCommands(commandScratch)", source);
        StringAssert.Contains("UiShellRuntimeGateway.TryEnqueueTransitionComplete(pendingCompletion)", source);
        StringAssert.Contains("shellView.ApplyPresentationCommands(commandScratch)", source);
        StringAssert.Contains("shellView.EnsureMainMenuVisible(lastShellState.ActiveRoute)", source);
        StringAssert.Contains("\"Unity.Collections\"", asmdef, "The UI Toolkit edge assembly must explicitly reference Unity.Collections when referencing Unity.Entities.");
        StringAssert.Contains("\"Unity.Entities\"", asmdef, "The UI Toolkit edge assembly must explicitly reference Unity.Entities for its managed presentation SystemBase.");
        Assert.IsFalse(source.Contains("VisualElement", StringComparison.Ordinal), "The apply system should not read/write VisualElement directly.");
        Assert.IsFalse(source.Contains("SelectedTag", StringComparison.Ordinal), "The shell apply system must not contain gameplay selection queries.");
        Assert.IsFalse(source.Contains("SelectionState", StringComparison.Ordinal), "The shell apply system must not contain gameplay selection policy.");
        Assert.IsFalse(source.Contains("Path", StringComparison.Ordinal), "The shell apply system must not contain gameplay pathing policy.");
        Assert.IsFalse(source.Contains("BuildMode", StringComparison.Ordinal), "The shell apply system must not contain build gameplay policy.");
    }

    [Test]
    public void UiShellRuntimeUsesOnlyIntentionalManagedSystemBaseEdge()
    {
        var violations = new List<string>();
        string[] roots =
        {
            "Assets/Game/Scripts/UI/Toolkit",
            "Assets/Game/Scripts/UI/Shell/Ecs"
        };

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            string root = roots[rootIndex];
            if (!Directory.Exists(root))
                continue;

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string normalizedPath = NormalizeAssetPath(path);
                string source = File.ReadAllText(normalizedPath);
                if (!source.Contains("SystemBase", StringComparison.Ordinal))
                    continue;

                if (normalizedPath == ShellApplySystemPath)
                    continue;

                violations.Add($"{normalizedPath} -> SystemBase");
            }
        }

        string diagnosticsSource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/UiDiagnosticsReadModelSystem.cs");
        StringAssert.Contains("public partial struct UiDiagnosticsReadModelSystem : ISystem", diagnosticsSource, "Diagnostics FPS/log read model must remain an ISystem.");
        StringAssert.Contains("UiDiagnosticsRuntimeLogBuffer.EnsureSubscribed()", diagnosticsSource, "Managed Unity log subscription must stay isolated behind the diagnostics log buffer helper.");
        AssertNoViolations(violations, "The UI shell runtime must not introduce managed SystemBase systems outside the intentional UI Toolkit apply edge.");
    }

    [Test]
    public void UiToolkitRuntimeClassNamesFollowArchitectureContract()
    {
        var violations = new List<string>();
        string[] roots =
        {
            "Assets/Game/Scripts/UI/Toolkit",
            "Assets/Game/Scripts/UI/Shell/Ecs",
            "Assets/Game/Scripts/UI/Contracts"
        };
        var forbiddenNamePattern = new Regex(@"\b(?:class|struct)\s+\w*(?:Controller|Presenter|Bridge|Manager|Button)\b", RegexOptions.CultureInvariant);

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            string root = roots[rootIndex];
            if (!Directory.Exists(root))
                continue;

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string normalizedPath = NormalizeAssetPath(path);
                string source = File.ReadAllText(normalizedPath);
                Match match = forbiddenNamePattern.Match(source);
                if (match.Success)
                    violations.Add($"{normalizedPath} -> {match.Value}");
            }
        }

        AssertNoViolations(violations, "Migrated UI Toolkit runtime must not add forbidden Controller/Presenter/Bridge/Manager/Button class names.");
    }

    [Test]
    public void UiToolkitRuntimeAssembliesStayLayered()
    {
        var violations = new List<string>();
        (string Root, string ExpectedAsmdef)[] roots =
        {
            ("Assets/Game/Scripts/UI/Contracts", "Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef"),
            ("Assets/Game/Scripts/UI/Shell/Ecs/Contracts", "Assets/Game/Scripts/UI/Shell/Ecs/Contracts/Game.UI.Shell.Contracts.Ecs.asmdef"),
            ("Assets/Game/Scripts/UI/Shell/Ecs", "Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef"),
            ("Assets/Game/Scripts/UI/Toolkit", UiToolkitAsmdefPath)
        };

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            (string root, string expectedAsmdef) = roots[rootIndex];
            if (!Directory.Exists(root))
                continue;

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string normalizedPath = NormalizeAssetPath(path);
                if (root == "Assets/Game/Scripts/UI/Shell/Ecs" &&
                    normalizedPath.StartsWith("Assets/Game/Scripts/UI/Shell/Ecs/Contracts/", StringComparison.Ordinal))
                {
                    continue;
                }

                string nearestAsmdef = FindNearestAsmdef(normalizedPath);
                if (!string.Equals(nearestAsmdef, expectedAsmdef, StringComparison.Ordinal))
                    violations.Add($"{normalizedPath} -> {nearestAsmdef}");
            }
        }

        AssertNoViolations(violations, "Migrated UI Toolkit runtime sources must stay under explicit UI assembly definitions.");

        string uiContractsAsmdef = File.ReadAllText("Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef");
        string shellContractsAsmdef = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/Contracts/Game.UI.Shell.Contracts.Ecs.asmdef");
        string shellEcsAsmdef = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef");
        string toolkitAsmdef = File.ReadAllText(UiToolkitAsmdefPath);

        StringAssert.Contains("\"name\": \"Game.UI.Contracts\"", uiContractsAsmdef);
        StringAssert.Contains("\"references\": []", uiContractsAsmdef, "UI contracts must stay dependency-free.");
        Assert.IsFalse(shellContractsAsmdef.Contains("Game.UI.Toolkit", StringComparison.Ordinal), "ECS shell contracts must not depend on the UI Toolkit edge.");
        Assert.IsFalse(shellEcsAsmdef.Contains("Game.UI.Toolkit", StringComparison.Ordinal), "ECS shell systems must not depend on the UI Toolkit edge.");
        Assert.IsFalse(toolkitAsmdef.Contains("Game.UI.Shell.Ecs", StringComparison.Ordinal), "The UI Toolkit edge must depend on contracts/gateway only, not concrete ECS shell systems.");
    }

    [Test]
    public void UiToolkitHotPathsAvoidKnownAllocationPatterns()
    {
        var violations = new List<string>();
        string[] allocationTokens =
        {
            ".ToList(",
            ".ToArray(",
            "Enumerable.",
            "string.Format",
            "string.Join",
            "new List<",
            "new Dictionary<",
            "GetComponent<",
            "GameObject.Find",
            "FindObject",
            "Resources.Load",
            "CloneTree(",
            ".Q<",
            ".Query<"
        };

        foreach (string path in Directory.GetFiles("Assets/Game/Scripts/UI/Shell/Ecs", "*.cs", SearchOption.AllDirectories))
            CheckHotPathSourceForAllocationPatterns(NormalizeAssetPath(path), allocationTokens, violations);

        CheckHotPathSourceForAllocationPatterns(ShellApplySystemPath, allocationTokens, violations);

        AssertNoViolations(violations, "UI Toolkit ECS/apply hot paths must avoid known recurring allocation and element lookup patterns.");
    }

    [Test]
    public void UiToolkitManagedApplyEdgeIsClassifiedAndDocumented()
    {
        string plan = File.ReadAllText("Design/Architecture/ui_toolkit_canvas_replacement_plan.md");
        string applySource = File.ReadAllText(ShellApplySystemPath);

        StringAssert.Contains("## Managed UI Toolkit Edge Classification", plan);
        StringAssert.Contains("intentional managed presentation boundary", plan);
        StringAssert.Contains("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs", plan);
        StringAssert.Contains("PresentationSystemGroup", plan);
        StringAssert.Contains("Allowed inputs: ECS/read-model data exposed through `UiShellRuntimeGateway`", plan);
        StringAssert.Contains("Allowed outputs: visual apply calls on `UiToolkitShellView`", plan);
        StringAssert.Contains("Forbidden behavior: gameplay decisions", plan);
        StringAssert.Contains("Residual constraint: this system remains `SystemBase`", plan);
        StringAssert.Contains("Fallback removal constraint: Canvas fallback objects can only be removed after the user approves fallback deletion", plan);

        StringAssert.Contains("[UpdateInGroup(typeof(PresentationSystemGroup))]", applySource);
        StringAssert.Contains("public sealed partial class UiToolkitShellApplySystem : SystemBase", applySource);
        StringAssert.Contains("UiShellRuntimeGateway.TryReadShellState", applySource);
        StringAssert.Contains("UiShellRuntimeGateway.TryConsumePresentationCommands(commandScratch)", applySource);
        StringAssert.Contains("shellView.ApplyPresentationCommands(commandScratch)", applySource);
        StringAssert.Contains("UiShellRuntimeGateway.TryEnqueueTransitionComplete(pendingCompletion)", applySource);
        Assert.IsFalse(applySource.Contains("EntityManager.SetComponentData", StringComparison.Ordinal), "The managed UI Toolkit apply edge must not directly mutate ECS component data.");
        Assert.IsFalse(applySource.Contains("GetEntityQuery", StringComparison.Ordinal), "The managed UI Toolkit apply edge must not own ECS queries.");
    }

    [Test]
    public void MenuBootstrapViewWiresUiToolkitShellReferencePath()
    {
        Assert.IsTrue(File.Exists(MenuBootstrapViewPath), $"Missing menu bootstrap view: {MenuBootstrapViewPath}");
        string source = File.ReadAllText(MenuBootstrapViewPath);

        StringAssert.Contains("ConfigureUiToolkitApplySystem(useUiToolkit)", source);
        StringAssert.Contains("World.DefaultGameObjectInjectionWorld", source);
        StringAssert.Contains("world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>()", source);
        StringAssert.Contains("applySystem.ConfigureShellView(uiToolkitShellView)", source);
        StringAssert.Contains("existingSystem?.ClearShellView(uiToolkitShellView)", source);
        Assert.IsFalse(source.Contains("VisualElement", StringComparison.Ordinal), "MenuBootstrapView must not manipulate UI Toolkit elements directly.");
    }

    [Test]
    public void EcsSystemsDoNotTouchUiToolkitObjectsDirectly()
    {
        var violations = new List<string>();
        string[] forbiddenTokens =
        {
            "VisualElement",
            "UIDocument",
            "PanelSettings",
            "TemplateContainer",
            "StyleBackground"
        };

        foreach (string path in Directory.GetFiles("Assets/Game/Scripts", "*.cs", SearchOption.AllDirectories))
        {
            string normalizedPath = NormalizeAssetPath(path);
            if (!IsEcsSystemSource(normalizedPath))
                continue;

            string source = File.ReadAllText(normalizedPath);
            if (!LooksLikeEcsSystemSource(normalizedPath, source))
                continue;

            for (int i = 0; i < forbiddenTokens.Length; i++)
            {
                string token = forbiddenTokens[i];
                if (source.Contains(token, StringComparison.Ordinal))
                    violations.Add($"{normalizedPath} -> {token}");
            }
        }

        AssertNoViolations(violations, "ECS systems must not read or write UI Toolkit objects directly; use read models and the managed UI apply edge.");
    }

    [Test]
    public void LoadingUxmlExposesCanvasParityBindings()
    {
        VisualTreeAsset loadingAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingUxmlPath);
        Assert.IsNotNull(loadingAsset, $"Missing loading UXML asset: {LoadingUxmlPath}");

        string uxml = File.ReadAllText(LoadingUxmlPath);
        string uss = File.ReadAllText(LoadingUssPath);

        string[] requiredNames =
        {
            "SCN01_LoadingContent",
            "LoadingBody",
            "Background",
            "Brand_LogoLockup",
            "CommandSystem_Text",
            "LoadingPanel_Frame",
            "LoadingPanel_Status",
            "LoadingPanel_Percent",
            "Progress_Frame",
            "Progress_Fill",
            "BottomStatus_Spinner",
            "BottomStatus_Text"
        };

        for (int i = 0; i < requiredNames.Length; i++)
            StringAssert.Contains($"name=\"{requiredNames[i]}\"", uxml, $"Loading UXML missing binding element: {requiredNames[i]}");

        StringAssert.Contains("INITIALIZING COMMAND NET", uxml, "Loading UXML must keep the approved new-art default status text.");
        StringAssert.Contains("LOADING REQUIRED DATA", uxml, "Loading UXML must keep the approved bottom status text.");
        StringAssert.Contains("Progress_Fill", uxml, "Loading progress fill must remain a separate bindable element.");
        StringAssert.Contains("TargetLockV04Imagegen", uss, "Loading USS must use the approved new-art loading asset set.");
    }

    [Test]
    public void UiToolkitShellViewMountsLoadingUxmlIntoLoadingSlot()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset loadingAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(loadingAsset, $"Missing loading UXML asset: {LoadingUxmlPath}");

        GameObject host = new("UiToolkitShellLoadingMountSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, loadingAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting loading content.");
            Assert.IsTrue(shellView.HasMountedLoadingScreen, "Configured loading UXML must mount into LoadingScreenSlot.");
            Assert.IsNotNull(shellView.LoadingContentRoot, "Mounted loading content root must be cached.");
            Assert.AreEqual("SCN01_LoadingContent", shellView.LoadingContentRoot.name, "Mounted loading content must keep its binding root name.");
            Assert.AreEqual(1, shellView.LoadingScreenSlot.childCount, "Loading slot must contain a single loading UXML tree.");
            Assert.IsNotNull(shellView.LoadingContentRoot.Q<VisualElement>("Progress_Fill"), "Mounted loading UXML must expose the progress fill binding.");
            Assert.IsNotNull(shellView.LoadingContentRoot.Q<Label>("LoadingPanel_Percent"), "Mounted loading UXML must expose the percent label binding.");

            Assert.IsTrue(shellView.Mount(), "Repeated shell mount must remain stable.");
            Assert.AreEqual(1, shellView.LoadingScreenSlot.childCount, "Repeated shell mount must not duplicate loading content.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasMountedLoadingScreen, "ClearCache must clear mounted loading state.");
            Assert.IsNull(shellView.LoadingContentRoot, "ClearCache must clear loading content root.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewAppliesLoadingProgressReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset loadingAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(loadingAsset, $"Missing loading UXML asset: {LoadingUxmlPath}");

        GameObject host = new("UiToolkitShellLoadingApplySmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, loadingAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before applying loading progress.");
            Assert.IsTrue(shellView.HasRequiredLoadingBindings, "Mounted loading UXML must bind all visual-only and dynamic loading elements.");
            Assert.AreEqual("0%", shellView.LoadingPercentLabel.text, "Mounting loading content must override the static mockup percent to runtime 0%.");
            Assert.AreEqual("Preparing command interface", shellView.LoadingStatusLabel.text, "Empty initial loading status must use the Canvas-compatible fallback.");
            AssertPercentWidth(shellView.LoadingProgressFill, 0f, "Initial mounted loading fill");

            Assert.IsTrue(shellView.ApplyLoadingProgress(new UiShellLoadingProgressModel(0.42f, "Loading terrain", false)));

            Assert.AreEqual("42%", shellView.LoadingPercentLabel.text);
            Assert.AreEqual("Loading terrain", shellView.LoadingStatusLabel.text);
            AssertPercentWidth(shellView.LoadingProgressFill, 42f, "Mid-progress loading fill");
            Assert.AreEqual("LOADING REQUIRED DATA", shellView.LoadingBottomStatusLabel.text, "Bottom status remains the approved static text until animation binding is added.");

            Assert.IsTrue(shellView.ApplyLoadingProgress(new UiShellLoadingProgressModel(1.5f, string.Empty, true)));

            Assert.AreEqual("100%", shellView.LoadingPercentLabel.text, "Loading percent must clamp above-range progress to 100%.");
            Assert.AreEqual("Preparing command interface", shellView.LoadingStatusLabel.text, "Empty runtime status must keep the fallback text.");
            AssertPercentWidth(shellView.LoadingProgressFill, 100f, "Complete loading fill");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewAppliesLoadingPresentationCommands()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset loadingAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(loadingAsset, $"Missing loading UXML asset: {LoadingUxmlPath}");

        GameObject host = new("UiToolkitShellLoadingCommandSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, loadingAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before applying loading commands.");
            Assert.IsTrue(shellView.LoadingLayer.ClassListContains("shell-hidden"), "Loading layer must remain hidden before a ShowLoading command.");

            var commands = new List<UiShellPresentationCommandModel>
            {
                new(
                    UiShellCommandKind.ShowLoading,
                    UiShellRegionId.LoadingLayer,
                    UIRoute.Match,
                    UiShellMode.Loading,
                    7)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsFalse(shellView.LoadingLayer.ClassListContains("shell-hidden"), "ShowLoading must reveal the UI Toolkit loading layer.");
            Assert.IsTrue(shellView.LoadingScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)), "ShowLoading must apply the visible shell motion state.");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.ExitLoading,
                UiShellRegionId.LoadingLayer,
                UIRoute.Match,
                UiShellMode.MatchHud,
                8);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsTrue(shellView.LoadingLayer.ClassListContains("shell-hidden"), "ExitLoading must hide the UI Toolkit loading layer.");
            Assert.IsTrue(shellView.LoadingScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.FadeOut)), "ExitLoading must apply the fade-out shell motion state.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void MenuBootstrapSystemKeepsLoadingProgressActiveInUiToolkitMode()
    {
        Assert.IsTrue(File.Exists(MenuBootstrapSystemPath), $"Missing menu bootstrap system: {MenuBootstrapSystemPath}");
        string source = File.ReadAllText(MenuBootstrapSystemPath);

        StringAssert.Contains("bool useUiToolkit = view.IsUiToolkitMode;", source);
        StringAssert.Contains("if (!useUiToolkit)\n            ApplyUiPresentationMode(view.UiCamera, view.UiCanvas, shellState, entityManager);", source);
        StringAssert.Contains("QueueDeferredMatchLoadAfterLoadingFeedback(entityManager, shellState);", source);
        StringAssert.Contains("UpdateActualLoadingProgress(entityManager, boundary, shellState);", source);
        StringAssert.Contains("if (useUiToolkit)\n        {\n            BindUiToolkitMatchReadModels(shellState);\n            ClearBoundMatchRuntimeUi();\n            return;\n        }\n\n        ClearUiToolkitMatchReadModels();\n        BindMatchRuntimeUi(view, shellState);", source);
        StringAssert.Contains("if (!wasInitialized)\n                ResetShellForFreshMenuScene();", source);

        int modeIndex = source.IndexOf("bool useUiToolkit = view.IsUiToolkitMode;", StringComparison.Ordinal);
        int progressIndex = source.IndexOf("UpdateActualLoadingProgress(entityManager, boundary, shellState);", StringComparison.Ordinal);
        int toolkitReturnIndex = source.IndexOf("if (useUiToolkit)\n        {\n            BindUiToolkitMatchReadModels(shellState);", StringComparison.Ordinal);

        Assert.GreaterOrEqual(modeIndex, 0, "Update must cache UI Toolkit mode after applying runtime mode.");
        Assert.Greater(progressIndex, modeIndex, "Loading progress must update after mode detection.");
        Assert.Greater(toolkitReturnIndex, progressIndex, "UI Toolkit mode may skip Canvas binding only after loading progress is updated.");
    }

    [Test]
    public void UiToolkitLoadingMountDoesNotShowInitialLoadingOrBlockByDefault()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset loadingAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(loadingAsset, $"Missing loading UXML asset: {LoadingUxmlPath}");

        GameObject host = new("UiToolkitShellNoInitialLoadingSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, loadingAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating initial loading visibility.");
            Assert.IsTrue(shellView.HasMountedLoadingScreen, "Loading content should mount so it is ready for route requests.");
            Assert.IsTrue(shellView.LoadingLayer.ClassListContains("shell-hidden"), "Mounted loading content must remain hidden on the default menu boot path.");
            Assert.IsFalse(shellView.IsElementBlockingUi(shellView.LoadingContentRoot, out _), "Hidden mounted loading content must not block world/menu input before ShowLoading.");
            Assert.AreEqual("0%", shellView.LoadingPercentLabel.text, "Mounted loading content should initialize to runtime 0% even while hidden.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitLoadingLayerStaysAboveVisiblePopupWhenShown()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset loadingAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(loadingAsset, $"Missing loading UXML asset: {LoadingUxmlPath}");

        GameObject host = new("UiToolkitShellLoadingTopmostSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, loadingAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating loading topmost behavior.");
            shellView.ModalOverlay.RemoveFromClassList("shell-hidden");

            var commands = new List<UiShellPresentationCommandModel>
            {
                new(
                    UiShellCommandKind.ShowLoading,
                    UiShellRegionId.LoadingLayer,
                    UIRoute.Match,
                    UiShellMode.Loading,
                    13)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.ModalOverlay, "Visible loading layer must draw above visible popups.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.TooltipLayer, "Visible loading layer must draw above tooltip overlays.");
            Assert.IsTrue(shellView.IsElementBlockingUi(shellView.LoadingContentRoot, out string source), "Visible loading content must block underlying menus, popups, and world clicks.");
            Assert.AreEqual("SCN01_LoadingContent", source);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitLoadingLayerOverridesMountedPopupDuringTransition()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset loadingAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingUxmlPath);
        VisualTreeAsset resultAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MissionResultPopupUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(loadingAsset, $"Missing loading UXML asset: {LoadingUxmlPath}");
        Assert.IsNotNull(resultAsset, $"Missing Mission Result popup UXML asset: {MissionResultPopupUxmlPath}");

        GameObject host = new("UiToolkitShellLoadingOverridesPopupSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(
                document,
                shellAsset,
                loadingAsset,
                null,
                null,
                null,
                null,
                null,
                null,
                resultAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating loading popup precedence.");
            Assert.IsTrue(shellView.MountMissionResultPopup(), "Mission Result popup must mount before popup/loading precedence validation.");

            var popupCommands = new[]
            {
                new UiShellPresentationCommandModel(
                    UiShellCommandKind.ShowPopup,
                    UiShellRegionId.PopupLayer,
                    UIRoute.Match,
                    UiShellMode.PopupOnly,
                    21)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(popupCommands), "ShowPopup presentation must apply before loading transition.");
            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "Popup slot must be visible before loading transition.");
            Assert.IsFalse(shellView.ModalOverlay.ClassListContains("shell-hidden"), "Modal overlay must be visible before loading transition.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupVisible)), "Popup slot must use visible popup motion before loading transition.");

            var loadingCommands = new[]
            {
                new UiShellPresentationCommandModel(
                    UiShellCommandKind.ShowLoading,
                    UiShellRegionId.LoadingLayer,
                    UIRoute.Match,
                    UiShellMode.Loading,
                    22)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(loadingCommands), "ShowLoading must apply over a visible popup during route transition.");
            Assert.IsFalse(shellView.LoadingLayer.ClassListContains("shell-hidden"), "Loading layer must be visible while overriding a popup.");
            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "Loading must cover, not depend on destroying, the visible popup layer.");
            AssertDrawsAfter(shellView.SafeAreaRoot, shellView.LoadingLayer, shellView.ModalOverlay, "Loading layer must draw above a mounted visible popup during route transition.");
            Assert.IsTrue(shellView.IsElementBlockingUi(shellView.LoadingContentRoot, out string source), "Visible loading content must block mounted popup and world clicks during route transition.");
            Assert.AreEqual("SCN01_LoadingContent", source);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitLoadingTextBindingsStayVisibleWhenShown()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset loadingAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(loadingAsset, $"Missing loading UXML asset: {LoadingUxmlPath}");

        GameObject host = new("UiToolkitShellLoadingTextVisibilitySmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, loadingAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating loading text visibility.");
            Assert.IsTrue(shellView.HasRequiredLoadingBindings, "Mounted loading UXML must bind every loading text and progress element.");

            var commands = new List<UiShellPresentationCommandModel>
            {
                new(
                    UiShellCommandKind.ShowLoading,
                    UiShellRegionId.LoadingLayer,
                    UIRoute.Match,
                    UiShellMode.Loading,
                    21)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsFalse(shellView.LoadingLayer.ClassListContains("shell-hidden"), "ShowLoading must remove the hidden class from the loading layer before text visibility validation.");

            AssertVisibleLabelText(shellView.LoadingStatusLabel, "Loading status label", "Preparing command interface");
            AssertVisibleLabelText(shellView.LoadingPercentLabel, "Loading percent label", "0%");
            AssertVisibleLabelText(shellView.LoadingBottomStatusLabel, "Loading bottom status label", "LOADING REQUIRED DATA");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitLoadingProgressCompletionCanExitLoadingLayer()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset loadingAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(loadingAsset, $"Missing loading UXML asset: {LoadingUxmlPath}");
        Assert.IsTrue(File.Exists(UiShellFlowSystemPath), $"Missing shell flow system: {UiShellFlowSystemPath}");

        GameObject host = new("UiToolkitShellLoadingCompletionSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, loadingAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating completion-to-exit behavior.");

            var commands = new List<UiShellPresentationCommandModel>
            {
                new(
                    UiShellCommandKind.ShowLoading,
                    UiShellRegionId.LoadingLayer,
                    UIRoute.Match,
                    UiShellMode.Loading,
                    31)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsTrue(shellView.ApplyLoadingProgress(new UiShellLoadingProgressModel(1f, "Command shell ready", true)));
            Assert.AreEqual("100%", shellView.LoadingPercentLabel.text, "Completed loading must display 100% before the route exit command is applied.");
            AssertPercentWidth(shellView.LoadingProgressFill, 100f, "Completed loading fill");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.ExitLoading,
                UiShellRegionId.LoadingLayer,
                UIRoute.Match,
                UiShellMode.MatchHud,
                32);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsTrue(shellView.LoadingLayer.ClassListContains("shell-hidden"), "ExitLoading must hide the completed loading layer.");

            string flowSource = File.ReadAllText(UiShellFlowSystemPath);
            string applySource = File.ReadAllText(ShellApplySystemPath);
            StringAssert.Contains("if (shellState.CurrentMode == UiShellMode.Loading && loading.IsComplete != 0)", flowSource, "The ECS shell flow must still route out of loading when progress is complete.");
            StringAssert.Contains("UiShellCommandKind.ExitLoading", flowSource, "Completed loading must enqueue an ExitLoading command.");
            StringAssert.Contains("UiShellCommandKind.EnterMatchHud", flowSource, "Completed match loading must enqueue the Match HUD route command.");
            StringAssert.Contains("UiShellCommandKind.EnterMenu", flowSource, "Completed Main Menu loading must enqueue the Main Menu route command.");
            StringAssert.Contains("case UiShellRouteIntent.ReturnToMainMenu", flowSource, "Return-to-menu requests must route through loading before Main Menu is shown.");
            StringAssert.Contains("UiShellRuntimeGateway.TryEnqueueTransitionComplete(pendingCompletion)", applySource, "The UI Toolkit apply edge must complete presentation commands so the shell route can continue.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void MainMenuUxmlExposesCanvasParityBindings()
    {
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");

        string uxml = File.ReadAllText(MainMenuUxmlPath);
        string uss = File.ReadAllText(MainMenuUssPath);

        string[] requiredNames =
        {
            "SCN02_MainMenuContent",
            "MenuBackgroundContent",
            "BackgroundArt",
            "HeaderContent",
            "HeaderLogoPanel",
            "HeaderResourceArea",
            "CreditsPanel",
            "SuppliesPanel",
            "CommandPanel",
            "InboxButton",
            "SettingsButton",
            "MenuButton",
            "LeftContent",
            "LeftNavPanel",
            "Nav_Campaign",
            "Nav_Armory",
            "Nav_Supply",
            "Nav_Command",
            "Nav_TechTree",
            "Nav_Profile",
            "MiddleContent",
            "Card_Campaign",
            "Card_Skirmish",
            "Card_Operations",
            "RightContent",
            "CommanderPanel",
            "CommanderPortraitPanel",
            "IdentityPanel",
            "ProgressPanel",
            "ReadinessPanel",
            "FooterContent",
            "DeployOperationButton"
        };

        for (int i = 0; i < requiredNames.Length; i++)
            StringAssert.Contains($"name=\"{requiredNames[i]}\"", uxml, $"Main Menu UXML missing parity binding element: {requiredNames[i]}");

        string[] buttonNames =
        {
            "InboxButton",
            "SettingsButton",
            "MenuButton",
            "Nav_Campaign",
            "Nav_Armory",
            "Nav_Supply",
            "Nav_Command",
            "Nav_TechTree",
            "Nav_Profile",
            "Card_Campaign",
            "Card_Skirmish",
            "Card_Operations",
            "CommanderPanel",
            "DeployOperationButton"
        };

        for (int i = 0; i < buttonNames.Length; i++)
            StringAssert.Contains($"<ui:Button name=\"{buttonNames[i]}\"", uxml, $"Main Menu actionable element must remain a UI Toolkit Button: {buttonNames[i]}");

        string[] requiredTexts =
        {
            "CAMPAIGN",
            "ARMORY",
            "SUPPLY",
            "COMMAND",
            "TECH TREE",
            "PROFILE",
            "SKIRMISH",
            "OPERATIONS",
            "COMMANDER",
            "COL. ALEX MORGAN",
            "VICTORY IS PLANNED",
            "FACTION STANDING",
            "DEPLOY"
        };

        for (int i = 0; i < requiredTexts.Length; i++)
            StringAssert.Contains(requiredTexts[i], uxml, $"Main Menu UXML missing default text: {requiredTexts[i]}");

        StringAssert.Contains("MainMenuBrightCommand", uss, "Main Menu USS must use the approved new-art-direction MainMenuBrightCommand asset set.");
        StringAssert.Contains("TargetLockV04Imagegen", uss, "Main Menu header must use the approved new loading logo lockup instead of stale menu logos.");
    }

    [Test]
    public void MainMenuHeaderIconsUseCenteredSafeRects()
    {
        string uss = File.ReadAllText(MainMenuUssPath);

        string headerLogoBlock = GetCssBlock(uss, ".header-logo {");
        string resourceIconBlock = GetCssBlock(uss, ".resource-icon {");
        string resourcePlusBlock = GetCssBlock(uss, ".resource-plus {");
        string headerButtonBlock = GetCssBlock(uss, ".header-icon-button {");
        string headerButtonFrameBlock = GetCssBlock(uss, ".header-icon-button-frame {");
        string headerActionIconBlock = GetLastCssBlock(uss, ".header-action-icon {");

        AssertCssContains(headerLogoBlock, "-unity-background-scale-mode: scale-to-fit;", "Header logo must fit inside the logo slot instead of cropping.");

        AssertVerticalCenterFromTopHeight(resourceIconBlock, "Header resource icons");
        AssertVerticalCenterFromTopHeight(resourcePlusBlock, "Header plus icons");
        AssertSymmetricInsets(headerActionIconBlock, "Header action icons");
        AssertSymmetricInsets(headerButtonFrameBlock, "Header action button frame");
        AssertSymmetricSlice(headerButtonFrameBlock, "Header action button frame");

        float buttonWidth = ReadPercentProperty(headerButtonBlock, "width");
        Assert.Greater(buttonWidth, 24f, "Header action hit areas must remain close to their visible square frames.");
        Assert.Less(buttonWidth, 34f, "Header action hit areas must not spill beyond their visible square frames.");
    }

    [Test]
    public void MainMenuSettingsAndMailHitAreasMatchVisibleFrames()
    {
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");

        TemplateContainer root = mainMenuAsset.CloneTree();
        string uss = File.ReadAllText(MainMenuUssPath);

        string buttonResetBlock = GetCssBlock(uss, ".scn02-main-menu-content Button {");
        string headerActionsBlock = GetCssBlock(uss, ".header-actions-panel {");
        string headerButtonBlock = GetCssBlock(uss, ".header-icon-button {");
        string headerButtonFrameBlock = GetCssBlock(uss, ".header-icon-button-frame {");
        string headerActionIconBlock = GetLastCssBlock(uss, ".header-action-icon {");

        AssertCssContains(buttonResetBlock, "padding-left: 0;", "Header action buttons must not inherit default UI Toolkit padding that desyncs hit rect from the visible frame.");
        AssertCssContains(buttonResetBlock, "padding-right: 0;", "Header action buttons must not inherit default UI Toolkit padding that desyncs hit rect from the visible frame.");
        AssertCssContains(buttonResetBlock, "padding-top: 0;", "Header action buttons must not inherit default UI Toolkit padding that desyncs hit rect from the visible frame.");
        AssertCssContains(buttonResetBlock, "padding-bottom: 0;", "Header action buttons must not inherit default UI Toolkit padding that desyncs hit rect from the visible frame.");
        AssertCssContains(buttonResetBlock, "background-color: rgba(0, 0, 0, 0);", "Header action hit areas must not show an extra default button plate.");

        AssertCssContains(headerActionsBlock, "flex-direction: row;", "Settings and mail buttons must share the target horizontal header action rail.");
        AssertCssContains(headerActionsBlock, "justify-content: space-between;", "Header action buttons must not overlap or drift from their visible frames.");
        AssertCssContains(headerButtonBlock, "position: relative;", "The button itself must be the positioned hit rect for its visible frame.");
        AssertCssContains(headerButtonBlock, "height: 100%;", "Header action hit rect height must match the visible frame height.");
        AssertSymmetricInsets(headerButtonFrameBlock, "Header action button frame");
        AssertSymmetricInsets(headerActionIconBlock, "Header action icon");
        AssertCssSelectorMissing(uss, ".inbox-button {", "InboxButton must not have a positional USS override separate from the shared visible frame.");
        AssertCssSelectorMissing(uss, ".settings-button {", "SettingsButton must not have a positional USS override separate from the shared visible frame.");

        AssertHeaderActionButtonUsesSharedFrame(root, "InboxButton", "inbox-icon");
        AssertHeaderActionButtonUsesSharedFrame(root, "SettingsButton", "settings-icon");
    }

    [Test]
    public void MainMenuModeCardsKeepTextAndPortraitsInsideSafePadding()
    {
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");

        TemplateContainer root = mainMenuAsset.CloneTree();
        string uss = File.ReadAllText(MainMenuUssPath);

        string modeCardBlock = GetCssBlock(uss, ".mode-card {");
        string artBlock = GetCssBlock(uss, ".mode-card-art {");
        string fillBlock = GetCssBlock(uss, ".mode-card-fill {");
        string frameBlock = GetCssBlock(uss, ".mode-card-frame {");
        string labelPlateBlock = GetCssBlock(uss, ".mode-card-label-plate {");
        string badgeFrameBlock = GetCssBlock(uss, ".mode-badge-frame {");
        string badgeIconBlock = GetCssBlock(uss, ".mode-badge-icon {");
        string titleBlock = GetCssBlock(uss, ".mode-title {");
        string dividerBlock = GetCssBlock(uss, ".mode-divider {");
        string bottomStarBlock = GetLastCssBlock(uss, ".mode-bottom-star {");

        float cardWidth = ReadPercentProperty(modeCardBlock, "width");
        Assert.Greater(cardWidth, 28f, "Mode cards must remain wide enough for readable target-lock title sections.");
        Assert.Less(cardWidth, 34f, "Mode cards must not overgrow and collide with adjacent cards.");
        AssertCssContains(modeCardBlock, "height: 100%;", "Mode cards must fill the middle panel height defined by the target grid.");

        AssertSymmetricInsets(frameBlock, "Mode card frame");
        AssertSymmetricInsets(fillBlock, "Mode card backing");
        AssertSymmetricSlice(frameBlock, "Mode card frame");
        AssertSymmetricSlice(fillBlock, "Mode card backing");
        AssertCssContains(artBlock, "-unity-background-scale-mode: scale-and-crop;", "Mode card portraits must fill the art well without stretching.");
        AssertCssContains(titleBlock, "-unity-text-align: middle-center;", "Mode card titles must be centered inside their label plates.");
        AssertCssContains(titleBlock, "white-space: nowrap;", "Mode card titles must not wrap into the chrome.");

        AssertModeCardUpperArtHasSafePadding(artBlock);
        AssertModeCardTitleInsideLabelPlate(titleBlock, labelPlateBlock);
        AssertModeCardBadgeInsideJunction(badgeFrameBlock, badgeIconBlock);
        AssertModeCardBottomDecorInsideLabelPlate(dividerBlock, bottomStarBlock, labelPlateBlock);

        AssertModeCardStructure(root, "Card_Campaign", "CAMPAIGN", "campaign-art", "campaign-frame", "campaign-label-plate", "campaign-title-icon");
        AssertModeCardStructure(root, "Card_Skirmish", "SKIRMISH", "skirmish-art", "skirmish-frame", "skirmish-label-plate", "skirmish-title-icon");
        AssertModeCardStructure(root, "Card_Operations", "OPERATIONS", "operations-art", "operations-frame", "operations-label-plate", "operations-title-icon");
    }

    [Test]
    public void MainMenuCommanderProfileUsesReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset commanderAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CommanderProfileUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(commanderAsset, $"Missing Commander/Profile UXML asset: {CommanderProfileUxmlPath}");

        string uxml = File.ReadAllText(MainMenuUxmlPath);
        string uss = File.ReadAllText(MainMenuUssPath);
        StringAssert.Contains("class=\"commander-portrait commander-portrait-default\"", uxml, "Commander portrait must expose a semantic portrait class for read-model selection.");
        StringAssert.Contains(".commander-portrait-default", uss, "Commander portrait default art must be class-based so the read model can swap portrait classes.");

        GameObject host = new("UiToolkitShellMainMenuCommanderReadModelSmoke");
        using World world = new("UiToolkitShellMainMenuCommanderReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, null, null, null, commanderAsset);

            var gateway = new RecordingUiShellRuntimeGateway
            {
                HasShellState = true,
                ShellState = new UiShellStateModel(
                    UiShellMode.MainMenu,
                    UIRoute.MainMenu,
                    UiShellTransitionPhase.MenuReady,
                    61,
                    false),
                HasCommanderProfile = true,
                CommanderProfile = new UiShellCommanderProfileModel(
                    "GEN. MAYA VALE",
                    "COMMAND READY",
                    "commander-portrait-default")
            };

            UiShellRuntimeGateway.Register(gateway);
            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasCommanderProfile, "Apply system must read the commander/profile read model.");
            Assert.AreEqual("GEN. MAYA VALE", applySystem.LastCommanderProfile.Name, "Apply system did not capture commander name.");
            Assert.AreEqual("COMMAND READY", applySystem.LastCommanderProfile.Subtitle, "Apply system did not capture commander subtitle.");
            Assert.IsTrue(shellView.HasRequiredMainMenuBindings, "Mounted Main Menu must expose commander/profile binding targets.");
            Assert.AreEqual("GEN. MAYA VALE", shellView.MainMenuCommanderNameLabel.text, "Commander name must be applied from the read model.");
            Assert.AreEqual("COMMAND READY", shellView.MainMenuCommanderSubtitleLabel.text, "Commander subtitle must be applied from the read model.");
            Assert.IsTrue(shellView.MainMenuCommanderPortrait.ClassListContains("commander-portrait-default"), "Commander portrait class must be applied from the read model.");

            gateway.CommanderProfile = new UiShellCommanderProfileModel(string.Empty, string.Empty, string.Empty);
            applySystem.Update();

            Assert.AreEqual("COL. ALEX MORGAN", shellView.MainMenuCommanderNameLabel.text, "Empty read-model name must fall back to the approved default.");
            Assert.AreEqual("VICTORY IS PLANNED", shellView.MainMenuCommanderSubtitleLabel.text, "Empty read-model subtitle must fall back to the approved default.");
            Assert.IsTrue(shellView.MainMenuCommanderPortrait.ClassListContains("commander-portrait-default"), "Empty portrait class must fall back to the approved default portrait.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void MainMenuResourceValuesUseReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset commanderAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CommanderProfileUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(commanderAsset, $"Missing Commander/Profile UXML asset: {CommanderProfileUxmlPath}");

        GameObject host = new("UiToolkitShellMainMenuResourcesReadModelSmoke");
        using World world = new("UiToolkitShellMainMenuResourcesReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, null, null, null, commanderAsset);

            var gateway = new RecordingUiShellRuntimeGateway
            {
                HasShellState = true,
                ShellState = new UiShellStateModel(
                    UiShellMode.MainMenu,
                    UIRoute.MainMenu,
                    UiShellTransitionPhase.MenuReady,
                    62,
                    false),
                HasMainMenuResources = true,
                MainMenuResources = new UiShellMainMenuResourcesModel("187,540", "2,860", "92/120")
            };

            UiShellRuntimeGateway.Register(gateway);
            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasMainMenuResources, "Apply system must read the Main Menu resources read model.");
            Assert.AreEqual("187,540", applySystem.LastMainMenuResources.CreditsText, "Apply system did not capture credits text.");
            Assert.AreEqual("2,860", applySystem.LastMainMenuResources.SuppliesText, "Apply system did not capture supplies text.");
            Assert.AreEqual("92/120", applySystem.LastMainMenuResources.CommandText, "Apply system did not capture command text.");
            Assert.IsTrue(shellView.HasRequiredMainMenuBindings, "Mounted Main Menu must expose resource binding targets.");
            Assert.AreEqual("187,540", shellView.MainMenuCreditsValueLabel.text, "Credits must be applied from the read model.");
            Assert.AreEqual("2,860", shellView.MainMenuSuppliesValueLabel.text, "Supplies must be applied from the read model.");
            Assert.AreEqual("92/120", shellView.MainMenuCommandValueLabel.text, "Command must be applied from the read model.");

            gateway.MainMenuResources = new UiShellMainMenuResourcesModel(string.Empty, string.Empty, string.Empty);
            applySystem.Update();

            Assert.AreEqual("12,450", shellView.MainMenuCreditsValueLabel.text, "Empty credits read model must fall back to the approved default.");
            Assert.AreEqual("1,280", shellView.MainMenuSuppliesValueLabel.text, "Empty supplies read model must fall back to the approved default.");
            Assert.AreEqual("78/100", shellView.MainMenuCommandValueLabel.text, "Empty command read model must fall back to the approved default.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewMountsMainMenuUxmlIntoMainMenuSlot()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset commanderAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CommanderProfileUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(commanderAsset, $"Missing Commander/Profile UXML asset: {CommanderProfileUxmlPath}");

        GameObject host = new("UiToolkitShellMainMenuMountSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, null, null, null, commanderAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Main Menu content.");
            Assert.AreSame(mainMenuAsset, shellView.MainMenuScreenAsset, "Configured Main Menu asset must be retained by the shell view.");
            Assert.IsTrue(shellView.HasMountedMainMenuScreen, "Configured Main Menu UXML must mount into MainMenuScreenSlot.");
            Assert.IsTrue(shellView.HasRequiredMainMenuBindings, "Mounted Main Menu UXML must bind every Phase 3 action target.");
            Assert.IsNotNull(shellView.MainMenuContentRoot, "Mounted Main Menu content root must be cached.");
            Assert.AreEqual("SCN02_MainMenuContent", shellView.MainMenuContentRoot.name, "Mounted Main Menu content must keep its binding root name.");
            Assert.AreEqual(1, shellView.MainMenuScreenSlot.childCount, "Main Menu slot must contain a single Main Menu UXML tree.");
            Assert.IsNotNull(shellView.MainMenuContentRoot.Q<Button>("DeployOperationButton"), "Mounted Main Menu UXML must expose the deploy button binding.");
            Assert.IsNotNull(shellView.MainMenuContentRoot.Q<Button>("CommanderPanel"), "Mounted Main Menu UXML must expose the commander/profile button binding.");
            Assert.IsNotNull(shellView.MainMenuContentRoot.Q<VisualElement>("HeaderContent"), "Mounted Main Menu UXML must expose persistent header content.");

            Assert.IsTrue(shellView.Mount(), "Repeated shell mount must remain stable.");
            Assert.AreEqual(1, shellView.MainMenuScreenSlot.childCount, "Repeated shell mount must not duplicate Main Menu content.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasMountedMainMenuScreen, "ClearCache must clear mounted Main Menu state.");
            Assert.IsNull(shellView.MainMenuContentRoot, "ClearCache must clear Main Menu content root.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewMountsArmoryUxmlIntoArmorySlot()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset armoryAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ArmoryUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(armoryAsset, $"Missing Armory UXML asset: {ArmoryUxmlPath}");

        GameObject host = new("UiToolkitShellArmoryMountSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, armoryAsset, null, null);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Armory content.");
            Assert.AreSame(armoryAsset, shellView.ArmoryScreenAsset, "Configured Armory asset must be retained by the shell view.");
            Assert.IsFalse(shellView.HasMountedArmoryScreen, "Shell mount must not eagerly instantiate the Armory screen while Main Menu is active.");

            var commands = new List<UiShellPresentationCommandModel>
            {
                new(
                    UiShellCommandKind.SwapMenuMiddle,
                    UiShellRegionId.MiddleRegion,
                    UIRoute.Armory,
                    UiShellMode.MainMenu,
                    81)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsTrue(shellView.HasMountedArmoryScreen, "Armory route must mount the configured Armory UXML into ArmoryScreenSlot.");
            Assert.IsTrue(shellView.HasRequiredArmoryBindings, "Mounted Armory UXML must expose the Phase 7 screen and action bindings.");
            Assert.IsNotNull(shellView.ArmoryContentRoot, "Mounted Armory content root must be cached.");
            Assert.AreEqual("SCN19_ArmoryContent", shellView.ArmoryContentRoot.name, "Mounted Armory content must keep its binding root name.");
            Assert.AreEqual(1, shellView.ArmoryScreenSlot.childCount, "Armory slot must contain a single Armory UXML tree.");
            Assert.IsTrue(shellView.MainMenuScreenSlot.ClassListContains("shell-hidden"), "Armory route must hide the Main Menu body when a dedicated Armory screen is mounted.");
            Assert.IsFalse(shellView.ArmoryScreenSlot.ClassListContains("shell-hidden"), "Armory route must reveal the dedicated Armory screen slot.");
            Assert.IsTrue(shellView.ArmoryScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)), "Armory route must apply the visible motion state to the dedicated Armory slot.");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.SwapMenuMiddle,
                UiShellRegionId.MiddleRegion,
                UIRoute.MainMenu,
                UiShellMode.MainMenu,
                82);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsFalse(shellView.MainMenuScreenSlot.ClassListContains("shell-hidden"), "Returning to Main Menu must reveal the Main Menu body.");
            Assert.IsTrue(shellView.ArmoryScreenSlot.ClassListContains("shell-hidden"), "Returning to Main Menu must hide the dedicated Armory screen.");

            Assert.IsTrue(shellView.Mount(), "Repeated shell mount must remain stable.");
            Assert.AreEqual(1, shellView.ArmoryScreenSlot.childCount, "Repeated shell mount must not duplicate Armory content.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasMountedArmoryScreen, "ClearCache must clear mounted Armory state.");
            Assert.IsNull(shellView.ArmoryContentRoot, "ClearCache must clear Armory content root.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewBindsArmoryRetainedItemsAndActions()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset armoryAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ArmoryUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(armoryAsset, $"Missing Armory UXML asset: {ArmoryUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasArmoryCategory = true,
            ArmoryCategory = ArmoryCatalogCategory.Characters
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellArmoryBindingSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, armoryAsset, null, null);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before Armory retained binding validation.");
            Assert.IsTrue(shellView.MountArmoryScreen(), "Armory retained binding validation must mount the Armory screen explicitly.");
            Assert.IsTrue(shellView.HasRequiredArmoryRuntimeBindings, "Armory runtime binding cache must include roster, category nav, inspection, tabs, and actions.");
            Assert.IsNotNull(shellView.ArmoryScrollView, "Armory roster scroll view must be cached.");
            Assert.IsNotNull(shellView.ArmoryCatalogContent, "Armory retained item content root must be cached.");
            Assert.AreEqual(8, shellView.ArmoryItems.Count, "Armory retained item cache must preserve the eight visible target rows.");
            Assert.AreEqual("RIFLE SQUAD", shellView.ArmoryItemTitleLabels[0].text, "Armory item title binding mismatch.");
            Assert.IsTrue(shellView.ArmoryItems[0].ClassListContains("selected"), "Initial Armory item must be selected.");
            Assert.IsTrue(shellView.ArmoryItems[4].ClassListContains("locked"), "Locked Armory item state must be retained.");

            Assert.IsTrue(shellView.SelectArmoryItem(1), "Unlocked Armory item selection must update retained state.");
            Assert.IsFalse(shellView.ArmoryItems[0].ClassListContains("selected"), "Previous Armory item must leave selected state.");
            Assert.IsTrue(shellView.ArmoryItems[1].ClassListContains("selected"), "Selected Armory item must gain selected state.");
            Assert.AreEqual("FAST APC", shellView.ArmoryInspectionNameLabel.text, "Inspection title must follow retained selection.");
            Assert.AreEqual("VEHICLE", shellView.ArmoryInspectionTypeLabel.text, "Inspection type must follow retained selection.");

            Assert.IsFalse(shellView.SelectArmoryItem(4), "Locked Armory items must not trigger selection.");
            Assert.IsFalse(shellView.ArmoryItems[4].ClassListContains("selected"), "Locked Armory item must not gain selected state.");
            Assert.IsTrue(shellView.ArmoryItems[1].ClassListContains("selected"), "Rejected locked selection must preserve the previous selected item.");

            Assert.IsTrue(shellView.TrySubmitArmoryCategory(ArmoryCatalogCategory.Vehicles), "Armory category action must enqueue through the shell runtime gateway.");
            Assert.AreEqual(1, gateway.ArmoryCategoryRequests.Count, "Armory category request must be recorded once.");
            Assert.AreEqual(ArmoryCatalogCategory.Vehicles, gateway.ArmoryCategoryRequests[0], "Armory category request kind mismatch.");
            Assert.IsTrue(shellView.ArmoryCategoryActions[1].ClassListContains("category-selected"), "Submitted Armory category must update visual state.");
            Assert.IsFalse(shellView.ArmoryCategoryActions[0].ClassListContains("category-selected"), "Submitted Armory category must clear prior visual state.");

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            StringAssert.Contains("BindArmoryScreen", shellViewSource, "Shell view must bind Armory retained elements through a dedicated method.");
            StringAssert.Contains("CacheArmoryItem", shellViewSource, "Shell view must cache retained Armory item rows.");
            StringAssert.Contains("TrySubmitArmoryCategory", shellViewSource, "Shell view must submit Armory category changes through the runtime gateway.");
            StringAssert.Contains("UiShellRuntimeGateway.TryEnqueueArmoryCategory", shellViewSource, "Armory category actions must use the ECS shell category boundary.");
            StringAssert.Contains("ClassListContains(\"locked\")", shellViewSource, "Armory item selection must guard locked rows.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesArmoryCategoryReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset armoryAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ArmoryUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(armoryAsset, $"Missing Armory UXML asset: {ArmoryUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MainMenu,
                UIRoute.Armory,
                UiShellTransitionPhase.MenuReady,
                91,
                false),
            HasArmoryCategory = true,
            ArmoryCategory = ArmoryCatalogCategory.Aircrafts
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellArmoryCategoryReadModelSmoke");
        using World world = new("UiToolkitShellArmoryCategoryReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, armoryAsset, null, null);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasArmoryCategory, "Apply system must read Armory category state through the runtime gateway.");
            Assert.AreEqual(ArmoryCatalogCategory.Aircrafts, applySystem.LastArmoryCategory, "Apply system did not capture Armory category state.");
            Assert.IsTrue(shellView.HasMountedArmoryScreen, "Armory read-model apply must keep the dedicated Armory screen mounted.");
            Assert.IsTrue(shellView.ArmoryCategoryActions[2].ClassListContains("category-selected"), "Armory category read model must select Aircraft.");
            Assert.IsFalse(shellView.ArmoryCategoryActions[0].ClassListContains("category-selected"), "Armory category read model must clear default Units state.");

            gateway.ArmoryCategory = ArmoryCatalogCategory.Buildings;
            applySystem.Update();

            Assert.AreEqual(ArmoryCatalogCategory.Buildings, applySystem.LastArmoryCategory, "Apply system must capture updated Armory category state.");
            Assert.IsTrue(shellView.ArmoryCategoryActions[3].ClassListContains("category-selected"), "Updated Armory category read model must select Buildings.");
            Assert.IsFalse(shellView.ArmoryCategoryActions[2].ClassListContains("category-selected"), "Updated Armory category read model must clear Aircraft.");

            string shellApplySource = File.ReadAllText(ShellApplySystemPath);
            StringAssert.Contains("UiShellRuntimeGateway.TryReadArmoryCategory", shellApplySource, "Apply system must read Armory category through the runtime gateway.");
            StringAssert.Contains("shellView.ApplyArmoryCategory", shellApplySource, "Apply system must apply Armory category through the view edge.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitArmoryScrollUsesRetainedRowsWithoutPerOperationReplacement()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset armoryAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ArmoryUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(armoryAsset, $"Missing Armory UXML asset: {ArmoryUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MainMenu,
                UIRoute.Armory,
                UiShellTransitionPhase.MenuReady,
                74,
                false),
            HasArmoryCategory = true,
            ArmoryCategory = ArmoryCatalogCategory.Characters
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellArmoryRetainedScrollSmoke");
        using World world = new("UiToolkitShellArmoryRetainedScrollSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, armoryAsset, null, null);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(shellView.HasRequiredArmoryRuntimeBindings, "Armory retained-scroll validation requires complete runtime bindings.");
            Assert.IsNotNull(shellView.ArmoryCatalogContent, "Armory catalog content root must be cached.");
            Assert.AreEqual(8, shellView.ArmoryItems.Count, "Armory retained item cache must preserve the eight visible target rows.");

            int initialChildCount = shellView.ArmoryCatalogContent.childCount;
            Button firstItem = shellView.ArmoryItems[0];
            Button secondItem = shellView.ArmoryItems[1];
            Button lockedItem = shellView.ArmoryItems[4];
            Assert.IsNotNull(firstItem, "First Armory row must be retained.");
            Assert.IsNotNull(secondItem, "Second Armory row must be retained.");
            Assert.IsNotNull(lockedItem, "Locked Armory row must be retained.");

            ArmoryCatalogCategory[] categories =
            {
                ArmoryCatalogCategory.Characters,
                ArmoryCatalogCategory.Vehicles,
                ArmoryCatalogCategory.Aircrafts,
                ArmoryCatalogCategory.Buildings,
                ArmoryCatalogCategory.Support
            };

            for (int i = 0; i < categories.Length; i++)
            {
                gateway.ArmoryCategory = categories[i];
                applySystem.Update();

                Assert.AreEqual(initialChildCount, shellView.ArmoryCatalogContent.childCount, $"Armory category update {categories[i]} must not add or remove retained rows.");
                Assert.AreSame(firstItem, shellView.ArmoryItems[0], $"Armory category update {categories[i]} must not replace the first retained row.");
                Assert.AreSame(secondItem, shellView.ArmoryItems[1], $"Armory category update {categories[i]} must not replace the second retained row.");
                Assert.AreSame(lockedItem, shellView.ArmoryItems[4], $"Armory category update {categories[i]} must not replace the locked retained row.");

                Assert.IsTrue(shellView.SelectArmoryItem(1), "Unlocked retained row selection must still work after category updates.");
                Assert.IsTrue(secondItem.ClassListContains("selected"), "Selected retained row must keep selected state.");
                Assert.IsFalse(shellView.SelectArmoryItem(4), "Locked retained row must continue rejecting selection after category updates.");
                Assert.IsFalse(lockedItem.ClassListContains("selected"), "Locked retained row must not gain selected state.");
                Assert.IsTrue(shellView.SelectArmoryItem(0), "Selection must be able to return to the first retained row.");
                Assert.AreEqual(initialChildCount, shellView.ArmoryCatalogContent.childCount, "Repeated item selection must not mutate retained row count.");
            }

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            StringAssert.Contains("private readonly Button[] armoryItems", shellViewSource, "Armory rows must be cached in a retained array.");
            StringAssert.Contains("private readonly EventCallback<ClickEvent>[] armoryItemCallbacks", shellViewSource, "Armory item callbacks must be retained instead of rebuilt per operation.");
            StringAssert.Contains("CacheArmoryItem", shellViewSource, "Armory retained row lookup must stay isolated to mount/cache time.");
            StringAssert.Contains("SelectArmoryItem(0)", shellViewSource, "Armory mount must initialize selection without rebuilding rows.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitArmoryItemSelectionSwapsGeneratedFrameStateClasses()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset armoryAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ArmoryUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(armoryAsset, $"Missing Armory UXML asset: {ArmoryUxmlPath}");

        string uss = File.ReadAllText(ArmoryUssPath);
        string defaultFrameBlock = GetCssBlock(uss, ".item-frame {");
        string selectedFrameBlock = GetCssBlock(uss, ".armory-item-view.selected .item-frame {");
        string lockedFrameBlock = GetCssBlock(uss, ".armory-item-view.locked .item-frame {");
        AssertCssContains(defaultFrameBlock, "scn19_roster_card_default_frame.png", "Default Armory rows must use the generated default frame.");
        AssertCssContains(selectedFrameBlock, "scn19_roster_card_selected_frame.png", "Selected Armory rows must use the generated selected frame.");
        AssertCssContains(lockedFrameBlock, "scn19_roster_card_locked_frame.png", "Locked Armory rows must use the generated locked frame.");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasArmoryCategory = true,
            ArmoryCategory = ArmoryCatalogCategory.Characters
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellArmorySelectedFrameSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, armoryAsset, null, null);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before Armory selected-frame validation.");
            Assert.IsTrue(shellView.HasRequiredArmoryRuntimeBindings, "Armory selected-frame validation requires complete runtime bindings.");

            Button firstItem = shellView.ArmoryItems[0];
            Button secondItem = shellView.ArmoryItems[1];
            Button lockedItem = shellView.ArmoryItems[4];
            Assert.IsNotNull(firstItem, "First Armory row must be cached.");
            Assert.IsNotNull(secondItem, "Second Armory row must be cached.");
            Assert.IsNotNull(lockedItem, "Locked Armory row must be cached.");
            Assert.IsTrue(firstItem.ClassListContains("selected"), "Initial Armory row must start selected for static parity.");
            Assert.IsFalse(firstItem.ClassListContains("default"), "Initial selected row must not also keep the default visual state.");
            Assert.IsFalse(secondItem.ClassListContains("selected"), "Second row must not start selected.");
            Assert.IsFalse(secondItem.ClassListContains("locked"), "Second row must be selectable.");

            Assert.IsTrue(shellView.SelectArmoryItem(1), "Selecting an unlocked Armory row must succeed.");
            Assert.AreEqual(1, shellView.SelectedArmoryItemIndex, "Selected Armory index must update to the clicked row.");
            Assert.IsFalse(firstItem.ClassListContains("selected"), "Previously selected Armory row must remove selected class.");
            Assert.IsTrue(firstItem.ClassListContains("default"), "Previously selected Armory row must restore default frame state.");
            Assert.IsTrue(secondItem.ClassListContains("selected"), "Newly selected Armory row must gain selected class.");
            Assert.IsFalse(secondItem.ClassListContains("default"), "Newly selected Armory row must remove default frame state.");
            Assert.AreEqual("FAST APC", shellView.ArmoryInspectionNameLabel.text, "Inspection title must track the selected row.");
            Assert.AreEqual("VEHICLE", shellView.ArmoryInspectionTypeLabel.text, "Inspection type must track the selected row.");

            Assert.IsFalse(shellView.SelectArmoryItem(4), "Locked Armory row selection must be rejected.");
            Assert.AreEqual(1, shellView.SelectedArmoryItemIndex, "Rejected locked selection must preserve the prior selected row.");
            Assert.IsTrue(lockedItem.ClassListContains("locked"), "Locked Armory row must preserve locked frame state.");
            Assert.IsFalse(lockedItem.ClassListContains("selected"), "Locked Armory row must not gain selected state.");
            Assert.IsTrue(secondItem.ClassListContains("selected"), "Prior selected row must remain selected after locked-row rejection.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitArmoryLockedRowsKeepLockedStateAndRejectUnavailableActions()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset armoryAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ArmoryUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(armoryAsset, $"Missing Armory UXML asset: {ArmoryUxmlPath}");

        string uxml = File.ReadAllText(ArmoryUxmlPath);
        string itemUxml = File.ReadAllText(ArmoryItemUxmlPath);
        string uss = File.ReadAllText(ArmoryUssPath);
        StringAssert.Contains("class=\"armory-item-view item-card locked item-vehicle\"", uxml, "Armory UXML must seed locked vehicle rows.");
        StringAssert.Contains("class=\"armory-item-view item-card locked item-aircraft\"", uxml, "Armory UXML must seed locked aircraft rows.");
        StringAssert.Contains("class=\"item-state-label locked-state\" text=\"LOCKED\"", uxml, "Locked Armory rows must expose a visible LOCKED state label.");
        StringAssert.Contains("class=\"lock-badge\"", uxml, "Locked Armory rows must expose a lock badge.");
        StringAssert.Contains("class=\"item-action-availability available\"", itemUxml, "Retained Armory item template must expose the action availability surface.");
        StringAssert.Contains(".armory-item-view.locked .item-action-icon", uss, "Locked rows must override the action marker to unavailable styling.");
        StringAssert.Contains(".armory-item-view.locked .item-action-label", uss, "Locked rows must override the action text to unavailable styling.");
        StringAssert.Contains("scn19_icon_hold_shield.png", uss, "Locked unavailable marker must use a generated new-art-direction icon.");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasArmoryCategory = true,
            ArmoryCategory = ArmoryCatalogCategory.Characters
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellArmoryLockedRowsSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, armoryAsset, null, null);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before Armory locked-row validation.");
            Assert.IsTrue(shellView.MountArmoryScreen(), "Armory locked-row validation must mount the Armory screen explicitly.");
            Assert.IsTrue(shellView.HasRequiredArmoryRuntimeBindings, "Armory locked-row validation requires complete runtime bindings.");
            Assert.IsTrue(shellView.SelectArmoryItem(1), "Unlocked setup selection must succeed before locked-row rejection checks.");
            Assert.AreEqual("FAST APC", shellView.ArmoryInspectionNameLabel.text, "Setup selection title mismatch.");
            Assert.AreEqual("VEHICLE", shellView.ArmoryInspectionTypeLabel.text, "Setup selection type mismatch.");

            int[] lockedIndexes = { 4, 5, 6, 7 };
            for (int i = 0; i < lockedIndexes.Length; i++)
            {
                int index = lockedIndexes[i];
                Button lockedItem = shellView.ArmoryItems[index];
                Assert.IsNotNull(lockedItem, $"Locked Armory row {index} must be cached.");
                Assert.IsTrue(lockedItem.ClassListContains("locked"), $"Locked Armory row {index} must keep the locked class.");
                Assert.IsFalse(lockedItem.ClassListContains("selected"), $"Locked Armory row {index} must not start selected.");
                Assert.AreEqual("LOCKED", lockedItem.Q<Label>("StateLabel")?.text, $"Locked Armory row {index} must show LOCKED state text.");
                Assert.IsTrue(lockedItem.Q<VisualElement>("Badge")?.ClassListContains("lock-badge") ?? false, $"Locked Armory row {index} must keep the lock badge.");

                Assert.IsFalse(shellView.SelectArmoryItem(index), $"Locked Armory row {index} must reject selection/action trigger.");
                Assert.AreEqual(1, shellView.SelectedArmoryItemIndex, $"Locked Armory row {index} must not replace the selected row.");
                Assert.IsFalse(lockedItem.ClassListContains("selected"), $"Locked Armory row {index} must not gain selected frame state.");
                Assert.IsFalse(lockedItem.ClassListContains("default"), $"Locked Armory row {index} must not fall back to the default unlocked frame state.");
                Assert.AreEqual("FAST APC", shellView.ArmoryInspectionNameLabel.text, $"Locked Armory row {index} must not update the inspection title.");
                Assert.AreEqual("VEHICLE", shellView.ArmoryInspectionTypeLabel.text, $"Locked Armory row {index} must not update the inspection type.");
                Assert.AreEqual(0, gateway.UiActionRequests.Count, $"Locked Armory row {index} must not enqueue unavailable UI actions.");
            }

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            StringAssert.Contains("if (item == null || item.ClassListContains(\"locked\"))\n            return false;", shellViewSource, "Armory selection must reject locked rows before mutating selection, labels, or actions.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewAppliesMainMenuPresentationCommands()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset commanderAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CommanderProfileUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(commanderAsset, $"Missing Commander/Profile UXML asset: {CommanderProfileUxmlPath}");

        GameObject host = new("UiToolkitShellMainMenuCommandSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, null, null, null, commanderAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before applying Main Menu commands.");
            Assert.IsTrue(shellView.HasMountedMainMenuScreen, "Main Menu content must be mounted before command presentation.");
            Assert.IsTrue(shellView.HasPersistentMainMenuHeader, "Mounted Main Menu content must expose a visible persistent header.");

            var commands = new List<UiShellPresentationCommandModel>
            {
                new(
                    UiShellCommandKind.ExitMenu,
                    UiShellRegionId.None,
                    UIRoute.Match,
                    UiShellMode.Loading,
                    41)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsTrue(shellView.MainMenuScreenSlot.ClassListContains("shell-hidden"), "ExitMenu must hide the retained Main Menu screen slot.");
            Assert.IsTrue(shellView.MainMenuScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.ScaleOut)), "ExitMenu must apply the scale-out motion state.");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.EnterMenu,
                UiShellRegionId.None,
                UIRoute.MainMenu,
                UiShellMode.MainMenu,
                42);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsFalse(shellView.MainMenuScreenSlot.ClassListContains("shell-hidden"), "EnterMenu must reveal the retained Main Menu screen slot.");
            Assert.IsTrue(shellView.MainMenuScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)), "EnterMenu must apply the visible motion state.");
            Assert.IsFalse(shellView.IsCommanderProfileSubRouteVisible, "Root Main Menu route must keep the commander/profile sub-route slot hidden.");
            Assert.IsTrue(shellView.MainMenuContentRoot.ClassListContains("main-menu-route-root"), "Root Main Menu route must apply the root route class.");
            VisualElement headerBeforeSwap = shellView.MainMenuHeaderContent;
            Button inboxBeforeSwap = shellView.MainMenuContentRoot.Q<Button>("InboxButton");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.SwapMenuMiddle,
                UiShellRegionId.MiddleRegion,
                UIRoute.Armory,
                UiShellMode.MainMenu,
                43);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsFalse(shellView.MainMenuScreenSlot.ClassListContains("shell-hidden"), "Menu sub-route swaps must keep Main Menu content visible until a dedicated replacement screen is mounted.");
            Assert.AreEqual(1, shellView.MainMenuScreenSlot.childCount, "Menu sub-route swaps must not recreate or duplicate the Main Menu UXML tree.");
            Assert.IsTrue(shellView.HasPersistentMainMenuHeader, "Menu sub-route swaps must keep the persistent header visible.");
            Assert.AreSame(headerBeforeSwap, shellView.MainMenuHeaderContent, "Menu sub-route swaps must keep the same header instance mounted.");
            Assert.AreSame(inboxBeforeSwap, shellView.MainMenuContentRoot.Q<Button>("InboxButton"), "Header action bindings must not be recreated during a middle-region swap.");
            Assert.IsTrue(RequireButton(shellView.MainMenuContentRoot, "Nav_Armory").ClassListContains("nav-item-selected"), "Armory sub-route must select the Armory navigation item.");
            Assert.IsFalse(RequireButton(shellView.MainMenuContentRoot, "Nav_Campaign").ClassListContains("nav-item-selected"), "Armory sub-route must not keep Campaign selected.");
            Assert.IsTrue(shellView.MainMenuContentRoot.ClassListContains("main-menu-route-armory"), "Armory sub-route must apply the Armory route class.");
            Assert.IsFalse(shellView.IsCommanderProfileSubRouteVisible, "Armory sub-route must not show the commander/profile screen slot.");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.SwapMenuMiddle,
                UiShellRegionId.MiddleRegion,
                UIRoute.CommandFeed,
                UiShellMode.MainMenu,
                44);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.AreSame(headerBeforeSwap, shellView.MainMenuHeaderContent, "Commander/profile route must keep the same persistent header instance.");
            Assert.IsTrue(shellView.HasPersistentMainMenuHeader, "Commander/profile route must keep the persistent header visible.");
            Assert.IsTrue(shellView.IsCommanderProfileSubRouteVisible, "Commander/profile route must reveal the commander/profile screen slot.");
            Assert.IsTrue(RequireButton(shellView.MainMenuContentRoot, "Nav_Profile").ClassListContains("nav-item-selected"), "Commander/profile route must select the Profile navigation item.");
            Assert.IsTrue(shellView.MainMenuContentRoot.ClassListContains("main-menu-route-profile"), "Commander/profile route must apply the profile route class.");
            Assert.IsFalse(RequireButton(shellView.MainMenuContentRoot, "Nav_Armory").ClassListContains("nav-item-selected"), "Commander/profile route must clear Armory navigation selection.");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.SwapMenuMiddle,
                UiShellRegionId.MiddleRegion,
                UIRoute.MainMenu,
                UiShellMode.MainMenu,
                45);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.AreSame(headerBeforeSwap, shellView.MainMenuHeaderContent, "Returning to the root Main Menu must still keep the same header instance.");
            Assert.IsFalse(shellView.IsCommanderProfileSubRouteVisible, "Returning to the root Main Menu must hide the commander/profile screen slot.");
            Assert.IsTrue(RequireButton(shellView.MainMenuContentRoot, "Nav_Campaign").ClassListContains("nav-item-selected"), "Root Main Menu route must restore Campaign navigation selection.");
            Assert.IsFalse(RequireButton(shellView.MainMenuContentRoot, "Nav_Armory").ClassListContains("nav-item-selected"), "Root Main Menu route must clear Armory navigation selection.");
            Assert.IsFalse(RequireButton(shellView.MainMenuContentRoot, "Nav_Profile").ClassListContains("nav-item-selected"), "Root Main Menu route must clear Profile navigation selection.");
            Assert.IsTrue(shellView.MainMenuContentRoot.ClassListContains("main-menu-route-root"), "Returning to the root Main Menu must restore the root route class.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitMainMenuActionsEnqueueShellRouteRequests()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset commanderAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CommanderProfileUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(commanderAsset, $"Missing Commander/Profile UXML asset: {CommanderProfileUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway();
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMainMenuActionSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, null, null, null, commanderAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating Main Menu actions.");
            Assert.IsTrue(shellView.HasRequiredMainMenuBindings, "Main Menu action bindings must be present before click routing.");

            (string Name, UiShellRouteIntent Intent, UIRoute Route, bool PushHistory)[] expected =
            {
                ("DeployOperationButton", UiShellRouteIntent.EnterMatch, UIRoute.Match, false),
                ("SettingsButton", UiShellRouteIntent.OpenSettings, UIRoute.Settings, true),
                ("InboxButton", UiShellRouteIntent.OpenMenuRoute, UIRoute.Inbox, true),
                ("MenuButton", UiShellRouteIntent.OpenMenuRoute, UIRoute.MainMenu, false),
                ("Nav_Campaign", UiShellRouteIntent.OpenMenuRoute, UIRoute.MainMenu, false),
                ("Nav_Armory", UiShellRouteIntent.OpenMenuRoute, UIRoute.Armory, true),
                ("Nav_Supply", UiShellRouteIntent.OpenMenuRoute, UIRoute.LoadoutSquadPrep, true),
                ("Nav_Command", UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandExchange, true),
                ("Nav_TechTree", UiShellRouteIntent.OpenMenuRoute, UIRoute.Events, true),
                ("Nav_Profile", UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandFeed, true),
                ("Card_Campaign", UiShellRouteIntent.OpenMenuRoute, UIRoute.MainMenu, false),
                ("Card_Skirmish", UiShellRouteIntent.OpenMenuRoute, UIRoute.QuickCustomSetup, true),
                ("Card_Operations", UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandExchange, true),
                ("CommanderPanel", UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandFeed, true)
            };

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsNotNull(RequireButton(shellView.MainMenuContentRoot, expected[i].Name), $"Missing button binding for {expected[i].Name}.");
                Assert.IsTrue(shellView.TrySubmitMainMenuAction(expected[i].Name), $"Main Menu action did not submit: {expected[i].Name}");
            }

            Assert.AreEqual(expected.Length, gateway.RouteRequests.Count, "Every Main Menu action click must enqueue exactly one shell route request.");
            for (int i = 0; i < expected.Length; i++)
            {
                RecordedRouteRequest actual = gateway.RouteRequests[i];
                Assert.AreEqual(expected[i].Intent, actual.Intent, $"{expected[i].Name} intent mismatch.");
                Assert.AreEqual(expected[i].Route, actual.Route, $"{expected[i].Name} route mismatch.");
                Assert.AreEqual(expected[i].PushHistory, actual.PushHistory, $"{expected[i].Name} push-history mismatch.");
            }
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesMainMenuSelectedStateFromShellReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset commanderAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CommanderProfileUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(commanderAsset, $"Missing Commander/Profile UXML asset: {CommanderProfileUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MainMenu,
                UIRoute.CommandFeed,
                UiShellTransitionPhase.MenuReady,
                51,
                false)
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMainMenuReadModelSmoke");
        using World world = new("UiToolkitShellMainMenuReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, null, null, null, commanderAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasShellState, "Apply system must read shell state from the runtime gateway.");
            Assert.AreEqual(UIRoute.CommandFeed, applySystem.LastShellState.ActiveRoute, "Fake shell state route was not captured.");
            Assert.IsTrue(shellView.IsCommanderProfileSubRouteVisible, "Shell read model CommandFeed route must reveal the commander/profile slot.");
            Assert.IsTrue(RequireButton(shellView.MainMenuContentRoot, "Nav_Profile").ClassListContains("nav-item-selected"), "Shell read model CommandFeed route must select Profile.");
            Assert.IsTrue(shellView.MainMenuContentRoot.ClassListContains("main-menu-route-profile"), "Shell read model CommandFeed route must apply the profile route class.");

            gateway.ShellState = new UiShellStateModel(
                UiShellMode.MainMenu,
                UIRoute.Armory,
                UiShellTransitionPhase.MenuReady,
                52,
                false);
            applySystem.Update();

            Assert.AreEqual(UIRoute.Armory, applySystem.LastShellState.ActiveRoute, "Updated fake shell state route was not captured.");
            Assert.IsFalse(shellView.IsCommanderProfileSubRouteVisible, "Shell read model Armory route must hide the commander/profile slot.");
            Assert.IsTrue(RequireButton(shellView.MainMenuContentRoot, "Nav_Armory").ClassListContains("nav-item-selected"), "Shell read model Armory route must select Armory.");
            Assert.IsFalse(RequireButton(shellView.MainMenuContentRoot, "Nav_Profile").ClassListContains("nav-item-selected"), "Shell read model Armory route must clear Profile.");
            Assert.IsTrue(shellView.MainMenuContentRoot.ClassListContains("main-menu-route-armory"), "Shell read model Armory route must apply the Armory route class.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void MatchHudUxmlExposesCanvasParityBindings()
    {
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        VisualTreeAsset passengerItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudPassengerItemUxmlPath);
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");
        Assert.IsNotNull(passengerItemAsset, $"Missing Match HUD passenger item UXML asset: {MatchHudPassengerItemUxmlPath}");

        string uxml = File.ReadAllText(MatchHudUxmlPath);
        string passengerItemUxml = File.ReadAllText(MatchHudPassengerItemUxmlPath);
        string uss = File.ReadAllText(MatchHudUssPath);

        string[] requiredNames =
        {
            "SCN08_MatchHudContent",
            "HeaderContent",
            "LogoPanel",
            "CurrentOrderBanner",
            "ResourceStrip",
            "MenuButton",
            "LeftContent",
            "ObjectivesPanel",
            "SelectedSquadPanel",
            "Badge",
            "Title",
            "Subtitle",
            "PortraitFrame",
            "Portrait",
            "HealthFrame",
            "HealthFill",
            "HealthText",
            "OrderRow",
            "OrderLabel",
            "OrderValue",
            "CommandButtons",
            "ReturnButton",
            "DestroyButton",
            "BoardButton",
            "PassengerChip",
            "TransportPassengerDrawer",
            "EmptyState",
            "Scroll_View",
            "Content",
            "ExitAllButton",
            "CloseButton",
            "RightContent",
            "ThreatJumpPanel",
            "JumpButton",
            "RightQuickRail",
            "PauseButton",
            "SettingsButton",
            "RightBuildCommand",
            "RightSupportCommand",
            "FooterContent",
            "SquadTray",
            "CommandRail",
            "SelectCommand",
            "MoveCommand",
            "AttackCommand",
            "HoldCommand",
            "StopCommand",
            "BuildCommand",
            "ScanCommand",
            "SupportCommand",
            "MinimapPanel",
            "Map",
            "Viewport",
            "ZoomIn",
            "ZoomOut",
            "ZoomFocus",
            "FeedbackPanel",
            "Feedback",
            "Actions",
            "BoardAllButton",
            "CancelButton"
        };

        for (int i = 0; i < requiredNames.Length; i++)
            StringAssert.Contains($"name=\"{requiredNames[i]}\"", uxml, $"Match HUD UXML missing Canvas parity binding element: {requiredNames[i]}");

        string[] requiredButtons =
        {
            "MenuButton",
            "ReturnButton",
            "DestroyButton",
            "BoardButton",
            "PassengerChip",
            "ExitAllButton",
            "CloseButton",
            "JumpButton",
            "PauseButton",
            "SettingsButton",
            "RightBuildCommand",
            "RightSupportCommand",
            "SquadCard1",
            "SquadCard2",
            "SquadCard3",
            "SquadCard4",
            "SquadCard5",
            "SelectCommand",
            "MoveCommand",
            "AttackCommand",
            "HoldCommand",
            "StopCommand",
            "BuildCommand",
            "ScanCommand",
            "SupportCommand",
            "ZoomIn",
            "ZoomOut",
            "ZoomFocus",
            "BoardAllButton",
            "CancelButton"
        };

        for (int i = 0; i < requiredButtons.Length; i++)
            StringAssert.Contains($"<ui:Button name=\"{requiredButtons[i]}\"", uxml, $"Match HUD actionable element must remain a UI Toolkit Button: {requiredButtons[i]}");

        for (int i = 1; i <= 5; i++)
            StringAssert.Contains($"name=\"SquadCard{i}\"", uxml, $"Match HUD must keep five squad tray buttons. Missing SquadCard{i}.");

        string[] commandLabels =
        {
            "SELECT",
            "MOVE",
            "ATTACK",
            "HOLD",
            "STOP",
            "BUILD",
            "SCAN",
            "SUPPORT"
        };

        for (int i = 0; i < commandLabels.Length; i++)
            StringAssert.Contains($"text=\"{commandLabels[i]}\"", uxml, $"Match HUD command rail missing label: {commandLabels[i]}");

        StringAssert.Contains("<ui:Template name=\"PassengerItemViewTemplate\"", uxml, "Match HUD must declare a reusable passenger item template.");
        StringAssert.Contains("<ui:Instance name=\"PassengerItemView\" template=\"PassengerItemViewTemplate\"", uxml, "Passenger drawer must include a retained passenger item instance for runtime binding.");
        StringAssert.Contains("horizontal-scroller-visibility=\"Hidden\"", uxml, "Passenger drawer must preserve hidden Canvas scrollbar behavior.");
        StringAssert.Contains("vertical-scroller-visibility=\"Hidden\"", uxml, "Passenger drawer must preserve hidden Canvas scrollbar behavior.");

        string[] passengerItemNames =
        {
            "PassengerItemView",
            "Portrait",
            "Name",
            "Role",
            "HealthFrame",
            "HealthFill",
            "Health",
            "ExitButton"
        };

        for (int i = 0; i < passengerItemNames.Length; i++)
            StringAssert.Contains($"name=\"{passengerItemNames[i]}\"", passengerItemUxml, $"Passenger item UXML missing binding element: {passengerItemNames[i]}");

        StringAssert.Contains("<ui:Button name=\"ExitButton\"", passengerItemUxml, "Passenger item exit action must remain a UI Toolkit Button.");
        StringAssert.Contains("TargetLockV02", uss, "Match HUD USS must use the approved new-art-direction V02 asset set.");
    }

    [Test]
    public void UiToolkitShellViewMountsMatchHudUxmlIntoMatchSlot()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");

        GameObject host = new("UiToolkitShellMatchHudMountSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Match HUD content.");
            Assert.AreSame(matchHudAsset, shellView.MatchHudScreenAsset, "Configured Match HUD asset must be retained by the shell view.");
            Assert.IsFalse(shellView.HasMountedMatchHudScreen, "Shell mount must not eagerly instantiate the Match HUD while Main Menu is active.");

            Assert.IsTrue(shellView.Mount(), "Repeated shell mount must remain stable.");
            Assert.AreEqual(0, shellView.MatchScreenSlot.childCount, "Repeated shell mount must not create hidden Match HUD content before EnterMatchHud.");

            var commands = new List<UiShellPresentationCommandModel>
            {
                new(
                    UiShellCommandKind.EnterMatchHud,
                    UiShellRegionId.None,
                    UIRoute.Match,
                    UiShellMode.MatchHud,
                    71)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsTrue(shellView.HasMountedMatchHudScreen, "EnterMatchHud must mount the configured Match HUD UXML into MatchScreenSlot.");
            Assert.IsTrue(shellView.HasRequiredMatchHudBindings, "Mounted Match HUD UXML must bind the Phase 4 root regions and command surface.");
            Assert.IsNotNull(shellView.MatchHudContentRoot, "Mounted Match HUD content root must be cached.");
            Assert.AreEqual("SCN08_MatchHudContent", shellView.MatchHudContentRoot.name, "Mounted Match HUD content must keep its binding root name.");
            Assert.AreEqual(1, shellView.MatchScreenSlot.childCount, "Match screen slot must contain a single Match HUD UXML tree.");
            Assert.IsNotNull(shellView.MatchHudContentRoot.Q<Button>("SelectCommand"), "Mounted Match HUD UXML must expose the select command binding.");
            Assert.IsNotNull(shellView.MatchHudContentRoot.Q<Button>("BuildCommand"), "Mounted Match HUD UXML must expose the build command binding.");
            Assert.IsNotNull(shellView.MatchHudContentRoot.Q<VisualElement>("TransportPassengerDrawer"), "Mounted Match HUD UXML must expose the passenger drawer binding.");
            Assert.IsFalse(shellView.MatchScreenSlot.ClassListContains("shell-hidden"), "EnterMatchHud must reveal the retained Match HUD screen slot.");
            Assert.IsTrue(shellView.MatchScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)), "EnterMatchHud must apply the visible motion state.");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.ExitMatchHud,
                UiShellRegionId.None,
                UIRoute.MainMenu,
                UiShellMode.Loading,
                72);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands));
            Assert.IsTrue(shellView.MatchScreenSlot.ClassListContains("shell-hidden"), "ExitMatchHud must hide the retained Match HUD screen slot.");
            Assert.IsTrue(shellView.MatchScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.ScaleOut)), "ExitMatchHud must apply the scale-out motion state.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasMountedMatchHudScreen, "ClearCache must clear mounted Match HUD state.");
            Assert.IsNull(shellView.MatchHudContentRoot, "ClearCache must clear Match HUD content root.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitMatchHudActionsEnqueueUiActionRequests()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");

        (string Name, UiActionKind Kind, int PayloadId)[] expected =
        {
            ("MenuButton", UiActionKind.MatchMenu, 0),
            ("ReturnButton", UiActionKind.ReturnSelection, 0),
            ("DestroyButton", UiActionKind.DestroySelection, 0),
            ("BoardButton", UiActionKind.BoardSelection, 0),
            ("PassengerChip", UiActionKind.TogglePassengerDrawer, 0),
            ("ExitAllButton", UiActionKind.ExitAllPassengers, 0),
            ("CloseButton", UiActionKind.ClosePassengerDrawer, 0),
            ("JumpButton", UiActionKind.JumpToThreat, 0),
            ("PauseButton", UiActionKind.Pause, 0),
            ("SettingsButton", UiActionKind.OpenSettings, 0),
            ("RightBuildCommand", UiActionKind.RightBuild, 0),
            ("RightSupportCommand", UiActionKind.RightSupport, 0),
            ("SquadCard1", UiActionKind.SquadSlot1, 1),
            ("SquadCard2", UiActionKind.SquadSlot2, 2),
            ("SquadCard3", UiActionKind.SquadSlot3, 3),
            ("SquadCard4", UiActionKind.SquadSlot4, 4),
            ("SquadCard5", UiActionKind.SquadSlot5, 5),
            ("SelectCommand", UiActionKind.Select, 0),
            ("MoveCommand", UiActionKind.Move, 0),
            ("AttackCommand", UiActionKind.Attack, 0),
            ("HoldCommand", UiActionKind.Hold, 0),
            ("StopCommand", UiActionKind.Stop, 0),
            ("BuildCommand", UiActionKind.Build, 0),
            ("ScanCommand", UiActionKind.Scan, 0),
            ("SupportCommand", UiActionKind.Support, 0),
            ("ZoomIn", UiActionKind.MinimapZoomIn, 0),
            ("ZoomOut", UiActionKind.MinimapZoomOut, 0),
            ("ZoomFocus", UiActionKind.MinimapFocus, 0),
            ("BoardAllButton", UiActionKind.BoardAll, 0),
            ("CancelButton", UiActionKind.CancelFeedback, 0)
        };

        var gateway = new RecordingUiShellRuntimeGateway();
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMatchHudActionsSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating Match HUD action requests.");
            Assert.IsTrue(shellView.MountMatchHudScreen(), "Match HUD action validation must mount the Match HUD screen explicitly.");
            Assert.IsTrue(shellView.HasRequiredMatchHudBindings, "Match HUD action bindings must be present before click routing.");

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsNotNull(RequireButton(shellView.MatchHudContentRoot, expected[i].Name), $"Missing Match HUD button binding for {expected[i].Name}.");
                StringAssert.Contains($"RegisterMatchHudAction(\"{expected[i].Name}\", UiActionKind.{expected[i].Kind}", shellViewSource, $"Match HUD mount must register the {expected[i].Name} callback.");
                Assert.IsTrue(shellView.TrySubmitMatchHudAction(expected[i].Kind, expected[i].PayloadId), $"Match HUD action did not submit: {expected[i].Name}");
            }

            Assert.AreEqual(expected.Length, gateway.UiActionRequests.Count, "Every Match HUD action must enqueue exactly one UI action request.");
            for (int i = 0; i < expected.Length; i++)
            {
                RecordedUiActionRequest actual = gateway.UiActionRequests[i];
                Assert.AreEqual(expected[i].Kind, actual.Kind, $"{expected[i].Name} action mismatch.");
                Assert.AreEqual(expected[i].PayloadId, actual.PayloadId, $"{expected[i].Name} payload mismatch.");
            }
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesMatchHudSelectionReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MatchHud,
                UIRoute.Match,
                UiShellTransitionPhase.MatchHudReady,
                81,
                false),
            HasMatchHudSelection = true,
            MatchHudSelection = new UiMatchHudSelectionPanelModel(
                true,
                "FAST APC",
                "VEHICLE | TRANSPORT",
                "MOVING",
                "180 / 240",
                0.75f,
                false,
                true,
                true,
                true)
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMatchHudSelectionReadModelSmoke");
        using World world = new("UiToolkitShellMatchHudSelectionReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasMatchHudSelection, "Apply system must read Match HUD selection state from the runtime gateway.");
            Assert.AreEqual("FAST APC", applySystem.LastMatchHudSelection.Title, "Apply system did not capture selected title.");
            Assert.IsTrue(shellView.HasRequiredMatchHudBindings, "Mounted Match HUD must expose selected panel read-model targets.");
            Assert.IsFalse(shellView.MatchHudSelectedPanel.ClassListContains("shell-hidden"), "Visible selection read model must show the selected panel.");
            Assert.AreEqual("FAST APC", shellView.MatchHudSelectedTitleLabel.text, "Selected title must come from the read model.");
            Assert.AreEqual("VEHICLE | TRANSPORT", shellView.MatchHudSelectedSubtitleLabel.text, "Selected subtitle must come from the read model.");
            Assert.AreEqual("MOVING", shellView.MatchHudSelectedOrderValueLabel.text, "Selected order must come from the read model.");
            Assert.AreEqual("180 / 240", shellView.MatchHudSelectedHealthTextLabel.text, "Selected health text must come from the read model.");
            AssertPercentWidth(shellView.MatchHudSelectedHealthFill, 75f, "Selected health fill");
            Assert.IsTrue(shellView.MatchHudSelectedBadge.ClassListContains("shell-hidden"), "Vehicle selections must hide the character badge.");
            Assert.IsTrue(shellView.MatchHudSelectedReturnAction.enabledSelf, "Return action must reflect read-model availability.");
            Assert.IsTrue(shellView.MatchHudSelectedDestroyAction.enabledSelf, "Destroy action must reflect read-model availability.");
            Assert.IsTrue(shellView.MatchHudSelectedBoardAction.enabledSelf, "Board action must reflect transport read-model availability.");

            gateway.MatchHudSelection = UiMatchHudSelectionPanelModel.Hidden;
            applySystem.Update();

            Assert.IsTrue(shellView.MatchHudSelectedPanel.ClassListContains("shell-hidden"), "Hidden selection read model must deactivate the selected panel.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesMatchHudCommandStateReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MatchHud,
                UIRoute.Match,
                UiShellTransitionPhase.MatchHudReady,
                82,
                false),
            HasMatchHudCommandState = true,
            MatchHudCommandState = new UiMatchHudCommandStateModel(TacticalCommandMode.Move, false)
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMatchHudCommandStateReadModelSmoke");
        using World world = new("UiToolkitShellMatchHudCommandStateReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasMatchHudCommandState, "Apply system must read Match HUD command state from the runtime gateway.");
            Assert.AreEqual(TacticalCommandMode.Move, applySystem.LastMatchHudCommandState.ActiveCommandMode, "Apply system did not capture active command mode.");
            Assert.IsTrue(shellView.MatchHudMoveCommand.ClassListContains("command-button-selected"), "Move command must select from active command mode.");
            Assert.IsFalse(shellView.MatchHudSelectCommand.ClassListContains("command-button-selected"), "Select command must clear when Move is active.");
            Assert.IsFalse(shellView.MatchHudBuildCommand.ClassListContains("command-button-selected"), "Build command must stay clear when drawer is closed.");
            Assert.IsFalse(shellView.MatchHudRightBuildCommand.ClassListContains("quick-command-selected"), "Right Build command must stay clear when drawer is closed.");

            gateway.MatchHudCommandState = new UiMatchHudCommandStateModel(TacticalCommandMode.None, true);
            applySystem.Update();

            Assert.IsFalse(shellView.MatchHudMoveCommand.ClassListContains("command-button-selected"), "Move command must clear when active mode is None.");
            Assert.IsTrue(shellView.MatchHudBuildCommand.ClassListContains("command-button-selected"), "Build command must stay selected while the drawer is open.");
            Assert.IsTrue(shellView.MatchHudRightBuildCommand.ClassListContains("quick-command-selected"), "Right Build command must stay selected while the drawer is open.");

            gateway.MatchHudCommandState = default;
            applySystem.Update();

            Assert.IsFalse(shellView.MatchHudBuildCommand.ClassListContains("command-button-selected"), "Build command must clear when the drawer closes.");
            Assert.IsFalse(shellView.MatchHudRightBuildCommand.ClassListContains("quick-command-selected"), "Right Build command must clear when the drawer closes.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesMatchHudPassengerDrawerReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MatchHud,
                UIRoute.Match,
                UiShellTransitionPhase.MatchHudReady,
                83,
                false),
            HasMatchHudPassengerDrawer = true,
            MatchHudPassengerDrawer = new UiMatchHudPassengerDrawerModel(
                true,
                true,
                2,
                4,
                2,
                new UiMatchHudPassengerRowModel("RIFLEMAN", "ONBOARD", "80 / 100", 0.8f),
                new UiMatchHudPassengerRowModel("ENGINEER", "ONBOARD", "45 / 90", 0.5f),
                default)
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMatchHudPassengerDrawerReadModelSmoke");
        using World world = new("UiToolkitShellMatchHudPassengerDrawerReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasMatchHudPassengerDrawer, "Apply system must read Match HUD passenger drawer state from the runtime gateway.");
            Assert.AreEqual(2, applySystem.LastMatchHudPassengerDrawer.PassengerCount, "Apply system did not capture passenger count.");
            Assert.IsTrue(shellView.HasRequiredMatchHudBindings, "Mounted Match HUD must expose passenger drawer read-model targets.");
            Assert.IsTrue(shellView.MatchHudPassengerChip.ClassListContains("passenger-chip-visible"), "Transport-capable selections must show the passenger chip.");
            Assert.IsTrue(shellView.MatchHudPassengerDrawer.ClassListContains("transport-passenger-drawer-visible"), "Open passenger drawer read model must show the drawer.");
            Assert.AreEqual("PASSENGERS 2/4", shellView.MatchHudPassengerChipLabel.text, "Passenger chip must show count and capacity.");
            Assert.AreEqual("PASSENGERS 2/4", shellView.MatchHudPassengerDrawerHeaderLabel.text, "Passenger drawer header must show count and capacity.");
            Assert.IsTrue(shellView.MatchHudPassengerEmptyState.ClassListContains("shell-hidden"), "Passenger empty state must hide when rows are present.");
            Assert.AreEqual("RIFLEMAN", shellView.MatchHudPassengerNameLabels[0].text, "Passenger row 1 name must come from read model.");
            Assert.AreEqual("ONBOARD", shellView.MatchHudPassengerRoleLabels[0].text, "Passenger row 1 role must come from read model.");
            Assert.AreEqual("80 / 100", shellView.MatchHudPassengerHealthLabels[0].text, "Passenger row 1 health must come from read model.");
            AssertPercentWidth(shellView.MatchHudPassengerHealthFills[0], 80f, "Passenger row 1 health fill");
            Assert.AreEqual("ENGINEER", shellView.MatchHudPassengerNameLabels[1].text, "Passenger row 2 name must come from read model.");
            AssertPercentWidth(shellView.MatchHudPassengerHealthFills[1], 50f, "Passenger row 2 health fill");
            Assert.IsTrue(shellView.MatchHudPassengerRows[2].ClassListContains("shell-hidden"), "Unused passenger rows must stay hidden.");

            gateway.MatchHudPassengerDrawer = UiMatchHudPassengerDrawerModel.Hidden;
            applySystem.Update();

            Assert.IsFalse(shellView.MatchHudPassengerChip.ClassListContains("passenger-chip-visible"), "Hidden drawer read model must hide the passenger chip.");
            Assert.IsFalse(shellView.MatchHudPassengerDrawer.ClassListContains("transport-passenger-drawer-visible"), "Hidden drawer read model must hide the drawer.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesMatchHudHeaderReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MatchHud,
                UIRoute.Match,
                UiShellTransitionPhase.MatchHudReady,
                83,
                false),
            HasMatchHudHeader = true,
            MatchHudHeader = new UiMatchHudHeaderModel(
                "ATTACK ORDER",
                "ARMOR WING",
                "221,900",
                "3,120",
                "101/140",
                "LOW")
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMatchHudHeaderReadModelSmoke");
        using World world = new("UiToolkitShellMatchHudHeaderReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasMatchHudHeader, "Apply system must read Match HUD header/resource state from the runtime gateway.");
            Assert.AreEqual("ATTACK ORDER", applySystem.LastMatchHudHeader.OrderText, "Apply system did not capture order text.");
            Assert.IsTrue(shellView.HasRequiredMatchHudBindings, "Mounted Match HUD must expose header/resource read-model targets.");
            Assert.AreEqual("ATTACK ORDER", shellView.MatchHudOrderTextLabel.text, "Order text must come from read model.");
            Assert.AreEqual("ARMOR WING", shellView.MatchHudSquadTextLabel.text, "Squad text must come from read model.");
            Assert.AreEqual("221,900", shellView.MatchHudCreditsValueLabel.text, "Credits text must come from read model.");
            Assert.AreEqual("3,120", shellView.MatchHudFuelValueLabel.text, "Fuel text must come from read model.");
            Assert.AreEqual("101/140", shellView.MatchHudSupplyValueLabel.text, "Supply text must come from read model.");
            Assert.AreEqual("LOW", shellView.MatchHudCivilianRiskValueLabel.text, "Civilian risk text must come from read model.");

            gateway.MatchHudHeader = UiMatchHudHeaderModel.Default;
            applySystem.Update();

            Assert.AreEqual("MOVE ORDER", shellView.MatchHudOrderTextLabel.text, "Default header model must restore order text.");
            Assert.AreEqual("MED", shellView.MatchHudCivilianRiskValueLabel.text, "Default header model must restore civilian risk text.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesMatchHudStatusSurfacesReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MatchHud,
                UIRoute.Match,
                UiShellTransitionPhase.MatchHudReady,
                85,
                false),
            HasMatchHudStatusSurfaces = true,
            MatchHudStatusSurfaces = new UiMatchHudStatusSurfacesModel(
                "PRIMARY TASKS",
                new UiMatchHudObjectiveRowModel("Secure depot", UiMatchHudObjectiveIconKind.Checked),
                new UiMatchHudObjectiveRowModel("Protect convoy", UiMatchHudObjectiveIconKind.Unchecked),
                new UiMatchHudObjectiveRowModel("Bonus clear", UiMatchHudObjectiveIconKind.Star),
                "ELAPSED: 12:34",
                true,
                "RADAR CONTACT",
                "North road, 85m",
                false,
                true,
                "Board transport ready",
                true,
                false,
                false,
                false)
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMatchHudStatusSurfacesReadModelSmoke");
        using World world = new("UiToolkitShellMatchHudStatusSurfacesReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasMatchHudStatusSurfaces, "Apply system must read Match HUD objective/threat/feedback state from the runtime gateway.");
            Assert.AreEqual("PRIMARY TASKS", applySystem.LastMatchHudStatusSurfaces.ObjectivesTitle, "Apply system did not capture objective title.");
            Assert.IsTrue(shellView.HasRequiredMatchHudBindings, "Mounted Match HUD must expose objectives/threat/feedback read-model targets.");
            Assert.AreEqual("PRIMARY TASKS", shellView.MatchHudObjectivesTitleLabel.text);
            Assert.AreEqual("Secure depot", shellView.MatchHudObjective0Label.text);
            Assert.IsTrue(shellView.MatchHudObjective0Icon.ClassListContains("objective-checked"));
            Assert.AreEqual("Protect convoy", shellView.MatchHudObjective1Label.text);
            Assert.IsTrue(shellView.MatchHudObjective1Icon.ClassListContains("objective-unchecked"));
            Assert.AreEqual("Bonus clear", shellView.MatchHudObjective2Label.text);
            Assert.IsTrue(shellView.MatchHudObjective2Icon.ClassListContains("objective-star"));
            Assert.AreEqual("ELAPSED: 12:34", shellView.MatchHudObjectivesElapsedLabel.text);
            Assert.AreEqual("RADAR CONTACT", shellView.MatchHudThreatTitleLabel.text);
            Assert.AreEqual("North road, 85m", shellView.MatchHudThreatSubtitleLabel.text);
            Assert.IsFalse(shellView.MatchHudThreatJumpAction.enabledSelf, "Jump action enabled state must come from read model.");
            Assert.AreEqual("Board transport ready", shellView.MatchHudFeedbackTextLabel.text);
            Assert.IsFalse(shellView.MatchHudFeedbackPanel.ClassListContains("shell-hidden"), "Visible feedback model must show the feedback panel.");
            Assert.IsFalse(shellView.MatchHudFeedbackBoardAllAction.enabledSelf, "Board-all enabled state must come from read model.");
            Assert.IsTrue(shellView.MatchHudFeedbackCancelAction.ClassListContains("shell-hidden"), "Hidden cancel action must be hidden.");

            gateway.MatchHudStatusSurfaces = UiMatchHudStatusSurfacesModel.Default;
            applySystem.Update();

            Assert.AreEqual("OBJECTIVES", shellView.MatchHudObjectivesTitleLabel.text, "Default status model must restore objective title.");
            Assert.IsTrue(shellView.MatchHudThreatJumpAction.enabledSelf, "Default status model must restore jump availability.");
            Assert.IsFalse(shellView.MatchHudFeedbackCancelAction.ClassListContains("shell-hidden"), "Default status model must restore cancel visibility.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesMatchHudMinimapReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MatchHud,
                UIRoute.Match,
                UiShellTransitionPhase.MatchHudReady,
                86,
                false),
            HasMatchHudMinimap = true,
            MatchHudMinimap = new UiMatchHudMinimapModel(
                12f,
                18f,
                44f,
                31f,
                false,
                true,
                false,
                new UiMatchHudMinimapMarkerModel(true, 21f, 34f),
                new UiMatchHudMinimapMarkerModel(false, 35f, 42f),
                new UiMatchHudMinimapMarkerModel(true, 62f, 47f),
                new UiMatchHudMinimapMarkerModel(true, 78f, 58f))
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMatchHudMinimapReadModelSmoke");
        using World world = new("UiToolkitShellMatchHudMinimapReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasMatchHudMinimap, "Apply system must read Match HUD minimap state from the runtime gateway.");
            Assert.AreEqual(12f, applySystem.LastMatchHudMinimap.ViewportLeftPercent, 0.01f, "Apply system did not capture minimap viewport data.");
            Assert.IsTrue(shellView.HasRequiredMatchHudBindings, "Mounted Match HUD must expose minimap read-model targets.");
            AssertPercentStyle(shellView.MatchHudMinimapViewport, "left", 12f, "Minimap viewport left");
            AssertPercentStyle(shellView.MatchHudMinimapViewport, "top", 18f, "Minimap viewport top");
            AssertPercentWidth(shellView.MatchHudMinimapViewport, 44f, "Minimap viewport width");
            AssertPercentStyle(shellView.MatchHudMinimapViewport, "height", 31f, "Minimap viewport height");
            AssertPercentStyle(shellView.MatchHudMinimapFriendlyA, "left", 21f, "Friendly A marker left");
            AssertPercentStyle(shellView.MatchHudMinimapFriendlyA, "top", 34f, "Friendly A marker top");
            Assert.IsTrue(shellView.MatchHudMinimapFriendlyB.ClassListContains("shell-hidden"), "Hidden marker must receive shell-hidden.");
            AssertPercentStyle(shellView.MatchHudMinimapHostileA, "left", 62f, "Hostile marker left");
            AssertPercentStyle(shellView.MatchHudMinimapCivilian, "top", 58f, "Civilian marker top");
            Assert.IsFalse(shellView.MatchHudMinimapZoomInAction.enabledSelf, "Zoom-in enabled state must come from read model.");
            Assert.IsTrue(shellView.MatchHudMinimapZoomOutAction.enabledSelf, "Zoom-out enabled state must come from read model.");
            Assert.IsFalse(shellView.MatchHudMinimapFocusAction.enabledSelf, "Focus enabled state must come from read model.");

            gateway.MatchHudMinimap = UiMatchHudMinimapModel.Default;
            applySystem.Update();

            AssertPercentStyle(shellView.MatchHudMinimapViewport, "left", 26f, "Default minimap viewport left");
            Assert.IsFalse(shellView.MatchHudMinimapFriendlyB.ClassListContains("shell-hidden"), "Default minimap model must restore marker visibility.");
            Assert.IsTrue(shellView.MatchHudMinimapZoomInAction.enabledSelf, "Default minimap model must restore zoom-in availability.");
            Assert.IsTrue(shellView.MatchHudMinimapFocusAction.enabledSelf, "Default minimap model must restore focus availability.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesMatchHudSquadTrayReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MatchHud,
                UIRoute.Match,
                UiShellTransitionPhase.MatchHudReady,
                84,
                false),
            HasMatchHudSquadTray = true,
            MatchHudSquadTray = new UiMatchHudSquadTrayModel(
                MatchHudSquadTraySlot.CombatVehicles,
                new UiMatchHudSquadTrayCardModel(true, "RIFLE TEAM", "110/120", 0.92f),
                new UiMatchHudSquadTrayCardModel(true, "FAST APC", "180/240", 0.75f),
                new UiMatchHudSquadTrayCardModel(true, "DRONE", "60/80", 0.75f),
                new UiMatchHudSquadTrayCardModel(false, "JET", "0/0", 0f),
                new UiMatchHudSquadTrayCardModel(true, "TRANSPORT", "0/0", 0f))
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMatchHudSquadTrayReadModelSmoke");
        using World world = new("UiToolkitShellMatchHudSquadTrayReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasMatchHudSquadTray, "Apply system must read Match HUD squad tray state from the runtime gateway.");
            Assert.AreEqual(MatchHudSquadTraySlot.CombatVehicles, applySystem.LastMatchHudSquadTray.SelectedSlot, "Apply system did not capture selected squad tray slot.");
            Assert.IsTrue(shellView.HasRequiredMatchHudBindings, "Mounted Match HUD must expose squad tray read-model targets.");
            Assert.AreEqual("RIFLE TEAM", shellView.MatchHudSquadTitleLabels[0].text, "Squad card 1 title must come from read model.");
            Assert.AreEqual("110/120", shellView.MatchHudSquadHealthLabels[0].text, "Squad card 1 health text must come from read model.");
            AssertPercentWidth(shellView.MatchHudSquadHealthFills[0], 92f, "Squad card 1 health fill");
            Assert.AreEqual("FAST APC", shellView.MatchHudSquadTitleLabels[1].text, "Squad card 2 title must come from read model.");
            Assert.IsTrue(shellView.MatchHudSquadCards[1].ClassListContains("squad-card-selected"), "Selected squad tray slot must add selected class to the matching card.");
            Assert.IsFalse(shellView.MatchHudSquadCards[0].ClassListContains("squad-card-selected"), "Non-selected squad tray cards must clear selected class.");
            Assert.IsTrue(shellView.MatchHudSquadCards[3].ClassListContains("shell-hidden"), "Invisible squad tray cards must be hidden.");
            Assert.IsFalse(shellView.MatchHudSquadCards[4].ClassListContains("shell-hidden"), "Visible squad tray cards must stay visible.");

            gateway.MatchHudSquadTray = UiMatchHudSquadTrayModel.Default;
            applySystem.Update();

            Assert.IsFalse(shellView.MatchHudSquadCards[1].ClassListContains("squad-card-selected"), "Default squad tray model must clear the selected card.");
            Assert.IsFalse(shellView.MatchHudSquadCards[3].ClassListContains("shell-hidden"), "Default squad tray model must restore visible cards.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiActionRequestSystemProcessesMatchHudActionRequests()
    {
        Assert.IsTrue(File.Exists(UiActionRequestSystemPath), $"Missing UI action request system: {UiActionRequestSystemPath}");

        string source = File.ReadAllText(UiActionRequestSystemPath);
        StringAssert.Contains("public partial struct UiActionRequestSystem : ISystem", source, "UI action request processing must stay in an ECS ISystem.");
        StringAssert.Contains("DynamicBuffer<UiActionRequestComponent>", source, "The system must consume the UI action request buffer.");
        StringAssert.Contains("DynamicBuffer<RtsSelectionCommandIntentRequestElement>", source, "The system must enqueue existing RTS selection command intents.");
        StringAssert.Contains("CaptureUiClickSequence", source, "UI Toolkit command clicks must suppress the matching world click just like Canvas commands.");
        StringAssert.Contains("UiActionKind.Select", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.EnterSelectionMode", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.ExitSelectionMode", source);
        StringAssert.Contains("UiActionKind.Move", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.EnterMoveTargetMode", source);
        StringAssert.Contains("UiActionKind.Attack", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.EnterAttackTargetMode", source);
        StringAssert.Contains("UiActionKind.Hold", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.HoldPosition", source);
        StringAssert.Contains("UiActionKind.Stop", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.Stop", source);
        StringAssert.Contains("UiActionKind.Scan", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.EnterScanTargetMode", source);
        StringAssert.Contains("UiActionKind.ReturnSelection", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.ReturnToBase", source);
        StringAssert.Contains("UiActionKind.DestroySelection", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.DestroyFocusedUnit", source);
        StringAssert.Contains("UiActionKind.BoardSelection", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.EnterBoardTargetMode", source);
        StringAssert.Contains("UiActionKind.BoardAll", source);
        StringAssert.Contains("RtsSelectionCommandIntentKind.BoardAllSelectedTransport", source);
        StringAssert.Contains("UiActionKind.Build", source);
        StringAssert.Contains("BuildDrawer", File.ReadAllText("Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs"), "Build drawer popup kind must be part of the ECS shell contract.");

        Assert.That(source, Does.Not.Contain("SystemBase"), "The request processor must not be a managed SystemBase.");
        Assert.That(source, Does.Not.Contain("VisualElement"), "ECS request processing must not touch UI Toolkit objects.");
        Assert.That(source, Does.Not.Contain("UIDocument"), "ECS request processing must not touch UI Toolkit documents.");
        Assert.That(source, Does.Not.Contain("UnityEngine.UI"), "ECS request processing must not depend on Canvas UI.");
    }

    [Test]
    public void BuildDrawerUxmlExposesCanvasParityBindings()
    {
        VisualTreeAsset buildDrawerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerUxmlPath);
        VisualTreeAsset catalogItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerCatalogItemUxmlPath);
        VisualTreeAsset queueItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerProductionQueueItemUxmlPath);
        VisualTreeAsset activeItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerProductionActiveItemUxmlPath);

        Assert.IsNotNull(buildDrawerAsset, $"Missing Build Drawer UXML asset: {BuildDrawerUxmlPath}");
        Assert.IsNotNull(catalogItemAsset, $"Missing Build Drawer catalog item template: {BuildDrawerCatalogItemUxmlPath}");
        Assert.IsNotNull(queueItemAsset, $"Missing Build Drawer queue item template: {BuildDrawerProductionQueueItemUxmlPath}");
        Assert.IsNotNull(activeItemAsset, $"Missing Build Drawer active production template: {BuildDrawerProductionActiveItemUxmlPath}");

        string uxml = File.ReadAllText(BuildDrawerUxmlPath);
        string catalogItemUxml = File.ReadAllText(BuildDrawerCatalogItemUxmlPath);
        string queueItemUxml = File.ReadAllText(BuildDrawerProductionQueueItemUxmlPath);
        string activeItemUxml = File.ReadAllText(BuildDrawerProductionActiveItemUxmlPath);

        string[] requiredNames =
        {
            "SCN09_BuildDrawerPopup",
            "BuildDrawerRoot",
            "DrawerFrame",
            "BuildPanel",
            "ProductionPanel",
            "Tabs",
            "AircraftsTab",
            "VehiclesTab",
            "SoldiersTab",
            "BuildingsTab",
            "CatalogScrollView",
            "Content",
            "InstructionStrip",
            "Instruction",
            "Name",
            "Role",
            "Preview",
            "Description",
            "SizePanel",
            "RequirementsPanel",
            "PlacementPanel",
            "ProductionTimePanel",
            "CostPanel",
            "CreditsCost",
            "SuppliesCost",
            "BuildButton",
            "NoProduction",
            "ProductionPanelActive",
            "Numbers",
            "ProductionScrollView",
            "ButtonsPanel",
            "RushButton",
            "ClearButton",
            "CloseButton"
        };

        for (int i = 0; i < requiredNames.Length; i++)
            StringAssert.Contains($"name=\"{requiredNames[i]}\"", uxml, $"Build Drawer UXML missing Canvas parity binding element: {requiredNames[i]}");

        string[] requiredButtons =
        {
            "AircraftsTab",
            "VehiclesTab",
            "SoldiersTab",
            "BuildingsTab",
            "BuildButton",
            "RushButton",
            "ClearButton",
            "CloseButton"
        };

        for (int i = 0; i < requiredButtons.Length; i++)
            StringAssert.Contains($"<ui:Button name=\"{requiredButtons[i]}\"", uxml, $"Build Drawer actionable element must remain a UI Toolkit Button: {requiredButtons[i]}");

        StringAssert.Contains("<ui:Template name=\"BuildCatalogItemView\"", uxml, "Build Drawer must retain a catalog item template.");
        StringAssert.Contains("<ui:Template name=\"ProductionQueueItemView\"", uxml, "Build Drawer must retain a queue item template.");
        StringAssert.Contains("<ui:Template name=\"ProductionActiveItemView\"", uxml, "Build Drawer must retain an active production item template.");
        StringAssert.Contains("vertical-scroller-visibility=\"Hidden\"", uxml, "Build Drawer scrollbars must stay hidden where the Canvas target hides them.");

        string[] catalogItemNames =
        {
            "ItemView",
            "Frame",
            "Thumb",
            "Title",
            "Role",
            "CostPanel",
            "CreditsTinyCost",
            "SuppliesTinyCost",
            "TimeTinyCost"
        };

        for (int i = 0; i < catalogItemNames.Length; i++)
            StringAssert.Contains($"name=\"{catalogItemNames[i]}\"", catalogItemUxml, $"Build Drawer catalog item template missing binding element: {catalogItemNames[i]}");
        StringAssert.Contains("<ui:Button name=\"ItemView\"", catalogItemUxml, "Build Drawer catalog items must remain actionable UI Toolkit Buttons.");

        string[] queueItemNames =
        {
            "ProductionItemView",
            "NumberPanel",
            "Number",
            "Image",
            "Name",
            "TimeText",
            "OrderButton"
        };

        for (int i = 0; i < queueItemNames.Length; i++)
            StringAssert.Contains($"name=\"{queueItemNames[i]}\"", queueItemUxml, $"Build Drawer queue item template missing binding element: {queueItemNames[i]}");
        StringAssert.Contains("<ui:Button name=\"OrderButton\"", queueItemUxml, "Build Drawer queue order action must remain a UI Toolkit Button.");

        string[] activeItemNames =
        {
            "ProductionActiveItemView",
            "Image",
            "Name",
            "Slider",
            "Background",
            "Fill Area",
            "Fill",
            "PercentageCompleteText",
            "CancelButton"
        };

        for (int i = 0; i < activeItemNames.Length; i++)
            StringAssert.Contains($"name=\"{activeItemNames[i]}\"", activeItemUxml, $"Build Drawer active production template missing binding element: {activeItemNames[i]}");
        StringAssert.Contains("<ui:Button name=\"CancelButton\"", activeItemUxml, "Build Drawer active production cancel action must remain a UI Toolkit Button.");
    }

    [Test]
    public void UiToolkitShellViewMountsBuildDrawerUxmlIntoPopupSlot()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset buildDrawerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(buildDrawerAsset, $"Missing Build Drawer UXML asset: {BuildDrawerUxmlPath}");

        GameObject host = new("UiToolkitShellBuildDrawerMountSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, null, buildDrawerAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Build Drawer popup content.");
            Assert.AreSame(buildDrawerAsset, shellView.BuildDrawerPopupAsset, "Configured Build Drawer popup asset must be retained by the shell view.");
            Assert.IsFalse(shellView.HasMountedBuildDrawerPopup, "Shell mount must not eagerly instantiate the Build Drawer popup while Main Menu is active.");
            Assert.IsTrue(shellView.MountBuildDrawerPopup(), "Build Drawer popup must mount on demand into PopupScreenSlot.");
            Assert.IsTrue(shellView.HasRequiredBuildDrawerBindings, "Mounted Build Drawer UXML must bind the Phase 5 root panels, scrolls, and actions.");
            Assert.IsNotNull(shellView.BuildDrawerPopupRoot, "Mounted Build Drawer popup root must be cached.");
            Assert.AreEqual("SCN09_BuildDrawerPopup", shellView.BuildDrawerPopupRoot.name, "Mounted Build Drawer content must keep its binding root name.");
            Assert.AreEqual(1, shellView.PopupScreenSlot.childCount, "Popup screen slot must contain a single Build Drawer UXML tree.");
            Assert.IsNotNull(shellView.BuildDrawerCatalogScrollView, "Mounted Build Drawer UXML must expose the catalog scroll binding.");
            Assert.IsNotNull(shellView.BuildDrawerProductionScrollView, "Mounted Build Drawer UXML must expose the production scroll binding.");
            Assert.IsNotNull(shellView.BuildDrawerBuildAction, "Mounted Build Drawer UXML must expose the build action binding.");
            Assert.IsNotNull(shellView.BuildDrawerRushAction, "Mounted Build Drawer UXML must expose the rush action binding.");
            Assert.IsNotNull(shellView.BuildDrawerClearAction, "Mounted Build Drawer UXML must expose the clear action binding.");
            Assert.IsNotNull(shellView.BuildDrawerCloseAction, "Mounted Build Drawer UXML must expose the close action binding.");
            Assert.IsNotNull(shellView.BuildDrawerBuildIcon, "Mounted Build Drawer UXML must expose the build icon binding.");
            Assert.IsNotNull(shellView.BuildDrawerRushIcon, "Mounted Build Drawer UXML must expose the rush icon binding.");
            Assert.IsNotNull(shellView.BuildDrawerClearIcon, "Mounted Build Drawer UXML must expose the clear icon binding.");
            Assert.IsNotNull(shellView.BuildDrawerNameLabel, "Mounted Build Drawer UXML must expose the selected item name binding.");
            Assert.IsNotNull(shellView.BuildDrawerRoleLabel, "Mounted Build Drawer UXML must expose the selected item role binding.");
            Assert.IsNotNull(shellView.BuildDrawerInstructionInfoIcon, "Mounted Build Drawer UXML must expose the instruction info icon binding.");
            Assert.IsNotNull(shellView.BuildDrawerInstructionLabel, "Mounted Build Drawer UXML must expose the instruction binding.");
            Assert.IsNotNull(shellView.BuildDrawerProductionCountLabel, "Mounted Build Drawer UXML must expose the production count binding.");
            Assert.IsNotNull(shellView.BuildDrawerActiveProductionRow, "Mounted Build Drawer UXML must expose the active production row.");
            Assert.IsNotNull(shellView.BuildDrawerActiveProductionImage, "Mounted Build Drawer UXML must expose the active production image binding.");
            Assert.IsNotNull(shellView.BuildDrawerActiveProductionCancelAction, "Mounted Build Drawer UXML must expose the active production cancel action.");
            Assert.AreEqual(7, shellView.BuildDrawerCatalogItems.Count, "Build Drawer must retain seven catalog item slots.");
            Assert.AreEqual(2, shellView.BuildDrawerQueueRows.Count, "Build Drawer must retain two queued production row slots.");
            for (int i = 0; i < shellView.BuildDrawerCatalogItems.Count; i++)
            {
                Assert.IsNotNull(shellView.BuildDrawerCatalogItems[i], $"Build Drawer catalog item {i} must be cached.");
                Assert.IsNotNull(shellView.BuildDrawerCatalogThumbs[i], $"Build Drawer catalog item {i} thumbnail must be cached.");
                Assert.IsNotNull(shellView.BuildDrawerCatalogTitleLabels[i], $"Build Drawer catalog item {i} title must be cached.");
            }
            for (int i = 0; i < shellView.BuildDrawerQueueRows.Count; i++)
            {
                Assert.IsNotNull(shellView.BuildDrawerQueueRows[i], $"Build Drawer queue row {i} must be cached.");
                Assert.IsNotNull(shellView.BuildDrawerQueueImages[i], $"Build Drawer queue row {i} image must be cached.");
                Assert.IsNotNull(shellView.BuildDrawerQueueNameLabels[i], $"Build Drawer queue row {i} name must be cached.");
                Assert.IsNotNull(shellView.BuildDrawerQueueOrderActions[i], $"Build Drawer queue row {i} order action must be cached.");
            }
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "Mounted Build Drawer must remain hidden until a popup presentation command is wired.");
            Assert.IsTrue(shellView.ModalOverlay.ClassListContains("shell-hidden"), "Build Drawer mount must not show the modal overlay before popup presentation wiring.");

            Assert.IsTrue(shellView.Mount(), "Repeated shell mount must remain stable.");
            Assert.AreEqual(1, shellView.PopupScreenSlot.childCount, "Repeated shell mount must not duplicate Build Drawer content.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasMountedBuildDrawerPopup, "ClearCache must clear mounted Build Drawer state.");
            Assert.IsNull(shellView.BuildDrawerPopupRoot, "ClearCache must clear Build Drawer root.");
            Assert.IsNull(shellView.BuildDrawerBuildAction, "ClearCache must clear Build Drawer action bindings.");
            Assert.IsNull(shellView.BuildDrawerBuildIcon, "ClearCache must clear Build Drawer icon bindings.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewRefreshesBuildDrawerRetainedTemplates()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset buildDrawerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(buildDrawerAsset, $"Missing Build Drawer UXML asset: {BuildDrawerUxmlPath}");

        GameObject host = new("UiToolkitShellBuildDrawerRetainedRefreshSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, null, buildDrawerAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before applying Build Drawer snapshots.");
            Assert.IsTrue(shellView.MountBuildDrawerPopup(), "Build Drawer retained refresh validation must mount the popup explicitly.");

            int catalogChildCount = shellView.BuildDrawerCatalogScrollView.contentContainer.childCount;
            int queueChildCount = shellView.BuildDrawerProductionScrollView.contentContainer.childCount;

            UiBuildDrawerModel populated = new(
                "GUARD TOWER",
                "DEFENSE",
                "Provides overwatch and expands line of sight.",
                "3 x 3",
                "HQ LEVEL 1",
                "VALID GROUND",
                "00:18",
                "420",
                "80",
                "Tap a valid footprint to place the structure.",
                "QUEUE",
                "2/3",
                true,
                true,
                true,
                false,
                new UiBuildDrawerActiveProductionModel(true, true, "BARRACKS", "65%", 0.65f),
                2,
                new UiBuildDrawerCatalogItemModel(true, true, "GUARD TOWER", "DEFENSE", "420", "80", "00:18"),
                new UiBuildDrawerCatalogItemModel(true, false, "BARRACKS", "INFANTRY", "900", "120", "00:30"),
                default,
                default,
                default,
                default,
                default,
                1,
                new UiBuildDrawerQueueRowModel(true, true, "1", "BARRACKS", "00:14"),
                default);

            Assert.IsTrue(shellView.ApplyBuildDrawer(populated), "Build Drawer populated snapshot must apply to mounted retained templates.");
            Assert.AreEqual(catalogChildCount, shellView.BuildDrawerCatalogScrollView.contentContainer.childCount, "Build Drawer refresh must not create/destroy catalog rows.");
            Assert.AreEqual(queueChildCount, shellView.BuildDrawerProductionScrollView.contentContainer.childCount, "Build Drawer refresh must not create/destroy queue rows.");
            Assert.AreEqual("GUARD TOWER", shellView.BuildDrawerNameLabel.text, "Build Drawer detail name must update from the retained snapshot.");
            Assert.AreEqual("GUARD TOWER", shellView.BuildDrawerCatalogTitleLabels[0].text, "Catalog row 0 title must update in place.");
            Assert.AreEqual("BARRACKS", shellView.BuildDrawerCatalogTitleLabels[1].text, "Catalog row 1 title must update in place.");
            Assert.IsFalse(shellView.BuildDrawerCatalogItems[0].ClassListContains("shell-hidden"), "Visible catalog row must remain visible.");
            Assert.IsFalse(shellView.BuildDrawerCatalogItems[1].enabledSelf, "Disabled catalog row must disable its retained button.");
            Assert.IsTrue(shellView.BuildDrawerCatalogItems[2].ClassListContains("shell-hidden"), "Unused catalog rows must hide without destruction.");
            Assert.AreEqual("BARRACKS", shellView.BuildDrawerQueueNameLabels[0].text, "Queue row 0 name must update in place.");
            Assert.IsTrue(shellView.BuildDrawerQueueRows[1].ClassListContains("shell-hidden"), "Unused queue rows must hide without destruction.");

            UiBuildDrawerModel reduced = new(
                "AIRFIELD",
                "AIRCRAFT",
                "Unlocks aircraft production.",
                "5 x 4",
                "HQ LEVEL 2",
                "NEEDS CLEARANCE",
                "01:20",
                "1,800",
                "240",
                "Select another structure or clear the queue.",
                "QUEUE",
                "0/3",
                false,
                false,
                true,
                true,
                default,
                1,
                new UiBuildDrawerCatalogItemModel(true, true, "AIRFIELD", "AIRCRAFT", "1,800", "240", "01:20"),
                default,
                default,
                default,
                default,
                default,
                default,
                0,
                default,
                default);

            Assert.IsTrue(shellView.ApplyBuildDrawer(reduced), "Build Drawer reduced snapshot must apply to mounted retained templates.");
            Assert.AreEqual(catalogChildCount, shellView.BuildDrawerCatalogScrollView.contentContainer.childCount, "Reduced Build Drawer refresh must not create/destroy catalog rows.");
            Assert.AreEqual(queueChildCount, shellView.BuildDrawerProductionScrollView.contentContainer.childCount, "Reduced Build Drawer refresh must not create/destroy queue rows.");
            Assert.AreEqual("AIRFIELD", shellView.BuildDrawerNameLabel.text, "Build Drawer detail name must update on reduced snapshot.");
            Assert.AreEqual("AIRFIELD", shellView.BuildDrawerCatalogTitleLabels[0].text, "Catalog row 0 must be reused for the reduced snapshot.");
            Assert.IsTrue(shellView.BuildDrawerCatalogItems[1].ClassListContains("shell-hidden"), "Catalog rows outside the reduced count must hide.");
            Assert.IsTrue(shellView.BuildDrawerQueueRows[0].ClassListContains("shell-hidden"), "Queue rows outside the reduced count must hide.");
            Assert.IsTrue(shellView.BuildDrawerActiveProductionRow.ClassListContains("shell-hidden"), "Inactive active-production row must hide without destruction.");
            Assert.IsFalse(shellView.BuildDrawerBuildAction.enabledSelf, "Build action enabled state must update from the snapshot.");
            Assert.IsTrue(shellView.BuildDrawerClearAction.enabledSelf, "Clear action enabled state must update from the snapshot.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitBuildDrawerCloseActionOnlyHidesBuildDrawerPopup()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset buildDrawerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(buildDrawerAsset, $"Missing Build Drawer UXML asset: {BuildDrawerUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway();
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellBuildDrawerCloseSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, null, buildDrawerAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating Build Drawer close action.");
            Assert.IsTrue(shellView.MountBuildDrawerPopup(), "Build Drawer close validation must mount the popup explicitly.");
            Assert.IsNotNull(shellView.BuildDrawerCloseAction, "Build Drawer close action must be cached.");

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            StringAssert.Contains("RegisterBuildDrawerCloseAction", shellViewSource, "Build Drawer close button must register its own callback.");
            StringAssert.Contains("UiActionKind.CloseBuildDrawer", shellViewSource, "Build Drawer close button must enqueue the CloseBuildDrawer UI action.");

            Assert.IsTrue(shellView.TrySubmitMatchHudAction(UiActionKind.CloseBuildDrawer), "Build Drawer close action did not submit through the UI action boundary.");
            Assert.AreEqual(1, gateway.UiActionRequests.Count, "Build Drawer close must enqueue exactly one UI action request.");
            Assert.AreEqual(UiActionKind.CloseBuildDrawer, gateway.UiActionRequests[0].Kind, "Build Drawer close action kind mismatch.");
            Assert.AreEqual(0, gateway.RouteRequests.Count, "Build Drawer close must not enqueue a Main Menu route request from the view.");

            string requestSystemSource = File.ReadAllText(UiActionRequestSystemPath);
            Match closeCase = Regex.Match(
                requestSystemSource,
                @"case\s+UiActionKind\.CloseBuildDrawer:(?<body>.*?)break;",
                RegexOptions.Singleline | RegexOptions.CultureInvariant);
            Assert.IsTrue(closeCase.Success, "UI action request system must process CloseBuildDrawer.");
            string closeBody = closeCase.Groups["body"].Value;
            StringAssert.Contains("CaptureUiClickSequence", closeBody, "CloseBuildDrawer must suppress the underlying world click.");
            StringAssert.Contains("UiShellPopupKind.BuildDrawer", closeBody, "CloseBuildDrawer must target only the Build Drawer popup.");
            StringAssert.Contains("UiShellPopupIntent.Hide", closeBody, "CloseBuildDrawer must hide, not show, the Build Drawer popup.");
            StringAssert.Contains("RtsSelectionCommandIntentKind.CancelActiveCommandMode", closeBody, "CloseBuildDrawer must clear Build command selected state.");
            Assert.That(closeBody, Does.Not.Contain("UiShellRouteRequestComponent"), "CloseBuildDrawer must not enqueue route requests.");
            Assert.That(closeBody, Does.Not.Contain("ReturnToMainMenu"), "CloseBuildDrawer must not route to Main Menu.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitBuildDrawerCatalogActionsEnqueueEcsBuildRequests()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset buildDrawerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(buildDrawerAsset, $"Missing Build Drawer UXML asset: {BuildDrawerUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway();
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellBuildDrawerCatalogActionsSmoke");
        Sprite previewSprite = CreateTestSprite("BuildDrawerPreviewSprite");
        Sprite lightApcSprite = CreateTestSprite("BuildDrawerLightApcSprite");
        Sprite heavyApcSprite = CreateTestSprite("BuildDrawerHeavyApcSprite");
        Sprite activeProductionSprite = CreateTestSprite("BuildDrawerActiveProductionSprite");
        Sprite queuedProductionSprite = CreateTestSprite("BuildDrawerQueuedProductionSprite");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, null, buildDrawerAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating Build Drawer catalog actions.");
            Assert.IsTrue(shellView.MountBuildDrawerPopup(), "Build Drawer catalog action validation must mount the popup explicitly.");
            Assert.AreEqual(7, shellView.BuildDrawerCatalogItems.Count, "Build Drawer must retain seven catalog action slots.");

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            StringAssert.Contains("RegisterBuildDrawerCatalogAction", shellViewSource, "Build Drawer catalog rows must register retained row callbacks.");
            StringAssert.Contains("UiActionKind.BuildCatalogItem", shellViewSource, "Build Drawer catalog rows must enqueue BuildCatalogItem actions.");
            StringAssert.Contains("RegisterBuildDrawerTabAction", shellViewSource, "Build Drawer tabs must register retained tab callbacks.");
            StringAssert.Contains("UiActionKind.BuildDrawerTab", shellViewSource, "Build Drawer tabs must enqueue BuildDrawerTab actions.");

            Button[] tabActions =
            {
                shellView.BuildDrawerPopupRoot.Q<Button>("BuildingsTab"),
                shellView.BuildDrawerPopupRoot.Q<Button>("VehiclesTab"),
                shellView.BuildDrawerPopupRoot.Q<Button>("AircraftsTab"),
                shellView.BuildDrawerPopupRoot.Q<Button>("SoldiersTab")
            };

            for (int i = 0; i < tabActions.Length; i++)
            {
                Assert.IsNotNull(tabActions[i], $"Build Drawer tab {i} must expose a button.");
                Assert.IsTrue(shellView.TrySubmitMatchHudAction(UiActionKind.BuildDrawerTab, i), $"Build Drawer tab action {i} did not submit through the UI action boundary.");
            }

            Assert.AreEqual(tabActions.Length, gateway.UiActionRequests.Count, "Every Build Drawer tab must enqueue exactly one UI action request before catalog rows are submitted.");
            for (int i = 0; i < tabActions.Length; i++)
            {
                Assert.AreEqual(UiActionKind.BuildDrawerTab, gateway.UiActionRequests[i].Kind, $"Build Drawer tab {i} action kind mismatch.");
                Assert.AreEqual(i, gateway.UiActionRequests[i].PayloadId, $"Build Drawer tab {i} payload mismatch.");
            }

            gateway.UiActionRequests.Clear();

            for (int i = 0; i < shellView.BuildDrawerCatalogItems.Count; i++)
            {
                Assert.IsNotNull(shellView.BuildDrawerCatalogItems[i], $"Build Drawer catalog row {i} must expose a button.");
                Assert.IsTrue(shellView.TrySubmitMatchHudAction(UiActionKind.BuildCatalogItem, i), $"Build Drawer catalog action {i} did not submit through the UI action boundary.");
            }

            Assert.AreEqual(shellView.BuildDrawerCatalogItems.Count, gateway.UiActionRequests.Count, "Every Build Drawer catalog row must enqueue exactly one UI action request.");
            for (int i = 0; i < gateway.UiActionRequests.Count; i++)
            {
                Assert.AreEqual(UiActionKind.BuildCatalogItem, gateway.UiActionRequests[i].Kind, $"Build Drawer catalog row {i} action kind mismatch.");
                Assert.AreEqual(i, gateway.UiActionRequests[i].PayloadId, $"Build Drawer catalog row {i} payload mismatch.");
            }

            string ecsComponentsSource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs");
            StringAssert.Contains("public struct UiBuildCatalogRequestComponent : IBufferElementData", ecsComponentsSource, "Build Drawer catalog actions must have an ECS request buffer component.");
            StringAssert.Contains("public int CatalogSlot;", ecsComponentsSource, "Build catalog request must preserve the selected retained row slot.");
            StringAssert.Contains("public int RequestId;", ecsComponentsSource, "Build catalog request must carry a monotonically increasing request id.");

            string boundarySource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs");
            StringAssert.Contains("AddBuffer<UiBuildCatalogRequestComponent>", boundarySource, "UI shell boundary must own the Build Drawer catalog request buffer.");
            StringAssert.Contains("EnsureUiBuildCatalogRequestBuffer", boundarySource, "Existing shell boundaries must be upgraded with the Build Drawer catalog request buffer.");

            string requestSystemSource = File.ReadAllText(UiActionRequestSystemPath);
            Match catalogCase = Regex.Match(
                requestSystemSource,
                @"case\s+UiActionKind\.BuildCatalogItem:(?<body>.*?)break;",
                RegexOptions.Singleline | RegexOptions.CultureInvariant);
            Assert.IsTrue(catalogCase.Success, "UI action request system must process BuildCatalogItem.");
            string catalogBody = catalogCase.Groups["body"].Value;
            StringAssert.Contains("CaptureUiClickSequence", catalogBody, "BuildCatalogItem must suppress the underlying world click.");
            StringAssert.Contains("buildCatalogRequests.Add", catalogBody, "BuildCatalogItem must enqueue an ECS Build Drawer catalog request.");
            StringAssert.Contains("CatalogSlot = request.PayloadId", catalogBody, "BuildCatalogItem must preserve the clicked row payload.");
            StringAssert.Contains("RequestId = queue.LastRequestId", catalogBody, "BuildCatalogItem must stamp the request id on the ECS request.");

            Match tabCase = Regex.Match(
                requestSystemSource,
                @"case\s+UiActionKind\.BuildDrawerTab:(?<body>.*?)break;",
                RegexOptions.Singleline | RegexOptions.CultureInvariant);
            Assert.IsTrue(tabCase.Success, "UI action request system must process BuildDrawerTab.");
            string tabBody = tabCase.Groups["body"].Value;
            StringAssert.Contains("CaptureUiClickSequence", tabBody, "BuildDrawerTab must suppress the underlying world click.");
            StringAssert.Contains("buildDrawerState.ActiveCategory", tabBody, "BuildDrawerTab must update the active drawer category.");
            StringAssert.Contains("buildDrawerState.SelectedCatalogSlot = 0", tabBody, "BuildDrawerTab must reset row selection when changing category.");

            var vehicleDrawer = new UiBuildDrawerModel(
                "APC",
                "VEHICLE",
                "Armored transport.",
                "-",
                "Vehicle bay",
                "-",
                "00:12",
                "300",
                "0",
                "Produce APC.",
                "PRODUCTION",
                "0",
                true,
                false,
                false,
                true,
                default,
                previewSprite,
                BuildDrawerCategory.Vehicles,
                2,
                3,
                0,
                1,
                1,
                2,
                new UiBuildDrawerCatalogItemModel(true, true, false, lightApcSprite, "LIGHT APC", "VEHICLE", "250", "0", "00:10"),
                new UiBuildDrawerCatalogItemModel(true, true, true, heavyApcSprite, "HEAVY APC", "VEHICLE", "300", "0", "00:12"),
                default,
                default,
                default,
                default,
                default,
                0,
                default,
                default);
            Assert.IsTrue(shellView.ApplyBuildDrawer(vehicleDrawer), "Build Drawer model with active Vehicles tab must apply.");
            Assert.IsTrue(tabActions[1].ClassListContains("drawer-tab-selected"), "Active Vehicles tab must show selected state.");
            Assert.IsFalse(tabActions[2].enabledSelf, "Empty Aircrafts tab must be disabled when it is not active.");
            Assert.IsFalse(shellView.BuildDrawerCatalogItems[0].ClassListContains("selected"), "Unselected catalog row must clear selected state.");
            Assert.IsTrue(shellView.BuildDrawerCatalogItems[1].ClassListContains("selected"), "Selected catalog row must show selected state after tab refresh.");
            AssertBackgroundSprite(shellView.BuildDrawerPreview, previewSprite, "Build Drawer selected detail preview");
            AssertBackgroundSprite(shellView.BuildDrawerCatalogThumbs[0], lightApcSprite, "Build Drawer catalog row 0 thumbnail");
            AssertBackgroundSprite(shellView.BuildDrawerCatalogThumbs[1], heavyApcSprite, "Build Drawer catalog row 1 thumbnail");
            Assert.IsTrue(shellView.BuildDrawerProductionPanelActive.ClassListContains("shell-hidden"), "Build Drawer production progress panel must stay hidden when no production is active.");

            var productionDrawer = new UiBuildDrawerModel(
                "HEAVY APC",
                "VEHICLE",
                "Armored transport.",
                "-",
                "Vehicle bay",
                "-",
                "00:12",
                "300",
                "0",
                "Producing APC.",
                "PRODUCTION",
                "2",
                true,
                false,
                true,
                false,
                new UiBuildDrawerActiveProductionModel(true, true, activeProductionSprite, "HEAVY APC", "65%", 0.65f),
                previewSprite,
                BuildDrawerCategory.Vehicles,
                2,
                3,
                0,
                1,
                1,
                2,
                new UiBuildDrawerCatalogItemModel(true, true, false, lightApcSprite, "LIGHT APC", "VEHICLE", "250", "0", "00:10"),
                new UiBuildDrawerCatalogItemModel(true, true, true, heavyApcSprite, "HEAVY APC", "VEHICLE", "300", "0", "00:12"),
                default,
                default,
                default,
                default,
                default,
                1,
                new UiBuildDrawerQueueRowModel(true, true, queuedProductionSprite, "2", "LIGHT APC", "00:10"),
                default);
            Assert.IsTrue(shellView.ApplyBuildDrawer(productionDrawer), "Build Drawer model with active production must apply.");
            Assert.IsFalse(shellView.BuildDrawerProductionPanelActive.ClassListContains("shell-hidden"), "Build Drawer production progress panel must show when production is active.");
            Assert.IsFalse(shellView.BuildDrawerActiveProductionRow.ClassListContains("shell-hidden"), "Build Drawer active production row must show when production is active.");
            AssertBackgroundSprite(shellView.BuildDrawerActiveProductionImage, activeProductionSprite, "Build Drawer active production thumbnail");
            AssertBackgroundSprite(shellView.BuildDrawerQueueImages[0], queuedProductionSprite, "Build Drawer queued production thumbnail");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(previewSprite);
            UnityEngine.Object.DestroyImmediate(lightApcSprite);
            UnityEngine.Object.DestroyImmediate(heavyApcSprite);
            UnityEngine.Object.DestroyImmediate(activeProductionSprite);
            UnityEngine.Object.DestroyImmediate(queuedProductionSprite);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitBuildDrawerProductionActionsEnqueueEcsProductionRequests()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset buildDrawerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(buildDrawerAsset, $"Missing Build Drawer UXML asset: {BuildDrawerUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway();
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellBuildDrawerProductionActionsSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, null, buildDrawerAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating Build Drawer production actions.");
            Assert.IsTrue(shellView.MountBuildDrawerPopup(), "Build Drawer production action validation must mount the popup explicitly.");
            Assert.IsNotNull(shellView.BuildDrawerRushAction, "Build Drawer Rush action must be cached.");
            Assert.IsNotNull(shellView.BuildDrawerClearAction, "Build Drawer Clear action must be cached.");
            Assert.IsNotNull(shellView.BuildDrawerActiveProductionCancelAction, "Build Drawer active production cancel action must be cached.");
            Assert.AreEqual(2, shellView.BuildDrawerQueueOrderActions.Count, "Build Drawer must retain two queued production action slots.");

            (UiActionKind Kind, int PayloadId)[] expected =
            {
                (UiActionKind.BuildProductionRush, 0),
                (UiActionKind.BuildProductionClear, 0),
                (UiActionKind.BuildProductionCancelActive, 0),
                (UiActionKind.BuildProductionCancelQueued, 0),
                (UiActionKind.BuildProductionCancelQueued, 1)
            };

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            StringAssert.Contains("RegisterBuildDrawerProductionAction", shellViewSource, "Build Drawer Rush/Clear/Cancel actions must use typed production callbacks.");
            StringAssert.Contains("RegisterBuildDrawerQueueAction", shellViewSource, "Build Drawer retained queue rows must register production callbacks.");

            for (int i = 0; i < shellView.BuildDrawerQueueOrderActions.Count; i++)
                Assert.IsNotNull(shellView.BuildDrawerQueueOrderActions[i], $"Build Drawer queue action {i} must expose a button.");

            for (int i = 0; i < expected.Length; i++)
                Assert.IsTrue(shellView.TrySubmitMatchHudAction(expected[i].Kind, expected[i].PayloadId), $"Build Drawer production action {expected[i].Kind}/{expected[i].PayloadId} did not submit.");

            Assert.AreEqual(expected.Length, gateway.UiActionRequests.Count, "Every Build Drawer production surface must enqueue exactly one UI action request.");
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].Kind, gateway.UiActionRequests[i].Kind, $"Build Drawer production action {i} kind mismatch.");
                Assert.AreEqual(expected[i].PayloadId, gateway.UiActionRequests[i].PayloadId, $"Build Drawer production action {i} payload mismatch.");
            }

            string contractsSource = File.ReadAllText("Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs");
            StringAssert.Contains("UiBuildProductionActionKind", contractsSource, "Build production requests must use an explicit action enum.");
            StringAssert.Contains("BuildProductionRush", contractsSource, "Build Drawer Rush must have a typed UI action.");
            StringAssert.Contains("BuildProductionClear", contractsSource, "Build Drawer Clear must have a typed UI action.");
            StringAssert.Contains("BuildProductionCancelActive", contractsSource, "Build Drawer active cancel must have a typed UI action.");
            StringAssert.Contains("BuildProductionCancelQueued", contractsSource, "Build Drawer queued cancel must have a typed UI action.");

            string ecsComponentsSource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs");
            StringAssert.Contains("public struct UiBuildProductionRequestComponent : IBufferElementData", ecsComponentsSource, "Build Drawer production actions must have an ECS request buffer component.");
            StringAssert.Contains("public UiBuildProductionActionKind ActionKind;", ecsComponentsSource, "Build production request must preserve action kind.");
            StringAssert.Contains("public int QueueSlot;", ecsComponentsSource, "Build production request must preserve queue row payload.");
            StringAssert.Contains("public int RequestId;", ecsComponentsSource, "Build production request must carry a request id.");

            string boundarySource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs");
            StringAssert.Contains("AddBuffer<UiBuildProductionRequestComponent>", boundarySource, "UI shell boundary must own the Build Drawer production request buffer.");
            StringAssert.Contains("EnsureUiBuildProductionRequestBuffer", boundarySource, "Existing shell boundaries must be upgraded with the Build Drawer production request buffer.");

            string requestSystemSource = File.ReadAllText(UiActionRequestSystemPath);
            StringAssert.Contains("DynamicBuffer<UiBuildProductionRequestComponent> buildProductionRequests", requestSystemSource, "UI action request system must acquire the Build Drawer production request buffer.");
            StringAssert.Contains("EnqueueBuildProductionRequest", requestSystemSource, "UI action request system must map production UI actions through one ECS request helper.");
            AssertBuildProductionCase(requestSystemSource, "BuildProductionRush", "UiBuildProductionActionKind.Rush", "0");
            AssertBuildProductionCase(requestSystemSource, "BuildProductionClear", "UiBuildProductionActionKind.Clear", "0");
            AssertBuildProductionCase(requestSystemSource, "BuildProductionCancelActive", "UiBuildProductionActionKind.CancelActive", "0");
            AssertBuildProductionCase(requestSystemSource, "BuildProductionCancelQueued", "UiBuildProductionActionKind.CancelQueued", "request.PayloadId");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitBuildDrawerPopupCommandsPreserveSelectedBuildState()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        VisualTreeAsset buildDrawerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");
        Assert.IsNotNull(buildDrawerAsset, $"Missing Build Drawer UXML asset: {BuildDrawerUxmlPath}");

        GameObject host = new("UiToolkitShellBuildDrawerPopupStateSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset, buildDrawerAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating Build Drawer popup commands.");
            Assert.IsFalse(shellView.HasMountedMatchHudScreen, "Shell mount must not eagerly instantiate the Match HUD while Main Menu is active.");
            Assert.IsFalse(shellView.HasMountedBuildDrawerPopup, "Shell mount must not eagerly instantiate the Build Drawer popup.");
            Assert.IsTrue(shellView.ModalOverlay.ClassListContains("shell-hidden"), "Modal overlay must start hidden.");

            var commands = new List<UiShellPresentationCommandModel>
            {
                new(
                    UiShellCommandKind.EnterMatchHud,
                    UiShellRegionId.MiddleRegion,
                    UIRoute.Match,
                    UiShellMode.MatchHud,
                    91)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands), "EnterMatchHud presentation must apply.");
            Assert.IsTrue(shellView.HasMountedMatchHudScreen, "EnterMatchHud must mount the Match HUD so command selected classes can be validated.");
            Assert.IsFalse(shellView.MatchScreenSlot.ClassListContains("shell-hidden"), "Match HUD must be visible before showing Build Drawer.");
            Assert.IsTrue(shellView.ApplyMatchHudCommandState(new UiMatchHudCommandStateModel(TacticalCommandMode.None, false)), "Closed drawer command state must apply.");
            Assert.IsFalse(shellView.MatchHudBuildCommand.ClassListContains("command-button-selected"), "Build command must start deselected when drawer is closed.");
            Assert.IsFalse(shellView.MatchHudRightBuildCommand.ClassListContains("quick-command-selected"), "Right Build command must start deselected when drawer is closed.");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.ShowPopup,
                UiShellRegionId.PopupLayer,
                UIRoute.Match,
                UiShellMode.PopupOnly,
                92);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands), "ShowPopup presentation must apply.");
            Assert.IsTrue(shellView.HasMountedBuildDrawerPopup, "ShowPopup must mount the Build Drawer popup into PopupScreenSlot.");
            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "ShowPopup must reveal the popup screen slot.");
            Assert.IsFalse(shellView.ModalOverlay.ClassListContains("shell-hidden"), "ShowPopup must reveal the modal overlay.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupVisible)), "ShowPopup must apply popup scale-in motion.");
            Assert.IsTrue(shellView.ApplyMatchHudCommandState(new UiMatchHudCommandStateModel(TacticalCommandMode.None, true)), "Open drawer command state must apply.");
            Assert.IsTrue(shellView.MatchHudBuildCommand.ClassListContains("command-button-selected"), "Build command must stay selected while Build Drawer is open.");
            Assert.IsTrue(shellView.MatchHudRightBuildCommand.ClassListContains("quick-command-selected"), "Right Build command must stay selected while Build Drawer is open.");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.HidePopup,
                UiShellRegionId.PopupLayer,
                UIRoute.Match,
                UiShellMode.MatchHud,
                93);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands), "HidePopup presentation must apply.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "HidePopup must hide the popup screen slot.");
            Assert.IsTrue(shellView.ModalOverlay.ClassListContains("shell-hidden"), "HidePopup must hide the modal overlay.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupHidden)), "HidePopup must apply popup scale-out motion.");
            Assert.IsTrue(shellView.ApplyMatchHudCommandState(new UiMatchHudCommandStateModel(TacticalCommandMode.None, false)), "Closed drawer command state must reapply.");
            Assert.IsFalse(shellView.MatchHudBuildCommand.ClassListContains("command-button-selected"), "Build command must clear after Build Drawer closes.");
            Assert.IsFalse(shellView.MatchHudRightBuildCommand.ClassListContains("quick-command-selected"), "Right Build command must clear after Build Drawer closes.");

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            StringAssert.Contains("case UiShellCommandKind.ShowPopup", shellViewSource, "Build Drawer popup must be shown through shell presentation commands.");
            StringAssert.Contains("case UiShellCommandKind.HidePopup", shellViewSource, "Build Drawer popup must be hidden through shell presentation commands.");
            StringAssert.Contains("UiToolkitShellMotionState.PopupVisible", shellViewSource, "ShowPopup must use popup scale-in motion.");
            StringAssert.Contains("UiToolkitShellMotionState.PopupHidden", shellViewSource, "HidePopup must use popup scale-out motion.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitBuildDrawerPrimaryBuildEnqueuesEcsBuildRequest()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset buildDrawerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(buildDrawerAsset, $"Missing Build Drawer UXML asset: {BuildDrawerUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway();
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellBuildDrawerPrimaryBuildSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, null, buildDrawerAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before validating Build Drawer primary Build action.");
            Assert.IsTrue(shellView.MountBuildDrawerPopup(), "Build Drawer primary action validation must mount the popup explicitly.");
            Assert.IsNotNull(shellView.BuildDrawerBuildAction, "Build Drawer primary Build action must be cached.");

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            StringAssert.Contains("buildDrawerBuildActionCallback", shellViewSource, "Build Drawer primary Build action must own an unregisterable callback.");
            StringAssert.Contains("UiActionKind.BuildDrawerPrimaryBuild", shellViewSource, "Build Drawer primary Build action must enqueue the primary Build UI action.");

            Assert.IsTrue(shellView.TrySubmitMatchHudAction(UiActionKind.BuildDrawerPrimaryBuild), "Build Drawer primary Build action did not submit through the UI action boundary.");
            Assert.AreEqual(1, gateway.UiActionRequests.Count, "Build Drawer primary Build must enqueue exactly one UI action request.");
            Assert.AreEqual(UiActionKind.BuildDrawerPrimaryBuild, gateway.UiActionRequests[0].Kind, "Build Drawer primary Build action kind mismatch.");
            Assert.AreEqual(0, gateway.UiActionRequests[0].PayloadId, "Build Drawer primary Build action must not depend on a catalog row payload.");

            string ecsComponentsSource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs");
            StringAssert.Contains("public struct UiBuildPrimaryRequestComponent : IBufferElementData", ecsComponentsSource, "Build Drawer primary Build action must have an ECS request buffer component.");
            StringAssert.Contains("public int RequestId;", ecsComponentsSource, "Primary Build request must carry a request id.");

            string boundarySource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs");
            StringAssert.Contains("AddBuffer<UiBuildPrimaryRequestComponent>", boundarySource, "UI shell boundary must own the primary Build request buffer.");
            StringAssert.Contains("EnsureUiBuildPrimaryRequestBuffer", boundarySource, "Existing shell boundaries must be upgraded with the primary Build request buffer.");

            string requestSystemSource = File.ReadAllText(UiActionRequestSystemPath);
            Match primaryCase = Regex.Match(
                requestSystemSource,
                @"case\s+UiActionKind\.BuildDrawerPrimaryBuild:(?<body>.*?)break;",
                RegexOptions.Singleline | RegexOptions.CultureInvariant);
            Assert.IsTrue(primaryCase.Success, "UI action request system must process BuildDrawerPrimaryBuild.");
            string primaryBody = primaryCase.Groups["body"].Value;
            StringAssert.Contains("CaptureUiClickSequence", primaryBody, "BuildDrawerPrimaryBuild must suppress the underlying world click.");
            StringAssert.Contains("buildPrimaryRequests.Add", primaryBody, "BuildDrawerPrimaryBuild must enqueue an ECS primary Build request.");
            StringAssert.Contains("RequestId = queue.LastRequestId", primaryBody, "BuildDrawerPrimaryBuild must stamp the request id on the ECS request.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesBuildDrawerReadModel()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset matchHudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MatchHudUxmlPath);
        VisualTreeAsset buildDrawerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildDrawerUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(matchHudAsset, $"Missing Match HUD UXML asset: {MatchHudUxmlPath}");
        Assert.IsNotNull(buildDrawerAsset, $"Missing Build Drawer UXML asset: {BuildDrawerUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MatchHud,
                UIRoute.Match,
                UiShellTransitionPhase.MatchHudReady,
                95,
                false),
            HasBuildDrawer = true,
            BuildDrawer = new UiBuildDrawerModel(
                "GUARD TOWER",
                "DEFENSE",
                "Provides overwatch and expands line of sight.",
                "3 x 3",
                "HQ LEVEL 1",
                "VALID GROUND",
                "00:18",
                "420",
                "80",
                "Tap a valid footprint to place the structure.",
                "QUEUE",
                "2/3",
                true,
                true,
                true,
                false,
                new UiBuildDrawerActiveProductionModel(true, true, "BARRACKS", "65%", 0.65f),
                2,
                new UiBuildDrawerCatalogItemModel(true, true, "GUARD TOWER", "DEFENSE", "420", "80", "00:18"),
                new UiBuildDrawerCatalogItemModel(true, false, "BARRACKS", "INFANTRY", "900", "120", "00:30"),
                default,
                default,
                default,
                default,
                default,
                1,
                new UiBuildDrawerQueueRowModel(true, true, "1", "BARRACKS", "00:14"),
                default)
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellBuildDrawerReadModelSmoke");
        using World world = new("UiToolkitShellBuildDrawerReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, matchHudAsset, buildDrawerAsset);
            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Build Drawer for read-model validation.");
            Assert.IsTrue(shellView.MountBuildDrawerPopup(), "Build Drawer read-model validation must mount the popup explicitly.");

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasBuildDrawer, "Apply system must read Build Drawer state from the runtime gateway.");
            Assert.AreEqual("GUARD TOWER", applySystem.LastBuildDrawer.Name, "Apply system did not capture Build Drawer name.");
            Assert.IsTrue(shellView.HasMountedBuildDrawerPopup, "Build Drawer popup must be mounted before applying the read model.");
            Assert.AreEqual("GUARD TOWER", shellView.BuildDrawerNameLabel.text, "Build Drawer detail name must come from the read model.");
            Assert.AreEqual("DEFENSE", shellView.BuildDrawerRoleLabel.text, "Build Drawer role must come from the read model.");
            Assert.AreEqual("GUARD TOWER", shellView.BuildDrawerCatalogTitleLabels[0].text, "Catalog row 0 title must come from the read model.");
            Assert.AreEqual("BARRACKS", shellView.BuildDrawerCatalogTitleLabels[1].text, "Catalog row 1 title must come from the read model.");
            Assert.IsFalse(shellView.BuildDrawerCatalogItems[1].enabledSelf, "Catalog row enabled state must come from the read model.");
            Assert.AreEqual("BARRACKS", shellView.BuildDrawerQueueNameLabels[0].text, "Production queue row must come from the read model.");
            AssertPercentWidth(shellView.BuildDrawerActiveProductionFill, 65f, "Build Drawer active production progress");

            gateway.BuildDrawer = UiBuildDrawerModel.Empty;
            applySystem.Update();

            Assert.AreEqual("SELECT STRUCTURE", shellView.BuildDrawerNameLabel.text, "Empty Build Drawer model must apply fallback detail name.");
            Assert.IsTrue(shellView.BuildDrawerCatalogItems[0].ClassListContains("shell-hidden"), "Empty Build Drawer model must hide catalog rows.");
            Assert.IsTrue(shellView.BuildDrawerQueueRows[0].ClassListContains("shell-hidden"), "Empty Build Drawer model must hide queue rows.");
            Assert.IsTrue(shellView.BuildDrawerActiveProductionRow.ClassListContains("shell-hidden"), "Empty Build Drawer model must hide active production.");

            string runtimeGatewaySource = File.ReadAllText("Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs");
            StringAssert.Contains("bool TryReadBuildDrawer(out UiBuildDrawerModel drawer)", runtimeGatewaySource, "Runtime gateway must expose Build Drawer read model access.");

            string ecsComponentsSource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs");
            StringAssert.Contains("public struct UiBuildDrawerDetailComponent : IComponentData", ecsComponentsSource, "Build Drawer detail must be an ECS read-model component.");
            StringAssert.Contains("public struct UiBuildDrawerCatalogItemComponent : IBufferElementData", ecsComponentsSource, "Build Drawer catalog rows must be ECS read-model buffer elements.");
            StringAssert.Contains("public struct UiBuildDrawerQueueRowComponent : IBufferElementData", ecsComponentsSource, "Build Drawer queue rows must be ECS read-model buffer elements.");

            string boundarySource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs");
            StringAssert.Contains("EnsureBuildDrawerDetailComponent", boundarySource, "UI shell boundary must upgrade existing boundaries with Build Drawer detail state.");
            StringAssert.Contains("EnsureUiBuildDrawerCatalogBuffer", boundarySource, "UI shell boundary must own Build Drawer catalog row state.");
            StringAssert.Contains("EnsureUiBuildDrawerQueueBuffer", boundarySource, "UI shell boundary must own Build Drawer queue row state.");

            string ecsGatewaySource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs");
            StringAssert.Contains("public static bool TryReadBuildDrawer(out UiBuildDrawerModel drawer)", ecsGatewaySource, "ECS gateway must map Build Drawer ECS state to the UI contract.");
            StringAssert.Contains("ToBuildDrawerCatalogItem", ecsGatewaySource, "ECS gateway must convert catalog buffer rows at the presentation boundary.");
            StringAssert.Contains("ToBuildDrawerQueueRow", ecsGatewaySource, "ECS gateway must convert queue buffer rows at the presentation boundary.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void BuildPlacementConfirmationBarUxmlExposesCanvasParityBindings()
    {
        VisualTreeAsset placementBarAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildPlacementConfirmationBarUxmlPath);
        Assert.IsNotNull(placementBarAsset, $"Missing Build Placement Confirmation Bar UXML asset: {BuildPlacementConfirmationBarUxmlPath}");

        string uxml = File.ReadAllText(BuildPlacementConfirmationBarUxmlPath);
        string uss = File.ReadAllText(BuildPlacementConfirmationBarUssPath);
        string matchHudUss = File.ReadAllText(MatchHudUssPath);

        string[] requiredNames =
        {
            "SCN08_BuildPlacementConfirmationBar",
            "BuildPlacementConfirmationBarRoot",
            "Frame",
            "Title",
            "StatusChip",
            "Status",
            "CreditsIcon",
            "Cost",
            "TimeIcon",
            "Duration",
            "CancelButton",
            "RotateButton",
            "ConfirmButton",
            "InstructionStrip",
            "InfoIcon",
            "Instruction"
        };

        for (int i = 0; i < requiredNames.Length; i++)
            StringAssert.Contains($"name=\"{requiredNames[i]}\"", uxml, $"Build Placement Confirmation Bar UXML missing Canvas parity binding element: {requiredNames[i]}");

        string[] requiredButtons =
        {
            "CancelButton",
            "RotateButton",
            "ConfirmButton"
        };

        for (int i = 0; i < requiredButtons.Length; i++)
            StringAssert.Contains($"<ui:Button name=\"{requiredButtons[i]}\"", uxml, $"Build Placement Confirmation Bar actionable element must remain a UI Toolkit Button: {requiredButtons[i]}");

        StringAssert.Contains("build-placement-confirmation-bar-root", uxml, "Build Placement Confirmation Bar root must keep a dedicated pointer-blocking region class.");
        StringAssert.Contains("bar-frame", uxml, "Build Placement Confirmation Bar must keep a frame element instead of flattening the popup.");
        StringAssert.Contains(".build-placement-confirmation-bar-root", uss, "Build Placement Confirmation Bar USS must define the pointer-blocking root region.");
        StringAssert.Contains(".cancel-action", uss, "Build Placement Confirmation Bar USS must style the cancel action separately.");
        StringAssert.Contains(".rotate-action", uss, "Build Placement Confirmation Bar USS must style the rotate action separately.");
        StringAssert.Contains(".confirm-action", uss, "Build Placement Confirmation Bar USS must style the confirm action separately.");
        StringAssert.Contains("pointer-events", uss, "Build Placement Confirmation Bar USS must explicitly control pointer blocking over the active bar.");
        StringAssert.Contains("pointer-events: none;", uss, "Build Placement Confirmation Bar full-screen root must stay pointer-transparent outside the active bar rect.");
        StringAssert.Contains("pointer-events: auto;", uss, "Build Placement Confirmation Bar active rect must block world clicks over the bar only.");
        StringAssert.Contains("bottom: 20.5%;", uss, "Build Placement Confirmation Bar must stay above the Match HUD footer/command rail band.");
        StringAssert.Contains("height: 18%;", uss, "Build Placement Confirmation Bar height must remain bounded so it does not overlap the command rail.");
        StringAssert.Contains(".footer-content", matchHudUss, "Match HUD USS must expose the footer band that contains the command rail.");
        StringAssert.Contains("bottom: 1.25%;", matchHudUss, "Match HUD footer must remain below the Build Placement Confirmation Bar.");
        StringAssert.Contains("height: 18.7%;", matchHudUss, "Match HUD footer height must remain below the Build Placement Confirmation Bar safe band.");
        StringAssert.Contains(".command-rail", matchHudUss, "Match HUD USS must expose the command rail layout for placement-bar overlap validation.");
        StringAssert.Contains(".placement-valid .status-chip", uss, "Build Placement Confirmation Bar must express valid feedback as a visual state without changing UXML structure.");
        StringAssert.Contains(".placement-invalid .status-chip", uss, "Build Placement Confirmation Bar must express invalid feedback as a visual state without changing UXML structure.");
        StringAssert.Contains(".placement-invalid .confirm-action", uss, "Invalid placement feedback must visually disable confirm without replacing the action element.");
        StringAssert.Contains("min-width: 17%;", uss, "Status feedback slot must keep a fixed width so status text changes do not shift layout.");
        StringAssert.Contains("max-width: 17%;", uss, "Status feedback slot must keep a fixed width so status text changes do not shift layout.");
        StringAssert.Contains("white-space: nowrap;", uss, "Build Placement Confirmation Bar text fields must keep single-line feedback to avoid vertical layout shifts.");
        StringAssert.Contains("overflow: hidden;", uss, "Build Placement Confirmation Bar text fields must clip overflow inside fixed safe rects.");
    }

    [Test]
    public void UiToolkitShellViewMountsBuildPlacementConfirmationBarIntoMatchSlot()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset placementBarAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildPlacementConfirmationBarUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(placementBarAsset, $"Missing Build Placement Confirmation Bar UXML asset: {BuildPlacementConfirmationBarUxmlPath}");

        GameObject host = new("UiToolkitShellBuildPlacementBarMountSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, null, null, placementBarAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Build Placement Confirmation Bar content.");
            Assert.AreSame(placementBarAsset, shellView.BuildPlacementConfirmationBarAsset, "Configured Build Placement Confirmation Bar asset must be retained by the shell view.");
            Assert.IsFalse(shellView.HasMountedBuildPlacementConfirmationBar, "Shell mount must not eagerly instantiate the Build Placement Confirmation Bar while Main Menu is active.");
            Assert.IsTrue(shellView.MountBuildPlacementConfirmationBar(), "Build Placement Confirmation Bar must mount on demand into MatchScreenSlot.");
            Assert.IsTrue(shellView.HasRequiredBuildPlacementConfirmationBarBindings, "Mounted Build Placement Confirmation Bar UXML must bind Phase 6 title/status/cost/duration/instruction/actions.");
            Assert.IsNotNull(shellView.BuildPlacementConfirmationBarRoot, "Mounted Build Placement Confirmation Bar root must be cached.");
            Assert.AreEqual("SCN08_BuildPlacementConfirmationBar", shellView.BuildPlacementConfirmationBarRoot.name, "Mounted Build Placement Confirmation Bar content must keep its binding root name.");
            Assert.AreEqual(1, shellView.MatchScreenSlot.childCount, "Match screen slot must contain a single placement bar UXML tree when no Match HUD asset is configured.");
            Assert.IsNotNull(shellView.BuildPlacementTitleLabel, "Mounted Build Placement Confirmation Bar must expose the title binding.");
            Assert.IsNotNull(shellView.BuildPlacementStatusLabel, "Mounted Build Placement Confirmation Bar must expose the status binding.");
            Assert.IsNotNull(shellView.BuildPlacementCostLabel, "Mounted Build Placement Confirmation Bar must expose the cost binding.");
            Assert.IsNotNull(shellView.BuildPlacementDurationLabel, "Mounted Build Placement Confirmation Bar must expose the duration binding.");
            Assert.IsNotNull(shellView.BuildPlacementInstructionLabel, "Mounted Build Placement Confirmation Bar must expose the instruction binding.");
            Assert.IsNotNull(shellView.BuildPlacementCancelAction, "Mounted Build Placement Confirmation Bar must expose the cancel action binding.");
            Assert.IsNotNull(shellView.BuildPlacementRotateAction, "Mounted Build Placement Confirmation Bar must expose the rotate action binding.");
            Assert.IsNotNull(shellView.BuildPlacementConfirmAction, "Mounted Build Placement Confirmation Bar must expose the confirm action binding.");
            Assert.IsTrue(shellView.BuildPlacementConfirmationBarRoot.ClassListContains("shell-hidden"), "Mounted Build Placement Confirmation Bar must remain hidden until placement read-model binding is added.");

            Assert.IsTrue(shellView.Mount(), "Repeated shell mount must remain stable.");
            Assert.AreEqual(1, shellView.MatchScreenSlot.childCount, "Repeated shell mount must not duplicate Build Placement Confirmation Bar content.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasMountedBuildPlacementConfirmationBar, "ClearCache must clear mounted Build Placement Confirmation Bar state.");
            Assert.IsNull(shellView.BuildPlacementConfirmationBarRoot, "ClearCache must clear Build Placement Confirmation Bar root.");
            Assert.IsNull(shellView.BuildPlacementConfirmAction, "ClearCache must clear Build Placement Confirmation Bar action bindings.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesBuildPlacementConfirmationBarReadModelAndActions()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset placementBarAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildPlacementConfirmationBarUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(placementBarAsset, $"Missing Build Placement Confirmation Bar UXML asset: {BuildPlacementConfirmationBarUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasShellState = true,
            ShellState = new UiShellStateModel(
                UiShellMode.MatchHud,
                UIRoute.Match,
                UiShellTransitionPhase.MatchHudReady,
                117,
                false),
            HasBuildPlacementConfirmationBar = true,
            BuildPlacementConfirmationBar = new UiBuildPlacementConfirmationBarModel(
                true,
                true,
                true,
                true,
                "BUILD: GUARD TOWER",
                "VALID PLACEMENT",
                "420",
                "00:18",
                "TAP TO PLACE BUILDING")
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellBuildPlacementBarReadModelSmoke");
        using World world = new("UiToolkitShellBuildPlacementBarReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, null, null, null, placementBarAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            applySystem.Update();

            Assert.IsTrue(applySystem.HasBuildPlacementConfirmationBar, "Apply system must read Build Placement Confirmation Bar state from the runtime gateway.");
            Assert.AreEqual("BUILD: GUARD TOWER", applySystem.LastBuildPlacementConfirmationBar.Title, "Apply system did not capture Build Placement Confirmation Bar title.");
            Assert.IsFalse(shellView.BuildPlacementConfirmationBarRoot.ClassListContains("shell-hidden"), "Visible placement read model must show the bar.");
            Assert.AreEqual("BUILD: GUARD TOWER", shellView.BuildPlacementTitleLabel.text, "Placement title must come from the read model.");
            Assert.AreEqual("VALID PLACEMENT", shellView.BuildPlacementStatusLabel.text, "Placement status must come from the read model.");
            Assert.AreEqual("420", shellView.BuildPlacementCostLabel.text, "Placement cost must come from the read model.");
            Assert.AreEqual("00:18", shellView.BuildPlacementDurationLabel.text, "Placement duration must come from the read model.");
            Assert.AreEqual("TAP TO PLACE BUILDING", shellView.BuildPlacementInstructionLabel.text, "Placement instruction must come from the read model.");
            Assert.IsTrue(shellView.BuildPlacementConfirmAction.enabledSelf, "Confirm must be enabled for a valid placement.");
            Assert.IsTrue(shellView.BuildPlacementCancelAction.enabledSelf, "Cancel must be enabled while placement is active.");
            Assert.IsTrue(shellView.BuildPlacementRotateAction.enabledSelf, "Rotate must be enabled while placement is active.");
            Assert.IsTrue(shellView.BuildPlacementConfirmationBarRoot.ClassListContains("placement-valid"), "Valid placement must add the valid visual state.");

            gateway.BuildPlacementConfirmationBar = new UiBuildPlacementConfirmationBarModel(
                true,
                false,
                true,
                true,
                "PLACE BARRACKS",
                "BLOCKED: TERRAIN",
                "900",
                "00:30",
                "DRAG TO POSITION, CONFIRM TO BUILD");
            applySystem.Update();
            Assert.IsFalse(shellView.BuildPlacementConfirmationBarRoot.ClassListContains("shell-hidden"), "Invalid visible placement must keep the bar visible.");
            Assert.IsFalse(shellView.BuildPlacementConfirmationBarRoot.ClassListContains("placement-valid"), "Invalid placement must remove the valid visual state.");
            Assert.IsTrue(shellView.BuildPlacementConfirmationBarRoot.ClassListContains("placement-invalid"), "Invalid placement must add the invalid visual state without layout replacement.");
            Assert.AreEqual("BLOCKED: TERRAIN", shellView.BuildPlacementStatusLabel.text, "Invalid placement status must update in-place.");
            Assert.IsFalse(shellView.BuildPlacementConfirmAction.enabledSelf, "Invalid placement must disable confirm while preserving cancel and rotate.");
            Assert.IsTrue(shellView.BuildPlacementCancelAction.enabledSelf, "Invalid placement must preserve cancel behavior.");
            Assert.IsTrue(shellView.BuildPlacementRotateAction.enabledSelf, "Invalid placement must preserve rotate behavior.");

            gateway.BuildPlacementConfirmationBar = UiBuildPlacementConfirmationBarModel.Hidden;
            applySystem.Update();
            Assert.IsTrue(shellView.BuildPlacementConfirmationBarRoot.ClassListContains("shell-hidden"), "Hidden placement read model must hide the bar.");
            Assert.IsFalse(shellView.BuildPlacementConfirmAction.enabledSelf, "Hidden placement read model must disable confirm.");

            Assert.IsTrue(shellView.TrySubmitMatchHudAction(UiActionKind.BuildPlacementConfirm), "Build placement confirm action must submit through the UI action boundary.");
            Assert.IsTrue(shellView.TrySubmitMatchHudAction(UiActionKind.BuildPlacementCancel), "Build placement cancel action must submit through the UI action boundary.");
            Assert.IsTrue(shellView.TrySubmitMatchHudAction(UiActionKind.BuildPlacementRotate), "Build placement rotate action must submit through the UI action boundary.");
            Assert.AreEqual(UiActionKind.BuildPlacementConfirm, gateway.UiActionRequests[0].Kind, "Confirm action kind mismatch.");
            Assert.AreEqual(UiActionKind.BuildPlacementCancel, gateway.UiActionRequests[1].Kind, "Cancel action kind mismatch.");
            Assert.AreEqual(UiActionKind.BuildPlacementRotate, gateway.UiActionRequests[2].Kind, "Rotate action kind mismatch.");

            string runtimeGatewaySource = File.ReadAllText("Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs");
            StringAssert.Contains("bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar)", runtimeGatewaySource, "Runtime gateway must expose Build Placement Confirmation Bar read model access.");

            string ecsComponentsSource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs");
            StringAssert.Contains("public struct UiBuildPlacementConfirmationBarComponent : IComponentData", ecsComponentsSource, "Build Placement Confirmation Bar must be an ECS read-model component.");

            string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
            StringAssert.Contains("ApplyBuildPlacementConfirmationBar", shellViewSource, "Shell view must expose a narrow apply method for placement bar visuals.");
            StringAssert.Contains("RegisterBuildPlacementAction", shellViewSource, "Placement bar actions must use typed callbacks.");
            StringAssert.Contains("UiActionKind.BuildPlacementConfirm", shellViewSource, "Placement confirm button must enqueue a typed UI action.");
            StringAssert.Contains("target.enabledInHierarchy", shellViewSource, "Placement bar actions must not enqueue gameplay requests from disabled Toolkit elements.");

            string shellApplySource = File.ReadAllText(ShellApplySystemPath);
            StringAssert.Contains("UiShellRuntimeGateway.TryReadBuildPlacementConfirmationBar", shellApplySource, "Apply system must read placement state through the runtime gateway.");
            StringAssert.Contains("shellView.ApplyBuildPlacementConfirmationBar", shellApplySource, "Apply system must apply placement state through the view edge.");

            string actionRequestSource = File.ReadAllText(UiActionRequestSystemPath);
            StringAssert.Contains("case UiActionKind.BuildPlacementConfirm:", actionRequestSource, "UI action request system must process BuildPlacementConfirm.");
            StringAssert.Contains("case UiActionKind.BuildPlacementCancel:", actionRequestSource, "UI action request system must process BuildPlacementCancel.");
            StringAssert.Contains("case UiActionKind.BuildPlacementRotate:", actionRequestSource, "UI action request system must process BuildPlacementRotate.");
            StringAssert.Contains("BuildingUiPlacementCommandRequestElement.KindConfirmPlacement", actionRequestSource, "Confirm must enqueue the existing building placement confirm request.");
            StringAssert.Contains("BuildingUiPlacementCommandRequestElement.KindCancelPlacement", actionRequestSource, "Cancel must enqueue the existing building placement cancel request.");
            StringAssert.Contains("BuildingUiPlacementCommandRequestElement.KindRotatePlacement", actionRequestSource, "Rotate must enqueue the existing building placement rotate request.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiBuildPlacementReadModelSystemMirrorsActivePlacementState()
    {
        string readModelSource = File.ReadAllText("Assets/Game/Scripts/UI/Shell/Ecs/UiBuildPlacementReadModelSystem.cs");
        StringAssert.Contains("public partial struct UiBuildPlacementReadModelSystem : ISystem", readModelSource, "Build Placement read model producer must remain an ISystem.");
        StringAssert.Contains("UiBuildPlacementReadModelSource", readModelSource, "Build Placement read model system must read through a narrow source adapter.");
        StringAssert.Contains("IBuildingUiCommand", readModelSource, "Build Placement read model source must mirror the existing building UI command boundary.");
        StringAssert.Contains("buildingUiCommand.HasPendingBuildingPlacement", readModelSource, "Build Placement bar must be hidden when there is no active placement.");
        StringAssert.Contains("buildingUiCommand.CanConfirmBuildingPlacement", readModelSource, "Build Placement confirm state must mirror the runtime placement command boundary.");
        StringAssert.Contains("buildingUiCommand.PlacementStatusText", readModelSource, "Build Placement title/status text must mirror the runtime placement command boundary.");
        StringAssert.Contains("buildingUiCommand.ActivePlacementCost", readModelSource, "Build Placement cost must mirror the runtime placement command boundary.");
        StringAssert.Contains("buildingUiCommand.ActivePlacementDurationSeconds", readModelSource, "Build Placement duration must mirror the runtime placement command boundary.");
        StringAssert.Contains("UiBuildPlacementConfirmationBarComponent", readModelSource, "Build Placement read model system must write the shell ECS component.");
        StringAssert.Contains("Visible = 1", readModelSource, "Active placement must make the shell read model visible.");
        StringAssert.Contains("Visible = 0", readModelSource, "Inactive placement must make the shell read model hidden.");
        StringAssert.Contains("SplitPlacementStatus", readModelSource, "Build Placement read model must match the Canvas title/status split.");
        StringAssert.Contains("FormatCost", readModelSource, "Build Placement read model must match the Canvas cost formatting.");
        StringAssert.Contains("FormatDuration", readModelSource, "Build Placement read model must match the Canvas duration formatting.");

        string bootstrapSource = File.ReadAllText(MenuBootstrapSystemPath);
        StringAssert.Contains("BindUiToolkitMatchReadModels", bootstrapSource, "Menu bootstrap must bind UI Toolkit match read-model sources.");
        StringAssert.Contains("UiBuildPlacementReadModelSource.Configure(command)", bootstrapSource, "Menu bootstrap must configure the Build Placement read-model source from the loaded Match bootstrap.");
        StringAssert.Contains("ClearUiToolkitMatchReadModels", bootstrapSource, "Menu bootstrap must clear UI Toolkit match read-model sources when leaving Match or UI Toolkit mode.");
        StringAssert.Contains("matchBootstrap.BuildingUiCommandContract", bootstrapSource, "Build Placement read model must use the same building UI command contract as the Canvas bar.");
    }

    [Test]
    public void ArmoryUxmlExposesCanvasParityBindingsAndRetainedItemTemplate()
    {
        VisualTreeAsset armoryAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ArmoryUxmlPath);
        VisualTreeAsset itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ArmoryItemUxmlPath);
        Assert.IsNotNull(armoryAsset, $"Missing Armory UXML asset: {ArmoryUxmlPath}");
        Assert.IsNotNull(itemAsset, $"Missing Armory item template UXML asset: {ArmoryItemUxmlPath}");

        string uxml = File.ReadAllText(ArmoryUxmlPath);
        string itemUxml = File.ReadAllText(ArmoryItemUxmlPath);
        string uss = File.ReadAllText(ArmoryUssPath);

        string[] requiredScreenBindings =
        {
            "SCN19_ArmoryContent",
            "HeaderContent",
            "LeftContent",
            "MiddleContent",
            "RightContent",
            "FooterContent",
            "Scroll_View",
            "Content",
            "InspectionPanel",
            "PortraitFrame",
            "PortraitArt",
            "LevelSection",
            "StatsSection",
            "UpgradeButton",
            "EquipButton",
            "CloseButton",
            "BottomNavPanel"
        };

        for (int i = 0; i < requiredScreenBindings.Length; i++)
            StringAssert.Contains($"name=\"{requiredScreenBindings[i]}\"", uxml, $"Armory UXML missing Canvas parity binding: {requiredScreenBindings[i]}");

        string[] categoryButtons =
        {
            "Nav_Units",
            "Nav_Vehicles",
            "Nav_Aircraft",
            "Nav_Buildings",
            "Nav_Upgrades"
        };

        for (int i = 0; i < categoryButtons.Length; i++)
            StringAssert.Contains($"<ui:Button name=\"{categoryButtons[i]}\"", uxml, $"Armory category must remain an actionable UI Toolkit Button: {categoryButtons[i]}");

        string[] requiredActions =
        {
            "FilterDropdown",
            "SortDropdown",
            "UpgradeButton",
            "EquipButton",
            "CloseButton",
            "ArmoryTab",
            "WorkshopTab",
            "DoctrineTab",
            "DepotTab",
            "OfficersTab"
        };

        for (int i = 0; i < requiredActions.Length; i++)
            StringAssert.Contains($"<ui:Button name=\"{requiredActions[i]}\"", uxml, $"Armory action surface must remain a UI Toolkit Button: {requiredActions[i]}");

        StringAssert.Contains("<ui:Template name=\"ArmoryItemView\"", uxml, "Armory must declare a retained reusable item template.");
        StringAssert.Contains("src=\"SCN19_ArmoryItemView.uxml\"", uxml, "Armory item template must come from SCN19_ArmoryItemView.uxml.");
        StringAssert.Contains("<ui:ScrollView name=\"Scroll_View\"", uxml, "Armory catalog must keep the Canvas scroll binding name.");
        StringAssert.Contains("class=\"armory-catalog-grid\"", uxml, "Armory catalog content must keep a grid container for retained item rows.");

        string[] itemBindings =
        {
            "ItemView",
            "Frame",
            "Rarity",
            "Art",
            "Title",
            "State",
            "StateLabel",
            "Badge",
            "Progress",
            "Track",
            "Fill",
            "Level",
            "Type"
        };

        for (int i = 0; i < itemBindings.Length; i++)
            StringAssert.Contains($"name=\"{itemBindings[i]}\"", itemUxml, $"Armory retained item template missing binding: {itemBindings[i]}");

        StringAssert.Contains("<ui:Button name=\"ItemView\"", itemUxml, "Armory item template root must be actionable for selection.");
        StringAssert.Contains("item-card selected", uxml, "Armory static seed data must include a selected item state for styling parity.");
        StringAssert.Contains("item-card locked", uxml, "Armory static seed data must include locked item states for styling parity.");
        StringAssert.Contains(".armory-item-view.selected", uss, "Armory USS must define selected item visual state.");
        StringAssert.Contains(".armory-item-view.locked", uss, "Armory USS must define locked item visual state.");
        StringAssert.Contains("scn19_roster_card_selected_frame.png", uss, "Armory selected item state must use the generated selected roster frame.");
        StringAssert.Contains("scn19_roster_card_locked_frame.png", uss, "Armory locked item state must use the generated locked roster frame.");
        StringAssert.Contains("Generated/Armory/LayeredOneGo", uss, "Armory USS must use the new-art-direction generated Armory asset set.");
        Assert.IsFalse(uss.Contains("SCN-19_Armory_Landscape_Target", StringComparison.Ordinal), "Armory USS must not reference the deleted old Armory target.");
        Assert.IsFalse(uss.Contains("TargetLockV01", StringComparison.Ordinal), "Armory USS must not reference stale TargetLockV01 art.");
    }

    [Test]
    public void ArmoryItemTemplateSupportsFullRetainedStateBindings()
    {
        VisualTreeAsset itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ArmoryItemUxmlPath);
        Assert.IsNotNull(itemAsset, $"Missing Armory item template UXML asset: {ArmoryItemUxmlPath}");

        string itemUxml = File.ReadAllText(ArmoryItemUxmlPath);
        string uss = File.ReadAllText(ArmoryUssPath);

        string[] requiredTemplateBindings =
        {
            "ItemView",
            "Frame",
            "Rarity",
            "Art",
            "Title",
            "Subtitle",
            "State",
            "StateLabel",
            "Badge",
            "Stats",
            "Stat_Primary",
            "Stat_Primary_Label",
            "Stat_Primary_Value",
            "Stat_Secondary",
            "Stat_Secondary_Label",
            "Stat_Secondary_Value",
            "Progress",
            "Track",
            "Fill",
            "Level",
            "Type",
            "ActionAvailability",
            "ActionIcon",
            "ActionLabel"
        };

        for (int i = 0; i < requiredTemplateBindings.Length; i++)
            StringAssert.Contains($"name=\"{requiredTemplateBindings[i]}\"", itemUxml, $"Armory item template missing retained binding: {requiredTemplateBindings[i]}");

        StringAssert.Contains("<ui:Button name=\"ItemView\"", itemUxml, "Armory item template root must stay actionable.");
        StringAssert.Contains("class=\"armory-item-view item-card\"", itemUxml, "Armory item template must keep stable default item classes.");
        StringAssert.Contains("class=\"item-rarity rarity-gold\"", itemUxml, "Armory item template must expose rarity classes.");
        StringAssert.Contains("class=\"item-art rifle-art\"", itemUxml, "Armory item template must expose portrait/art classes.");
        StringAssert.Contains("class=\"item-action-availability available\"", itemUxml, "Armory item template must expose action availability state.");

        string[] requiredStyles =
        {
            ".item-subtitle",
            ".item-stats",
            ".item-stat-row",
            ".item-stat-label",
            ".item-stat-value",
            ".item-action-availability",
            ".item-action-icon",
            ".item-action-label",
            ".armory-item-view.locked .item-action-icon",
            ".armory-item-view.locked .item-action-label"
        };

        for (int i = 0; i < requiredStyles.Length; i++)
            StringAssert.Contains(requiredStyles[i], uss, $"Armory stylesheet missing retained item template style: {requiredStyles[i]}");

        StringAssert.Contains("scn19_badge_owned_checkmark.png", uss, "Available action marker must use generated Armory new-art asset.");
        StringAssert.Contains("scn19_icon_hold_shield.png", uss, "Locked action marker must use generated Armory new-art asset.");
        Assert.IsFalse(uss.Contains("SCN-19_Armory_Landscape_Target", StringComparison.Ordinal), "Armory item template styles must not reference old Armory target art.");
        Assert.IsFalse(uss.Contains("TargetLockV01", StringComparison.Ordinal), "Armory item template styles must not reference stale TargetLockV01 art.");
    }

    [Test]
    public void CommanderProfileUxmlExposesCanvasParityBindingsAndNewArtAssets()
    {
        VisualTreeAsset commanderAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CommanderProfileUxmlPath);
        StyleSheet commanderStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(CommanderProfileUssPath);
        Assert.IsNotNull(commanderAsset, $"Missing Commander/Profile UXML asset: {CommanderProfileUxmlPath}");
        Assert.IsNotNull(commanderStyle, $"Missing Commander/Profile USS asset: {CommanderProfileUssPath}");

        string uxml = File.ReadAllText(CommanderProfileUxmlPath);
        string uss = File.ReadAllText(CommanderProfileUssPath);

        string[] requiredBindings =
        {
            "SCN03_CommanderProfileContent",
            "LeftContent",
            "MiddleContent",
            "RightContent",
            "FooterContent",
            "BackButton",
            "RouteStrip",
            "OverviewTab",
            "StatsTab",
            "BadgesTab",
            "HistoryTab",
            "UpgradesTab",
            "ScreenTitle",
            "ScreenSubtitle",
            "CommanderIdentityPanel",
            "PortraitPanel",
            "Portrait",
            "EditIdButton",
            "IdentityCard",
            "Badge",
            "Title",
            "Name",
            "Subtitle",
            "Level",
            "OverviewPanel",
            "VictoriesStatCard",
            "MissionsStatCard",
            "CiviliansStatCard",
            "LostStatCard",
            "AccountSnapshotPanel",
            "ReadinessSnapshot",
            "OperationsSnapshot",
            "SkirmishSnapshot",
            "RewardTrackPanel",
            "XpProgress",
            "RewardNodes",
            "ClaimButton",
            "RecentHistoryPanel",
            "FirstContactRow",
            "OldMarketRow",
            "OpenArmoryButton",
            "DetailButton",
            "ReplayButton"
        };

        for (int i = 0; i < requiredBindings.Length; i++)
            StringAssert.Contains($"name=\"{requiredBindings[i]}\"", uxml, $"Commander/Profile UXML missing binding: {requiredBindings[i]}");

        string[] requiredTexts =
        {
            "COMMANDER PROFILE",
            "FIELD COMMANDER",
            "COL. ALEX MORGAN",
            "VICTORY IS PLANNED",
            "LEVEL 38",
            "OVERVIEW",
            "STATS",
            "BADGES",
            "HISTORY",
            "UPGRADES",
            "ACCOUNT SNAPSHOT",
            "COMMANDER REWARD TRACK",
            "RECENT HISTORY",
            "OPEN ARMORY",
            "BACK"
        };

        for (int i = 0; i < requiredTexts.Length; i++)
            StringAssert.Contains(requiredTexts[i], uxml, $"Commander/Profile UXML missing visible text: {requiredTexts[i]}");

        Assert.IsFalse(uxml.Contains("HeaderContent", StringComparison.Ordinal), "Commander/Profile content must not duplicate the persistent Main Menu header.");
        Assert.IsFalse(uxml.Contains("HeaderBar", StringComparison.Ordinal), "Commander/Profile content must not own shell header structure.");

        StringAssert.Contains("MainMenuBrightCommand/Sprites", uss, "Commander/Profile USS must use the approved new bright command-table shared assets.");
        StringAssert.Contains("Armory/LayeredOneGo", uss, "Commander/Profile USS must use approved new-art reusable panel assets.");
        StringAssert.Contains("scn02c_commander_portrait.png", uss, "Commander/Profile USS must use the new-art commander portrait.");
        StringAssert.Contains("scn02c_nav_chevron_icon.png", uss, "Commander/Profile USS must use the new-art back icon.");
        Assert.IsFalse(uxml.Contains("TargetLockV01", StringComparison.Ordinal), "Commander/Profile UXML must not reference stale TargetLockV01 art.");
        Assert.IsFalse(uss.Contains("TargetLockV01", StringComparison.Ordinal), "Commander/Profile USS must not reference stale TargetLockV01 art.");
        Assert.IsFalse(uss.Contains("Generated/CommanderProfile", StringComparison.Ordinal), "Commander/Profile USS must not reference stale CommanderProfile generated art.");
        Assert.IsFalse(uss.Contains("UnityEngine.UI", StringComparison.Ordinal), "Commander/Profile USS must not reference Canvas UI APIs.");

        string screenTitleBlock = GetCssBlock(uss, ".screen-title {");
        string identityNameBlock = GetCssBlock(uss, ".identity-name {");
        string tabLabelBlock = GetCssBlock(uss, ".tab-label {");
        string statValueBlock = GetCssBlock(uss, ".stat-value {");
        AssertCssContains(screenTitleBlock, "font-size: 32px;");
        AssertCssContains(screenTitleBlock, "white-space: nowrap;");
        AssertCssContains(identityNameBlock, "font-size: 32px;");
        AssertCssContains(tabLabelBlock, "font-size: 18px;");
        AssertCssContains(statValueBlock, "font-size: 30px;");
    }

    [Test]
    public void CommanderProfileLayoutKeepsRegionsAndTextInsideSafeRects()
    {
        string uss = File.ReadAllText(CommanderProfileUssPath);

        string rootBlock = GetCssBlock(uss, ".scn03-commander-profile-content {");
        string leftBlock = GetCssBlock(uss, ".left-content {");
        string middleBlock = GetCssBlock(uss, ".middle-content {");
        string rightBlock = GetCssBlock(uss, ".right-content {");
        string footerBlock = GetCssBlock(uss, ".footer-content {");
        string identityPanelBlock = GetCssBlock(uss, ".identity-panel {");
        string overviewPanelBlock = GetCssBlock(uss, ".overview-panel {");
        string accountPanelBlock = GetCssBlock(uss, ".account-panel {");
        string rewardPanelBlock = GetCssBlock(uss, ".reward-track-panel {");
        string historyPanelBlock = GetCssBlock(uss, ".recent-history-panel {");
        string identityNameBlock = GetCssBlock(uss, ".identity-name {");
        string identitySubtitleBlock = GetCssBlock(uss, ".identity-subtitle {");
        string panelTitleBlock = GetCssBlock(uss, ".panel-title {");
        string rewardTextBlock = GetCssBlock(uss, ".reward-text {");
        string footerActionBlock = GetCssBlock(uss, ".footer-action {");

        AssertCssContains(rootBlock, "overflow: hidden;", "Commander/Profile root must clip only at the screen boundary.");
        AssertCssContains(leftBlock, "top: 13%;", "Commander/Profile content must leave the persistent Main Menu header clear.");
        AssertCssContains(middleBlock, "top: 13%;", "Commander/Profile middle region must align below the persistent header.");
        AssertCssContains(rightBlock, "top: 13%;", "Commander/Profile right region must align below the persistent header.");
        AssertCssContains(leftBlock, "width: 16%;", "Left tab rail must remain narrow enough to preserve middle/right content gutters.");
        AssertCssContains(middleBlock, "left: 18.4%;", "Middle content must start after the left rail gutter.");
        AssertCssContains(middleBlock, "width: 45.2%;", "Middle content width must preserve the right panel gutter.");
        AssertCssContains(rightBlock, "right: 1.2%;", "Right content must stay inside the shell safe edge.");
        AssertCssContains(rightBlock, "width: 34%;", "Right content must stay large enough for reward/history panels without clipping.");
        AssertCssContains(footerBlock, "left: 18.4%;", "Footer actions must align with middle content, not the left tab rail.");
        AssertCssContains(footerBlock, "right: 1.2%;", "Footer actions must stay inside the shell safe edge.");

        AssertPanelWithinColumn(identityPanelBlock, "identity panel");
        AssertPanelWithinColumn(overviewPanelBlock, "overview panel");
        AssertPanelWithinColumn(accountPanelBlock, "account panel");
        AssertPanelWithinColumn(rewardPanelBlock, "reward track panel");
        AssertPanelWithinColumn(historyPanelBlock, "recent history panel");

        AssertCssContains(identityNameBlock, "white-space: nowrap;", "Commander/Profile identity name must not wrap into the portrait or frame chrome.");
        AssertCssContains(identitySubtitleBlock, "white-space: nowrap;", "Commander/Profile subtitle must not wrap into adjacent labels.");
        AssertCssContains(panelTitleBlock, "right: 6%;", "Panel titles must reserve right-side padding inside frames.");
        AssertCssContains(rewardTextBlock, "-unity-text-align: middle-center;", "Reward copy must stay centered inside its safe row.");
        AssertCssContains(footerActionBlock, "width: 20%;", "Footer action buttons must keep enough width for readable labels.");
    }

    [Test]
    public void MissionResultPopupUxmlExposesCanvasParityBindingsAndNewArtAssets()
    {
        VisualTreeAsset resultAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MissionResultPopupUxmlPath);
        StyleSheet resultStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(MissionResultPopupUssPath);
        Assert.IsNotNull(resultAsset, $"Missing Mission Result popup UXML asset: {MissionResultPopupUxmlPath}");
        Assert.IsNotNull(resultStyle, $"Missing Mission Result popup USS asset: {MissionResultPopupUssPath}");

        string uxml = File.ReadAllText(MissionResultPopupUxmlPath);
        string uss = File.ReadAllText(MissionResultPopupUssPath);

        string[] requiredBindings =
        {
            "POP05_MissionResultPopup",
            "BackdropScrim",
            "PopupFrame",
            "HeaderPanel",
            "ResultBadge",
            "Title",
            "Subtitle",
            "SummaryPanel",
            "SummaryTitle",
            "SummaryBody",
            "PrimaryObjectiveRow",
            "CivilianObjectiveRow",
            "LossObjectiveRow",
            "StatsPanel",
            "TimeStat",
            "UnitsLostStat",
            "CiviliansStat",
            "ScoreStat",
            "RewardsPanel",
            "RewardCredits",
            "RewardSupply",
            "RewardIntel",
            "ContinueButton",
            "ReplayButton"
        };

        for (int i = 0; i < requiredBindings.Length; i++)
            StringAssert.Contains($"name=\"{requiredBindings[i]}\"", uxml, $"Mission Result popup UXML missing binding: {requiredBindings[i]}");

        StringAssert.Contains("<ui:Button name=\"ContinueButton\"", uxml, "Mission Result continue action must remain a UI Toolkit Button.");
        StringAssert.Contains("<ui:Button name=\"ReplayButton\"", uxml, "Mission Result replay action must remain a UI Toolkit Button.");
        StringAssert.Contains("shell-motion shell-motion-popup-hidden", uxml, "Mission Result popup must start from the shared popup scale-motion hidden state.");
        StringAssert.Contains("TargetLockV02", uss, "Mission Result popup must use the approved new-art Match HUD TargetLockV02 asset set.");
        StringAssert.Contains("Armory/LayeredOneGo", uss, "Mission Result popup may reuse approved new-art Armory chrome assets.");
        StringAssert.Contains(".popup-frame", uss, "Mission Result stylesheet must define the popup frame safe rect.");
        StringAssert.Contains("transform-origin: 50% 50%;", uss, "Mission Result popup must scale from center.");
        StringAssert.Contains(".result-title", uss, "Mission Result stylesheet must define readable result title text.");
        StringAssert.Contains("font-size: 58px;", uss, "Mission Result title must be target-readable.");
        StringAssert.Contains(".action-label", uss, "Mission Result action labels must be styled separately from button chrome.");

        Assert.IsFalse(uxml.Contains("UnityEngine.UI,", StringComparison.Ordinal), "Mission Result UXML must not reference Canvas UI.");
        Assert.IsFalse(uxml.Contains("TMPro", StringComparison.Ordinal), "Mission Result UXML must not reference TextMesh Pro.");
        Assert.IsFalse(uss.Contains("TargetLockV01", StringComparison.Ordinal), "Mission Result USS must not reference stale TargetLockV01 art.");
        Assert.IsFalse(uss.Contains("Art/UI/Final", StringComparison.Ordinal), "Mission Result USS must not pull old final Canvas-era sprites.");
    }

    [Test]
    public void UiToolkitShellViewMountsMissionResultPopupIntoPopupSlot()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset resultAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MissionResultPopupUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(resultAsset, $"Missing Mission Result popup UXML asset: {MissionResultPopupUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway();
        UiShellRuntimeGateway.Register(gateway);
        GameObject host = new("UiToolkitShellMissionResultPopupSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(
                document,
                shellAsset,
                null,
                null,
                null,
                null,
                null,
                null,
                configuredMissionResultPopupAsset: resultAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Mission Result popup.");
            Assert.IsTrue(shellView.MountMissionResultPopup(), "Mission Result popup must mount into PopupScreenSlot.");
            Assert.AreSame(resultAsset, shellView.MissionResultPopupAsset, "Configured Mission Result asset must be retained by the shell view.");
            Assert.IsTrue(shellView.HasMountedMissionResultPopup, "Mission Result popup must mount into PopupScreenSlot.");
            Assert.IsTrue(shellView.HasRequiredMissionResultBindings, "Mission Result popup must cache required labels and actions.");
            Assert.AreEqual("VICTORY", shellView.MissionResultTitleLabel.text, "Mission Result title binding mismatch.");
            Assert.IsNotNull(shellView.MissionResultContinueAction, "Mission Result Continue action must be cached.");
            Assert.IsNotNull(shellView.MissionResultReplayAction, "Mission Result Replay action must be cached.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "Mission Result popup must start hidden.");
            Assert.IsTrue(shellView.ModalOverlay.ClassListContains("shell-hidden"), "Mission Result modal overlay must start hidden.");

            var commands = new[]
            {
                new UiShellPresentationCommandModel(
                    UiShellCommandKind.ShowPopup,
                    UiShellRegionId.PopupLayer,
                    UIRoute.Match,
                    UiShellMode.PopupOnly,
                    94)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands), "ShowPopup presentation must apply to Mission Result popup.");
            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "ShowPopup must reveal the Mission Result popup slot.");
            Assert.IsFalse(shellView.ModalOverlay.ClassListContains("shell-hidden"), "ShowPopup must reveal the modal overlay.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupVisible)), "Mission Result popup must use shared popup scale-in motion.");

            Assert.IsTrue(shellView.TrySubmitMissionResultAction("ContinueButton"), "Mission Result Continue action must submit through the shell route boundary.");

            Assert.AreEqual(1, gateway.RouteRequests.Count, "Mission Result Continue must enqueue a route request.");
            Assert.AreEqual(UiShellRouteIntent.ReturnToMainMenu, gateway.RouteRequests[0].Intent, "Mission Result Continue must route through loading before returning to Main Menu.");
            Assert.AreEqual(UIRoute.MainMenu, gateway.RouteRequests[0].Route, "Mission Result Continue must target Main Menu after loading.");
            Assert.IsFalse(gateway.RouteRequests[0].PushHistory, "Mission Result Continue must not push route history.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasMountedMissionResultPopup, "ClearCache must clear Mission Result mounted state.");
            Assert.IsNull(shellView.MissionResultPopupRoot, "ClearCache must clear Mission Result root.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellApplySystemAppliesMissionResultReadModelVariants()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset resultAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MissionResultPopupUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(resultAsset, $"Missing Mission Result popup UXML asset: {MissionResultPopupUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasMissionResult = true,
            MissionResult = new UiMissionResultPopupModel(
                UiMissionResultOutcome.Loss,
                "MISSION FAILED",
                "Extraction required.",
                "Primary objectives were not completed. Remaining squads are returning to base for reassignment.",
                replayEnabled: false)
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellMissionResultReadModelSmoke");
        using World world = new("UiToolkitShellMissionResultReadModelSmokeWorld");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(
                document,
                shellAsset,
                null,
                null,
                null,
                null,
                null,
                null,
                configuredMissionResultPopupAsset: resultAsset);

            UiToolkitShellApplySystem applySystem = world.GetOrCreateSystemManaged<UiToolkitShellApplySystem>();
            applySystem.ConfigureShellView(shellView);
            Assert.IsTrue(shellView.MountMissionResultPopup(), "Mission Result popup must mount before applying its read model.");

            applySystem.Update();

            Assert.IsTrue(applySystem.HasMissionResult, "Apply system must read Mission Result state from the runtime gateway.");
            Assert.AreEqual("MISSION FAILED", applySystem.LastMissionResult.Title, "Apply system did not capture Mission Result title.");
            Assert.AreEqual("MISSION FAILED", shellView.MissionResultTitleLabel.text, "Mission Result title must come from the read model.");
            Assert.AreEqual("Extraction required.", shellView.MissionResultSubtitleLabel.text, "Mission Result subtitle must come from the read model.");
            StringAssert.Contains("Remaining squads", shellView.MissionResultSummaryBodyLabel.text, "Mission Result summary must come from the read model.");
            Assert.IsTrue(shellView.MissionResultBadge.ClassListContains("loss-badge"), "Loss result must apply the loss badge style.");
            Assert.IsFalse(shellView.MissionResultReplayAction.enabledSelf, "Replay enabled state must come from the read model.");

            gateway.MissionResult = UiMissionResultPopupModel.VictoryDefault;
            applySystem.Update();

            Assert.AreEqual("VICTORY", shellView.MissionResultTitleLabel.text, "Victory model must restore the victory title.");
            Assert.IsTrue(shellView.MissionResultBadge.ClassListContains("victory-badge"), "Victory result must restore the victory badge style.");
            Assert.IsTrue(shellView.MissionResultReplayAction.enabledSelf, "Victory model must restore replay enabled state.");

            string runtimeGatewaySource = File.ReadAllText("Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs");
            StringAssert.Contains("bool TryReadMissionResult(out UiMissionResultPopupModel result)", runtimeGatewaySource, "Runtime gateway must expose Mission Result read model access.");

            string applySource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs");
            StringAssert.Contains("UiShellRuntimeGateway.TryReadMissionResult", applySource, "Managed apply edge must read Mission Result from the runtime gateway.");
            StringAssert.Contains("shellView.ApplyMissionResult", applySource, "Managed apply edge must apply Mission Result through the shell view.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void MissionResultUsesSingleUiToolkitPopupForVictoryAndLossVariants()
    {
        Assert.IsTrue(File.Exists(MissionResultPopupUxmlPath), $"Missing Mission Result UXML: {MissionResultPopupUxmlPath}");
        Assert.IsTrue(File.Exists(MissionResultPopupUssPath), $"Missing Mission Result USS: {MissionResultPopupUssPath}");

        var resultSurfacePaths = new List<string>();
        string[] uxmlPaths = Directory.GetFiles(UiToolkitRoot, "*.uxml", SearchOption.AllDirectories);
        for (int i = 0; i < uxmlPaths.Length; i++)
        {
            string fileName = Path.GetFileNameWithoutExtension(uxmlPaths[i]);
            if (fileName.Contains("Result", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("Victory", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("Loss", StringComparison.OrdinalIgnoreCase))
            {
                resultSurfacePaths.Add(NormalizeAssetPath(uxmlPaths[i]));
            }
        }

        Assert.AreEqual(1, resultSurfacePaths.Count, "Victory/Loss result variants must stay inside the single POP05 UI Toolkit surface instead of adding duplicate result UXMLs.");
        Assert.AreEqual(MissionResultPopupUxmlPath, resultSurfacePaths[0], "POP05 must remain the only UI Toolkit result popup surface.");

        string resultUss = File.ReadAllText(MissionResultPopupUssPath);
        string shellViewSource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs");
        string componentsSource = File.ReadAllText("Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs");
        StringAssert.Contains(".victory-badge", resultUss, "Single Mission Result surface must style the Victory variant.");
        StringAssert.Contains(".loss-badge", resultUss, "Single Mission Result surface must style the Loss variant.");
        StringAssert.Contains("UiMissionResultOutcome.Loss ? \"loss-badge\" : \"victory-badge\"", shellViewSource, "Mission Result view must select badge state from the read model outcome.");
        StringAssert.Contains("public static UiMissionResultPopupModel VictoryDefault", componentsSource, "Mission Result contracts must retain a Victory default model.");
        StringAssert.Contains("public static UiMissionResultPopupModel LossDefault", componentsSource, "Mission Result contracts must retain a Loss default model.");
    }

    [Test]
    public void SettingsPopupUxmlExposesShellBindingsAndRoutesThroughGateway()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset settingsAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SettingsPopupUxmlPath);
        StyleSheet settingsStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(SettingsPopupUssPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(settingsAsset, $"Missing Settings popup UXML asset: {SettingsPopupUxmlPath}");
        Assert.IsNotNull(settingsStyle, $"Missing Settings popup USS asset: {SettingsPopupUssPath}");

        string uxml = File.ReadAllText(SettingsPopupUxmlPath);
        string uss = File.ReadAllText(SettingsPopupUssPath);
        StringAssert.Contains("POP06_SettingsPopup", uxml, "Settings popup must expose a stable root binding.");
        StringAssert.Contains("name=\"CloseButton\"", uxml, "Settings popup must expose a close action.");
        StringAssert.Contains("settings-popup-frame", uxml, "Settings popup must use the shared popup scale-motion frame.");
        StringAssert.Contains("shell-motion-popup-hidden", uxml, "Settings popup must start with hidden popup motion state.");
        StringAssert.Contains("scn08_v02_panel_frame_large.png", uss, "Settings popup must use the new-art-direction V02 chrome.");
        StringAssert.Contains("scn08_v02_icon_settings_gear.png", uss, "Settings popup must use the new-art-direction settings icon.");
        Assert.IsFalse(uss.Contains("TargetLockV01", StringComparison.Ordinal), "Settings popup USS must not reference stale TargetLockV01 art.");
        Assert.IsFalse(uss.Contains("Art/UI/Final", StringComparison.Ordinal), "Settings popup USS must not pull old final Canvas-era sprites.");

        var gateway = new RecordingUiShellRuntimeGateway();
        UiShellRuntimeGateway.Register(gateway);
        GameObject host = new("UiToolkitShellSettingsPopupSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(
                document,
                shellAsset,
                null,
                null,
                null,
                null,
                null,
                null,
                configuredSettingsPopupAsset: settingsAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Settings popup.");
            Assert.IsTrue(shellView.MountSettingsPopup(), "Settings popup must mount into PopupScreenSlot.");
            Assert.AreSame(settingsAsset, shellView.SettingsPopupAsset, "Configured Settings popup asset must be retained by the shell view.");
            Assert.IsTrue(shellView.HasMountedSettingsPopup, "Settings popup must mount into PopupScreenSlot.");
            Assert.IsTrue(shellView.HasRequiredSettingsBindings, "Settings popup must cache title and close action bindings.");
            Assert.AreEqual("SETTINGS", shellView.SettingsTitleLabel.text, "Settings title binding mismatch.");
            Assert.IsNotNull(shellView.SettingsCloseAction, "Settings close action must be cached.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "Settings popup must start hidden.");

            var commands = new[]
            {
                new UiShellPresentationCommandModel(
                    UiShellCommandKind.ShowPopup,
                    UiShellRegionId.PopupLayer,
                    UIRoute.Settings,
                    UiShellMode.PopupOnly,
                    12)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands), "ShowPopup presentation must apply to Settings popup.");
            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "ShowPopup must reveal the Settings popup slot.");
            Assert.IsFalse(shellView.ModalOverlay.ClassListContains("shell-hidden"), "ShowPopup must reveal the modal overlay.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupVisible)), "Settings popup must use shared popup scale-in motion.");

            Assert.IsTrue(shellView.TrySubmitSettingsAction("CloseButton"), "Settings Close action must submit through the shell route boundary.");
            Assert.AreEqual(1, gateway.RouteRequests.Count, "Settings Close must enqueue one route request.");
            Assert.AreEqual(UiShellRouteIntent.BackMenuRoute, gateway.RouteRequests[0].Intent, "Settings Close must use the back/menu route boundary.");
            Assert.AreEqual(UIRoute.MainMenu, gateway.RouteRequests[0].Route, "Settings Close must route back to Main Menu.");
            Assert.IsFalse(gateway.RouteRequests[0].PushHistory, "Settings Close must not push route history.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasMountedSettingsPopup, "ClearCache must clear Settings mounted state.");
            Assert.IsNull(shellView.SettingsPopupRoot, "ClearCache must clear Settings root.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void InboxPopupUxmlExposesShellBindingsAndRoutesThroughGateway()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset inboxAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(InboxPopupUxmlPath);
        StyleSheet inboxStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(InboxPopupUssPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(inboxAsset, $"Missing Inbox popup UXML asset: {InboxPopupUxmlPath}");
        Assert.IsNotNull(inboxStyle, $"Missing Inbox popup USS asset: {InboxPopupUssPath}");

        string uxml = File.ReadAllText(InboxPopupUxmlPath);
        string uss = File.ReadAllText(InboxPopupUssPath);
        StringAssert.Contains("POP07_InboxPopup", uxml, "Inbox popup must expose a stable root binding.");
        StringAssert.Contains("name=\"CloseButton\"", uxml, "Inbox popup must expose a close action.");
        StringAssert.Contains("name=\"MessageRow0\"", uxml, "Inbox popup must expose retained message rows for later read-model binding.");
        StringAssert.Contains("inbox-popup-frame", uxml, "Inbox popup must use the shared popup scale-motion frame.");
        StringAssert.Contains("shell-motion-popup-hidden", uxml, "Inbox popup must start with hidden popup motion state.");
        StringAssert.Contains("scn08_v02_panel_frame_large.png", uss, "Inbox popup must use the new-art-direction V02 chrome.");
        StringAssert.Contains("scn02c_mail_icon.png", uss, "Inbox popup must use the new-art-direction Main Menu mail icon.");
        Assert.IsFalse(uss.Contains("TargetLockV01", StringComparison.Ordinal), "Inbox popup USS must not reference stale TargetLockV01 art.");
        Assert.IsFalse(uss.Contains("Art/UI/Final", StringComparison.Ordinal), "Inbox popup USS must not pull old final Canvas-era sprites.");

        var gateway = new RecordingUiShellRuntimeGateway();
        UiShellRuntimeGateway.Register(gateway);
        GameObject host = new("UiToolkitShellInboxPopupSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(
                document,
                shellAsset,
                null,
                null,
                null,
                null,
                null,
                null,
                configuredInboxPopupAsset: inboxAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Inbox popup.");
            Assert.IsTrue(shellView.MountInboxPopup(), "Inbox popup must mount into PopupScreenSlot.");
            Assert.AreSame(inboxAsset, shellView.InboxPopupAsset, "Configured Inbox popup asset must be retained by the shell view.");
            Assert.IsTrue(shellView.HasMountedInboxPopup, "Inbox popup must mount into PopupScreenSlot.");
            Assert.IsTrue(shellView.HasRequiredInboxBindings, "Inbox popup must cache title and close action bindings.");
            Assert.AreEqual("INBOX", shellView.InboxTitleLabel.text, "Inbox title binding mismatch.");
            Assert.IsNotNull(shellView.InboxCloseAction, "Inbox close action must be cached.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "Inbox popup must start hidden.");

            var commands = new[]
            {
                new UiShellPresentationCommandModel(
                    UiShellCommandKind.ShowPopup,
                    UiShellRegionId.PopupLayer,
                    UIRoute.Inbox,
                    UiShellMode.PopupOnly,
                    13)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands), "ShowPopup presentation must apply to Inbox popup.");
            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains("shell-hidden"), "ShowPopup must reveal the Inbox popup slot.");
            Assert.IsFalse(shellView.ModalOverlay.ClassListContains("shell-hidden"), "ShowPopup must reveal the modal overlay.");
            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupVisible)), "Inbox popup must use shared popup scale-in motion.");

            Assert.IsTrue(shellView.TrySubmitInboxAction("CloseButton"), "Inbox Close action must submit through the shell route boundary.");
            Assert.AreEqual(1, gateway.RouteRequests.Count, "Inbox Close must enqueue one route request.");
            Assert.AreEqual(UiShellRouteIntent.BackMenuRoute, gateway.RouteRequests[0].Intent, "Inbox Close must use the back/menu route boundary.");
            Assert.AreEqual(UIRoute.MainMenu, gateway.RouteRequests[0].Route, "Inbox Close must route back to Main Menu.");
            Assert.IsFalse(gateway.RouteRequests[0].PushHistory, "Inbox Close must not push route history.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasMountedInboxPopup, "ClearCache must clear Inbox mounted state.");
            Assert.IsNull(shellView.InboxPopupRoot, "ClearCache must clear Inbox root.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellDiagnosticsOverlayExposesFpsAndRuntimeLogActions()
    {
        string shellUxml = File.ReadAllText(ShellUxmlPath);
        string shellUss = File.ReadAllText(ShellUssPath);
        StringAssert.Contains("name=\"DiagnosticsOverlay\"", shellUxml, "Shell UXML must expose a persistent diagnostics overlay.");
        StringAssert.Contains("name=\"DiagnosticsFpsButton\"", shellUxml, "FPS counter must be a clickable Toolkit button.");
        StringAssert.Contains("name=\"DiagnosticsLogPanel\"", shellUxml, "Runtime log popup panel must be present in Toolkit shell.");
        StringAssert.Contains("name=\"DiagnosticsCloseButton\"", shellUxml, "Runtime log popup must expose a close action.");
        StringAssert.Contains(".diagnostics-fps-button", shellUss, "Shell USS must style the FPS counter.");
        StringAssert.Contains(".diagnostics-log-panel", shellUss, "Shell USS must style the runtime log overlay.");
        Assert.IsFalse(shellUxml.Contains("UnityEngine.UI,", StringComparison.Ordinal), "Diagnostics overlay must not reference Canvas UI.");
        Assert.IsFalse(shellUxml.Contains("TMPro", StringComparison.Ordinal), "Diagnostics overlay must not reference TextMesh Pro.");

        string componentsSource = File.ReadAllText("Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs");
        StringAssert.Contains("ToggleDiagnosticsOverlay", componentsSource, "FPS click must enqueue an ECS UI action.");
        StringAssert.Contains("CloseDiagnosticsOverlay", componentsSource, "Runtime log close must enqueue an ECS UI action.");

        string applySource = File.ReadAllText("Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs");
        StringAssert.Contains("UiShellRuntimeGateway.TryReadDiagnosticsOverlay", applySource, "Managed apply edge must read diagnostics from the runtime gateway.");
        StringAssert.Contains("shellView.ApplyDiagnosticsOverlay", applySource, "Managed apply edge must apply diagnostics through the shell view.");

        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasDiagnosticsOverlay = true,
            DiagnosticsOverlay = new UiDiagnosticsOverlayModel(
                58,
                true,
                "[WARN] sample warning\n\n[ERROR] sample error")
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellDiagnosticsOverlaySmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before diagnostics overlay validation.");
            Assert.IsTrue(shellView.HasRequiredDiagnosticsBindings, "Diagnostics overlay must cache required FPS/log bindings.");
            Assert.IsNotNull(shellView.DiagnosticsFpsAction, "FPS counter action must be cached.");
            Assert.IsNotNull(shellView.DiagnosticsCloseAction, "Diagnostics close action must be cached.");

            Assert.IsTrue(shellView.ApplyDiagnosticsOverlay(gateway.DiagnosticsOverlay), "Diagnostics read model must apply to the shell overlay.");
            Assert.AreEqual("58", shellView.DiagnosticsFpsValueLabel.text, "FPS text must come from the diagnostics read model.");
            StringAssert.Contains("sample warning", shellView.DiagnosticsLogTextLabel.text, "Log text must come from the diagnostics read model.");
            Assert.IsFalse(shellView.DiagnosticsLogPanel.ClassListContains("shell-hidden"), "Visible diagnostics model must show the runtime log panel.");

            Assert.IsTrue(shellView.TrySubmitMatchHudAction(UiActionKind.ToggleDiagnosticsOverlay), "FPS diagnostics click must enqueue a UI action.");
            Assert.IsTrue(shellView.TrySubmitMatchHudAction(UiActionKind.CloseDiagnosticsOverlay), "Diagnostics close click must enqueue a UI action.");
            Assert.AreEqual(2, gateway.UiActionRequests.Count, "Diagnostics actions must be routed through the UI action boundary.");
            Assert.AreEqual(UiActionKind.ToggleDiagnosticsOverlay, gateway.UiActionRequests[0].Kind, "FPS click action kind mismatch.");
            Assert.AreEqual(UiActionKind.CloseDiagnosticsOverlay, gateway.UiActionRequests[1].Kind, "Close click action kind mismatch.");

            shellView.ApplyDiagnosticsOverlay(new UiDiagnosticsOverlayModel(60, false, string.Empty));
            Assert.AreEqual("60", shellView.DiagnosticsFpsValueLabel.text, "FPS text must update on subsequent read-model applies.");
            Assert.IsTrue(shellView.DiagnosticsLogPanel.ClassListContains("shell-hidden"), "Hidden diagnostics model must hide the runtime log panel.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellViewMountsCommanderProfileUxmlIntoCommanderProfileSlot()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset commanderAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CommanderProfileUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");
        Assert.IsNotNull(mainMenuAsset, $"Missing Main Menu UXML asset: {MainMenuUxmlPath}");
        Assert.IsNotNull(commanderAsset, $"Missing Commander/Profile UXML asset: {CommanderProfileUxmlPath}");

        var gateway = new RecordingUiShellRuntimeGateway
        {
            HasCommanderProfile = true,
            CommanderProfile = new UiShellCommanderProfileModel(
                "MAJ. RILEY STONE",
                "TACTICAL ADVANTAGE READY",
                "commander-portrait-default")
        };
        UiShellRuntimeGateway.Register(gateway);

        GameObject host = new("UiToolkitShellCommanderProfileMountSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset, null, mainMenuAsset, null, null, null, null, commanderAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before mounting Commander/Profile content.");
            Assert.AreSame(commanderAsset, shellView.CommanderProfileScreenAsset, "Configured Commander/Profile asset must be retained by the shell view.");
            Assert.IsFalse(shellView.HasMountedCommanderProfileScreen, "Shell mount must not eagerly instantiate the Commander/Profile screen while Main Menu is active.");
            VisualElement headerBeforeCommandFeed = shellView.MainMenuHeaderContent;

            var commands = new List<UiShellPresentationCommandModel>
            {
                new(
                    UiShellCommandKind.SwapMenuMiddle,
                    UiShellRegionId.MiddleRegion,
                    UIRoute.CommandFeed,
                    UiShellMode.MainMenu,
                    900)
            };

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands), "CommandFeed route command must apply successfully.");
            Assert.IsTrue(shellView.HasMountedCommanderProfileScreen, "CommandFeed route must mount the Commander/Profile UXML into CommanderProfileScreenSlot.");
            Assert.IsTrue(shellView.HasRequiredCommanderProfileBindings, "Commander/Profile runtime bindings must be cached.");
            Assert.IsTrue(shellView.IsCommanderProfileSubRouteVisible, "CommandFeed route must reveal the Commander/Profile slot.");
            Assert.IsTrue(shellView.CommanderProfileScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)), "CommandFeed route must apply the visible motion class to the Commander/Profile slot.");
            Assert.IsTrue(shellView.HasPersistentMainMenuHeader, "Commander/Profile route must keep the persistent Main Menu header mounted.");
            Assert.AreSame(headerBeforeCommandFeed, shellView.MainMenuHeaderContent, "Commander/Profile route must preserve the same persistent header instance.");
            Assert.AreEqual("MAJ. RILEY STONE", shellView.CommanderProfileNameLabel.text, "Commander/Profile name must use the commander read model.");
            Assert.AreEqual("TACTICAL ADVANTAGE READY", shellView.CommanderProfileSubtitleLabel.text, "Commander/Profile subtitle must use the commander read model.");

            commands[0] = new UiShellPresentationCommandModel(
                UiShellCommandKind.SwapMenuMiddle,
                UiShellRegionId.MiddleRegion,
                UIRoute.MainMenu,
                UiShellMode.MainMenu,
                901);

            Assert.IsTrue(shellView.ApplyPresentationCommands(commands), "Returning to Main Menu route must apply successfully.");
            Assert.IsFalse(shellView.IsCommanderProfileSubRouteVisible, "Returning to Main Menu must hide the Commander/Profile slot.");
            Assert.AreSame(headerBeforeCommandFeed, shellView.MainMenuHeaderContent, "Returning from Commander/Profile must keep the same persistent header instance.");

            Assert.IsTrue(shellView.TrySubmitCommanderProfileAction("BackButton"), "Back action must submit through the shell route boundary.");
            Assert.AreEqual(1, gateway.RouteRequests.Count, "Back action must enqueue one route request.");
            Assert.AreEqual(UiShellRouteIntent.BackMenuRoute, gateway.RouteRequests[0].Intent, "Back action must use the shell back-route intent.");
            Assert.AreEqual(UIRoute.MainMenu, gateway.RouteRequests[0].Route, "Back action must fall back to Main Menu.");
            Assert.IsFalse(gateway.RouteRequests[0].PushHistory, "Back action must not push history.");

            Assert.IsTrue(shellView.TrySubmitCommanderProfileAction("OpenArmoryButton"), "Open Armory action must submit through the shell route boundary.");
            Assert.AreEqual(2, gateway.RouteRequests.Count, "Open Armory action must enqueue a route request.");
            Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, gateway.RouteRequests[1].Intent, "Open Armory action must use a menu-route intent.");
            Assert.AreEqual(UIRoute.Armory, gateway.RouteRequests[1].Route, "Open Armory action must route to Armory.");
            Assert.IsTrue(gateway.RouteRequests[1].PushHistory, "Open Armory action must push route history.");
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitViewsDoNotOwnFramePolling()
    {
        var violations = new List<string>();

        foreach (string path in Directory.GetFiles("Assets/Game/Scripts/UI/Toolkit", "*View.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);
            if (source.Contains("void Update(", StringComparison.Ordinal))
                violations.Add($"{path} -> Update");
            if (source.Contains("void LateUpdate(", StringComparison.Ordinal))
                violations.Add($"{path} -> LateUpdate");
            if (source.Contains("IEnumerator", StringComparison.Ordinal))
                violations.Add($"{path} -> coroutine");
        }

        AssertNoViolations(violations, "UI Toolkit *View classes must not own frame polling or coroutines.");
    }

    [Test]
    public void UiToolkitShellUsesFluidFullscreenScaffold()
    {
        string source = File.ReadAllText(ShellUssPath);
        string rootBlock = GetCssBlock(source, ".ui-shell-app-canvas");
        string fullscreenBlock = GetCssBlock(source, ".safe-area-root,");

        AssertCssContains(rootBlock, "width: 100%;");
        AssertCssContains(rootBlock, "height: 100%;");
        AssertCssContains(rootBlock, "position: relative;");
        AssertCssContains(rootBlock, "overflow: hidden;");
        Assert.IsFalse(Regex.IsMatch(rootBlock, @"(?m)^\s*(width|height)\s*:\s*\d+(\.\d+)?px\s*;"), "Shell root must stay fluid and must not use fixed pixel width/height.");

        AssertCssContains(fullscreenBlock, "position: absolute;");
        AssertCssContains(fullscreenBlock, "left: 0;");
        AssertCssContains(fullscreenBlock, "right: 0;");
        AssertCssContains(fullscreenBlock, "top: 0;");
        AssertCssContains(fullscreenBlock, "bottom: 0;");

        string uxml = File.ReadAllText(ShellUxmlPath);
        StringAssert.Contains("name=\"UIShellAppCanvas\"", uxml);
        StringAssert.Contains("name=\"SafeAreaRoot\"", uxml);
        StringAssert.Contains("name=\"LoadingLayer\"", uxml);
        StringAssert.Contains("name=\"PopupScreenSlot\"", uxml);
    }

    [Test]
    public void UiToolkitShellAspectSmokeCoversSixteenNineAndTwentyNine()
    {
        string source = File.ReadAllText(ShellUssPath);
        string headerBlock = GetCssBlock(source, ".header-bar");
        string footerBlock = GetCssBlock(source, ".footer-bar");
        string placeholderBlock = GetCssBlock(source, ".placeholder-popup");

        float headerHeightPercent = ReadPercentProperty(headerBlock, "height");
        float footerHeightPercent = ReadPercentProperty(footerBlock, "height");
        AssertShellAspect("16:9", 1600f, 900f, headerHeightPercent, footerHeightPercent, placeholderBlock);
        AssertShellAspect("20:9", 2000f, 900f, headerHeightPercent, footerHeightPercent, placeholderBlock);
    }

    [Test]
    public void UiToolkitShellViewBindsRequiredScreenSlotsByName()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellScreenSlotSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed only when required screen slots are bound.");
            Assert.IsTrue(shellView.HasRequiredScreenSlots, "Shell view must cache every required screen slot.");
            Assert.IsNotNull(shellView.LoadingScreenSlot, "Missing LoadingScreenSlot.");
            Assert.IsNotNull(shellView.MainMenuScreenSlot, "Missing MainMenuScreenSlot.");
            Assert.IsNotNull(shellView.MatchScreenSlot, "Missing MatchScreenSlot.");
            Assert.IsNotNull(shellView.ArmoryScreenSlot, "Missing ArmoryScreenSlot.");
            Assert.IsNotNull(shellView.CommanderProfileScreenSlot, "Missing CommanderProfileScreenSlot.");
            Assert.IsNotNull(shellView.ResultScreenSlot, "Missing ResultScreenSlot.");
            Assert.IsNotNull(shellView.PopupScreenSlot, "Missing PopupScreenSlot.");

            shellView.ClearCache();

            Assert.IsFalse(shellView.HasRequiredScreenSlots, "ClearCache must clear required-screen-slot state.");
            Assert.IsNull(shellView.LoadingScreenSlot, "ClearCache must clear LoadingScreenSlot.");
            Assert.IsNull(shellView.PopupScreenSlot, "ClearCache must clear PopupScreenSlot.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void UiToolkitShellMotionStyleClassesExist()
    {
        string source = File.ReadAllText(ShellUssPath);

        Assert.That(source, Does.Contain(".shell-motion "), "Shell USS must define the shared motion base class.");
        Assert.That(source, Does.Contain(".shell-motion-visible"), "Shell USS must define visible motion state.");
        Assert.That(source, Does.Contain(".shell-motion-fade-out"), "Shell USS must define fade-out motion state.");
        Assert.That(source, Does.Contain(".shell-motion-slide-left-out"), "Shell USS must define left slide motion state.");
        Assert.That(source, Does.Contain(".shell-motion-slide-right-out"), "Shell USS must define right slide motion state.");
        Assert.That(source, Does.Contain(".shell-motion-slide-top-out"), "Shell USS must define top slide motion state.");
        Assert.That(source, Does.Contain(".shell-motion-slide-bottom-out"), "Shell USS must define bottom slide motion state.");
        Assert.That(source, Does.Contain(".shell-motion-scale-out"), "Shell USS must define scale-out motion state.");
        Assert.That(source, Does.Contain(".shell-motion-popup-visible"), "Shell USS must define popup visible motion state.");
        Assert.That(source, Does.Contain(".shell-motion-popup-hidden"), "Shell USS must define popup hidden motion state.");
    }

    [Test]
    public void UiToolkitShellViewAppliesMotionStateClasses()
    {
        VisualTreeAsset shellAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellUxmlPath);
        Assert.IsNotNull(shellAsset, $"Missing shell UXML asset: {ShellUxmlPath}");

        GameObject host = new("UiToolkitShellMotionSmoke");
        try
        {
            UIDocument document = host.AddComponent<UIDocument>();
            UiToolkitShellView shellView = host.AddComponent<UiToolkitShellView>();
            shellView.Configure(document, shellAsset);

            Assert.IsTrue(shellView.Mount(), "Shell mount must succeed before applying motion states.");

            shellView.ApplyShellMotion(shellView.HeaderBar, UiToolkitShellMotionState.SlideTopOut);

            Assert.IsTrue(shellView.HeaderBar.ClassListContains(UiToolkitShellView.MotionBaseClass), "Motion target must include the motion base class.");
            Assert.IsTrue(shellView.HeaderBar.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.SlideTopOut)), "Motion target must include the requested state class.");
            Assert.IsFalse(shellView.HeaderBar.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)), "Motion target must not retain stale state classes.");

            shellView.ApplyShellMotion(shellView.HeaderBar, UiToolkitShellMotionState.Visible);

            Assert.IsTrue(shellView.HeaderBar.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)), "Motion target must switch to visible state.");
            Assert.IsFalse(shellView.HeaderBar.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.SlideTopOut)), "Motion target must remove previous slide state.");

            shellView.ApplyShellMotion(shellView.PopupScreenSlot, UiToolkitShellMotionState.PopupHidden);

            Assert.IsTrue(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupHidden)), "Popup slot must support popup scale hidden state.");

            shellView.RemoveShellMotion(shellView.PopupScreenSlot);

            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.MotionBaseClass), "RemoveShellMotion must clear the base class.");
            Assert.IsFalse(shellView.PopupScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.PopupHidden)), "RemoveShellMotion must clear state classes.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private static IEnumerable<string> EnumerateAssets(params string[] patterns)
    {
        for (int i = 0; i < patterns.Length; i++)
        {
            string[] paths = Directory.GetFiles(UiToolkitRoot, patterns[i], SearchOption.AllDirectories);
            Array.Sort(paths, StringComparer.Ordinal);
            for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
                yield return NormalizeAssetPath(paths[pathIndex]);
        }
    }

    private static string ResolveAssetPath(string sourcePath, string url)
    {
        string normalized = url.Replace('\\', '/');
        if (normalized.StartsWith("project://database/", StringComparison.Ordinal))
            normalized = normalized.Substring("project://database/".Length);
        if (normalized.StartsWith("/", StringComparison.Ordinal))
            normalized = normalized.TrimStart('/');
        if (normalized.StartsWith("Assets/", StringComparison.Ordinal))
            return normalized;

        string sourceDirectory = Path.GetDirectoryName(sourcePath) ?? UiToolkitRoot;
        string combined = Path.GetFullPath(Path.Combine(sourceDirectory, normalized));
        string projectRoot = Path.GetFullPath(".");
        if (!combined.StartsWith(projectRoot, StringComparison.Ordinal))
            return string.Empty;

        return NormalizeAssetPath(Path.GetRelativePath(projectRoot, combined));
    }

    private static bool ShouldSkipUrl(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.StartsWith("#", StringComparison.Ordinal)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAssetPath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string FindNearestAsmdef(string normalizedPath)
    {
        string directory = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/') ?? string.Empty;
        while (!string.IsNullOrEmpty(directory) && directory.StartsWith("Assets/", StringComparison.Ordinal))
        {
            string[] asmdefs = Directory.GetFiles(directory, "*.asmdef", SearchOption.TopDirectoryOnly);
            if (asmdefs.Length > 0)
            {
                Array.Sort(asmdefs, StringComparer.Ordinal);
                return NormalizeAssetPath(asmdefs[0]);
            }

            directory = Path.GetDirectoryName(directory)?.Replace('\\', '/') ?? string.Empty;
        }

        return string.Empty;
    }

    private static void CheckHotPathSourceForAllocationPatterns(
        string normalizedPath,
        string[] allocationTokens,
        List<string> violations)
    {
        if (!File.Exists(normalizedPath))
            return;

        string source = File.ReadAllText(normalizedPath);
        for (int tokenIndex = 0; tokenIndex < allocationTokens.Length; tokenIndex++)
        {
            string token = allocationTokens[tokenIndex];
            if (source.Contains(token, StringComparison.Ordinal))
                violations.Add($"{normalizedPath} -> {token}");
        }
    }

    private static bool IsEcsSystemSource(string normalizedPath)
    {
        return normalizedPath.StartsWith("Assets/Game/Scripts/", StringComparison.Ordinal)
            && !normalizedPath.Contains("/Editor/", StringComparison.Ordinal)
            && !normalizedPath.Contains("/Tests/", StringComparison.Ordinal);
    }

    private static bool LooksLikeEcsSystemSource(string normalizedPath, string source)
    {
        return normalizedPath.EndsWith("System.cs", StringComparison.Ordinal)
            || source.Contains(": SystemBase", StringComparison.Ordinal)
            || source.Contains(": ISystem", StringComparison.Ordinal)
            || source.Contains("[UpdateInGroup(", StringComparison.Ordinal);
    }

    private static bool IsApprovedLegacyCanvasFallbackSource(string normalizedPath)
    {
        if (normalizedPath.StartsWith("Assets/Game/Scripts/UI/Toolkit/", StringComparison.Ordinal))
            return false;
        if (normalizedPath.StartsWith("Assets/Game/Scripts/UI/Shell/Ecs/", StringComparison.Ordinal))
            return false;

        return normalizedPath.StartsWith("Assets/Game/Scripts/UI/", StringComparison.Ordinal) ||
               normalizedPath == "Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs" ||
               normalizedPath == "Assets/Game/Scripts/Composition/MenuBootstrapView.cs";
    }

    private static string GetCssBlock(string source, string selector)
    {
        int selectorIndex = source.IndexOf(selector, StringComparison.Ordinal);
        Assert.GreaterOrEqual(selectorIndex, 0, $"Missing USS selector: {selector}");

        int openBrace = source.IndexOf('{', selectorIndex);
        Assert.GreaterOrEqual(openBrace, 0, $"Missing opening brace for USS selector: {selector}");

        int closeBrace = source.IndexOf('}', openBrace + 1);
        Assert.GreaterOrEqual(closeBrace, 0, $"Missing closing brace for USS selector: {selector}");

        return source.Substring(openBrace + 1, closeBrace - openBrace - 1);
    }

    private static string GetLastCssBlock(string source, string selector)
    {
        int selectorIndex = source.LastIndexOf(selector, StringComparison.Ordinal);
        Assert.GreaterOrEqual(selectorIndex, 0, $"Missing USS selector: {selector}");

        int openBrace = source.IndexOf('{', selectorIndex);
        Assert.GreaterOrEqual(openBrace, 0, $"Missing opening brace for USS selector: {selector}");

        int closeBrace = source.IndexOf('}', openBrace + 1);
        Assert.GreaterOrEqual(closeBrace, 0, $"Missing closing brace for USS selector: {selector}");

        return source.Substring(openBrace + 1, closeBrace - openBrace - 1);
    }

    private static void AssertCssContains(string block, string declaration)
    {
        StringAssert.Contains(declaration, block);
    }

    private static void AssertCssContains(string block, string declaration, string message)
    {
        StringAssert.Contains(declaration, block, message);
    }

    private static void AssertCssSelectorMissing(string source, string selector, string message)
    {
        Assert.Less(source.IndexOf(selector, StringComparison.Ordinal), 0, message);
    }

    private static float ReadPercentProperty(string block, string propertyName)
    {
        Match match = Regex.Match(
            block,
            $@"(?m)^\s*{Regex.Escape(propertyName)}\s*:\s*(?<value>-?\d+(\.\d+)?)%\s*;",
            RegexOptions.CultureInvariant);
        Assert.IsTrue(match.Success, $"Missing percent USS property: {propertyName}");
        return float.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
    }

    private static float ReadNumberProperty(string block, string propertyName)
    {
        Match match = Regex.Match(
            block,
            $@"(?m)^\s*{Regex.Escape(propertyName)}\s*:\s*(?<value>-?\d+(\.\d+)?)(px|%)?\s*;?",
            RegexOptions.CultureInvariant);
        Assert.IsTrue(match.Success, $"Missing numeric USS property: {propertyName}");
        return float.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
    }

    private static bool TryReadPercentProperty(string block, string propertyName, out float value)
    {
        Match match = Regex.Match(
            block,
            $@"(?m)^\s*{Regex.Escape(propertyName)}\s*:\s*(?<value>-?\d+(\.\d+)?)%\s*;",
            RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            value = 0f;
            return false;
        }

        value = float.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
        return true;
    }

    private static void AssertPanelWithinColumn(string block, string label)
    {
        float height = ReadPercentProperty(block, "height");
        Assert.Greater(height, 0f, $"{label} must have a positive height.");
        Assert.LessOrEqual(height, 100f, $"{label} must fit within its parent column.");

        if (TryReadPercentProperty(block, "top", out float top))
            Assert.LessOrEqual(top + height, 100f, $"{label} top + height must stay inside its column.");

        if (TryReadPercentProperty(block, "bottom", out float bottom))
            Assert.LessOrEqual(bottom + height, 100f, $"{label} bottom + height must stay inside its column.");

        if (TryReadPercentProperty(block, "width", out float width))
            Assert.LessOrEqual(width, 100f, $"{label} width must stay inside its parent column.");
    }

    private static void AssertSymmetricInsets(string block, string label)
    {
        float left = ReadPercentOrNumberProperty(block, "left");
        float right = ReadPercentOrNumberProperty(block, "right");
        float top = ReadPercentOrNumberProperty(block, "top");
        float bottom = ReadPercentOrNumberProperty(block, "bottom");

        Assert.AreEqual(left, right, 0.01f, $"{label} must use symmetric horizontal insets.");
        Assert.AreEqual(top, bottom, 0.01f, $"{label} must use symmetric vertical insets.");
    }

    private static void AssertVerticalCenterFromTopHeight(string block, string label)
    {
        float top = ReadPercentProperty(block, "top");
        float height = ReadPercentProperty(block, "height");
        Assert.AreEqual(50f, top + height * 0.5f, 0.01f, $"{label} must be vertically centered in its section.");
    }

    private static void AssertSymmetricSlice(string block, string label)
    {
        float left = ReadNumberProperty(block, "-unity-slice-left");
        float right = ReadNumberProperty(block, "-unity-slice-right");
        float top = ReadNumberProperty(block, "-unity-slice-top");
        float bottom = ReadNumberProperty(block, "-unity-slice-bottom");

        Assert.AreEqual(left, right, 0.01f, $"{label} must use symmetric left/right slice values.");
        Assert.AreEqual(top, bottom, 0.01f, $"{label} must use symmetric top/bottom slice values.");
    }

    private static float ReadPercentOrNumberProperty(string block, string propertyName)
    {
        Match match = Regex.Match(
            block,
            $@"(?m)^\s*{Regex.Escape(propertyName)}\s*:\s*(?<value>-?\d+(\.\d+)?)(%?)\s*;",
            RegexOptions.CultureInvariant);
        Assert.IsTrue(match.Success, $"Missing USS property: {propertyName}");
        return float.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
    }

    private static void AssertShellAspect(
        string label,
        float width,
        float height,
        float headerHeightPercent,
        float footerHeightPercent,
        string placeholderBlock)
    {
        Rect rootRect = new(0f, 0f, width, height);
        Rect headerRect = new(0f, 0f, width, height * headerHeightPercent / 100f);
        Rect footerRect = new(0f, height - (height * footerHeightPercent / 100f), width, height * footerHeightPercent / 100f);
        Rect placeholderRect = CreateInsetRect(
            width,
            height,
            ReadPercentProperty(placeholderBlock, "left"),
            ReadPercentProperty(placeholderBlock, "right"),
            ReadPercentProperty(placeholderBlock, "top"),
            ReadPercentProperty(placeholderBlock, "bottom"));

        AssertRectInside(rootRect, headerRect, $"{label} header");
        AssertRectInside(rootRect, footerRect, $"{label} footer");
        AssertRectInside(rootRect, placeholderRect, $"{label} placeholder popup");

        Assert.Greater(headerRect.height, 48f, $"{label} header must remain visible.");
        Assert.Greater(footerRect.height, 48f, $"{label} footer must remain visible.");
        Assert.Greater(placeholderRect.width, width * 0.25f, $"{label} popup must keep usable width.");
        Assert.Greater(placeholderRect.height, height * 0.35f, $"{label} popup must keep usable height.");
    }

    private static Rect CreateInsetRect(float width, float height, float leftPercent, float rightPercent, float topPercent, float bottomPercent)
    {
        float left = width * leftPercent / 100f;
        float right = width * rightPercent / 100f;
        float top = height * topPercent / 100f;
        float bottom = height * bottomPercent / 100f;
        return new Rect(left, top, width - left - right, height - top - bottom);
    }

    private static void AssertRectInside(Rect outer, Rect inner, string label)
    {
        Assert.Greater(inner.width, 0f, $"{label} width must be positive.");
        Assert.Greater(inner.height, 0f, $"{label} height must be positive.");
        Assert.GreaterOrEqual(inner.xMin, outer.xMin, $"{label} must not clip left.");
        Assert.GreaterOrEqual(inner.yMin, outer.yMin, $"{label} must not clip top.");
        Assert.LessOrEqual(inner.xMax, outer.xMax, $"{label} must not clip right.");
        Assert.LessOrEqual(inner.yMax, outer.yMax, $"{label} must not clip bottom.");
    }

    private static void AssertDrawsAfter(VisualElement parent, VisualElement upper, VisualElement lower, string message)
    {
        int upperIndex = GetChildIndex(parent, upper);
        int lowerIndex = GetChildIndex(parent, lower);

        Assert.GreaterOrEqual(upperIndex, 0, $"Upper element is not a direct child. {message}");
        Assert.GreaterOrEqual(lowerIndex, 0, $"Lower element is not a direct child. {message}");
        Assert.Greater(upperIndex, lowerIndex, message);
    }

    private static int GetChildIndex(VisualElement parent, VisualElement child)
    {
        int index = 0;
        foreach (VisualElement element in parent.Children())
        {
            if (ReferenceEquals(element, child))
                return index;
            index++;
        }

        return -1;
    }

    private static void AssertNoViolations(List<string> violations, string message)
    {
        violations.Sort(StringComparer.Ordinal);
        Assert.IsEmpty(violations, $"{message}\n{string.Join("\n", violations)}");
    }

    private static void AssertPercentWidth(VisualElement element, float expectedPercent, string label)
    {
        Assert.IsNotNull(element, $"{label} element is missing.");
        StyleLength width = element.style.width;
        Assert.AreEqual(LengthUnit.Percent, width.value.unit, $"{label} must use retained percentage width instead of recreating elements.");
        Assert.AreEqual(expectedPercent, width.value.value, 0.01f, $"{label} width mismatch.");
    }

    private static void AssertBackgroundSprite(VisualElement element, Sprite expectedSprite, string label)
    {
        Assert.IsNotNull(element, $"{label} element is missing.");
        Assert.AreSame(expectedSprite, element.style.backgroundImage.value.sprite, $"{label} sprite mismatch.");
    }

    private static Sprite CreateTestSprite(string name)
    {
        Sprite sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f));
        sprite.name = name;
        return sprite;
    }

    private static void AssertPercentStyle(VisualElement element, string propertyName, float expectedPercent, string label)
    {
        Assert.IsNotNull(element, $"{label} element is missing.");
        StyleLength styleLength = propertyName switch
        {
            "left" => element.style.left,
            "top" => element.style.top,
            "height" => element.style.height,
            _ => throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, "Unsupported percent style property.")
        };
        Assert.AreEqual(LengthUnit.Percent, styleLength.value.unit, $"{label} must use percentage style values.");
        Assert.AreEqual(expectedPercent, styleLength.value.value, 0.01f, $"{label} mismatch.");
    }

    private static Button RequireButton(VisualElement root, string name)
    {
        Assert.IsNotNull(root, "Cannot query a button from a null root.");
        Button button = root.Q<Button>(name);
        Assert.IsNotNull(button, $"Missing required button: {name}");
        return button;
    }

    private static void AssertBuildProductionCase(
        string requestSystemSource,
        string actionName,
        string productionKind,
        string queueSlotExpression)
    {
        Match actionCase = Regex.Match(
            requestSystemSource,
            $@"case\s+UiActionKind\.{actionName}:(?<body>.*?)break;",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.IsTrue(actionCase.Success, $"UI action request system must process {actionName}.");
        string body = actionCase.Groups["body"].Value;
        StringAssert.Contains("CaptureUiClickSequence", body, $"{actionName} must suppress the underlying world click.");
        StringAssert.Contains("EnqueueBuildProductionRequest", body, $"{actionName} must enqueue an ECS production request.");
        StringAssert.Contains(productionKind, body, $"{actionName} must map to {productionKind}.");
        StringAssert.Contains(queueSlotExpression, body, $"{actionName} must preserve the expected queue slot expression.");
    }

    private static void AssertHeaderActionButtonUsesSharedFrame(VisualElement root, string buttonName, string iconClass)
    {
        Button button = RequireButton(root, buttonName);
        Assert.IsTrue(button.ClassListContains("header-icon-button"), $"{buttonName} must use the shared header action hit-area class.");
        Assert.IsTrue(string.IsNullOrEmpty(button.text), $"{buttonName} must not add text that changes the hit rect or visual frame layout.");

        VisualElement frame = button.Q<VisualElement>("Frame");
        Assert.IsNotNull(frame, $"{buttonName} must keep a visible frame child.");
        Assert.AreSame(button, frame.parent, $"{buttonName} frame must be a direct child so it fills the button hit rect.");
        Assert.IsTrue(frame.ClassListContains("header-icon-button-frame"), $"{buttonName} frame must use the shared header frame class.");

        VisualElement icon = button.Q<VisualElement>("Icon");
        Assert.IsNotNull(icon, $"{buttonName} must keep a visible icon child.");
        Assert.AreSame(button, icon.parent, $"{buttonName} icon must be a direct child of the hit rect.");
        Assert.IsTrue(icon.ClassListContains("header-action-icon"), $"{buttonName} icon must use the shared centered icon class.");
        Assert.IsTrue(icon.ClassListContains(iconClass), $"{buttonName} icon must use the expected semantic icon class.");
    }

    private static void AssertModeCardUpperArtHasSafePadding(string artBlock)
    {
        float left = ReadPercentProperty(artBlock, "left");
        float right = ReadPercentProperty(artBlock, "right");
        float top = ReadPercentProperty(artBlock, "top");
        float height = ReadPercentProperty(artBlock, "height");
        float artBottom = top + height;

        Assert.AreEqual(left, right, 0.01f, "Mode card portrait well must use symmetric horizontal padding.");
        Assert.GreaterOrEqual(left, 3f, "Mode card portrait must not touch the side chrome.");
        Assert.GreaterOrEqual(top, 3f, "Mode card portrait must not touch the top chrome.");
        Assert.Greater(height, 60f, "Mode card portrait well must stay large like the target mockup.");
        Assert.LessOrEqual(artBottom, 76f, "Mode card portrait well must leave room for the title/label plate.");
    }

    private static void AssertModeCardTitleInsideLabelPlate(string titleBlock, string labelPlateBlock)
    {
        float labelBottom = ReadPercentProperty(labelPlateBlock, "bottom");
        float labelHeight = ReadPercentProperty(labelPlateBlock, "height");
        float labelTop = 100f - labelBottom - labelHeight;
        float labelBottomEdge = 100f - labelBottom;

        float titleLeft = ReadPercentProperty(titleBlock, "left");
        float titleRight = ReadPercentProperty(titleBlock, "right");
        float titleBottom = ReadPercentProperty(titleBlock, "bottom");
        float titleHeight = ReadPercentProperty(titleBlock, "height");
        float titleTop = 100f - titleBottom - titleHeight;
        float titleBottomEdge = 100f - titleBottom;
        float titleFont = ReadNumberProperty(titleBlock, "font-size");

        Assert.AreEqual(titleLeft, titleRight, 0.01f, "Mode card title must use symmetric side padding.");
        Assert.GreaterOrEqual(titleLeft, 2f, "Mode card title must not touch side chrome.");
        Assert.Greater(titleFont, 54f, "Mode card title font must stay readable against the target mockup.");
        Assert.GreaterOrEqual(titleTop, labelTop + 3f, "Mode card title must stay inside the label plate top padding.");
        Assert.LessOrEqual(titleBottomEdge, labelBottomEdge - 3f, "Mode card title must stay inside the label plate bottom padding.");
    }

    private static void AssertModeCardBadgeInsideJunction(string badgeFrameBlock, string badgeIconBlock)
    {
        float frameBottom = ReadPercentProperty(badgeFrameBlock, "bottom");
        float frameHeight = ReadPercentProperty(badgeFrameBlock, "height");
        float frameTop = 100f - frameBottom - frameHeight;
        float frameBottomEdge = 100f - frameBottom;

        float iconBottom = ReadPercentProperty(badgeIconBlock, "bottom");
        float iconHeight = ReadPercentProperty(badgeIconBlock, "height");
        float iconTop = 100f - iconBottom - iconHeight;
        float iconBottomEdge = 100f - iconBottom;

        AssertCssContains(badgeFrameBlock, "left: 50%;", "Mode badge frame must stay centered.");
        AssertCssContains(badgeFrameBlock, "translate: -50% 0;", "Mode badge frame must be centered by visible bounds.");
        AssertCssContains(badgeIconBlock, "left: 50%;", "Mode badge icon must stay centered.");
        AssertCssContains(badgeIconBlock, "translate: -50% 0;", "Mode badge icon must be centered by visible bounds.");
        Assert.Greater(frameHeight, 10f, "Mode badge frame must remain readable.");
        Assert.GreaterOrEqual(iconTop, frameTop + 1f, "Mode badge icon must stay inside the badge frame.");
        Assert.LessOrEqual(iconBottomEdge, frameBottomEdge - 1f, "Mode badge icon must stay inside the badge frame.");
    }

    private static void AssertModeCardBottomDecorInsideLabelPlate(string dividerBlock, string bottomStarBlock, string labelPlateBlock)
    {
        float labelBottom = ReadPercentProperty(labelPlateBlock, "bottom");
        float labelHeight = ReadPercentProperty(labelPlateBlock, "height");
        float labelTop = 100f - labelBottom - labelHeight;
        float labelBottomEdge = 100f - labelBottom;

        float dividerBottom = ReadPercentProperty(dividerBlock, "bottom");
        float dividerTop = 100f - dividerBottom - ReadNumberProperty(dividerBlock, "height");
        float starBottom = ReadPercentProperty(bottomStarBlock, "bottom");
        float starHeight = ReadPercentProperty(bottomStarBlock, "height");
        float starTop = 100f - starBottom - starHeight;
        float starBottomEdge = 100f - starBottom;

        Assert.GreaterOrEqual(dividerTop, labelTop, "Mode divider must stay inside the lower label plate.");
        Assert.LessOrEqual(starBottomEdge, labelBottomEdge, "Mode bottom star must stay inside the lower label plate.");
        Assert.GreaterOrEqual(starTop, labelTop, "Mode bottom star must stay inside the lower label plate.");
        AssertCssContains(bottomStarBlock, "left: 50%;", "Mode bottom star must stay centered.");
        AssertCssContains(bottomStarBlock, "translate: -50% 0;", "Mode bottom star must be centered by visible bounds.");
    }

    private static void AssertModeCardStructure(
        VisualElement root,
        string cardName,
        string titleText,
        string artClass,
        string frameClass,
        string labelPlateClass,
        string badgeIconClass)
    {
        Button card = RequireButton(root, cardName);
        Assert.IsTrue(card.ClassListContains("mode-card"), $"{cardName} must use the shared mode-card layout class.");
        Assert.IsTrue(string.IsNullOrEmpty(card.text), $"{cardName} must not add button text that shifts inner layout.");

        AssertDirectChildHasClass(card, "Fill", "mode-card-fill", cardName);
        AssertDirectChildHasClass(card, "Art", "mode-card-art", cardName);
        AssertDirectChildHasClass(card, "Art", artClass, cardName);
        AssertDirectChildHasClass(card, "Frame", "mode-card-frame", cardName);
        AssertDirectChildHasClass(card, "Frame", frameClass, cardName);
        AssertDirectChildHasClass(card, "LabelPlate", "mode-card-label-plate", cardName);
        AssertDirectChildHasClass(card, "LabelPlate", labelPlateClass, cardName);
        AssertDirectChildHasClass(card, "BadgeFrame", "mode-badge-frame", cardName);
        AssertDirectChildHasClass(card, "BadgeIcon", "mode-badge-icon", cardName);
        AssertDirectChildHasClass(card, "BadgeIcon", badgeIconClass, cardName);
        AssertDirectChildHasClass(card, "BottomStar", "mode-bottom-star", cardName);

        Label title = card.Q<Label>("Title");
        Assert.IsNotNull(title, $"{cardName} must expose a title label.");
        Assert.AreSame(card, title.parent, $"{cardName} title must be a direct child so USS safe-padding rules apply.");
        Assert.IsTrue(title.ClassListContains("mode-title"), $"{cardName} title must use the shared mode-title class.");
        Assert.AreEqual(titleText, title.text, $"{cardName} title text mismatch.");
    }

    private static void AssertDirectChildHasClass(VisualElement parent, string childName, string className, string ownerName)
    {
        VisualElement child = parent.Q<VisualElement>(childName);
        Assert.IsNotNull(child, $"{ownerName} missing child: {childName}");
        Assert.AreSame(parent, child.parent, $"{ownerName}/{childName} must be a direct child so the USS geometry remains deterministic.");
        Assert.IsTrue(child.ClassListContains(className), $"{ownerName}/{childName} missing class: {className}");
    }

    private static void AssertVisibleLabelText(Label label, string labelName, string expectedText)
    {
        Assert.IsNotNull(label, $"{labelName} is missing.");
        Assert.AreEqual(expectedText, label.text, $"{labelName} text mismatch.");
        Assert.IsFalse(HasHiddenClassInAncestorChain(label), $"{labelName} must not be under a shell-hidden element while loading is shown.");
        Assert.AreNotEqual(DisplayStyle.None, label.resolvedStyle.display, $"{labelName} must not resolve to display none while loading is shown.");
    }

    private static bool HasHiddenClassInAncestorChain(VisualElement element)
    {
        for (VisualElement current = element; current != null; current = current.parent)
        {
            if (current.ClassListContains("shell-hidden"))
                return true;
        }

        return false;
    }

    private static RuntimeUiConfig CreateRuntimeUiConfig(RuntimeUiMode mode)
    {
        RuntimeUiConfig config = ScriptableObject.CreateInstance<RuntimeUiConfig>();
        var serializedObject = new SerializedObject(config);
        SerializedProperty modeProperty = serializedObject.FindProperty("mode");
        Assert.IsNotNull(modeProperty, "RuntimeUiConfig must keep a serialized mode field for asset editing.");
        modeProperty.enumValueIndex = (int)mode;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return config;
    }

    private sealed class RuntimeUiModeSmokeScope : IDisposable
    {
        private readonly GameObject host;
        private readonly RuntimeUiConfig config;

        public RuntimeUiModeSmokeScope(RuntimeUiMode mode)
        {
            host = new GameObject("RuntimeUiModeSmoke");
            GameObject canvasObject = new("CanvasFallback");
            GameObject documentObject = new("UiToolkitDocument");
            UiToolkitRoot = new GameObject("UiToolkitShellRoot");
            GameObject legacyPresentationObject = new("LegacyCanvasPresentation");
            GameObject legacyContentObject = new("LegacyCanvasContent");
            GameObject legacyRouterObject = new("LegacyCanvasRouter");

            canvasObject.transform.SetParent(host.transform);
            documentObject.transform.SetParent(host.transform);
            UiToolkitRoot.transform.SetParent(host.transform);
            legacyPresentationObject.transform.SetParent(host.transform);
            legacyContentObject.transform.SetParent(host.transform);
            legacyRouterObject.transform.SetParent(host.transform);

            BootstrapView = host.AddComponent<MenuBootstrapView>();
            Canvas = canvasObject.AddComponent<Canvas>();
            UiDocument = documentObject.AddComponent<UIDocument>();
            LegacyShellPresentation = legacyPresentationObject.AddComponent<UIShellEcsPresentationSystem>();
            LegacyContentSystem = legacyContentObject.AddComponent<UIShellContentView>();
            LegacyRouter = legacyRouterObject.AddComponent<UIRouterView>();
            config = CreateRuntimeUiConfig(mode);

            BootstrapView.Configure(null, Canvas, null, LegacyShellPresentation, LegacyContentSystem, LegacyRouter, config, UiDocument, UiToolkitRoot, null);
        }

        public MenuBootstrapView BootstrapView { get; }
        public Canvas Canvas { get; }
        public UIDocument UiDocument { get; }
        public GameObject UiToolkitRoot { get; }
        public UIShellEcsPresentationSystem LegacyShellPresentation { get; }
        public UIShellContentView LegacyContentSystem { get; }
        public UIRouterView LegacyRouter { get; }

        public void Dispose()
        {
            if (host != null)
                UnityEngine.Object.DestroyImmediate(host);
            if (config != null)
                UnityEngine.Object.DestroyImmediate(config);
        }
    }

    private readonly struct RecordedRouteRequest
    {
        public readonly UiShellRouteIntent Intent;
        public readonly UIRoute Route;
        public readonly bool PushHistory;

        public RecordedRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
        {
            Intent = intent;
            Route = route;
            PushHistory = pushHistory;
        }
    }

    private readonly struct RecordedUiActionRequest
    {
        public readonly UiActionKind Kind;
        public readonly int PayloadId;

        public RecordedUiActionRequest(UiActionKind kind, int payloadId)
        {
            Kind = kind;
            PayloadId = payloadId;
        }
    }

    private sealed class RecordingUiShellRuntimeGateway : IUiShellRuntimeGateway
    {
        public readonly List<RecordedRouteRequest> RouteRequests = new();
        public readonly List<RecordedUiActionRequest> UiActionRequests = new();
        public readonly List<ArmoryCatalogCategory> ArmoryCategoryRequests = new();
        public UiShellStateModel ShellState;
        public UiDiagnosticsOverlayModel DiagnosticsOverlay;
        public UiShellCommanderProfileModel CommanderProfile;
        public UiShellMainMenuResourcesModel MainMenuResources;
        public UiMissionResultPopupModel MissionResult;
        public UiMatchHudSelectionPanelModel MatchHudSelection;
        public UiMatchHudCommandStateModel MatchHudCommandState;
        public UiMatchHudHeaderModel MatchHudHeader;
        public UiMatchHudStatusSurfacesModel MatchHudStatusSurfaces;
        public UiMatchHudMinimapModel MatchHudMinimap;
        public UiMatchHudPassengerDrawerModel MatchHudPassengerDrawer;
        public UiMatchHudSquadTrayModel MatchHudSquadTray;
        public UiBuildDrawerModel BuildDrawer;
        public UiBuildPlacementConfirmationBarModel BuildPlacementConfirmationBar;
        public ArmoryCatalogCategory ArmoryCategory;
        public bool HasShellState;
        public bool HasDiagnosticsOverlay;
        public bool HasCommanderProfile;
        public bool HasMainMenuResources;
        public bool HasMissionResult;
        public bool HasMatchHudSelection;
        public bool HasMatchHudCommandState;
        public bool HasMatchHudHeader;
        public bool HasMatchHudStatusSurfaces;
        public bool HasMatchHudMinimap;
        public bool HasMatchHudPassengerDrawer;
        public bool HasMatchHudSquadTray;
        public bool HasBuildDrawer;
        public bool HasBuildPlacementConfirmationBar;
        public bool HasArmoryCategory;

        public bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
        {
            RouteRequests.Add(new RecordedRouteRequest(intent, route, pushHistory));
            return true;
        }

        public bool TryEnqueueUiAction(UiActionKind kind, int payloadId)
        {
            UiActionRequests.Add(new RecordedUiActionRequest(kind, payloadId));
            return true;
        }

        public bool TryReadLoadingProgress(out UiShellLoadingProgressModel loading)
        {
            loading = default;
            return false;
        }

        public bool TrySetLoadingProgress(float progress01, string status, bool complete)
        {
            return false;
        }

        public bool TryReadDiagnosticsOverlay(out UiDiagnosticsOverlayModel diagnostics)
        {
            diagnostics = DiagnosticsOverlay;
            return HasDiagnosticsOverlay;
        }

        public bool TryReadShellState(out UiShellStateModel state)
        {
            state = ShellState;
            return HasShellState;
        }

        public bool TryReadCommanderProfile(out UiShellCommanderProfileModel profile)
        {
            profile = CommanderProfile;
            return HasCommanderProfile;
        }

        public bool TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources)
        {
            resources = MainMenuResources;
            return HasMainMenuResources;
        }

        public bool TryReadMissionResult(out UiMissionResultPopupModel result)
        {
            result = MissionResult;
            return HasMissionResult;
        }

        public bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection)
        {
            selection = MatchHudSelection;
            return HasMatchHudSelection;
        }

        public bool TryReadMatchHudCommandState(out UiMatchHudCommandStateModel commandState)
        {
            commandState = MatchHudCommandState;
            return HasMatchHudCommandState;
        }

        public bool TryReadMatchHudHeader(out UiMatchHudHeaderModel header)
        {
            header = MatchHudHeader;
            return HasMatchHudHeader;
        }

        public bool TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel statusSurfaces)
        {
            statusSurfaces = MatchHudStatusSurfaces;
            return HasMatchHudStatusSurfaces;
        }

        public bool TryReadMatchHudMinimap(out UiMatchHudMinimapModel minimap)
        {
            minimap = MatchHudMinimap;
            return HasMatchHudMinimap;
        }

        public bool TryReadMatchHudPassengerDrawer(out UiMatchHudPassengerDrawerModel passengerDrawer)
        {
            passengerDrawer = MatchHudPassengerDrawer;
            return HasMatchHudPassengerDrawer;
        }

        public bool TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel squadTray)
        {
            squadTray = MatchHudSquadTray;
            return HasMatchHudSquadTray;
        }

        public bool TryReadBuildDrawer(out UiBuildDrawerModel drawer)
        {
            drawer = BuildDrawer;
            return HasBuildDrawer;
        }

        public bool TryReadBuildPlacementConfirmationBar(out UiBuildPlacementConfirmationBarModel placementBar)
        {
            placementBar = BuildPlacementConfirmationBar;
            return HasBuildPlacementConfirmationBar;
        }

        public bool TryReadArmoryCategory(out ArmoryCatalogCategory category)
        {
            category = ArmoryCategory;
            return HasArmoryCategory;
        }

        public bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
        {
            ArmoryCategoryRequests.Add(category);
            ArmoryCategory = category;
            HasArmoryCategory = true;
            return true;
        }

        public bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands)
        {
            commands?.Clear();
            return false;
        }

        public bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion)
        {
            return false;
        }
    }
}
#endif
