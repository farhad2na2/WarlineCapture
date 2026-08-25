using System;
using System.IO;
using System.Reflection;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Tactical.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class M01FirstContactContractTests
{
    private const string PassMarker = "[M01FirstContactContractValidation] result=Passed tests=12";
    private const string ScenarioPath =
        "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset";

    public static void RunFocusedValidation()
    {
        try
        {
            M01FirstContactContractTests tests = new();
            tests.EnumNumericValuesAreFrozen();
            tests.ContractsUseDependencyRootAssembly();
            tests.ValidM01DefinitionPasses();
            tests.InvalidMissionIdentityFailsClosed();
            tests.DuplicateObjectivesFailClosed();
            tests.MissingReferencesFailClosed();
            tests.InvalidStarThresholdFailsClosed();
            tests.M01IntelRewardFailsClosed();
            tests.DuplicateCommandsFailClosed();
            tests.MissingReadinessFailsClosed();
            tests.DuplicateCatalogIdentityFailsClosed();
            tests.CatalogResolvesCanonicalDefinition();
            Debug.Log(PassMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactContractValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void EnumNumericValuesAreFrozen()
    {
        Assert.AreEqual(2, (int)MissionLaunchOriginKind.CampaignOperations);
        Assert.AreEqual(3, (int)MissionRunKind.Replay);
        Assert.AreEqual(10, (int)MissionPhaseKind.ReturnReplay);
        Assert.AreEqual(2, (int)MissionOutcomeKind.Defeat);
        Assert.AreEqual(5, (int)MissionActionKind.SetReplayTutorial);
        Assert.AreEqual(2, (int)MissionReturnDestinationKind.CampaignOperations);
        Assert.AreEqual(2, (int)MissionObjectiveRuleKind.ProtectMissionRole);
        Assert.AreEqual(3, (int)MissionStarRuleKind.CompleteUnderMilliseconds);
        Assert.AreEqual(4, (int)MissionRewardKind.Intel);
    }

    [Test]
    public void ContractsUseDependencyRootAssembly()
    {
        Assembly assembly = typeof(MissionLaunchOriginKind).Assembly;
        Assert.AreEqual("Game.Missions.Contracts", assembly.GetName().Name);
        Assert.IsNotNull(typeof(MissionLaunchPayload));
        Assert.IsNotNull(typeof(MissionResultSummary));
        Assert.IsNotNull(typeof(MissionActionResult));
    }

    [Test]
    public void ValidM01DefinitionPasses()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            Assert.IsTrue(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error), error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void InvalidMissionIdentityFailsClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, "missionId", "mission 01");
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("Invalid mission id", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void DuplicateObjectivesFailClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            MissionObjectiveDefinitionConfig objective = Objective();
            SetField(definition, "objectives", new[] { objective, objective });
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("duplicate objective", error.ToLowerInvariant());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void MissingReferencesFailClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, "scenarioId", string.Empty);
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("scenario or operation-map", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void InvalidStarThresholdFailsClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, "stars", new[]
            {
                new MissionStarDefinitionConfig(1, MissionStarRuleKind.CompleteUnderMilliseconds, 0)
            });
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("invalid star rule", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void M01IntelRewardFailsClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, "firstClearRewards", new[]
            {
                new MissionRewardDefinitionConfig(MissionRewardKind.Intel, 1)
            });
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("invalid first-clear reward", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void DuplicateCommandsFailClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, "commandPolicy", new MissionCommandPolicyConfig(new[]
            {
                TacticalCommandMode.Move,
                TacticalCommandMode.Move
            }));
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("duplicate command", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void MissingReadinessFailsClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, "requiredFeatureIds", Array.Empty<string>());
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("feature-readiness", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void DuplicateCatalogIdentityFailsClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        MissionDefinitionCatalogConfig catalog = CreateCatalog(definition, definition);
        try
        {
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateCatalog(catalog, out string error));
            StringAssert.Contains("Duplicate mission catalog id", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void CatalogResolvesCanonicalDefinition()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        MissionDefinitionCatalogConfig catalog = CreateCatalog(definition);
        try
        {
            Assert.IsTrue(MissionDefinitionContractValidation.TryValidateCatalog(catalog, out string error), error);
            Assert.IsTrue(catalog.TryResolve(MissionDefinitionContractValidation.FirstContactMissionId, out var resolved));
            Assert.AreSame(definition, resolved);
            Assert.IsFalse(catalog.TryResolve("saga.ch01.m02.missing", out _));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    private static MissionDefinitionConfig CreateValidDefinition()
    {
        MissionDefinitionConfig definition = ScriptableObject.CreateInstance<MissionDefinitionConfig>();
        SetField(definition, "missionId", MissionDefinitionContractValidation.FirstContactMissionId);
        SetField(definition, "schemaVersion", 1);
        SetField(definition, "displayNameKey", "mission.m01.name");
        SetField(definition, "displaySummaryKey", "mission.m01.summary");
        SetField(definition, "locationNameKey", "mission.m01.location");
        SetField(definition, "scenarioId", "scenario.ch01.m01.first_contact");
        SetField(definition, "operationMapId", "opmap.ch01.district_edge_01");
        SetField(definition, "briefingSequenceId", "seq.ch01.m01.brief");
        SetField(definition, "commsSequenceId", "seq.ch01.m01.comms");
        SetField(definition, "debriefSequenceId", "seq.ch01.m01.debrief");
        SetField(definition, "objectives", new[] { Objective() });
        SetField(definition, "stars", new[]
        {
            new MissionStarDefinitionConfig(1, MissionStarRuleKind.CompleteMission),
            new MissionStarDefinitionConfig(2, MissionStarRuleKind.NoSquadLoss),
            new MissionStarDefinitionConfig(3, MissionStarRuleKind.CompleteUnderMilliseconds, 420000)
        });
        SetField(definition, "firstClearRewards", new[]
        {
            new MissionRewardDefinitionConfig(MissionRewardKind.Credits, 1200),
            new MissionRewardDefinitionConfig(MissionRewardKind.Materials, 250)
        });
        SetField(definition, "replayRewards", new[]
        {
            new MissionRewardDefinitionConfig(MissionRewardKind.Credits, 250)
        });
        SetField(definition, "commandPolicy", new MissionCommandPolicyConfig(new[]
        {
            TacticalCommandMode.Select,
            TacticalCommandMode.Move,
            TacticalCommandMode.Attack,
            TacticalCommandMode.Hold,
            TacticalCommandMode.Stop
        }));
        SetField(definition, "replayAllowed", true);
        SetField(definition, "requireOperationMapReady", true);
        SetField(definition, "requireGridReady", true);
        SetField(definition, "requireUnitCatalogReady", true);
        SetField(definition, "requiredFeatureIds", new[]
        {
            "feature.operation_map",
            "feature.unit_catalog",
            "feature.tactical_commands"
        });
        return definition;
    }

    private static MissionObjectiveDefinitionConfig Objective() => new(
        "objective.m01.secure_corridor",
        "mission.m01.objective.secure_corridor",
        MissionObjectiveRuleKind.DestroyMissionRole,
        "role.hostile.patrol",
        3);

    private static MissionDefinitionCatalogConfig CreateCatalog(
        MissionDefinitionConfig definition,
        MissionDefinitionConfig duplicate = null)
    {
        ScenarioSetupConfig scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(ScenarioPath);
        Assert.IsNotNull(scenario, $"Missing canonical M1 scenario at '{ScenarioPath}'.");
        MissionDefinitionCatalogConfig catalog = ScriptableObject.CreateInstance<MissionDefinitionCatalogConfig>();
        MissionDefinitionCatalogEntryConfig entry = new(definition.MissionId, definition, scenario);
        SetField(catalog, "entries", duplicate == null
            ? new[] { entry }
            : new[]
            {
                entry,
                new MissionDefinitionCatalogEntryConfig(duplicate.MissionId, duplicate, scenario)
            });
        return catalog;
    }

    private static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{name}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
