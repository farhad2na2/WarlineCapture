using System;
using System.Collections.Generic;

namespace Game.Runtime
{
    /// <summary>
    /// Data-only ordering state shared by the original map-building owner and headless hosts.
    /// </summary>
    internal sealed class MapBuildingPlacementSequenceKernel
    {
        private readonly HashSet<int> prioritySpawnedIndices = new();
        private int nextPlacementIndex;

        internal bool PriorityPassCompleted { get; private set; }
        internal bool IsQueued { get; private set; }
        internal int NextPlacementIndex => nextPlacementIndex;

        internal void RecordPrioritySuccess(int index)
        {
            if (PriorityPassCompleted || IsQueued || index < 0 || !prioritySpawnedIndices.Add(index))
                throw new InvalidOperationException("Invalid or duplicate priority placement success");
        }

        internal bool WasPrioritySuccessful(int index) => prioritySpawnedIndices.Contains(index);

        internal bool Advance(
            int count,
            int maximumExamined,
            Func<int, bool> isPriorityPlacement,
            Func<int, bool> trySpawnPlacement)
        {
            if (count < 0 || maximumExamined <= 0 || isPriorityPlacement == null || trySpawnPlacement == null)
                throw new ArgumentException("Bounded placement sequence callbacks and count required");
            if (IsQueued)
                return true;

            if (!PriorityPassCompleted)
            {
                for (int index = 0; index < count; index++)
                {
                    if (isPriorityPlacement(index) && trySpawnPlacement(index))
                        RecordPrioritySuccess(index);
                }
                CompletePriorityPass();
            }

            int processed = 0;
            while (TryBeginSequentialAttempt(count, maximumExamined, processed, out int index))
            {
                if (!WasPrioritySuccessful(index))
                    trySpawnPlacement(index);
                CompleteSequentialAttempt(index);
                processed++;
            }

            return QueueWhenExhausted(count);
        }

        internal void CompletePriorityPass()
        {
            if (PriorityPassCompleted || IsQueued)
                throw new InvalidOperationException("Map placement priority pass already closed");
            PriorityPassCompleted = true;
        }

        internal bool TryBeginSequentialAttempt(int count, int maximumExamined, int examinedThisUpdate, out int index)
        {
            if (!PriorityPassCompleted || IsQueued || count < 0 || maximumExamined <= 0 || examinedThisUpdate < 0)
                throw new InvalidOperationException("Map placement sequence is not ready for a bounded batch");
            index = nextPlacementIndex;
            return examinedThisUpdate < maximumExamined && index < count;
        }

        internal void CompleteSequentialAttempt(int index)
        {
            if (index != nextPlacementIndex || IsQueued)
                throw new InvalidOperationException("Map placement sequential cursor advanced out of order");
            nextPlacementIndex++;
        }

        internal bool QueueWhenExhausted(int count)
        {
            if (count < 0 || IsQueued)
                throw new InvalidOperationException("Map placement sequence cannot be queued at this state");
            if (nextPlacementIndex >= count)
                IsQueued = true;
            return IsQueued;
        }

        internal void QueueEmpty()
        {
            IsQueued = true;
        }
    }
}
