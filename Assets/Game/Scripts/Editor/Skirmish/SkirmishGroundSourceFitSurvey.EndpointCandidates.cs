#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        static void SurveyUnconfiguredCompoundCandidates(OperationMapDefinition source,JObject report)
        {
            const string proposalPath="Design/AgentReports/SkirmishExpansion/GroundPilots/source-fit-analysis-20261015-02.json";
            Require(Hash(proposalPath)=="ed1ac3ea0240affaa91c1ab8d093f17ec1024babaff4008a8032b30850040300","Reviewed serialized-grid proposal bytes changed");
            var proposal=JObject.Parse(File.ReadAllText(proposalPath));
            var rows=((JArray)proposal["sources"]).OfType<JObject>().Where(r=>(string)r["group"]==(string)report["group"]).ToArray();
            Require(rows.Length==1&&(string)rows[0]["source_sha256"]==(string)report["sourceAssetSha256"],"Exact proposal/source join required");
            var pair=rows[0]["compound_rectangle_proposals"]?["separated_pair"] as JArray;
            Require(pair!=null&&pair.Count==2,"Two measured separated rectangle proposals required");
            using var world=StreamingWorld();Load(world,source.NavigationMetadata.AuthoredSubSceneGuid);
            Load(world,AssetDatabase.AssetPathToGUID(Support));
            using var runtimeSurface=PublishMissingPinnedSurface(world,source,(string)report["surfacePath"],report);
            var em=world.EntityManager;
            using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(GridRoad));
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            Require(grids.CalculateEntityCount()==1&&surfaces.CalculateEntityCount()==1,"Exact native grid/surface required for compound probes");
            var grid=grids.GetSingleton<GridConfig>();var gridEntity=grids.GetSingletonEntity();var surface=surfaces.GetSingleton<MapSurfaceComponent>();
            Require(grid.Width==source.GridMetadata.Dimensions.x&&grid.Height==source.GridMetadata.Dimensions.y&&
                grid.CellSize==source.GridMetadata.CellSize&&math.all(grid.Origin==(float3)source.GridMetadata.Origin)&&
                surface.Dimensions.x==grid.Width&&surface.Dimensions.y==grid.Height&&surface.CellSize==grid.CellSize&&
                math.all(surface.GridOrigin==grid.Origin)&&surface.HasSurfaceData!=0&&surface.SurfaceBlob.IsCreated,"Pinned native grid/surface mismatch");
            var rectangles=new JArray();report["nativeCompoundRectangleProbes"]=rectangles;
            var centers=new List<int2>();var walkable=em.GetBuffer<GridWalkable>(gridEntity,true);
            foreach(JObject p in pair)
            {
                int2 origin=new int2((int)p["originCellXZ"][0],(int)p["originCellXZ"][1]);
                int2 size=new int2((int)p["sizeCellsXZ"][0],(int)p["sizeCellsXZ"][1]);
                Require(size.x>0&&size.y>0&&size.x*size.y<=20000,"Bounded explicit compound engineering rectangle required");
                int blocked=0,sampled=0;float min=float.PositiveInfinity,max=float.NegativeInfinity;
                for(int z=origin.y;z<origin.y+size.y;z++)for(int x=origin.x;x<origin.x+size.x;x++)
                {
                    var cell=new int2(x,z);Require(GridUtils.InBounds(cell,grid.Width,grid.Height),"Compound proposal outside actual native grid");
                    if(walkable[GridUtils.CellToIndex(cell,grid.Width)].Value==0)blocked++;
                    if(MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value,cell,out var sample))
                    {sampled++;min=math.min(min,sample.Height);max=math.max(max,sample.Height);}
                }
                centers.Add(origin+size/2);
                rectangles.Add(new JObject { ["proposal"]=p.DeepClone(),["actualBakedBlockedCells"]=blocked,["surfaceSampledCells"]=sampled,
                    ["heightMin"]=sampled>0?(JToken)min:JValue.CreateNull(),["heightMax"]=sampled>0?(JToken)max:JValue.CreateNull(),
                    ["scope"]="NativeGridAndExactProductionSurfaceOnly",["sceneryOverlap"]="NotMeasured",["fullFacilityPacking"]="NotMeasured",["runtimeFitCertified"]=false });
            }
            var routes=new JArray();report["nativeProposedCompoundDirectRoutes"]=routes;
            foreach(var role in new[]{SkirmishRoleKind.Tank,SkirmishRoleKind.Siege,SkirmishRoleKind.ApcHeavy})
            {
                var prefab=FindNativePrefab(em,SkirmishRoleCatalogConfig.RuntimePrefabKey(role));Require(prefab!=Entity.Null,"Exact native heavy prefab missing");
                var row=new JObject { ["role"]=role.ToString(),["requestedStartCell"]=new JArray(centers[0].x,centers[0].y),
                    ["requestedGoalCell"]=new JArray(centers[1].x,centers[1].y),["runtimeFitCertified"]=false };
                routes.Add(row);QueryPath(em,gridEntity,grid,surface,prefab,centers[0],centers[1],row);
            }
            report["nativeCompoundsAndRoutes"]="MeasuredCandidateRectanglesOnly_FullLogicalPlacementAndSceneryPending";
        }

        // Diagnostic proposals only: the same chosen anchor is reused in every
        // route and role. No per-leg endpoint substitution or asset mutation.
        static void SurveyLogicalEndpointCandidates(EntityManager em,Entity gridEntity,
            GridConfig grid,MapSurfaceComponent surface,SkirmishMapLayoutConfig layout,JObject report)
        {
            var candidates=new JArray();report["logicalEndpointCandidates"]=candidates;
            var repaired=new JArray();report["endpointCandidateRouteQueries"]=repaired;
            var roles=new[]{SkirmishRoleKind.Tank,SkirmishRoleKind.Siege,SkirmishRoleKind.ApcHeavy};
            var prefabs=roles.Select(role=>FindNativePrefab(em,SkirmishRoleCatalogConfig.RuntimePrefabKey(role))).ToArray();
            Require(prefabs.All(p=>p!=Entity.Null&&em.HasComponent<UnitFootprint>(p)&&em.HasComponent<UnitMovementBehavior>(p)),
                "All three exact heavy role templates required for common endpoint proposals");
            var selected=new Dictionary<string,int2>(StringComparer.Ordinal);
            foreach(string id in layout.Routes.Where(r=>r.AllowsVehicles).SelectMany(r=>r.WaypointAnchorIds).Distinct(StringComparer.Ordinal))
            {
                Require(layout.TryGetAnchorById(id,out var anchor),"Actual typed route anchor required");
                Require(layout.TryProjectLocalToMap(anchor.WorldX,anchor.WorldZ,out float x,out float z),
                    "Actual typed route anchor projection required");
                var requested=GridUtils.WorldToCell(grid,new float3(x,0,z));
                var row=new JObject { ["anchorId"]=id,["requestedCell"]=new JArray(requested.x,requested.y),
                    ["scope"]="StaticNativeHeavyEndpointProposalOnly",["searchRadiusCells"]=24,["runtimeFitCertified"]=false };
                candidates.Add(row);
                row["requestedEligibility"]=new JArray(prefabs.Select((prefab,index)=>EndpointEligibility(em,gridEntity,grid,surface,prefab,requested,roles[index])));
                bool found=false;int2 best=default;int bestDistance=int.MaxValue;
                // Deterministic nearest Euclidean distance, then x/z tie break.
                for(int dx=-24;dx<=24;dx++)for(int dz=-24;dz<=24;dz++)
                {
                    var cell=requested+new int2(dx,dz);int distance=dx*dx+dz*dz;
                    if(distance>24*24||distance>=bestDistance)continue;
                    if(prefabs.Any(prefab=>!EndpointLegal(em,gridEntity,grid,surface,prefab,cell)))continue;
                    best=cell;bestDistance=distance;found=true;
                }
                row["commonNativeStaticEndpointFound"]=found;
                if(!found)continue;
                row["proposedCell"]=new JArray(best.x,best.y);row["displacementCellsSquared"]=bestDistance;
                selected.Add(id,best);
            }
            foreach(var route in layout.Routes.Where(r=>r.AllowsVehicles))
                for(int roleIndex=0;roleIndex<roles.Length;roleIndex++)
                    for(int leg=1;leg<route.WaypointAnchorIds.Length;leg++)
                    {
                        var row=new JObject { ["routeId"]=route.RouteId,["role"]=roles[roleIndex].ToString(),["leg"]=leg,
                            ["scope"]="ActualPathfindBatchJob_ConsistentNearestStaticEndpoints",["runtimeFitCertified"]=false };
                        repaired.Add(row);
                        if(!selected.TryGetValue(route.WaypointAnchorIds[leg-1],out var from)||!selected.TryGetValue(route.WaypointAnchorIds[leg],out var to))
                        {row["status"]="NoCommonLegalEndpointWithinBound";continue;}
                        row["requestedStartCell"]=new JArray(from.x,from.y);row["requestedGoalCell"]=new JArray(to.x,to.y);
                        QueryPath(em,gridEntity,grid,surface,prefabs[roleIndex],from,to,row);
                    }
        }

        static JObject EndpointEligibility(EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,
            Entity prefab,int2 cell,SkirmishRoleKind role)
        {
            bool inside=GridUtils.InBounds(cell,grid.Width,grid.Height);
            bool walkable=inside&&em.GetBuffer<GridWalkable>(gridEntity,true)[GridUtils.CellToIndex(cell,grid.Width)].Value!=0;
            var size=em.GetComponentData<UnitFootprint>(prefab).Size;
            return new JObject { ["role"]=role.ToString(),["insideGrid"]=inside,["centerWalkable"]=walkable,
                ["nativeFootprintX"]=size.x,["nativeFootprintZ"]=size.y,["nativeStaticPlacementAndSurface"]=EndpointLegal(em,gridEntity,grid,surface,prefab,cell) };
        }
        static bool EndpointLegal(EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,Entity prefab,int2 cell)
        {
            if(!GridUtils.InBounds(cell,grid.Width,grid.Height))return false;
            var footprint=em.GetComponentData<UnitFootprint>(prefab);
            bool vehicle=UnitVehicleMovementUtility.IsVehicle(footprint,em.GetComponentData<UnitMovementBehavior>(prefab));
            return UnitPathPlacementValidation.CanPlaceForPathing(grid,em.GetBuffer<GridWalkable>(gridEntity,true).AsNativeArray(),
                default,default,default,default,default,default,default,Entity.Null,cell,footprint.Size,new int2(-100000,-100000),vehicle,true,1)&&
                new MapSurfaceTraversalValidation().CanTraverseFootprint(surface,1,grid,cell,footprint.Size,vehicle);
        }
    }
}
#endif
