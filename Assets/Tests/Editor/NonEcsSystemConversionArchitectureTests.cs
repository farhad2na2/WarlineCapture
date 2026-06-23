#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class NonEcsSystemConversionArchitectureTests
{
    private const string GameScriptsRoot = "Assets/Game/Scripts";
    private const string UiScriptsRoot = "Assets/Game/Scripts/UI";
    private const string InventoryPath = "Design/Architecture/non_ecs_to_ecs_system_inventory.md";

    private static readonly Regex TypeDeclarationRegex = new(
        @"^[ \t]*(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly)\s+)*(?<kind>class|struct)\s+(?<name>[A-Za-z_]\w*)\s*(?<bases>:[^{;\r\n]+)?",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex EcsSystemBaseRegex = new(
        @"\b(ISystem|SystemBase|ComponentSystemBase|ComponentSystem|JobComponentSystem)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex MonoBehaviourBaseRegex = new(
        @"\b(MonoBehaviour|UnityEngine\.MonoBehaviour)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex TopLevelTypeDeclarationRegex = new(
        @"^(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly)\s+)*(?<kind>class|struct|interface|enum)\s+(?<name>[A-Za-z_]\w*)\b",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex NamingEscapeSuffixRegex = new(
        @"(?:Service|Query|Rule|Cell|Resolver|Adapter|Composer|Context)$",
        RegexOptions.CultureInvariant);

    private static readonly Regex UnitPathRequestCreationRegex = new(
        @"\bnew\s+UnitPathRequest\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex DirectCommandExecutionEntrypointRegex = new(
        @"\b(?:TryIssue|Issue)[A-Za-z0-9_]*\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex ConvertedTargetManagedPrefabDependencyRegex = new(
        @"\b(?:GameObject|UnityEngine\.Object|List\s*<\s*GameObject\s*>|Dictionary\s*<[^>\r\n]*GameObject|ProducedUnitPrefabs|TryResolveConvertedPrefabEntity|GetPrefabName|FindAtlasEntry)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex PublicCommandMutatorEntrypointRegex = new(
        @"^[ \t]*public\s+(?:static\s+)?(?:readonly\s+)?[A-Za-z0-9_<>,\.\?\[\]\s]+\s+(?<name>(?:TryIssue|Issue|EnqueueAndProcess|ProcessPending|Clear[A-Za-z0-9_]*Order|Request[A-Za-z0-9_]*(?:Order|Command)|TryRequest[A-Za-z0-9_]*(?:Order|Command))[A-Za-z0-9_]*)\s*\(",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly HashSet<string> ApprovedUnitPathRequestWriterPaths = new(StringComparer.Ordinal)
    {
        "Assets/Game/Scripts/Systems/AICombatOrderSystem.cs",
        "Assets/Game/Scripts/Systems/BaseBreachOrderSystem.cs",
        "Assets/Game/Scripts/Systems/EngageTargetValidateSystem.cs",
        "Assets/Game/Scripts/Systems/UnitAttackSystem.cs",
        "Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs",
        "Assets/Game/Scripts/Systems/UnitIdleWanderSystem.cs",
        "Assets/Game/Scripts/Systems/UnitManualMoveRetrySystem.cs",
        "Assets/Game/Scripts/Systems/UnitMoveOrderRequestSystem.cs",
        "Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs"
    };

    private static readonly Dictionary<string, int> ApprovedPublicNonEcsCommandMutatorMethods = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs|EnqueueAndProcessBeginConfiguredPlacement"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs|EnqueueAndProcessBeginPlacementForConfiguredSpawnable"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs|EnqueueAndProcessBeginSoldierBasePlacement"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs|EnqueueAndProcessCancelBuildingPlacement"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs|EnqueueAndProcessConfirmBuildingPlacement"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs|EnqueueAndProcessExitBuildMode"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs|EnqueueAndProcessRotateBuildingPlacement"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs|ProcessPendingUiPlacementCommands"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingPlacementCommandSystem.cs|ProcessPendingUiPlacementCommandsIfPresent"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs|EnqueueAndProcessClearSelectedBuilding"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs|EnqueueAndProcessDeleteSelectedBuilding"] = 1,
        ["Assets/Game/Scripts/Systems/BuildingSelectionSystem.cs|ProcessPendingUiSelectionCommands"] = 1,
        ["Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs|EnqueueAndProcessCancelRoadBuildSession"] = 1,
        ["Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs|EnqueueAndProcessConfirmRoadBuildSession"] = 1,
        ["Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs|EnqueueAndProcessEnterRoadBuildMode"] = 1,
        ["Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs|EnqueueAndProcessExitBuildMode"] = 1,
        ["Assets/Game/Scripts/Systems/RoadBuildCommandSystem.cs|ProcessPendingRoadBuildCommands"] = 1,
        ["Assets/Game/Scripts/Systems/RtsSelectionPointerTargetCommandSystem.cs|TryRequestBoardSelectedTransportOrdersToPassengerRect"] = 1,
        ["Assets/Game/Scripts/Systems/RtsSelectionPointerTargetCommandSystem.cs|TryRequestMoveOrderToBuilding"] = 1,
        ["Assets/Game/Scripts/Systems/SelectionBuildingInteractionSystem.cs|TryRequestMoveOrderToBuilding"] = 1,
        ["Assets/Game/Scripts/Systems/SelectionRectangleRequestSystem.cs|ProcessPendingRequests"] = 1
    };

    private static readonly HashSet<string> ApprovedUiRuntimeEcsBoundaryPaths = new(StringComparer.Ordinal)
    {
        "Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs"
    };

    private static readonly HashSet<string> ApprovedTopLevelNamingEscapeTypes = new(StringComparer.Ordinal)
    {
        "Assets/Game/Scripts/Components/GridComponents.cs|UnitPathCell",
        "Assets/Game/Scripts/Components/MapSurfaceComponents.cs|MapSurfaceCell",
        "Assets/Game/Scripts/Composition/MatchIntroEcsStateQuery.cs|MatchIntroEcsStateQuery",
        "Assets/Game/Scripts/Composition/UiRuntimeBoundaryAdapters.cs|BuildingUiCommandAdapter",
        "Assets/Game/Scripts/Composition/UiRuntimeBoundaryAdapters.cs|BuildingUiQueryAdapter",
        "Assets/Game/Scripts/Composition/UiRuntimeBoundaryAdapters.cs|MatchHudCameraControlAdapter",
        "Assets/Game/Scripts/Composition/UiRuntimeBoundaryAdapters.cs|MatchHudMinimapDataSourceAdapter",
        "Assets/Game/Scripts/Composition/UiRuntimeBoundaryAdapters.cs|MatchRuntimeStateAdapter",
        "Assets/Game/Scripts/Composition/UiRuntimeBoundaryAdapters.cs|SelectionDiagnosticsSinkAdapter",
        "Assets/Game/Scripts/Composition/UiRuntimeBoundaryAdapters.cs|SelectionRectangleStateAdapter",
        "Assets/Game/Scripts/Persistence/SaveService.cs|SaveService",
        "Assets/Game/Scripts/UI/Contracts/IMatchIntroStateQuery.cs|IMatchIntroStateQuery",
        "Assets/Game/Scripts/UI/Contracts/IMatchIntroStateQuery.cs|NullMatchIntroStateQuery",
        "Assets/Game/Scripts/UI/Contracts/UiRuntimeBoundaryContracts.cs|IBuildingUiQuery",
        "Assets/Game/Scripts/UI/Settings/SettingsService.cs|SettingsService"
    };

    private static readonly string[] UiGameplayMutationTokens =
    {
        "using Unity.Entities",
        "Unity.Entities.",
        "EntityManager",
        "EntityQuery",
        "DynamicBuffer<",
        "EntityCommandBuffer",
        "World.DefaultGameObjectInjectionWorld",
        "GetExistingSystem",
        "SetComponentData",
        "AddComponentData",
        "RemoveComponent",
        "DestroyEntity",
        "CreateEntity"
    };

    private static readonly string[] ConcreteGameplaySystemTokens =
    {
        "SelectionUiCommandSystem",
        "BuildingUiCommandBoundary",
        "RuntimeGameplayStateSystem",
        "SelectionUiCameraSystem",
        "RtsSelectionInputSystem",
        "RtsSelectionInputStateSystem",
        "RtsSelectionPointerTargetCommandSystem",
        "SelectedMoveOrderCommandSystem",
        "AttackOrderCommandSystem",
        "ScanIntelCommandSystem",
        "TransportBoardingCommandSystem",
        "BuildingPlacementCommandSystem",
        "RoadBuildCommandSystem"
    };

    private static readonly string[] PointerCommandBoundaryPaths =
    {
        "Assets/Game/Scripts/Systems/RtsSelectionPointerTargetCommandSystem.cs",
        "Assets/Game/Scripts/Systems/SelectionGameplayStartupSystem.cs"
    };

    private static readonly Dictionary<string, string> FiveSystemBaseConversionTargets = new(StringComparer.Ordinal)
    {
        ["Assets/Game/Scripts/Systems/BuildingSpawnSystem.cs"] = "BuildingSpawnSystem",
        ["Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeSystem.cs"] = "BuildingProductionTransportBridgeSystem",
        ["Assets/Game/Scripts/Systems/CitizenVisibleUnitSystem.cs"] = "CitizenVisibleUnitSystem",
        ["Assets/Game/Scripts/Systems/MapVehiclePlacementSpawnSystem.cs"] = "MapVehiclePlacementSpawnSystem",
        ["Assets/Game/Scripts/Systems/CustomGameStartupSystem.cs"] = "CustomGameStartupSystem"
    };

    private static readonly string[] RetiredDirectCallContextSystemTokens =
    {
        "RtsSelectionCommandResultContextSystem",
        "RtsSelectionFocusCommandContextSystem",
        "RtsSelectionPointerTargetCommandContextSystem",
        "RtsSelectionRuntimeInputContextSystem",
        "RtsSelectionRuntimeCameraContextSystem",
        "SelectionRuntimeContextSystem",
        "SelectionRuntimeUpdateSystem"
    };

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new NonEcsSystemConversionArchitectureTests();
            tests.RuntimeSystemInventoryCanBeEnumerated();
            tests.GeneratedInventoryContainsEveryRuntimeNonEcsSystem();
            tests.DirectUnitPathRequestWritesStayInApprovedOrderOwners();
            tests.UiRuntimeMustNotMutateGameplayDirectly();
            tests.PointerAndUiBoundariesMustUseCommandRequests();
            tests.PublicNonEcsCommandMutatorHelpersStayOnApprovedTransitionList();
            tests.TopLevelGameplayNamingEscapesStayOnApprovedBoundaryList();
            tests.RetiredDirectCallContextSystemsStayDeleted();
            tests.ConvertedFiveSystemBaseTargetsStayFreeOfManagedPrefabDependencies();
            Debug.Log("[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[NonEcsSystemConversionArchitectureValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void RuntimeSystemInventoryCanBeEnumerated()
    {
        List<SystemDeclaration> declarations = EnumerateSystemDeclarations().ToList();
        List<SystemDeclaration> ecsSystems = declarations.Where(IsUnityEcsSystem).ToList();
        List<SystemDeclaration> monoBehaviours = declarations.Where(IsMonoBehaviour).ToList();
        List<SystemDeclaration> editorSystems = declarations.Where(IsEditorOnlyPath).ToList();
        List<SystemDeclaration> conversionDenominator = declarations
            .Where(declaration => !IsUnityEcsSystem(declaration))
            .Where(declaration => !IsMonoBehaviour(declaration))
            .Where(declaration => !IsEditorOnlyPath(declaration))
            .ToList();

        Debug.Log(
            "[NonEcsSystemInventory] " +
            $"totalSystemDeclarations={declarations.Count} " +
            $"unityEcs={ecsSystems.Count} " +
            $"monoBehaviour={monoBehaviours.Count} " +
            $"editorOnly={editorSystems.Count} " +
            $"runtimeNonEcsDenominator={conversionDenominator.Count}");
        Debug.Log("[NonEcsSystemInventory] editorOnlySystems:\n" + FormatDeclarations(editorSystems));
        Debug.Log("[NonEcsSystemInventory] firstWaveCandidates:\n" + FormatFirstWaveCandidates(conversionDenominator));

        Assert.Greater(
            declarations.Count,
            0,
            "The non-ECS system conversion inventory should find runtime `*System` declarations.");
        Assert.Greater(
            conversionDenominator.Count,
            0,
            "The conversion denominator should contain current plain runtime non-ECS `*System` declarations.");
    }

    [Test]
    public void GeneratedInventoryContainsEveryRuntimeNonEcsSystem()
    {
        Assert.IsTrue(
            File.Exists(InventoryPath),
            $"The non-ECS system conversion inventory is missing at `{InventoryPath}`.");

        HashSet<string> current = EnumerateSystemDeclarations()
            .Where(declaration => !IsUnityEcsSystem(declaration))
            .Where(declaration => !IsMonoBehaviour(declaration))
            .Where(declaration => !IsEditorOnlyPath(declaration))
            .Select(ToInventoryKey)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> inventoried = ParseInventoryRows(File.ReadAllLines(InventoryPath));

        string[] missing = current.Except(inventoried, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] stale = inventoried.Except(current, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();

        Assert.IsEmpty(
            missing,
            "Every plain runtime non-ECS `*System` must be present in the generated conversion inventory. Missing:\n" +
            string.Join(Environment.NewLine, missing));
        Assert.IsEmpty(
            stale,
            "The generated conversion inventory contains stale rows that no longer match runtime non-ECS `*System` declarations:\n" +
            string.Join(Environment.NewLine, stale));
    }

    [Test]
    public void DirectUnitPathRequestWritesStayInApprovedOrderOwners()
    {
        string[] violations = EnumerateSourceFiles(GameScriptsRoot)
            .Select(NormalizePath)
            .Where(path => UnitPathRequestCreationRegex.IsMatch(File.ReadAllText(path)))
            .Where(path => !ApprovedUnitPathRequestWriterPaths.Contains(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Direct `new UnitPathRequest` writes must stay inside centralized move-order, pathing, AI, or internal recovery owners. " +
            "Command/UI/boundary code should use `UnitMoveOrderRequestSystem` request APIs instead. Violations:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void UiRuntimeMustNotMutateGameplayDirectly()
    {
        string[] violations = EnumerateSourceFiles(UiScriptsRoot)
            .Select(NormalizePath)
            .Where(IsConcreteUiGameplayPath)
            .SelectMany(path => FindTokenReferences(path, UiGameplayMutationTokens.Concat(ConcreteGameplaySystemTokens)))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Concrete UI runtime code must not mutate gameplay ECS state or bind concrete gameplay systems directly. " +
            "Use UI contracts that enqueue ECS requests/results through composition/runtime boundaries. Violations:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void PointerAndUiBoundariesMustUseCommandRequests()
    {
        string[] violations = EnumeratePointerAndUiCommandBoundaryFiles()
            .SelectMany(path => FindRegexReferences(path, DirectCommandExecutionEntrypointRegex))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Pointer/UI command boundaries must request or queue ECS command data, not expose direct `Issue*` execution entrypoints. " +
            "Use `Request*`, `Queue*`, and request/result drain methods instead. Violations:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void PublicNonEcsCommandMutatorHelpersStayOnApprovedTransitionList()
    {
        Dictionary<string, int> current = EnumeratePlainRuntimeNonEcsSystemFiles()
            .SelectMany(FindPublicNonEcsCommandMutatorMethods)
            .GroupBy(method => method.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        string[] unexpected = current.Keys
            .Except(ApprovedPublicNonEcsCommandMutatorMethods.Keys, StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] countMismatches = current
            .Where(pair =>
                ApprovedPublicNonEcsCommandMutatorMethods.TryGetValue(pair.Key, out int approvedCount) &&
                approvedCount != pair.Value)
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}: approved={ApprovedPublicNonEcsCommandMutatorMethods[pair.Key]} current={pair.Value}")
            .ToArray();
        string[] stale = ApprovedPublicNonEcsCommandMutatorMethods.Keys
            .Except(current.Keys, StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            unexpected,
            "New public command-shaped helper methods on plain runtime non-ECS `*System` classes must not mutate ECS state. " +
            "Convert the owner to an ECS system/request boundary, make the helper private/internal to an ECS owner, or update the transition list with a removal owner. Unexpected:\n" +
            string.Join(Environment.NewLine, unexpected));
        Assert.IsEmpty(
            countMismatches,
            "Approved public non-ECS command mutator helper counts changed. Shrink or split the transition list instead of leaving broad approvals. Mismatches:\n" +
            string.Join(Environment.NewLine, countMismatches));
        Assert.IsEmpty(
            stale,
            "Approved public non-ECS command mutator helper entries are stale. Remove them when the transitional API is converted or folded. Stale:\n" +
            string.Join(Environment.NewLine, stale));
    }

    [Test]
    public void TopLevelGameplayNamingEscapesStayOnApprovedBoundaryList()
    {
        HashSet<string> current = EnumerateSourceFiles(GameScriptsRoot)
            .Select(NormalizePath)
            .Where(path => !path.Contains("/Editor/", StringComparison.Ordinal))
            .SelectMany(FindTopLevelNamingEscapeTypes)
            .Select(type => type.Key)
            .ToHashSet(StringComparer.Ordinal);

        string[] unexpected = current
            .Except(ApprovedTopLevelNamingEscapeTypes, StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] stale = ApprovedTopLevelNamingEscapeTypes
            .Except(current, StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            unexpected,
            "Do not rename gameplay behavior into top-level `Service`, `Query`, `Rule`, `Cell`, `Resolver`, `Adapter`, `Composer`, or `Context` types. " +
            "Use an ECS system, owner-local helper, passive UI contract, or explicitly approved boundary instead. Unexpected:\n" +
            string.Join(Environment.NewLine, unexpected));
        Assert.IsEmpty(
            stale,
            "Approved top-level naming-escape entries are stale. Remove approvals when the boundary or passive type is renamed or deleted. Stale:\n" +
            string.Join(Environment.NewLine, stale));
    }

    [Test]
    public void RetiredDirectCallContextSystemsStayDeleted()
    {
        string[] violations = EnumerateSourceFiles(GameScriptsRoot)
            .Select(NormalizePath)
            .SelectMany(path => FindTokenReferences(path, RetiredDirectCallContextSystemTokens))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Retired direct-call context wrapper systems must stay deleted. Selection startup should build narrow payloads directly and UI/pointer paths should use ECS requests/results. Violations:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void ConvertedFiveSystemBaseTargetsStayFreeOfManagedPrefabDependencies()
    {
        Dictionary<string, SystemDeclaration> declarations = EnumerateSystemDeclarations()
            .ToDictionary(ToInventoryKey, declaration => declaration, StringComparer.Ordinal);

        List<string> violations = new();
        foreach (KeyValuePair<string, string> target in FiveSystemBaseConversionTargets)
        {
            string declarationKey = $"{target.Key}|{target.Value}";
            if (!declarations.TryGetValue(declarationKey, out SystemDeclaration declaration) ||
                !IsConvertedUnmanagedSystem(declaration))
            {
                continue;
            }

            violations.AddRange(FindRegexReferences(target.Key, ConvertedTargetManagedPrefabDependencyRegex));
        }

        Assert.IsEmpty(
            violations,
            "Converted five-SystemBase target files must not carry managed prefab dependencies into `ISystem` code. " +
            "Keep `GameObject`, `UnityEngine.Object`, `List<GameObject>`, `Dictionary<..., GameObject>`, and prefab reverse lookup code in explicit managed/passive boundaries. Violations:\n" +
            string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<SystemDeclaration> EnumerateSystemDeclarations()
    {
        foreach (string path in EnumerateSourceFiles(GameScriptsRoot))
        {
            string text = File.ReadAllText(path);
            foreach (Match match in TypeDeclarationRegex.Matches(text))
            {
                string name = match.Groups["name"].Value;
                if (!name.EndsWith("System", StringComparison.Ordinal))
                    continue;

                yield return new SystemDeclaration(
                    NormalizePath(path),
                    name,
                    match.Groups["bases"].Success ? match.Groups["bases"].Value.TrimStart(':').Trim() : string.Empty);
            }
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        return Directory.Exists(root)
            ? Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal)
            : Array.Empty<string>();
    }

    private static IEnumerable<string> EnumeratePlainRuntimeNonEcsSystemFiles()
    {
        return EnumerateSystemDeclarations()
            .Where(declaration => !IsUnityEcsSystem(declaration))
            .Where(declaration => !IsMonoBehaviour(declaration))
            .Where(declaration => !IsEditorOnlyPath(declaration))
            .Select(declaration => declaration.Path)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static IEnumerable<string> EnumeratePointerAndUiCommandBoundaryFiles()
    {
        foreach (string path in PointerCommandBoundaryPaths)
        {
            if (File.Exists(path))
                yield return path;
        }

        foreach (string path in EnumerateSourceFiles(UiScriptsRoot).Select(NormalizePath).Where(IsConcreteUiGameplayPath))
            yield return path;
    }

    private static IEnumerable<PublicCommandMutatorMethod> FindPublicNonEcsCommandMutatorMethods(string path)
    {
        string text = File.ReadAllText(path);
        foreach (Match match in PublicCommandMutatorEntrypointRegex.Matches(text))
        {
            string body = FindMethodBody(text, match.Index);
            if (!ContainsEcsMutationToken(body))
                continue;

            yield return new PublicCommandMutatorMethod(
                path,
                match.Groups["name"].Value,
                GetLineNumber(text, match.Index));
        }
    }

    private static IEnumerable<NamingEscapeType> FindTopLevelNamingEscapeTypes(string path)
    {
        string text = File.ReadAllText(path);
        foreach (Match match in TopLevelTypeDeclarationRegex.Matches(text))
        {
            string name = match.Groups["name"].Value;
            if (!NamingEscapeSuffixRegex.IsMatch(name))
                continue;

            yield return new NamingEscapeType(path, name, GetLineNumber(text, match.Index));
        }
    }

    private static bool IsUnityEcsSystem(SystemDeclaration declaration)
    {
        return EcsSystemBaseRegex.IsMatch(declaration.Bases);
    }

    private static bool IsMonoBehaviour(SystemDeclaration declaration)
    {
        return MonoBehaviourBaseRegex.IsMatch(declaration.Bases);
    }

    private static bool IsConvertedUnmanagedSystem(SystemDeclaration declaration)
    {
        return Regex.IsMatch(declaration.Bases, @"\bISystem\b", RegexOptions.CultureInvariant);
    }

    private static bool IsEditorOnlyPath(SystemDeclaration declaration)
    {
        return declaration.Path.Contains("/Editor/", StringComparison.Ordinal);
    }

    private static string FormatFirstWaveCandidates(IReadOnlyCollection<SystemDeclaration> conversionDenominator)
    {
        string[] firstWave =
        {
            "SelectionMoveCommandRequestSystem",
            "SelectedMoveOrderCommandSystem",
            "SelectionAttackCommandRequestSystem",
            "AttackOrderCommandSystem",
            "SelectionScanCommandRequestSystem",
            "ScanIntelCommandSystem",
            "TransportBoardingCommandSystem",
            "BuildingTargetMoveOrderSystem",
            "CitizenMovementCommandSystem"
        };

        Dictionary<string, SystemDeclaration> byName = conversionDenominator
            .GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        return string.Join(
            Environment.NewLine,
            firstWave.Select(name => byName.TryGetValue(name, out SystemDeclaration declaration)
                ? $"{declaration.Path}: {declaration.Name}"
                : $"missing: {name}"));
    }

    private static string FormatDeclarations(IReadOnlyCollection<SystemDeclaration> declarations)
    {
        if (declarations.Count == 0)
            return "(none)";

        return string.Join(
            Environment.NewLine,
            declarations
                .OrderBy(declaration => declaration.Path, StringComparer.Ordinal)
                .ThenBy(declaration => declaration.Name, StringComparer.Ordinal)
                .Select(declaration => $"{declaration.Path}: {declaration.Name}"));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool IsConcreteUiGameplayPath(string path)
    {
        return !path.Contains("/UI/Shell/Ecs/", StringComparison.Ordinal) &&
               !ApprovedUiRuntimeEcsBoundaryPaths.Contains(path);
    }

    private static IEnumerable<string> FindTokenReferences(string path, IEnumerable<string> tokens)
    {
        string[] lines = File.ReadAllLines(path);
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            foreach (string token in tokens)
            {
                if (line.Contains(token, StringComparison.Ordinal))
                    yield return $"{path}:{lineIndex + 1} references `{token}`: {line.Trim()}";
            }
        }
    }

    private static IEnumerable<string> FindRegexReferences(string path, Regex regex)
    {
        string[] lines = File.ReadAllLines(path);
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            Match match = regex.Match(line);
            if (match.Success)
                yield return $"{path}:{lineIndex + 1} matches `{match.Value}`: {line.Trim()}";
        }
    }

    private static string FindMethodBody(string text, int declarationIndex)
    {
        int openBrace = text.IndexOf('{', declarationIndex);
        if (openBrace < 0)
            return string.Empty;

        int depth = 0;
        for (int index = openBrace; index < text.Length; index++)
        {
            char value = text[index];
            if (value == '{')
            {
                depth++;
                continue;
            }

            if (value != '}')
                continue;

            depth--;
            if (depth == 0)
                return text[declarationIndex..(index + 1)];
        }

        return text[declarationIndex..];
    }

    private static bool ContainsEcsMutationToken(string text)
    {
        string[] tokens =
        {
            "EntityManager",
            "EntityCommandBuffer",
            "DynamicBuffer<",
            "GetBuffer<",
            "SetComponentData",
            "AddComponent",
            "RemoveComponent",
            "DestroyEntity",
            "CreateEntity",
            ".Playback(",
            "UnitMoveOrderRequestSystem.EnqueueAndProcess",
            "UnitAttackOrderRequestSystem.EnqueueAndProcess",
            "ClearMovementOrderComponents",
            "ClearCommandedAttackOrderComponents"
        };

        return tokens.Any(token => text.Contains(token, StringComparison.Ordinal));
    }

    private static int GetLineNumber(string text, int index)
    {
        int line = 1;
        for (int i = 0; i < index; i++)
        {
            if (text[i] == '\n')
                line++;
        }

        return line;
    }

    private static HashSet<string> ParseInventoryRows(IEnumerable<string> lines)
    {
        HashSet<string> rows = new(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            if (!line.StartsWith("| `Assets/", StringComparison.Ordinal))
                continue;

            string[] columns = line.Split('|');
            if (columns.Length < 4)
                continue;

            string path = UnwrapCode(columns[1].Trim());
            string name = UnwrapCode(columns[2].Trim());
            if (path.Length == 0 || name.Length == 0)
                continue;

            rows.Add($"{path}|{name}");
        }

        return rows;
    }

    private static string ToInventoryKey(SystemDeclaration declaration)
    {
        return $"{declaration.Path}|{declaration.Name}";
    }

    private static string UnwrapCode(string value)
    {
        return value.Length >= 2 && value[0] == '`' && value[^1] == '`'
            ? value[1..^1]
            : value;
    }

    private readonly struct SystemDeclaration
    {
        public readonly string Path;
        public readonly string Name;
        public readonly string Bases;

        public SystemDeclaration(string path, string name, string bases)
        {
            Path = path;
            Name = name;
            Bases = bases;
        }
    }

    private readonly struct PublicCommandMutatorMethod
    {
        public readonly string Path;
        public readonly string Name;
        public readonly int Line;

        public PublicCommandMutatorMethod(string path, string name, int line)
        {
            Path = path;
            Name = name;
            Line = line;
        }

        public string Key => $"{Path}|{Name}";
    }

    private readonly struct NamingEscapeType
    {
        public readonly string Path;
        public readonly string Name;
        public readonly int Line;

        public NamingEscapeType(string path, string name, int line)
        {
            Path = path;
            Name = name;
            Line = line;
        }

        public string Key => $"{Path}|{Name}";
    }
}
#endif
