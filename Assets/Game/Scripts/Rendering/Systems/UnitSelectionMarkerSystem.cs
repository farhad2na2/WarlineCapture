using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using SnivelerCode.GpuAnimation.Scripts.Components;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct UnitSelectionMarkerSystem : ISystem
{
    private const float MarkerGroundLift = 0.12f;
    private const float MarkerFootprintScaleMultiplier = 1.35f;
    private const float VehicleMarkerFootprintCellWorldSize = 1.25f;
    private const float VehicleMarkerMeshBoundsPadding = 1.08f;
    private const float VehicleMarkerMeshWidth = 1.24f;
    private const float VehicleMarkerMeshDepth = 0.82f;
    private const float VehicleMarkerMinimumWorldWidth = 1.6f;
    private const float VehicleMarkerMinimumWorldDepth = 1.1f;
    private const float VehicleMarkerMaximumScale = 8f;
    private const float MarkerMinimumVehicleScale = 1.15f;
    private const float MarkerMinimumCharacterScale = 1f;
    private const float CharacterSelectionVolumeDefaultRadius = 0.68f;
    private const float CharacterSelectionVolumeMinRadius = 0.56f;
    private const float CharacterSelectionVolumeMaxRadius = 0.86f;
    private const float CharacterSelectionVolumeDefaultHeight = 1.5f;
    private const float CharacterSelectionVolumeMinHeight = 1.2f;
    private const float CharacterSelectionVolumeMaxHeight = 2.05f;
    private const int MaxSelectionObjectOutlineRenderers = 48;
    private const int MaxSelectionObjectOutlineParentDepth = 64;
    private EntityStorageInfoLookup _entityStorageInfoLookup;
    private EntityQuery _unitRenderEntityQuery;
    private EntityQuery _unitQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitSelectionMarkerPrefabReference>();
        _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
        _unitQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitHealth>());
        _unitRenderEntityQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Parent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<MaterialMeshInfo>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<SelectionMarkerTag>(),
                ComponentType.ReadOnly<SelectionObjectOutlineTag>(),
                ComponentType.ReadOnly<HealthBarFill>(),
                ComponentType.ReadOnly<DisableRendering>(),
            },
            Options = EntityQueryOptions.IncludeDisabledEntities
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        int unitCount = _unitQuery.CalculateEntityCount();
        var create = new NativeList<Entity>(math.max(1, unitCount), Allocator.TempJob);
        var removeReference = new NativeList<Entity>(math.max(1, unitCount), Allocator.TempJob);
        var destroy = new NativeList<Entity>(math.max(1, unitCount), Allocator.TempJob);
        _entityStorageInfoLookup.Update(ref state);
        state.Dependency = new CollectSelectionMarkerChangesJob
        {
            SelectedLookup = SystemAPI.GetComponentLookup<SelectedUnitTag>(true),
            PrefabReferenceLookup = SystemAPI.GetComponentLookup<UnitSelectionMarkerPrefabReference>(true),
            PassengerLookup = SystemAPI.GetComponentLookup<UnitTransportPassenger>(true),
            InstanceReferenceLookup = SystemAPI.GetComponentLookup<UnitSelectionMarkerInstanceReference>(true),
            EntityStorageInfoLookup = _entityStorageInfoLookup,
            Create = create.AsParallelWriter(),
            RemoveReference = removeReference.AsParallelWriter(),
            Destroy = destroy.AsParallelWriter()
        }.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        for (int i = 0; i < removeReference.Length; i++)
            RemoveMarkerReference(em, removeReference[i]);

        for (int i = 0; i < destroy.Length; i++)
            DestroyMarker(em, destroy[i]);

        for (int i = 0; i < create.Length; i++)
            CreateMarker(em, create[i], _unitRenderEntityQuery);

        foreach (var (instance, entity) in SystemAPI
                     .Query<RefRO<UnitSelectionMarkerInstanceReference>>()
                     .WithAll<SelectedUnitTag>()
                     .WithEntityAccess())
        {
            Entity marker = instance.ValueRO.Instance;
            if (marker == Entity.Null || !em.Exists(marker))
                continue;

            bool usesVehicleMarker = UsesVehicleSelectionMarker(em, entity);
            bool isAirUnit = em.HasComponent<UnitAirMovement>(entity);
            ApplyVehicleVariantVisibility(em, marker, usesVehicleMarker, isAirUnit);
            ApplyAirSelectionOutlineFilterState(em, marker, isAirUnit);

        }

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
        public NativeList<Entity>.ParallelWriter Create;
        public NativeList<Entity>.ParallelWriter RemoveReference;
        public NativeList<Entity>.ParallelWriter Destroy;

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
                RemoveReference.AddNoResize(entity);
                if (shouldShow)
                    Create.AddNoResize(entity);
                return;
            }

            if (!canOwnMarker && hasReference)
            {
                Destroy.AddNoResize(entity);
                return;
            }

            if (shouldShow && !hasInstance)
                Create.AddNoResize(entity);
        }
    }

    private static void CreateMarker(EntityManager em, Entity unit, EntityQuery renderEntityQuery)
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

        bool usesVehicleMarker = UsesVehicleSelectionMarker(em, unit);
        bool isAirUnit = em.HasComponent<UnitAirMovement>(unit);
        EnsureSelectionMarkerComponents(em, marker, ResolveMarkerScale(em, unit, usesVehicleMarker, renderEntityQuery));
        ApplyVehicleVariantVisibility(em, marker, usesVehicleMarker, isAirUnit);
        ApplyAirSelectionOutlineFilterState(em, marker, isAirUnit);
        em.AddComponentData(unit, new UnitSelectionMarkerInstanceReference { Instance = marker });
    }

    private static void EnsureSelectionMarkerComponents(EntityManager em, Entity marker, float2 visibleScale)
    {
        if (!em.HasComponent<SelectionMarkerTag>(marker))
            em.AddComponent<SelectionMarkerTag>(marker);

        float uniformVisibleScale = math.max(visibleScale.x, visibleScale.y);
        if (em.HasComponent<SelectionMarkerVisualChild>(marker))
        {
            SelectionMarkerVisualChild visualChild = em.GetComponentData<SelectionMarkerVisualChild>(marker);
            visualChild.VisibleScale = uniformVisibleScale;
            visualChild.VisibleScaleX = visibleScale.x;
            visualChild.VisibleScaleZ = visibleScale.y;
            em.SetComponentData(marker, visualChild);
            EnsureNonUniformMarkerScaleComponent(em, visualChild.Value, visibleScale);
            return;
        }

        Entity visual = ResolveRenderableLinkedChild(em, marker);
        if (visual == Entity.Null)
            visual = marker;

        EnsureNonUniformMarkerScaleComponent(em, visual, visibleScale);
        em.AddComponentData(marker, new SelectionMarkerVisualChild
        {
            Value = visual,
            VisibleScale = uniformVisibleScale,
            VisibleScaleX = visibleScale.x,
            VisibleScaleZ = visibleScale.y
        });
    }

    private static void EnsureNonUniformMarkerScaleComponent(EntityManager em, Entity visual, float2 visibleScale)
    {
        if (visual == Entity.Null || !em.Exists(visual) || !em.HasComponent<LocalTransform>(visual))
            return;

        PostTransformMatrix matrix = new()
        {
            Value = float4x4.Scale(new float3(math.max(0f, visibleScale.x), 1f, math.max(0f, visibleScale.y)))
        };
        if (!em.HasComponent<PostTransformMatrix>(visual))
            em.AddComponentData(visual, matrix);
        else
            em.SetComponentData(visual, matrix);
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

    private static float2 ResolveMarkerScale(
        EntityManager em,
        Entity unit,
        bool usesVehicleMarker,
        EntityQuery renderEntityQuery)
    {
        float minimumScale = usesVehicleMarker
            ? MarkerMinimumVehicleScale
            : MarkerMinimumCharacterScale;

        if (!em.HasComponent<UnitFootprint>(unit))
            return new float2(minimumScale, minimumScale);

        int2 size = em.GetComponentData<UnitFootprint>(unit).Size;
        if (!usesVehicleMarker)
        {
            float scale = math.max(minimumScale, math.max(size.x, size.y) * MarkerFootprintScaleMultiplier);
            return new float2(scale, scale);
        }

        if (TryResolveUnitMeshFootprintSize(em, unit, renderEntityQuery, out float2 meshFootprint))
        {
            float2 renderWorldSize = meshFootprint * VehicleMarkerMeshBoundsPadding;
            float2 minimumWorldSize = new(VehicleMarkerMinimumWorldWidth, VehicleMarkerMinimumWorldDepth);
            return ClampVehicleMarkerScale(math.max(renderWorldSize, minimumWorldSize), minimumScale);
        }

        float2 fallbackWorldSize = new(
            math.max(VehicleMarkerMinimumWorldWidth, math.max(1, size.x) * VehicleMarkerFootprintCellWorldSize),
            math.max(VehicleMarkerMinimumWorldDepth, math.max(1, size.y) * VehicleMarkerFootprintCellWorldSize));
        return ClampVehicleMarkerScale(fallbackWorldSize, minimumScale);
    }

    private static float2 ClampVehicleMarkerScale(float2 desiredWorldSize, float minimumScale)
    {
        return math.min(
            new float2(
                math.max(minimumScale, desiredWorldSize.x / VehicleMarkerMeshWidth),
                math.max(minimumScale, desiredWorldSize.y / VehicleMarkerMeshDepth)),
            new float2(VehicleMarkerMaximumScale, VehicleMarkerMaximumScale));
    }

    private static bool TryResolveUnitMeshFootprintSize(
        EntityManager em,
        Entity unit,
        EntityQuery renderEntityQuery,
        out float2 size)
    {
        size = float2.zero;
        using NativeList<Entity> sources = new(MaxSelectionObjectOutlineRenderers, Allocator.Temp);
        CollectRenderableDescendants(em, unit, sources);
        if (sources.Length == 0)
            CollectRenderableDescendantsByAncestryScan(em, unit, renderEntityQuery, sources);
        if (sources.Length == 0)
            return false;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;
        bool found = false;
        for (int i = 0; i < sources.Length; i++)
        {
            Entity source = sources[i];
            if (!em.HasComponent<RenderMeshArray>(source) ||
                !em.HasComponent<MaterialMeshInfo>(source) ||
                !em.HasComponent<LocalTransform>(source))
            {
                continue;
            }

            if (!TryResolveLocalToOwnerMatrix(em, source, unit, out float4x4 localToOwner))
                continue;

            bool sourceFound = false;
            RenderMeshArray renderMeshArray = em.GetSharedComponentManaged<RenderMeshArray>(source);
            MaterialMeshInfo meshInfo = em.GetComponentData<MaterialMeshInfo>(source);
            if (meshInfo.HasMaterialMeshIndexRange)
            {
                MaterialMeshIndex[] materialMeshIndices = renderMeshArray.MaterialMeshIndices;
                if (materialMeshIndices != null)
                {
                    RangeInt range = meshInfo.MaterialMeshIndexRange;
                    int end = math.min(range.end, materialMeshIndices.Length);
                    for (int meshIndex = range.start; meshIndex < end; meshIndex++)
                    {
                        Mesh mesh = ResolveRenderMeshArrayMesh(renderMeshArray, materialMeshIndices[meshIndex].MeshIndex);
                        sourceFound |= AccumulateMeshFootprint(mesh, localToOwner, ref minX, ref maxX, ref minZ, ref maxZ);
                    }
                }
            }
            else
            {
                Mesh mesh = renderMeshArray.GetMesh(meshInfo);
                sourceFound = AccumulateMeshFootprint(mesh, localToOwner, ref minX, ref maxX, ref minZ, ref maxZ);
            }

            found |= sourceFound;
        }

        if (!found)
            return false;

        size = new float2(math.max(0f, maxX - minX), math.max(0f, maxZ - minZ));
        return math.all(size > new float2(0.001f, 0.001f));
    }

    private static bool TryResolveLocalToOwnerMatrix(EntityManager em, Entity source, Entity owner, out float4x4 localToOwner)
    {
        localToOwner = float4x4.identity;
        if (source == Entity.Null || owner == Entity.Null || !em.Exists(source) || !em.Exists(owner))
            return false;

        using NativeList<float4x4> chain = new(MaxSelectionObjectOutlineParentDepth, Allocator.Temp);
        Entity current = source;
        for (int depth = 0; depth < MaxSelectionObjectOutlineParentDepth; depth++)
        {
            if (!em.HasComponent<LocalTransform>(current))
                return false;

            LocalTransform transform = em.GetComponentData<LocalTransform>(current);
            float4x4 localMatrix = float4x4.TRS(transform.Position, transform.Rotation, new float3(transform.Scale));
            if (em.HasComponent<PostTransformMatrix>(current))
                localMatrix = math.mul(localMatrix, em.GetComponentData<PostTransformMatrix>(current).Value);
            chain.Add(localMatrix);

            if (current == owner)
                break;

            if (!em.HasComponent<Parent>(current))
                return false;

            current = em.GetComponentData<Parent>(current).Value;
            if (current == Entity.Null || !em.Exists(current))
                return false;
        }

        if (chain.Length == 0 || current != owner)
            return false;

        localToOwner = float4x4.identity;
        for (int i = chain.Length - 2; i >= 0; i--)
            localToOwner = math.mul(localToOwner, chain[i]);

        return true;
    }

    private static bool AccumulateMeshFootprint(
        Mesh mesh,
        float4x4 localToOwner,
        ref float minX,
        ref float maxX,
        ref float minZ,
        ref float maxZ)
    {
        if (mesh == null || mesh.vertexCount <= 0)
            return false;

        Bounds bounds = mesh.bounds;
        if (bounds.size.sqrMagnitude <= 0.0001f)
            return false;

        float3 center = bounds.center;
        float3 extents = bounds.extents;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            float3 localCorner = center + extents * new float3(x, y, z);
            float3 ownerCorner = math.transform(localToOwner, localCorner);
            minX = math.min(minX, ownerCorner.x);
            maxX = math.max(maxX, ownerCorner.x);
            minZ = math.min(minZ, ownerCorner.z);
            maxZ = math.max(maxZ, ownerCorner.z);
        }

        return true;
    }

    private static bool UsesVehicleSelectionMarker(EntityManager em, Entity unit)
    {
        return em.HasComponent<UnitMovementBehavior>(unit) &&
               em.GetComponentData<UnitMovementBehavior>(unit).UsesVehicleMotion != 0;
    }

    private static void ApplyVehicleVariantVisibility(EntityManager em, Entity marker, bool usesVehicleMarker, bool isAirUnit)
    {
        bool showVehicleGroundMarker = usesVehicleMarker && !isAirUnit;

        if (em.HasBuffer<LinkedEntityGroup>(marker))
        {
            DynamicBuffer<LinkedEntityGroup> linked = em.GetBuffer<LinkedEntityGroup>(marker);
            for (int i = 0; i < linked.Length; i++)
            {
                Entity entity = linked[i].Value;
                if (entity != marker)
                    ApplyVehicleVariantVisibilityToEntity(em, entity, usesVehicleMarker, showVehicleGroundMarker);
            }
        }

        if (!em.HasBuffer<Child>(marker))
            return;

        using NativeList<Entity> stack = new(Allocator.Temp);
        DynamicBuffer<Child> children = em.GetBuffer<Child>(marker);
        for (int i = 0; i < children.Length; i++)
            stack.Add(children[i].Value);

        while (stack.Length > 0)
        {
            int last = stack.Length - 1;
            Entity entity = stack[last];
            stack.RemoveAt(last);
            if (entity == Entity.Null || !em.Exists(entity))
                continue;

            ApplyVehicleVariantVisibilityToEntity(em, entity, usesVehicleMarker, showVehicleGroundMarker);

            if (!em.HasBuffer<Child>(entity))
                continue;

            DynamicBuffer<Child> nestedChildren = em.GetBuffer<Child>(entity);
            for (int i = 0; i < nestedChildren.Length; i++)
                stack.Add(nestedChildren[i].Value);
        }
    }

    private static void ApplyVehicleVariantVisibilityToEntity(
        EntityManager em,
        Entity entity,
        bool usesVehicleMarker,
        bool showVehicleGroundMarker)
    {
        if (entity == Entity.Null ||
            !em.Exists(entity) ||
            !em.HasComponent<LocalTransform>(entity))
        {
            return;
        }

        string name = em.GetName(entity);
        if (string.IsNullOrEmpty(name))
            return;

        bool vehicleVisual = IsVehicleSelectionMarkerVisualName(name);
        bool infantryVisual = IsInfantrySelectionMarkerVisualName(name);
        if (!vehicleVisual && !infantryVisual)
            return;

        bool visible = vehicleVisual ? showVehicleGroundMarker : !usesVehicleMarker;
        LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
        transform.Scale = visible ? 1f : 0f;
        em.SetComponentData(entity, transform);
        SetSelectionMarkerVisualRendering(em, entity, visible);
    }

    private static void SetSelectionMarkerVisualRendering(EntityManager em, Entity entity, bool visible)
    {
        if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<MaterialMeshInfo>(entity))
            return;

        bool renderingDisabled = em.HasComponent<DisableRendering>(entity);
        if (visible && renderingDisabled)
            em.RemoveComponent<DisableRendering>(entity);
        else if (!visible && !renderingDisabled)
            em.AddComponent<DisableRendering>(entity);
    }

    private static bool IsVehicleSelectionMarkerVisualName(string name)
    {
        return name.Contains("Vehicle", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInfantrySelectionMarkerVisualName(string name)
    {
        return name.Contains("Infantry", System.StringComparison.OrdinalIgnoreCase) ||
               name.Contains("CapsuleAura", System.StringComparison.OrdinalIgnoreCase) ||
               name.Contains("OuterReadabilityArcs", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyAirSelectionOutlineFilterState(EntityManager em, Entity marker, bool isAirUnit)
    {
        if (marker == Entity.Null || !em.Exists(marker))
            return;

        bool alreadyFiltered = em.HasComponent<SelectionMarkerAirOutlineFilteredTag>(marker);
        if (isAirUnit)
        {
            if (alreadyFiltered)
                return;

            UnitSelectionObjectOutlinePresentationSystem.DestroySelectionObjectOutlines(em, marker);
            em.AddComponent<SelectionMarkerAirOutlineFilteredTag>(marker);
            return;
        }

        if (alreadyFiltered)
            em.RemoveComponent<SelectionMarkerAirOutlineFilteredTag>(marker);
    }

    private static void DestroyMarker(EntityManager em, Entity unit)
    {
        UnitSelectionMarkerInstanceReference instance = em.GetComponentData<UnitSelectionMarkerInstanceReference>(unit);
        LogSelectionClickDebug($"[SelectionClick] markerDestroy unit={DescribeUnit(em, unit)} marker={instance.Instance}");
        UnitSelectionObjectOutlinePresentationSystem.DestroySelectionObjectOutlines(em, instance.Instance);
        VehicleVisualEntityUtility.DestroyVisualTree(em, instance.Instance);
        RemoveMarkerReference(em, unit);
    }

    private static bool IsAirSelectionObjectOutlineSourceSuppressed(EntityManager em, Entity source, Entity owner)
    {
        if (IsAirSelectionObjectOutlineBladeReferenceSource(em, source, owner))
            return true;

        Entity current = source;
        for (int depth = 0; depth < MaxSelectionObjectOutlineParentDepth; depth++)
        {
            if (current == Entity.Null || !em.Exists(current))
                return false;

            string name = em.GetName(current);
            if (IsAirSelectionObjectOutlineSourceNameSuppressed(name))
                return true;

            if (current == owner || !em.HasComponent<Parent>(current))
                return false;

            current = em.GetComponentData<Parent>(current).Value;
        }

        return false;
    }

    private static bool IsAirSelectionObjectOutlineBladeReferenceSource(EntityManager em, Entity source, Entity owner)
    {
        if (source == Entity.Null ||
            owner == Entity.Null ||
            !em.Exists(source) ||
            !em.Exists(owner) ||
            !em.HasBuffer<UnitHelicopterBladeReference>(owner))
        {
            return false;
        }

        DynamicBuffer<UnitHelicopterBladeReference> blades = em.GetBuffer<UnitHelicopterBladeReference>(owner);
        if (blades.Length == 0)
            return false;

        Entity current = source;
        for (int depth = 0; depth < MaxSelectionObjectOutlineParentDepth; depth++)
        {
            if (current == Entity.Null || !em.Exists(current))
                return false;

            for (int i = 0; i < blades.Length; i++)
            {
                if (current == blades[i].Blade)
                    return true;
            }

            if (current == owner || !em.HasComponent<Parent>(current))
                return false;

            current = em.GetComponentData<Parent>(current).Value;
        }

        return false;
    }

    private static bool IsAirSelectionObjectOutlineSourceNameSuppressed(string name)
    {
        return !string.IsNullOrEmpty(name) &&
               (name.Contains("Blade", System.StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Rotor", System.StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Propeller", System.StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Shadow", System.StringComparison.OrdinalIgnoreCase) ||
                name.Contains("SelectionMarker", System.StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Footprint", System.StringComparison.OrdinalIgnoreCase) ||
                name.Contains("BoundsFrame", System.StringComparison.OrdinalIgnoreCase) ||
                name.Contains("CornerBracket", System.StringComparison.OrdinalIgnoreCase));
    }

    private static void CollectRenderableDescendants(EntityManager em, Entity unit, NativeList<Entity> sources)
    {
        if (!em.HasBuffer<Child>(unit))
            return;

        using NativeList<Entity> stack = new(Allocator.Temp);
        DynamicBuffer<Child> rootChildren = em.GetBuffer<Child>(unit);
        for (int i = 0; i < rootChildren.Length; i++)
            stack.Add(rootChildren[i].Value);

        while (stack.Length > 0 && sources.Length < MaxSelectionObjectOutlineRenderers)
        {
            int last = stack.Length - 1;
            Entity current = stack[last];
            stack.RemoveAt(last);
            if (current == Entity.Null || !em.Exists(current))
                continue;

            if (CanUseSelectionObjectOutlineSource(em, current, unit))
                AddUniqueSelectionObjectOutlineSource(sources, current);

            if (!em.HasBuffer<Child>(current))
                continue;

            DynamicBuffer<Child> children = em.GetBuffer<Child>(current);
            for (int i = 0; i < children.Length; i++)
                stack.Add(children[i].Value);
        }
    }

    private static void CollectRenderableDescendantsByAncestryScan(
        EntityManager em,
        Entity unit,
        EntityQuery renderEntityQuery,
        NativeList<Entity> sources)
    {
        if (renderEntityQuery.IsEmptyIgnoreFilter)
            return;

        using NativeArray<Entity> candidates = renderEntityQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < candidates.Length && sources.Length < MaxSelectionObjectOutlineRenderers; i++)
        {
            Entity candidate = candidates[i];
            if (candidate == Entity.Null ||
                !em.Exists(candidate) ||
                !IsDescendantOf(em, candidate, unit) ||
                !CanUseSelectionObjectOutlineSource(em, candidate, unit))
            {
                continue;
            }

            AddUniqueSelectionObjectOutlineSource(sources, candidate);
        }
    }

    private static void CollectReferencedVisualRootRenderSources(
        EntityManager em,
        Entity unit,
        EntityQuery renderEntityQuery,
        NativeList<Entity> sources)
    {
        if (em.HasComponent<UnitDetailedVisualReference>(unit))
            CollectRenderableRootAndDescendants(em, em.GetComponentData<UnitDetailedVisualReference>(unit).Root, renderEntityQuery, sources);

        if (em.HasComponent<UnitModelInstanceReference>(unit))
            CollectRenderableRootAndDescendants(em, em.GetComponentData<UnitModelInstanceReference>(unit).Instance, renderEntityQuery, sources);

        if (em.HasComponent<UnitMidLodInstanceReference>(unit))
            CollectRenderableRootAndDescendants(em, em.GetComponentData<UnitMidLodInstanceReference>(unit).Instance, renderEntityQuery, sources);

        if (em.HasComponent<UnitLowLodInstanceReference>(unit))
            CollectRenderableRootAndDescendants(em, em.GetComponentData<UnitLowLodInstanceReference>(unit).Instance, renderEntityQuery, sources);
    }

    private static void CollectRenderableRootAndDescendants(
        EntityManager em,
        Entity root,
        EntityQuery renderEntityQuery,
        NativeList<Entity> sources)
    {
        if (root == Entity.Null || !em.Exists(root))
            return;

        if (CanUseSelectionObjectOutlineSource(em, root, root))
            AddUniqueSelectionObjectOutlineSource(sources, root);

        if (em.HasBuffer<Child>(root))
        {
            using NativeList<Entity> stack = new(Allocator.Temp);
            DynamicBuffer<Child> rootChildren = em.GetBuffer<Child>(root);
            for (int i = 0; i < rootChildren.Length; i++)
                stack.Add(rootChildren[i].Value);

            while (stack.Length > 0 && sources.Length < MaxSelectionObjectOutlineRenderers)
            {
                int last = stack.Length - 1;
                Entity current = stack[last];
                stack.RemoveAt(last);
                if (current == Entity.Null || !em.Exists(current))
                    continue;

                if (CanUseSelectionObjectOutlineSource(em, current, root))
                    AddUniqueSelectionObjectOutlineSource(sources, current);

                if (!em.HasBuffer<Child>(current))
                    continue;

                DynamicBuffer<Child> children = em.GetBuffer<Child>(current);
                for (int i = 0; i < children.Length; i++)
                    stack.Add(children[i].Value);
            }
        }

        CollectRenderableDescendantsByAncestryScan(em, root, renderEntityQuery, sources);
    }

    private static void AddUniqueSelectionObjectOutlineSource(NativeList<Entity> sources, Entity source)
    {
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] == source)
                return;
        }

        if (sources.Length < MaxSelectionObjectOutlineRenderers)
            sources.Add(source);
    }

    private static bool CanUseSelectionObjectOutlineSource(EntityManager em, Entity source, Entity owner)
    {
        if (!em.HasComponent<LocalTransform>(source) ||
            !em.HasComponent<MaterialMeshInfo>(source) ||
            !em.HasComponent<RenderMeshArray>(source) ||
            em.HasComponent<DisableRendering>(source))
        {
            return false;
        }

        Entity current = source;
        for (int depth = 0; depth < MaxSelectionObjectOutlineParentDepth; depth++)
        {
            if (em.HasComponent<SelectionMarkerTag>(current) ||
                em.HasComponent<SelectionObjectOutlineTag>(current) ||
                em.HasComponent<HealthBarFill>(current) ||
                em.HasComponent<DisableRendering>(current))
            {
                return false;
            }

            if (current == owner)
                return true;

            if (!em.HasComponent<Parent>(current))
                return false;

            current = em.GetComponentData<Parent>(current).Value;
            if (current == Entity.Null || !em.Exists(current))
                return false;
        }

        return false;
    }

    private static bool IsGpuAnimatedSelectionObjectOutlineSource(EntityManager em, Entity source, Entity owner)
    {
        if (em.HasComponent<MaterialPropertyRenderPixel>(source) ||
            em.HasComponent<MaterialPropertyShowModel>(source) ||
            em.HasComponent<MaterialPropertyAlphaEnabled>(source))
        {
            return true;
        }

        return TryResolveGpuAnimationGroup(em, source, owner, out _);
    }

    private static bool TryResolveGpuAnimationGroup(EntityManager em, Entity source, Entity owner, out Entity group)
    {
        group = Entity.Null;
        if (source == Entity.Null || !em.Exists(source))
            return false;

        if (em.HasComponent<MeshLODComponent>(source))
        {
            Entity meshGroup = em.GetComponentData<MeshLODComponent>(source).Group;
            if (meshGroup != Entity.Null &&
                em.Exists(meshGroup) &&
                HasGpuAnimationRootComponent(em, meshGroup))
            {
                group = meshGroup;
                return true;
            }
        }

        Entity current = source;
        for (int depth = 0; depth < MaxSelectionObjectOutlineParentDepth; depth++)
        {
            if (HasGpuAnimationRootComponent(em, current))
            {
                group = current;
                return true;
            }

            if (current == owner || !em.HasComponent<Parent>(current))
                return false;

            current = em.GetComponentData<Parent>(current).Value;
            if (current == Entity.Null || !em.Exists(current))
                return false;
        }

        return false;
    }

    private static bool HasGpuAnimationRootComponent(EntityManager em, Entity entity)
    {
        return em.HasComponent<MaterialAnimationIndex>(entity) ||
               em.HasComponent<MaterialAnimationData>(entity) ||
               em.HasComponent<MaterialAnimatorLink>(entity);
    }

    private static bool IsDescendantOf(EntityManager em, Entity entity, Entity owner)
    {
        if (entity == owner)
            return true;

        Entity current = entity;
        for (int depth = 0; depth < MaxSelectionObjectOutlineParentDepth; depth++)
        {
            if (!em.HasComponent<Parent>(current))
                return false;

            current = em.GetComponentData<Parent>(current).Value;
            if (current == owner)
                return true;

            if (current == Entity.Null || !em.Exists(current))
                return false;
        }

        return false;
    }

    private static Mesh ResolveRenderMeshArrayMesh(RenderMeshArray renderMeshArray, int meshIndex)
    {
        if (renderMeshArray.MeshReferences == null || meshIndex < 0 || meshIndex >= renderMeshArray.MeshReferences.Length)
            return null;

        return renderMeshArray.MeshReferences[meshIndex].Value;
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
