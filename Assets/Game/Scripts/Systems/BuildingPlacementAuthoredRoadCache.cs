using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>One road projection per map/mesh revision, shared by preview, walls, and final validation.</summary>
    internal sealed class BuildingPlacementAuthoredRoadCache
    {
        private World world;
        private Entity owner;
        private uint revision;
        private MapSurfaceComponent surface;
        private GridConfig grid;
        private bool[] mask;

        internal void Ensure(EntityManager em,EntityQuery query,GridConfig currentGrid)
        {
            if(query.CalculateEntityCount()!=1) {mask=null;world=null;return;}
            Entity entity=query.GetSingletonEntity();
            var current=em.GetComponentData<MapSurfaceComponent>(entity);
            uint version=em.HasComponent<MapSurfaceSceneOverlayRevision>(entity)
                ? em.GetComponentData<MapSurfaceSceneOverlayRevision>(entity).Value : 0;
            if(world==em.World && owner==entity && revision==version && surface.SurfaceBlob==current.SurfaceBlob && surface.HasSurfaceData==current.HasSurfaceData &&
                surface.CellSize==current.CellSize && math.all(surface.GridOrigin==current.GridOrigin) &&
                grid.Width==currentGrid.Width && grid.Height==currentGrid.Height && grid.CellSize==currentGrid.CellSize &&
                math.all(grid.Origin==currentGrid.Origin) && mask!=null) return;
            world=em.World;owner=entity;revision=version;surface=current;grid=currentGrid;
            mask=new bool[grid.Width*grid.Height];
            BuildingPlacementRoadSurfaceMask.Append(em,entity,grid,mask);
        }
        internal void AppendTo(bool[] target)
        {
            if(mask==null || mask.Length!=target.Length) return;
            for(int i=0;i<mask.Length;i++) target[i]|=mask[i];
        }
        internal bool Overlaps(GridConfig currentGrid,Vector2Int origin,Vector2Int footprint)
        {
            if(mask==null || currentGrid.Width!=grid.Width || currentGrid.Height!=grid.Height) return false;
            if(!BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(origin,footprint,currentGrid)) return true;
            for(int y=origin.y;y<origin.y+footprint.y;y++)
                for(int x=origin.x;x<origin.x+footprint.x;x++) if(mask[y*grid.Width+x]) return true;
            return false;
        }
    }
}
