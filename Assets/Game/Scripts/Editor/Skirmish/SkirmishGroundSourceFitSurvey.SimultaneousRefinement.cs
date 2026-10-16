#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static void RunGround30SimultaneousLogicalRefinement()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle checked Editor required");
            string id=Environment.GetEnvironmentVariable("WARLINE_GROUND30_REFINEMENT_CATALOG");
            string output=Environment.GetEnvironmentVariable("WARLINE_GROUND30_REFINEMENT_OUTPUT");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),"Fresh persistent refinement output required");
            var envelope=ReadGround30Envelope();var missions=((JArray)envelope["missions"]).OfType<JObject>().Where(m=>(string)m["catalog_id"]==id).ToArray();
            Require(missions.Length==1,"One exact pinned Ground30 catalog ID required");
            var warm=Ground30PrewarmCompanions();var scenes=Ground30SceneOwnership();var worlds=CaptureMaterializationWorldCensus();
            var defaultWorld=Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var report=new JObject{["schema"]=1,["scope"]="NormalAllocatorSimultaneousLogicalAuthoring_NotMissionAcceptance",
                ["inputSha256"]=Hash(GroundObjective30Inputs),["catalogId"]=id,["missions"]=new JArray(),["companionPrewarm"]=warm,["refinementOutputPrefix"]=output+".authoring",
                ["sourceFitCertified"]=false,["cameraAccepted"]=false,["result"]="Incomplete"};
            try
            {
                CollectGround30Source(missions,report,true);
                Require(worlds.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(defaultWorld,Unity.Entities.World.DefaultGameObjectInjectionWorld)&&
                    JToken.DeepEquals(scenes,Ground30SceneOwnership()),"Exact refinement World/default/scene/root cleanup required");
                report["cleanupVerified"]=true;report["result"]="CollectedUnqualified";
            }
            catch(Exception failure){report["error"]=failure.ToString();throw;}
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                {writer.Write(report.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            }
            Debug.Log("[Ground30SimultaneousLogicalRefinement] result=Collected catalog="+id+" sourceFitCertified=0 output="+output);
        }

        // Caller owns one already-loaded exact source world and archives returned
        // facts after each mission. No Play world, Assets or Resources are written.
        // All failed passes remain in output. This is an authoring proposal, not
        // promotion of an unconstrained fallback to a valid starting placement.
        public static JObject RefineGround30LoadedMission(JObject mission,OperationMapDefinition source,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,string artifactPrefix=null)
        {
            Require(TryCreateGroundObjective30Candidate(mission,source,out var owner,out string error),error);
            using(owner)return RefineLoadedCandidate(mission,owner,source,em,gridEntity,grid,surface,artifactPrefix);
        }

        // The caller owns definition/layout lifetime. This core creates only
        // proposal metadata and uses the same native allocator for every family.
        static JObject RefineLoadedCandidateCore(JObject mission,GroundObjective30CandidateOwner owner,OperationMapDefinition source,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,string artifactPrefix=null,Action<JObject> checkpoint=null,JObject compoundOverride=null,bool requireExactJointRoutes=false,bool requireExactJointBaseline=false,bool preparedAuthoring=false)
        {
            using var live=em.CreateEntityQuery(typeof(SkirmishMatchState));
            Require(live.IsEmptyIgnoreFilter,"Refinement requires disposable source world, never live gameplay");
            Require(owner!=null&&owner.Definition!=null&&owner.Layout!=null&&source!=null&&
                owner.Definition.CampaignMap==source&&owner.Definition.MapLayout==owner.Layout&&
                owner.Layout.OperationMapId==source.OperationMapId&&owner.Layout.WorldBinding.SourceHash==source.ContentHash&&
                owner.Definition.CatalogId==(string)mission["catalog_id"],"Exact caller-owned source/definition/layout identity required");
            string error;
            {
                if(!preparedAuthoring)
                {AddRefinementDefenseApproaches(owner.Definition,grid);RebuildRefinementConvoyBays(owner.Definition,em,grid);}
                Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out error),error);
                var budgets=new List<SkirmishStaticAuthoringBudget>();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                        budget.ExactCanonicalQuantities,"Exact canonical typed budget required: "+string.Join(";",reasons));
                    budgets.Add(budget);
                }
                var passes=new JArray();
                var output=new JObject{["catalogId"]=mission["catalog_id"],["sourcePins"]=mission["source_candidate"].DeepClone(),
                    ["passes"]=passes,["sourceFitCertified"]=false,["cameraAccepted"]=false,["normalMissionReady"]=false};
                output["defenderApproachFireSafety"]=new JObject{
                    ["status"]="Pending_ActualResolvedTowerWeaponRangeLineOfFireAndPlayerStartZoneValidation",
                    ["unavoidableStartZoneFireExcluded"]=false,
                    ["requirement"]="Both extra defender towers must avoid unavoidable fire into the player start zone; logical envelope placement alone is insufficient"};
                var baselineRoutes=new JArray();var routeClearance=new List<RectInt>();var distinctClearance=new HashSet<RectInt>();
                foreach(var budget in budgets)
                {
                    var routes=Ground30RouteQueries(budget,owner.Layout,em,gridEntity,grid,surface,new JArray());
                    baselineRoutes.Add(routes);
                    foreach(JObject query in (JArray)routes["queries"])
                    {
                        if((string)query["status"]!="NativeQueryFoundExactGoal"||(int?)query["failureCode"]!=0)
                        {output["approachEndpointOrGeometryUnresolved"]=true;continue;}
                        var footprint=new int2((int)query["footprintX"],(int)query["footprintZ"]);
                        foreach(JArray point in (JArray)query["pathCells"])
                        {
                            var cell=new int2((int)point[0],(int)point[1]);bool entryZone=false;
                            foreach(byte side in new byte[]{1,2})
                            {
                                Require(owner.Layout.TryGetPad(side,SkirmishLegalPadKind.GroundStaging,out var staging),"Actual entry pad required");
                                var zone=PilotExactAxisAlignedPadCells(owner.Layout,staging,grid);
                                // Exact authored entry pad only, clipped to16 cells
                                // around its actual center. No broad endpoint waiver.
                                int zx=zone.x+zone.width/2,zz=zone.y+zone.height/2;
                                entryZone|=zone.Contains(new Vector2Int(cell.x,cell.y))&&math.abs(cell.x-zx)<=16&&math.abs(cell.y-zz)<=16;
                            }
                            if(entryZone)continue;
                            var min=UnitFootprintUtility.GetMinCell(cell,footprint);
                            var rect=new RectInt(min.x,min.y,footprint.x,footprint.y);
                            if(distinctClearance.Add(rect))routeClearance.Add(rect);
                        }
                    }
                }
                output["baselineNativeApproaches"]=baselineRoutes;
                output["approachFootprintReservations"]=new JArray(routeClearance.Select(RectJson));
                if(requireExactJointBaseline&&baselineRoutes.OfType<JObject>().Any(r=>!JointRoutesExact(r)))
                {output["jointRouteRefused"]="Actual all-profile baseline contains nonexact native path; no facility search or fit claim";return output;}
                if(refinementFacilityPreferencesRequested&&!refinementPreferenceReplay)
                    return RefineFacilityPreferences(mission,owner,source,em,gridEntity,grid,surface,budgets,(JArray)output["baselineNativeApproaches"],artifactPrefix,checkpoint);
                // Discovery can relocate the entire roster inside declared logical
                // compounds. Derived hulls must then pass the ordinary strict-pad
                // allocator and complete army occupancy on another pass.
                var originalCompounds=compoundOverride??CandidateCompoundRects((JObject)mission["layout_input"]["pilot_candidate"]);
                string originalLogicalFrame=JsonUtility.ToJson(owner.Layout);
                if(refinementLogicalFrameSearchRequested)
                {
                    Require(!ProtectedRefinementCatalog(owner.Definition.CatalogId),"Protected coded layout cannot be widened");
                    output["logicalFrameSearchScope"]="Current_Plus32_Plus64_Plus128_ExistingPhysicalSourceOnly";
                    output["originalLogicalFrameJson"]=originalLogicalFrame;
                }
                var activeCompounds=(JObject)originalCompounds.DeepClone();
                int compoundAttempt=0,strictPasses=0;bool discoveryPending=true;
                for(int pass=0;pass<(refinementCompoundSearchRequested?6:3);pass++)
                {
                    bool discovery=discoveryPending;bool complete=true,incompleteFacilities=false;
                    var sizes=new JArray();passes.Add(new JObject{["pass"]=pass,["discoveryOnly"]=discovery,["sizes"]=sizes});
                    foreach(var budget in budgets)
                    {
                        var row=new JObject{["catalogId"]=mission["catalog_id"],["size"]=budget.Size.ToString(),
                            ["group"]=mission["collector_source_group"],["newLogicalCandidateMaterialized"]=true,
                            ["candidateCompoundWorldRects"]=activeCompounds.DeepClone(),
                            ["originalCandidateCompoundWorldRects"]=originalCompounds.DeepClone(),
                            ["boundedLogicalCompoundSearch"]=refinementCompoundSearchRequested,["compoundEnvelopeAttempt"]=compoundAttempt,
                            ["requestedCompoundExpansionMetres"]=refinementLogicalFrameSearchRequested?RefinementLogicalFrameMargins[compoundAttempt]:RefinementCompoundMargins[compoundAttempt],
                            ["logicalFrameSearch"]=refinementLogicalFrameSearchRequested,
                            ["actualLogicalFrameJson"]=refinementLogicalFrameSearchRequested?JsonUtility.ToJson(owner.Layout):null,
                            ["allSizeFacilities"]=Ground30PreferredFacilities(budget,owner.Layout,grid)};
                        sizes.Add(row);
                        try
                        {
                        ProbePilotNormalFacilityRelocation(new JObject(),System.Array.Empty<JObject>(),owner.Layout,owner.Definition.RoleCatalog,
                            em,gridEntity,grid,surface,row,true,discovery,routeClearance);
                        var facilities=(JArray)row["normalFacilityRelocationProposals"];
                        bool allFacilities=facilities!=null&&facilities.Count==budget.Structures.Length&&
                            facilities.All(f=>(bool?)f["entireConfigurationFound"]==true);
                        row["completeFacilities"]=allFacilities;
                        if(!allFacilities){complete=false;incompleteFacilities=true;row["packingStatus"]="NotQueried_IncompleteFacilities";continue;}
                        using var typedPlacement=new Game.Runtime.SkirmishTypedSpawnConstraint(em,gridEntity,grid,owner.Layout);
                        var packing=Ground30PackBudget(budget,owner.Layout,em,gridEntity,grid,surface,facilities,typedPlacement);
                        row["postFacilityForcePacking"]=packing;
                        bool allActors=packing.Count==budget.Forces.Length&&packing.OfType<JObject>().All(f=>
                            (int?)f["foundCount"]==(int?)f["requestedCount"]&&
                            (int?)f["surfaceTraversableCount"]==(int?)f["requestedCount"]&&
                            (int?)f["insideTypedPadCount"]==(int?)f["requestedCount"]);
                        row["allActorsInsidePads"]=allActors;complete&=allActors;
                        row["routeQueriesWithSameFacilityReservations"]=Ground30RouteQueries(budget,owner.Layout,em,gridEntity,grid,surface,facilities);
                        if(requireExactJointRoutes&&!JointRoutesExact((JObject)row["routeQueriesWithSameFacilityReservations"]))
                        {row["jointRouteRefused"]="Nonexact native route with actual facility blockers";complete=false;}
                        if(refinementPreferenceReplay)
                        {
                            bool independent=PreferencePathsIndependent(owner.Layout,grid,(JObject)row["routeQueriesWithSameFacilityReservations"],out var independence);
                            row["actualProfileRouteIndependence"]=independence;
                            if(!independent)complete=false;
                        }
                        }
                        catch(Exception failure){row["sizeError"]=failure.ToString();throw;}
                        finally{checkpoint?.Invoke(new JObject{["catalogId"]=mission["catalog_id"],["pass"]=pass,["discoveryOnly"]=discovery,["size"]=budget.Size.ToString(),["facts"]=row.DeepClone()});}
                    }
                    if(!discovery&&complete)
                    {
                        output["strictThreeSizePlacementPass"]=pass;
                        try{FinalizeRefinementAuthoring(owner,em,grid,surface,output,artifactPrefix);}
                        catch(Exception failure){output["logicalFinalizationUnresolved"]=failure.ToString();}
                        // Raw paths are still subject to independence/merge-point,
                        // service circulation and real live occupancy validation.
                        break;
                    }
                    if(refinementCompoundSearchRequested&&discovery&&incompleteFacilities)
                    {
                        bool expanded=refinementLogicalFrameSearchRequested
                            ?TryNextRefinementLogicalFrame(originalLogicalFrame,originalCompounds,compoundAttempt+1,owner.Layout,source,grid,out var nextCompounds,out string searchError)
                            :TryExpandedRefinementCompounds(originalCompounds,compoundAttempt+1,owner.Layout,source,grid,out nextCompounds,out searchError);
                        if(expanded)
                        {
                            compoundAttempt++;activeCompounds=nextCompounds;
                            output["boundedCompoundSearchStatus"]=refinementLogicalFrameSearchRequested
                                ?"RetryingDiscoveryWithinExplicitWidenedLogicalSourceAndGridBounds"
                                :"RetryingDiscoveryWithinCurrentLogicalSourceAndGridBounds";
                            continue;
                        }
                        output["boundedCompoundSearchStatus"]="ExhaustedOrRefused_NoFitClaim";
                        output["boundedCompoundSearchRefusal"]=searchError;
                    }
                    if(!TryApplyRefinementHulls(owner.Layout,sizes,grid,out string hullError))
                    {output["unresolved"]=hullError;break;}
                    discoveryPending=false;
                    if(!discovery&&++strictPasses>=2)break;
                }
                output["proposedLayoutJson"]=JsonUtility.ToJson(owner.Layout);
                output["proposedLayoutSemanticSha256"]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(owner.Layout);
                output["pending"]=new JArray("Exact three independent approaches/no shared interior bottleneck",
                    "Full compound/service circulation and live occupancy","Authored source-bound engineering view and definition logical binding",
                    "Actual defender tower weapon/range/line-of-fire against all player start zones",
                    "Native default/min/max infantry and parked/cruising aircraft camera evidence","Normal mission and device acceptance");
                if(output["strictThreeSizePlacementPass"]==null)
                    Require(!owner.Layout.WorldBinding.HasMeasuredDeployments&&!owner.Layout.WorldBinding.BindAllConfigurations,
                        "Incomplete placement must not manufacture measured-deployment flags");
                return output;
            }
        }

        static bool TryApplyRefinementHulls(SkirmishMapLayoutConfig layout,JArray sizes,GridConfig grid,out string error)
        {
            error=null;var observed=new Dictionary<string,List<RectInt>>(StringComparer.Ordinal);
            void Add(string id,RectInt rect)
            {if(!observed.TryGetValue(id,out var list))observed.Add(id,list=new List<RectInt>());list.Add(rect);}
            foreach(JObject size in sizes)
            {
                if((bool?)size["completeFacilities"]!=true){error="Cannot derive complete all-size hulls from partial facility allocation";return false;}
                foreach(JObject facility in (JArray)size["normalFacilityRelocationProposals"])
                {
                    var r=(JObject)facility["resolvedEffectiveRect"];
                    Add((string)facility["padId"],new RectInt((int)r["x"],(int)r["z"],(int)r["width"],(int)r["depth"]));
                }
                foreach(JObject force in (JArray)size["postFacilityForcePacking"])
                {
                    if((int?)force["foundCount"]!=(int?)force["requestedCount"]||
                        (int?)force["surfaceTraversableCount"]!=(int?)force["requestedCount"])
                    {error="Cannot derive army hull from missing or nontraversable actual members";return false;}
                    var footprint=new int2((int)force["footprintX"],(int)force["footprintZ"]);
                    foreach(JObject cell in (JArray)force["candidateCells"])
                    {
                        var min=UnitFootprintUtility.GetMinCell(new int2((int)cell["x"],(int)cell["z"]),footprint);
                        Add((string)force["padId"],new RectInt(min.x,min.y,footprint.x,footprint.y));
                    }
                }
            }
            var encoded=new JArray();
            var anchors=new JArray(layout.Anchors.Select(EncodePilotAnchor));
            foreach(var pad in layout.Pads)
            {
                var json=EncodePilotPad(pad);encoded.Add(json);
                if(!observed.TryGetValue(pad.PadId,out var rects))continue;
                // Refuse rotated/scaled frames; existing helper proves the exact
                // source-axis metre convention before using these native cells.
                var original=PilotExactAxisAlignedPadCells(layout,pad,grid);
                // Keep the actual Service interaction anchor/area. Its dimensions
                // may expand symmetrically, but the repair point never moves.
                bool spawn=pad.Kind==SkirmishLegalPadKind.InfantrySpawn||pad.Kind==SkirmishLegalPadKind.VehicleSpawn||pad.Kind==SkirmishLegalPadKind.ObjectiveTruckSpawn||pad.Kind==SkirmishLegalPadKind.AirReturn;
                if(pad.Kind==SkirmishLegalPadKind.Service||spawn)rects.Add(original);
                int left=rects.Min(r=>r.xMin),right=rects.Max(r=>r.xMax),bottom=rects.Min(r=>r.yMin),top=rects.Max(r=>r.yMax);
                Require(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float oldX,out float oldZ),"Actual pad projection required");
                float worldX=grid.Origin.x+(left+right)*.5f*grid.CellSize,worldZ=grid.Origin.z+(bottom+top)*.5f*grid.CellSize;
                if(pad.Kind==SkirmishLegalPadKind.Service||spawn)
                {
                    json["WidthMetres"]=math.max(pad.WidthMetres,2*math.max(oldX-(grid.Origin.x+left*grid.CellSize),grid.Origin.x+right*grid.CellSize-oldX));
                    json["DepthMetres"]=math.max(pad.DepthMetres,2*math.max(oldZ-(grid.Origin.z+bottom*grid.CellSize),grid.Origin.z+top*grid.CellSize-oldZ));
                }
                else
                {
                    json["CenterX"]=pad.CenterX+worldX-oldX;json["CenterZ"]=pad.CenterZ+worldZ-oldZ;
                    json["WidthMetres"]=(right-left)*grid.CellSize;json["DepthMetres"]=(top-bottom)*grid.CellSize;
                    {
                        var anchor=anchors.OfType<Newtonsoft.Json.Linq.JObject>().Single(a=>(string)a["AnchorId"]==pad.AnchorId);
                        anchor["WorldX"]=json["CenterX"].DeepClone();anchor["WorldZ"]=json["CenterZ"].DeepClone();
                        anchor["NormalizedU"]=((float)json["CenterX"]-layout.OriginX)/layout.WorldWidthMetres;
                        anchor["NormalizedV"]=((float)json["CenterZ"]-layout.OriginZ)/layout.WorldDepthMetres;
                    }
                }
            }
            JsonUtility.FromJsonOverwrite(new JObject{["pads"]=encoded,["anchors"]=anchors}.ToString(Newtonsoft.Json.Formatting.None),layout);
            return true;
        }
    }
}
#endif
