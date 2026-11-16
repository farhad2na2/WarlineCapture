using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    // Shared source policy for converting an owner-evaluated world point into
    // the bounded building production spawn-point read model.
    internal static class BuildingFactionProductionSpawnPointReadModelProjection
    {
        internal static bool TryCreate(
            byte factionId,
            FixedString128Bytes buildingId,
            int buildingRuntimeId,
            int slotIndex,
            GridConfig grid,
            float3 worldPosition,
            out BuildingFactionProductionSpawnPointReadModel row)
        {
            row = default;
            int2 cell = GridUtils.WorldToCell(
                grid, new Vector3(worldPosition.x, worldPosition.y, worldPosition.z));
            if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                return false;

            row = new BuildingFactionProductionSpawnPointReadModel
            {
                FactionId = factionId,
                BuildingId = buildingId,
                BuildingRuntimeId = buildingRuntimeId,
                SlotIndex = slotIndex,
                Cell = cell,
                WorldPosition = worldPosition
            };
            return true;
        }
    }
}
