using System;

namespace Game.Runtime.Movement
{
    public enum GridPathRangeDecision : byte { NoWork, InvalidRange, Ready }
    public enum GridPathOccupancyDecision : byte { Wait, Repath }
    public enum InfantryWaypointAction : byte { Stop, Advance, PartialMove, GroupedStop }

    // Value-only state. Pathfinding, grid conversion, legality/occupancy sampling,
    // vehicle steering, quaternion construction and ECS effects belong to adapters.
    public struct InfantryPathState
    {
        public float X, Y, Z, RemainingDistance;
        public int CellX, CellZ, PathIndex;
    }

    public struct InfantryWaypointInput
    {
        public int PathLength, TargetCellX, TargetCellZ;
        public bool PoolIndexValid, FinalPoolIndexValid, GroupedManualMove;
        public float TargetX, TargetZ, FinalTargetX, FinalTargetZ, ArriveDistance, CellSize;
    }

    public struct InfantryWaypointResult
    {
        public InfantryPathState State;
        public InfantryWaypointAction Action;
        public bool WriteFacing, SettleCurrentCell;
        public float DirectionX, DirectionY, DirectionZ;
    }

    public static class GridPathMovementKernel
    {
        public const int InfantryMaxWaypointAdvancesPerFrame = 8;
        public const float InfantryOccupiedRepathDelaySeconds = 0.35f;
        public const float InfantryGroupFinalStopDistanceCells = 1.25f;

        public static GridPathRangeDecision EvaluateRange(int start, int length, int pathIndex, int poolLength)
        {
            if (length <= 0 || (uint)pathIndex >= (uint)length) return GridPathRangeDecision.NoWork;
            if (start < 0 || start > poolLength || length > poolLength - start) return GridPathRangeDecision.InvalidRange;
            return GridPathRangeDecision.Ready;
        }

        public static bool ShouldUseGroupedManualStop(bool isVehicle, bool campaignGuidedMove,
            bool manualMoveGroupMember, bool hasBoardingTarget) =>
            !isVehicle && !campaignGuidedMove && manualMoveGroupMember && !hasBoardingTarget;

        public static bool ShouldBlockPathCell(bool campaignGuidedMove, bool cellBlocked) =>
            !campaignGuidedMove && cellBlocked;

        // Call only after the adapter resolves footprint overlap and all passable occupant policies.
        // Grid/blocker and occupant queries retain their original evaluation order in production.
        public static GridPathOccupancyDecision AdvanceOccupiedStall(bool isVehicle, float deltaTime,
            ref float currentSpeed, ref float stallSeconds)
        {
            if (!isVehicle)
            {
                currentSpeed = 0f;
                stallSeconds += deltaTime;
                if (stallSeconds < InfantryOccupiedRepathDelaySeconds) return GridPathOccupancyDecision.Wait;
                stallSeconds = 0f;
            }
            return GridPathOccupancyDecision.Repath;
        }

        public static float BeginInfantryDistance(float speed, float deltaTime) => Max(0f, speed * deltaTime);

        public static InfantryWaypointResult StepInfantryWaypoint(in InfantryPathState state, in InfantryWaypointInput input)
        {
            var result = new InfantryWaypointResult { State = state };
            if ((uint)state.PathIndex >= (uint)input.PathLength || !input.PoolIndexValid || !input.FinalPoolIndexValid)
                return result;
            bool finalPathStep = state.PathIndex >= input.PathLength - 1;
            var position = new Scalar3(state.X, state.Y, state.Z);
            var toFinal = new Scalar3(input.FinalTargetX, state.Y, input.FinalTargetZ) - position;
            toFinal.Y = 0f;
            float groupedDistance = Max(input.ArriveDistance, input.CellSize * InfantryGroupFinalStopDistanceCells);
            if (input.GroupedManualMove && finalPathStep && LengthSquared(toFinal) <= groupedDistance * groupedDistance)
            {
                result.State.PathIndex = input.PathLength;
                result.Action = InfantryWaypointAction.GroupedStop;
                result.SettleCurrentCell = true;
                return result;
            }
            var target = new Scalar3(input.TargetX, state.Y, input.TargetZ);
            var toTarget = target - position;
            toTarget.Y = 0f;
            float distSq = LengthSquared(toTarget);
            float arriveDistSq = input.ArriveDistance * input.ArriveDistance;
            if (distSq <= arriveDistSq)
            {
                Advance(ref result, input);
                return result;
            }
            float dist = Sqrt(distSq);
            if (dist <= 0.0001f)
            {
                Advance(ref result, input);
                return result;
            }
            var direction = toTarget / dist;
            result.DirectionX = direction.X; result.DirectionY = direction.Y; result.DirectionZ = direction.Z;
            result.WriteFacing = true;
            if (state.RemainingDistance >= dist)
            {
                result.State.X = input.TargetX; result.State.Y = state.Y; result.State.Z = input.TargetZ;
                result.State.RemainingDistance -= dist;
                Advance(ref result, input);
                return result;
            }
            var moved = position + direction * state.RemainingDistance;
            result.State.X = moved.X; result.State.Y = moved.Y; result.State.Z = moved.Z;
            result.State.RemainingDistance = 0f;
            result.Action = InfantryWaypointAction.PartialMove;
            return result;
        }

        private static void Advance(ref InfantryWaypointResult result, in InfantryWaypointInput input)
        {
            result.State.CellX = input.TargetCellX; result.State.CellZ = input.TargetCellZ;
            result.State.PathIndex++;
            result.Action = InfantryWaypointAction.Advance;
        }

        // Scalar Unity.Mathematics semantics, including NaN and signed-zero ties.
        private static float Max(float a, float b) => float.IsNaN(b) || a > b ? a : b;
        // Keep the original float3 operation/field-store boundaries. Combining scalar
        // multiply/add expressions can retain different intermediate precision on Mono.
        private struct Scalar3
        {
            public float X, Y, Z;
            public Scalar3(float x, float y, float z) { X = x; Y = y; Z = z; }
            public static Scalar3 operator -(Scalar3 a, Scalar3 b) => new Scalar3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            public static Scalar3 operator +(Scalar3 a, Scalar3 b) => new Scalar3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
            public static Scalar3 operator *(Scalar3 a, float b) => new Scalar3(a.X * b, a.Y * b, a.Z * b);
            public static Scalar3 operator /(Scalar3 a, float b) => new Scalar3(a.X / b, a.Y / b, a.Z / b);
        }
        private static float LengthSquared(Scalar3 value) => value.X * value.X + value.Y * value.Y + value.Z * value.Z;
        private static float Sqrt(float value) => (float)Math.Sqrt((float)value);
    }
}
