using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class RuntimeBuildingSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new RuntimeBuildingSystemTests();
            tests.AllocateId_IsSequentialAndResetsOnClear();
            tests.SelectionTracksOnlyExistingBuildings();
            tests.RemovingOtherBuilding_PreservesCurrentSelection();
            tests.RuntimeBuildingSelection_SelectAndFocusIgnoresEnemyBuilding();
            tests.BuildingUiSelectionCommandRequest_DeletesSelectedBuildingAndWritesResult();
            tests.BuildingUiSelectionCommandRequest_ClearsSelectionAndWritesResult();
            tests.StaticReuseHitShapePrefersNearestAuthoredCenterOverSharedRenderers();
            Debug.Log("[RuntimeBuildingSystemFocusedValidation] result=Passed tests=7");
            ValidationExit.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[RuntimeBuildingSystemFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AllocateId_IsSequentialAndResetsOnClear()
    {
        var system = new RuntimeBuildingCollection<string>();

        Assert.AreEqual(1, system.AllocateId());
        Assert.AreEqual(2, system.AllocateId());

        system.AddBuilding(1, "Barracks");
        system.Clear();

        Assert.AreEqual(0, system.Count);
        Assert.AreEqual(1, system.AllocateId());
    }

    [Test]
    public void SelectionTracksOnlyExistingBuildings()
    {
        var system = new RuntimeBuildingCollection<string>();
        system.AddBuilding(10, "HQ");
        system.SelectBuilding(10);

        Assert.IsTrue(system.HasSelectedBuilding());
        Assert.AreEqual(10, system.CurrentActiveBuildingId);

        system.RemoveBuilding(10);

        Assert.IsFalse(system.HasSelectedBuilding());
        Assert.IsFalse(system.CurrentActiveBuildingId.HasValue);
    }

    [Test]
    public void RemovingOtherBuilding_PreservesCurrentSelection()
    {
        var system = new RuntimeBuildingCollection<string>();
        system.AddBuilding(1, "Barracks");
        system.AddBuilding(2, "HQ");
        system.SelectBuilding(2);

        system.RemoveBuilding(1);

        Assert.IsTrue(system.HasSelectedBuilding());
        Assert.AreEqual(2, system.CurrentActiveBuildingId);
    }

    [Test]
    public void RuntimeBuildingSelection_SelectAndFocusIgnoresEnemyBuilding()
    {
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity friendlyBuilding = new()
        {
            Id = 1,
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId
        };
        RuntimeBuildingEntity enemyBuilding = new()
        {
            Id = 2,
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.EnemyFactionId
        };
        runtimeBuildings.AddBuilding(friendlyBuilding.Id, friendlyBuilding);
        runtimeBuildings.AddBuilding(enemyBuilding.Id, enemyBuilding);
        var selectionSystem = new BuildingSelectionRuntimeCompositionSystemHelper();
        int hudSelectionCount = 0;
        int clearFocusedCount = 0;
        BuildingSelectionRuntimeCompositionSystemHelper.Context context = CreateSelectionContext(
            runtimeBuildings,
            clearFocusedUnit: () => clearFocusedCount++,
            showHudSelection: _ => hudSelectionCount++);

        selectionSystem.SelectAndFocusBuilding(context, enemyBuilding);

        Assert.IsFalse(runtimeBuildings.CurrentActiveBuildingId.HasValue);
        Assert.AreEqual(0, hudSelectionCount);
        Assert.AreEqual(0, clearFocusedCount);

        selectionSystem.SelectAndFocusBuilding(context, friendlyBuilding);

        Assert.AreEqual(friendlyBuilding.Id, runtimeBuildings.CurrentActiveBuildingId);
        Assert.AreEqual(1, hudSelectionCount);
        Assert.AreEqual(1, clearFocusedCount);
    }

    [Test]
    public void BuildingUiSelectionCommandRequest_DeletesSelectedBuildingAndWritesResult()
    {
        using World world = new("BuildingUiSelectionCommandDeleteTest");
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity building = new() { Id = 7 };
        runtimeBuildings.AddBuilding(building.Id, building);
        runtimeBuildings.SelectBuilding(building.Id);
        var selectionSystem = new BuildingSelectionRuntimeCompositionSystemHelper();
        BuildingSelectionRuntimeCompositionSystemHelper.Context context = CreateSelectionContext(runtimeBuildings);

        int requestId = selectionSystem.EnqueueDeleteSelectedBuilding(world.EntityManager);
        selectionSystem.ProcessPendingUiSelectionCommands(
            world.EntityManager,
            context,
            buildingId => runtimeBuildings.RemoveBuilding(buildingId));

        Assert.IsTrue(selectionSystem.TryGetUiSelectionCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiSelectionCommandResultElement result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(BuildingUiSelectionCommandRequestElement.KindDeleteSelectedBuilding, result.RequestKind);
        Assert.AreEqual(BuildingUiSelectionCommandResultElement.Completed, result.ResultCode);
        Assert.AreEqual(building.Id, result.BuildingId);
        Assert.IsFalse(runtimeBuildings.ContainsBuilding(building.Id));
        Assert.IsFalse(runtimeBuildings.CurrentActiveBuildingId.HasValue);

        using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingUiSelectionCommandQueueComponent>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiSelectionCommandRequestElement>(queueEntity).Length);
    }

    [Test]
    public void BuildingUiSelectionCommandRequest_ClearsSelectionAndWritesResult()
    {
        using World world = new("BuildingUiSelectionCommandClearTest");
        var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
        RuntimeBuildingEntity building = new() { Id = 9 };
        runtimeBuildings.AddBuilding(building.Id, building);
        runtimeBuildings.SelectBuilding(building.Id);
        int refreshCount = 0;
        var selectionSystem = new BuildingSelectionRuntimeCompositionSystemHelper();
        BuildingSelectionRuntimeCompositionSystemHelper.Context context = CreateSelectionContext(runtimeBuildings, () => refreshCount++);

        int requestId = selectionSystem.EnqueueClearSelectedBuilding(world.EntityManager);
        selectionSystem.ProcessPendingUiSelectionCommands(world.EntityManager, context, null);

        Assert.IsTrue(selectionSystem.TryGetUiSelectionCommandResult(
            world.EntityManager,
            requestId,
            out BuildingUiSelectionCommandResultElement result));
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(BuildingUiSelectionCommandRequestElement.KindClearSelectedBuilding, result.RequestKind);
        Assert.AreEqual(BuildingUiSelectionCommandResultElement.Completed, result.ResultCode);
        Assert.AreEqual(0, result.BuildingId);
        Assert.IsFalse(runtimeBuildings.CurrentActiveBuildingId.HasValue);
        Assert.AreEqual(1, refreshCount);
    }

    [Test]
    public void StaticReuseHitShapePrefersNearestAuthoredCenterOverSharedRenderers()
    {
        GameObject cameraObject = new("StaticReuseSelectionCamera");
        GameObject barracksObject = new("StaticReuseBarracks");
        GameObject tentObject = new("StaticReuseContractorTent");
        GameObject decoyObject = new("StaticReuseNearbyDecoy");
        GameObject sharedPackedVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 20f;
            camera.aspect = 1f;
            camera.pixelRect = new Rect(0f, 0f, 512f, 512f);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 30f, 0f), Quaternion.Euler(90f, 0f, 0f));

            barracksObject.AddComponent<MapAuthoredBuildingVisualComponent>()
                .ConfigurePresentationGeometry(Vector3.zero, new Vector3(24f, 4f, 6f), 90f);
            tentObject.AddComponent<MapAuthoredBuildingVisualComponent>()
                .ConfigurePresentationGeometry(new Vector3(8f, 0f, 0f), new Vector3(2f, 2f, 2f), 0f);
            decoyObject.AddComponent<MapAuthoredBuildingVisualComponent>()
                .ConfigurePresentationGeometry(new Vector3(8f, 0f, 1f), Vector3.one, 0f);
            sharedPackedVisual.name = "SharedPackedBuildingRenderer";
            sharedPackedVisual.transform.SetParent(barracksObject.transform, false);
            sharedPackedVisual.transform.localScale = new Vector3(30f, 4f, 18f);
            Renderer sharedRenderer = sharedPackedVisual.GetComponent<Renderer>();

            var barracks = new RuntimeBuildingEntity
            {
                Id = 1,
                Instance = barracksObject,
                OriginCell = new Vector2Int(-2, -2),
                Definition = new BuildingDefinition
                {
                    FootprintCells = new Vector2Int(30, 18),
                    HasLocalBounds = true,
                    LocalBounds = new Bounds(Vector3.zero, new Vector3(30f, 4f, 18f))
                },
                FactionVisualRenderers = new[] { sharedRenderer },
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId
            };
            var contractorTent = new RuntimeBuildingEntity
            {
                Id = 2,
                Instance = tentObject,
                OriginCell = new Vector2Int(7, -1),
                Definition = new BuildingDefinition { FootprintCells = new Vector2Int(2, 2) },
                FactionVisualRenderers = new[] { sharedRenderer },
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId
            };
            var nearbyDecoy = new RuntimeBuildingEntity
            {
                Id = 3,
                Instance = decoyObject,
                OriginCell = new Vector2Int(8, 1),
                Definition = new BuildingDefinition { FootprintCells = Vector2Int.one },
                FactionVisualRenderers = new[] { sharedRenderer },
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId
            };
            var runtimeBuildings = new RuntimeBuildingCollection<RuntimeBuildingEntity>();
            runtimeBuildings.AddBuilding(barracks.Id, barracks);
            runtimeBuildings.AddBuilding(contractorTent.Id, contractorTent);
            runtimeBuildings.AddBuilding(nearbyDecoy.Id, nearbyDecoy);
            var grid = new GridConfig
            {
                Width = 64,
                Height = 64,
                CellSize = 1f,
                Origin = float3.zero
            };
            var selection = new BuildingSelectionRuntimeCompositionSystemHelper();
            var context = new BuildingSelectionRuntimeCompositionSystemHelper.Context(
                runtimeBuildings,
                runtimeBuildings.Buildings,
                camera,
                TryGetGrid,
                (origin, footprint, config) => new Vector3(
                    config.Origin.x + (origin.x + footprint.x * 0.5f) * config.CellSize,
                    config.Origin.y,
                    config.Origin.z + (origin.y + footprint.y * 0.5f) * config.CellSize),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
            Vector3 tentScreen = camera.WorldToScreenPoint(tentObject.GetComponent<MapAuthoredBuildingVisualComponent>().PresentationWorldCenter);

            Assert.IsTrue(selection.HandleBuildingSelectionClick(
                context,
                new Vector2(tentScreen.x, tentScreen.y),
                new Vector2Int(8, 0)));
            Assert.AreEqual(contractorTent.Id, runtimeBuildings.CurrentActiveBuildingId);

            bool TryGetGrid(out GridConfig value)
            {
                value = grid;
                return true;
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tentObject);
            UnityEngine.Object.DestroyImmediate(decoyObject);
            UnityEngine.Object.DestroyImmediate(sharedPackedVisual);
            UnityEngine.Object.DestroyImmediate(barracksObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static BuildingSelectionRuntimeCompositionSystemHelper.Context CreateSelectionContext(
        RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildings,
        BuildingSelectionRuntimeCompositionSystemHelper.RuntimeAction refreshMarkers = null,
        BuildingSelectionRuntimeCompositionSystemHelper.RuntimeAction clearFocusedUnit = null,
        BuildingSelectionRuntimeCompositionSystemHelper.BuildingHudSelectionAction showHudSelection = null)
    {
        var selectionSystem = new BuildingSelectionRuntimeCompositionSystemHelper();
        return selectionSystem.CreateContext(
            runtimeBuildings,
            runtimeBuildings.Buildings,
            null,
            null,
            null,
            null,
            refreshMarkers,
            clearFocusedUnit,
            showHudSelection,
            null,
            null,
            null,
            null,
            null);
    }
}
#endif
