using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingRuntimeSpawnCompositionSystemHelper
    {
        public bool TryFindValidInitialBuildingOrigin(
            Context context,
            BuildingDefinition definition,
            Vector2Int preferredOrigin,
            bool rotateVertical,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            out Vector2Int originCell,
            bool requirePreferredOrigin = false, bool allowAuthoredRoadOverlap = false)
        {
            originCell = default;
            if (definition == null || context.GetPlacementFootprint == null || context.GetEffectivePlacementRect == null || context.IsPlacementValid == null)
                return false;

            Vector2Int placementFootprint = context.GetPlacementFootprint(definition, rotateVertical);
            Vector2Int clampedPreferred = new(
                Mathf.Clamp(preferredOrigin.x, 0, Mathf.Max(0, grid.Width - placementFootprint.x)),
                Mathf.Clamp(preferredOrigin.y, 0, Mathf.Max(0, grid.Height - placementFootprint.y)));

            RectInt preferredPlacementRect = context.GetEffectivePlacementRect(definition, clampedPreferred, grid, rotateVertical);
            bool trustDiagnostic=requirePreferredOrigin&&IsTrustPlacementDiagnostic();
            if(trustDiagnostic)Debug.Log($"[TrustPlacement] begin prefab={definition.Prefab?.name} configured={definition.FootprintCells} model={placementFootprint} preferred={preferredOrigin} effectiveRect={preferredPlacementRect} gridOrigin={grid.Origin} gridSize={grid.Width}x{grid.Height}");
            bool citywideDiagnostic=requirePreferredOrigin&&IsCitywidePlacementDiagnostic();
            if(citywideDiagnostic)
                Debug.Log($"[CitywidePlacement] begin prefab={definition.Prefab?.name} configuredFootprint={definition.FootprintCells} modelFootprint={placementFootprint} preferred={preferredOrigin} clamped={clampedPreferred} effectiveRect={preferredPlacementRect} gridOrigin={grid.Origin} gridSize={grid.Width}x{grid.Height}");
            if (allowAuthoredRoadOverlap)
            {
                // Authored mission obstruction requests are identity-checked at the
                // owning spawn boundary. They may occupy roads, never existing owners.
                if (!requirePreferredOrigin || clampedPreferred != preferredOrigin ||
                    !BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(preferredPlacementRect.position, preferredPlacementRect.size, grid) ||
                    !blockerData.Blocked.IsCreated) return false;
                for (int y=preferredPlacementRect.yMin;y<preferredPlacementRect.yMax;y++)
                    for (int x=preferredPlacementRect.xMin;x<preferredPlacementRect.xMax;x++)
                        if(blockerData.Blocked.IsSet(y*grid.Width+x)) return false;
                var buildings=context.WallValidationContext.RuntimeBuildings;
                if(buildings!=null)
                    foreach(var pair in buildings)
                    {
                        var existing=pair.Value;
                        if(existing!=null && existing.Definition!=null &&
                            preferredPlacementRect.Overlaps(new RectInt(existing.OriginCell,existing.Definition.FootprintCells))) return false;
                    }
                originCell=preferredOrigin;return true;
            }
            int footprintSearchRadius = Mathf.Max(placementFootprint.x, placementFootprint.y) * 4;
            // Spawn relocation must not lose its reach when an oversized model reservation is corrected.
            int maxSearchRadius = Mathf.Max(
                80,
                Mathf.Min(
                    160,
                    Mathf.Max(
                        footprintSearchRadius,
                        preferredPlacementRect.width,
                        preferredPlacementRect.height)));
            if (requirePreferredOrigin)
            {
                if (clampedPreferred != preferredOrigin) { if(citywideDiagnostic)Debug.LogError("[CitywidePlacement] reject=clamped-preferred");return false; }
                maxSearchRadius = 0;
            }
            for (int radius = 0; radius <= maxSearchRadius; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (radius > 0 && Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                            continue;

                        Vector2Int candidate = clampedPreferred + new Vector2Int(dx, dy);
                        RectInt candidateRect = context.GetEffectivePlacementRect(definition, candidate, grid, rotateVertical);
                        if (!BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(definition) && context.HasCachedInvalidCellInFootprint != null &&
                            context.HasCachedInvalidCellInFootprint(candidateRect.position, candidateRect.size))
                        {
                            if(trustDiagnostic)LogTrustPlacementCells(context,definition,candidateRect,grid,roads,blockerData,"cached-invalid-prefix");
                            if(citywideDiagnostic)LogCitywidePlacementCells(context,definition,candidateRect,grid,roads,blockerData,"cached-invalid-prefix");
                            continue;
                        }

                        if (!context.IsPlacementValid(definition, candidate, placementFootprint, rotateVertical, grid, roads, blockerData))
                        {
                            if(trustDiagnostic)LogTrustPlacementCells(context,definition,candidateRect,grid,roads,blockerData,"placement-validation");
                            if(citywideDiagnostic)LogCitywidePlacementCells(context,definition,candidateRect,grid,roads,blockerData,"placement-validation");
                            if(citywideDiagnostic&&definition.Prefab?.name=="Building_Citywide_ReserveDepot")
                                SurveyCitywideReserveCandidates(context,grid,roads,blockerData,preferredOrigin);
                            continue;
                        }

                        originCell = candidate;
                        return true;
                    }
                }
            }

            return false;
        }
        private static void SurveyCitywideReserveCandidates(Context context,GridConfig grid,DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockers,Vector2Int rejectedOrigin)
        {
            var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;
            var em=world.EntityManager;using var query=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if(query.CalculateEntityCount()!=1)return;
            var surface=em.GetComponentData<MapSurfaceComponent>(query.GetSingletonEntity());
            var cache=new BuildingPlacementAuthoredRoadCache();cache.Ensure(em,query,grid);
            bool west=grid.Origin.x+rejectedOrigin.x*grid.CellSize<1150;
            Vector2Int Cell(int x,int z)=>new(Mathf.FloorToInt((x-grid.Origin.x)/grid.CellSize),Mathf.FloorToInt((z-grid.Origin.z)/grid.CellSize));
            bool Survey(Vector2Int origin,bool log,bool stopAtBlocked)
            {
                var size=new Vector2Int(26,25);int cached=0,startup=0,road=0,blocked=0,sidewalk=0,water=0,authoredRoad=0,missingSurface=0;
                float minHeight=float.PositiveInfinity,maxHeight=float.NegativeInfinity,maxSlope=0;
                var sidewalkMask=cache.GetSidewalks();var waterMask=cache.GetWater();
                for(int y=origin.y;y<origin.y+size.y;y++)for(int x=origin.x;x<origin.x+size.x;x++)
                {
                    if(x<0||y<0||x>=grid.Width||y>=grid.Height)return false;
                    int index=y*grid.Width+x;var cell=new Vector2Int(x,y);
                    bool c=context.HasCachedInvalidCellInFootprint?.Invoke(cell,Vector2Int.one)==true;
                    bool v=context.WallValidationContext.HasRoadInFootprint?.Invoke(grid,cell,Vector2Int.one)==true;
                    bool r=index<roads.Length&&roads[index].Value!=0;
                    bool b=blockers.Blocked.IsCreated&&blockers.Blocked.IsSet(index);
                    bool sw=sidewalkMask!=null&&sidewalkMask[index],w=waterMask!=null&&waterMask[index];
                    bool ar=cache.Overlaps(grid,cell,Vector2Int.one);
                    if(stopAtBlocked&&(c||v||r||b||sw||w||ar))return false;
                    if(c)cached++;if(v)startup++;if(r)road++;if(b)blocked++;if(sw)sidewalk++;if(w)water++;if(ar)authoredRoad++;
                    if(surface.SurfaceBlob.IsCreated&&MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value,new Unity.Mathematics.int2(x,y),out var sample))
                    {minHeight=Mathf.Min(minHeight,sample.Height);maxHeight=Mathf.Max(maxHeight,sample.Height);maxSlope=Mathf.Max(maxSlope,sample.SlopeDegrees);}
                    else missingSurface++;
                }
                bool terrainClear=cached+startup+road+blocked+sidewalk+water+authoredRoad==0;
                if(log)Debug.Log($"[CitywideReserveSurvey] passiveOnly=true west={west} origin={origin} world=({grid.Origin.x+origin.x*grid.CellSize},{grid.Origin.z+origin.y*grid.CellSize}) footprint={size} clearTerrain={terrainClear} cached={cached} startupRoad={startup} gridRoad={road} blocker={blocked} authoredRoad={authoredRoad} sidewalk={sidewalk} water={water} missingPrimary={missingSurface} minHeight={minHeight} maxHeight={maxHeight} maxSlope={maxSlope}");
                return terrainClear;
            }
            if(Survey(west?Cell(1076,378):Cell(1224,378),true,false))return;
            int found=0;int minX=west?1048:1208,maxX=west?1105:1265;
            for(int z=327;z<=397&&found<5;z++)for(int x=minX;x<=maxX&&found<5;x++)
            {
                var candidate=Cell(x,z);if(!Survey(candidate,false,true))continue;
                Survey(candidate,true,false);found++;x+=12;
            }
            Debug.Log($"[CitywideReserveSurvey] passiveOnly=true west={west} nearbyClearCandidates={found}");
        }
        private static bool IsTrustPlacementDiagnostic()
        {
            var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return false;
            using var query=world.EntityManager.CreateEntityQuery(typeof(CampaignMissionRuntimeComponent));
            return query.CalculateEntityCount()==1&&query.GetSingleton<CampaignMissionRuntimeComponent>().MissionId.Equals(new Unity.Collections.FixedString64Bytes(Game.Missions.Contracts.CampaignMissionSequence.TrustUnderFire));
        }
        private static void LogTrustPlacementCells(Context context,BuildingDefinition definition,RectInt rect,GridConfig grid,
            DynamicBuffer<GridRoad> roads,DynamicBlockerComponent blockers,string reason)
        {
            var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;
            var em=world.EntityManager;using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            var cache=new BuildingPlacementAuthoredRoadCache();cache.Ensure(em,surfaces,grid);
            var sidewalks=cache.GetSidewalks();var waters=cache.GetWater();
            int cached=0,startup=0,road=0,blocked=0,authored=0,sidewalk=0,water=0,outside=0;string first="none";
            for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++)
            {
                var cell=new Vector2Int(x,y);bool o=x<0||y<0||x>=grid.Width||y>=grid.Height;int index=y*grid.Width+x;
                bool c=!o&&context.HasCachedInvalidCellInFootprint?.Invoke(cell,Vector2Int.one)==true;
                bool v=!o&&context.WallValidationContext.HasRoadInFootprint?.Invoke(grid,cell,Vector2Int.one)==true;
                bool r=!o&&index<roads.Length&&roads[index].Value!=0,b=!o&&blockers.Blocked.IsCreated&&blockers.Blocked.IsSet(index);
                bool ar=!o&&cache.Overlaps(grid,cell,Vector2Int.one),sw=!o&&sidewalks!=null&&index<sidewalks.Length&&sidewalks[index],w=!o&&waters!=null&&index<waters.Length&&waters[index];
                if(c)cached++;if(v)startup++;if(r)road++;if(b)blocked++;if(ar)authored++;if(sw)sidewalk++;if(w)water++;if(o)outside++;
                if(first=="none"&&(c||v||r||b||ar||sw||w||o))first=$"local={cell} world=({grid.Origin.x+x*grid.CellSize},{grid.Origin.z+y*grid.CellSize}) cached={c} startupRoad={v} gridRoad={r} blocked={b} authoredRoad={ar} sidewalk={sw} water={w} outside={o}";
            }
            string overlaps="none";
            if(context.WallValidationContext.RuntimeBuildings!=null)foreach(var pair in context.WallValidationContext.RuntimeBuildings)
            {var existing=pair.Value;if(existing?.Definition!=null&&rect.Overlaps(new RectInt(existing.OriginCell,existing.Definition.FootprintCells)))overlaps+=$" runtimeId={pair.Key} origin={existing.OriginCell} footprint={existing.Definition.FootprintCells}";}
            Debug.LogError($"[TrustPlacement] reject={reason} prefab={definition.Prefab?.name} rect={rect} cached={cached} startupRoad={startup} gridRoad={road} blockers={blocked} authoredRoad={authored} sidewalk={sidewalk} water={water} outside={outside} first={first} overlaps={overlaps}");
            if(definition.Prefab?.name!="Building_Trust_ReserveDepot")return;
            // Passive survey only: every candidate must pass the unchanged actual placement callback.
            int found=0;
            for(int z=590;z<=650&&found<3;z+=2)for(int x=780;x<=890&&found<3;x+=2)
            {
                var cell=new Vector2Int(Mathf.FloorToInt((x-grid.Origin.x)/grid.CellSize),Mathf.FloorToInt((z-grid.Origin.z)/grid.CellSize));
                var size=context.GetPlacementFootprint(definition,false);var candidate=context.GetEffectivePlacementRect(definition,cell,grid,false);
                if(context.HasCachedInvalidCellInFootprint?.Invoke(candidate.position,candidate.size)==true)continue;
                if(!context.IsPlacementValid(definition,cell,size,false,grid,roads,blockers))continue;
                Debug.Log($"[TrustReserveSurvey] passiveOnly=true actualPlacementValid=true world=({x},{z}) local={cell} footprint={size} effectiveRect={candidate}");found++;
            }
            Debug.Log($"[TrustReserveSurvey] passiveOnly=true qualifiedCandidates={found}");
        }
        private static bool IsCitywidePlacementDiagnostic()
        {
            var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return false;
            using var query=world.EntityManager.CreateEntityQuery(typeof(CampaignMissionRuntimeComponent));
            return query.CalculateEntityCount()==1&&query.GetSingleton<CampaignMissionRuntimeComponent>().MissionId.Equals(new Unity.Collections.FixedString64Bytes(Game.Missions.Contracts.CampaignMissionSequence.CitywideAlert));
        }
        private static void LogCitywidePlacementCells(Context context,BuildingDefinition definition,RectInt rect,GridConfig grid,
            DynamicBuffer<GridRoad> roads,DynamicBlockerComponent blockers,string reason)
        {
            int cached=0,road=0,blocked=0,visualRoad=0,outside=0;string first="none";
            for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++)
            {
                var cell=new Vector2Int(x,y);bool c=context.HasCachedInvalidCellInFootprint?.Invoke(cell,Vector2Int.one)==true;
                bool v=context.WallValidationContext.HasRoadInFootprint?.Invoke(grid,cell,Vector2Int.one)==true;
                bool o=x<0||y<0||x>=grid.Width||y>=grid.Height;int index=y*grid.Width+x;
                bool r=!o&&index<roads.Length&&roads[index].Value!=0;
                bool b=!o&&blockers.Blocked.IsCreated&&blockers.Blocked.IsSet(index);
                if(c)cached++;if(v)visualRoad++;if(r)road++;if(b)blocked++;if(o)outside++;
                if(first=="none"&&(c||v||r||b||o))first=$"cell={cell} cached={c} startupRoad={v} gridRoad={r} blocked={b} outside={o}";
            }
            Debug.LogError($"[CitywidePlacement] reject={reason} prefab={definition.Prefab?.name} effectiveRect={rect} cachedCells={cached} startupRoadCells={visualRoad} gridRoadCells={road} blockerCells={blocked} outsideCells={outside} first={first}");
        }
    }
}
