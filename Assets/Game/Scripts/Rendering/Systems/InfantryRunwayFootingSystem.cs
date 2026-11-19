using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Rendering
{
    [UpdateInGroup(typeof(TransformSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(LocalToWorldSystem))]
    public partial struct InfantryRunwayFootingSystem : ISystem
    {
        struct RunwayBounds
        {
            public float2 Min, Max;
            public int Start, End;
        }

        EntityQuery runways;
        bool hadRunways;

        public void OnCreate(ref SystemState state)
        {
            runways = state.GetEntityQuery(ComponentType.ReadOnly<RunwayRenderTriangle>(), ComponentType.ReadOnly<LocalToWorld>());
            state.RequireForUpdate<InfantryVisualFooting>();
        }

        public void OnUpdate(ref SystemState state)
        {
            bool loaded = !runways.IsEmptyIgnoreFilter;
            if (!loaded && !hadRunways) return;
            hadRunways = loaded;
            var surfaces = new NativeList<RunwayRenderTriangle>(state.WorldUpdateAllocator);
            var bounds = new NativeList<RunwayBounds>(state.WorldUpdateAllocator);
            foreach (var (mesh, transform) in SystemAPI.Query<DynamicBuffer<RunwayRenderTriangle>, RefRO<LocalToWorld>>())
            {
                var matrix = transform.ValueRO.Value;
                var span = new RunwayBounds { Min = float.MaxValue, Max = float.MinValue, Start = surfaces.Length };
                foreach (var triangle in mesh)
                {
                    var t = new RunwayRenderTriangle {
                        A = math.transform(matrix, triangle.A), B = math.transform(matrix, triangle.B), C = math.transform(matrix, triangle.C)
                    };
                    surfaces.Add(t);
                    span.Min = math.min(span.Min, math.min(t.A.xz, math.min(t.B.xz,t.C.xz)));
                    span.Max = math.max(span.Max, math.max(t.A.xz, math.max(t.B.xz,t.C.xz)));
                }
                span.End = surfaces.Length;
                if (span.Start < span.End) bounds.Add(span);
            }
            state.Dependency = new ApplyFooting
            {
                Bounds = bounds.AsArray(),
                Surfaces = surfaces.AsArray(),
                Transforms = SystemAPI.GetComponentLookup<LocalTransform>(),
                Parents = SystemAPI.GetComponentLookup<Parent>(true),
                WorldMatrices = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                Detailed = SystemAPI.GetComponentLookup<UnitDetailedVisualReference>(true),
                Model = SystemAPI.GetComponentLookup<UnitModelInstanceReference>(true),
                Mid = SystemAPI.GetComponentLookup<UnitMidLodInstanceReference>(true),
                Low = SystemAPI.GetComponentLookup<UnitLowLodInstanceReference>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(UnitAirMovement))]
        partial struct ApplyFooting : IJobEntity
        {
            [ReadOnly] public NativeArray<RunwayBounds> Bounds;
            [ReadOnly] public NativeArray<RunwayRenderTriangle> Surfaces;
            public ComponentLookup<LocalTransform> Transforms;
            [ReadOnly] public ComponentLookup<Parent> Parents;
            [ReadOnly] public ComponentLookup<LocalToWorld> WorldMatrices;
            [ReadOnly] public ComponentLookup<UnitDetailedVisualReference> Detailed;
            [ReadOnly] public ComponentLookup<UnitModelInstanceReference> Model;
            [ReadOnly] public ComponentLookup<UnitMidLodInstanceReference> Mid;
            [ReadOnly] public ComponentLookup<UnitLowLodInstanceReference> Low;

            void Execute(Entity unit, ref InfantryVisualFooting footing, in UnitSurfaceComponent ground, in UnitModelLocalTransform pose)
            {
                if (!Transforms.HasComponent(unit)) return;
                var owner = Transforms[unit];
                float lift = 0;
                if (ground.HasSurface != 0)
                {
                    foreach (var bounds in Bounds)
                    {
                        if (math.any(owner.Position.xz < bounds.Min) || math.any(owner.Position.xz > bounds.Max)) continue;
                        for (int i = bounds.Start; i < bounds.End; i++)
                        {
                            var t = Surfaces[i];
                            float2 p = owner.Position.xz-t.A.xz, b = t.B.xz-t.A.xz, c = t.C.xz-t.A.xz;
                            float det = b.x*c.y-b.y*c.x;
                            if (math.abs(det)<1e-6f) continue;
                            float u = (p.x*c.y-p.y*c.x)/det, v = (b.x*p.y-b.y*p.x)/det;
                            if (u < -1e-5f || v < -1e-5f || u+v > 1.00001f) continue;
                            float height = t.A.y + u*(t.B.y-t.A.y) + v*(t.C.y-t.A.y);
                            float delta = height-ground.LastSampledHeight;
                            // A separate level beneath the airfield must retain its own footing.
                            if (delta <= 2f) lift = math.max(lift, delta);
                        }
                    }
                }
                if (lift == 0 && footing.Lift == 0) return;
                footing.Lift = lift;
                if (Detailed.TryGetComponent(unit, out var d)) PositionModel(d.Root, unit, owner, pose.Position, lift);
                if (Model.TryGetComponent(unit, out var m)) PositionModel(m.Instance, unit, owner, pose.Position, lift);
                if (Mid.TryGetComponent(unit, out var mid)) PositionModel(mid.Instance, unit, owner, pose.Position, lift);
                if (Low.TryGetComponent(unit, out var low)) PositionModel(low.Instance, unit, owner, pose.Position, lift);
            }

            void PositionModel(Entity model, Entity unit, LocalTransform owner, float3 basePosition, float lift)
            {
                if (model == unit || !Parents.TryGetComponent(model,out var parent) || !Transforms.TryGetComponent(model,out var local)) return;
                float3 up = new float3(0, lift, 0);
                if (parent.Value == unit)
                    up = math.rotate(math.inverse(owner.Rotation), up) / math.max(owner.Scale, .0001f);
                else if (WorldMatrices.TryGetComponent(parent.Value, out var parentMatrix))
                    up = math.rotate(math.inverse(parentMatrix.Value), up);
                else return;
                var position = basePosition + up;
                if (math.distancesq(local.Position, position) < 1e-10f) return;
                local.Position = position;
                Transforms[model] = local;
            }
        }
    }
}
