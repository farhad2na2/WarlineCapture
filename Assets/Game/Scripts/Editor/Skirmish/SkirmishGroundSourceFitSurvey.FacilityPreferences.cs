#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {



        const string ReviewedRouteDeltaEnv="WARLINE_ALL120_ROUTE_DELTA_INPUT";
        const string ReviewedRouteDeltaShaEnv="WARLINE_ALL120_ROUTE_DELTA_INPUT_SHA256";
        static string RouteDeltaSha(byte[] bytes)
        {
            using var sha=System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
        }
        static void RouteDeltaFields(JObject row,params string[] fields)
            =>Require(row!=null&&row.Properties().Select(p=>p.Name).OrderBy(v=>v,StringComparer.Ordinal)
                .SequenceEqual(fields.OrderBy(v=>v,StringComparer.Ordinal)),"Exact reviewed route object fields required");
        static float RouteDeltaNumber(JToken value)
        {
            Require(value!=null&&(value.Type==JTokenType.Integer||value.Type==JTokenType.Float),"Numeric route coordinate required");
            double number=(double)value;float result=(float)number;
            Require(!double.IsNaN(number)&&!double.IsInfinity(number)&&float.IsFinite(result)&&Math.Abs(number-result)<.00001,
                "Finite representable route coordinate required");return result;
        }
        static JObject ParseReviewedRouteDelta(byte[] bytes,string digest)
        {
            Require(bytes!=null&&bytes.Length>0&&bytes.Length<=1048576&&digest!=null&&digest.Length==64&&
                digest.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'))&&RouteDeltaSha(bytes)==digest,
                "Exact bounded reviewed route bytes required");
            Require(!(bytes.Length>=3&&bytes[0]==239&&bytes[1]==187&&bytes[2]==191),"Route input is strict UTF8 without BOM");
            string text=new System.Text.UTF8Encoding(false,true).GetString(bytes);
            return JObject.Parse(text,new Newtonsoft.Json.Linq.JsonLoadSettings{
                DuplicatePropertyNameHandling=Newtonsoft.Json.Linq.DuplicatePropertyNameHandling.Error});
        }
        static JObject ReadReviewedRouteDelta(JObject mission)
        {
            string path=Environment.GetEnvironmentVariable(ReviewedRouteDeltaEnv),digest=Environment.GetEnvironmentVariable(ReviewedRouteDeltaShaEnv);
            if(string.IsNullOrEmpty(path)&&string.IsNullOrEmpty(digest))return null;
            Require(!string.IsNullOrEmpty(path)&&!string.IsNullOrEmpty(digest)&&!apGroundJointPreferencesRequested&&
                !mpGroundBaPilotRequested&&!s049BoundedCompoundRouteRequested&&!ProtectedRefinementCatalog((string)mission["catalog_id"]),
                "Exclusive reviewed generic BA route input required; coded/MP/AP paths unchanged");
            RequireFiniteOriginRequestPath(path);
            Require(new System.IO.FileInfo(path).Length<=1048576&&Hash(path)==digest,"Bounded exact route input path required");
            byte[] bytes=System.IO.File.ReadAllBytes(path);var input=ParseReviewedRouteDelta(bytes,digest);
            RequireFiniteOriginRequestPath(path);Require(Hash(path)==digest,"Route input changed while reading");
            return input;
        }

        static bool RouteDeltaFootprintInsideFrame(SkirmishMapLayoutConfig layout,GridConfig grid,int2 cell)
        {
            if(!layout.TryProjectLocalToMap(layout.OriginX,layout.OriginZ,out float ox,out float oz)||
                !layout.TryProjectLocalToMap(layout.OriginX+1,layout.OriginZ,out float ax,out float az)||
                !layout.TryProjectLocalToMap(layout.OriginX,layout.OriginZ+1,out float bx,out float bz))return false;
            float fx=ax-ox,fz=az-oz,cx=bx-ox,cz=bz-oz,det=fx*cz-fz*cx;
            if(!float.IsFinite(det)||Math.Abs(det)<.000001f)return false;
            foreach(int dx in new[]{-1,2})foreach(int dz in new[]{-1,2})
            {
                float x=grid.Origin.x+(cell.x+dx)*grid.CellSize-ox,z=grid.Origin.z+(cell.y+dz)*grid.CellSize-oz;
                float u=(x*cz-z*cx)/det,v=(fx*z-fz*x)/det;
                if(!float.IsFinite(u)||!float.IsFinite(v)||u<0||v<0||u>layout.WorldWidthMetres||v>layout.WorldDepthMetres)return false;
            }
            return true;
        }
        static string EncodeReviewedRouteDelta(string original,JObject mission,JObject input,GridConfig grid,out JObject evidence)
        {
            evidence=null;
            RouteDeltaFields(input,"schema","scope","catalogId","definitionId","sourcePins","canonical_size_rows",
                "layoutBeforeSha256","routes","sourceFitCertified","missionAccepted");
            Require(input["schema"]?.Type==JTokenType.Integer&&(int)input["schema"]==1&&
                input["scope"]?.Type==JTokenType.String&&(string)input["scope"]=="ReviewedBoundedRouteDelta_NotPathOrSourceFit"&&
                input["catalogId"]?.Type==JTokenType.String&&(string)input["catalogId"]==(string)mission["catalog_id"]&&
                input["definitionId"]?.Type==JTokenType.String&&(string)input["definitionId"]==(string)mission["definition_id"]&&
                !ProtectedRefinementCatalog((string)mission["catalog_id"])&&
                (string)mission["tuple"]?["objective_id"]=="BA"&&(string)mission["tuple"]?["army_profile"]=="G"&&
                JToken.DeepEquals(input["sourcePins"],mission["source_candidate"])&&
                JToken.DeepEquals(input["canonical_size_rows"],mission["canonical_size_rows"])&&
                input["sourceFitCertified"]?.Type==JTokenType.Boolean&&!(bool)input["sourceFitCertified"]&&
                input["missionAccepted"]?.Type==JTokenType.Boolean&&!(bool)input["missionAccepted"]&&
                input["layoutBeforeSha256"]?.Type==JTokenType.String&&
                (string)input["layoutBeforeSha256"]==RouteDeltaSha(new System.Text.UTF8Encoding(false).GetBytes(original)),
                "Exact unqualified generic ground BA subject/source/three-size layout ownership required");
            var sizes=mission["canonical_size_rows"] as JArray;
            Require(sizes!=null&&sizes.Count==3&&sizes.OfType<JObject>().Select(r=>(string)r["size_id"])
                .SequenceEqual(new[]{"Standard","War","LargeWar"}),"Exactly three canonical sizes required");
            var data=JObject.Parse(original);var routes=data["routes"] as JArray;var anchors=data["anchors"] as JArray;
            var changes=input["routes"] as JArray;
            Require(routes!=null&&routes.Count==3&&anchors!=null&&changes!=null&&changes.Count==3,
                "Exactly three existing typed approaches required");
            Require(grid.Width>0&&grid.Height>0&&float.IsFinite(grid.CellSize)&&grid.CellSize>0,"Actual source grid required");
            var originalAnchors=anchors.OfType<JObject>().ToDictionary(a=>(string)a["AnchorId"],a=>(JObject)a.DeepClone(),StringComparer.Ordinal);
            var owned=new HashSet<string>(StringComparer.Ordinal);var usedNew=new HashSet<string>(StringComparer.Ordinal);
            int totalPoints=0,added=0,moved=0;var signatures=new HashSet<string>(StringComparer.Ordinal);
            var clone=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            try
            {
                JsonUtility.FromJsonOverwrite(original,clone);
                for(int r=0;r<3;r++)
                {
                    var route=(JObject)routes[r];var change=changes[r] as JObject;RouteDeltaFields(change,"routeId","points");
                    Require((string)route["RouteId"]=="route.pilot."+r&&change["routeId"]?.Type==JTokenType.String&&
                        (string)change["routeId"]==(string)route["RouteId"]&&route["Kind"]?.Type==JTokenType.Integer&&
                        (int)route["Kind"]==r+1&&route["ProvisionalTimes"]?.Type==JTokenType.Boolean&&(bool)route["ProvisionalTimes"]&&
                        route["AllowsVehicles"]?.Type==JTokenType.Boolean&&(bool)route["AllowsVehicles"]&&
                        route["AllowsAircraft"]?.Type==JTokenType.Boolean&&!(bool)route["AllowsAircraft"],
                        "Original generic three route roles/ground permissions/provisional timing required");
                    var oldIds=((JArray)route["WaypointAnchorIds"]).Values<string>().ToArray();
                    Require(oldIds.Length>=2&&oldIds.All(id=>originalAnchors.ContainsKey(id)&&owned.Add(id))&&
                        !((JArray)data["pads"]).OfType<JObject>().Any(p=>oldIds.Contains((string)p["AnchorId"])),
                        "Exclusive original route anchors; no pad/return ownership changes");
                    var points=change["points"] as JArray;
                    Require(points!=null&&points.Count>=2&&points.Count<=14,"Each reviewed route has2..14 bounded points");
                    totalPoints+=points.Count;
                    var ids=new JArray();var seen=new HashSet<string>(StringComparer.Ordinal);var cells=new List<int2>();float length=0;
                    var a0=originalAnchors[oldIds[0]];var b0=originalAnchors[oldIds[oldIds.Length-1]];
                    var start=new Vector2((float)a0["WorldX"],(float)a0["WorldZ"]);
                    var end=new Vector2((float)b0["WorldX"],(float)b0["WorldZ"]);
                    float lastProgress=-1;
                    for(int p=0;p<points.Count;p++)
                    {
                        var point=points[p] as JObject;RouteDeltaFields(point,"anchorId","roleId","x","z");
                        Require(point["anchorId"]?.Type==JTokenType.String&&point["roleId"]?.Type==JTokenType.String,"Typed route point required");
                        string id=(string)point["anchorId"],role=(string)point["roleId"];
                        Require(seen.Add(id),"No repeated route anchor");
                        Require((p!=0||id==oldIds[0])&&(p!=points.Count-1||id==oldIds[oldIds.Length-1]),"Original entry/exit anchor identities retained");
                        JObject anchor;
                        if(originalAnchors.TryGetValue(id,out var existing))
                        {
                            Require(oldIds.Contains(id)&&role==(string)existing["RoleId"],"Only this route's typed anchors may move");
                            anchor=anchors.OfType<JObject>().Single(v=>(string)v["AnchorId"]==id);
                        }
                        else
                        {
                            Require(id=="anchor.reviewed."+r+"."+p&&role=="reviewed.route."+r+"."+p&&usedNew.Add(id),
                                "Canonical fresh route-only steering identity required");
                            anchor=new JObject{["AnchorId"]=id,["RoleId"]=role};anchors.Add(anchor);added++;
                        }
                        float x=RouteDeltaNumber(point["x"]),z=RouteDeltaNumber(point["z"]);
                        if(originalAnchors.TryGetValue(id,out var originalPoint)&&
                            (x!=(float)originalPoint["WorldX"]||z!=(float)originalPoint["WorldZ"]))moved++;
                        Require(x>=clone.OriginX&&z>=clone.OriginZ&&x<=clone.OriginX+clone.WorldWidthMetres&&
                            z<=clone.OriginZ+clone.WorldDepthMetres,"Finite route point inside unchanged authored frame");
                        Require(clone.TryProjectLocalToMap(x,z,out float wx,out float wz)&&float.IsFinite(wx)&&float.IsFinite(wz),
                            "Actual finite source projection required");
                        var cell=GridUtils.WorldToCell(grid,new float3(wx,0,wz));
                        Require(cell.x>=1&&cell.y>=1&&cell.x+1<grid.Width&&cell.y+1<grid.Height,
                            "Complete canonical3x3 requested footprint inside source grid");
                        Require(RouteDeltaFootprintInsideFrame(clone,grid,cell),"Canonical footprint stays inside unchanged logical frame");
                        if(p>0&&p<points.Count-1&&r!=0)
                        {
                            var axis=end-start;float side=axis.x*(z-start.y)-axis.y*(x-start.x);
                            Require(r==1?side>0:side<0,"Original north/south flank side required for every interior point");
                        }
                        var v=new Vector2(x,z);float progress=Vector2.Dot(v-start,end-start)/(end-start).sqrMagnitude;
                        Require(float.IsFinite(progress)&&progress>=-.0001f&&progress<=1.0001f&&progress>lastProgress,
                            "Ordered forward approach without loops/backtracking");lastProgress=progress;
                        bool semantic=false;
                        for(int leg=1;leg<oldIds.Length;leg++)
                        {
                            var aa=originalAnchors[oldIds[leg-1]];var bb=originalAnchors[oldIds[leg]];
                            var a=new Vector2((float)aa["WorldX"],(float)aa["WorldZ"]);var b=new Vector2((float)bb["WorldX"],(float)bb["WorldZ"]);
                            float t=Mathf.Clamp01(Vector2.Dot(v-a,b-a)/(b-a).sqrMagnitude);
                            semantic|=RouteCandidateWithinSemantics(v,a,b,start,end,t);
                        }
                        Require(semantic,"Reviewed points retain existing32m bounded leg corridor and flank side");
                        anchor["WorldX"]=x;anchor["WorldZ"]=z;anchor["NormalizedU"]=(x-clone.OriginX)/clone.WorldWidthMetres;
                        anchor["NormalizedV"]=(z-clone.OriginZ)/clone.WorldDepthMetres;
                        if(cells.Count>0){Require(!cells[cells.Count-1].Equals(cell),"Distinct successive native cells");length+=Vector2.Distance(
                            new Vector2((float)points[p-1]["x"],(float)points[p-1]["z"]),v);}
                        cells.Add(cell);ids.Add(id);
                    }
                    Require(signatures.Add(string.Join(";",cells.Select(c=>c.x+","+c.y))),"Three distinct authored approach sequences required");
                    route["WaypointAnchorIds"]=ids;RescaleBoundedRouteTimes(route,(float)route["LengthMetres"],length);
                    route["LengthMetres"]=length;route["ProvisionalTimes"]=true;
                }
                Require(moved<=4,"At most4 changed original macro anchors required");
                Require(totalPoints<=42&&added<=30,"Unchanged maximum30 added anchors and42 total route points");
                string next=data.ToString(Newtonsoft.Json.Formatting.None);JsonUtility.FromJsonOverwrite(next,clone);
                Require(TryPreflightAuthoredRouteEndpoints(clone,grid,out var policy),"Existing exact shared endpoint/zone policy required");
                evidence=new JObject{["scope"]="ReviewedRouteDeltaNecessaryPreflight_NotPathIndependence",["input"]=input.DeepClone(),
                    ["beforeLayoutSha256"]=input["layoutBeforeSha256"].DeepClone(),["appliedLayoutSha256"]=RouteDeltaSha(new System.Text.UTF8Encoding(false).GetBytes(JsonUtility.ToJson(clone))),["pointCount"]=totalPoints,["addedAnchors"]=added,["movedOriginalAnchors"]=moved,
                    ["endpointPolicy"]=policy,["nativeQueries"]=0,["sourceFitCertified"]=false,["missionAccepted"]=false};
                return JsonUtility.ToJson(clone);
            }
            finally{UnityEngine.Object.DestroyImmediate(clone);}
        }

        static void ValidateReviewedRouteReplayProof(string layout,JObject mission,JObject proof)
        {
            Require(proof!=null&&proof["input"] is JObject&&
                (string)proof["appliedLayoutSha256"]==RouteDeltaSha(new System.Text.UTF8Encoding(false).GetBytes(layout))&&
                (string)proof["input"]["catalogId"]==(string)mission["catalog_id"]&&
                (string)proof["input"]["definitionId"]==(string)mission["definition_id"]&&
                JToken.DeepEquals(proof["input"]["sourcePins"],mission["source_candidate"])&&
                JToken.DeepEquals(proof["input"]["canonical_size_rows"],mission["canonical_size_rows"])&&
                proof["nativeQueries"]?.Type==JTokenType.Integer&&(int)proof["nativeQueries"]==0&&
                proof["sourceFitCertified"]?.Type==JTokenType.Boolean&&!(bool)proof["sourceFitCertified"]&&
                proof["missionAccepted"]?.Type==JTokenType.Boolean&&!(bool)proof["missionAccepted"],
                "Exact prepared route clone/source/three canonical budgets; necessary preflight only");
        }
        static void AppendReviewedRoutePointClearance(JObject evidence,List<RectInt> clearance)
        {
            var kept=new HashSet<RectInt>(clearance);var added=new HashSet<RectInt>();
            foreach(JObject size in (JArray)evidence["queriesBySize"])
            foreach(JObject query in (JArray)size["queries"])
            {
                int x=(int)query["footprintX"],z=(int)query["footprintZ"];
                Require(x>0&&z>0&&x<=3&&z<=3,"Actual canonical ground footprint within preflight3x3 envelope");
                foreach(string field in new[]{"fromCell","toCell"})
                {
                    var c=(JArray)query[field];var min=UnitFootprintUtility.GetMinCell(new int2((int)c[0],(int)c[1]),new int2(x,z));
                    var rect=new RectInt(min.x,min.y,x,z);if(kept.Add(rect)){clearance.Add(rect);added.Add(rect);}
                }
            }
            evidence["reviewedRequestedPointFootprints"]=new JArray(added.Select(RectJson));
            evidence["requestedPointClearanceScope"]="All actual canonical requested waypoint/endpoint footprints, including staging zones; not path independence";
            evidence["footprintReservations"]=new JArray(clearance.Select(RectJson));
        }

        public static void RunReviewedRouteDeltaControls()
        {
            Require(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode route input controls required");
            int count=0;void Check(bool ok,string message){Require(ok,message);count++;}
            var grid=new GridConfig{Width=256,Height=256,CellSize=1,Origin=float3.zero};
            var fixture=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            var mission=new JObject{["catalog_id"]="S074",["definition_id"]="fixture.definition",
                ["tuple"]=new JObject{["objective_id"]="BA",["army_profile"]="G"},
                ["source_candidate"]=new JObject{["map"]="fixture.exact.source"},
                ["canonical_size_rows"]=new JArray(new[]{"Standard","War","LargeWar"}.Select(s=>
                    (JToken)new JObject{["size_id"]=s,["budget"]="unchanged.fixture"}))};
            var anchors=new JArray();var routes=new JArray();
            for(int r=0;r<3;r++)
            {
                var ids=new JArray();float length=0;Vector2 previous=default;
                for(int p=0;p<4;p++)
                {
                    float x=new[]{20.5f,50.5f,190.5f,220.5f}[p],z=128.5f+(p==0||p==3?0:r==1?24:r==2?-24:0);
                    string id="fixture.route."+r+"."+p;ids.Add(id);
                    anchors.Add(new JObject{["AnchorId"]=id,["RoleId"]="approach."+r+"."+p,["WorldX"]=x,["WorldZ"]=z,
                        ["NormalizedU"]=x/256,["NormalizedV"]=z/256});
                    if(p>0)length+=Vector2.Distance(previous,new Vector2(x,z));previous=new Vector2(x,z);
                }
                routes.Add(new JObject{["RouteId"]="route.pilot."+r,["Kind"]=r+1,["WaypointAnchorIds"]=ids,["WidthMetres"]=8,
                    ["LengthMetres"]=length,["AllowsVehicles"]=true,["AllowsAircraft"]=false,["ProvisionalTimes"]=true,
                    ["InfantryTransitSeconds"]=length/4,["GroundTransitSeconds"]=length/8,["AirTransitSeconds"]=0f,
                    ["InfantryFirstContactSeconds"]=length/8,["GroundFirstContactSeconds"]=length/16});
            }
            anchors.Add(new JObject{["AnchorId"]="return.original",["RoleId"]="air.player",["WorldX"]=12f,["WorldZ"]=24f});
            var data=new JObject{["layoutId"]="fixture.reviewed.routes",["operationMapId"]="fixture.map",
                ["originX"]=0f,["originZ"]=0f,["worldWidthMetres"]=256f,["worldDepthMetres"]=256f,["cellSize"]=1f,
                ["worldBinding"]=new JObject{["OriginX"]=0f,["OriginZ"]=128f,["ForwardX"]=1f,["ForwardZ"]=0f,
                    ["AcrossX"]=0f,["AcrossZ"]=1f,["ForwardMetres"]=256f,["AcrossMetres"]=256f,
                    ["SourceHash"]="fixture.source",["PlayerAnchorId"]="fixture.route.0.0",["EnemyAnchorId"]="fixture.route.0.3"},
                ["anchors"]=anchors,["routes"]=routes,["pads"]=new JArray(new byte[]{1,2}.Select(f=>(JToken)new JObject{
                    ["PadId"]="stage."+f,["AnchorId"]="stage."+f,["RoleId"]="staging."+f,
                    ["Kind"]=(int)Game.Configs.SkirmishLegalPadKind.GroundStaging,["FactionId"]=f,
                    ["CenterX"]=f==1?20f:220f,["CenterZ"]=128f,["WidthMetres"]=16f,["DepthMetres"]=16f}))};
            JsonUtility.FromJsonOverwrite(data.ToString(),fixture);string before=JsonUtility.ToJson(fixture);
            var exact=JObject.Parse(before);var changes=new JArray();
            for(int r=0;r<3;r++)
            {
                var points=new JArray();
                for(int p=0;p<4;p++)
                {
                    var a=((JArray)exact["anchors"]).OfType<JObject>().Single(v=>(string)v["AnchorId"]=="fixture.route."+r+"."+p);
                    if(p==2)points.Add(new JObject{["anchorId"]="anchor.reviewed."+r+".2",["roleId"]="reviewed.route."+r+".2",
                        ["x"]=120.5f,["z"]=r==1?152.5f:r==2?104.5f:128.5f});
                    points.Add(new JObject{["anchorId"]=a["AnchorId"].DeepClone(),["roleId"]=a["RoleId"].DeepClone(),
                        ["x"]=a["WorldX"].DeepClone(),["z"]=a["WorldZ"].DeepClone()});
                }
                changes.Add(new JObject{["routeId"]="route.pilot."+r,["points"]=points});
            }
            var input=new JObject{["schema"]=1,["scope"]="ReviewedBoundedRouteDelta_NotPathOrSourceFit",
                ["catalogId"]=mission["catalog_id"].DeepClone(),["definitionId"]=mission["definition_id"].DeepClone(),
                ["sourcePins"]=mission["source_candidate"].DeepClone(),["canonical_size_rows"]=mission["canonical_size_rows"].DeepClone(),
                ["layoutBeforeSha256"]=RouteDeltaSha(new System.Text.UTF8Encoding(false).GetBytes(before)),
                ["routes"]=changes,["sourceFitCertified"]=false,["missionAccepted"]=false};
            byte[] Bytes(JObject row)=>new System.Text.UTF8Encoding(false).GetBytes(row.ToString(Newtonsoft.Json.Formatting.None));
            bool Refuses(Action action){try{action();return false;}catch(Exception e)when(e is InvalidOperationException||
                e is Newtonsoft.Json.JsonException||e is System.Text.DecoderFallbackException||e is System.IO.IOException){return true;}}
            string pathBefore=Environment.GetEnvironmentVariable(ReviewedRouteDeltaEnv),shaBefore=Environment.GetEnvironmentVariable(ReviewedRouteDeltaShaEnv);
            string folder=System.IO.Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction","RouteDeltaControls-"+Guid.NewGuid().ToString("N"));
            string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(folder,"input.json"));bool created=false;
            try
            {
                var bytes=Bytes(input);var parsed=ParseReviewedRouteDelta(bytes,RouteDeltaSha(bytes));
                Check(JToken.DeepEquals(input,parsed),"Genuine strict UTF8 parser positive");
                string next=EncodeReviewedRouteDelta(before,mission,parsed,grid,out var proof);
                var after=JObject.Parse(next);
                Check((int)proof["pointCount"]==15&&(int)proof["addedAnchors"]==3&&!(bool)proof["sourceFitCertified"],
                    "Actual encoder splits bounded legs without source-fit credit");
                Check(JToken.DeepEquals(exact["pads"],after["pads"])&&JToken.DeepEquals(exact["worldBinding"],after["worldBinding"])&&
                    JToken.DeepEquals(((JArray)exact["anchors"]).Single(a=>(string)a["AnchorId"]=="return.original"),
                        ((JArray)after["anchors"]).Single(a=>(string)a["AnchorId"]=="return.original"))&&before==JsonUtility.ToJson(fixture),
                    "Clone-only encoder preserves pads/source/old return/nonroute anchors and original object");
                Check(((JArray)after["routes"]).All(r=>(float)r["AirTransitSeconds"]==0&&
                    Math.Abs((float)r["LengthMetres"]/(float)r["InfantryTransitSeconds"]-4)<.0001),
                    "Inactive air zero and active movement speeds preserved");
                void Reject(Action<JObject> mutate)
                {
                    var bad=(JObject)input.DeepClone();mutate(bad);
                    Check(Refuses(()=>EncodeReviewedRouteDelta(before,mission,bad,grid,out _))&&before==JsonUtility.ToJson(fixture),
                        "Malformed route request refuses with no layout mutation");
                }
                Reject(v=>v["schema"]="1");Reject(v=>v["schema"]=1.5);Reject(v=>v["catalogId"]="S050");
                Reject(v=>v["definitionId"]="other");Reject(v=>v["sourcePins"]=new JObject());
                Reject(v=>v["canonical_size_rows"][1]["budget"]="changed");Reject(v=>v["canonical_size_rows"]=new JArray());
                Reject(v=>v["layoutBeforeSha256"]=new string('0',64));Reject(v=>v["sourceFitCertified"]=true);
                Reject(v=>v["missionAccepted"]="false");Reject(v=>v["routes"]=new JArray());
                Reject(v=>v["routes"][0]["routeId"]="route.pilot.other");Reject(v=>v["routes"][0]["points"][0]["anchorId"]="return.original");
                Reject(v=>v["routes"][0]["points"][1]["roleId"]="foreign");Reject(v=>v["routes"][0]["points"][2]["anchorId"]="foreign");
                Reject(v=>v["routes"][0]["points"][1]["x"]="50.5");Reject(v=>v["routes"][0]["points"][1]["x"]=double.NaN);
                Reject(v=>v["routes"][0]["points"][1]["x"]=300f);Reject(v=>v["routes"][0]["points"][0]["z"]=190f);
                Reject(v=>v["routes"][1]["points"][1]["z"]=100f);Reject(v=>v["routes"][0]["points"][2]["x"]=40.5f);
                Reject(v=>v["routes"][0]["points"][1]["z"]=200f);Reject(v=>((JArray)v["routes"][0]["points"]).Add(v["routes"][0]["points"][1].DeepClone()));
                Reject(v=>v["unknown"]=0);
                Reject(v=>{v["routes"][1]["points"][1]["x"]=20.6f;v["routes"][1]["points"][1]["z"]=127.5f;});

                Reject(v=>v["routes"][0]["points"][1]["x"]=true);
                Reject(v=>v["canonical_size_rows"][1]["size_id"]="Standard");
                Reject(v=>{var a=(JArray)v["routes"][0]["points"];while(a.Count<15)a.Add(a[1].DeepClone());});
                Check(Refuses(()=>ParseReviewedRouteDelta(bytes,new string('0',64))),"Actual bytes bad SHA refuses");
                var duplicate=new System.Text.UTF8Encoding(false).GetBytes("{\"schema\":1,\"schema\":1}");
                Check(Refuses(()=>ParseReviewedRouteDelta(duplicate,RouteDeltaSha(duplicate))),"Duplicate JSON keys refuse");
                var bom=new byte[]{239,187,191}.Concat(bytes).ToArray();
                Check(Refuses(()=>ParseReviewedRouteDelta(bom,RouteDeltaSha(bom))),"Explicit BOM refusal");
                Environment.SetEnvironmentVariable(ReviewedRouteDeltaEnv,null);Environment.SetEnvironmentVariable(ReviewedRouteDeltaShaEnv,null);
                Check(ReadReviewedRouteDelta(mission)==null,"No override default remains null");
                Environment.SetEnvironmentVariable(ReviewedRouteDeltaEnv,path);
                Check(Refuses(()=>ReadReviewedRouteDelta(mission)),"Partial path-only override refuses");
                Environment.SetEnvironmentVariable(ReviewedRouteDeltaEnv,null);Environment.SetEnvironmentVariable(ReviewedRouteDeltaShaEnv,RouteDeltaSha(bytes));
                Check(Refuses(()=>ReadReviewedRouteDelta(mission)),"Partial SHA-only override refuses");
                System.IO.Directory.CreateDirectory(folder);created=true;System.IO.File.WriteAllBytes(path,bytes);
                Environment.SetEnvironmentVariable(ReviewedRouteDeltaEnv,path);
                Check(JToken.DeepEquals(ReadReviewedRouteDelta(mission),input),"Real owned Logs path+SHA reader positive");
                Environment.SetEnvironmentVariable(ReviewedRouteDeltaEnv,System.IO.Path.Combine(folder,"..",System.IO.Path.GetFileName(folder),"input.json"));
                Check(Refuses(()=>ReadReviewedRouteDelta(mission)),"Lexical ancestor traversal refuses");
                Environment.SetEnvironmentVariable(ReviewedRouteDeltaEnv,path);System.IO.File.AppendAllText(path," ");
                Check(Refuses(()=>ReadReviewedRouteDelta(mission)),"Changed input bytes refuse");
                var qa=new JObject{["queriesBySize"]=new JArray(new JObject{["queries"]=new JArray(new JObject{
                    ["fromCell"]=new JArray(20,128),["toCell"]=new JArray(50,128),["footprintX"]=3,["footprintZ"]=3})})};
                var clearance=new List<RectInt>();AppendReviewedRoutePointClearance(qa,clearance);
                Check(clearance.Contains(new RectInt(19,127,3,3))&&clearance.Contains(new RectInt(49,127,3,3)),
                    "Same actual helper retains staging endpoint and waypoint footprints, not independence credit");
                ValidateReviewedRouteReplayProof(next,mission,proof);Check(true,"Actual normal replay proof helper positive");
                var wrongProof=(JObject)proof.DeepClone();wrongProof["appliedLayoutSha256"]=new string('0',64);
                Check(Refuses(()=>ValidateReviewedRouteReplayProof(next,mission,wrongProof)),"Normal replay refuses unrelated prepared layout");
                wrongProof=(JObject)proof.DeepClone();wrongProof["input"]["sourcePins"]=new JObject();
                Check(Refuses(()=>ValidateReviewedRouteReplayProof(next,mission,wrongProof)),"Normal replay refuses changed source proof");
                var six=(JObject)input.DeepClone();
                foreach(JObject route in (JArray)six["routes"])
                {
                    var points=(JArray)route["points"];points[0]["z"]=(float)points[0]["z"]+1;
                    points[points.Count-1]["z"]=(float)points[points.Count-1]["z"]+1;
                }
                bool sixRefused=false;try{EncodeReviewedRouteDelta(before,mission,six,grid,out _);}
                catch(InvalidOperationException e){sixRefused=e.Message=="At most4 changed original macro anchors required";}
                Check(sixRefused&&before==JsonUtility.ToJson(fixture),"Actual encoder rejects six otherwise shared in-zone endpoint moves at original4 macro cap");
                Require(count==46,"Exactly46 reviewed route parser/encoder/reader/clearance controls");
            }
            finally
            {
                Environment.SetEnvironmentVariable(ReviewedRouteDeltaEnv,pathBefore);Environment.SetEnvironmentVariable(ReviewedRouteDeltaShaEnv,shaBefore);
                UnityEngine.Object.DestroyImmediate(fixture);
                if(created)
                {
                    RequireFiniteOriginRequestPath(path);System.IO.File.Delete(path);System.IO.Directory.Delete(folder,false);
                }
            }
            Debug.Log("[ReviewedBoundedRouteDeltaControls] result=Passed controls=46 nativeQueries=0 sourceFitCertified=0 missionAccepted=0");
        }


        // Runs existing pure subject checks followed by the genuine no-World serializer producer.
        // Context request controls remain separate until a real schema2 fixture is available.
        public static void RunNormalOriginSubjectAndRouteSerializerControls()
        {
            RunNormalOriginSubjectControls();
            RunReviewedRouteSerializerBoundaryPreflight();
            Debug.Log("[NormalOriginSubjectAndRouteSerializer] result=Collected subjectControls=12 serializedSizes=3 worldsCreated=0 nativeQueries=0 contextControlsExecuted=0 sourceFitCertified=0 normalMissionReady=0");
        }

        public static void RunReviewedRouteSerializerBoundaryPreflight()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&
                completeGroundBaAuthority==null&&completeGroundBaSourcePins==null&&normalFacilityOriginExportRequest==null&&
                finiteFacilityOriginExportRequest==null&&!mpGroundBaPilotRequested&&!s049BoundedCompoundRouteRequested&&
                !apGroundJointPreferencesRequested&&!refinementFacilityPreferencesRequested&&!refinementPreferenceReplay&&
                !refinementCompoundSearchRequested&&!refinementJointPadSearchRequested&&!refinementLogicalFrameSearchRequested&&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ReviewedRouteDeltaEnv))&&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ReviewedRouteDeltaShaEnv)),
                "Idle unowned serializer preflight, before any route override");
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            Require(id!="S049"&&id!="S050","Dedicated MP endpoint revision path is not this generic serializer boundary");
            var authority=GroundBa10Rows().Single(r=>(string)r["catalog_id"]==id);ValidateCompleteGroundBaAuthorityRow(authority);
            var mission=All120MeasurementMission(authority,GroundBaTemplate(authority));
            var pin=(JObject)mission["source_candidate"];
            var pad=ReadReviewedFacilityPadInput(mission);Require(pad!=null,"Actual reviewed frame/pad input required");
            string output=Environment.GetEnvironmentVariable("WARLINE_REVIEWED_ROUTE_BOUNDARY_OUTPUT");
            Require(!string.IsNullOrEmpty(output)&&System.IO.Path.IsPathRooted(output)&&
                !output.Split(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar).Contains(".."),
                "Explicit owned serializer output required");
            string project=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,".."));
            string logs=System.IO.Path.Combine(project,"Logs","SkirmishProduction")+System.IO.Path.DirectorySeparatorChar;
            output=System.IO.Path.GetFullPath(output);string layoutPath=output+".layout.json";
            Require(output.StartsWith(logs,StringComparison.OrdinalIgnoreCase)&&
                !System.IO.File.Exists(output)&&!System.IO.Directory.Exists(output)&&
                !System.IO.File.Exists(layoutPath)&&!System.IO.Directory.Exists(layoutPath)&&
                System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(output)),"Fresh files under existing owned Logs directory required");
            string current=System.IO.Path.GetDirectoryName(output);
            while(!string.Equals(current,project,StringComparison.OrdinalIgnoreCase))
            {
                Require(current!=null&&(System.IO.File.GetAttributes(current)&System.IO.FileAttributes.ReparsePoint)==0,
                    "No serializer output reparse ancestry");current=System.IO.Path.GetDirectoryName(current);
            }
            var worlds=CaptureMaterializationWorldCensus();var defaultWorld=Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var scenes=Ground30SceneOwnership();byte[] bytes;JObject result;
            string sourcePath=(string)pin["sourcePath"];var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);
            Require(source!=null&&!EditorUtility.IsDirty(source)&&Hash(sourcePath)==(string)pin["sourceAssetSha256"]&&
                Hash((string)pin["gridPath"])==(string)pin["gridAssetSha256"]&&
                Hash((string)pin["surfacePath"])==(string)pin["surfaceAssetSha256"],"Actual clean pinned Campaign source/grid/surface required");
            var metadata=source.GridMetadata;
            var grid=new GridConfig{Width=metadata.Dimensions.x,Height=metadata.Dimensions.y,
                CellSize=metadata.CellSize,Origin=(float3)metadata.Origin};
            Require(grid.Width>0&&grid.Height>0&&float.IsFinite(grid.CellSize)&&grid.CellSize>0&&math.all(math.isfinite(grid.Origin))&&
                AssetDatabase.GUIDToAssetPath(metadata.AssetGuid)==(string)pin["gridPath"],
                "Actual pinned authored source grid metadata required; not a loaded-native-grid observation");
            Require(TryCreateGroundBa10Candidate(authority,source,out var owner,out string error),error);
            using(owner)
            {
                Require(owner.Definition.ObjectiveConfig.Kind==SkirmishObjectiveKind.BaseAssault&&
                    owner.Layout.OperationMapId==source.OperationMapId&&owner.Layout.WorldBinding.SourceHash==source.ContentHash,
                    "Actual generic BA factory and source binding required");
                string original=JsonUtility.ToJson(owner.Layout);
                var compounds=CandidateCompoundRects((JObject)mission["layout_input"]["pilot_candidate"]);
                Require(TryNextRefinementLogicalFrame(original,compounds,(int)pad["fixedLogicalFrameAttempt"],owner.Layout,source,grid,
                    out var framedCompounds,out string refused),refused);
                string afterFrame=JsonUtility.ToJson(owner.Layout);
                Require(TryEncodeReviewedFacilityPads(afterFrame,pad,grid,framedCompounds,out string encoded,out refused),refused);
                JsonUtility.FromJsonOverwrite(encoded,owner.Layout);
                // This is precisely the r4 encoder's pre-route string, not JObject reserialization.
                string boundary=JsonUtility.ToJson(owner.Layout);bytes=new System.Text.UTF8Encoding(false).GetBytes(boundary);
                Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out error),error);
                var budgets=new JArray();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                        budget.ExactCanonicalQuantities,"Actual canonical static authoring budget required: "+string.Join(";",reasons));
                    budgets.Add(ApPreferenceBudgetKey(budget));
                }
                Require(boundary==JsonUtility.ToJson(owner.Layout),"Static budget inspection must preserve exact serializer boundary");
                result=new JObject{["schema"]=1,["scope"]="ExactFactoryFramePadSerializerBoundary_NoWorldOrPlacement",
                    ["catalogId"]=id,["definitionId"]=owner.Definition.DefinitionId,["scenarioSetupId"]=owner.Definition.ScenarioSetupId,
                    ["sourcePins"]=pin.DeepClone(),["sourceGridMetadata"]=new JObject{["scope"]="AuthoredPinnedMetadata_NotLoadedNativeWorld",
                        ["width"]=grid.Width,["height"]=grid.Height,["cellSize"]=grid.CellSize,
                        ["origin"]=new JArray(grid.Origin.x,grid.Origin.y,grid.Origin.z)},
                    ["authorityIndex"]=new JObject{["path"]=All120RefinementIndex,["sha256"]=All120RefinementIndexSha},
                    ["canonical_size_rows"]=authority["canonical_size_rows"].DeepClone(),["canonicalStaticBudgets"]=budgets,
                    ["logicalPadInput"]=new JObject{["path"]=pad["observedInputPath"].DeepClone(),["sha256"]=pad["observedInputSha256"].DeepClone()},
                    ["frameAttempt"]=pad["fixedLogicalFrameAttempt"].DeepClone(),["originalCompounds"]=compounds,["appliedCompounds"]=framedCompounds,
                    ["originalFactoryLayoutSha256"]=RouteDeltaSha(new System.Text.UTF8Encoding(false).GetBytes(original)),
                    ["postFrameLayoutSha256"]=RouteDeltaSha(new System.Text.UTF8Encoding(false).GetBytes(afterFrame)),
                    ["layoutBeforeRouteInput"]=new JObject{["path"]=layoutPath,["sha256"]=RouteDeltaSha(bytes),["bytes"]=bytes.Length},
                    ["worldsCreated"]=0,["nativeQueries"]=0,["placementMeasured"]=false,["sourceFitCertified"]=false,
                    ["normalMissionReady"]=false,["missionAccepted"]=false};
            }
            Require(worlds.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(defaultWorld,Unity.Entities.World.DefaultGameObjectInjectionWorld)&&
                JToken.DeepEquals(scenes,Ground30SceneOwnership())&&!EditorUtility.IsDirty(source)&&
                Hash(sourcePath)==(string)pin["sourceAssetSha256"]&&Hash((string)pin["gridPath"])==(string)pin["gridAssetSha256"]&&
                Hash((string)pin["surfacePath"])==(string)pin["surfaceAssetSha256"]&&
                Hash((string)pad["observedInputPath"])==(string)pad["observedInputSha256"],
                "Exact World/scene/default/source/grid/surface/input restoration before output");
            void WriteNew(string path,byte[] data)
            {
                string ancestor=System.IO.Path.GetDirectoryName(path);
                while(!string.Equals(ancestor,project,StringComparison.OrdinalIgnoreCase))
                {
                    Require(ancestor!=null&&(System.IO.File.GetAttributes(ancestor)&System.IO.FileAttributes.ReparsePoint)==0,
                        "Output ancestry must remain ordinary immediately before each create");ancestor=System.IO.Path.GetDirectoryName(ancestor);
                }
                using var stream=new System.IO.FileStream(path,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read);
                stream.Write(data,0,data.Length);stream.Flush(true);
            }
            WriteNew(layoutPath,bytes);
            Require(Hash(layoutPath)==(string)result["layoutBeforeRouteInput"]["sha256"],"Durable exact boundary bytes required");
            WriteNew(output,new System.Text.UTF8Encoding(false).GetBytes(result.ToString(Newtonsoft.Json.Formatting.None)));
            Debug.Log("[ReviewedRouteSerializerBoundary] result=Collected catalog="+id+
                " sizes=3 worldsCreated=0 nativeQueries=0 sourceFitCertified=0 normalMissionReady=0 output="+output);
        }

        const string NormalOriginRequestEnv="WARLINE_NORMAL_FACILITY_ORIGIN_REQUEST";
        const string NormalOriginRequestShaEnv="WARLINE_NORMAL_FACILITY_ORIGIN_REQUEST_SHA256";
        static JObject normalFacilityOriginExportRequest;

        // This is a replay of one recorded allocator prefix, not independent capacity.
        static JToken NormalOriginJson(JToken value)=>JToken.Parse(value.ToString(Newtonsoft.Json.Formatting.None));

        static bool NormalOriginOriginalFc(JObject request,JObject mission)
            =>request?["schema"]?.Type==JTokenType.Integer&&(int)request["schema"]==2&&
                (string)mission?["tuple"]?["army_profile"]=="G"&&(string)mission?["tuple"]?["objective_id"]=="FC";
        static void ValidateNormalOriginFcSubject(JObject authority,JObject candidate,JObject original)
        {
            Require(authority!=null&&candidate!=null&&original!=null&&
                authority["protected_binding"]?.Type==JTokenType.Null&&!ProtectedRefinementCatalog((string)authority["catalog_id"])&&
                (string)authority["tuple"]?["army_profile"]=="G"&&(string)authority["tuple"]?["objective_id"]=="FC"&&
                JToken.DeepEquals(candidate,original)&&candidate["publication_enabled"]?.Type==JTokenType.Boolean&&
                !(bool)candidate["publication_enabled"],"Exact unprotected immutable ground FC subject required");
            foreach(string key in new[]{"catalog_id","definition_id","scenario_setup_id","tuple","packet","canonical_size_rows"})
                Require(JToken.DeepEquals(authority[key],candidate[key]),"FC normal-prefix canonical subject mismatch: "+key);
            var canonical=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>()
                .SingleOrDefault(r=>(string)r["catalog_id"]==(string)authority["catalog_id"]);
            Require(canonical!=null&&JToken.DeepEquals(authority,canonical),"Exact pinned Full120 FC authority required");
            var capabilities=canonical["required_capabilities"] as JArray;
            Require(capabilities!=null&&capabilities.Count>0&&
                capabilities.All(v=>v.Type==JTokenType.String&&!string.IsNullOrEmpty((string)v))&&
                capabilities.Select(v=>(string)v).Distinct(StringComparer.Ordinal).Count()==capabilities.Count,
                "Canonical typed nonempty unique FC capabilities required");
            // Ground30 never authored this field. Preserve that immutable row and
            // validate the existing Full120 overlay actually consumed by refinement.
            var effective=All120MeasurementMission(authority,candidate);
            Require(JToken.DeepEquals(effective["required_capabilities"],capabilities),
                "Effective FC measurement must retain exact canonical capability order and values");

        }
        static void ResolveNormalOriginSubject(string id,out JObject authority,out JObject mission,out bool groundBa)
        {
            var found=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().SingleOrDefault(r=>(string)r["catalog_id"]==id);
            Require(found!=null&&found["protected_binding"]?.Type==JTokenType.Null&&!ProtectedRefinementCatalog(id)&&
                (string)found["tuple"]?["army_profile"]=="G","One real unprotected ground authority required");
            groundBa=(string)found["tuple"]["objective_id"]=="BA";
            if(groundBa)
            {
                authority=GroundBa10Rows().Single(r=>(string)r["catalog_id"]==id);ValidateCompleteGroundBaAuthorityRow(authority);
                mission=All120MeasurementMission(authority,GroundBaTemplate(authority));return;
            }
            Require((string)found["tuple"]["objective_id"]=="FC","Only existing BA or FC normal-prefix diagnostic subjects allowed");
            var original=((JArray)ReadGround30Envelope()["missions"]).OfType<JObject>().Single(r=>(string)r["catalog_id"]==id);
            ValidateNormalOriginFcSubject(found,original,original);
            authority=(JObject)found.DeepClone();mission=All120MeasurementMission(authority,original);
        }
        public static void RunNormalOriginSubjectControls()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle normal-origin subject controls required");
            int controls=0;void Check(bool ok,string why){Require(ok,why);controls++;}
            ResolveNormalOriginSubject("S008",out var fc,out var mission,out bool ba);
            var original=((JArray)ReadGround30Envelope()["missions"]).OfType<JObject>().Single(r=>(string)r["catalog_id"]=="S008");
            Check(!ba&&JToken.DeepEquals(mission["source_candidate"],original["source_candidate"])&&
                JToken.DeepEquals(mission["canonical_size_rows"],original["canonical_size_rows"])&&
                (string)mission["tuple"]["objective_id"]=="FC"&&
                JToken.DeepEquals(mission["required_capabilities"],fc["required_capabilities"]),"Real S008 FC source/budgets/objective/capabilities retained");
            ResolveNormalOriginSubject("S074",out var previous,out var unchanged,out ba);
            Check(ba&&JToken.DeepEquals(unchanged,All120MeasurementMission(previous,GroundBaTemplate(previous))),"Existing BA resolution unchanged");
            Check(completeGroundBaAuthority==null&&completeGroundBaSourcePins==null,"Resolver cannot install BA service authority");
            void BadCandidate(Action<JObject> mutate)
            {
                var bad=(JObject)original.DeepClone();mutate(bad);bool refused=false;
                try{ValidateNormalOriginFcSubject(fc,bad,original);}catch(InvalidOperationException){refused=true;}
                Check(refused,"Immutable FC candidate mutation refused");
            }
            BadCandidate(v=>v["source_candidate"]["gridAssetSha256"]=new string('0',64));
            BadCandidate(v=>v["canonical_size_rows"][0]["player_rifle"]="999");
            BadCandidate(v=>v["tuple"]["objective_id"]="BA");BadCandidate(v=>v["publication_enabled"]=true);
            BadCandidate(v=>v["definition_id"]="foreign");
            var authorityMutations=new Action<JObject>[]{
                v=>v["canonical_size_rows"][1]["enemy_tank"]="999",
                v=>v.Remove("required_capabilities"),
                v=>v["required_capabilities"]=JValue.CreateNull(),
                v=>v["required_capabilities"]="ground",
                v=>v["required_capabilities"]=new JArray(),
                v=>v["required_capabilities"][0]="foreign",
                v=>v["required_capabilities"]=new JArray(((JArray)v["required_capabilities"]).Reverse().Select(x=>x.DeepClone())),
                v=>((JArray)v["required_capabilities"]).Add(v["required_capabilities"][0].DeepClone())};
            bool denied=true;
            foreach(var mutate in authorityMutations)
            {
                var foreign=(JObject)fc.DeepClone();mutate(foreign);bool refused=false;
                try{ValidateNormalOriginFcSubject(foreign,original,original);}catch(InvalidOperationException){refused=true;}
                denied&=refused;
            }
            Check(denied,"All eight canonical authority/capability mutations refused by actual subject validator");
            foreach(string id in new[]{"S999","S001","S013"})
            {
                bool refused=false;try{ResolveNormalOriginSubject(id,out _,out _,out _);}catch(InvalidOperationException){refused=true;}
                Check(refused,"Unknown/protected/other-family subject refused");
            }
            Require(controls==12,"Exactly12 subject controls");
            Debug.Log("[NormalOriginSubjectControls] result=Passed controls=12 nativeQueries=0 sourceFitCertified=0 normalMissionReady=0");
        }

        static void ValidateNormalOriginRequest(JObject request,JObject mission)
        {
            bool originalFc=NormalOriginOriginalFc(request,mission);
            string[] fields={"schema","scope","catalogId","canonicalSizes","faction","structureId","sourcePins","canonical_size_rows",
                "logicalPadInputSha256","maximumOriginsPerRow","maximumOriginsTotal","layout","approachClearance","budgets","contexts",
                "sourceFitCertified","normalMissionReady"};
            if(originalFc)fields=fields.Where(f=>f!="logicalPadInputSha256").Concat(new[]{"originalAuthoringEnvelopeSha256","layoutPolicy"}).ToArray();
            Require(request!=null&&request.Properties().Select(p=>p.Name).OrderBy(p=>p,StringComparer.Ordinal)
                .SequenceEqual(fields.OrderBy(p=>p,StringComparer.Ordinal)),"Exact normal-prefix diagnostic request shape required");
            Require(request["schema"]?.Type==JTokenType.Integer&&(int)request["schema"]==(originalFc?2:1)&&
                (string)request["scope"]=="NormalAllocatorPrefixFiniteOrigins_NotConfigurationFit"&&
                request["catalogId"]?.Type==JTokenType.String&&(string)request["catalogId"]==(string)mission["catalog_id"]&&
                request["faction"]?.Type==JTokenType.Integer&&new[]{1,2}.Contains((int)request["faction"])&&
                request["structureId"]?.Type==JTokenType.String&&!string.IsNullOrEmpty((string)request["structureId"])&&
                JToken.DeepEquals(request["canonicalSizes"],new JArray("Standard","War","LargeWar"))&&
                JToken.DeepEquals(request["sourcePins"],mission["source_candidate"])&&
                JToken.DeepEquals(request["canonical_size_rows"],mission["canonical_size_rows"])&&
                (originalFc
                    ?request["originalAuthoringEnvelopeSha256"]?.Type==JTokenType.String&&
                        (string)request["originalAuthoringEnvelopeSha256"]==GroundObjective30InputsSha&&
                        request["layoutPolicy"]?.Type==JTokenType.String&&
                        (string)request["layoutPolicy"]=="ImmutableAuthoredOriginal_NoPadOrRouteOverride"
                    :request["logicalPadInputSha256"]?.Type==JTokenType.String&&((string)request["logicalPadInputSha256"]).Length==64&&
                        ((string)request["logicalPadInputSha256"]).All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f')))&&
                request["maximumOriginsPerRow"]?.Type==JTokenType.Integer&&(int)request["maximumOriginsPerRow"]>0&&
                (int)request["maximumOriginsPerRow"]<=100000&&
                request["maximumOriginsTotal"]?.Type==JTokenType.Integer&&
                (int)request["maximumOriginsTotal"]==3*(int)request["maximumOriginsPerRow"]&&
                request["layout"] is JObject&&request["approachClearance"] is JArray&&
                request["budgets"] is JArray&&((JArray)request["budgets"]).Count==3&&
                request["contexts"] is JArray&&((JArray)request["contexts"]).Count==3&&
                request["sourceFitCertified"]?.Type==JTokenType.Boolean&&!(bool)request["sourceFitCertified"]&&
                request["normalMissionReady"]?.Type==JTokenType.Boolean&&!(bool)request["normalMissionReady"],
                "Own subject/source/three canonical budgets/typed bounded diagnostic required");
            var contexts=((JArray)request["contexts"]).OfType<JObject>().ToArray();
            Require(contexts.Length==3&&contexts.Select(c=>(string)c["size"]).SequenceEqual(new[]{"Standard","War","LargeWar"})&&
                contexts.All(c=>c["priorReservations"] is JArray&&c["qaKeepClear"] is JArray&&c["target"] is JObject&&
                    c["compound"] is JObject&&c["typedPad"] is JObject),"Exact three recorded allocator contexts required");
        }

        static JObject NormalOriginFacilityIdentity(JObject row)=>new JObject{
            ["catalogId"]=row["catalogId"].DeepClone(),["size"]=row["size"].DeepClone(),
            ["faction"]=row["faction"].DeepClone(),["structureId"]=row["structureId"].DeepClone(),
            ["padId"]=row["padId"].DeepClone(),["prefabPath"]=row["prefabPath"].DeepClone(),
            ["prefabSha256"]=row["prefabSha256"].DeepClone()};
        static JObject NormalOriginPriorIdentity(JObject row)
        {
            var identity=NormalOriginFacilityIdentity(row);
            Require(row["resolvedEffectiveRect"] is JObject,"Every preceding facility must have a real accepted reservation");
            identity["rect"]=row["resolvedEffectiveRect"].DeepClone();return identity;
        }
        static void ValidateNormalOriginContext(JObject expected,JObject actual)
        {
            Require(JToken.DeepEquals(NormalOriginJson(expected),NormalOriginJson(actual)),
                "Recorded normal facility prefix, target, compound, typed pad and full QA must match exactly");
        }

        public static void RunNormalFacilityOriginRequestControls()
        {
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            ResolveNormalOriginSubject(id,out _,out var mission,out _);
            string path=Environment.GetEnvironmentVariable(NormalOriginRequestEnv),digest=Environment.GetEnvironmentVariable(NormalOriginRequestShaEnv);
            RequireFiniteOriginRequestPath(path);
            Require(digest!=null&&digest.Length==64&&Hash(path)==digest&&new System.IO.FileInfo(path).Length<=1048576,
                "Actual reviewed bounded request fixture required for pure context controls");
            var request=JObject.Parse(System.IO.File.ReadAllText(path));
            int controls=0;ValidateNormalOriginRequest(request,mission);controls++;
            void Reject(Action<JObject> mutate)
            {
                var changed=(JObject)request.DeepClone();mutate(changed);bool refused=false;
                try{ValidateNormalOriginRequest(changed,mission);}catch(InvalidOperationException){refused=true;}
                Require(refused,"Malformed normal-origin request must refuse");controls++;
            }
            Reject(r=>r["schema"]="1");Reject(r=>r["catalogId"]=id=="S050"?"S074":"S050");Reject(r=>r["faction"]=0);
            Reject(r=>r["faction"]="1");Reject(r=>r["structureId"]="");Reject(r=>r["sourcePins"]=new JObject());
            Reject(r=>r["canonical_size_rows"]=new JArray());Reject(r=>r["canonicalSizes"]=new JArray("Standard"));
            Reject(r=>r["logicalPadInputSha256"]="bad");Reject(r=>r["maximumOriginsPerRow"]=100001);
            Reject(r=>r["maximumOriginsTotal"]=1);Reject(r=>r["contexts"]=new JArray());
            Reject(r=>r["contexts"][1]["size"]="Standard");Reject(r=>r["sourceFitCertified"]=true);
            Reject(r=>r["normalMissionReady"]=true);
            var context=(JObject)request["contexts"][0].DeepClone();
            ValidateNormalOriginContext(context,(JObject)context.DeepClone());controls++;
            foreach(string field in new[]{"priorReservations","qaKeepClear","target","compound","typedPad"})
            {
                var changed=(JObject)context.DeepClone();
                if(field=="priorReservations"&&((JArray)changed[field]).Count==0)((JArray)changed[field]).Add(new JObject{["unexpectedReservation"]=true});
                else if(field=="priorReservations")changed[field][0]["rect"]["x"]=(int)changed[field][0]["rect"]["x"]+1;
                else if(field=="qaKeepClear")((JArray)changed[field]).RemoveAt(((JArray)changed[field]).Count-1);
                else if(field=="target")changed[field]["prefabSha256"]=new string('0',64);
                else changed[field]["width"]=(int)changed[field]["width"]+1;
                bool refused=false;try{ValidateNormalOriginContext(context,changed);}catch(InvalidOperationException){refused=true;}
                Require(refused,"Actual allocator-context mutation must refuse");controls++;
            }
            Require(Hash(path)==digest,"Actual reviewed context fixture unchanged after controls");
            if(NormalOriginOriginalFc(request,mission))
            {
                Reject(r=>r["originalAuthoringEnvelopeSha256"]=new string('0',64));
                Reject(r=>r["layoutPolicy"]="widen-or-rewrite");
            }
            Require(controls==(NormalOriginOriginalFc(request,mission)?24:22),"Exact normal-prefix request/context controls");
            Debug.Log("[NormalFacilityOriginRequestControls] result=Passed controls="+controls+" worldQueries=0 sourceFitCertified=0 normalMissionReady=0");
        }

        public static void RunReviewedNormalFacilityOriginExport()
        {
            Require(normalFacilityOriginExportRequest==null&&finiteFacilityOriginExportRequest==null&&
                completeGroundBaAuthority==null&&completeGroundBaSourcePins==null&&!mpGroundBaPilotRequested&&
                !s049BoundedCompoundRouteRequested&&!strictLogicalCandidatesRequested&&!apGroundJointPreferencesRequested&&
                !refinementFacilityPreferencesRequested&&!refinementPreferenceReplay&&!refinementCompoundSearchRequested&&
                !refinementJointPadSearchRequested&&!refinementLogicalFrameSearchRequested&&
                !EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,
                "Fresh exclusive idle normal-origin diagnostic scope required");
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            ResolveNormalOriginSubject(id,out var authority,out var mission,out bool groundBa);
            string path=Environment.GetEnvironmentVariable(NormalOriginRequestEnv),digest=Environment.GetEnvironmentVariable(NormalOriginRequestShaEnv);
            RequireFiniteOriginRequestPath(path);
            Require(digest!=null&&digest.Length==64&&digest.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'))&&Hash(path)==digest,
                "Reviewed request path and exact SHA pair required");
            Require(new System.IO.FileInfo(path).Length<=1048576,"Bounded diagnostic request bytes required");
            var request=JObject.Parse(System.IO.File.ReadAllText(path));ValidateNormalOriginRequest(request,mission);
            var pad=ReadReviewedFacilityPadInput(mission);
            Require((groundBa
                    ?pad!=null&&(string)pad["observedInputSha256"]==(string)request["logicalPadInputSha256"]
                    :pad==null&&NormalOriginOriginalFc(request,mission)&&
                        string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ReviewedRouteDeltaEnv))&&
                        string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ReviewedRouteDeltaShaEnv)))&&Hash(path)==digest,
                "Exact BA pad input or immutable original FC layout; no FC pad/route override");
            RunNormalOriginSubjectControls();RunNormalFacilityOriginRequestControls();
            normalFacilityOriginExportRequest=(JObject)request.DeepClone();
            if(groundBa)
            {
                completeGroundBaAuthority=(JObject)authority.DeepClone();
                completeGroundBaSourcePins=(JObject)mission["source_candidate"].DeepClone();
            }
            try{RunAll120BoundedFacilityPreferences();}
            finally
            {
                normalFacilityOriginExportRequest=null;completeGroundBaAuthority=null;completeGroundBaSourcePins=null;
                Require(Hash(path)==digest&&(groundBa
                    ?Hash((string)pad["observedInputPath"])==(string)request["logicalPadInputSha256"]
                    :Hash(GroundObjective30Inputs)==GroundObjective30InputsSha),
                    "Diagnostic original inputs unchanged after collection");
            }
            var output=JObject.Parse(System.IO.File.ReadAllText(Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT")));
            var collected=output.SelectTokens("$..normalPrefixOriginExport").OfType<JObject>().ToArray();
            Require(collected.Length==1&&collected[0]["rows"] is JArray&&((JArray)collected[0]["rows"]).Count==3,
                "Exactly one complete three-size diagnostic export required");
            Debug.Log("[ReviewedNormalFacilityOriginExport] result=Collected sizes=3 normalPrefix=1 packing=0 fullRouteAcceptance=0 sourceFitCertified=0 normalMissionReady=0");
        }

        const string FiniteOriginRequestEnv="WARLINE_FINITE_FACILITY_ORIGIN_REQUEST";
        const string FiniteOriginRequestShaEnv="WARLINE_FINITE_FACILITY_ORIGIN_REQUEST_SHA256";
        const string FiniteOriginPadSha="17b3013d6cba8ebd6a738f5d43c7d301fd6407d4935b7b1272c9021cf580ccce";
        static JObject finiteFacilityOriginExportRequest;
        static readonly string[] FiniteOriginStructures={"building.barracks","building.refinery","building.refinery.module"};

        static void ValidateFiniteOriginRequest(JObject request,JObject mission)
        {
            string[] fields={"schema","scope","catalogId","canonicalSizes","faction","structureIds","sourcePins","canonical_size_rows",
                "logicalPadInputSha256","maximumOriginsPerRow","sourceFitCertified","normalMissionReady"};
            Require(request!=null&&request.Properties().Select(p=>p.Name).OrderBy(p=>p,StringComparer.Ordinal).SequenceEqual(fields.OrderBy(p=>p,StringComparer.Ordinal)),
                "Exact finite-origin diagnostic request shape required");
            Require(request["schema"]?.Type==JTokenType.Integer&&(int)request["schema"]==1&&
                (string)request["scope"]=="IndependentFullFiniteOriginExport_NotConfigurationFit"&&
                (string)request["catalogId"]=="S050"&&(string)mission["catalog_id"]=="S050"&&
                request["faction"]?.Type==JTokenType.Integer&&(int)request["faction"]==2&&
                JToken.DeepEquals(request["canonicalSizes"],new JArray("Standard","War","LargeWar"))&&
                JToken.DeepEquals(request["structureIds"],new JArray(FiniteOriginStructures))&&
                JToken.DeepEquals(request["sourcePins"],mission["source_candidate"])&&
                JToken.DeepEquals(request["canonical_size_rows"],mission["canonical_size_rows"])&&
                (string)request["logicalPadInputSha256"]==FiniteOriginPadSha&&
                request["maximumOriginsPerRow"]?.Type==JTokenType.Integer&&(int)request["maximumOriginsPerRow"]==20850&&
                request["sourceFitCertified"]?.Type==JTokenType.Boolean&&!(bool)request["sourceFitCertified"]&&
                request["normalMissionReady"]?.Type==JTokenType.Boolean&&!(bool)request["normalMissionReady"],
                "Exact S050 own source/three budgets/input/faction/finite bound required");
        }

        static void RequireFiniteOriginRequestPath(string path)
        {
            string root=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,".."));
            string logs=System.IO.Path.Combine(root,"Logs","SkirmishProduction")+System.IO.Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(path)&&System.IO.Path.IsPathRooted(path)&&
                !path.Split(System.IO.Path.DirectorySeparatorChar,System.IO.Path.AltDirectorySeparatorChar).Contains("..")&&
                System.IO.Path.GetFullPath(path).StartsWith(logs,StringComparison.OrdinalIgnoreCase)&&System.IO.File.Exists(path),
                "Existing owned Logs request path required");
            string current=System.IO.Path.GetFullPath(path);
            while(!string.Equals(current,root,StringComparison.OrdinalIgnoreCase))
            {
                Require((System.IO.File.GetAttributes(current)&System.IO.FileAttributes.ReparsePoint)==0,"No request reparse ancestry");
                current=System.IO.Path.GetDirectoryName(current);Require(current!=null,"Owned request ancestry required");
            }
        }

        static JObject FiniteOriginControlRequest(JObject mission)=>new JObject{
            ["schema"]=1,["scope"]="IndependentFullFiniteOriginExport_NotConfigurationFit",["catalogId"]="S050",
            ["canonicalSizes"]=new JArray("Standard","War","LargeWar"),["faction"]=2,["structureIds"]=new JArray(FiniteOriginStructures),
            ["sourcePins"]=mission["source_candidate"].DeepClone(),["canonical_size_rows"]=mission["canonical_size_rows"].DeepClone(),
            ["logicalPadInputSha256"]=FiniteOriginPadSha,["maximumOriginsPerRow"]=20850,["sourceFitCertified"]=false,["normalMissionReady"]=false};

        public static void RunFiniteFacilityOriginRequestControls()
        {
            var authority=GroundBa10Rows().Single(r=>(string)r["catalog_id"]=="S050");
            var mission=All120MeasurementMission(authority,GroundBaTemplate(authority));
            var valid=FiniteOriginControlRequest(mission);int controls=0;
            ValidateFiniteOriginRequest(valid,mission);controls++;
            void Refuse(Action<JObject> change)
            {
                var bad=(JObject)valid.DeepClone();change(bad);bool refused=false;
                try{ValidateFiniteOriginRequest(bad,mission);}catch(InvalidOperationException){refused=true;}
                Require(refused,"Malformed finite-origin request must refuse before native work");controls++;
            }
            Refuse(x=>x["schema"]="1");Refuse(x=>x["schema"]=1.0);
            Refuse(x=>x["faction"]="2");Refuse(x=>x["faction"]=1);
            Refuse(x=>x["catalogId"]="S049");Refuse(x=>x["scope"]="ConfigurationFit");
            Refuse(x=>x["canonicalSizes"]=new JArray("War","Standard","LargeWar"));
            Refuse(x=>((JArray)x["structureIds"]).RemoveAt(2));
            Refuse(x=>x["structureIds"][2]="building.refinery");
            Refuse(x=>x["sourcePins"]["contentHash"]="foreign");
            Refuse(x=>x["canonical_size_rows"][0]["materials_each"]=(string)x["canonical_size_rows"][0]["materials_each"]+".corrupt");
            Refuse(x=>x["logicalPadInputSha256"]=new string('0',64));
            Refuse(x=>x["maximumOriginsPerRow"]=20851);
            Refuse(x=>x["sourceFitCertified"]=true);Refuse(x=>x["extra"]=true);
            Refuse(x=>x["normalMissionReady"]="false");
            bool missing=false;try{ValidateFiniteOriginRequest(null,mission);}catch(InvalidOperationException){missing=true;}
            Require(missing,"Missing finite-origin request refused");controls++;
            Require(controls==18,"Exact eighteen finite-origin request controls");
            Debug.Log("[FiniteFacilityOriginRequestControls] result=Passed controls=18 nativeQueries=0 sourceWorlds=0 sourceFitCertified=0 normalMissionReady=0");
        }

        public static void RunReviewedFiniteFacilityOriginExport()
        {
            Require(finiteFacilityOriginExportRequest==null&&!mpGroundBaPilotRequested&&!s049BoundedCompoundRouteRequested&&!strictLogicalCandidatesRequested&&
                !apGroundJointPreferencesRequested&&!refinementFacilityPreferencesRequested&&!refinementPreferenceReplay&&
                !refinementCompoundSearchRequested&&!refinementJointPadSearchRequested&&!refinementLogicalFrameSearchRequested&&
                !EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,
                "Fresh idle unowned finite-origin diagnostic scope required");
            Require(Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG")=="S050","Exact S050 diagnostic subject");
            var authority=GroundBa10Rows().Single(r=>(string)r["catalog_id"]=="S050");
            var mission=All120MeasurementMission(authority,GroundBaTemplate(authority));
            string path=Environment.GetEnvironmentVariable(FiniteOriginRequestEnv),digest=Environment.GetEnvironmentVariable(FiniteOriginRequestShaEnv);
            RequireFiniteOriginRequestPath(path);
            Require(digest!=null&&digest.Length==64&&digest.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'))&&Hash(path)==digest,"Exact request byte pin required");
            var request=JObject.Parse(System.IO.File.ReadAllText(path));ValidateFiniteOriginRequest(request,mission);
            Require(Hash(path)==digest,"Request unchanged after read");
            var pad=ReadReviewedFacilityPadInput(mission);
            Require(pad!=null&&(string)pad["observedInputSha256"]==FiniteOriginPadSha,"Exact unchanged reviewed S050 pad input required");
            RunFiniteFacilityOriginRequestControls();
            finiteFacilityOriginExportRequest=(JObject)request.DeepClone();mpGroundBaPilotRequested=true;
            try{RunAll120BoundedFacilityPreferences();}
            finally
            {
                finiteFacilityOriginExportRequest=null;mpGroundBaPilotRequested=false;
                Require(Hash(path)==digest&&Hash((string)pad["observedInputPath"])==FiniteOriginPadSha,"Diagnostic inputs unchanged; no authority renewal");
            }
            Debug.Log("[ReviewedFiniteFacilityOriginExport] result=Collected catalog=S050 canonicalSizes=3 originConfigurations=1 faction=2 structures=3 fullRouteAcceptance=0 sourceFitCertified=0 normalMissionReady=0");
        }

        static bool refinementFacilityPreferencesRequested,refinementPreferenceReplay;
        static bool apGroundJointPreferencesRequested;
        const string ApJointPadInputEnv="WARLINE_AP_JOINT_PAD_INPUT";
        const string ApJointPadInputShaEnv="WARLINE_AP_JOINT_PAD_INPUT_SHA256";

        public static void RunApGroundPairJointPreferenceStaticCollection()
        {
            Require(!apGroundJointPreferencesRequested&&!refinementFacilityPreferencesRequested&&!refinementPreferenceReplay&&
                !refinementJointPadSearchRequested&&!refinementCompoundSearchRequested&&!refinementLogicalFrameSearchRequested,
                "Exclusive bounded AP logical-preference collection required");
            RunFacilityPreferenceControls();
            WithApJointPreferenceMode(()=>
            {
                RunApGroundPairStaticCollection();
                Debug.Log("[APGroundPairJointPreferenceCollection] result=Collected routeSeedLimit=4 assignmentLimit=9 sourceFitCertified=0 normalMissionReady=0");
            });
        }

        static void WithApJointPreferenceMode(Action collect)
        {
            Require(collect!=null&&!apGroundJointPreferencesRequested&&!refinementFacilityPreferencesRequested&&!refinementPreferenceReplay&&
                !refinementJointPadSearchRequested&&!refinementCompoundSearchRequested&&!refinementLogicalFrameSearchRequested,
                "Exclusive AP preference mode required");
            apGroundJointPreferencesRequested=refinementFacilityPreferencesRequested=true;
            try{collect();}
            finally{apGroundJointPreferencesRequested=refinementFacilityPreferencesRequested=refinementPreferenceReplay=false;}
        }

        // AP keeps the original faction separator while widening only logical search space.
        // Generic refinement retains its existing symmetric expansion/refusal policy.
        static bool TryNextApJointLogicalFrame(string originalJson,JObject originalCompounds,
            SkirmishMapLayoutConfig current,OperationMapDefinition source,GridConfig grid,out JObject compounds,out string error)
        {
            compounds=null;error=null;
            var original=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            var proposed=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            try
            {
                JsonUtility.FromJsonOverwrite(originalJson,original);
                Require(source!=null&&original.OperationMapId==source.OperationMapId&&original.WorldBinding.SourceHash==source.ContentHash,
                    "Exact original AP Campaign source binding required");
                var oldRect=new Vector4(original.OriginX,original.OriginZ,original.OriginX+original.WorldWidthMetres,original.OriginZ+original.WorldDepthMetres);
                var bounds=new Vector4(math.max(source.Bounds.PlayableMin.x,grid.Origin.x),math.max(source.Bounds.PlayableMin.z,grid.Origin.z),
                    math.min(source.Bounds.PlayableMax.x,grid.Origin.x+grid.Width*grid.CellSize),
                    math.min(source.Bounds.PlayableMax.z,grid.Origin.z+grid.Height*grid.CellSize));
                if(!TryBoundedLogicalFrameRect(oldRect,bounds,128,out var rect,out error)||
                    !TryExpandApSeparatedCompounds(originalCompounds,128,rect,out var expanded,out error))return false;
                string next=EncodeWidenedRefinementLayout(original,rect);
                JsonUtility.FromJsonOverwrite(next,proposed);
                RequireLogicalFrameWorldEquivalence(original,proposed);
                JsonUtility.FromJsonOverwrite(next,current);
                RequireLogicalFrameWorldEquivalence(original,current);
                compounds=expanded;return true;
            }
            finally{UnityEngine.Object.DestroyImmediate(proposed);UnityEngine.Object.DestroyImmediate(original);}
        }

        static bool TryExpandApSeparatedCompounds(JObject original,float margin,Vector4 envelope,out JObject result,out string error)
        {
            result=null;error=null;
            if(original==null||original.Properties().Count()!=2||original["1"] is not JArray one||original["2"] is not JArray two||
                !float.IsFinite(margin)||margin<=0||!float.IsFinite(envelope.x)||!float.IsFinite(envelope.y)||
                !float.IsFinite(envelope.z)||!float.IsFinite(envelope.w)||envelope.z<=envelope.x||envelope.w<=envelope.y)
            {error="Exact two AP compounds and finite source/grid envelope required";return false;}
            foreach(var row in new[]{one,two})
            {
                if(row.Count!=4||row.Any(v=>v.Type!=JTokenType.Float&&v.Type!=JTokenType.Integer)||
                    row.Any(v=>!float.IsFinite((float)v))||(float)row[2]<=(float)row[0]||(float)row[3]<=(float)row[1]||
                    (float)row[0]<envelope.x||(float)row[1]<envelope.y||(float)row[2]>envelope.z||(float)row[3]>envelope.w)
                {error="Original AP compound outside exact source/grid/frame envelope";return false;}
            }
            int axis;bool oneLow;float separator;
            if((float)one[2]<=(float)two[0]){axis=0;oneLow=true;separator=(float)one[2]+((float)two[0]-(float)one[2])*.5f;}
            else if((float)two[2]<=(float)one[0]){axis=0;oneLow=false;separator=(float)two[2]+((float)one[0]-(float)two[2])*.5f;}
            else if((float)one[3]<=(float)two[1]){axis=1;oneLow=true;separator=(float)one[3]+((float)two[1]-(float)one[3])*.5f;}
            else if((float)two[3]<=(float)one[1]){axis=1;oneLow=false;separator=(float)two[3]+((float)one[1]-(float)two[3])*.5f;}
            else{error="Original AP factions overlap; no legal separator or territory waiver";return false;}
            var next=new JObject();
            foreach(string side in new[]{"1","2"})
            {
                var old=(JArray)original[side];
                var row=new JArray(math.max(envelope.x,(float)old[0]-margin),math.max(envelope.y,(float)old[1]-margin),
                    math.min(envelope.z,(float)old[2]+margin),math.min(envelope.w,(float)old[3]+margin));
                bool low=(side=="1")==oneLow;
                if(low)row[axis+2]=math.min((float)row[axis+2],separator);
                else row[axis]=math.max((float)row[axis],separator);
                Require((float)row[0]<=(float)old[0]&&(float)row[1]<=(float)old[1]&&
                    (float)row[2]>=(float)old[2]&&(float)row[3]>=(float)old[3],"Every original AP typed compound retained");
                next[side]=row;
            }
            var a=(JArray)next["1"];var b=(JArray)next["2"];
            if((float)a[0]<(float)b[2]&&(float)b[0]<(float)a[2]&&(float)a[1]<(float)b[3]&&(float)b[1]<(float)a[3])
            {error="Expanded AP factions overlap; no territory waiver";return false;}
            result=next;return true;
        }

        static JObject ApplyApJointPadInput(JObject mission,SkirmishMapLayoutConfig layout,OperationMapDefinition source,GridConfig grid,List<SkirmishStaticAuthoringBudget> budgets)
        {
            string path=Environment.GetEnvironmentVariable(ApJointPadInputEnv),hash=Environment.GetEnvironmentVariable(ApJointPadInputShaEnv);
            string allowed=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+System.IO.Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(path)&&System.IO.Path.IsPathRooted(path)&&System.IO.Path.GetFullPath(path).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&
                hash!=null&&System.Text.RegularExpressions.Regex.IsMatch(hash,"^[a-f0-9]{64}$")&&Hash(path)==hash,"Exact reviewed logical pad input required");
            var input=JObject.Parse(System.IO.File.ReadAllText(path));
            Require((int?)input["schema"]==1&&JToken.DeepEquals(input["sourcePins"],mission["source_candidate"])&&
                ((JArray)input["catalogIds"]).Values<string>().SequenceEqual(new[]{"S097","S098"}),"Exact existing AP pair/source authority required");
            string id=(string)mission["catalog_id"];var rows=((JArray)input["padHints"]).OfType<JObject>().Where(r=>(string)r["catalogId"]==id).ToArray();
            var expected=new List<string>();
            foreach(byte side in new byte[]{1,2})
            {
                Require(layout.TryGetPad(side,SkirmishLegalPadKind.Supply,out var supply),"Actual faction Supply required");expected.Add(supply.PadId);
                if(budgets.Any(b=>b.Structures.Any(s=>s.FactionId==side&&s.StructureId==SkirmishStructureIds.Helipad)))
                {Require(layout.TryGetPad(side,SkirmishLegalPadKind.AirReturn,out var air),"Actual faction helipad area required");expected.Add(air.PadId);}
            }
            expected=expected.OrderBy(v=>v,StringComparer.Ordinal).ToList();
            Require(rows.Length==expected.Count&&rows.Select(r=>(string)r["padId"]).OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(expected),
                "Complete exact reviewed Supply/AirReturn pad set required");
            string before=JsonUtility.ToJson(layout);var data=JObject.Parse(before);var pads=(JArray)data["pads"];
            foreach(var row in rows)
            {
                var pad=pads.OfType<JObject>().Single(p=>(string)p["PadId"]==(string)row["padId"]);
                Require((string)pad["AnchorId"]==(string)row["anchorIdUnchanged"]&&(int)pad["Kind"]==(int)row["kind"],"Stable typed pad owner/kind");
                var original=(JObject)row["before"];var next=(JObject)row["proposed"];
                foreach(string key in new[]{"CenterX","CenterZ","WidthMetres","DepthMetres"})
                    Require((float)original[key]==(float)pad[key]&&float.IsFinite((float)next[key]),"Exact original and finite proposed pad field");
                float x=(float)next["CenterX"],z=(float)next["CenterZ"],w=(float)next["WidthMetres"],d=(float)next["DepthMetres"];
                float l=x-w*.5f,r=x+w*.5f,b=z-d*.5f,t=z+d*.5f;
                Require(w>0&&d>0&&l>=source.Bounds.PlayableMin.x&&r<=source.Bounds.PlayableMax.x&&b>=source.Bounds.PlayableMin.z&&t<=source.Bounds.PlayableMax.z&&
                    l>=grid.Origin.x&&r<=grid.Origin.x+grid.Width*grid.CellSize&&b>=grid.Origin.z&&t<=grid.Origin.z+grid.Height*grid.CellSize&&
                    l>=layout.OriginX&&r<=layout.OriginX+layout.WorldWidthMetres&&b>=layout.OriginZ&&t<=layout.OriginZ+layout.WorldDepthMetres,
                    "Full proposed pad must remain in actual source/playable/grid/logical bounds");
                float ox=(float)pad["CenterX"],oz=(float)pad["CenterZ"],ow=(float)pad["WidthMetres"],od=(float)pad["DepthMetres"];
                Require(l<=ox-ow*.5f&&r>=ox+ow*.5f&&b<=oz-od*.5f&&t>=oz+od*.5f,"Original typed area must be retained");
                foreach(string key in new[]{"CenterX","CenterZ","WidthMetres","DepthMetres"})pad[key]=next[key].DeepClone();
            }
            JsonUtility.FromJsonOverwrite(data.ToString(Newtonsoft.Json.Formatting.None),layout);
            Require(Hash(path)==hash,"Pad input unchanged after read");
            return new JObject{["inputPath"]=path,["inputSha256"]=hash,["beforeLayoutJson"]=before,["afterLayoutJson"]=JsonUtility.ToJson(layout),
                ["sourceFitCertified"]=false,["normalMissionReady"]=false};
        }

        static JObject PrepareApJointRouteSeed(GroundObjective30CandidateOwner owner,List<SkirmishStaticAuthoringBudget> budgets,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface)
        {
            string original=JsonUtility.ToJson(owner.Layout);var trials=new JArray();
            var offsets=new[]{new Vector2(-32,0),new Vector2(0,32),new Vector2(0,-32),new Vector2(32,0)};
            var report=new JObject{["maximumEndpointCandidates"]=4,["maximumNativeRepairQueriesPerCandidate"]=2048,["trials"]=trials,["sourceFitCertified"]=false};
            for(int candidate=0;candidate<offsets.Length;candidate++)
            {
                JsonUtility.FromJsonOverwrite(original,owner.Layout);
                var trial=new JObject{["candidate"]=candidate,["offsetX"]=offsets[candidate].x,["offsetZ"]=offsets[candidate].y};trials.Add(trial);
                if(!TryEncodeJointPadTranslation(original,offsets[candidate],true,out string translated,out string refusal))
                {trial["refused"]=refusal;continue;}
                JsonUtility.FromJsonOverwrite(translated,owner.Layout);
                var baseline=new JArray(budgets.Select(b=>Ground30RouteQueries(b,owner.Layout,em,gridEntity,grid,surface,new JArray())));
                trial["repair"]=ProposeBoundedNativeRouteRepair(owner.Layout,budgets,baseline,em,gridEntity,grid,surface);
                var verified=new JArray();bool exact=true;
                foreach(var budget in budgets)
                {
                    var routes=Ground30RouteQueries(budget,owner.Layout,em,gridEntity,grid,surface,new JArray());
                    bool independent=PreferencePathsIndependent(owner.Layout,grid,routes,out var evidence);
                    verified.Add(new JObject{["size"]=budget.Size.ToString(),["routes"]=routes,["independence"]=evidence});
                    exact&=JointRoutesExact(routes)&&independent;
                }
                trial["afterRepair"]=verified;
                if(!exact)continue;
                report["selectedCandidate"]=candidate;report["selectedLayoutJson"]=JsonUtility.ToJson(owner.Layout);return report;
            }
            JsonUtility.FromJsonOverwrite(original,owner.Layout);
            report["refused"]="Four explicit logical endpoint seeds exhausted; no legal source fit inferred";
            return report;
        }

        static JObject CanonicalPreferenceCheckpoint(JObject step,int candidate)
        {
            Require(step!=null&&candidate>=0&&candidate<9&&step["pass"]?.Type==JTokenType.Integer&&
                (int)step["pass"]>=0&&(int)step["pass"]<3&&step["discoveryOnly"]?.Type==JTokenType.Boolean&&
                new[]{"S097","S098"}.Contains((string)step["catalogId"])&&new[]{"Standard","War","LargeWar"}.Contains((string)step["size"])&&
                step["facts"] is JObject,"Exact bounded AP local checkpoint required");
            return new JObject{["catalogId"]=step["catalogId"].DeepClone(),["pass"]=candidate*3+(int)step["pass"],
                ["discoveryOnly"]=step["discoveryOnly"].DeepClone(),["size"]=step["size"].DeepClone(),["facts"]=step["facts"].DeepClone()};
        }



        public static void RunAll120BoundedFacilityPreferences()
        {
            Require(!refinementFacilityPreferencesRequested&&!refinementJointPadSearchRequested&&!refinementCompoundSearchRequested&&
                !refinementLogicalFrameSearchRequested&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,
                "Fresh idle source-only facility preference proposal required");
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            Require(!string.IsNullOrEmpty(id)&&!ProtectedRefinementCatalog(id),"Protected coded bindings cannot be reauthored");
            RunFacilityPreferenceControls();
            refinementFacilityPreferencesRequested=true;
            try{RunAll120SimultaneousLogicalRefinement();}
            finally{refinementFacilityPreferencesRequested=refinementPreferenceReplay=false;}
        }

        static JObject RefineFacilityPreferences(JObject mission,GroundObjective30CandidateOwner owner,OperationMapDefinition source,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,List<SkirmishStaticAuthoringBudget> budgets,
            JArray baselineRoutes,string prefix,Action<JObject> checkpoint)
        {
            Require(!ProtectedRefinementCatalog(owner.Definition.CatalogId),"No coded layout preference changes");
            string before=JsonUtility.ToJson(owner.Layout);
            var originalCompounds=CandidateCompoundRects((JObject)mission["layout_input"]["pilot_candidate"]);
            var report=new JObject{["scope"]="BoundedLogicalPreferences_SourceQaHintsThenCanonicalNativeReplay",
                ["sourceFitCertified"]=false,["cameraAccepted"]=false,["normalMissionReady"]=false,["maximumPreferenceAssignments"]=9};
            var logicalPadInput=ReadReviewedFacilityPadInput(mission);
            var reviewedRouteInput=ReadReviewedRouteDelta(mission);
            JObject reviewedRouteProof=null;
            int frameAttempt=logicalPadInput==null?3:(int)logicalPadInput["fixedLogicalFrameAttempt"];
            JObject compounds;string error;
            bool originalFc=normalFacilityOriginExportRequest!=null&&NormalOriginOriginalFc(normalFacilityOriginExportRequest,mission);
            bool frameReady;
            if(originalFc)
            {
                Require(logicalPadInput==null&&reviewedRouteInput==null,"FC prefix uses exact original layout, never optional geometry hints");
                compounds=(JObject)originalCompounds.DeepClone();error=null;frameReady=true;
                report["immutableOriginalFcPrefixDiagnostic"]=true;
            }
            else frameReady=apGroundJointPreferencesRequested
                ?TryNextApJointLogicalFrame(before,originalCompounds,owner.Layout,source,grid,out compounds,out error)
                :TryNextRefinementLogicalFrame(before,originalCompounds,frameAttempt,owner.Layout,source,grid,out compounds,out error);
            if(!frameReady)
            {report["refused"]=error;return report;}
            if(logicalPadInput!=null)
            {
                RunReviewedFacilityPadInputControls();
                var budgetBefore=new JArray(budgets.Select(ApPreferenceBudgetKey));
                Require(TryEncodeReviewedFacilityPads(JsonUtility.ToJson(owner.Layout),logicalPadInput,grid,compounds,out string next,out string refused),refused);
                JsonUtility.FromJsonOverwrite(next,owner.Layout);
                Require(JToken.DeepEquals(budgetBefore,new JArray(budgets.Select(ApPreferenceBudgetKey))),"Logical pads cannot alter any canonical budget");
                report["reviewedLogicalPadInput"]=logicalPadInput.DeepClone();report["reviewedLogicalPadCompounds"]=compounds.DeepClone();
                report["reviewedLogicalPadAppliedLayoutJson"]=next;report["reviewedLogicalPadBudgets"]=budgetBefore;
                report["fixedReviewedPadCandidate"]=true;
            }

            if(reviewedRouteInput!=null)
            {
                var unchangedBudgets=new JArray(budgets.Select(ApPreferenceBudgetKey));
                string applied=EncodeReviewedRouteDelta(JsonUtility.ToJson(owner.Layout),mission,reviewedRouteInput,grid,out var routeInputEvidence);
                int legs=((JArray)JObject.Parse(applied)["routes"]).OfType<JObject>().Sum(r=>((JArray)r["WaypointAnchorIds"]).Count-1);
                int conservativeProfiles=budgets.Sum(b=>b.Forces.Select(f=>f.FactionId+"/"+f.RuntimePrefabKey).Distinct().Count());
                Require((long)legs*conservativeProfiles<=2048,"Existing bounded source-approach query cap cannot increase");
                JsonUtility.FromJsonOverwrite(applied,owner.Layout);
                Require(JToken.DeepEquals(unchangedBudgets,new JArray(budgets.Select(ApPreferenceBudgetKey))),"Routes cannot alter canonical budgets");
                reviewedRouteProof=routeInputEvidence;report["reviewedRouteInput"]=routeInputEvidence;
                report["reviewedRouteAppliedLayoutJson"]=applied;
            }
            if(apGroundJointPreferencesRequested)
            {
                report["apJointPadInput"]=ApplyApJointPadInput(mission,owner.Layout,source,grid,budgets);
                report["apJointRouteSeed"]=PrepareApJointRouteSeed(owner,budgets,em,gridEntity,grid,surface);
                if(report["apJointRouteSeed"]["selectedCandidate"]==null)return report;
            }
            else if(reviewedRouteInput==null&&!originalFc)
            {
                var routes=ProposeBoundedNativeRouteRepair(owner.Layout,budgets,baselineRoutes,em,gridEntity,grid,surface);
                report["boundedRouteProposal"]=routes;
            }
            // Re-query complete actual paths after the proposal, before deriving
            // any facility keep-clear. Failed native paths remain explicit facts.
            var approachEvidence=new JObject();
            var clearance=PreferenceApproachClearance(owner.Layout,budgets,em,gridEntity,grid,surface,approachEvidence);
            if(reviewedRouteInput!=null)AppendReviewedRoutePointClearance(approachEvidence,clearance);
            report["actualProposedApproaches"]=approachEvidence;

            if(normalFacilityOriginExportRequest!=null)
            {
                var request=normalFacilityOriginExportRequest;ValidateNormalOriginRequest(request,mission);
                Require((originalFc?logicalPadInput==null:
                    logicalPadInput!=null&&(string)logicalPadInput["observedInputSha256"]==(string)request["logicalPadInputSha256"])&&
                    JToken.DeepEquals(NormalOriginJson(JObject.Parse(JsonUtility.ToJson(owner.Layout))),NormalOriginJson(request["layout"]))&&
                    JToken.DeepEquals(NormalOriginJson(new JArray(clearance.Select(RectJson))),NormalOriginJson(request["approachClearance"]))&&
                    JToken.DeepEquals(NormalOriginJson(new JArray(budgets.Select(ApPreferenceBudgetKey))),NormalOriginJson(request["budgets"])),
                    "Exact recorded layout, real approach clearances and all three own budgets required");
                var exported=new JArray();
                foreach(var budget in budgets)
                {
                    var row=new JObject{["catalogId"]=owner.Definition.CatalogId,["size"]=budget.Size.ToString(),
                        ["group"]=mission["collector_source_group"].DeepClone(),["newLogicalCandidateMaterialized"]=true,
                        ["candidateCompoundWorldRects"]=compounds.DeepClone(),
                        ["allSizeFacilities"]=Ground30PreferredFacilities(budget,owner.Layout,grid)};
                    ProbePilotNormalFacilityRelocation(new JObject(),System.Array.Empty<JObject>(),owner.Layout,owner.Definition.RoleCatalog,
                        em,gridEntity,grid,surface,row,true,true,clearance,false,serviceAuthority:originalFc?null:owner.Definition);
                    var targets=((JArray)row["normalFacilityRelocationProposals"]).OfType<JObject>()
                        .Where(r=>r["normalPrefixFiniteOrigins"] is JObject).ToArray();
                    Require(targets.Length==1&&(bool)targets[0]["normalPrefixFiniteOrigins"]["enumerationComplete"],
                        "Requested normal-search point must be reached exactly once for each size");
                    exported.Add(row);
                }
                report["normalPrefixOriginExport"]=new JObject{["scope"]="NormalAllocatorPrefixFiniteOrigins_NotConfigurationFit",
                    ["request"]=request.DeepClone(),["rows"]=exported,["packingMeasured"]=false,
                    ["fullRouteAcceptance"]=false,["sourceFitCertified"]=false,["normalMissionReady"]=false};
                report["result"]="CollectedNormalPrefixFiniteOrigins_NoConfigurationFit";return report;
            }
            var maxBudget=budgets.OrderByDescending(b=>b.Structures.Length).ThenByDescending(b=>(int)b.Size).First();
            var inventory=new JObject{["newLogicalCandidateMaterialized"]=true,["candidateCompoundWorldRects"]=compounds.DeepClone(),
                ["allSizeFacilities"]=Ground30PreferredFacilities(maxBudget,owner.Layout,grid)};
            ProbePilotNormalFacilityRelocation(new JObject(),System.Array.Empty<JObject>(),owner.Layout,owner.Definition.RoleCatalog,
                em,gridEntity,grid,surface,inventory,true,true,clearance,true,serviceAuthority:(mpGroundBaPilotRequested||ApPairServiceAuthority(owner.Definition)||CompleteGroundBaServiceAuthority(owner.Definition))?owner.Definition:null);
            report["independentSourceQaCapacityHints"]=inventory;
            if(finiteFacilityOriginExportRequest!=null)
            {
                ValidateFiniteOriginRequest(finiteFacilityOriginExportRequest,mission);
                var exports=((JArray)inventory["normalFacilityRelocationProposals"]).OfType<JObject>()
                    .Where(row=>(int?)row["faction"]==2&&FiniteOriginStructures.Contains((string)row["structureId"])).ToArray();
                Require(exports.Length==3&&exports.Select(row=>(string)row["structureId"]).OrderBy(x=>x,StringComparer.Ordinal)
                    .SequenceEqual(FiniteOriginStructures.OrderBy(x=>x,StringComparer.Ordinal))&&
                    exports.All(row=>row["allSourceQaLegalOrigins"] is JArray&&
                        (long)row["independentlySourceQaLegalOriginCount"]==((JArray)row["allSourceQaLegalOrigins"]).Count),
                    "Complete three requested independent origin inventories required");
                report["finiteOriginExport"]=new JObject{["scope"]="IndependentFullFiniteOriginExport_NotConfigurationFit",
                    ["request"]=finiteFacilityOriginExportRequest.DeepClone(),["canonicalBudgets"]=new JArray(budgets.Select(ApPreferenceBudgetKey)),
                    ["originConfigurationSize"]=maxBudget.Size.ToString(),["canonicalSizeCount"]=budgets.Count,
                    ["origins"]=new JArray(exports.Select(row=>row.DeepClone())),["priorFacilityReservationsIncluded"]=false,
                    ["nativeRoutePreflightMayHaveRun"]=true,["forcePackingMeasured"]=false,["strictFacilityReplayMeasured"]=false,
                    ["sourceFitCertified"]=false,["normalMissionReady"]=false};
                report["result"]="CollectedFiniteOrigins_NoConfigurationFit";
                return report;
            }

            var hints=(JArray)inventory["normalFacilityRelocationProposals"];
            var assignments=logicalPadInput==null?BuildPreferenceAssignments(owner.Layout,maxBudget.Structures,hints,grid,compounds):
                new JArray(new JObject{["score"]=0,["pads"]=new JArray()});
            report["preferenceAssignments"]=assignments;
            string baseline=JsonUtility.ToJson(owner.Layout),definition=JsonUtility.ToJson(owner.Definition);
            var results=new JArray();report["nativePreferenceReplays"]=results;
            JObject accepted=null;var globalPasses=new JArray();
            for(int candidate=0;candidate<assignments.Count;candidate++)
            {
                JsonUtility.FromJsonOverwrite(baseline,owner.Layout);JsonUtility.FromJsonOverwrite(definition,owner.Definition);
                ApplyPreferenceAssignment(owner.Layout,(JObject)assignments[candidate]);
                var evidence=new JObject{["candidate"]=candidate,["preference"]=assignments[candidate].DeepClone(),
                    ["airReturnForceConsumers"]=new JArray(budgets.SelectMany(b=>b.Forces).Where(f=>f.FactionId==2&&SkirmishRoleIds.Category(f.RoleKind)==SkirmishPopulationCategory.Air)
                        .Select(f=>new JObject{["roleId"]=f.RoleId,["quantity"]=f.Quantity,["spawnAnchorId"]=f.SpawnAnchorId})),
                    ["airAndGroundSpawnRevalidationRequired"]=true,["runtimeStructureOrderChanged"]=false};results.Add(evidence);
                int captured=candidate;
                refinementPreferenceReplay=true;
                JObject measured;
                try
                {
                    measured=RefineLoadedCandidateCore(mission,owner,source,em,gridEntity,grid,surface,
                        prefix==null?null:prefix+".preference"+candidate,step=>
                        {
                            if(apGroundJointPreferencesRequested)checkpoint?.Invoke(CanonicalPreferenceCheckpoint(step,captured));
                            else
                            {
                                var row=(JObject)step.DeepClone();row["preferenceCandidate"]=captured;
                                row["candidateLocalPass"]=row["pass"];row["pass"]=captured*3+(int)row["pass"];
                                checkpoint?.Invoke(row);
                            }
                        },compounds,true,false,true,reviewedRouteDeltaProof:reviewedRouteProof);
                }
                finally{refinementPreferenceReplay=false;}
                evidence["measurement"]=measured;
                if(apGroundJointPreferencesRequested)
                    foreach(JObject pass in (JArray)measured["passes"])
                    {var global=(JObject)pass.DeepClone();global["pass"]=candidate*3+(int)pass["pass"];globalPasses.Add(global);}
                if(measured["strictThreeSizePlacementPass"]!=null&&measured["logicalFinalizationUnresolved"]==null)
                {accepted=(JObject)measured.DeepClone();report["strictAuthoringPreferenceCandidate"]=candidate;
                    if(apGroundJointPreferencesRequested){accepted["strictThreeSizePlacementPass"]=candidate*3+(int)measured["strictThreeSizePlacementPass"];accepted["passes"]=globalPasses.DeepClone();}
                    break;}
            }
            report["proposedLayoutJson"]=JsonUtility.ToJson(owner.Layout);
            report["normalMissionReady"]=false;report["sourceFitCertified"]=false;
            if(accepted!=null)
            {
                // Preserve the existing strict materializer's flat measurement
                // contract. Search history is additive diagnostic evidence only.
                var result=(JObject)accepted.DeepClone();
                result["preferenceSearchDiagnostics"]=report;
                return result;
            }
            return report;
        }

        static List<RectInt> PreferenceApproachClearance(SkirmishMapLayoutConfig layout,List<SkirmishStaticAuthoringBudget> budgets,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,JObject evidence)
        {
            var result=new HashSet<RectInt>();var rows=new JArray();evidence["queriesBySize"]=rows;
            foreach(var budget in budgets)
            {
                var routes=Ground30RouteQueries(budget,layout,em,gridEntity,grid,surface,new JArray());rows.Add(routes);
                foreach(JObject query in (JArray)routes["queries"])
                {
                    if((string)query["status"]!="NativeQueryFoundExactGoal"||(int?)query["failureCode"]!=0)continue;
                    var size=new int2((int)query["footprintX"],(int)query["footprintZ"]);
                    foreach(JArray point in (JArray)query["pathCells"])
                    {
                        var cell=new int2((int)point[0],(int)point[1]);bool entry=false;
                        foreach(byte faction in new byte[]{1,2})
                        {
                            Require(layout.TryGetPad(faction,SkirmishLegalPadKind.GroundStaging,out var staging),"Exact staged endpoint zone required");
                            var zone=PilotExactAxisAlignedPadCells(layout,staging,grid);int x=zone.x+zone.width/2,z=zone.y+zone.height/2;
                            entry|=zone.Contains(new Vector2Int(cell.x,cell.y))&&math.abs(cell.x-x)<=16&&math.abs(cell.y-z)<=16;
                        }
                        if(entry)continue;
                        var min=UnitFootprintUtility.GetMinCell(cell,size);result.Add(new RectInt(min.x,min.y,size.x,size.y));
                    }
                }
            }
            evidence["sourceFitCertified"]=false;evidence["footprintReservations"]=new JArray(result.Select(RectJson));
            return result.ToList();
        }

        static JArray BuildPreferenceAssignments(SkirmishMapLayoutConfig layout,SkirmishResolvedStructureEntry[] structures,JArray hints,GridConfig grid,JObject compounds)
        {
            var beam=new List<JObject>{new JObject{["score"]=0d,["pads"]=new JArray()}};
            var targets=new List<SkirmishLegalPadConfig>();
            if(apGroundJointPreferencesRequested)
            {
                foreach(byte side in new byte[]{1,2})foreach(var kind in new[]{SkirmishLegalPadKind.BaseBarracks,SkirmishLegalPadKind.Supply})
                {Require(layout.TryGetPad(side,kind,out var actual),"Exact actual faction preference pad required");targets.Add(actual);}
            }
            else foreach(var kind in new[]{SkirmishLegalPadKind.BaseBarracks,SkirmishLegalPadKind.AirReturn,SkirmishLegalPadKind.Supply})
            {Require(layout.TryGetPad(2,kind,out var actual),"Exact enemy facility preference pad required");targets.Add(actual);}
            foreach(var pad in targets)
            {
                var kind=pad.Kind;
                var seed=hints?.OfType<JObject>().Where(r=>(int?)r["faction"]==pad.FactionId&&(string)r["padId"]==pad.PadId&&
                    (long?)r["independentlySourceQaLegalOriginCount"]>0&&r["representativePlots"] is JArray plots&&plots.Count>0)
                    .OrderBy(r=>(long)r["independentlySourceQaLegalOriginCount"]).ThenBy(r=>(string)r["structureId"],StringComparer.Ordinal).FirstOrDefault();
                if(seed==null)return new JArray();
                var structure=structures.Single(s=>s.FactionId==pad.FactionId&&s.StructureId==(string)seed["structureId"]);
                float dx=0,dz=0;Game.Configs.SkirmishMapLayoutBuilder.ApplyFrameOffset(layout,structure.AcrossOffsetMetres,structure.ForwardOffsetMetres,ref dx,ref dz);
                var footprint=(JArray)seed["nativePlacementFootprint"];var next=new List<JObject>();
                foreach(var prior in beam)foreach(JObject plot in (JArray)seed["representativePlots"])
                {
                    var origin=(JArray)plot["preferredOriginCell"];
                    float x=grid.Origin.x+((int)origin[0]+(int)footprint[0]/2+.5f)*grid.CellSize-dx;
                    float z=grid.Origin.z+((int)origin[1]+(int)footprint[1]/2+.5f)*grid.CellSize-dz;
                    var bounds=(JArray)compounds[pad.FactionId.ToString(System.Globalization.CultureInfo.InvariantCulture)];
                    float halfWidth=pad.WidthMetres*.5f+(apGroundJointPreferencesRequested?Math.Abs(x-pad.CenterX):0);
                    float halfDepth=pad.DepthMetres*.5f+(apGroundJointPreferencesRequested?Math.Abs(z-pad.CenterZ):0);
                    if(x-halfWidth<(float)bounds[0]||z-halfDepth<(float)bounds[1]||
                        x+halfWidth>(float)bounds[2]||z+halfDepth>(float)bounds[3])continue;
                    var rect=new RectInt((int)plot["x"],(int)plot["z"],(int)plot["width"],(int)plot["depth"]);
                    if(((JArray)prior["pads"]).OfType<JObject>().Any(p=>PreferenceRect((JObject)p["hintRect"]).Overlaps(rect)))continue;
                    var candidate=(JObject)prior.DeepClone();
                    candidate["score"]=(double)prior["score"]+Math.Pow(x-pad.CenterX,2)+Math.Pow(z-pad.CenterZ,2);
                    ((JArray)candidate["pads"]).Add(new JObject{["padId"]=pad.PadId,["anchorId"]=pad.AnchorId,["kind"]=(int)kind,
                        ["faction"]=pad.FactionId,["centerX"]=x,["centerZ"]=z,["seedStructureId"]=structure.StructureId,["hintRect"]=plot.DeepClone()});next.Add(candidate);
                }
                beam=next.OrderBy(n=>(double)n["score"]).ThenBy(n=>n.ToString(Newtonsoft.Json.Formatting.None),StringComparer.Ordinal).Take(9).ToList();
            }
            return new JArray(beam);
        }

        static RectInt PreferenceRect(JObject r)=>new RectInt((int)r["x"],(int)r["z"],(int)r["width"],(int)r["depth"]);
        static void ApplyPreferenceAssignment(SkirmishMapLayoutConfig layout,JObject assignment)
        {
            var data=JObject.Parse(JsonUtility.ToJson(layout));var pads=(JArray)data["pads"];
            foreach(JObject preference in (JArray)assignment["pads"])
            {
                var pad=pads.OfType<JObject>().Single(p=>(string)p["PadId"]==(string)preference["padId"]&&(int)p["FactionId"]==((int?)preference["faction"]??2));
                Require((string)pad["AnchorId"]==(string)preference["anchorId"]&&(int)pad["Kind"]==(int)preference["kind"],"Typed preference ownership cannot change");
                if(apGroundJointPreferencesRequested)
                {
                    float oldX=(float)pad["CenterX"],oldZ=(float)pad["CenterZ"],x=(float)preference["centerX"],z=(float)preference["centerZ"];
                    pad["WidthMetres"]=(float)pad["WidthMetres"]+2*Math.Abs(x-oldX);
                    pad["DepthMetres"]=(float)pad["DepthMetres"]+2*Math.Abs(z-oldZ);
                }
                pad["CenterX"]=preference["centerX"];pad["CenterZ"]=preference["centerZ"];
            }
            // Existing explicit pad centres are normal preferences, independent
            // of their stable owner-anchor positions. No macro anchor moves here.
            JsonUtility.FromJsonOverwrite(data.ToString(Newtonsoft.Json.Formatting.None),layout);
        }

        static JObject ApPreferenceBudgetKey(SkirmishStaticAuthoringBudget budget)
        {
            return new JObject{["catalog"]=budget.CatalogId,["definition"]=budget.DefinitionId,["size"]=budget.Size.ToString(),
                ["structures"]=JArray.FromObject(budget.Structures.Select(s=>new{s.FactionId,s.StructureId,s.SpawnAnchorId,s.ObjectiveRoleId,s.DesignatedBase,s.AcrossOffsetMetres,s.ForwardOffsetMetres})),
                ["forces"]=JArray.FromObject(budget.Forces.Select(f=>new{f.FactionId,f.RoleId,f.RoleKind,f.RuntimePrefabKey,f.Quantity,f.SupplyCost,f.SpawnAnchorId,f.ObjectiveRoleId}))};
        }

        public static void RunApSeparatedCompoundExpansionControls()
        {
            int controls=0;
            JObject Rects(float ax,float az,float ar,float at,float bx,float bz,float br,float bt)
                =>new JObject{["1"]=new JArray(ax,az,ar,at),["2"]=new JArray(bx,bz,br,bt)};
            JObject Check(JObject original,float margin,Vector4 envelope,bool expected,string label)
            {
                string before=original.ToString(Newtonsoft.Json.Formatting.None);
                bool accepted=TryExpandApSeparatedCompounds(original,margin,envelope,out var result,out string error);
                Require(accepted==expected&&original.ToString(Newtonsoft.Json.Formatting.None)==before,
                    "Actual AP separator result and immutable input required: "+label+" "+error);
                if(!expected)Require(result==null&&!string.IsNullOrEmpty(error),"Explicit refusal required: "+label);
                else
                {
                    foreach(string side in new[]{"1","2"})
                    {
                        var old=(JArray)original[side];var row=(JArray)result[side];
                        Require((float)row[0]>=envelope.x&&(float)row[1]>=envelope.y&&
                            (float)row[2]<=envelope.z&&(float)row[3]<=envelope.w&&
                            (float)row[0]<=(float)old[0]&&(float)row[1]<=(float)old[1]&&
                            (float)row[2]>=(float)old[2]&&(float)row[3]>=(float)old[3],
                            "Exact original area and full envelope containment: "+label);
                    }
                    var a=(JArray)result["1"];var b=(JArray)result["2"];
                    Require(!((float)a[0]<(float)b[2]&&(float)b[0]<(float)a[2]&&
                        (float)a[1]<(float)b[3]&&(float)b[1]<(float)a[3]),"No expanded faction overlap: "+label);
                }
                controls++;return result;
            }
            var bounds=new Vector4(0,0,100,100);
            var x=Check(Rects(10,20,30,40,70,20,90,40),128,bounds,true,"X forward");
            Require((float)x["1"][2]==50&&(float)x["2"][0]==50,"Actual X gap midpoint");
            var rx=Check(Rects(70,20,90,40,10,20,30,40),128,bounds,true,"X reversed");
            Require((float)rx["2"][2]==50&&(float)rx["1"][0]==50,"Reversed ownership preserved");
            var z=Check(Rects(20,10,40,30,20,70,40,90),128,bounds,true,"Z forward");
            Require((float)z["1"][3]==50&&(float)z["2"][1]==50,"Actual Z gap midpoint");
            var rz=Check(Rects(20,70,40,90,20,10,40,30),128,bounds,true,"Z reversed");
            Require((float)rz["2"][3]==50&&(float)rz["1"][1]==50,"Reversed Z ownership preserved");
            var touch=Check(Rects(10,20,50,40,50,20,90,40),128,bounds,true,"Touching original boundaries");
            Require((float)touch["1"][2]==50&&(float)touch["2"][0]==50,"Touching is disjoint without area loss");
            Check(Rects(10,20,60,60,50,30,90,70),128,bounds,false,"Original positive area overlap");
            var malformed=Rects(10,20,30,40,70,20,90,40);malformed["2"]=new JArray(70,20,90);
            Check(malformed,128,bounds,false,"Malformed rectangle");
            Check(Rects(-1,20,30,40,70,20,90,40),128,bounds,false,"Original outside envelope");
            Check(Rects(10,20,30,40,70,20,90,40),float.NaN,bounds,false,"Nonfinite margin");
            var diagonal=Check(Rects(10,10,30,30,70,70,90,90),128,bounds,true,"Diagonal separation selects existing X axis");
            Require((float)diagonal["1"][2]==50&&(float)diagonal["2"][0]==50,"Deterministic existing diagonal separator");
            Check(Rects(10,20,30,40,70,20,90,40),128,new Vector4(0,0,float.PositiveInfinity,100),false,"Nonfinite envelope");
            Check(Rects(10,20,30,40,70,20,90,40),128,new Vector4(100,0,0,100),false,"Reversed envelope");

            var playable=new Vector4(-10,10,110,100);
            const float cellSize=2;var gridOrigin=new Vector2(3,7);
            var grid=new Vector4(gridOrigin.x,gridOrigin.y,gridOrigin.x+50*cellSize,gridOrigin.y+40*cellSize);
            var intersection=new Vector4(math.max(playable.x,grid.x),math.max(playable.y,grid.y),
                math.min(playable.z,grid.z),math.min(playable.w,grid.w));
            Require(TryBoundedLogicalFrameRect(new Vector4(10,20,90,80),intersection,128,out var clipped,out string refusal)&&
                clipped==intersection,"Actual frame helper clips to playable/grid intersection: "+refusal);
            Check(Rects(10,20,30,40,70,20,90,40),128,clipped,true,"Source/grid clipped frame");
            Require(controls==13,"Exact separator boundary control count");
            Debug.Log("[APSeparatedCompoundExpansionControls] result=Passed controls=13 immutableOriginals=1 overlapWaiver=0 nativeQueries=0 sourceFitCertified=0");
        }

        public static void RunApJointPreferenceBranchControls()
        {
            Require(!apGroundJointPreferencesRequested&&!refinementFacilityPreferencesRequested&&!refinementPreferenceReplay&&
                !refinementJointPadSearchRequested&&!refinementCompoundSearchRequested&&!refinementLogicalFrameSearchRequested&&
                !EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,
                "Idle exclusive AP branch controls required");
            RunApSeparatedCompoundExpansionControls();
            string inputPath=Environment.GetEnvironmentVariable(ApJointPadInputEnv),inputHash=Environment.GetEnvironmentVariable(ApJointPadInputShaEnv);
            Require(!string.IsNullOrEmpty(inputPath)&&System.IO.Path.IsPathRooted(inputPath)&&Hash(inputPath)==inputHash,"Exact frozen AP input required for controls");
            var doc=JObject.Parse(System.IO.File.ReadAllText(inputPath));
            var authority=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().Single(r=>(string)r["catalog_id"]=="S098");
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)doc["sourcePins"]["sourcePath"]);
            Require(source!=null&&!EditorUtility.IsDirty(source),"Actual clean pinned Campaign source required");
            Require(TryCreateGroundBa10Candidate(authority,source,out var owner,out string error),error);
            using(owner)
            {
                var grid=new GridConfig{Width=source.GridMetadata.Dimensions.x,Height=source.GridMetadata.Dimensions.y,
                    CellSize=source.GridMetadata.CellSize,Origin=(float3)source.GridMetadata.Origin};
                AddRefinementDefenseApproaches(owner.Definition,grid);
                string original=JsonUtility.ToJson(owner.Layout);
                var mission=All120MeasurementMission(authority,GroundBaTemplate(authority));
                var originalCompounds=CandidateCompoundRects((JObject)mission["layout_input"]["pilot_candidate"]);
                var overlapping=(JObject)originalCompounds.DeepClone();overlapping["2"]=overlapping["1"].DeepClone();
                Require(!TryNextApJointLogicalFrame(original,overlapping,owner.Layout,source,grid,out _,out string overlapRefusal)&&
                    !string.IsNullOrEmpty(overlapRefusal)&&JsonUtility.ToJson(owner.Layout)==original&&
                    JToken.DeepEquals(overlapping["1"],overlapping["2"]),"Actual AP layout must remain byte-identical after overlap refusal");
                Require(TryNextApJointLogicalFrame(original,originalCompounds,owner.Layout,source,grid,out var compounds,out error),error);
                string widened=JsonUtility.ToJson(owner.Layout),definitionBefore=JsonUtility.ToJson(owner.Definition);
                Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out error),error);
                var budgets=new List<SkirmishStaticAuthoringBudget>();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&budget.ExactCanonicalQuantities,
                        "Actual canonical AP control budget required: "+string.Join(";",reasons));budgets.Add(budget);
                }
                var budgetBefore=new JArray(budgets.Select(ApPreferenceBudgetKey));int controls=0;
                string logs=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"));
                Require((System.IO.File.GetAttributes(logs)&System.IO.FileAttributes.ReparsePoint)==0,"Ordinary owned diagnostic parent required");
                string name="ApPreferenceControls-"+Guid.NewGuid().ToString("N"),folder=System.IO.Path.Combine(logs,name),file=System.IO.Path.Combine(folder,"input.json");
                Require(!System.IO.Directory.Exists(folder)&&!System.IO.File.Exists(folder),"Fresh owned AP fixture path");
                System.IO.Directory.CreateDirectory(folder);
                try
                {
                    void Write(JObject value)
                    {
                        System.IO.File.WriteAllText(file,value.ToString(Newtonsoft.Json.Formatting.None),new System.Text.UTF8Encoding(false));
                        Environment.SetEnvironmentVariable(ApJointPadInputEnv,file);Environment.SetEnvironmentVariable(ApJointPadInputShaEnv,Hash(file));
                    }
                    JObject Row(JObject value)=>((JArray)value["padHints"]).OfType<JObject>().First(r=>(string)r["catalogId"]=="S098");
                    Write(doc);var before=JObject.Parse(widened);
                    ApplyApJointPadInput(mission,owner.Layout,source,grid,budgets);var after=JObject.Parse(JsonUtility.ToJson(owner.Layout));
                    Require(!JToken.DeepEquals(before["pads"],after["pads"])&&JToken.DeepEquals(before["anchors"],after["anchors"])&&
                        JToken.DeepEquals(before["routes"],after["routes"])&&JToken.DeepEquals(before["worldBinding"],after["worldBinding"]),
                        "Actual pad proposal must preserve all anchors/routes/source binding");controls++;
                    Require(JsonUtility.ToJson(owner.Definition)==definitionBefore&&JToken.DeepEquals(budgetBefore,new JArray(budgets.Select(ApPreferenceBudgetKey))),
                        "Exact original identity/three canonical budget vectors retained");controls++;
                    string prepared=JsonUtility.ToJson(owner.Layout);
                    void Refuses(Action<JObject> mutate,string message)
                    {
                        JsonUtility.FromJsonOverwrite(widened,owner.Layout);var invalid=(JObject)doc.DeepClone();mutate(invalid);Write(invalid);
                        bool refused=false;try{ApplyApJointPadInput(mission,owner.Layout,source,grid,budgets);}
                        catch(InvalidOperationException){refused=true;}
                        Require(refused&&JsonUtility.ToJson(owner.Layout)==widened,message+" must refuse without partial layout mutation");controls++;
                    }
                    Refuses(v=>v["sourcePins"]["sourceIdentityHash"]="foreign","Wrong source");
                    Refuses(v=>Row(v)["before"]["CenterX"]=(float)Row(v)["before"]["CenterX"]+1,"Wrong original field");
                    Refuses(v=>((JArray)v["padHints"]).Add(Row(v).DeepClone()),"Duplicate typed pad");
                    Refuses(v=>Row(v).Remove(),"Omitted typed pad");
                    Refuses(v=>Row(v)["proposed"]["CenterX"]=350,"Outside physical playable bounds");
                    Refuses(v=>Row(v)["proposed"]["CenterX"]=-10,"Outside native grid bounds");
                    Refuses(v=>{var air=((JArray)v["padHints"]).OfType<JObject>().First(r=>(string)r["catalogId"]=="S098"&&(int)r["kind"]==6);air["proposed"]["WidthMetres"]=1;},"Lost original AirReturn area");
                    Refuses(v=>Row(v)["padId"]="foreign.pad","Unknown pad identity");
                    Write(doc);JsonUtility.FromJsonOverwrite(prepared,owner.Layout);
                    foreach(var offset in new[]{new Vector2(-32,0),new Vector2(0,32),new Vector2(0,-32),new Vector2(32,0)})
                    {
                        Require(TryEncodeJointPadTranslation(prepared,offset,true,out string next,out error),error);
                        var changed=JObject.Parse(next);
                        Require(((JArray)changed["routes"]).OfType<JObject>().All(r=>(string)r["WaypointAnchorIds"][0]=="anchor.skirmish.db.staging_player"&&
                            (string)r["WaypointAnchorIds"][((JArray)r["WaypointAnchorIds"]).Count-1]=="anchor.skirmish.db.staging_enemy"),
                            "All three routes must retain the same typed endpoints");
                        JsonUtility.FromJsonOverwrite(next,owner.Layout);
                        Require(TryPreflightAuthoredRouteEndpoints(owner.Layout,grid,out var policy)&&(bool)policy["accepted"],"Actual endpoint ownership remains coherent; not native path proof");
                        foreach(var pad in before["pads"].OfType<JObject>().Where(p=>(int)p["Kind"]==6||(int)p["Kind"]==8))
                            Require(JToken.DeepEquals(after["pads"].OfType<JObject>().Single(p=>(string)p["PadId"]==(string)pad["PadId"]),
                                changed["pads"].OfType<JObject>().Single(p=>(string)p["PadId"]==(string)pad["PadId"])),"AirReturn/service areas untouched by staging transform");
                        controls++;
                    }
                    JsonUtility.FromJsonOverwrite(prepared,owner.Layout);
                    var structures=budgets.OrderByDescending(b=>b.Structures.Length).First().Structures;
                    var hints=new JArray();
                    foreach(byte side in new byte[]{1,2})foreach(var kind in new[]{SkirmishLegalPadKind.BaseBarracks,SkirmishLegalPadKind.Supply})
                    {
                        Require(owner.Layout.TryGetPad(side,kind,out var pad),"Actual control preference pad");
                        var structure=structures.First(s=>s.FactionId==side&&Game.Configs.SkirmishMapLayoutBuilder.TryPadForStructure(owner.Layout,s,out var actual)&&actual.PadId==pad.PadId);
                        float x=pad.CenterX,z=pad.CenterZ;Game.Configs.SkirmishMapLayoutBuilder.ApplyFrameOffset(owner.Layout,structure.AcrossOffsetMetres,structure.ForwardOffsetMetres,ref x,ref z);
                        int cx=(int)Math.Floor((x-grid.Origin.x)/grid.CellSize),cz=(int)Math.Floor((z-grid.Origin.z)/grid.CellSize);
                        hints.Add(new JObject{["faction"]=side,["padId"]=pad.PadId,["structureId"]=structure.StructureId,
                            ["independentlySourceQaLegalOriginCount"]=1,["nativePlacementFootprint"]=new JArray(1,1),
                            ["representativePlots"]=new JArray(new JObject{["preferredOriginCell"]=new JArray(cx,cz),["x"]=cx,["z"]=cz,["width"]=1,["depth"]=1})});
                    }
                    WithApJointPreferenceMode(()=>
                    {
                        string same=JsonUtility.ToJson(owner.Layout);var assignments=BuildPreferenceAssignments(owner.Layout,structures,hints,grid,compounds);
                        Require(assignments.Count==1&&same==JsonUtility.ToJson(owner.Layout),"Synthetic branch beam must be finite and leave source layout unchanged");
                        var pads=(JArray)assignments[0]["pads"];Require(pads.Count==4&&pads.OfType<JObject>().Select(p=>(int)p["faction"]).OrderBy(v=>v).SequenceEqual(new[]{1,1,2,2}),"AP beam covers both actual factions");
                        ApplyPreferenceAssignment(owner.Layout,(JObject)assignments[0]);
                        var final=JObject.Parse(JsonUtility.ToJson(owner.Layout));
                        foreach(var air in after["pads"].OfType<JObject>().Where(p=>(int)p["Kind"]==6))
                            Require(JToken.DeepEquals(air,final["pads"].OfType<JObject>().Single(p=>(string)p["PadId"]==(string)air["PadId"])),"AP beam cannot alter reviewed AirReturn area");
                    });controls++;
                    var step=new JObject{["catalogId"]="S098",["pass"]=2,["discoveryOnly"]=false,["size"]="War",["facts"]=new JObject{["control"]=1},["extra"]="not durable"};
                    var checkpoint=CanonicalPreferenceCheckpoint(step,1);
                    Require(checkpoint.Properties().Count()==5&&(int)checkpoint["pass"]==5&&!ReferenceEquals(checkpoint["facts"],step["facts"]),"Exact five-field deep-cloned global checkpoint");controls++;
                    foreach(var pair in new[]{new[]{9,2},new[]{1,3}})
                    {
                        var invalid=(JObject)step.DeepClone();invalid["pass"]=pair[1];bool refused=false;
                        try{CanonicalPreferenceCheckpoint(invalid,pair[0]);}catch(InvalidOperationException){refused=true;}
                        Require(refused,"Out-of-window global checkpoint refused");controls++;
                    }
                    bool threw=false;try{WithApJointPreferenceMode(()=>throw new InvalidOperationException("owned mode sentinel"));}
                    catch(InvalidOperationException e){threw=e.Message=="owned mode sentinel";}
                    Require(threw&&!apGroundJointPreferencesRequested&&!refinementFacilityPreferencesRequested&&!refinementPreferenceReplay,"Mode reset on actual callback throw");controls++;
                    apGroundJointPreferencesRequested=true;bool busy=false;
                    try{WithApJointPreferenceMode(()=>{});}catch(InvalidOperationException){busy=true;}
                    finally{apGroundJointPreferencesRequested=false;}
                    Require(busy&&!refinementFacilityPreferencesRequested,"Nested AP mode refuses without replacing ownership");controls++;
                    Require(controls==20,"Exact twenty AP branch controls");
                }
                finally
                {
                    Environment.SetEnvironmentVariable(ApJointPadInputEnv,inputPath);Environment.SetEnvironmentVariable(ApJointPadInputShaEnv,inputHash);
                    apGroundJointPreferencesRequested=refinementFacilityPreferencesRequested=refinementPreferenceReplay=false;
                    Require(System.IO.Path.GetFileName(folder)==name&&System.IO.Path.GetDirectoryName(folder)==logs&&
                        (System.IO.File.GetAttributes(folder)&System.IO.FileAttributes.ReparsePoint)==0,"Exact owned ordinary AP fixture cleanup");
                    if(System.IO.File.Exists(file))
                    {
                        Require((System.IO.File.GetAttributes(file)&(System.IO.FileAttributes.ReparsePoint|System.IO.FileAttributes.Directory))==0,"Ordinary owned fixture file");
                        System.IO.File.Delete(file);
                    }
                    System.IO.Directory.Delete(folder,false);
                    Require(Hash(inputPath)==inputHash,"Original frozen input unchanged");
                }
            }
            Debug.Log("[APJointPreferenceBranchControls] result=Passed controls=20 actualCanonicalSizes=3 cardinalStagingSeeds=4 syntheticBeamOnly=1 nativeQueries=0 sourceFitCertified=0 missionReady=0");
        }

        static JObject ReadReviewedFacilityPadInput(JObject mission)
        {
            string path=Environment.GetEnvironmentVariable("WARLINE_ALL120_LOGICAL_PAD_INPUT"),digest=Environment.GetEnvironmentVariable("WARLINE_ALL120_LOGICAL_PAD_INPUT_SHA256");
            if(string.IsNullOrEmpty(path)&&string.IsNullOrEmpty(digest))return null;
            Require(!apGroundJointPreferencesRequested&&!string.IsNullOrEmpty(path)&&!string.IsNullOrEmpty(digest),"Exclusive exact generic pad input required");
            string allowed=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+System.IO.Path.DirectorySeparatorChar;
            Require(System.IO.Path.IsPathRooted(path)&&System.IO.Path.GetFullPath(path).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&Hash(path)==digest,"Exact owned Logs input required");
            var input=JObject.Parse(System.IO.File.ReadAllText(path));
            Require((int?)input["schema"]==1&&(string)input["scope"]=="ReviewedLogicalPadProposal_NotPlacement"&&
                (string)input["catalogId"]==(string)mission["catalog_id"]&&(string)input["definitionId"]==(string)mission["definition_id"]&&
                JToken.DeepEquals(input["sourcePins"],mission["source_candidate"])&&JToken.DeepEquals(input["canonical_size_rows"],mission["canonical_size_rows"])&&
                (int?)input["maximumPreferenceAssignments"]==9&&(bool?)input["sourceFitCertified"]==false&&(bool?)input["missionAccepted"]==false,"Exact subject/source/budgets required");
            Require((int?)input["fixedLogicalFrameAttempt"]>=1&&(int?)input["fixedLogicalFrameAttempt"]<=3,"Existing finite frame attempt required");
            Require(Hash(path)==digest,"Reviewed logical-pad input changed during read");
            input["observedInputPath"]=System.IO.Path.GetFullPath(path);input["observedInputSha256"]=digest;return input;
        }
        static bool TryEncodeReviewedFacilityPads(string original,JObject input,GridConfig grid,JObject compounds,out string next,out string error)
        {
            next=original;error=null;
            try
            {
                var data=JObject.Parse(original);var pads=(JArray)data["pads"];var changes=input["padOverrides"]as JArray;
                Require(changes!=null&&changes.Count>0&&changes.Count<=8,"Bounded pad input required");var seen=new HashSet<string>(StringComparer.Ordinal);
                foreach(JObject change in changes)
                {
                    string id=(string)change["padId"];Require(!string.IsNullOrEmpty(id)&&seen.Add(id),"Unique pad input required");
                    var matches=pads.OfType<JObject>().Where(p=>(string)p["PadId"]==id).ToArray();Require(matches.Length==1,"One actual typed pad required");var pad=matches[0];
                    Require((int?)change["faction"]==(int?)pad["FactionId"]&&(int?)change["kind"]==(int?)pad["Kind"]&&(string)change["anchorId"]==(string)pad["AnchorId"]&&(string)change["roleId"]==(string)pad["RoleId"],"Stable typed ownership required");
                    var after=(JObject)change["after"];string[] fields={"CenterX","CenterZ","WidthMetres","DepthMetres"};
                    Require(after.Properties().Select(p=>p.Name).OrderBy(p=>p).SequenceEqual(fields.OrderBy(p=>p)),"Only logical dimensions/centre may change");
                    foreach(string field in fields)Require(JToken.DeepEquals(pad[field],change["before"][field]),"Exact pad precondition required");
                    double x=(double)after["CenterX"],z=(double)after["CenterZ"],w=(double)after["WidthMetres"],h=(double)after["DepthMetres"];
                    Require(new[]{x,z,w,h}.All(v=>!double.IsNaN(v)&&!double.IsInfinity(v))&&w>0&&h>0&&grid.CellSize>0,"Finite positive pad/grid required");
                    double x0=x-w/2,z0=z-h/2,x1=x+w/2,z1=z+h/2;
                    foreach(double cell in new[]{(x0-grid.Origin.x)/grid.CellSize,(x1-grid.Origin.x)/grid.CellSize,(z0-grid.Origin.z)/grid.CellSize,(z1-grid.Origin.z)/grid.CellSize})
                        Require(Math.Abs(cell-Math.Round(cell))<.00001,"Raw bounds must align to native grid");
                    Require(x0>=grid.Origin.x&&z0>=grid.Origin.z&&x1<=grid.Origin.x+grid.Width*grid.CellSize&&z1<=grid.Origin.z+grid.Height*grid.CellSize,"Raw bounds inside source grid required");
                    var rect=(JArray)compounds[((int)pad["FactionId"]).ToString(System.Globalization.CultureInfo.InvariantCulture)];
                    Require(x0>=(double)rect[0]&&z0>=(double)rect[1]&&x1<=(double)rect[2]&&z1<=(double)rect[3],"Raw bounds inside legal compound required");
                    Require(x0>=(double)data["originX"]&&z0>=(double)data["originZ"]&&x1<=(double)data["originX"]+(double)data["worldWidthMetres"]&&z1<=(double)data["originZ"]+(double)data["worldDepthMetres"],"Raw bounds inside logical frame required");
                    if((int)pad["Kind"]==(int)SkirmishLegalPadKind.AirReturn)
                        Require(x0<=(double)pad["CenterX"]-(double)pad["WidthMetres"]/2&&z0<=(double)pad["CenterZ"]-(double)pad["DepthMetres"]/2&&x1>=(double)pad["CenterX"]+(double)pad["WidthMetres"]/2&&z1>=(double)pad["CenterZ"]+(double)pad["DepthMetres"]/2,"Original air return retained");
                    foreach(string field in fields)pad[field]=after[field].DeepClone();
                }
                next=data.ToString(Newtonsoft.Json.Formatting.None);return true;
            }
            catch(Exception failure){next=original;error=failure.Message;return false;}
        }

        public static void RunReviewedFacilityPadInputControls()
        {
            var pad=new JObject{["PadId"]="pad.fixture",["FactionId"]=2,["Kind"]=(int)SkirmishLegalPadKind.AirReturn,
                ["AnchorId"]="anchor.fixture",["RoleId"]="air.fixture",["CenterX"]=20.2,["CenterZ"]=20.2,["WidthMetres"]=16,["DepthMetres"]=16};
            var original=new JObject{["originX"]=0,["originZ"]=0,["worldWidthMetres"]=100,["worldDepthMetres"]=100,
                ["anchors"]=new JArray("stable-anchor"),["routes"]=new JArray("stable-route"),["pads"]=new JArray(pad)};
            var fields=new[]{"CenterX","CenterZ","WidthMetres","DepthMetres"};
            var before=new JObject();foreach(var field in fields)before[field]=pad[field].DeepClone();
            var change=new JObject{["padId"]="pad.fixture",["faction"]=2,["kind"]=(int)SkirmishLegalPadKind.AirReturn,
                ["anchorId"]="anchor.fixture",["roleId"]="air.fixture",["before"]=before,
                ["after"]=new JObject{["CenterX"]=20.5,["CenterZ"]=20.5,["WidthMetres"]=17,["DepthMetres"]=17}};
            var input=new JObject{["padOverrides"]=new JArray(change)};
            var grid=new GridConfig{Width=100,Height=100,CellSize=1,Origin=float3.zero};
            var compounds=new JObject{["2"]=new JArray(0,0,100,100)};string text=original.ToString();int count=0;
            Require(TryEncodeReviewedFacilityPads(text,input,grid,compounds,out string positive,out _)&&
                JToken.DeepEquals(JObject.Parse(positive)["anchors"],original["anchors"])&&JToken.DeepEquals(JObject.Parse(positive)["routes"],original["routes"]),
                "Actual encoder must preserve all anchors/routes");count++;
            void Refuse(Action<JObject> edit,string reason)
            {
                var bad=(JObject)input.DeepClone();edit(bad);
                Require(!TryEncodeReviewedFacilityPads(text,bad,grid,compounds,out string rejected,out _)&&rejected==text,reason);count++;
            }
            Refuse(v=>((JArray)v["padOverrides"]).Add(v["padOverrides"][0].DeepClone()),"Duplicate pad refuses unchanged");
            Refuse(v=>v["padOverrides"][0]["anchorId"]="foreign","Foreign owner refuses unchanged");
            Refuse(v=>v["padOverrides"][0]["before"]["CenterX"]=21,"Stale original refuses unchanged");
            Refuse(v=>v["padOverrides"][0]["after"]["CenterX"]=20.6,"Fractional raw edge refuses");
            Refuse(v=>v["padOverrides"][0]["after"]["CenterX"]=100.5,"Outside source refuses");
            Refuse(v=>v["padOverrides"][0]["after"]["WidthMetres"]=15,"Air area contraction refuses");
            Refuse(v=>v["padOverrides"][0]["after"]["RoleId"]="foreign","Extra mutation field refuses");
            var narrow=new JObject{["2"]=new JArray(15,15,25,25)};
            Require(!TryEncodeReviewedFacilityPads(text,input,grid,narrow,out string rejectedCompound,out _)&&rejectedCompound==text,"Outside compound refuses");count++;
            var small=(JObject)original.DeepClone();small["worldWidthMetres"]=25;
            Require(!TryEncodeReviewedFacilityPads(small.ToString(),input,grid,compounds,out string rejectedFrame,out _)&&rejectedFrame==small.ToString(),"Outside frame refuses");count++;
            Debug.Log("[ReviewedFacilityPadInputControls] result=Passed controls="+count+" nativeQueries=0 sourceFitCertified=0");
        }

        public static void RunFacilityPreferenceControls()
        {
            Require(!new RectInt(0,0,19,16).Overlaps(new RectInt(20,0,16,15))&&
                new RectInt(0,0,19,16).Overlaps(new RectInt(10,0,16,15)),"Hint assignments must preserve effective footprint nonoverlap");
            Require(PreferenceRect(new JObject{["x"]=1497,["z"]=658,["width"]=25,["depth"]=15}).Overlaps(new RectInt(1500,658,19,16)),
                "Actual competing refinery/depot observations cannot become a combined fit");
            var layout=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            try
            {
                var kinds=new[]{SkirmishLegalPadKind.BaseBarracks,SkirmishLegalPadKind.AirReturn,SkirmishLegalPadKind.Supply};
                var ids=new[]{SkirmishStructureIds.Barracks,SkirmishStructureIds.Helipad,SkirmishStructureIds.Refinery};
                var pads=new JArray();var hints=new JArray();var structures=new SkirmishResolvedStructureEntry[3];
                for(int slot=0;slot<3;slot++)
                {
                    pads.Add(new JObject{["PadId"]="pad"+slot,["AnchorId"]="anchor"+slot,["Kind"]=(int)kinds[slot],["FactionId"]=2,
                        ["CenterX"]=100+slot*400,["CenterZ"]=100,["WidthMetres"]=34,["DepthMetres"]=54});
                    structures[slot]=new SkirmishResolvedStructureEntry{FactionId=2,StructureId=ids[slot],SpawnAnchorId="anchor"+slot,
                        AcrossOffsetMetres=slot==2?12:0,ForwardOffsetMetres=slot==2?-8:0};
                    var plots=new JArray();
                    for(int i=0;i<12;i++)plots.Add(new JObject{["x"]=30+i*100,["z"]=40+slot*70,["width"]=25,["depth"]=15,
                        ["preferredOriginCell"]=new JArray(30+i*100,40+slot*70)});
                    hints.Add(new JObject{["faction"]=2,["structureId"]=ids[slot],["padId"]="pad"+slot,
                        ["independentlySourceQaLegalOriginCount"]=12,["nativePlacementFootprint"]=new JArray(25,15),["representativePlots"]=plots});
                }
                var data=new JObject{["originX"]=0,["originZ"]=0,["worldWidthMetres"]=2000,["worldDepthMetres"]=400,["pads"]=pads,
                    ["worldBinding"]=new JObject{["SourceHash"]="pure-preference-control",["ForwardX"]=1,["ForwardZ"]=0,["AcrossX"]=0,["AcrossZ"]=1,
                        ["ForwardMetres"]=2000,["AcrossMetres"]=400,["OriginX"]=0,["OriginZ"]=200,
                        ["PlayerAnchorId"]="control.player",["EnemyAnchorId"]="control.enemy"}};
                JsonUtility.FromJsonOverwrite(data.ToString(),layout);string before=JsonUtility.ToJson(layout);
                var compound=new JObject{["2"]=new JArray(0,0,2000,400)};var grid=new GridConfig{Width=2000,Height=400,CellSize=1};
                var beam=BuildPreferenceAssignments(layout,structures,hints,grid,compound);
                Require(beam.Count==9&&before==JsonUtility.ToJson(layout),"Finite best9 authoring preferences do not mutate original layout");
                ApplyPreferenceAssignment(layout,(JObject)beam[0]);
                foreach(JObject preferred in (JArray)beam[0]["pads"])
                {
                    var structure=structures.Single(s=>s.StructureId==(string)preferred["seedStructureId"]);
                    Require(Game.Configs.SkirmishMapLayoutBuilder.TryPadForStructure(layout,structure,out var pad)&&pad.AnchorId==structure.SpawnAnchorId,
                        "Normal typed structure owner preserved");
                    Require(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z),"Normal preference projection required");
                    Game.Configs.SkirmishMapLayoutBuilder.ApplyFrameOffset(layout,structure.AcrossOffsetMetres,structure.ForwardOffsetMetres,ref x,ref z);
                    var center=GridUtils.WorldToCell(grid,new float3(x,0,z));var expected=(JArray)preferred["hintRect"]["preferredOriginCell"];
                    Require(center.x-25/2==(int)expected[0]&&center.y-15/2==(int)expected[1],"Actual binder offset inversion reproduces ordinary preferred origin");
                }
                Require(BuildPreferenceAssignments(layout,structures,new JArray(),grid,compound).Count==0,"Missing native hints cannot yield a preference proposal");
            }
            finally{UnityEngine.Object.DestroyImmediate(layout);}
            RunBoundedRouteRepairControls();
            Debug.Log("[BoundedFacilityPreferenceControls] result=Passed controls=5 preferenceLimit=9 hintOnly=1 nativeSourceFitPending=1");
        }
    }
}
#endif
