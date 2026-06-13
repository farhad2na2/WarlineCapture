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
    private const float MarkerMinimumVehicleScale = 2.5f;
    private const float MarkerMinimumCharacterScale = 1f;
    private const int MaxSelectionObjectOutlineRenderers = 48;
    private const int MaxSelectionObjectOutlineParentDepth = 64;
    private const string SelectionObjectOutlineShaderName = "WarlineCapture/Markers/SelectionObjectOutline";
    private const string BaseColorProperty = "_BaseColor";
    private const string EmissionColorProperty = "_EmissionColor";
    private const string OutlineWidthProperty = "_OutlineWidth";
    private const string OutlineAlphaProperty = "_OutlineAlpha";
    private const string RimAlphaProperty = "_RimAlpha";
    private const string RimPowerProperty = "_RimPower";
    private const string ScanStrengthProperty = "_ScanStrength";
    private const string ScanSpeedProperty = "_ScanSpeed";
    private static readonly Color SelectionObjectOutlineBaseColor = new(0.02f, 0.9f, 1f, 0.9f);
    private static readonly Color SelectionObjectOutlineEmissionColor = new(0.05f, 1f, 1f, 1f);
    private static Material _characterSelectionObjectOutlineMaterial;
    private static Material _vehicleSelectionObjectOutlineMaterial;
    private EntityStorageInfoLookup _entityStorageInfoLookup;
    private EntityQuery _unitRenderEntityQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitSelectionMarkerPrefabReference>();
        _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
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
            CreateMarker(em, create[i], _unitRenderEntityQuery);

        foreach (var (instance, entity) in SystemAPI
                     .Query<RefRO<UnitSelectionMarkerInstanceReference>>()
                     .WithAll<SelectedUnitTag>()
                     .WithEntityAccess())
        {
            Entity marker = instance.ValueRO.Instance;
            if (marker == Entity.Null || !em.Exists(marker))
                continue;

            if (em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker) &&
                em.GetBuffer<SelectionObjectOutlineInstanceElement>(marker).Length > 0)
            {
                continue;
            }

            CreateSelectionObjectOutlines(em, entity, marker, UsesVehicleSelectionMarker(em, entity), _unitRenderEntityQuery);
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
        EnsureSelectionMarkerComponents(em, marker, ResolveMarkerScale(em, unit, usesVehicleMarker));
        ApplyVehicleVariantVisibility(em, marker, usesVehicleMarker);
        CreateSelectionObjectOutlines(em, unit, marker, usesVehicleMarker, renderEntityQuery);
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
            VisibleScale = visibleScale
        });
    }

    private static void EnsureNonUniformMarkerScaleComponent(EntityManager em, Entity visual, float visibleScale)
    {
        if (visual == Entity.Null || !em.Exists(visual) || !em.HasComponent<LocalTransform>(visual))
            return;

        PostTransformMatrix matrix = new()
        {
            Value = float4x4.Scale(new float3(math.max(0f, visibleScale), 1f, math.max(0f, visibleScale)))
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

    private static float ResolveMarkerScale(EntityManager em, Entity unit, bool usesVehicleMarker)
    {
        float minimumScale = usesVehicleMarker
            ? MarkerMinimumVehicleScale
            : MarkerMinimumCharacterScale;

        if (!em.HasComponent<UnitFootprint>(unit))
            return minimumScale;

        int2 size = em.GetComponentData<UnitFootprint>(unit).Size;
        return math.max(minimumScale, math.max(size.x, size.y) * MarkerFootprintScaleMultiplier);
    }

    private static bool UsesVehicleSelectionMarker(EntityManager em, Entity unit)
    {
        return em.HasComponent<UnitMovementBehavior>(unit) &&
               em.GetComponentData<UnitMovementBehavior>(unit).UsesVehicleMotion != 0;
    }

    private static void ApplyVehicleVariantVisibility(EntityManager em, Entity marker, bool usesVehicleMarker)
    {
        if (!em.HasBuffer<LinkedEntityGroup>(marker))
            return;

        DynamicBuffer<LinkedEntityGroup> linked = em.GetBuffer<LinkedEntityGroup>(marker);
        for (int i = 0; i < linked.Length; i++)
        {
            Entity entity = linked[i].Value;
            if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<LocalTransform>(entity))
                continue;

            string name = em.GetName(entity);
            if (string.IsNullOrEmpty(name) || !name.Contains("Vehicle", System.StringComparison.OrdinalIgnoreCase))
                continue;

            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            transform.Scale = usesVehicleMarker ? 1f : 0f;
            em.SetComponentData(entity, transform);
        }
    }

    private static void DestroyMarker(EntityManager em, Entity unit)
    {
        UnitSelectionMarkerInstanceReference instance = em.GetComponentData<UnitSelectionMarkerInstanceReference>(unit);
        LogSelectionClickDebug($"[SelectionClick] markerDestroy unit={DescribeUnit(em, unit)} marker={instance.Instance}");
        DestroySelectionObjectOutlines(em, instance.Instance);
        VehicleVisualEntityUtility.DestroyVisualTree(em, instance.Instance);
        RemoveMarkerReference(em, unit);
    }

    private static void CreateSelectionObjectOutlines(
        EntityManager em,
        Entity unit,
        Entity marker,
        bool usesVehicleMarker,
        EntityQuery renderEntityQuery)
    {
        if (unit == Entity.Null || marker == Entity.Null || !em.Exists(unit) || !em.Exists(marker))
            return;

        if (!em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker))
            em.AddBuffer<SelectionObjectOutlineInstanceElement>(marker);

        if (GetSelectionObjectOutlineCount(em, marker) > 0)
            return;

        Material material = GetSelectionObjectOutlineMaterial(usesVehicleMarker);
        if (material == null)
            return;

        using NativeList<Entity> sources = new(MaxSelectionObjectOutlineRenderers, Allocator.Temp);
        CollectRenderableDescendants(em, unit, sources);
        if (sources.Length == 0)
            CollectRenderableDescendantsByAncestryScan(em, unit, renderEntityQuery, sources);

        for (int i = 0; i < sources.Length && GetSelectionObjectOutlineCount(em, marker) < MaxSelectionObjectOutlineRenderers; i++)
            CreateSelectionObjectOutlineForSource(em, unit, marker, sources[i], material, usesVehicleMarker);
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
                sources.Add(current);

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

            sources.Add(candidate);
        }
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
                HasGpuAnimationComponent(em, current) ||
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

    private static bool HasGpuAnimationComponent(EntityManager em, Entity entity)
    {
        return em.HasComponent<MaterialAnimationIndex>(entity) ||
               em.HasComponent<MaterialAnimationData>(entity) ||
               em.HasComponent<MaterialAnimatorLink>(entity) ||
               em.HasComponent<MaterialPropertyRenderPixel>(entity) ||
               em.HasComponent<MaterialPropertyShowModel>(entity) ||
               em.HasComponent<MaterialPropertyAlphaEnabled>(entity);
    }

    private static bool IsDescendantOf(EntityManager em, Entity entity, Entity owner)
    {
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

    private static void CreateSelectionObjectOutlineForSource(
        EntityManager em,
        Entity unit,
        Entity marker,
        Entity source,
        Material material,
        bool usesVehicleMarker)
    {
        RenderMeshArray sourceRenderMeshArray = em.GetSharedComponentManaged<RenderMeshArray>(source);
        MaterialMeshInfo sourceInfo = em.GetComponentData<MaterialMeshInfo>(source);
        if (sourceInfo.HasMaterialMeshIndexRange)
        {
            MaterialMeshIndex[] materialMeshIndices = sourceRenderMeshArray.MaterialMeshIndices;
            if (materialMeshIndices == null)
                return;

            RangeInt range = sourceInfo.MaterialMeshIndexRange;
            int end = math.min(range.end, materialMeshIndices.Length);
            for (int i = range.start; i < end && GetSelectionObjectOutlineCount(em, marker) < MaxSelectionObjectOutlineRenderers; i++)
            {
                MaterialMeshIndex index = materialMeshIndices[i];
                Mesh mesh = ResolveRenderMeshArrayMesh(sourceRenderMeshArray, index.MeshIndex);
                ushort subMesh = ResolveSubMesh(mesh, index.SubMeshIndex);
                CreateSelectionObjectOutlineEntity(em, unit, marker, source, material, mesh, subMesh, usesVehicleMarker);
            }

            return;
        }

        Mesh singleMesh = sourceRenderMeshArray.GetMesh(sourceInfo);
        ushort singleSubMesh = ResolveSubMesh(singleMesh, sourceInfo.SubMesh);
        CreateSelectionObjectOutlineEntity(em, unit, marker, source, material, singleMesh, singleSubMesh, usesVehicleMarker);
    }

    private static Mesh ResolveRenderMeshArrayMesh(RenderMeshArray renderMeshArray, int meshIndex)
    {
        if (renderMeshArray.MeshReferences == null || meshIndex < 0 || meshIndex >= renderMeshArray.MeshReferences.Length)
            return null;

        return renderMeshArray.MeshReferences[meshIndex].Value;
    }

    private static ushort ResolveSubMesh(Mesh mesh, int subMesh)
    {
        if (mesh == null || mesh.subMeshCount <= 0)
            return 0;

        return (ushort)math.clamp(subMesh, 0, mesh.subMeshCount - 1);
    }

    private static void CreateSelectionObjectOutlineEntity(
        EntityManager em,
        Entity unit,
        Entity marker,
        Entity source,
        Material material,
        Mesh mesh,
        ushort subMesh,
        bool usesVehicleMarker)
    {
        if (mesh == null || GetSelectionObjectOutlineCount(em, marker) >= MaxSelectionObjectOutlineRenderers)
            return;

        Entity outline = em.CreateEntity();
        em.SetName(outline, usesVehicleMarker ? "UnitSelectionVehicleObjectOutline" : "UnitSelectionCharacterObjectOutline");
        em.AddComponent<SelectionObjectOutlineTag>(outline);
        em.AddComponentData(outline, new SelectionMarkerOwner { Value = unit });

        LocalTransform sourceTransform = em.HasComponent<LocalTransform>(source)
            ? em.GetComponentData<LocalTransform>(source)
            : LocalTransform.Identity;
        em.AddComponentData(outline, sourceTransform);
        em.AddComponentData(outline, new SelectionObjectOutlineVisibleScale
        {
            Value = math.max(0.0001f, sourceTransform.Scale)
        });

        Entity parent = em.HasComponent<Parent>(source)
            ? em.GetComponentData<Parent>(source).Value
            : unit;
        em.AddComponentData(outline, new Parent { Value = parent });

        if (em.HasComponent<PostTransformMatrix>(source))
            em.AddComponentData(outline, em.GetComponentData<PostTransformMatrix>(source));

        if (em.HasComponent<MeshLODComponent>(source))
            em.AddComponentData(outline, em.GetComponentData<MeshLODComponent>(source));

        RenderMeshDescription description = CreateSelectionObjectOutlineRenderDescription(em, source);
        RenderMeshArray outlineRenderMeshArray = new(new[] { material }, new[] { mesh });
        RenderMeshUtility.AddComponents(
            outline,
            em,
            description,
            outlineRenderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, subMesh));

        if (em.HasComponent<Unity.Rendering.RenderBounds>(source) &&
            em.HasComponent<Unity.Rendering.RenderBounds>(outline))
        {
            em.SetComponentData(outline, em.GetComponentData<Unity.Rendering.RenderBounds>(source));
        }

        em.GetBuffer<SelectionObjectOutlineInstanceElement>(marker).Add(new SelectionObjectOutlineInstanceElement { Value = outline });
    }

    private static int GetSelectionObjectOutlineCount(EntityManager em, Entity marker)
    {
        return marker != Entity.Null &&
               em.Exists(marker) &&
               em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker)
            ? em.GetBuffer<SelectionObjectOutlineInstanceElement>(marker).Length
            : 0;
    }

    private static RenderMeshDescription CreateSelectionObjectOutlineRenderDescription(EntityManager em, Entity source)
    {
        int layer = 0;
        uint renderingLayerMask = 0xffffffff;
        if (em.HasComponent<RenderFilterSettings>(source))
        {
            RenderFilterSettings sourceSettings = em.GetSharedComponentManaged<RenderFilterSettings>(source);
            layer = sourceSettings.Layer;
            renderingLayerMask = sourceSettings.RenderingLayerMask;
        }

        return new RenderMeshDescription(
            ShadowCastingMode.Off,
            false,
            MotionVectorGenerationMode.ForceNoMotion,
            layer,
            renderingLayerMask,
            LightProbeUsage.Off,
            false);
    }

    private static Material GetSelectionObjectOutlineMaterial(bool usesVehicleMarker)
    {
        if (usesVehicleMarker)
        {
            if (_vehicleSelectionObjectOutlineMaterial == null)
                _vehicleSelectionObjectOutlineMaterial = CreateSelectionObjectOutlineMaterial("Mat_Selection_ECS_Vehicle_ObjectOutline", 0.045f, 0.86f, 0.28f);
            return _vehicleSelectionObjectOutlineMaterial;
        }

        if (_characterSelectionObjectOutlineMaterial == null)
            _characterSelectionObjectOutlineMaterial = CreateSelectionObjectOutlineMaterial("Mat_Selection_ECS_Character_ObjectOutline", 0.022f, 0.92f, 0.34f);
        return _characterSelectionObjectOutlineMaterial;
    }

    private static Material CreateSelectionObjectOutlineMaterial(string name, float outlineWidth, float outlineAlpha, float rimAlpha)
    {
        Shader shader = Shader.Find(SelectionObjectOutlineShaderName);
        if (shader == null)
            return null;

        Material material = new(shader)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave,
            enableInstancing = true,
            renderQueue = (int)RenderQueue.Transparent + 5
        };
        material.SetColor(BaseColorProperty, SelectionObjectOutlineBaseColor);
        material.SetColor(EmissionColorProperty, SelectionObjectOutlineEmissionColor);
        material.SetFloat(OutlineWidthProperty, outlineWidth);
        material.SetFloat(OutlineAlphaProperty, outlineAlpha);
        material.SetFloat(RimAlphaProperty, rimAlpha);
        material.SetFloat(RimPowerProperty, 2.2f);
        material.SetFloat(ScanStrengthProperty, ResolveSelectionObjectOutlineScanStrength(outlineWidth));
        material.SetFloat(ScanSpeedProperty, 0.22f);
        return material;
    }

    private static float ResolveSelectionObjectOutlineScanStrength(float outlineWidth)
    {
        return outlineWidth >= 0.04f ? 0.12f : 0.16f;
    }

    private static void DestroySelectionObjectOutlines(EntityManager em, Entity marker)
    {
        if (marker == Entity.Null || !em.Exists(marker) || !em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker))
            return;

        DynamicBuffer<SelectionObjectOutlineInstanceElement> outlines = em.GetBuffer<SelectionObjectOutlineInstanceElement>(marker);
        using NativeList<Entity> outlineEntities = new(outlines.Length, Allocator.Temp);
        for (int i = 0; i < outlines.Length; i++)
            outlineEntities.Add(outlines[i].Value);

        outlines.Clear();

        for (int i = 0; i < outlineEntities.Length; i++)
        {
            Entity outline = outlineEntities[i];
            if (outline != Entity.Null && em.Exists(outline))
                VehicleVisualEntityUtility.DestroyVisualTree(em, outline);
        }
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
