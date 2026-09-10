using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        // Stage only the UI preview on a plot accepted by the complete production validator.
        // Construction, resource charges, and world entities still require the actual Place button.
        private static void StageLegalPlacementPreview(MatchBootstrapCompositionSystemHelper bootstrap)
        {
            var source=FindPlacementSource(bootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement,new HashSet<object>(),0);
            if(source==null) throw new InvalidOperationException("Missing live placement composition.");
            var placement=source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement;
            if(!ReadGrid(source,out _,out var grid,out var roads,out var blockers)) throw new InvalidOperationException("Missing placement grid.");
            var footprint=placement.Definition.FootprintCells;var initial=placement.OriginCell;
            for(int radius=0;radius<=180;radius++)
                for(int y=initial.y-radius;y<=initial.y+radius;y++)
                    for(int x=initial.x-radius;x<=initial.x+radius;x++)
                    {
                        if(radius>0 && x!=initial.x-radius && x!=initial.x+radius && y!=initial.y-radius && y!=initial.y+radius) continue;
                        var origin=new Vector2Int(x,y);
                        if(!source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(source,placement.Definition,
                            origin,footprint,false,grid,roads,blockers,EffectiveRect,OverlapsOccupant)) continue;
                        placement.OriginCell=placement.CommittedOriginCell=origin;
                        Debug.Log("[M03HudRoad] actual legal plot="+origin+" footprint="+footprint);
                        return;
                    }
            throw new InvalidOperationException("No legal "+placement.Definition.DisplayName+" plot in the mission area.");
        }
        private static BuildingGameplaySourceCompositionSystemHelper FindPlacementSource(object value,HashSet<object> visited,int depth)
        {
            if(value is BuildingGameplaySourceCompositionSystemHelper source) return source;
            if(value==null || depth>8 || !visited.Add(value)) return null;
            if(value is Delegate callback) return FindPlacementSource(callback.Target,visited,depth+1);
            if(value.GetType().Namespace?.StartsWith("Game.",StringComparison.Ordinal)!=true) return null;
            foreach(var field in value.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            {var found=FindPlacementSource(field.GetValue(value),visited,depth+1);if(found!=null)return found;}
            return null;
        }
        private static bool ReadGrid(BuildingGameplaySourceCompositionSystemHelper source,out Entity entity,out GridConfig grid,
            out DynamicBuffer<GridRoad> roads,out DynamicBlockerComponent blockers)=>source.BuildingGridCompositionSystem.TryGetGridData(
                source,source.BuildingEntityManagerAccessSystem.TryGetEntityManager,out entity,out grid,out roads,out blockers);
        private static RectInt EffectiveRect(BuildingGameplaySourceCompositionSystemHelper source,BuildingDefinition definition,
            Vector2Int origin,GridConfig grid,bool rotated)=>source.BuildingRuntimeQueryCompositionSystemHelper.GetEffectivePlacementRect(source,definition,origin,grid,rotated);
        private static bool OverlapsOccupant(BuildingGameplaySourceCompositionSystemHelper source,RectInt rect)=>
            source.BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyRuntimeBuilding(source,rect,ReadGrid,EffectiveRect) ||
            source.BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(source,rect,source.BuildingEntityManagerAccessSystem.TryGetEntityManager);
    }
}
