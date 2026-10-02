using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Composition;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>Read-only qualification of saved Last Corridor physical content.</summary>
    public static class CH05M04LastCorridorMapValidation
    {
        [Serializable]private sealed class Receipt{public SourceFile[] files;}
        [Serializable]private sealed class SourceFile{public string path,sha256;}
        private static Dictionary<string,string> ReadSourceHashes()
        {
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText("Design/AgentReports/CH05M04LastCorridor/source-sha-before-generation.json"));
            if(receipt?.files==null||receipt.files.Length!=6)throw new InvalidOperationException("Corridor source-preservation receipt must contain exactly six sources.");
            var hashes=receipt.files.ToDictionary(f=>f.path,f=>f.sha256,StringComparer.Ordinal);
            if(!hashes.Keys.OrderBy(p=>p,StringComparer.Ordinal).SequenceEqual(CH05M04LastCorridorConfigBuilder.ProtectedSources.OrderBy(p=>p,StringComparer.Ordinal)))throw new InvalidOperationException("Source receipt paths do not match the protected Urban source inputs.");return hashes;
        }
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Saved map qualification requires edit mode.");
            var errors=new List<string>();void Check(bool condition,string message){if(!condition)errors.Add(message);}
            var map=Load<OperationMapDefinition>(CH05M04LastCorridorConfigBuilder.MapPath);
            var grid=Load<GridAuthoringSceneConfigAsset>(CH05M04LastCorridorConfigBuilder.GridPath);
            var surface=Load<MapSurfaceDataAsset>(CH05M04LastCorridorConfigBuilder.SurfacePath);
            string entityGuid=AssetDatabase.AssetPathToGUID(CH05M04LastCorridorConfigBuilder.EntityScenePath),bindingGuid=AssetDatabase.AssetPathToGUID(CH05M04LastCorridorConfigBuilder.BindingPath);
            Check(map.OperationMapId==CH05M04LastCorridorConfigBuilder.MapId&&!map.SourceBinding.IsConfigured,"Derivative must own a standalone physical map identity.");
            Check(map.TryValidateMetadata(out string metadataError),"Metadata: "+metadataError);Check(map.TryValidateLocalContentReferences(out string referenceError),"Local content: "+referenceError);
            foreach(var lane in new[]{("main",420f),("alternate",432f)})
            {
                foreach(var waypoint in new[]{("entry",980f),("middle",1100f),("exit",1230f)})
                {
                    var anchor=map.Anchors.ToArray().Single(a=>a.AnchorId=="anchor.ch05.m04."+lane.Item1+"_"+waypoint.Item1);
                    Check(anchor.Position.x==waypoint.Item2&&anchor.Position.z==lane.Item2,"Connected ground lane waypoint mismatch: "+lane.Item1+"/"+waypoint.Item1);
                }
            }
            var savedAnchors=map.Anchors.ToArray();
            foreach(var receiver in new[]{("medicine_receiver",1240f,432f),("fuel_receiver",1240f,420f),("reinforcement_receiver",1216f,432f),("key_receiver",1228f,426f)})
            {
                string id="anchor.ch05.m04."+receiver.Item1;
                var matches=savedAnchors.Where(a=>a.AnchorId==id).ToArray();
                Check(matches.Length==1,"Saved receiver must exist exactly once: "+id);
                if(matches.Length!=1)continue;
                var anchor=matches[0];
                Check(anchor.Position.x==receiver.Item2&&anchor.Position.z==receiver.Item3&&anchor.Radius==6,
                    "Stale/shared receiving anchor: "+id+" expected="+receiver.Item2+","+receiver.Item3+" radius=6 actual="+anchor.Position+" radius="+anchor.Radius);
            }
            var repair=map.Anchors.ToArray().Single(a=>a.AnchorId=="anchor.ch05.m04.repair");
            Check(repair.Position.x==1120&&repair.Position.z==420&&repair.Radius==6,"Original engineer repair must use external qualified gate.");
            foreach(var item in new[]{(CH05M04LastCorridorConfigBuilder.CivicPrefabPath,new Vector2Int(14,12),1000),(CH05M04LastCorridorConfigBuilder.ReceiverPrefabPath,new Vector2Int(26,25),0),(CH05M04LastCorridorConfigBuilder.ReservePrefabPath,new Vector2Int(26,25),0)})
            {
                var prefab=Load<GameObject>(item.Item1);Check(PrefabUtility.GetPrefabAssetType(prefab)!=PrefabAssetType.Variant,"Mission building must be independent: "+item.Item1);
                var definition=prefab.GetComponent<BuildingDefinitionAuthoring>();Check(definition!=null&&definition.ConfiguredFootprintCells==item.Item2&&(item.Item3==0||definition.ConfiguredMaxHealth==item.Item3),"Saved native building footprint/health mismatch: "+item.Item1);
            }
            Check(!string.IsNullOrEmpty(entityGuid)&&!string.IsNullOrEmpty(bindingGuid)&&entityGuid!=bindingGuid,"Separate entity/binding GUIDs required.");
            Check(map.NavigationMetadata.AuthoredSubSceneGuid==entityGuid,"Definition entity-scene GUID mismatch.");
            Check(map.SourceSceneReference.AssetGUID==bindingGuid,"Definition must reference its own binding scene.");
            Check(map.GridMetadata.AssetGuid==AssetDatabase.AssetPathToGUID(CH05M04LastCorridorConfigBuilder.GridPath),"Definition grid GUID mismatch.");
            Check(map.MapSurfaceDataReference.AssetGUID==AssetDatabase.AssetPathToGUID(CH05M04LastCorridorConfigBuilder.SurfacePath),"Definition surface GUID mismatch.");
            Check(map.MinimapRasterReference.AssetGUID==AssetDatabase.AssetPathToGUID(CH05M04LastCorridorConfigBuilder.Folder+"/Minimap.png"),"Definition minimap GUID mismatch.");
            var raster=new Texture2D(2,2,TextureFormat.RGBA32,false,false);
            try{Check(ImageConversion.LoadImage(raster,File.ReadAllBytes(CH05M04LastCorridorConfigBuilder.Folder+"/Minimap.png"),false)&&CH05M04LastCorridorConfigBuilder.IsReadableRaster(raster),"Minimap must be readable and reject black/uniform output.");}finally{UnityEngine.Object.DestroyImmediate(raster);}
            var dimensions=new Vector2Int(CH05M04LastCorridorConfigBuilder.Width,CH05M04LastCorridorConfigBuilder.Height);var origin=new Vector3(CH05M04LastCorridorConfigBuilder.CropX,0,CH05M04LastCorridorConfigBuilder.CropZ);
            Check(grid.Width==dimensions.x&&grid.Height==dimensions.y&&grid.Origin==origin&&Mathf.Approximately(grid.CellSize,1),"Saved grid dimensions/origin/cell size mismatch.");
            Check(map.GridMetadata.Dimensions==dimensions&&map.GridMetadata.Origin==origin,"Definition grid metadata mismatch.");
            Check(surface.Dimensions==dimensions&&surface.GridOrigin==origin&&Mathf.Approximately(surface.CellSize,1),"Saved surface dimensions/origin mismatch.");
            Check(map.GridMetadata.ContentHash==FileHash(CH05M04LastCorridorConfigBuilder.GridPath),"Saved grid SHA does not match definition.");
            Check(map.SurfaceMetadata.ContentHash==FileHash(CH05M04LastCorridorConfigBuilder.SurfacePath),"Saved surface SHA does not match definition.");
            Check(map.Bounds.WorldMin.x==origin.x&&map.Bounds.WorldMin.z==origin.z&&map.Bounds.WorldMax.x==origin.x+dimensions.x&&map.Bounds.WorldMax.z==origin.z+dimensions.y,"World bounds must match the independent crop.");
            foreach(var pair in ReadSourceHashes())Check(File.Exists(pair.Key)&&FileHash(pair.Key)==pair.Value,"Source SHA changed: "+pair.Key);
            var previous=SceneManager.GetActiveScene();Scene entities=default,binding=default;int identityCount=0,ownerCount=0,renderCount=0,rootCount=0;
            try
            {
                entities=EditorSceneManager.OpenScene(CH05M04LastCorridorConfigBuilder.EntityScenePath,OpenSceneMode.Additive);
                var identities=All<DenseCityPresentationIdentityAuthoring>(entities);var owners=All<OperationMapBuildingAuthoring>(entities);var roots=All<OperationMapEntityPresentationRootAuthoring>(entities);
                Check(All<OperationMapEntityPresentationIdentityAuthoring>(entities).Length==0,"Own cropped scene must not retain accepted whole-city descriptors alongside generated identities.");
                Check(All<OperationMapAuthoredVehicleOwnershipAuthoring>(entities).Length==0,"Own cropped scene must not retain unrelated source authored vehicles.");
                identityCount=identities.Length;ownerCount=owners.Length;renderCount=identities.Count(i=>i.Role==OperationMapEntityPresentationRole.RenderOnly);rootCount=roots.Length;
                Check(identityCount>0&&identities.Select(i=>i.StableId).Distinct(StringComparer.Ordinal).Count()==identityCount,"Generated presentation identities must be nonempty and unique.");
                foreach(var identity in identities)Check(identity.TryValidate(out string error),"Identity "+identity.StableId+": "+error);
                Check(ownerCount>0&&identities.Count(i=>i.Role==OperationMapEntityPresentationRole.GameplayBuildings)==ownerCount,"Gameplay owners/identities count mismatch.");
                Check(rootCount==3&&roots.Select(r=>r.Role).Distinct().Count()==3,"Exactly three distinct presentation ownership roots required.");
                foreach(var root in roots)
                {
                    Check(root.TryValidate(out string error),"Presentation root: "+error);Check(root.OperationMapId==map.OperationMapId,"Presentation root map identity mismatch.");
                    Check(root.ExpectedGeneratedIdentityCount==identityCount&&root.ExpectedGameplayBuildingCount==ownerCount&&root.ExpectedGameplayVehicleCount==0&&root.ExpectedRenderOnlyCount==renderCount,"Presentation root expected counts mismatch actual native authoring: "+root.Role);
                    Check(root.MigrationRecordSetHash==map.SourceIdentityHash,"Presentation migration hash must match derivative identity hash.");
                }
                foreach(var owner in owners)
                {
                    Check(owner.TryValidate(out string error),"Owner "+owner.StableId+": "+error);Check(owner.OperationMapId==map.OperationMapId,"Owner map identity mismatch: "+owner.StableId);
                    Vector2Int cell=owner.OriginCell,size=owner.FootprintCells;
                    Check(cell.x>=0&&cell.y>=0&&size.x>0&&size.y>0&&cell.x+size.x<=dimensions.x&&cell.y+size.y<=dimensions.y,"Rebased owner footprint outside cropped grid: "+owner.StableId+" origin="+cell+" size="+size);
                }
                Check(All<OperationMapVirtualizedPresentationAuthoring>(entities).Length==0&&map.RenderResidencyMode==OperationMapRenderResidencyMode.ResidentEntities,"Corridor crop must own resident rendering without whole-city virtualization database.");
                var authoredGrids=All<GridAuthoring>(entities);var authoredSurfaces=All<MapSurfaceAuthoring>(entities);
                Check(authoredGrids.Length==1&&authoredSurfaces.Length==1,"Entity scene must own exactly one grid and one surface authoring.");
                if(authoredSurfaces.Length==1)Check(authoredSurfaces[0].BakedSurfaceData==surface&&authoredSurfaces[0].GridConfig==grid,"Entity surface must reference own assets.");
                if(authoredGrids.Length==1)
                {
                    Check(authoredGrids[0].Width==dimensions.x&&authoredGrids[0].Height==dimensions.y,"Entity grid dimensions mismatch.");
                    long localId=checked((long)GlobalObjectId.GetGlobalObjectIdSlow(authoredGrids[0]).targetObjectId);Check(localId==map.NavigationMetadata.GridAuthoringLocalId,"Definition grid authoring local ID mismatch.");
                    var config=new SerializedObject(authoredGrids[0]).FindProperty("config").objectReferenceValue;Check(config==grid,"Entity grid must reference own config asset.");
                }
                binding=EditorSceneManager.OpenScene(CH05M04LastCorridorConfigBuilder.BindingPath,OpenSceneMode.Additive);var views=All<OperationMapSceneView>(binding);Check(views.Length==1,"Binding must own one operation-map view.");
                if(views.Length==1)
                {
                    var view=views[0];Check(view.TryValidate(out string error),"Binding view: "+error);Check(view.OperationMapId==map.OperationMapId&&view.Definition==map,"Binding definition identity mismatch.");
                    Check(view.PresentationSourceSceneGuid==entityGuid&&view.PresentationSourceScenePath==CH05M04LastCorridorConfigBuilder.EntityScenePath,"Binding presentation delivery mismatch.");
                    Check(view.GridAuthoringConfig==grid&&view.MapSurfaceAuthoring!=null&&view.MapSurfaceAuthoring.BakedSurfaceData==surface&&view.MapSurfaceAuthoring.GridConfig==grid,"Binding must reference own grid/surface assets.");
                    Check(view.MapSubScene!=null&&!view.MapSubScene.enabled&&!view.MapSubScene.AutoLoadScene&&view.MapSubScene.SceneAsset==null,"Binding SubScene must remain disabled, unbound and not autoloaded.");
                }
            }
            finally
            {
                if(binding.IsValid()&&binding.isLoaded)EditorSceneManager.CloseScene(binding,true);if(entities.IsValid()&&entities.isLoaded)EditorSceneManager.CloseScene(entities,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            }
            if(errors.Count>0){foreach(string error in errors)Debug.LogError("[LastCorridorMap] check=Failed "+error);throw new InvalidOperationException("Last Corridor saved map failed "+errors.Count+" checks.");}
            CH05M04LastCorridorConfigBuilder.ValidateRoutes();
            Debug.Log("[LastCorridorMap] result=Passed identities="+identityCount+" owners="+ownerCount+" renderOnly="+renderCount+" roots="+rootCount+" grid=440x220 sourceHashes=6 sourcePreserved=true binding=OwnExplicitLoader rebasedFootprints=Passed");
        }
        public static void RunWrapper(){try{Run();MissionEditorValidationExit.Complete(true);}catch(Exception error){Debug.LogException(error);MissionEditorValidationExit.Complete(false);}}
        private static T[] All<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        private static T Load<T>(string path)where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException("Required asset missing: "+path);
        private static string FileHash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-",string.Empty).ToLowerInvariant();}
    }
}
