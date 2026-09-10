using System;
using System.Collections.Generic;
using Game.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Measures planted feet in the authored GPU animation, preserving natural airborne run frames.</summary>
    public static class UnitGroundContactProfileBuilder
    {
        private readonly struct FootVertex
        {
            internal readonly Vector3 Position;
            internal readonly Vector4 Bones,Weights;
            internal FootVertex(Vector3 position,Vector4 bones,Vector4 weights){Position=position;Bones=bones;Weights=weights;}
        }
        public static void Build()
        {
            var pixels=new Dictionary<Texture2D,Color[]>();int count=0;
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab Unit_Chr_",new[]{"Assets/Game/Prefabs/Characters"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var authoring=root.GetComponent<UnitGridAuthoring>();if(authoring==null)continue;
                    var data=new SerializedObject(authoring);
                    var model=data.FindProperty("modelRoot").objectReferenceValue as Transform ?? root.transform.Find("Model");
                    if(model==null)continue;
                    var index=model.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);if(index==null||index.animator==null)continue;
                    var animator=index.animator.GetComponent<MaterialAnimatorAuthoring>();if(animator==null)continue;
                    var minima=new float[animator.animations.Count];for(int i=1;i<minima.Length;i++)minima[i]=float.PositiveInfinity;
                    foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||filter.sharedMesh==null)continue;
                        var material=renderer.sharedMaterial;if(material==null||!material.HasProperty("_SnivelerMainTextureFirst"))continue;
                        var t0=material.GetTexture("_SnivelerMainTextureFirst") as Texture2D;
                        var t1=material.GetTexture("_SnivelerMainTextureSecond") as Texture2D;
                        var t2=material.GetTexture("_SnivelerMainTextureThird") as Texture2D;
                        if(t0==null||t1==null||t2==null)continue;
                        Color[] row0=Read(pixels,t0),row1=Read(pixels,t1),row2=Read(pixels,t2);
                        var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                        var mesh=filter.sharedMesh;var vertices=mesh.vertices;var bones=new List<Vector4>();var weights=new List<Vector4>();mesh.GetUVs(2,bones);mesh.GetUVs(3,weights);
                        if(bones.Count!=vertices.Length||weights.Count!=vertices.Length)continue;
                        var feet=new List<FootVertex>();for(int v=0;v<vertices.Length;v++)if(matrix.MultiplyPoint3x4(vertices[v]).y<=.35f)feet.Add(new FootVertex(vertices[v],bones[v],weights[v]));
                        for(int a=1;a<animator.animations.Count;a++)
                        {
                            var animation=animator.animations[a];
                            for(int f=0;f<animation.frames;f++)
                            {
                                int start=animation.start+f*animator.bonesCount;
                                foreach(var vertex in feet)
                                {
                                    Vector3 point=Vector3.zero;
                                    for(int b=0;b<4;b++)
                                    {
                                        float weight=vertex.Weights[b];if(weight<=0)continue;
                                        int bone=start+Mathf.Clamp(Mathf.RoundToInt(vertex.Bones[b]),0,animator.bonesCount-1);
                                        var x=row0[bone];var y=row1[bone];var z=row2[bone];var p=vertex.Position;
                                        point+=new Vector3(x.r*p.x+x.g*p.y+x.b*p.z+x.a,y.r*p.x+y.g*p.y+y.b*p.z+y.a,z.r*p.x+z.g*p.y+z.b*p.z+z.a)*weight;
                                    }
                                    minima[a]=Mathf.Min(minima[a],matrix.MultiplyPoint3x4(point).y);
                                }
                            }
                        }
                    }
                    var offsets=data.FindProperty("animationGroundOffsets");offsets.arraySize=minima.Length;
                    for(int i=0;i<minima.Length;i++)
                    {
                        if(float.IsInfinity(minima[i]))throw new InvalidOperationException("Missing foot geometry: "+path+" animation="+i);
                        offsets.GetArrayElementAtIndex(i).floatValue=-minima[i];
                    }
                    data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);count++;
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("[UnitGroundContactProfiles] result=Passed characters="+count);
        }
        private static Color[] Read(Dictionary<Texture2D,Color[]> cache,Texture2D texture)
        {if(!cache.TryGetValue(texture,out var pixels)){pixels=texture.GetPixels();cache.Add(texture,pixels);}return pixels;}
    }
}
