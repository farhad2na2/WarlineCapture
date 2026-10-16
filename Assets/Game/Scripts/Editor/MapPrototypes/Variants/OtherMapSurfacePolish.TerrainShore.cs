using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        // Intersect existing ground triangles with the water plane. No scene, mesh,
        // collision, or navigation data is changed; only the map-owned water material.
        static void ApplyTerrainShore(WaterRoadMap data, MeshRenderer[] visible)
        {
            var waters=visible.Where(NaturalWater).ToArray();
            if(waters.Length==0)return;
            var bounds=waters[0].bounds;
            foreach(var water in waters.Skip(1))bounds.Encapsulate(water.bounds);
            var rect=new Vector4(bounds.min.x-2,bounds.min.z-2,bounds.size.x+4,bounds.size.z+4);
            int width=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(rect.z*3)),256,2048);
            int height=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(rect.w*3)),256,2048);
            var distances=Enumerable.Repeat(16f,width*height).ToArray();
            var segments=new List<(Vector2 a,Vector2 b)>();
            var levels=waters.Select(w=>w.transform.TransformPoint(w.GetComponent<MeshFilter>().sharedMesh.vertices[0]).y).Distinct().ToArray();
            foreach(var ground in visible.Where(r=>r.name.StartsWith("Ground_",StringComparison.Ordinal)&&TouchesXZ(r.bounds,bounds)))
            {
                var mesh=ground.GetComponent<MeshFilter>()?.sharedMesh;
                if(mesh==null)continue;
                var vertices=mesh.vertices.Select(ground.transform.TransformPoint).ToArray();
                var triangles=mesh.triangles;
                foreach(float level in levels)
                for(int t=0;t<triangles.Length;t+=3)
                {
                    var points=new List<Vector2>(3);
                    for(int edge=0;edge<3;edge++)
                    {
                        var a=vertices[triangles[t+edge]];var b=vertices[triangles[t+(edge+1)%3]];
                        if((a.y<level)==(b.y<level)||Mathf.Abs(a.y-b.y)<1e-6f)continue;
                        var p=Vector3.Lerp(a,b,(level-a.y)/(b.y-a.y));points.Add(new Vector2(p.x,p.z));
                    }
                    if(points.Count==2&&(points[1]-points[0]).sqrMagnitude>1e-8f)segments.Add((points[0],points[1]));
                }
            }
            if(segments.Count==0)throw new InvalidOperationException("No terrain/water intersections for "+data.map);
            foreach(var segment in segments)
            {
                int x0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(segment.a.x,segment.b.x)-16-rect.x)/rect.z*width),0,width-1);
                int x1=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(segment.a.x,segment.b.x)+16-rect.x)/rect.z*width),0,width-1);
                int z0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(segment.a.y,segment.b.y)-16-rect.y)/rect.w*height),0,height-1);
                int z1=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(segment.a.y,segment.b.y)+16-rect.y)/rect.w*height),0,height-1);
                var delta=segment.b-segment.a;
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
                {
                    var p=new Vector2(rect.x+(x+.5f)/width*rect.z,rect.y+(z+.5f)/height*rect.w);
                    var nearest=segment.a+delta*Mathf.Clamp01(Vector2.Dot(p-segment.a,delta)/delta.sqrMagnitude);
                    int index=z*width+x;distances[index]=Mathf.Min(distances[index],Vector2.Distance(p,nearest));
                }
            }
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false,true);
            texture.SetPixels(distances.Select(d=>new Color(d/16,d/16,d/16,1)).ToArray());texture.Apply();
            string path=WaterRoadArt+"/Shore/"+data.map+".png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.SaveAndReimport();
            var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(path)??AssetDatabase.LoadAllAssetsAtPath(path).OfType<Texture2D>().FirstOrDefault();
            if(mask==null)throw new InvalidOperationException("Shore mask did not import "+path);
            foreach(var material in data.waterMaterials.Where(m=>MapOwnedMaterial(AssetDatabase.GetAssetPath(m),data.map)))
            {
                material.SetTexture("_ShoreDistance",mask);material.SetVector("_ShoreRect",rect);material.SetFloat("_ShoreStrength",1);
                material.SetFloat("_FoamWidth",.75f);Save(material);
            }
            data.report.shoreSegments=segments.Count;
            Debug.Log($"[TerrainShore] map={data.map} segments={segments.Count} mask={width}x{height} geometry=unchanged");
        }
    }
}
