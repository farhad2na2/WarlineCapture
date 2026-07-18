using System;
using Game.Components;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;

public sealed class CurrentOperationMapScenarioSetupTests
{
    private const string ScenarioPath =
        "Assets/Game/Configs/OperationMaps/Scenarios/ScenarioSetup_Skirmish_DesertBaseStandard.asset";
    private const string MapPath =
        "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";

    [Test]
    public void StandardSkirmish_UsesCurrentPhysicalMapAndTypedDeploymentAnchors()
    {
        ScenarioSetupConfig scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(ScenarioPath);
        OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(MapPath);

        Assert.That(scenario, Is.Not.Null);
        Assert.That(map, Is.Not.Null);
        Assert.That(scenario.TryValidate(out string error), Is.True, error);
        Assert.That(scenario.ScenarioId, Is.EqualTo("scenario.skirmish.desert_base_standard"));
        Assert.That(scenario.OperationMapId, Is.EqualTo(map.OperationMapId));

        ReadOnlySpan<ScenarioAnchorRequirementConfig> requirements = scenario.RequiredAnchors;
        Assert.That(requirements.Length, Is.EqualTo(2));
        for (int index = 0; index < requirements.Length; index++)
        {
            ScenarioAnchorRequirementConfig requirement = requirements[index];
            Assert.That(requirement.Kind, Is.EqualTo(OperationMapAnchorKind.Deployment));
            Assert.That(ContainsMatchingAnchor(map.Anchors, in requirement), Is.True,
                $"Required deployment anchor '{requirement.AnchorId}' is absent from the current map.");
        }
    }

    private static bool ContainsMatchingAnchor(
        ReadOnlySpan<OperationMapAnchorConfig> anchors,
        in ScenarioAnchorRequirementConfig requirement)
    {
        for (int index = 0; index < anchors.Length; index++)
        {
            if (string.Equals(anchors[index].AnchorId, requirement.AnchorId, StringComparison.Ordinal) &&
                anchors[index].Kind == requirement.Kind)
            {
                return true;
            }
        }

        return false;
    }
}
