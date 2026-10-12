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
        static int FinishCampaignRoadTerminals(string map,MeshRenderer[] all,OperationMapDefinition def,List<string> journal,List<Tuple<string,Vector3,float>> views)
        {
            var roads=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&(r.name=="Asphalt"||MeshName(r).Contains("DirtRoad"))).ToArray();int count=0;var unique=new HashSet<string>();
            var water=all.Where(NaturalWater).Select(r=>r.bounds).ToArray();
            var occluders=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&(NaturalRock(r)||MeshName(r).Contains("SandDunes")||IsBuilding(r))).ToArray();
            foreach(var r in roads.Where(r=>r.name=="Asphalt"&&Hierarchy(r.transform).Contains("Road_Asphalt_With_Sidewalk_Straight/")&&Within(r,def,80)&&r.bounds.size.y<.1f))
            {
                var b=r.bounds;var axis=r.transform.TransformVector(Vector3.up);axis.y=0;axis.Normalize();
                if(Mathf.Max(Mathf.Abs(axis.x),Mathf.Abs(axis.z))<.999f)continue;
                float halfLength=Mathf.Abs(axis.x)*b.extents.x+Mathf.Abs(axis.z)*b.extents.z;
                var ends=new List<Vector4>();
                foreach(float side in new[]{-1f,1f})
                {
                    var direction=axis*side;var end=b.center+direction*halfLength;var ahead=end+direction*.8f;
                    if(roads.Any(o=>o!=r&&PlanarContains(o.bounds,ahead,.15f)))continue;
                    if(water.Any(w=>PlanarContains(w,ahead,3)))continue; // Bridge approaches retain their water-crossing treatment.
                    if(end.x<def.Bounds.PlayableMin.x-80||end.x>def.Bounds.PlayableMax.x+80||end.z<def.Bounds.PlayableMin.z-80||end.z>def.Bounds.PlayableMax.z+80||!CampaignCurbVisible(end+Vector3.up*.2f,occluders))continue;
                    ends.Add(new Vector4(end.x,end.z,direction.x,direction.z));if(views.Count(v=>v.Item1.StartsWith("road-end-"))<4)views.Add(Tuple.Create("road-end-"+count+"-"+side,end,50f));journal.Add("ROAD TERMINAL "+Hierarchy(r.transform)+" end="+end+" direction="+direction);
                }
                if(ends.Count==0)continue;
                string id=TextHash(GlobalObjectId.GetGlobalObjectIdSlow(r).ToString()).Substring(0,20);bool assigned=false;
                r.sharedMaterials=r.sharedMaterials.Select((original,index)=>
                {
                    if(original==null||original.shader.name!="Game/Environment/GroundMacroVariation")return original;
                    string path=FixArt+"/RoadEndsV2/"+map+"_"+id+"_"+index+".mat";Directory.CreateDirectory(Path.GetDirectoryName(path));
                    if(!unique.Add(path))throw new Exception("Road terminal instance asset collision");var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(original);AssetDatabase.CreateAsset(material,path);}
                    material.SetVector("_RoadTerminalA",ends[0]);material.SetVector("_RoadTerminalB",ends.Count>1?ends[1]:Vector4.zero);material.SetFloat("_RoadTerminalStrength",1);
                    material.SetColor("_RoadTerminalEarth",new Color(.65f,.60f,.49f,1));Save(material);assigned=true;return material;
                }).ToArray();
                if(assigned){EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);count+=ends.Count;}
            }
            return count;
        }
    }
}
