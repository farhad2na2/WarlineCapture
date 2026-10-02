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
using Game.Editor.MapVariants;
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

namespace Game.Editor
{
    /// <summary>Independent bounded Frontier derivative. Source assets remain unchanged.</summary>
    public static partial class CH04M05ArmorBreakConfigBuilder
    {
        public const string MissionId="saga.ch04.m05.armor_break", ScenarioId="scenario.ch04.m05.armor_break";
        public const string MapId="opmap.ch04.armor_break", Prefix="anchor.ch04.m05.";
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter04/MissionDefinition_Ch04_M05_ArmorBreak.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter04/ScenarioSetup_Ch04_M05_ArmorBreak.asset";
        public const string Folder="Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak";
        public const string MapPath=Folder+"/Definition.asset", SurfacePath=Folder+"/Surface.asset", GridPath=Folder+"/Grid.asset";
        public const string SceneFolder="Assets/Game/Scenes/OperationMaps/CH04M05ArmorBreak";
        public const string EntityScenePath=SceneFolder+"/PreparedEntities.unity", BindingPath=SceneFolder+"/RuntimeBinding.unity";
        public const string ReserveBuildingId="Building_ArmorBreak_ReserveDepot";
        public const string ReservePrefabPath="Assets/Game/Prefabs/Buildings/CH04M05ArmorBreak/"+ReserveBuildingId+".prefab";
        public const string PreparedHash="e2d40d5eea9a0b81a1500cd14040329fc638b7909249ddf095ea3e5d9c6d54cf";
        public const int CropX=740,CropZ=260,Width=1280,Height=520;
        private static readonly Rect Sector=new Rect(CropX,CropZ,Width,Height);
        private const string SourceFolder="Assets/Game/GeneratedOperationMaps/Variants/Frontier/Candidate";
        private const string SourceScene="Assets/Game/Scenes/OperationMaps/Variants/Frontier/PreparedEntities.unity";
        private const string SourceBinding="Assets/Game/Scenes/OperationMaps/Variants/Frontier/RuntimeBinding.unity";
        private const string SourceManifest="Design/MapVariants/Preparation/Frontier/Candidate/output-manifest.json";
        public static void Build()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Armor Break generation requires edit mode.");
            foreach(string folder in new[]{Folder,SceneFolder,"Assets/Game/Configs/Missions/Chapter04","Assets/Game/Configs/Scenarios/Chapter04"}) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            EditorApplication.LockReloadAssemblies();
            try {BuildReservePrefab(); BuildPhysicalMap(); BuildScenario(); BuildMission(); BuildSupportContext(); AssetDatabase.SaveAssets();}
            finally {EditorApplication.UnlockReloadAssemblies();}
            Debug.Log("[ArmorBreakConfig] result=Passed airDefense=1 groundLauncher=1 armor=3 infantry=4 aircraft=1 hostileAir=3 protectedRelief=3 finiteFuel=200");
        }
        /// <summary>Resume after physical authoring without regenerating source scenes or raster imports.</summary>
        public static void CompletePreparedConfiguration()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Armor Break configuration requires edit mode.");
            EditorApplication.LockReloadAssemblies();
            try
            {
                var map=Load<OperationMapDefinition>(MapPath);ConfigureAnchors(map);Hash(map);EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
                Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);
                ValidateRoutes();BuildScenario();BuildMission();BuildSupportContext();AssetDatabase.SaveAssets();
                Debug.Log("[ArmorBreakConfig] result=Passed airDefense=1 groundLauncher=1 armor=3 infantry=4 aircraft=1 hostileAir=3 protectedRelief=3 finiteFuel=200 resumedPreparedMap=true");
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
                Debug.Log("[ArmorBreakSceneOwnership] result=Passed owner=ExplicitEntitySceneLoader autoLoad=false disabledPlaceholder=true guid="+view.Definition.NavigationMetadata.AuthoredSubSceneGuid);
            }
            finally {if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        public static void BuildPhysicalMap()
        {
            var source=Load<OperationMapDefinition>(SourceFolder+"/Definition.asset");
            Require(source.ContentHash==PreparedHash,"Frontier prepared source hash changed; re-audit derivative.");
            var protectedHashes=new[]{SourceScene,SourceBinding,SourceFolder+"/Definition.asset",SourceFolder+"/Grid.asset",SourceFolder+"/Surface.asset",SourceFolder+"/Minimap.png"}.ToDictionary(p=>p,p=>FileHash(p));
            var manifest=JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText(SourceManifest));
            var removalKeys=new HashSet<string>(manifest.classifications.Where(RemovePlacement).Select(r=>r.stableKey));
            foreach(var row in manifest.classifications)if(row.attached&&removalKeys.Contains(row.attachmentOwnerKey))removalKeys.Add(row.stableKey);
            var removed=manifest.classifications.Where(r=>removalKeys.Contains(r.stableKey)).ToArray();
            var previous=SceneManager.GetActiveScene(); Scene original=default,scene=default,binding=default;
            try
            {
                original=EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Additive);
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
                foreach(var root in original.GetRootGameObjects()) {var copy=UnityEngine.Object.Instantiate(root);copy.name=root.name;SceneManager.MoveGameObjectToScene(copy,scene);}
                EditorSceneManager.CloseScene(original,true);original=default;
                string PositionKey(Vector3 p)=>Mathf.RoundToInt(p.x*100)+","+Mathf.RoundToInt(p.y*100)+","+Mathf.RoundToInt(p.z*100);
                var removalLookup=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).GroupBy(i=>PositionKey(i.transform.position)).ToDictionary(g=>g.Key,g=>g.Select(i=>i.transform).ToArray());
                var ownerIds=manifest.owners.ToDictionary(o=>o.placementKey,o=>o.stableId,StringComparer.Ordinal);
                var ownerLookup=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).ToDictionary(o=>o.StableId,StringComparer.Ordinal);
                foreach(var row in removed)
                {
                    // Walkable source recipes are already merged into terrain/road chunks.
                    // The bounded mesh pass below removes wholly outside chunks.
                    if(row.category=="WalkableSurface")continue;
                    // Realized building roots sit at their grid footprint origin; the
                    // manifest's art pivot is not their transform. Resolve exact owner identity.
                    if(ownerIds.TryGetValue(row.stableKey,out var ownerId))
                    {
                        Require(ownerLookup.TryGetValue(ownerId,out var owner) && owner!=null,"Missing derivative owner "+ownerId);
                        UnityEngine.Object.DestroyImmediate(owner.gameObject);continue;
                    }
                    string name=Path.GetFileNameWithoutExtension(row.sourceRecipe.Split('|')[0].Split('~')[0].Split('@')[0]);
                    var candidates=removalLookup.TryGetValue(PositionKey(row.runtimePosition),out var matches)?matches.Where(t=>t!=null&&Vector3.Distance(t.position,row.runtimePosition)<.02f&&t.name.StartsWith(name,StringComparison.Ordinal)).ToArray():Array.Empty<Transform>();
                    if(candidates.Length==0&&row.attached)continue;
                    Require(candidates.Length==1,"Ambiguous derivative removal "+row.stableKey+" candidates="+candidates.Length);
                    UnityEngine.Object.DestroyImmediate(candidates[0].gameObject);
                }
                var staticMask=new bool[2048*1024];
                var inventory=new MapPreparationManifest {runtimePlayableMin=manifest.runtimePlayableMin,playableSize=manifest.playableSize};
                foreach(var row in manifest.classifications.Except(removed)) if(row.movementBlocked&&!row.damageEligible)
                    MapVariantPreparedCandidateBuilder.Rasterize(row.runtimeFootprint,staticMask,inventory);
                // Remove unclassified road/backdrop render owners wholly outside the bounded sector.
                foreach(var identity in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).ToArray())
                {
                    var renderers=identity.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)continue;
                    var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
                    if(bounds.max.x<CropX||bounds.min.x>CropX+Width||bounds.max.z<CropZ||bounds.min.z>CropZ+Height)UnityEngine.Object.DestroyImmediate(identity.gameObject);
                }
                BuildSurface(staticMask,manifest);
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
                var identities=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).ToArray();
                int owners=scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length);
                int rendered=identities.Length-owners;
                Require(identities.Select(i=>i.StableId).Distinct(StringComparer.Ordinal).Count()==identities.Length,"Generated derivative identity collision.");
                foreach(var marker in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true)))
                {var d=new SerializedObject(marker);d.FindProperty("migrationRecordSetHash").stringValue=Digest(MapId+":frontier-sector-v1:"+source.ContentHash);d.FindProperty("expectedGameplayBuildingCount").intValue=owners;d.FindProperty("expectedGameplayVehicleCount").intValue=0;d.FindProperty("expectedRenderOnlyCount").intValue=rendered;d.FindProperty("expectedGeneratedIdentityCount").intValue=identities.Length;d.ApplyModifiedPropertiesWithoutUndo();}
                CreateMinimap(scene,manifest);
                Require(EditorSceneManager.SaveScene(scene,EntityScenePath),"Derivative entity save failed");
                ValidateProtectedRelief(scene);
                var gaSaved=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GridAuthoring>(true)).Single();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(gaSaved,out _,out long localId);
                if(localId<=0)localId=checked((long)GlobalObjectId.GetGlobalObjectIdSlow(gaSaved).targetObjectId);
                var map=Clone<OperationMapDefinition>(SourceFolder+"/Definition.asset",MapPath);var md=new SerializedObject(map);
                S(md,"operationMapId",MapId);S(md,"sourceIdentityHash",Digest(MapId+":frontier-sector-v1:"+source.ContentHash));
                // This owns a different physical scene. SourceBinding denotes logical reuse of
                // the exact same source scene and must therefore remain unconfigured.
                var sb=md.FindProperty("sourceBinding");S(sb,"sourceOperationMapId",string.Empty);S(sb,"sourceIdentityHash",string.Empty);S(sb,"sourceContentHash",string.Empty);
                md.FindProperty("mapSurfaceDataReference").FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(SurfacePath);
                md.FindProperty("minimapRasterReference").FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(Folder+"/Minimap.png");
                md.FindProperty("additionalBuildingPlacements").objectReferenceValue=null;md.ApplyModifiedPropertiesWithoutUndo();
                SetField(map,"gridMetadata",new OperationMapGridMetadataConfig(AssetDatabase.AssetPathToGUID(GridPath),FileHash(GridPath),new Vector3(CropX,0,CropZ),new Vector2Int(Width,Height),1,grid.BlockedCells.Length));
                SetField(map,"surfaceMetadata",new OperationMapSurfaceMetadataConfig(AssetDatabase.AssetPathToGUID(SurfacePath),FileHash(SurfacePath),surface.ComputeRuntimeBlobHash().ToString(),surface.SurfaceCount,surface.PayloadVersion,surface.PayloadEncoding,source.SurfaceMetadata.MinimumHeight,source.SurfaceMetadata.MaximumHeight));
                SetField(map,"navigationMetadata",new OperationMapNavigationMetadataConfig(AssetDatabase.AssetPathToGUID(EntityScenePath),localId,scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<StaticGridBlockerAuthoring>(true).Length)+owners,true,true,true));
                var minimapData=new SerializedObject(map);var mini=minimapData.FindProperty("minimap");S(mini,"minimapId","minimap.ch04.m05.armor_break");mini.FindPropertyRelative("projectionOrigin").vector3Value=new Vector3(CropX,0,CropZ);mini.FindPropertyRelative("projectionSize").vector2Value=new Vector2(Width,Height);minimapData.ApplyModifiedPropertiesWithoutUndo();
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
                Debug.Log("[ArmorBreakMap] result=Passed removedStatic="+removed.Length+" identities="+identities.Length+" sourcePreserved="+source.ContentHash+" derivative="+map.ContentHash);
            }
            finally {if(binding.IsValid()&&binding.isLoaded)EditorSceneManager.CloseScene(binding,true);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(original.IsValid()&&original.isLoaded)EditorSceneManager.CloseScene(original,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        private static void ValidateProtectedRelief(Scene scene)
        {
            var center=new Vector2(1900,480);const float radius=40;int facilities=0;
            foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
            {
                if(Vector2.Distance(new Vector2(t.position.x,t.position.z),center)>radius)continue;
                Require(t.GetComponent<OperationMapBuildingAuthoring>()==null&&t.GetComponent<BuildingDefinitionAuthoring>()==null&&t.GetComponent<UnitGridAuthoring>()==null,"Damage authoring in protected relief corridor: "+t.name);
                if(t.GetComponent<DenseCityPresentationIdentityAuthoring>()!=null&&t.name.Contains("Hospital"))facilities++;
            }
            Debug.Log("[ArmorBreakRelief] result=Passed protectedRadius=40 damageableFacilityOwners=0 medicalFixtures="+facilities);
        }
        private static void CreateMinimap(Scene scene,PreparedCandidateOutput manifest)
        {
            var lights=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Lighting").ToArray();
            var lightStates=lights.Select(t=>t.gameObject.activeSelf).ToArray();foreach(var t in lights)t.gameObject.SetActive(true);
            var owners=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).ToArray();
            foreach(var owner in owners)owner.DestroyedVisualRoot.transform.localScale=Vector3.zero;
            var go=new GameObject("ArmorBreakMinimap",typeof(Camera));SceneManager.MoveGameObjectToScene(go,scene);
            var camera=go.GetComponent<Camera>();var center=Sector.center;
            camera.transform.SetPositionAndRotation(new Vector3(center.x,700,center.y),Quaternion.Euler(90,0,0));camera.orthographic=true;camera.orthographicSize=Height*.5f;camera.farClipPlane=4000;
            int height=800,width=Mathf.RoundToInt(height*(float)Width/Height);var target=new RenderTexture(width,height,24);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
            try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Folder+"/Minimap.png",texture.EncodeToPNG());}
            finally {RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);for(int i=0;i<lights.Length;i++)lights[i].gameObject.SetActive(lightStates[i]);foreach(var owner in owners)owner.DestroyedVisualRoot.transform.localScale=Vector3.one;}
            AssetDatabase.ImportAsset(Folder+"/Minimap.png",ImportAssetOptions.ForceUpdate);
        }
        private static bool RemovePlacement(MapPreparationPlacement row)
        {
            if(!Sector.Contains(new Vector2(row.runtimePosition.x,row.runtimePosition.z))&&!row.runtimeFootprint.Any(Sector.Contains))return true;
            if(row.damageEligible&&row.runtimeFootprint.Any(p=>!Sector.Contains(p)))return true;
            if(row.attached)return false;
            // Preserve static relief hospitals but keep the protected corridor free of destructible owners.
            var p=new Vector2(row.runtimePosition.x,row.runtimePosition.z);
            if(row.damageEligible&&Vector2.Distance(p,new Vector2(1900,480))<=48)return true;
            if(!row.movementBlocked)return false;
            // Military placements must be functional scenario entities, never doubled static props.
            return ClearZones.Any(zone=>row.runtimeFootprint.Any(zone.Contains));
        }
        private static readonly Rect[] ClearZones={new Rect(1260,485,270,60),new Rect(1530,365,40,35),new Rect(1540,510,225,38),new Rect(1888,465,25,28),new Rect(1880,280,45,35)};
        private static void BuildSurface(bool[] mask,PreparedCandidateOutput manifest)
        {
            var source=Load<MapSurfaceDataAsset>(SourceFolder+"/Surface.asset");Require(source.TryCreateRuntimeBlobAsset(Allocator.Temp,out var original),"Source surface missing");
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
                    else if(sample.SurfaceType!=MapSurfaceType.Blocked)
                    {sample.MovementMask=MapSurfaceMovementMask.AirGrounded;if(sample.SlopeDegrees<=35)sample.MovementMask|=MapSurfaceMovementMask.Infantry;if(sample.SlopeDegrees<=22)sample.MovementMask|=MapSurfaceMovementMask.WheeledVehicle|MapSurfaceMovementMask.TrackedVehicle;}
                    // Only the qualified reserve footprint gains placement permission after its
                    // source dressing was removed. Roads remain protected by their real flags.
                    if(x+CropX>=1305&&x+CropX<1331&&z+CropZ>=485&&z+CropZ<510&&!mask[(z+CropZ)*2048+x+CropX]&&(sample.Flags&(MapSurfaceFlags.Road|MapSurfaceFlags.Highway|MapSurfaceFlags.Bridge))==0&&sample.SlopeDegrees<=10)
                    {sample.MovementMask|=MapSurfaceMovementMask.BuildingPlacement;sample.Flags&=~MapSurfaceFlags.Reserved;}
                    if((sample.MovementMask&MapSurfaceMovementMask.AllGroundUnits)!=MapSurfaceMovementMask.AllGroundUnits)blocked.Add(new Vector2Int(x,z));samples[i]=sample;
                }
                using var baked=builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);var surface=Clone<MapSurfaceDataAsset>(SourceFolder+"/Surface.asset",SurfacePath);surface.ConfigureBakedSurface(new Vector3(CropX,0,CropZ),1,new Vector2Int(Width,Height),baked,false);EditorUtility.SetDirty(surface);
                var grid=Clone<GridAuthoringSceneConfigAsset>(SourceFolder+"/Grid.asset",GridPath);SetField(typeof(GridAuthoringConfig),grid,"origin",new Vector3(CropX,0,CropZ));SetField(typeof(GridAuthoringConfig),grid,"width",Width);SetField(typeof(GridAuthoringConfig),grid,"height",Height);SetField(typeof(GridAuthoringConfig),grid,"blockedCells",blocked.ToArray());EditorUtility.SetDirty(grid);AssetDatabase.SaveAssets();
            }
        }
        private static void ConfigureAnchors(OperationMapDefinition map)
        {
            SetField(map,"sourceBinding",default(OperationMapSourceBindingConfig));var surface=Load<MapSurfaceDataAsset>(SurfacePath);Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out var blob),"Derivative surface missing");using(blob)
            {
                (string id,int x,int z,OperationMapAnchorKind kind,int faction,float radius)[] seeds={
                    ("forward_post",1380,529,OperationMapAnchorKind.Base,1,8),("fuel_reserve",1305,485,OperationMapAnchorKind.Build,1,1),("initial_barracks",1325,500,OperationMapAnchorKind.Build,1,1),("build_zone",1360,515,OperationMapAnchorKind.Build,1,65),
                    ("coverage",1440,529,OperationMapAnchorKind.Lane,1,24),("radar",1440,529,OperationMapAnchorKind.Deployment,1,1),("launcher_air",1480,529,OperationMapAnchorKind.Deployment,1,1),("launcher_ground",1280,529,OperationMapAnchorKind.Deployment,1,1),
                    ("armor",1360,529,OperationMapAnchorKind.Deployment,1,1),("armor_b",1348,529,OperationMapAnchorKind.Deployment,1,1),("armor_c",1336,529,OperationMapAnchorKind.Deployment,1,1),("command_squad",1385,515,OperationMapAnchorKind.Deployment,1,3),("aircraft",1540,379,OperationMapAnchorKind.Deployment,1,1),
                    ("battery",1560,529,OperationMapAnchorKind.Spawn,2,1),("hostile_armor",1585,529,OperationMapAnchorKind.Spawn,2,1),("hostile_armor_b",1597,529,OperationMapAnchorKind.Spawn,2,1),("hostile_armor_c",1609,529,OperationMapAnchorKind.Spawn,2,1),
                    ("command",1700,529,OperationMapAnchorKind.Spawn,2,3),("command_staff",1715,529,OperationMapAnchorKind.Spawn,2,3),("authority",1740,529,OperationMapAnchorKind.Lane,1,12),
                    ("relief",1900,480,OperationMapAnchorKind.Civilian,0,40),("air_approach",1900,294,OperationMapAnchorKind.Spawn,2,5),("air_flank",1900,700,OperationMapAnchorKind.Lane,2,5),("air_contact",1500,700,OperationMapAnchorKind.Hostile,2,5),
                    ("assault_approach",1640,529,OperationMapAnchorKind.Hostile,2,6),("contact",1510,529,OperationMapAnchorKind.Hostile,2,4),("fork",1440,529,OperationMapAnchorKind.Lane,1,4),("inner_core",1390,529,OperationMapAnchorKind.Base,1,5),("return_rts",1380,529,OperationMapAnchorKind.Camera,1,2)};
                var data=new SerializedObject(map);A(data.FindProperty("anchors"),seeds.Length,(p,i)=>{var seed=seeds[i];Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(seed.x-CropX,seed.z-CropZ),out var sample),seed.id);S(p,"anchorId",Prefix+seed.id);S(p,"kind",(int)seed.kind);S(p,"factionId",seed.faction);S(p,"laneIndex",0);p.FindPropertyRelative("position").vector3Value=new Vector3(seed.x,sample.Height,seed.z);p.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;p.FindPropertyRelative("radius").floatValue=seed.radius;});
                var bounds=data.FindProperty("bounds");bounds.FindPropertyRelative("worldMin").vector3Value=new Vector3(CropX,-20,CropZ);bounds.FindPropertyRelative("worldMax").vector3Value=new Vector3(CropX+Width,1000,CropZ+Height);bounds.FindPropertyRelative("playableMin").vector3Value=new Vector3(CropX+8,-20,CropZ+8);bounds.FindPropertyRelative("playableMax").vector3Value=new Vector3(CropX+Width-8,980,CropZ+Height-8);bounds.FindPropertyRelative("cameraMin").vector3Value=new Vector3(CropX,-20,CropZ);bounds.FindPropertyRelative("cameraMax").vector3Value=new Vector3(CropX+Width,980,CropZ+Height);
                S(data,"planningCameraId","camera.ch04.m05.planning");S(data,"battleCameraId","camera.ch04.m05.battle");
                A(data.FindProperty("cameras"),2,(p,i)=>{var focus=new Vector3(1400,0,529);var pos=focus+new Vector3(0,i==0?155:90,i==0?-95:-65);S(p,"cameraId","camera.ch04.m05."+(i==0?"planning":"battle"));p.FindPropertyRelative("position").vector3Value=pos;p.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(focus-pos).eulerAngles;p.FindPropertyRelative("fieldOfView").floatValue=58;p.FindPropertyRelative("orthographic").boolValue=false;});data.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        public static void ValidateRoutes()
        {
            var grid=Load<GridAuthoringSceneConfigAsset>(GridPath);var blocked=new HashSet<Vector2Int>(grid.BlockedCells);bool Fits(int x,int z,int r){for(int dz=-r;dz<=r;dz++)for(int dx=-r;dx<=r;dx++)if(blocked.Contains(new Vector2Int(x+dx-CropX,z+dz-CropZ)))return false;return true;}
            for(int x=1275;x<=1745;x++)Require(Fits(x,529,3),"Armor Break military highway blocked: "+x);
            foreach(var p in new[]{new Vector2Int(1385,515),new Vector2Int(1900,480),new Vector2Int(1740,529)})Require(Fits(p.x,p.y,3),"Armor Break formation blocked: "+p);
            var reserve=Load<GameObject>(ReservePrefabPath).GetComponent<BuildingDefinitionAuthoring>();Require(reserve.ConfiguredFootprintCells==new Vector2Int(26,25)&&reserve.ConfiguredFuelStorageCapacity==240,"Qualified reserve footprint/capacity drift");
            var surface=Load<MapSurfaceDataAsset>(SurfacePath);Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out var blob),"Reserve surface missing");using(blob)for(int z=485;z<510;z++)for(int x=1305;x<1331;x++)Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(x-CropX,z-CropZ),out var sample)&&(sample.MovementMask&MapSurfaceMovementMask.BuildingPlacement)!=0,"Reserve placement footprint rejected "+x+","+z);
            var weapon=Load<GroundMissileLauncherConfig>("Assets/Game/Configs/Weapons/GroundMissileLauncher_Ground_Config.asset");Require(280>weapon.MinRange&&280<weapon.MaxRange,"Authority battery outside real G2G range");Require(Vector2.Distance(new Vector2(1560,529),new Vector2(1900,480))>40+weapon.DamageRadius,"Battery missile collateral overlaps protected relief");
            Debug.Log("[ArmorBreakRoutes] result=Passed militaryHighway=471x7 G2GRange=280 reliefRadius=40 grid=1280x520 sourcePreserved=true");
        }
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
