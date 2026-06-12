using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionOrderMarkerSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.ShowAttackOrderMarker_UsesSelectionPrefabForBuildingTargets());
            RunCase(test => test.ShowAttackOrderMarker_UsesRuntimeBuildingBoundsWhenAvailable());
            RunCase(test => test.ShowAttackOrderMarker_FallsBackToPrefabForUntargetedWorldPoint());
            RunCase(test => test.UpdateAttackTargetPreviewMarkers_ShowsOnlyLivingHostileTargets());
            RunCase(test => test.UpdateBoardTargetPreviewMarkers_ShowsOnlyValidPlayerTargets());
            UnityEngine.Debug.Log("[SelectionOrderMarkerFocusedValidation] result=Passed tests=5");
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

        GameObject movePrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject attackPrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject attackTargetPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
            Assert.AreEqual(0.035f, attackMarker.position.y, 0.001f);
            Assert.AreEqual(6f, attackMarker.position.z, 0.001f);
            Assert.AreEqual(5f, attackMarker.localScale.x, 0.001f);
            Assert.AreEqual(1f, attackMarker.localScale.y, 0.001f);
            Assert.AreEqual(7.5f, attackMarker.localScale.z, 0.001f);

            Renderer renderer = attackMarker.GetComponent<Renderer>();
            Assert.IsNotNull(renderer);
            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            Color color = propertyBlock.GetColor("_BaseColor");
            Assert.Greater(color.r, 0.9f);
            Assert.Less(color.g, 0.2f);
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

        GameObject movePrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject attackPrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject attackTargetPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
            Assert.AreEqual(bounds.min.y + 0.035f, attackMarker.position.y, 0.001f);
            Assert.AreEqual(bounds.center.z, attackMarker.position.z, 0.001f);
            Assert.AreEqual(5f, attackMarker.localScale.x, 0.001f);
            Assert.AreEqual(1f, attackMarker.localScale.y, 0.001f);
            Assert.AreEqual(7.5f, attackMarker.localScale.z, 0.001f);
            Assert.AreEqual(35f, attackMarker.eulerAngles.y, 0.001f);
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

        GameObject movePrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject attackPrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject runtimeRoot = new("MarkerRoot");
        var markers = new SelectionOrderMarkerSystem();
        try
        {
            markers.Initialize(movePrefab, attackPrefab, null, null, 1f, runtimeRoot.transform);
            markers.ShowAttackOrderMarker(em, new Vector3(4f, 0f, 5f), 6f);

            Transform attackMarker = FindChildByNameForTest(runtimeRoot.transform, "AttackOrderMarkerRuntime");
            Assert.IsNotNull(attackMarker);
            Assert.AreEqual("AttackOrderMarkerRuntime", attackMarker.name);
            Assert.AreEqual(4f, attackMarker.position.x, 0.001f);
            Assert.AreEqual(0.45f, attackMarker.position.y, 0.001f);
            Assert.AreEqual(5f, attackMarker.position.z, 0.001f);
            Assert.IsTrue(attackMarker.gameObject.activeSelf);
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
        Entity hostile = CreatePreviewTarget(em, FactionIdentitySystem.EnemyFactionId, new float3(2f, 0f, 3f), 100);
        CreatePreviewTarget(em, FactionIdentitySystem.PlayerFactionId, new float3(4f, 0f, 5f), 100);
        CreatePreviewTarget(em, FactionIdentitySystem.EnemyFactionId, new float3(6f, 0f, 7f), 0);

        GameObject movePrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject attackPrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
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
            Assert.AreEqual(0.05f, preview.position.y, 0.001f);
            Assert.AreEqual(3f, preview.position.z, 0.001f);
            Assert.IsTrue(em.Exists(hostile));
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
        Entity passenger = CreatePreviewTarget(em, FactionIdentitySystem.PlayerFactionId, new float3(8f, 0f, 9f), 100);
        CreatePreviewTarget(em, FactionIdentitySystem.PlayerFactionId, new float3(10f, 0f, 11f), 100);
        CreatePreviewTarget(em, FactionIdentitySystem.EnemyFactionId, new float3(12f, 0f, 13f), 100);

        GameObject movePrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject attackPrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
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
            Assert.AreEqual(0.05f, preview.position.y, 0.001f);
            Assert.AreEqual(9f, preview.position.z, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(movePrefab);
            Object.DestroyImmediate(attackPrefab);
            Object.DestroyImmediate(runtimeRoot);
        }
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

    private static Entity CreateMarkerGrid(EntityManager em)
    {
        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 32,
            Height = 32,
            CellSize = 1f,
            Origin = new float3(0f, 0f, 0f)
        });
        em.AddBuffer<GridWalkable>(gridEntity);
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
}
