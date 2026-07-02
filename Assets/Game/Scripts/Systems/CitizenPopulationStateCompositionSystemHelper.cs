using System.Collections.Generic;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class CitizenPopulationStateCompositionSystemHelper
    {
        private readonly Dictionary<int, int> _householdIdsByHomeBuildingId = new();
        private readonly Dictionary<int, CitizenHouseholdRecordComponent> _householdsById = new();
        private readonly Dictionary<int, CitizenRecordComponent> _citizensById = new();
        private readonly Dictionary<int, VisibleCitizenComponent> _visibleCitizensById = new();
        private readonly List<int> _scratchCitizenIds = new();
        private readonly List<int> _scratchHouseholdIds = new();
        private readonly List<int> _scratchVisibleCitizenIds = new();
        private readonly List<int> _scratchRemovedBuildingIds = new();
        private int _nextHouseholdId = 1;
        private int _nextCitizenId = 1;

        public Dictionary<int, int> HouseholdIdsByHomeBuildingId => _householdIdsByHomeBuildingId;
        public Dictionary<int, CitizenHouseholdRecordComponent> HouseholdsById => _householdsById;
        public Dictionary<int, CitizenRecordComponent> CitizensById => _citizensById;
        public Dictionary<int, VisibleCitizenComponent> VisibleCitizensById => _visibleCitizensById;
        public List<int> ScratchCitizenIds => _scratchCitizenIds;
        public List<int> ScratchHouseholdIds => _scratchHouseholdIds;
        public List<int> ScratchVisibleCitizenIds => _scratchVisibleCitizenIds;
        public List<int> ScratchRemovedBuildingIds => _scratchRemovedBuildingIds;
        public int CitizenCount => _citizensById.Count;
        public int HouseholdCount => _householdsById.Count;

        public void Reset()
        {
            _householdIdsByHomeBuildingId.Clear();
            _householdsById.Clear();
            _citizensById.Clear();
            _visibleCitizensById.Clear();
            _scratchCitizenIds.Clear();
            _scratchHouseholdIds.Clear();
            _scratchVisibleCitizenIds.Clear();
            _scratchRemovedBuildingIds.Clear();
            _nextHouseholdId = 1;
            _nextCitizenId = 1;
        }

        public int AllocateHouseholdId()
        {
            return _nextHouseholdId++;
        }

        public int AllocateCitizenId()
        {
            return _nextCitizenId++;
        }

        public CitizenHouseholdRecordComponent StoreHousehold(CitizenHouseholdRecordComponent household)
        {
            _householdsById[household.HouseholdId] = household;
            if (household.HomeBuildingId != 0)
                _householdIdsByHomeBuildingId[household.HomeBuildingId] = household.HouseholdId;
            return household;
        }

        public CitizenRecordComponent StoreCitizen(CitizenRecordComponent citizen)
        {
            _citizensById[citizen.CitizenId] = citizen;
            return citizen;
        }

        public bool TryGetCitizen(int citizenId, out CitizenRecordComponent citizen)
        {
            return _citizensById.TryGetValue(citizenId, out citizen);
        }

        public bool TryGetHousehold(int householdId, out CitizenHouseholdRecordComponent household)
        {
            return _householdsById.TryGetValue(householdId, out household);
        }

        public void RemoveHomeMapping(int buildingId)
        {
            _householdIdsByHomeBuildingId.Remove(buildingId);
        }

        public void PopulateCitizenIds()
        {
            _scratchCitizenIds.Clear();
            foreach (int citizenId in _citizensById.Keys)
                _scratchCitizenIds.Add(citizenId);
        }

        public void PopulateHouseholdIds()
        {
            _scratchHouseholdIds.Clear();
            foreach (int householdId in _householdsById.Keys)
                _scratchHouseholdIds.Add(householdId);
        }

        public void PopulateVisibleCitizenIds()
        {
            _scratchVisibleCitizenIds.Clear();
            foreach (int citizenId in _visibleCitizensById.Keys)
                _scratchVisibleCitizenIds.Add(citizenId);
        }
    }
}
