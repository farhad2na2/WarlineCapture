using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static void BuildDestinationPads(string map,MeshRenderer[] renderers,OperationMapDefinition definition,List<(Bounds bounds,string kind)> pads)
        {
            if(pads.Count==0)return;
            var materials=new Dictionary<Material,Material>();
            foreach(var r in renderers.Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.shader.name=="Game/Environment/GroundMacroVariation") &&
                (MeshName(r).Contains("Ground")||Hierarchy(r.transform).Contains("/Ground/"))))
            {
                r.sharedMaterials=r.sharedMaterials.Select(original=>
                {
                    if(original==null||original.shader.name!="Game/Environment/GroundMacroVariation")return original;
                    string originalPath=AssetDatabase.GetAssetPath(original);
                    if(originalPath.StartsWith(FixArt+"/DestinationPads/",StringComparison.Ordinal))
                    {
                        string sourceGuid=Path.GetFileNameWithoutExtension(originalPath).Split('_').Last();
                        var sourceMaterial=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(sourceGuid));
                        if(sourceMaterial==null)throw new Exception("Missing original destination ground material");original=sourceMaterial;originalPath=AssetDatabase.GetAssetPath(original);
                    }
                    if(materials.TryGetValue(original,out var cached))return cached;
                    bool owned=originalPath.StartsWith(FixArt+"/DestinationPads/",StringComparison.Ordinal);
                    string key=owned?Path.GetFileNameWithoutExtension(originalPath):map+"_"+AssetDatabase.AssetPathToGUID(originalPath);
                    string folder=FixArt+"/DestinationPads";Directory.CreateDirectory(folder);
                    var rect=original.GetVector("_DistrictSurfaceRect");
                    if(rect.z<1||rect.w<1)rect=new Vector4(definition.Bounds.PlayableMin.x,definition.Bounds.PlayableMin.z,definition.Bounds.PlayableMax.x-definition.Bounds.PlayableMin.x,definition.Bounds.PlayableMax.z-definition.Bounds.PlayableMin.z);
                    var source=original.GetTexture("_DistrictSurfaceMap");int width=source==null?1024:Mathf.Max(512,source.width),height=source==null?1024:Mathf.Max(512,source.height);
                    width=Mathf.Min(2048,width);height=Mathf.Min(2048,height);
                    var texture=new Texture2D(width,height,TextureFormat.RGBA32,false,true);var pixels=new Color[width*height];
                    float oldStrength=original.GetFloat("_DistrictSurfaceStrength"),strength=Mathf.Max(oldStrength,.85f),ratio=oldStrength/strength;
                    if(source!=null&&oldStrength>.001f)
                    {
                        var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
                        try{Graphics.Blit(source,target);RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();pixels=texture.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]*=ratio;}
                        finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
                    }
                    foreach(var pad in pads)
                    {
                        var b=pad.bounds;int channel=pad.kind is "service" or "fuel" or "camp" or "repair" or "airfield"?0:pad.kind=="domestic"?2:1;
                        int x0=Mathf.Clamp(Mathf.FloorToInt((b.min.x-1.5f-rect.x)/rect.z*width),0,width-1),x1=Mathf.Clamp(Mathf.CeilToInt((b.max.x+1.5f-rect.x)/rect.z*width),0,width-1);
                        int z0=Mathf.Clamp(Mathf.FloorToInt((b.min.z-1.5f-rect.y)/rect.w*height),0,height-1),z1=Mathf.Clamp(Mathf.CeilToInt((b.max.z+1.5f-rect.y)/rect.w*height),0,height-1);
                        for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
                        {
                            float wx=rect.x+(x+.5f)/width*rect.z,wz=rect.y+(z+.5f)/height*rect.w;
                            float edge=Mathf.Min(wx-(b.min.x-1.5f),(b.max.x+1.5f)-wx,wz-(b.min.z-1.5f),(b.max.z+1.5f)-wz);
                            float amount=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/.7f));var color=Color.clear;color[channel]=1;
                            pixels[z*width+x]=Color.Lerp(pixels[z*width+x],color,amount);
                        }
                    }
                    texture.SetPixels(pixels);texture.Apply();string maskPath=folder+"/"+key+".png";File.WriteAllBytes(maskPath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(maskPath,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(maskPath);
                    importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                    foreach(string platform in new[]{"Android","iPhone"}){var settings=importer.GetPlatformTextureSettings(platform);settings.name=platform;settings.overridden=true;settings.maxTextureSize=2048;settings.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(settings);}importer.SaveAndReimport();
                    string materialPath=folder+"/"+key+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(material==null){material=new Material(original);AssetDatabase.CreateAsset(material,materialPath);}
                    material.CopyPropertiesFromMaterial(original);
                    material.SetTexture("_DistrictSurfaceMap",AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));material.SetVector("_DistrictSurfaceRect",rect);material.SetFloat("_DistrictSurfaceStrength",strength);material.SetVector("_DistrictSurfaceConstant",original.GetVector("_DistrictSurfaceConstant")*ratio);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
                    materials[original]=material;return material;
                }).ToArray();EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            Debug.Log("[CampaignDestinationPads] map="+map+" pads="+pads.Count+" materials="+materials.Count+" topology=unchanged");
        }
    }
}
