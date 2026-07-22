using System;
using System.Collections.Generic;

namespace Game.Editor
{
    internal sealed class DenseCityGenerationTransactionContext : IDisposable
    {
        private readonly Dictionary<int, int> nextBuildingSequenceByDistrict = new();
        private bool disposed;

        internal DenseCityGenerationTransactionContext(
            int buildingCapacity,
            int surfaceCapacity,
            int presentationCapacity)
        {
            Records = new DenseCityGenerationRecordSet(
                buildingCapacity,
                surfaceCapacity,
                presentationCapacity);
        }

        internal DenseCityGenerationRecordSet Records { get; }

        internal bool TryPlaceBuilding(
            int districtId,
            Func<int, DenseCityBuildingRecordGroup> createGroup,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (createGroup == null)
                throw new ArgumentNullException(nameof(createGroup));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int sequenceStart = nextBuildingSequenceByDistrict.TryGetValue(districtId, out int nextSequence)
                ? nextSequence
                : 0;
            if (sequenceStart > int.MaxValue - 5)
                throw new InvalidOperationException("Dense-city building sequence capacity is exhausted.");

            DenseCityBuildingRecordGroup group = createGroup(sequenceStart);
            nextBuildingSequenceByDistrict[districtId] = sequenceStart + 5;
            return DenseCityBuildingPlacementTransaction.TryCommitAndRealize(
                Records,
                group,
                realize);
        }

        internal void Seal()
        {
            RequireActive();
            Records.Seal();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            Records.Dispose();
            nextBuildingSequenceByDistrict.Clear();
            disposed = true;
        }

        private void RequireActive()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DenseCityGenerationTransactionContext));
        }
    }
}
