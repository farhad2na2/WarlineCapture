#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Game.Skirmish.Contracts;
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
        static readonly string[] Ground30Ids={"S007","S008","S013","S014","S019","S020","S031","S032","S037","S038","S043","S044","S055","S056","S061","S062","S067","S068","S079","S080","S085","S086","S091","S092","S103","S104","S109","S110","S115","S116"};

        public static void RunGroundObjective30QualificationCollection()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle owned Editor required");
            var input=ReadGround30Envelope();
            string output=Environment.GetEnvironmentVariable("WARLINE_GROUND_OBJECTIVE30_OUTPUT");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),"Fresh persistent ground30 output required");
            var before=CaptureMaterializationWorldCensus();var defaultBefore=World.DefaultGameObjectInjectionWorld;
            int scenes=UnityEngine.SceneManagement.SceneManager.sceneCount,previews=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            var sources=new JArray();var report=new JObject{["schema"]=1,["scope"]="StaticAuthoringSourceQueries_NotCompiledMissionQualification",
                ["inputSha256"]=Hash(GroundObjective30Inputs),["sourceFitCertified"]=false,["playerReady"]=false,["sources"]=sources};
            var groups=((JArray)input["missions"]).OfType<JObject>().GroupBy(m=>(string)m["source_candidate"]["sourcePath"]).OrderBy(g=>g.Key,StringComparer.Ordinal).ToArray();
            Require(groups.Length==4,"Exactly four existing physical sources required; DB aliases CC");
            int errors=0,worlds=0;
            foreach(var group in groups)
            {
                var row=new JObject{["sourcePath"]=group.Key,["missions"]=new JArray()};sources.Add(row);
                try{CollectGround30Source(group.ToArray(),row);worlds++;}
                catch(Exception error)
                {
                    errors++;row["sourceError"]=error.ToString();
                    var existing=((JArray)row["missions"]).OfType<JObject>().Select(m=>(string)m["catalogId"]).ToArray();
                    foreach(var mission in group.Where(m=>!existing.Contains((string)m["catalog_id"])))
                    {var failed=Ground30MissionRow(mission);failed["error"]=error.ToString();failed["status"]="SourceInfrastructureFailure";((JArray)row["missions"]).Add(failed);}
                }
            }
            var rows=sources.OfType<JObject>().SelectMany(s=>((JArray)s["missions"]).OfType<JObject>()).ToArray();
            Require(rows.Length==30&&rows.Select(r=>(string)r["catalogId"]).OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(Ground30Ids),"All thirty unique mission result rows required");
            Require(rows.Sum(r=>((JArray)r["sizes"]).Count)==90,"Every mission retains all three size result rows");
            errors+=rows.Count(r=>r["error"]!=null)+rows.Sum(r=>((JArray)r["sizes"]).OfType<JObject>().Count(s=>s["error"]!=null));
            bool restored=before.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(defaultBefore,World.DefaultGameObjectInjectionWorld)&&scenes==UnityEngine.SceneManagement.SceneManager.sceneCount&&previews==UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            if(!restored){errors++;report["cleanupError"]="World/default/scene/preview census changed; no unrelated scenes closed";}
            report["sourceWorldsDisposed"]=worlds;report["collectionErrors"]=errors;
            report["normalCompilerRejectedRows"]=rows.Sum(r=>((JArray)r["sizes"]).OfType<JObject>().Count(s=>(bool?)s["normalCompilerPassed"]!=true));
            report["result"]=errors==0?"CollectedUnqualified":"FailedCollection";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using(var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented));
            Require(errors==0,"Ground30 collection/cleanup failures preserved: "+output);
            Debug.Log("[SkirmishGroundObjective30Collection] result=Collected missions=30 sizeRows=90 physicalSources=4 sourceFitCertified=0 output="+output);
        }

        static JObject ReadGround30Envelope()
        {
            Require(Hash(GroundObjective30Inputs)==GroundObjective30InputsSha,"Exact reviewed ground30 input bytes required");
            var input=JObject.Parse(File.ReadAllText(GroundObjective30Inputs));
            ValidateGround30Envelope(input);
            return input;
        }

        static void ValidateGround30Envelope(JObject input)
        {
            Require((int?)input["schema_version"]==1&&(string)input["kind"]=="UnqualifiedGroundObjective30Authoring"&&(int?)input["mission_count"]==30&&(int?)input["canonical_size_vectors"]==90,"Exact ground30 envelope required");
            foreach(var authority in new[]{"full_index","logical_inputs_manifest","pilot_candidates","source_survey"})
            {var pin=(JObject)input["authority"][authority];Require(Hash((string)pin["path"])==(string)pin["sha256"],"Ground30 authority changed: "+authority);}
            var index=JObject.Parse(File.ReadAllText((string)input["authority"]["full_index"]["path"]));
            Require(Hash("Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv")== (string)index["catalog_sha256"]&&Hash("Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv")== (string)index["matrix_sha256"],"Actual canonical catalog/matrix changed");
            var missions=((JArray)input["missions"]).OfType<JObject>().ToArray();
            Require(missions.Length==30&&missions.Select(m=>(string)m["catalog_id"]).OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(Ground30Ids),"Exact thirty unique ground objective IDs required");
            foreach(var mission in missions)
            {
                var originals=((JArray)index["missions"]).OfType<JObject>().Where(m=>(string)m["catalog_id"]==(string)mission["catalog_id"]).ToArray();
                Require(originals.Length==1,"Canonical index mission missing");
                foreach(var key in new[]{"definition_id","scenario_setup_id","tuple","packet","canonical_size_rows","canonical_size_rows_sha256"})
                    Require(JToken.DeepEquals(originals[0][key],mission[key]),"Canonical ground30 field differs: "+mission["catalog_id"]+"/"+key);
                Require((bool?)mission["publication_enabled"]==false&&(string)mission["tuple"]["army_profile"]=="G"&&
                    mission["typed_objective_areas"]["measurement"].Type==JTokenType.Null&&mission["view_intent"]["scalars"].Type==JTokenType.Null,"Unmeasured authoring cannot publish or invent camera/proof");
                Require(Hash((string)mission["packet"]["path"])==(string)mission["packet"]["sha256"],"Governing packet changed");
            }
        }

        static JObject Ground30MissionRow(JObject mission)
        {
            return new JObject{["catalogId"]=mission["catalog_id"],["canonicalRowsSha256"]=mission["canonical_size_rows_sha256"],
                ["sourceFitCertified"]=false,["readiness"]="Pending",
                ["sizes"]=new JArray(new[]{"Standard","War","LargeWar"}.Select(size=>new JObject{["size"]=size,["status"]="NotCollected",["normalCompilerPassed"]=false}))};
        }

        static void CollectGround30Source(JObject[] missions,JObject report,bool refineLogical=false)
        {
            var pin=(JObject)missions[0]["source_candidate"];
            foreach(var mission in missions)foreach(var key in new[]{"sourcePath","sourceAssetSha256","gridPath","gridAssetSha256","surfacePath","surfaceAssetSha256","operationMapId","sourceIdentityHash","contentHash","sourceSceneGuid","authoredSubSceneGuid"})
                Require(JToken.DeepEquals(pin[key],mission["source_candidate"][key]),"Shared world requires identical physical source pin: "+key);
            string path=(string)pin["sourcePath"],gridPath=(string)pin["gridPath"],surfacePath=(string)pin["surfacePath"];
            Require(Hash(path)==(string)pin["sourceAssetSha256"]&&Hash(gridPath)==(string)pin["gridAssetSha256"]&&Hash(surfacePath)==(string)pin["surfaceAssetSha256"],"Exact physical source/grid/surface bytes changed");
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
            Require(source!=null&&!EditorUtility.IsDirty(source)&&source.SourceIdentityHash==(string)pin["sourceIdentityHash"]&&source.ContentHash==(string)pin["contentHash"]&&source.OperationMapId==(string)pin["operationMapId"]&&source.SourceSceneReference.AssetGUID==(string)pin["sourceSceneGuid"]&&source.NavigationMetadata.AuthoredSubSceneGuid==(string)pin["authoredSubSceneGuid"],"Actual clean source identity/scene binding changed");
            Require(AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid)==gridPath&&source.MapSurfaceDataReference!=null&&AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID)==surfacePath,"Actual source grid/surface references differ from pinned paths");
            report["sourcePins"]=pin.DeepClone();report["sourceOnly"]="ExistingCampaignPhysicalGeometry_NoLiveGameplaySystems";
            Ground30MemoryCheckpoint("BeforeWorld", report);
            using(var world=StreamingWorld())
            {
                Ground30MemoryCheckpoint("WorldCreated", report);
                Load(world,source.NavigationMetadata.AuthoredSubSceneGuid);
                Ground30MemoryCheckpoint("SourceLoaded", report);
                Load(world,AssetDatabase.AssetPathToGUID(Support));
                Ground30MemoryCheckpoint("SupportLoaded", report);
                using var runtimeSurface=PublishMissingPinnedSurface(world,source,surfacePath,report);
                if (ground30MemoryWriter != null) Ground30MemoryCheckpoint("LoadedQueryFacts", Ground30LoadedQueryFacts(world));
                var em=world.EntityManager;using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(GridRoad));using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
                Require(grids.CalculateEntityCount()==1&&surfaces.CalculateEntityCount()==1,"Actual unique native grid/surface unavailable; no clear fallback");
                var gridEntity=grids.GetSingletonEntity();var grid=grids.GetSingleton<GridConfig>();var surface=surfaces.GetSingleton<MapSurfaceComponent>();
                Require(grid.Width==source.GridMetadata.Dimensions.x&&grid.Height==source.GridMetadata.Dimensions.y&&grid.CellSize==source.GridMetadata.CellSize&&math.all(grid.Origin==(float3)source.GridMetadata.Origin)&&surface.HasSurfaceData!=0&&surface.SurfaceBlob.IsCreated&&surface.Dimensions.Equals(new int2(grid.Width,grid.Height))&&surface.CellSize==grid.CellSize&&math.all(surface.GridOrigin==grid.Origin),"Native source grid/surface metadata differs");
                Require(SkirmishSetupMatrixTable.TryLoad(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),out var matrix,out string matrixError),matrixError);
                foreach(var mission in missions)
                {
                    if(refineLogical)
                    {
                        ((JArray)report["missions"]).Add(RefineGround30LoadedMission(mission,source,em,gridEntity,grid,surface,(string)report["refinementOutputPrefix"]));
                        continue;
                    }
                    var row=Ground30MissionRow(mission);((JArray)report["missions"]).Add(row);
                    GroundObjective30CandidateOwner owner=null;
                    try
                    {
                        Require(TryCreateGroundObjective30Candidate(mission,source,out owner,out string error),error);
                        row["layoutSemanticSha256"]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(owner.Layout);
                        row["areaGeometryDraftSha256"]=SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(owner.AreaDraft);
                        row["typedAreaSamples"]=Ground30AreaSamples(owner.AreaDraft,grid,surface);
                        foreach(var sizeRow in ((JArray)row["sizes"]).OfType<JObject>())
                        {
                            if (ground30MemoryWriter != null && (string)sizeRow["size"] != ground30MemorySize) continue;
                            Ground30MemoryCheckpoint("BeforeSize", new JObject{["catalogId"]=row["catalogId"],["size"]=sizeRow["size"]});
                            try
                            {
                                Require(Enum.TryParse((string)sizeRow["size"],out SkirmishSizeId size),"Canonical native size missing");
                                var manifest=new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds};
                                bool inspected=SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,manifest,matrix,out var budget,out var reasons);
                                sizeRow["normalCompileReasons"]=new JArray(reasons.Select(reason=>new JObject{["code"]=reason.Code.ToString(),["field"]=reason.Field,["detail"]=reason.Detail}));
                                Require(inspected&&budget!=null,"Actual typed budget unavailable; normal reasons retained");
                                sizeRow["normalCompilerPassed"]=budget.NormalCompilerPassed;sizeRow["staticBudgetCanonicalQuantitiesExact"]=budget.ExactCanonicalQuantities;
                                sizeRow["staticBudgetErrors"]=new JArray(budget.CanonicalBudgetErrors);
                                sizeRow["nativeForcePackingSourceOnly"]=Ground30PackBudget(budget,owner.Layout,em,gridEntity,grid,surface,new JArray());
                                Ground30MemoryCheckpoint("SourcePackingComplete", sizeRow);
                                sizeRow["allSizeFacilities"]=Ground30PreferredFacilities(budget,owner.Layout,grid);
                                sizeRow["group"]=mission["collector_source_group"];sizeRow["newLogicalCandidateMaterialized"]=true;
                                sizeRow["candidateCompoundWorldRects"]=CandidateCompoundRects((JObject)mission["layout_input"]["pilot_candidate"]);
                                var request=new JObject{["catalogId"]=mission["catalog_id"],["canonicalSetupRows"]=new JArray(((JArray)mission["canonical_size_rows"]).Where(v=>(string)v["size_id"]==size.ToString()).Select(v=>v.DeepClone()))};
                                if(((JArray)sizeRow["allSizeFacilities"]).All(v=>v["prefabPath"]!=null))
                                    ProbePilotNormalFacilityRelocation(new JObject(),new[]{request},owner.Layout,owner.Definition.RoleCatalog,em,gridEntity,grid,surface,sizeRow,true);
                                else sizeRow["normalFacilityRelocationStatus"]="UnavailableExactNativeFacilityMetadata_RawFailuresPreserved";
                                Ground30MemoryCheckpoint("FacilitiesComplete", sizeRow);
                                var accepted=new JArray(((JArray)sizeRow["normalFacilityRelocationProposals"]??new JArray()).Where(v=>(bool?)v["entireConfigurationFound"]==true).Select(v=>v.DeepClone()));
                                bool complete=budget.ExactCanonicalQuantities&&accepted.Count==budget.Structures.Length&&accepted.Count>0;
                                sizeRow["completeCanonicalFacilityConfigurationMeasured"]=complete;
                                sizeRow["nativeForcePackingWithCompleteFacilities"]=complete?Ground30PackBudget(budget,owner.Layout,em,gridEntity,grid,surface,accepted):new JArray();
                                sizeRow["completeFacilityPackingStatus"]=complete?"CollectedStaticReservations_LiveFitPending":"UnavailableCompleteCanonicalFacilityConfiguration";
                                sizeRow["nativeRouteQueriesWithCompleteFacilities"]=complete?Ground30RouteQueries(budget,owner.Layout,em,gridEntity,grid,surface,accepted):new JObject{["status"]="UnavailableCompleteCanonicalFacilityConfiguration"};
                                sizeRow["status"]="TypedStaticBudgetAndSourcePackingCollected_NormalCompilationAndLiveFitIndependent";
                                sizeRow["routeIndependenceAndLiveCirculationStatus"]="PendingExactThreeRouteBottleneckProofAndLiveNormalMission";
                                sizeRow["sourceFitCertified"]=false;
                            }
                            catch(Exception sizeError){sizeRow["error"]=sizeError.ToString();sizeRow["status"]="SizeCollectionFailed";}
                            finally { Ground30MemoryCheckpoint("SizeCompleted", sizeRow); }
                        }
                        row["status"]="AuthoringMeasurementsCollected_Unqualified";
                    }
                    catch(Exception error){row["error"]=error.ToString();row["status"]="AuthoringCandidateRejected";}
                    finally{owner?.Dispose();}
                }
            }
            Ground30MemoryCheckpoint("WorldDisposed", report);
            Require(!EditorUtility.IsDirty(source)&&Hash(path)==(string)pin["sourceAssetSha256"]&&Hash(gridPath)==(string)pin["gridAssetSha256"]&&Hash(surfacePath)==(string)pin["surfaceAssetSha256"],"Source bytes/clean state changed during authoring collection");
        }

        static JArray Ground30PreferredFacilities(SkirmishStaticAuthoringBudget budget,SkirmishMapLayoutConfig layout,GridConfig grid)
        {
            var output=new JArray();
            foreach(var structure in budget.Structures)
            {
                var row=new JObject{["catalogId"]=budget.CatalogId,["size"]=budget.Size.ToString(),["faction"]=structure.FactionId,["structureId"]=structure.StructureId,["scope"]="ActualTypedCompilerStructureRoster_PreferredPointNotPlacement"};output.Add(row);
                try
                {
                    float x=0,z=0;
                    Require(Game.Configs.SkirmishMapLayoutBuilder.TryPadForStructure(layout,structure,out var pad)&&layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out x,out z),"Exact typed facility pad/projection required");
                    Game.Configs.SkirmishMapLayoutBuilder.ApplyFrameOffset(layout,structure.AcrossOffsetMetres,structure.ForwardOffsetMetres,ref x,ref z);
                    string key=SkirmishStructureIds.VisualKey(structure.StructureId);
                    var paths=AssetDatabase.FindAssets(key+" t:Prefab",new[]{"Assets/Game/Prefabs/Buildings","Assets/Game/Prefabs/Skirmish"}).Select(AssetDatabase.GUIDToAssetPath).Where(path=>Path.GetFileNameWithoutExtension(path)==key).ToArray();
                    Require(paths.Length==1,"Exact native facility prefab missing or ambiguous: "+key);
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);var metadata=prefab!=null?prefab.GetComponent<BuildingDefinitionAuthoring>():null;
                    Require(metadata!=null,"Actual serialized facility metadata required");var footprint=metadata.ConfiguredFootprintCells;
                    Require(footprint.x>0&&footprint.y>0,"Positive actual facility footprint required");
                    var origin=GridUtils.WorldToCell(grid,new float3(x,0,z))-new int2(footprint.x,footprint.y)/2;
                    row["padId"]=pad.PadId;row["x"]=x;row["z"]=z;row["prefabPath"]=paths[0];row["prefabSha256"]=Hash(paths[0]);
                    row["originX"]=origin.x;row["originZ"]=origin.y;row["widthCells"]=footprint.x;row["depthCells"]=footprint.y;
                    row["footprintSource"]="ActualConfiguredPrefab_ReadOnly";row["status"]="PreferredOnly_NormalAllocatorRequired";
                }
                catch(Exception error){row["error"]=error.ToString();row["status"]="ExactFacilityMetadataUnavailable";}
            }
            return output;
        }

        static void Ground30OccupyFacilities(JArray facilities,GridConfig grid,NativeBitArray occupied)
        {
            foreach(JObject facility in facilities)
            {
                Require((bool?)facility["entireConfigurationFound"]==true,"Never use partial facility placements as a full configuration");
                var r=(JObject)facility["resolvedEffectiveRect"];int x=(int)r["x"],z=(int)r["z"],width=(int)r["width"],depth=(int)r["depth"];
                Require(width>0&&depth>0&&x>=0&&z>=0&&x+width<=grid.Width&&z+depth<=grid.Height,"Exact accepted facility footprint outside native grid");
                for(int zz=z;zz<z+depth;zz++)for(int xx=x;xx<x+width;xx++)occupied.Set(zz*grid.Width+xx,true);
            }
        }

        static JObject Ground30RouteQueries(SkirmishStaticAuthoringBudget budget,SkirmishMapLayoutConfig layout,EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,JArray facilities)
        {
            if(!TryPreflightAuthoredRouteEndpoints(layout,grid,out var endpointPolicy))
                return new JObject{["scope"]="ExactPathfindJobInputs_WithCompleteStaticFacilityReservations_NotLiveFit",
                    ["status"]="NotQueried_AuthoredEndpointPolicyRefused",["endpointPolicyPreflight"]=endpointPolicy,
                    ["nativeQueriesExecuted"]=0,["roleLegReferences"]=0,["queries"]=new JArray(),["roleReferences"]=new JArray(),["sourceFitCertified"]=false};
            using var blockers=new NativeBitArray(grid.Width*grid.Height,Allocator.Persistent,NativeArrayOptions.ClearMemory);Ground30OccupyFacilities(facilities,grid,blockers);
            var queries=new JArray();var roleRefs=new JArray();var cache=new Dictionary<string,int>(StringComparer.Ordinal);
            var report=new JObject{["scope"]="ExactPathfindJobInputs_WithCompleteStaticFacilityReservations_NotLiveFit",["queries"]=queries,["roleReferences"]=roleRefs,["sourceFitCertified"]=false};
            report["endpointPolicyPreflight"]=endpointPolicy;
            int index=0;
            foreach(var force in budget.Forces)
            {
                int forceIndex=index++;var prefab=FindNativePrefab(em,force.RuntimePrefabKey);
                if(prefab==Entity.Null||!em.HasComponent<UnitFootprint>(prefab)||!em.HasComponent<UnitMovementBehavior>(prefab)||em.HasComponent<UnitAirMovement>(prefab))
                {roleRefs.Add(new JObject{["forceIndex"]=forceIndex,["faction"]=force.FactionId,["roleId"]=force.RoleId,["status"]="NativeGroundPathUnavailableOrAirLifecycleSeparate"});continue;}
                var footprint=em.GetComponentData<UnitFootprint>(prefab).Size;var behavior=em.GetComponentData<UnitMovementBehavior>(prefab);
                foreach(var route in layout.Routes)
                {
                    if(behavior.UsesVehicleMotion!=0&&!route.AllowsVehicles)continue;
                    var anchors=force.FactionId==2?route.WaypointAnchorIds.Reverse().ToArray():route.WaypointAnchorIds;
                    for(int leg=1;leg<anchors.Length;leg++)
                    {
                        var reference=new JObject{["forceIndex"]=forceIndex,["faction"]=force.FactionId,["roleId"]=force.RoleId,["objectiveRoleId"]=force.ObjectiveRoleId,["routeId"]=route.RouteId,["leg"]=leg,["nativePrefabIndex"]=prefab.Index,["nativePrefabVersion"]=prefab.Version};roleRefs.Add(reference);
                        SkirmishLayoutAnchorConfig a=default,b=default;float ax=0,az=0,bx=0,bz=0;
                        Require(layout.TryGetAnchorById(anchors[leg-1],out a)&&layout.TryGetAnchorById(anchors[leg],out b),"Exact route anchors required");
                        Require(layout.TryProjectLocalToMap(a.WorldX,a.WorldZ,out ax,out az)&&layout.TryProjectLocalToMap(b.WorldX,b.WorldZ,out bx,out bz),"Exact route source projection required");
                        var from=GridUtils.WorldToCell(grid,new float3(ax,0,az));var to=GridUtils.WorldToCell(grid,new float3(bx,0,bz));
                        string key=Ground30PathQueryKey(force.FactionId,footprint,behavior,from,to);
                        if(!cache.TryGetValue(key,out int queryId))
                        {
                            queryId=queries.Count;cache.Add(key,queryId);var row=new JObject{["queryId"]=queryId,["exactInputKey"]=key,["fromCell"]=new JArray(from.x,from.y),["toCell"]=new JArray(to.x,to.y)};queries.Add(row);
                            QueryPath(em,gridEntity,grid,surface,prefab,from,to,row,force.FactionId,blockers);
                        }
                        reference["nativeQueryId"]=queryId;reference["reuseScope"]="IdenticalFootprintBehaviorFactionEndpointsAndSameConfigurationBlockers";
                    }
                }
            }
            report["nativeQueriesExecuted"]=queries.Count;report["roleLegReferences"]=roleRefs.Count;return report;
        }

        static string Ground30PathQueryKey(byte faction,int2 footprint,UnitMovementBehavior behavior,int2 from,int2 to)
        {
            return faction+"/"+footprint.x+"/"+footprint.y+"/"+behavior.AllowIdleWander+"/"+behavior.UsesVehicleMotion+"/"+from.x+"/"+from.y+"/"+to.x+"/"+to.y;
        }

        static JArray Ground30AreaSamples(SkirmishObjectiveAreaConfig draft,GridConfig grid,MapSurfaceComponent surface)
        {
            var rows=new JArray();
            foreach(var area in draft.Areas??System.Array.Empty<SkirmishObjectiveArea>())
            {
                var samples=new JArray();var row=new JObject{["areaId"]=area.StableAreaId,["role"]=area.Role.ToString(),["shape"]=area.Shape.ToString(),["scope"]="ExactAuthoredVerticesOnly_NotFullAreaClearance",["sourceFitCertified"]=false,["samples"]=samples};rows.Add(row);
                foreach(var point in area.Points??System.Array.Empty<SkirmishObjectiveAreaPoint>())
                {
                    var cell=GridUtils.WorldToCell(grid,new float3(point.WorldX,0,point.WorldZ));var blob=surface.SurfaceBlob;
                    bool sampled=MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample);
                    samples.Add(new JObject{["worldX"]=point.WorldX,["worldZ"]=point.WorldZ,["cellX"]=cell.x,["cellZ"]=cell.y,["insideGrid"]=GridUtils.InBounds(cell,grid.Width,grid.Height),["sampled"]=sampled,["height"]=sampled?(JToken)sample.Height:JValue.CreateNull(),["slopeDegrees"]=sampled?(JToken)sample.SlopeDegrees:JValue.CreateNull()});
                }
            }
            return rows;
        }

        static JArray Ground30PackBudget(SkirmishStaticAuthoringBudget budget,SkirmishMapLayoutConfig layout,EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface,JArray acceptedFacilities,Game.Runtime.SkirmishTypedSpawnConstraint typedConstraint=null)
        {
            using var matches=em.CreateEntityQuery(typeof(SkirmishMatchState));Require(matches.IsEmptyIgnoreFilter,"Never query a live gameplay World");
            Entity context=em.CreateEntity(typeof(SkirmishMatchState));
            try
            {
                var output=new JArray();var walkable=em.GetBuffer<GridWalkable>(gridEntity,true).AsNativeArray();
                using var blocked=new NativeBitArray(grid.Width*grid.Height,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                using var occupied=new NativeBitArray(grid.Width*grid.Height,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                var reserved=new NativeBitArray(grid.Width*grid.Height,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                try
                {
                    for(int i=0;i<walkable.Length;i++)if(walkable[i].Value==0)blocked.Set(i,true);
                    var sourceBlocked=blocked;
                    InitialUnitsSpawnSystem.ReserveStaticBlockerFootprints(em,ref sourceBlocked,grid);
                    Ground30OccupyFacilities(acceptedFacilities,grid,occupied);
                    var random=new Unity.Mathematics.Random(1);int forceIndex=0;
                    foreach(var force in budget.Forces)
                    {
                        var row=new JObject{["forceIndex"]=forceIndex++,["faction"]=force.FactionId,["roleId"]=force.RoleId,["role"]=force.RoleKind.ToString(),["objectiveRoleId"]=force.ObjectiveRoleId,["requestedCount"]=force.Quantity,["runtimePrefabKey"]=force.RuntimePrefabKey,["scope"]=acceptedFacilities.Count==0?"StaticNativeSourceOnly_ExactTypedCanonicalRoster_SeedOne":"StaticNativeWithCompleteFacilityReservations_ExactTypedCanonicalRoster_SeedOne",["liveOccupancyIncluded"]=false,["candidateFacilityOccupancyIncluded"]=acceptedFacilities.Count>0,["sourceFitCertified"]=false};output.Add(row);
                        var prefab=FindNativePrefab(em,force.RuntimePrefabKey);
                        if(prefab==Entity.Null||!em.HasComponent<UnitFootprint>(prefab)){row["status"]="ExactNativeRolePrefabMissing";continue;}
                        float x=0,z=0;
                        if(!Game.Configs.SkirmishMapLayoutBuilder.TryPadForForce(layout,force,out var pad)||!layout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out x,out z)){row["status"]="ExactTypedForcePadMissing";continue;}
                        var footprint=em.GetComponentData<UnitFootprint>(prefab).Size;bool air=em.HasComponent<UnitAirMovement>(prefab);
                        bool vehicle=em.HasComponent<UnitMovementBehavior>(prefab)&&em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion!=0;
                        var center=GridUtils.WorldToCell(grid,new float3(x,0,z));var cells=new JArray();row["candidateCells"]=cells;
                        row["nativePrefabEntityIndex"]=prefab.Index;row["nativePrefabEntityVersion"]=prefab.Version;row["footprintX"]=footprint.x;row["footprintZ"]=footprint.y;row["padId"]=pad.PadId;
                        if(em.HasComponent<UnitMovementBehavior>(prefab))
                        {
                            var actualBehavior=em.GetComponentData<UnitMovementBehavior>(prefab);
                            row["nativeMovementBehavior"]=new JObject{["AllowIdleWander"]=actualBehavior.AllowIdleWander,
                                ["UsesVehicleMotion"]=actualBehavior.UsesVehicleMotion};
                        }
                        int found=0,traversable=0,contained=0;
                        NativeBitArray constrained=default;
                        Require(typedConstraint==null||typedConstraint.TryMask(force,occupied,out constrained),"Exact typed source-authoring spawn mask required");
                        var placementOccupancy=typedConstraint==null?occupied:constrained;
                        try
                        {
                        for(int member=0;member<force.Quantity;member++)
                        {
                            var requestedCenter=center;
                            if(typedConstraint!=null)
                            {
                                Require(typedConstraint.TryRequestedWorld(force,member,out var requested),"Exact normal member offset required");
                                requestedCenter=GridUtils.WorldToCell(grid,new float3(requested.x,requested.y,requested.z));
                                random=Game.Runtime.SkirmishTypedSpawnConstraint.PlacementRandom(1,
                                    new FixedString64Bytes(force.RoleId+"."+force.FactionId+"."+(forceIndex-1)+"."+member),requestedCenter);
                            }
                            if(!InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref random,grid,walkable,blocked,placementOccupancy,ref reserved,requestedCenter,12,footprint,air,out var cell))break;
                            var initial=cell;bool accepted=air||SkirmishSpawnPlacement.EnsureTraversable(em,grid,walkable,blocked,placementOccupancy,ref reserved,prefab,footprint,ref cell);
                            if(!accepted){cells.Add(new JObject{["member"]=member,["initialX"]=initial.x,["initialZ"]=initial.y,["normalFallbackAccepted"]=false});break;}
                            bool terrain=air||new MapSurfaceTraversalValidation().CanTraverseFootprint(surface,surface.HasSurfaceData,grid,cell,footprint,vehicle);
                            bool inside=PilotFootprintInsideProjectedPad(layout,pad,grid,cell,footprint);found++;if(terrain)traversable++;if(inside)contained++;
                            cells.Add(new JObject{["member"]=member,["x"]=cell.x,["z"]=cell.y,["normalFallbackAccepted"]=true,["surfaceTraversable"]=terrain,["insideActualTypedPad"]=inside});
                        }
                        }
                        finally{if(constrained.IsCreated)constrained.Dispose();}
                        row["typedSpawnConstraintApplied"]=typedConstraint!=null;
                        row["foundCount"]=found;row["surfaceTraversableCount"]=traversable;row["insideTypedPadCount"]=contained;
                        row["status"]=found==force.Quantity&&traversable==force.Quantity&&contained==force.Quantity?"StaticSourcePackingInsidePad_LiveAndFacilitiesPending":"StaticSourcePackingFailedOrOutsidePad";
                    }
                    return output;
                }
                finally{reserved.Dispose();}
            }
            finally{if(em.Exists(context))em.DestroyEntity(context);}
        }
    }
}
#endif
