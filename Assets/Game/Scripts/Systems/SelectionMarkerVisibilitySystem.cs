using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct SelectionMarkerVisibilitySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SelectionMarkerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var selectedLookup = SystemAPI.GetComponentLookup<SelectedUnitTag>(true);
        var healthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
        var passengerLookup = SystemAPI.GetComponentLookup<UnitTransportPassenger>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false);
        var postTransformLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(false);

        state.Dependency = new UpdateSelectionMarkerJob
        {
            SelectedLookup = selectedLookup,
            HealthLookup = healthLookup,
            PassengerLookup = passengerLookup,
            TransformLookup = transformLookup,
            PostTransformLookup = postTransformLookup
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new UpdateSelectionObjectOutlineJob
        {
            SelectedLookup = selectedLookup,
            HealthLookup = healthLookup,
            PassengerLookup = passengerLookup
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    [WithAll(typeof(SelectionMarkerTag))]
    private partial struct UpdateSelectionMarkerJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SelectedUnitTag> SelectedLookup;
        [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
        [ReadOnly] public ComponentLookup<UnitTransportPassenger> PassengerLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<PostTransformMatrix> PostTransformLookup;

        public void Execute(Entity entity, in Parent parent, in SelectionMarkerVisualChild visualChild)
        {
            bool visible =
                SelectedLookup.HasComponent(parent.Value) &&
                HealthLookup.HasComponent(parent.Value) &&
                HealthLookup[parent.Value].Current > 0 &&
                !PassengerLookup.HasComponent(parent.Value);

            if (TransformLookup.HasComponent(entity))
            {
                LocalTransform markerTransform = TransformLookup[entity];
                markerTransform.Scale = 1f;
                TransformLookup[entity] = markerTransform;
            }

            if (!TransformLookup.HasComponent(visualChild.Value))
                return;

            LocalTransform childTransform = TransformLookup[visualChild.Value];
            if (PostTransformLookup.HasComponent(visualChild.Value))
            {
                childTransform.Scale = visible ? 1f : 0f;
                TransformLookup[visualChild.Value] = childTransform;
                float scale = visible ? math.max(0f, visualChild.VisibleScale) : 0f;
                PostTransformLookup[visualChild.Value] = new PostTransformMatrix
                {
                    Value = float4x4.Scale(new float3(scale, 1f, scale))
                };
                return;
            }

            childTransform.Scale = visible ? visualChild.VisibleScale : 0f;
            TransformLookup[visualChild.Value] = childTransform;
        }
    }

    [BurstCompile]
    [WithAll(typeof(SelectionObjectOutlineTag))]
    private partial struct UpdateSelectionObjectOutlineJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SelectedUnitTag> SelectedLookup;
        [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
        [ReadOnly] public ComponentLookup<UnitTransportPassenger> PassengerLookup;

        public void Execute(ref LocalTransform transform, in SelectionMarkerOwner owner, in SelectionObjectOutlineVisibleScale visibleScale)
        {
            Entity unit = owner.Value;
            bool visible =
                SelectedLookup.HasComponent(unit) &&
                HealthLookup.HasComponent(unit) &&
                HealthLookup[unit].Current > 0 &&
                !PassengerLookup.HasComponent(unit);

            transform.Scale = visible ? math.max(0f, visibleScale.Value) : 0f;
        }
    }
}
