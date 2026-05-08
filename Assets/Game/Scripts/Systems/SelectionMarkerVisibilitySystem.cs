using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false);

        state.Dependency = new UpdateSelectionMarkerJob
        {
            SelectedLookup = selectedLookup,
            TransformLookup = transformLookup
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    [WithAll(typeof(SelectionMarkerTag))]
    private partial struct UpdateSelectionMarkerJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SelectedUnitTag> SelectedLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

        public void Execute(Entity entity, in Parent parent, in SelectionMarkerVisualChild visualChild)
        {
            bool selected = SelectedLookup.HasComponent(parent.Value);

            if (TransformLookup.HasComponent(entity))
            {
                LocalTransform markerTransform = TransformLookup[entity];
                markerTransform.Scale = 1f;
                TransformLookup[entity] = markerTransform;
            }

            if (!TransformLookup.HasComponent(visualChild.Value))
                return;

            LocalTransform childTransform = TransformLookup[visualChild.Value];
            childTransform.Scale = selected ? visualChild.VisibleScale : 0f;
            TransformLookup[visualChild.Value] = childTransform;
        }
    }
}
