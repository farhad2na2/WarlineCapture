using System;
using System.Linq;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class OperationMapEntitySceneAddressablesOwnershipTests
{
    public static void RunFocusedValidation()
    {
        var tests = new OperationMapEntitySceneAddressablesOwnershipTests();
        int passed = 0;
        try
        {
            tests.Planner_RequiresDistinctCandidateEntitySceneAndCoreRoles();
            passed++;
            tests.Planner_DoesNotPromoteTransitiveDependenciesToExplicitOwnership();
            passed++;
            tests.CandidateDefinition_ReferencesOnlyEntitySceneRuntimeAssets();
            passed++;
            tests.ProductionDefinition_RemainsStaticSceneChunksWhileCandidatePathIsSeparate();
            passed++;
            tests.DenseCityPlanner_RequiresDistinctPathsAndExactlyFiveCoreRoles();
            passed++;
            tests.DenseCityPlanner_IsGuidIsolatedAndContainsNoProductionReferences();
            passed++;
            tests.DenseCityCandidateDefinition_ReferencesOnlyDenseEntitySceneRuntimeAssets();
            passed++;
            Debug.Log($"[OperationMapEntitySceneAddressablesOwnershipValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[OperationMapEntitySceneAddressablesOwnershipValidation] " +
                $"result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

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
        Assert.That(plan.SharedDependencyCount, Is.EqualTo(0));
        Assert.That(plan.Entries, Has.Count.EqualTo(5));
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
    public void CandidateDefinition_ReferencesOnlyEntitySceneRuntimeAssets()
    {
        OperationMapDefinition candidate = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath);
        Assert.That(candidate, Is.Not.Null);
        Assert.That(candidate.PresentationKind, Is.EqualTo(OperationMapPresentationKind.EntityScene));
        Assert.That(
            candidate.TryValidateLocalContentReferences(out string validationError),
            Is.True,
            validationError);

        AssertReference(
            candidate.SourceSceneReference,
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath);
        AssertReference(
            candidate.MapSurfaceDataReference,
            OperationMapAddressablesLayoutBuilder.MapSurfacePath);
        AssertReference(
            candidate.MinimapRasterReference,
            OperationMapAddressablesLayoutBuilder.MinimapRasterPath);
        Assert.That(
            candidate.NavigationMetadata.AuthoredSubSceneGuid,
            Is.EqualTo(AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath)));

        AssertNoReference(candidate.StaticPresentationManifestReference);
        AssertNoReference(candidate.BuildingPlacementsReference);
        AssertNoReference(candidate.VehiclePlacementsReference);
    }

    [Test]
    public void Planner_DoesNotPromoteTransitiveDependenciesToExplicitOwnership()
    {
        Assert.That(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreatePlan(
                out OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
                out string rejectionReason),
            Is.True,
            rejectionReason);

        Assert.That(plan.SharedDependencyCount, Is.EqualTo(0));
        Assert.That(
            plan.Entries.Any(entry => entry.Role == "shared-dependency"),
            Is.False);
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

    [Test]
    public void DenseCityPlanner_RequiresDistinctPathsAndExactlyFiveCoreRoles()
    {
        Assume.That(
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath),
            Is.Not.Null,
            "Dense-city candidate EntityScene must exist before ownership planning.");

        Assert.That(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreateDenseCityPlan(
                out OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
                out string rejectionReason),
            Is.True,
            rejectionReason);

        Assert.That(plan.OperationMapId, Is.EqualTo("opmap.skirmish.desert_base_01"));
        Assert.That(
            plan.PackLabel,
            Is.EqualTo(OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                .DenseCandidatePackLabel));
        Assert.That(
            plan.AddressPrefix,
            Is.EqualTo(OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                .DenseCandidateAddressPrefix));
        Assert.That(plan.Entries, Has.Count.EqualTo(5));
        Assert.That(plan.Entries.Count(entry => entry.Role == "definition"), Is.EqualTo(1));
        Assert.That(plan.Entries.Count(entry => entry.Role == "source-scene"), Is.EqualTo(1));
        Assert.That(plan.Entries.Count(entry => entry.Role == "entity-scene"), Is.EqualTo(1));
        Assert.That(plan.Entries.Count(entry => entry.Role == "map-surface"), Is.EqualTo(1));
        Assert.That(plan.Entries.Count(entry => entry.Role == "minimap-raster"), Is.EqualTo(1));
        Assert.That(plan.SharedDependencyCount, Is.EqualTo(0));
        Assert.That(plan.Entries.Count(entry => entry.Role == "shared-dependency"), Is.EqualTo(0));
        Assert.That(plan.Entries.Count(entry => entry.Role == "static-manifest"), Is.EqualTo(0));
        Assert.That(plan.Entries.Count(entry => entry.Role == "presentation"), Is.EqualTo(0));
        Assert.That(plan.Entries.Count(entry => entry.Role == "building-placements"), Is.EqualTo(0));
        Assert.That(plan.Entries.Count(entry => entry.Role == "vehicle-placements"), Is.EqualTo(0));

        Assert.That(
            plan.Entries.Single(entry => entry.Role == "definition").AssetPath,
            Is.EqualTo(OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                .DenseCandidateDefinitionPath));
        Assert.That(
            plan.Entries.Single(entry => entry.Role == "source-scene").AssetPath,
            Is.EqualTo(OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                .DenseCandidateRuntimeBindingPath));
        Assert.That(
            plan.Entries.Single(entry => entry.Role == "entity-scene").AssetPath,
            Is.EqualTo(DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath));
        Assert.That(
            plan.Entries.All(entry =>
                entry.Address.StartsWith(plan.AddressPrefix, StringComparison.Ordinal)),
            Is.True);
        Assert.That(
            plan.Entries.All(entry => !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(
                entry.AssetPath))),
            Is.True);
    }

    [Test]
    public void DenseCityPlanner_IsGuidIsolatedAndContainsNoProductionReferences()
    {
        Assert.That(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreateDenseCityPlan(
                out OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
                out string rejectionReason),
            Is.True,
            rejectionReason);

        string acceptedSourceSubSceneGuid = AssetDatabase.AssetPathToGUID(
            OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);
        string acceptedCandidateSubSceneGuid = AssetDatabase.AssetPathToGUID(
            OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath);
        Assert.That(plan.EntitySceneGuid, Is.Not.EqualTo(acceptedSourceSubSceneGuid));
        Assert.That(plan.EntitySceneGuid, Is.Not.EqualTo(acceptedCandidateSubSceneGuid));

        string[] protectedGuids =
        {
            acceptedSourceSubSceneGuid,
            acceptedCandidateSubSceneGuid,
            AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.DefinitionPath),
            AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.SourceScenePath),
            AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.AuthoringScenePath),
            AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.MapSurfacePath),
            AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.MinimapRasterPath),
            AssetDatabase.AssetPathToGUID(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath),
            AssetDatabase.AssetPathToGUID(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                    .CandidateRuntimeBindingPath)
        };
        string denseDefinitionGuid = AssetDatabase.AssetPathToGUID(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                .DenseCandidateDefinitionPath);
        string denseRuntimeBindingGuid = AssetDatabase.AssetPathToGUID(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                .DenseCandidateRuntimeBindingPath);
        if (!string.IsNullOrEmpty(denseDefinitionGuid))
            Assert.That(protectedGuids, Does.Not.Contain(denseDefinitionGuid));
        if (!string.IsNullOrEmpty(denseRuntimeBindingGuid))
            Assert.That(protectedGuids, Does.Not.Contain(denseRuntimeBindingGuid));
        Assert.That(protectedGuids, Does.Not.Contain(plan.EntitySceneGuid));
        if (!string.IsNullOrEmpty(denseDefinitionGuid) &&
            !string.IsNullOrEmpty(denseRuntimeBindingGuid))
        {
            Assert.That(denseDefinitionGuid, Is.Not.EqualTo(denseRuntimeBindingGuid));
        }

        string[] forbiddenPaths =
        {
            OperationMapAddressablesLayoutBuilder.ManifestPath,
            OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath,
            OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath,
            OperationMapAddressablesLayoutBuilder.SourceScenePath,
            OperationMapAddressablesLayoutBuilder.DefinitionPath,
            OperationMapAddressablesLayoutBuilder.AuthoringScenePath,
            OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
            OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath
        };
        Assert.That(
            plan.Entries
                .Select(entry => entry.AssetPath)
                .Intersect(forbiddenPaths, StringComparer.Ordinal),
            Is.Empty);
    }

    [Test]
    public void DenseCityCandidateDefinition_ReferencesOnlyDenseEntitySceneRuntimeAssets()
    {
        OperationMapDefinition candidate = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                .DenseCandidateDefinitionPath);
        Assume.That(
            candidate,
            Is.Not.Null,
            "Dense-city candidate definition is created by the later builder checkpoint.");

        Assert.That(candidate.PresentationKind, Is.EqualTo(OperationMapPresentationKind.EntityScene));
        Assert.That(
            candidate.TryValidateLocalContentReferences(out string validationError),
            Is.True,
            validationError);
        AssertReference(
            candidate.SourceSceneReference,
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                .DenseCandidateRuntimeBindingPath);
        AssertReference(
            candidate.MapSurfaceDataReference,
            OperationMapAddressablesLayoutBuilder.MapSurfacePath);
        AssertReference(
            candidate.MinimapRasterReference,
            OperationMapAddressablesLayoutBuilder.MinimapRasterPath);
        Assert.That(
            candidate.NavigationMetadata.AuthoredSubSceneGuid,
            Is.EqualTo(AssetDatabase.AssetPathToGUID(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath)));
        AssertNoReference(candidate.StaticPresentationManifestReference);
        AssertNoReference(candidate.OptionalHeavyMetadataReference);
        AssertNoReference(candidate.BuildingPlacementsReference);
        AssertNoReference(candidate.VehiclePlacementsReference);
    }

    private static void AssertReference(AssetReference reference, string expectedPath)
    {
        Assert.That(reference, Is.Not.Null);
        Assert.That(reference.AssetGUID, Is.EqualTo(AssetDatabase.AssetPathToGUID(expectedPath)));
        Assert.That(reference.RuntimeKeyIsValid(), Is.True);
    }

    private static void AssertNoReference(AssetReference reference)
    {
        Assert.That(reference == null || string.IsNullOrEmpty(reference.AssetGUID), Is.True);
        Assert.That(reference?.RuntimeKeyIsValid() ?? false, Is.False);
    }
}
