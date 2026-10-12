using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static void FinishRunwayMaterials(string map,MeshRenderer[] strips,MeshRenderer[] all)
        {
            var extent=strips[0].bounds;foreach(var r in strips.Skip(1))extent.Encapsulate(r.bounds);
            var ground=all.Where(r=>Hierarchy(r.transform).Contains("/Ground/")).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m!=null&&m.HasProperty("_SandWornColor"));
            var earth=ground!=null?ground.GetColor("_SandWornColor"):new Color(.67f,.62f,.53f);
            foreach(var r in strips)
            {
                if(Mathf.Abs(Vector3.Dot(r.transform.right,Vector3.right))<.999f)throw new InvalidOperationException("Runway finish requires an authored east-west strip");
                var materials=r.sharedMaterials;var original=materials[0];
                if(original.shader.name!="Game/Environment/GroundMacroVariation")throw new InvalidOperationException("Unexpected runway asphalt shader");
                string folder=FixArt+"/RunwayMaterials";Directory.CreateDirectory(folder);string path=folder+"/"+map+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(original);AssetDatabase.CreateAsset(material,path);}
                material.SetVector("_RunwayEdgeRect",new Vector4(extent.min.x,extent.min.z,extent.max.x,extent.max.z));material.SetColor("_RunwayEdgeColor",earth);material.SetFloat("_RunwayEdgeStrength",1);
                EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);materials[0]=material;r.sharedMaterials=materials;EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
        }
    }
}
