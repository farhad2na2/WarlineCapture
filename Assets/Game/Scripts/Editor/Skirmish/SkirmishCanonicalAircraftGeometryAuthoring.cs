using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Game.Editor
{
    public static class SkirmishCanonicalAircraftGeometryAuthoring
    {
        public const string Output="Assets/Game/Resources/SkirmishCanonicalAircraftGeometry_V1.asset";
        const string Catalog="Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishRoleCatalog_V1.asset";
        const string Registry="Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset";
        public static void CreateAdditionalAircraftMetadata()
        {
            SkirmishAircraftRotorGeometryAuthoring.CreateMeasuredAdditionalAircraftMetadata();
        }
        public static void VerifyInstalled()
        {
            var installed=AssetDatabase.LoadAssetAtPath<SkirmishCanonicalAircraftGeometryConfig>(Output);
            if(installed==null||EditorUtility.IsDirty(installed))throw new InvalidOperationException("Clean additional aircraft metadata required");
            Validate(installed);
            Debug.Log("[CanonicalAircraftGeometry] result=Passed roles=6 transportUnchanged=1 nativeCameraAcceptance=Pending");
        }
        public static SkirmishCanonicalAircraftGeometryConfig Measure()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(Catalog);
            var registry=AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(Registry);
            if(catalog==null||registry==null||EditorUtility.IsDirty(catalog)||EditorUtility.IsDirty(registry))throw new InvalidOperationException("Clean actual canonical catalog/registry required");
            var cache=ScriptableObject.CreateInstance<SkirmishCanonicalAircraftGeometryConfig>();
            try
            {
                var entries=new List<SkirmishCanonicalAircraftGeometryConfig.Entry>();
                foreach(var binding in catalog.Roles.Where(r=>SkirmishCanonicalAircraftGeometryConfig.Supported(r.Kind)).OrderBy(r=>(byte)r.Kind))
                {
                    if(binding.RuntimePrefabKey!=SkirmishCanonicalAircraftGeometryConfig.ExpectedKey(binding.Kind))throw new InvalidOperationException("Canonical role key changed");
                    var prefabs=registry.UnitSpawnPrefabs.Where(p=>p!=null&&p.name==binding.RuntimePrefabKey).ToArray();
                    if(prefabs.Length!=1||EditorUtility.IsDirty(prefabs[0]))throw new InvalidOperationException("Unique clean canonical registry prefab required");
                    var prefab=prefabs[0];var authoring=prefab.GetComponent<UnitGridAuthoring>();
                    if(authoring==null)throw new InvalidOperationException("Actual UnitGridAuthoring required");
                    var serialized=new SerializedObject(authoring);
                    if(AssetDatabase.GetAssetPath(serialized.FindProperty("config").objectReferenceValue)!=binding.SourceConfigPath)throw new InvalidOperationException("Exact canonical source config mismatch");
                    var model=serialized.FindProperty("modelRoot").objectReferenceValue as Transform;
                    if(model==null)model=prefab.transform.Find("Model");
                    if(model==null||!model.IsChildOf(prefab.transform))throw new InvalidOperationException("Actual owned Detail root ancestry required");
                    var entry=new SkirmishCanonicalAircraftGeometryConfig.Entry{Role=binding.Kind,PrefabKey=binding.RuntimePrefabKey};
                    var sources=new List<SkirmishCanonicalAircraftGeometryConfig.Source>();var draws=new List<SkirmishRigidAircraftGeometryConfig.Draw>();var seen=new HashSet<(Mesh,int)>();
                    Collect(prefab,model,1,sources,draws,seen);
                    if(authoring.MidLodPrefab!=null)Collect(authoring.MidLodPrefab,authoring.MidLodPrefab.transform,2,sources,draws,seen);
                    if(authoring.LowLodPrefab!=null)Collect(authoring.LowLodPrefab,authoring.LowLodPrefab.transform,3,sources,draws,seen);
                    entry.Sources=sources.ToArray();entry.Draws=draws.ToArray();if(!entry.Prepare())throw new InvalidOperationException("Measured canonical aircraft entry invalid");entries.Add(entry);
                }
                cache.Roles=entries.ToArray();
                if(entries.Count!=6||!cache.TryRole(SkirmishRoleKind.Fighter,"Unit_Veh_Jet_02",1,out _))throw new InvalidOperationException("Exact six additional role entries required");
                return cache;
            }
            catch{UnityEngine.Object.DestroyImmediate(cache);throw;}
        }
        static void Collect(GameObject prefab,Transform root,byte visual,List<SkirmishCanonicalAircraftGeometryConfig.Source> sources,List<SkirmishRigidAircraftGeometryConfig.Draw> draws,HashSet<(Mesh,int)> seen)
        {
            if(EditorUtility.IsDirty(prefab)||root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length!=0||root.GetComponentsInChildren<Animator>(true).Length!=0||root.GetComponentsInChildren<Animation>(true).Length!=0)throw new InvalidOperationException("Only exact clean rigid imported sources supported");
            string path=AssetDatabase.GetAssetPath(prefab);sources.Add(new SkirmishCanonicalAircraftGeometryConfig.Source{VisualKind=visual,Prefab=prefab,Path=path,DependencyHash=AssetDatabase.GetAssetDependencyHash(path).ToString()});
            var renderers=root.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new InvalidOperationException("Actual source draws missing");
            foreach(var renderer in renderers)
            {
                if(!(renderer is MeshRenderer)||renderer.sharedMaterials.Length==0)throw new InvalidOperationException("Rigid source renderer/material required");
                foreach(var material in renderer.sharedMaterials)if(material==null||EditorUtility.IsDirty(material)||material.shader==null||AssetDatabase.GetAssetPath(material.shader)!="Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader")throw new InvalidOperationException("Unreviewed deforming shader/material");
                var filter=renderer.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;
                if(mesh==null||EditorUtility.IsDirty(mesh)||mesh.blendShapeCount!=0)throw new InvalidOperationException("Clean rigid source mesh required");
                for(int sub=0;sub<mesh.subMeshCount;sub++)if(seen.Add((mesh,sub)))draws.Add(SkirmishRigidAircraftGeometryAuthoring.MeasureDraw(mesh,sub));
            }
        }
        public static void Validate(SkirmishCanonicalAircraftGeometryConfig installed)
        {
            var measured=Measure();
            try
            {
                if(installed.SchemaVersion!=1||installed.Roles==null||installed.Roles.Length!=measured.Roles.Length)throw new InvalidOperationException("Additional aircraft metadata schema drift");
                for(int i=0;i<measured.Roles.Length;i++)
                {
                    var expected=measured.Roles[i];var actual=installed.Roles[i];
                    if(actual==null||actual.Role!=expected.Role||actual.PrefabKey!=expected.PrefabKey||!actual.Prepare()||actual.Sources.Length!=expected.Sources.Length||actual.Draws.Length!=expected.Draws.Length)throw new InvalidOperationException("Canonical role/source metadata drift");
                    for(int j=0;j<expected.Sources.Length;j++)
                    {var a=actual.Sources[j];var b=expected.Sources[j];if(a.VisualKind!=b.VisualKind||a.Prefab!=b.Prefab||a.Path!=b.Path||a.DependencyHash!=b.DependencyHash)throw new InvalidOperationException("Actual source dependency changed");}
                    foreach(var b in expected.Draws)if(!actual.TryRead(b.Mesh,b.SubMesh,out var a)||a.GeometrySha256!=b.GeometrySha256||a.VertexCount!=b.VertexCount||a.DrawnIndexCount!=b.DrawnIndexCount||a.LocalBounds.center!=b.LocalBounds.center||a.LocalBounds.size!=b.LocalBounds.size)throw new InvalidOperationException("Actual indexed draw geometry changed");
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(measured);}
        }
        [InitializeOnLoadMethod] static void Register()
        {EditorApplication.playModeStateChanged-=OnPlay;EditorApplication.playModeStateChanged+=OnPlay;}
        static void OnPlay(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingEditMode)return;
            var installed=AssetDatabase.LoadAssetAtPath<SkirmishCanonicalAircraftGeometryConfig>(Output);if(installed==null)return;
            try{if(EditorUtility.IsDirty(installed))throw new InvalidOperationException("Dirty canonical aircraft metadata");Validate(installed);}
            catch(Exception error){EditorApplication.isPlaying=false;Debug.LogError("[CanonicalAircraftGeometry] result=Failed stage=EditorPlayEntry reason="+error.Message);}
        }
    }
    public sealed class SkirmishCanonicalAircraftGeometryBuildGuard:IPreprocessBuildWithReport
    {
        public int callbackOrder=>1;
        public void OnPreprocessBuild(BuildReport report)
        {if(AssetDatabase.LoadMainAssetAtPath(SkirmishCanonicalAircraftGeometryAuthoring.Output)!=null)SkirmishCanonicalAircraftGeometryAuthoring.VerifyInstalled();}
    }
}
