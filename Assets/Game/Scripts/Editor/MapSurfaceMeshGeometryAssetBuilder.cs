using System.Collections.Generic;
using System.IO;
using Game.Authoring;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal static class MapSurfaceMeshGeometryAssetBuilder
    {
        private const string Folder="Assets/Game/Configs/MapSurfaces/GroundGeometry";
        internal static void Persist(MapSurfaceSceneOverlayAuthoringData[] overlays)
        {
            Directory.CreateDirectory(Folder);if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.ImportAsset(Folder,ImportAssetOptions.ForceSynchronousImport);
            var persisted=new Dictionary<MapSurfaceMeshGeometryAsset,MapSurfaceMeshGeometryAsset>();
            for(int i=0;i<overlays.Length;i++)
            {
                var source=overlays[i].Geometry;
                if(source==null && !string.IsNullOrEmpty(overlays[i].GeometryAssetPath))source=AssetDatabase.LoadAssetAtPath<MapSurfaceMeshGeometryAsset>(overlays[i].GeometryAssetPath);
                if(source==null)
                {
                    if((overlays[i].Flags & Game.Components.MapSurfaceFlags.ExactMesh)!=0)throw new System.InvalidOperationException("Baked road geometry was lost before scene publication.");
                    continue;
                }
                string existingPath=AssetDatabase.GetAssetPath(source);
                if(!string.IsNullOrEmpty(existingPath)){overlays[i].Geometry=source;overlays[i].GeometryAssetPath=existingPath;continue;}
                if(!persisted.TryGetValue(source,out var asset))
                {
                    if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source.Source,out string guid,out long localId))
                        throw new System.InvalidOperationException("Road contact geometry requires a persistent source mesh.");
                    string path=Folder+"/Ground_"+guid+"_"+localId+"_"+source.SubMeshIndex+".asset";
                    asset=AssetDatabase.LoadAssetAtPath<MapSurfaceMeshGeometryAsset>(path);
                    if(asset==null){asset=Object.Instantiate(source);asset.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(asset,path);}
                    else{EditorUtility.CopySerialized(source,asset);asset.hideFlags=HideFlags.None;EditorUtility.SetDirty(asset);}
                    persisted.Add(source,asset);
                }
                overlays[i].Geometry=asset;overlays[i].GeometryAssetPath=AssetDatabase.GetAssetPath(asset);
            }
        }
    }
}
