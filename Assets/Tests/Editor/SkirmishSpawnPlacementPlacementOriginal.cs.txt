using Game.Components;
using Game.Runtime.Pathfinding;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class SkirmishSpawnPlacement
    {
        internal static bool EnsureTraversable(EntityManager em, in GridConfig grid,
            NativeArray<GridWalkable> walkable, NativeBitArray blocked, NativeBitArray occupied,
            ref NativeBitArray reserved, Entity prefab, int2 size, ref int2 cell)
        {
            using var match=em.CreateEntityQuery(typeof(SkirmishMatchState));
            if(match.IsEmptyIgnoreFilter)return true;
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if(surfaces.CalculateEntityCount()!=1)return false;
            var surface=surfaces.GetSingleton<MapSurfaceComponent>();
            bool vehicle=em.HasComponent<UnitMovementBehavior>(prefab)&&em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion!=0;
            var validation=new MapSurfaceTraversalValidation();
            if(validation.CanTraverseFootprint(surface,1,grid,cell,size,vehicle))return true;

            // The compatibility grid can call a slope or a baked rooftop "clear".
            // Release only this just-reserved candidate and search valid terrain.
            var minimum=UnitFootprintUtility.GetMinCell(cell,size);
            for(int y=0;y<size.y;y++)for(int x=0;x<size.x;x++)reserved.Set((minimum.y+y)*grid.Width+minimum.x+x,false);
            for(int radius=1;radius<=48;radius++)
                for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)
                {
                    if(math.abs(x)!=radius&&math.abs(z)!=radius)continue;
                    var candidate=cell+new int2(x,z);
                    if(!validation.CanTraverseFootprint(surface,1,grid,candidate,size,vehicle))continue;
                    minimum=UnitFootprintUtility.GetMinCell(candidate,size);
                    bool free=true;
                    for(int dy=0;dy<size.y&&free;dy++)for(int dx=0;dx<size.x;dx++)
                    {int index=(minimum.y+dy)*grid.Width+minimum.x+dx;if(walkable[index].Value==0||blocked.IsSet(index)||occupied.IsSet(index)||reserved.IsSet(index)){free=false;break;}}
                    if(!free)continue;
                    for(int dy=0;dy<size.y;dy++)for(int dx=0;dx<size.x;dx++)reserved.Set((minimum.y+dy)*grid.Width+minimum.x+dx,true);
                    cell=candidate;return true;
                }
            return false;
        }
    }
}
