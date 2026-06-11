#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        { "Assets/Game/Scripts/UI/Shell/UIGameLaunchUtility.cs|Debug.LogError", 2 },
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
        "BuildingPlacementAdapterSystem",
    };

    private static readonly HashSet<string> SelectionPanelConcreteSystemBindingAllowlist = new(StringComparer.Ordinal)
    {
        "Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs",
        "Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs",
    };

    private static readonly HashSet<string> StaticUiRegistryDebtAllowlist = new(StringComparer.Ordinal);

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

    private static readonly string[] BroadNameTokens =
    {
        "Manager",
        "Controller",
        "Presenter",
        "Facade",
        "Installer",
        "Orchestrator",
    };

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
    public void RuntimeAssemblyMustNotReferenceUiShellEcsAssembly()
    {
        string runtimeAsmdefPath = Path.Combine(GameScriptsRoot, "Game.Runtime.asmdef");
        string asmdef = File.ReadAllText(runtimeAsmdefPath);

        Assert.IsFalse(
            asmdef.Contains("\"Game.UI.Shell.Ecs\"", StringComparison.Ordinal),
            "`Game.Runtime` must not reference `Game.UI.Shell.Ecs`. Shared shell route/state data belongs in `Game.UI.Contracts`; shell systems stay UI-owned.");
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
    public void BuildingProductionSystemMustNotReadAuthoringComponents()
    {
        string productionPath = Path.Combine(GameScriptsRoot, "Systems/BuildingProductionSystem.cs");
        string source = File.ReadAllText(productionPath);

        Assert.IsFalse(
            source.Contains("UnitGridAuthoring", StringComparison.Ordinal) ||
            source.Contains("BuildingDefinitionAuthoring", StringComparison.Ordinal),
            "`BuildingProductionSystem` must not read authoring components. Composition can inject unit production metadata through `ConfigureUnitProductionMetadataResolver`.");
    }

    [Test]
    public void SceneAndMapAuthoringBootstrapMustStayInComposition()
    {
        string[] compositionOwnedFiles =
        {
            "GameplayFeatureStartupSystem.cs",
            "GameplaySceneBindingSystem.cs",
            "MapSurfaceRuntimeBootstrapSystem.cs",
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

    private static bool IsCompositionPath(string path)
    {
        return NormalizePath(path).Contains("/Composition/", StringComparison.Ordinal);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
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
}
#endif
