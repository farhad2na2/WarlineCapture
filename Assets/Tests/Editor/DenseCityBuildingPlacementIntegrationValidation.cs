using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DenseCityBuildingPlacementIntegrationValidation
{
    private const string ScenePath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";

    public static void RunFocusedValidation()
    {
        byte[] sourceHash = ComputeHash(ScenePath);
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RuntimeCityRAndDMapView[] views = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<RuntimeCityRAndDMapView>(true))
                .ToArray();
            Assert.That(views, Has.Length.EqualTo(1));

            RuntimeCityRAndDMapView view = views[0];
            DenseMiddleEasternCityEditModeBuilder.Result result =
                RuntimeCityRAndDEditModeBuilder.BuildDenseMapWide(view);
            ValidateGeneratedFeatureRecordContracts(result.Records);
            Assert.That(view.GeneratedRoot.position, Is.EqualTo(Vector3.zero));
            Assert.That(Quaternion.Angle(view.GeneratedRoot.rotation, Quaternion.identity), Is.LessThan(0.0001f));
            Assert.That(view.GeneratedRoot.lossyScale, Is.EqualTo(Vector3.one));
            Assert.That(result.Buildings, Is.GreaterThan(0));
            Assert.That(result.SemanticBuildings, Is.GreaterThan(result.Buildings));
            Assert.That(result.SemanticBuildingAttachments, Is.GreaterThan(0));
            Assert.That(result.SemanticRoadShoulders, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalWaterExclusions, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalBankTerrains, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalParkTerrains, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalTrees, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalBushes, Is.GreaterThan(0));
            Assert.That(result.SemanticCanalLights, Is.GreaterThan(0));
            Assert.That(result.SemanticCivicBuildings, Is.GreaterThan(0));
            Assert.That(result.SemanticCivicRoads, Is.GreaterThan(0));
            Assert.That(result.SemanticHorizonMountains, Is.GreaterThan(0));
            Assert.That(result.SemanticBoulevardMedianTrees, Is.GreaterThan(0));
            Assert.That(result.SemanticBoulevardMedianLights, Is.GreaterThan(0));
            Assert.That(result.SemanticSidewalkStreetLights, Is.GreaterThan(0));
            Assert.That(result.SemanticGrassPatches, Is.GreaterThan(0));
            Assert.That(result.SemanticMainStreetBushes, Is.GreaterThan(0));
            Assert.That(result.SemanticPowerPoles, Is.GreaterThan(0));
            Assert.That(result.SemanticPowerLines, Is.GreaterThan(0));
            Assert.That(result.SemanticCourtyardWalls, Is.GreaterThan(0));
            Assert.That(result.SemanticCourtyardPillars, Is.GreaterThan(0));
            Assert.That(result.SemanticCourtyardWells, Is.GreaterThan(0));
            Assert.That(result.SemanticCourtyardBushes, Is.GreaterThan(0));
            Assert.That(result.SemanticStreetProps, Is.GreaterThan(0));
            Assert.That(result.SemanticUrbanTrees, Is.GreaterThan(0));
            Assert.That(result.SemanticUrbanRocks, Is.GreaterThan(0));
            Assert.That(result.SemanticCivicFountains, Is.EqualTo(2));
            Assert.That(result.SemanticOpenGroundTerrains, Is.GreaterThan(0));
            Assert.That(result.SemanticSurfaces, Is.GreaterThan(result.SemanticBuildings * 2));
            Debug.Log(
                $"[DenseCityBuildingPlacementIntegrationValidation] result=Passed " +
                $"districtBuildings={result.Buildings} semanticBuildings={result.SemanticBuildings} " +
                $"semanticAttachments={result.SemanticBuildingAttachments} " +
                $"surfaces={result.SemanticSurfaces} presentations={result.SemanticPresentations} " +
                $"roadShoulders={result.SemanticRoadShoulders} " +
                $"canalWaterExclusions={result.SemanticCanalWaterExclusions} " +
                $"canalBankTerrains={result.SemanticCanalBankTerrains} " +
                $"canalParkTerrains={result.SemanticCanalParkTerrains} " +
                $"canalTrees={result.SemanticCanalTrees} canalBushes={result.SemanticCanalBushes} " +
                $"canalLights={result.SemanticCanalLights} " +
                $"civicBuildings={result.SemanticCivicBuildings} " +
                $"civicRoads={result.SemanticCivicRoads} " +
                $"horizonMountains={result.SemanticHorizonMountains} " +
                $"boulevardMedianTrees={result.SemanticBoulevardMedianTrees} " +
                $"boulevardMedianLights={result.SemanticBoulevardMedianLights} " +
                $"sidewalkStreetLights={result.SemanticSidewalkStreetLights} " +
                $"grassPatches={result.SemanticGrassPatches} " +
                $"mainStreetBushes={result.SemanticMainStreetBushes} " +
                $"powerPoles={result.SemanticPowerPoles} " +
                $"powerLines={result.SemanticPowerLines} " +
                $"courtyardWalls={result.SemanticCourtyardWalls} " +
                $"courtyardPillars={result.SemanticCourtyardPillars} " +
                $"courtyardWells={result.SemanticCourtyardWells} " +
                $"courtyardBushes={result.SemanticCourtyardBushes} " +
                $"streetProps={result.SemanticStreetProps} " +
                $"urbanTrees={result.SemanticUrbanTrees} " +
                $"urbanRocks={result.SemanticUrbanRocks} " +
                $"civicFountains={result.SemanticCivicFountains} " +
                $"openGroundTerrains={result.SemanticOpenGroundTerrains}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[DenseCityBuildingPlacementIntegrationValidation] result=Failed");
            ValidationExit.Exit(1);
            return;
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        byte[] finalHash = ComputeHash(ScenePath);
        if (!sourceHash.SequenceEqual(finalHash))
        {
            Debug.LogError("[DenseCityBuildingPlacementIntegrationValidation] accepted scene bytes changed.");
            ValidationExit.Exit(1);
            return;
        }

        ValidationExit.Exit(0);
    }

    private static void ValidateGeneratedFeatureRecordContracts(
        DenseCityGenerationRecordSnapshot records)
    {
        Assert.That(records, Is.Not.Null);
        AssertBuildingKinds(records, "building", "civic-building");

        AssertSurfaceKinds(
            records,
            ("foundation", DenseCitySurfaceRecordKind.Terrain),
            ("blocker", DenseCitySurfaceRecordKind.Blocker),
            ("road", DenseCitySurfaceRecordKind.Road),
            ("road-shoulder", DenseCitySurfaceRecordKind.Terrain),
            ("road-terrain-patch", DenseCitySurfaceRecordKind.Terrain),
            ("canal-water-exclusion", DenseCitySurfaceRecordKind.Blocker),
            ("canal-bridge", DenseCitySurfaceRecordKind.Bridge),
            ("canal-bridge-ramp-a", DenseCitySurfaceRecordKind.Ramp),
            ("canal-bridge-ramp-b", DenseCitySurfaceRecordKind.Ramp),
            ("canal-bank-terrain", DenseCitySurfaceRecordKind.Terrain),
            ("canal-park-terrain", DenseCitySurfaceRecordKind.Terrain),
            ("civic-foundation", DenseCitySurfaceRecordKind.Terrain),
            ("civic-blocker", DenseCitySurfaceRecordKind.Blocker),
            ("civic-road", DenseCitySurfaceRecordKind.Road),
            ("civic-road-terrain-patch", DenseCitySurfaceRecordKind.Terrain),
            ("courtyard-wall", DenseCitySurfaceRecordKind.Blocker),
            ("urban-rock", DenseCitySurfaceRecordKind.Blocker),
            ("open-ground-terrain", DenseCitySurfaceRecordKind.Terrain));

        AssertPresentationKinds(
            records,
            ("building-intact", DenseCityPresentationCategory.GameplayBuildingIntact),
            ("building-destroyed", DenseCityPresentationCategory.GameplayBuildingDestroyed),
            ("civic-building-intact", DenseCityPresentationCategory.GameplayBuildingIntact),
            ("civic-building-destroyed", DenseCityPresentationCategory.GameplayBuildingDestroyed),
            ("building-attachment-intact", DenseCityPresentationCategory.BuildingAttachmentIntact),
            ("road-visual", DenseCityPresentationCategory.Infrastructure),
            ("road-terrain-patch-visual", DenseCityPresentationCategory.Infrastructure),
            ("canal-bed-visual", DenseCityPresentationCategory.Infrastructure),
            ("canal-water-visual", DenseCityPresentationCategory.Infrastructure),
            ("canal-bridge-visual", DenseCityPresentationCategory.Infrastructure),
            ("canal-bank-base-visual", DenseCityPresentationCategory.Infrastructure),
            ("canal-bank-visual", DenseCityPresentationCategory.Infrastructure),
            ("canal-park-base-visual", DenseCityPresentationCategory.Infrastructure),
            ("canal-park-visual", DenseCityPresentationCategory.Infrastructure),
            ("canal-tree-visual", DenseCityPresentationCategory.Vegetation),
            ("canal-bush-visual", DenseCityPresentationCategory.Vegetation),
            ("canal-light-visual", DenseCityPresentationCategory.Infrastructure),
            ("civic-road-visual", DenseCityPresentationCategory.Infrastructure),
            ("civic-road-terrain-patch-visual", DenseCityPresentationCategory.Infrastructure),
            ("civic-fountain-visual", DenseCityPresentationCategory.Prop),
            ("horizon-mountain-visual", DenseCityPresentationCategory.Horizon),
            ("boulevard-median-tree-visual", DenseCityPresentationCategory.Vegetation),
            ("boulevard-median-light-visual", DenseCityPresentationCategory.Infrastructure),
            ("sidewalk-street-light-visual", DenseCityPresentationCategory.Infrastructure),
            ("free-ground-grass-visual", DenseCityPresentationCategory.Vegetation),
            ("main-street-bush-visual", DenseCityPresentationCategory.Vegetation),
            ("power-pole-visual", DenseCityPresentationCategory.Infrastructure),
            ("power-line-visual", DenseCityPresentationCategory.Infrastructure),
            ("courtyard-wall-visual", DenseCityPresentationCategory.Infrastructure),
            ("courtyard-pillar-visual", DenseCityPresentationCategory.Infrastructure),
            ("courtyard-well-visual", DenseCityPresentationCategory.Prop),
            ("courtyard-bush-visual", DenseCityPresentationCategory.Vegetation),
            ("street-prop-visual", DenseCityPresentationCategory.Prop),
            ("urban-tree-visual", DenseCityPresentationCategory.Vegetation),
            ("urban-rock-visual", DenseCityPresentationCategory.Prop),
            ("open-ground-visual", DenseCityPresentationCategory.Infrastructure));
    }

    private static void AssertBuildingKinds(
        DenseCityGenerationRecordSnapshot records,
        params string[] expectedKinds)
    {
        foreach (string expectedKind in expectedKinds)
        {
            Assert.That(
                records.Buildings.Any(record =>
                    string.Equals(record.Identity.Kind, expectedKind, StringComparison.Ordinal)),
                Is.True,
                $"Dense-city feature '{expectedKind}' emitted no building record.");
        }
    }

    private static void AssertSurfaceKinds(
        DenseCityGenerationRecordSnapshot records,
        params (string Kind, DenseCitySurfaceRecordKind SurfaceKind)[] expected)
    {
        foreach ((string kind, DenseCitySurfaceRecordKind surfaceKind) in expected)
        {
            IReadOnlyList<DenseCitySurfaceBakeRecord> matches = records.Surfaces
                .Where(record => string.Equals(record.Identity.Kind, kind, StringComparison.Ordinal))
                .ToArray();
            Assert.That(matches, Is.Not.Empty, $"Dense-city feature '{kind}' emitted no surface record.");
            Assert.That(
                matches.All(record => record.Kind == surfaceKind),
                Is.True,
                $"Dense-city feature '{kind}' did not emit only {surfaceKind} records.");
        }
    }

    private static void AssertPresentationKinds(
        DenseCityGenerationRecordSnapshot records,
        params (string Kind, DenseCityPresentationCategory Category)[] expected)
    {
        foreach ((string kind, DenseCityPresentationCategory category) in expected)
        {
            IReadOnlyList<DenseCityPresentationBakeRecord> matches = records.Presentations
                .Where(record => string.Equals(record.Identity.Kind, kind, StringComparison.Ordinal))
                .ToArray();
            Assert.That(
                matches,
                Is.Not.Empty,
                $"Dense-city feature '{kind}' emitted no presentation record.");
            Assert.That(
                matches.All(record => record.Category == category),
                Is.True,
                $"Dense-city feature '{kind}' did not emit only {category} presentations.");
        }
    }

    private static byte[] ComputeHash(string path)
    {
        using SHA256 sha = SHA256.Create();
        return sha.ComputeHash(File.ReadAllBytes(path));
    }
}
