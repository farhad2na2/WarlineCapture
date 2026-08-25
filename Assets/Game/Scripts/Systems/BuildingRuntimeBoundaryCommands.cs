using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class BuildingRuntimeDeleteCommandProcessor
    {
        private readonly List<int> _pendingBuildingIds = new();

        public void Process(Func<int, bool> deleteBuildingById, EntityManager entityManager, Entity boundary)
        {
            _pendingBuildingIds.Clear();
            DynamicBuffer<BuildingRuntimeDeleteRequest> requests =
                BuildingRuntimeBoundaryBuffers.EnsureBoundaryBuffer<BuildingRuntimeDeleteRequest>(entityManager, boundary);
            for (int index = 0; index < requests.Length; index++)
            {
                int buildingRuntimeId = requests[index].BuildingRuntimeId;
                if (buildingRuntimeId > 0 && !_pendingBuildingIds.Contains(buildingRuntimeId))
                    _pendingBuildingIds.Add(buildingRuntimeId);
            }

            requests.Clear();
            if (deleteBuildingById == null)
                return;

            for (int index = 0; index < _pendingBuildingIds.Count; index++)
                deleteBuildingById(_pendingBuildingIds[index]);
        }
    }

    internal static class BuildingRuntimeBoundaryBuffers
    {
        public static DynamicBuffer<T> EnsureBoundaryBuffer<T>(EntityManager entityManager, Entity boundary)
            where T : unmanaged, IBufferElementData
        {
            if (!entityManager.HasBuffer<T>(boundary))
                entityManager.AddBuffer<T>(boundary);

            return entityManager.GetBuffer<T>(boundary);
        }

        public static FixedString128Bytes ToFixedString128(string value) =>
            new(value ?? string.Empty);

        public static FixedString64Bytes ToUnitSourceKey(GameObject prefab) =>
            new(prefab != null ? prefab.name : string.Empty);
    }
}
