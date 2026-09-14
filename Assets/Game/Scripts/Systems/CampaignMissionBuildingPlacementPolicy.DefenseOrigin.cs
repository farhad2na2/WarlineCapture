using Game.Components;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal static partial class CampaignMissionBuildingPlacementPolicy
    {
        private static Vector2Int DefenseOrigin(BuildingGameplaySourceCompositionSystemHelper source,
            BuildingRuntimeSpawnCompositionSystemHelper.Context context, BuildingDefinition definition,
            Vector2Int footprint, Vector2Int fallback)
        {
            string suffix = definition?.Prefab?.name switch
            {
                "Building_Road_Barrier" => "defense_barrier",
                "Building_GuardTower" => "defense_tower",
                _ => null
            };
            if (suffix == null || !source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out var em)) return fallback;
            var queries = source.BuildingGameplayEcsQueryCompositionSystemHelper;
            queries.EnsureEntityQueries(em);
            if (queries.CampaignMissionQuery.CalculateEntityCount()!=1 || queries.OperationMapQuery.CalculateEntityCount()!=1 ||
                queries.CampaignMissionQuery.GetSingleton<CampaignMissionRuntimeComponent>().MissionId.ToString()!="saga.ch01.m03.radar_warning" ||
                !context.TryGetGridData(out _,out var grid,out _,out _)) return fallback;
            var metadata=queries.OperationMapQuery.GetSingleton<OperationMapMetadataComponent>();
            if (!metadata.Blob.IsCreated) return fallback;
            ref var map=ref metadata.Blob.Value;
            for(int i=0;i<map.Anchors.Length;i++)
            {
                ref var anchor=ref map.Anchors[i];
                if(anchor.Id.ToString()!="anchor.ch01.m03."+suffix) continue;
                var cell=(int2)math.floor((anchor.Position.xz-grid.Origin.xz)/grid.CellSize);
                return new Vector2Int(cell.x-footprint.x/2,cell.y-footprint.y/2);
            }
            return fallback;
        }
    }
}
