using System.Collections.Generic;
using UnityEngine;

internal sealed class RoadBuildPlacementStorageSystem
{
    private readonly RuntimeBuildingCollection<RuntimeBuildingEntity> _runtimeBuildingSystem = new();

    public IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings => _runtimeBuildingSystem.Buildings;
    public BuildingDefinition SoldierBaseDefinition { get; private set; }
    public BuildingPlacementLifecycleSystem.PlacementState ActivePlacement { get; private set; }
    public bool HasPendingBuildingPlacement => ActivePlacement != null;
    public bool CanConfirmBuildingPlacement => ActivePlacement != null && ActivePlacement.IsValid;
    public bool HasSelectedBuilding => _runtimeBuildingSystem.HasSelectedBuilding();

    public void SetSoldierBaseDefinition(BuildingDefinition definition)
    {
        SoldierBaseDefinition = definition;
    }

    public void BeginPlacement(BuildingDefinition definition, GameObject previewInstance, Vector2Int originCell)
    {
        ActivePlacement = new BuildingPlacementLifecycleSystem.PlacementState
        {
            Definition = definition,
            PreviewInstance = previewInstance,
            OriginCell = originCell,
            CommittedOriginCell = originCell,
            DragStartOriginCell = originCell,
            DragCurrentOriginCell = originCell
        };
    }

    public GameObject ClearActivePlacement()
    {
        GameObject previewInstance = ActivePlacement?.PreviewInstance;
        ActivePlacement = null;
        return previewInstance;
    }

    public void ReleaseActivePlacementPreview()
    {
        if (ActivePlacement != null)
            ActivePlacement.PreviewInstance = null;
    }

    public int AllocateBuildingId()
    {
        return _runtimeBuildingSystem.AllocateId();
    }

    public void AddBuilding(RuntimeBuildingEntity building)
    {
        if (building == null)
            return;

        _runtimeBuildingSystem.AddBuilding(building.Id, building);
    }

    public bool RemoveBuilding(int buildingId)
    {
        return _runtimeBuildingSystem.RemoveBuilding(buildingId);
    }

    public bool ContainsBuilding(int buildingId)
    {
        return _runtimeBuildingSystem.ContainsBuilding(buildingId);
    }

    public bool TryGetBuilding(int buildingId, out RuntimeBuildingEntity building)
    {
        return _runtimeBuildingSystem.TryGetBuilding(buildingId, out building);
    }

    public bool TryGetSelectedBuilding(out RuntimeBuildingEntity building)
    {
        building = null;
        int? selectedBuildingId = _runtimeBuildingSystem.SelectedBuildingId;
        return selectedBuildingId.HasValue &&
               _runtimeBuildingSystem.TryGetBuilding(selectedBuildingId.Value, out building);
    }

    public void SelectBuilding(int buildingId)
    {
        if (_runtimeBuildingSystem.ContainsBuilding(buildingId))
            _runtimeBuildingSystem.SelectBuilding(buildingId);
    }

    public void ClearSelection()
    {
        _runtimeBuildingSystem.ClearSelection();
    }

    public void Clear()
    {
        _runtimeBuildingSystem.Clear();
        ActivePlacement = null;
        SoldierBaseDefinition = null;
    }
}
