using Unity.Burst;
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
    private EntityStorageInfoLookup _entityStorageInfoLookup;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitSelectionMarkerPrefabReference>();
        _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        var create = new NativeList<Entity>(Allocator.TempJob);
        var removeReference = new NativeList<Entity>(Allocator.TempJob);
        var destroy = new NativeList<Entity>(Allocator.TempJob);
        _entityStorageInfoLookup.Update(ref state);
        new CollectSelectionMarkerChangesJob
        {
            SelectedLookup = SystemAPI.GetComponentLookup<SelectedUnitTag>(true),
            PrefabReferenceLookup = SystemAPI.GetComponentLookup<UnitSelectionMarkerPrefabReference>(true),
            PassengerLookup = SystemAPI.GetComponentLookup<UnitTransportPassenger>(true),
            InstanceReferenceLookup = SystemAPI.GetComponentLookup<UnitSelectionMarkerInstanceReference>(true),
            EntityStorageInfoLookup = _entityStorageInfoLookup,
            Create = create,
            RemoveReference = removeReference,
            Destroy = destroy
        }.Run();

        for (int i = 0; i < removeReference.Length; i++)
            RemoveMarkerReference(em, removeReference[i]);

        for (int i = 0; i < destroy.Length; i++)
            DestroyMarker(em, destroy[i]);

        for (int i = 0; i < create.Length; i++)
            CreateMarker(em, create[i]);

        create.Dispose();
        removeReference.Dispose();
        destroy.Dispose();
    }

    [BurstCompile]
    private partial struct CollectSelectionMarkerChangesJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SelectedUnitTag> SelectedLookup;
        [ReadOnly] public ComponentLookup<UnitSelectionMarkerPrefabReference> PrefabReferenceLookup;
        [ReadOnly] public ComponentLookup<UnitTransportPassenger> PassengerLookup;
        [ReadOnly] public ComponentLookup<UnitSelectionMarkerInstanceReference> InstanceReferenceLookup;
        [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
        public NativeList<Entity> Create;
        public NativeList<Entity> RemoveReference;
        public NativeList<Entity> Destroy;

        private void Execute(Entity entity, in UnitHealth health)
        {
            bool canOwnMarker = health.Current > 0 &&
                                PrefabReferenceLookup.HasComponent(entity);
            bool shouldShow = canOwnMarker &&
                              SelectedLookup.HasComponent(entity) &&
                              !PassengerLookup.HasComponent(entity);
            bool hasReference = InstanceReferenceLookup.HasComponent(entity);
            bool hasInstance = hasReference &&
                               EntityStorageInfoLookup.Exists(InstanceReferenceLookup[entity].Instance);
            if (hasReference && !hasInstance)
            {
                RemoveReference.Add(entity);
                if (shouldShow)
                    Create.Add(entity);
                return;
            }

            if (!canOwnMarker && hasReference)
            {
                Destroy.Add(entity);
                return;
            }

            if (shouldShow && !hasInstance)
                Create.Add(entity);
        }
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
        RemoveMarkerReference(em, unit);
    }

    private static void RemoveMarkerReference(EntityManager em, Entity unit)
    {
        if (!em.HasComponent<UnitSelectionMarkerInstanceReference>(unit))
            return;

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
