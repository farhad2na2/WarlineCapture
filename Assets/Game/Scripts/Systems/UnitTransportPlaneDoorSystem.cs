using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitTransportPlaneDoorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitTransportPlaneDoorState>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (RefRW<UnitTransportPlaneDoorState> doorState in SystemAPI.Query<RefRW<UnitTransportPlaneDoorState>>())
            doorState.ValueRW.TargetOpen = 0;

        ComponentLookup<UnitTransportPlaneDoorState> doorStateLookup = SystemAPI.GetComponentLookup<UnitTransportPlaneDoorState>();
        ComponentLookup<UnitAirMovement> airMovementLookup = SystemAPI.GetComponentLookup<UnitAirMovement>(true);
        ComponentLookup<UnitAirComponent> airComponentLookup = SystemAPI.GetComponentLookup<UnitAirComponent>(true);
        ComponentLookup<LocalTransform> localTransformReadLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        foreach (RefRO<UnitTransportBoardingTarget> boarding in SystemAPI
                     .Query<RefRO<UnitTransportBoardingTarget>>()
                     .WithNone<Disabled>())
        {
            Entity transport = boarding.ValueRO.Transport;
            if (!doorStateLookup.HasComponent(transport) ||
                !IsTransportLandedForDoor(
                    transport,
                    airMovementLookup,
                    airComponentLookup,
                    localTransformReadLookup))
            {
                continue;
            }

            UnitTransportPlaneDoorState doorState = doorStateLookup[transport];
            doorState.TargetOpen = 1;
            doorStateLookup[transport] = doorState;
        }

        foreach (var (request, entity) in SystemAPI
                     .Query<RefRW<UnitTransportPlaneDoorOpenRequest>>()
                     .WithEntityAccess())
        {
            if (!doorStateLookup.HasComponent(entity))
            {
                ecb.RemoveComponent<UnitTransportPlaneDoorOpenRequest>(entity);
                continue;
            }

            UnitTransportPlaneDoorState doorState = doorStateLookup[entity];
            doorState.TargetOpen = 1;
            doorStateLookup[entity] = doorState;

            UnitTransportPlaneDoorOpenRequest doorRequest = request.ValueRO;
            doorRequest.RemainingSeconds -= deltaTime;
            if (doorRequest.RemainingSeconds <= 0f)
                ecb.RemoveComponent<UnitTransportPlaneDoorOpenRequest>(entity);
            else
                request.ValueRW = doorRequest;
        }

        foreach (var (_, entity) in SystemAPI
                     .Query<RefRO<UnitTransportAirdropRequest>>()
                     .WithEntityAccess())
        {
            if (!doorStateLookup.HasComponent(entity))
                continue;

            UnitTransportPlaneDoorState doorState = doorStateLookup[entity];
            doorState.TargetOpen = 1;
            doorStateLookup[entity] = doorState;
        }

        ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
        state.Dependency = new UpdateDoorJob
        {
            DeltaTime = deltaTime,
            TransformLookup = transformLookup
        }.Schedule(state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static bool IsTransportLandedForDoor(
        Entity transport,
        ComponentLookup<UnitAirMovement> airMovementLookup,
        ComponentLookup<UnitAirComponent> airComponentLookup,
        ComponentLookup<LocalTransform> localTransformLookup)
    {
        if (!airMovementLookup.HasComponent(transport))
            return true;

        if (!airComponentLookup.HasComponent(transport) || !localTransformLookup.HasComponent(transport))
            return false;

        UnitAirComponent airState = airComponentLookup[transport];
        LocalTransform transform = localTransformLookup[transport];
        float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
        bool physicallyGrounded = transform.Position.y <= groundY + TransportBoardingData.AirBoardingGroundedHeightTolerance;
        return airState.Airborne == 0 &&
               airState.TakeoffRolling == 0 &&
               airState.LandingRolling == 0 &&
               physicallyGrounded;
    }

    [BurstCompile]
    private partial struct UpdateDoorJob : IJobEntity
    {
        public float DeltaTime;
        [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

        private void Execute(
            ref UnitTransportPlaneDoorState state,
            in UnitTransportPlaneDoorReference reference)
        {
            if (reference.DoorEntity == Entity.Null || !TransformLookup.HasComponent(reference.DoorEntity))
                return;

            float target = state.TargetOpen != 0 ? 1f : 0f;
            float duration = state.TargetOpen != 0
                ? math.max(0.01f, reference.OpenSeconds)
                : math.max(0.01f, reference.CloseSeconds);
            float step = DeltaTime / duration;
            state.Open01 = math.saturate(state.Open01 + (target > state.Open01 ? step : -step));

            LocalTransform doorTransform = TransformLookup[reference.DoorEntity];
            doorTransform.Rotation = math.slerp(reference.ClosedLocalRotation, reference.OpenLocalRotation, state.Open01);
            TransformLookup[reference.DoorEntity] = doorTransform;
        }
    }
}
