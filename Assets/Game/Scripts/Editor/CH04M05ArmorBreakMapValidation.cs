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
    /// <summary>Read-only qualification of saved Armor Break physical content.</summary>
    public static class CH04M05ArmorBreakMapValidation
    {
        private const string SourceFolder="Assets/Game/GeneratedOperationMaps/Variants/Frontier/Candidate/";
        private static readonly Dictionary<string,string> ExpectedSourceHashes=new()
        {
            [SourceFolder+"Definition.asset"]="aaaa53263cfd17e032b01db9c5bc65492df0ab11f81bb7d9a03d5533e64df167",
            [SourceFolder+"Surface.asset"]="3b03cc8035ffde5ff3ca52863aecfa024aec2e25b9a4ad409c73b3622c201388",
            [SourceFolder+"Grid.asset"]="e8ad7d074eff75cc1a4f645b42324cd3b268530f43bb77b2abcf2336785ccc28",
            [SourceFolder+"Minimap.png"]="3659927ea9d4d1c07b9ef50ed6224e1c059ecdf4e30e82f85a013448a362994b",
            ["Assets/Game/Scenes/OperationMaps/Variants/Frontier/PreparedEntities.unity"]="720ccbb703d8389bc7d4e4b85825e29357a9c17ffb7cc6c52881c5a684e78bff",
            ["Assets/Game/Scenes/OperationMaps/Variants/Frontier/RuntimeBinding.unity"]="e2d3acbd08c99e51536f11af061158f4d98231b8aeef1f2b1ff56828db9ac428"
        };
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Saved map qualification requires edit mode.");
            var errors=new List<string>();void Check(bool condition,string message){if(!condition)errors.Add(message);}
            var map=Load<OperationMapDefinition>(CH04M05ArmorBreakConfigBuilder.MapPath);
            var grid=Load<GridAuthoringSceneConfigAsset>(CH04M05ArmorBreakConfigBuilder.GridPath);
            var surface=Load<MapSurfaceDataAsset>(CH04M05ArmorBreakConfigBuilder.SurfacePath);
            string entityGuid=AssetDatabase.AssetPathToGUID(CH04M05ArmorBreakConfigBuilder.EntityScenePath),bindingGuid=AssetDatabase.AssetPathToGUID(CH04M05ArmorBreakConfigBuilder.BindingPath);
            Check(map.OperationMapId==CH04M05ArmorBreakConfigBuilder.MapId&&!map.SourceBinding.IsConfigured,"Derivative must own a standalone physical map identity.");
            Check(map.TryValidateMetadata(out string metadataError),"Metadata: "+metadataError);Check(map.TryValidateLocalContentReferences(out string referenceError),"Local content: "+referenceError);
            Check(!string.IsNullOrEmpty(entityGuid)&&!string.IsNullOrEmpty(bindingGuid)&&entityGuid!=bindingGuid,"Separate entity/binding GUIDs required.");
            Check(map.NavigationMetadata.AuthoredSubSceneGuid==entityGuid,"Definition entity-scene GUID mismatch.");
            Check(map.SourceSceneReference.AssetGUID==bindingGuid,"Definition must reference its own binding scene.");
            Check(map.GridMetadata.AssetGuid==AssetDatabase.AssetPathToGUID(CH04M05ArmorBreakConfigBuilder.GridPath),"Definition grid GUID mismatch.");
            Check(map.MapSurfaceDataReference.AssetGUID==AssetDatabase.AssetPathToGUID(CH04M05ArmorBreakConfigBuilder.SurfacePath),"Definition surface GUID mismatch.");
            Check(map.MinimapRasterReference.AssetGUID==AssetDatabase.AssetPathToGUID(CH04M05ArmorBreakConfigBuilder.Folder+"/Minimap.png"),"Definition minimap GUID mismatch.");
            var dimensions=new Vector2Int(CH04M05ArmorBreakConfigBuilder.Width,CH04M05ArmorBreakConfigBuilder.Height);var origin=new Vector3(CH04M05ArmorBreakConfigBuilder.CropX,0,CH04M05ArmorBreakConfigBuilder.CropZ);
            Check(grid.Width==dimensions.x&&grid.Height==dimensions.y&&grid.Origin==origin&&Mathf.Approximately(grid.CellSize,1),"Saved grid dimensions/origin/cell size mismatch.");
            Check(map.GridMetadata.Dimensions==dimensions&&map.GridMetadata.Origin==origin,"Definition grid metadata mismatch.");
            Check(surface.Dimensions==dimensions&&surface.GridOrigin==origin&&Mathf.Approximately(surface.CellSize,1),"Saved surface dimensions/origin mismatch.");
            Check(map.GridMetadata.ContentHash==FileHash(CH04M05ArmorBreakConfigBuilder.GridPath),"Saved grid SHA does not match definition.");
            Check(map.SurfaceMetadata.ContentHash==FileHash(CH04M05ArmorBreakConfigBuilder.SurfacePath),"Saved surface SHA does not match definition.");
            Check(map.Bounds.WorldMin.x==origin.x&&map.Bounds.WorldMin.z==origin.z&&map.Bounds.WorldMax.x==origin.x+dimensions.x&&map.Bounds.WorldMax.z==origin.z+dimensions.y,"World bounds must match the independent crop.");
            foreach(var pair in ExpectedSourceHashes)Check(File.Exists(pair.Key)&&FileHash(pair.Key)==pair.Value,"Source SHA changed: "+pair.Key);
            var previous=SceneManager.GetActiveScene();Scene entities=default,binding=default;int identityCount=0,ownerCount=0,renderCount=0,rootCount=0;
            try
            {
                entities=EditorSceneManager.OpenScene(CH04M05ArmorBreakConfigBuilder.EntityScenePath,OpenSceneMode.Additive);
                var identities=All<DenseCityPresentationIdentityAuthoring>(entities);var owners=All<OperationMapBuildingAuthoring>(entities);var roots=All<OperationMapEntityPresentationRootAuthoring>(entities);
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
                var authoredGrids=All<GridAuthoring>(entities);var authoredSurfaces=All<MapSurfaceAuthoring>(entities);
                Check(authoredGrids.Length==1&&authoredSurfaces.Length==1,"Entity scene must own exactly one grid and one surface authoring.");
                if(authoredSurfaces.Length==1)Check(authoredSurfaces[0].BakedSurfaceData==surface&&authoredSurfaces[0].GridConfig==grid,"Entity surface must reference own assets.");
                if(authoredGrids.Length==1)
                {
                    Check(authoredGrids[0].Width==dimensions.x&&authoredGrids[0].Height==dimensions.y,"Entity grid dimensions mismatch.");
                    long localId=checked((long)GlobalObjectId.GetGlobalObjectIdSlow(authoredGrids[0]).targetObjectId);Check(localId==map.NavigationMetadata.GridAuthoringLocalId,"Definition grid authoring local ID mismatch.");
                    var config=new SerializedObject(authoredGrids[0]).FindProperty("config").objectReferenceValue;Check(config==grid,"Entity grid must reference own config asset.");
                }
                binding=EditorSceneManager.OpenScene(CH04M05ArmorBreakConfigBuilder.BindingPath,OpenSceneMode.Additive);var views=All<OperationMapSceneView>(binding);Check(views.Length==1,"Binding must own one operation-map view.");
                if(views.Length==1)
                {
                    var view=views[0];Check(view.TryValidate(out string error),"Binding view: "+error);Check(view.OperationMapId==map.OperationMapId&&view.Definition==map,"Binding definition identity mismatch.");
                    Check(view.PresentationSourceSceneGuid==entityGuid&&view.PresentationSourceScenePath==CH04M05ArmorBreakConfigBuilder.EntityScenePath,"Binding presentation delivery mismatch.");
                    Check(view.GridAuthoringConfig==grid&&view.MapSurfaceAuthoring!=null&&view.MapSurfaceAuthoring.BakedSurfaceData==surface&&view.MapSurfaceAuthoring.GridConfig==grid,"Binding must reference own grid/surface assets.");
                    Check(view.MapSubScene!=null&&!view.MapSubScene.enabled&&!view.MapSubScene.AutoLoadScene&&view.MapSubScene.SceneAsset==null,"Binding SubScene must remain disabled, unbound and not autoloaded.");
                }
            }
            finally
            {
                if(binding.IsValid()&&binding.isLoaded)EditorSceneManager.CloseScene(binding,true);if(entities.IsValid()&&entities.isLoaded)EditorSceneManager.CloseScene(entities,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            }
            if(errors.Count>0){foreach(string error in errors)Debug.LogError("[ArmorBreakMap] check=Failed "+error);throw new InvalidOperationException("Armor Break saved map failed "+errors.Count+" checks.");}
            CH04M05ArmorBreakConfigBuilder.ValidateRoutes();
            Debug.Log("[ArmorBreakMap] result=Passed identities="+identityCount+" owners="+ownerCount+" renderOnly="+renderCount+" roots="+rootCount+" grid=1280x520 sourceHashes=6 sourcePreserved=true binding=OwnExplicitLoader rebasedFootprints=Passed");
        }
        public static void RunWrapper(){try{Run();MissionEditorValidationExit.Complete(true);}catch(Exception error){Debug.LogException(error);MissionEditorValidationExit.Complete(false);}}
        private static T[] All<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        private static T Load<T>(string path)where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException("Required asset missing: "+path);
        private static string FileHash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-",string.Empty).ToLowerInvariant();}
    }
}
