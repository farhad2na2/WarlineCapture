#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Tests.Editor
{
    // Exact saved original method body; wrapper and minimal fields only.
    internal struct GridPathMovementOriginalOracle
    {
        private const int InfantryMaxWaypointAdvancesPerFrame = 8;
        private const float InfantryGroupFinalStopDistanceCells = 1.25f;
        public float DeltaTime;
        public GridConfig Grid;
        public NativeArray<int2> Pool;
        public void Run(ref LocalTransform transform, ref UnitGrid unitGrid, ref UnitPathFollow follow,
            ref UnitVehicleKinematics kinematics, in UnitMove move, in UnitPathRange range, float speed, bool grouped) =>
            MoveInfantryAlongPath(ref transform, ref unitGrid, ref follow, ref kinematics, move, range, speed, grouped);
        private void MoveInfantryAlongPath(
            ref LocalTransform transform,
            ref UnitGrid unitGrid,
            ref UnitPathFollow follow,
            ref UnitVehicleKinematics vehicleKinematics,
            in UnitMove move,
            in UnitPathRange range,
            float speed,
            bool groupedManualMove)
        {
            vehicleKinematics.CurrentSpeed = 0f;
            vehicleKinematics.StallSeconds = 0f;

            float remainingDistance = math.max(0f, speed * DeltaTime);
            float arriveDistance = move.ArriveDistance;
            float arriveDistSq = arriveDistance * arriveDistance;
            float groupedFinalStopDistance = math.max(move.ArriveDistance, Grid.CellSize * InfantryGroupFinalStopDistanceCells);
            float groupedFinalStopDistSq = groupedFinalStopDistance * groupedFinalStopDistance;

            for (int i = 0; i < InfantryMaxWaypointAdvancesPerFrame && remainingDistance > 0f; i++)
            {
                if ((uint)follow.PathIndex >= (uint)range.Length)
                    return;

                int poolIndex = range.Start + follow.PathIndex;
                if ((uint)poolIndex >= (uint)Pool.Length)
                    return;

                int2 targetCell = Pool[poolIndex];
                bool finalPathStep = follow.PathIndex >= (range.Length - 1);
                int finalPoolIndex = range.Start + range.Length - 1;
                if ((uint)finalPoolIndex >= (uint)Pool.Length)
                    return;

                int2 finalCell = Pool[finalPoolIndex];
                float3 finalTargetPos = GridUtils.CellToWorldCenter(Grid, finalCell);
                finalTargetPos.y = transform.Position.y;
                float3 toFinalTarget = finalTargetPos - transform.Position;
                toFinalTarget.y = 0f;

                if (groupedManualMove && finalPathStep && math.lengthsq(toFinalTarget) <= groupedFinalStopDistSq)
                {
                    follow.PathIndex = range.Length;
                    int2 settledCell = GridUtils.WorldToCell(Grid, transform.Position);
                    if (GridUtils.InBounds(settledCell, Grid.Width, Grid.Height) && !settledCell.Equals(unitGrid.Cell))
                        unitGrid.Cell = settledCell;
                    return;
                }

                float3 targetPos = GridUtils.CellToWorldCenter(Grid, targetCell);
                targetPos.y = transform.Position.y;
                float3 toTarget = targetPos - transform.Position;
                toTarget.y = 0f;
                float distSq = math.lengthsq(toTarget);
                if (distSq <= arriveDistSq)
                {
                    unitGrid.Cell = targetCell;
                    follow.PathIndex++;
                    continue;
                }

                float dist = math.sqrt(distSq);
                if (dist <= 0.0001f)
                {
                    unitGrid.Cell = targetCell;
                    follow.PathIndex++;
                    continue;
                }

                float3 dir = toTarget / dist;
                if (remainingDistance >= dist)
                {
                    transform.Position = targetPos;
                    transform.Rotation = quaternion.LookRotationSafe(dir, math.up());
                    remainingDistance -= dist;
                    unitGrid.Cell = targetCell;
                    follow.PathIndex++;
                    continue;
                }

                transform.Position += dir * remainingDistance;
                transform.Rotation = quaternion.LookRotationSafe(dir, math.up());
                remainingDistance = 0f;
            }
        }

    }
}
#endif
