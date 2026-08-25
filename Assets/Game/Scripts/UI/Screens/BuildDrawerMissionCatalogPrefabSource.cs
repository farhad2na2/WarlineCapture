using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed class BuildDrawerMissionCatalogPrefabSource : ICatalogPrefabSource
    {
        private static readonly IReadOnlyList<GameObject> EmptyPrefabs = Array.Empty<GameObject>();

        private readonly List<string> _allowedBuildingConfigIds = new();
        private readonly List<GameObject> _filteredUnits = new();
        private readonly List<GameObject> _filteredBuildings = new();
        private ICatalogPrefabSource _unitSource;
        private ICatalogPrefabSource _buildingSource;
        private string _requiredUnitConfigId = string.Empty;
        private bool _restricted;

        public IReadOnlyList<GameObject> UnitSpawnPrefabs =>
            _restricted ? _filteredUnits : _unitSource?.UnitSpawnPrefabs;

        public IReadOnlyList<GameObject> BuildingSpawnPrefabs =>
            _restricted ? _filteredBuildings : _buildingSource?.BuildingSpawnPrefabs;

        public void Refresh(ICatalogPrefabSource unitSource, ICatalogPrefabSource buildingSource)
        {
            _unitSource = unitSource;
            _buildingSource = buildingSource;
            _allowedBuildingConfigIds.Clear();
            _filteredUnits.Clear();
            _filteredBuildings.Clear();
            _requiredUnitConfigId = string.Empty;
            _restricted = UiShellRuntimeGateway.TryReadMissionBuildCatalog(
                out UiMissionBuildCatalogModel catalog);
            if (!_restricted)
                return;

            for (int index = 0; index < catalog.EntryCount; index++)
            {
                if (UiShellRuntimeGateway.TryReadMissionBuildCatalogEntry(
                        index, out UiMissionBuildCatalogEntryModel entry) &&
                    !string.IsNullOrWhiteSpace(entry.BuildingConfigId))
                {
                    _allowedBuildingConfigIds.Add(entry.BuildingConfigId);
                }
            }

            if (catalog.CanRequestRequiredUnit)
                _requiredUnitConfigId = catalog.RequiredUnitConfigId;
            PopulateFilteredUnits();
            PopulateFilteredBuildings();
        }

        internal void ApplyForTests(
            ICatalogPrefabSource unitSource,
            ICatalogPrefabSource buildingSource,
            bool restricted,
            IReadOnlyList<UiMissionBuildCatalogEntryModel> entries,
            string requiredUnitConfigId = null)
        {
            _unitSource = unitSource;
            _buildingSource = buildingSource;
            _restricted = restricted;
            _allowedBuildingConfigIds.Clear();
            _filteredUnits.Clear();
            _filteredBuildings.Clear();
            _requiredUnitConfigId = requiredUnitConfigId ?? string.Empty;
            if (!restricted)
                return;

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    if (!string.IsNullOrWhiteSpace(entries[index].BuildingConfigId))
                        _allowedBuildingConfigIds.Add(entries[index].BuildingConfigId);
                }
            }

            PopulateFilteredUnits();
            PopulateFilteredBuildings();
        }

        private void PopulateFilteredUnits()
        {
            if (string.IsNullOrWhiteSpace(_requiredUnitConfigId))
                return;

            IReadOnlyList<GameObject> units = _unitSource?.UnitSpawnPrefabs;
            if (units == null)
                return;

            for (int index = 0; index < units.Count; index++)
            {
                GameObject prefab = units[index];
                if (prefab != null && string.Equals(
                        prefab.name, _requiredUnitConfigId, StringComparison.Ordinal))
                {
                    _filteredUnits.Add(prefab);
                }
            }
        }

        private void PopulateFilteredBuildings()
        {
            IReadOnlyList<GameObject> buildings = _buildingSource?.BuildingSpawnPrefabs;
            if (buildings == null)
                return;

            for (int prefabIndex = 0; prefabIndex < buildings.Count; prefabIndex++)
            {
                GameObject prefab = buildings[prefabIndex];
                if (prefab == null)
                    continue;

                for (int idIndex = 0; idIndex < _allowedBuildingConfigIds.Count; idIndex++)
                {
                    if (!string.Equals(prefab.name, _allowedBuildingConfigIds[idIndex],
                            StringComparison.Ordinal))
                        continue;

                    _filteredBuildings.Add(prefab);
                    break;
                }
            }
        }
    }
}
