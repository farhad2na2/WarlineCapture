using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    internal static class MapVehiclePlacementClearanceSystemHelper
    {
        private const int VehicleDepartureClearancePaddingCells =
            UnitPathPlacementValidation.VehicleOccupancyPaddingCells;
        private const int VehicleDepartureCorridorMaxCells = 32;

        internal static void RefreshPlacementClearance(
            MapVehiclePlacementSpawnPrefabSystemHelper.Context context,
            EntityManager em,
            ref MapVehiclePlacementProgressState progress)
        {
            progress.LastClearedBlockerCells = 0;
            if (context.Config == null ||
                context.Config.Placements == null ||
                context.TryGetGridData == null ||
                !context.TryGetGridData(out _, out GridConfig grid, out _, out DynamicBlockerComponent blockerData) ||
                !blockerData.Blocked.IsCreated)
            {
                return;
            }

            context.UnitPrefabContext.EnsureEntityQueries?.Invoke(em);
            int clearedCells = 0;
            for (int i = 0; i < context.Config.Placements.Count; i++)
            {
                MapVehiclePlacementConfigEntry placement = context.Config.Placements[i];
                if (placement == null ||
                    string.IsNullOrWhiteSpace(placement.VehicleSourceKey) ||
                    !TryResolvePlacementFootprint(context, em, placement, out int2 footprintSize))
                {
                    continue;
                }

                int2 centerCell = GridUtils.WorldToCell(grid, ToFloat3(placement.WorldCenter));
                clearedCells += ClearRuntimeBlockersInFootprint(
                    grid,
                    ref blockerData,
                    centerCell,
                    footprintSize,
                    VehicleDepartureClearancePaddingCells);
                clearedCells += ClearRuntimeBlockerDepartureCorridor(
                    grid,
                    ref blockerData,
                    centerCell,
                    footprintSize,
                    placement.WorldEulerAngles.y,
                    VehicleDepartureCorridorMaxCells);
            }

            progress.LastClearedBlockerCells = clearedCells;
        }

        internal static int ClearRuntimeBlockersInFootprint(
            in GridConfig grid,
            ref DynamicBlockerComponent blockerData,
            int2 centerCell,
            int2 footprintSize,
            int paddingCells = 0)
        {
            if (!blockerData.Blocked.IsCreated || grid.Width <= 0 || grid.Height <= 0)
                return 0;

            int2 clampedSize = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(centerCell, clampedSize);
            int2 max = min + clampedSize;
            int padding = math.max(0, paddingCells);
            min = math.max(min - new int2(padding, padding), int2.zero);
            max = math.min(max + new int2(padding, padding), new int2(grid.Width, grid.Height));

            int clearedCells = 0;
            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    int index = row + x;
                    if ((uint)index >= (uint)blockerData.GridSize)
                        continue;

                    if (blockerData.Blocked.IsSet(index))
                        clearedCells++;

                    blockerData.Blocked.Set(index, false);
                    if (blockerData.Counts.IsCreated && (uint)index < (uint)blockerData.Counts.Length)
                        blockerData.Counts[index] = 0;
                    if (blockerData.FriendlyPassFactionIds.IsCreated &&
                        (uint)index < (uint)blockerData.FriendlyPassFactionIds.Length)
                    {
                        blockerData.FriendlyPassFactionIds[index] = byte.MaxValue;
                    }
                }
            }

            return clearedCells;
        }

        internal static int ClearRuntimeBlockerDepartureCorridor(
            in GridConfig grid,
            ref DynamicBlockerComponent blockerData,
            int2 centerCell,
            int2 footprintSize,
            float headingDegrees,
            int maxDistanceCells)
        {
            if (!blockerData.Blocked.IsCreated || maxDistanceCells <= 0)
                return 0;

            int2 forward = ResolveCardinalHeading(headingDegrees);
            int2 right = new(forward.y, -forward.x);
            int2 bestDirection = int2.zero;
            int bestDistance = int.MaxValue;
            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                int2 direction = directionIndex switch
                {
                    0 => forward,
                    1 => right,
                    2 => -right,
                    _ => -forward
                };

                for (int distance = 1; distance <= maxDistanceCells; distance++)
                {
                    int2 candidate = centerCell + direction * distance;
                    if (!IsBlockerClearanceOpen(
                            grid,
                            blockerData.Blocked,
                            candidate,
                            footprintSize,
                            VehicleDepartureClearancePaddingCells))
                    {
                        continue;
                    }

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestDirection = direction;
                    }

                    break;
                }
            }

            if (bestDistance == int.MaxValue)
                return 0;

            int clearedCells = 0;
            for (int distance = 1; distance <= bestDistance; distance++)
            {
                clearedCells += ClearRuntimeBlockersInFootprint(
                    grid,
                    ref blockerData,
                    centerCell + bestDirection * distance,
                    footprintSize,
                    VehicleDepartureClearancePaddingCells);
            }

            return clearedCells;
        }

        private static bool TryResolvePlacementFootprint(
            MapVehiclePlacementSpawnPrefabSystemHelper.Context context,
            EntityManager em,
            MapVehiclePlacementConfigEntry placement,
            out int2 footprintSize)
        {
            footprintSize = new int2(1, 1);
            if (placement == null || string.IsNullOrWhiteSpace(placement.VehicleSourceKey))
                return false;

            string lookupKey = BuildingDefinitionPrefabSystemHelper.GetSpawnableLookupKey(placement.VehicleSourceKey);
            if (string.IsNullOrWhiteSpace(lookupKey))
                return false;

            if (context.UnitPrefabSystem.TryResolveConfiguredUnitPrefabEntity(
                    context.UnitPrefabContext,
                    new FixedString64Bytes(lookupKey),
                    out Entity prefabEntity) &&
                prefabEntity != Entity.Null &&
                em.Exists(prefabEntity) &&
                em.HasComponent<UnitFootprint>(prefabEntity))
            {
                footprintSize = UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(prefabEntity).Size);
                return true;
            }

            return false;
        }

        private static int2 ResolveCardinalHeading(float headingDegrees)
        {
            float radians = math.radians(headingDegrees);
            float2 forward = new(math.sin(radians), math.cos(radians));
            if (math.abs(forward.x) >= math.abs(forward.y))
                return new int2(forward.x >= 0f ? 1 : -1, 0);

            return new int2(0, forward.y >= 0f ? 1 : -1);
        }

        private static bool IsBlockerClearanceOpen(
            in GridConfig grid,
            in NativeBitArray blocked,
            int2 centerCell,
            int2 footprintSize,
            int paddingCells)
        {
            int2 clampedSize = UnitFootprintUtility.ClampSize(footprintSize);
            int padding = math.max(0, paddingCells);
            int2 min = UnitFootprintUtility.GetMinCell(centerCell, clampedSize) - new int2(padding, padding);
            int2 max = min + clampedSize + new int2(padding * 2, padding * 2);
            if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
                return false;

            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    int index = row + x;
                    if (blocked.IsSet(index))
                        return false;
                }
            }

            return true;
        }

        private static float3 ToFloat3(Vector3 value)
        {
            return new float3(value.x, value.y, value.z);
        }
    }
}
