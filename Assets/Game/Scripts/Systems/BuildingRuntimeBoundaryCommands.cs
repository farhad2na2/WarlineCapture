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
        private readonly List<BuildingRuntimeDeleteRequest> _pendingDeletes = new(16);

        public void Process(Func<int, bool> deleteBuildingById, EntityManager entityManager, Entity boundary,Action<int> cleanupBuildingById=null)
        {
            _pendingDeletes.Clear();
            DynamicBuffer<BuildingRuntimeDeleteRequest> requests =
                BuildingRuntimeBoundaryBuffers.EnsureBoundaryBuffer<BuildingRuntimeDeleteRequest>(entityManager, boundary);
            for (int index = 0; index < requests.Length; index++)
            {
                var request=requests[index];
                if(request.BuildingRuntimeId<=0 || request.ImmediateCleanup>1) continue;
                int existing=-1;
                for(int i=0;i<_pendingDeletes.Count;i++) if(_pendingDeletes[i].BuildingRuntimeId==request.BuildingRuntimeId) {existing=i; break;}
                if(existing<0) _pendingDeletes.Add(request);
                else if(request.ImmediateCleanup!=0) _pendingDeletes[existing]=request;
            }

            requests.Clear();
            for(int index=0;index<_pendingDeletes.Count;index++)
            {
                var request=_pendingDeletes[index];
                if(request.ImmediateCleanup!=0) cleanupBuildingById?.Invoke(request.BuildingRuntimeId);
                else deleteBuildingById?.Invoke(request.BuildingRuntimeId);
            }
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
