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
            int frameAttempt=logicalPadInput==null?3:(int)logicalPadInput["fixedLogicalFrameAttempt"];
            JObject compounds;string error;
            bool frameReady=apGroundJointPreferencesRequested
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
            if(apGroundJointPreferencesRequested)
            {
                report["apJointPadInput"]=ApplyApJointPadInput(mission,owner.Layout,source,grid,budgets);
                report["apJointRouteSeed"]=PrepareApJointRouteSeed(owner,budgets,em,gridEntity,grid,surface);
                if(report["apJointRouteSeed"]["selectedCandidate"]==null)return report;
            }
            else
            {
                var routes=ProposeBoundedNativeRouteRepair(owner.Layout,budgets,baselineRoutes,em,gridEntity,grid,surface);
                report["boundedRouteProposal"]=routes;
            }
            // Re-query complete actual paths after the proposal, before deriving
            // any facility keep-clear. Failed native paths remain explicit facts.
            var approachEvidence=new JObject();
            var clearance=PreferenceApproachClearance(owner.Layout,budgets,em,gridEntity,grid,surface,approachEvidence);
            report["actualProposedApproaches"]=approachEvidence;
            var maxBudget=budgets.OrderByDescending(b=>b.Structures.Length).ThenByDescending(b=>(int)b.Size).First();
            var inventory=new JObject{["newLogicalCandidateMaterialized"]=true,["candidateCompoundWorldRects"]=compounds.DeepClone(),
                ["allSizeFacilities"]=Ground30PreferredFacilities(maxBudget,owner.Layout,grid)};
            ProbePilotNormalFacilityRelocation(new JObject(),System.Array.Empty<JObject>(),owner.Layout,owner.Definition.RoleCatalog,
                em,gridEntity,grid,surface,inventory,true,true,clearance,true,serviceAuthority:(mpGroundBaPilotRequested||ApPairServiceAuthority(owner.Definition))?owner.Definition:null);
            report["independentSourceQaCapacityHints"]=inventory;
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
                        },compounds,true,false,true);
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
