using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    internal struct UnitPathRequestBuffer
    {
        public NativeList<Entity> Entities;
        public NativeList<UnitGrid> UnitGrids;
        public NativeList<UnitPathRequest> Goals;
        public NativeList<UnitFootprint> Footprints;
        public NativeList<UnitMovementBehavior> MovementBehaviors;
        public NativeList<byte> Factions;
        public NativeList<byte> ManualMoves;
        public NativeList<Entity> IgnoredOccupancyEntities;
        public NativeList<int2> IgnoredOccupancyCells;
        public NativeList<int2> IgnoredOccupancySizes;
        public NativeList<int2> AssignedGoals;
        public NativeList<byte> Status;
        public NativeList<int> FailureCodes;
        public NativeList<int> ExpansionCounts;
        public NativeList<byte> Segmented;
        public NativeList<byte> ContinuationMoves;
        public NativeList<byte> CheapSegmentModes;
        public NativeList<byte> AlternateSearchSkipped;
        public NativeList<int> AlternateAttempts;

        public void Initialize()
        {
            int capacity = UnitPathfindingBudget.MaxRequestsPerFrame;
            Entities = new NativeList<Entity>(capacity, Allocator.Persistent);
            UnitGrids = new NativeList<UnitGrid>(capacity, Allocator.Persistent);
            Goals = new NativeList<UnitPathRequest>(capacity, Allocator.Persistent);
            Footprints = new NativeList<UnitFootprint>(capacity, Allocator.Persistent);
            MovementBehaviors = new NativeList<UnitMovementBehavior>(capacity, Allocator.Persistent);
            Factions = new NativeList<byte>(capacity, Allocator.Persistent);
            ManualMoves = new NativeList<byte>(capacity, Allocator.Persistent);
            IgnoredOccupancyEntities = new NativeList<Entity>(capacity, Allocator.Persistent);
            IgnoredOccupancyCells = new NativeList<int2>(capacity, Allocator.Persistent);
            IgnoredOccupancySizes = new NativeList<int2>(capacity, Allocator.Persistent);
            AssignedGoals = new NativeList<int2>(capacity, Allocator.Persistent);
            Status = new NativeList<byte>(capacity, Allocator.Persistent);
            FailureCodes = new NativeList<int>(capacity, Allocator.Persistent);
            ExpansionCounts = new NativeList<int>(capacity, Allocator.Persistent);
            Segmented = new NativeList<byte>(capacity, Allocator.Persistent);
            ContinuationMoves = new NativeList<byte>(capacity, Allocator.Persistent);
            CheapSegmentModes = new NativeList<byte>(capacity, Allocator.Persistent);
            AlternateSearchSkipped = new NativeList<byte>(capacity, Allocator.Persistent);
            AlternateAttempts = new NativeList<int>(capacity, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (Entities.IsCreated) Entities.Dispose();
            if (UnitGrids.IsCreated) UnitGrids.Dispose();
            if (Goals.IsCreated) Goals.Dispose();
            if (Footprints.IsCreated) Footprints.Dispose();
            if (MovementBehaviors.IsCreated) MovementBehaviors.Dispose();
            if (Factions.IsCreated) Factions.Dispose();
            if (ManualMoves.IsCreated) ManualMoves.Dispose();
            if (IgnoredOccupancyEntities.IsCreated) IgnoredOccupancyEntities.Dispose();
            if (IgnoredOccupancyCells.IsCreated) IgnoredOccupancyCells.Dispose();
            if (IgnoredOccupancySizes.IsCreated) IgnoredOccupancySizes.Dispose();
            if (AssignedGoals.IsCreated) AssignedGoals.Dispose();
            if (Status.IsCreated) Status.Dispose();
            if (FailureCodes.IsCreated) FailureCodes.Dispose();
            if (ExpansionCounts.IsCreated) ExpansionCounts.Dispose();
            if (Segmented.IsCreated) Segmented.Dispose();
            if (ContinuationMoves.IsCreated) ContinuationMoves.Dispose();
            if (CheapSegmentModes.IsCreated) CheapSegmentModes.Dispose();
            if (AlternateSearchSkipped.IsCreated) AlternateSearchSkipped.Dispose();
            if (AlternateAttempts.IsCreated) AlternateAttempts.Dispose();
        }

        public void ClearForCollection()
        {
            Entities.Clear();
            UnitGrids.Clear();
            Goals.Clear();
            Footprints.Clear();
            MovementBehaviors.Clear();
            Factions.Clear();
            ManualMoves.Clear();
            IgnoredOccupancyEntities.Clear();
            IgnoredOccupancyCells.Clear();
            IgnoredOccupancySizes.Clear();
            ContinuationMoves.Clear();
        }
    }
}
