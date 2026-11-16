#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using Game.Components;
using Game.Configs;
using Game.Runtime;
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
        // Read-only source-fit survey of the actual staged S049 definition/layout.
        // This deliberately does not call the authoring/refinement pilots, which
        // materialize a different disposable layout from the FC template.
        public static void RunS049StagedSourceFitSurvey()
        {
            Require(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode&&ground30MemoryWriter==null,
                "Idle Edit Mode and exclusive source survey required");
            Require(!mpGroundBaPilotRequested,"No other MP pilot may own the static source survey lane");
            const string definitionPath="Assets/Game/Configs/SkirmishExpansion/StagedS049/SkirmishScenario_S049.asset";
            const string layoutPath="Assets/Game/Configs/SkirmishExpansion/StagedS049/Logical/Layout_S049.asset";
            const string outputEnv="WARLINE_S049_STAGED_SOURCE_FIT_OUTPUT";
            string output=Environment.GetEnvironmentVariable(outputEnv);
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrWhiteSpace(output)&&Path.IsPathRooted(output)&&
                Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),
                "Fresh one-run staged S049 survey output under Logs/SkirmishProduction required");

            var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(definitionPath);
            var layout=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(layoutPath);
            Require(definition!=null&&layout!=null&&!UnityEditor.EditorUtility.IsDirty(definition)&&
                !UnityEditor.EditorUtility.IsDirty(layout)&&definition.CatalogId=="S049"&&definition.MapLayout==layout&&
                definition.CampaignMap!=null&&definition.CampaignMap.OperationMapId=="opmap.ch04.armor_break"&&
                definition.CampaignMap.ContentHash=="da42d5bc876e1f2eed02b3d7050d41759c010de973dfd7c51c51eba1a26e5a86"&&
                definition.ObjectiveConfig!=null&&definition.ArmyProfileConfig!=null&&definition.StartPackageConfig!=null&&
                definition.MissionLogicalBinding!=null&&definition.MissionLogicalBinding.CatalogId=="S049"&&
                layout.OperationMapId==definition.CampaignMap.OperationMapId&&layout.WorldBinding.SourceHash==definition.CampaignMap.ContentHash,
                "Exact clean staged S049 definition/layout and current original Campaign binding required");
            string layoutBefore=JsonUtility.ToJson(layout),definitionBefore=JsonUtility.ToJson(definition);
            var source=definition.CampaignMap;
            string sourcePath=UnityEditor.AssetDatabase.GetAssetPath(source);
            string gridPath=UnityEditor.AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid);
            string surfacePath=UnityEditor.AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID);
            string sceneGuid=source.NavigationMetadata.AuthoredSubSceneGuid;
            string scenePath=UnityEditor.AssetDatabase.GUIDToAssetPath(sceneGuid);
            Require(!string.IsNullOrEmpty(sourcePath)&&!string.IsNullOrEmpty(gridPath)&&!string.IsNullOrEmpty(surfacePath)&&
                !string.IsNullOrEmpty(scenePath),"Current Campaign source/grid/surface/scene asset joins required");
            var pins=new JObject{["stagedDefinition"] = new JObject{["path"]=definitionPath,["sha256"]=Hash(definitionPath)},
                ["stagedLayout"] = new JObject{["path"]=layoutPath,["sha256"]=Hash(layoutPath)},
                ["source"] = new JObject{["path"]=sourcePath,["sha256"]=Hash(sourcePath),["operationMapId"]=source.OperationMapId,
                    ["sourceIdentityHash"]=source.SourceIdentityHash,["contentHash"]=source.ContentHash},
                ["grid"] = new JObject{["path"]=gridPath,["sha256"]=Hash(gridPath)},
                ["surface"] = new JObject{["path"]=surfacePath,["sha256"]=Hash(surfacePath)},
                ["scene"] = new JObject{["guid"]=sceneGuid,["path"]=scenePath,["sha256"]=Hash(scenePath)}};
            var dependencies=new JArray();
            foreach(var asset in new UnityEngine.Object[]{definition.ObjectiveConfig,definition.ArmyProfileConfig,
                definition.StartPackageConfig,definition.EconomyConfig,definition.UpgradeCatalog,definition.IntelConfig,
                definition.RoleCatalog,definition.MissionLogicalBinding})
            {
                string path=UnityEditor.AssetDatabase.GetAssetPath(asset);
                Require(!string.IsNullOrEmpty(path)&&!UnityEditor.EditorUtility.IsDirty(asset),"Clean exact staged dependency required");
                dependencies.Add(new JObject{["path"]=path,["sha256"]=Hash(path)});
            }
            pins["stagedDependencies"]=dependencies;
            var report=new JObject{["schema"]=1,["catalogId"]="S049",["scope"]="ActualStagedLayout_PadBoundedStaticAllocatorProposals_NotStagedPositionsOrLiveFit",
                ["sourcePins"]=pins,["sourceFitCertified"]=false,["waterQualified"]=false,["cameraQualified"]=false,
                ["missionReady"]=false,["playerAccepted"]=false,["result"]="Incomplete",
                ["group"]="MP",["stagedAssetReadOnly"]=true,
                ["facilityPlacementInterpretation"]="Native normal allocator relocation candidates constrained to actual staged typed facility pads; these are not the authored facility positions and do not certify live occupancy or clearance",
                ["armyRouteServiceInterpretation"]="Static queries use the returned relocation candidate reservations; they do not prove runtime placement or live mission readiness"};
            var rows=new JArray();report["sizes"]=rows;
            JObject scenesBefore=null;string[] worldsBefore=null;World previousDefault=null;bool baselineCaptured=false;
            Exception primaryFailure=null,cleanupFailure=null;bool cleanupRestored=false;
            mpGroundBaPilotRequested=true;
            try
            {
                report["companionPrewarm"]=Ground30PrewarmCompanions();
                scenesBefore=Ground30SceneOwnership();worldsBefore=CaptureMaterializationWorldCensus();
                previousDefault=World.DefaultGameObjectInjectionWorld;baselineCaptured=true;
                Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string matrixError),matrixError);
                var budgets=new List<SkirmishStaticAuthoringBudget>();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                        budget.ExactCanonicalQuantities,"Actual staged S049 canonical budget failed for "+size+": "+string.Join(";",reasons));
                    budgets.Add(budget);
                }
                report["budgets"]=new JArray(budgets.Select(b=>new JObject{["size"]=b.Size.ToString(),
                    ["forces"]=JArray.FromObject(b.Forces),["structures"]=JArray.FromObject(b.Structures),
                    ["exactCanonicalQuantities"]=b.ExactCanonicalQuantities}));
                using(var world=StreamingWorld())
                {
                    Load(world,sceneGuid);Load(world,AssetDatabase.AssetPathToGUID(Support));
                    using var runtimeSurface=PublishMissingPinnedSurface(world,source,surfacePath,report);
                    var em=world.EntityManager;
                    using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(GridRoad));
                    using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
                    Require(grids.CalculateEntityCount()==1&&surfaces.CalculateEntityCount()==1,"One original Campaign native grid and surface required");
                    var gridEntity=grids.GetSingletonEntity();var grid=grids.GetSingleton<GridConfig>();var surface=surfaces.GetSingleton<MapSurfaceComponent>();
                    Require(grid.Width==source.GridMetadata.Dimensions.x&&grid.Height==source.GridMetadata.Dimensions.y&&
                        grid.CellSize==source.GridMetadata.CellSize&&math.all(grid.Origin==(float3)source.GridMetadata.Origin)&&
                        surface.HasSurfaceData!=0&&surface.SurfaceBlob.IsCreated&&surface.Dimensions.Equals(new int2(grid.Width,grid.Height))&&
                        surface.CellSize==grid.CellSize&&math.all(surface.GridOrigin==grid.Origin),"Loaded native Campaign grid/surface identity mismatch");
                    var walkable=em.GetBuffer<GridWalkable>(gridEntity,true);int blockedCount=0;
                    for(int i=0;i<walkable.Length;i++)if(walkable[i].Value==0)blockedCount++;
                    report["nativeGrid"]=new JObject{["width"]=grid.Width,["height"]=grid.Height,["cellSize"]=grid.CellSize,
                        ["originXZ"]=new JArray(grid.Origin.x,grid.Origin.z),["actualBakedBlockedCellCount"]=blockedCount,
                        ["surfaceSamples"]=source.SurfaceMetadata.SurfaceCount};
                    var facilities=new JArray();
                    foreach(var budget in budgets)
                    {
                        var preferred=Ground30PreferredFacilities(budget,layout,grid);foreach(var value in preferred)facilities.Add(value.DeepClone());
                        rows.Add(new JObject{["size"]=budget.Size.ToString(),["allSizeFacilities"]=preferred});
                    }
                    var placementReport=new JObject{["group"]="MP",["stagedAssetReadOnly"]=true,["allSizeFacilities"]=facilities};
                    ProbePilotNormalFacilityRelocation(new JObject(),System.Array.Empty<JObject>(),layout,definition.RoleCatalog,
                        em,gridEntity,grid,surface,placementReport,false,false,new List<RectInt>(),serviceAuthority:definition);
                    report["normalFacilityPlacement"]=placementReport;
                    report["normalFacilityPlacementInterpretation"]=(string)placementReport["normalFacilityRelocationScope"]+
                        "; returned rows are static relocation proposals, not the authored facility positions";
                    foreach(var row in rows.OfType<JObject>())
                    {
                        string size=(string)row["size"];var budget=budgets.Single(b=>b.Size.ToString()==size);
                        var placed=((JArray)placementReport["normalFacilityRelocationProposals"]).OfType<JObject>()
                            .Where(f=>(string)f["size"]==size).ToArray();
                        bool complete=placed.Length==budget.Structures.Length&&placed.All(f=>(bool?)f["entireConfigurationFound"]==true);
                        row["completeStaticRelocationProposalWithinStagedTypedPads"]=complete;
                        if(!complete){row["armyPacking"]="NotRun_IncompleteFacilityConfiguration";row["routeQueries"]="NotRun_IncompleteFacilityConfiguration";continue;}
                        using var typedPlacement=new Game.Runtime.SkirmishTypedSpawnConstraint(em,gridEntity,grid,layout);
                        var packed=Ground30PackBudget(budget,layout,em,gridEntity,grid,surface,new JArray(placed.Select(f=>f.DeepClone())),typedPlacement);
                        row["armyPacking"]=packed;
                        bool actors=packed.Count==budget.Forces.Length&&packed.OfType<JObject>().All(f=>
                            (int?)f["foundCount"]==(int?)f["requestedCount"]&&(int?)f["surfaceTraversableCount"]==(int?)f["requestedCount"]&&
                            (int?)f["insideTypedPadCount"]==(int?)f["requestedCount"]);
                        row["allCanonicalActorsFitTypedPads"]=actors;
                        var routes=Ground30RouteQueries(budget,layout,em,gridEntity,grid,surface,new JArray(placed.Select(f=>f.DeepClone())));
                        row["routeQueries"]=routes;
                        row["allCanonicalRouteQueriesExact"]=JointRoutesExact(routes);
                        row["routeProfilesCovered"]=MpPilotCanonicalRouteCoverage(budget,layout,em,grid,routes);
                        row["routesIndependent"]=PreferencePathsIndependent(layout,grid,routes,out var independence);
                        row["routeIndependenceDetails"]=independence;
                        row["serviceAccess"]=actors?MpPilotServiceAccess(budget,definition,layout,em,gridEntity,grid,surface,
                            new JArray(placed.Select(f=>f.DeepClone())),packed):new JObject{["status"]="NotRun_ActorPackingIncomplete",
                                ["allMembersHaveExactServiceIngressAndReturn"]=false};
                    }
                }
                Require(Hash(definitionPath)==(string)pins["stagedDefinition"]["sha256"]&&Hash(layoutPath)==(string)pins["stagedLayout"]["sha256"]&&
                    JsonUtility.ToJson(layout)==layoutBefore&&JsonUtility.ToJson(definition)==definitionBefore&&
                    !EditorUtility.IsDirty(definition)&&!EditorUtility.IsDirty(layout),"Staged S049 assets changed during read-only survey");
                foreach(string key in new[]{"source","grid","surface","scene"})
                {var pin=(JObject)pins[key];Require(Hash((string)pin["path"])==(string)pin["sha256"],"Original Campaign input changed during survey: "+key);}
                foreach(JObject pin in dependencies.OfType<JObject>())
                    Require(Hash((string)pin["path"])==(string)pin["sha256"],"Staged S049 dependency changed during survey");
                report["stagedAndPhysicalPinsRechecked"]=true;
                bool staticPassed=rows.OfType<JObject>().All(r=>(bool?)r["completeStaticRelocationProposalWithinStagedTypedPads"]==true&&
                    (bool?)r["allCanonicalActorsFitTypedPads"]==true&&(bool?)r["allCanonicalRouteQueriesExact"]==true&&
                    (bool?)r["routeProfilesCovered"]==true&&(bool?)r["routesIndependent"]==true&&
                    (bool?)r["serviceAccess"]?["allMembersHaveExactServiceIngressAndReturn"]==true);
                report["boundedStaticSourceFitSubgatesPassed"]=staticPassed;
                report["result"]=staticPassed?"StaticSubgatesPassed_LiveOccupancyWaterCameraAndMissionAcceptancePending":
                    "StaticSourceFitSubgateFailedOrUnresolved";
            }
            catch(Exception failure)
            {
                primaryFailure=failure;report["error"]=failure.ToString();report["primaryFailure"]=failure.ToString();report["result"]="SurveyFailed";
            }
            finally
            {
                mpGroundBaPilotRequested=false;
                try
                {
                    if(!baselineCaptured)throw new InvalidOperationException("Cleanup census unavailable because the prewarmed baseline was not captured");
                    var worldsAfter=CaptureMaterializationWorldCensus();var scenesAfter=Ground30SceneOwnership();
                    bool worldCensusRestored=worldsBefore.SequenceEqual(worldsAfter);
                    bool defaultWorldRestored=ReferenceEquals(previousDefault,World.DefaultGameObjectInjectionWorld);
                    bool sceneOwnershipRestored=JToken.DeepEquals(scenesBefore,scenesAfter);
                    cleanupRestored=worldCensusRestored&&defaultWorldRestored&&sceneOwnershipRestored;
                    report["cleanupChecks"]=new JObject{["worldCensusRestored"]=worldCensusRestored,
                        ["defaultWorldIdentityRestored"]=defaultWorldRestored,["sceneOwnershipRestored"]=sceneOwnershipRestored,
                        ["worldsBefore"]=JArray.FromObject(worldsBefore),["worldsAfter"]=JArray.FromObject(worldsAfter),
                        ["scenesBefore"]=scenesBefore.DeepClone(),["scenesAfter"]=scenesAfter};
                    report["cleanupVerified"]=cleanupRestored;
                    if(!cleanupRestored)cleanupFailure=new InvalidOperationException("Original native source survey World/default/scene cleanup required");
                }
                catch(Exception failure)
                {
                    cleanupFailure=failure;report["cleanupVerified"]=false;report["cleanupFailure"]=failure.ToString();
                }
                if(cleanupFailure!=null)report["cleanupFailure"]=cleanupFailure.ToString();
                if(primaryFailure!=null&&cleanupFailure!=null)
                    report["combinedFailure"]=new AggregateException("Survey and cleanup both failed",primaryFailure,cleanupFailure).ToString();
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                    report["receiptPersisted"]=true;
                    using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                    using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                    {writer.Write(report.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
                }
                catch(Exception failure)
                {
                    if(cleanupFailure==null)cleanupFailure=failure;
                    else cleanupFailure=new AggregateException("Cleanup and receipt persistence failed",cleanupFailure,failure);
                }
            }
            if(primaryFailure!=null&&cleanupFailure!=null)
                throw new AggregateException("S049 staged source survey and/or cleanup failed; inspect persisted receipt",primaryFailure,cleanupFailure);
            if(primaryFailure!=null)ExceptionDispatchInfo.Capture(primaryFailure).Throw();
            if(cleanupFailure!=null)ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
            Debug.Log("[S049StagedSourceFitSurvey] result=Collected catalog=S049 sizes=3 stagedAssets=readOnly sourceFitCertified=0 missionReady=0 output="+output);
        }
    }
}
#endif
