#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Explicit conservative engineering probe policy, not player acceptance.
        const int SharedEndpointRadiusCells = 16;
        const int MinimumUniqueInteriorCells = 32;

        static bool EndpointCellInZone(int2 cell,int2 min,int2 max)
            => math.all(cell>=min)&&math.all(cell<=max);

        static bool TryReadAuthoredEndpointZones(SkirmishMapLayoutConfig layout,GridConfig grid,
            out int2[] min,out int2[] max,out JArray rows,out string error)
        {
            min=new int2[2];max=new int2[2];rows=new JArray();error=null;
            if(layout==null||layout.Pads==null||!float.IsFinite(grid.CellSize)||grid.CellSize<=0)
            {error="Missing layout/pads or invalid actual cell size";return false;}
            for(byte faction=1;faction<=2;faction++)
            {
                if(!layout.TryGetPad(faction,SkirmishLegalPadKind.GroundStaging,out var pad)||
                    !float.IsFinite(pad.WidthMetres)||!float.IsFinite(pad.DepthMetres)||
                    !layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z)||
                    !float.IsFinite(x)||!float.IsFinite(z))
                {error="Actual typed GroundStaging pad/projection required";return false;}
                var center=GridUtils.WorldToCell(grid,new float3(x,0,z));
                var half=math.min((int2)math.floor(new float2(pad.WidthMetres,pad.DepthMetres)/(2*grid.CellSize)),new int2(SharedEndpointRadiusCells));
                if(!math.all(half>0)){error="Positive actual bounded endpoint zone required";return false;}
                min[faction-1]=center-half;max[faction-1]=center+half;
                rows.Add(new JObject{["faction"]=faction,["actualTypedPadId"]=pad.PadId,
                    ["minCellInclusive"]=new JArray(min[faction-1].x,min[faction-1].y),
                    ["maxCellInclusive"]=new JArray(max[faction-1].x,max[faction-1].y),
                    ["scope"]="Actual source-pinned GroundStaging pad clipped to maximum16 cells; not a new clear compound"});
            }
            return true;
        }

        static bool TryPreflightAuthoredRouteEndpoints(SkirmishMapLayoutConfig layout,GridConfig grid,out JObject evidence)
        {
            evidence=new JObject{["scope"]="NecessaryAuthoredEndpointPolicyOnly_NotPathOrSourceFit",
                ["nativeQueriesExecuted"]=0,["sourceFitCertified"]=false,["missionReady"]=false};
            if(!TryReadAuthoredEndpointZones(layout,grid,out var min,out var max,out var zones,out string error))
            {evidence["accepted"]=false;evidence["rejection"]=error;return false;}
            evidence["actualAuthoredEndpointZones"]=zones;
            if(layout.Routes==null||layout.Routes.Length!=3)
            {evidence["accepted"]=false;evidence["rejection"]="Three authored approaches required";return false;}
            var routes=new JArray();evidence["routes"]=routes;int2? start=null,goal=null;
            foreach(var route in layout.Routes)
            {
                var ids=route.WaypointAnchorIds;float ax=0,az=0,bx=0,bz=0;
                if(ids==null||ids.Length<2||!layout.TryGetAnchorById(ids[0],out var a)||
                    !layout.TryGetAnchorById(ids[ids.Length-1],out var b)||
                    !layout.TryProjectLocalToMap(a.WorldX,a.WorldZ,out ax,out az)||
                    !layout.TryProjectLocalToMap(b.WorldX,b.WorldZ,out bx,out bz)||
                    !float.IsFinite(ax)||!float.IsFinite(az)||!float.IsFinite(bx)||!float.IsFinite(bz))
                {evidence["accepted"]=false;evidence["rejection"]="Actual authored route endpoint/projection required";return false;}
                var from=GridUtils.WorldToCell(grid,new float3(ax,0,az));var to=GridUtils.WorldToCell(grid,new float3(bx,0,bz));
                routes.Add(new JObject{["routeId"]=route.RouteId,["fromCell"]=new JArray(from.x,from.y),["toCell"]=new JArray(to.x,to.y)});
                if(start.HasValue&&(!start.Value.Equals(from)||!goal.Value.Equals(to)))
                {evidence["accepted"]=false;evidence["rejection"]="Approaches must share exact authored entry and exit";return false;}
                start=from;goal=to;
                if(!EndpointCellInZone(from,min[0],max[0])||!EndpointCellInZone(to,min[1],max[1]))
                {evidence["accepted"]=false;evidence["rejection"]="Native path endpoint is outside actual authored compound entry/exit";return false;}
            }
            evidence["accepted"]=true;return true;
        }

        public static void RunAuthoredRouteEndpointPreflightControls()
        {
            Require(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode endpoint controls required");
            int controls=0;
            void Check(bool ok,string message){Require(ok,message);controls++;}
            var layout=UnityEngine.ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            var grid=new GridConfig{Width=2048,Height=1024,CellSize=1,Origin=float3.zero};
            JObject Fixture(int sx=961,int sz=650,int ex=1352,int ez=650)
            {
                JObject Anchor(string id,float x,float z)=>new JObject{["AnchorId"]=id,["WorldX"]=x,["WorldZ"]=z};
                return new JObject{["originX"]=0,["originZ"]=0,["worldWidthMetres"]=2048,["worldDepthMetres"]=1024,
                    ["worldBinding"]=new JObject{["OriginX"]=0,["OriginZ"]=512,["ForwardX"]=1,["ForwardZ"]=0,["AcrossX"]=0,["AcrossZ"]=1,
                        ["ForwardMetres"]=2048,["AcrossMetres"]=1024,["PlayerAnchorId"]="stageP",["EnemyAnchorId"]="stageE"},
                    ["anchors"]=new JArray(Anchor("s",sx+.5f,sz+.5f),Anchor("m",1161.5f,674.5f),Anchor("e",ex+.5f,ez+.5f),
                        Anchor("stageP",970.7473f,650),Anchor("stageE",1352.0526f,650)),
                    ["pads"]=new JArray(new JObject{["PadId"]="stageP",["AnchorId"]="stageP",["Kind"]=(int)SkirmishLegalPadKind.GroundStaging,
                        ["FactionId"]=1,["CenterX"]=970.7473f,["CenterZ"]=650,["WidthMetres"]=19.85965f,["DepthMetres"]=14},
                        new JObject{["PadId"]="stageE",["AnchorId"]="stageE",["Kind"]=(int)SkirmishLegalPadKind.GroundStaging,
                        ["FactionId"]=2,["CenterX"]=1352.0526f,["CenterZ"]=650,["WidthMetres"]=19.85965f,["DepthMetres"]=14}),
                    ["routes"]=new JArray(Enumerable.Range(0,3).Select(i=>(JToken)new JObject{["RouteId"]="route"+i,["WaypointAnchorIds"]=new JArray("s","m","e")}))};
            }
            bool LoadAndPreflight(JObject data)
            {
                UnityEngine.JsonUtility.FromJsonOverwrite(data.ToString(Newtonsoft.Json.Formatting.None),layout);
                string before=UnityEngine.JsonUtility.ToJson(layout);
                bool accepted=TryPreflightAuthoredRouteEndpoints(layout,grid,out var evidence);
                Require(before==UnityEngine.JsonUtility.ToJson(layout)&&!(bool)evidence["sourceFitCertified"]&&(int)evidence["nativeQueriesExecuted"]==0,
                    "Preflight must be pure, query-free and never source-fit proof");
                return accepted;
            }
            try
            {
                Check(LoadAndPreflight(Fixture()),"Known in-zone961,650 accepts necessary endpoint gate");
                Check(TryReadAuthoredEndpointZones(layout,grid,out var min,out var max,out _,out _)&&
                    min[0].Equals(new int2(961,643))&&max[0].Equals(new int2(979,657)),"Actual S032 decimal-width floor is preserved");
                Check(LoadAndPreflight(Fixture(961,643,1361,657)),"Inclusive minimum/maximum corners accept");
                Check(LoadAndPreflight(Fixture(979,657,1343,643)),"Opposite inclusive corners accept");
                foreach(var point in new[]{new int2(960,650),new int2(980,650),new int2(970,642),new int2(970,658)})
                    Check(!LoadAndPreflight(Fixture(point.x,point.y)),"One-cell exterior must reject without enlarging zone");
                Check(!LoadAndPreflight(Fixture(961,650,1362,650)),"Enemy exterior rejects, protecting reversed-faction exit");
                var data=Fixture();((JArray)data["pads"]).RemoveAt(1);Check(!LoadAndPreflight(data),"Missing actual faction pad rejects");
                data=Fixture();((JArray)data["anchors"]).Add(new JObject{["AnchorId"]="other",["WorldX"]=962.5f,["WorldZ"]=650.5f});
                data["routes"][1]["WaypointAnchorIds"]=new JArray("other","m","e");Check(!LoadAndPreflight(data),"Different in-zone route endpoints still reject");
                data=Fixture();data["routes"][0]["WaypointAnchorIds"]=new JArray("missing","m","e");
                Check(!LoadAndPreflight(data),"Missing endpoint anchor rejects");
                data=Fixture();data["pads"][0]["WidthMetres"]=1.99f;Check(!LoadAndPreflight(data),"Zero floored half-span rejects");
                data=Fixture(986,650);data["pads"][0]["WidthMetres"]=100;data["pads"][0]["DepthMetres"]=100;
                Check(LoadAndPreflight(data)&&TryReadAuthoredEndpointZones(layout,grid,out min,out max,out _,out _)&&
                    min[0].Equals(new int2(954,634))&&max[0].Equals(new int2(986,666)),"Existing16-cell cap remains exact");
                data=Fixture(987,650);data["pads"][0]["WidthMetres"]=100;data["pads"][0]["DepthMetres"]=100;
                Check(!LoadAndPreflight(data),"One beyond existing16-cell cap rejects");
                LoadAndPreflight(Fixture());var shifted=grid;shifted.CellSize=2;shifted.Origin=new float3(740,0,260);
                Check(TryReadAuthoredEndpointZones(layout,shifted,out min,out max,out _,out _)&&
                    min[0].Equals(new int2(111,192))&&max[0].Equals(new int2(119,198)),"Actual grid origin/cell size is used");
                LoadAndPreflight(Fixture(960,650));
                var refused=Ground30RouteQueries(null,layout,default(Unity.Entities.EntityManager),Unity.Entities.Entity.Null,grid,default(MapSurfaceComponent),new JArray());
                Check((string)refused["status"]=="NotQueried_AuthoredEndpointPolicyRefused"&&(int)refused["nativeQueriesExecuted"]==0&&
                    ((JArray)refused["queries"]).Count==0&&!JointRoutesExact(refused),"Actual collector rejects before ECS/grid allocation or path query");
                RunPilotRouteIndependenceControls();
                UnityEngine.Debug.Log("[AuthoredRouteEndpointPreflightControls] result=Passed controls="+controls+
                    " existingIndependenceControls=3 nativeQueries=0 sourceFitCertified=0 missionReady=0");
            }
            finally{UnityEngine.Object.DestroyImmediate(layout);}
        }

        static void RecordPilotRouteIndependence(JObject report, SkirmishMapLayoutConfig layout, GridConfig grid)
        {
            var frame = layout.WorldBinding;
            Require(math.abs(frame.ForwardX - 1) < .0001f && math.abs(frame.ForwardZ) < .0001f &&
                math.abs(frame.AcrossX) < .0001f && math.abs(frame.AcrossZ - 1) < .0001f,
                "This bounded endpoint-zone adapter requires the actual axis-aligned AP/CC authored frame");
            Require(TryReadAuthoredEndpointZones(layout,grid,out var zoneMin,out var zoneMax,out var zoneRows,out string zoneError),zoneError);
            var groups = new List<JObject[]>();
            var source = (string)report["group"];
            var retained = ((JArray)report["endpointCandidateRouteQueries"]).OfType<JObject>()
                .Where(r => (string)r["routeId"] == (source == "AP" ? "route.skirmish.db.main" : "route.skirmish.db.flank_a")).ToArray();
            groups.Add(retained);
            foreach (JObject proposal in (JArray)report["failedRouteDetourProposals"])
                groups.Add(((JArray)proposal["nativeLegQueries"]).OfType<JObject>().ToArray());
            var rows = new JArray(); report["nativeThreeApproachIndependence"] = rows;
            foreach (var role in new[] { SkirmishRoleKind.Tank, SkirmishRoleKind.Siege, SkirmishRoleKind.ApcHeavy })
            {
                var paths = new List<List<int2>>(); int clearance = 0; bool complete = groups.Count == 3;
                var failures = new JArray();
                for (int route = 0; route < groups.Count; route++)
                {
                    var legs = groups[route].Where(r => (string)r["role"] == role.ToString()).OrderBy(r => (int)r["leg"]).ToArray();
                    if (!TryAssembleExactNativeLegs(legs, out var path, out int radius, out string error))
                    { complete = false; failures.Add(new JObject { ["routeIndex"] = route, ["error"] = error }); }
                    paths.Add(path); clearance = Math.Max(clearance, radius);
                }
                var result = new JObject { ["role"] = role.ToString(), ["completeExactNativePaths"] = complete,
                    ["retainedRouteIndex"] = 0, ["retainedRouteId"] = source == "AP" ? "route.skirmish.db.main" : "route.skirmish.db.flank_a",
                    ["detourRouteIndices"] = new JArray(1, 2), ["assemblyFailures"] = failures,
                    ["scope"] = "StaticNativeCenterlineClearanceProbe_ExcludesLiveAndSceneryOccupancy",
                    ["sourceFitCertified"] = false };
                rows.Add(result);
                if (!complete) { result["independentUnderDeclaredProbePolicy"] = false; continue; }
                result["actualAuthoredEndpointZones"] = zoneRows.DeepClone();
                var comparison = ComparePilotApproaches(paths, clearance, zoneMin[0], zoneMax[0], zoneMin[1], zoneMax[1]);
                foreach (var property in comparison.Properties()) result[property.Name] = property.Value.DeepClone();
            }
        }

        static bool TryAssembleExactNativeLegs(JObject[] legs, out List<int2> points, out int radius, out string error)
        {
            points = new List<int2>(); radius = 0; error = null;
            if (legs.Length == 0) { error = "No native legs"; return false; }
            for (int i = 0; i < legs.Length; i++)
            {
                var row = legs[i];
                if ((int?)row["leg"] != i + 1 || (string)row["status"] != "NativeQueryFoundExactGoal" ||
                    (int?)row["failureCode"] != 0 || !(row["pathCells"] is JArray cells) || cells.Count < 2 ||
                    !(row["requestedStartCell"] is JArray from) || !(row["requestedGoalCell"] is JArray to) ||
                    !((int?)row["footprintX"] >= 1) || !((int?)row["footprintZ"] >= 1))
                { error = "Missing, failed or noncontiguous exact native leg"; return false; }
                var a = new int2((int)from[0], (int)from[1]); var b = new int2((int)to[0], (int)to[1]);
                var first = new int2((int)cells[0][0], (int)cells[0][1]);
                var last = new int2((int)cells[cells.Count - 1][0], (int)cells[cells.Count - 1][1]);
                if (!first.Equals(a) || !last.Equals(b) || (i > 0 && !points[points.Count - 1].Equals(a)))
                { error = "Native endpoints or cross-leg join differ"; return false; }
                radius = Math.Max(radius, (Math.Max((int)row["footprintX"], (int)row["footprintZ"]) + 1) / 2);
                for (int n = i == 0 ? 0 : 1; n < cells.Count; n++) points.Add(new int2((int)cells[n][0], (int)cells[n][1]));
            }
            return true;
        }

        static JObject ComparePilotApproaches(List<List<int2>> paths, int clearance,
            int2 entryMin, int2 entryMax, int2 exitMin, int2 exitMax)
        {
            Require(paths.Count == 3 && paths.All(p => p.Count >= 2) && clearance >= 0 && clearance <= 16,
                "Three bounded native path centerlines required");
            var start = paths[0][0]; var goal = paths[0][paths[0].Count - 1];
            Require(paths.All(p => p[0].Equals(start) && p[p.Count - 1].Equals(goal)), "Approaches must share exact native entry and exit");
            if (!EndpointCellInZone(start, entryMin, entryMax) || !EndpointCellInZone(goal, exitMin, exitMax))
                return new JObject { ["independentUnderDeclaredProbePolicy"] = false, ["rejection"] = "Native path endpoint is outside actual authored compound entry/exit" };
            var centers = new List<HashSet<int2>>(); var corridors = new List<HashSet<int2>>();
            foreach (var path in paths)
            {
                var center = RasterizePilotCenterline(path);
                center.RemoveWhere(p => (EndpointCellInZone(p, entryMin, entryMax) && math.lengthsq(p - start) <= SharedEndpointRadiusCells * SharedEndpointRadiusCells) ||
                    (EndpointCellInZone(p, exitMin, exitMax) && math.lengthsq(p - goal) <= SharedEndpointRadiusCells * SharedEndpointRadiusCells));
                centers.Add(center); var corridor = new HashSet<int2>();
                foreach (var cell in center) for (int x = -clearance; x <= clearance; x++) for (int z = -clearance; z <= clearance; z++)
                    corridor.Add(cell + new int2(x, z));
                corridors.Add(corridor);
            }
            var pairs = new JArray(); var uniqueRows = new JArray(); bool independent = true;
            for (int i = 0; i < 3; i++)
            {
                var unique = new HashSet<int2>(centers[i]);
                for (int j = 0; j < 3; j++) if (i != j) unique.ExceptWith(corridors[j]);
                uniqueRows.Add(new JObject { ["routeIndex"] = i, ["uniqueInteriorCenterlineCells"] = unique.Count });
                independent &= unique.Count >= MinimumUniqueInteriorCells;
                for (int j = i + 1; j < 3; j++)
                {
                    var overlap = new HashSet<int2>(corridors[i]); overlap.IntersectWith(corridors[j]);
                    pairs.Add(new JObject { ["firstRouteIndex"] = i, ["secondRouteIndex"] = j, ["interiorClearanceOverlapCells"] = overlap.Count,
                        ["overlapWitnessCells"] = new JArray(overlap.OrderBy(p => p.x).ThenBy(p => p.y).Take(64).Select(p => new JArray(p.x, p.y))) });
                    independent &= overlap.Count == 0;
                }
            }
            return new JObject { ["probePolicy"] = "ThreeStaticInteriorCorridors.v1", ["sharedEndpointRadiusCells"] = SharedEndpointRadiusCells,
                ["conservativeChebyshevClearanceCells"] = clearance, ["minimumUniqueInteriorCells"] = MinimumUniqueInteriorCells,
                ["uniqueInteriorRows"] = uniqueRows, ["mergeAndOverlapPairs"] = pairs,
                ["independentUnderDeclaredProbePolicy"] = independent };
        }

        static HashSet<int2> RasterizePilotCenterline(List<int2> points)
        {
            var cells = new HashSet<int2>();
            for (int i = 1; i < points.Count; i++)
            {
                int x = points[i - 1].x, z = points[i - 1].y, endX = points[i].x, endZ = points[i].y;
                int dx = Math.Abs(endX - x), dz = -Math.Abs(endZ - z), sx = x < endX ? 1 : -1, sz = z < endZ ? 1 : -1, error = dx + dz;
                Require(dx - dz <= 10000, "Native compressed path segment exceeds bounded source probe");
                while (true)
                {
                    cells.Add(new int2(x, z)); if (x == endX && z == endZ) break;
                    int twice = error * 2; if (twice >= dz) { error += dz; x += sx; } if (twice <= dx) { error += dx; z += sz; }
                }
            }
            return cells;
        }

        static void RunPilotRouteIndependenceControls()
        {
            var main = new List<int2> { new int2(0, 0), new int2(200, 0) };
            var north = new List<int2> { new int2(0, 0), new int2(20, 20), new int2(180, 20), new int2(200, 0) };
            var south = new List<int2> { new int2(0, 0), new int2(20, -20), new int2(180, -20), new int2(200, 0) };
            var entryMin = new int2(-16, -16); var entryMax = new int2(16, 16);
            var exitMin = new int2(184, -16); var exitMax = new int2(216, 16);
            Require((bool)ComparePilotApproaches(new List<List<int2>> { main, north, south }, 2, entryMin, entryMax, exitMin, exitMax)["independentUnderDeclaredProbePolicy"],
                "Separated synthetic native-path policy control must pass");
            Require(!(bool)ComparePilotApproaches(new List<List<int2>> { main, main, south }, 2, entryMin, entryMax, exitMin, exitMax)["independentUnderDeclaredProbePolicy"],
                "Renamed identical route must reject");
            var merged = new List<int2> { new int2(0, 0), new int2(20, 20), new int2(100, 20), new int2(120, 0), new int2(200, 0) };
            Require(!(bool)ComparePilotApproaches(new List<List<int2>> { main, merged, south }, 2, entryMin, entryMax, exitMin, exitMax)["independentUnderDeclaredProbePolicy"],
                "Distinct route that merges into an interior chokepoint must reject");
        }
    }
}
#endif
