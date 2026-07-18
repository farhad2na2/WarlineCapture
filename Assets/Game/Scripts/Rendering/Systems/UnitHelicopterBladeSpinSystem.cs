using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;
using Game.Components;

namespace Game.Rendering
{
    [UpdateAfter(typeof(UnitModelSpawnSystem))]
    public partial struct UnitHelicopterBladeSpinSystem : ISystem
    {
        private const float FlyingHeightEpsilon = 0.25f;
        private bool _diagnosticLogged;
        private BufferLookup<UnitHelicopterBladeReference> _bladeLookup;
        private BufferLookup<Child> _childLookup;
        private ComponentLookup<UnitModelInstanceReference> _modelLookup;
        private ComponentLookup<UnitDetailedVisualReference> _detailLookup;
        private ComponentLookup<UnitMidLodInstanceReference> _midLookup;
        private ComponentLookup<UnitLowLodInstanceReference> _lowLookup;
        private ComponentLookup<UnitSourcePrefabKey> _sourceLookup;
        private ComponentLookup<UnitDisplayInfo> _displayLookup;
        private ComponentLookup<UnitRenderVisualComponent> _visualStateLookup;
        private ComponentLookup<UnitAirMovement> _airLookup;
        private ComponentLookup<UnitAirComponent> _airStateLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private EntityQuery _airMovementQuery;

        public void OnCreate(ref SystemState state)
        {
            _diagnosticLogged = false;
            _airMovementQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitAirMovement>());
            _bladeLookup = state.GetBufferLookup<UnitHelicopterBladeReference>(true);
            _childLookup = state.GetBufferLookup<Child>(true);
            _modelLookup = state.GetComponentLookup<UnitModelInstanceReference>(true);
            _detailLookup = state.GetComponentLookup<UnitDetailedVisualReference>(true);
            _midLookup = state.GetComponentLookup<UnitMidLodInstanceReference>(true);
            _lowLookup = state.GetComponentLookup<UnitLowLodInstanceReference>(true);
            _sourceLookup = state.GetComponentLookup<UnitSourcePrefabKey>(true);
            _displayLookup = state.GetComponentLookup<UnitDisplayInfo>(true);
            _visualStateLookup = state.GetComponentLookup<UnitRenderVisualComponent>(true);
            _airLookup = state.GetComponentLookup<UnitAirMovement>(true);
            _airStateLookup = state.GetComponentLookup<UnitAirComponent>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>();
            state.RequireForUpdate(_airMovementQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            _bladeLookup.Update(ref state);
            _childLookup.Update(ref state);
            _modelLookup.Update(ref state);
            _detailLookup.Update(ref state);
            _midLookup.Update(ref state);
            _lowLookup.Update(ref state);
            _sourceLookup.Update(ref state);
            _displayLookup.Update(ref state);
            _visualStateLookup.Update(ref state);
            _airLookup.Update(ref state);
            _airStateLookup.Update(ref state);
            _transformLookup.Update(ref state);

            var em = state.EntityManager;
            float radians = math.radians(1440f) * SystemAPI.Time.DeltaTime;
            bool shouldLogDiagnostics = SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
                                        SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;

            JobHandle bakedBladeHandle = new RotateBakedBladeReferencesJob
            {
                Radians = radians,
                BladeLookup = _bladeLookup,
                TransformLookup = _transformLookup
            }.Schedule(state.Dependency);
            bakedBladeHandle.Complete();
            state.Dependency = bakedBladeHandle;

            using var rotatedBlades = new NativeHashSet<Entity>(32, Allocator.Temp);

            foreach (var (_, entity) in SystemAPI
                         .Query<RefRO<UnitAirMovement>>()
                         .WithNone<UnitDeathAnimationComponent>()
                         .WithEntityAccess()
                         )
            {
                bool shouldSpin = ShouldSpinBlades(entity, _transformLookup, _airStateLookup);
                if (!shouldSpin)
                {
                    if (shouldLogDiagnostics &&
                        !_diagnosticLogged &&
                        IsHelicopterDiagnosticCandidate(em, _childLookup, entity, _bladeLookup, _detailLookup, _modelLookup, _midLookup, _lowLookup, _sourceLookup))
                    {
                        _diagnosticLogged = true;
                        LogHelicopterBladeDiagnostic(
                            em,
                            _childLookup,
                            entity,
                            _bladeLookup,
                            _detailLookup,
                            _modelLookup,
                            _midLookup,
                            _lowLookup,
                            _sourceLookup,
                            _displayLookup,
                            _visualStateLookup,
                            _airLookup,
                            _airStateLookup,
                            _transformLookup,
                            false,
                            0,
                            0,
                            0,
                            SystemAPI.Time.DeltaTime);
                    }

                    continue;
                }

                rotatedBlades.Clear();

                int detailRotated = 0;
                int modelRotated = 0;
                int bakedRotated = MarkBakedBlades(_bladeLookup, entity, rotatedBlades);
                bool hasVisualState = TryGetCurrentVisualKind(entity, _visualStateLookup, out UnitRenderVisualKind currentVisual);
                bool scanAllFallbackRoots = !hasVisualState && bakedRotated == 0;
                bool scanDetailFallback = scanAllFallbackRoots ||
                                          (bakedRotated == 0 &&
                                           (currentVisual == UnitRenderVisualKind.Detail ||
                                            currentVisual == UnitRenderVisualKind.Unknown));
                bool scanMidFallback = scanAllFallbackRoots ||
                                       currentVisual == UnitRenderVisualKind.Mid;
                bool scanLowFallback = scanAllFallbackRoots ||
                                       currentVisual == UnitRenderVisualKind.Low;

                if (scanDetailFallback && _detailLookup.HasComponent(entity))
                    detailRotated = RotateBladeDescendants(em, _childLookup, _detailLookup[entity].Root, radians, rotatedBlades);

                if (scanDetailFallback && _modelLookup.HasComponent(entity))
                    modelRotated = RotateBladeDescendants(em, _childLookup, _modelLookup[entity].Instance, radians, rotatedBlades);

                if (scanMidFallback && _midLookup.HasComponent(entity))
                    RotateBladeDescendants(em, _childLookup, _midLookup[entity].Instance, radians, rotatedBlades);

                if (scanLowFallback && _lowLookup.HasComponent(entity))
                    RotateBladeDescendants(em, _childLookup, _lowLookup[entity].Instance, radians, rotatedBlades);

                if (shouldLogDiagnostics &&
                    !_diagnosticLogged &&
                    IsHelicopterDiagnosticCandidate(em, _childLookup, entity, _bladeLookup, _detailLookup, _modelLookup, _midLookup, _lowLookup, _sourceLookup))
                {
                    _diagnosticLogged = true;
                    LogHelicopterBladeDiagnostic(
                        em,
                        _childLookup,
                        entity,
                        _bladeLookup,
                        _detailLookup,
                        _modelLookup,
                        _midLookup,
                        _lowLookup,
                        _sourceLookup,
                        _displayLookup,
                        _visualStateLookup,
                        _airLookup,
                        _airStateLookup,
                        _transformLookup,
                        true,
                        detailRotated,
                        modelRotated,
                        bakedRotated,
                        SystemAPI.Time.DeltaTime);
                }
            }

            if (shouldLogDiagnostics && !_diagnosticLogged)
            {
                foreach (var (sourceKey, entity) in SystemAPI
                             .Query<RefRO<UnitSourcePrefabKey>>()
                             .WithAll<UnitGrid>()
                             .WithNone<UnitDeathAnimationComponent>()
                             .WithEntityAccess())
                {
                    if (!sourceKey.ValueRO.Value.ToString().Contains("Helicopter", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    _diagnosticLogged = true;
                    LogHelicopterBladeDiagnostic(
                        em,
                        _childLookup,
                        entity,
                        _bladeLookup,
                        _detailLookup,
                        _modelLookup,
                        _midLookup,
                        _lowLookup,
                        _sourceLookup,
                        _displayLookup,
                        _visualStateLookup,
                        _airLookup,
                        _airStateLookup,
                        _transformLookup,
                        false,
                        0,
                        0,
                        0,
                        SystemAPI.Time.DeltaTime);
                    break;
                }
            }
        }

        private static bool ShouldSpinBlades(
            Entity entity,
            ComponentLookup<LocalTransform> transformLookup,
            ComponentLookup<UnitAirComponent> airStateLookup)
        {
            if (!airStateLookup.HasComponent(entity))
                return false;

            UnitAirComponent airState = airStateLookup[entity];
            if (airState.Airborne != 0 ||
                airState.TakeoffRolling != 0 ||
                airState.LandingRolling != 0)
            {
                return true;
            }

            if (airState.HomeInitialized == 0 || !transformLookup.HasComponent(entity))
                return false;

            return transformLookup[entity].Position.y > airState.HomePosition.y + FlyingHeightEpsilon;
        }

        private static bool TryGetCurrentVisualKind(
            Entity entity,
            ComponentLookup<UnitRenderVisualComponent> visualStateLookup,
            out UnitRenderVisualKind currentVisual)
        {
            currentVisual = UnitRenderVisualKind.Detail;
            if (!visualStateLookup.HasComponent(entity))
                return false;

            currentVisual = (UnitRenderVisualKind)visualStateLookup[entity].Current;
            if (currentVisual == UnitRenderVisualKind.Unknown)
                currentVisual = UnitRenderVisualKind.Detail;
            return true;
        }

        private static int MarkBakedBlades(
            BufferLookup<UnitHelicopterBladeReference> bladeLookup,
            Entity entity,
            NativeHashSet<Entity> rotatedBlades)
        {
            if (!bladeLookup.HasBuffer(entity))
                return 0;

            DynamicBuffer<UnitHelicopterBladeReference> blades = bladeLookup[entity];
            int markedCount = 0;
            for (int i = 0; i < blades.Length; i++)
            {
                UnitHelicopterBladeReference bladeRef = blades[i];
                if (rotatedBlades.Add(bladeRef.Blade))
                    markedCount++;
            }

            return markedCount;
        }

        private static int RotateBladeDescendants(
            EntityManager em,
            BufferLookup<Child> childLookup,
            Entity root,
            float radians,
            NativeHashSet<Entity> rotatedBlades)
        {
            if (root == Entity.Null || !em.Exists(root))
                return 0;

            int spunCount = 0;
            using var stack = new NativeList<Entity>(Allocator.Temp);
            stack.Add(root);
            while (stack.Length > 0)
            {
                Entity current = stack[stack.Length - 1];
                stack.RemoveAt(stack.Length - 1);

                FixedString64Bytes name = em.GetName(current);
                if (TryGetBladeAxis(name.ToString(), out byte axis) &&
                    em.HasComponent<LocalTransform>(current) &&
                    rotatedBlades.Add(current))
                {
                    LocalTransform transform = em.GetComponentData<LocalTransform>(current);
                    transform.Rotation = math.mul(transform.Rotation, CreateBladeDeltaRotation(axis, radians));
                    em.SetComponentData(current, transform);
                    spunCount++;
                }

                if (!childLookup.HasBuffer(current))
                    continue;

                DynamicBuffer<Child> children = childLookup[current];
                for (int i = 0; i < children.Length; i++)
                    stack.Add(children[i].Value);
            }

            return spunCount;
        }

        private static quaternion CreateBladeDeltaRotation(byte axis, float radians)
        {
            if (axis == 0)
                return quaternion.AxisAngle(math.right(), radians);
            if (axis == 1)
                return quaternion.AxisAngle(math.up(), radians);
            return quaternion.AxisAngle(math.forward(), radians);
        }

        private static bool TryGetBladeAxis(string name, out byte axis)
        {
            axis = 0;
            if (string.IsNullOrEmpty(name) || !name.Contains("Blade", System.StringComparison.Ordinal))
                return false;
            if (name.EndsWith("_X", System.StringComparison.Ordinal))
            {
                axis = 0;
                return true;
            }
            if (name.EndsWith("_Y", System.StringComparison.Ordinal))
            {
                axis = 1;
                return true;
            }
            if (name.EndsWith("_Z", System.StringComparison.Ordinal))
            {
                axis = 2;
                return true;
            }

            return false;
        }

        private static bool IsHelicopterDiagnosticCandidate(
            EntityManager em,
            BufferLookup<Child> childLookup,
            Entity entity,
            BufferLookup<UnitHelicopterBladeReference> bladeLookup,
            ComponentLookup<UnitDetailedVisualReference> detailLookup,
            ComponentLookup<UnitModelInstanceReference> modelLookup,
            ComponentLookup<UnitMidLodInstanceReference> midLookup,
            ComponentLookup<UnitLowLodInstanceReference> lowLookup,
            ComponentLookup<UnitSourcePrefabKey> sourceLookup)
        {
            if (sourceLookup.HasComponent(entity) &&
                sourceLookup[entity].Value.ToString().Contains("Helicopter", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (bladeLookup.HasBuffer(entity) && bladeLookup[entity].Length > 0)
                return true;

            if (detailLookup.HasComponent(entity) && CountBladeDescendants(em, childLookup, detailLookup[entity].Root) > 0)
                return true;
            if (modelLookup.HasComponent(entity) && CountBladeDescendants(em, childLookup, modelLookup[entity].Instance) > 0)
                return true;
            if (midLookup.HasComponent(entity) && CountBladeDescendants(em, childLookup, midLookup[entity].Instance) > 0)
                return true;
            if (lowLookup.HasComponent(entity) && CountBladeDescendants(em, childLookup, lowLookup[entity].Instance) > 0)
                return true;

            return false;
        }

        private static void LogHelicopterBladeDiagnostic(
            EntityManager em,
            BufferLookup<Child> childLookup,
            Entity entity,
            BufferLookup<UnitHelicopterBladeReference> bladeLookup,
            ComponentLookup<UnitDetailedVisualReference> detailLookup,
            ComponentLookup<UnitModelInstanceReference> modelLookup,
            ComponentLookup<UnitMidLodInstanceReference> midLookup,
            ComponentLookup<UnitLowLodInstanceReference> lowLookup,
            ComponentLookup<UnitSourcePrefabKey> sourceLookup,
            ComponentLookup<UnitDisplayInfo> displayLookup,
            ComponentLookup<UnitRenderVisualComponent> visualStateLookup,
            ComponentLookup<UnitAirMovement> airLookup,
            ComponentLookup<UnitAirComponent> airStateLookup,
            ComponentLookup<LocalTransform> transformLookup,
            bool shouldSpin,
            int detailRotated,
            int modelRotated,
            int bakedRotated,
            float deltaTime)
        {
            string source = sourceLookup.HasComponent(entity) ? sourceLookup[entity].Value.ToString() : "<none>";
            string display = displayLookup.HasComponent(entity) ? displayLookup[entity].Name.ToString() : "<none>";
            string visual = visualStateLookup.HasComponent(entity)
                ? $"{(UnitRenderVisualKind)visualStateLookup[entity].Current}->{(UnitRenderVisualKind)visualStateLookup[entity].Desired}"
                : "<none>";
            bool hasAir = airLookup.HasComponent(entity);
            string airState = FormatAirState(entity, airStateLookup, transformLookup);
            int bakedCount = bladeLookup.HasBuffer(entity) ? bladeLookup[entity].Length : 0;
            Entity detailRoot = detailLookup.HasComponent(entity) ? detailLookup[entity].Root : Entity.Null;
            Entity modelRoot = modelLookup.HasComponent(entity) ? modelLookup[entity].Instance : Entity.Null;
            Entity midRoot = midLookup.HasComponent(entity) ? midLookup[entity].Instance : Entity.Null;
            Entity lowRoot = lowLookup.HasComponent(entity) ? lowLookup[entity].Instance : Entity.Null;

            Debug.Log(
                "[HeliBladeDiag] " +
                $"unit={FormatEntity(entity)} source={source} display={display} air={hasAir} spin={shouldSpin} state={airState} visual={visual} dt={deltaTime:F4} " +
                $"detail={FormatRoot(em, childLookup, detailRoot)} detailRot={detailRotated} " +
                $"model={FormatRoot(em, childLookup, modelRoot)} modelRot={modelRotated} " +
                $"mid={FormatRoot(em, childLookup, midRoot)} " +
                $"low={FormatRoot(em, childLookup, lowRoot)} " +
                $"bakedBlades={bakedCount} bakedRot={bakedRotated}");
        }

        private static string FormatAirState(
            Entity entity,
            ComponentLookup<UnitAirComponent> airStateLookup,
            ComponentLookup<LocalTransform> transformLookup)
        {
            if (!airStateLookup.HasComponent(entity))
                return "<none>";

            UnitAirComponent airState = airStateLookup[entity];
            float currentY = transformLookup.HasComponent(entity) ? transformLookup[entity].Position.y : float.NaN;
            return $"homeInit={airState.HomeInitialized}:airborne={airState.Airborne}:takeoff={airState.TakeoffRolling}:landing={airState.LandingRolling}:returning={airState.ReturningHome}:y={currentY:F2}:homeY={airState.HomePosition.y:F2}";
        }

        private static string FormatRoot(EntityManager em, BufferLookup<Child> childLookup, Entity root)
        {
            if (root == Entity.Null)
                return "null";
            if (!em.Exists(root))
                return $"{FormatEntity(root)} missing";

            string name = em.GetName(root).ToString();
            bool disabled = em.HasComponent<Disabled>(root);
            bool hidden = em.HasComponent<DisableRendering>(root);
            bool culled = em.HasComponent<UnitRenderBudgetCulledTag>(root);
            int directChildren = childLookup.HasBuffer(root) ? childLookup[root].Length : 0;
            int bladeCount = CountBladeDescendants(em, childLookup, root);
            return $"{FormatEntity(root)}:{name}:disabled={disabled}:hidden={hidden}:culled={culled}:children={directChildren}:blades={bladeCount}";
        }

        private static int CountBladeDescendants(EntityManager em, BufferLookup<Child> childLookup, Entity root)
        {
            if (root == Entity.Null || !em.Exists(root))
                return 0;

            int count = 0;
            using var stack = new NativeList<Entity>(Allocator.Temp);
            stack.Add(root);
            while (stack.Length > 0)
            {
                Entity current = stack[stack.Length - 1];
                stack.RemoveAt(stack.Length - 1);

                FixedString64Bytes name = em.GetName(current);
                if (TryGetBladeAxis(name.ToString(), out _))
                    count++;

                if (!childLookup.HasBuffer(current))
                    continue;

                DynamicBuffer<Child> children = childLookup[current];
                for (int i = 0; i < children.Length; i++)
                    stack.Add(children[i].Value);
            }

            return count;
        }

        private static string FormatEntity(Entity entity)
        {
            return $"{entity.Index}:{entity.Version}";
        }

        [BurstCompile]
        [WithNone(typeof(UnitDeathAnimationComponent))]
        private partial struct RotateBakedBladeReferencesJob : IJobEntity
        {
            public float Radians;
            [ReadOnly] public BufferLookup<UnitHelicopterBladeReference> BladeLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

            private void Execute(Entity entity, in UnitAirMovement airMovement, in UnitAirComponent airState)
            {
                if (!BladeLookup.HasBuffer(entity) || !ShouldSpinBlades(entity, airState))
                    return;

                DynamicBuffer<UnitHelicopterBladeReference> blades = BladeLookup[entity];
                for (int i = 0; i < blades.Length; i++)
                {
                    UnitHelicopterBladeReference bladeRef = blades[i];
                    if (!TransformLookup.HasComponent(bladeRef.Blade))
                        continue;

                    LocalTransform transform = TransformLookup[bladeRef.Blade];
                    transform.Rotation = math.mul(transform.Rotation, CreateBladeDeltaRotation(bladeRef.Axis, Radians));
                    TransformLookup[bladeRef.Blade] = transform;
                }
            }

            private bool ShouldSpinBlades(Entity entity, in UnitAirComponent airState)
            {
                if (airState.Airborne != 0 ||
                    airState.TakeoffRolling != 0 ||
                    airState.LandingRolling != 0)
                {
                    return true;
                }

                if (airState.HomeInitialized == 0 || !TransformLookup.HasComponent(entity))
                    return false;

                return TransformLookup[entity].Position.y > airState.HomePosition.y + FlyingHeightEpsilon;
            }
        }
    }
}
