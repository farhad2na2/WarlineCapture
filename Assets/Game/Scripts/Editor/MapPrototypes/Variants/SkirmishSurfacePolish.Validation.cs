using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class SkirmishSurfacePolish
    {
        public static void ValidateAssets()
        {
            Check();
            var shader=Shader.Find("Game/Environment/GroundMacroVariation");
            if(shader==null || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Ground shader compilation failed.");
            var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath);
            if(mask==null || mask.width!=2048 || mask.height!=1024 || mask.wrapMode!=TextureWrapMode.Clamp)throw new InvalidOperationException("District mask contract failed.");
            int concrete=0,stone=0,tile=0;
            foreach(var p in mask.GetPixels32()){if(p.r>127)concrete++;if(p.g>127)stone++;if(p.b>127)tile++;}
            if(concrete<500 || stone<500 || tile<500)throw new InvalidOperationException("Missing district coverage.");
            int vertices=0,triangles=0;
            foreach(var name in new[]{"FlatPaving","KerbPaving","Lanes","Cracks","SandSpill"})
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/DenseCity_"+name+".asset");
                if(mesh==null || mesh.vertexCount==0 || mesh.subMeshCount!=1)throw new InvalidOperationException("Missing surface mesh "+name);
                var points=mesh.vertices;
                if(points.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)))throw new InvalidOperationException("Nonfinite mesh "+name);
                if(mesh.triangles.Any(i=>i<0||i>=points.Length))throw new InvalidOperationException("Invalid indices "+name);
                vertices+=points.Length;triangles+=mesh.triangles.Length/3;
            }
            if(vertices>400000 || triangles>200000)throw new InvalidOperationException("Road surface geometry exceeds the mobile authoring budget.");
            foreach(var name in new[]{"OutskirtsSand","Warm_PolygonMilitary_GroundVariation","Warm_GroundVariation_PolygonMilitary_Mat_03_A","Warm_GroundVariation_PolygonMilitary_Mat_04_A"})
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+name+".mat");
                if(m==null || m.GetTexture("_DistrictSurfaceMap")!=mask || m.GetFloat("_DistrictSurfaceStrength")!=1)throw new InvalidOperationException("Ground material not bound: "+name);
            }
            var build=JsonUtility.FromJson<BuildReport>(File.ReadAllText(Report+"/surface-build.json"));
            if(build.result!="Passed" || Hash(Source)!=build.sourceSceneSha256Before)throw new InvalidOperationException("Authored scene changed.");
            string result=$"[SkirmishSurfaceValidation] result=Passed shader=Compiled mask=2048x1024 concretePixels={concrete} stonePixels={stone} tilePixels={tile} vertices={vertices} triangles={triangles} sourceScene=Unchanged nativeVisualReview=6Views fullMission=Pending device=Pending";
            File.WriteAllText(Report+"/asset-validation.txt",result+Environment.NewLine);Debug.Log(result);
        }
    }
}
