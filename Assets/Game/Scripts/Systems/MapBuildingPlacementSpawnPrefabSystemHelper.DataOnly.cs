using System;

namespace Game.Runtime
{
    // Shared data-only portion of the original placement owner. The Unity helper
    // and headless host use this same bounded sequence and completion state.
    internal sealed partial class MapBuildingPlacementSpawnPrefabSystemHelper
    {
        private const int MaxPlacementsPerUpdate = 32;
        private bool _authoringHidden;
        private readonly MapBuildingPlacementSequenceKernel _sequence = new();

        public bool IsComplete => _sequence.IsQueued && _authoringHidden;

        // A headless host supplies the same owner's source/config and actor
        // callbacks, without creating authored visual instances.
        internal bool UpdateDataOnly(int placementCount, Func<int, bool> isPriorityPlacement,
            Func<int, bool> trySpawnPlacement)
        {
            if (placementCount < 0)
                throw new ArgumentOutOfRangeException(nameof(placementCount));
            if (_sequence.IsQueued)
            {
                _authoringHidden = true;
                return true;
            }
            if (placementCount == 0)
            {
                _sequence.QueueEmpty();
                _authoringHidden = true;
                return true;
            }

            bool queued = _sequence.Advance(placementCount, MaxPlacementsPerUpdate,
                isPriorityPlacement, trySpawnPlacement);
            if (queued)
                _authoringHidden = true;
            return queued;
        }
    }
}
