using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Configs;
using Game.Rendering.Contracts;
using Game.Skirmish.Contracts;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishAircraftRotorGeometryAuthoring
    {
        public const string Output="Assets/Game/Resources/SkirmishAircraftRotorGeometry_V1.asset";
        const string Catalog="Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishRoleCatalog_V1.asset";
        const string Registry="Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset";
        [Serializable] sealed class NativeReport {public string scope;public int expectedRoles,sourceRoleCount,bakedRowCount;public bool bakedRegistryAvailable;public NativeSource[] sources;}
        [Serializable] sealed class NativeSource {public string role,runtimePrefabKey,prefab,dependencyHash;public NativeObject resolvedRoot;}
        [Serializable] sealed class NativeObject {public bool exists;public string guid;public long localFileId;}
        public static void CreateMeasuredAdditionalAircraftMetadata()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                throw new InvalidOperationException("Idle checked preparation required");
            if(AssetDatabase.LoadMainAssetAtPath(Output)!=null||File.Exists(Output)||
                AssetDatabase.LoadMainAssetAtPath(SkirmishCanonicalAircraftGeometryAuthoring.Output)!=null)
                throw new InvalidOperationException("Preserve installed metadata; no automatic overwrite");
            string reportPath=Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_ROOT_PROVENANCE_REPORT");
            if(string.IsNullOrWhiteSpace(reportPath)||!File.Exists(reportPath))throw new InvalidOperationException("Actual seven-role native source report required");
            byte[] raw=File.ReadAllBytes(reportPath);var report=JsonUtility.FromJson<NativeReport>(System.Text.Encoding.UTF8.GetString(raw).TrimStart('\uFEFF'));
            if(report==null||report.scope!="ReadOnlyAllSevenCanonicalAircraftRootBladeDrawBindings"||report.expectedRoles!=7||
                report.sourceRoleCount!=7||report.bakedRowCount!=7||!report.bakedRegistryAvailable||report.sources==null||report.sources.Length!=7)
                throw new InvalidOperationException("Complete actual seven-role native registry/source report required");
            string provenance;using(var sha=SHA256.Create())provenance=BitConverter.ToString(sha.ComputeHash(raw)).Replace("-","").ToLowerInvariant();
            if(provenance!=Environment.GetEnvironmentVariable("WARLINE_AIRCRAFT_ROOT_PROVENANCE_SHA256"))
                throw new InvalidOperationException("Actual checked native report SHA256 must be pinned explicitly");
            var catalog=AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(Catalog);
            var registry=AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(Registry);
            if(catalog==null||registry==null||EditorUtility.IsDirty(catalog)||EditorUtility.IsDirty(registry))throw new InvalidOperationException("Clean actual catalog/registry required");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var binding in catalog.Roles.Where(r=>SkirmishRoleIds.Category(r.Kind)==SkirmishPopulationCategory.Air))
            {
                var rows=report.sources.Where(s=>s.runtimePrefabKey==binding.RuntimePrefabKey).ToArray();
                var prefabs=registry.UnitSpawnPrefabs.Where(p=>p!=null&&p.name==binding.RuntimePrefabKey).ToArray();
                if(rows.Length!=1||prefabs.Length!=1||!seen.Add(binding.RuntimePrefabKey))throw new InvalidOperationException("Exact unique seven native role source keys required");
                var row=rows[0];var prefab=prefabs[0];var root=ResolveRoot(prefab);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(root,out string guid,out long id);
                if(row.prefab!=AssetDatabase.GetAssetPath(prefab)||row.dependencyHash!=AssetDatabase.GetAssetDependencyHash(row.prefab).ToString()||
                    row.resolvedRoot==null||!row.resolvedRoot.exists||row.resolvedRoot.guid!=guid||row.resolvedRoot.localFileId!=id)
                    throw new InvalidOperationException("Actual native body-root/source dependency changed: "+binding.RuntimePrefabKey);
            }
            if(seen.Count!=7)throw new InvalidOperationException("Exact seven canonical source keys required");
            var rigid=SkirmishCanonicalAircraftGeometryAuthoring.Measure();var rotor=Measure(provenance);
            // Both fully measured objects exist before either is saved. These are
            // new logical metadata assets; no mesh/material/prefab import edits.
            AssetDatabase.CreateAsset(rigid,SkirmishCanonicalAircraftGeometryAuthoring.Output);
            AssetDatabase.CreateAsset(rotor,Output);AssetDatabase.SaveAssetIfDirty(rigid);AssetDatabase.SaveAssetIfDirty(rotor);
            ValidateInstalled();
            Debug.Log("[MeasuredCanonicalAircraftGeometry] result=Passed additionalRoles=6 rotorRoles=4 transportUnchanged=1 nativeActorAndCameraAcceptance=Pending");
        }
        public static SkirmishAircraftRotorGeometryConfig Measure(string provenance)
        {
            var catalog=AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(Catalog);
            var registry=AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(Registry);
            if(catalog==null||registry==null||EditorUtility.IsDirty(catalog)||EditorUtility.IsDirty(registry))throw new InvalidOperationException("Clean actual catalog and registry required");
            var cache=ScriptableObject.CreateInstance<SkirmishAircraftRotorGeometryConfig>();
            try
            {
                cache.NativeTemplateProvenanceSha256=provenance;var rows=new List<SkirmishAircraftRotorGeometryConfig.Binding>();
                foreach(var role in catalog.Roles.Where(r=>SkirmishAircraftRotorGeometryConfig.HasRotors(r.Kind)).OrderBy(r=>(byte)r.Kind))
                {
                    var prefabs=registry.UnitSpawnPrefabs.Where(p=>p!=null&&p.name==role.RuntimePrefabKey).ToArray();
                    if(prefabs.Length!=1)throw new InvalidOperationException("Unique actual rotor source required");
                    var prefab=prefabs[0];var owner=prefab.GetComponent<UnitGridAuthoring>();var root=ResolveRoot(prefab);
                    Collect(role.Kind,prefab,root,1,rows);
                    if(owner.MidLodPrefab!=null)Collect(role.Kind,owner.MidLodPrefab,owner.MidLodPrefab.transform,2,rows);
                    if(owner.LowLodPrefab!=null)Collect(role.Kind,owner.LowLodPrefab,owner.LowLodPrefab.transform,3,rows);
                }
                cache.Bindings=rows.ToArray();if(!cache.Prepare())throw new InvalidOperationException("Measured indexed rotor metadata invalid");return cache;
            }
            catch{UnityEngine.Object.DestroyImmediate(cache);throw;}
        }
        static Transform ResolveRoot(GameObject prefab)
        {
            if(prefab==null||EditorUtility.IsDirty(prefab))throw new InvalidOperationException("Clean actual aircraft prefab required");
            var owner=prefab.GetComponent<UnitGridAuthoring>();if(owner==null)throw new InvalidOperationException("Actual aircraft authoring required");
            var serialized=new SerializedObject(owner);var root=serialized.FindProperty("modelRoot").objectReferenceValue as Transform;
            if(root==null)root=prefab.transform.Find("Model");
            if(root==null||!root.IsChildOf(prefab.transform))throw new InvalidOperationException("Owned explicit/resolved aircraft body root required");return root;
        }
        static bool Axis(Transform t,out byte axis)
        {
            axis=0;string n=t.name;if(!n.Contains("Blade",StringComparison.Ordinal))return false;
            if(n.EndsWith("_X",StringComparison.Ordinal))return true;
            if(n.EndsWith("_Y",StringComparison.Ordinal)){axis=1;return true;}
            if(n.EndsWith("_Z",StringComparison.Ordinal)){axis=2;return true;}return false;
        }
        static string PathOf(Transform node,Transform root)
        {
            var indices=new List<int>();while(node!=root){if(node==null)throw new InvalidOperationException("Source path escaped owned root");indices.Add(node.GetSiblingIndex());node=node.parent;}
            indices.Reverse();return "root/"+string.Join("/",indices);
        }
        static void Collect(SkirmishRoleKind role,GameObject prefab,Transform root,byte visual,List<SkirmishAircraftRotorGeometryConfig.Binding> rows)
        {
            if(EditorUtility.IsDirty(prefab)||root.GetComponentsInChildren<Animator>(true).Length!=0||root.GetComponentsInChildren<Animation>(true).Length!=0||
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length!=0)throw new InvalidOperationException("No unreviewed animated source path");
            int expected=role==SkirmishRoleKind.Drone?1:2;var blades=root.GetComponentsInChildren<Transform>(true).Where(t=>Axis(t,out _)).ToArray();
            if(blades.Length!=expected)throw new InvalidOperationException("Exact source blade count differs from actual native role: "+role+" visual="+visual);
            foreach(var blade in blades)
            {
                Axis(blade,out byte axis);
                for(var parent=blade.parent;parent!=null&&parent!=root.parent;parent=parent.parent)
                    if(Axis(parent,out _))throw new InvalidOperationException("Nested source spinner unsupported");
                if(blade.localScale.x<=0||Mathf.Abs(blade.localScale.x-blade.localScale.y)>1e-6f||Mathf.Abs(blade.localScale.x-blade.localScale.z)>1e-6f)
                    throw new InvalidOperationException("Rotating LocalTransform must be uniform positive scale");
                foreach(var t in blade.GetComponentsInChildren<Transform>(true))
                    if(t.GetComponents<MonoBehaviour>().Length!=0)throw new InvalidOperationException("Unreviewed moving descendant authoring under blade");
                var renderers=blade.GetComponentsInChildren<MeshRenderer>(true);
                if(renderers.Length==0)throw new InvalidOperationException("Actual blade indexed draw missing");
                foreach(var renderer in renderers)
                {
                    var filter=renderer.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;
                    if(mesh==null||mesh.blendShapeCount!=0||EditorUtility.IsDirty(mesh)||renderer.sharedMaterials.Length!=mesh.subMeshCount)
                        throw new InvalidOperationException("Exact rigid blade draw/material mapping required");
                    foreach(var material in renderer.sharedMaterials)
                        if(material==null||material.shader==null||AssetDatabase.GetAssetPath(material.shader)!="Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader")
                            throw new InvalidOperationException("Unknown blade deformation shader");
                    var matrix=blade.worldToLocalMatrix*renderer.localToWorldMatrix;
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        var draw=SkirmishRigidAircraftGeometryAuthoring.MeasureDraw(mesh,sub);
                        using var dataArray=MeshUtility.AcquireReadOnlyMeshData(mesh);var data=dataArray[0];
                        using var vertices=new NativeArray<Vector3>(data.vertexCount,Allocator.Temp);
                        using var indices=new NativeArray<int>(data.GetSubMesh(sub).indexCount,Allocator.Temp);
                        data.GetVertices(vertices);data.GetIndices(indices,sub,true);
                        var indexed=new Vector3[indices.Length];for(int i=0;i<indices.Length;i++){if(indices[i]<0||indices[i]>=vertices.Length)throw new InvalidOperationException("Indexed blade vertex out of range");indexed[i]=vertices[indices[i]];}
                        if(!SkirmishRigidRotorSweep.TryMeasure(indexed,matrix,axis,out var extent))throw new InvalidOperationException("Indexed blade sweep failed");
                        string path=AssetDatabase.GetAssetPath(prefab);
                        rows.Add(new SkirmishAircraftRotorGeometryConfig.Binding{Role=role,VisualKind=visual,Axis=axis,Mesh=mesh,SubMesh=sub,
                            GeometrySha256=draw.GeometrySha256,SourcePath=path,DependencyHash=AssetDatabase.GetAssetDependencyHash(path).ToString(),
                            RendererPath=PathOf(renderer.transform,root),BladePath=PathOf(blade,root),MeshToBlade=matrix,
                            BladeToVisualRoot=root.worldToLocalMatrix*blade.localToWorldMatrix,
                            MinimumAxial=extent.MinimumAxial,MaximumAxial=extent.MaximumAxial,MaximumRadius=extent.MaximumRadius});
                    }
                }
            }
        }
        public static void ValidateInstalled()
        {
            var installed=AssetDatabase.LoadAssetAtPath<SkirmishAircraftRotorGeometryConfig>(Output);
            if(installed==null||EditorUtility.IsDirty(installed)||!installed.Prepare())throw new InvalidOperationException("Valid clean rotor metadata required");
            var measured=Measure(installed.NativeTemplateProvenanceSha256);
            try
            {
                if(measured.Bindings.Length!=installed.Bindings.Length)throw new InvalidOperationException("Rotor draw coverage changed");
                for(int i=0;i<measured.Bindings.Length;i++)if(!new SkirmishAircraftRotorGeometryConfig.Snapshot(measured.Bindings[i]).Matches(installed.Bindings[i]))
                    throw new InvalidOperationException("Actual source/dependency/indexed rotor geometry changed");
            }
            finally{UnityEngine.Object.DestroyImmediate(measured);}
            SkirmishCanonicalAircraftGeometryAuthoring.VerifyInstalled();
        }
        [InitializeOnLoadMethod] static void Register(){EditorApplication.playModeStateChanged-=OnPlay;EditorApplication.playModeStateChanged+=OnPlay;}
        static void OnPlay(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingEditMode||AssetDatabase.LoadMainAssetAtPath(SkirmishCanonicalAircraftGeometryAuthoring.Output)==null)return;
            try{ValidateInstalled();}catch(Exception error){EditorApplication.isPlaying=false;Debug.LogError("[MeasuredAircraftRotorGeometry] result=Failed stage=EditorPlayEntry reason="+error.Message);}
        }
    }
    public sealed class SkirmishAircraftRotorGeometryBuildGuard:IPreprocessBuildWithReport
    {public int callbackOrder=>2;public void OnPreprocessBuild(BuildReport report){if(AssetDatabase.LoadMainAssetAtPath(SkirmishCanonicalAircraftGeometryAuthoring.Output)!=null)SkirmishAircraftRotorGeometryAuthoring.ValidateInstalled();}}
}
