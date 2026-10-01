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
    /// <summary>Independent Airfield derivative. Source assets remain unchanged.</summary>
    public static class CH04M04GroundedSignalConfigBuilder
    {
        public const string MissionId="saga.ch04.m04.grounded_signal", ScenarioId="scenario.ch04.m04.grounded_signal";
        public const string MapId="opmap.ch04.grounded_signal", Prefix="anchor.ch04.m04.";
        public const string MissionPath="Assets/Game/Configs/Missions/Chapter04/MissionDefinition_Ch04_M04_GroundedSignal.asset";
        public const string ScenarioPath="Assets/Game/Configs/Scenarios/Chapter04/ScenarioSetup_Ch04_M04_GroundedSignal.asset";
        public const string Folder="Assets/Game/GeneratedOperationMaps/CH04M04GroundedSignal";
        public const string MapPath=Folder+"/Definition.asset", SurfacePath=Folder+"/Surface.asset", GridPath=Folder+"/Grid.asset";
        public const string SceneFolder="Assets/Game/Scenes/OperationMaps/CH04M04GroundedSignal";
        public const string EntityScenePath=SceneFolder+"/PreparedEntities.unity", BindingPath=SceneFolder+"/RuntimeBinding.unity";
        public const string PassengerRole="role.friendly.specialist", CarrierRole="role.friendly.rescue_apc", AircraftRole="role.friendly.airlift", RelayRole="role.hostile.relay", CivilianRole="role.protected.terminal";
        private const string SourceFolder="Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate";
        private const string SourceScene="Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/PreparedEntities.unity";
        private const string SourceBinding="Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/RuntimeBinding.unity";
        private const string SourceManifest="Design/MapVariants/Preparation/CityEdgeAirfield/Candidate/output-manifest.json";
        public static void Build()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Grounded Signal generation requires edit mode.");
            foreach(string folder in new[]{Folder,SceneFolder,"Assets/Game/Configs/Missions/Chapter04","Assets/Game/Configs/Scenarios/Chapter04"}) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            EditorApplication.LockReloadAssemblies();
            try {BuildPhysicalMap(); BuildScenario(); BuildMission(); AssetDatabase.SaveAssets();}
            finally {EditorApplication.UnlockReloadAssemblies();}
            Debug.Log("[GroundedSignalConfig] result=Passed specialists=2 plane=1 APC=1 escort=4 relay=1 protectedCivilians=3 insertion=RunwayUnload extraction=APC");
        }
        /// <summary>Resume after physical authoring without regenerating source scenes or raster imports.</summary>
        public static void CompletePreparedConfiguration()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Grounded Signal configuration requires edit mode.");
            EditorApplication.LockReloadAssemblies();
            try
            {
                var map=Load<OperationMapDefinition>(MapPath);ConfigureAnchors(map);Hash(map);EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
                Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);
                ValidateRoutes();BuildScenario();BuildMission();AssetDatabase.SaveAssets();
                Debug.Log("[GroundedSignalConfig] result=Passed specialists=2 plane=1 APC=1 escort=4 relay=1 protectedCivilians=3 insertion=RunwayUnload extraction=APC resumedPreparedMap=true");
            }
            finally {EditorApplication.UnlockReloadAssemblies();}
        }
        public static void RepairPreparedIdentities()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Identity repair requires edit mode.");
            var previous=SceneManager.GetActiveScene();Scene scene=default;
            try
            {
                scene=EditorSceneManager.OpenScene(EntityScenePath,OpenSceneMode.Additive);
                var identities=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).ToArray();int changed=0;
                foreach(var identity in identities)
                {
                    if(!identity.name.StartsWith("RelayFence_",StringComparison.Ordinal))continue;
                    var matrix=identity.transform.localToWorldMatrix;
                    string transformKey=string.Join(",",Enumerable.Range(0,16).Select(i=>matrix[i].ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
                    string suffix=Digest(MapId+"/relay-fence/"+transformKey);
                    identity.name="RelayFence_"+suffix.Substring(0,16);
                    identity.ConfigureForEditor("densecity."+suffix,OperationMapEntityPresentationRole.RenderOnly,DenseCityPresentationSemanticCategory.Prop);changed++;
                }
                Require(identities.Select(i=>i.StableId).Distinct(StringComparer.Ordinal).Count()==identities.Length,"Remaining generated identity collision after own fence repair.");
                foreach(var identity in identities)Require(identity.TryValidate(out string error),error);
                Require(EditorSceneManager.SaveScene(scene,EntityScenePath),"Identity repair scene save failed.");
                Debug.Log("[GroundedSignalIdentities] result=Passed repairedFences="+changed+" unique="+identities.Length+" preservedNonFenceIdentities=true");
            }
            finally {if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
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
                Debug.Log("[GroundedSignalSceneOwnership] result=Passed owner=ExplicitEntitySceneLoader autoLoad=false disabledPlaceholder=true guid="+view.Definition.NavigationMetadata.AuthoredSubSceneGuid);
            }
            finally {if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        public static void BuildPhysicalMap()
        {
            var source=Load<OperationMapDefinition>(M04AirliftConfigBuilder.PreparedMapPath);
            Require(source.ContentHash==M04AirliftConfigBuilder.PreparedHash,"Airfield prepared source hash changed; re-audit derivative.");
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
                foreach(var row in removed)
                {
                    string name=Path.GetFileNameWithoutExtension(row.sourceRecipe.Split('|')[0]);
                    var candidates=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true))
                        .Where(t=>Vector3.Distance(t.position,row.runtimePosition)<.02f&&t.name.StartsWith(name,StringComparison.Ordinal)&&t.GetComponent<DenseCityPresentationIdentityAuthoring>()!=null).ToArray();
                    Require(candidates.Length==1,"Ambiguous derivative removal "+row.stableKey+" candidates="+candidates.Length);
                    UnityEngine.Object.DestroyImmediate(candidates[0].gameObject);
                }
                var staticMask=new bool[2048*1024];
                var inventory=new MapPreparationManifest {runtimePlayableMin=manifest.runtimePlayableMin,playableSize=manifest.playableSize};
                foreach(var row in manifest.classifications.Except(removed)) if(row.movementBlocked&&!row.damageEligible)
                    MapVariantPreparedCandidateBuilder.Rasterize(row.runtimeFootprint,staticMask,inventory);
                var fenceRoot=new GameObject("GroundedSignalMilitaryCompound");SceneManager.MoveGameObjectToScene(fenceRoot,scene);
                // Single fence perimeter; southern gate remains 20 cells wide for passengers/APC.
                FenceLine(fenceRoot.transform,new Vector2(780,625),new Vector2(790,625),staticMask);
                FenceLine(fenceRoot.transform,new Vector2(810,625),new Vector2(842,625),staticMask);
                FenceLine(fenceRoot.transform,new Vector2(780,625),new Vector2(780,675),staticMask);
                FenceLine(fenceRoot.transform,new Vector2(842,625),new Vector2(842,675),staticMask);
                FenceLine(fenceRoot.transform,new Vector2(780,675),new Vector2(842,675),staticMask);
                AddRunwayConnector(scene);
                BuildSurface(staticMask,manifest);
                var grid=Load<GridAuthoringSceneConfigAsset>(GridPath);var surface=Load<MapSurfaceDataAsset>(SurfacePath);
                foreach(var root in scene.GetRootGameObjects()) foreach(var component in root.GetComponentsInChildren<Component>(true))
                {
                    if(component==null)continue;
                    var data=new SerializedObject(component);var p=data.GetIterator();
                    while(p.Next(true)) if(p.propertyType==SerializedPropertyType.String&&p.stringValue==source.OperationMapId)p.stringValue=MapId;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    if(component is GridAuthoring ga)ga.Configure(grid);
                    if(component is MapSurfaceAuthoring sa){var d=new SerializedObject(sa);d.FindProperty("gridConfig").objectReferenceValue=grid;d.FindProperty("bakedSurfaceData").objectReferenceValue=surface;d.ApplyModifiedPropertiesWithoutUndo();}
                }
                var identities=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).ToArray();
                int owners=scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length);
                int rendered=identities.Length-owners;
                Require(identities.Select(i=>i.StableId).Distinct(StringComparer.Ordinal).Count()==identities.Length,"Generated derivative identity collision.");
                foreach(var marker in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true)))
                {var d=new SerializedObject(marker);d.FindProperty("migrationRecordSetHash").stringValue=Digest(MapId+":airfield-derivative-v2:"+source.ContentHash);d.FindProperty("expectedRenderOnlyCount").intValue=rendered;d.FindProperty("expectedGeneratedIdentityCount").intValue=identities.Length;d.ApplyModifiedPropertiesWithoutUndo();}
                CreateMinimap(scene,manifest);
                Require(EditorSceneManager.SaveScene(scene,EntityScenePath),"Derivative entity save failed");
                ValidateProtectedTerminal(scene);
                var gaSaved=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GridAuthoring>(true)).Single();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(gaSaved,out _,out long localId);
                if(localId<=0)localId=checked((long)GlobalObjectId.GetGlobalObjectIdSlow(gaSaved).targetObjectId);
                var map=Clone<OperationMapDefinition>(M04AirliftConfigBuilder.PreparedMapPath,MapPath);var md=new SerializedObject(map);
                S(md,"operationMapId",MapId);S(md,"sourceIdentityHash",Digest(MapId+":airfield-derivative-v2:"+source.ContentHash));
                // This owns a different physical scene. SourceBinding denotes logical reuse of
                // the exact same source scene and must therefore remain unconfigured.
                var sb=md.FindProperty("sourceBinding");S(sb,"sourceOperationMapId",string.Empty);S(sb,"sourceIdentityHash",string.Empty);S(sb,"sourceContentHash",string.Empty);
                md.FindProperty("mapSurfaceDataReference").FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(SurfacePath);
                md.FindProperty("minimapRasterReference").FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(Folder+"/Minimap.png");
                md.FindProperty("additionalBuildingPlacements").objectReferenceValue=null;md.ApplyModifiedPropertiesWithoutUndo();
                SetField(map,"gridMetadata",new OperationMapGridMetadataConfig(AssetDatabase.AssetPathToGUID(GridPath),FileHash(GridPath),Vector3.zero,new Vector2Int(2048,1024),1,grid.BlockedCells.Length));
                SetField(map,"surfaceMetadata",new OperationMapSurfaceMetadataConfig(AssetDatabase.AssetPathToGUID(SurfacePath),FileHash(SurfacePath),surface.ComputeRuntimeBlobHash().ToString(),surface.SurfaceCount,surface.PayloadVersion,surface.PayloadEncoding,source.SurfaceMetadata.MinimumHeight,source.SurfaceMetadata.MaximumHeight));
                SetField(map,"navigationMetadata",new OperationMapNavigationMetadataConfig(AssetDatabase.AssetPathToGUID(EntityScenePath),localId,scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<StaticGridBlockerAuthoring>(true).Length)+owners,true,true,true));
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
                Debug.Log("[GroundedSignalMap] result=Passed removedStatic="+removed.Length+" identities="+identities.Length+" sourcePreserved="+source.ContentHash+" derivative="+map.ContentHash);
            }
            finally {if(binding.IsValid()&&binding.isLoaded)EditorSceneManager.CloseScene(binding,true);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(original.IsValid()&&original.isLoaded)EditorSceneManager.CloseScene(original,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        private static void AddRunwayConnector(Scene scene)
        {
            Bounds RenderBounds(GameObject go){var renderers=go.GetComponentsInChildren<Renderer>(true);Require(renderers.Length>0,"Runway has no rendered mesh.");var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;}
            var segments=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true))
                .Where(t=>t.name.StartsWith("SM_Env_Runway_01",StringComparison.Ordinal)&&t.GetComponent<DenseCityPresentationIdentityAuthoring>()!=null)
                .Select(t=>(root:t,bounds:RenderBounds(t.gameObject))).OrderBy(p=>p.bounds.min.x).ToArray();
            Require(segments.Length>=2,"Prepared Airfield runway segments unavailable.");int patches=0;
            for(int i=0;i<segments.Length-1;i++)
            {
                var left=segments[i];var right=segments[i+1];float gap=right.bounds.min.x-left.bounds.max.x;
                if(gap<=.03f)continue;
                Require(gap<=30,"Unexpected large physical runway gap requires re-authoring.");
                var clone=UnityEngine.Object.Instantiate(left.root.gameObject,left.root.parent);clone.name="GroundedSignalRunwayConnector_"+i;
                Vector3 scale=clone.transform.localScale;Vector3 worldX=clone.transform.InverseTransformDirection(Vector3.right);
                float ratio=(gap-.02f)/left.bounds.size.x;
                if(Mathf.Abs(worldX.x)>Mathf.Abs(worldX.z))scale.x*=ratio;else scale.z*=ratio;
                clone.transform.localScale=scale;var bounds=RenderBounds(clone);
                var center=new Vector3((left.bounds.max.x+right.bounds.min.x)*.5f,bounds.center.y,(left.bounds.center.z+right.bounds.center.z)*.5f);
                clone.transform.position+=center-bounds.center;
                clone.GetComponent<DenseCityPresentationIdentityAuthoring>().ConfigureForEditor("densecity."+Digest(MapId+"/continuous-runway/"+i),OperationMapEntityPresentationRole.RenderOnly,DenseCityPresentationSemanticCategory.Infrastructure);
                bounds=RenderBounds(clone);
                Require(bounds.min.x>=left.bounds.max.x&&bounds.max.x<=right.bounds.min.x,"Runway connector overlaps existing meshes.");
                patches++;
                Debug.Log("[GroundedSignalRunwayConnector] segment="+i+" gap="+gap+" matchingMaterials=true overlap=0 connectorBounds="+bounds);
            }
            Debug.Log("[GroundedSignalRunwayVisual] result=Passed connectors="+patches+" authoredRunwaySegments="+segments.Length+" style=SourceMeshesAndMaterials duplicateOverlap=0");
        }
        private static void ValidateProtectedTerminal(Scene scene)
        {
            var center=new Vector2(515,665);const float radius=42;
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var hangars=all.Where(t=>t.name.StartsWith("SM_Bld_Hangar",StringComparison.Ordinal)&&t.GetComponent<DenseCityPresentationIdentityAuthoring>()!=null&&Vector2.Distance(new Vector2(t.position.x,t.position.z),center)<=radius).ToArray();
            Require(hangars.Length>0,"Protected civilian terminal requires original hangar fixtures.");
            foreach(var t in all)
            {
                if(Vector2.Distance(new Vector2(t.position.x,t.position.z),center)>radius)continue;
                Require(t.GetComponent<OperationMapBuildingAuthoring>()==null&&t.GetComponent<BuildingDefinitionAuthoring>()==null&&t.GetComponent<UnitGridAuthoring>()==null,"Damage authoring found in protected terminal zone: "+t.name);
            }
            foreach(var t in hangars)Require(t.GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length==0&&t.GetComponentsInChildren<BuildingDefinitionAuthoring>(true).Length==0&&t.GetComponentsInChildren<UnitGridAuthoring>(true).Length==0,"Civilian hangar exposes damage authoring.");
            Debug.Log("[GroundedSignalProtectedTerminal] result=Passed staticHangars="+hangars.Length+" center=515,665 radius=42 damageAuthoring=0 staff=3 staffRole="+CivilianRole+" fixtures="+string.Join(";",hangars.Select(t=>t.name+"@"+t.position)));
        }
        private static void CreateMinimap(Scene scene,PreparedCandidateOutput manifest)
        {
            var lights=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Lighting").ToArray();
            var lightStates=lights.Select(t=>t.gameObject.activeSelf).ToArray();foreach(var t in lights)t.gameObject.SetActive(true);
            var owners=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).ToArray();
            foreach(var owner in owners)owner.DestroyedVisualRoot.transform.localScale=Vector3.zero;
            var go=new GameObject("GroundedSignalMinimap",typeof(Camera));SceneManager.MoveGameObjectToScene(go,scene);
            var camera=go.GetComponent<Camera>();var center=manifest.runtimePlayableMin+manifest.playableSize*.5f;
            camera.transform.SetPositionAndRotation(new Vector3(center.x,700,center.y),Quaternion.Euler(90,0,0));camera.orthographic=true;camera.orthographicSize=manifest.playableSize.y*.5f;camera.farClipPlane=4000;
            int height=800,width=Mathf.RoundToInt(height*manifest.playableSize.x/manifest.playableSize.y);var target=new RenderTexture(width,height,24);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
            try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Folder+"/Minimap.png",texture.EncodeToPNG());}
            finally {RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);for(int i=0;i<lights.Length;i++)lights[i].gameObject.SetActive(lightStates[i]);foreach(var owner in owners)owner.DestroyedVisualRoot.transform.localScale=Vector3.one;}
            AssetDatabase.ImportAsset(Folder+"/Minimap.png",ImportAssetOptions.ForceUpdate);
        }
        private static bool RemovePlacement(MapPreparationPlacement row)
        {
            if(row.sourceRecipe==MapVariantCityAirfield.TransportPlane||row.sourceRecipe==MapVariantCityAirfield.RunwayBarrier)return true;
            if(row.sourceRecipe==MapVariantCityAirfield.Helipad)return row.runtimeFootprint.Any(p=>new Rect(776,621,72,58).Contains(p));
            if(!row.movementBlocked||row.damageEligible||row.backdrop)return false;
            var poly=row.runtimeFootprint;
            // Clear military yard and both opening formation/plane-unload areas by exact footprint intersection.
            return poly.Any(p=>new Rect(776,621,72,58).Contains(p)) || poly.Any(p=>new Rect(570,535,100,72).Contains(p));
        }
        private static void BuildSurface(bool[] mask,PreparedCandidateOutput manifest)
        {
            var source=Load<MapSurfaceDataAsset>(SourceFolder+"/Surface.asset");Require(source.TryCreateRuntimeBlobAsset(Allocator.Temp,out var original),"Source surface missing");
            using(original)using(var builder=new BlobBuilder(Allocator.Temp))
            {
                ref var blob=ref builder.ConstructRoot<MapSurfaceBlob>();blob.GridOrigin=original.Value.GridOrigin;blob.CellSize=1;blob.Dimensions=original.Value.Dimensions;
                int count=2048*1024;var cells=builder.Allocate(ref blob.Cells,count);var samples=builder.Allocate(ref blob.Samples,count);builder.Allocate(ref blob.CompactSamples,0);builder.Allocate(ref blob.Connections,0);
                var blocked=new List<Vector2Int>();var bounds=new Rect(manifest.runtimePlayableMin,manifest.playableSize);
                for(int i=0;i<count;i++)
                {
                    Require(MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref original.Value,i,out var s),"Source sample missing "+i);
                    cells[i]=new MapSurfaceCell{FirstSurfaceIndex=i,SurfaceCount=1};s.FirstConnectionIndex=0;s.ConnectionCount=0;
                    bool inside=bounds.Contains(new Vector2(s.Cell.x+.5f,s.Cell.y+.5f));
                    if(inside&&mask[i]){s.MovementMask=MapSurfaceMovementMask.None;blocked.Add(new Vector2Int(s.Cell.x,s.Cell.y));}
                    else if(inside&&s.SurfaceType!=MapSurfaceType.Blocked)
                    {
                        s.MovementMask=MapSurfaceMovementMask.AirGrounded;
                        if(s.SlopeDegrees<=35)s.MovementMask|=MapSurfaceMovementMask.Infantry;
                        if(s.SlopeDegrees<=22)s.MovementMask|=MapSurfaceMovementMask.WheeledVehicle|MapSurfaceMovementMask.TrackedVehicle;
                        // Construction is disabled in this mission; keep existing placement reservations.
                    }
                    samples[i]=s;
                }
                using var baked=builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
                var surface=Clone<MapSurfaceDataAsset>(SourceFolder+"/Surface.asset",SurfacePath);surface.ConfigureBakedSurface(Vector3.zero,1,new Vector2Int(2048,1024),baked,false);EditorUtility.SetDirty(surface);
                var grid=Clone<GridAuthoringSceneConfigAsset>(SourceFolder+"/Grid.asset",GridPath);SetField(typeof(GridAuthoringConfig),grid,"blockedCells",blocked.ToArray());EditorUtility.SetDirty(grid);AssetDatabase.SaveAssets();
            }
        }
        private static void FenceLine(Transform parent,Vector2 from,Vector2 to,bool[] mask)
        {
            var prefab=Load<GameObject>(MapVariantKits.Fence);var direction=(to-from).normalized;float length=Vector2.Distance(from,to);int pieces=Mathf.Max(1,Mathf.FloorToInt(length/5));float spacing=length/pieces;
            for(int n=0;n<pieces;n++)
            {
                var p=from+direction*(n+.5f)*spacing;var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name="RelayFence_"+from.x+"_"+from.y+"_"+to.x+"_"+to.y+"_"+n;
                var renderers=go.GetComponentsInChildren<Renderer>(true);Bounds bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                bool alongZ=bounds.size.z>bounds.size.x;float along=alongZ?bounds.size.z:bounds.size.x;
                float yaw=-Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg+(alongZ?90:0);
                go.transform.rotation=Quaternion.Euler(0,yaw,0);
                go.transform.localScale=alongZ?new Vector3(1,1,spacing/Mathf.Max(.1f,along)):new Vector3(spacing/Mathf.Max(.1f,along),1,1);
                bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                go.transform.position+=new Vector3(p.x-bounds.center.x,-bounds.min.y,p.y-bounds.center.z);
                bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                foreach(var c in go.GetComponentsInChildren<Component>(true))if(c is Collider||c is Rigidbody)UnityEngine.Object.DestroyImmediate(c);
                var matrix=go.transform.localToWorldMatrix;string transformKey=string.Join(",",Enumerable.Range(0,16).Select(i=>matrix[i].ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
                go.AddComponent<DenseCityPresentationIdentityAuthoring>().ConfigureForEditor("densecity."+Digest(MapId+"/relay-fence/"+transformKey),OperationMapEntityPresentationRole.RenderOnly,DenseCityPresentationSemanticCategory.Prop);
                for(int z=Mathf.FloorToInt(bounds.min.z);z<Mathf.CeilToInt(bounds.max.z);z++)for(int x=Mathf.FloorToInt(bounds.min.x);x<Mathf.CeilToInt(bounds.max.x);x++)mask[z*2048+x]=true;
            }
        }
        private static void ConfigureAnchors(OperationMapDefinition map)
        {
            SetField(map,"sourceBinding",default(OperationMapSourceBindingConfig));
            var surface=Load<MapSurfaceDataAsset>(SurfacePath);Require(surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out var blob),"Derivative surface missing");using(blob)
            {
                (string id,int x,int z,OperationMapAnchorKind kind,int faction,float radius)[] seeds={
                    ("plane",600,560,OperationMapAnchorKind.Deployment,1,1),("apron",610,580,OperationMapAnchorKind.Lane,1,40),
                    ("specialists",610,590,OperationMapAnchorKind.Deployment,1,2),("carrier",635,590,OperationMapAnchorKind.Deployment,1,1),
                    ("escort",620,600,OperationMapAnchorKind.Deployment,1,3),("relay",800,648,OperationMapAnchorKind.Hostile,2,1),
                    ("hardware",812,650,OperationMapAnchorKind.Lane,1,12),("guards",828,644,OperationMapAnchorKind.Spawn,2,2),
                    ("civilian_terminal",515,650,OperationMapAnchorKind.Civilian,0,3),("exit",945,605,OperationMapAnchorKind.Lane,1,14),("departure",960,605,OperationMapAnchorKind.Camera,1,14),
                    ("return_rts",615,590,OperationMapAnchorKind.Camera,1,2)};
                var data=new SerializedObject(map);A(data.FindProperty("anchors"),seeds.Length,(p,i)=>{var seed=seeds[i];Require(MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,new int2(seed.x,seed.z),out var sample),seed.id);S(p,"anchorId",Prefix+seed.id);S(p,"kind",(int)seed.kind);S(p,"factionId",seed.faction);S(p,"laneIndex",0);p.FindPropertyRelative("position").vector3Value=new Vector3(seed.x,sample.Height,seed.z);p.FindPropertyRelative("eulerAngles").vector3Value=seed.id=="plane"?new Vector3(0,270,0):Vector3.zero;p.FindPropertyRelative("radius").floatValue=seed.radius;});
                var bounds=data.FindProperty("bounds");bounds.FindPropertyRelative("playableMin").vector3Value=new Vector3(450,-20,525);bounds.FindPropertyRelative("playableMax").vector3Value=new Vector3(975,980,690);bounds.FindPropertyRelative("cameraMin").vector3Value=new Vector3(420,-20,500);bounds.FindPropertyRelative("cameraMax").vector3Value=new Vector3(995,980,700);
                S(data,"planningCameraId","camera.ch04.m04.planning");S(data,"battleCameraId","camera.ch04.m04.battle");
                A(data.FindProperty("cameras"),2,(c,i)=>{var focus=new Vector3(630,0,585);var pos=focus+new Vector3(0,i==0?145:90,i==0?-80:-65);S(c,"cameraId","camera.ch04.m04."+(i==0?"planning":"battle"));c.FindPropertyRelative("position").vector3Value=pos;c.FindPropertyRelative("eulerAngles").vector3Value=Quaternion.LookRotation(focus-pos).eulerAngles;c.FindPropertyRelative("fieldOfView").floatValue=58;c.FindPropertyRelative("orthographic").boolValue=false;});data.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        public static void ValidateRoutes()
        {
            var grid=Load<GridAuthoringSceneConfigAsset>(GridPath);var blocked=new HashSet<Vector2Int>(grid.BlockedCells);
            bool Fits(Vector2Int p,int radius){for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)if(blocked.Contains(p+new Vector2Int(x,z)))return false;return true;}
            int Distance(Vector2Int start,Vector2Int goal,int radius)
            {Require(Fits(start,radius)&&Fits(goal,radius),"Grounded Signal blocked endpoint "+start+" / "+goal);var q=new Queue<Vector2Int>();var d=new Dictionary<Vector2Int,int>{{start,0}};q.Enqueue(start);while(q.Count>0){var p=q.Dequeue();if(p==goal)return d[p];foreach(var dir in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var n=p+dir;if(n.x<450||n.x>975||n.y<525||n.y>690||d.ContainsKey(n)||!Fits(n,radius))continue;d[n]=d[p]+1;q.Enqueue(n);}}throw new InvalidOperationException("Grounded Signal disconnected route "+start+" / "+goal);}
            for(int x=475;x<=925;x++)for(int z=547;z<=573;z++)Require(!blocked.Contains(new Vector2Int(x,z)),"Continuous runway obstruction "+x+","+z);
            int recovery=Distance(new Vector2Int(610,590),new Vector2Int(812,650),1),exit=Distance(new Vector2Int(635,590),new Vector2Int(945,605),3);
            Require(Fits(new Vector2Int(620,600),3),"Escort formation blocked");
            Debug.Log("[GroundedSignalRoutes] result=Passed runway=451x27 specialistToHardware="+recovery+" APCToExit="+exit+" APCMargin=3 gateWidth=20");
        }
        private static void BuildScenario()
        {
            var scenario=Clone<ScenarioSetupConfig>(M04AirliftConfigBuilder.ScenarioPath,ScenarioPath);var d=new SerializedObject(scenario);Replace(d,"ch01.m04","ch04.m04");Replace(d,"mission.m04.","mission.grounded_signal.");S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"deterministicSeed",4004001);
            var map=Load<OperationMapDefinition>(MapPath);A(d.FindProperty("requiredAnchors"),map.Anchors.Length,(p,i)=>{S(p,"anchorId",map.Anchors[i].AnchorId);S(p,"kind",(int)map.Anchors[i].Kind);});
            A(d.FindProperty("patrolRoutes"),0,null);A(d.FindProperty("ambientPresentations"),0,null);S(d.FindProperty("defense"),"enabled",false);S(d.FindProperty("missionRuntime"),"enabled",false);
            var restrictions=d.FindProperty("restrictions");foreach(var field in new[]{"buildingDisabled","productionDisabled","economyDisabled"})S(restrictions,field,true);S(restrictions,"transportDisabled",false);S(restrictions,"airDisabled",false);
            string[] names={"plane","specialists","carrier","escort","relay","guards","civilian_terminal"};string[][] units={new[]{"Veh_Plane_Transport"},new[]{"Chr_Pilot_Male_01","Chr_Bombsuit_Male_01"},new[]{"Veh_APC_Heavy"},new[]{"Chr_Soldier_Male_02_Alt_02","Chr_Soldier_Male_02_Alt_04","Chr_Soldier_Female_01_Alt_01","Chr_Soldier_Female_02_Alt_01"},new[]{"Veh_Radar_Tank"},new[]{"Chr_Insurgent_Male_03","Chr_Insurgent_Female_01","Chr_Insurgent_Male_03"},new[]{"Chr_Civilian_Female_01","Chr_Civilian_Female_02","Chr_Civilian_Male_01"}};
            string[] roles={AircraftRole,PassengerRole,CarrierRole,"role.friendly.command_squad",RelayRole,"role.hostile.guards",CivilianRole};
            A(d.FindProperty("unitGroups"),names.Length,(group,i)=>{S(group,"groupId","group.ch04.m04."+names[i]);S(group,"factionIndex",i is 4 or 5?2:1);A(group.FindPropertyRelative("units"),units[i].Length,(u,n)=>{string key="Unit_"+units[i][n],path="Assets/Game/Prefabs/"+(units[i][n].StartsWith("Veh_")?"Vehicles/":"Characters/")+key+".prefab";Load<GameObject>(path);S(u,"unitConfigKey","unit.ch04.m04."+units[i][n].ToLowerInvariant());S(u,"runtimePrefabSourceKey",key);S(u,"expectedAssetGuid",AssetDatabase.AssetPathToGUID(path));S(u,"spawnAnchorId",Prefix+names[i]);S(u,"missionRoleId",roles[i]);S(u,"count",1);});});
            var extraction=d.FindProperty("extraction");S(extraction,"enabled",true);S(extraction,"passengerRoleId",PassengerRole);S(extraction,"carrierRoleId",CarrierRole);S(extraction,"aircraftRoleId",AircraftRole);S(extraction,"rescueAnchorId",Prefix+"apron");S(extraction,"landingAnchorId",Prefix+"exit");S(extraction,"departureAnchorId",Prefix+"departure");S(extraction,"requiredPassengers",2);S(extraction,"secureHoldMilliseconds",6000);S(extraction,"deadlineMilliseconds",900000);S(extraction,"vehiclesSelfSupplied",true);S(extraction,"authoredMapDefensesDormant",true);extraction.FindPropertyRelative("landingRadius").floatValue=14;extraction.FindPropertyRelative("departureRadius").floatValue=14;
            d.ApplyModifiedPropertiesWithoutUndo();Require(scenario.TryValidate(out string error),error);EditorUtility.SetDirty(scenario);
        }
        private static void BuildMission()
        {
            var mission=Clone<MissionDefinitionConfig>(M04AirliftConfigBuilder.MissionPath,MissionPath);var d=new SerializedObject(mission);S(d,"missionId",MissionId);S(d,"scenarioId",ScenarioId);S(d,"operationMapId",MapId);S(d,"displayNameKey","mission.grounded_signal.name");S(d,"displaySummaryKey","mission.grounded_signal.summary");S(d,"locationNameKey","mission.grounded_signal.location");foreach(string stage in new[]{"briefing","comms","debrief"})S(d,stage+"SequenceId","seq.ch04.m04."+(stage=="briefing"?"brief":stage));
            string[] names={"extract","relay","civilian"};MissionObjectiveRuleKind[] rules={MissionObjectiveRuleKind.ExtractPassengers,MissionObjectiveRuleKind.DestroyMissionRole,MissionObjectiveRuleKind.ProtectMissionRole};string[] roles={PassengerRole,RelayRole,CivilianRole};
            A(d.FindProperty("objectives"),3,(p,i)=>{S(p,"objectiveId","obj.ch04.m04."+names[i]);S(p,"displayTextKey","mission.grounded_signal.objective."+names[i]);S(p,"rule",(int)rules[i]);S(p,"missionRoleId",roles[i]);S(p,"requiredCount",i==0?2:i==2?3:1);S(p,"failureOnRuleBreak",i==2);});
            A(d.FindProperty("stars"),3,(p,i)=>{S(p,"starIndex",i+1);S(p,"rule",(int)(i==0?MissionStarRuleKind.CompleteMission:i==1?MissionStarRuleKind.NoSquadLoss:MissionStarRuleKind.NoCivilianLoss));S(p,"displayTextKey","mission.grounded_signal.star."+(i+1));S(p,"threshold",0);});
            A(d.FindProperty("firstClearRewards"),3,(p,i)=>{S(p,"kind",i==1?1:0);S(p,"rewardConfigId",i==0?"reward.commander_xp":i==1?string.Empty:"reward.ch04.m04.paratrooper_reinforcements_unlock");S(p,"displayTextKey",i==0?"mission.reward.commander_xp":i==1?"mission.reward.credits":"mission.reward.paratroopers");S(p,"amount",i==0?2250:i==1?10000:1);});
            d.ApplyModifiedPropertiesWithoutUndo();Require(MissionDefinitionContractValidation.TryValidateDefinition(mission,out string error),error);EditorUtility.SetDirty(mission);
        }
        private static string FileHash(string path)=>Digest(File.ReadAllBytes(path));
        private static string Digest(string text)=>Digest(Encoding.UTF8.GetBytes(text));
        private static string Digest(byte[] bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-",string.Empty).ToLowerInvariant();}
        private static void Hash(OperationMapDefinition map){var d=new SerializedObject(map);S(d,"contentHash",string.Empty);S(d,"generatedMetadataHash",string.Empty);d.ApplyModifiedPropertiesWithoutUndo();string hash=Digest(EditorJsonUtility.ToJson(map));d.Update();S(d,"contentHash",hash);S(d,"generatedMetadataHash",hash);d.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetField(object target,string name,object value)=>SetField(target.GetType(),target,name,value);
        private static void SetField(Type type,object target,string name,object value){var f=type.GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);Require(f!=null,"Missing field "+name);f.SetValue(target,value);}
        private static T Load<T>(string path)where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException(path);
        private static T Clone<T>(string source,string target)where T:ScriptableObject{var result=AssetDatabase.LoadAssetAtPath<T>(target);if(result==null){result=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(result,target);}EditorUtility.CopySerialized(Load<T>(source),result);return result;}
        private static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
        private static void Replace(SerializedObject d,string from,string to){var p=d.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.String)p.stringValue=p.stringValue.Replace(from,to);}
        private static void A(SerializedProperty p,int count,Action<SerializedProperty,int> action){p.arraySize=count;for(int i=0;i<count;i++)action?.Invoke(p.GetArrayElementAtIndex(i),i);}
        private static void S(SerializedObject p,string name,object value)=>S(p.FindProperty(name),value);
        private static void S(SerializedProperty p,string name,object value)=>S(p.FindPropertyRelative(name),value);
        private static void S(SerializedProperty p,object value){Require(p!=null,"Missing serialized property");switch(value){case string s:p.stringValue=s;break;case int i:p.intValue=i;break;case bool b:p.boolValue=b;break;default:throw new ArgumentException(p.propertyPath);}}
    }
}
