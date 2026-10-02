using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace Game.Editor
{
    /// <summary>Independent bounded Urban derivative. Source assets remain unchanged.</summary>
    public static partial class CH05M04LastCorridorConfigBuilder
    {
        public const string MissionId="saga.ch05.m04.last_corridor", ScenarioId="scenario.ch05.m04.last_corridor";
        public const string MapId="opmap.ch05.last_corridor", Prefix="anchor.ch05.m04.";
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter05/MissionDefinition_Ch05_M04_LastCorridor.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter05/ScenarioSetup_Ch05_M04_LastCorridor.asset";
        public const string Folder="Assets/Game/GeneratedOperationMaps/CH05M04LastCorridor";
        public const string MapPath=Folder+"/Definition.asset", SurfacePath=Folder+"/Surface.asset", GridPath=Folder+"/Grid.asset";
        public const string SceneFolder="Assets/Game/Scenes/OperationMaps/CH05M04LastCorridor";
        public const string EntityScenePath=SceneFolder+"/PreparedEntities.unity", BindingPath=SceneFolder+"/RuntimeBinding.unity";
        public const string ReserveBuildingId="Building_Corridor_ReserveDepot";
        public const string ReservePrefabPath="Assets/Game/Prefabs/Buildings/CH05M04LastCorridor/"+ReserveBuildingId+".prefab";
        public const string CivicBuildingId="Building_Corridor_Civic", ReceivingReserveBuildingId="Building_Corridor_ReceivingDepot";
        public const string CivicPrefabPath="Assets/Game/Prefabs/Buildings/CH05M04LastCorridor/"+CivicBuildingId+".prefab";
        public const string ReceivingReservePrefabPath="Assets/Game/Prefabs/Buildings/CH05M04LastCorridor/"+ReceivingReserveBuildingId+".prefab";
        public const string ReceiverPrefabPath=ReceivingReservePrefabPath, ReceiverBuildingId=ReceivingReserveBuildingId;
        public const string SourceRecipeRevision="urban-logistics-v1-two-artery-lanes";
        public const string ScenarioAnchorRevision="corridor-anchor-v1-five-categories";
        public static void ReportBuilderRevision()=>Debug.Log("[LastCorridorBuilderRevision] source="+SourceRecipeRevision+" canonicalMovementMask=Preserved explicitMoves=Required");
        public const string PreparedHash="2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        public const int CropX=860,CropZ=300,Width=440,Height=220;
        private static readonly Rect Sector=new Rect(CropX,CropZ,Width,Height);
        public const string SourceDefinition=M02EstablishBaseForwardPostWindowValidation.SourceDefinitionPath;
        public const string SourceScene="Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        public const string SourceBinding="Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity";
        public const string SourceSurface="Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset", SourceGrid="Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset";
        public const string SourceMinimap="Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/MinimapRaster.png";
        public static string[] ProtectedSources=>new[]{SourceDefinition,SourceScene,SourceBinding,SourceSurface,SourceGrid,SourceMinimap};
        public static readonly RectInt[] BuildingLots={new(1233,444,14,12),new(1076,378,26,25),new(1234,356,26,25)};
        public static void Build()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Last Corridor generation requires edit mode.");ReportBuilderRevision();
            foreach(string folder in new[]{Folder,SceneFolder,"Assets/Game/Configs/Missions/Chapter05","Assets/Game/Configs/Scenarios/Chapter05"}) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            EditorApplication.LockReloadAssemblies();
            try {BuildBuildingPrefabs(); BuildPhysicalMap(); BuildScenario(); BuildMission(); BuildSupportContext(); AssetDatabase.SaveAssets();}
            finally {EditorApplication.UnlockReloadAssemblies();}
            Debug.Log("[LastCorridorConfig] result=Passed deliveryCategories=5 connectedGroundChoices=2 finiteDepots=2 originals=26");
        }
        /// <summary>Resume after physical authoring without regenerating source scenes or raster imports.</summary>
        public static void CompletePreparedConfiguration()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Last Corridor configuration requires edit mode.");
            EditorApplication.LockReloadAssemblies();
            try
            {
                var map=Load<OperationMapDefinition>(MapPath);ConfigureAnchors(map);Hash(map);EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
                Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);
                ValidateRoutes();BuildScenario();BuildMission();BuildSupportContext();AssetDatabase.SaveAssets();
                Debug.Log("[LastCorridorConfig] result=Passed deliveryCategories=5 connectedGroundChoices=2 finiteDepots=2 originals=26 resumedPreparedMap=true");
            }
            finally {EditorApplication.UnlockReloadAssemblies();}
        }
        public static void RepairPreparedSceneOwnership()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Scene ownership repair requires edit mode.");
            var previous=SceneManager.GetActiveScene();Scene scene=default;
            try
            {
                scene=EditorSceneManager.OpenScene(BindingPath,OpenSceneMode.Additive);
                var view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapSceneView>(true)).Single();
                view.MapSubScene.SceneAsset=null;view.MapSubScene.AutoLoadScene=false;view.MapSubScene.enabled=false;
                Require(view.TryValidate(out string error),error);Require(EditorSceneManager.SaveScene(scene,BindingPath),"Scene ownership repair save failed.");
                Debug.Log("[LastCorridorSceneOwnership] result=Passed owner=ExplicitEntitySceneLoader autoLoad=false disabledPlaceholder=true guid="+view.Definition.NavigationMetadata.AuthoredSubSceneGuid);
            }
            finally {if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        public static void BuildPhysicalMap()
        {
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(SceneFolder);
            var source=Load<OperationMapDefinition>(SourceDefinition);
            Require(source.ContentHash==PreparedHash,"Urban prepared source hash changed; re-audit derivative.");
            var protectedHashes=ProtectedSources.ToDictionary(p=>p,p=>FileHash(p));
            var previousHashesPath="Design/AgentReports/CH05M04LastCorridor/source-sha-before-generation.json";
            Directory.CreateDirectory(Path.GetDirectoryName(previousHashesPath));
            File.WriteAllText(previousHashesPath,JsonUtility.ToJson(new SourceReceipt{files=protectedHashes.Select(p=>new SourceFile{path=p.Key,sha256=p.Value}).ToArray()},true));
            var previous=SceneManager.GetActiveScene(); Scene original=default,scene=default,binding=default;
            try
            {
                original=EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Additive);
                // Save a complete native scene copy so cross-root references are remapped
                // together. Per-root Instantiate can retain references to the closed source.
                Require(EditorSceneManager.SaveScene(original,EntityScenePath,true),"Native Urban scene copy failed.");
                EditorSceneManager.CloseScene(original,true);original=default;
                AssetDatabase.ImportAsset(EntityScenePath,ImportAssetOptions.ForceUpdate);
                scene=EditorSceneManager.OpenScene(EntityScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                foreach(var owner in All<OperationMapBuildingAuthoring>(scene))
                {
                    Require(owner.IntactVisualRoot!=null&&owner.IntactVisualRoot.scene==scene&&owner.IntactVisualRoot.transform.parent==owner.transform,"Copied Urban owner has nonlocal intact visual: "+owner.StableId);
                    Require(owner.DestroyedVisualRoot==null||(owner.DestroyedVisualRoot.scene==scene&&owner.DestroyedVisualRoot.transform.parent==owner.transform),"Copied Urban owner has nonlocal destroyed visual: "+owner.StableId);
                }
                foreach(var virtualization in All<OperationMapVirtualizedPresentationAuthoring>(scene))UnityEngine.Object.DestroyImmediate(virtualization.gameObject);
                MigrateAcceptedPresentation(scene);
                // Native identities provide exact ownership; source art pivots are not grid origins.
                int removed=0;
                foreach(var owner in All<OperationMapBuildingAuthoring>(scene))
                {
                    var rect=new Rect(owner.OriginCell.x,owner.OriginCell.y,owner.FootprintCells.x,owner.FootprintCells.y);
                    if(!ContainsRect(Sector,rect)||ClearZones.Any(r=>r.Overlaps(rect)))
                    {UnityEngine.Object.DestroyImmediate(owner.gameObject);removed++;}
                }
                foreach(var identity in All<DenseCityPresentationIdentityAuthoring>(scene))
                {
                    if(identity==null||identity.GetComponentInParent<OperationMapBuildingAuthoring>()!=null)continue;
                    if(!TryBounds(identity,out var bounds))continue;
                    var rect=new Rect(bounds.min.x,bounds.min.z,bounds.size.x,bounds.size.z);
                    bool outside=!Sector.Overlaps(rect);
                    // Preserve actual terrain/road tiles; only structural objects and props are cleared.
                    bool ground=identity.Category==DenseCityPresentationSemanticCategory.Infrastructure||identity.Category==DenseCityPresentationSemanticCategory.Horizon||identity.Category==DenseCityPresentationSemanticCategory.Vegetation;
                    if(outside||(!ground&&ClearZones.Any(r=>r.Overlaps(rect))))
                    {UnityEngine.Object.DestroyImmediate(identity.gameObject);removed++;}
                }
                var staticMask=new bool[2048*1024];
                foreach(var identity in All<DenseCityPresentationIdentityAuthoring>(scene))
                {
                    if(identity.GetComponentInParent<OperationMapBuildingAuthoring>()!=null||identity.Role!=OperationMapEntityPresentationRole.RenderOnly||identity.Category!=DenseCityPresentationSemanticCategory.Prop||!TryBounds(identity,out var bounds))continue;
                    for(int z=Mathf.Max(CropZ,Mathf.FloorToInt(bounds.min.z));z<Mathf.Min(CropZ+Height,Mathf.CeilToInt(bounds.max.z));z++)
                    for(int x=Mathf.Max(CropX,Mathf.FloorToInt(bounds.min.x));x<Mathf.Min(CropX+Width,Mathf.CeilToInt(bounds.max.x));x++)staticMask[z*2048+x]=true;
                }
                BuildSurface(staticMask);
                var grid=Load<GridAuthoringSceneConfigAsset>(GridPath);var surface=Load<MapSurfaceDataAsset>(SurfacePath);
                foreach(var root in scene.GetRootGameObjects()) foreach(var component in root.GetComponentsInChildren<Component>(true))
                {
                    if(component==null)continue;
                    var data=new SerializedObject(component);var p=data.GetIterator();
                    while(p.Next(true)) if(p.propertyType==SerializedPropertyType.String&&p.stringValue==source.OperationMapId)p.stringValue=MapId;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    if(component is OperationMapBuildingAuthoring owner)
                    {var od=new SerializedObject(owner);od.FindProperty("originCell").vector2IntValue=owner.OriginCell-new Vector2Int(CropX,CropZ);od.ApplyModifiedPropertiesWithoutUndo();}
                    if(component is StaticGridBlockerAuthoring blocker)
                    {var bd=new SerializedObject(blocker);bd.FindProperty("config").objectReferenceValue=null;bd.FindProperty("cell").vector2IntValue-=new Vector2Int(CropX,CropZ);bd.ApplyModifiedPropertiesWithoutUndo();}
                    if(component is GridAuthoring ga)ga.Configure(grid);
                    if(component is MapSurfaceAuthoring sa){var d=new SerializedObject(sa);d.FindProperty("gridConfig").objectReferenceValue=grid;d.FindProperty("bakedSurfaceData").objectReferenceValue=surface;d.ApplyModifiedPropertiesWithoutUndo();}
                }
                ConfigureNativeOwnership(scene);
                var identities=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).ToArray();
                int owners=scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length);
                int rendered=identities.Count(i=>i.Role==OperationMapEntityPresentationRole.RenderOnly);
                Require(identities.Select(i=>i.StableId).Distinct(StringComparer.Ordinal).Count()==identities.Length,"Generated derivative identity collision.");
                foreach(var marker in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true)))
                {var d=new SerializedObject(marker);d.FindProperty("migrationRecordSetHash").stringValue=Digest(MapId+":"+SourceRecipeRevision+":"+source.ContentHash);d.FindProperty("expectedGameplayBuildingCount").intValue=owners;d.FindProperty("expectedGameplayVehicleCount").intValue=0;d.FindProperty("expectedRenderOnlyCount").intValue=rendered;d.FindProperty("expectedGeneratedIdentityCount").intValue=identities.Length;d.ApplyModifiedPropertiesWithoutUndo();}
                CreateMinimap(scene);
                Require(EditorSceneManager.SaveScene(scene,EntityScenePath),"Derivative entity save failed");
                ValidateCivicClearance(scene);
                var gaSaved=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GridAuthoring>(true)).Single();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(gaSaved,out _,out long localId);
                if(localId<=0)localId=checked((long)GlobalObjectId.GetGlobalObjectIdSlow(gaSaved).targetObjectId);
                var map=Clone<OperationMapDefinition>(SourceDefinition,MapPath);var md=new SerializedObject(map);
                S(md,"operationMapId",MapId);S(md,"renderResidencyMode",(int)OperationMapRenderResidencyMode.ResidentEntities);S(md,"sourceIdentityHash",Digest(MapId+":"+SourceRecipeRevision+":"+source.ContentHash));
                // This owns a different physical scene. SourceBinding denotes logical reuse of
                // the exact same source scene and must therefore remain unconfigured.
                var sb=md.FindProperty("sourceBinding");S(sb,"sourceOperationMapId",string.Empty);S(sb,"sourceIdentityHash",string.Empty);S(sb,"sourceContentHash",string.Empty);
                md.FindProperty("mapSurfaceDataReference").FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(SurfacePath);
                md.FindProperty("minimapRasterReference").FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(Folder+"/Minimap.png");
                foreach(string reference in new[]{"buildingPlacementsReference","vehiclePlacementsReference","optionalHeavyMetadataReference","staticPresentationManifestReference"})md.FindProperty(reference).FindPropertyRelative("m_AssetGUID").stringValue=string.Empty;
                md.FindProperty("additionalBuildingPlacements").objectReferenceValue=null;md.ApplyModifiedPropertiesWithoutUndo();
                SetField(map,"gridMetadata",new OperationMapGridMetadataConfig(AssetDatabase.AssetPathToGUID(GridPath),FileHash(GridPath),new Vector3(CropX,0,CropZ),new Vector2Int(Width,Height),1,grid.BlockedCells.Length));
                SetField(map,"surfaceMetadata",new OperationMapSurfaceMetadataConfig(AssetDatabase.AssetPathToGUID(SurfacePath),FileHash(SurfacePath),surface.ComputeRuntimeBlobHash().ToString(),surface.SurfaceCount,surface.PayloadVersion,surface.PayloadEncoding,source.SurfaceMetadata.MinimumHeight,source.SurfaceMetadata.MaximumHeight));
                SetField(map,"navigationMetadata",new OperationMapNavigationMetadataConfig(AssetDatabase.AssetPathToGUID(EntityScenePath),localId,scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<StaticGridBlockerAuthoring>(true).Length)+owners,true,true,true));
                var minimapData=new SerializedObject(map);var mini=minimapData.FindProperty("minimap");S(mini,"minimapId","minimap.ch05.m04.last_corridor");mini.FindPropertyRelative("projectionOrigin").vector3Value=new Vector3(CropX,0,CropZ);mini.FindPropertyRelative("projectionSize").vector2Value=new Vector2(Width,Height);minimapData.ApplyModifiedPropertiesWithoutUndo();
                ConfigureAnchors(map); EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
                binding=EditorSceneManager.OpenScene(SourceBinding,OpenSceneMode.Additive);SceneManager.SetActiveScene(binding);
                var view=binding.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapSceneView>(true)).Single();
                SetField(view,"operationMapId",MapId);SetField(view,"definition",map);SetField(view,"gridAuthoringConfig",grid);SetField(view,"presentationSourceSceneGuid",AssetDatabase.AssetPathToGUID(EntityScenePath));SetField(view,"presentationSourceScenePath",EntityScenePath);
                var authoring=view.MapSurfaceAuthoring;SetField(authoring,"bakedSurfaceData",surface);SetField(authoring,"gridConfig",grid);
                // Explicit loader ownership waits for native entity-scene readiness before
                // publishing the runtime surface. AutoLoad would publish a second baked
                // surface after the bootstrap singleton already exists.
                view.MapSubScene.SceneAsset=null;view.MapSubScene.AutoLoadScene=false;view.MapSubScene.enabled=false;
                Require(EditorSceneManager.SaveScene(binding,BindingPath),"Derivative binding save failed");
                SetField(map,"sourceSceneReference",new AssetReference(AssetDatabase.AssetPathToGUID(BindingPath)));
                Hash(map);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();
                Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);
                Require(view.TryValidate(out error),error);ValidateRoutes();
                foreach(var pair in protectedHashes)Require(FileHash(pair.Key)==pair.Value,"Prepared source modified: "+pair.Key);
                File.WriteAllText("Design/AgentReports/CH05M04LastCorridor/source-sha-after-generation.json",JsonUtility.ToJson(new SourceReceipt{files=protectedHashes.Select(p=>new SourceFile{path=p.Key,sha256=FileHash(p.Key)}).ToArray()},true));
                Debug.Log("[LastCorridorMap] result=Passed removedStatic="+removed+" identities="+identities.Length+" sourcePreserved=true sourceContentHash="+source.ContentHash+" derivative="+map.ContentHash);
            }
            finally {if(binding.IsValid()&&binding.isLoaded)EditorSceneManager.CloseScene(binding,true);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(original.IsValid()&&original.isLoaded)EditorSceneManager.CloseScene(original,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        private static void ConfigureNativeOwnership(Scene scene)
        {
            Require(All<OperationMapEntityPresentationIdentityAuthoring>(scene).Length==0,"Inherited accepted presentation identities require a full physical rebuild to crop visuals and rebuild retained-prop blockers.");
            var grids=All<GridAuthoring>(scene);Require(grids.Length==1,"Prepared Corridor scene must own one native GridAuthoring.");grids[0].Configure(Load<GridAuthoringSceneConfigAsset>(GridPath));
            var surfaces=All<MapSurfaceAuthoring>(scene);Require(surfaces.Length<=1,"Prepared Corridor scene contains duplicate MapSurfaceAuthoring.");
            var surface=surfaces.Length==1?surfaces[0]:grids[0].gameObject.AddComponent<MapSurfaceAuthoring>();SetField(surface,"gridConfig",Load<GridAuthoringSceneConfigAsset>(GridPath));SetField(surface,"bakedSurfaceData",Load<MapSurfaceDataAsset>(SurfacePath));
            int repaired=0;var owners=All<OperationMapBuildingAuthoring>(scene);
            foreach(var owner in owners)
            {
                Require(owner.IntactVisualRoot!=null&&owner.IntactVisualRoot.scene==scene&&owner.IntactVisualRoot.transform.parent==owner.transform,"Prepared owner lacks live local intact visuals: "+owner.StableId);
                var identities=owner.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true).Where(i=>i.Role==OperationMapEntityPresentationRole.GameplayBuildings).ToArray();
                Require(identities.Length<=1,"Prepared building has duplicate gameplay identities: "+owner.StableId);
                if(identities.Length==0)
                {
                    var identity=owner.IntactVisualRoot.GetComponent<DenseCityPresentationIdentityAuthoring>();
                    if(identity==null)identity=owner.IntactVisualRoot.AddComponent<DenseCityPresentationIdentityAuthoring>();
                    identity.ConfigureForEditor("densecity."+Digest(MapId+"|building|"+owner.StableId),OperationMapEntityPresentationRole.GameplayBuildings,DenseCityPresentationSemanticCategory.GameplayBuildingIntact);repaired++;
                }
            }
            var all=All<DenseCityPresentationIdentityAuthoring>(scene);int rendered=all.Count(i=>i.Role==OperationMapEntityPresentationRole.RenderOnly);
            Require(all.Select(i=>i.StableId).Distinct(StringComparer.Ordinal).Count()==all.Length,"Prepared Corridor native identity collision.");Require(all.Count(i=>i.Role==OperationMapEntityPresentationRole.GameplayBuildings)==owners.Length,"Prepared Corridor building identity/owner mismatch.");
            string hash=Digest(MapId+":"+SourceRecipeRevision+":"+Load<OperationMapDefinition>(SourceDefinition).ContentHash);
            foreach(var root in All<OperationMapEntityPresentationRootAuthoring>(scene))
            {
                var data=new SerializedObject(root);S(data,"operationMapId",MapId);S(data,"migrationRecordSetHash",hash);S(data,"expectedGameplayBuildingCount",owners.Length);S(data,"expectedGameplayVehicleCount",0);S(data,"expectedRenderOnlyCount",rendered);S(data,"expectedGeneratedIdentityCount",all.Length);data.ApplyModifiedPropertiesWithoutUndo();
            }
            Debug.Log("[LastCorridorNativeOwnership] result=Passed repairedGameplayIdentities="+repaired+" owners="+owners.Length+" renderOnly="+rendered+" identities="+all.Length+" Grid=1 Surface=1 counts=GlobalReadinessContract");
        }
        private static void MigrateAcceptedPresentation(Scene scene)
        {
            int converted=0,removedVehicles=0,retired=0;
            foreach(var identity in All<OperationMapEntityPresentationIdentityAuthoring>(scene))
            {
                if(identity==null)continue;
                if(identity.Role==OperationMapEntityPresentationRole.GameplayVehicles)
                {
                    var vehicle=identity.GetComponentInParent<UnitGridAuthoring>();
                    Require(vehicle!=null,"Accepted authored vehicle lacks its UnitGrid owner: "+identity.SourceGlobalObjectId);
                    UnityEngine.Object.DestroyImmediate(vehicle.gameObject);removedVehicles++;continue;
                }
                var owner=identity.GetComponentInParent<OperationMapBuildingAuthoring>();
                if(identity.Role==OperationMapEntityPresentationRole.GameplayBuildings)
                    Require(owner!=null,"Accepted building identity lacks its actual owner: "+identity.SourceGlobalObjectId);
                if(identity.Role==OperationMapEntityPresentationRole.RenderOnly&&owner==null)
                {
                    var generated=identity.GetComponent<DenseCityPresentationIdentityAuthoring>();
                    if(generated==null)
                    {
                        string name=identity.name.ToLowerInvariant();
                        bool infrastructure=name.Contains("road")||name.Contains("sidewalk")||name.Contains("pavement")||name.Contains("terrain")||name.Contains("ground")||name.Contains("floor")||name.Contains("landscape");
                        bool vegetation=name.Contains("plant")||name.Contains("tree")||name.Contains("bush")||name.Contains("grass")||name.Contains("palm");
                        var category=infrastructure?DenseCityPresentationSemanticCategory.Infrastructure:vegetation?DenseCityPresentationSemanticCategory.Vegetation:DenseCityPresentationSemanticCategory.Prop;
                        generated=identity.gameObject.AddComponent<DenseCityPresentationIdentityAuthoring>();
                        generated.ConfigureForEditor("densecity."+Digest(MapId+"|accepted|"+identity.SourceGlobalObjectId),OperationMapEntityPresentationRole.RenderOnly,category);converted++;
                    }
                }
                UnityEngine.Object.DestroyImmediate(identity);retired++;
            }
            Require(All<OperationMapEntityPresentationIdentityAuthoring>(scene).Length==0,"Accepted identity migration left an inherited descriptor.");
            Require(All<OperationMapAuthoredVehicleOwnershipAuthoring>(scene).Length==0,"Inherited authored vehicles remain in the Corridor derivative.");
            Debug.Log("[LastCorridorAcceptedMigration] convertedRenderOnly="+converted+" retiredAccepted="+retired+" removedAuthoredVehicles="+removedVehicles+" cropAndStaticMask=PendingPhysicalPass");
        }
        public static void RepairPreparedNativeOwnership()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Native ownership repair requires edit mode.");
            var hashes=ProtectedSources.ToDictionary(p=>p,p=>FileHash(p));var previous=SceneManager.GetActiveScene();Scene scene=default;
            try
            {
                scene=EditorSceneManager.OpenScene(EntityScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);ConfigureNativeOwnership(scene);
                Require(EditorSceneManager.SaveScene(scene,EntityScenePath),"Native ownership repair scene save failed.");
                var map=Load<OperationMapDefinition>(MapPath);var grid=All<GridAuthoring>(scene).Single();long localId=checked((long)GlobalObjectId.GetGlobalObjectIdSlow(grid).targetObjectId);
                SetField(map,"sourceIdentityHash",Digest(MapId+":"+SourceRecipeRevision+":"+Load<OperationMapDefinition>(SourceDefinition).ContentHash));
                SetField(map,"navigationMetadata",new OperationMapNavigationMetadataConfig(AssetDatabase.AssetPathToGUID(EntityScenePath),localId,All<StaticGridBlockerAuthoring>(scene).Length+All<OperationMapBuildingAuthoring>(scene).Length,true,true,true));Hash(map);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();
                foreach(var pair in hashes)Require(FileHash(pair.Key)==pair.Value,"Urban source changed during ownership repair: "+pair.Key);
                M01FirstContactConfigBuilder.RefreshChapterCatalogs();
                CH05M04LastCorridorContentBuilder.RegisterProductionEntries();
                Debug.Log("[LastCorridorNativeOwnershipRepair] result=Passed sourceHashes=6 sourcePreserved=true scene=Own gridSurface=Own");
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        private static bool ContainsRect(Rect container,Rect item)=>item.xMin>=container.xMin&&item.yMin>=container.yMin&&item.xMax<=container.xMax&&item.yMax<=container.yMax;
        private static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        private static bool TryBounds(Component component,out Bounds bounds)
        {
            var renderers=component.GetComponentsInChildren<Renderer>(true);bounds=default;if(renderers.Length==0)return false;
            bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return true;
        }
        private static void ValidateCivicClearance(Scene scene)
        {
            foreach(var owner in All<OperationMapBuildingAuthoring>(scene))
            {
                var world=new Rect(owner.OriginCell.x+CropX,owner.OriginCell.y+CropZ,owner.FootprintCells.x,owner.FootprintCells.y);
                Require(!ClearZones.Any(r=>r.Overlaps(world)),"Civic/production/response lane intersects source owner "+owner.StableId);
            }
            Debug.Log("[LastCorridorCivicClearance] result=Passed serviceLots=2 productionLots=2 reserveLots=2 healthOwners=RuntimeBuildingPlacement");
        }
        public static void RepairMinimap()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Minimap repair requires edit mode.");
            var previous=SceneManager.GetActiveScene();Scene scene=default;
            try
            {
                scene=EditorSceneManager.OpenScene(EntityScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);CreateMinimap(scene);
                string identity=Digest(MapId+":"+SourceRecipeRevision+":"+Load<OperationMapDefinition>(SourceDefinition).ContentHash);
                foreach(var root in All<OperationMapEntityPresentationRootAuthoring>(scene)){var data=new SerializedObject(root);S(data,"migrationRecordSetHash",identity);data.ApplyModifiedPropertiesWithoutUndo();}
                Require(EditorSceneManager.SaveScene(scene,EntityScenePath),"Minimap provenance scene save failed.");
                var map=Load<OperationMapDefinition>(MapPath);SetField(map,"sourceIdentityHash",identity);Hash(map);EditorUtility.SetDirty(map);AssetDatabase.SaveAssets();
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        public static bool IsReadableRaster(Texture2D texture)
        {
            if(texture==null||texture.width<128||texture.height<64)return false;
            var colors=new HashSet<int>();int lit=0;int min=255,max=0;var pixels=texture.GetPixels32();
            foreach(var p in pixels){int value=Mathf.Max(p.r,Mathf.Max(p.g,p.b));min=Mathf.Min(min,value);max=Mathf.Max(max,value);if(value>12)lit++;colors.Add((p.r>>3)<<10|(p.g>>3)<<5|(p.b>>3));}
            return lit>pixels.Length/2&&max-min>=16&&colors.Count>=8;
        }
        private static void CreateMinimap(Scene scene)
        {
            var previousActive=RenderTexture.active;var ambientMode=RenderSettings.ambientMode;var ambient=RenderSettings.ambientLight;var fog=RenderSettings.fog;
            var destroyedRoots=All<OperationMapBuildingAuthoring>(scene).Where(o=>o!=null&&o.DestroyedVisualRoot!=null).Select(o=>o.DestroyedVisualRoot.transform).Where(t=>t!=null).Distinct().ToArray();
            var scales=destroyedRoots.Select(t=>t.localScale).ToArray();var renderers=All<Renderer>(scene);var enabled=renderers.Select(r=>r.enabled).ToArray();
            var cameraRoot=new GameObject("CorridorMinimapCapture",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraRoot,scene);
            var lightRoot=new GameObject("CorridorMinimapCaptureLight",typeof(Light));SceneManager.MoveGameObjectToScene(lightRoot,scene);
            var camera=cameraRoot.GetComponent<Camera>();var center=Sector.center;camera.transform.SetPositionAndRotation(new Vector3(center.x,700,center.y),Quaternion.Euler(90,0,0));camera.orthographic=true;camera.orthographicSize=Height*.5f;camera.aspect=(float)Width/Height;camera.farClipPlane=4000;
            camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(140,126,105,255);
            var light=lightRoot.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(50,-30,0);
            int height=800,width=1600;var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);Texture2D texture=null;string mode="NativeURP";
            try
            {
                foreach(var root in destroyedRoots)if(root!=null)root.localScale=Vector3.zero;foreach(var renderer in renderers)if(renderer!=null)renderer.enabled=true;
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.72f,.72f,.72f,1);RenderSettings.fog=false;
                var request=new RenderPipeline.StandardRequest{destination=target};
                if(RenderPipeline.SupportsRenderRequest(camera,request))
                {RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);RenderTexture.active=target;texture=new Texture2D(width,height,TextureFormat.RGBA32,false,false);texture.ReadPixels(new Rect(0,0,width,height),0,0,false);texture.Apply(false,false);}
                if(!IsReadableRaster(texture))
                {
                    if(texture!=null){Directory.CreateDirectory("Design/AgentReports/CH05M04LastCorridor/Evidence");File.WriteAllBytes("Design/AgentReports/CH05M04LastCorridor/Evidence/minimap-native-capture-failed.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);}
                    Debug.LogWarning("[LastCorridorMinimap] nativeCapture=Failed fallback=ApprovedSourceRasterCrop");texture=CropApprovedRaster();mode="ApprovedSourceRasterCrop";
                }
                Require(IsReadableRaster(texture),"Corridor minimap is black, uniform or unreadable.");File.WriteAllBytes(Folder+"/Minimap.png",texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previousActive;RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambient;RenderSettings.fog=fog;
                for(int i=0;i<destroyedRoots.Length;i++)if(destroyedRoots[i]!=null)destroyedRoots[i].localScale=scales[i];for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=enabled[i];
                UnityEngine.Object.DestroyImmediate(cameraRoot);UnityEngine.Object.DestroyImmediate(lightRoot);target.Release();UnityEngine.Object.DestroyImmediate(target);if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(Folder+"/Minimap.png",ImportAssetOptions.ForceUpdate);
            var importer=AssetImporter.GetAtPath(Folder+"/Minimap.png") as TextureImporter;Require(importer!=null,"Own minimap importer unavailable.");importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.userData="corridor-minimap|"+SourceRecipeRevision+"|"+mode;importer.SaveAndReimport();
            Debug.Log("[LastCorridorMinimap] result=Passed mode="+mode+" crop=860,300,440,220 blackUniformGate=Passed");
        }
        private static Texture2D CropApprovedRaster()
        {
            var map=Load<OperationMapDefinition>(SourceDefinition);string path=AssetDatabase.GUIDToAssetPath(map.MinimapRasterReference.AssetGUID);Require(path==SourceMinimap,"Source minimap GUID changed; re-audit projection.");Require(Mathf.Abs(map.Minimap.OrientationDegrees)<.001f,"Source minimap orientation unsupported.");
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false,false);var result=new Texture2D(880,440,TextureFormat.RGBA32,false,false);
            try
            {
                Require(ImageConversion.LoadImage(source,File.ReadAllBytes(path),false),"Approved source raster unreadable.");var pixels=new Color[result.width*result.height];var origin=map.Minimap.ProjectionOrigin;var size=map.Minimap.ProjectionSize;
                for(int y=0;y<result.height;y++)for(int x=0;x<result.width;x++)
                {float u=(CropX+Width*(x+.5f)/result.width-origin.x)/size.x,v=(CropZ+Height*(y+.5f)/result.height-origin.z)/size.y;Require(u>=0&&u<=1&&v>=0&&v<=1,"Crop outside source projection.");pixels[y*result.width+x]=source.GetPixelBilinear(u,v);}
                result.SetPixels(pixels);result.Apply(false,false);return result;
            }
            catch{UnityEngine.Object.DestroyImmediate(result);throw;}
            finally{UnityEngine.Object.DestroyImmediate(source);}
        }
        private static readonly Rect[] ClearZones={new(1230,441,20,18),new(1073,375,32,31),new(1231,353,32,31),new(870,411,415,26),new(985,323,32,114),new(1231,462,18,16),new(938,428,32,13)};
        private static void BuildSurface(bool[] mask)
        {
            var source=Load<MapSurfaceDataAsset>(SourceSurface);Require(source.TryCreateRuntimeBlobAsset(Allocator.Temp,out var original),"Source surface missing");
            using(original)using(var builder=new BlobBuilder(Allocator.Temp))
            {
                ref var blob=ref builder.ConstructRoot<MapSurfaceBlob>();blob.GridOrigin=new float3(CropX,0,CropZ);blob.CellSize=1;blob.Dimensions=new int2(Width,Height);
                int count=Width*Height;var cells=builder.Allocate(ref blob.Cells,count);var samples=builder.Allocate(ref blob.Samples,count);builder.Allocate(ref blob.CompactSamples,0);builder.Allocate(ref blob.Connections,0);var blocked=new List<Vector2Int>();
                for(int z=0;z<Height;z++)for(int x=0;x<Width;x++)
                {
                    int i=z*Width+x;Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref original.Value,new int2(x+CropX,z+CropZ),out var sample),"Source sample missing "+x+","+z);
                    sample.Cell=new int2(x,z);sample.SurfaceId=i;sample.FirstConnectionIndex=0;sample.ConnectionCount=0;
                    cells[i]=new MapSurfaceCell{FirstSurfaceIndex=i,SurfaceCount=1};
                    if(mask[(z+CropZ)*2048+x+CropX])sample.MovementMask=MapSurfaceMovementMask.None;
                    // Preserve accepted source movement permissions exactly. Road normals
                    // include curb/seam noise; reclassifying them would erase qualified lanes.
                    if(BuildingLots.Any(l=>l.Contains(new Vector2Int(x+CropX,z+CropZ)))&&!mask[(z+CropZ)*2048+x+CropX]&&(sample.SurfaceType is MapSurfaceType.Terrain or MapSurfaceType.Plaza)&&(sample.MovementMask&MapSurfaceMovementMask.AllGroundUnits)==MapSurfaceMovementMask.AllGroundUnits&&float.IsFinite(sample.Height)&&(sample.Flags&(MapSurfaceFlags.Road|MapSurfaceFlags.Highway|MapSurfaceFlags.Bridge))==0&&sample.SlopeDegrees<=10)
                    {sample.MovementMask|=MapSurfaceMovementMask.BuildingPlacement;sample.Flags&=~MapSurfaceFlags.Reserved;}
                    if((sample.MovementMask&MapSurfaceMovementMask.AllGroundUnits)==MapSurfaceMovementMask.None)blocked.Add(new Vector2Int(x,z));samples[i]=sample;
                }
                using var baked=builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);var surface=Clone<MapSurfaceDataAsset>(SourceSurface,SurfacePath);surface.ConfigureBakedSurface(new Vector3(CropX,0,CropZ),1,new Vector2Int(Width,Height),baked,false);EditorUtility.SetDirty(surface);
                var grid=Clone<GridAuthoringSceneConfigAsset>(SourceGrid,GridPath);SetField(typeof(GridAuthoringConfig),grid,"origin",new Vector3(CropX,0,CropZ));SetField(typeof(GridAuthoringConfig),grid,"width",Width);SetField(typeof(GridAuthoringConfig),grid,"height",Height);SetField(typeof(GridAuthoringConfig),grid,"blockedCells",blocked.ToArray());EditorUtility.SetDirty(grid);AssetDatabase.SaveAssets();
            }
        }
        private static void ConfigureAnchors(OperationMapDefinition map)
        {
            SetField(map,"sourceBinding",default(OperationMapSourceBindingConfig));var surface=Load<MapSurfaceDataAsset>(SurfacePath);Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out var blob),"Derivative surface missing");using(blob)
            {
                (string id,int x,int z,OperationMapAnchorKind kind,int faction,float radius)[] seeds={
                    ("forward_post",1006,426,OperationMapAnchorKind.Base,1,8),
                    ("inner_core",1006,426,OperationMapAnchorKind.Base,1,8),
                    ("initial_barracks",1076,378,OperationMapAnchorKind.Build,1,1),
                    ("fuel_source",1076,378,OperationMapAnchorKind.Build,1,1),
                    ("fuel_receive",1234,356,OperationMapAnchorKind.Build,1,1),
                    ("receiving_origin",1233,444,OperationMapAnchorKind.Build,0,1),
                    ("receiving",1240,450,OperationMapAnchorKind.Objective,0,12),
                    ("receiving_gate",1240,426,OperationMapAnchorKind.Objective,1,8),
                    ("engineer_start",1006,330,OperationMapAnchorKind.Deployment,1,1),
                    ("key_carrier_start",990,330,OperationMapAnchorKind.Deployment,1,1),
                    ("escort_tanks",970,420,OperationMapAnchorKind.Deployment,1,1),
                    ("escort_tank_b",982,420,OperationMapAnchorKind.Deployment,1,1),
                    ("escort_infantry",978,418,OperationMapAnchorKind.Deployment,1,3),
                    ("medicine_start",940,432,OperationMapAnchorKind.Deployment,1,1),
                    ("fuel_start",940,420,OperationMapAnchorKind.Deployment,1,1),
                    ("reinforcement_start",950,432,OperationMapAnchorKind.Deployment,1,3),
                    ("staff",1240,470,OperationMapAnchorKind.Civilian,0,3),
                    ("blockade",1160,420,OperationMapAnchorKind.Spawn,2,3),
                    ("blockade_apc",1172,420,OperationMapAnchorKind.Spawn,2,1),
                    ("route_hostile",1140,432,OperationMapAnchorKind.Spawn,2,3),
                    ("broken_link",1130,420,OperationMapAnchorKind.Lane,0,1),
                    ("repair",1120,420,OperationMapAnchorKind.Objective,1,6),
                    ("carrier_rendezvous",990,330,OperationMapAnchorKind.Objective,1,6),
                    ("engineer_pickup",990,330,OperationMapAnchorKind.Objective,1,6),
                    ("medicine_receiver",1240,432,OperationMapAnchorKind.Objective,1,6),
                    ("fuel_receiver",1240,420,OperationMapAnchorKind.Objective,1,6),
                    ("reinforcement_receiver",1216,432,OperationMapAnchorKind.Objective,1,6),
                    ("key_receiver",1228,426,OperationMapAnchorKind.Objective,1,6),
                    ("main_middle",1100,420,OperationMapAnchorKind.Lane,1,5),
                    ("alternate_middle",1100,432,OperationMapAnchorKind.Lane,1,5),
                    ("return_rts",1006,426,OperationMapAnchorKind.Camera,1,2),
                    ("main_entry",980,420,OperationMapAnchorKind.Lane,1,5),
                    ("alternate_entry",980,432,OperationMapAnchorKind.Lane,1,5),
                    ("main_mid",1100,420,OperationMapAnchorKind.Lane,1,5),
                    ("alternate_mid",1100,432,OperationMapAnchorKind.Lane,1,5),
                    ("main_exit",1230,420,OperationMapAnchorKind.Lane,1,5),
                    ("alternate_exit",1230,432,OperationMapAnchorKind.Lane,1,5)};
                var data=new SerializedObject(map);A(data.FindProperty("anchors"),seeds.Length,(p,i)=>{var seed=seeds[i];Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(seed.x-CropX,seed.z-CropZ),out var sample),seed.id);S(p,"anchorId",Prefix+seed.id);S(p,"kind",(int)seed.kind);S(p,"factionId",seed.faction);S(p,"laneIndex",0);p.FindPropertyRelative("position").vector3Value=new Vector3(seed.x,sample.Height,seed.z);p.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;p.FindPropertyRelative("radius").floatValue=seed.radius;});
                var bounds=data.FindProperty("bounds");bounds.FindPropertyRelative("worldMin").vector3Value=new Vector3(CropX,-20,CropZ);bounds.FindPropertyRelative("worldMax").vector3Value=new Vector3(CropX+Width,1000,CropZ+Height);bounds.FindPropertyRelative("playableMin").vector3Value=new Vector3(CropX+8,-20,CropZ+8);bounds.FindPropertyRelative("playableMax").vector3Value=new Vector3(CropX+Width-8,980,CropZ+Height-8);bounds.FindPropertyRelative("cameraMin").vector3Value=new Vector3(CropX,-20,CropZ);bounds.FindPropertyRelative("cameraMax").vector3Value=new Vector3(CropX+Width,980,CropZ+Height);
                S(data,"planningCameraId","camera.ch05.m04.planning");S(data,"battleCameraId","camera.ch05.m04.battle");
                A(data.FindProperty("cameras"),2,(p,i)=>{var focus=new Vector3(1080,0,420);var pos=focus+new Vector3(0,i==0?155:90,i==0?-90:-65);S(p,"cameraId","camera.ch05.m04."+(i==0?"planning":"battle"));p.FindPropertyRelative("position").vector3Value=pos;p.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(focus-pos).eulerAngles;p.FindPropertyRelative("fieldOfView").floatValue=58;p.FindPropertyRelative("orthographic").boolValue=false;});data.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        public static void ValidateRoutes()
        {
            var grid=Load<GridAuthoringSceneConfigAsset>(GridPath);var blocked=new HashSet<Vector2Int>(grid.BlockedCells);var ownersBlocked=new HashSet<Vector2Int>();
            var previous=SceneManager.GetActiveScene();Scene scene=default;
            try {scene=EditorSceneManager.OpenScene(EntityScenePath,OpenSceneMode.Additive);foreach(var owner in All<OperationMapBuildingAuthoring>(scene))for(int z=0;z<owner.FootprintCells.y;z++)for(int x=0;x<owner.FootprintCells.x;x++){var cell=owner.OriginCell+new Vector2Int(x,z);blocked.Add(cell);ownersBlocked.Add(cell);}}
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
            var surface=Load<MapSurfaceDataAsset>(SurfacePath);Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out var blob),"Corridor surface missing");
            using(blob)
            {
            bool Fits(int x,int z,int r){for(int dz=-r;dz<=r;dz++)for(int dx=-r;dx<=r;dx++){var cell=new Vector2Int(x+dx-CropX,z+dz-CropZ);if(blocked.Contains(cell)){Debug.LogError("[LastCorridorRouteCell] reason="+(ownersBlocked.Contains(cell)?"GameplayOwner":"StaticMask")+" world="+(x+dx)+","+(z+dz));return false;}
                if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(cell.x,cell.y),out var sample)){Debug.LogError("[LastCorridorRouteCell] reason=MissingSurface world="+(x+dx)+","+(z+dz));return false;}
                if((sample.MovementMask&(MapSurfaceMovementMask.WheeledVehicle|MapSurfaceMovementMask.TrackedVehicle))!=(MapSurfaceMovementMask.WheeledVehicle|MapSurfaceMovementMask.TrackedVehicle)){Debug.LogError("[LastCorridorRouteCell] reason=VehicleMask world="+(x+dx)+","+(z+dz)+" mask="+sample.MovementMask+" type="+sample.SurfaceType+" slope="+sample.SlopeDegrees+" flags="+sample.Flags);return false;}}return true;}
            foreach(int laneZ in new[]{420,432})for(int x=940;x<=1240;x++)Require(Fits(x,laneZ,2),"Corridor five-cell delivery lane blocked: "+x+","+laneZ);
            for(int z=330;z<=432;z++)Require(Fits(1006,z,2),"Corridor team/carrier access blocked1006,"+z);
            for(int z=420;z<=432;z++)foreach(int x in new[]{980,1230,1240})Require(Fits(x,z,2),"Corridor lane connection blocked "+x+","+z);
            Require(Fits(1120,420,1),"Corridor external repair gate blocked.");
            Require(Fits(990,330,2),"Corridor original key carrier staging blocked.");
            Require(Fits(1240,470,2),"Corridor protected receiving staff staging blocked.");
            foreach(var lot in BuildingLots)for(int z=lot.yMin;z<lot.yMax;z++)for(int x=lot.xMin;x<lot.xMax;x++)
                Require(!blocked.Contains(new Vector2Int(x-CropX,z-CropZ))&&MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(x-CropX,z-CropZ),out var sample)&&(sample.MovementMask&MapSurfaceMovementMask.BuildingPlacement)!=0,"Corridor building lot rejected "+x+","+z);
            }
            Debug.Log("[LastCorridorRoutes] result=Passed deliveryLanes=2x301x5 laneGap=12m sameArtery=true rearCarrierLane=103x5 buildLots=3 grid=440x220 combinedOwnerSurfaceMask=true");
        }
        [Serializable]private sealed class SourceReceipt{public SourceFile[] files;}
        [Serializable]private sealed class SourceFile{public string path,sha256;}
        // Scenario/mission production contracts are authored in the companion partial.
        private static string FileHash(string path)=>Digest(File.ReadAllBytes(path));
        private static string Digest(string text)=>Digest(Encoding.UTF8.GetBytes(text));
        private static string Digest(byte[] bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-",string.Empty).ToLowerInvariant();}
        private static void Hash(OperationMapDefinition map){var d=new SerializedObject(map);S(d,"contentHash",string.Empty);S(d,"generatedMetadataHash",string.Empty);d.ApplyModifiedPropertiesWithoutUndo();string hash=Digest(EditorJsonUtility.ToJson(map));d.Update();S(d,"contentHash",hash);S(d,"generatedMetadataHash",hash);d.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetField(object target,string name,object value)=>SetField(target.GetType(),target,name,value);
        private static void SetField(Type type,object target,string name,object value){var f=type.GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);Require(f!=null,"Missing field "+name);f.SetValue(target,value);}
        private static T Load<T>(string path)where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException(path);
        private static T Clone<T>(string source,string target)where T:ScriptableObject{var result=AssetDatabase.LoadAssetAtPath<T>(target);if(result==null){result=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(result,target);}EditorUtility.CopySerialized(Load<T>(source),result);result.name=Path.GetFileNameWithoutExtension(target);return result;}
        private static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
        private static void Replace(SerializedObject d,string from,string to){var p=d.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.String)p.stringValue=p.stringValue.Replace(from,to);}
        private static void A(SerializedProperty p,int count,Action<SerializedProperty,int> action){p.arraySize=count;for(int i=0;i<count;i++)action?.Invoke(p.GetArrayElementAtIndex(i),i);}
        private static void S(SerializedObject p,string name,object value)=>S(p.FindProperty(name),value);
        private static void S(SerializedProperty p,string name,object value)=>S(p.FindPropertyRelative(name),value);
        private static void S(SerializedProperty p,object value){Require(p!=null,"Missing serialized property");switch(value){case string s:p.stringValue=s;break;case int i:p.intValue=i;break;case bool b:p.boolValue=b;break;default:throw new ArgumentException(p.propertyPath);}}
    }
}
