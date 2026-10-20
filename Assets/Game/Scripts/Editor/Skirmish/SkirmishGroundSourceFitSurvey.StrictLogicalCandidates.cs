#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        const string StrictJointDirectory="Logs/SkirmishProduction/Offline/S049StableMainAnchorPrecisionRepair_20261017/inputs/";
        const string StrictJointCandidateSha="42b68874892ca706d05d71d859914c4c8f1ae3405d7f4b096f0066d8329bdd57";
        const string StrictJointBaselineSha="0446382e9ab133ca943bd2c29eba2d4a32afeaa2d0f85da2d104e0b306962df8";
        const string StrictJointReport="Logs/SkirmishProduction/Offline/S049LogicalFrameSettledBoundProposal_20261017/Native-01/refinement.json";
        const string StrictJointReportSha="7336d2bfc6a381b3b0f760c47acc7eb6d1a3a900c4059aaccde631e009e0b9f9";
        static bool strictLogicalCandidatesRequested;
        static JObject strictLogicalCandidates;
        const string ReviewedJointPathEnv="WARLINE_REVIEWED_LOGICAL_CANDIDATES_INPUT";
        const string ReviewedJointShaEnv="WARLINE_REVIEWED_LOGICAL_CANDIDATES_SHA256";
        static JObject strictLogicalInputReceipt;
        static void RequireReviewedJointPair(string path,string digest)
        {
            Require(string.IsNullOrEmpty(path)==string.IsNullOrEmpty(digest),"Complete reviewed input path/hash pair required");
            if(!string.IsNullOrEmpty(path))Require(digest.Length==64&&digest.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f'),"Exact lowercase SHA256 required");
        }
        static string ReviewedJointOrdinaryPath(string root,string path)
        {
            Require(!string.IsNullOrWhiteSpace(path)&&Path.IsPathRooted(path),"Absolute reviewed input required");
            root=Path.GetFullPath(root);path=Path.GetFullPath(path);
            var comparison=Application.platform==RuntimePlatform.WindowsEditor?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal;
            Require(path.StartsWith(Path.Combine(root,"Logs")+Path.DirectorySeparatorChar,comparison),"Reviewed input must remain under owned Logs");
            Require(File.Exists(path),"Actual reviewed input file required");
            for(string cursor=path;!string.IsNullOrEmpty(cursor);cursor=Path.GetDirectoryName(cursor))
                BuildScript.RequirePreparedNonReparse(File.GetAttributes(cursor));
            return path;
        }
        static void ValidateReviewedJointEnvelope(JObject input,JObject reference,JObject baseline,JObject authority)
        {
            Require(input.Properties().Select(x=>x.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(new[]{"schema","catalogId","definitionId","canonical_size_rows","inputReportSha256","sourceAuthority","candidates","nativePending","logicalDifferencePolicy"}.OrderBy(x=>x,StringComparer.Ordinal)),"Exact reviewed input envelope shape required");
            Require(input["schema"]?.Type==JTokenType.Integer&&(int?)input["schema"]==1&&(string)input["catalogId"]==(string)authority["catalog_id"]&&
                (string)input["definitionId"]==(string)authority["definition_id"],"Actual catalog/definition authority required");
            Require(JToken.DeepEquals(input["canonical_size_rows"],authority["canonical_size_rows"])&&
                ((JArray)authority["canonical_size_rows"]).Count==3,"Exact own three canonical budgets required");
            Require(JToken.DeepEquals(input["sourceAuthority"],reference["sourceAuthority"])&&
                (string)input["inputReportSha256"]==StrictJointReportSha,"Unchanged physical source/grid and historical report required");
            Require(input["nativePending"]?.Type==JTokenType.Boolean&&(bool?)input["nativePending"]==true&&(string)input["logicalDifferencePolicy"]==JointStagingPolicy&&
                input["candidates"] is JArray values&&values.Count==3&&values.All(x=>x is JObject),"Exact three reviewed unqualified candidates required");
            int index=0;
            foreach(JObject item in (JArray)input["candidates"])
            {
                Require(item["candidate"]?.Type==JTokenType.Integer&&(int?)item["candidate"]==index++&&item["layout"] is JObject,"Exact ordered candidate identities required");
                ValidateReviewedJointCandidate(baseline,(JObject)item["layout"],JointStagingPolicy);
            }
        }
        static JObject ReadReviewedJointOverride(string root,string path,string digest,JObject reference,JObject baseline,JObject authority)
        {
            RequireReviewedJointPair(path,digest);path=ReviewedJointOrdinaryPath(root,path);
            Require(Hash(path)==digest,"Reviewed logical input hash mismatch");
            var input=JObject.Parse(File.ReadAllText(path));
            ValidateReviewedJointEnvelope(input,reference,baseline,authority);
            Require(Hash(path)==digest,"Reviewed logical input changed during read");
            return input;
        }
        public static void RunReviewedLogicalInputAuthorityControls()
        {
            var reference=JObject.Parse(File.ReadAllText(StrictJointDirectory+"candidates.json"));
            var baseline=JObject.Parse(File.ReadAllText(StrictJointDirectory+"baseline-layout.json"));
            var authority=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().Single(x=>(string)x["catalog_id"]=="S049");
            var valid=(JObject)reference.DeepClone();valid["schema"]=1;valid["catalogId"]=authority["catalog_id"].DeepClone();
            valid["definitionId"]=authority["definition_id"].DeepClone();valid["canonical_size_rows"]=authority["canonical_size_rows"].DeepClone();
            ValidateReviewedJointEnvelope(valid,reference,baseline,authority);int controls=1;
            void Refuse(Action action)
            {
                bool refused=false;try{action();}catch(InvalidOperationException){refused=true;}
                Require(refused,"Malformed reviewed input must refuse");controls++;
            }
            void Changed(Action<JObject> change)
            {var v=(JObject)valid.DeepClone();change(v);Refuse(()=>ValidateReviewedJointEnvelope(v,reference,baseline,authority));}
            foreach(JToken token in new JToken[]{new JValue("1"),new JValue(1.0),new JValue(true)})
                Changed(v=>v["schema"]=token.DeepClone());
            foreach(JToken token in new JToken[]{new JValue("0"),new JValue(0.0),new JValue(false)})
                Changed(v=>v["candidates"][0]["candidate"]=token.DeepClone());
            foreach(JToken token in new JToken[]{new JValue("true"),new JValue(1)})
                Changed(v=>v["nativePending"]=token.DeepClone());
            Changed(v=>v["unknownOverride"]=true);
            Changed(v=>v["catalogId"]="S050");
            Changed(v=>v["definitionId"]="foreign");
            Changed(v=>v["canonical_size_rows"][0]["materials_each"]=((int)v["canonical_size_rows"][0]["materials_each"]+1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            Changed(v=>v["sourceAuthority"]["sourcePins"]["sourceIdentityHash"]="foreign");
            Changed(v=>((JArray)v["candidates"]).RemoveAt(0));
            Changed(v=>v["candidates"][0]["layout"]["worldBinding"]["SourceHash"]="foreign");
            Refuse(()=>RequireReviewedJointPair("path",null));
            Refuse(()=>RequireReviewedJointPair(null,new string('0',64)));
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            Refuse(()=>ReadReviewedJointOverride(root,Path.GetFullPath(StrictJointDirectory+"candidates.json"),new string('0',64),reference,baseline,authority));
            Refuse(()=>ReviewedJointOrdinaryPath(root,Path.Combine(root,"Assets","foreign.json")));
            Refuse(()=>ReviewedJointOrdinaryPath(root,Path.Combine(root,"Logs","..","Assets","foreign.json")));
            Require(controls==21,"Exact twenty-one input-authority controls");
            Debug.Log("[ReviewedLogicalInputAuthorityControls] result=Passed controls=21 nativeQueries=0 sourceFitCertified=0");
        }
        public static void RunReviewedLogicalJointStaticPilot()
        {
            Require(!strictLogicalCandidatesRequested&&!mpGroundBaPilotRequested&&!refinementJointPadSearchRequested&&
                !refinementCompoundSearchRequested&&!refinementLogicalFrameSearchRequested&&!refinementFacilityPreferencesRequested&&
                !refinementPreferenceReplay&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,
                "Idle exclusive disposable logical candidate collection required");
            Require(Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG")=="S049","Reviewed unprotected catalog required");
            Require(Hash(StrictJointDirectory+"candidates.json")==StrictJointCandidateSha&&
                Hash(StrictJointDirectory+"baseline-layout.json")==StrictJointBaselineSha&&Hash(StrictJointReport)==StrictJointReportSha,
                "Exact immutable candidate/baseline/failed native report required");
            string selectedPath=ReviewedJointOrdinaryPath(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),Path.GetFullPath(StrictJointDirectory+"candidates.json"));
            string selectedSha=StrictJointCandidateSha;
            string overridePath=Environment.GetEnvironmentVariable(ReviewedJointPathEnv),overrideSha=Environment.GetEnvironmentVariable(ReviewedJointShaEnv);
            RequireReviewedJointPair(overridePath,overrideSha);
            var reference=JObject.Parse(File.ReadAllText(selectedPath));
            var input=reference;
            if(!string.IsNullOrEmpty(overridePath))
            {
                var own=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().Single(x=>(string)x["catalog_id"]=="S049");
                input=ReadReviewedJointOverride(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),overridePath,overrideSha,reference,
                    JObject.Parse(File.ReadAllText(StrictJointDirectory+"baseline-layout.json")),own);
                selectedPath=Path.GetFullPath(overridePath);selectedSha=overrideSha;
            }
            Require((string)input["inputReportSha256"]==StrictJointReportSha&&(bool?)input["nativePending"]==true&&
                input["candidates"] is JArray candidates&&candidates.Count==3,"Exactly three unqualified candidates required");
            var baseline=JObject.Parse(File.ReadAllText(StrictJointDirectory+"baseline-layout.json"));
            Require((string)input["logicalDifferencePolicy"]==JointStagingPolicy,"Explicit reviewed joint-staging policy required");
            foreach(JObject candidate in (JArray)input["candidates"])ValidateReviewedJointCandidate(baseline,(JObject)candidate["layout"],JointStagingPolicy);
            RunReviewedLogicalJointControls();RunReviewedJointStagingControls();RunAuthoredRouteEndpointPreflightControls();RunMpGroundBaEndpointAdmissionControls();
            RunMpGroundBaZeroDistanceQueryControls();RunMpGroundBaServiceRegionControls();
            RunReviewedLogicalInputAuthorityControls();
            strictLogicalInputReceipt=new JObject{["path"]=selectedPath,["sha256"]=selectedSha,["overrideApplied"]=!string.IsNullOrEmpty(overridePath),["sourceFitCertified"]=false};
            strictLogicalCandidates=input;strictLogicalCandidatesRequested=true;
            mpGroundBaPilotRequested=true;s049BoundedCompoundRouteRequested=true;
            try{RunAll120SimultaneousLogicalRefinement();}
            finally{strictLogicalCandidatesRequested=false;strictLogicalCandidates=null;strictLogicalInputReceipt=null;mpGroundBaPilotRequested=false;s049BoundedCompoundRouteRequested=false;}
            Require(Hash(StrictJointDirectory+"candidates.json")==StrictJointCandidateSha&&Hash(StrictJointDirectory+"baseline-layout.json")==StrictJointBaselineSha&&Hash(StrictJointReport)==StrictJointReportSha,
                "Input evidence changed during native collection");
            Require(Hash(ReviewedJointOrdinaryPath(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),selectedPath))==selectedSha,"Reviewed input changed during collection");
            ValidateMpGroundBaStaticPilotResult("S049");
            Debug.Log("[ReviewedLogicalJointStaticPilot] result=Passed catalog=S049 sizes=3 candidateLimit=3 overlapWaiver=0 sourceFitCertified=0 normalMissionReady=0");
        }
        public static void RunReviewedLogicalJointControls()
        {
            Require(Hash(StrictJointDirectory+"candidates.json")==StrictJointCandidateSha&&Hash(StrictJointDirectory+"baseline-layout.json")==StrictJointBaselineSha,"Pinned control inputs required");
            var baseline=JObject.Parse(File.ReadAllText(StrictJointDirectory+"baseline-layout.json"));
            var items=(JArray)JObject.Parse(File.ReadAllText(StrictJointDirectory+"candidates.json"))["candidates"];
            foreach(JObject item in items)ValidateReviewedJointCandidate(baseline,(JObject)item["layout"],JointStagingPolicy);
            void Refuse(Action<JObject> mutate)
            {
                var candidate=(JObject)items[0]["layout"].DeepClone();mutate(candidate);
                bool refused=false;try{ValidateReviewedJointCandidate(baseline,candidate,JointStagingPolicy);}catch(InvalidOperationException){refused=true;}
                Require(refused,"Malformed or authority-changing candidate must refuse");
            }
            Refuse(v=>v["worldBinding"]["SourceHash"]="foreign");
            Refuse(v=>((JArray)v["pads"]).OfType<JObject>().Single(p=>(string)p["PadId"]=="pad.skirmish.db.staging_player")["CenterX"]=0);
            Refuse(v=>v["routes"][0]["AllowsVehicles"]=false);
            Refuse(v=>v["anchors"][1]["RoleId"]=v["anchors"][0]["RoleId"].DeepClone());
            Refuse(v=>((JArray)v["anchors"]).Add(((JArray)v["anchors"])[0].DeepClone()));
            Refuse(v=>v["pads"][1]["PadId"]=v["pads"][0]["PadId"].DeepClone());
            Refuse(v=>v["unknownAuthoringOverride"]=true);
            Refuse(v=>v["pads"][0]["WidthMetres"]=float.NaN);
            Debug.Log("[ReviewedLogicalJointControls] result=Passed controls=11 candidateLimit=3 nativeQueries=0 sourceFitCertified=0");
        }
        const string JointStagingPolicy="S049JointStagingAndSharedEndpoints-v2";
        static void ValidateReviewedJointCandidate(JObject baseline,JObject candidate,string policy)
        {
            if(policy==null){ValidateStrictLogicalFields(baseline,candidate);return;}
            Require(policy==JointStagingPolicy,"Unknown reviewed logical-difference policy");
            var normalized=(JObject)candidate.DeepClone();
            var oldPads=((JArray)baseline["pads"]).OfType<JObject>().ToArray();
            var pads=((JArray)candidate["pads"]).OfType<JObject>().ToArray();
            var normalizedPads=((JArray)normalized["pads"]).OfType<JObject>().ToArray();
            var oldAnchors=((JArray)baseline["anchors"]).OfType<JObject>().ToArray();
            var anchors=((JArray)candidate["anchors"]).OfType<JObject>().ToArray();
            var normalizedAnchors=((JArray)normalized["anchors"]).OfType<JObject>().ToArray();
            string[] editable={"pad.skirmish.db.base_player","pad.skirmish.db.base_enemy","pad.skirmish.db.supply_player","pad.skirmish.db.supply_enemy","pad.skirmish.db.staging_player","pad.skirmish.db.staging_enemy"};
            foreach(string id in editable)
            {
                var pad=pads.Single(p=>(string)p["PadId"]==id);
                var anchor=anchors.Single(a=>(string)a["AnchorId"]==(string)pad["AnchorId"]);
                foreach(string key in new[]{"CenterX","CenterZ","WidthMetres","DepthMetres"})Require(float.IsFinite((float)pad[key]),"Finite reviewed facility bounds required");
                float x=(float)pad["CenterX"],z=(float)pad["CenterZ"],w=(float)pad["WidthMetres"],h=(float)pad["DepthMetres"];
                Require(w>0&&h>0&&x-w/2>=(float)candidate["originX"]&&z-h/2>=(float)candidate["originZ"]&&x+w/2<=(float)candidate["originX"]+(float)candidate["worldWidthMetres"]&&z+h/2<=(float)candidate["originZ"]+(float)candidate["worldDepthMetres"],"Reviewed facility bounds outside unchanged logical frame");
                Require((float)anchor["WorldX"]==x&&(float)anchor["WorldZ"]==z,"Actual typed pad/anchor center mismatch");
                Require(math.abs((float)anchor["NormalizedU"]-(x-(float)candidate["originX"])/(float)candidate["worldWidthMetres"])<.00001f&&math.abs((float)anchor["NormalizedV"]-(z-(float)candidate["originZ"])/(float)candidate["worldDepthMetres"])<.00001f,"Actual facility normalized anchor mismatch");
                var original=oldAnchors.Single(a=>(string)a["AnchorId"]==(string)pad["AnchorId"]);
                var copy=normalizedAnchors.Single(a=>(string)a["AnchorId"]==(string)pad["AnchorId"]);
                foreach(string key in new[]{"WorldX","WorldZ","NormalizedU","NormalizedV"})copy[key]=original[key].DeepClone();
                if(id.Contains(".staging_"))normalizedPads.Single(p=>(string)p["PadId"]==id).Replace(oldPads.Single(p=>(string)p["PadId"]==id).DeepClone());
            }
            ValidateStrictLogicalFields(baseline,normalized);
            Vector2? entry=null,exit=null;
            foreach(JObject route in (JArray)candidate["routes"])
            {
                var ids=((JArray)route["WaypointAnchorIds"]).Values<string>().ToArray();
                Require(ids.Length>=2,"Real authored route requires at least two anchors");
                var points=ids.Select(id=>anchors.Single(a=>(string)a["AnchorId"]==id)).ToArray();
                foreach(var point in points)Require(float.IsFinite((float)point["WorldX"])&&float.IsFinite((float)point["WorldZ"]),"Finite actual route anchors required");
                var start=new Vector2((float)points[0]["WorldX"],(float)points[0]["WorldZ"]);
                var end=new Vector2((float)points[points.Length-1]["WorldX"],(float)points[points.Length-1]["WorldZ"]);
                Require(!entry.HasValue||(entry.Value.x==start.x&&entry.Value.y==start.y&&exit.Value.x==end.x&&exit.Value.y==end.y),"Exact same authored entry/exit coordinates required; only interiors fan out");
                entry=start;exit=end;
                float length=0;for(int i=1;i<points.Length;i++)length+=Vector2.Distance(new Vector2((float)points[i-1]["WorldX"],(float)points[i-1]["WorldZ"]),new Vector2((float)points[i]["WorldX"],(float)points[i]["WorldZ"]));
                Require(length>0&&math.abs((float)route["LengthMetres"]-length)<.02f&&(bool?)route["ProvisionalTimes"]==true,"Actual provisional route geometry required; no native path credit");
                foreach(var metric in new[]{("InfantryTransitSeconds",4f),("GroundTransitSeconds",8f),("InfantryFirstContactSeconds",8f),("GroundFirstContactSeconds",16f)})
                    Require(math.abs((float)route[metric.Item1]-length/metric.Item2)<.02f,"Provisional timing must derive from actual polyline");
            }
            var trial=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            try
            {
                JsonUtility.FromJsonOverwrite(candidate.ToString(Newtonsoft.Json.Formatting.None),trial);
                var grid=new GridConfig{Width=1280,Height=520,CellSize=1,Origin=new float3(740,0,260)};
                Require(TryPreflightAuthoredRouteEndpoints(trial,grid,out _),"Actual typed staging zones must admit common endpoints before native");
            }
            finally{UnityEngine.Object.DestroyImmediate(trial);}
        }
        public static void RunReviewedJointStagingControls()
        {
            var baseline=JObject.Parse(File.ReadAllText(StrictJointDirectory+"baseline-layout.json"));
            var items=(JArray)JObject.Parse(File.ReadAllText(StrictJointDirectory+"candidates.json"))["candidates"];
            int controls=0;
            foreach(JObject item in items){ValidateReviewedJointCandidate(baseline,(JObject)item["layout"],JointStagingPolicy);controls++;}
            void Refuse(Action<JObject> mutation,string policy)
            {
                var c=(JObject)items[0]["layout"].DeepClone();mutation(c);
                bool refused=false;try{ValidateReviewedJointCandidate(baseline,c,policy);}catch(InvalidOperationException){refused=true;}
                Require(refused,"Reviewed joint staging control must refuse");controls++;
            }
            Refuse(v=>{},null);
            Refuse(v=>{},"unknown");
            Refuse(v=>v["anchors"][0]["WorldX"]=0,JointStagingPolicy);
            Refuse(v=>((JArray)v["anchors"]).OfType<JObject>().Single(a=>(string)a["AnchorId"]=="anchor.pilot.approach.1.0")["WorldX"]=1380.5f,JointStagingPolicy);
            Refuse(v=>((JArray)v["pads"]).OfType<JObject>().Single(p=>(string)p["PadId"]=="pad.skirmish.db.staging_player")["CenterX"]=0,JointStagingPolicy);
            Refuse(v=>((JArray)v["pads"]).OfType<JObject>().Single(p=>(string)p["PadId"]=="pad.skirmish.db.spawn_vehicle_enemy")["WidthMetres"]=1,JointStagingPolicy);
            Refuse(v=>v["worldBinding"]["SourceHash"]="foreign",JointStagingPolicy);
            Require(controls==10,"Exact bounded staging controls");
            Debug.Log("[ReviewedJointStagingControls] result=Passed controls=10 nativeQueries=0 sourceFitCertified=0");
        }
        static void ValidateStrictLogicalFields(JObject baseline,JObject candidate)
        {
            Require(candidate!=null&&JToken.DeepEquals(baseline["worldBinding"],candidate["worldBinding"]),"Unchanged source/deployment binding required");
            var b=(JObject)baseline.DeepClone();var c=(JObject)candidate.DeepClone();
            foreach(string key in new[]{"pads","anchors","routes"}){b.Remove(key);c.Remove(key);}
            Require(JToken.DeepEquals(b,c),"No logical frame/source/other field changes allowed");
            var bp=((JArray)baseline["pads"]).OfType<JObject>().ToArray();var cp=((JArray)candidate["pads"]).OfType<JObject>().ToArray();
            Require(bp.Length==cp.Length&&bp.Select(p=>(string)p["PadId"]).SequenceEqual(cp.Select(p=>(string)p["PadId"])),"Exact typed pad identities/order required");
            for(int i=0;i<bp.Length;i++)
            {
                b=(JObject)bp[i].DeepClone();c=(JObject)cp[i].DeepClone();string id=(string)b["PadId"];
                bool fixedPad=new[]{".staging_",".service_",".spawn_",".rally_",".air_",".supply_expansion_"}.Any(id.Contains);
                if(!fixedPad)foreach(string key in new[]{"CenterX","CenterZ","WidthMetres","DepthMetres"}){b.Remove(key);c.Remove(key);}
                Require(JToken.DeepEquals(b,c),"Only reviewed facility center/bounds may change");
                foreach(string key in new[]{"CenterX","CenterZ","WidthMetres","DepthMetres"})
                    Require(float.IsFinite((float)cp[i][key]),"Finite logical pad dimensions required");
                Require((float)cp[i]["WidthMetres"]>0&&(float)cp[i]["DepthMetres"]>0,"Positive logical pads required");
            }
            var ba=((JArray)baseline["anchors"]).OfType<JObject>().ToArray();var ca=((JArray)candidate["anchors"]).OfType<JObject>().ToArray();
            Require(ba.Select(a=>(string)a["AnchorId"]).Distinct().Count()==ba.Length&&ca.Select(a=>(string)a["AnchorId"]).Distinct().Count()==ca.Length&&ca.Length==ba.Length+4,
                "Exact existing anchors plus four named fanout anchors required");
            Require(ca.Select(a=>(string)a["RoleId"]).All(v=>!string.IsNullOrEmpty(v))&&ca.Select(a=>(string)a["RoleId"]).Distinct().Count()==ca.Length,
                "Unique actual anchor roles required");
            foreach(var anchor in ba)
            {
                string id=(string)anchor["AnchorId"];b=(JObject)anchor.DeepClone();c=(JObject)ca.Single(a=>(string)a["AnchorId"]==id).DeepClone();
                bool endpoint=id=="anchor.pilot.approach.1.0"||id=="anchor.pilot.approach.1.3"||id=="anchor.pilot.approach.2.0"||id=="anchor.pilot.approach.2.3";
                if(endpoint)foreach(string key in new[]{"WorldX","WorldZ","NormalizedU","NormalizedV"}){b.Remove(key);c.Remove(key);}
                Require(JToken.DeepEquals(b,c),"Stable non-route anchors and endpoint roles required");
            }
            Require(ca.Where(a=>!ba.Any(v=>(string)v["AnchorId"]==(string)a["AnchorId"])).Select(a=>(string)a["AnchorId"]).OrderBy(x=>x)
                .SequenceEqual(new[]{"anchor.pilot.joint.1.left","anchor.pilot.joint.1.right","anchor.pilot.joint.2.left","anchor.pilot.joint.2.right"}),"Exact new route anchors required");
            var br=((JArray)baseline["routes"]).OfType<JObject>().ToArray();var cr=((JArray)candidate["routes"]).OfType<JObject>().ToArray();
            Require(br.Length==3&&cr.Length==3,"Exact three routes required");
            for(int i=0;i<3;i++)
            {
                b=(JObject)br[i].DeepClone();c=(JObject)cr[i].DeepClone();
                foreach(string key in new[]{"WaypointAnchorIds","LengthMetres","InfantryTransitSeconds","GroundTransitSeconds","InfantryFirstContactSeconds","GroundFirstContactSeconds","ProvisionalTimes"}){b.Remove(key);c.Remove(key);}
                Require(JToken.DeepEquals(b,c),"Route identities/capabilities/width unchanged");
            }
        }
        static JObject RefineStrictLogicalCandidates(JObject mission,GroundObjective30CandidateOwner owner,OperationMapDefinition source,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,string prefix,Action<JObject> checkpoint)
        {
            Require(strictLogicalCandidatesRequested&&strictLogicalCandidates!=null&&!ProtectedRefinementCatalog(owner.Definition.CatalogId),"Exact owned strict candidate scope required");
            mission["reviewedLogicalInput"]=strictLogicalInputReceipt.DeepClone();
            var authority=(JObject)strictLogicalCandidates["sourceAuthority"];var pins=(JObject)authority["sourcePins"];
            Require(grid.Width==(int)authority["grid"]["width"]&&grid.Height==(int)authority["grid"]["height"]&&grid.CellSize==(float)authority["grid"]["cellSize"]&&
                grid.Origin.x==(float)authority["grid"]["originXZ"][0]&&grid.Origin.z==(float)authority["grid"]["originXZ"][1]&&
                source.OperationMapId==(string)pins["operationMapId"]&&source.SourceIdentityHash==(string)pins["sourceIdentityHash"]&&source.ContentHash==(string)pins["contentHash"],
                "Exact actual loaded Campaign source and grid required");
            foreach(string field in new[]{"source","grid","surface"})
                Require(Hash((string)pins[field+"Path"])==(string)pins[field+"AssetSha256"],"Actual physical source bytes changed");
            string original=JsonUtility.ToJson(owner.Layout),definition=JsonUtility.ToJson(owner.Definition);
            var diagnostics=new JArray();JObject last=null,accepted=null;int lastIndex=-1,acceptedIndex=-1;
            var compounds=(JObject)JObject.Parse(File.ReadAllText(StrictJointReport))["missions"][0]["measurement"]["passes"][1]["sizes"][0]["candidateCompoundWorldRects"];
            try
            {
                foreach(JObject item in (JArray)strictLogicalCandidates["candidates"])
                {
                    int index=(int)item["candidate"];lastIndex=index;Require(index==diagnostics.Count,"Finite ordered candidate identity required");
                    JsonUtility.FromJsonOverwrite(original,owner.Layout);JsonUtility.FromJsonOverwrite(definition,owner.Definition);
                    string candidate=((JObject)item["layout"]).ToString(Newtonsoft.Json.Formatting.None);
                    last=RefineLoadedCandidateCore(mission,owner,source,em,gridEntity,grid,surface,prefix+".strictJoint"+index,step=>
                    {var row=(JObject)step.DeepClone();row["strictCandidate"]=index;row["candidateLocalPass"]=row["pass"];row["pass"]=index*3+(int)row["pass"];checkpoint?.Invoke(row);},
                    compounds,true,false,true,layout=>JsonUtility.FromJsonOverwrite(candidate,layout));
                    diagnostics.Add(new JObject{["candidate"]=index,["measurement"]=last.DeepClone(),["sourceFitCertified"]=false});
                    if(last["strictThreeSizePlacementPass"]!=null&&last["logicalFinalizationUnresolved"]==null){accepted=last;acceptedIndex=index;break;}
                }
                var result=(JObject)(accepted??last??new JObject()).DeepClone();
                int selectedIndex=accepted!=null?acceptedIndex:lastIndex;
                if(result["passes"] is JArray selectedPasses)
                    foreach(JObject pass in selectedPasses){int local=(int)pass["pass"];pass["candidateLocalPass"]=local;pass["pass"]=selectedIndex*3+local;}
                if(result["strictThreeSizePlacementPass"]!=null)
                    result["strictThreeSizePlacementPass"]=selectedIndex*3+(int)result["strictThreeSizePlacementPass"];
                result["reviewedLogicalInput"]=strictLogicalInputReceipt.DeepClone();
                result["strictLogicalSelectedCandidate"]=selectedIndex;result["strictLogicalCandidateDiagnostics"]=diagnostics;
                result["sourceFitCertified"]=false;result["normalMissionReady"]=false;result["cameraAccepted"]=false;
                return result;
            }
            finally
            {
                JsonUtility.FromJsonOverwrite(original,owner.Layout);JsonUtility.FromJsonOverwrite(definition,owner.Definition);
            }
        }
    }
}
#endif
