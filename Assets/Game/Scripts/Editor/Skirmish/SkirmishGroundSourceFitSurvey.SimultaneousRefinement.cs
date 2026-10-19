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
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,string artifactPrefix=null,Action<JObject> checkpoint=null,JObject compoundOverride=null,bool requireExactJointRoutes=false,bool requireExactJointBaseline=false,bool preparedAuthoring=false,Action<SkirmishMapLayoutConfig> strictCandidatePreparation=null)
        {
            using var live=em.CreateEntityQuery(typeof(SkirmishMatchState));
            Require(live.IsEmptyIgnoreFilter,"Refinement requires disposable source world, never live gameplay");
            Require(owner!=null&&owner.Definition!=null&&owner.Layout!=null&&source!=null&&
                owner.Definition.CampaignMap==source&&owner.Definition.MapLayout==owner.Layout&&
                owner.Layout.OperationMapId==source.OperationMapId&&owner.Layout.WorldBinding.SourceHash==source.ContentHash&&
                owner.Definition.CatalogId==(string)mission["catalog_id"],"Exact caller-owned source/definition/layout identity required");
            bool apPairServiceRequested=ApPairServiceAuthority(owner.Definition);
            if(apPairServiceRequested)
                Require(grid.Width==source.GridMetadata.Dimensions.x&&grid.Height==source.GridMetadata.Dimensions.y&&
                    grid.CellSize==source.GridMetadata.CellSize&&math.all(grid.Origin==(float3)source.GridMetadata.Origin),
                    "AP service queries require actual pinned source grid metadata");
            string error;
            JObject mpEndpointRevision=null;
            if(mpGroundBaPilotRequested)mpEndpointRevision=ApplyMpPilotEndpointRevision(owner.Definition,grid,preparedAuthoring);
            JObject s049MainRouteRevision=s049BoundedCompoundRouteRequested?ApplyS049NativeCorridorMainSegments(owner.Definition,grid):null;
            if(strictCandidatePreparation!=null)
            {
                Require(preparedAuthoring&&!ProtectedRefinementCatalog(owner.Definition.CatalogId),
                    "Explicit disposable logical authoring candidate required");
                strictCandidatePreparation(owner.Layout);
                Require(owner.Definition.MapLayout==owner.Layout&&owner.Definition.CampaignMap==source&&
                    owner.Layout.OperationMapId==source.OperationMapId&&owner.Layout.WorldBinding.SourceHash==source.ContentHash&&
                    !owner.Layout.WorldBinding.HasMeasuredDeployments&&!owner.Layout.WorldBinding.BindAllConfigurations,
                    "Strict candidate must preserve existing source and unqualified deployment authority");
            }
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
                if(mpEndpointRevision!=null)output["mpPilotLogicalEndpointRevision"]=mpEndpointRevision;
                if(s049MainRouteRevision!=null)output["s049ActualMainRouteSegmentation"]=s049MainRouteRevision;
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
                int compoundAttempt=0,strictPasses=0;bool discoveryPending=strictCandidatePreparation==null;
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
                            em,gridEntity,grid,surface,row,true,discovery,routeClearance,serviceAuthority:(mpGroundBaPilotRequested||apPairServiceRequested)?owner.Definition:null);
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
                        if(s049BoundedCompoundRouteRequested)row["s049OriginalCentralLegDiagnostic"]=ProbeS049OriginalCentralLegControls(budget,em,gridEntity,grid,surface,facilities);
                        if((bool?)row["routeQueriesWithSameFacilityReservations"]?["endpointPolicyPreflight"]?["accepted"]!=true)
                        {row["jointRouteRefused"]="Authored endpoint policy preflight refused; no native route queries";complete=false;}

                        if((requireExactJointRoutes||apPairServiceRequested)&&!JointRoutesExact((JObject)row["routeQueriesWithSameFacilityReservations"]))
                        {row["jointRouteRefused"]="Nonexact native route with actual facility blockers";complete=false;}
                        if(refinementPreferenceReplay||mpGroundBaPilotRequested||apPairServiceRequested)
                        {
                            bool independent=PreferencePathsIndependent(owner.Layout,grid,(JObject)row["routeQueriesWithSameFacilityReservations"],out var independence);
                            row["actualProfileRouteIndependence"]=independence;
                            if(!independent)complete=false;
                        }
                        if(mpGroundBaPilotRequested||apPairServiceRequested)
                        {
                            bool coverage=MpPilotCanonicalRouteCoverage(budget,owner.Layout,em,grid,(JObject)row["routeQueriesWithSameFacilityReservations"]);
                            row["mpPilotCanonicalRouteCoverage"]=coverage;
                            // Discovery must derive strict typed hulls before service admission.
                            var service=!discovery&&allActors
                                ?MpPilotServiceAccess(budget,owner.Definition,owner.Layout,em,gridEntity,grid,surface,facilities,packing)
                                :new JObject{["allMembersHaveExactServiceIngressAndReturn"]=false,["status"]="DeferredUntilStrictCompleteActorPacking"};
                            // Legacy receipt field retained for the existing strict auditor.
                            // Catalog/size/authority comes from this actual AP or MP owner.
                            row["mpPilotServiceAccess"]=service;
                            if(apPairServiceRequested)row["apPairStaticServiceScope"]=new JObject{
                                ["catalogId"]=owner.Definition.CatalogId,["sourceIdentityHash"]=source.SourceIdentityHash,
                                ["contentHash"]=source.ContentHash,["sourceFitCertified"]=false,
                                ["normalPaidRepairCertified"]=false,["liveOccupancyIncluded"]=false};
                            if(!coverage||(bool?)service["allMembersHaveExactServiceIngressAndReturn"]!=true)complete=false;
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
                    if(strictCandidatePreparation!=null)
                    {
                        output["strictCandidateRefused"]="Actual strict all-size gates failed; immutable candidate is not reanchored or promoted";
                        break;
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
                // Preserve every canonical typed structure/force preference and endpoint.
                // A shared-pad reanchor otherwise changes all structure offsets on replay.
                json["WidthMetres"]=math.max(pad.WidthMetres,2*math.max(oldX-(grid.Origin.x+left*grid.CellSize),grid.Origin.x+right*grid.CellSize-oldX));
                json["DepthMetres"]=math.max(pad.DepthMetres,2*math.max(oldZ-(grid.Origin.z+bottom*grid.CellSize),grid.Origin.z+top*grid.CellSize-oldZ));
            }
            // Native pad membership rounds inward. Verify actual float projection/cell
            // roundtrip on a disposable clone; never relax the runtime rounding predicate.
            var trial=UnityEngine.Object.Instantiate(layout);
            try
            {
                for(int attempt=0;attempt<2;attempt++)
                {
                    JsonUtility.FromJsonOverwrite(new JObject{["pads"]=encoded}.ToString(Newtonsoft.Json.Formatting.None),trial);
                    bool contained=true;
                    foreach(var pad in trial.Pads)
                    {
                        if(!observed.TryGetValue(pad.PadId,out var rects))continue;
                        if(pad.CenterX-pad.WidthMetres*.5f<trial.OriginX||pad.CenterZ-pad.DepthMetres*.5f<trial.OriginZ||
                            pad.CenterX+pad.WidthMetres*.5f>trial.OriginX+trial.WorldWidthMetres||
                            pad.CenterZ+pad.DepthMetres*.5f>trial.OriginZ+trial.WorldDepthMetres)
                        {error="Fixed-origin logical hull exceeds unchanged authored frame: "+pad.PadId;return false;}
                        foreach(int xs in new[]{-1,1})foreach(int zs in new[]{-1,1})
                        {
                            if(!trial.TryProjectLocalToMap(pad.CenterX+xs*pad.WidthMetres*.5f,pad.CenterZ+zs*pad.DepthMetres*.5f,
                                out float cornerX,out float cornerZ)||!math.isfinite(cornerX)||!math.isfinite(cornerZ)||
                                cornerX<grid.Origin.x||cornerZ<grid.Origin.z||
                                cornerX>grid.Origin.x+grid.Width*grid.CellSize||cornerZ>grid.Origin.z+grid.Height*grid.CellSize)
                            {error="Logical hull exceeds actual native grid corners: "+pad.PadId;return false;}
                        }
                        var cells=PilotExactAxisAlignedPadCells(trial,pad,grid);
                        if(cells.xMin<0||cells.yMin<0||cells.xMax>grid.Width||cells.yMax>grid.Height)
                        {error="Logical hull exceeds actual native grid: "+pad.PadId;return false;}
                        if(rects.All(r=>PilotCompoundContains(cells,r)))continue;
                        if(attempt!=0){error="Logical hull projection roundtrip lost observed cells: "+pad.PadId;return false;}
                        // One finite authoring-only cell per side, then exact recheck.
                        // Source geometry, allocation constraints and native gates are unchanged.
                        var json=encoded.OfType<JObject>().Single(p=>(string)p["PadId"]==pad.PadId);
                        json["WidthMetres"]=pad.WidthMetres+2*grid.CellSize;
                        json["DepthMetres"]=pad.DepthMetres+2*grid.CellSize;
                        contained=false;
                    }
                    if(!contained)continue;
                    JsonUtility.FromJsonOverwrite(new JObject{["pads"]=encoded}.ToString(Newtonsoft.Json.Formatting.None),layout);
                    return true;
                }
                error="Bounded logical hull roundtrip did not converge";return false;
            }
            finally{UnityEngine.Object.DestroyImmediate(trial);}
        }

        public static void RunRefinementHullRoundtripControls()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,
                "Idle owned authoring controls required");
            int controls=0;
            var owned=new List<SkirmishMapLayoutConfig>();
            var grid=new GridConfig{Width=4096,Height=2048,CellSize=.5f,Origin=new float3(10.25f,0,20.75f)};
            SkirmishMapLayoutConfig Fixture()
            {
                var layout=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();owned.Add(layout);
                var pads=new JArray();var anchors=new JArray();
                var kinds=new[]{SkirmishLegalPadKind.GroundStaging,SkirmishLegalPadKind.Supply,SkirmishLegalPadKind.Service,SkirmishLegalPadKind.InfantrySpawn};
                var centers=new[]{new Vector2(110.25f,120.75003f),new Vector2(220.25f,220.75f),new Vector2(300.25f,200.75f),new Vector2(400.25f,250.75f)};
                for(int i=0;i<kinds.Length;i++)
                {
                    pads.Add(new JObject{["PadId"]="hull.pad."+i,["AnchorId"]="hull.anchor."+i,["RoleId"]="hull.role."+i,
                        ["Kind"]=(int)kinds[i],["FactionId"]=2,["CenterX"]=centers[i].x,["CenterZ"]=centers[i].y,
                        ["WidthMetres"]=i==0?18:10,["DepthMetres"]=i==0?6:10});
                    anchors.Add(new JObject{["AnchorId"]="hull.anchor."+i,["RoleId"]="hull.role."+i,
                        ["WorldX"]=centers[i].x,["WorldZ"]=centers[i].y,["NormalizedU"]=centers[i].x/2048,["NormalizedV"]=centers[i].y/1024});
                }
                JsonUtility.FromJsonOverwrite(new JObject{["layoutId"]="hull.controls",["operationMapId"]="existing.source.control",
                    ["contentVersion"]=2,["contentHash"]="unchanged-control",["originX"]=0,["originZ"]=0,
                    ["worldWidthMetres"]=2048,["worldDepthMetres"]=1024,["cellSize"]=.5,
                    ["pads"]=pads,["anchors"]=anchors,["routes"]=new JArray(new JObject{["RouteId"]="hull.route",
                        ["WaypointAnchorIds"]=new JArray("hull.anchor.0","hull.anchor.1"),["WidthMetres"]=8,["AllowsVehicles"]=true}),
                    ["worldBinding"]=new JObject{["OriginX"]=0,["OriginZ"]=512,["ForwardX"]=1,["ForwardZ"]=0,
                        ["AcrossX"]=0,["AcrossZ"]=1,["ForwardMetres"]=2048,["AcrossMetres"]=1024,
                        ["PlayerAnchorId"]="hull.anchor.0",["EnemyAnchorId"]="hull.anchor.1",["SourceHash"]=new string('a',64)}}.ToString(),layout);
                return layout;
            }
            JArray Sizes()
            {
                var result=new JArray();
                for(int i=0;i<3;i++)
                    result.Add(new JObject{["size"]=new[]{"Standard","War","LargeWar"}[i],["completeFacilities"]=true,
                        ["normalFacilityRelocationProposals"]=new JArray(
                            new JObject{["padId"]="hull.pad.0",["resolvedEffectiveRect"]=RectJson(new RectInt(180-i,180-i,14+i,7+i))},
                            new JObject{["padId"]="hull.pad.1",["resolvedEffectiveRect"]=RectJson(new RectInt(360-i,360,15,4))},
                            new JObject{["padId"]="hull.pad.1",["resolvedEffectiveRect"]=RectJson(new RectInt(375,350-i,25+i,15+i))}),
                        ["postFacilityForcePacking"]=new JArray(
                            new JObject{["padId"]="hull.pad.2",["requestedCount"]=1,["foundCount"]=1,["surfaceTraversableCount"]=1,
                                ["footprintX"]=2,["footprintZ"]=2,["candidateCells"]=new JArray(new JObject{["x"]=600+i,["z"]=360})},
                            new JObject{["padId"]="hull.pad.3",["requestedCount"]=1,["foundCount"]=1,["surfaceTraversableCount"]=1,
                                ["footprintX"]=1,["footprintZ"]=1,["candidateCells"]=new JArray(new JObject{["x"]=800+i,["z"]=460})})});
                return result;
            }
            JObject Invariants(SkirmishMapLayoutConfig layout)
            {
                var j=JObject.Parse(JsonUtility.ToJson(layout));j.Remove("pads");
                return j;
            }
            JArray Preferences(SkirmishMapLayoutConfig layout)
            {
                var result=new JArray();
                foreach(var structure in new[]{
                    new SkirmishResolvedStructureEntry{FactionId=2,StructureId=SkirmishStructureIds.OilPump,AcrossOffsetMetres=-12,ForwardOffsetMetres=12},
                    new SkirmishResolvedStructureEntry{FactionId=2,StructureId=SkirmishStructureIds.Refinery,AcrossOffsetMetres=12,ForwardOffsetMetres=-12}})
                {
                    Require(Game.Configs.SkirmishMapLayoutBuilder.TryPadForStructure(layout,structure,out var pad),"Actual shared supply owner required");
                    Require(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z),"Actual canonical preference projection required");
                    Game.Configs.SkirmishMapLayoutBuilder.ApplyFrameOffset(layout,structure.AcrossOffsetMetres,structure.ForwardOffsetMetres,ref x,ref z);
                    result.Add(new JObject{["structureId"]=structure.StructureId,["x"]=x,["z"]=z,["across"]=structure.AcrossOffsetMetres,["forward"]=structure.ForwardOffsetMetres});
                }
                return result;
            }
            void Refuse(Action<SkirmishMapLayoutConfig,JArray> mutate,string expected)
            {
                var layout=Fixture();var sizes=Sizes();mutate(layout,sizes);
                string before=JsonUtility.ToJson(layout);
                Require(!TryApplyRefinementHulls(layout,sizes,grid,out string error)&&error!=null&&error.Contains(expected)&&before==JsonUtility.ToJson(layout),
                    "Actual refused hull must leave caller unchanged: "+expected);controls++;
            }
            try
            {
                var layout=Fixture();var sizes=Sizes();string budget=sizes.ToString(Newtonsoft.Json.Formatting.None);
                var invariant=Invariants(layout);var preferences=Preferences(layout);var centers=layout.Pads.ToArray();
                Require(TryApplyRefinementHulls(layout,sizes,grid,out string error),error);
                foreach(JObject size in sizes)foreach(JObject facility in (JArray)size["normalFacilityRelocationProposals"])
                {
                    var pad=layout.Pads.Single(p=>p.PadId==(string)facility["padId"]);
                    var r=(JObject)facility["resolvedEffectiveRect"];var rect=new RectInt((int)r["x"],(int)r["z"],(int)r["width"],(int)r["depth"]);
                    Require(PilotCompoundContains(PilotExactAxisAlignedPadCells(layout,pad,grid),rect),"All three actual facility-cell unions retained after inward rounding");
                }
                controls++; // fractional center, nonunit cell, nonzero origin and all3 facility union
                foreach(JObject size in sizes)foreach(JObject force in (JArray)size["postFacilityForcePacking"])
                {
                    var pad=layout.Pads.Single(p=>p.PadId==(string)force["padId"]);
                    foreach(JObject cell in (JArray)force["candidateCells"])
                    {
                        var footprint=new int2((int)force["footprintX"],(int)force["footprintZ"]);
                        var min=UnitFootprintUtility.GetMinCell(new int2((int)cell["x"],(int)cell["z"]),footprint);
                        Require(PilotCompoundContains(PilotExactAxisAlignedPadCells(layout,pad,grid),new RectInt(min.x,min.y,footprint.x,footprint.y)),
                            "Actual Service/spawn footprints remain contained");
                    }
                }
                controls++;
                foreach(var prior in centers)
                {
                    var pad=layout.Pads.Single(p=>p.PadId==prior.PadId);
                    Require(pad.CenterX==prior.CenterX&&pad.CenterZ==prior.CenterZ&&pad.AnchorId==prior.AnchorId&&pad.Kind==prior.Kind&&
                        pad.FactionId==prior.FactionId&&pad.RoleId==prior.RoleId,"Service/spawn/nonservice ownership and centers immutable");
                }
                controls++;
                Require(JToken.DeepEquals(preferences,Preferences(layout)),"Actual builder canonical shared-pad structure offsets/preferences immutable");controls++;
                Require(JToken.DeepEquals(invariant,Invariants(layout))&&budget==sizes.ToString(Newtonsoft.Json.Formatting.None),
                    "Source/frame/anchors/routes/canonical quantities and evidence remain unchanged");controls++;
                Refuse((l,s)=>((JObject)s[0]["normalFacilityRelocationProposals"][0]["resolvedEffectiveRect"])["x"]=10000,"authored frame");
                Refuse((l,s)=>{
                    var j=JObject.Parse(JsonUtility.ToJson(l));var p=(JObject)j["pads"][0];
                    p["CenterX"]=10.5f;p["CenterZ"]=120;p["WidthMetres"]=1;p["DepthMetres"]=2;
                    JsonUtility.FromJsonOverwrite(j.ToString(),l);
                    foreach(JObject row in s)row["normalFacilityRelocationProposals"]=new JArray(new JObject{["padId"]="hull.pad.0",
                        ["resolvedEffectiveRect"]=RectJson(new RectInt(0,198,1,1))});
                },"native grid corners"); // inward cells may fit while raw lower corner lies outside10.25
                Refuse((l,s)=>s[1]["completeFacilities"]=false,"partial facility allocation");
                Refuse((l,s)=>s[1]["postFacilityForcePacking"][0]["foundCount"]=0,"missing or nontraversable");
                Refuse((l,s)=>s[1]["postFacilityForcePacking"][0]["surfaceTraversableCount"]=0,"missing or nontraversable");
                var rotated=Fixture();var data=JObject.Parse(JsonUtility.ToJson(rotated));data["worldBinding"]["ForwardX"]=2;
                JsonUtility.FromJsonOverwrite(data.ToString(),rotated);string before=JsonUtility.ToJson(rotated);bool rejected=false;
                try{TryApplyRefinementHulls(rotated,Sizes(),grid,out _);}catch(InvalidOperationException){rejected=true;}
                Require(rejected&&before==JsonUtility.ToJson(rotated),"Scaled frame refuses before source mutation");controls++;
                var replay=Fixture();var old=JObject.Parse(JsonUtility.ToJson(replay));old["pads"][0]["CenterX"]=150.25f;
                JsonUtility.FromJsonOverwrite(old.ToString(),replay);var beforeCenters=replay.Pads.Select(p=>new Vector2(p.CenterX,p.CenterZ)).ToArray();
                Require(TryApplyRefinementHulls(replay,Sizes(),grid,out error)&&replay.Pads.Select(p=>new Vector2(p.CenterX,p.CenterZ)).SequenceEqual(beforeCenters),
                    "Different canonical preferred centers remain fixed; no learned per-SID pose");controls++;
                Require(controls==12,"Exact bounded actual helper control count");
                Debug.Log("[RefinementHullRoundtripControls] result=Passed controls=12 nativeQueries=0 sourceFitCertified=0 missionReady=0");
            }
            finally{foreach(var value in owned)if(value!=null)UnityEngine.Object.DestroyImmediate(value);}
        }

    }
}
#endif
