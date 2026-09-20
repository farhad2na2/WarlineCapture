using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    /// <summary>
    /// One-shot-safe runtime pass. Building roots already at grade no-op
    /// because their Y falls outside the stale raised band. Resident
    /// RenderOnly plates no-op once their world Y leaves the raised band.
    /// Game.Runtime must not reference Unity.Entities.Graphics; the visible
    /// sand tiles and statues carry LocalTransform, so the resident pass
    /// does not filter on MaterialMeshInfo.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(OperationMapBuildingDestructionSystem))]
    public partial struct CityCrossroadsFloatingShelfBuildingCorrectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OperationMapBuildingComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new CorrectRaisedShelfBuildingsJob().ScheduleParallel(state.Dependency);
            state.Dependency = new CorrectRaisedResidentVisualsJob().ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(OperationMapBuildingComponent))]
        private partial struct CorrectRaisedShelfBuildingsJob : IJobEntity
        {
            public void Execute(ref LocalTransform transform)
            {
                CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref transform);
            }
        }

        /// <summary>
        /// Drops RenderOnly SubScene meshes (sand tiles, statues, leftover
        /// hill props) whose world XZ sits in a shelf AABB. Local Y must also
        /// be raised so parented children with local Y≈0 follow the corrected
        /// parent instead of being lowered twice.
        /// </summary>
        [BurstCompile]
        [WithNone(
            typeof(OperationMapBuildingComponent),
            typeof(UnitMove),
            typeof(UnitAirComponent),
            typeof(OperationMapRenderProxySlotComponent),
            typeof(OperationMapAuthoredVehiclePresentation))]
        private partial struct CorrectRaisedResidentVisualsJob : IJobEntity
        {
            public void Execute(ref LocalTransform transform, ref LocalToWorld localToWorld)
            {
                if (!CityCrossroadsFloatingShelfBuildingCorrection.TryCorrectResidentVisual(
                        ref transform,
                        localToWorld))
                {
                    return;
                }

                CityCrossroadsFloatingShelfBuildingCorrection.ApplyResidentWorldDelta(
                    ref localToWorld,
                    CityCrossroadsFloatingShelfBuildingCorrection.ResidentHeightDelta);
            }
        }
    }
}
