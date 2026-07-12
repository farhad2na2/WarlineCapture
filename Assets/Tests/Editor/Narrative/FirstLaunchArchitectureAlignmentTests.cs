using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public sealed class FirstLaunchArchitectureAlignmentTests
{
    private const string CompositionRoot = "Assets/Game/Scripts/Composition/Narrative";
    private const string ConfigRoot = "Assets/Game/Scripts/Configs/Narrative";
    private const string NarrativeContractsRoot = "Assets/Game/Scripts/Narrative/Contracts";
    private const string NarrativeRuntimeRoot = "Assets/Game/Scripts/Narrative/Runtime";
    private const string UiRoot = "Assets/Game/Scripts/UI/Narrative";
    private const string NarrativeRuntimeAsmdefPath =
        "Assets/Game/Scripts/Narrative/Runtime/Game.Narrative.Runtime.asmdef";
    private const string NarrativeContractsAsmdefPath =
        "Assets/Game/Scripts/Narrative/Contracts/Game.Narrative.Contracts.asmdef";
    private const string NarrativeUiContractsPath =
        "Assets/Game/Scripts/UI/Contracts/Narrative/NarrativeUiContracts.cs";
    private const string UiAsmdefPath = "Assets/Game/Scripts/UI/Game.UI.Runtime.asmdef";
    private const string MenuBootstrapPath = "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs";
    private const string FirstLaunchCompositionPath =
        "Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeCompositionSystemHelper.cs";

    private static readonly string[] BroadTypeTokens =
    {
        "Manager",
        "Controller",
        "Player",
        "Coordinator",
        "Presenter",
        "Facade",
        "Installer",
        "Orchestrator",
        "Factory",
    };

    private static readonly string[] ApprovedClassSuffixes =
    {
        "View",
        "Config",
        "Catalog",
        "Record",
        "Model",
        "PresentationSystemHelper",
        "CompositionSystemHelper",
        "UtilitySystemHelper",
    };

    private static readonly Regex ClassDeclarationRegex = new(
        @"\bclass\s+(?<name>[A-Za-z_]\w*)\b",
        RegexOptions.CultureInvariant);

    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchArchitectureAlignmentTests tests = new();
            tests.RuntimeTypeNamesUseApprovedFirstLaunchBoundaries();
            tests.UiRuntimeAssemblyPreservesNarrativeDependencyDirection();
            tests.MenuBootstrapUsesOnlyTheFirstLaunchCompositionBoundary();
            tests.CompositionOwnerUsesDedicatedProfileShellAndReviewBoundaries();
            tests.NarrativeRuntimeOwnsRoutePolicyWithoutUiCompositionOrEcsDependencies();
            tests.SequenceProgressionStaysInPureNarrativeRuntime();
            tests.NarrativeContractsOwnDomainDataWithoutUiDependencies();
            tests.ProductionPolicyConsumesAuthoredNarrativeMetadata();
            tests.PanelResidencyIsAsynchronousAndOutsideSequenceProgression();
            tests.NarrativeViewsRemainPassiveReferenceAndIntentBoundaries();
            Debug.Log("[FirstLaunchArchitectureAlignmentValidation] result=Passed tests=10");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[FirstLaunchArchitectureAlignmentValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RuntimeTypeNamesUseApprovedFirstLaunchBoundaries()
    {
        List<string> violations = new();
        foreach (string path in EnumerateRuntimeFiles())
        {
            string source = File.ReadAllText(path);
            foreach (Match match in ClassDeclarationRegex.Matches(source))
            {
                string typeName = match.Groups["name"].Value;
                string broadToken = BroadTypeTokens.FirstOrDefault(token =>
                    typeName.Contains(token, StringComparison.Ordinal));
                if (broadToken != null)
                {
                    violations.Add($"{path} declares `{typeName}` using broad token `{broadToken}`.");
                    continue;
                }

                if (!ApprovedClassSuffixes.Any(suffix => typeName.EndsWith(suffix, StringComparison.Ordinal)))
                    violations.Add($"{path} declares `{typeName}` without an approved FirstLaunch boundary suffix.");
            }
        }

        Assert.IsEmpty(
            violations,
            "FirstLaunch runtime classes must use passive View/Config/data names or approved managed reason suffixes.\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void UiRuntimeAssemblyPreservesNarrativeDependencyDirection()
    {
        string asmdef = File.ReadAllText(UiAsmdefPath);
        string[] forbiddenReferences =
        {
            "Game.Composition",
            "Game.Configs",
            "Game.Runtime",
            "Unity.Entities",
        };

        string[] violations = forbiddenReferences
            .Where(reference => asmdef.Contains($"\"{reference}\"", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Game.UI.Runtime must remain a presentation edge and must not reference composition, configs, runtime, or ECS. Violations: " +
            string.Join(", ", violations));
    }

    [Test]
    public void MenuBootstrapUsesOnlyTheFirstLaunchCompositionBoundary()
    {
        string source = File.ReadAllText(MenuBootstrapPath);
        StringAssert.Contains("FirstLaunchNarrativeCompositionSystemHelper", source);

        string[] forbiddenConcreteOwners =
        {
            "FirstLaunchNarrativeSequencePresentationSystemHelper",
            "FirstLaunchNarrativeAudioPresentationSystemHelper",
            "FirstLaunchNarrativeModelUtilitySystemHelper",
            "NarrativeDialoguePresentationSystemHelper",
            "NarrativePanelAssetResidencyPresentationSystemHelper",
        };
        string[] violations = forbiddenConcreteOwners
            .Where(source.Contains)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "Menu bootstrap may bind FirstLaunch only through its composition boundary. Violations: " +
            string.Join(", ", violations));
    }

    [Test]
    public void CompositionOwnerUsesDedicatedProfileShellAndReviewBoundaries()
    {
        string source = File.ReadAllText(FirstLaunchCompositionPath);
        string[] requiredBoundaries =
        {
            "FirstLaunchNarrativeProfileCompositionSystemHelper",
            "FirstLaunchNarrativeShellCompositionSystemHelper",
            "FirstLaunchNarrativeReviewPresentationSystemHelper",
        };
        foreach (string required in requiredBoundaries)
            StringAssert.Contains(required, source);

        string[] forbiddenResponsibilities =
        {
            "PlayerProfileSaveData",
            "UiShellStartupDispositionComponent",
            "UiShellRouteRequestComponent",
            "NarrativeReviewerAction",
            "lastReviewerStateIndex",
        };
        string[] violations = forbiddenResponsibilities
            .Where(source.Contains)
            .ToArray();

        Assert.IsEmpty(
            violations,
            "FirstLaunch composition must delegate profile storage, shell ECS state, and reviewer presentation. Violations: " +
            string.Join(", ", violations));
    }

    [Test]
    public void NarrativeRuntimeOwnsRoutePolicyWithoutUiCompositionOrEcsDependencies()
    {
        string asmdef = File.ReadAllText(NarrativeRuntimeAsmdefPath);
        string[] forbiddenReferences =
        {
            "Game.Composition",
            "Game.UI.Contracts",
            "Game.UI.Runtime",
            "Unity.Entities",
        };
        string[] assemblyViolations = forbiddenReferences
            .Where(reference => asmdef.Contains($"\"{reference}\"", StringComparison.Ordinal))
            .ToArray();
        Assert.IsEmpty(
            assemblyViolations,
            "Game.Narrative.Runtime must remain independent of UI, composition, and ECS. Violations: " +
            string.Join(", ", assemblyViolations));

        string compositionSource = File.ReadAllText(FirstLaunchCompositionPath);
        string reviewSource = File.ReadAllText(
            Path.Combine(CompositionRoot, "FirstLaunchNarrativeReviewPresentationSystemHelper.cs"));
        string[] forbiddenRouteLiterals =
        {
            "first_launch.m01_handoff",
            "first_launch.gameplay_placeholder",
            "first_launch.command_base_reveal",
            "FL-P19",
        };
        string[] policyViolations = forbiddenRouteLiterals
            .Where(route => compositionSource.Contains(route, StringComparison.Ordinal) ||
                            reviewSource.Contains(route, StringComparison.Ordinal))
            .ToArray();
        Assert.IsEmpty(
            policyViolations,
            "Composition and reviewer presentation must apply typed narrative route decisions, not own route IDs. Violations: " +
            string.Join(", ", policyViolations));
    }

    [Test]
    public void SequenceProgressionStaysInPureNarrativeRuntime()
    {
        string runtimeSource = File.ReadAllText(Path.Combine(
            NarrativeRuntimeRoot,
            "FirstLaunchNarrativeSequenceUtilitySystemHelper.cs"));
        string[] forbiddenRuntimeDependencies =
        {
            "Game.UI",
            "Game.Composition",
            "UnityEngine",
            "NarrativeSequenceView",
            "AudioSource",
            "Addressables",
            "AssetDatabase",
        };
        string[] runtimeViolations = forbiddenRuntimeDependencies
            .Where(runtimeSource.Contains)
            .ToArray();
        Assert.IsEmpty(
            runtimeViolations,
            "Pure narrative sequence progression cannot depend on presentation, composition, Unity objects, or editor APIs. Violations: " +
            string.Join(", ", runtimeViolations));

        string presentationSource = File.ReadAllText(Path.Combine(
            CompositionRoot,
            "FirstLaunchNarrativeSequencePresentationSystemHelper.cs"));
        StringAssert.Contains("FirstLaunchNarrativeSequenceUtilitySystemHelper", presentationSource);
        string[] forbiddenPresentationState =
        {
            "private float stateElapsed",
            "private bool autoAdvancePending",
            "transitionToken++",
            "private void AdvanceLineOrState",
            "private static bool Reached",
        };
        string[] presentationViolations = forbiddenPresentationState
            .Where(presentationSource.Contains)
            .ToArray();
        Assert.IsEmpty(
            presentationViolations,
            "Managed sequence presentation must apply typed runtime outputs, not own progression state. Violations: " +
            string.Join(", ", presentationViolations));
    }

    [Test]
    public void NarrativeContractsOwnDomainDataWithoutUiDependencies()
    {
        string asmdef = File.ReadAllText(NarrativeContractsAsmdefPath);
        string[] forbiddenReferences =
        {
            "Game.Composition",
            "Game.Configs",
            "Game.Runtime",
            "Game.UI.Contracts",
            "Game.UI.Runtime",
            "Unity.Entities",
        };
        string[] assemblyViolations = forbiddenReferences
            .Where(reference => asmdef.Contains($"\"{reference}\"", StringComparison.Ordinal))
            .ToArray();
        Assert.IsEmpty(
            assemblyViolations,
            "Game.Narrative.Contracts must remain a dependency-free domain contract boundary. Violations: " +
            string.Join(", ", assemblyViolations));

        string uiContracts = File.ReadAllText(NarrativeUiContractsPath);
        string[] domainTypes =
        {
            "NarrativeCommanderIdentityData",
            "NarrativeGuidanceMode",
            "NarrativeCompletionPayload",
            "NarrativeHandoffResult",
        };
        string[] ownershipViolations = domainTypes
            .Where(uiContracts.Contains)
            .ToArray();
        Assert.IsEmpty(
            ownershipViolations,
            "Narrative identity, guidance, completion, and handoff contracts cannot return to UI ownership. Violations: " +
            string.Join(", ", ownershipViolations));
    }

    [Test]
    public void ProductionPolicyConsumesAuthoredNarrativeMetadata()
    {
        string[] policySources =
        {
            "FirstLaunchNarrativeAudioPresentationSystemHelper.cs",
            "FirstLaunchNarrativeModelUtilitySystemHelper.cs",
            "FirstLaunchNarrativeProfileCompositionSystemHelper.cs",
        };
        List<string> violations = new();
        foreach (string fileName in policySources)
        {
            string source = File.ReadAllText(Path.Combine(CompositionRoot, fileName));
            if (source.Contains("\"FL-P", StringComparison.Ordinal) ||
                source.Contains("\"first_launch.", StringComparison.Ordinal))
            {
                violations.Add(fileName);
            }
        }

        string routeSource = File.ReadAllText(Path.Combine(
            NarrativeRuntimeRoot,
            "FirstLaunchNarrativeRouteUtilitySystemHelper.cs"));
        if (routeSource.Contains("\"FL-P", StringComparison.Ordinal) ||
            routeSource.Contains("\"first_launch.", StringComparison.Ordinal))
        {
            violations.Add("FirstLaunchNarrativeRouteUtilitySystemHelper.cs");
        }
        Assert.IsEmpty(
            violations,
            "Production narrative policy must consume authored cues, roles, and completion metadata instead of state IDs. Violations: " +
            string.Join(", ", violations));

        string configSource = File.ReadAllText(Path.Combine(ConfigRoot, "NarrativeSequenceConfig.cs"));
        foreach (string property in new[]
                 {
                     "MusicCue", "AmbienceCue", "VehicleCue", "EventCue", "RouteRole",
                     "CompletionPayloadId", "EvidenceIds", "MissionContextFlags"
                 })
        {
            StringAssert.Contains(property, configSource);
        }
    }

    [Test]
    public void PanelResidencyIsAsynchronousAndOutsideSequenceProgression()
    {
        string residencySource = File.ReadAllText(Path.Combine(
            UiRoot,
            "NarrativePanelAssetResidencyPresentationSystemHelper.cs"));
        StringAssert.DoesNotContain("WaitForCompletion", residencySource);
        StringAssert.Contains("Addressables.LoadAssetAsync", residencySource);
        StringAssert.Contains("CurrentReady", residencySource);

        string sequenceSource = File.ReadAllText(FirstLaunchCompositionPath.Replace(
            "FirstLaunchNarrativeCompositionSystemHelper.cs",
            "FirstLaunchNarrativeSequencePresentationSystemHelper.cs"));
        StringAssert.Contains("FirstLaunchNarrativePanelPresentationSystemHelper", sequenceSource);
        StringAssert.DoesNotContain("Addressables.LoadAssetAsync", sequenceSource);
        StringAssert.DoesNotContain("AssetReferenceSprite", sequenceSource);
    }

    [Test]
    public void NarrativeViewsRemainPassiveReferenceAndIntentBoundaries()
    {
        string sequenceViewSource = File.ReadAllText(Path.Combine(UiRoot, "NarrativeSequenceView.cs"));
        string commanderViewSource = File.ReadAllText(Path.Combine(UiRoot, "NarrativeCommanderIdentityView.cs"));
        string guidanceViewSource = File.ReadAllText(Path.Combine(UiRoot, "NarrativeGuidanceChoiceView.cs"));
        string[] policyTokens =
        {
            "NarrativeUiAction",
            "SetActionContext",
            "commitRequested",
            "defaultCallsign",
            "defaultDisplayName",
            "defaultPortraitIndex",
            "PlayerPrefs",
            "Addressables",
            "Resources.Load",
            "transform.Find",
        };
        List<string> violations = new();
        foreach ((string name, string source) in new[]
                 {
                     ("NarrativeSequenceView", sequenceViewSource),
                     ("NarrativeCommanderIdentityView", commanderViewSource),
                     ("NarrativeGuidanceChoiceView", guidanceViewSource),
                 })
        {
            violations.AddRange(policyTokens
                .Where(source.Contains)
                .Select(token => $"{name}:{token}"));
        }

        Assert.IsEmpty(
            violations,
            "Narrative views may bind references, project visuals, and emit raw UI intents, but cannot own context, defaults, persistence, loading, or commit policy. Violations: " +
            string.Join(", ", violations));

        string interactiveSource = File.ReadAllText(Path.Combine(
            CompositionRoot,
            "FirstLaunchNarrativeInteractivePresentationSystemHelper.cs"));
        StringAssert.Contains("NarrativeUiAction", interactiveSource);
        StringAssert.Contains("commitRequested", interactiveSource);
        StringAssert.Contains("FallbackCallsign", interactiveSource);
    }

    private static IEnumerable<string> EnumerateRuntimeFiles()
    {
        return new[] { CompositionRoot, ConfigRoot, NarrativeContractsRoot, NarrativeRuntimeRoot, UiRoot }
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.Ordinal);
    }
}
