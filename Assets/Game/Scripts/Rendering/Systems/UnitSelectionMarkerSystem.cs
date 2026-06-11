using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct UnitSelectionMarkerSystem : ISystem
{
    private const float MarkerGroundLift = 0.04f;
    private const float MarkerFootprintScaleMultiplier = 1.35f;
    private const float MarkerMinimumVehicleScale = 2.5f;
    private const float MarkerMinimumCharacterScale = 1f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitSelectionMarkerPrefabReference>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        var create = new NativeList<Entity>(Allocator.Temp);
        var remove = new NativeList<Entity>(Allocator.Temp);

        foreach (var (health, entity) in SystemAPI
                 .Query<RefRO<UnitHealth>>()
                 .WithEntityAccess())
        {
            bool shouldShow = health.ValueRO.Current > 0 &&
                              em.HasComponent<SelectedUnitTag>(entity) &&
                              em.HasComponent<UnitSelectionMarkerPrefabReference>(entity) &&
                              !em.HasComponent<UnitTransportPassenger>(entity);
            bool hasReference = em.HasComponent<UnitSelectionMarkerInstanceReference>(entity);
            bool hasInstance = hasReference &&
                               em.Exists(em.GetComponentData<UnitSelectionMarkerInstanceReference>(entity).Instance);
            if (hasReference && !hasInstance)
            {
                remove.Add(entity);
                if (shouldShow)
                    create.Add(entity);
                continue;
            }

            if (shouldShow && !hasInstance)
                create.Add(entity);
            else if (!shouldShow && hasReference)
                remove.Add(entity);
        }

        for (int i = 0; i < remove.Length; i++)
            DestroyMarker(em, remove[i]);

        for (int i = 0; i < create.Length; i++)
            CreateMarker(em, create[i]);

        create.Dispose();
        remove.Dispose();
    }

    private static void CreateMarker(EntityManager em, Entity unit)
    {
        UnitSelectionMarkerPrefabReference prefabRef = em.GetComponentData<UnitSelectionMarkerPrefabReference>(unit);
        if (prefabRef.Prefab == Entity.Null || !em.Exists(prefabRef.Prefab))
            return;

        Entity marker = em.Instantiate(prefabRef.Prefab);
        em.SetName(marker, "UnitSelectionMarker");
        LogSelectionClickDebug($"[SelectionClick] markerCreate unit={DescribeUnit(em, unit)} marker={marker} prefab={prefabRef.Prefab}");
        if (!em.HasComponent<Parent>(marker))
            em.AddComponentData(marker, new Parent { Value = unit });
        else
            em.SetComponentData(marker, new Parent { Value = unit });

        if (em.HasComponent<LocalTransform>(marker))
        {
            LocalTransform transform = em.GetComponentData<LocalTransform>(marker);
            transform.Position = new float3(0f, MarkerGroundLift, 0f);
            transform.Rotation = quaternion.identity;
            transform.Scale = 1f;
            em.SetComponentData(marker, transform);
        }

        EnsureSelectionMarkerComponents(em, marker, ResolveMarkerScale(em, unit));
        em.AddComponentData(unit, new UnitSelectionMarkerInstanceReference { Instance = marker });
    }

    private static void EnsureSelectionMarkerComponents(EntityManager em, Entity marker, float visibleScale)
    {
        if (!em.HasComponent<SelectionMarkerTag>(marker))
            em.AddComponent<SelectionMarkerTag>(marker);

        if (em.HasComponent<SelectionMarkerVisualChild>(marker))
        {
            SelectionMarkerVisualChild visualChild = em.GetComponentData<SelectionMarkerVisualChild>(marker);
            visualChild.VisibleScale = visibleScale;
            em.SetComponentData(marker, visualChild);
            return;
        }

        Entity visual = ResolveRenderableLinkedChild(em, marker);
        if (visual == Entity.Null)
            visual = marker;

        em.AddComponentData(marker, new SelectionMarkerVisualChild
        {
            Value = visual,
            VisibleScale = visibleScale
        });
    }

    private static Entity ResolveRenderableLinkedChild(EntityManager em, Entity marker)
    {
        if (!em.HasBuffer<LinkedEntityGroup>(marker))
            return Entity.Null;

        DynamicBuffer<LinkedEntityGroup> linked = em.GetBuffer<LinkedEntityGroup>(marker);
        for (int i = 0; i < linked.Length; i++)
        {
            Entity entity = linked[i].Value;
            if (entity == marker || !em.Exists(entity))
                continue;

            if (em.HasComponent<MaterialMeshInfo>(entity))
                return entity;
        }

        for (int i = 0; i < linked.Length; i++)
        {
            Entity entity = linked[i].Value;
            if (entity != marker && em.Exists(entity) && em.HasComponent<LocalTransform>(entity))
                return entity;
        }

        return Entity.Null;
    }

    private static float ResolveMarkerScale(EntityManager em, Entity unit)
    {
        float minimumScale = em.HasComponent<UnitMovementBehavior>(unit) &&
                             em.GetComponentData<UnitMovementBehavior>(unit).UsesVehicleMotion != 0
            ? MarkerMinimumVehicleScale
            : MarkerMinimumCharacterScale;

        if (!em.HasComponent<UnitFootprint>(unit))
            return minimumScale;

        int2 size = em.GetComponentData<UnitFootprint>(unit).Size;
        return math.max(minimumScale, math.max(size.x, size.y) * MarkerFootprintScaleMultiplier);
    }

    private static void DestroyMarker(EntityManager em, Entity unit)
    {
        UnitSelectionMarkerInstanceReference instance = em.GetComponentData<UnitSelectionMarkerInstanceReference>(unit);
        LogSelectionClickDebug($"[SelectionClick] markerDestroy unit={DescribeUnit(em, unit)} marker={instance.Instance}");
        VehicleVisualEntityUtility.DestroyVisualTree(em, instance.Instance);
        em.RemoveComponent<UnitSelectionMarkerInstanceReference>(unit);
    }

    [System.Diagnostics.Conditional("WARLINE_SELECTION_CLICK_DIAGNOSTICS")]
    private static void LogSelectionClickDebug(string message)
    {
        Debug.Log(message);
    }

    private static string DescribeUnit(EntityManager em, Entity unit)
    {
        if (unit == Entity.Null || !em.Exists(unit))
            return "null";

        string source = em.HasComponent<UnitSourcePrefabKey>(unit)
            ? em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString()
            : em.GetName(unit);
        byte faction = em.HasComponent<Faction>(unit)
            ? em.GetComponentData<Faction>(unit).Id
            : (byte)0;
        return $"{unit}/{source}/faction={faction}/selected={em.HasComponent<SelectedUnitTag>(unit)}";
    }
}
