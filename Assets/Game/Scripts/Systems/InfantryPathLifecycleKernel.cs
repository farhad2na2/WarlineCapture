using Game.Runtime.Pathfinding;

namespace Game.Runtime.Movement
{
    public interface IInfantryPathContext : IPathPlacementContext
    {
        int PoolLength { get; }
        PlacementCell PathCell(int index);
        PlacementCell WorldToCell(float x, float y, float z);
        void CellToWorld(PlacementCell cell, out float x, out float z);
        byte Sidewalk(int index);
        bool BoardingTransportOccupies(PlacementIdentity passenger, int cellIndex);
    }
    public struct InfantryPathLifecycleInput
    {
        public PlacementIdentity Entity;
        public PlacementCell Footprint;
        public int PathStart, PathLength;
        public byte Faction;
        public float DeltaTime, CellSize, Speed, WalkSpeed, RoadSpeedMultiplier, ArriveDistance;
        public bool AutoWander, CampaignGuided, ManualGroupMember, HasBoardingTarget;
    }
    public struct InfantryPathLifecycleState
    {
        public InfantryPathState Path;
        public float CurrentSpeed, StallSeconds;
    }
    public struct InfantryPathLifecycleResult
    {
        public bool RequestRepath, WriteFacing;
        public float DirectionX, DirectionY, DirectionZ;
    }
    public struct PathRepathIntent
    {
        public bool Requested;
        public PlacementCell Goal;
    }
    public struct PathCompletionIntent
    {
        public bool Complete, RemoveGuided, ContinueLongDistance, RemoveLongDistance;
        public bool AddManual, RemoveManual;
        public PlacementCell Goal;
    }
    public enum PathResultDisposition : byte { Ignore, Success, Retry, Abandon }
    public struct PathResultIntent
    {
        public PathResultDisposition Disposition;
        public bool AppendPool, SetLongDistance, RemoveLongDistance;
        public int PathStart, PathLength, PathIndex, ResumeFrame;
        public PlacementCell Target, FinalGoal;
        public byte ManualMove;
    }
    // Only infantry path following. Vehicle steering remains in its existing native branch.
    // Legality is sampled for the initial waypoint only, exactly as the production job.
    public static class InfantryPathLifecycleKernel
    {
        public static bool CanOccupyMovementTarget<T>(ref T context, PlacementCell target,
            PlacementCell size, byte faction) where T : struct, IPathPlacementContext
        {
            var clamped = PlacementFootprint.ClampSize(size);
            var min = PlacementFootprint.GetMinCell(target, clamped);
            var max = min + clamped;
            if (min.x < 0 || min.y < 0 || max.x > context.Width || max.y > context.Height) return false;
            for (int y = min.y; y < max.y; y++)
            for (int x = min.x; x < max.x; x++)
            {
                int index = y * context.Width + x;
                if (context.Walkable(index) == 0) return false;
                if (context.HasDynamicBlocked && context.DynamicBlocked(index) &&
                    !(context.HasFriendlyPass && (uint)index < (uint)context.FriendlyPassLength && context.FriendlyPass(index) == faction)) return false;
            }
            return true;
        }
        public static bool IsOnlySelfOccupyingCell<T>(ref T context, PlacementIdentity entity, int cellIndex)
            where T : struct, IPathPlacementContext
        {
            var cell = new PlacementCell(cellIndex % context.Width, cellIndex / context.Width);
            bool foundSelf = false;
            for (int i = 0; i < context.UnitCount; i++)
            {
                if (!PlacementFootprint.ContainsCell(context.UnitCell(i), context.UnitSize(i), cell)) continue;
                if (context.UnitEntity(i) != entity) return false;
                foundSelf = true;
            }
            return foundSelf;
        }
        private static bool IsOnlyGroupAtCell<T>(ref T context, PlacementIdentity entity, int cellIndex)
            where T : struct, IPathPlacementContext
        {
            var cell = new PlacementCell(cellIndex % context.Width, cellIndex / context.Width);
            bool found = false;
            for (int i = 0; i < context.UnitCount; i++)
            {
                if (!PlacementFootprint.ContainsCell(context.UnitCell(i), context.UnitSize(i), cell)) continue;
                if (context.UnitEntity(i) != entity && context.ManualGroup(i) == 0) return false;
                found = true;
            }
            return found;
        }
        private static bool InBounds<T>(ref T context, PlacementCell cell) where T : struct, IPathPlacementContext =>
            cell.x >= 0 && cell.y >= 0 && cell.x < context.Width && cell.y < context.Height;
        private static void SyncCell<T>(ref T context, ref InfantryPathState path) where T : struct, IInfantryPathContext
        {
            var cell = context.WorldToCell(path.X, path.Y, path.Z);
            if (InBounds(ref context, cell)) { path.CellX = cell.x; path.CellZ = cell.y; }
        }
        public static InfantryPathLifecycleResult Step<T>(ref T context, in InfantryPathLifecycleInput input,
            ref InfantryPathLifecycleState state) where T : struct, IInfantryPathContext
        {
            var result = new InfantryPathLifecycleResult();
            var decision = GridPathMovementKernel.EvaluateRange(input.PathStart, input.PathLength, state.Path.PathIndex, context.PoolLength);
            if (decision == GridPathRangeDecision.NoWork) return result;
            if (decision == GridPathRangeDecision.InvalidRange)
            { state.CurrentSpeed = 0f; state.StallSeconds = 0f; result.RequestRepath = true; return result; }
            int poolIndex = input.PathStart + state.Path.PathIndex;
            if ((uint)poolIndex >= (uint)context.PoolLength) return result;
            var target = context.PathCell(poolIndex);
            int targetIndex = target.y * context.Width + target.x;
            bool grouped = GridPathMovementKernel.ShouldUseGroupedManualStop(false, input.CampaignGuided, input.ManualGroupMember, input.HasBoardingTarget);
            SyncCell(ref context, ref state.Path);
            var current = new PlacementCell(state.Path.CellX, state.Path.CellZ);
            if (target.x != current.x || target.y != current.y)
            {
                bool overlaps = PlacementFootprint.Overlaps(current, input.Footprint, target, input.Footprint);
                bool blocked = GridPathMovementKernel.ShouldBlockPathCell(input.CampaignGuided,
                    !overlaps && !CanOccupyMovementTarget(ref context, target, input.Footprint, input.Faction));
                if (blocked)
                { state.CurrentSpeed = 0f; state.StallSeconds = 0f; result.RequestRepath = true; return result; }
                bool occupied = GridPathMovementKernel.ShouldBlockPathCell(input.CampaignGuided, context.Occupied(targetIndex));
                if (occupied)
                {
                    bool self = IsOnlySelfOccupyingCell(ref context, input.Entity, targetIndex);
                    bool group = grouped && IsOnlyGroupAtCell(ref context, input.Entity, targetIndex);
                    bool boarding = context.BoardingTransportOccupies(input.Entity, targetIndex);
                    occupied = !self && !group && !boarding;
                }
                if (!overlaps && occupied)
                {
                    if (GridPathMovementKernel.AdvanceOccupiedStall(false, input.DeltaTime, ref state.CurrentSpeed, ref state.StallSeconds) == GridPathOccupancyDecision.Wait) return result;
                    result.RequestRepath = true; return result;
                }
            }
            var final = context.PathCell(input.PathStart + input.PathLength - 1);
            context.CellToWorld(final, out float finalX, out float finalZ);
            float dx = finalX - state.Path.X, dz = finalZ - state.Path.Z;
            float groupedStop = Max(input.ArriveDistance, input.CellSize * GridPathMovementKernel.InfantryGroupFinalStopDistanceCells);
            if (grouped && state.Path.PathIndex >= input.PathLength - 1 && dx * dx + 0f + dz * dz <= groupedStop * groupedStop)
            {
                state.CurrentSpeed = 0f; state.StallSeconds = 0f; state.Path.PathIndex = input.PathLength;
                SyncCell(ref context, ref state.Path); return result;
            }
            float speed = input.AutoWander ? input.WalkSpeed : input.Speed;
            var worldCell = context.WorldToCell(state.Path.X, state.Path.Y, state.Path.Z);
            if (InBounds(ref context, worldCell) && context.Sidewalk(worldCell.y * context.Width + worldCell.x) != 0) speed *= input.RoadSpeedMultiplier;
            state.CurrentSpeed = 0f; state.StallSeconds = 0f;
            state.Path.RemainingDistance = GridPathMovementKernel.BeginInfantryDistance(speed, input.DeltaTime);
            for (int i = 0; i < GridPathMovementKernel.InfantryMaxWaypointAdvancesPerFrame && state.Path.RemainingDistance > 0f; i++)
            {
                if ((uint)state.Path.PathIndex >= (uint)input.PathLength) return result;
                poolIndex = input.PathStart + state.Path.PathIndex;
                if ((uint)poolIndex >= (uint)context.PoolLength) return result;
                int finalIndex = input.PathStart + input.PathLength - 1;
                if ((uint)finalIndex >= (uint)context.PoolLength) return result;
                target = context.PathCell(poolIndex);
                context.CellToWorld(target, out float targetX, out float targetZ);
                context.CellToWorld(context.PathCell(finalIndex), out finalX, out finalZ);
                var step = GridPathMovementKernel.StepInfantryWaypoint(state.Path, new InfantryWaypointInput
                {
                    PathLength = input.PathLength, PoolIndexValid = true, FinalPoolIndexValid = true,
                    TargetCellX = target.x, TargetCellZ = target.y, TargetX = targetX, TargetZ = targetZ,
                    FinalTargetX = finalX, FinalTargetZ = finalZ, ArriveDistance = input.ArriveDistance,
                    CellSize = input.CellSize, GroupedManualMove = grouped
                });
                state.Path = step.State;
                if (step.WriteFacing)
                { result.WriteFacing = true; result.DirectionX = step.DirectionX; result.DirectionY = step.DirectionY; result.DirectionZ = step.DirectionZ; }
                if (step.SettleCurrentCell) SyncCell(ref context, ref state.Path);
                if (step.Action == InfantryWaypointAction.Stop || step.Action == InfantryWaypointAction.GroupedStop) return result;
            }
            return result;
        }
        public static PathRepathIntent Repath(bool hasLongDistance, PlacementCell finalGoal, bool hasTarget, PlacementCell target) =>
            new PathRepathIntent { Requested = hasLongDistance || hasTarget, Goal = hasLongDistance ? finalGoal : target };
        public static PathCompletionIntent Complete(int pathLength, int pathIndex, bool guided, bool hasLongDistance,
            bool hasGrid, PlacementCell current, PlacementCell finalGoal, byte longDistanceManual, bool hasManual)
        {
            var result = new PathCompletionIntent();
            if (!(pathLength <= 0 || (uint)pathIndex >= (uint)pathLength)) return result;
            result.Complete = true; result.RemoveGuided = guided;
            if (hasLongDistance && hasGrid)
            {
                if (current.x != finalGoal.x || current.y != finalGoal.y)
                {
                    result.ContinueLongDistance = true; result.Goal = finalGoal;
                    result.AddManual = longDistanceManual != 0 && !hasManual;
                    result.RemoveManual = longDistanceManual == 0 && hasManual;
                    return result;
                }
                result.RemoveLongDistance = true;
            }
            result.RemoveManual = true;
            return result;
        }
        public const int FailedManualRetryDelayFrames = 8;
        public static bool MatchingRequest(bool exists, bool hasRequest, PlacementCell current, PlacementCell scheduled) =>
            exists && hasRequest && current.x == scheduled.x && current.y == scheduled.y;
        public static bool SuccessfulResult(byte status, int count) => status == 1 && count > 0;
        public static bool ShouldRetry(bool hasManual, bool hasLongDistance, byte segmented) => hasManual || hasLongDistance || segmented != 0;
        public static int RetryResumeFrame(int frame) => unchecked(frame + FailedManualRetryDelayFrames);
        // Result application is immediate in UnitPathfindingSystem, unlike movement's EndECB intents.
        // Support collection validation and surface metadata remain explicit host adapters.
        public static PathResultIntent ApplyResult(bool matchingRequest, byte status, int count, int poolStart,
            PlacementCell requestGoal, PlacementCell assignedGoal, byte segmented, byte manualMove,
            bool hasManual, bool hasLongDistance, int frame)
        {
            var result = new PathResultIntent { AppendPool = matchingRequest };
            if (!matchingRequest) return result;
            if (SuccessfulResult(status, count))
            {
                result.Disposition = PathResultDisposition.Success;
                result.PathStart = poolStart; result.PathLength = count; result.PathIndex = 0;
                result.Target = assignedGoal; result.FinalGoal = requestGoal; result.ManualMove = manualMove;
                result.SetLongDistance = segmented != 0; result.RemoveLongDistance = segmented == 0 && hasLongDistance;
            }
            else if (ShouldRetry(hasManual, hasLongDistance, segmented))
            {
                result.Disposition = PathResultDisposition.Retry; result.SetLongDistance = segmented != 0;
                result.Target = requestGoal; result.FinalGoal = requestGoal; result.ManualMove = manualMove;
                result.ResumeFrame = RetryResumeFrame(frame);
            }
            else result.Disposition = PathResultDisposition.Abandon;
            return result;
        }
        private static float Max(float a, float b) => float.IsNaN(b) || a > b ? a : b;
    }
}
