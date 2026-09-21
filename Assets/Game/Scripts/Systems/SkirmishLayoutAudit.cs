using System.Text;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>Read-only acceptance check against authored road meshes and actual spawned footprints.</summary>
    public static class SkirmishLayoutAudit
    {
        public static string Inspect(EntityManager em, out bool passed)
        {
            passed=false;
            using var grids=em.CreateEntityQuery(typeof(GridConfig));
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if(grids.CalculateEntityCount()!=1 || surfaces.CalculateEntityCount()!=1)return "Map is not ready.";
            var grid=grids.GetSingleton<GridConfig>();
            var cache=new BuildingPlacementAuthoredRoadCache();cache.Ensure(em,surfaces,grid);
            var roads=new bool[grid.Width*grid.Height];cache.AppendTo(roads);
            var sidewalks=cache.GetSidewalks();
            var water=cache.GetWater();
            var preset=SkirmishPresetResolver.Load(em);
            var report=new StringBuilder();int found=0,failures=0;
            using var query=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),ComponentType.ReadOnly<UnitSourcePrefabKey>()},
                None=new[]{ComponentType.ReadOnly<OperationMapBuildingComponent>()}});
            using var buildings=query.ToEntityArray(Allocator.Temp);
            foreach(var faction in preset.buildingPlacement.InitialUnitsConfig.Factions)
                foreach(var entry in faction.Buildings)
                {
                    int2 expected=new(faction.SpawnCell.x+entry.OriginOffset.x,faction.SpawnCell.y+entry.OriginOffset.y);
                    bool matched=false;
                    foreach(var entity in buildings)
                    {
                        var info=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                        if(info.OwnerFactionId!=faction.FactionId || !em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString().Contains(entry.Prefab.name))continue;
                        if(!math.all(info.OriginCell==expected))continue;
                        matched=true;found++;int roadCells=0,sidewalkCells=0,waterCells=0;
                        for(int y=0;y<info.FootprintCells.y;y++)for(int x=0;x<info.FootprintCells.x;x++)
                        {
                            int2 cell=info.OriginCell+new int2(x,y);
                            if(!GridUtils.InBounds(cell,grid.Width,grid.Height)){failures++;continue;}
                            int index=cell.y*grid.Width+cell.x;
                            if(roads[index])roadCells++;
                            if(sidewalks!=null&&sidewalks[index])sidewalkCells++;
                            if(water!=null&&water[index])waterCells++;
                        }
                        bool level = BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(
                            surfaces.GetSingleton<MapSurfaceComponent>(),
                            new RectInt(info.OriginCell.x, info.OriginCell.y, info.FootprintCells.x, info.FootprintCells.y));
                        if(roadCells+sidewalkCells+waterCells>0 || !level)failures++;
                        report.AppendLine($"Faction {faction.FactionId} {entry.Prefab.name}: origin={info.OriginCell} size={info.FootprintCells} roads={roadCells} sidewalks={sidewalkCells} water={waterCells} level={level}");
                        break;
                    }
                    if(!matched){failures++;report.AppendLine($"MISSING OR RELOCATED: faction={faction.FactionId} {entry.Prefab.name} expected={expected}");}
                }
            passed=failures==0&&found>0;
            report.AppendLine($"[SkirmishLayout] result={(passed?"Passed":"Failed")} buildings={found} failures={failures}");
            return report.ToString();
        }
    }
}
