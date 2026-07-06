using Game.UI.Contracts;
using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Rendering;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ScriptArchitectureAlignmentContractTests
{
    private const string GameScriptsRoot = "Assets/Game/Scripts";
    private const string ProjectNamePrefix = "WarlineCapture";
    private const string SelfPath = "Assets/Tests/Editor/ScriptArchitectureAlignmentContractTests.cs";
    private const string LegacyBootstrapRoot = "Assets/Game/Scripts/Bootstrap";
    private const string MainMenuContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
    private const string MatchHudContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string ArmoryContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab";

    private static readonly Dictionary<string, int> RuntimeLookupDebtAllowlist = new(StringComparer.Ordinal)
    {
    };

    private static readonly Dictionary<string, int> RuntimeUiDebugLogDebtAllowlist = new(StringComparer.Ordinal)
    {
        { "Assets/Game/Scripts/UI/Shell/UIShellRouteButtonView.cs|Debug.LogError", 1 },
    };

    private static readonly HashSet<string> NonViewUiMonoBehaviourDebtAllowlist = new(StringComparer.Ordinal)
    {
        "CampListItemViewReferences",
        "MatchHudMinimapZoomPressRelay",
        "UIAccessibilityApplier",
        "UIBootstrap",
        "UIButtonAnimationState",
        "UIShellEcsPresentationSystem",
    };

    private static readonly HashSet<string> BroadNameDebtAllowlist = new(StringComparer.Ordinal)
    {
        "AIControllerConfig",
        "AIControllerSceneConfigAsset",
        "BuildingEntityManagerAccessSystem",
        "BuildingPlacementAdapterCompositionSystemHelper",
    };

    private static readonly string[] BootstrapCompositionGuardrailFiles =
    {
        "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Composition/GameplayFeatureStartupCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/ManagedGameplayStartupSystemHelper.cs",
    };

    private static readonly string[] BootstrapCompositionForbiddenPolicyTokens =
    {
        "FactionEconomy",
        "FactionControlEntry",
        "AIBuildPlan",
        "AIProductionPlan",
        "AISquadPlan",
        "AITargetPrioritySetting",
        "FactionEconomyPolicy",
        "MissionCameraSystem",
        "MissionStartupSystem",
        "AIDiagnosticLog",
        "PerfDiag",
        "FreezeDetect",
        "FrameRateDiag",
    };

    private static readonly string[] BootstrapCompositionForbiddenStandaloneTypes =
    {
        "BuildingPlacementSystem",
        "BuildingGameplaySystem",
        "RTSSelectionSystem",
        "SelectionRuntimeContextSystem",
    };

    private static readonly HashSet<string> RuntimeCompositionSystemHelperLedger = new(StringComparer.Ordinal)
    {
        "Assets/Game/Scripts/Systems/BuildingCitizenPopulationCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplayBindingCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplayDependencyCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplayDisposalCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplayDisposalExecutionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplayEcsQueryCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplayGridDataCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplayResultCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingGameplayStartupCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementAdapterCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementCommandCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementCommandRequestCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementCommitCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementInputTickCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementInteractionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementInteractionContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementLifecycleCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementRedirectCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementSessionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingPlacementVisualUpdateCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingProductionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingProductionContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingProductionTickCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingProductionUpdateCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingResourceHaulerBridgeCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeContextFactoryCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeCreationCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeEntityCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimePublishCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeQueryCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeResourcePrefabCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeResourcePrefabContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeSideEffectCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeTickCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeUpdateCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingSelectionClickCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingSelectionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingSelectionRuntimeCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingUiCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingUiContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenBuildingReadCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenDangerCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenHouseholdRegistrationCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenPopulationCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenPopulationEcsProjectionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenPopulationEventCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenPopulationLifecycleCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenPopulationReadModelCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenPopulationRuntimeUpdateCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenPopulationStateCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenPopulationTotalsCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenRefugeeCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenResourceCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenScheduleCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/CitizenStatusTransitionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/FocusedUnitLifecycleCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildBuildingPlacementCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildCommandCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildCompositionContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildCompositionLifecycleCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildCompositionSourceCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildDependencyCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildDisposalCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildEcsCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildInputCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildInteractionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildInteractionContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildMutationCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildPlacementStorageCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildReadModelCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildRuntimeActionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadBuildSessionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadNetworkCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadRuntimeGenerationCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RoadRuntimeGenerationContextCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RtsSelectionCommandResultFlushCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RtsSelectionFocusCommandCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RtsSelectionInputCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RtsSelectionInputStateCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RtsSelectionPointerTargetCommandCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/RtsSelectionRuntimeInputCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/SelectedUnitOrderSnapshotCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/SelectionBuildingInteractionCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/SelectionRectangleRequestCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/SelectionStateCompositionSystemHelper.cs",
    };

    private static readonly HashSet<string> SelectionPanelConcreteSystemBindingAllowlist = new(StringComparer.Ordinal)
    {
        "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs",
    };

    private static readonly HashSet<string> StaticUiRegistryDebtAllowlist = new(StringComparer.Ordinal);

    private static readonly HashSet<string> StaticGameplayRegistryDebtAllowlist = new(StringComparer.Ordinal)
    {
        "Assets/Game/Scripts/Rendering/SharedPrefabPreviewCache.cs|Cache",
        "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs|EmptyTransformList",
        "Assets/Game/Scripts/Utilities/GameStrings.cs|Entries",
        "Assets/Game/Scripts/Utilities/UnitTransportVisualUtility.cs|RestoreEntries",
        "Assets/Game/Scripts/Utilities/UnitTransportVisualUtility.cs|VisitedEntities",
        "Assets/Game/Scripts/Utilities/UnitTransportVisualUtility.cs|VisualEntities",
    };

    private static readonly string[] RuntimeInstantiateOwnershipScanRoots =
    {
        "Assets/Game/Scripts/Systems",
        "Assets/Game/Scripts/Environment",
        "Assets/Game/Scripts/Rendering/Systems",
        "Assets/Game/Scripts/UI/Shell/Ecs",
    };

    private static readonly HashSet<string> ClassifiedRuntimeGameObjectInstantiateCalls = new(StringComparer.Ordinal)
    {
        "Assets/Game/Scripts/Environment/DayNightSystem.cs|_runtimeSkyboxMaterial = Object.Instantiate(RenderSettings.skybox);",
        "Assets/Game/Scripts/Environment/RuntimeCityVisualPresentationSystemHelper.cs|visual = UnityEngine.Object.Instantiate(combinedMesh.gameObject, wrapper.transform);",
        "Assets/Game/Scripts/Environment/RuntimeCityVisualPresentationSystemHelper.cs|visual = UnityEngine.Object.Instantiate(prefab, wrapper.transform);",
        "Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerPresentationSystemHelper.cs|GameObject instance = Object.Instantiate(prefab, _rootTransform);",
        "Assets/Game/Scripts/Environment/RuntimeGridBlockerPresentationSystemHelper.cs|GameObject visual = Object.Instantiate(prefab, root.transform);",
        "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs|? Object.Instantiate(definition.VisualTemplate)",
        "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs|: Object.Instantiate(definition.Prefab);",
        "Assets/Game/Scripts/Systems/BuildingDestroyedVisualPresentationSystemHelper.cs|GameObject instance = Object.Instantiate(prefab, parent, false);",
        "Assets/Game/Scripts/Systems/BuildingPlacementVisualPresentationSystemHelper.cs|visual = Object.Instantiate(definition.Prefab, wrapper.transform);",
        "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs|? Instantiate(prefab, runtimeRoot, false)",
        "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs|: Instantiate(prefab);",
        "Assets/Game/Scripts/Systems/BuildingSelectionMarkerPresentationSystemHelper.cs|_markerInstance = UnityEngine.Object.Instantiate(context.MarkerPrefab, context.MarkerParent);",
        "Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs|GameObject visual = UnityEngine.Object.Instantiate(source.gameObject, wrapper.transform);",
        "Assets/Game/Scripts/Systems/RoadBuildBuildingPlacementCompositionSystemHelper.cs|Instantiate(definition.Prefab, context.BuildingRoot),",
        "Assets/Game/Scripts/Systems/RoadBuildDefinitionProjectionSystem.cs|GameObject temp = UnityEngine.Object.Instantiate(definition.Prefab);",
        "Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs|GameObject intersectionObject = UnityEngine.Object.Instantiate(",
        "Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs|GameObject roadObject = UnityEngine.Object.Instantiate(",
        "Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs|roadObject = UnityEngine.Object.Instantiate(prefab, parent);",
        "Assets/Game/Scripts/Systems/RoadVisualVariantSystem.cs|GameObject temp = Instantiate(prefab);",
        "Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs|UnityEngine.Object markerInstance = UnityEngine.Object.Instantiate((UnityEngine.Object)_attackOrderMarkerPrefab);",
        "Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs|UnityEngine.Object markerInstance = UnityEngine.Object.Instantiate((UnityEngine.Object)_moveOrderMarkerPrefab);",
        "Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs|_attackTargetSelectionMarker = UnityEngine.Object.Instantiate(_attackTargetMarkerPrefab, _runtimeRoot);",
    };

    private static readonly Regex StaticMutableCollectionFieldRegex = new(
        @"\bstatic\s+(?:readonly\s+)?(?:(?:System\.Collections\.Generic\.)?(?:List|Dictionary|HashSet)|(?:Unity\.Collections\.)?Native(?:List|HashMap|HashSet|ParallelHashMap|ParallelHashSet))\s*<[^>\r\n]+>\s+(?<name>[A-Za-z_]\w*)\s*(?:=|;)",
        RegexOptions.CultureInvariant);

    private static readonly string[] ConcreteUiRuntimeTypes =
    {
        "MainMenuPlayUI",
        "BattleHudRuntimeFeedbackView",
        "MatchHudSquadTrayView",
        "MatchHudSelectionPanelView",
        "BuildDrawerView",
        "MatchHudMinimapView",
        "MatchOverlayCommandControlsView",
        "MatchHudRightQuickRailView",
        "SelectionRectangleView",
        "UIShellContentView",
        "MenuBootstrapView",
        "MatchSceneView",
    };

    private static readonly string[] RuntimeTypesForbiddenInUiRuntime =
    {
        "RuntimeGameplayStateSystem",
        "SelectionUiCameraSystemHelper",
        "RtsSelectionInputStateCompositionSystemHelper",
        "BuildingUiCommandSystemHelper",
        "BuildingUiQueryUiSystemHelper",
        "SceneLifecycleSceneSystemHelper",
        "MatchStartRequestStartupSystemHelper",
        "SelectionRuntimeDiagnosticsSystemHelper",
        "AISettingsRuntimeState",
    };

    private static readonly string[] FuelLogisticsMutationTokensForbiddenInUiRuntime =
    {
        "SetComponentData<BuildingResourceStorageComponent",
        "AddComponentData<BuildingResourceStorageComponent",
        "GetComponentDataRW<BuildingResourceStorageComponent",
        "RefRW<BuildingResourceStorageComponent",
        "SetBuffer<BuildingRuntimeFactionUsableFuelSummary",
        "AddBuffer<BuildingRuntimeFactionUsableFuelSummary",
        "BuildingResourceStorageTransferSystemHelper",
        "ResourceHaulerUtilitySystemHelper.",
        "VehicleFuelConsumptionSystem",
    };

    private static readonly string[] BroadNameTokens =
    {
        "Manager",
        "Controller",
        "Presenter",
        "Facade",
        "Installer",
        "Orchestrator",
    };

    public static void RunAssemblyBoundaryValidation()
    {
        try
        {
            var tests = new ScriptArchitectureAlignmentContractTests();
            tests.UiAndCompositionAssembliesMustNotReferenceUnusedHeavyPackages();
            tests.GameRuntimeStatsMustNotReadAuthoringComponents();
            tests.BuildingProductionQueueCompositionSystemHelperMustNotReadAuthoringComponents();
            tests.BuildingProductionRequestSystemHelperMustNotReadAuthoringComponents();
            tests.BuildingProductionTransportPresentationSystemHelperMustNotReadAuthoringComponents();
            tests.BuildingSpawnPrefabSystemMustNotReadAuthoringComponents();
            tests.BuildingDefinitionPrefabSystemHelperMustNotReadAuthoringComponents();
            tests.SceneAndMapAuthoringBootstrapMustStayInComposition();
            tests.RuntimeAssemblyMustNotReferenceAuthoringAssembly();
            tests.RuntimeAssemblyMustNotReferenceConcreteUiRuntimeAssembly();
            tests.RuntimeAssemblyMustNotReferenceUiShellEcsContractsAssembly();
            tests.RuntimeAssemblyMustNotReferenceConcreteRenderingAssembly();
            tests.RenderingAssemblyMustNotReferenceAuthoringAssembly();
            tests.RenderingAssemblyMustNotReadAuthoringComponents();
            tests.ConfigsAssemblyMustNotReferenceUiContractsAssembly();
            tests.UiRuntimeAssemblyMustNotReferenceRuntimeAssembly();
            tests.UiRuntimeAssemblyMustNotReferenceAuthoringAssembly();
            tests.UiRuntimeAssemblyMustNotReferenceComponentsAssembly();
            tests.UiRuntimeAssemblyMustNotReferenceEntitiesPackage();
            tests.UiRuntimeAssemblyMustNotReferenceCollectionsPackage();
            tests.UiRuntimeAssemblyMustNotReferenceUiShellEcsAssembly();
            tests.UiContractsAssemblyMustNotReferenceGameComponentsOrConfigs();
            tests.UiContractsAssemblyMustNotReferenceEcsPackages();
            tests.UiRuntimeAssemblyMustNotReferenceConfigsAssembly();
            tests.GameScriptAsmdefsMustDeclareMatchingRootNamespace();
            tests.GameScriptsMustDeclareOwningAssemblyNamespace();
            tests.UiRuntimeAssemblyMustNotReadAuthoringComponents();
            tests.UiRuntimeScriptsMustNotUseDirectEcsApis();
            tests.UiRuntimeScriptsMustNotReferenceSelectionUiCommandUiSystemHelper();
            tests.UiRuntimeScriptsMustNotReferenceConcreteRuntimeTypes();
            tests.UiRuntimeScriptsMustNotMutateFuelLogisticsSimulationState();
            Debug.Log("[ScriptArchitectureBoundaryValidation] result=Passed tests=31");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[ScriptArchitectureBoundaryValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    public static void RunBroadShellValidation()
    {
        try
        {
            var tests = new ScriptArchitectureAlignmentContractTests();
            tests.RuntimeTypeNamesMustNotIntroduceBroadApplicationLayerSuffixes();
            Debug.Log("[ScriptBroadShellValidation] result=Passed tests=1");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[ScriptBroadShellValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    public static void RunBootstrapCompositionGuardrailValidation()
    {
        try
        {
            var tests = new ScriptArchitectureAlignmentContractTests();
            tests.RuntimeScriptsMustNotAddHierarchyLookupOrObjectFindUsage();
            tests.RuntimeScriptsMustNotUseCameraMain();
            tests.RuntimeGameplayLogicMustNotAddStaticMutableRegistries();
            tests.BootstrapCompositionSystemsMustNotOwnGameplayPolicy();
            tests.RuntimeCompositionSystemHelpersMustStayOnArchitectureLedger();
            tests.RuntimeTypeNamesMustNotIntroduceBroadApplicationLayerSuffixes();
            Debug.Log("[BootstrapCompositionGuardrailValidation] result=Passed tests=6");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[BootstrapCompositionGuardrailValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    public static void RunRuntimeCompositionHelperLedgerValidation()
    {
        try
        {
            var tests = new ScriptArchitectureAlignmentContractTests();
            tests.RuntimeCompositionSystemHelpersMustStayOnArchitectureLedger();
            Debug.Log("[RuntimeCompositionHelperLedgerValidation] result=Passed tests=1");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[RuntimeCompositionHelperLedgerValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void SourceFilenamesMustNotStartWithProjectName()
    {
        List<string> violations = EnumerateSourceFiles(GameScriptsRoot)
            .Select(NormalizePath)
            .Where(path => Path.GetFileNameWithoutExtension(path).StartsWith(ProjectNamePrefix, StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(
            violations,
            "Source filenames must not start with the project/product name. Use feature or domain prefixes so project renaming stays cheap.");
    }

    [Test]
    public void AssemblyCSharpProjectsMustNotCompileAssetsSourceFiles()
    {
        List<string> violations = new();

        foreach (string projectFile in Directory.GetFiles(".", "Assembly-CSharp*.csproj", SearchOption.TopDirectoryOnly))
        {
            string normalizedProject = NormalizePath(projectFile).TrimStart('.', '/');
            bool isPlayerProject = normalizedProject.EndsWith(".Player.csproj", StringComparison.Ordinal);
            string[] lines = File.ReadAllLines(projectFile);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                bool compilesAssetsSource = line.Contains("<Compile Include=\"Assets/", StringComparison.Ordinal);
                bool compilesGameOrTestSource =
                    line.Contains("<Compile Include=\"Assets/Game/Scripts/", StringComparison.Ordinal) ||
                    line.Contains("<Compile Include=\"Assets/Tests/", StringComparison.Ordinal);
                if ((!isPlayerProject && compilesAssetsSource) || (isPlayerProject && compilesGameOrTestSource))
                    violations.Add($"{normalizedProject}:{lineIndex + 1} still compiles an Assets source file: {line.Trim()}");
            }
        }

        AssertNoViolations(
            violations,
            "Assembly-CSharp editor projects must stay empty of Assets source files, and player projects must not compile game/test source. Add or update asmdefs instead of letting new scripts fall back to default assemblies.");
    }

    [Test]
    public void RuntimeAssemblyMustNotReferenceConcreteUiRuntimeAssembly()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.UI.Runtime\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference `Game.UI.Runtime`. Runtime code can depend on `Game.UI.Contracts`; Composition owns concrete UI wiring.");
    }

    [Test]
    public void RuntimeAssemblyMustNotReferenceAuthoringAssembly()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.Authoring\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference `Game.Authoring`. Composition and authoring assemblies own prefab authoring reads.");
    }

    [Test]
    public void UiRuntimeAssemblyMustNotReferenceRuntimeAssembly()
    {
        string uiRuntimeAsmdefPath = Path.Combine(GameScriptsRoot, "UI/Game.UI.Runtime.asmdef");
        string asmdef = File.ReadAllText(uiRuntimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.Runtime\"", StringComparison.Ordinal),
            "`Game.UI.Runtime` must not reference `Game.Runtime`. Composition owns concrete runtime-to-UI wiring.");
    }

    [Test]
    public void UiRuntimeAssemblyMustNotReferenceAuthoringAssembly()
    {
        string uiRuntimeAsmdefPath = Path.Combine(GameScriptsRoot, "UI/Game.UI.Runtime.asmdef");
        string asmdef = File.ReadAllText(uiRuntimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.Authoring\"", StringComparison.Ordinal),
            "`Game.UI.Runtime` must not reference `Game.Authoring`. Composition can inject UI catalog metadata derived from authoring components.");
    }

    [Test]
    public void UiRuntimeAssemblyMustNotReferenceComponentsAssembly()
    {
        string uiRuntimeAsmdefPath = Path.Combine(GameScriptsRoot, "UI/Game.UI.Runtime.asmdef");
        string asmdef = File.ReadAllText(uiRuntimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.Components\"", StringComparison.Ordinal),
            "`Game.UI.Runtime` must not reference `Game.Components`. Composition owns ECS component reads and maps them into UI contracts.");
    }

    [Test]
    public void UiRuntimeAssemblyMustNotReferenceEntitiesPackage()
    {
        string uiRuntimeAsmdefPath = Path.Combine(GameScriptsRoot, "UI/Game.UI.Runtime.asmdef");
        string asmdef = File.ReadAllText(uiRuntimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Unity.Entities\"", StringComparison.Ordinal),
            "`Game.UI.Runtime` must not reference `Unity.Entities`. Keep direct ECS access in `Game.UI.Shell.Ecs`, contracts, runtime, or composition.");
    }

    [Test]
    public void UiRuntimeAssemblyMustNotReferenceCollectionsPackage()
    {
        string uiRuntimeAsmdefPath = Path.Combine(GameScriptsRoot, "UI/Game.UI.Runtime.asmdef");
        string asmdef = File.ReadAllText(uiRuntimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Unity.Collections\"", StringComparison.Ordinal),
            "`Game.UI.Runtime` must not reference `Unity.Collections`. Keep native ECS/buffer data in ECS-facing assemblies and expose UI DTOs through contracts.");
    }

    [Test]
    public void UiRuntimeAssemblyMustNotReferenceUiShellEcsAssembly()
    {
        string uiRuntimeAsmdefPath = Path.Combine(GameScriptsRoot, "UI/Game.UI.Runtime.asmdef");
        string asmdef = File.ReadAllText(uiRuntimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.UI.Shell.Ecs\"", StringComparison.Ordinal),
            "`Game.UI.Runtime` must not reference `Game.UI.Shell.Ecs`. Concrete UI uses `Game.UI.Contracts`; ECS shell systems provide the registered gateway implementation.");
    }

    [Test]
    public void ConfigsAssemblyMustNotReferenceUiContractsAssembly()
    {
        string configsAsmdefPath = Path.Combine(GameScriptsRoot, "Configs/Game.Configs.asmdef");
        string asmdef = File.ReadAllText(configsAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.UI.Contracts\"", StringComparison.Ordinal),
            "`Game.Configs` must not reference UI contracts. Shared config/UI catalog surfaces belong in `Game.Catalog.Contracts`.");
    }

    [Test]
    public void GameScriptAsmdefsMustDeclareMatchingRootNamespace()
    {
        List<string> violations = new();

        foreach (string asmdefPath in Directory.GetFiles(GameScriptsRoot, "*.asmdef", SearchOption.AllDirectories))
        {
            string normalizedPath = NormalizePath(asmdefPath);
            string asmdef = File.ReadAllText(asmdefPath);
            string assemblyName = ReadAsmdefStringValue(asmdef, "name");
            string rootNamespace = ReadAsmdefStringValue(asmdef, "rootNamespace");

            if (string.IsNullOrWhiteSpace(rootNamespace))
            {
                violations.Add($"{normalizedPath} has an empty rootNamespace.");
                continue;
            }

            if (!string.Equals(rootNamespace, assemblyName, StringComparison.Ordinal))
                violations.Add($"{normalizedPath} rootNamespace `{rootNamespace}` does not match assembly name `{assemblyName}`.");
        }

        AssertNoViolations(
            violations,
            "Game asmdefs under Assets/Game/Scripts must declare a non-empty rootNamespace matching the asmdef name.");
    }

    [Test]
    public void GameScriptsMustDeclareOwningAssemblyNamespace()
    {
        List<AssemblyNamespaceRule> rules = EnumerateGameScriptNamespaceRules()
            .OrderByDescending(rule => rule.RootPath.Length)
            .ToList();
        List<string> violations = new();

        foreach (string path in EnumerateSourceFiles(GameScriptsRoot))
        {
            string normalizedPath = NormalizePath(path);
            AssemblyNamespaceRule owner = rules.FirstOrDefault(rule => IsPathOwnedByRule(normalizedPath, rule.RootPath));
            if (string.IsNullOrEmpty(owner.Namespace))
            {
                violations.Add($"{normalizedPath} has no owning game asmdef namespace rule.");
                continue;
            }

            string declaredNamespace = ReadDeclaredNamespace(File.ReadAllText(path));
            if (string.IsNullOrEmpty(declaredNamespace))
            {
                violations.Add($"{normalizedPath} does not declare namespace `{owner.Namespace}`.");
                continue;
            }

            if (!string.Equals(declaredNamespace, owner.Namespace, StringComparison.Ordinal))
                violations.Add($"{normalizedPath} declares namespace `{declaredNamespace}` but owning asmdef expects `{owner.Namespace}`.");
        }

        AssertNoViolations(
            violations,
            "Every first-party game script under Assets/Game/Scripts must use the namespace of its owning asmdef rootNamespace.");
    }

    [Test]
    public void UiContractsAssemblyMustNotReferenceGameComponentsOrConfigs()
    {
        string uiContractsAsmdefPath = Path.Combine(GameScriptsRoot, "UI/Contracts/Game.UI.Contracts.asmdef");
        string asmdef = File.ReadAllText(uiContractsAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.Components\"", StringComparison.Ordinal) ||
            asmdef.Contains("\"Game.Configs\"", StringComparison.Ordinal),
            "`Game.UI.Contracts` must define UI-facing contracts and DTOs without depending on gameplay components or gameplay config assemblies. Composition owns mapping.");
    }

    [Test]
    public void UiContractsAssemblyMustNotReferenceEcsPackages()
    {
        string uiContractsAsmdefPath = Path.Combine(GameScriptsRoot, "UI/Contracts/Game.UI.Contracts.asmdef");
        string asmdef = File.ReadAllText(uiContractsAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Unity.Entities\"", StringComparison.Ordinal) ||
            asmdef.Contains("\"Unity.Collections\"", StringComparison.Ordinal),
            "`Game.UI.Contracts` must stay pure UI-facing contracts/DTOs. ECS shell components belong in `Game.UI.Shell.Contracts.Ecs`.");
    }

    [Test]
    public void UiRuntimeAssemblyMustNotReferenceConfigsAssembly()
    {
        string uiRuntimeAsmdefPath = Path.Combine(GameScriptsRoot, "UI/Game.UI.Runtime.asmdef");
        string asmdef = File.ReadAllText(uiRuntimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.Configs\"", StringComparison.Ordinal),
            "`Game.UI.Runtime` must not reference `Game.Configs`. Use UI contracts and config-owned adapters/source interfaces instead.");
    }

    [Test]
    public void UiRuntimeAssemblyMustNotReadAuthoringComponents()
    {
        List<string> violations = EnumerateSourceFiles(Path.Combine(GameScriptsRoot, "UI"))
            .SelectMany(path => FindAuthoringComponentReferences(path))
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(
            violations,
            "`Game.UI.Runtime` must not read authoring components. Use UI catalog metadata delegates injected by composition.");
    }

    [Test]
    public void UiRuntimeScriptsMustNotReferenceSelectionUiCommandUiSystemHelper()
    {
        List<string> violations = EnumerateSourceFiles(Path.Combine(GameScriptsRoot, "UI"))
            .SelectMany(path => FindTokenReferences(path, "SelectionUiCommandUiSystemHelper"))
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(
            violations,
            "`Game.UI.Runtime` must use `ISelectionUiCommand` from `Game.UI.Contracts`; the concrete `SelectionUiCommandUiSystemHelper` stays in runtime/composition.");
    }

    [Test]
    public void UiRuntimeScriptsMustNotReferenceConcreteRuntimeTypes()
    {
        List<string> violations = EnumerateSourceFiles(Path.Combine(GameScriptsRoot, "UI"))
            .SelectMany(path => FindTokenReferences(path, RuntimeTypesForbiddenInUiRuntime))
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(
            violations,
            "`Game.UI.Runtime` must use contracts from `Game.UI.Contracts`; concrete runtime systems stay in runtime/composition.");
    }

    [Test]
    public void UiRuntimeScriptsMustNotMutateFuelLogisticsSimulationState()
    {
        List<string> violations = EnumerateSourceFiles(Path.Combine(GameScriptsRoot, "UI"))
            .SelectMany(path => FindTokenReferences(path, FuelLogisticsMutationTokensForbiddenInUiRuntime))
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(
            violations,
            "`Game.UI.Runtime` may consume fuel logistics read models, but Oil/Fuel storage, hauler transfer, and fuel-consumption mutation must stay in ECS/runtime systems.");
    }

    [Test]
    public void UiRuntimeScriptsMustNotUseDirectEcsApis()
    {
        string[] forbiddenTokens =
        {
            "using Unity.Entities",
            "Unity.Entities.",
            "EntityQuery",
            "EntityManager",
            "DynamicBuffer<",
            "World.DefaultGameObjectInjectionWorld",
            "Entity.Null",
            "Action<Entity>",
            "UiShellEcsGateway",
        };

        List<string> violations = EnumerateSourceFiles(Path.Combine(GameScriptsRoot, "UI"))
            .Where(IsConcreteUiRuntimePath)
            .SelectMany(path => FindTokenReferences(path, forbiddenTokens))
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(
            violations,
            "`Game.UI.Runtime` must not directly use ECS APIs. Route shell ECS state through `Game.UI.Shell.Ecs` and map gameplay entities through UI DTOs/contracts.");
    }

    [Test]
    public void RuntimeAssemblyMustNotReferenceUiShellEcsAssembly()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.UI.Shell.Ecs\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference `Game.UI.Shell.Ecs`. Shared shell route/state data belongs in `Game.UI.Contracts`; shell systems stay UI-owned.");
    }

    [Test]
    public void RuntimeAssemblyMustNotReferenceUiShellEcsContractsAssembly()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.UI.Shell.Contracts.Ecs\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference ECS shell contracts. Runtime code should depend on `Game.UI.Contracts`; composition adapts shell ECS state.");
    }

    [Test]
    public void RuntimeAssemblyMustNotReferenceUnityUiPackage()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"UnityEngine.UI\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference `UnityEngine.UI`. Runtime code should query UI-blocking state through `Game.UI.Contracts`; concrete UI hit testing belongs in `Game.UI.Runtime`.");
    }

    [Test]
    public void RuntimeAssemblyMustNotReferenceTextMeshProPackage()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Unity.TextMeshPro\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference `Unity.TextMeshPro`. Runtime text presentation belongs in UI or composition assemblies.");
    }

    [Test]
    public void RuntimeAssemblyMustNotReferenceEntitiesHybridPackage()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Unity.Entities.Hybrid\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference `Unity.Entities.Hybrid` unless runtime source directly uses hybrid Entities APIs.");
    }

    [Test]
    public void RuntimeAssemblyMustNotReferenceEntitiesGraphicsPackage()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Unity.Entities.Graphics\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference `Unity.Entities.Graphics`. Unit render/model visibility systems belong in `Game.Rendering`.");
    }

    [Test]
    public void RuntimeAssemblyMustNotReferenceConcreteRenderingAssembly()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.Rendering\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference `Game.Rendering`. Runtime code can depend on `Game.Rendering.Contracts`; composition owns concrete renderer creation.");
    }

    [Test]
    public void RenderingAssemblyMustNotReferenceRuntimeAssembly()
    {
        string renderingAsmdefPath = Path.Combine(GameScriptsRoot, "Rendering/Game.Rendering.asmdef");
        string asmdef = File.ReadAllText(renderingAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.Runtime\"", StringComparison.Ordinal),
            "`Game.Rendering` must not reference `Game.Runtime`. Move shared renderer-facing data into contracts/configs/components instead of creating an assembly cycle.");
    }

    [Test]
    public void RenderingAssemblyMustNotReferenceAuthoringAssembly()
    {
        string renderingAsmdefPath = Path.Combine(GameScriptsRoot, "Rendering/Game.Rendering.asmdef");
        string asmdef = File.ReadAllText(renderingAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.Authoring\"", StringComparison.Ordinal),
            "`Game.Rendering` must not reference `Game.Authoring`. Composition can inject rendering metadata derived from authoring components.");
    }

    [Test]
    public void RenderingAssemblyMustNotReadAuthoringComponents()
    {
        List<string> violations = EnumerateSourceFiles(Path.Combine(GameScriptsRoot, "Rendering"))
            .SelectMany(path => FindAuthoringComponentReferences(path))
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(
            violations,
            "`Game.Rendering` must not read authoring components. Use rendering metadata delegates injected by composition.");
    }

    [Test]
    public void UiAndCompositionAssembliesMustNotReferenceUnusedHeavyPackages()
    {
        string[] asmdefPaths =
        {
            Path.Combine(GameScriptsRoot, "UI/Game.UI.Runtime.asmdef"),
            Path.Combine(GameScriptsRoot, "Composition/Game.Composition.asmdef"),
        };
        string[] disallowedReferences =
        {
            "\"sniveler-code.gpu-animation\"",
            "\"Unity.Burst\"",
            "\"Unity.Mathematics.Extensions\"",
        };
        List<string> violations = new();

        foreach (string asmdefPath in asmdefPaths)
        {
            string asmdef = File.ReadAllText(asmdefPath);
            foreach (string disallowedReference in disallowedReferences)
            {
                if (asmdef.Contains(disallowedReference, StringComparison.Ordinal))
                    violations.Add($"{NormalizePath(asmdefPath)} references {disallowedReference}.");
            }
        }

        AssertNoViolations(
            violations,
            "`Game.UI.Runtime` and `Game.Composition` must not carry heavy runtime/rendering package references unless their source directly uses them.");
    }

    [Test]
    public void GameRuntimeStatsMustNotReadAuthoringComponents()
    {
        string statsPath = Path.Combine(GameScriptsRoot, "Balance/GameRuntimeStats.cs");
        string source = File.ReadAllText(statsPath);

        Assert.IsFalse(
            source.Contains("UnitGridAuthoring", StringComparison.Ordinal) ||
            source.Contains("BuildingDefinitionAuthoring", StringComparison.Ordinal),
            "`GameRuntimeStats` must not read authoring components. Composition can inject prefab classification through `ConfigureUnitPrefabClassifier`.");
    }

    [Test]
    public void BuildingProductionQueueCompositionSystemHelperMustNotReadAuthoringComponents()
    {
        string productionPath = Path.Combine(GameScriptsRoot, "Systems/BuildingProductionQueueCompositionSystemHelper.cs");
        string source = File.ReadAllText(productionPath);

        Assert.IsFalse(
            source.Contains("UnitGridAuthoring", StringComparison.Ordinal) ||
            source.Contains("BuildingDefinitionAuthoring", StringComparison.Ordinal),
            "`BuildingProductionQueueCompositionSystemHelper` must not read authoring components. Composition can inject unit production metadata through `ConfigureUnitProductionMetadataResolver`.");
    }

    [Test]
    public void BuildingProductionRequestSystemHelperMustNotReadAuthoringComponents()
    {
        string productionRequestPath = Path.Combine(GameScriptsRoot, "Systems/BuildingProductionRequestSystemHelper.cs");
        string source = File.ReadAllText(productionRequestPath);

        Assert.IsFalse(
            source.Contains("UnitGridAuthoring", StringComparison.Ordinal) ||
            source.Contains("BuildingDefinitionAuthoring", StringComparison.Ordinal),
            "`BuildingProductionRequestSystemHelper` must not read authoring components. Use configured-unit read models from `BuildingDefinitionPrefabSystemHelper`.");
    }

    [Test]
    public void BuildingProductionTransportPresentationSystemHelperMustNotReadAuthoringComponents()
    {
        string productionTransportPath = Path.Combine(GameScriptsRoot, "Systems/BuildingProductionTransportPresentationSystemHelper.cs");
        string source = File.ReadAllText(productionTransportPath);

        Assert.IsFalse(
            source.Contains("UnitGridAuthoring", StringComparison.Ordinal) ||
            source.Contains("BuildingDefinitionAuthoring", StringComparison.Ordinal),
            "`BuildingProductionTransportPresentationSystemHelper` must not read authoring components. Composition can inject transport-drop visual preparation.");
    }

    [Test]
    public void BuildingSpawnPrefabSystemMustNotReadAuthoringComponents()
    {
        string spawnPrefabPath = Path.Combine(GameScriptsRoot, "Systems/BuildingSpawnPrefabSystem.cs");
        string source = File.ReadAllText(spawnPrefabPath);

        Assert.IsFalse(
            source.Contains("UnitGridAuthoring", StringComparison.Ordinal) ||
            source.Contains("BuildingDefinitionAuthoring", StringComparison.Ordinal),
            "`BuildingSpawnPrefabSystem` must not read authoring components. Composition can inject spawn-prefab lookup keys.");
    }

    [Test]
    public void BuildingDefinitionPrefabSystemHelperMustNotReadAuthoringComponents()
    {
        string definitionPath = Path.Combine(GameScriptsRoot, "Systems/BuildingDefinitionPrefabSystemHelper.cs");
        string source = File.ReadAllText(definitionPath);

        Assert.IsFalse(
            source.Contains("UnitGridAuthoring", StringComparison.Ordinal) ||
            source.Contains("BuildingDefinitionAuthoring", StringComparison.Ordinal),
            "`BuildingDefinitionPrefabSystemHelper` must not read authoring components. Composition can inject building and unit definition metadata.");
    }

    [Test]
    public void SceneAndMapAuthoringBootstrapMustStayInComposition()
    {
        string[] compositionOwnedFiles =
        {
            "GameplayFeatureStartupCompositionSystemHelper.cs",
            "GameplaySceneBindingSceneSystemHelper.cs",
            "MapSurfaceRuntimeBootstrapSceneSystemHelper.cs",
        };
        List<string> violations = new();

        foreach (string fileName in compositionOwnedFiles)
        {
            string runtimePath = Path.Combine(GameScriptsRoot, "Systems", fileName);
            string compositionPath = Path.Combine(GameScriptsRoot, "Composition", fileName);

            if (File.Exists(runtimePath))
                violations.Add($"{NormalizePath(runtimePath)} exists under runtime systems.");
            if (!File.Exists(compositionPath))
                violations.Add($"{NormalizePath(compositionPath)} is missing from composition.");
        }

        AssertNoViolations(
            violations,
            "Scene binding and map-surface authoring bootstrap code must stay in `Game.Composition`, where scene authoring references are allowed.");
    }

    [Test]
    public void BootstrapCompositionSystemsMustNotOwnGameplayPolicy()
    {
        List<string> violations = new();

        foreach (string path in BootstrapCompositionGuardrailFiles)
        {
            if (!File.Exists(path))
            {
                violations.Add($"{NormalizePath(path)} is missing from the bootstrap composition guardrail scan.");
                continue;
            }

            string normalized = NormalizePath(path);
            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                string policyToken = BootstrapCompositionForbiddenPolicyTokens.FirstOrDefault(t => line.Contains(t, StringComparison.Ordinal));
                if (policyToken != null)
                    violations.Add($"{normalized}:{lineIndex + 1} references policy token `{policyToken}`: {line.Trim()}");

                string standaloneType = BootstrapCompositionForbiddenStandaloneTypes.FirstOrDefault(t => ContainsStandaloneIdentifier(line, t));
                if (standaloneType != null)
                    violations.Add($"{normalized}:{lineIndex + 1} references broad gameplay shell `{standaloneType}`: {line.Trim()}");
            }
        }

        AssertNoViolations(
            violations,
            "Bootstrap/composition systems must stay at serialized binding and lifecycle orchestration edges. Keep AI/economy/mission/perf diagnostics policy and retired gameplay shells in their owning ECS or narrow runtime systems.");
    }

    [Test]
    public void RuntimeCompositionSystemHelpersMustStayOnArchitectureLedger()
    {
        HashSet<string> current = EnumerateSourceFiles(Path.Combine(GameScriptsRoot, "Systems"))
            .Select(NormalizePath)
            .Where(path => path.EndsWith("CompositionSystemHelper.cs", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        List<string> violations = current
            .Except(RuntimeCompositionSystemHelperLedger, StringComparer.Ordinal)
            .Select(path => $"{path} is a new runtime CompositionSystemHelper. Prefer a Burst-capable ISystem for gameplay ownership, or update the architecture ledger with a reviewed reason.")
            .Concat(RuntimeCompositionSystemHelperLedger
                .Except(current, StringComparer.Ordinal)
                .Select(path => $"{path} is no longer present; remove the stale runtime CompositionSystemHelper ledger entry."))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(
            violations,
            "Runtime CompositionSystemHelper files are frozen as existing debt. New gameplay work should not add managed composition helpers without explicit architecture review.");
    }

    [Test]
    public void LegacyBootstrapFolderMustNotContainRuntimeSourceFiles()
    {
        List<string> violations = EnumerateSourceFiles(LegacyBootstrapRoot)
            .Select(NormalizePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(
            violations,
            "`Assets/Game/Scripts/Bootstrap` is a legacy ownership bucket. Put scene wiring in `Assets/Game/Scripts/Composition` and runtime state or logic in its owning runtime folder.");
    }

    [Test]
    public void RuntimeLogicMustNotReferenceConcreteUiViews()
    {
        List<string> violations = new();

        foreach (string path in EnumerateRuntimeLogicSourceFiles())
        {
            string normalized = NormalizePath(path);
            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                foreach (string concreteType in ConcreteUiRuntimeTypes)
                {
                    if (ContainsConcreteTypeReference(line, concreteType))
                    {
                        violations.Add($"{normalized}:{lineIndex + 1} references concrete UI type `{concreteType}`: {line.Trim()}");
                    }
                }
            }
        }

        AssertNoViolations(
            violations,
            "Runtime logic must depend on `Game.UI.Contracts`, not concrete UI views. Keep concrete view lookup in `Assets/Game/Scripts/Composition` or UI-owned files.");
    }

    [Test]
    public void SourceMustNotHardcodeLegacyDefaultAssemblyNames()
    {
        string legacyAssemblyName = "Assembly" + "-CSharp";
        List<string> violations = new();

        foreach (string path in EnumerateSourceFiles("Assets/Game/Scripts").Concat(EnumerateSourceFiles("Assets/Tests")))
        {
            string normalized = NormalizePath(path);
            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (line.Contains(legacyAssemblyName, StringComparison.Ordinal))
                    violations.Add($"{normalized}:{lineIndex + 1} hardcodes `{legacyAssemblyName}`: {line.Trim()}");
            }
        }

        AssertNoViolations(
            violations,
            "Source must not hardcode legacy default assembly names. Use direct type references, asmdef names, or assembly-agnostic lookup helpers.");
    }

    [Test]
    public void RuntimeScriptsMustNotAddHierarchyLookupOrObjectFindUsage()
    {
        Dictionary<string, int> occurrences = new(StringComparer.Ordinal);
        List<string> violations = new();

        foreach (string path in EnumerateRuntimeSourceFiles())
        {
            string normalized = NormalizePath(path);
            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                string violationKind = ResolveForbiddenRuntimeLookupKind(line);
                if (violationKind == null)
                    continue;

                string key = normalized + "|" + violationKind;
                occurrences.TryGetValue(key, out int count);
                occurrences[key] = count + 1;

                int allowedCount = RuntimeLookupDebtAllowlist.TryGetValue(key, out int allowed) ? allowed : 0;
                if (occurrences[key] > allowedCount)
                    violations.Add($"{normalized}:{lineIndex + 1} uses {violationKind}: {line.Trim()}");
            }
        }

        AssertNoViolations(
            violations,
            "Runtime scripts must not add hierarchy string lookup or Object.Find-style discovery. Add serialized references, authoring data, cached spawn references, or ECS managed references instead.");
    }

    [Test]
    public void RuntimeScriptsMustNotUseCameraMain()
    {
        List<string> violations = new();

        foreach (string path in EnumerateRuntimeSourceFiles())
        {
            string normalized = NormalizePath(path);
            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (line.Contains("Camera.main", StringComparison.Ordinal))
                    violations.Add($"{normalized}:{lineIndex + 1} uses Camera.main: {line.Trim()}");
            }
        }

        AssertNoViolations(
            violations,
            "Runtime scripts must not use Camera.main. Pass cameras through serialized references, scene bindings, or explicit runtime contexts.");
    }

    [Test]
    public void RuntimeLogicMustNotReferenceUnityUiImplementationTypes()
    {
        string[] forbiddenTokens =
        {
            "using UnityEngine.UI",
            "UnityEngine.UI.",
            "GraphicRaycaster",
            "using UnityEngine.EventSystems",
            "UnityEngine.EventSystems",
            "PointerEventData",
            "RaycastResult",
            "using TMPro",
            "TMPro.",
            "TextMeshPro",
            "TMP_",
        };
        List<string> violations = new();

        foreach (string path in EnumerateRuntimeLogicSourceFiles())
        {
            string normalized = NormalizePath(path);
            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                foreach (string forbiddenToken in forbiddenTokens)
                {
                    if (line.Contains(forbiddenToken, StringComparison.Ordinal))
                        violations.Add($"{normalized}:{lineIndex + 1} references UI implementation token `{forbiddenToken}`: {line.Trim()}");
                }
            }
        }

        AssertNoViolations(
            violations,
            "Runtime logic must not reference Unity UI implementation types. Expose UI hit testing through `Game.UI.Contracts` and keep EventSystem/GraphicRaycaster code in `Game.UI.Runtime`.");
    }

    [Test]
    public void RuntimeUiScriptsMustNotAddDirectDebugLogs()
    {
        Dictionary<string, int> occurrences = new(StringComparer.Ordinal);
        List<string> violations = new();

        foreach (string path in EnumerateSourceFiles("Assets/Game/Scripts/UI"))
        {
            if (IsEditorPath(path))
                continue;

            string normalized = NormalizePath(path);
            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                string logKind = ResolveDebugLogKind(line);
                if (logKind == null)
                    continue;

                string key = normalized + "|" + logKind;
                occurrences.TryGetValue(key, out int count);
                occurrences[key] = count + 1;

                int allowedCount = RuntimeUiDebugLogDebtAllowlist.TryGetValue(key, out int allowed) ? allowed : 0;
                if (occurrences[key] > allowedCount)
                    violations.Add($"{normalized}:{lineIndex + 1} uses {logKind}: {line.Trim()}");
            }
        }

        AssertNoViolations(
            violations,
            "Runtime UI scripts must not add direct Debug.Log* diagnostics. Use user-facing feedback, gated diagnostics, or ECS diagnostic buffers instead.");
    }

    [Test]
    public void SelectionSystemsMustBindSelectionPanelThroughContract()
    {
        List<string> violations = new();

        foreach (string path in EnumerateSourceFiles("Assets/Game/Scripts/Systems"))
        {
            string normalized = NormalizePath(path);
            if (SelectionPanelConcreteSystemBindingAllowlist.Contains(normalized))
                continue;

            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (line.Contains("MatchHudSelectionPanelView", StringComparison.Ordinal) &&
                    !line.Contains("IMatchHudSelectionPanelView", StringComparison.Ordinal))
                {
                    violations.Add($"{normalized}:{lineIndex + 1} binds concrete selection panel view: {line.Trim()}");
                }
            }
        }

        AssertNoViolations(
            violations,
            "Selection/runtime systems must bind the match HUD selection panel through `IMatchHudSelectionPanelView`. Concrete `MatchHudSelectionPanelView` lookup is limited to bootstrap scene/UI discovery.");
    }

    [Test]
    public void RuntimeScriptsMustNotAddStaticViewRegistries()
    {
        List<string> violations = new();

        foreach (string path in EnumerateRuntimeSourceFiles())
        {
            string normalized = NormalizePath(path);
            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                string violationKind = ResolveForbiddenStaticViewRegistryKind(line);
                if (violationKind == null)
                    continue;

                violations.Add($"{normalized}:{lineIndex + 1} uses {violationKind}: {line.Trim()}");
            }
        }

        AssertNoViolations(
            violations,
            "Runtime scripts must not add static mutable view registries. Bind views through serialized references or explicit shell/gameplay dependency edges.");
    }

    [Test]
    public void RuntimeGameplayLogicMustNotAddStaticMutableRegistries()
    {
        HashSet<string> currentAllowedDebt = new(StringComparer.Ordinal);
        List<string> violations = new();

        foreach (string path in EnumerateRuntimeLogicSourceFiles())
        {
            string normalized = NormalizePath(path);
            string[] lines = File.ReadAllLines(path);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string fieldName = ResolveStaticMutableCollectionFieldName(lines[lineIndex]);
                if (fieldName == null)
                    continue;

                string key = normalized + "|" + fieldName;
                if (StaticGameplayRegistryDebtAllowlist.Contains(key))
                {
                    currentAllowedDebt.Add(key);
                    continue;
                }

                violations.Add($"{normalized}:{lineIndex + 1} declares static mutable collection `{fieldName}`: {lines[lineIndex].Trim()}");
            }
        }

        IEnumerable<string> staleAllowlistEntries = StaticGameplayRegistryDebtAllowlist
            .Except(currentAllowedDebt, StringComparer.Ordinal)
            .Select(key => $"{key} is no longer present; remove the stale static registry allowlist entry.");

        AssertNoViolations(
            violations.Concat(staleAllowlistEntries).ToArray(),
            "Runtime gameplay logic must not add static mutable registries. Use ECS singletons, buffers, system-owned instance state, or explicit managed presentation boundaries instead.");
    }

    [Test]
    public void UiMonoBehavioursMustEndWithViewOrBeAllowlisted()
    {
        List<string> violations = new();

        foreach (string path in EnumerateSourceFiles("Assets/Game/Scripts/UI"))
        {
            string source = File.ReadAllText(path);
            foreach (ClassDeclaration declaration in ExtractClassDeclarations(source))
            {
                if (!IsUiMonoBehaviourDeclaration(declaration))
                    continue;

                if (declaration.Name.EndsWith("View", StringComparison.Ordinal))
                    continue;

                if (NonViewUiMonoBehaviourDebtAllowlist.Contains(declaration.Name))
                    continue;

                violations.Add($"{NormalizePath(path)} declares UI MonoBehaviour `{declaration.Name}`. UI MonoBehaviours should be `*View` reference binders unless explicitly allowlisted.");
            }
        }

        AssertNoViolations(
            violations,
            "New UI MonoBehaviours must use `*View` naming or move flow/state behavior into ECS/shell systems.");
    }

    [Test]
    public void RuntimeTypeNamesMustNotIntroduceBroadApplicationLayerSuffixes()
    {
        List<string> violations = new();

        foreach (string path in EnumerateSourceFiles(GameScriptsRoot))
        {
            if (IsEditorPath(path))
                continue;

            string source = File.ReadAllText(path);
            foreach (ClassDeclaration declaration in ExtractClassDeclarations(source))
            {
                if (BroadNameDebtAllowlist.Contains(declaration.Name))
                    continue;

                string token = BroadNameTokens.FirstOrDefault(t => declaration.Name.Contains(t, StringComparison.Ordinal));
                if (token == null)
                    continue;

                violations.Add($"{NormalizePath(path)} declares `{declaration.Name}` using broad token `{token}`.");
            }
        }

        AssertNoViolations(
            violations,
            "Runtime type names must not introduce Manager/Controller/Presenter/Facade/Installer/Orchestrator-style shells. Use ECS `*System`, `*Component`, `*Entity`, `*View`, `*Config`, or approved service-edge names.");
    }

    [Test]
    public void UiMonoBehavioursMustNotAddStaticMutableRegistries()
    {
        List<string> violations = new();

        foreach (string path in EnumerateSourceFiles("Assets/Game/Scripts/UI"))
        {
            string source = File.ReadAllText(path);
            if (!ContainsStaticMutableRegistryPattern(source))
                continue;

            foreach (ClassDeclaration declaration in ExtractClassDeclarations(source))
            {
                if (!IsUiMonoBehaviourDeclaration(declaration))
                    continue;

                if (StaticUiRegistryDebtAllowlist.Contains(declaration.Name))
                    continue;

                violations.Add($"{NormalizePath(path)} declares UI MonoBehaviour `{declaration.Name}` with a static mutable registry pattern.");
            }
        }

        AssertNoViolations(
            violations,
            "UI views must not add static mutable registries. Bind views through serialized shell references, installed content roots, or ECS managed references.");
    }

    [Test]
    public void ShellContentPrefabsMustExposeSerializedSectionReferences()
    {
        AssertShellContentSections(
            MainMenuContentPrefabPath,
            UIShellContentSectionId.MenuBackground,
            UIShellContentSectionId.Header,
            UIShellContentSectionId.Left,
            UIShellContentSectionId.Middle,
            UIShellContentSectionId.Right,
            UIShellContentSectionId.Footer);
        AssertShellContentSections(
            MatchHudContentPrefabPath,
            UIShellContentSectionId.Header,
            UIShellContentSectionId.Left,
            UIShellContentSectionId.Right,
            UIShellContentSectionId.Footer);
        AssertShellContentSections(
            ArmoryContentPrefabPath,
            UIShellContentSectionId.Left,
            UIShellContentSectionId.Middle,
            UIShellContentSectionId.Right,
            UIShellContentSectionId.Footer);
    }

    [Test]
    public void RuntimeInstantiateCallsMustStayEntityOwnedOrClassifiedPresentation()
    {
        List<string> violations = new();

        foreach (string root in RuntimeInstantiateOwnershipScanRoots)
        {
            foreach (string path in EnumerateSourceFiles(root))
            {
                if (IsEditorPath(path))
                    continue;

                string normalizedPath = NormalizePath(path);
                string[] lines = File.ReadAllLines(path);
                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string normalizedLine = NormalizeSourceLine(lines[lineIndex]);
                    if (!IsUnityObjectInstantiateCall(normalizedLine))
                        continue;

                    string signature = normalizedPath + "|" + normalizedLine;
                    if (!ClassifiedRuntimeGameObjectInstantiateCalls.Contains(signature))
                        violations.Add($"{normalizedPath}:{lineIndex + 1} has unclassified Unity object instantiate: {normalizedLine}");
                }
            }
        }

        AssertNoViolations(
            violations,
            "Runtime ECS/system code must not add gameplay GameObject spawn paths. Use entity prefab/ECB ownership for gameplay spawns, or classify presentation/probe/material instantiates explicitly in this test.");
    }

    private static IEnumerable<string> EnumerateRuntimeSourceFiles()
    {
        foreach (string path in EnumerateSourceFiles(GameScriptsRoot))
        {
            if (IsEditorPath(path) || IsAuthoringPath(path))
                continue;

            yield return path;
        }
    }

    private static IEnumerable<string> EnumerateRuntimeLogicSourceFiles()
    {
        foreach (string path in EnumerateSourceFiles(GameScriptsRoot))
        {
            if (IsEditorPath(path) ||
                IsAuthoringPath(path) ||
                IsUiPath(path) ||
                IsCompositionPath(path))
            {
                continue;
            }

            yield return path;
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        if (!Directory.Exists(root))
            yield break;

        foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (string.Equals(NormalizePath(path), SelfPath, StringComparison.Ordinal))
                continue;

            yield return path;
        }
    }

    private static IEnumerable<AssemblyNamespaceRule> EnumerateGameScriptNamespaceRules()
    {
        foreach (string asmdefPath in Directory.GetFiles(GameScriptsRoot, "*.asmdef", SearchOption.AllDirectories))
        {
            string asmdef = File.ReadAllText(asmdefPath);
            string rootNamespace = ReadAsmdefStringValue(asmdef, "rootNamespace");
            yield return new AssemblyNamespaceRule(NormalizePath(Path.GetDirectoryName(asmdefPath)), rootNamespace);
        }
    }

    private static string ReadAsmdefStringValue(string asmdef, string propertyName)
    {
        Match match = Regex.Match(
            asmdef,
            "\"" + Regex.Escape(propertyName) + "\"\\s*:\\s*\"(?<value>[^\"]*)\"",
            RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value : string.Empty;
    }

    private static string ReadDeclaredNamespace(string source)
    {
        Match match = Regex.Match(
            source,
            @"^\s*namespace\s+(?<name>[A-Za-z_][A-Za-z0-9_.]*)\s*[;{]",
            RegexOptions.CultureInvariant | RegexOptions.Multiline);
        return match.Success ? match.Groups["name"].Value : string.Empty;
    }

    private static bool IsPathOwnedByRule(string sourcePath, string ruleRootPath)
    {
        return sourcePath.StartsWith(ruleRootPath + "/", StringComparison.Ordinal);
    }

    private static IEnumerable<string> FindAuthoringComponentReferences(string path)
    {
        string normalized = NormalizePath(path);
        string[] lines = File.ReadAllLines(path);
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            if (line.Contains("UnitGridAuthoring", StringComparison.Ordinal) ||
                line.Contains("BuildingDefinitionAuthoring", StringComparison.Ordinal))
            {
                yield return $"{normalized}:{lineIndex + 1} references authoring component: {line.Trim()}";
            }
        }
    }

    private static IEnumerable<string> FindTokenReferences(string path, params string[] tokens)
    {
        string normalized = NormalizePath(path);
        string[] lines = File.ReadAllLines(path);
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            foreach (string token in tokens)
            {
                if (line.Contains(token, StringComparison.Ordinal))
                    yield return $"{normalized}:{lineIndex + 1} references token `{token}`: {line.Trim()}";
            }
        }
    }

    private static void AssertShellContentSections(string prefabPath, params UIShellContentSectionId[] requiredSections)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.NotNull(prefab, prefabPath);

        UIShellContentSectionsView sectionsView = prefab.GetComponent<UIShellContentSectionsView>();
        Assert.NotNull(sectionsView, $"{prefabPath} missing {nameof(UIShellContentSectionsView)}.");

        for (int i = 0; i < requiredSections.Length; i++)
        {
            UIShellContentSectionId sectionId = requiredSections[i];
            Assert.IsTrue(
                sectionsView.TryGetSection(sectionId, out GameObject sectionRoot) && sectionRoot != null,
                $"{prefabPath} missing section reference {sectionId}.");
        }
    }

    private static string ResolveForbiddenRuntimeLookupKind(string line)
    {
        if (line.Contains("Object.Find", StringComparison.Ordinal) ||
            line.Contains("GameObject.Find", StringComparison.Ordinal) ||
            line.Contains("FindObjectOfType", StringComparison.Ordinal) ||
            line.Contains("FindObjectsOfType", StringComparison.Ordinal) ||
            line.Contains("FindFirstObjectByType", StringComparison.Ordinal) ||
            line.Contains("FindAnyObjectByType", StringComparison.Ordinal) ||
            line.Contains("FindObjectsByType", StringComparison.Ordinal))
        {
            return "ObjectFind";
        }

        if (line.Contains(".Find(", StringComparison.Ordinal) &&
            !line.Contains("Shader.Find(", StringComparison.Ordinal) &&
            !line.Contains(".FindProperty(", StringComparison.Ordinal))
        {
            return "HierarchyFind";
        }

        return null;
    }

    private static bool ContainsConcreteTypeReference(string line, string concreteType)
    {
        if (line.Contains("I" + concreteType, StringComparison.Ordinal))
            return false;

        string trimmed = line.TrimStart();
        return StartsWithConcreteTypeDeclaration(trimmed, concreteType) ||
               line.Contains("<" + concreteType, StringComparison.Ordinal) ||
               line.Contains(concreteType + ">", StringComparison.Ordinal) ||
               line.Contains("typeof(" + concreteType + ")", StringComparison.Ordinal) ||
               line.Contains("new " + concreteType + "(", StringComparison.Ordinal) ||
               line.Contains("as " + concreteType, StringComparison.Ordinal) ||
               line.Contains("is " + concreteType, StringComparison.Ordinal);
    }

    private static bool StartsWithConcreteTypeDeclaration(string trimmedLine, string concreteType)
    {
        string[] prefixes =
        {
            "public ",
            "private ",
            "protected ",
            "internal ",
            "public readonly ",
            "private readonly ",
            "protected readonly ",
            "internal readonly ",
            "public static ",
            "private static ",
            "protected static ",
            "internal static ",
            "public static readonly ",
            "private static readonly ",
            "protected static readonly ",
            "internal static readonly ",
            "readonly ",
            "static ",
        };

        if (StartsWithBareTypeDeclaration(trimmedLine, concreteType))
            return true;

        foreach (string prefix in prefixes)
        {
            if (trimmedLine.StartsWith(prefix + concreteType + " ", StringComparison.Ordinal) ||
                trimmedLine.StartsWith(prefix + concreteType + "\t", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool StartsWithBareTypeDeclaration(string trimmedLine, string concreteType)
    {
        string prefix = concreteType + " ";
        if (!trimmedLine.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        int variableStart = prefix.Length;
        return variableStart < trimmedLine.Length &&
               (char.IsLower(trimmedLine[variableStart]) || trimmedLine[variableStart] == '_');
    }

    private static string ResolveDebugLogKind(string line)
    {
        if (line.Contains("Debug.LogException", StringComparison.Ordinal))
            return "Debug.LogException";
        if (line.Contains("Debug.LogError", StringComparison.Ordinal))
            return "Debug.LogError";
        if (line.Contains("Debug.LogWarning", StringComparison.Ordinal))
            return "Debug.LogWarning";
        if (line.Contains("Debug.Log", StringComparison.Ordinal))
            return "Debug.Log";

        return null;
    }

    private static string ResolveForbiddenStaticViewRegistryKind(string line)
    {
        if (line.Contains("ActiveView", StringComparison.Ordinal) ||
            line.Contains("StatesByView", StringComparison.Ordinal))
        {
            return "StaticViewRegistry";
        }

        if (!line.Contains("static", StringComparison.Ordinal) ||
            !line.Contains("View", StringComparison.Ordinal))
        {
            return null;
        }

        if (line.Contains("Dictionary<", StringComparison.Ordinal) ||
            line.Contains("List<", StringComparison.Ordinal) ||
            line.Contains("HashSet<", StringComparison.Ordinal))
        {
            return "StaticViewCollection";
        }

        return null;
    }

    private static bool ContainsStaticMutableRegistryPattern(string source)
    {
        return source.Contains("RegisteredInstances", StringComparison.Ordinal) ||
            source.Contains("static readonly List<", StringComparison.Ordinal) ||
            source.Contains("static List<", StringComparison.Ordinal) ||
            source.Contains("static readonly HashSet<", StringComparison.Ordinal) ||
            source.Contains("static HashSet<", StringComparison.Ordinal) ||
            source.Contains("static readonly Dictionary<", StringComparison.Ordinal) ||
            source.Contains("static Dictionary<", StringComparison.Ordinal);
    }

    private static string ResolveStaticMutableCollectionFieldName(string line)
    {
        Match match = StaticMutableCollectionFieldRegex.Match(line);
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static IEnumerable<ClassDeclaration> ExtractClassDeclarations(string source)
    {
        string[] lines = source.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (!line.Contains(" class ", StringComparison.Ordinal))
                continue;

            int classIndex = line.IndexOf(" class ", StringComparison.Ordinal);
            string afterClass = line.Substring(classIndex + " class ".Length).TrimStart();
            string name = ReadIdentifier(afterClass);
            if (string.IsNullOrEmpty(name))
                continue;

            string baseClause = string.Empty;
            int colonIndex = line.IndexOf(':');
            if (colonIndex >= 0)
                baseClause = line.Substring(colonIndex + 1);

            yield return new ClassDeclaration(name, baseClause);
        }
    }

    private static string ReadIdentifier(string text)
    {
        int length = 0;
        while (length < text.Length)
        {
            char c = text[length];
            if (!char.IsLetterOrDigit(c) && c != '_')
                break;

            length++;
        }

        return length > 0 ? text.Substring(0, length) : string.Empty;
    }

    private static bool IsUiMonoBehaviourDeclaration(ClassDeclaration declaration)
    {
        return declaration.BaseClause.Contains("MonoBehaviour", StringComparison.Ordinal) ||
               declaration.BaseClause.Contains("UIScreenView", StringComparison.Ordinal);
    }

    private static bool IsEditorPath(string path)
    {
        return NormalizePath(path).Contains("/Editor/", StringComparison.Ordinal);
    }

    private static bool IsAuthoringPath(string path)
    {
        return NormalizePath(path).Contains("/Authorings/", StringComparison.Ordinal);
    }

    private static bool IsUiPath(string path)
    {
        return NormalizePath(path).Contains("/UI/", StringComparison.Ordinal);
    }

    private static bool IsConcreteUiRuntimePath(string path)
    {
        string normalized = NormalizePath(path);
        return normalized.Contains("/UI/", StringComparison.Ordinal) &&
               !normalized.Contains("/UI/Contracts/", StringComparison.Ordinal) &&
               !normalized.Contains("/UI/Shell/Ecs/", StringComparison.Ordinal) &&
               !IsEditorPath(normalized);
    }

    private static bool IsCompositionPath(string path)
    {
        return NormalizePath(path).Contains("/Composition/", StringComparison.Ordinal);
    }

    private static bool IsUnityObjectInstantiateCall(string normalizedLine)
    {
        if (!normalizedLine.Contains("Instantiate(", StringComparison.Ordinal))
            return false;

        if (normalizedLine.Contains("ecb.Instantiate(", StringComparison.Ordinal) ||
            normalizedLine.Contains("em.Instantiate(", StringComparison.Ordinal) ||
            normalizedLine.Contains("EntityManager.Instantiate(", StringComparison.Ordinal) ||
            normalizedLine.Contains(".EntityManager.Instantiate(", StringComparison.Ordinal))
        {
            return false;
        }

        return normalizedLine.Contains("Object.Instantiate(", StringComparison.Ordinal) ||
               Regex.IsMatch(normalizedLine, @"(?:^|[=?:,(]\s*)Instantiate\(", RegexOptions.CultureInvariant);
    }

    private static string NormalizeSourceLine(string line)
    {
        return Regex.Replace(line.Trim(), @"\s+", " ", RegexOptions.CultureInvariant);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool ContainsStandaloneIdentifier(string line, string identifier)
    {
        int searchIndex = 0;
        while (searchIndex < line.Length)
        {
            int matchIndex = line.IndexOf(identifier, searchIndex, StringComparison.Ordinal);
            if (matchIndex < 0)
                return false;

            int before = matchIndex - 1;
            int after = matchIndex + identifier.Length;
            bool hasIdentifierBefore = before >= 0 && IsIdentifierPart(line[before]);
            bool hasIdentifierAfter = after < line.Length && IsIdentifierPart(line[after]);
            if (!hasIdentifierBefore && !hasIdentifierAfter)
                return true;

            searchIndex = matchIndex + identifier.Length;
        }

        return false;
    }

    private static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private static void AssertNoViolations(IReadOnlyCollection<string> violations, string header)
    {
        if (violations.Count == 0)
            return;

        Assert.Fail(header + "\n" + string.Join("\n", violations.OrderBy(v => v, StringComparer.Ordinal)));
    }

    private readonly struct ClassDeclaration
    {
        public ClassDeclaration(string name, string baseClause)
        {
            Name = name;
            BaseClause = baseClause ?? string.Empty;
        }

        public string Name { get; }
        public string BaseClause { get; }
    }

    private readonly struct AssemblyNamespaceRule
    {
        public AssemblyNamespaceRule(string rootPath, string namespaceName)
        {
            RootPath = rootPath ?? string.Empty;
            Namespace = namespaceName ?? string.Empty;
        }

        public string RootPath { get; }
        public string Namespace { get; }
    }
}
#endif
