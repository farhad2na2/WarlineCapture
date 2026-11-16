using System;

namespace Game.Runtime.Pathfinding
{
    public struct PathRoutingRequest
    {
        public PathSearchRequest Search;
        public int DesiredGoalX, DesiredGoalZ, GoalSearchRadius, MaxCandidateAttempts;
        public byte CheapMode;
        public bool HasModes, ManualMove, SkipAlternateSearch;
    }
    // Initialize from current native metadata: early returns deliberately preserve
    // fields the original job did not touch (including prior expansion/attempt values).
    public struct PathRoutingState
    {
        public int GoalX, GoalZ, FailureCode, ExpansionCount, AlternateAttempts;
        public byte Status, Segmented;
    }

    public struct PathRoutingAttempt
    {
        public int GoalX, GoalZ, SearchEpoch, HeuristicWeight, FailureCode, ExpansionCount;
        public bool Found, SetExpansionCount;
    }
    public interface IPathRoutingObserver { void OnAttempt(in PathRoutingAttempt attempt); }

    public static class GridPathRoutingKernel
    {
        public const int ManualProgressMaxRadius = 16;

        private struct NoObserver : IPathRoutingObserver { public void OnAttempt(in PathRoutingAttempt attempt) { } }
        public static void Route<T>(ref T context, in PathRoutingRequest request, ref PathRoutingState state)
            where T : struct, IPathSearchContext
        {
            var observer = new NoObserver();
            Route(ref context, request, ref state, ref observer);
        }
        public static void Route<T, TObserver>(ref T context, in PathRoutingRequest request, ref PathRoutingState state,
            ref TObserver observer) where T : struct, IPathSearchContext where TObserver : struct, IPathRoutingObserver
        {
            var search = request.Search;
            int startFailure = GridPathSearchKernel.ValidateStart(ref context, search);
            if (startFailure != 0) { state.Status = 0; state.FailureCode = startFailure; return; }
            int gx = search.GoalX, gz = search.GoalZ;
            if (TrySearch(ref context, search, gx, gz, search.SearchEpoch, 1, ref state, ref observer))
            { state.Status = 1; return; }

            bool detailedHauler = request.HasModes && request.CheapMode == 3;
            if (detailedHauler && state.FailureCode == 9 &&
                TrySearch(ref context, search, gx, gz, unchecked(search.SearchEpoch + 1), 2, ref state, ref observer))
            { state.Status = 1; return; }

            int centerX = gx, centerZ = gz;
            if (!GridPathSearchKernel.InBounds(centerX, centerZ, search.Width, search.Height))
            {
                centerX = Clamp(centerX, 0, search.Width - 1);
                centerZ = Clamp(centerZ, 0, search.Height - 1);
            }
            bool finalSegment = request.DesiredGoalX == gx && request.DesiredGoalZ == gz;
            if (finalSegment && GridPathSearchKernel.InBounds(request.DesiredGoalX, request.DesiredGoalZ, search.Width, search.Height))
            { centerX = request.DesiredGoalX; centerZ = request.DesiredGoalZ; }
            int radiusLimit = Math.Min(Math.Max(search.Width, search.Height), request.GoalSearchRadius);
            int attempts = 0;
            if (!request.SkipAlternateSearch)
            {
                for (int radius = 1; radius <= radiusLimit; radius++)
                {
                    int ringLength = Math.Max(1, unchecked(8 * radius));
                    for (int step = 0; step < ringLength; step++)
                    {
                        if (attempts >= request.MaxCandidateAttempts) { radius = radiusLimit + 1; break; }
                        var offset = GridPathSearchKernel.SquareRingOffset(radius, step);
                        int x = unchecked(centerX + offset.X), z = unchecked(centerZ + offset.Z);
                        if (!GridPathSearchKernel.InBounds(x, z, search.Width, search.Height)) continue;
                        if ((x == gx && z == gz) || (x == search.StartX && z == search.StartZ)) continue;
                        if (!CanReachGoal(ref context, search, x, z)) continue;
                        attempts++;
                        if (TrySearch(ref context, search, x, z, unchecked(search.SearchEpoch + attempts + 1), 1, ref state, ref observer))
                        {
                            state.AlternateAttempts = attempts; state.GoalX = x; state.GoalZ = z; state.Status = 1; return;
                        }
                    }
                }
            }
            state.AlternateAttempts = attempts;
            if (request.ManualMove && TryManualProgress(ref context, search, request.DesiredGoalX, request.DesiredGoalZ, out PathSearchCell fallback))
            {
                state.GoalX = fallback.X; state.GoalZ = fallback.Z;
                state.Segmented = 1; state.Status = 1; return;
            }
            state.Status = 0;
            if (state.FailureCode == 0) state.FailureCode = 12;
        }

        private static bool TrySearch<T, TObserver>(ref T context, PathSearchRequest search, int goalX, int goalZ,
            int epoch, int weight, ref PathRoutingState state, ref TObserver observer)
            where T : struct, IPathSearchContext where TObserver : struct, IPathRoutingObserver
        {
            search.GoalX = goalX; search.GoalZ = goalZ; search.SearchEpoch = epoch; search.HeuristicWeight = weight;
            var result = GridPathSearchKernel.TrySearch(ref context, search);
            state.FailureCode = result.FailureCode;
            if (result.SetExpansionCount) state.ExpansionCount = result.ExpansionCount;
            observer.OnAttempt(new PathRoutingAttempt { GoalX = goalX, GoalZ = goalZ, SearchEpoch = epoch,
                HeuristicWeight = weight, Found = result.Found, FailureCode = result.FailureCode,
                ExpansionCount = result.ExpansionCount, SetExpansionCount = result.SetExpansionCount });
            return result.Found;
        }
        private static bool CanReachGoal<T>(ref T context, in PathSearchRequest search, int x, int z)
            where T : struct, IPathSearchContext
        {
            if (!context.IsWalkable(unchecked(z * search.Width + x))) return false;
            if (!context.IsSurfaceCellPathable(x, z)) return false;
            return context.CanPlace(x, z, search.StartX, search.StartZ) && context.IsSurfaceFootprintPathable(x, z);
        }
        public static bool TryManualProgress<T>(ref T context, in PathSearchRequest search, int desiredX, int desiredZ,
            out PathSearchCell fallback) where T : struct, IPathSearchContext
        {
            int bestDistanceSq = int.MaxValue;
            int bestX = search.StartX, bestZ = search.StartZ;
            fallback = new PathSearchCell(bestX, bestZ);
            int maxRadius = Math.Min(ManualProgressMaxRadius, Math.Max(search.Width, search.Height));
            for (int radius = maxRadius; radius >= 1; radius--)
            {
                int ringLength = Math.Max(1, unchecked(8 * radius));
                for (int step = 0; step < ringLength; step++)
                {
                    var offset = GridPathSearchKernel.SquareRingOffset(radius, step);
                    int x = unchecked(search.StartX + offset.X), z = unchecked(search.StartZ + offset.Z);
                    if (!GridPathSearchKernel.InBounds(x, z, search.Width, search.Height)) continue;
                    if (x == search.StartX && z == search.StartZ) continue;
                    if (!CanReachGoal(ref context, search, x, z)) continue;
                    if (!GridPathSearchKernel.HasDirectPath(ref context, search.StartX, search.StartZ, x, z)) continue;
                    int dx = unchecked(x - desiredX), dz = unchecked(z - desiredZ);
                    int distanceSq = unchecked(dx * dx + dz * dz);
                    if (distanceSq >= bestDistanceSq) continue;
                    bestDistanceSq = distanceSq; bestX = x; bestZ = z;
                }
                if (bestX != search.StartX || bestZ != search.StartZ) break;
            }
            if (bestX == search.StartX && bestZ == search.StartZ) return false;
            fallback = new PathSearchCell(bestX, bestZ);
            context.WriteCell(search.StartX, search.StartZ); context.WriteCell(bestX, bestZ);
            return true;
        }
        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
    }
}
