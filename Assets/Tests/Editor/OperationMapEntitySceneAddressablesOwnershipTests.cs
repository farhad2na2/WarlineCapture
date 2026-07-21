using System.Linq;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;

public sealed class OperationMapEntitySceneAddressablesOwnershipTests
{
    [Test]
    public void Planner_RequiresDistinctCandidateEntitySceneAndCoreRoles()
    {
        Assume.That(
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath),
            Is.Not.Null,
            "Candidate entity SubScene must exist before ownership planning.");

        Assert.That(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreatePlan(
                out OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
                out string rejectionReason),
            Is.True,
            rejectionReason);

        Assert.That(plan.OperationMapId, Is.EqualTo("opmap.skirmish.desert_base_01"));
        Assert.That(
            plan.PackLabel,
            Is.EqualTo(OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidatePackLabel));
        Assert.That(plan.EntitySceneGuid, Is.EqualTo(
            AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath)));
        Assert.That(
            plan.EntitySceneGuid,
            Is.Not.EqualTo(
                AssetDatabase.AssetPathToGUID(
                    OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath)));

        Assert.That(plan.Entries.Count(entry => entry.Role == "definition"), Is.EqualTo(1));
        Assert.That(plan.Entries.Count(entry => entry.Role == "source-scene"), Is.EqualTo(1));
        Assert.That(plan.Entries.Count(entry => entry.Role == "entity-scene"), Is.EqualTo(1));
        Assert.That(plan.Entries.Count(entry => entry.Role == "map-surface"), Is.EqualTo(1));
        Assert.That(plan.Entries.Count(entry => entry.Role == "minimap-raster"), Is.EqualTo(1));
        Assert.That(plan.SharedDependencyCount, Is.GreaterThan(0));
        Assert.That(plan.Entries.Count(entry => entry.Role == "static-manifest"), Is.EqualTo(0));
        Assert.That(plan.Entries.Count(entry => entry.Role == "presentation"), Is.EqualTo(0));
        Assert.That(plan.Entries.Count(entry => entry.Role == "building-placements"), Is.EqualTo(0));
        Assert.That(plan.Entries.Count(entry => entry.Role == "vehicle-placements"), Is.EqualTo(0));

        Assert.That(
            plan.Entries.Select(entry => entry.AssetPath),
            Does.Not.Contain(OperationMapAddressablesLayoutBuilder.ManifestPath));
        Assert.That(
            plan.Entries.Select(entry => entry.AssetPath),
            Does.Not.Contain(OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath));
        Assert.That(
            plan.Entries.Select(entry => entry.AssetPath),
            Does.Not.Contain(OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath));
        Assert.That(
            plan.Entries.Select(entry => entry.AssetPath),
            Does.Not.Contain(OperationMapAddressablesLayoutBuilder.DefinitionPath));
        Assert.That(
            plan.Entries.Select(entry => entry.AssetPath),
            Does.Not.Contain(OperationMapAddressablesLayoutBuilder.SourceScenePath));
    }

    [Test]
    public void ProductionDefinition_RemainsStaticSceneChunksWhileCandidatePathIsSeparate()
    {
        OperationMapDefinition production = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            OperationMapAddressablesLayoutBuilder.DefinitionPath);
        Assert.That(production, Is.Not.Null);
        Assert.That(
            production.PresentationKind,
            Is.EqualTo(OperationMapPresentationKind.StaticSceneChunks));

        Assert.That(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
            Is.Not.EqualTo(OperationMapAddressablesLayoutBuilder.DefinitionPath));
        Assert.That(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
            Is.Not.EqualTo(OperationMapAddressablesLayoutBuilder.SourceScenePath));
    }
}
