using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Game.Configs;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        [Serializable] private sealed class ConnectedGroundReceipt
        {
            public string map,result,sourceHashBefore,sourceHashAfter,mask;
            public int connectedTexels,protectedRoadTexels,groundMaterials;
            public float squareMetresAdded;
        }
        // World-space mask edits remain presentation-only. Closing gaps of at most six
        // metres joins neighboring frontages without inventing a new road or route.
        private static byte[] Extremum(byte[] input,int width,int height,int radius,bool horizontal,bool maximum)
        {
            var output=new byte[input.Length];int lines=horizontal?height:width,length=horizontal?width:height;
            for(int line=0;line<lines;line++)for(int p=0;p<length;p++)
            {
                byte value=maximum?(byte)0:(byte)255;
                for(int d=-radius;d<=radius;d++)
                {
                    int q=Mathf.Clamp(p+d,0,length-1),i=horizontal?line*width+q:q*width+line;
                    value=maximum?Math.Max(value,input[i]):Math.Min(value,input[i]);
                }
                output[horizontal?line*width+p:p*width+line]=value;
            }
            return output;
        }
        private static byte[] CloseField(byte[] field,int width,int height,int rx,int rz)
        {
            field=Extremum(field,width,height,rx,true,true);field=Extremum(field,width,height,rz,false,true);
            field=Extremum(field,width,height,rx,true,false);return Extremum(field,width,height,rz,false,false);
        }
        public static void BuildConnectedGround()
        {
            Check();Directory.CreateDirectory(VisualReport);var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string map in VisualMaps)
                {
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);string hash=Hash(Source(map));
                    var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var materials=rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.shader.name=="Game/Environment/GroundMacroVariation").Distinct().ToArray();
                    var ground=map=="DenseCity"?AssetDatabase.LoadAssetAtPath<Material>(Templates+"/Warm_PolygonMilitary_GroundVariation.mat"):AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+map+"/Ground.mat");
                    var mask=ground.GetTexture("_DistrictSurfaceMap") as Texture2D;
                    if(mask==null)throw new InvalidOperationException("No district mask: "+map);
                    var rect=ground.GetVector("_DistrictSurfaceRect");int width=mask.width,height=mask.height;
                    string backup=VisualReport+"/local-baselines/"+map+"-mask.rgba";Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    var pixels=mask.GetPixels32();
                    if(!File.Exists(backup))
                    {
                        var raw=new byte[pixels.Length*4];for(int i=0;i<pixels.Length;i++){raw[i*4]=pixels[i].r;raw[i*4+1]=pixels[i].g;raw[i*4+2]=pixels[i].b;raw[i*4+3]=pixels[i].a;}File.WriteAllBytes(backup,raw);
                    }
                    else
                    {
                        var raw=File.ReadAllBytes(backup);if(raw.Length!=pixels.Length*4)throw new InvalidOperationException("Mask baseline dimensions changed.");
                        for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(raw[i*4],raw[i*4+1],raw[i*4+2],raw[i*4+3]);
                    }
                    var protectedRoad=new bool[pixels.Length];
                    foreach(var r in rs.Where(r=>r.name=="Asphalt"||MeshName(r).StartsWith("SM_Env_DirtRoad_")||MeshName(r).ToLowerInvariant().Contains("runway")))
                    {
                        var b=r.bounds;b.Expand(new Vector3(1.5f,0,1.5f));
                        int xa=Mathf.Clamp(Mathf.FloorToInt((b.min.x-rect.x)/rect.z*width),0,width-1),xb=Mathf.Clamp(Mathf.CeilToInt((b.max.x-rect.x)/rect.z*width),0,width-1);
                        int za=Mathf.Clamp(Mathf.FloorToInt((b.min.z-rect.y)/rect.w*height),0,height-1),zb=Mathf.Clamp(Mathf.CeilToInt((b.max.z-rect.y)/rect.w*height),0,height-1);
                        for(int z=za;z<=zb;z++)for(int x=xa;x<=xb;x++)protectedRoad[z*width+x]=true;
                    }
                    var fields=new byte[3][];
                    for(int channel=0;channel<3;channel++)
                    {
                        fields[channel]=new byte[pixels.Length];for(int i=0;i<pixels.Length;i++)fields[channel][i]=channel==0?pixels[i].r:channel==1?pixels[i].g:pixels[i].b;
                        fields[channel]=CloseField(fields[channel],width,height,Mathf.Clamp(Mathf.RoundToInt(3*width/rect.z),1,6),Mathf.Clamp(Mathf.RoundToInt(3*height/rect.w),1,6));
                    }
                    var receipt=new ConnectedGroundReceipt{map=map,sourceHashBefore=hash,mask=AssetDatabase.GetAssetPath(mask),protectedRoadTexels=protectedRoad.Count(x=>x)};
                    for(int z=2;z<height-2;z++)for(int x=2;x<width-2;x++)
                    {
                        int i=z*width+x;if(protectedRoad[i])continue;
                        byte old=Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b));if(old>=240)continue;
                        int channel=fields[1][i]>fields[0][i]?1:0;if(fields[2][i]>fields[channel][i])channel=2;
                        float wx=rect.x+(x+.5f)/width*rect.z,wz=rect.y+(z+.5f)/height*rect.w;
                        byte value=(byte)(fields[channel][i]*Mathf.Lerp(.86f,.98f,Mathf.PerlinNoise(wx*.23f+7,wz*.23f+31)));
                        if(value<=old+12)continue;
                        pixels[i]=channel==0?new Color32(value,0,0,255):channel==1?new Color32(0,value,0,255):new Color32(0,0,value,255);receipt.connectedTexels++;
                    }
                    mask.SetPixels32(pixels);mask.Apply(true,false);EditorUtility.SetDirty(mask);AssetDatabase.SaveAssetIfDirty(mask);
                    foreach(var m in materials.Where(m=>m==ground||m.name=="TrackSoil"||(map=="DenseCity"&&(m.name=="OutskirtsSand"||m.name.StartsWith("Warm_")))))
                    {
                        m.SetColor("_SandDryColor",new Color(.78f,.73f,.62f));m.SetColor("_SandWornColor",new Color(.67f,.62f,.53f));
                        m.SetColor("_SandScrubColor",new Color(.70f,.68f,.56f));m.SetFloat("_SandScrubCoverage",.10f);m.SetFloat("_MacroStrength",.22f);
                        Save(m);receipt.groundMaterials++;
                    }
                    receipt.sourceHashAfter=Hash(Source(map));receipt.squareMetresAdded=receipt.connectedTexels*rect.z/width*rect.w/height;
                    if(hash!=receipt.sourceHashAfter||receipt.groundMaterials==0)throw new InvalidOperationException("Connected ground changed scene or had no materials: "+map);
                    if(ShaderUtil.ShaderHasError(ground.shader))throw new InvalidOperationException("Ground shader error: "+map);
                    receipt.result="Passed";File.WriteAllText(VisualReport+"/"+map+"-connected-ground.json",JsonUtility.ToJson(receipt,true));
                    Capture(map,scene,def,rs,"connected-ground",VisualReport,DetailViews(rs,def));
                    Debug.Log("[MapConnectedGround] "+JsonUtility.ToJson(receipt));
                }
                Debug.Log("[MapConnectedGround] result=Passed maps=11 sceneGeometryAndGameplay=unchanged");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
