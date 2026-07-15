using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal sealed class ResourceHaulerAIOilAllocationPolicySystemHelper
    {
        private FixedList512Bytes<CacheEntry> _inputCache;

        private struct CacheEntry
        {
            public byte FactionId;
            public byte HasInput;
            public BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput Input;
        }

        internal void ClearInputCache()
        {
            _inputCache.Clear();
        }

        internal bool TryResolveCachedInput(
            BuildingResourceHaulerBridgeCompositionSystemHelper.TryResolveFactionAIOilAllocationInputDelegate resolver,
            EntityManager em,
            byte factionId,
            out BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput input)
        {
            input = default;
            for (int i = 0; i < _inputCache.Length; i++)
            {
                CacheEntry entry = _inputCache[i];
                if (entry.FactionId != factionId)
                    continue;

                input = entry.Input;
                return entry.HasInput != 0;
            }

            bool hasInput = resolver != null && resolver(em, factionId, out input);
            if (_inputCache.Length < _inputCache.Capacity)
            {
                _inputCache.Add(new CacheEntry
                {
                    FactionId = factionId,
                    HasInput = hasInput ? (byte)1 : (byte)0,
                    Input = input
                });
            }

            return hasInput;
        }

        internal static int ResolveDestinationStrategicPriority(
            bool isFabricationDepot,
            bool isFuelBuilding,
            in BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput input)
        {
            if (isFabricationDepot)
            {
                return ResolveConstructionPressureBand(
                    input.PlannedMaterialsCost,
                    input.AvailableMaterials,
                    input.MaterialsCapacity);
            }

            return isFuelBuilding
                ? ResolveFuelPressureBand(input.StoredFuelBarrels, input.FuelStorageCapacity)
                : 0;
        }

        internal static int ResolveConstructionPressureBand(
            int plannedMaterialsCost,
            int availableMaterials,
            int materialsCapacity)
        {
            int cost = math.max(0, plannedMaterialsCost);
            if (cost == 0 || cost > math.max(0, materialsCapacity) || availableMaterials >= cost)
                return 0;

            return availableMaterials <= 0 || availableMaterials * 2 < cost ? 2 : 1;
        }

        internal static int ResolveFuelPressureBand(float storedFuelBarrels, int fuelStorageCapacity)
        {
            int capacity = math.max(0, fuelStorageCapacity);
            if (capacity == 0)
                return 0;

            float ratio = math.saturate(math.max(0f, storedFuelBarrels) / capacity);
            if (ratio <= 0.1f)
                return 3;
            return ratio <= 0.25f ? 1 : 0;
        }
    }
}
