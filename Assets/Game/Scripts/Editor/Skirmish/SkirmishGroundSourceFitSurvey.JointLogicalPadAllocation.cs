#if UNITY_EDITOR
using System;
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
        static bool refinementJointPadSearchRequested;
        static readonly Vector2[] JointPadOffsets={Vector2.zero,new Vector2(32,0),new Vector2(-32,0),new Vector2(0,32),new Vector2(0,-32)};

        public static void RunJointLogicalRefinementLegacyAndOpeningWitness()
        {
            RunAll120JointLogicalPadRefinement();
            SkirmishLegacyProductionControls.RunFocusedValidation();
            SkirmishS002AriaRunHarness.RunS005OpeningWitnessWithLateEcsCensus();
        }

        public static void RunAll120JointLogicalPadRefinement()
        {
            Require(!refinementJointPadSearchRequested&&!refinementCompoundSearchRequested&&!refinementLogicalFrameSearchRequested&&
                !EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,
                "Fresh idle joint logical-pad authoring required");
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            Require(!string.IsNullOrEmpty(id)&&!ProtectedRefinementCatalog(id),"One unprotected catalog required");
            RunJointLogicalPadControls();
            refinementJointPadSearchRequested=true;
            try{RunAll120SimultaneousLogicalRefinement();}
            finally{refinementJointPadSearchRequested=false;}
        }

        public static JObject RefineLoadedCandidate(JObject mission,GroundObjective30CandidateOwner owner,OperationMapDefinition source,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,string artifactPrefix=null,Action<JObject> checkpoint=null)
        {
            if(strictLogicalCandidatesRequested)
                return RefineStrictLogicalCandidates(mission,owner,source,em,gridEntity,grid,surface,artifactPrefix,checkpoint);
            if(!refinementJointPadSearchRequested)
                return RefineLoadedCandidateCore(mission,owner,source,em,gridEntity,grid,surface,artifactPrefix,checkpoint);
            Require(owner!=null&&owner.Layout!=null&&!ProtectedRefinementCatalog(owner.Definition.CatalogId),"Unprotected caller-owned candidate required");
            Require(!refinementCompoundSearchRequested&&!refinementLogicalFrameSearchRequested,"Joint search cannot multiply other candidate windows");
            string original=JsonUtility.ToJson(owner.Layout);
            string originalDefinition=JsonUtility.ToJson(owner.Definition);
            var compounds=CandidateCompoundRects((JObject)mission["layout_input"]["pilot_candidate"]);
            var candidates=new JArray();JObject last=null;
            for(int candidate=0;candidate<9;candidate++)
            {
                JsonUtility.FromJsonOverwrite(original,owner.Layout);
                JsonUtility.FromJsonOverwrite(originalDefinition,owner.Definition);
                var evidence=new JObject{["candidate"]=candidate,["maximumCandidates"]=9,["maximumStrictPasses"]=2,
                    ["frameMarginMetres"]=128,["sourceFitCertified"]=false};
                candidates.Add(evidence);
                if(!TryNextRefinementLogicalFrame(original,compounds,3,owner.Layout,source,grid,out var widened,out string refusal))
                {evidence["status"]="Refused_LogicalFrame";evidence["reason"]=refusal;continue;}
                bool staging=candidate>=5;Vector2 offset=JointPadOffsets[candidate==0?0:((candidate-1)%4)+1];
                evidence["translateStaging"]=staging;evidence["offsetX"]=offset.x;evidence["offsetZ"]=offset.y;
                if(!TryEncodeJointPadTranslation(JsonUtility.ToJson(owner.Layout),offset,staging,out string next,out refusal))
                {evidence["status"]="Refused_LogicalPadOrRoute";evidence["reason"]=refusal;continue;}
                JsonUtility.FromJsonOverwrite(next,owner.Layout);
                evidence["proposedBeforeDiscoveryJson"]=next;
                int capturedCandidate=candidate;
                last=RefineLoadedCandidateCore(mission,owner,source,em,gridEntity,grid,surface,
                    artifactPrefix==null?null:artifactPrefix+".joint"+candidate,step=>
                    {
                        var observed=(JObject)step.DeepClone();observed["jointCandidate"]=capturedCandidate;
                        observed["candidateLocalPass"]=observed["pass"];
                        observed["pass"]=capturedCandidate*3+(int)observed["pass"];
                        checkpoint?.Invoke(observed);
                    },widened,true,staging);
                evidence["status"]="CollectedUnqualified";evidence["measurement"]=last.DeepClone();
                // Placement alone and source-only diagnostic plots never select a winner.
                if(last["strictThreeSizePlacementPass"]!=null&&last["logicalFinalizationUnresolved"]==null)
                {evidence["status"]="StrictAuthoringCandidate_MissionAcceptancePending";break;}
            }
            var result=last==null?new JObject():(JObject)last.DeepClone();
            result["jointLogicalPadCandidates"]=candidates;result["maximumJointCandidates"]=9;
            result["sourceFitCertified"]=false;result["normalMissionReady"]=false;result["cameraAccepted"]=false;
            if(last==null){JsonUtility.FromJsonOverwrite(original,owner.Layout);JsonUtility.FromJsonOverwrite(originalDefinition,owner.Definition);}
            return result;
        }

        // Source-axis metres are already proved by the existing widening binder.
        // Route metric updates are provisional geometry only. Each moved-staging
        // candidate must also pass fresh all-profile native paths before discovery
        // and again with the actual accepted facility reservations.
        static bool JointRoutesExact(JObject routes)
        {
            var queries=routes?["queries"] as JArray;
            return queries!=null&&queries.Count>0&&queries.OfType<JObject>().Count()==queries.Count&&
                queries.OfType<JObject>().All(q=>(string)q["status"]=="NativeQueryFoundExactGoal"&&(int?)q["failureCode"]==0);
        }

        static bool TryEncodeJointPadTranslation(string original,Vector2 offset,bool staging,out string next,out string error)
        {
            next=null;error=null;
            if(!float.IsFinite(offset.x)||!float.IsFinite(offset.y)||!JointPadOffsets.Contains(offset))
            {error="Only finite declared zero/cardinal32m translations allowed";return false;}
            var data=JObject.Parse(original);var pads=(JArray)data["pads"];var anchors=(JArray)data["anchors"];var routes=(JArray)data["routes"];
            var spawn=pads.OfType<JObject>().Where(p=>(int?)p["FactionId"]==2&&(int?)p["Kind"]==(int)SkirmishLegalPadKind.InfantrySpawn).ToArray();
            var ground=pads.OfType<JObject>().Where(p=>(int?)p["FactionId"]==2&&(int?)p["Kind"]==(int)SkirmishLegalPadKind.GroundStaging).ToArray();
            if(spawn.Length!=1||ground.Length!=1){error="Unique actual enemy InfantrySpawn/GroundStaging required";return false;}
            if(offset==Vector2.zero&&!staging){next=original;return true;}
            if(staging)
            {
                string anchorId=(string)ground[0]["AnchorId"];
                if((string)data["worldBinding"]["PlayerAnchorId"]==anchorId||(string)data["worldBinding"]["EnemyAnchorId"]==anchorId)
                {error="DeploymentAnchorCannotMove";return false;}
                var matches=anchors.OfType<JObject>().Where(a=>(string)a["AnchorId"]==anchorId).ToArray();
                if(matches.Length!=1){error="Unique actual staging anchor required";return false;}
                var a=matches[0];float x=(float)a["WorldX"]+offset.x,z=(float)a["WorldZ"]+offset.y;
                a["WorldX"]=x;a["WorldZ"]=z;
                a["NormalizedU"]=(x-(float)data["originX"])/(float)data["worldWidthMetres"];
                a["NormalizedV"]=(z-(float)data["originZ"])/(float)data["worldDepthMetres"];
                if((float)a["NormalizedU"]<0||(float)a["NormalizedU"]>1||(float)a["NormalizedV"]<0||(float)a["NormalizedV"]>1)
                {error="Actual staging anchor outside clipped logical frame";return false;}
                foreach(var route in routes.OfType<JObject>().Where(r=>((JArray)r["WaypointAnchorIds"]).Values<string>().Contains(anchorId)))
                {
                    var ids=((JArray)route["WaypointAnchorIds"]).Values<string>().ToArray();
                    var points=new SkirmishLayoutAnchorConfig[ids.Length];
                    for(int i=0;i<ids.Length;i++)
                    {
                        var found=anchors.OfType<JObject>().Where(row=>(string)row["AnchorId"]==ids[i]).ToArray();
                        if(found.Length!=1){error="Unique actual route waypoint required";return false;}
                        points[i]=found[0].ToObject<SkirmishLayoutAnchorConfig>();
                    }
                    float oldLength=(float?)route["LengthMetres"]??0;
                    // Same polyline value calculation as the existing builder;
                    // kept Editor-local because the frozen owning Configs ref
                    // intentionally does not export its current helper method.
                    float length=0;
                    for(int i=1;i<points.Length;i++)
                    {
                        float dx=points[i].WorldX-points[i-1].WorldX,dz=points[i].WorldZ-points[i-1].WorldZ;
                        length+=(float)Math.Sqrt(dx*dx+dz*dz);
                    }
                    if(!float.IsFinite(oldLength)||oldLength<=0||!float.IsFinite(length)||length<=0)
                    {error="Positive finite existing route metrics required";return false;}
                    foreach(string metric in new[]{"InfantryTransitSeconds","GroundTransitSeconds","AirTransitSeconds","InfantryFirstContactSeconds","GroundFirstContactSeconds"})
                    {
                        float seconds=(float?)route[metric]??0;
                        if(!float.IsFinite(seconds)||seconds<=0){error="Unchanged positive existing provisional movement rate required";return false;}
                        route[metric]=seconds*(length/oldLength);
                    }
                    route["LengthMetres"]=length;route["ProvisionalTimes"]=true;
                }
            }
            foreach(var pad in staging?new[]{spawn[0],ground[0]}:new[]{spawn[0]})
            {
                float x=(float)pad["CenterX"]+offset.x,z=(float)pad["CenterZ"]+offset.y;
                if(x-(float)pad["WidthMetres"]*.5f<(float)data["originX"]||z-(float)pad["DepthMetres"]*.5f<(float)data["originZ"]||
                    x+(float)pad["WidthMetres"]*.5f>(float)data["originX"]+(float)data["worldWidthMetres"]||
                    z+(float)pad["DepthMetres"]*.5f>(float)data["originZ"]+(float)data["worldDepthMetres"])
                {error="Translated full typed pad exceeds physical/grid-clipped logical frame";return false;}
                pad["CenterX"]=x;pad["CenterZ"]=z;
            }
            next=data.ToString(Newtonsoft.Json.Formatting.None);return true;
        }

        public static void RunJointLogicalPadControls()
        {
            Require(JointPadOffsets.Length==5&&JointPadOffsets.Distinct().Count()==5&&JointPadOffsets.Skip(1).All(v=>v.magnitude==32),"Finite nine-mode window");
            var data=new JObject{["originX"]=0,["originZ"]=0,["worldWidthMetres"]=200,["worldDepthMetres"]=200,
                ["worldBinding"]=new JObject{["PlayerAnchorId"]="own",["EnemyAnchorId"]="base"},
                ["anchors"]=new JArray(new JObject{["AnchorId"]="staging",["WorldX"]=100,["WorldZ"]=100,["NormalizedU"]=.5,["NormalizedV"]=.5},new JObject{["AnchorId"]="own",["WorldX"]=20,["WorldZ"]=100,["NormalizedU"]=.1,["NormalizedV"]=.5}),
                ["routes"]=new JArray(new JObject{["RouteId"]="route",["Kind"]=1,["WidthMetres"]=6,["AllowsVehicles"]=true,["WaypointAnchorIds"]=new JArray("own","staging"),["LengthMetres"]=80,["InfantryTransitSeconds"]=20,["GroundTransitSeconds"]=10,["AirTransitSeconds"]=5,["InfantryFirstContactSeconds"]=10,["GroundFirstContactSeconds"]=5,["ProvisionalTimes"]=true}),
                ["pads"]=new JArray(new JObject{["PadId"]="spawn",["AnchorId"]="staging",["FactionId"]=2,["Kind"]=(int)SkirmishLegalPadKind.InfantrySpawn,["CenterX"]=100,["CenterZ"]=100,["WidthMetres"]=24,["DepthMetres"]=25},
                    new JObject{["PadId"]="staging",["AnchorId"]="staging",["FactionId"]=2,["Kind"]=(int)SkirmishLegalPadKind.GroundStaging,["CenterX"]=90,["CenterZ"]=90,["WidthMetres"]=20,["DepthMetres"]=14},
                    new JObject{["PadId"]="vehicle",["AnchorId"]="staging",["FactionId"]=2,["Kind"]=(int)SkirmishLegalPadKind.VehicleSpawn,["CenterX"]=140,["CenterZ"]=140,["WidthMetres"]=27,["DepthMetres"]=27})};
            string original=data.ToString();
            Require(TryEncodeJointPadTranslation(original,Vector2.zero,false,out var unchanged,out _)&&JToken.DeepEquals(data,JObject.Parse(unchanged)),"Baseline exact parity");
            foreach(var offset in JointPadOffsets.Skip(1))
            {
                Require(TryEncodeJointPadTranslation(original,offset,false,out var encoded,out _),"All four infantry translations");
                var moved=JObject.Parse(encoded);var expected=(JObject)data.DeepClone();
                expected["pads"][0]["CenterX"]=100+offset.x;expected["pads"][0]["CenterZ"]=100+offset.y;
                Require(JToken.DeepEquals(moved,expected),"Only actual InfantrySpawn centre moves; shared anchor/routes/other pads unchanged");
            }
            Require(TryEncodeJointPadTranslation(original,new Vector2(32,0),true,out var routed,out _)&&
                (float)JObject.Parse(routed)["routes"][0]["LengthMetres"]==112&&
                (float)JObject.Parse(routed)["routes"][0]["InfantryTransitSeconds"]==28&&
                JToken.DeepEquals(data["routes"][0]["WaypointAnchorIds"],JObject.Parse(routed)["routes"][0]["WaypointAnchorIds"]),"Actual route IDs retained; provisional metrics preserve movement rate");
            data["routes"]=new JArray();
            Require(TryEncodeJointPadTranslation(data.ToString(),new Vector2(0,32),true,out var joint,out _)&&
                (float)JObject.Parse(joint)["anchors"][0]["WorldZ"]==132&&
                (float)JObject.Parse(joint)["anchors"][0]["NormalizedV"]==.66f,"Unbound joint staging anchor normalized projection");
            Require(!TryEncodeJointPadTranslation(original,new Vector2(float.NaN,0),false,out _,out _)&&
                !TryEncodeJointPadTranslation(original,new Vector2(64,0),false,out _,out _),"Nonfinite/unbounded offsets refuse");
            data["pads"][0]["CenterX"]=190;
            Require(!TryEncodeJointPadTranslation(data.ToString(),new Vector2(32,0),false,out _,out _),"Full footprint cannot leave clipped source frame");
            Require(ProtectedRefinementCatalog("S005")&&!ProtectedRefinementCatalog("S026"),"Coded binding protection retained");
            var exact=new JObject{["queries"]=new JArray(new JObject{["status"]="NativeQueryFoundExactGoal",["failureCode"]=0})};
            Require(JointRoutesExact(exact),"Actual native exact query positive");
            exact["queries"][0]["failureCode"]=1;
            Require(!JointRoutesExact(exact)&&!JointRoutesExact(new JObject{["queries"]=new JArray()})&&!JointRoutesExact(null),"Failed/missing/empty native queries refuse");
            Debug.Log("[JointLogicalPadControls] result=Passed controls=10 candidateWindow=9 maximumStrictPasses=2 sourceFitCertified=0");
        }
    }
}
#endif
