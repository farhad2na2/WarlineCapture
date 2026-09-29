using System;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Editor.MapVariants;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MapVariantPreparedCandidateTests
{
    public static void RunFrontierPreparationAndContent()
    {
        MapVariantPreparedCandidateBuilder.RequireMediumRuntimeProof();
        // GUI-licensed executeMethod starts with an unsaved Untitled scene;
        // Unity refuses to add a preparation scene until a saved scene is open.
        EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity", OpenSceneMode.Single);
        MapVariantPreparedCandidateBuilder.Build("Frontier");
        MapVariantPreparedCandidateBuilder.Build("Frontier");
        Validate("Frontier");
        Debug.Log("[MapPreparedValidation] result=Passed map=Frontier deterministicRebuild=Passed fullBake=Passed fullPlayable=2048x1024");
        MapVariantCandidateRuntimeBuilder.BuildFrontierContent();
    }

    public static void RunFrontierContent()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity", OpenSceneMode.Single);
        MapVariantCandidateRuntimeBuilder.BuildFrontierContent();
        Debug.Log("[MapFrontierContent] result=Passed scope=IsolatedCandidate");
    }

    public static void RunMediumPreparationAndContent()
    {
        RunMediumPreparation();
        MapVariantCandidateRuntimeBuilder.BuildMediumContent();
    }

    public static void RunIntegrationContent()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity", OpenSceneMode.Single);
        MapVariantCandidateRuntimeBuilder.BuildIntegrationContent();
    }

    public static void RunMediumPreparation()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity", OpenSceneMode.Single);
        foreach (string map in MapVariantPreparedCandidateBuilder.MediumMaps)
        {
            MapVariantPreparedCandidateBuilder.Build(map);
            MapVariantPreparedCandidateBuilder.Build(map);
            Validate(map);
        }
        var roots = new OperationMapEntityPresentationRootAuthoringTests();
        try { roots.TryValidate_AcceptsZeroActiveVehiclesButRejectsNegativeCounts(); roots.TearDown(); roots.TryValidate_AcceptsCompleteDeterministicIdentity(); }
        finally { roots.TearDown(); }
        Debug.Log("[MapPreparedValidation] result=Passed maps=3 deterministicRebuild=Passed fullBake=Passed scope=AuthoringSurfacesAndDestruction");
    }
    public static void RunRefinery() => Validate("RefineryDistrict");
    public static void RunAirfield() => Validate("CityEdgeAirfield");
    public static void RunPort() => Validate("AshLinePort");

    internal static void Validate(string map)
    {
        var output = JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText(
            "Design/MapVariants/Preparation/" + map + "/Candidate/output-manifest.json"));
        Assert.That(output.owners.Count, Is.GreaterThan(100));
        Assert.That(output.duplicateOriginals, Is.Zero);
        Assert.That(output.classifications.Select(p => p.stableKey).Distinct().Count(), Is.EqualTo(output.classifications.Count));
        Assert.That(output.classifications.Any(p => p.category == "StaticObstacle" && p.qualification.Contains("non-destructible")), Is.True);
        Assert.That(output.classifications.Where(p => p.category == "RenderOnlyProp").All(p => !p.movementBlocked && !p.damageEligible), Is.True);
        Assert.That(output.classifications.All(p => !p.targetEligible), Is.True);
        Assert.That(output.zones.All(z => z.entireFootprintInsidePlayable), Is.True);
        Assert.That(output.owners.Select(p => p.stableId).Distinct().Count(), Is.EqualTo(output.owners.Count));
        MapSurfaceDataAsset asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(output.surfacePath);
        Assert.That(asset.TryCreateRuntimeBlobAsset(Allocator.Persistent, out var surface), Is.True);
        using (surface)
        {
            ref var blob = ref surface.Value;
            Assert.That(blob.Dimensions, Is.EqualTo(new int2(2048, 1024)));
            Assert.That(MapSurfaceBlobAccess.SurfaceCount(ref blob), Is.EqualTo(2048 * 1024));
            Assert.That(MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref blob, 0, out var outside), Is.True);
            if (map != "Frontier") Assert.That(outside.MovementMask, Is.EqualTo(MapSurfaceMovementMask.None));
            else
            {
                Assert.That(output.runtimePlayableMin, Is.EqualTo(Vector2.zero));
                Assert.That(output.playableSize, Is.EqualTo(new Vector2(2048, 1024)));
                Assert.That(output.sourceToRuntimeTranslation, Is.EqualTo(new Vector3(-176, 0, -176)));
            }
            foreach (var zone in output.zones.Where(z => z.id is "Anchor_MainBridge" or "Anchor_SouthBridge"))
            {
                int x = Mathf.FloorToInt(zone.runtimeCenter.x), z = Mathf.FloorToInt(zone.runtimeCenter.z);
                Assert.That(MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref blob, z * 2048 + x, out var deck), Is.True);
                Assert.That(deck.Height, Is.EqualTo(.08f).Within(.011f));
                Assert.That(deck.SurfaceType, Is.EqualTo(MapSurfaceType.BridgeDeck));
                Assert.That((deck.MovementMask & MapSurfaceMovementMask.Infantry) != 0, Is.True, "Static scenery blocks bridge anchor.");
            }
        }
        var grid = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(output.gridPath);
        Assert.That(grid.BlockedCells.Length, Is.EqualTo(output.waterAndStaticBlockedCells));
        Scene scene = EditorSceneManager.OpenScene(output.scenePath, OpenSceneMode.Additive);
        using var world = new World("PreparedCandidateValidation-" + map);
        IDisposable blobs = null;
        var fixture = new GameObject("CandidateBakeFixture");
        SceneManager.MoveGameObjectToScene(fixture, scene);
        try
        {
            foreach (var root in scene.GetRootGameObjects().Where(r => r != fixture)) root.transform.SetParent(fixture.transform, true);
            var authorings = fixture.GetComponentsInChildren<OperationMapBuildingAuthoring>(true);
            Assert.That(authorings.Length, Is.EqualTo(output.owners.Count));
            foreach (var authoring in authorings)
            {
                var expected = output.owners.Single(o => o.stableId == authoring.StableId);
                Assert.That(Vector3.Distance(authoring.transform.position, expected.position), Is.LessThan(.001f));
                Assert.That(authoring.TryValidate(out string error), Is.True, error);
            }
            MapVariantPreparationTests.Bake(world, fixture, out blobs);
            var em = world.EntityManager;
            using var query = em.CreateEntityQuery(typeof(OperationMapBuildingIdentity), typeof(OperationMapBuildingPresentation));
            using var owners = query.ToEntityArray(Allocator.Temp);
            Assert.That(owners.Length, Is.EqualTo(output.owners.Count));
            using var gridQuery = em.CreateEntityQuery(typeof(GridConfig));
            Assert.That(gridQuery.CalculateEntityCount(), Is.EqualTo(1));
            using var contractQuery = em.CreateEntityQuery(typeof(OperationMapEntityPresentationReadinessContract));
            Assert.That(contractQuery.CalculateEntityCount(), Is.EqualTo(1));
            var contract = contractQuery.GetSingleton<OperationMapEntityPresentationReadinessContract>();
            Assert.That(contract.ExpectedGameplayVehicleCount, Is.Zero);
            Assert.That(contract.ExpectedGeneratedIdentityCount, Is.EqualTo(output.generatedIdentityCount));
            using var identityQuery = em.CreateEntityQuery(typeof(DenseCityPresentationIdentity));
            Assert.That(identityQuery.CalculateEntityCount(), Is.EqualTo(output.generatedIdentityCount));
            using (var identities = identityQuery.ToComponentDataArray<DenseCityPresentationIdentity>(Allocator.Temp))
                Assert.That(identities.Select(i => i.StableId.ToString()).Distinct().Count(), Is.EqualTo(output.generatedIdentityCount));
            using var surfaceQuery = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            Assert.That(surfaceQuery.CalculateEntityCount(), Is.EqualTo(1));
            MapVariantPreparationTests.Update(world);
            foreach (var owner in owners)
            {
                var expected = output.owners.Single(o => o.stableId == em.GetComponentData<OperationMapBuildingIdentity>(owner).StableId.ToString());
                Assert.That(math.distance(em.GetComponentData<LocalTransform>(owner).Position, (float3)expected.position),
                    Is.LessThan(.01f), "Baked owner pose differs from translated manifest");
                MapVariantPreparationTests.RequireState(em, owner, false);
            }
            for (int i = 0; i < owners.Length; i += Math.Max(1, owners.Length / 5))
            {
                var health = em.GetComponentData<UnitHealth>(owners[i]); health.Current = 0; em.SetComponentData(owners[i], health);
                MapVariantPreparationTests.Update(world);
                MapVariantPreparationTests.RequireState(em, owners[i], true);
                Assert.That(em.HasComponent<StaticGridBlocker>(owners[i]), Is.True);
            }
            Debug.Log($"[MapPreparedBake] result=Passed map={map} owners={owners.Length} entities={em.UniversalQuery.CalculateEntityCount()} " +
                $"surfaceCells=2097152 independentDamage=Passed semanticHash={output.semanticHash} scope=DirectBake");
        }
        finally { blobs?.Dispose(); EditorSceneManager.CloseScene(scene, true); }
    }
}
