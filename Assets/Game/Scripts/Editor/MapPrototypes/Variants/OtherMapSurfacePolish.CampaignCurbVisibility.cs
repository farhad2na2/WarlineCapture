using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static bool CampaignCurbVisible(Vector3 end,MeshRenderer[] all)
        {
            foreach(var r in all)
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||!Intact(r)||!(NaturalRock(r)||MeshName(r).Contains("SandDunes")||IsBuilding(r)))continue;
                var b=r.bounds;if(!PlanarContains(b,end,.1f)||b.max.y<end.y+.2f||b.min.y>end.y+30)continue;
                var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();
                try{if(collider.Raycast(new Ray(new Vector3(end.x,b.max.y+1,end.z),Vector3.down),out var hit,b.size.y+2)&&hit.point.y>end.y+.2f)return false;}
                finally{UnityEngine.Object.DestroyImmediate(collider);}
            }
            return true;
        }
        static int RestoreOccludedCampaignCurbs(MeshRenderer[] all,List<string> journal)
        {
            var occluders=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&(NaturalRock(r)||MeshName(r).Contains("SandDunes")||IsBuilding(r))).ToArray();
            var original=AssetDatabase.LoadAllAssetsAtPath("Assets/PolygonMilitary/Models/SM_Env_Sidewalk_Straight_02.fbx").OfType<Mesh>().Single(m=>m.name=="SM_Env_Sidewalk_Straight_02");int count=0;
            foreach(var r in all)
            {
                var mf=r.GetComponent<MeshFilter>();if(mf?.sharedMesh==null||!AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith(FixArt+"/CurbsV2/",StringComparison.Ordinal))continue;
                var b=original.bounds;bool hidden=false;
                foreach(float x in new[]{b.min.x,b.max.x})
                    if(!CampaignCurbVisible(r.transform.TransformPoint(new Vector3(x,b.max.y,b.center.z)),occluders)){hidden=true;break;}
                if(!hidden)continue;
                mf.sharedMesh=original;EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);journal.Add("RESTORED OCCLUDED CURB "+Hierarchy(r.transform));count++;
            }
            return count;
        }
    }
}
