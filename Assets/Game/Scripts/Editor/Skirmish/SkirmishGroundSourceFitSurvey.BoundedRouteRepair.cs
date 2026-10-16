#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        sealed class RouteProfile {public Entity Prefab;public byte Faction;public int2 Size;public UnitMovementBehavior Behavior;}
        sealed class RouteAuthoringCap:Exception {}
        static readonly int2[] RepairNeighborOffsets={new int2(0,0),new int2(-1,0),new int2(1,0),new int2(0,-1),new int2(0,1),
            new int2(-1,-1),new int2(-1,1),new int2(1,-1),new int2(1,1)};

        static JObject ProposeBoundedNativeRouteRepair(SkirmishMapLayoutConfig layout,List<SkirmishStaticAuthoringBudget> budgets,JArray baseline,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface)
        {
            string original=JsonUtility.ToJson(layout);var data=JObject.Parse(original);
            var report=new JObject{["scope"]="ExplicitLogicalRouteRevision_ActualNormalPathQueries_NotMissionAcceptance",
                ["maximumMovedMacroCenters"]=4,["maximumAddedAnchors"]=30,["maximumAdditionalNativeQueries"]=2048,
                ["sourceFitCertified"]=false,["provisionalTravelOnly"]=true};
            var profiles=new List<RouteProfile>();var profileKeys=new HashSet<string>(StringComparer.Ordinal);
            var profileFacts=new JArray();report["actualNativeProfiles"]=profileFacts;
            foreach(var force in budgets.SelectMany(b=>b.Forces))
            {
                var prefab=FindNativePrefab(em,force.RuntimePrefabKey);
                Require(prefab!=Entity.Null&&em.HasComponent<UnitFootprint>(prefab),"Actual canonical route actor required");
                if(em.HasComponent<UnitAirMovement>(prefab))continue;
                Require(em.HasComponent<UnitMovementBehavior>(prefab),"Actual ground movement behavior required");
                var size=em.GetComponentData<UnitFootprint>(prefab).Size;var behavior=em.GetComponentData<UnitMovementBehavior>(prefab);
                string key=Ground30PathQueryKey(force.FactionId,size,behavior,default,default);
                if(profileKeys.Add(key))
                {
                    profiles.Add(new RouteProfile{Prefab=prefab,Faction=force.FactionId,Size=size,Behavior=behavior});
                    profileFacts.Add(new JObject{["profileKey"]=key,["faction"]=force.FactionId,["roleId"]=force.RoleId,["runtimePrefabKey"]=force.RuntimePrefabKey,
                        ["nativePrefabIndex"]=prefab.Index,["nativePrefabVersion"]=prefab.Version,["footprintX"]=size.x,["footprintZ"]=size.y,
                        ["allowIdleWander"]=behavior.AllowIdleWander,["usesVehicleMotion"]=behavior.UsesVehicleMotion});
                }
            }
            Require(profiles.Count>0,"Actual ground route profiles required; air lifecycle is separate");
            var cache=new Dictionary<string,JObject>(StringComparer.Ordinal);var invalidGoals=new HashSet<int2>();
            foreach(JObject size in baseline)foreach(JObject row in (JArray)size["queries"])
            {
                string key=(string)row["exactInputKey"];
                if(!cache.ContainsKey(key))cache.Add(key,(JObject)row.DeepClone());
                if((int?)row["failureCode"]==7||(int?)row["failureCode"]==8)
                    invalidGoals.Add(new int2((int)row["toCell"][0],(int)row["toCell"][1]));
            }
            int calls=0,added=0,moved=0;var probes=new JArray();report["additionalNativeQueries"]=probes;
            var decisions=new JArray();report["decisions"]=decisions;
            JObject Probe(RouteProfile profile,int2 from,int2 to)
            {
                string key=Ground30PathQueryKey(profile.Faction,profile.Size,profile.Behavior,from,to);
                if(cache.TryGetValue(key,out var found))return found;
                if(calls>=2048)throw new RouteAuthoringCap();
                calls++;var row=new JObject{["exactInputKey"]=key,["fromCell"]=new JArray(from.x,from.y),["toCell"]=new JArray(to.x,to.y)};
                QueryPath(em,gridEntity,grid,surface,profile.Prefab,from,to,row,profile.Faction);
                cache.Add(key,row);probes.Add(row);return row;
            }
            bool Exact(JObject row)=>(string)row["status"]=="NativeQueryFoundExactGoal"&&(int?)row["failureCode"]==0;
            bool Segment(int2 a,int2 b,JObject route)
            {
                if(math.all(a==b))return false;
                foreach(var profile in profiles)
                {
                    if(profile.Behavior.UsesVehicleMotion!=0&&(bool?)route["AllowsVehicles"]!=true)continue;
                    if(!Exact(profile.Faction==2?Probe(profile,b,a):Probe(profile,a,b)))return false;
                }
                return true;
            }
            Vector2 World(int2 cell)=>new Vector2(grid.Origin.x+(cell.x+.5f)*grid.CellSize,grid.Origin.z+(cell.y+.5f)*grid.CellSize);
            bool Inside(int2 cell)
            {
                var p=World(cell);return p.x>=layout.OriginX&&p.y>=layout.OriginZ&&
                    p.x<=layout.OriginX+layout.WorldWidthMetres&&p.y<=layout.OriginZ+layout.WorldDepthMetres;
            }
            bool Endpoint(int2 candidate)
            {
                if(!Inside(candidate))return false;
                foreach(var profile in profiles)
                {
                    bool reached=false;
                    foreach(var neighbor in RepairNeighborOffsets.Skip(1).Take(4))
                        if(Exact(Probe(profile,candidate+neighbor,candidate))){reached=true;break;}
                    if(!reached)return false;
                }
                return true;
            }
            var anchors=(JArray)data["anchors"];var routes=(JArray)data["routes"];
            JObject Anchor(string id)=>anchors.OfType<JObject>().Single(a=>(string)a["AnchorId"]==id);
            int2 Cell(JObject anchor)=>GridUtils.WorldToCell(grid,new float3((float)anchor["WorldX"],0,(float)anchor["WorldZ"]));
            void SetPoint(JObject anchor,int2 cell)
            {
                var p=World(cell);anchor["WorldX"]=p.x;anchor["WorldZ"]=p.y;
                anchor["NormalizedU"]=(p.x-layout.OriginX)/layout.WorldWidthMetres;
                anchor["NormalizedV"]=(p.y-layout.OriginZ)/layout.WorldDepthMetres;
            }
            try
            {
                var originalIds=new HashSet<string>(routes.OfType<JObject>().SelectMany(r=>((JArray)r["WaypointAnchorIds"]).Values<string>()),StringComparer.Ordinal);
                foreach(string id in originalIds.OrderBy(v=>v,StringComparer.Ordinal))
                {
                    var anchor=Anchor(id);var cell=Cell(anchor);if(!invalidGoals.Contains(cell))continue;
                    if(moved>=4){report["refused"]="Macro repair window exhausted";return report;}
                    if(layout.Pads.Any(p=>p.AnchorId==id))
                    {report["refused"]="Invalid pad-owned macro endpoint requires explicit owner revision";return report;}
                    bool repaired=false;
                    foreach(var offset in RepairNeighborOffsets)
                    {
                        var candidate=cell+offset;if(!Endpoint(candidate))continue;
                        SetPoint(anchor,candidate);moved++;repaired=true;
                        decisions.Add(new JObject{["kind"]="MovedSourceInvalidMacroCenter",["anchorId"]=id,["fromCell"]=new JArray(cell.x,cell.y),["toCell"]=new JArray(candidate.x,candidate.y)});break;
                    }
                    if(!repaired){report["refused"]="No exact local native endpoint candidate: "+id;return report;}
                }
                var globalStart=World(Cell(Anchor((string)routes[0]["WaypointAnchorIds"][0])));
                var mainIds=(JArray)routes[0]["WaypointAnchorIds"];
                var globalEnd=World(Cell(Anchor((string)mainIds[mainIds.Count-1])));
                int routeIndex=0;
                foreach(JObject route in routes)
                {
                    var ids=((JArray)route["WaypointAnchorIds"]).Values<string>().ToArray();var revised=new JArray(ids[0]);
                    for(int leg=1;leg<ids.Length;leg++)
                    {
                        var start=Cell(Anchor(ids[leg-1]));var goal=Cell(Anchor(ids[leg]));
                        if(Segment(start,goal,route)){revised.Add(ids[leg]);continue;}
                        int2 cursor=start;bool connected=false;
                        for(int slot=1;slot<=2;slot++)
                        {
                            var seed=(int2)math.floor(math.lerp((float2)start,(float2)goal,slot/3f));bool selected=false;
                            foreach(var offset in RepairNeighborOffsets)
                            {
                                var candidate=seed+offset*16;var point=World(candidate);
                                if(!Inside(candidate)||!RouteCandidateWithinSemantics(point,World(start),World(goal),globalStart,globalEnd,slot/3f)||
                                    !Segment(cursor,candidate,route))continue;
                                if(added>=30){report["refused"]="Steering anchor window exhausted";return report;}
                                string id="anchor."+layout.LayoutId+".native-steer."+routeIndex+"."+leg+"."+slot;
                                Require(!anchors.OfType<JObject>().Any(a=>(string)a["AnchorId"]==id),"Fresh logical steering identity required");
                                var anchor=new JObject{["AnchorId"]=id,["RoleId"]="route.steer."+routeIndex+"."+leg+"."+slot};
                                SetPoint(anchor,candidate);anchors.Add(anchor);revised.Add(id);added++;cursor=candidate;selected=true;
                                decisions.Add(new JObject{["kind"]="AddedNativeMeasuredSteeringAnchor",["routeId"]=route["RouteId"],["originalLeg"]=leg,["slot"]=slot,["anchor"]=anchor.DeepClone()});break;
                            }
                            if(!selected)break;
                            if(Segment(cursor,goal,route)){connected=true;break;}
                        }
                        if(!connected){report["refused"]="Bounded native steering did not connect original leg: "+route["RouteId"]+"/"+leg;return report;}
                        revised.Add(ids[leg]);
                    }
                    route["WaypointAnchorIds"]=revised;
                    float oldLength=(float)route["LengthMetres"],length=0;
                    for(int i=1;i<revised.Count;i++)
                    {
                        var a=Anchor((string)revised[i-1]);var b=Anchor((string)revised[i]);
                        float x=(float)b["WorldX"]-(float)a["WorldX"],z=(float)b["WorldZ"]-(float)a["WorldZ"];length+=(float)Math.Sqrt(x*x+z*z);
                    }
                    Require(float.IsFinite(oldLength)&&oldLength>0&&float.IsFinite(length)&&length>0,"Finite existing route rates required");
                    foreach(string metric in new[]{"InfantryTransitSeconds","GroundTransitSeconds","AirTransitSeconds","InfantryFirstContactSeconds","GroundFirstContactSeconds"})
                    {float value=(float)route[metric];Require(float.IsFinite(value)&&value>0,"Unchanged existing rate required");route[metric]=value*(length/oldLength);}
                    route["LengthMetres"]=length;route["ProvisionalTimes"]=true;routeIndex++;
                }
                JsonUtility.FromJsonOverwrite(data.ToString(Newtonsoft.Json.Formatting.None),layout);
                report["status"]="ProposedNativeMeasuredSegments_FinalFacilityReservationsAndIndependenceStillRequired";
                report["proposedLayoutJson"]=JsonUtility.ToJson(layout);return report;
            }
            catch(RouteAuthoringCap){report["refused"]="Additional native authoring query cap exhausted";return report;}
            finally
            {
                report["additionalNativeQueryCount"]=calls;report["movedMacroCenters"]=moved;report["addedSteeringAnchors"]=added;
                if(report["status"]==null)JsonUtility.FromJsonOverwrite(original,layout);
            }
        }

        static bool RouteCandidateWithinSemantics(Vector2 point,Vector2 a,Vector2 b,Vector2 globalStart,Vector2 globalEnd,float t)
        {
            if(!float.IsFinite(point.x)||!float.IsFinite(point.y))return false;
            var segment=b-a;float u=segment.sqrMagnitude==0?0:Mathf.Clamp01(Vector2.Dot(point-a,segment)/segment.sqrMagnitude);
            if(Vector2.Distance(point,a+u*segment)>32.0001f)return false;
            var axis=globalEnd-globalStart;var original=Vector2.Lerp(a,b,t)-globalStart;var proposed=point-globalStart;
            float originalSide=axis.x*original.y-axis.y*original.x,proposedSide=axis.x*proposed.y-axis.y*proposed.x;
            // A flank steering point cannot cross the main staging axis. Main
            // route candidates remain in their original bounded leg corridor.
            return Math.Abs(originalSide)<.001f||originalSide*proposedSide>0;
        }

        static bool PreferencePathsIndependent(SkirmishMapLayoutConfig layout,GridConfig grid,JObject measured,out JObject evidence)
        {
            evidence=new JObject{["scope"]="ActualCanonicalProfileNativeLegs_ExistingSharedEndpointPolicy",["sourceFitCertified"]=false};
            var queries=(JArray)measured["queries"];var references=(JArray)measured["roleReferences"];
            var rows=new JArray();evidence["profiles"]=rows;
            var min=new int2[2];var max=new int2[2];
            for(byte side=1;side<=2;side++)
            {
                if(!layout.TryGetPad(side,Game.Configs.SkirmishLegalPadKind.GroundStaging,out var pad))return false;
                if(!layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z))return false;
                var center=GridUtils.WorldToCell(grid,new float3(x,0,z));
                var half=math.min((int2)math.floor(new float2(pad.WidthMetres,pad.DepthMetres)/(2*grid.CellSize)),new int2(SharedEndpointRadiusCells));
                if(!math.all(half>0))return false;min[side-1]=center-half;max[side-1]=center+half;
            }
            var groups=references.OfType<JObject>().Where(r=>r["nativeQueryId"]!=null).GroupBy(r=>
                string.Join("/",((string)queries[(int)r["nativeQueryId"]]["exactInputKey"]).Split('/').Take(5)));
            bool all=true;int count=0;
            foreach(var profile in groups)
            {
                count++;int force=profile.Min(r=>(int)r["forceIndex"]);var paths=new List<List<int2>>();int clearance=0;bool complete=true;
                var row=new JObject{["profileKey"]=profile.Key};rows.Add(row);
                foreach(var route in layout.Routes)
                {
                    var legs=profile.Where(r=>(int)r["forceIndex"]==force&&(string)r["routeId"]==route.RouteId).OrderBy(r=>(int)r["leg"])
                        .Select(r=>PreferenceNativeLeg((JObject)queries[(int)r["nativeQueryId"]],r)).ToArray();
                    if(!TryAssembleExactNativeLegs(legs,out var path,out int radius,out string error)){complete=false;row["assemblyError"]=error;}
                    paths.Add(path);clearance=Math.Max(clearance,radius);
                }
                row["completeExactNativePaths"]=complete;
                if(!complete||paths.Count!=3){all=false;continue;}
                var firstQuery=(JObject)queries[(int)profile.First()["nativeQueryId"]];
                var footprint=new int2((int)firstQuery["footprintX"],(int)firstQuery["footprintZ"]);
                var frame=PilotRoundPadCells(layout.OriginX,layout.OriginZ,layout.OriginX+layout.WorldWidthMetres,layout.OriginZ+layout.WorldDepthMetres,grid,false);
                bool inside=paths.All(path=>RasterizePilotCenterline(path).All(cell=>
                {
                    var origin=UnitFootprintUtility.GetMinCell(cell,footprint);
                    return PilotCompoundContains(frame,new RectInt(origin.x,origin.y,footprint.x,footprint.y));
                }));
                row["fullNativePathFootprintsInsideLogicalFrame"]=inside;
                if(!inside){all=false;continue;}
                int entry=PreferenceEntryIndex((int)profile.First()["faction"]);
                var comparison=ComparePilotApproaches(paths,clearance,min[entry],max[entry],min[1-entry],max[1-entry]);
                foreach(var property in comparison.Properties())row[property.Name]=property.Value.DeepClone();
                if((bool?)row["independentUnderDeclaredProbePolicy"]!=true)all=false;
            }
            evidence["allProfilesIndependent"]=all&&count>0;return all&&count>0;
        }

        static JObject PreferenceNativeLeg(JObject query,JObject reference)
        {
            var leg=(JObject)query.DeepClone();leg["leg"]=reference["leg"].DeepClone();
            leg["requestedStartCell"]=leg["fromCell"].DeepClone();leg["requestedGoalCell"]=leg["toCell"].DeepClone();return leg;
        }
        static int PreferenceEntryIndex(int faction)
        {Require(faction==1||faction==2,"Exact native profile faction required");return faction-1;}

        public static void RunBoundedRouteRepairControls()
        {
            Require(RepairNeighborOffsets.Length==9&&RepairNeighborOffsets.Distinct().Count()==9&&RepairNeighborOffsets.Contains(new int2(-1,0)),"Finite native endpoint window includes measured west candidate");
            Require(RouteCandidateWithinSemantics(new Vector2(20,20),new Vector2(0,20),new Vector2(60,20),Vector2.zero,new Vector2(100,0),1f/3)&&
                !RouteCandidateWithinSemantics(new Vector2(20,-1),new Vector2(0,20),new Vector2(60,20),Vector2.zero,new Vector2(100,0),1f/3),"Flank cannot silently become opposite-side route");
            Require(!RouteCandidateWithinSemantics(new Vector2(20,60),Vector2.zero,new Vector2(60,0),Vector2.zero,new Vector2(100,0),1f/3)&&
                !RouteCandidateWithinSemantics(new Vector2(float.NaN,0),Vector2.zero,new Vector2(60,0),Vector2.zero,new Vector2(100,0),1f/3),"Outside32m/nonfinite candidate refuses");
            // Exact emitted q45 from actual Joint candidate0/Standard. Mission
            // artifact SHA2fc85ad7e7a395f7b839e113f1ff4232fc0d41c7f79a9869dc78c7b510b6f312.
            // This is a producer-schema regression, not a new native fit claim.
            var actual=JObject.Parse(@"{""queryId"":45,""exactInputKey"":""2/3/3/1/1/1352/650/1268/650"",""fromCell"":[1352,650],""toCell"":[1268,650],""status"":""NativeQueryFoundExactGoal"",""failureCode"":0,""expansions"":0,""pathCells"":[[1352,650],[1268,650]],""footprintX"":3,""footprintZ"":3,""nativeQueryFaction"":2,""dynamicBlockersIncluded"":false,""liveOccupancyIncluded"":false,""plannedFacilityBlockerScope"":""ExactAcceptedStaticCandidateRectangles_NotLiveDynamicOccupancy""}");
            var leg=PreferenceNativeLeg(actual,new JObject{["leg"]=1});
            Require(TryAssembleExactNativeLegs(new[]{leg},out var path,out int radius,out _)&&path[0].Equals(new int2(1352,650))&&radius==2&&PreferenceEntryIndex(2)==1,
                "Actual native producer endpoints/leg/profile direction must join existing assembler");
            actual["toCell"]=new JArray(1269,650);
            Require(!TryAssembleExactNativeLegs(new[]{PreferenceNativeLeg(actual,new JObject{["leg"]=1})},out _,out _,out _),"Contradictory native endpoint receipt refuses");
            Debug.Log("[BoundedRouteRepairControls] result=Passed controls=5 macroMoves=4 endpointCandidates=9 steeringPerLeg=2 totalSteering=30 nativeQueryCap=2048 actualNativeFitPending=1");
        }
    }
}
#endif
