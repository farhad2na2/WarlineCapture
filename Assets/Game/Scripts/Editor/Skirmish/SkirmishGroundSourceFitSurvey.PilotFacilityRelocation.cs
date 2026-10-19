#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Calls the NORMAL production origin-search owner and actual placement
        // metadata/caches. Static proposals do not stand in for live scene fitting.
        static void ProbePilotNormalFacilityRelocation(JObject input,JObject[] requests,
            SkirmishMapLayoutConfig layout,SkirmishRoleCatalogConfig roles,EntityManager em,
            Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,JObject report,bool exactTypedRosterCaller=false,bool discoverFacilityPads=false,List<RectInt> plannedApproachClearance=null,bool capacityOnly=false,SkirmishScenarioDefinitionConfig serviceAuthority=null)
        {
            string group=(string)report["group"];
            if(group!="MP"&&group!="IB"&&(bool?)report["newLogicalCandidateMaterialized"]!=true)return;
            if(serviceAuthority!=null)Require((mpGroundBaPilotRequested||ApPairServiceAuthority(serviceAuthority)||CompleteGroundBaServiceAuthority(serviceAuthority))&&serviceAuthority.MapLayout==layout&&serviceAuthority.RoleCatalog==roles,
                "Exact MP or authority-bound AP disposable service authoring context required");
            var output=new JArray();report["normalFacilityRelocationProposals"]=output;
            report["normalFacilityRelocationScope"]="ActualProductionSearchAndStaticSourceCaches_NoLiveGameplayCertificate";
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            var cache=new BuildingPlacementInvalidCellCacheCompositionSystemHelper();cache.EnsureAuthoredGeometry(em,surfaces,grid);
            report["normalFacilityPackedGeometryCacheReady"]=cache.AuthoredBuildings.IsReady;
            if(!cache.AuthoredBuildings.IsReady)
            {report["normalFacilityRelocationStatus"]="UnavailableActualPackedOrAuthoredWorldGeometry";return;}
            var walkable=em.GetBuffer<GridWalkable>(gridEntity,true);
            Require(walkable.Length==grid.Width*grid.Height,"Complete actual baked blocking projection required");
            using var blocked=new NativeBitArray(walkable.Length,Allocator.Persistent,NativeArrayOptions.ClearMemory);
            for(int i=0;i<walkable.Length;i++)if(walkable[i].Value==0)blocked.Set(i,true);
            var blocker=new DynamicBlockerComponent{Blocked=blocked};
            report["normalFacilityBlockerScope"]="ConservativeProjectionOfActualBakedWalkability_NotLiveDynamicBlockers";
            report["normalFacilityLiveOccupancyMeasured"]=false;
            var roads=em.GetBuffer<GridRoad>(gridEntity,true);
            Require(roads.Length==walkable.Length,"Complete actual road buffer required");
            var definitions=new BuildingDefinitionPrefabSystemHelper();
            definitions.ConfigureAuthoringMetadataResolvers(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            var runway=new BuildingRunwaySystem();var geometry=new BuildingPlacementGridCameraSystemHelper();
            var search=new BuildingRuntimeSpawnCompositionSystemHelper();
            var dependency=new BuildingGameplayDependencyCompositionSystemHelper();
            var startup=new BuildingPlacementStartupSystemHelper();
            // Immutable source-only early rejection. No typed QA or prior accepted
            // reservation enters this prefix; surviving candidates still call Valid.
            var staticMask=new bool[grid.Width*grid.Height];
            startup.FillRoadFootprintMask(grid,staticMask);
            cache.AuthoredBuildings.AppendTo(staticMask);
            cache.RoadSurfaces.AppendTo(staticMask);
            cache.RoadSurfaces.AppendWaterTo(staticMask);
            int[] staticPrefix=null;
            BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid,roads,blocker,staticMask,
                dependency.IsRuntimeBlockerCell,ref staticPrefix,out int prefixWidth,out int prefixHeight,out bool prefixReady);
            Require(prefixReady,"Actual immutable source prefix required");
            report["normalFacilityStaticInvalidPrefixReady"]=true;
            report["normalFacilityStaticInvalidPrefixScope"]="ExactProbeSourceRoadWaterSceneryWalkability_ExcludesQaAndReservations";
            var sourceRows=((JArray)report["allSizeFacilities"]).OfType<JObject>().ToArray();
            foreach(var configuration in sourceRows.GroupBy(row=>((string)row["catalogId"],(string)row["size"])))
            {
                if(boundedPilotObserver!=null)BoundedPilotCheckpoint("FacilitySizeStarted",new JObject{["catalogId"]=configuration.Key.Item1,["size"]=configuration.Key.Item2});
                var reserved=new List<RectInt>();bool complete=true;
                var serviceFootprints=new Dictionary<byte,List<RectInt>>();var serviceAvailable=new Dictionary<byte,bool>();
                foreach(byte side in new byte[]{1,2})
                {
                    var serviceRows=new JArray();
                    List<RectInt> keepClear;
                    serviceAvailable[side]=serviceAuthority!=null
                        ?MpPilotRepairInteractionFootprints(serviceAuthority,layout,em,gridEntity,grid,surface,side,out keepClear,serviceRows)
                        :PilotRepairInteractionFootprints(layout,em,gridEntity,grid,surface,side,out keepClear,serviceRows);
                    serviceFootprints[side]=keepClear;
                    bool support=configuration.Any(value=>(byte)value["faction"]==side&&(string)value["structureId"]==Game.Skirmish.Contracts.SkirmishStructureIds.GroundStaging);
                    serviceAvailable[side]&=support;
                    if(report["nativeRepairInteractionClearance"]==null)report["nativeRepairInteractionClearance"]=new JArray();
                    ((JArray)report["nativeRepairInteractionClearance"]).Add(new JObject{["catalogId"]=configuration.Key.Item1,["size"]=configuration.Key.Item2,
                        ["faction"]=side,["requiredNormalGroundStagingInCanonicalConfiguration"]=support,["allStaticFootprintsAvailableAndLegal"]=serviceAvailable[side],
                        ["roles"]=serviceRows,["paidRepairOrLiveOccupancyCertified"]=false});
                }
                foreach(var preferred in configuration)
                {
                    var result=(JObject)preferred.DeepClone();output.Add(result);
                    result["preferredFootprintMeasurement"]=preferred.DeepClone();
                    foreach(string field in new[]{"status","x","z","originX","originZ","widthCells","depthCells","insideGrid",
                        "foundationLevel","roadCells","sidewalkCells","waterCells","blockedGridCells","packedSceneryOverlap"})result.Remove(field);
                    result["status"]="NativeNormalSearchNotCompleted";result["sourceFitCertified"]=false;
                    string path=(string)preferred["prefabPath"];
                    if(string.IsNullOrEmpty(path)||Hash(path)!=(string)preferred["prefabSha256"])
                    {result["status"]="UnavailableExactNativePrefab";complete=false;continue;}
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Require(prefab!=null&&!EditorUtility.IsDirty(prefab),"Clean actual facility prefab required");
                    var definition=definitions.CreateRuntimeBuildingDefinition(prefab,prefab.name,"",Vector2Int.one,1,runway);
                    byte faction=(byte)preferred["faction"];
                    float cx=0,cz=0;
                    Require(layout.TryGetPad(faction,SkirmishLegalPadKind.GroundStaging,out var staging)&&
                        layout.TryProjectLocalToMap(staging.CenterX,staging.CenterZ,out cx,out cz),"Actual compound frame required");
                    // Original proposals default to their measured 96x80 box.
                    // Materialized candidates supply explicit larger logical bounds;
                    // neither may exploit normal search outside that declared box.
                    var compound=PilotRoundPadCells(cx-48,cz-40,cx+48,cz+40,grid,false);
                    if(report["candidateCompoundWorldRects"]?[faction.ToString()] is JArray declared)
                        compound=PilotRoundPadCells((float)declared[0],(float)declared[1],(float)declared[2],(float)declared[3],grid,false);
                    result["declaredCompoundCells"]=RectJson(compound);
                    var contracts=layout.Pads.Where(pad=>pad.PadId==(string)preferred["padId"]&&pad.FactionId==faction).ToArray();
                    Require(contracts.Length==1,"Unique actual typed facility pad contract required");
                    var contract=contracts[0];
                    var padBounds=PilotExactAxisAlignedPadCells(layout,contract,grid);
                    result["actualTypedFacilityPadCells"]=RectJson(padBounds);
                    // Authoring discovery searches only inside the explicitly
                    // declared compound, preserving all native source/occupancy
                    // and circulation predicates. It does NOT call this a fit of
                    // the old typed pad. Default collection is unchanged.
                    if(contract.Kind==SkirmishLegalPadKind.DefenseApproach)compound=padBounds;
                    result["declaredCompoundCells"]=RectJson(compound);
                    var searchPadBounds=discoverFacilityPads&&contract.Kind!=SkirmishLegalPadKind.DefenseApproach?compound:padBounds;
                    if(discoverFacilityPads)result["authoringSearchEnvelopeCells"]=RectJson(searchPadBounds);
                    int outsideCompound=0,outsidePad=0,circulationConflict=0,sourceRejected=0;
                    var serviceKeepClear=new List<RectInt>();
                    foreach(var kind in new[]{SkirmishLegalPadKind.InfantrySpawn,
                        SkirmishLegalPadKind.VehicleSpawn,SkirmishLegalPadKind.Rally})
                    {
                        Require(layout.TryGetPad(faction,kind,out var pad),"Actual typed circulation pad required");
                        // Keep-clear rounds OUT so even a partial intersecting cell
                        // cannot count as free circulation space.
                        serviceKeepClear.Add(PilotExactAxisAlignedPadCells(layout,pad,grid,keepClear:true));
                    }
                    // Service is shared by native Watchtower/Dish structures. Keep
                    // only actual vehicle interaction footprints free, not the whole pad.
                    serviceKeepClear.AddRange(serviceFootprints[faction]);
                    if(plannedApproachClearance!=null)serviceKeepClear.AddRange(plannedApproachClearance);
                    result["additionalQaClearanceScope"]="DeclaredCompoundAndTypedPad_WholeSpawnRallyPads_ActualRepairVehicleFootprints";
                    result["repairInteractionClearanceAvailable"]=serviceAvailable[faction];
                    RectInt Effective(BuildingDefinition d,Vector2Int origin,GridConfig g,bool rotated)
                        =>runway.GetEffectivePlacementRect(d,origin,g,rotated,grid.Origin.y,geometry.GetPlacementFootprint);
                    bool NativeValid(BuildingDefinition d,Vector2Int origin,Vector2Int footprint,bool rotated,GridConfig g,
                        DynamicBuffer<GridRoad> nativeRoads,DynamicBlockerComponent nativeBlockers)
                    {
                        var rect=Effective(d,origin,g,rotated);
                        return BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface,rect)&&
                            cache.IsPlacementValid(d,origin,footprint,rotated,g,nativeRoads,nativeBlockers,dependency,startup,
                                Effective,candidate=>reserved.Any(candidate.Overlaps));
                    }
                    bool Valid(BuildingDefinition d,Vector2Int origin,Vector2Int footprint,bool rotated,GridConfig g,
                        DynamicBuffer<GridRoad> nativeRoads,DynamicBlockerComponent nativeBlockers)
                    {
                        var rect=Effective(d,origin,g,rotated);
                        if(!PilotCompoundContains(compound,rect)){outsideCompound++;return false;}
                        if(!PilotCompoundContains(searchPadBounds,rect)){outsidePad++;return false;}
                        if(serviceKeepClear.Any(rect.Overlaps)){circulationConflict++;return false;}
                        if(!serviceAvailable[faction]){circulationConflict++;return false;}
                        bool valid=NativeValid(d,origin,footprint,rotated,g,nativeRoads,nativeBlockers);
                        if(!valid)sourceRejected++;return valid;
                    }
                    bool CachedSourceInvalid(Vector2Int candidate,Vector2Int size)=>
                        PilotStaticPrefixAllowed(definition)&&BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(
                            staticPrefix,prefixWidth,prefixHeight,candidate,size);
                    if(capacityOnly)
                    {
                        complete=false;
                        result["status"]="IndependentSourceQaCapacityHints_NotConfigurationAcceptance";
                        result["priorFacilityReservationsIncluded"]=false;
                        result["existingTypedPadIgnoredForHintsOnly"]=true;
                        result["additionalQaClearanceScope"]="WholeDeclaredCompound_OriginalTypedPadNotCertified_FixedSpawnRallyRepairAndActualApproachClearance";
                        if(faction!=2){result["status"]="NotRequested_PlayerPreferencesFixed";continue;}
                        JArray allOrigins=null;
                        if(finiteFacilityOriginExportRequest!=null)
                        {
                            Require(configuration.Key.Item1=="S050","Exact owned finite-origin subject");
                            if(faction!=2||!FiniteOriginStructures.Contains((string)preferred["structureId"]))
                            {result["status"]="NotRequested_FiniteOriginExport";continue;}
                            Require((long)compound.width*compound.height<=(int)finiteFacilityOriginExportRequest["maximumOriginsPerRow"],
                                "Finite original compound enumeration bound required");
                            allOrigins=new JArray();result["allSourceQaLegalOrigins"]=allOrigins;
                            result["allOriginsEnumerationComplete"]=false;
                            result["sameQaKeepClearCells"]=new JArray(serviceKeepClear.Select(RectJson));
                            result["originGrid"]=new JObject{["width"]=grid.Width,["height"]=grid.Height,
                                ["cellSize"]=grid.CellSize,["origin"]=new JArray(grid.Origin.x,grid.Origin.y,grid.Origin.z)};
                        }

                        var plots=new JArray();var representatives=new List<RectInt>();
                        result["representativePlots"]=plots;
                        var hintFootprint=geometry.GetPlacementFootprint(definition,false);
                        result["nativePlacementFootprint"]=new JArray(hintFootprint.x,hintFootprint.y);
                        long examined=0,outsideFullCompound=0,prefixRejected=0,qaRejected=0,nativeRejected=0,sourceLegal=0;
                        // Full finite source-compound enumeration, not an enlargement
                        // of the normal runtime relocation radius. These are hints.
                        for(int z=compound.yMin;z<compound.yMax;z++)for(int x=compound.xMin;x<compound.xMax;x++)
                        {
                            examined++;var originHint=new Vector2Int(x,z);
                            var rect=Effective(definition,originHint,grid,false);
                            if(!PilotCompoundContains(compound,rect)){outsideFullCompound++;continue;}
                            if(CachedSourceInvalid(rect.position,rect.size)){prefixRejected++;continue;}
                            if(serviceKeepClear.Any(rect.Overlaps)||!serviceAvailable[faction]){qaRejected++;continue;}
                            if(!NativeValid(definition,originHint,hintFootprint,false,grid,roads,blocker)){nativeRejected++;continue;}
                            sourceLegal++;
                            if(allOrigins!=null)
                            {
                                var measuredOrigin=RectJson(rect);measuredOrigin["originCell"]=new JArray(originHint.x,originHint.y);
                                allOrigins.Add(measuredOrigin);
                            }
                            if(representatives.Count>=32||representatives.Any(rect.Overlaps))continue;
                            representatives.Add(rect);var plot=RectJson(rect);
                            plot["preferredOriginCell"]=new JArray(originHint.x,originHint.y);plots.Add(plot);
                        }
                        result["allCompoundOriginsExamined"]=examined;
                        Require(examined==outsideFullCompound+prefixRejected+qaRejected+nativeRejected+sourceLegal,"Complete finite inventory outcome accounting required");
                        result["outsideFullCompound"]=outsideFullCompound;result["nativeSourceRejected"]=nativeRejected;
                        result["staticPrefixRejected"]=prefixRejected;result["qaRejected"]=qaRejected;
                        result["independentlySourceQaLegalOriginCount"]=sourceLegal;
                        if(allOrigins!=null)
                        {
                            Require(allOrigins.Count==sourceLegal,"Every legal origin recorded without truncation");
                            result["allOriginsEnumerationComplete"]=true;
                            result["allOriginsAreIndependentNotJointPacking"]=true;
                        }
                        result["representativeLimit"]=32;
                        result["representativesAreExhaustivePacking"]=false;
                        result["maximumRepresentativeLimitReached"]=representatives.Count==32;
                        continue;
                    }
                    var context=new BuildingRuntimeSpawnCompositionSystemHelper.Context(null,null,null,null,default,null,
                        geometry.GetPlacementFootprint,Effective,Valid,CachedSourceInvalid,null,null,null,null);
                    var footprint=geometry.GetPlacementFootprint(definition,false);
                    var center=GridUtils.WorldToCell(grid,new float3((float)preferred["x"],0,(float)preferred["z"]));
                    var origin=new Vector2Int(center.x-footprint.x/2,center.y-footprint.y/2);
                    result["actualModelFootprintCells"]=new JArray(footprint.x,footprint.y);
                    var nativeContext=new BuildingRuntimeSpawnCompositionSystemHelper.Context(null,null,null,null,default,null,
                        geometry.GetPlacementFootprint,Effective,NativeValid,CachedSourceInvalid,null,null,null,null);
                    bool nativeFound=search.TryFindValidInitialBuildingOrigin(nativeContext,definition,origin,false,grid,roads,blocker,out var nativeOrigin);
                    result["normalStaticPlacementWithoutAdditionalQa"]=new JObject{["found"]=nativeFound,
                        ["scope"]="ActualProductionSourceChecksAndPriorAcceptedReservations_NoCompoundPadOrCirculationQa",
                        ["acceptedAsCandidateFacility"]=false};
                    if(nativeFound)
                    {
                        var nativeRect=Effective(definition,nativeOrigin,grid,false);
                        result["normalStaticPlacementWithoutAdditionalQa"]["resolvedEffectiveRect"]=RectJson(nativeRect);
                        result["normalStaticPlacementWithoutAdditionalQa"]["insideDeclaredCompound"]=PilotCompoundContains(compound,nativeRect);
                        result["normalStaticPlacementWithoutAdditionalQa"]["insideActualTypedPad"]=PilotCompoundContains(padBounds,nativeRect);
                        result["normalStaticPlacementWithoutAdditionalQa"]["overlapsAdditionalQaClearance"]=serviceKeepClear.Any(nativeRect.Overlaps);
                    }
                    bool foundFit=search.TryFindValidInitialBuildingOrigin(context,definition,origin,false,grid,roads,blocker,out var found);
                    result["nativeCandidateRejections"]=new JObject{["outsideCompound"]=outsideCompound,["outsideTypedPad"]=outsidePad,
                        ["circulationPadConflict"]=circulationConflict,["sourceGeometrySurfaceOrPriorReservation"]=sourceRejected};
                    if(!foundFit)
                    {result["status"]="NativeNormalSearchNoFitInsideDeclaredCompoundAndPad";complete=false;continue;}
                    var accepted=Effective(definition,found,grid,false);
                    Require(PilotCompoundContains(compound,accepted)&&PilotCompoundContains(searchPadBounds,accepted)&&!reserved.Any(accepted.Overlaps),"Actual result escaped declared reservation contract");
                    reserved.Add(accepted);result["originX"]=accepted.x;result["originZ"]=accepted.y;
                    result["widthCells"]=accepted.width;result["depthCells"]=accepted.height;result["insideGrid"]=true;
                    result["resolvedEffectiveRect"]=RectJson(accepted);
                    result["resolvedWorldCenterXZ"]=new JArray(grid.Origin.x+(accepted.x+accepted.width*.5f)*grid.CellSize,grid.Origin.z+(accepted.y+accepted.height*.5f)*grid.CellSize);
                    result["status"]=discoverFacilityPads?"AuthoringDiscovery_NormalSourceAndReservations_OriginalTypedPadNotCertified":"NativeNormalStaticSearchFound_WithinCompound_LivePending";
                    if(discoverFacilityPads)result["insideOriginalTypedPad"]=PilotCompoundContains(padBounds,accepted);
                }
                foreach(var result in output.OfType<JObject>().Where(row=>(string)row["catalogId"]==configuration.Key.Item1&&(string)row["size"]==configuration.Key.Item2))
                    result["entireConfigurationFound"]=complete;
                if(boundedPilotObserver!=null)BoundedPilotCheckpoint("FacilitySizeCompleted",new JObject{["catalogId"]=configuration.Key.Item1,["size"]=configuration.Key.Item2,["complete"]=complete,["rows"]=new JArray(output.OfType<JObject>().Where(r=>(string)r["catalogId"]==configuration.Key.Item1&&(string)r["size"]==configuration.Key.Item2).Select(r=>r.DeepClone()))});
            }
            if(capacityOnly){report["normalFacilityRelocationStatus"]="CollectedIndependentSourceQaHints_NotConfigurationFit";return;}
            if(exactTypedRosterCaller){report["normalFacilityRelocationStatus"]="CollectedStaticFacilities_ExactTypedRosterCallerOwnsPacking";return;}
            // Preserve original force/facility rows. Re-query canonical force budgets
            // against ONLY complete new reservation sets, never partial successes.
            var completeRows=new JArray(output.OfType<JObject>().Where(row=>(bool?)row["entireConfigurationFound"]==true).Select(row=>row.DeepClone()));
            var completeRequests=new List<JObject>();
            foreach(var request in requests)
            {
                var clone=(JObject)request.DeepClone();
                clone["canonicalSetupRows"]=new JArray(((JArray)request["canonicalSetupRows"]).OfType<JObject>().Where(canonical=>
                    completeRows.OfType<JObject>().Any(row=>(string)row["catalogId"]==(string)request["catalogId"]&&
                        (string)row["size"]==(string)canonical["size_id"])).Select(row=>row.DeepClone()));
                if(((JArray)clone["canonicalSetupRows"]).Count>0)completeRequests.Add(clone);
            }
            if(boundedPilotObserver!=null)
            {
                var plainAll=new JArray();var fallbackAll=new JArray();
                var unavailable=new JArray();
                foreach(var request in requests)foreach(JObject canonical in (JArray)request["canonicalSetupRows"])
                    if(!completeRows.OfType<JObject>().Any(r=>(string)r["catalogId"]==(string)request["catalogId"]&&(string)r["size"]==(string)canonical["size_id"]))
                    {
                        var row=new JObject{["catalogId"]=request["catalogId"],["size"]=canonical["size_id"],["status"]="NotQueried_IncompleteCanonicalFacilityConfiguration"};
                        unavailable.Add(row);BoundedPilotCheckpoint("PostFacilityArmySizeUnavailable",row);
                    }
                report["postFacilityPackingUnavailableSizes"]=unavailable;
                foreach(var request in completeRequests)foreach(JObject canonical in (JArray)request["canonicalSetupRows"])
                {
                    var one=(JObject)request.DeepClone();one["canonicalSetupRows"]=new JArray(canonical.DeepClone());
                    var placed=new JArray(completeRows.OfType<JObject>().Where(r=>(string)r["catalogId"]==(string)request["catalogId"]&&(string)r["size"]==(string)canonical["size_id"]).Select(r=>r.DeepClone()));
                    var plain=SurveyForcePacking(input,new[]{one},layout,roles,em,gridEntity,grid,surface,placed);
                    foreach(var row in plain)plainAll.Add(row.DeepClone());
                    var fallback=new JObject();ProbePilotNormalTerrainFallback(input,new[]{one},layout,roles,em,gridEntity,grid,surface,placed,fallback);
                    var rows=(JArray)fallback["normalTerrainFallbackForcePacking"];
                    foreach(var row in rows)fallbackAll.Add(row.DeepClone());
                    report["packingAfterNormalStaticFacilityRelocation"]=plainAll;
                    report["packingAfterNormalStaticFacilityRelocationWithTerrainFallback"]=fallbackAll;
                    BoundedPilotCheckpoint("PostFacilityArmySizeCompleted",new JObject{["catalogId"]=request["catalogId"],["size"]=canonical["size_id"],["facilityRows"]=placed,["rawPacking"]=plain,["terrainFallbackPacking"]=rows});
                }
                report["packingAfterNormalStaticFacilityRelocation"]=plainAll;
                report["packingAfterNormalStaticFacilityRelocationWithTerrainFallback"]=fallbackAll;
                report["packingAfterRelocationScope"]="CompleteFacilityConfigurationsOnly_ActualStaticReservations_NoLiveOccupancy";
                report["normalFacilityRelocationStatus"]="CollectedStaticProposals_LiveSourceFitAndReadabilityPending";
                return;
            }
            report["packingAfterNormalStaticFacilityRelocation"]=SurveyForcePacking(input,completeRequests.ToArray(),layout,roles,em,gridEntity,grid,surface,completeRows);
            var fallbackReport=new JObject();
            ProbePilotNormalTerrainFallback(input,completeRequests.ToArray(),layout,roles,em,gridEntity,grid,surface,completeRows,fallbackReport);
            report["packingAfterNormalStaticFacilityRelocationWithTerrainFallback"]=fallbackReport["normalTerrainFallbackForcePacking"];
            report["packingAfterRelocationScope"]="CompleteFacilityConfigurationsOnly_ActualStaticReservations_NoLiveOccupancy";
            report["normalFacilityRelocationStatus"]="CollectedStaticProposals_LiveSourceFitAndReadabilityPending";
        }
        static bool PilotStaticPrefixAllowed(BuildingDefinition definition)=>
            !BuildingBarrierUtilitySystemHelper.RequiresRoadPlacement(definition)&&
            !BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(definition);

        public static void RunPilotStaticPrefixAndOpeningWitnessPreparation()
        {
            RunPilotStaticPlacementPrefixControls();
            SkirmishS002AriaRunHarness.RunS005OpeningPlacementWitnessPreparation();
            Debug.Log("[PilotStaticPrefixAndOpeningWitnessPreparation] result=Passed prefix=6 opening=18 warmup=1 gameplayCertified=0");
        }

        public static void RunPilotStaticPlacementPrefixControls()
        {
            using var world=new World("PilotStaticPrefixControls");var em=world.EntityManager;
            var owner=em.CreateEntity();em.AddBuffer<GridRoad>(owner);
            var roads=em.GetBuffer<GridRoad>(owner);roads.ResizeUninitialized(64);
            for(int i=0;i<64;i++)roads[i]=default;
            roads[2*8+2]=new GridRoad{Value=1};
            using var bits=new NativeBitArray(64,Allocator.Temp,NativeArrayOptions.ClearMemory);bits.Set(5*8+5,true);
            var blockers=new DynamicBlockerComponent{Blocked=bits};
            var grid=new GridConfig{Width=8,Height=8,CellSize=1};
            var sourceMask=new bool[64];sourceMask[3*8+4]=true;sourceMask[1*8+6]=true;
            int[] prefix=null;
            BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid,roads,blockers,sourceMask,null,
                ref prefix,out int width,out int height,out bool ready);
            Require(ready,"Source prefix control must initialize");
            bool SourceMask(GridConfig g,Vector2Int o,Vector2Int s)
            {for(int y=o.y;y<o.y+s.y;y++)for(int x=o.x;x<o.x+s.x;x++)if(sourceMask[y*8+x])return true;return false;}
            bool Brute(RectInt rect)=>BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(
                rect,grid,roads,blockers,false,null,0,0,null,SourceMask,null);
            bool Cached(Vector2Int o,Vector2Int s)=>BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(prefix,width,height,o,s);
            int comparisons=0;
            for(int y=-1;y<=8;y++)for(int x=-1;x<=8;x++)for(int h=1;h<=3;h++)for(int w=1;w<=3;w++)
            {var rect=new RectInt(x,y,w,h);Require(Brute(rect)==!Cached(rect.position,rect.size),"Exact source-prefix/brute rectangle equivalence");comparisons++;}
            var definition=new BuildingDefinition{FootprintCells=new Vector2Int(2,2)};
            var search=new BuildingRuntimeSpawnCompositionSystemHelper();var reservation=new RectInt(0,0,2,2);
            var qa=new RectInt(0,0,8,8);
            bool Final(BuildingDefinition d,Vector2Int o,Vector2Int size,bool r,GridConfig g,DynamicBuffer<GridRoad> rr,DynamicBlockerComponent bb)
            {var rect=new RectInt(o,size);return PilotCompoundContains(qa,rect)&&!reservation.Overlaps(rect)&&Brute(rect);}
            BuildingRuntimeSpawnCompositionSystemHelper.Context Context(bool use)=>new(null,null,null,null,default,null,
                (d,r)=>d.FootprintCells,(d,o,g,r)=>new RectInt(o,d.FootprintCells),Final,
                use?Cached:null,null,null,null,null);
            bool off=search.TryFindValidInitialBuildingOrigin(Context(false),definition,Vector2Int.zero,false,grid,roads,blockers,out var a);
            bool on=search.TryFindValidInitialBuildingOrigin(Context(true),definition,Vector2Int.zero,false,grid,roads,blockers,out var b);
            Require(off&&on&&a==b&&!reservation.Overlaps(new RectInt(a,definition.FootprintCells)),"Native first-fit order and reservation refusal preserved");
            qa=reservation;
            Require(!search.TryFindValidInitialBuildingOrigin(Context(false),definition,Vector2Int.zero,false,grid,roads,blockers,out _)&&
                !search.TryFindValidInitialBuildingOrigin(Context(true),definition,Vector2Int.zero,false,grid,roads,blockers,out _),"QA-only no-fit remains failure");
            var roadPrefab=new GameObject("Building_Road_Barrier");
            try{Require(!PilotStaticPrefixAllowed(new BuildingDefinition{Prefab=roadPrefab})&&
                !PilotStaticPrefixAllowed(new BuildingDefinition{DisplayName="Road_Barrier"})&&PilotStaticPrefixAllowed(definition),"Normal road gate exceptions must bypass prefix");}
            finally{UnityEngine.Object.DestroyImmediate(roadPrefab);}
            Require(!Cached(new Vector2Int(0,0),Vector2Int.one)&&Cached(new Vector2Int(-1,0),Vector2Int.one),"Free and outside-grid refusal");
            sourceMask[0]=true;
            BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid,roads,blockers,sourceMask,null,
                ref prefix,out width,out height,out ready);
            Require(ready&&Cached(Vector2Int.zero,Vector2Int.one),"Changed source must rebuild rather than reuse stale prefix");
            Debug.Log("[PilotStaticPlacementPrefixControls] result=Passed controls=6 rectangleComparisons="+comparisons+" nativeSearchParity=1 sourceFitCertified=0");
        }

        static void ProbePilotNormalTerrainFallback(JObject input,JObject[] requests,SkirmishMapLayoutConfig layout,
            SkirmishRoleCatalogConfig roles,EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,JArray facilities,JObject report)
        {
            // The original seed-one first-candidate rows above remain unchanged.
            // Production terrain fallback is gated by a match component. Install
            // only that validation context in this disposable, non-ticking world.
            using var matches=em.CreateEntityQuery(typeof(SkirmishMatchState));
            Require(matches.IsEmptyIgnoreFilter,"Survey must not reuse a live native match context");
            var context=em.CreateEntity(typeof(SkirmishMatchState));
            try
            {
                report["normalTerrainFallbackValidationContext"]=true;
                report["normalTerrainFallbackLiveOccupancyIncluded"]=false;
                report["normalTerrainFallbackForcePacking"]=SurveyForcePacking(input,requests,layout,roles,em,gridEntity,grid,surface,facilities,true);
            }
            finally{if(em.Exists(context))em.DestroyEntity(context);}
            Require(matches.IsEmptyIgnoreFilter,"Disposable fallback context must be removed");
        }
        static bool PilotFootprintInsideProjectedPad(SkirmishMapLayoutConfig layout,SkirmishLegalPadConfig pad,GridConfig grid,int2 cell,int2 footprint)
        {
            var polygon=new float2[4];int index=0;
            foreach(var corner in new[]{new int2(-1,-1),new int2(1,-1),new int2(1,1),new int2(-1,1)})
            {
                if(!layout.TryProjectLocalToMap(pad.CenterX+corner.x*pad.WidthMetres/2,pad.CenterZ+corner.y*pad.DepthMetres/2,out float x,out float z)||!math.isfinite(x)||!math.isfinite(z))return false;
                polygon[index++]=new float2(x,z);
            }
            var minimum=UnitFootprintUtility.GetMinCell(cell,footprint);
            foreach(var corner in new[]{new int2(0,0),new int2(footprint.x,0),new int2(footprint.x,footprint.y),new int2(0,footprint.y)})
            {
                var point=new float2(grid.Origin.x+(minimum.x+corner.x)*grid.CellSize,grid.Origin.z+(minimum.y+corner.y)*grid.CellSize);
                float winding=0;
                for(int i=0;i<4;i++)
                {
                    var edge=polygon[(i+1)%4]-polygon[i];var relative=point-polygon[i];float cross=edge.x*relative.y-edge.y*relative.x;
                    if(math.abs(cross)<.0001f)continue;
                    if(winding!=0&&math.sign(cross)!=winding)return false;winding=math.sign(cross);
                }
                if(winding==0)return false;
            }
            return true;
        }
        static bool PilotRepairInteractionFootprints(SkirmishMapLayoutConfig layout,EntityManager em,Entity gridEntity,GridConfig grid,
            MapSurfaceComponent surface,byte faction,out List<RectInt> rectangles,JArray evidence)
        {
            rectangles=new List<RectInt>();
            var pads=layout.Pads.Where(p=>p.FactionId==faction&&p.Kind==SkirmishLegalPadKind.Service).ToArray();
            Require(pads.Length==1,"Unique actual typed Service pad required; no duplicate or guessed point");
            var pad=pads[0];Require(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z),"Actual repair interaction point required");
            var cell=GridUtils.WorldToCell(grid,new float3(x,0,z));bool available=true;
            foreach(var role in new[]{Game.Skirmish.Contracts.SkirmishRoleKind.Car,Game.Skirmish.Contracts.SkirmishRoleKind.ApcFast,
                Game.Skirmish.Contracts.SkirmishRoleKind.ApcArmored,Game.Skirmish.Contracts.SkirmishRoleKind.ApcHeavy,
                Game.Skirmish.Contracts.SkirmishRoleKind.Tank,Game.Skirmish.Contracts.SkirmishRoleKind.AntiAir,
                Game.Skirmish.Contracts.SkirmishRoleKind.Radar,Game.Skirmish.Contracts.SkirmishRoleKind.Siege,
                Game.Skirmish.Contracts.SkirmishRoleKind.Tanker,Game.Skirmish.Contracts.SkirmishRoleKind.LogisticsTruck})
            {
                var row=new JObject{["role"]=role.ToString(),["servicePadId"]=pad.PadId,["interactionWorldXZ"]=new JArray(x,z),
                    ["requestedCell"]=new JArray(cell.x,cell.y),["scope"]="ActualNativeFootprintAtTypedRepairPoint_NotPaidRepairOrThreatProof"};evidence.Add(row);
                var prefab=FindNativePrefab(em,SkirmishRoleCatalogConfig.RuntimePrefabKey(role));
                if(prefab==Entity.Null||!em.HasComponent<UnitFootprint>(prefab)||!em.HasComponent<UnitMovementBehavior>(prefab)||
                    em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion==0)
                {row["status"]="UnavailableExactNativeRepairVehicleFootprint";available=false;continue;}
                var footprint=em.GetComponentData<UnitFootprint>(prefab).Size;
                var minimum=UnitFootprintUtility.GetMinCell(cell,footprint);var rect=new RectInt(minimum.x,minimum.y,footprint.x,footprint.y);
                row["footprintCells"]=new JArray(footprint.x,footprint.y);row["reservedInteractionRect"]=RectJson(rect);
                bool nativeLegal=EndpointLegal(em,gridEntity,grid,surface,prefab,cell);
                bool insidePad=PilotFootprintInsideProjectedPad(layout,pad,grid,cell,footprint);
                row["nativeStaticEndpointLegal"]=nativeLegal;row["insideTypedServicePad"]=insidePad;
                row["status"]=nativeLegal&&insidePad?"StaticRepairInteractionFootprintAvailable_LiveRepairPending":"StaticRepairInteractionRejectedOrOutsideTypedPad";
                available&=nativeLegal&&insidePad;rectangles.Add(rect);
            }
            return available;
        }
        static void RunPilotCompoundReservationControls()
        {
            var compound=new RectInt(4,4,12,12);var reservation=new RectInt(8,8,4,4);
            Require(PilotCompoundContains(compound,new RectInt(4,4,12,12)),"Inclusive declared compound positive");
            Require(!PilotCompoundContains(compound,new RectInt(15,15,2,2)),"Footprint corners cannot escape compound");
            var search=new BuildingRuntimeSpawnCompositionSystemHelper();var definition=new BuildingDefinition{FootprintCells=new Vector2Int(4,4)};
            var grid=new GridConfig{Width=32,Height=32,CellSize=1};
            bool Valid(BuildingDefinition d,Vector2Int origin,Vector2Int size,bool rotated,GridConfig g,
                DynamicBuffer<GridRoad> roads,DynamicBlockerComponent blockers)=>
                PilotCompoundContains(compound,new RectInt(origin,size))&&!reservation.Overlaps(new RectInt(origin,size));
            var context=new BuildingRuntimeSpawnCompositionSystemHelper.Context(null,null,null,null,default,null,
                (d,r)=>d.FootprintCells,(d,o,g,r)=>new RectInt(o,d.FootprintCells),Valid,null,null,null,null,null);
            Require(search.TryFindValidInitialBuildingOrigin(context,definition,new Vector2Int(8,8),false,grid,default,default,out var found)&&
                PilotCompoundContains(compound,new RectInt(found,definition.FootprintCells))&&!reservation.Overlaps(new RectInt(found,definition.FootprintCells)),
                "Actual search must relocate within declared compound around reservation");
            compound=new RectInt(8,8,4,4);
            Require(!search.TryFindValidInitialBuildingOrigin(context,definition,new Vector2Int(8,8),false,grid,default,default,out _),
                "No normal search fallback may escape full reserved compound");
            var inward=PilotRoundPadCells(4.25f,4.25f,15.75f,15.75f,grid,false);
            Require(inward==new RectInt(5,5,10,10),"Only whole cells inside typed pad may host facilities");
            var nonAlignedCompound=PilotRoundPadCells(-48.25f,-40.25f,47.75f,39.75f,grid,false);
            Require(nonAlignedCompound==new RectInt(-48,-40,95,79)&&
                !PilotCompoundContains(nonAlignedCompound,new RectInt(-49,-40,1,1))&&
                !PilotCompoundContains(nonAlignedCompound,new RectInt(47,0,1,1)),
                "Non-grid-aligned compound boundaries must exclude every partly outside footprint");
            var outward=PilotRoundPadCells(4.25f,4.25f,15.75f,15.75f,grid,true);
            Require(outward==new RectInt(4,4,12,12),"Partial circulation cells must remain reserved");
            Debug.Log("[SkirmishPilotCompoundReservationControls] result=Passed controls=6 sourceFitCertified=0");
        }
        static RectInt PilotExactAxisAlignedPadCells(SkirmishMapLayoutConfig layout,SkirmishLegalPadConfig pad,GridConfig grid,bool keepClear=false)
        {
            Require(grid.CellSize>0&&pad.WidthMetres>0&&pad.DepthMetres>0,"Finite positive pad/grid dimensions required");
            Require(layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float cx,out float cz),"Actual pad center projection required");
            float minX=float.PositiveInfinity,minZ=float.PositiveInfinity,maxX=float.NegativeInfinity,maxZ=float.NegativeInfinity;
            foreach(int xSign in new[]{-1,1})foreach(int zSign in new[]{-1,1})
            {
                Require(layout.TryProjectLocalToMap(pad.CenterX+xSign*pad.WidthMetres/2,pad.CenterZ+zSign*pad.DepthMetres/2,out float x,out float z),"Actual pad corner projection required");
                // This bounded proposal uses source-axis metre rectangles. Refuse
                // changed scale/rotation instead of treating a rotated AABB as pad.
                Require(math.isfinite(x)&&math.isfinite(z)&&math.abs(x-(cx+xSign*pad.WidthMetres/2))<.0001f&&
                    math.abs(z-(cz+zSign*pad.DepthMetres/2))<.0001f,"Probe requires exact axis-aligned metre pad projection");
                minX=math.min(minX,x);minZ=math.min(minZ,z);maxX=math.max(maxX,x);maxZ=math.max(maxZ,z);
            }
            return PilotRoundPadCells(minX,minZ,maxX,maxZ,grid,keepClear);
        }
        static RectInt PilotRoundPadCells(float minX,float minZ,float maxX,float maxZ,GridConfig grid,bool keepClear)
        {
            float lx=(minX-grid.Origin.x)/grid.CellSize,lz=(minZ-grid.Origin.z)/grid.CellSize;
            float hx=(maxX-grid.Origin.x)/grid.CellSize,hz=(maxZ-grid.Origin.z)/grid.CellSize;
            int x=(int)(keepClear?math.floor(lx):math.ceil(lx)),z=(int)(keepClear?math.floor(lz):math.ceil(lz));
            int right=(int)(keepClear?math.ceil(hx):math.floor(hx)),top=(int)(keepClear?math.ceil(hz):math.floor(hz));
            return new RectInt(x,z,math.max(0,right-x),math.max(0,top-z));
        }
        static JObject RectJson(RectInt rect)=>new JObject{["x"]=rect.x,["z"]=rect.y,["width"]=rect.width,["depth"]=rect.height};
        static bool PilotCompoundContains(RectInt compound,RectInt footprint)=>footprint.width>0&&footprint.height>0&&
            footprint.xMin>=compound.xMin&&footprint.yMin>=compound.yMin&&footprint.xMax<=compound.xMax&&footprint.yMax<=compound.yMax;
    }
}
#endif
