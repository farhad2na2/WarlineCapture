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
            if(!TryNextRefinementLogicalFrame(before,originalCompounds,3,owner.Layout,source,grid,out var compounds,out string error))
            {report["refused"]=error;return report;}
            var routes=ProposeBoundedNativeRouteRepair(owner.Layout,budgets,baselineRoutes,em,gridEntity,grid,surface);
            report["boundedRouteProposal"]=routes;
            // Re-query complete actual paths after the proposal, before deriving
            // any facility keep-clear. Failed native paths remain explicit facts.
            var approachEvidence=new JObject();
            var clearance=PreferenceApproachClearance(owner.Layout,budgets,em,gridEntity,grid,surface,approachEvidence);
            report["actualProposedApproaches"]=approachEvidence;
            var maxBudget=budgets.OrderByDescending(b=>b.Structures.Length).ThenByDescending(b=>(int)b.Size).First();
            var inventory=new JObject{["newLogicalCandidateMaterialized"]=true,["candidateCompoundWorldRects"]=compounds.DeepClone(),
                ["allSizeFacilities"]=Ground30PreferredFacilities(maxBudget,owner.Layout,grid)};
            ProbePilotNormalFacilityRelocation(new JObject(),System.Array.Empty<JObject>(),owner.Layout,owner.Definition.RoleCatalog,
                em,gridEntity,grid,surface,inventory,true,true,clearance,true,serviceAuthority:mpGroundBaPilotRequested?owner.Definition:null);
            report["independentSourceQaCapacityHints"]=inventory;
            var hints=(JArray)inventory["normalFacilityRelocationProposals"];
            var assignments=BuildPreferenceAssignments(owner.Layout,maxBudget.Structures,hints,grid,compounds);
            report["preferenceAssignments"]=assignments;
            string baseline=JsonUtility.ToJson(owner.Layout),definition=JsonUtility.ToJson(owner.Definition);
            var results=new JArray();report["nativePreferenceReplays"]=results;
            JObject accepted=null;
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
                            var row=(JObject)step.DeepClone();row["preferenceCandidate"]=captured;
                            row["candidateLocalPass"]=row["pass"];row["pass"]=captured*3+(int)row["pass"];
                            checkpoint?.Invoke(row);
                        },compounds,true,false,true);
                }
                finally{refinementPreferenceReplay=false;}
                evidence["measurement"]=measured;
                if(measured["strictThreeSizePlacementPass"]!=null&&measured["logicalFinalizationUnresolved"]==null)
                {accepted=measured;report["strictAuthoringPreferenceCandidate"]=candidate;break;}
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
            foreach(var kind in new[]{SkirmishLegalPadKind.BaseBarracks,SkirmishLegalPadKind.AirReturn,SkirmishLegalPadKind.Supply})
            {
                Require(layout.TryGetPad(2,kind,out var pad),"Exact enemy facility preference pad required");
                var seed=hints?.OfType<JObject>().Where(r=>(int?)r["faction"]==2&&(string)r["padId"]==pad.PadId&&
                    (long?)r["independentlySourceQaLegalOriginCount"]>0&&r["representativePlots"] is JArray plots&&plots.Count>0)
                    .OrderBy(r=>(long)r["independentlySourceQaLegalOriginCount"]).ThenBy(r=>(string)r["structureId"],StringComparer.Ordinal).FirstOrDefault();
                if(seed==null)return new JArray();
                var structure=structures.Single(s=>s.FactionId==2&&s.StructureId==(string)seed["structureId"]);
                float dx=0,dz=0;Game.Configs.SkirmishMapLayoutBuilder.ApplyFrameOffset(layout,structure.AcrossOffsetMetres,structure.ForwardOffsetMetres,ref dx,ref dz);
                var footprint=(JArray)seed["nativePlacementFootprint"];var next=new List<JObject>();
                foreach(var prior in beam)foreach(JObject plot in (JArray)seed["representativePlots"])
                {
                    var origin=(JArray)plot["preferredOriginCell"];
                    float x=grid.Origin.x+((int)origin[0]+(int)footprint[0]/2+.5f)*grid.CellSize-dx;
                    float z=grid.Origin.z+((int)origin[1]+(int)footprint[1]/2+.5f)*grid.CellSize-dz;
                    var bounds=(JArray)compounds["2"];
                    if(x-pad.WidthMetres*.5f<(float)bounds[0]||z-pad.DepthMetres*.5f<(float)bounds[1]||
                        x+pad.WidthMetres*.5f>(float)bounds[2]||z+pad.DepthMetres*.5f>(float)bounds[3])continue;
                    var rect=new RectInt((int)plot["x"],(int)plot["z"],(int)plot["width"],(int)plot["depth"]);
                    if(((JArray)prior["pads"]).OfType<JObject>().Any(p=>PreferenceRect((JObject)p["hintRect"]).Overlaps(rect)))continue;
                    var candidate=(JObject)prior.DeepClone();
                    candidate["score"]=(double)prior["score"]+Math.Pow(x-pad.CenterX,2)+Math.Pow(z-pad.CenterZ,2);
                    ((JArray)candidate["pads"]).Add(new JObject{["padId"]=pad.PadId,["anchorId"]=pad.AnchorId,["kind"]=(int)kind,
                        ["centerX"]=x,["centerZ"]=z,["seedStructureId"]=structure.StructureId,["hintRect"]=plot.DeepClone()});next.Add(candidate);
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
                var pad=pads.OfType<JObject>().Single(p=>(string)p["PadId"]==(string)preference["padId"]&&(int)p["FactionId"]==2);
                Require((string)pad["AnchorId"]==(string)preference["anchorId"]&&(int)pad["Kind"]==(int)preference["kind"],"Typed preference ownership cannot change");
                pad["CenterX"]=preference["centerX"];pad["CenterZ"]=preference["centerZ"];
            }
            // Existing explicit pad centres are normal preferences, independent
            // of their stable owner-anchor positions. No macro anchor moves here.
            JsonUtility.FromJsonOverwrite(data.ToString(Newtonsoft.Json.Formatting.None),layout);
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
