using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SelectionOrderMarkerSystemTests
{
    private const string RtsSelectionConfigPath = "Assets/Game/Configs/Scene/Game_RTSSelection_Config.asset";
    private const string BuildingPlacementConfigPath = "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset";
    private const string MoveMarkerPrefabPath = "Assets/Game/Prefabs/Shapes/Target_Move.prefab";
    private const string AttackMarkerPrefabPath = "Assets/Game/Prefabs/Shapes/Target_Attack.prefab";
    private const string AttackTargetMarkerPrefabPath = "Assets/Game/Prefabs/Shapes/AttackTargetSelectionMarker.prefab";
    private const string BuildingSelectionMarkerPrefabPath = "Assets/Game/Prefabs/Buildings/BuildingSelectionMarker.prefab";
    private const string VehicleSelectionMarkerPrefabPath = "Assets/Game/Prefabs/Vehicles/VehicleSelectionMarker.prefab";
    private const string MoveMarkerMaterialPath = "Assets/Game/Rendering/Materials/Selection/Mat_Command_Move_Hologram.mat";
    private const string AttackMarkerMaterialPath = "Assets/Game/Rendering/Materials/Selection/Mat_Command_Attack_Hologram.mat";
    private const string TargetLockMaterialPath = "Assets/Game/Rendering/Materials/Selection/Mat_TargetLock_Attack_Hologram.mat";
    private const string HologramShaderPath = "Assets/Game/Rendering/Shaders/SelectionHologram.shader";
    private const string HologramShaderName = "WarlineCapture/Markers/SelectionHologram";
    private const float MoveOrderMarkerExpectedYOffset = 0.18f;
    private const float MoveOrderMarkerExpectedHorizontalScale = 2.4f;
    private const float AttackOrderMarkerExpectedYOffset = 0.45f;
    private const float AttackOrderMarkerExpectedHorizontalScale = 2.1f;

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.ShowMoveOrderMarker_ShowsUpgradedMoveMarker());
            RunCase(test => test.TryShowCommandResultMarker_ConsumesMoveAttackScanAndBoardResults());
            RunCase(test => test.ShowScanOrderMarker_UsesReadableCompositeMarker());
            RunCase(test => test.ShowScanOrderMarker_UsesOverlayAndStaysReadableAboveSurface());
            RunCase(test => test.ShowAttackOrderMarker_UsesSelectionPrefabForBuildingTargets());
            RunCase(test => test.ShowAttackOrderMarker_UsesRuntimeBuildingBoundsWhenAvailable());
            RunCase(test => test.ShowAttackOrderMarker_UsesSelectionPrefabForEntityTargets());
            RunCase(test => test.ShowAttackOrderMarker_FallsBackToPrefabForUntargetedWorldPoint());
            RunCase(test => test.UpdateAttackTargetPreviewMarkers_ShowsOnlyLivingHostileTargets());
            RunCase(test => test.UpdateBoardTargetPreviewMarkers_ShowsOnlyValidPlayerTargets());
            RunCase(test => test.MarkerAssetConfiguration_UsesDedicatedTargetLockPrefab());
            RunCase(test => test.MarkerPrefabs_UseHologramMaterialsAndDisableExpensiveRendering());
            RunCase(test => test.MoveMarkerPrefab_UsesCleanConnectedWaypointPieces());
            RunCase(test => test.SelectionHologramShader_DefinesDotsInstancingVariant());
            RunCase(test => test.GameplayPrefabs_DoNotContainForbiddenMarkerChildren());
            UnityEngine.Debug.Log("[SelectionOrderMarkerFocusedValidation] result=Passed tests=15");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            UnityEngine.Debug.LogError("[SelectionOrderMarkerFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(System.Action<SelectionOrderMarkerSystemTests> testCase)
    {
        testCase(new SelectionOrderMarkerSystemTests());
    }

    [Test]
    public void ShowMoveOrderMarker_ShowsUpgradedMoveMarker()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_MoveMarker");
        EntityManager em = world.EntityManager;
        CreateMarkerGrid(em);

        GameObject movePrefab = CreateMarkerPrefab("MoveMarkerPrefab", PrimitiveType.Quad, MoveMarkerMaterialPath);
        GameObject attackPrefab = CreateMarkerPrefab("AttackMarkerPrefab", PrimitiveType.Quad, AttackMarkerMaterialPath);
        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(movePrefab, attackPrefab, null, null, 1f, runtimeRoot.transform);
            markers.ShowMoveOrderMarker(em, new int2(4, 5), new Vector3(4f, 1.35f, 5f), FactionIdentity.PlayerFactionId);

            Transform moveMarker = FindChildByNameForTest(runtimeRoot.transform, "MoveOrderMarkerRuntime");
            Assert.IsNotNull(moveMarker);
            Assert.IsTrue(moveMarker.gameObject.activeSelf);
            Assert.AreEqual(4f, moveMarker.position.x, 0.001f);
            Assert.That(moveMarker.position.y, Is.GreaterThanOrEqualTo(1.35f + MoveOrderMarkerExpectedYOffset - 0.001f));
            Assert.AreEqual(5f, moveMarker.position.z, 0.001f);
            Assert.AreEqual(MoveOrderMarkerExpectedHorizontalScale, moveMarker.localScale.x, 0.001f);
            Assert.AreEqual(1f, moveMarker.localScale.y, 0.001f);
            Assert.AreEqual(MoveOrderMarkerExpectedHorizontalScale, moveMarker.localScale.z, 0.001f);

            Renderer renderer = moveMarker.GetComponent<Renderer>();
            Assert.IsNotNull(renderer);
            AssertRuntimeMarkerRendererConfigured(renderer, MoveMarkerMaterialPath);
            AssertMarkerRenderableMinY(moveMarker.gameObject, 1.35f + MoveOrderMarkerExpectedYOffset);
        }
        finally
        {
            Object.DestroyImmediate(movePrefab);
            Object.DestroyImmediate(attackPrefab);
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void TryShowCommandResultMarker_ConsumesMoveAttackScanAndBoardResults()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_ResultMarkers");
        EntityManager em = world.EntityManager;
        CreateMarkerGrid(em);

        GameObject movePrefab = CreateMarkerPrefab("MoveMarkerPrefab", PrimitiveType.Quad, MoveMarkerMaterialPath);
        GameObject attackPrefab = CreateMarkerPrefab("AttackMarkerPrefab", PrimitiveType.Quad, AttackMarkerMaterialPath);
        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(movePrefab, attackPrefab, null, null, 1f, runtimeRoot.transform);

            Assert.IsTrue(markers.TryShowCommandResultMarker(em, new RtsSelectionCommandResultElement
            {
                Kind = RtsSelectionCommandIntentKind.Move,
                Accepted = 1,
                TargetCell = new int2(4, 5),
                WorldPosition = new float3(4f, 1f, 5f),
                HasTargetCell = 1,
                HasWorldPosition = 1,
                MarkerFactionId = FactionIdentity.PlayerFactionId
            }));

            Transform moveMarker = FindChildByNameForTest(runtimeRoot.transform, "MoveOrderMarkerRuntime");
            Assert.IsNotNull(moveMarker);
            Assert.IsTrue(moveMarker.gameObject.activeSelf);
            Assert.AreEqual(4f, moveMarker.position.x, 0.001f);
            Assert.AreEqual(5f, moveMarker.position.z, 0.001f);

            Assert.IsTrue(markers.TryShowCommandResultMarker(em, new RtsSelectionCommandResultElement
            {
                Kind = RtsSelectionCommandIntentKind.BoardTransport,
                Accepted = 1,
                TargetCell = new int2(7, 8),
                WorldPosition = new float3(7f, 1f, 8f),
                HasTargetCell = 1,
                HasWorldPosition = 1,
                MarkerFactionId = FactionIdentity.PlayerFactionId
            }));
            Assert.AreEqual(7f, moveMarker.position.x, 0.001f);
            Assert.AreEqual(8f, moveMarker.position.z, 0.001f);

            Assert.IsTrue(markers.TryShowCommandResultMarker(em, new RtsSelectionCommandResultElement
            {
                Kind = RtsSelectionCommandIntentKind.Attack,
                Accepted = 1,
                WorldPosition = new float3(10f, 1f, 11f),
                HasWorldPosition = 1
            }));

            Transform attackMarker = FindChildByNameForTest(runtimeRoot.transform, "AttackOrderMarkerRuntime");
            Assert.IsNotNull(attackMarker);
            Assert.IsTrue(attackMarker.gameObject.activeSelf);
            Assert.AreEqual(10f, attackMarker.position.x, 0.001f);
            Assert.AreEqual(11f, attackMarker.position.z, 0.001f);

            Assert.IsTrue(markers.TryShowCommandResultMarker(em, new RtsSelectionCommandResultElement
            {
                Kind = RtsSelectionCommandIntentKind.Scan,
                Accepted = 1,
                TargetCell = new int2(12, 13),
                WorldPosition = new float3(12f, 0f, 13f),
                HasTargetCell = 1,
                HasWorldPosition = 1,
                RadiusCells = 4
            }));

            Transform scanMarker = FindChildByNameForTest(runtimeRoot.transform, "ScanOrderMarkerRuntime");
            Assert.IsNotNull(scanMarker);
            Assert.IsTrue(scanMarker.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(movePrefab);
            Object.DestroyImmediate(attackPrefab);
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void ShowScanOrderMarker_UsesReadableCompositeMarker()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_ScanMarker");
        EntityManager em = world.EntityManager;
        CreateMarkerGrid(em);

        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(null, null, null, null, 1f, runtimeRoot.transform);
            markers.ShowScanOrderMarker(
                em,
                new int2(3, 3),
                new float3(3f, 1.2f, 3f),
                radiusCells: 1,
                visibleSeconds: 0.25f);

            Transform scanMarker = FindChildByNameForTest(runtimeRoot.transform, "ScanOrderMarkerRuntime");
            Assert.IsNotNull(scanMarker);
            Assert.IsTrue(scanMarker.gameObject.activeSelf);

            LineRenderer[] renderers = scanMarker.GetComponentsInChildren<LineRenderer>(true);
            Assert.GreaterOrEqual(renderers.Length, 6);

            LineRenderer outerRing = AssertScanRenderer(scanMarker, "ScanOrderMarker_OuterRing", loop: true);
            LineRenderer innerRing = AssertScanRenderer(scanMarker, "ScanOrderMarker_InnerRing", loop: true);
            Assert.AreEqual(128, outerRing.positionCount);
            Assert.AreEqual(128, innerRing.positionCount);
            Assert.That(outerRing.widthMultiplier, Is.GreaterThanOrEqualTo(0.2f));
            Assert.That(innerRing.widthMultiplier, Is.GreaterThanOrEqualTo(0.08f));

            Vector3 outerPosition = outerRing.GetPosition(0);
            float visibleRadius = new Vector2(outerPosition.x - 3f, outerPosition.z - 3f).magnitude;
            Assert.That(visibleRadius, Is.GreaterThanOrEqualTo(2.99f));
            Assert.That(outerPosition.y, Is.GreaterThan(1.2f));

            for (int i = 0; i < 4; i++)
            {
                LineRenderer bracket = AssertScanRenderer(scanMarker, $"ScanOrderMarker_Bracket_{i}", loop: false);
                Assert.AreEqual(12, bracket.positionCount);
                Assert.That(bracket.widthMultiplier, Is.GreaterThanOrEqualTo(0.18f));
            }
        }
        finally
        {
            markers.Dispose();
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void ShowScanOrderMarker_UsesOverlayAndStaysReadableAboveSurface()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_ScanMarkerGrounding");
        EntityManager em = world.EntityManager;
        CreateMarkerGrid(em, width: 16, height: 16, cellSize: 2f, originY: 5f);

        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(null, null, null, null, 1f, runtimeRoot.transform);
            markers.ShowScanOrderMarker(
                em,
                new int2(3, 4),
                new float3(6f, -10f, 8f),
                radiusCells: 0,
                visibleSeconds: 0.1f);

            Transform scanMarker = FindChildByNameForTest(runtimeRoot.transform, "ScanOrderMarkerRuntime");
            Assert.IsNotNull(scanMarker);
            Assert.IsTrue(scanMarker.gameObject.activeSelf);

            LineRenderer outerRing = AssertScanRenderer(scanMarker, "ScanOrderMarker_OuterRing", loop: true);
            LineRenderer innerRing = AssertScanRenderer(scanMarker, "ScanOrderMarker_InnerRing", loop: true);
            AssertScanLineStaysAboveY(outerRing, 5f + 0.18f);
            AssertScanLineStaysAboveY(innerRing, 5f + 0.18f);

            Vector3 outerPosition = outerRing.GetPosition(0);
            float visibleRadius = new Vector2(outerPosition.x - 6f, outerPosition.z - 8f).magnitude;
            Assert.That(visibleRadius, Is.GreaterThanOrEqualTo(5.99f));
            AssertScanLineIsConnected(outerRing, maxSegmentLength: 0.6f);
            AssertScanLineIsConnected(innerRing, maxSegmentLength: 0.4f);

            for (int i = 0; i < 4; i++)
            {
                LineRenderer bracket = AssertScanRenderer(scanMarker, $"ScanOrderMarker_Bracket_{i}", loop: false);
                AssertScanLineStaysAboveY(bracket, 5f + 0.18f);
                AssertScanLineIsConnected(bracket, maxSegmentLength: 0.7f);
            }
        }
        finally
        {
            markers.Dispose();
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void ShowAttackOrderMarker_UsesSelectionPrefabForBuildingTargets()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_AttackTargetRing");
        EntityManager em = world.EntityManager;
        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 16,
            Height = 16,
            CellSize = 1f,
            Origin = new float3(0f, 0f, 0f)
        });
        em.AddBuffer<GridWalkable>(gridEntity);
        Entity building = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitFootprint));
        em.SetComponentData(building, new RuntimeBuildingCombatInfo
        {
            OriginCell = new int2(8, 3),
            FootprintCells = new int2(4, 6)
        });
        em.SetComponentData(building, new UnitFootprint { Size = new int2(4, 6) });

        GameObject movePrefab = CreateMarkerPrefab("MoveMarkerPrefab", PrimitiveType.Quad, MoveMarkerMaterialPath);
        GameObject attackPrefab = CreateMarkerPrefab("AttackMarkerPrefab", PrimitiveType.Quad, AttackMarkerMaterialPath);
        GameObject attackTargetPrefab = CreateMarkerPrefab("AttackTargetMarkerPrefab", PrimitiveType.Cube, TargetLockMaterialPath);
        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(movePrefab, attackPrefab, attackTargetPrefab, null, 1f, runtimeRoot.transform);
            markers.ShowAttackOrderMarker(em, building, new Vector3(4f, 0f, 5f), 6f);

            Transform attackMarker = FindChildByNameForTest(runtimeRoot.transform, "AttackTargetSelectionMarkerRuntime");
            Assert.IsNotNull(attackMarker);
            Assert.IsTrue(attackMarker.gameObject.activeSelf);
            Assert.AreEqual(10f, attackMarker.position.x, 0.001f);
            Assert.AreEqual(6f, attackMarker.position.z, 0.001f);
            Assert.AreEqual(5f, attackMarker.localScale.x, 0.001f);
            Assert.AreEqual(1f, attackMarker.localScale.y, 0.001f);
            Assert.AreEqual(7.5f, attackMarker.localScale.z, 0.001f);

            Renderer renderer = attackMarker.GetComponent<Renderer>();
            Assert.IsNotNull(renderer);
            AssertRuntimeMarkerRendererConfigured(renderer, TargetLockMaterialPath);
            AssertMarkerRenderableMinY(attackMarker.gameObject, 0.05f);
            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            AssertTargetLockPropertyBlock(propertyBlock);
        }
        finally
        {
            Object.DestroyImmediate(movePrefab);
            Object.DestroyImmediate(attackPrefab);
            Object.DestroyImmediate(attackTargetPrefab);
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void ShowAttackOrderMarker_UsesRuntimeBuildingBoundsWhenAvailable()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_AttackTargetRuntimeBounds");
        EntityManager em = world.EntityManager;
        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 32,
            Height = 32,
            CellSize = 1f,
            Origin = new float3(0f, 0f, 0f)
        });
        em.AddBuffer<GridWalkable>(gridEntity);
        Entity building = em.CreateEntity(typeof(RuntimeBuildingCombatInfo));
        em.SetComponentData(building, new RuntimeBuildingCombatInfo
        {
            RuntimeBuildingId = 42,
            OriginCell = new int2(8, 3),
            FootprintCells = new int2(4, 6)
        });

        GameObject movePrefab = CreateMarkerPrefab("MoveMarkerPrefab", PrimitiveType.Quad, MoveMarkerMaterialPath);
        GameObject attackPrefab = CreateMarkerPrefab("AttackMarkerPrefab", PrimitiveType.Quad, AttackMarkerMaterialPath);
        GameObject attackTargetPrefab = CreateMarkerPrefab("AttackTargetMarkerPrefab", PrimitiveType.Cube, TargetLockMaterialPath);
        GameObject buildingInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buildingInstance.name = "RuntimeTargetBuilding";
        buildingInstance.transform.position = new Vector3(20f, 2f, 30f);
        buildingInstance.transform.localScale = new Vector3(2f, 4f, 6f);
        buildingInstance.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(
                movePrefab,
                attackPrefab,
                attackTargetPrefab,
                (Entity _, int runtimeBuildingId, out GameObject instance) =>
                {
                    instance = runtimeBuildingId == 42 ? buildingInstance : null;
                    return instance != null;
                },
                1f,
                runtimeRoot.transform);
            markers.ShowAttackOrderMarker(em, building, new Vector3(4f, 0f, 5f), 6f);

            Transform attackMarker = FindChildByNameForTest(runtimeRoot.transform, "AttackTargetSelectionMarkerRuntime");
            Assert.IsNotNull(attackMarker);
            Assert.IsTrue(attackMarker.gameObject.activeSelf);
            Bounds bounds = buildingInstance.GetComponent<Renderer>().bounds;
            Assert.AreEqual(bounds.center.x, attackMarker.position.x, 0.001f);
            Assert.AreEqual(bounds.center.z, attackMarker.position.z, 0.001f);
            Assert.AreEqual(5f, attackMarker.localScale.x, 0.001f);
            Assert.AreEqual(1f, attackMarker.localScale.y, 0.001f);
            Assert.AreEqual(7.5f, attackMarker.localScale.z, 0.001f);
            Assert.AreEqual(35f, attackMarker.eulerAngles.y, 0.001f);
            AssertMarkerRenderableMinY(attackMarker.gameObject, bounds.min.y + 0.05f);
        }
        finally
        {
            Object.DestroyImmediate(movePrefab);
            Object.DestroyImmediate(attackPrefab);
            Object.DestroyImmediate(attackTargetPrefab);
            Object.DestroyImmediate(buildingInstance);
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void ShowAttackOrderMarker_UsesSelectionPrefabForEntityTargets()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_AttackTargetEntity");
        EntityManager em = world.EntityManager;
        CreateMarkerGrid(em);
        Entity target = em.CreateEntity(typeof(LocalTransform), typeof(UnitFootprint), typeof(Faction), typeof(UnitHealth));
        em.SetComponentData(target, LocalTransform.FromPositionRotation(
            new float3(7f, 0f, 8f),
            quaternion.RotateY(math.radians(25f))));
        em.SetComponentData(target, new UnitFootprint { Size = new int2(2, 3) });
        em.SetComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });

        GameObject movePrefab = CreateMarkerPrefab("MoveMarkerPrefab", PrimitiveType.Quad, MoveMarkerMaterialPath);
        GameObject attackPrefab = CreateMarkerPrefab("AttackMarkerPrefab", PrimitiveType.Quad, AttackMarkerMaterialPath);
        GameObject attackTargetPrefab = CreateMarkerPrefab("AttackTargetMarkerPrefab", PrimitiveType.Cube, TargetLockMaterialPath);
        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(movePrefab, attackPrefab, attackTargetPrefab, null, 1f, runtimeRoot.transform);
            markers.ShowAttackOrderMarker(em, target, Vector3.zero, 6f);

            Transform attackMarker = FindChildByNameForTest(runtimeRoot.transform, "AttackTargetSelectionMarkerRuntime");
            Assert.IsNotNull(attackMarker);
            Assert.IsTrue(attackMarker.gameObject.activeSelf);
            Assert.AreEqual(7f, attackMarker.position.x, 0.001f);
            Assert.AreEqual(8f, attackMarker.position.z, 0.001f);
            Assert.AreEqual(2.5f, attackMarker.localScale.x, 0.001f);
            Assert.AreEqual(1f, attackMarker.localScale.y, 0.001f);
            Assert.AreEqual(3.75f, attackMarker.localScale.z, 0.001f);
            Assert.AreEqual(25f, attackMarker.eulerAngles.y, 0.001f);

            Renderer renderer = attackMarker.GetComponent<Renderer>();
            Assert.IsNotNull(renderer);
            AssertRuntimeMarkerRendererConfigured(renderer, TargetLockMaterialPath);
            AssertMarkerRenderableMinY(attackMarker.gameObject, 0.05f);
            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            AssertTargetLockPropertyBlock(propertyBlock);
        }
        finally
        {
            Object.DestroyImmediate(movePrefab);
            Object.DestroyImmediate(attackPrefab);
            Object.DestroyImmediate(attackTargetPrefab);
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void ShowAttackOrderMarker_FallsBackToPrefabForUntargetedWorldPoint()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_AttackMarkerFallback");
        EntityManager em = world.EntityManager;
        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 16,
            Height = 16,
            CellSize = 1f,
            Origin = new float3(0f, 0f, 0f)
        });
        em.AddBuffer<GridWalkable>(gridEntity);

        GameObject movePrefab = CreateMarkerPrefab("MoveMarkerPrefab", PrimitiveType.Quad, MoveMarkerMaterialPath);
        GameObject attackPrefab = CreateMarkerPrefab("AttackMarkerPrefab", PrimitiveType.Quad, AttackMarkerMaterialPath);
        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(movePrefab, attackPrefab, null, null, 1f, runtimeRoot.transform);
            markers.ShowAttackOrderMarker(em, new Vector3(4f, 1.2f, 5f), 6f);

            Transform attackMarker = FindChildByNameForTest(runtimeRoot.transform, "AttackOrderMarkerRuntime");
            Assert.IsNotNull(attackMarker);
            Assert.AreEqual("AttackOrderMarkerRuntime", attackMarker.name);
            Assert.AreEqual(4f, attackMarker.position.x, 0.001f);
            Assert.That(attackMarker.position.y, Is.GreaterThanOrEqualTo(1.2f + AttackOrderMarkerExpectedYOffset - 0.001f));
            Assert.AreEqual(5f, attackMarker.position.z, 0.001f);
            Assert.AreEqual(AttackOrderMarkerExpectedHorizontalScale, attackMarker.localScale.x, 0.001f);
            Assert.AreEqual(1f, attackMarker.localScale.y, 0.001f);
            Assert.AreEqual(AttackOrderMarkerExpectedHorizontalScale, attackMarker.localScale.z, 0.001f);
            Assert.IsTrue(attackMarker.gameObject.activeSelf);

            Renderer renderer = attackMarker.GetComponent<Renderer>();
            Assert.IsNotNull(renderer);
            AssertRuntimeMarkerRendererConfigured(renderer, AttackMarkerMaterialPath);
        }
        finally
        {
            Object.DestroyImmediate(movePrefab);
            Object.DestroyImmediate(attackPrefab);
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void UpdateAttackTargetPreviewMarkers_ShowsOnlyLivingHostileTargets()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_AttackPreview");
        EntityManager em = world.EntityManager;
        CreateMarkerGrid(em);
        Entity hostile = CreatePreviewTarget(em, FactionIdentity.EnemyFactionId, new float3(2f, 0f, 3f), 100);
        CreatePreviewTarget(em, FactionIdentity.PlayerFactionId, new float3(4f, 0f, 5f), 100);
        CreatePreviewTarget(em, FactionIdentity.EnemyFactionId, new float3(6f, 0f, 7f), 0);

        GameObject movePrefab = CreateMarkerPrefab("MoveMarkerPrefab", PrimitiveType.Quad, MoveMarkerMaterialPath);
        GameObject attackPrefab = CreateMarkerPrefab("AttackMarkerPrefab", PrimitiveType.Quad, AttackMarkerMaterialPath);
        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(movePrefab, attackPrefab, null, null, 1f, runtimeRoot.transform);
            markers.UpdateAttackTargetPreviewMarkers(em, visible: true);

            Assert.AreEqual(1, CountActiveChildren(runtimeRoot.transform, "AttackTargetPreviewMarkerRuntime"));
            Transform preview = FindChildByNameForTest(runtimeRoot.transform, "AttackTargetPreviewMarkerRuntime");
            Assert.IsNotNull(preview);
            Assert.AreEqual(2f, preview.position.x, 0.001f);
            Assert.AreEqual(3f, preview.position.z, 0.001f);
            Assert.IsTrue(em.Exists(hostile));

            Renderer renderer = preview.GetComponent<Renderer>();
            Assert.IsNotNull(renderer);
            AssertMarkerRenderableMinY(preview.gameObject, 0.1f);
            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            AssertAttackPreviewPropertyBlock(propertyBlock);
        }
        finally
        {
            Object.DestroyImmediate(movePrefab);
            Object.DestroyImmediate(attackPrefab);
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void UpdateBoardTargetPreviewMarkers_ShowsOnlyValidPlayerTargets()
    {
        using var world = new World("SelectionOrderMarkerSystemTests_BoardPreview");
        EntityManager em = world.EntityManager;
        CreateMarkerGrid(em);
        Entity source = em.CreateEntity();
        Entity passenger = CreatePreviewTarget(em, FactionIdentity.PlayerFactionId, new float3(8f, 0f, 9f), 100);
        CreatePreviewTarget(em, FactionIdentity.PlayerFactionId, new float3(10f, 0f, 11f), 100);
        CreatePreviewTarget(em, FactionIdentity.EnemyFactionId, new float3(12f, 0f, 13f), 100);

        GameObject movePrefab = CreateMarkerPrefab("MoveMarkerPrefab", PrimitiveType.Quad, MoveMarkerMaterialPath);
        GameObject attackPrefab = CreateMarkerPrefab("AttackMarkerPrefab", PrimitiveType.Quad, AttackMarkerMaterialPath);
        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(movePrefab, attackPrefab, null, null, 1f, runtimeRoot.transform);
            markers.UpdateBoardTargetPreviewMarkers(
                em,
                visible: true,
                source,
                (_, _, target) => target == passenger);

            Assert.AreEqual(1, CountActiveChildren(runtimeRoot.transform, "AttackTargetPreviewMarkerRuntime"));
            Transform preview = FindChildByNameForTest(runtimeRoot.transform, "AttackTargetPreviewMarkerRuntime");
            Assert.IsNotNull(preview);
            Assert.AreEqual(8f, preview.position.x, 0.001f);
            Assert.AreEqual(9f, preview.position.z, 0.001f);

            Renderer renderer = preview.GetComponent<Renderer>();
            Assert.IsNotNull(renderer);
            AssertMarkerRenderableMinY(preview.gameObject, 0.1f);
            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            AssertBoardPreviewPropertyBlock(propertyBlock);
        }
        finally
        {
            Object.DestroyImmediate(movePrefab);
            Object.DestroyImmediate(attackPrefab);
            Object.DestroyImmediate(runtimeRoot);
        }
    }

    [Test]
    public void MarkerAssetConfiguration_UsesDedicatedTargetLockPrefab()
    {
        RTSSelectionSystemConfig rtsConfig = AssetDatabase.LoadAssetAtPath<RTSSelectionSystemConfig>(RtsSelectionConfigPath);
        Assert.IsNotNull(rtsConfig, $"Missing RTS selection config at {RtsSelectionConfigPath}");
        Assert.AreEqual(MoveMarkerPrefabPath, AssetDatabase.GetAssetPath(rtsConfig.MoveOrderMarkerPrefab));
        Assert.AreEqual(AttackMarkerPrefabPath, AssetDatabase.GetAssetPath(rtsConfig.AttackOrderMarkerPrefab));
        Assert.AreEqual(AttackTargetMarkerPrefabPath, AssetDatabase.GetAssetPath(rtsConfig.AttackTargetMarkerPrefab));

        BuildingPlacementSystemConfig buildingConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(BuildingPlacementConfigPath);
        Assert.IsNotNull(buildingConfig, $"Missing building placement config at {BuildingPlacementConfigPath}");
        Assert.AreEqual(BuildingSelectionMarkerPrefabPath, AssetDatabase.GetAssetPath(buildingConfig.BuildingSelectionMarkerPrefab));
    }

    [Test]
    public void MarkerPrefabs_UseHologramMaterialsAndDisableExpensiveRendering()
    {
        string[] markerPrefabPaths =
        {
            BuildingSelectionMarkerPrefabPath,
            VehicleSelectionMarkerPrefabPath,
            MoveMarkerPrefabPath,
            AttackMarkerPrefabPath,
            AttackTargetMarkerPrefabPath
        };

        for (int i = 0; i < markerPrefabPaths.Length; i++)
            AssertPremiumMarkerPrefab(markerPrefabPaths[i]);
    }

    [Test]
    public void MoveMarkerPrefab_UsesCleanConnectedWaypointPieces()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MoveMarkerPrefabPath);
        Assert.IsNotNull(prefab, $"Missing move marker prefab at {MoveMarkerPrefabPath}");

        Assert.IsNotNull(prefab.transform.Find("WaypointConnectedFill"));
        Assert.IsNotNull(prefab.transform.Find("WaypointConnectedOuterRing"));
        Assert.IsNotNull(prefab.transform.Find("WaypointConnectedInnerRing"));
        Assert.IsNotNull(prefab.transform.Find("WaypointCenterDot"));

        Assert.IsNull(prefab.transform.Find("WaypointDestinationPad_Subtle"));
        Assert.IsNull(prefab.transform.Find("WaypointRippleRings"));
        Assert.IsNull(prefab.transform.Find("WaypointDirectionChevrons"));
        Assert.IsNull(prefab.transform.Find("WaypointBeaconPin"));
    }

    [Test]
    public void SelectionHologramShader_DefinesDotsInstancingVariant()
    {
        Assert.IsTrue(File.Exists(HologramShaderPath), $"Missing hologram shader at {HologramShaderPath}");
        string shaderSource = File.ReadAllText(HologramShaderPath);

        Assert.That(shaderSource, Does.Contain("#pragma target 4.5"));
        Assert.That(shaderSource, Does.Contain("#pragma multi_compile _ DOTS_INSTANCING_ON"));
        Assert.That(shaderSource, Does.Contain("UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)"));
        Assert.That(shaderSource, Does.Contain("UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES"));

        string[] requiredDotsProperties =
        {
            "UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)",
            "UNITY_DOTS_INSTANCED_PROP(float4, _Color)",
            "UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)",
            "UNITY_DOTS_INSTANCED_PROP(float4, _AccentColor)",
            "UNITY_DOTS_INSTANCED_PROP(float, _Alpha)",
            "UNITY_DOTS_INSTANCED_PROP(float, _PulseStrength)",
            "UNITY_DOTS_INSTANCED_PROP(float, _PulseSpeed)",
            "UNITY_DOTS_INSTANCED_PROP(float, _ScanStrength)",
            "UNITY_DOTS_INSTANCED_PROP(float, _ScanSpeed)",
            "UNITY_DOTS_INSTANCED_PROP(float, _EdgeSoftness)"
        };

        for (int i = 0; i < requiredDotsProperties.Length; i++)
            Assert.That(shaderSource, Does.Contain(requiredDotsProperties[i]));
    }

    [Test]
    public void GameplayPrefabs_DoNotContainForbiddenMarkerChildren()
    {
        var failures = new List<string>();
        string[] prefabGuids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets/Game/Prefabs/Buildings", "Assets/Game/Prefabs/Vehicles", "Assets/Game/Prefabs/Characters" });

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            string fileName = Path.GetFileName(path);
            if (path.Contains("/Destroyed/") ||
                fileName == "BuildingSelectionMarker.prefab" ||
                fileName == "VehicleSelectionMarker.prefab")
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"Missing prefab at {path}");
            Transform[] children = prefab.GetComponentsInChildren<Transform>(true);
            for (int childIndex = 0; childIndex < children.Length; childIndex++)
            {
                Transform child = children[childIndex];
                if (child == prefab.transform)
                    continue;

                if (child.name is "SelectionMarker" or "FactionMarker" or "HealthBar" or "Destroyed")
                    failures.Add($"{path}:{GetTransformPath(child)}");
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    private static Transform FindChildByNameForTest(Transform root, string childName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static int CountActiveChildren(Transform root, string childName)
    {
        int count = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName && child.gameObject.activeSelf)
                count++;
        }

        return count;
    }

    private static Entity CreateMarkerGrid(EntityManager em, int width = 32, int height = 32, float cellSize = 1f, float originY = 0f)
    {
        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = cellSize,
            Origin = new float3(0f, originY, 0f)
        });
        DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
        for (int i = 0; i < width * height; i++)
            walkable.Add(new GridWalkable { Value = 1 });
        return gridEntity;
    }

    private static Entity CreatePreviewTarget(EntityManager em, byte factionId, float3 position, int health)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(LocalTransform),
            typeof(UnitHealth));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        return entity;
    }

    private static GameObject CreateMarkerPrefab(string name, PrimitiveType primitiveType, string materialPath)
    {
        GameObject marker = GameObject.CreatePrimitive(primitiveType);
        marker.name = name;
        Collider collider = marker.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        Renderer renderer = marker.GetComponent<Renderer>();
        Assert.IsNotNull(renderer);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        Assert.IsNotNull(material, $"Missing marker material at {materialPath}");
        renderer.sharedMaterial = material;
        return marker;
    }

    private static void AssertRuntimeMarkerRendererConfigured(Renderer renderer, string materialPath)
    {
        Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode);
        Assert.IsFalse(renderer.receiveShadows);
        Assert.AreEqual(LightProbeUsage.Off, renderer.lightProbeUsage);
        Assert.AreEqual(ReflectionProbeUsage.Off, renderer.reflectionProbeUsage);
        Assert.AreEqual(MotionVectorGenerationMode.ForceNoMotion, renderer.motionVectorGenerationMode);
        Assert.AreEqual(materialPath, AssetDatabase.GetAssetPath(renderer.sharedMaterial));
    }

    private static LineRenderer AssertScanRenderer(Transform scanMarker, string childName, bool loop)
    {
        Transform child = scanMarker.Find(childName);
        Assert.IsNotNull(child, $"Missing scan marker child {childName}");
        LineRenderer renderer = child.GetComponent<LineRenderer>();
        Assert.IsNotNull(renderer, $"{childName} must have a LineRenderer");
        Assert.AreEqual(loop, renderer.loop, $"{childName} loop mismatch");
        Assert.IsTrue(renderer.useWorldSpace, $"{childName} must use world-space positions");
        Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode);
        Assert.IsFalse(renderer.receiveShadows);
        Assert.AreEqual(LightProbeUsage.Off, renderer.lightProbeUsage);
        Assert.AreEqual(ReflectionProbeUsage.Off, renderer.reflectionProbeUsage);
        Assert.AreEqual(MotionVectorGenerationMode.ForceNoMotion, renderer.motionVectorGenerationMode);
        Assert.IsFalse(renderer.allowOcclusionWhenDynamic);
        Assert.IsNotNull(renderer.sharedMaterial);
        Assert.That(renderer.sharedMaterial.renderQueue, Is.GreaterThanOrEqualTo((int)RenderQueue.Overlay));
        return renderer;
    }

    private static void AssertScanLineStaysAboveY(LineRenderer renderer, float minimumY)
    {
        for (int i = 0; i < renderer.positionCount; i++)
            Assert.That(renderer.GetPosition(i).y, Is.GreaterThanOrEqualTo(minimumY - 0.001f));
    }

    private static void AssertScanLineIsConnected(LineRenderer renderer, float maxSegmentLength)
    {
        for (int i = 1; i < renderer.positionCount; i++)
        {
            float distance = Vector3.Distance(renderer.GetPosition(i - 1), renderer.GetPosition(i));
            Assert.That(distance, Is.LessThanOrEqualTo(maxSegmentLength));
        }

        if (!renderer.loop || renderer.positionCount <= 1)
            return;

        float closingDistance = Vector3.Distance(
            renderer.GetPosition(renderer.positionCount - 1),
            renderer.GetPosition(0));
        Assert.That(closingDistance, Is.LessThanOrEqualTo(maxSegmentLength));
    }

    private static void AssertTargetLockPropertyBlock(MaterialPropertyBlock propertyBlock)
    {
        AssertColorClose(new Color(1f, 0.08f, 0.04f, 0.95f), propertyBlock.GetColor("_BaseColor"));
        AssertColorClose(new Color(1f, 0.08f, 0.04f, 0.95f), propertyBlock.GetColor("_Color"));
        AssertColorClose(new Color(0.76f, 0.05f, 0.03f, 1f), propertyBlock.GetColor("_EmissionColor"));
        AssertColorClose(new Color(1f, 0.92f, 0.5f, 1f), propertyBlock.GetColor("_AccentColor"));
    }

    private static void AssertAttackPreviewPropertyBlock(MaterialPropertyBlock propertyBlock)
    {
        AssertColorClose(new Color(0.92f, 0.12f, 0.08f, 0.62f), propertyBlock.GetColor("_BaseColor"));
        AssertColorClose(new Color(0.92f, 0.12f, 0.08f, 0.62f), propertyBlock.GetColor("_Color"));
        AssertColorClose(new Color(0.24f, 0.03f, 0.02f, 1f), propertyBlock.GetColor("_EmissionColor"));
        AssertColorClose(new Color(1f, 0.64f, 0.42f, 1f), propertyBlock.GetColor("_AccentColor"));
    }

    private static void AssertBoardPreviewPropertyBlock(MaterialPropertyBlock propertyBlock)
    {
        AssertColorClose(new Color(0.2f, 1f, 0.78f, 0.68f), propertyBlock.GetColor("_BaseColor"));
        AssertColorClose(new Color(0.2f, 1f, 0.78f, 0.68f), propertyBlock.GetColor("_Color"));
        AssertColorClose(new Color(0.04f, 0.34f, 0.25f, 1f), propertyBlock.GetColor("_EmissionColor"));
        AssertColorClose(new Color(0.72f, 1f, 0.88f, 1f), propertyBlock.GetColor("_AccentColor"));
    }

    private static void AssertPremiumMarkerPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.IsNotNull(prefab, $"Missing marker prefab at {prefabPath}");
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        Assert.Greater(renderers.Length, 0, $"Marker prefab has no renderers: {prefabPath}");

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode, $"{prefabPath}:{GetTransformPath(renderer.transform)} casts shadows");
            Assert.IsFalse(renderer.receiveShadows, $"{prefabPath}:{GetTransformPath(renderer.transform)} receives shadows");
            Assert.AreEqual(LightProbeUsage.Off, renderer.lightProbeUsage, $"{prefabPath}:{GetTransformPath(renderer.transform)} uses light probes");
            Assert.AreEqual(ReflectionProbeUsage.Off, renderer.reflectionProbeUsage, $"{prefabPath}:{GetTransformPath(renderer.transform)} uses reflection probes");
            Assert.AreNotEqual(MotionVectorGenerationMode.Object, renderer.motionVectorGenerationMode, $"{prefabPath}:{GetTransformPath(renderer.transform)} writes object motion vectors");
            Assert.IsFalse(renderer.allowOcclusionWhenDynamic, $"{prefabPath}:{GetTransformPath(renderer.transform)} is a dynamic occludee");

            Material[] materials = renderer.sharedMaterials;
            Assert.Greater(materials.Length, 0, $"{prefabPath}:{GetTransformPath(renderer.transform)} has no materials");
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                AssertHologramMaterial(materials[materialIndex], $"{prefabPath}:{GetTransformPath(renderer.transform)}[{materialIndex}]");
        }
    }

    private static void AssertHologramMaterial(Material material, string label)
    {
        Assert.IsNotNull(material, $"{label} has a missing material");
        Assert.IsNotNull(material.shader, $"{label} has a missing shader");
        Assert.AreEqual(HologramShaderName, material.shader.name, $"{label} must use the premium marker shader");
        Assert.IsTrue(material.HasProperty("_BaseColor"), $"{label} missing _BaseColor");
        Assert.IsTrue(material.HasProperty("_Color"), $"{label} missing _Color");
        Assert.IsTrue(material.HasProperty("_EmissionColor"), $"{label} missing _EmissionColor");
        Assert.IsTrue(material.HasProperty("_AccentColor"), $"{label} missing _AccentColor");
    }

    private static string GetTransformPath(Transform transform)
    {
        var segments = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            segments.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", segments);
    }

    private static void AssertMarkerRenderableMinY(GameObject marker, float expectedMinimumY)
    {
        Assert.IsTrue(TryCalculateRendererBounds(marker, out Bounds bounds), "Marker must have renderer bounds.");
        Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(expectedMinimumY - 0.001f));
    }

    private static bool TryCalculateRendererBounds(GameObject marker, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = marker.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (hasBounds)
                bounds.Encapsulate(renderer.bounds);
            else
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
        }

        return hasBounds;
    }

    private static void AssertColorClose(Color expected, Color actual)
    {
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
        Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
    }
}
