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
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Disposable logical candidate experiment. Persistent staged assets, catalog, Campaign
        // geometry and objective sources are inputs only; no AssetDatabase writes are performed.
        public static void RunS049DisposableLogicalPadCandidateProbe()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&ground30MemoryWriter==null&&!mpGroundBaPilotRequested,
                "Idle, exclusive Editor source survey required");
            const string definitionPath="Assets/Game/Configs/SkirmishExpansion/StagedS049/SkirmishScenario_S049.asset";
            const string layoutPath="Assets/Game/Configs/SkirmishExpansion/StagedS049/Logical/Layout_S049.asset";
            string output=Environment.GetEnvironmentVariable("WARLINE_S049_LOGICAL_CANDIDATE_OUTPUT");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrWhiteSpace(output)&&Path.IsPathRooted(output)&&Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),
                "Fresh disposable S049 candidate receipt under Logs/SkirmishProduction required");

            var sourceDefinition=AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(definitionPath);
            var sourceLayout=AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(layoutPath);
            Require(sourceDefinition!=null&&sourceLayout!=null&&!EditorUtility.IsDirty(sourceDefinition)&&!EditorUtility.IsDirty(sourceLayout)&&
                sourceDefinition.CatalogId=="S049"&&sourceDefinition.MapLayout==sourceLayout&&sourceDefinition.MissionLogicalBinding!=null&&
                sourceDefinition.MissionLogicalBinding.CatalogId=="S049"&&sourceDefinition.CampaignMap!=null&&
                sourceDefinition.CampaignMap.OperationMapId=="opmap.ch04.armor_break"&&
                sourceDefinition.CampaignMap.ContentHash=="da42d5bc876e1f2eed02b3d7050d41759c010de973dfd7c51c51eba1a26e5a86",
                "Exact clean staged S049 definition/layout/current Armor Break binding required");
            string originalDefinitionJson=JsonUtility.ToJson(sourceDefinition),originalLayoutJson=JsonUtility.ToJson(sourceLayout);
            string sourcePath=AssetDatabase.GetAssetPath(sourceDefinition.CampaignMap);
            string gridPath=AssetDatabase.GUIDToAssetPath(sourceDefinition.CampaignMap.GridMetadata.AssetGuid);
            string surfacePath=AssetDatabase.GUIDToAssetPath(sourceDefinition.CampaignMap.MapSurfaceDataReference.AssetGUID);
            string sceneGuid=sourceDefinition.CampaignMap.NavigationMetadata.AuthoredSubSceneGuid;
            string scenePath=AssetDatabase.GUIDToAssetPath(sceneGuid);
            Require(!string.IsNullOrEmpty(sourcePath)&&!string.IsNullOrEmpty(gridPath)&&!string.IsNullOrEmpty(surfacePath)&&!string.IsNullOrEmpty(scenePath),
                "Exact current Campaign source/grid/surface/scene joins required");
            var pins=new JObject{
                ["stagedDefinition"]=new JObject{["path"]=definitionPath,["sha256"]=Hash(definitionPath)},
                ["stagedLayout"]=new JObject{["path"]=layoutPath,["sha256"]=Hash(layoutPath)},
                ["source"]=new JObject{["path"]=sourcePath,["sha256"]=Hash(sourcePath),["operationMapId"]=sourceDefinition.CampaignMap.OperationMapId,
                    ["sourceIdentityHash"]=sourceDefinition.CampaignMap.SourceIdentityHash,["contentHash"]=sourceDefinition.CampaignMap.ContentHash},
                ["grid"]=new JObject{["path"]=gridPath,["sha256"]=Hash(gridPath)},
                ["surface"]=new JObject{["path"]=surfacePath,["sha256"]=Hash(surfacePath)},
                ["scene"]=new JObject{["guid"]=sceneGuid,["path"]=scenePath,["sha256"]=Hash(scenePath)}};
            var dependencyPins=new JArray();
            foreach(var asset in new UnityEngine.Object[]{sourceDefinition.ObjectiveConfig,sourceDefinition.ArmyProfileConfig,sourceDefinition.StartPackageConfig,
                sourceDefinition.EconomyConfig,sourceDefinition.UpgradeCatalog,sourceDefinition.IntelConfig,sourceDefinition.RoleCatalog,sourceDefinition.MissionLogicalBinding})
            {
                string path=AssetDatabase.GetAssetPath(asset);
                Require(!string.IsNullOrEmpty(path)&&!EditorUtility.IsDirty(asset),"Clean exact staged S049 dependency required");
                dependencyPins.Add(new JObject{["path"]=path,["sha256"]=Hash(path)});
            }
            pins["stagedDependencies"]=dependencyPins;
            var report=new JObject{["schema"]=1,["catalogId"]="S049",
                ["scope"]="DisposableLogicalPadCandidate_OriginalArmorBreakSource_StaticAllocatorOnly",
                ["sourcePins"]=pins,["candidateOnly"]=true,["persistentAssetWrites"]=false,["runtimeCatalogChanged"]=false,
                ["physicalSourceChanged"]=false,["objectiveAssetChanged"]=false,["sourceFitCertified"]=false,["waterQualified"]=false,
                ["cameraQualified"]=false,["missionReady"]=false,["playerAccepted"]=false,["fullEditorSourceClosureQualified"]=false,
                ["candidateRevision"]="Native03_KeepPlayerEconomyAtOriginalZ700",
                ["candidateNoOpPreserved"]=new JArray(new JObject{["padId"]="pad.skirmish.db.supply_player",
                    ["anchorId"]="anchor.pilot.economy.player",["field"]="CenterZ",["value"]=700,
                    ["reason"]="Native-02 moved this pad north and the only source-valid depot candidate overlapped Rally; Native-05 found depot rect (644,442,19x16) inside the original typed Economy pad with no QA overlap, excluded only by the old 2-cell compound edge"}),
                ["sourceClosureLimit"]="This receipt pins the staged assets and original physical source inputs; it is not a full 8032-file Editor source closure receipt",
                ["result"]="Incomplete"};
            var changes=new JArray();report["candidateChanges"]=changes;
            JObject scenesBefore=null;string[] worldsBefore=null;World previousDefault=null;bool baselineCaptured=false;
            Exception primaryFailure=null,cleanupFailure=null;bool cleanupRestored=false;
            GroundObjective30CandidateOwner owner=null;
            mpGroundBaPilotRequested=true;
            try
            {
                report["companionPrewarm"]=Ground30PrewarmCompanions();
                scenesBefore=Ground30SceneOwnership();worldsBefore=CaptureMaterializationWorldCensus();
                previousDefault=World.DefaultGameObjectInjectionWorld;baselineCaptured=true;
                owner=new GroundObjective30CandidateOwner();
                owner.Layout=owner.Own(ScriptableObject.Instantiate(sourceLayout));
                owner.Definition=owner.Own(ScriptableObject.Instantiate(sourceDefinition));
                var oldLayout=JObject.Parse(originalLayoutJson,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                var candidateLayoutJson=(JObject)oldLayout.DeepClone();
                var anchors=((JArray)candidateLayoutJson["anchors"]).OfType<JObject>().ToDictionary(a=>(string)a["AnchorId"]);
                var pads=((JArray)candidateLayoutJson["pads"]).OfType<JObject>().ToDictionary(p=>(string)p["PadId"]);
                void MovePad(string padId,string anchorId,string field,float oldValue,float newValue,bool padX)
                {
                    var pad=pads[padId];var anchor=anchors[anchorId];
                    float oldPad=padX?(float)pad["CenterX"]:(float)pad["CenterZ"];
                    float oldAnchor=padX?(float)anchor["WorldX"]:(float)anchor["WorldZ"];
                    Require(math.abs(oldPad-oldValue)<.002f,"Expected original typed pad value changed: "+padId);
                    float updatedAnchor=newValue+(oldAnchor-oldPad);
                    if(padX){pad["CenterX"]=newValue;anchor["WorldX"]=updatedAnchor;anchor["NormalizedU"]=(updatedAnchor-owner.Layout.OriginX)/owner.Layout.WorldWidthMetres;}
                    else {pad["CenterZ"]=newValue;anchor["WorldZ"]=updatedAnchor;anchor["NormalizedV"]=(updatedAnchor-owner.Layout.OriginZ)/owner.Layout.WorldDepthMetres;}
                    changes.Add(new JObject{["padId"]=padId,["anchorId"]=anchorId,["field"]=field,["padBefore"]=oldPad,["padAfter"]=newValue,
                        ["anchorBefore"]=oldAnchor,["anchorAfter"]=updatedAnchor,["typedIdentityPreserved"]=true});
                }
                MovePad("pad.skirmish.db.supply_enemy","anchor.pilot.economy.enemy","CenterZ",700,680,false);
                MovePad("pad.skirmish.db.spawn_vehicle_enemy","anchor.pilot.spawn.vehicle.enemy","CenterX",1698.5f,1714.5f,true);
                candidateLayoutJson["anchors"]=new JArray(anchors.Values.OrderBy(a=>(string)a["AnchorId"]));
                candidateLayoutJson["pads"]=new JArray(pads.Values.OrderBy(p=>(string)p["PadId"]));
                JsonUtility.FromJsonOverwrite(candidateLayoutJson.ToString(Newtonsoft.Json.Formatting.None),owner.Layout);
                string candidateLayoutFingerprint=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(owner.Layout);
                var candidateLayoutSerialized=new SerializedObject(owner.Layout);
                var contentHashProperty=candidateLayoutSerialized.FindProperty("contentHash");
                Require(contentHashProperty!=null,"Candidate layout contentHash field must exist");
                contentHashProperty.stringValue="logical.refinement.v1."+candidateLayoutFingerprint;
                candidateLayoutSerialized.ApplyModifiedPropertiesWithoutUndo();
                Require(owner.Layout.OperationMapId==sourceDefinition.CampaignMap.OperationMapId&&
                    owner.Layout.WorldBinding.SourceHash==sourceDefinition.CampaignMap.ContentHash&&
                    owner.Layout.WorldBinding.HasMeasuredDeployments==sourceLayout.WorldBinding.HasMeasuredDeployments&&
                    owner.Layout.WorldBinding.BindAllConfigurations==sourceLayout.WorldBinding.BindAllConfigurations,
                    "Candidate must preserve original Campaign binding and deployment flags");
                var sourceSerialized=new SerializedObject(owner.Definition);var layoutProperty=sourceSerialized.FindProperty("mapLayout");
                Require(layoutProperty!=null,"Candidate mapLayout field must exist");layoutProperty.objectReferenceValue=owner.Layout;sourceSerialized.ApplyModifiedPropertiesWithoutUndo();
                Require(owner.Definition.MapLayout==owner.Layout&&owner.Definition.CampaignMap==sourceDefinition.CampaignMap&&
                    owner.Definition.ObjectiveConfig==sourceDefinition.ObjectiveConfig&&owner.Definition.CatalogId=="S049"&&
                    owner.Definition.DefinitionId==sourceDefinition.DefinitionId&&owner.Definition.ContentVersion==sourceDefinition.ContentVersion,
                    "Only candidate mapLayout reference may change on cloned definition");
                report["candidateLayoutFingerprint"]=candidateLayoutFingerprint;
                report["candidateCompoundWorldRects"]=new JObject{
                    ["1"]=new JArray(1324f,636f,1420f,720f),["2"]=new JArray(1580f,636f,1740f,720f)};
                foreach(JArray bounds in ((JObject)report["candidateCompoundWorldRects"]).Properties().Select(p=>(JArray)p.Value))
                    Require((float)bounds[0]>=owner.Layout.OriginX&&(float)bounds[2]<=owner.Layout.OriginX+owner.Layout.WorldWidthMetres&&
                        (float)bounds[1]>=owner.Layout.OriginZ&&(float)bounds[3]<=owner.Layout.OriginZ+owner.Layout.WorldDepthMetres,
                        "Candidate authoring bound escaped unchanged logical source frame");
                report["candidateCompoundInterpretation"]="Transient authoring search bounds, constrained again by exact candidate typed pads; no geometry or runtime asset is changed";

                Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string matrixError),matrixError);
                var budgets=new List<SkirmishStaticAuthoringBudget>();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                        budget.ExactCanonicalQuantities,"Disposable candidate canonical budget failed for "+size+": "+string.Join(";",reasons));
                    budgets.Add(budget);
                }
                report["budgets"]=new JArray(budgets.Select(b=>new JObject{["size"]=b.Size.ToString(),
                    ["forces"]=JArray.FromObject(b.Forces),["structures"]=JArray.FromObject(b.Structures),["exactCanonicalQuantities"]=b.ExactCanonicalQuantities}));
                using(var world=StreamingWorld())
                {
                    Load(world,sceneGuid);Load(world,AssetDatabase.AssetPathToGUID(Support));
                    using var runtimeSurface=PublishMissingPinnedSurface(world,sourceDefinition.CampaignMap,surfacePath,report);
                    var em=world.EntityManager;
                    using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(GridRoad));
                    using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
                    Require(grids.CalculateEntityCount()==1&&surfaces.CalculateEntityCount()==1,"One original Campaign native grid/surface required");
                    var gridEntity=grids.GetSingletonEntity();var grid=grids.GetSingleton<GridConfig>();var surface=surfaces.GetSingleton<MapSurfaceComponent>();
                    Require(grid.Width==sourceDefinition.CampaignMap.GridMetadata.Dimensions.x&&grid.Height==sourceDefinition.CampaignMap.GridMetadata.Dimensions.y&&
                        grid.CellSize==sourceDefinition.CampaignMap.GridMetadata.CellSize&&math.all(grid.Origin==(float3)sourceDefinition.CampaignMap.GridMetadata.Origin)&&
                        surface.HasSurfaceData!=0&&surface.SurfaceBlob.IsCreated&&surface.Dimensions.Equals(new int2(grid.Width,grid.Height))&&
                        surface.CellSize==grid.CellSize&&math.all(surface.GridOrigin==grid.Origin),"Loaded original Campaign grid/surface mismatch");
                    Require(TryPreflightAuthoredRouteEndpoints(owner.Layout,grid,out var endpointPolicy),"Candidate typed route endpoint policy refused: "+endpointPolicy);
                    report["routeEndpointPolicy"]=endpointPolicy;
                    var baselineRoutes=new JArray();var approachClearance=new List<RectInt>();var distinctApproachClearance=new HashSet<RectInt>();
                    foreach(var budget in budgets)
                    {
                        var baseline=Ground30RouteQueries(budget,owner.Layout,em,gridEntity,grid,surface,new JArray());
                        baselineRoutes.Add(baseline);
                        foreach(JObject query in (JArray)baseline["queries"])
                        {
                            if((string)query["status"]!="NativeQueryFoundExactGoal"||(int?)query["failureCode"]!=0)continue;
                            var footprint=new int2((int)query["footprintX"],(int)query["footprintZ"]);
                            foreach(JArray point in (JArray)query["pathCells"])
                            {
                                var cell=new int2((int)point[0],(int)point[1]);bool entryZone=false;
                                foreach(byte side in new byte[]{1,2})
                                {
                                    Require(owner.Layout.TryGetPad(side,SkirmishLegalPadKind.GroundStaging,out var staging),"Candidate faction GroundStaging pad required");
                                    var zone=PilotExactAxisAlignedPadCells(owner.Layout,staging,grid);
                                    int zx=zone.x+zone.width/2,zz=zone.y+zone.height/2;
                                    entryZone|=zone.Contains(new Vector2Int(cell.x,cell.y))&&math.abs(cell.x-zx)<=16&&math.abs(cell.y-zz)<=16;
                                }
                                if(entryZone)continue;
                                var min=UnitFootprintUtility.GetMinCell(cell,footprint);
                                var rect=new RectInt(min.x,min.y,footprint.x,footprint.y);
                                if(distinctApproachClearance.Add(rect))approachClearance.Add(rect);
                            }
                        }
                    }
                    report["baselineNativeApproaches"]=baselineRoutes;
                    report["approachFootprintReservations"]=new JArray(approachClearance.Select(RectJson));
                    var inputs=new List<SkirmishMissionLogicalAuthoringInputs>();
                    foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                    {
                        Require(SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                            new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var inputsForSize,out var reasons),
                            "Candidate logical inputs failed after binding: "+string.Join(";",reasons));inputs.Add(inputsForSize);
                    }
                    var engineeringTargets=CreateRefinementAircraftEngineeringTargets(inputs,em,out var targetFacts);
                    report["aircraftEngineeringTargets"]=targetFacts;
                    var variants=new List<SkirmishMissionLogicalVariant>();
                    foreach(var input in inputs)
                    {
                        Require(input.ObjectiveKind==SkirmishObjectiveKind.BaseAssault,"Exact existing Base Assault objective required");
                        var variant=CreateRefinementEngineeringView(owner.Definition,input,em,null,engineeringTargets);
                        owner.Own(variant.View);variants.Add(variant);
                    }
                    Require(TryAttachRefinementLogicalBinding(owner.Definition,variants.ToArray(),matrix,out var binding,out string bindingError),
                        "Disposable candidate logical binding rebuild failed: "+bindingError);
                    owner.Own(binding);
                    var bindingRows=new JArray();report["candidateBindingValidation"]=bindingRows;
                    foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                    {
                        Require(SkirmishSetupCompiler.TryCompile(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                            new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var setup,out var setupReasons),
                            "Candidate setup compile failed for "+size+": "+string.Join(";",setupReasons));
                        Require(SkirmishMissionLogicalBindingCompiler.TryCompile(owner.Definition,setup,out var compiled,out string compileError),
                            "Candidate mission logical binding compile failed for "+size+": "+compileError);
                        bindingRows.Add(new JObject{["size"]=size.ToString(),["setupHash"]=setup.SetupHash.ToString("x8"),
                            ["layoutFingerprint"]=setup.LayoutBindingFingerprint,["missionBindingFingerprint"]=compiled.Fingerprint,
                            ["sourceIdentityHash"]=owner.Definition.CampaignMap.SourceIdentityHash,
                            ["bindingSourceOperationMapId"]=binding.SourceOperationMapId,["bindingSourceSceneGuid"]=binding.SourceSceneGuid,
                            ["bindingSourceContentHash"]=binding.SourceContentHash,["bindingDefinitionId"]=binding.DefinitionId,
                            ["bindingCatalogId"]=binding.CatalogId,["reviewedLayoutSemanticFingerprint"]=binding.ReviewedLayoutSemanticFingerprint,
                            ["objectiveKind"]=setup.ObjectiveKind.ToString(),["passed"]=true});
                    }
                    var rows=new JArray();report["sizes"]=rows;
                    var allFacilities=new JArray();
                    foreach(var budget in budgets)
                    {
                        var preferred=Ground30PreferredFacilities(budget,owner.Layout,grid);foreach(var row in preferred)allFacilities.Add(row.DeepClone());
                        rows.Add(new JObject{["size"]=budget.Size.ToString(),["allSizeFacilities"]=preferred});
                    }
                    var placementReport=new JObject{["group"]="MP",["stagedAssetReadOnly"]=true,
                        ["candidateCompoundWorldRects"]=report["candidateCompoundWorldRects"].DeepClone(),["allSizeFacilities"]=allFacilities};
                    ProbePilotNormalFacilityRelocation(new JObject(),System.Array.Empty<JObject>(),owner.Layout,owner.Definition.RoleCatalog,
                        em,gridEntity,grid,surface,placementReport,false,false,approachClearance,serviceAuthority:owner.Definition);
                    report["normalFacilityPlacement"]=placementReport;
                    report["normalFacilityPlacementInterpretation"]="Every returned origin is a disposable static allocator proposal constrained to the candidate typed pads and transient candidate bounds; none is a saved authored placement or live clearance certificate";
                    foreach(var row in rows.OfType<JObject>())
                    {
                        string size=(string)row["size"];var budget=budgets.Single(b=>b.Size.ToString()==size);
                        var placements=((JArray)placementReport["normalFacilityRelocationProposals"]).OfType<JObject>().Where(f=>(string)f["size"]==size).ToArray();
                        var expected=(JArray)row["allSizeFacilities"];
                        var proposalAudit=AuditS049AllocatorProposals(expected,placements,owner.Layout,grid,
                            placementReport,approachClearance);
                        row["actualAllocatorProposalAudit"]=proposalAudit;
                        bool complete=(bool)proposalAudit["passed"]&&placements.Length==budget.Structures.Length&&
                            placements.All(f=>(bool?)f["entireConfigurationFound"]==true);
                        row["completeStaticFacilityConfiguration"]=complete;
                        if(!complete){row["armyPacking"]="NotRun_IncompleteOrInvalidAllocatorProposals";row["routeQueries"]="NotRun_IncompleteOrInvalidAllocatorProposals";continue;}
                        using var typed=new SkirmishTypedSpawnConstraint(em,gridEntity,grid,owner.Layout);
                        var facilities=new JArray(placements.Select(f=>f.DeepClone()));
                        var packed=Ground30PackBudget(budget,owner.Layout,em,gridEntity,grid,surface,facilities,typed);row["armyPacking"]=packed;
                        bool actors=packed.Count==budget.Forces.Length&&packed.OfType<JObject>().All(f=>
                            (int?)f["foundCount"]==(int?)f["requestedCount"]&&(int?)f["surfaceTraversableCount"]==(int?)f["requestedCount"]&&
                            (int?)f["insideTypedPadCount"]==(int?)f["requestedCount"]);
                        row["allCanonicalActorsFitTypedPads"]=actors;
                        var routes=Ground30RouteQueries(budget,owner.Layout,em,gridEntity,grid,surface,facilities);row["routeQueries"]=routes;
                        row["allCanonicalRouteQueriesExact"]=JointRoutesExact(routes);
                        row["routeProfilesCovered"]=MpPilotCanonicalRouteCoverage(budget,owner.Layout,em,grid,routes);
                        row["routesIndependent"]=PreferencePathsIndependent(owner.Layout,grid,routes,out var independence);row["routeIndependenceDetails"]=independence;
                        row["serviceAccess"]=actors?MpPilotServiceAccess(budget,owner.Definition,owner.Layout,em,gridEntity,grid,surface,facilities,packed):
                            new JObject{["status"]="NotRun_ActorPackingIncomplete",["allMembersHaveExactServiceIngressAndReturn"]=false};
                    }
                    report["staticCandidateSubgatesPassed"]=rows.OfType<JObject>().All(r=>(bool?)r["completeStaticFacilityConfiguration"]==true&&
                        (bool?)r["allCanonicalActorsFitTypedPads"]==true&&(bool?)r["allCanonicalRouteQueriesExact"]==true&&
                        (bool?)r["routeProfilesCovered"]==true&&(bool?)r["routesIndependent"]==true&&
                        (bool?)r["serviceAccess"]?["allMembersHaveExactServiceIngressAndReturn"]==true);
                    report["result"]=(bool)report["staticCandidateSubgatesPassed"]?
                        "StaticCandidateSubgatesPassed_WaterCameraAndMissionAcceptancePending":"StaticCandidateSubgateFailedOrUnresolved";
                    // Advance only Standard through the canonical starting-building
                    // grant and shared runtime placement owner. This is a separate
                    // measured subgate; it cannot promote source fit or readiness.
                    var standardRuntimeBounds=(JObject)report["candidateCompoundWorldRects"].DeepClone();
                    standardRuntimeBounds["standardFacilities"]=new JArray(
                        ((JArray)placementReport["normalFacilityRelocationProposals"]).OfType<JObject>()
                            .Where(r=>(string)r["size"]==SkirmishSizeId.Standard.ToString()).Select(r=>r.DeepClone()));
                    report["standardRuntimePlacement"]=ProbeS049StandardRuntimePlacement(owner.Definition,owner.Layout,matrix,standardRuntimeBounds);
                    report["liveRepairTested"]=false;
                    report["otherSizesLiveTested"]=false;
                }
                Require(JsonUtility.ToJson(sourceDefinition)==originalDefinitionJson&&JsonUtility.ToJson(sourceLayout)==originalLayoutJson&&
                    Hash(definitionPath)==(string)pins["stagedDefinition"]["sha256"]&&Hash(layoutPath)==(string)pins["stagedLayout"]["sha256"]&&
                    !EditorUtility.IsDirty(sourceDefinition)&&!EditorUtility.IsDirty(sourceLayout),"Persistent staged S049 assets changed during disposable candidate probe");
                foreach(string key in new[]{"source","grid","surface","scene"})
                {var pin=(JObject)pins[key];Require(Hash((string)pin["path"])==(string)pin["sha256"],"Original Campaign input changed: "+key);}
                foreach(JObject pin in dependencyPins.OfType<JObject>())Require(Hash((string)pin["path"])==(string)pin["sha256"],"Staged S049 dependency changed");
                report["persistentInputsRechecked"]=true;
            }
            catch(Exception failure)
            {
                primaryFailure=failure;report["error"]=failure.ToString();report["primaryFailure"]=failure.ToString();report["result"]="CandidateProbeFailed";
            }
            finally
            {
                try{owner?.Dispose();}catch(Exception disposeFailure){cleanupFailure=disposeFailure;}
                mpGroundBaPilotRequested=false;
                try
                {
                    if(!baselineCaptured)throw new InvalidOperationException("Cleanup census unavailable because prewarmed baseline was not captured");
                    var worldsAfter=CaptureMaterializationWorldCensus();var scenesAfter=Ground30SceneOwnership();
                    bool worldsRestored=worldsBefore.SequenceEqual(worldsAfter);
                    bool defaultRestored=ReferenceEquals(previousDefault,World.DefaultGameObjectInjectionWorld);
                    bool scenesRestored=JToken.DeepEquals(scenesBefore,scenesAfter);
                    cleanupRestored=worldsRestored&&defaultRestored&&scenesRestored;
                    report["cleanupChecks"]=new JObject{["worldCensusRestored"]=worldsRestored,["defaultWorldIdentityRestored"]=defaultRestored,
                        ["sceneOwnershipRestored"]=scenesRestored,["worldsBefore"]=JArray.FromObject(worldsBefore),["worldsAfter"]=JArray.FromObject(worldsAfter),
                        ["scenesBefore"]=scenesBefore.DeepClone(),["scenesAfter"]=scenesAfter};
                    report["cleanupVerified"]=cleanupRestored;
                    if(!cleanupRestored)cleanupFailure=new InvalidOperationException("Disposable S049 candidate probe cleanup checks failed");
                }
                catch(Exception failure){cleanupFailure=failure;report["cleanupVerified"]=false;report["cleanupFailure"]=failure.ToString();}
                if(cleanupFailure!=null)report["cleanupFailure"]=cleanupFailure.ToString();
                if(primaryFailure!=null&&cleanupFailure!=null)
                    report["combinedFailure"]=new AggregateException("Candidate probe and cleanup both failed",primaryFailure,cleanupFailure).ToString();
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(output));report["receiptPersisted"]=true;
                    using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                    using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                    {writer.Write(report.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
                }
                catch(Exception failure)
                {if(cleanupFailure==null)cleanupFailure=failure;else cleanupFailure=new AggregateException("Cleanup and receipt persistence failed",cleanupFailure,failure);}
            }
            if(primaryFailure!=null&&cleanupFailure!=null)throw new AggregateException("S049 logical candidate probe and cleanup failed; inspect receipt",primaryFailure,cleanupFailure);
            if(primaryFailure!=null)ExceptionDispatchInfo.Capture(primaryFailure).Throw();
            if(cleanupFailure!=null)ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
            Debug.Log("[S049DisposableLogicalPadCandidateProbe] result=Collected sizes=3 candidateOnly=1 sourceFitCertified=0 missionReady=0 output="+output);
        }

        // Independently audit the exact normal-allocator rectangles that are handed to
        // packing, route, and service queries. This remains a static proposal check; it
        // does not observe live occupancy or certify runtime placement.
        static JObject AuditS049AllocatorProposals(JArray expected,JObject[] proposals,SkirmishMapLayoutConfig layout,
            GridConfig grid,JObject placementReport,List<RectInt> plannedApproachClearance)
        {
            var rows=new JArray();var errors=new JArray();
            var declaredCompoundWorldRects=(JObject)placementReport["candidateCompoundWorldRects"];
            var expectedByKey=expected.OfType<JObject>().ToDictionary(S049FacilityKey,StringComparer.Ordinal);
            var proposalByKey=new Dictionary<string,JObject>(StringComparer.Ordinal);
            foreach(var proposal in proposals)
            {
                string key=S049FacilityKey(proposal);
                if(proposalByKey.ContainsKey(key))errors.Add(new JObject{["key"]=key,["error"]="DuplicateAllocatorProposal"});
                else proposalByKey[key]=proposal;
            }
            foreach(var entry in expectedByKey)
            {
                var failures=new JArray();
                if(!proposalByKey.TryGetValue(entry.Key,out var proposal))
                {
                    failures.Add("MissingAllocatorProposal");
                    rows.Add(new JObject{["key"]=entry.Key,["passed"]=false,["failures"]=failures});
                    continue;
                }
                var expectedRow=entry.Value;
                if((bool?)proposal["entireConfigurationFound"]!=true)failures.Add("AllocatorDidNotFindEntireConfiguration");
                int x=(int?)proposal["originX"]??int.MinValue,z=(int?)proposal["originZ"]??int.MinValue;
                int width=(int?)proposal["widthCells"]??0,depth=(int?)proposal["depthCells"]??0;
                var resolved=proposal["resolvedEffectiveRect"] as JObject;
                bool resolvedMatches=resolved!=null&&(int?)resolved["x"]==x&&(int?)resolved["z"]==z&&
                    (int?)resolved["width"]==width&&(int?)resolved["depth"]==depth;
                if(!resolvedMatches)failures.Add("AllocatorResolvedRectMismatch");
                if(width<=0||depth<=0)failures.Add("NonPositiveFootprint");
                var actualModel=proposal["actualModelFootprintCells"] as JArray;
                int modelWidth=actualModel!=null&&actualModel.Count==2?(int)actualModel[0]:0;
                int modelDepth=actualModel!=null&&actualModel.Count==2?(int)actualModel[1]:0;
                bool modelFootprintMatches=actualModel!=null&&actualModel.Count==2&&modelWidth==width&&modelDepth==depth;
                if(!modelFootprintMatches)failures.Add("SelectedRectangleDiffersFromActualProductionModelFootprint");
                var allocatedRect=new RectInt(x,z,width,depth);
                var configuredPreferenceRect=new RectInt((int?)expectedRow["originX"]??int.MinValue,
                    (int?)expectedRow["originZ"]??int.MinValue,(int?)expectedRow["widthCells"]??0,(int?)expectedRow["depthCells"]??0);
                bool selectedContainedByConfiguredPreference=PilotCompoundContains(configuredPreferenceRect,allocatedRect);
                if(x<0||z<0||x+width>grid.Width||z+depth>grid.Height)failures.Add("OutsideNativeGrid");
                var qaOverlaps=new JArray();
                if(!layout.TryGetPad((byte)expectedRow["faction"],SkirmishLegalPadKind.GroundStaging,out var staging)||
                    !layout.TryProjectLocalToMap(staging.CenterX,staging.CenterZ,out float cx,out float cz))
                    failures.Add("MissingTypedGroundStagingPad");
                else
                {
                    var stagingCompound=PilotRoundPadCells(cx-48,cz-40,cx+48,cz+40,grid,false);
                    var declared=declaredCompoundWorldRects?[((int)expectedRow["faction"]).ToString()] as JArray;
                    var compound=declared!=null&&declared.Count==4
                        ?PilotRoundPadCells((float)declared[0],(float)declared[1],(float)declared[2],(float)declared[3],grid,false)
                        :stagingCompound;
                    var rect=new RectInt(x,z,width,depth);
                    if(!PilotCompoundContains(compound,rect))failures.Add("OutsideDeclaredSizeGroupCompound");
                    string padId=(string)expectedRow["padId"];var typed=layout.Pads.Where(p=>p.FactionId==(byte)expectedRow["faction"]&&p.PadId==padId).ToArray();
                    if(typed.Length!=1)failures.Add("MissingOrAmbiguousTypedFacilityPad");
                    else
                    {
                        var actualPad=PilotExactAxisAlignedPadCells(layout,typed[0],grid);
                        if(!PilotCompoundContains(actualPad,allocatedRect))failures.Add("OutsideTypedFacilityPad");
                        if(proposal["actualTypedFacilityPadCells"] is JObject recordedPad&&
                            ((int?)recordedPad["x"]!=actualPad.x||(int?)recordedPad["z"]!=actualPad.y||
                             (int?)recordedPad["width"]!=actualPad.width||(int?)recordedPad["depth"]!=actualPad.height))
                            failures.Add("RecordedTypedPadBoundsDifferFromCurrentLayout");
                        else if(proposal["actualTypedFacilityPadCells"]==null)failures.Add("TypedFacilityPadBoundsNotRecorded");
                    }
                    foreach(var kind in new[]{SkirmishLegalPadKind.InfantrySpawn,SkirmishLegalPadKind.VehicleSpawn,SkirmishLegalPadKind.Rally})
                    {
                        if(!layout.TryGetPad((byte)expectedRow["faction"],kind,out var clearancePad))
                        {failures.Add("MissingTypedCirculationPad_"+kind);continue;}
                        var keepClear=PilotExactAxisAlignedPadCells(layout,clearancePad,grid,keepClear:true);
                        if(allocatedRect.Overlaps(keepClear))qaOverlaps.Add(new JObject{["source"]="TypedPad_"+kind,["rect"]=RectJson(keepClear)});
                    }
                    var serviceRows=(placementReport["nativeRepairInteractionClearance"] as JArray)?.OfType<JObject>()
                        .FirstOrDefault(r=>(string)r["size"]==(string)expectedRow["size"]&&(int?)r["faction"]==(int?)expectedRow["faction"]);
                    if(serviceRows==null||(bool?)serviceRows["allStaticFootprintsAvailableAndLegal"]!=true)
                        failures.Add("ExactNativeRepairInteractionClearanceUnavailable");
                    else foreach(var roleRow in (serviceRows["roles"] as JArray)?.OfType<JObject>()??Enumerable.Empty<JObject>())
                    {
                        if(roleRow["reservedInteractionRect"] is not JObject interactionRect)
                        {failures.Add("NativeRepairInteractionRectMissing");continue;}
                        var keepClear=new RectInt((int)interactionRect["x"],(int)interactionRect["z"],
                            (int)interactionRect["width"],(int)interactionRect["depth"]);
                        if(allocatedRect.Overlaps(keepClear))qaOverlaps.Add(new JObject{["source"]="NativeRepairInteraction_"+(string)roleRow["role"],["rect"]=RectJson(keepClear)});
                    }
                    if(plannedApproachClearance!=null)foreach(var keepClear in plannedApproachClearance)
                        if(allocatedRect.Overlaps(keepClear))qaOverlaps.Add(new JObject{["source"]="PlannedApproachClearance",["rect"]=RectJson(keepClear)});
                }
                bool selectedQaClear=qaOverlaps.Count==0;
                if(!selectedQaClear)failures.Add("SelectedAllocatorRectOverlapsAdditionalQAClearance");
                var noQaRect=proposal["normalStaticPlacementWithoutAdditionalQa"]?["resolvedEffectiveRect"];
                bool noQaRectMatchesSelected=noQaRect is JObject q&&resolved!=null&&JToken.DeepEquals(q,resolved);
                rows.Add(new JObject{["key"]=entry.Key,["originX"]=x,["originZ"]=z,["widthCells"]=width,["depthCells"]=depth,
                    ["resolvedRectMatchesConsumedRectangle"]=resolvedMatches,["passed"]=failures.Count==0,["failures"]=failures,
                    ["actualProductionModelFootprintCells"]=new JArray(modelWidth,modelDepth),["selectedRectangleMatchesActualProductionModelFootprint"]=modelFootprintMatches,
                    ["configuredPreferredFootprintCells"]=new JArray(configuredPreferenceRect.width,configuredPreferenceRect.height),
                    ["configuredPreferredFootprintRect"]=RectJson(configuredPreferenceRect),
                    ["selectedRectContainedByConfiguredPreferredFootprint"]=selectedContainedByConfiguredPreference,
                    ["configuredPreferredFootprintComparisonIsNonGating"]=true,
                    ["selectedRectangleQaClear"]=selectedQaClear,["selectedRectangleQaOverlaps"]=qaOverlaps,
                    ["noAdditionalQaSearchRectangleMatchesSelectedProposal"]=noQaRectMatchesSelected,
                    ["noAdditionalQaClearanceFlagUsedAsSelectedProposalProof"]=false,["liveOccupancyMeasured"]=false});
            }
            foreach(var extra in proposalByKey.Keys.Where(k=>!expectedByKey.ContainsKey(k)))
                errors.Add(new JObject{["key"]=extra,["error"]="UnexpectedAllocatorProposal"});
            var overlaps=new JArray();
            for(int i=0;i<proposals.Length;i++)for(int j=i+1;j<proposals.Length;j++)
            {
                if((string)proposals[i]["size"]!=(string)proposals[j]["size"])continue;
                var a=new RectInt((int)proposals[i]["originX"],(int)proposals[i]["originZ"],(int)proposals[i]["widthCells"],(int)proposals[i]["depthCells"]);
                var b=new RectInt((int)proposals[j]["originX"],(int)proposals[j]["originZ"],(int)proposals[j]["widthCells"],(int)proposals[j]["depthCells"]);
                if(a.Overlaps(b))overlaps.Add(new JObject{["first"]=S049FacilityKey(proposals[i]),["second"]=S049FacilityKey(proposals[j]),["error"]="AllocatorRectanglesOverlap"});
            }
            return new JObject{["scope"]="ExactPerSizeNormalAllocatorRectanglesConsumedByStaticQueries",
                ["footprintInterpretation"]="Selected footprint must equal actual production model footprint; configured prefab dimensions are retained as a non-gating preferred-point comparison, not treated as canonical budget or required selected-envelope containment",
                ["expectedFacilityCount"]=expected.Count,["actualFacilityCount"]=proposals.Length,["items"]=rows,["rosterErrors"]=errors,
                ["pairwiseOverlaps"]=overlaps,["liveOccupancyMeasured"]=false,
                ["passed"]=errors.Count==0&&overlaps.Count==0&&rows.Count==expected.Count&&rows.OfType<JObject>().All(r=>(bool)r["passed"])};
        }

        static string S049FacilityKey(JObject row)=>string.Join("|",(string)row["catalogId"],(string)row["size"],
            (string)row["faction"],(string)row["structureId"],(string)row["padId"]);
    }
}
#endif
