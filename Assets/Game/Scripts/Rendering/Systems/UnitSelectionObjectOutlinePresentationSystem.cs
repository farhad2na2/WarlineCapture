using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Game.Components;

namespace Game.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UnitSelectionMarkerSystem))]
    public sealed partial class UnitSelectionObjectOutlinePresentationSystem : SystemBase
    {
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
        private Material _characterSelectionObjectOutlineMaterial;
        private Material _vehicleSelectionObjectOutlineMaterial;
        private EntityQuery _unitRenderEntityQuery;

        protected override void OnCreate()
        {
            _unitRenderEntityQuery = GetEntityQuery(new EntityQueryDesc
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

        protected override void OnDestroy()
        {
            DestroyRuntimeObject(_characterSelectionObjectOutlineMaterial);
            DestroyRuntimeObject(_vehicleSelectionObjectOutlineMaterial);
            _characterSelectionObjectOutlineMaterial = null;
            _vehicleSelectionObjectOutlineMaterial = null;
        }

        protected override void OnUpdate()
        {
            EntityManager em = EntityManager;
            using NativeList<Entity> units = new(Allocator.Temp);
            using NativeList<Entity> markers = new(Allocator.Temp);
            using NativeList<byte> vehicleMarkers = new(Allocator.Temp);
            using NativeList<byte> airUnits = new(Allocator.Temp);

            foreach (var (instance, entity) in SystemAPI
                         .Query<RefRO<UnitSelectionMarkerInstanceReference>>()
                         .WithAll<SelectedUnitTag>()
                         .WithEntityAccess())
            {
                Entity marker = instance.ValueRO.Instance;
                if (marker == Entity.Null || !em.Exists(marker))
                    continue;

                units.Add(entity);
                markers.Add(marker);
                vehicleMarkers.Add(UsesVehicleSelectionMarker(em, entity) ? (byte)1 : (byte)0);
                airUnits.Add(em.HasComponent<UnitAirMovement>(entity) ? (byte)1 : (byte)0);
            }

            for (int i = 0; i < units.Length; i++)
            {
                // Operation-map buildings can reference a packed/static-reuse renderer whose
                // render hierarchy also contains neighbouring buildings and roads. Copying
                // that renderer into the generic unit outline turns the shared presentation
                // into one enormous cyan hit-looking region. Buildings retain the dedicated
                // footprint marker created by UnitSelectionMarkerSystem; only the unsafe
                // renderer-copy outline is suppressed here.
                if (UnitSelectionMarkerSystem.IsBuildingSelectionOwner(em, units[i]))
                {
                    DestroySelectionObjectOutlines(em, markers[i]);
                    MarkSelectionObjectOutlineResolved(em, markers[i]);
                    continue;
                }

                EnsureSelectionObjectOutlines(
                    em,
                    units[i],
                    markers[i],
                    vehicleMarkers[i] != 0,
                    airUnits[i] != 0,
                    _unitRenderEntityQuery);
            }
        }

        private void EnsureSelectionObjectOutlines(
            EntityManager em,
            Entity unit,
            Entity marker,
            bool usesVehicleMarker,
            bool isAirUnit,
            EntityQuery renderEntityQuery)
        {
            if (unit == Entity.Null || marker == Entity.Null || !em.Exists(unit) || !em.Exists(marker))
                return;

            if (!em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker))
                em.AddBuffer<SelectionObjectOutlineInstanceElement>(marker);

            if (!usesVehicleMarker)
            {
                // Infantry uses GPU animation, so copying its render hierarchy cannot
                // produce a valid object outline. Keep the authored ground marker and do
                // not ancestry-scan every dense-city renderer when the squad is selected.
                // Also remove an outline created before this policy was applied so a stale
                // rectangular render proxy cannot remain underneath the circular marker.
                if (GetSelectionObjectOutlineCount(em, marker) > 0)
                    DestroySelectionObjectOutlines(em, marker);
                MarkSelectionObjectOutlineResolved(em, marker);
                return;
            }

            if (em.HasComponent<SelectionObjectOutlineResolvedTag>(marker))
                return;

            using NativeList<Entity> sources = new(MaxSelectionObjectOutlineRenderers, Allocator.Temp);
            CollectRenderableDescendants(em, unit, sources);
            CollectRenderableDescendantsByAncestryScan(em, unit, renderEntityQuery, sources);
            CollectReferencedVisualRootRenderSources(em, unit, renderEntityQuery, sources);

            for (int i = 0; i < sources.Length && GetSelectionObjectOutlineCount(em, marker) < MaxSelectionObjectOutlineRenderers; i++)
            {
                Entity source = sources[i];
                if (isAirUnit && IsAirSelectionObjectOutlineSourceSuppressed(em, source, unit))
                    continue;

                bool isGpuAnimatedCharacter = !usesVehicleMarker && IsGpuAnimatedSelectionObjectOutlineSource(em, source, unit);
                if (isGpuAnimatedCharacter)
                {
                    // GPU-skinned meshes cannot be copied safely for an object outline. The
                    // authored infantry ground ring is the sole selection treatment here;
                    // the former generated arc overlay fragmented into white square-like
                    // corners on Android and obscured faction recognition.
                    continue;
                }

                CreateSelectionObjectOutlineForSource(em, unit, marker, source, usesVehicleMarker);
            }

            // Zero is the expected result for GPU-animated infantry. Cache that result so
            // four selected soldiers do not each ancestry-scan the dense city's complete
            // renderer query on every frame.
            MarkSelectionObjectOutlineResolved(em, marker);
        }

        public static void DestroySelectionObjectOutlines(EntityManager em, Entity marker)
        {
            if (marker == Entity.Null || !em.Exists(marker) || !em.HasBuffer<SelectionObjectOutlineInstanceElement>(marker))
                return;

            DynamicBuffer<SelectionObjectOutlineInstanceElement> outlines = em.GetBuffer<SelectionObjectOutlineInstanceElement>(marker);
            using NativeList<Entity> outlineEntities = new(outlines.Length, Allocator.Temp);
            for (int i = 0; i < outlines.Length; i++)
                outlineEntities.Add(outlines[i].Value);

            outlines.Clear();

            if (em.HasComponent<SelectionObjectOutlineResolvedTag>(marker))
                em.RemoveComponent<SelectionObjectOutlineResolvedTag>(marker);

            for (int i = 0; i < outlineEntities.Length; i++)
            {
                Entity outline = outlineEntities[i];
                if (outline != Entity.Null && em.Exists(outline))
                    VehicleVisualEntityUtility.DestroyVisualTree(em, outline);
            }
        }

        private static void MarkSelectionObjectOutlineResolved(EntityManager em, Entity marker)
        {
            if (marker != Entity.Null && em.Exists(marker) &&
                !em.HasComponent<SelectionObjectOutlineResolvedTag>(marker))
            {
                em.AddComponent<SelectionObjectOutlineResolvedTag>(marker);
            }
        }

        private static bool UsesVehicleSelectionMarker(EntityManager em, Entity unit)
        {
            return em.HasComponent<UnitMovementBehavior>(unit) &&
                   em.GetComponentData<UnitMovementBehavior>(unit).UsesVehicleMotion != 0;
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

        private void CreateSelectionObjectOutlineForSource(
            EntityManager em,
            Entity unit,
            Entity marker,
            Entity source,
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
                    Material material = GetSelectionObjectOutlineMaterial(usesVehicleMarker);
                    ushort subMesh = ResolveSubMesh(mesh, index.SubMeshIndex);
                    CreateSelectionObjectOutlineEntity(em, unit, marker, source, material, mesh, subMesh, usesVehicleMarker);
                }

                return;
            }

            Mesh singleMesh = sourceRenderMeshArray.GetMesh(sourceInfo);
            Material singleMaterial = GetSelectionObjectOutlineMaterial(usesVehicleMarker);
            ushort singleSubMesh = ResolveSubMesh(singleMesh, sourceInfo.SubMesh);
            CreateSelectionObjectOutlineEntity(em, unit, marker, source, singleMaterial, singleMesh, singleSubMesh, usesVehicleMarker);
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
            if (mesh == null || material == null || GetSelectionObjectOutlineCount(em, marker) >= MaxSelectionObjectOutlineRenderers)
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

        private Material GetSelectionObjectOutlineMaterial(bool usesVehicleMarker)
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

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }

        private static float ResolveSelectionObjectOutlineScanStrength(float outlineWidth)
        {
            return outlineWidth >= 0.04f ? 0.12f : 0.16f;
        }
    }
}
