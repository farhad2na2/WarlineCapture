using System;
using System.Reflection;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;

public sealed class M01FirstContactScenarioCompatibilityTests
{
    private const string PassMarker = "[M01FirstContactScenarioCompatibilityValidation] result=Passed tests=8";

    public static void RunFocusedValidation()
    {
        try
        {
            M01FirstContactScenarioCompatibilityTests tests = new();
            tests.LegacySkirmishJsonRetainsDefaultBehavior();
            tests.LegacySkirmishDoesNotRequireCampaignFields();
            tests.ValidCampaignScenarioPasses();
            tests.CampaignRequiresDeterministicSeed();
            tests.CampaignRequiresFriendlyAndHostileGroups();
            tests.UnitIdentityFailsClosed();
            tests.PatrolMustReferenceAGroup();
            tests.RestrictionsRoundTripWithoutChangingSkirmishDefaults();
            Debug.Log(PassMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactScenarioCompatibilityValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void LegacySkirmishJsonRetainsDefaultBehavior()
    {
        ScenarioSetupConfig scenario = CreateLegacySkirmish();
        try
        {
            Assert.IsTrue(scenario.TryValidate(out string error), error);
            Assert.AreEqual(0, scenario.DeterministicSeed);
            Assert.AreEqual(0, scenario.EncounterStartMilliseconds);
            Assert.AreEqual(0, scenario.UnitGroups.Length);
            Assert.AreEqual(0, scenario.PatrolRoutes.Length);
            Assert.AreEqual(0, scenario.AmbientPresentations.Length);
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void LegacySkirmishDoesNotRequireCampaignFields()
    {
        ScenarioSetupConfig scenario = CreateLegacySkirmish();
        try
        {
            SetField<ScenarioUnitGroupConfig[]>(scenario, "unitGroups", null);
            SetField<ScenarioPatrolRouteConfig[]>(scenario, "patrolRoutes", null);
            SetField<ScenarioAmbientPresentationConfig[]>(scenario, "ambientPresentations", null);
            Assert.IsTrue(scenario.TryValidate(out string error), error);
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void ValidCampaignScenarioPasses()
    {
        ScenarioSetupConfig scenario = CreateCampaign();
        try { Assert.IsTrue(scenario.TryValidate(out string error), error); }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void CampaignRequiresDeterministicSeed()
    {
        ScenarioSetupConfig scenario = CreateCampaign();
        try
        {
            SetField(scenario, "deterministicSeed", 0);
            Assert.IsFalse(scenario.TryValidate(out string error));
            StringAssert.Contains("deterministic setup", error);
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void CampaignRequiresFriendlyAndHostileGroups()
    {
        ScenarioSetupConfig scenario = CreateCampaign();
        try
        {
            SetField(scenario, "unitGroups", new[] { FriendlyGroup() });
            Assert.IsFalse(scenario.TryValidate(out string error));
            StringAssert.Contains("force groups", error);
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void UnitIdentityFailsClosed()
    {
        ScenarioSetupConfig scenario = CreateCampaign();
        try
        {
            ScenarioUnitEntryConfig invalid = new(
                "unit.jrc.rifle", "NOT_A_GUID", "anchor.m01.friendly_spawn", "role.friendly.squad", 4);
            SetField(scenario, "unitGroups", new[]
            {
                new ScenarioUnitGroupConfig("group.friendly", 1, new[] { invalid }), HostileGroup()
            });
            Assert.IsFalse(scenario.TryValidate(out string error));
            StringAssert.Contains("invalid unit", error);
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void PatrolMustReferenceAGroup()
    {
        ScenarioSetupConfig scenario = CreateCampaign();
        try
        {
            SetField(scenario, "patrolRoutes", new[]
            {
                new ScenarioPatrolRouteConfig(
                    "route.hostile.patrol", "group.missing",
                    new[] { "anchor.m01.patrol_a", "anchor.m01.patrol_b" }, 1000)
            });
            Assert.IsFalse(scenario.TryValidate(out string error));
            StringAssert.Contains("invalid patrol route", error);
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void RestrictionsRoundTripWithoutChangingSkirmishDefaults()
    {
        ScenarioSetupConfig campaign = CreateCampaign();
        ScenarioSetupConfig copy = ScriptableObject.CreateInstance<ScenarioSetupConfig>();
        ScenarioSetupConfig skirmish = CreateLegacySkirmish();
        try
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(campaign), copy);
            Assert.IsTrue(copy.Restrictions.BuildingDisabled);
            Assert.IsTrue(copy.Restrictions.ProductionDisabled);
            Assert.IsTrue(copy.Restrictions.EconomyDisabled);
            Assert.IsTrue(copy.Restrictions.TransportDisabled);
            Assert.IsTrue(copy.Restrictions.AirDisabled);
            Assert.IsFalse(skirmish.Restrictions.BuildingDisabled);
            Assert.IsFalse(skirmish.Restrictions.ProductionDisabled);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(campaign);
            UnityEngine.Object.DestroyImmediate(copy);
            UnityEngine.Object.DestroyImmediate(skirmish);
        }
    }

    private static ScenarioSetupConfig CreateLegacySkirmish()
    {
        ScenarioSetupConfig scenario = ScriptableObject.CreateInstance<ScenarioSetupConfig>();
        JsonUtility.FromJsonOverwrite(
            "{\"scenarioId\":\"scenario.skirmish.default\",\"operationMapId\":\"opmap.skirmish.desert_base_01\",\"requiredAnchors\":[]}",
            scenario);
        return scenario;
    }

    private static ScenarioSetupConfig CreateCampaign()
    {
        ScenarioSetupConfig scenario = ScriptableObject.CreateInstance<ScenarioSetupConfig>();
        SetField(scenario, "scenarioId", "scenario.ch01.m01.first_contact");
        SetField(scenario, "operationMapId", "opmap.ch01.district_edge_01");
        SetField(scenario, "requiredAnchors", Array.Empty<ScenarioAnchorRequirementConfig>());
        SetField(scenario, "deterministicSeed", 104729);
        SetField(scenario, "encounterStartMilliseconds", 1500);
        SetField(scenario, "unitGroups", new[] { FriendlyGroup(), HostileGroup() });
        SetField(scenario, "patrolRoutes", new[]
        {
            new ScenarioPatrolRouteConfig(
                "route.hostile.patrol", "group.hostile",
                new[] { "anchor.m01.patrol_a", "anchor.m01.patrol_b" }, 1000)
        });
        SetField(scenario, "restrictions", new ScenarioRestrictionConfig(true, true, true, true, true));
        SetField(scenario, "ambientPresentations", new[]
        {
            new ScenarioAmbientPresentationConfig(
                "ambient.civilians", "anchor.m01.civilian_safe", "route.civilian.evacuation", 6)
        });
        return scenario;
    }

    private static ScenarioUnitGroupConfig FriendlyGroup() => new(
        "group.friendly", 1, new[]
        {
            new ScenarioUnitEntryConfig(
                "unit.jrc.rifle", "0123456789abcdef0123456789abcdef",
                "anchor.m01.friendly_spawn", "role.friendly.squad", 4)
        });

    private static ScenarioUnitGroupConfig HostileGroup() => new(
        "group.hostile", 2, new[]
        {
            new ScenarioUnitEntryConfig(
                "unit.hostile.courier", "abcdef0123456789abcdef0123456789",
                "anchor.m01.hostile_spawn", "role.hostile.patrol", 3)
        });

    private static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{name}'.");
        field.SetValue(target, value);
    }
}
