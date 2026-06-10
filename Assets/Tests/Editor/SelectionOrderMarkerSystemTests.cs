using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectionOrderMarkerSystemTests
{
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
}
