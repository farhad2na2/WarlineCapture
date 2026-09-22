using System.Reflection;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapIdentityConfigTests
{
    [TestCase("opmap.skirmish.desert_base_01")]
    [TestCase("opmap.ch01.district_edge_01")]
    [TestCase("opmap.ch12.airfield_2")]
    [TestCase("opmap.operations.old_quarter")]
    [TestCase("opmap.operations.airport_perimeter")]
    public void OperationMapId_AcceptsCanonicalValues(string value)
    {
        Assert.That(OperationMapIdentityRules.IsValidOperationMapId(value), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("opmap.Ch01.district_edge_01")]
    [TestCase("opmap/ch01/district_edge_01")]
    [TestCase("opmap.ch01")]
    [TestCase("opmap.campaign.district_edge_01")]
    [TestCase("opmap.ch01.district-edge")]
    [TestCase("opmap.ch01._district")]
    [TestCase("opmap.operations.unknown")]
    [TestCase("opmap.operations.old_quarter.extra")]
    [TestCase("opmap.ch01.district.edge")]
    public void OperationMapId_RejectsNonCanonicalValues(string value)
    {
        Assert.That(OperationMapIdentityRules.IsValidOperationMapId(value), Is.False);
    }

    [TestCase("scenario.ch01.m01.first_contact")]
    [TestCase("scenario.ch12.m9.counter_attack_2")]
    [TestCase("scenario.skirmish.desert_base_standard")]
    [TestCase("scenario.operations.o001")]
    [TestCase("scenario.operations.o060")]
    public void ScenarioId_AcceptsCanonicalValues(string value)
    {
        Assert.That(OperationMapIdentityRules.IsValidScenarioId(value), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("scenario.ch01.first_contact")]
    [TestCase("scenario.campaign.m01.first_contact")]
    [TestCase("scenario.ch01.mission01.first_contact")]
    [TestCase("scenario.skirmish.desert.base")]
    [TestCase("scenario.operations.o000")]
    [TestCase("scenario.operations.o061")]
    [TestCase("scenario.operations.o1")]
    [TestCase("scenario.skirmish._desert")]
    public void ScenarioId_RejectsNonCanonicalValues(string value)
    {
        Assert.That(OperationMapIdentityRules.IsValidScenarioId(value), Is.False);
    }

    [Test]
    public void Configs_ValidateOnlyBoundedIdentityAndVersionData()
    {
        OperationMapDefinition map = ScriptableObject.CreateInstance<OperationMapDefinition>();
        ScenarioSetupConfig scenario = ScriptableObject.CreateInstance<ScenarioSetupConfig>();
        try
        {
            Set(map, "operationMapId", "opmap.skirmish.desert_base_01");
            Set(map, "schemaVersion", 1);
            Set(map, "contentVersion", 1);
            Set(scenario, "scenarioId", "scenario.skirmish.desert_base_standard");
            Set(scenario, "operationMapId", map.OperationMapId);

            Assert.That(map.TryValidateIdentity(out string mapError), Is.True, mapError);
            Assert.That(scenario.TryValidateIdentity(out string scenarioError), Is.True, scenarioError);
            Assert.That(map.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);
            Assert.That(scenario.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(map);
            Object.DestroyImmediate(scenario);
        }
    }

    private static void Set(Object target, string propertyName, string value)
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string propertyName, int value)
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
