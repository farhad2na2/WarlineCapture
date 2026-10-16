#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        static string boundedPilotSelectedId;
        static Action<string,JObject> boundedPilotObserver;
        static void BoundedPilotCheckpoint(string phase,JObject facts) => boundedPilotObserver?.Invoke(phase,facts);

        sealed class PreparedBoundedPilot
        {
            public JObject Input,SourceRow;public JObject[] Requests;public OperationMapDefinition Source;
            public SkirmishRoleCatalogConfig Roles;public string Group,SurveySha256;
        }
        static PreparedBoundedPilot ReadBoundedPilotInputs(string id)
        {
            Require(new[]{"S026","S049","S050","S074","S097","S098"}.Contains(id),"One exact unprotected six-pilot ID required");
            Require(Hash(MaterializedPilotConfig)==MaterializedPilotConfigSha,"Exact V2 candidate configuration required");
            var packet=JObject.Parse(File.ReadAllText(MaterializedPilotConfig));
            var candidates=((JArray)packet["candidates"]).OfType<JObject>().Where(c=>(string)c["catalog_id"]==id).ToArray();
            Require(candidates.Length==1,"Unique exact V2 candidate required");var candidate=candidates[0];
            string group=(string)candidate["group"];
            var input=JObject.Parse(File.ReadAllText(Inventory));
            Require(Hash((string)input["budgetAuthority"])==(string)input["matrixAssetSha256"],"Current canonical matrix changed");
            var requests=((JArray)input["requests"]).OfType<JObject>().Where(r=>(string)r["catalogId"]==id).ToArray();
            Require(requests.Length==1&&(string)requests[0]["mapGroup"]==group&&
                CanonicalBudgetHash(requests[0]["canonicalSetupRows"])==(string)candidate["canonical_budget_rows_sha256"],"Exact full canonical three-size request required");
            Require(((JArray)requests[0]["canonicalSetupRows"]).Count==3&&
                ((JArray)requests[0]["canonicalSetupRows"]).Select(r=>(string)r["size_id"]).OrderBy(s=>s).SequenceEqual(new[]{"LargeWar","Standard","War"}),"All three canonical sizes required");
            Require(Hash((string)requests[0]["packetPath"])==(string)requests[0]["packetSha256"],"Pilot governing packet changed");
            const string surveyPath="Logs/SkirmishProduction/ground-source-fit-20261015-11.json";
            Require(Hash(surveyPath)==(string)packet["survey_sha256"],"Original measured prefab-lineage authority changed");
            var survey=JObject.Parse(File.ReadAllText(surveyPath));
            Require(Hash(Inventory)==(string)survey["inventorySha256"],"Actual inventory/template/start-profile authority changed since pinned survey11");
            var authorities=((JArray)survey["sources"]).OfType<JObject>().Where(s=>(string)s["group"]==group).ToArray();
            Require(authorities.Length==1,"Unique original source lineage required");var authority=authorities[0];
            var sourceRow=new JObject{["group"]=group,["sourcesFromSurvey11"]="PrefabAndCanonicalLineageOnly_NotPlacementReuse",
                ["canonicalRequests"]=new JArray(new JObject{["catalogId"]=id,["rows"]=requests[0]["canonicalSetupRows"].DeepClone()})};
            foreach(string key in new[]{"sourcePath","sourceAssetSha256","operationMapId","sourceIdentityHash","contentHash","surfacePath","surfaceAssetSha256",
                "gridPath","gridAssetSha256","sourceSceneGuid","authoredSubSceneGuid"})sourceRow[key]=authority[key]?.DeepClone();
            foreach(var pair in new[]{("sourcePath","sourceAssetSha256"),("gridPath","gridAssetSha256"),("surfacePath","surfaceAssetSha256")})
                Require(Hash((string)sourceRow[pair.Item1])==(string)sourceRow[pair.Item2],"Actual pinned physical source changed: "+pair.Item1);
            Require((string)candidate["source_path"]==(string)sourceRow["sourcePath"]&&
                (string)candidate["source_asset_sha256"]==(string)sourceRow["sourceAssetSha256"]&&
                (string)candidate["grid_sha256"]==(string)sourceRow["gridAssetSha256"]&&
                (string)candidate["surface_sha256"]==(string)sourceRow["surfaceAssetSha256"],"Candidate physical source lineage changed");
            foreach(string key in new[]{"allSizeFacilities","allSizeForcePacking"})
                sourceRow[key]=new JArray(((JArray)authority[key]).OfType<JObject>().Where(r=>(string)r["catalogId"]==id).Select(r=>r.DeepClone()));
            foreach(JObject facility in sourceRow["allSizeFacilities"])
                Require(Hash((string)facility["prefabPath"])==(string)facility["prefabSha256"],"Original native building prefab lineage changed");
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)sourceRow["sourcePath"]);
            Require(source!=null&&!EditorUtility.IsDirty(source)&&source.OperationMapId==(string)sourceRow["operationMapId"]&&
                source.SourceIdentityHash==(string)sourceRow["sourceIdentityHash"]&&source.ContentHash==(string)sourceRow["contentHash"]&&
                source.SourceSceneReference.AssetGUID==(string)sourceRow["sourceSceneGuid"]&&
                source.NavigationMetadata.AuthoredSubSceneGuid==(string)sourceRow["authoredSubSceneGuid"]&&
                AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid)==(string)sourceRow["gridPath"]&&
                source.MapSurfaceDataReference!=null&&AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID)==(string)sourceRow["surfacePath"],"Actual source/grid/surface identity changed");
            var catalog=SkirmishExpansionCatalogConfig.Load();Require(catalog!=null&&!EditorUtility.IsDirty(catalog),"Actual clean profile catalog required");
            var roles=catalog.ToAuthoredSet().Roles;Require(roles!=null&&!EditorUtility.IsDirty(roles),"Actual persistent role catalog required");
            return new PreparedBoundedPilot{Input=input,SourceRow=sourceRow,Requests=requests,Source=source,Roles=roles,Group=group,SurveySha256=Hash(surveyPath)};
        }
        // Same actual preflight as the collector; no source World or authored asset.
        public sealed class BoundedPilotInputFacts
        {
            public string CatalogId,Group,CandidateConfigSha256;
            public string[] Sizes;
            public int PreferredFacilityRows,CanonicalForceRows;
            public bool NormalMissionQualified,SourceFitCertified;
        }
        public static BoundedPilotInputFacts InspectBoundedMaterializedPilotInputs(string id)
        {
            var prepared=ReadBoundedPilotInputs(id);
            return new BoundedPilotInputFacts{CatalogId=id,Group=prepared.Group,CandidateConfigSha256=MaterializedPilotConfigSha,
                Sizes=((JArray)prepared.Requests[0]["canonicalSetupRows"]).Select(r=>(string)r["size_id"]).ToArray(),
                PreferredFacilityRows=((JArray)prepared.SourceRow["allSizeFacilities"]).Count,
                CanonicalForceRows=((JArray)prepared.SourceRow["allSizeForcePacking"]).Count,
                NormalMissionQualified=false,SourceFitCertified=false};
        }

        // One existing source World, one exact V2 pilot, all three canonical sizes.
        // No old-template searches, registry-only World, or unrelated source groups.
        public static void RunBoundedMaterializedPilot()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !materializedPilotRequested &&
                boundedPilotObserver==null && ground30MemoryWriter==null,"Idle unowned bounded collector required");
            string id=Environment.GetEnvironmentVariable("WARLINE_MATERIALIZED_PILOT_ID");
            string output=Environment.GetEnvironmentVariable("WARLINE_MATERIALIZED_PILOT_OUTPUT");
            Require(new[]{"S026","S049","S050","S074","S097","S098"}.Contains(id),"One exact unprotected six-pilot ID required");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&
                !File.Exists(output),"Fresh absolute persistent bounded JSONL output required");
            var prepared=ReadBoundedPilotInputs(id);
            var input=prepared.Input;var sourceRow=prepared.SourceRow;var source=prepared.Source;var roles=prepared.Roles;
            var group=prepared.Group;var requests=prepared.Requests;
            var report=new JObject{["schema"]=1,["scope"]="BoundedOnePilotAllSizes_StaticPlacementOnly_NotSourceFit",
                ["candidateConfigSha256"]=MaterializedPilotConfigSha,["surveyLineageSha256"]=prepared.SurveySha256,["sources"]=new JArray(sourceRow),
                ["sourceFitCertified"]=false,["playerReady"]=false,["routesAndLiveOccupancyAndCamera"]="Pending_NotQueriedByThisPlacementOnlyScope"};
            Directory.CreateDirectory(Path.GetDirectoryName(output));bool completed=false;int sequence=0;
            using(var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))
            {
                boundedPilotObserver=(phase,facts)=>
                {
                    var row=new JObject{["schema"]=1,["sequence"]=++sequence,["phase"]=phase,["catalogId"]=id,["group"]=group,
                        ["candidateConfigSha256"]=MaterializedPilotConfigSha,["utc"]=DateTime.UtcNow.ToString("O"),
                        ["worlds"]=new JArray(CaptureMaterializationWorldCensus()),["sourceFitCertified"]=false,["facts"]=facts?.DeepClone()};
                    writer.WriteLine(row.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();stream.Flush(true);
                };
                boundedPilotSelectedId=id;materializedPilotRequested=true;
                materializedPilotCollected=materializedPilotErrors=materializedPilotNegativeControls=0;
                try
                {
                    BoundedPilotCheckpoint("Started",new JObject{["sourcePins"]=sourceRow.DeepClone()});
                    var prewarm=Ground30PrewarmCompanions();BoundedPilotCheckpoint("PackageCompanionBaseline",prewarm);
                    var sceneBefore=Ground30SceneOwnership();var worldsBefore=CaptureMaterializationWorldCensus();
                    var defaultBefore=World.DefaultGameObjectInjectionWorld;
                    int scenes=UnityEngine.SceneManagement.SceneManager.sceneCount,previews=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
                    try
                    {
                        using var templateOwner=new PilotCandidateOwner();SkirmishMapLayoutConfig template;
                        if(input["layoutSnapshots"]?[group] is JObject snapshot)
                        {
                            string path=(string)snapshot["templatePath"];Require(Hash(path)==(string)snapshot["templateAssetSha256"],"Pinned actual layout template changed");
                            template=AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(path);
                            Require(template!=null&&!EditorUtility.IsDirty(template),"Actual clean template required");
                        }
                        else template=CreatePilotLogicalCandidate(source,group,sourceRow,templateOwner);
                        BoundedPilotCheckpoint("BeforeWorld",null);
                        using(var world=StreamingWorld())
                        {
                            BoundedPilotCheckpoint("WorldCreated",null);Load(world,source.NavigationMetadata.AuthoredSubSceneGuid);
                            BoundedPilotCheckpoint("SourceLoaded",null);Load(world,AssetDatabase.AssetPathToGUID(Support));
                            using var runtimeSurface=PublishMissingPinnedSurface(world,source,(string)sourceRow["surfacePath"],sourceRow);
                            BoundedPilotCheckpoint("SupportAndSurfaceLoaded",null);
                            var em=world.EntityManager;using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(GridRoad));
                            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
                            Require(grids.CalculateEntityCount()==1&&surfaces.CalculateEntityCount()==1,"Unique actual source grid/surface required");
                            var grid=grids.GetSingleton<GridConfig>();var surface=surfaces.GetSingleton<MapSurfaceComponent>();
                            Require(grid.Width==source.GridMetadata.Dimensions.x&&grid.Height==source.GridMetadata.Dimensions.y&&
                                grid.CellSize==source.GridMetadata.CellSize&&math.all(grid.Origin==(float3)source.GridMetadata.Origin)&&
                                surface.HasSurfaceData!=0&&surface.SurfaceBlob.IsCreated&&surface.Dimensions.Equals(new int2(grid.Width,grid.Height))&&
                                surface.CellSize==grid.CellSize&&math.all(surface.GridOrigin==grid.Origin),"Actual source grid/surface metadata mismatch");
                            CollectMaterializedPilotCandidates(input,requests,source,template,roles,em,grids.GetSingletonEntity(),grid,surface,sourceRow);
                            Require(materializedPilotCollected==1&&materializedPilotErrors==0&&materializedPilotNegativeControls==2,"One candidate and two schema negatives required");
                            BoundedPilotCheckpoint("CandidatePlacementCollected",sourceRow);
                        }
                    }
                    finally
                    {
                        bool restored=worldsBefore.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(defaultBefore,World.DefaultGameObjectInjectionWorld)&&
                            scenes==UnityEngine.SceneManagement.SceneManager.sceneCount&&previews==UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount&&
                            JToken.DeepEquals(sceneBefore,Ground30SceneOwnership());
                        report["cleanupRestored"]=restored;BoundedPilotCheckpoint("OwnedCleanup",new JObject{["restored"]=restored,["sceneOwnership"]=Ground30SceneOwnership()});
                        Require(restored,"Bounded pilot cleanup changed exact ownership; no arbitrary preview closure");
                    }
                    Require(Hash((string)sourceRow["sourcePath"])==(string)sourceRow["sourceAssetSha256"]&&!EditorUtility.IsDirty(source),"Source mutated during query");
                    completed=true;report["collectionCompleted"]=true;
                }
                catch(Exception error){report["collectionCompleted"]=false;report["error"]=error.ToString();throw;}
                finally
                {
                    try{BoundedPilotCheckpoint("Completed",new JObject{["collected"]=completed,["report"]=report});}
                    finally{boundedPilotObserver=null;boundedPilotSelectedId=null;materializedPilotRequested=false;}
                }
            }
            Debug.Log("[SkirmishBoundedMaterializedPilot] result=Collected mission="+id+" sizeRows=3 sourceWorlds=1 sourceFitCertified=0 output="+output);
        }
    }
}
#endif
