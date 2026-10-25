using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    // Only starting-grant placement before player previews/rotation/runway flows.
    // All four masks must come from the admitted existing Campaign geometry bake.
    internal sealed class GeneratedStartingBuildingPlacement
    {
        readonly EntityManager em;
        readonly Entity gridEntity;
        readonly GridConfig grid;
        readonly bool[] authoredBuildings, authoredRoads, sidewalks, water;
        readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings;
        readonly Func<int, int, int, int, bool> runtimeBlockerCell;
        readonly BuildingRuntimeSpawnCompositionSystemHelper source = new BuildingRuntimeSpawnCompositionSystemHelper();
        internal GeneratedStartingBuildingPlacement(EntityManager em, Entity gridEntity,
            bool[] authoredBuildings, bool[] authoredRoads, bool[] sidewalks, bool[] water,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> liveBuildings,
            Func<int, int, int, int, bool> runtimeBlockerCell)
        {
            this.em = em ?? throw new ArgumentNullException(nameof(em)); this.gridEntity = gridEntity;
            grid = em.GetComponentData<GridConfig>(gridEntity);
            int count = checked(grid.Width * grid.Height);
            if (count < 1 || count > 4194304 || liveBuildings == null || runtimeBlockerCell == null)
                throw new NotSupportedException("Complete generated placement context required");
            foreach (var mask in new[] { authoredBuildings, authoredRoads, sidewalks, water })
                if (mask == null || mask.Length != count) throw new NotSupportedException("Complete Campaign geometry masks required");
            this.authoredBuildings = (bool[])authoredBuildings.Clone(); this.authoredRoads = (bool[])authoredRoads.Clone();
            this.sidewalks = (bool[])sidewalks.Clone(); this.water = (bool[])water.Clone();
            buildings = liveBuildings; this.runtimeBlockerCell = runtimeBlockerCell;
        }
        internal BuildingRuntimeSpawnCompositionSystemHelper.Context CreateContext(GeneratedConfiguredDefinitionSource definitions, GeneratedBuildingRegistration registration) => new BuildingRuntimeSpawnCompositionSystemHelper.Context {
            GetPlacementFootprint=(d,r)=>r?throw new NotSupportedException("Rotation unsupported"):d.FootprintCells,
            GetEffectivePlacementRect=(d,at,g,r)=>r?throw new NotSupportedException("Rotation unsupported"):new RectInt(at,d.FootprintCells),
            IsPlacementValid=IsValid,WallValidationContext=new BuildingPlacementValidationUtilitySystemHelper.WallValidationContext(buildings),
            TryGetGridData=(out Entity e,out GridConfig g,out DynamicBuffer<GridRoad> roads,out DynamicBlockerComponent blockers)=>{e=gridEntity;g=grid;roads=em.GetBuffer<GridRoad>(e);blockers=em.GetComponentData<DynamicBlockerComponent>(e);return true;},DefinitionSystem=definitions,Registration=registration};
        internal bool TryFind(RuntimeDefinition definition, Vector2Int preferred, out Vector2Int origin)
        {
            if (definition == null || !definition.SourceMetadataComplete || definition.FootprintCells.x < 1 ||
                definition.FootprintCells.y < 1 || definition.HasRunway || definition.IsWall ||
                BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(definition))
                throw new NotSupportedException("Complete ordinary ground building definition required");
            var context = new BuildingRuntimeSpawnCompositionSystemHelper.Context
            {
                GetPlacementFootprint = (d, rotated) => rotated ? throw new NotSupportedException("Starting grant rotation") : d.FootprintCells,
                GetEffectivePlacementRect = (d, at, g, rotated) => rotated ? throw new NotSupportedException("Starting grant rotation") : new RectInt(at, d.FootprintCells),
                IsPlacementValid = IsValid,
                WallValidationContext = new BuildingPlacementValidationUtilitySystemHelper.WallValidationContext(buildings)
            };
            return source.TryFindValidInitialBuildingOrigin(context, definition, preferred, false, grid,
                em.GetBuffer<GridRoad>(gridEntity), em.GetComponentData<DynamicBlockerComponent>(gridEntity), out origin);
        }
        bool IsValid(RuntimeDefinition definition, Vector2Int origin, Vector2Int footprint, bool rotated,
            GridConfig layout, DynamicBuffer<GridRoad> roads, DynamicBlockerComponent blockers)
        {
            if (rotated || !BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(origin, footprint, layout)) return false;
            for (int y = origin.y; y < origin.y + footprint.y; y++)
                for (int x = origin.x; x < origin.x + footprint.x; x++)
                {
                    int index = y * layout.Width + x;
                    if (authoredBuildings[index] || authoredRoads[index] || sidewalks[index] || water[index]) return false;
                }
            return BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(new RectInt(origin, footprint), layout,
                roads, blockers, false, null, 0, 0, runtimeBlockerCell, null, OverlapsRuntime);
        }
        bool OverlapsRuntime(RectInt rectangle)
        {
            foreach (var entry in buildings)
            {
                var building = entry.Value;
                if (building?.Definition != null && rectangle.Overlaps(new RectInt(building.OriginCell, building.Definition.FootprintCells))) return true;
            }
            return false;
        }
    }
}
