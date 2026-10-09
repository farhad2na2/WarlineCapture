using Game.Components;
using Game.Runtime;
using Game.Tactical.Contracts;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingPlacementValidationUtilitySystemHelperTests
{
    private World _world;

    public static void RunSharedSceneryClearanceValidation()
    {
        int failures = 0;
        var cases = new (string Name, Action Run)[] {
            ("ordinary-sidewalk", () => new BuildingPlacementValidationUtilitySystemHelperTests().OrdinaryPlacement_ReservesAuthoredSidewalks()),
            ("skirmish-tree", () => ValidatePackedSceneryClearance(true, DenseCityPresentationSemanticCategory.Vegetation)),
            ("campaign-tree", () => ValidatePackedSceneryClearance(false, DenseCityPresentationSemanticCategory.Vegetation)),
            ("skirmish-prop", () => ValidatePackedSceneryClearance(true, DenseCityPresentationSemanticCategory.Prop)),
            ("campaign-prop", () => ValidatePackedSceneryClearance(false, DenseCityPresentationSemanticCategory.Prop)),
            ("streamed-sidewalk", () => new BuildingPlacementValidationUtilitySystemHelperTests().AuthoredSidewalkCache_TracksLateSourceOwnersAndBufferPublication()),
            ("sidewalk-edges-and-rotation", () => new BuildingPlacementValidationUtilitySystemHelperTests().AuthoredSidewalk_ReservesOverlappingCellEdgesWithoutRotatedEmptyCorners()),
            ("multi-mesh-scenery", () => new BuildingPlacementValidationUtilitySystemHelperTests().AuthoredScenery_ReservesEveryStandingMeshInRange()),
            ("cold-player-placement-boundary", () => new BuildingPlacementValidationUtilitySystemHelperTests().OrdinaryPlayerPlacement_RejectsSceneryWithoutStartingGrantCache()),
            ("combined-native-model-bounds", () => new BuildingPlacementValidationUtilitySystemHelperTests().CombinedNativeModelBounds_IncludeDisabledActiveSourcesAndPlacementOffset())
        };
        foreach (var test in cases)
        {
            try { test.Run(); Debug.Log("[SharedSceneryClearanceValidation] case=" + test.Name + " result=Passed"); }
            catch (Exception ex) { failures++; Debug.LogException(ex); Debug.LogError("[SharedSceneryClearanceValidation] case=" + test.Name + " result=Failed"); }
        }
        Debug.Log("[SharedSceneryClearanceValidation] result=" + (failures == 0 ? "Passed" : "Failed") +
            " cases=" + cases.Length + " failures=" + failures + " nativePlacementAcceptance=Pending");
        if (failures == 0) ValidationExit.Passed(); else ValidationExit.Failed();
    }

    [Test]
    public void OrdinaryPlacement_ReservesAuthoredSidewalks()
    {
        using var world = new World("Ordinary military building sidewalk rejection");
        var em = world.EntityManager;
        em.CreateEntity(typeof(MapSurfaceComponent));
        using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
        var grid = new GridConfig { Width = 32, Height = 32, CellSize = 1 };
        var source = em.CreateEntity();
        em.AddBuffer<OperationMapSidewalkFootprint>(source).Add(new OperationMapSidewalkFootprint {
            WorldToLocal = Unity.Mathematics.float4x4.identity,
            LocalMin = new Unity.Mathematics.float3(5, 0, 5), LocalMax = new Unity.Mathematics.float3(7, 1, 7),
            WorldMin = new Unity.Mathematics.float3(5, 0, 5), WorldMax = new Unity.Mathematics.float3(7, 1, 7)
        });
        var cache = new BuildingPlacementAuthoredRoadCache(); cache.Ensure(em, surfaces, grid);
        var reserved = new bool[grid.Width * grid.Height]; cache.AppendTo(reserved);
        Assert.That(reserved[5 * grid.Width + 5], Is.True, "The ordinary placement prefix must reserve sidewalks, not only road-gate orientation.");
        Assert.That(cache.Overlaps(grid, new Vector2Int(5, 5), Vector2Int.one), Is.True,
            "Final validation must reject a sidewalk even before the prefix is built.");
        Assert.That(reserved[9 * grid.Width + 9], Is.False, "Adjacent free ground remains buildable.");
    }

    [TestCase(true, DenseCityPresentationSemanticCategory.Vegetation)]
    [TestCase(false, DenseCityPresentationSemanticCategory.Vegetation)]
    [TestCase(true, DenseCityPresentationSemanticCategory.Prop)]
    [TestCase(false, DenseCityPresentationSemanticCategory.Prop)]
    public void PackedScenery_ReservesSolidObjectsInEveryMode(bool skirmish, DenseCityPresentationSemanticCategory category)
        => ValidatePackedSceneryClearance(skirmish, category);

    [Test]
    public void CombinedNativeModelBounds_IncludeDisabledActiveSourcesAndPlacementOffset()
    {
        var root=new GameObject("Combined model placement wrapper");
        var mesh=new Mesh {bounds=new Bounds(new Vector3(7,1,3.5f),new Vector3(14,2,7))};
        try
        {
            var visual=new GameObject("Active geometry source");visual.transform.SetParent(root.transform,false);
            visual.transform.localPosition=new Vector3(-7,0,-3.5f);
            visual.AddComponent<MeshFilter>().sharedMesh=mesh;visual.AddComponent<MeshRenderer>().enabled=false;
            Assert.That(BuildingModelBounds.TryMeasure(root,out _),Is.False);
            Assert.That(BuildingModelBounds.TryMeasure(root,out var actual,true),Is.True);
            Assert.That(actual.center,Is.EqualTo(new Vector3(0,1,0)),"Read the placed visual offset instead of treating prefab bounds as wrapper coordinates.");
            Assert.That(actual.size,Is.EqualTo(new Vector3(14,2,7)));
            visual.SetActive(false);
            Assert.That(BuildingModelBounds.TryMeasure(root,out _,true),Is.False,"Inactive/destroyed visual variants are not part of the native intact model.");
        }
        finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);}
    }

    [Test]
    public void OrdinaryPlayerPlacement_RejectsSceneryWithoutStartingGrantCache()
    {
        using var world=new World("Ordinary player placement without a starting grant cache");
        var previous=World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld=world;
        try
        {
            var em=world.EntityManager;
            em.CreateEntity(typeof(MapSurfaceComponent));
            var grid=new GridConfig {Width=32,Height=32,CellSize=1};
            var roadsOwner=em.CreateEntity();var roads=em.AddBuffer<GridRoad>(roadsOwner);roads.ResizeUninitialized(grid.Width*grid.Height);
            for(int i=0;i<roads.Length;i++) roads[i]=default;
            var sidewalk=em.AddBuffer<OperationMapSidewalkFootprint>(em.CreateEntity());
            sidewalk.Add(new OperationMapSidewalkFootprint {
                WorldToLocal=Unity.Mathematics.float4x4.identity,
                LocalMin=new Unity.Mathematics.float3(5,0,5),LocalMax=new Unity.Mathematics.float3(7,1,7),
                WorldMin=new Unity.Mathematics.float3(5,0,5),WorldMax=new Unity.Mathematics.float3(7,1,7)});
            using var builder=new BlobBuilder(Allocator.Temp);
            ref var root=ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
            var prototypes=builder.Allocate(ref root.Prototypes,1);
            prototypes[0]=new OperationMapRenderPrototypeBlob {CombinedLocalBounds=new OperationMapRenderBoundsBlob {
                Center=new Unity.Mathematics.float3(0,4,0),Extents=new Unity.Mathematics.float3(2,4,2)}};
            var placements=builder.Allocate(ref root.Placements,1);
            placements[0]=new OperationMapRenderPlacementBlob {PrototypeIndex=0,SemanticCategory=DenseCityPresentationSemanticCategory.Vegetation,
                WorldMatrix=Unity.Mathematics.float4x4.Translate(new Unity.Mathematics.float3(16,0,16))};
            using var blob=builder.CreateBlobAssetReference<OperationMapRenderDatabaseBlob>(Allocator.Persistent);
            em.SetComponentData(em.CreateEntity(typeof(OperationMapRenderDatabaseComponent)),new OperationMapRenderDatabaseComponent {Blob=blob});
            var source=new BuildingGameplaySourceCompositionSystemHelper();
            bool Validate(Vector2Int origin) => source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(
                source,null,origin,Vector2Int.one,false,grid,em.GetBuffer<GridRoad>(roadsOwner),default,
                (_,__,cell,___,____)=>new RectInt(cell,Vector2Int.one),(_,__)=>false);
            Assert.That(source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.AuthoredBuildings.IsReady,Is.False);
            Assert.That(Validate(new Vector2Int(16,16)),Is.False,"The actual ordinary placement boundary must initialize scenery rejection, without a prior grant/batch.");
            Assert.That(Validate(new Vector2Int(5,5)),Is.False,"The same ordinary boundary must initialize sidewalks.");
            Assert.That(Validate(new Vector2Int(28,28)),Is.True,"A clear site remains legal after cache initialization.");
        }
        finally {World.DefaultGameObjectInjectionWorld=previous;}
    }

    [Test]
    public void AuthoredSidewalk_ReservesOverlappingCellEdgesWithoutRotatedEmptyCorners()
    {
        using var world = new World("Sidewalk edge and rotation clearance"); var em = world.EntityManager;
        em.CreateEntity(typeof(MapSurfaceComponent));
        using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
        var grid = new GridConfig { Width=32, Height=32, CellSize=1 };
        var source = em.CreateEntity(); var buffer = em.AddBuffer<OperationMapSidewalkFootprint>(source);
        buffer.Add(new OperationMapSidewalkFootprint {
            WorldToLocal=Unity.Mathematics.float4x4.identity,
            LocalMin=new Unity.Mathematics.float3(5.85f,0,5), LocalMax=new Unity.Mathematics.float3(6.15f,1,7),
            WorldMin=new Unity.Mathematics.float3(5.85f,0,5), WorldMax=new Unity.Mathematics.float3(6.15f,1,7)
        });
        var cache=new BuildingPlacementAuthoredRoadCache(); cache.Ensure(em,surfaces,grid);
        Assert.That(cache.Overlaps(grid,new Vector2Int(5,5),Vector2Int.one),Is.True,"A curb crossing a cell edge must block placement even outside its centre.");
        Assert.That(cache.Overlaps(grid,new Vector2Int(6,5),Vector2Int.one),Is.True);
        buffer=em.GetBuffer<OperationMapSidewalkFootprint>(source);buffer.Clear();
        var matrix=Unity.Mathematics.float4x4.TRS(new Unity.Mathematics.float3(8,0,8),
            Unity.Mathematics.quaternion.RotateY(Unity.Mathematics.math.PI*.25f),new Unity.Mathematics.float3(1));
        buffer.Add(new OperationMapSidewalkFootprint {
            WorldToLocal=Unity.Mathematics.math.inverse(matrix),
            LocalMin=new Unity.Mathematics.float3(-3,0,-.1f), LocalMax=new Unity.Mathematics.float3(3,1,.1f),
            WorldMin=new Unity.Mathematics.float3(5.8f,0,5.8f), WorldMax=new Unity.Mathematics.float3(10.2f,1,10.2f)
        });
        // Publish a source revision because the existing owner/buffer length is unchanged.
        em.AddComponentData(surfaces.GetSingletonEntity(),new MapSurfaceSceneOverlayRevision {Value=1});
        cache.Ensure(em,surfaces,grid);
        Assert.That(cache.Overlaps(grid,new Vector2Int(7,8),Vector2Int.one),Is.True);
        Assert.That(cache.Overlaps(grid,new Vector2Int(6,6),Vector2Int.one),Is.False,"Empty corners of a rotated sidewalk's AABB remain buildable.");
    }

    [Test]
    public void AuthoredScenery_ReservesEveryStandingMeshInRange()
    {
        using var world=new World("Multi-mesh authored scenery clearance");var em=world.EntityManager;
        var ground=new Mesh {name="SM_Prop_GroundCover",bounds=new Bounds(Vector3.zero,new Vector3(2,.1f,2))};
        var tree=new Mesh {name="SM_Tree_Palm",bounds=new Bounds(new Vector3(0,2,0),new Vector3(2,4,2))};
        var prop=new Mesh {name="SM_Prop_Crate",bounds=new Bounds(new Vector3(6,.25f,0),new Vector3(2,.5f,2))};
        try
        {
            var entity=em.CreateEntity(typeof(Unity.Transforms.LocalToWorld),typeof(Unity.Rendering.MaterialMeshInfo));
            em.SetComponentData(entity,new Unity.Transforms.LocalToWorld {Value=Unity.Mathematics.float4x4.TRS(
                new Unity.Mathematics.float3(10,0,10),Unity.Mathematics.quaternion.identity,new Unity.Mathematics.float3(1,4,1))});
            em.SetComponentData(entity,Unity.Rendering.MaterialMeshInfo.FromMaterialMeshIndexRange(0,3));
            em.AddSharedComponentManaged(entity,new Unity.Rendering.RenderMeshArray(Array.Empty<Material>(),new[]{ground,tree,prop},new[]{
                new Unity.Rendering.MaterialMeshIndex {MeshIndex=0},new Unity.Rendering.MaterialMeshIndex {MeshIndex=1},new Unity.Rendering.MaterialMeshIndex {MeshIndex=2}}));
            var reader=Game.Rendering.RenderBoundsPresentation.Create();
            Assert.That(reader.CountAuthoredBuildingGeometry(em),Is.EqualTo(1));
            var bounds=new NativeList<Bounds>(Allocator.Temp);
            try
            {
                reader.CollectAuthoredBuildingBounds(em,ref bounds);
                Assert.That(bounds.Length,Is.EqualTo(1));
                Assert.That(bounds[0].Contains(new Vector3(10,8,10)),Is.True,"A tree after the first flat mesh must reserve its actual bounds.");
                Assert.That(bounds[0].Contains(new Vector3(16,1,10)),Is.True,"Every standing mesh in a range must be included, using world scale.");
            }
            finally {bounds.Dispose();}
        }
        finally {UnityEngine.Object.DestroyImmediate(ground);UnityEngine.Object.DestroyImmediate(tree);UnityEngine.Object.DestroyImmediate(prop);}
    }

    private static void ValidatePackedSceneryClearance(bool skirmish, DenseCityPresentationSemanticCategory category)
    {
        using var world = new World("Packed solid scenery clearance"); var em = world.EntityManager;
        if (skirmish) em.CreateEntity(typeof(SkirmishMatchState));
        using var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
        var prototypes = builder.Allocate(ref root.Prototypes, 2);
        prototypes[0] = new OperationMapRenderPrototypeBlob { CombinedLocalBounds = new OperationMapRenderBoundsBlob {
            Center = new Unity.Mathematics.float3(0, 4, 0), Extents = new Unity.Mathematics.float3(2, 4, 2) } };
        prototypes[1] = new OperationMapRenderPrototypeBlob { CombinedLocalBounds = new OperationMapRenderBoundsBlob {
            Center = new Unity.Mathematics.float3(0, .05f, 0), Extents = new Unity.Mathematics.float3(2, .05f, 2) } };
        var placements = builder.Allocate(ref root.Placements, 2);
        placements[0] = new OperationMapRenderPlacementBlob { PrototypeIndex = 0, SemanticCategory = category,
            WorldMatrix = Unity.Mathematics.float4x4.Translate(new Unity.Mathematics.float3(10, 0, 10)) };
        placements[1] = new OperationMapRenderPlacementBlob { PrototypeIndex = 1, SemanticCategory = DenseCityPresentationSemanticCategory.Vegetation,
            WorldMatrix = Unity.Mathematics.float4x4.Translate(new Unity.Mathematics.float3(24, 0, 24)) };
        using var blob = builder.CreateBlobAssetReference<OperationMapRenderDatabaseBlob>(Allocator.Persistent);
        var source = em.CreateEntity(typeof(OperationMapRenderDatabaseComponent));
        em.SetComponentData(source, new OperationMapRenderDatabaseComponent { Blob = blob });
        var grid = new GridConfig { Width = 32, Height = 32, CellSize = 1 };
        var cache = new BuildingPlacementAuthoredBuildingCache(); cache.Ensure(em, grid);
        Assert.That(cache.IsReady, Is.True, "Packed scenery must protect Campaign and Operations as well as Skirmish.");
        Assert.That(cache.Overlaps(grid, new Vector2Int(10, 10), Vector2Int.one), Is.True,
            "Military foundations cannot contain an existing tree or solid prop.");
        Assert.That(cache.Overlaps(grid, new Vector2Int(24, 24), Vector2Int.one), Is.False,
            "Ground cover does not reserve a full building site.");
        Assert.That(cache.Overlaps(grid, new Vector2Int(28, 28), Vector2Int.one), Is.False);
    }

    public static void RunStreamedSidewalkCacheValidation()
    {
        try
        {
            new BuildingPlacementValidationUtilitySystemHelperTests().AuthoredSidewalkCache_TracksLateSourceOwnersAndBufferPublication();
            Debug.Log("[StreamedSidewalkCacheValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[StreamedSidewalkCacheValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void AuthoredSidewalkCache_TracksLateSourceOwnersAndBufferPublication()
    {
        using var world = new World("Delayed packed sidewalk source");
        var em = world.EntityManager;
        em.CreateEntity(typeof(MapSurfaceComponent));
        using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
        var grid = new GridConfig { Width = 32, Height = 32, CellSize = 1 };
        var cache = new BuildingPlacementAuthoredRoadCache();
        cache.Ensure(em, surfaces, grid);
        Assert.That(cache.GetSidewalks()[5 * 32 + 5], Is.False);
        var source = em.CreateEntity();
        em.AddBuffer<OperationMapSidewalkFootprint>(source);
        cache.Ensure(em, surfaces, grid);
        em.GetBuffer<OperationMapSidewalkFootprint>(source).Add(new OperationMapSidewalkFootprint
        {
            WorldToLocal = Unity.Mathematics.float4x4.identity,
            LocalMin = new Unity.Mathematics.float3(5, 0, 5),
            LocalMax = new Unity.Mathematics.float3(7, 1, 7),
            WorldMin = new Unity.Mathematics.float3(5, 0, 5),
            WorldMax = new Unity.Mathematics.float3(7, 1, 7)
        });
        cache.Ensure(em, surfaces, grid);
        Assert.That(cache.GetSidewalks()[5 * 32 + 5], Is.True, "Late baked sidewalks must block initial placement.");
        Assert.That(cache.GetSidewalks()[7 * 32 + 7], Is.False);
        em.DestroyEntity(source);
        cache.Ensure(em, surfaces, grid);
        Assert.That(cache.GetSidewalks()[5 * 32 + 5], Is.False, "Removed source must not leave a stale reservation.");
    }

    public static void RunPlacementCommandRequestValidation()
    {
        try
        {
            var tests = new BuildingPlacementValidationUtilitySystemHelperTests();
            tests.BuildingUiPlacementCommandRequest_RejectsMissingSession();
            tests.BuildingUiPlacementCommandRequest_ConfirmRejectsMissingActivePlacement();
            tests.BuildingUiPlacementCommandRequest_ConfirmRejectsBlockedPlacement();
            tests.BuildingUiPlacementCommandRequest_ConfirmRejectsInvalidPlacement();
            tests.BuildingUiPlacementCommandRequest_ConfirmRejectsNotEnoughMoney();
            tests.BuildingUiPlacementCommandRequest_ConfirmWritesAcceptedResult();
            tests.BuildingUiPlacementCommandRequest_CancelWritesAcceptedResult();
            tests.BuildingUiPlacementCommandRequest_RotateWritesAcceptedResult();
            tests.BuildingUiPlacementCommandRequest_ExitBuildModeHonorsClearSelectionFlag();
            tests.BuildingUiPlacementCommandRequest_BeginConfiguredPlacementWritesAcceptedResult();
            tests.BuildingUiPlacementCommandRequest_BeginConfiguredPlacementRejectsMissingConfig();
            tests.BuildingPlacementCommandResultMapper_MapsConfirmFailureResultCodes();
            tests.BuildingPlacementCommandResultMapper_MapsTacticalReasonCodes();
            tests.BuildingUiPlacementCommandEntityCache_ReusesWarmPositiveAndNegativeLookupsWithoutManagedAllocation();
            tests.BuildingUiPlacementCommandEntityCache_RebindsWhenWorldChanges();
            tests.BuildingUiPlacementCommandEntityCache_RecoversDestroyedEntityAndRepairsBuffers();
            tests.BuildingUiPlacementEconomyTransactionId_SurvivesQueueEntityRecreation();
            tests.BuildingPlacementInputRuntimeTick_ProcessesQueuedPlacementCommandBeforeCameraGate();
            tests.BuildingPlacementInputScratchLists_ReuseImmediatePreviewStorageWithoutSharingOwnedResults();
            Debug.Log("[BuildingPlacementCommandRequestValidation] result=Passed tests=19");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingPlacementCommandRequestValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunLiveOccupancyValidation()
    {
        try
        {
            var tests = new BuildingPlacementValidationUtilitySystemHelperTests();
            tests.PlacementRectValidation_RejectsRoadCellsAndRuntimeBuildingOverlap();
            tests.PlacementRectValidation_RejectsLiveInfantryAndVehicleFootprints();
            Debug.Log("[BuildingPlacementLiveOccupancyValidation] result=Passed tests=2");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingPlacementLiveOccupancyValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void BuildingPlacementCommandResultMapper_MapsConfirmFailureResultCodes()
    {
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.Rejected,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.None));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.MissingActivePlacement,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.MissingActivePlacement));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.BlockedPlacement,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.BlockedPlacement));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.InvalidPlacement,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InvalidPlacement));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.NotEnoughMoney,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientCredits));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.InsufficientMaterials,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientMaterials));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.InsufficientCreditsAndMaterials,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InsufficientCreditsAndMaterials));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.DuplicateTransaction,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.DuplicateTransaction));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.TransactionRejected,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.TransactionRejected));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.RegistrationFailed,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.RegistrationFailed));
        Assert.AreEqual(
            BuildingUiPlacementCommandResultElement.Rejected,
            BuildingPlacementCommandResultMapper.ToConfirmFailureResultCode(
                (BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason)int.MaxValue));
    }

    [Test]
    public void BuildingPlacementCommandResultMapper_MapsTacticalReasonCodes()
    {
        Assert.AreEqual(
            TacticalCommandReasonCode.None,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.Completed));
        Assert.AreEqual(
            TacticalCommandReasonCode.TargetBlocked,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.BlockedPlacement));
        Assert.AreEqual(
            TacticalCommandReasonCode.TargetUnreachable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.InvalidPlacement));
        Assert.AreEqual(
            TacticalCommandReasonCode.InsufficientResources,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.NotEnoughMoney));
        Assert.AreEqual(
            TacticalCommandReasonCode.InsufficientResources,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.InsufficientMaterials));
        Assert.AreEqual(
            TacticalCommandReasonCode.InsufficientResources,
            BuildingPlacementCommandResultMapper.ToReasonCode(
                BuildingUiPlacementCommandResultElement.InsufficientCreditsAndMaterials));
        Assert.AreEqual(
            TacticalCommandReasonCode.BuildUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.MissingActivePlacement));
        Assert.AreEqual(
            TacticalCommandReasonCode.BuildUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.MissingConfig));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.MissingSession));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.Rejected));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.DuplicateTransaction));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.RegistrationFailed));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(BuildingUiPlacementCommandResultElement.TransactionRejected));
        Assert.AreEqual(
            TacticalCommandReasonCode.CommandUnavailable,
            BuildingPlacementCommandResultMapper.ToReasonCode(byte.MaxValue));
    }

    [Test]
    public void BuildingUiPlacementCommandEntityCache_ReusesWarmPositiveAndNegativeLookupsWithoutManagedAllocation()
    {
        using World world = new("BuildingUiPlacementCommandEntityCacheAllocationTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();

        commandSystem.ProcessPendingUiPlacementCommandsIfPresent(world.EntityManager, default);
        long idleAllocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 300; i++)
        {
            commandSystem.ProcessPendingUiPlacementCommandsIfPresent(world.EntityManager, default);
            commandSystem.HasPendingUiPlacementCommands(world.EntityManager);
        }
        long idleAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - idleAllocationStart;

        commandSystem.TryGetUiPlacementCommandResult(world.EntityManager, -1, out _);
        long queueAllocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 300; i++)
            commandSystem.TryGetUiPlacementCommandResult(world.EntityManager, -1, out _);
        long queueAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - queueAllocationStart;

        Assert.AreEqual(0L, idleAllocatedBytes, "Warm idle placement command polling must reuse the negative queue lookup.");
        Assert.AreEqual(0L, queueAllocatedBytes, "Warm placement command reads must reuse the queue entity.");
    }

    [Test]
    public void BuildingUiPlacementCommandEntityCache_RebindsWhenWorldChanges()
    {
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        using (World firstWorld = new("BuildingUiPlacementCommandEntityCacheRebind.First"))
            commandSystem.TryGetUiPlacementCommandResult(firstWorld.EntityManager, -1, out _);

        using (World secondWorld = new("BuildingUiPlacementCommandEntityCacheRebind.Second"))
        {
            commandSystem.TryGetUiPlacementCommandResult(secondWorld.EntityManager, -1, out _);
            Assert.AreEqual(1, CountEntitiesWith<BuildingUiPlacementCommandQueueComponent>(secondWorld.EntityManager));
        }
    }

    [Test]
    public void BuildingUiPlacementCommandEntityCache_RecoversDestroyedEntityAndRepairsBuffers()
    {
        using World world = new("BuildingUiPlacementCommandEntityCacheRecoveryTest");
        EntityManager em = world.EntityManager;
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();

        commandSystem.TryGetUiPlacementCommandResult(em, -1, out _);
        Entity cachedEntity = GetSingletonEntity<BuildingUiPlacementCommandQueueComponent>(em);
        em.DestroyEntity(cachedEntity);
        Entity adoptedEntity = em.CreateEntity(typeof(BuildingUiPlacementCommandQueueComponent));

        commandSystem.TryGetUiPlacementCommandResult(em, -1, out _);

        Assert.IsTrue(em.HasBuffer<BuildingUiPlacementCommandRequestElement>(adoptedEntity));
        Assert.IsTrue(em.HasBuffer<BuildingUiPlacementCommandResultElement>(adoptedEntity));
    }

    [Test]
    public void BuildingUiPlacementEconomyTransactionId_SurvivesQueueEntityRecreation()
    {
        using World world = new(nameof(BuildingUiPlacementEconomyTransactionId_SurvivesQueueEntityRecreation));
        EntityManager em = world.EntityManager;
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();

        commandSystem.EnqueueConfirmBuildingPlacement(em);
        Entity firstQueue = GetSingletonEntity<BuildingUiPlacementCommandQueueComponent>(em);
        int firstTransactionId = em.GetBuffer<BuildingUiPlacementCommandRequestElement>(firstQueue)[0]
            .EconomyTransactionId;
        em.DestroyEntity(firstQueue);

        commandSystem.EnqueueConfirmBuildingPlacement(em);
        Entity replacementQueue = GetSingletonEntity<BuildingUiPlacementCommandQueueComponent>(em);
        int replacementTransactionId = em.GetBuffer<BuildingUiPlacementCommandRequestElement>(replacementQueue)[0]
            .EconomyTransactionId;

        Assert.Greater(firstTransactionId, 0);
        Assert.Greater(replacementTransactionId, firstTransactionId);
    }

    private static int CountEntitiesWith<T>(EntityManager entityManager)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.CalculateEntityCount();
    }

    private static Entity GetSingletonEntity<T>(EntityManager entityManager)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.GetSingletonEntity();
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
        _world = null;
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_RejectsMissingSession()
    {
        using World world = new("BuildingUiPlacementCommandMissingSessionTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = default;

        int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiPlacementCommandResultElement result));
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandRequestElement.KindConfirmPlacement, result.RequestKind);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.MissingSession, result.ResultCode);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmRejectsMissingActivePlacement()
    {
        using World world = new("BuildingUiPlacementCommandMissingActivePlacementTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreatePlacementCommandContext(
            world.EntityManager,
            new BuildingPlacementSessionCompositionSystemHelper());

        int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        AssertPlacementResult(
            world.EntityManager,
            commandSystem,
            requestId,
            BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
            accepted: false,
            BuildingUiPlacementCommandResultElement.MissingActivePlacement);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmRejectsBlockedPlacement()
    {
        using World world = new("BuildingUiPlacementCommandBlockedPlacementTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            world.EntityManager,
            out _,
            out GameObject prefab,
            out GameObject root,
            placementIsValid: false);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1)
                });

            int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                accepted: false,
                BuildingUiPlacementCommandResultElement.BlockedPlacement);
            Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiPlacementCommandResultElement typedResult));
            Assert.AreEqual((int)TacticalCommandReasonCode.TargetBlocked, typedResult.ReasonCode);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmRejectsInvalidPlacement()
    {
        using World world = new("BuildingUiPlacementCommandInvalidPlacementTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            world.EntityManager,
            out _,
            out GameObject prefab,
            out GameObject root,
            validateConfirm: _ => false);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1)
                });

            int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                accepted: false,
                BuildingUiPlacementCommandResultElement.InvalidPlacement);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmRejectsNotEnoughMoney()
    {
        using World world = new("BuildingUiPlacementCommandNotEnoughMoneyTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            world.EntityManager,
            out _,
            out GameObject prefab,
            out GameObject root,
            trySpendCost: _ => false);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1),
                    CreditsCost = 50
                });

            int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                accepted: false,
                BuildingUiPlacementCommandResultElement.NotEnoughMoney);
            Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiPlacementCommandResultElement typedResult));
            Assert.AreEqual((int)TacticalCommandReasonCode.InsufficientResources, typedResult.ReasonCode);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ConfirmWritesAcceptedResult()
    {
        using World world = new("BuildingUiPlacementCommandConfirmTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        int commitCount = 0;
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            world.EntityManager,
            out BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
            out GameObject prefab,
            out GameObject root,
            commitPlacement: _ => commitCount++);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1)
                });

            int requestId = commandSystem.EnqueueConfirmBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
                accepted: true,
                BuildingUiPlacementCommandResultElement.Completed);
            Assert.AreEqual(1, commitCount);
            Assert.IsFalse(lifecycleSystem.HasPendingBuildingPlacement);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_CancelWritesAcceptedResult()
    {
        using World world = new("BuildingUiPlacementCommandCancelTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        bool commandModeCleared = false;
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreatePlacementCommandContext(
            world.EntityManager,
            new BuildingPlacementSessionCompositionSystemHelper(),
            clearCommandMode: () => commandModeCleared = true);

        int requestId = commandSystem.EnqueueCancelBuildingPlacement(world.EntityManager);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiPlacementCommandResultElement result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandRequestElement.KindCancelPlacement, result.RequestKind);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, result.ResultCode);
        Assert.IsTrue(commandModeCleared);

        using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingUiPlacementCommandQueueComponent>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiPlacementCommandRequestElement>(queueEntity).Length);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_RotateWritesAcceptedResult()
    {
        using World world = new("BuildingUiPlacementCommandRotateTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        int updateCount = 0;
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            world.EntityManager,
            out BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
            out GameObject prefab,
            out GameObject root,
            updatePlacement: _ => updateCount++);

        try
        {
            context.SessionSystem.BeginPlacement(
                context.SessionContext,
                new BuildingDefinition
                {
                    Prefab = prefab,
                    FootprintCells = new Vector2Int(1, 1)
                });

            Assert.IsNotNull(lifecycleSystem.ActivePlacement);
            Assert.IsFalse(lifecycleSystem.ActivePlacement.AutoRotateVertical);

            int requestId = commandSystem.EnqueueRotateBuildingPlacement(world.EntityManager);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiPlacementCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiPlacementCommandRequestElement.KindRotatePlacement, result.RequestKind);
            Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, result.ResultCode);
            Assert.IsNotNull(lifecycleSystem.ActivePlacement);
            Assert.IsTrue(lifecycleSystem.ActivePlacement.AutoRotateVertical);
            Assert.AreEqual(2, updateCount);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_ExitBuildModeHonorsClearSelectionFlag()
    {
        using World world = new("BuildingUiPlacementCommandExitBuildModeTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        int clearSelectionCount = 0;
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreatePlacementCommandContext(
            world.EntityManager,
            new BuildingPlacementSessionCompositionSystemHelper(),
            clearSelectedBuilding: _ => clearSelectionCount++);

        int preservedSelectionRequestId = commandSystem.EnqueueExitBuildMode(
            world.EntityManager,
            clearBuildingSelection: false);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            preservedSelectionRequestId,
            out BuildingUiPlacementCommandResultElement preservedSelectionResult));
        Assert.AreEqual(1, preservedSelectionResult.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, preservedSelectionResult.ResultCode);
        Assert.AreEqual(0, clearSelectionCount);

        int clearSelectionRequestId = commandSystem.EnqueueExitBuildMode(
            world.EntityManager,
            clearBuildingSelection: true);
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            world.EntityManager,
            clearSelectionRequestId,
            out BuildingUiPlacementCommandResultElement clearSelectionResult));
        Assert.AreEqual(1, clearSelectionResult.Accepted);
        Assert.AreEqual(BuildingUiPlacementCommandResultElement.Completed, clearSelectionResult.ResultCode);
        Assert.AreEqual(1, clearSelectionCount);
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_BeginConfiguredPlacementWritesAcceptedResult()
    {
        using World world = new("BuildingUiPlacementCommandBeginConfiguredTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        var definitionSystem = new BuildingDefinitionPrefabSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreateActivePlacementCommandContext(
            world.EntityManager,
            out BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
            out GameObject prefab,
            out GameObject root,
            definitionSystem: definitionSystem);

        try
        {
            definitionSystem.RebuildSpawnablesLookup(new List<GameObject> { prefab }, null);
            definitionSystem.RebuildConfiguredSpawnableDefinitions(null, UnityEngine.Object.DestroyImmediate);

            int requestId = commandSystem.EnqueueBeginConfiguredPlacement(world.EntityManager, prefab.name);
            commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

            AssertPlacementResult(
                world.EntityManager,
                commandSystem,
                requestId,
                BuildingUiPlacementCommandRequestElement.KindBeginConfiguredPlacement,
                accepted: true,
                BuildingUiPlacementCommandResultElement.Completed);
            Assert.IsTrue(lifecycleSystem.HasPendingBuildingPlacement);
            Assert.AreEqual(prefab, lifecycleSystem.ActivePlacement.Definition.Prefab);
        }
        finally
        {
            definitionSystem.ClearConfiguredSpawnableDefinitions(UnityEngine.Object.DestroyImmediate);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void BuildingUiPlacementCommandRequest_BeginConfiguredPlacementRejectsMissingConfig()
    {
        using World world = new("BuildingUiPlacementCommandBeginConfiguredMissingConfigTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context = CreatePlacementCommandContext(
            world.EntityManager,
            new BuildingPlacementSessionCompositionSystemHelper());

        int requestId = commandSystem.EnqueueBeginConfiguredPlacement(world.EntityManager, "missing-building");
        commandSystem.ProcessPendingUiPlacementCommands(world.EntityManager, context);

        AssertPlacementResult(
            world.EntityManager,
            commandSystem,
            requestId,
            BuildingUiPlacementCommandRequestElement.KindBeginConfiguredPlacement,
            accepted: false,
            BuildingUiPlacementCommandResultElement.MissingConfig);

        using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingUiPlacementCommandQueueComponent>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiPlacementCommandRequestElement>(queueEntity).Length);
    }

    [Test]
    public void BuildingPlacementInputRuntimeTick_ProcessesQueuedPlacementCommandBeforeCameraGate()
    {
        using World world = new("BuildingPlacementInputTickQueuedPlacementCommandTest");
        var commandSystem = new BuildingPlacementCommandRequestCompositionSystemHelper();
        BuildingPlacementCommandRequestCompositionSystemHelper.Context commandContext = CreatePlacementCommandContext(
            world.EntityManager,
            new BuildingPlacementSessionCompositionSystemHelper());
        int requestId = commandSystem.EnqueueCancelBuildingPlacement(world.EntityManager);

        var tickSystem = new BuildingPlacementInputRuntimeTickUiSystemHelper();
        BuildingPlacementInputRuntimeTickUiSystemHelper.Context tickContext = new(
            getWorldCamera: () => null,
            getActivePlacement: () => null,
            placementInputSystem: null,
            activePlacementPointerContext: default,
            isPlayRequested: () => false,
            isBuildModeActive: () => false,
            placementPreviewSystem: null,
            hasActiveBuilding: () => false,
            runtimeGameplayStateSystem: new RuntimeGameplayStateSystem(world.EntityManager),
            getMainMenu: () => null,
            selectionClickSystem: null,
            selectionClickContext: default,
            shouldBlockBuildingSelectionClick: () => false,
            clickDragThresholdPixels: 8f,
            processPendingPlacementCommands: () => commandSystem.ProcessPendingUiPlacementCommandsIfPresent(
                world.EntityManager,
                commandContext));

        tickSystem.Update(tickContext);

        AssertPlacementResult(
            world.EntityManager,
            commandSystem,
            requestId,
            BuildingUiPlacementCommandRequestElement.KindCancelPlacement,
            accepted: true,
            BuildingUiPlacementCommandResultElement.Completed);
    }

    [Test]
    public void PlacementRectValidation_RejectsRoadCellsAndRuntimeBuildingOverlap()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        roads[GridUtils.CellToIndex(new Unity.Mathematics.int2(1, 1), grid.Width)] = new GridRoad { Value = 1 };
        DynamicBlockerComponent blockerData = default;

        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
            new RectInt(1, 1, 1, 1),
            grid,
            roads,
            blockerData,
            false,
            null,
            0,
            0,
            null,
            null,
            null));

        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
            new RectInt(2, 2, 1, 1),
            grid,
            roads,
            blockerData,
            false,
            null,
            0,
            0,
            null,
            null,
            rect => rect.position == new Vector2Int(2, 2)));
    }

    [Test]
    public void PlacementRectValidation_RejectsLiveInfantryAndVehicleFootprints()
    {
        using var world = new World("BuildingPlacementLiveOccupancyTests");
        EntityManager entityManager = world.EntityManager;
        Entity infantry = entityManager.CreateEntity(typeof(UnitGrid), typeof(UnitFootprint));
        entityManager.SetComponentData(infantry, new UnitGrid { Cell = new Unity.Mathematics.int2(2, 2) });
        entityManager.SetComponentData(infantry, new UnitFootprint { Size = new Unity.Mathematics.int2(1, 1) });

        Entity vehicle = entityManager.CreateEntity(typeof(UnitGrid), typeof(UnitFootprint), typeof(UnitMovementBehavior));
        entityManager.SetComponentData(vehicle, new UnitGrid { Cell = new Unity.Mathematics.int2(6, 6) });
        entityManager.SetComponentData(vehicle, new UnitFootprint { Size = new Unity.Mathematics.int2(3, 2) });
        entityManager.SetComponentData(vehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });

        Entity runtimeBuildingProxy = entityManager.CreateEntity(
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(RuntimeBuildingCombatTag));
        entityManager.SetComponentData(runtimeBuildingProxy, new UnitGrid { Cell = new Unity.Mathematics.int2(9, 9) });
        entityManager.SetComponentData(runtimeBuildingProxy, new UnitFootprint { Size = new Unity.Mathematics.int2(3, 3) });

        Entity operationMapBuilding = entityManager.CreateEntity(
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(RuntimeBuildingCombatTag),
            typeof(RuntimeBuildingCombatInfo),
            typeof(OperationMapBuildingComponent),
            typeof(StaticGridBlocker));
        entityManager.SetComponentData(operationMapBuilding, new UnitGrid { Cell = new Unity.Mathematics.int2(30, 30) });
        entityManager.SetComponentData(operationMapBuilding, new UnitFootprint { Size = new Unity.Mathematics.int2(4, 3) });
        entityManager.SetComponentData(operationMapBuilding, new RuntimeBuildingCombatInfo
        {
            OriginCell = new Unity.Mathematics.int2(16, 16),
            FootprintCells = new Unity.Mathematics.int2(4, 3)
        });

        Entity staticBlocker = entityManager.CreateEntity(
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(StaticGridBlocker));
        entityManager.SetComponentData(staticBlocker, new UnitGrid { Cell = new Unity.Mathematics.int2(12, 12) });
        entityManager.SetComponentData(staticBlocker, new UnitFootprint { Size = new Unity.Mathematics.int2(2, 2) });

        EntityQuery liveFootprints = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>());

        Assert.IsTrue(BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(
            entityManager,
            liveFootprints,
            new RectInt(2, 2, 1, 1)), "Infantry must block building placement.");
        Assert.IsTrue(BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(
            entityManager,
            liveFootprints,
            new RectInt(5, 5, 2, 2)), "The full vehicle footprint must block building placement.");
        Assert.IsFalse(BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(
            entityManager,
            liveFootprints,
            new RectInt(8, 8, 3, 3)), "Runtime buildings are checked by the canonical runtime-building owner, not twice through unit occupancy.");
        Assert.IsFalse(BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(
            entityManager,
            liveFootprints,
            new RectInt(12, 12, 2, 2)), "Static blockers remain owned by the static blocker validation path.");
        Assert.IsTrue(BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(
            entityManager,
            liveFootprints,
            new RectInt(16, 16, 4, 3)), "A baked operation-map building carrying both runtime-building and static-blocker tags must block building placement through its canonical combat origin even when its compatibility UnitGrid center is stale.");
        Assert.IsFalse(BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(
            entityManager,
            liveFootprints,
            new RectInt(24, 24, 1, 1)), "An empty footprint must remain valid for the live occupancy layer.");
    }

    [Test]
    public void PlacementRectValidation_AllowsRuntimeBlockerCellsButRejectsStaticBlockers()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        int blockedIndex = GridUtils.CellToIndex(new Unity.Mathematics.int2(1, 2), grid.Width);
        var blocked = new NativeBitArray(16, Allocator.Persistent);
        blocked.Set(blockedIndex, true);
        DynamicBlockerComponent blockerData = new() { GridSize = 16, Blocked = blocked };

        try
        {
            Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
                new RectInt(1, 2, 1, 1),
                grid,
                roads,
                blockerData,
                false,
                null,
                0,
                0,
                null,
                null,
                null));

            Assert.IsTrue(BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
                new RectInt(1, 2, 1, 1),
                grid,
                roads,
                blockerData,
                false,
                null,
                0,
                0,
                (x, y, _, _) => x == 1 && y == 2,
                null,
                null));
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
        }
    }

    [Test]
    public void InvalidPrefix_DetectsRoadFootprintMaskAndOutOfBounds()
    {
        CreateRoadBuffer(4, 4, out GridConfig grid, out DynamicBuffer<GridRoad> roads);
        bool[] roadFootprintMask = new bool[16];
        roadFootprintMask[GridUtils.CellToIndex(new Unity.Mathematics.int2(2, 1), grid.Width)] = true;
        int[] prefix = null;

        BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(
            grid,
            roads,
            default,
            roadFootprintMask,
            null,
            ref prefix,
            out int prefixWidth,
            out int prefixHeight,
            out bool hasPrefix);

        Assert.IsTrue(hasPrefix);
        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(0, 0), new Vector2Int(1, 1)));
        Assert.IsTrue(BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(2, 1), new Vector2Int(1, 1)));
        Assert.IsTrue(BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(prefix, prefixWidth, prefixHeight, new Vector2Int(4, 4), new Vector2Int(1, 1)));
    }

    [Test]
    public void WallSegmentConflict_OnlyRejectsOverlappingSameAxisSegments()
    {
        Assert.IsTrue(BuildingPlacementValidationUtilitySystemHelper.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(1, 3),
            new Vector2Int(1, 4),
            true));

        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(1, 3),
            new Vector2Int(4, 1),
            false));

        Assert.IsFalse(BuildingPlacementValidationUtilitySystemHelper.DoWallSegmentsConflict(
            new Vector2Int(1, 1),
            new Vector2Int(1, 4),
            true,
            new Vector2Int(3, 1),
            new Vector2Int(1, 4),
            true));
    }

    [Test]
    public void BuildingPlacementInputScratchLists_ReuseImmediatePreviewStorageWithoutSharingOwnedResults()
    {
        var input = new BuildingPlacementInputUiSystemHelper();
        var placement = new ScratchPlacementState
        {
            Definition = new BuildingDefinition { FootprintCells = new Vector2Int(1, 1) },
            DragStartOriginCell = new Vector2Int(1, 1),
            DragCurrentOriginCell = new Vector2Int(4, 1)
        };

        IReadOnlyList<Vector2Int> firstScratch = input.BuildWallPlacementOriginsScratch(placement, UnitFootprint);
        Assert.AreEqual(4, firstScratch.Count);
        Assert.AreEqual(new Vector2Int(4, 1), firstScratch[3]);

        List<Vector2Int> owned = input.BuildWallPlacementOrigins(placement, UnitFootprint);
        Assert.AreEqual(firstScratch.Count, owned.Count);
        Assert.AreNotSame(firstScratch, owned);

        placement.DragCurrentOriginCell = new Vector2Int(2, 1);
        IReadOnlyList<Vector2Int> secondScratch = input.BuildWallPlacementOriginsScratch(placement, UnitFootprint);
        Assert.AreSame(firstScratch, secondScratch);
        Assert.AreEqual(2, secondScratch.Count);
        Assert.AreEqual(4, owned.Count);
        Assert.AreEqual(new Vector2Int(4, 1), owned[3]);

        IReadOnlyList<BuildingPlacementInputUiSystemHelper.WallRun> firstRuns = input.BuildFinalWallRunsScratch(placement, UnitFootprint);
        IReadOnlyList<BuildingPlacementInputUiSystemHelper.WallRun> secondRuns = input.BuildFinalWallRunsScratch(placement, UnitFootprint);
        Assert.AreSame(firstRuns, secondRuns);
        Assert.AreEqual(1, secondRuns.Count);
        Assert.AreEqual(secondScratch.Count, secondRuns[0].Origins.Count);
        Assert.AreEqual(secondScratch[1], secondRuns[0].Origins[1]);
    }

    private static Vector2Int UnitFootprint(BuildingDefinition _, bool __)
    {
        return new Vector2Int(1, 1);
    }

    private static BuildingPlacementCommandRequestCompositionSystemHelper.Context CreateActivePlacementCommandContext(
        EntityManager entityManager,
        out BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
        out GameObject prefab,
        out GameObject root,
        bool placementIsValid = true,
        Action<BuildingPlacementLifecycleCompositionSystemHelper.PlacementState> updatePlacement = null,
        Action<BuildingPlacementLifecycleCompositionSystemHelper.PlacementState> commitPlacement = null,
        BuildingPlacementLifecycleCompositionSystemHelper.ValidateConfirmDelegate validateConfirm = null,
        Func<int, bool> trySpendCost = null,
        BuildingDefinitionPrefabSystemHelper definitionSystem = null)
    {
        var runtimeStateSystem = new RuntimeGameplayStateSystem(entityManager);
        lifecycleSystem = new BuildingPlacementLifecycleCompositionSystemHelper();
        var sessionSystem = new BuildingPlacementSessionCompositionSystemHelper();
        prefab = new GameObject("PlacementCommandTestPrefab");
        root = new GameObject("PlacementCommandTestRoot");
        Transform rootTransform = root.transform;
        BuildingPlacementLifecycleCompositionSystemHelper activeLifecycleSystem = lifecycleSystem;

        BuildingPlacementLifecycleCompositionSystemHelper.UpdatePlacementVisualDelegate updatePlacementVisual =
            (placement, _, _) =>
            {
                placement.IsValid = placementIsValid;
                updatePlacement?.Invoke(placement);
            };

        BuildingPlacementSessionCompositionSystemHelper.Context sessionContext = new(
            runtimeStateSystem,
            activeLifecycleSystem,
            null,
            null,
            () => new BuildingPlacementLifecycleCompositionSystemHelper.CancelContext(null, null, UnityEngine.Object.DestroyImmediate),
            () => new BuildingPlacementLifecycleCompositionSystemHelper.BeginContext(
                runtimeStateSystem,
                null,
                null,
                rootTransform,
                null,
                UnityEngine.Object.DestroyImmediate,
                _ => Vector2Int.zero,
                null,
                updatePlacementVisual,
                null,
                null,
                null),
            () => new BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext(
                validateConfirm ?? (placement => placement.IsValid),
                (_, creditsCost, _) => trySpendCost?.Invoke(creditsCost) == false
                    ? FactionConstructionResourceMutationResult.InsufficientCredits
                    : FactionConstructionResourceMutationResult.Applied,
                _ => FactionConstructionResourceMutationResult.Applied,
                _ => FactionConstructionResourceMutationResult.Applied,
                placement =>
                {
                    commitPlacement?.Invoke(placement);
                    return new BuildingPlacementCommitCompositionSystemHelper.CommitOutcome(null, 1);
                }),
            () => new BuildingPlacementLifecycleCompositionSystemHelper.RotateContext(updatePlacementVisual),
            null,
            null,
            null,
            null);

        return new BuildingPlacementCommandRequestCompositionSystemHelper.Context(
            null,
            definitionSystem,
            sessionSystem,
            sessionContext,
            null);
    }

    private static void AssertPlacementResult(
        EntityManager em,
        BuildingPlacementCommandRequestCompositionSystemHelper commandSystem,
        int requestId,
        byte requestKind,
        bool accepted,
        byte resultCode)
    {
        Assert.IsTrue(commandSystem.TryGetUiPlacementCommandResult(
            em,
            requestId,
            out BuildingUiPlacementCommandResultElement result));
        Assert.AreEqual(accepted ? 1 : 0, result.Accepted);
        Assert.AreEqual(requestKind, result.RequestKind);
        Assert.AreEqual(resultCode, result.ResultCode);
        if (requestKind == BuildingUiPlacementCommandRequestElement.KindConfirmPlacement)
            Assert.Greater(result.EconomyTransactionId, 0);
        else
            Assert.AreEqual(0, result.EconomyTransactionId);
    }

    private static BuildingPlacementCommandRequestCompositionSystemHelper.Context CreatePlacementCommandContext(
        EntityManager entityManager,
        BuildingPlacementSessionCompositionSystemHelper sessionSystem,
        Action<string> clearSelectedBuilding = null,
        Action clearCommandMode = null)
    {
        var runtimeStateSystem = new RuntimeGameplayStateSystem(entityManager);
        var lifecycleSystem = new BuildingPlacementLifecycleCompositionSystemHelper();
        BuildingPlacementSessionCompositionSystemHelper.Context sessionContext = new(
            runtimeStateSystem,
            lifecycleSystem,
            null,
            null,
            () => new BuildingPlacementLifecycleCompositionSystemHelper.CancelContext(null, null, null),
            () => default,
            () => default,
            () => default,
            null,
            null,
            clearSelectedBuilding,
            clearCommandMode);

        return new BuildingPlacementCommandRequestCompositionSystemHelper.Context(
            null,
            null,
            sessionSystem,
            sessionContext,
            null);
    }

    private void CreateRoadBuffer(int width, int height, out GridConfig grid, out DynamicBuffer<GridRoad> roads)
    {
        _world ??= new World("BuildingPlacementValidationUtilitySystemHelperTests");
        Entity entity = _world.EntityManager.CreateEntity();
        roads = _world.EntityManager.AddBuffer<GridRoad>(entity);
        roads.ResizeUninitialized(width * height);
        for (int i = 0; i < roads.Length; i++)
            roads[i] = default;

        grid = new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = default
        };
    }

    private sealed class ScratchPlacementState : BuildingPlacementInputUiSystemHelper.IPlacementState
    {
        public BuildingDefinition Definition { get; set; }
        public Vector2Int OriginCell { get; set; }
        public Vector2Int CommittedOriginCell { get; set; }
        public Vector2Int DragStartOriginCell { get; set; }
        public Vector2Int DragCurrentOriginCell { get; set; }
        public BuildingPlacementInputUiSystemHelper.DragFirstAxis DragFirstAxis { get; set; }
        public bool HideCurrentWallPreview { get; set; }
        public bool IsValid { get; set; } = true;
        public bool AutoRotateVertical { get; set; }
        public float LastPointerMovedAt { get; set; }
        public Vector2 LastPointerScreenPosition { get; set; }
        public List<BuildingPlacementInputUiSystemHelper.WallRun> CommittedWallRuns { get; set; }
    }
}
#endif
