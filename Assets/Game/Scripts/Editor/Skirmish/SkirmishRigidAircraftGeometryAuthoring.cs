using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishRigidAircraftGeometryAuthoring
    {
        public const string Output="Assets/Game/Resources/SkirmishRigidAircraftGeometry_Transport.asset";
        public static readonly string[] Prefabs={"Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab",
            "Assets/Game/Prefabs/Generated/MidLOD/MidLOD_Unit_Veh_Plane_Transport.prefab",
            "Assets/Game/Prefabs/Generated/MidLOD/LowLOD_Unit_Veh_Plane_Transport.prefab"};
        // Explicit checked preparation only. Never modifies a source prefab,
        // mesh, importer, material, animation texture or culling safety bound.
        public static void CreateTransportMetadata()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new InvalidOperationException("Idle checked preparation required");
            if(AssetDatabase.LoadMainAssetAtPath(Output)!=null||File.Exists(Output))throw new InvalidOperationException("Preserve existing framing cache; verify it or prepare a separately reviewed revision");
            var cache=Measure();
            try{AssetDatabase.CreateAsset(cache,Output);AssetDatabase.SaveAssetIfDirty(cache);VerifyInstalledTransportMetadata();}
            catch{if(!AssetDatabase.Contains(cache))UnityEngine.Object.DestroyImmediate(cache);throw;}
        }
        public static void VerifyInstalledTransportMetadata()
        {
            var installed=AssetDatabase.LoadAssetAtPath<SkirmishRigidAircraftGeometryConfig>(Output);
            if(installed==null||EditorUtility.IsDirty(installed))throw new InvalidOperationException("Clean actual installed Transport framing metadata required");
            ValidateForEditorPlay(installed);
            Debug.Log("[RigidAircraftGeometryCache] result=Passed sources=3 draws="+installed.Draws.Length+" currentDependencyAndVertexHashes=1 pixelAcceptance=0");
        }
        public static void ValidateForEditorPlay(SkirmishRigidAircraftGeometryConfig installed)
        {
            var measured=Measure();
            try
            {
                if(!Equal(installed,measured,out string error))throw new InvalidOperationException("Stale Transport framing metadata: "+error);
            }
            finally{UnityEngine.Object.DestroyImmediate(measured);}
        }
        [InitializeOnLoadMethod] static void InstallPlayEntryGuard()
        {
            EditorApplication.playModeStateChanged-=OnPlayEntry;
            EditorApplication.playModeStateChanged+=OnPlayEntry;
        }
        static void OnPlayEntry(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingEditMode)return;
            var installed=AssetDatabase.LoadAssetAtPath<SkirmishRigidAircraftGeometryConfig>(Output);
            if(installed==null)return; // Missing metadata leaves scoped framing unsupported.
            try
            {
                if(EditorUtility.IsDirty(installed))throw new InvalidOperationException("Dirty installed Transport framing metadata");
                ValidateForEditorPlay(installed);
            }
            catch(Exception error)
            {
                EditorApplication.isPlaying=false;
                Debug.LogError("[RigidAircraftGeometryCache] result=Failed stage=EditorPlayEntry reason="+error.Message);
            }
        }
        public static bool Equal(SkirmishRigidAircraftGeometryConfig actual,SkirmishRigidAircraftGeometryConfig expected,out string error)
        {
            error="InvalidMetadata";if(actual==null||expected==null||!actual.IsValid||!expected.IsValid||actual.Sources.Length!=expected.Sources.Length||actual.Draws.Length!=expected.Draws.Length)return false;
            for(int i=0;i<expected.Sources.Length;i++)
            {var a=actual.Sources[i];var b=expected.Sources[i];if(a.Prefab!=b.Prefab||a.Path!=b.Path||a.DependencyHash!=b.DependencyHash){error="SourceDependencyOrPrefabChanged";return false;}}
            foreach(var b in expected.Draws)
            {
                var a=actual.Draws.SingleOrDefault(item=>item.Mesh==b.Mesh&&item.SubMesh==b.SubMesh);
                if(a==null||a.VertexCount!=b.VertexCount||a.DrawnIndexCount!=b.DrawnIndexCount||a.GeometrySha256!=b.GeometrySha256||
                    a.LocalBounds.center!=b.LocalBounds.center||a.LocalBounds.size!=b.LocalBounds.size){error="ExactDrawGeometryChanged";return false;}
            }
            error=null;return true;
        }
        public static SkirmishRigidAircraftGeometryConfig Measure()
        {
            var cache=ScriptableObject.CreateInstance<SkirmishRigidAircraftGeometryConfig>();
            try
            {
                var sources=new List<SkirmishRigidAircraftGeometryConfig.Source>();var draws=new List<SkirmishRigidAircraftGeometryConfig.Draw>();var seen=new HashSet<(Mesh,int)>();
                foreach(string path in Prefabs)
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if(prefab==null||EditorUtility.IsDirty(prefab)||prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length!=0||prefab.GetComponentsInChildren<Animator>(true).Length!=0)
                        throw new InvalidOperationException("Exact clean rigid Transport prefab required: "+path);
                    var renderers=prefab.GetComponentsInChildren<MeshRenderer>(true);
                    if(renderers.Length!=19)throw new InvalidOperationException("Reviewed Transport19-part hierarchy changed: "+path);
                    sources.Add(new SkirmishRigidAircraftGeometryConfig.Source{Prefab=prefab,Path=path,DependencyHash=AssetDatabase.GetAssetDependencyHash(path).ToString()});
                    foreach(var renderer in renderers)
                    {
                        var filter=renderer.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;
                        if(mesh==null||EditorUtility.IsDirty(mesh)||renderer.sharedMaterials.Length==0)throw new InvalidOperationException("Actual rigid mesh/material missing");
                        foreach(var material in renderer.sharedMaterials)
                            if(material==null||EditorUtility.IsDirty(material)||material.shader==null||
                                AssetDatabase.GetAssetPath(material.shader)!="Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader")
                                throw new InvalidOperationException("Only reviewed actual URP Lit rigid Transport draws supported");
                        // All submeshes can be drawn by the native render range;
                        // runtime still selects only its actual active submeshes.
                        for(int sub=0;sub<mesh.subMeshCount;sub++)if(seen.Add((mesh,sub)))draws.Add(MeasureDraw(mesh,sub));
                    }
                }
                cache.Sources=sources.ToArray();cache.Draws=draws.OrderBy(draw=>AssetDatabase.GetAssetPath(draw.Mesh),StringComparer.Ordinal).ThenBy(draw=>draw.Mesh.name,StringComparer.Ordinal).ThenBy(draw=>draw.SubMesh).ToArray();
                if(!cache.IsValid)throw new InvalidOperationException("Measured Transport metadata invalid");return cache;
            }
            catch{UnityEngine.Object.DestroyImmediate(cache);throw;}
        }
        public static SkirmishRigidAircraftGeometryConfig.Draw MeasureDraw(Mesh mesh,int subMesh)
        {
            if(mesh==null||subMesh<0||subMesh>=mesh.subMeshCount||mesh.GetTopology(subMesh)!=MeshTopology.Triangles)throw new InvalidOperationException("Actual triangle draw required");
            using var dataArray=MeshUtility.AcquireReadOnlyMeshData(mesh);var data=dataArray[0];
            using var vertices=new NativeArray<Vector3>(data.vertexCount,Allocator.Temp);
            using var indices=new NativeArray<int>(data.GetSubMesh(subMesh).indexCount,Allocator.Temp);
            data.GetVertices(vertices);data.GetIndices(indices,subMesh,true);
            if(indices.Length<=0)throw new InvalidOperationException("Empty actual draw");
            bool found=false;Bounds bounds=default;string hash;
            using(var stream=new MemoryStream())
            {
                using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
                {
                    writer.Write(1);writer.Write(data.vertexCount);writer.Write(subMesh);writer.Write(indices.Length);
                    foreach(int index in indices)
                    {
                        if(index<0||index>=vertices.Length)throw new InvalidOperationException("Actual draw vertex index invalid");
                        var p=vertices[index];if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z))throw new InvalidOperationException("Actual draw nonfinite vertex");
                        writer.Write(index);writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);
                        if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
                    }
                }
                using var sha=SHA256.Create();hash=BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-","").ToLowerInvariant();
            }
            return new SkirmishRigidAircraftGeometryConfig.Draw{Mesh=mesh,SubMesh=subMesh,VertexCount=data.vertexCount,DrawnIndexCount=indices.Length,LocalBounds=bounds,GeometrySha256=hash};
        }
    }
    public sealed class SkirmishRigidAircraftGeometryBuildGuard:IPreprocessBuildWithReport
    {
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report)=>SkirmishRigidAircraftGeometryAuthoring.VerifyInstalledTransportMetadata();
    }
}
