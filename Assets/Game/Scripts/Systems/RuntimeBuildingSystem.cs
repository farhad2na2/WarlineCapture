using System.Collections.Generic;

namespace Game.Runtime
{
    public sealed class RuntimeBuildingCollection<TBuilding>
    {
        private readonly Dictionary<int, TBuilding> _buildings = new();
        private int _nextBuildingId = 1;

        public IReadOnlyDictionary<int, TBuilding> Buildings => _buildings;
        public int Count => _buildings.Count;
        public int? SelectedBuildingId { get; private set; }
        public int? ActiveBuildingId { get; private set; }
        public int? CurrentActiveBuildingId
        {
            get
            {
                if (ActiveBuildingId.HasValue && _buildings.ContainsKey(ActiveBuildingId.Value))
                    return ActiveBuildingId.Value;
                if (SelectedBuildingId.HasValue && _buildings.ContainsKey(SelectedBuildingId.Value))
                {
                    ActiveBuildingId = SelectedBuildingId.Value;
                    return SelectedBuildingId.Value;
                }

                return null;
            }
        }

        public int AllocateId()
        {
            return _nextBuildingId++;
        }

        public void Clear()
        {
            _buildings.Clear();
            _nextBuildingId = 1;
            ClearSelection();
        }

        public bool ContainsBuilding(int buildingId)
        {
            return _buildings.ContainsKey(buildingId);
        }

        public void AddBuilding(int buildingId, TBuilding building)
        {
            _buildings.Add(buildingId, building);
        }

        public bool RemoveBuilding(int buildingId)
        {
            bool removed = _buildings.Remove(buildingId);
            if (SelectedBuildingId == buildingId)
                SelectedBuildingId = null;
            if (ActiveBuildingId == buildingId)
                ActiveBuildingId = null;
            return removed;
        }

        public bool TryGetBuilding(int buildingId, out TBuilding building)
        {
            return _buildings.TryGetValue(buildingId, out building);
        }

        public bool HasSelectedBuilding()
        {
            return SelectedBuildingId.HasValue && _buildings.ContainsKey(SelectedBuildingId.Value);
        }

        public void SelectBuilding(int buildingId)
        {
            SelectedBuildingId = buildingId;
            ActiveBuildingId = buildingId;
        }

        public void ClearSelection()
        {
            SelectedBuildingId = null;
            ActiveBuildingId = null;
        }
    }
}
