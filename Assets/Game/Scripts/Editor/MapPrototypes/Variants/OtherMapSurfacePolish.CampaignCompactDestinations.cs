using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static Transform DestinationParent(MeshRenderer r)
        {
            var regular=DecorationRoot(r);if(regular!=null)return regular;
            var identity=r.GetComponentInParent<OperationMapEntityPresentationIdentityAuthoring>();
            if(identity==null||identity.Role!=OperationMapEntityPresentationRole.RenderOnly||identity.GetComponentInParent<OperationMapBuildingAuthoring>()!=null)return null;
            if(identity.GetComponentsInParent<Collider>(true).Length>0)return null;
            foreach(var c in identity.GetComponentsInChildren<Component>(true))
                if(c==null||!(c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup||c is OperationMapEntityPresentationIdentityAuthoring))return null;
            return identity.transform;
        }
        static IEnumerable<Vector3> DestinationCandidates(Vector3 anchor,bool dense)
        {
            if(dense)
            {
                var cells=new List<Vector3>();
                for(int z=-38;z<=38;z++)for(int x=-38;x<=38;x++)
                    if(x*x+z*z>=64&&x*x+z*z<=38*38)cells.Add(new Vector3(Mathf.Round(anchor.x)+x,anchor.y,Mathf.Round(anchor.z)+z));
                return cells.OrderBy(p=>(p-anchor).sqrMagnitude);
            }
            var ring=new List<Vector3>();
            for(int radius=8;radius<=38;radius+=3)for(int step=0;step<16;step++)
            {float angle=step*Mathf.PI/8;ring.Add(anchor+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));}
            return ring;
        }
        static Dictionary<Vector2Int,List<Rect>> DestinationObstacleBins(Rect[] rectangles)
        {
            var bins=new Dictionary<Vector2Int,List<Rect>>();
            foreach(var rect in rectangles)
                for(int z=Mathf.FloorToInt(rect.yMin/16);z<=Mathf.FloorToInt(rect.yMax/16);z++)
                for(int x=Mathf.FloorToInt(rect.xMin/16);x<=Mathf.FloorToInt(rect.xMax/16);x++)
                {var key=new Vector2Int(x,z);if(!bins.TryGetValue(key,out var list))bins[key]=list=new List<Rect>();list.Add(rect);}
            return bins;
        }
        static bool DestinationObstructed(Dictionary<Vector2Int,List<Rect>> bins,Rect rect)
        {
            for(int z=Mathf.FloorToInt(rect.yMin/16);z<=Mathf.FloorToInt(rect.yMax/16);z++)
            for(int x=Mathf.FloorToInt(rect.xMin/16);x<=Mathf.FloorToInt(rect.xMax/16);x++)
                if(bins.TryGetValue(new Vector2Int(x,z),out var list)&&list.Any(r=>r.Overlaps(rect)))return true;
            return false;
        }
        static GameObject CreateCompactDestination(string name,string kind)
        {
            var root=new GameObject(name);
            Bounds Add(string asset,float x,float z,float maxSize=2,float y=0,float yaw=0)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Props/"+asset+".prefab");
                if(prefab==null)throw new Exception("Missing compact scenery "+asset);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
                foreach(var c in go.GetComponentsInChildren<Component>(true))if(c!=null&&!(c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup))UnityEngine.Object.DestroyImmediate(c);
                go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one;var b=DecorationBounds(go.transform);
                float size=Mathf.Max(b.size.x,b.size.z);if(size>maxSize)go.transform.localScale*=maxSize/size;
                b=DecorationBounds(go.transform);go.transform.position+=new Vector3(x-b.center.x,y-b.min.y,z-b.center.z);
                foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;return DecorationBounds(go.transform);
            }
            if(kind=="market")
            {
                Add("SM_Prop_Cart_Stall_01",0,.2f,2.7f);Add("SM_Prop_Basket_03",-1.35f,-.8f,.65f);Add("SM_Prop_Pot_01",1.35f,-.8f,.65f);
                Add("Rugs/SM_Prop_Rug_Rolls_01",1.3f,.9f,.8f);
            }
            else if(kind=="aid"||kind=="records"||kind=="domestic"||kind=="airfield")
            {
                Add(kind=="domestic"?"SM_Prop_Umbrella_02":"SM_Prop_Umbrella_01",0,.2f,2.8f);
                var table=Add("SM_Prop_Table_Outdoor_01",0,.1f,1.6f);
                Add(kind=="aid"||kind=="airfield"?"Military/SM_Prop_MedicalBox_01":"SM_Prop_Crate_Wood_01",0,.1f,.6f,table.max.y+.01f);
                Add("SM_Prop_Chair_Folding_01",0,-1.1f,.8f,0,180);
                Add(kind=="domestic"?"SM_Prop_Basket_03":"SM_Prop_Crate_Wood_02",1.5f,.5f,.8f);
            }
            else
            {
                Add("SM_Prop_Cabinet_01",-1,.5f,1.8f);Add("SM_Prop_Generator_Small_01",.6f,.5f,1.2f);
                Add(kind=="camp"?"Military/SM_Prop_Bed_SleepingBag_Roll_01":"SM_Prop_Cable_01",.5f,-.7f,.9f);
                Add("SM_Prop_Crate_Wood_01",-1,-.7f,.8f);
            }
            var bounds=DecorationBounds(root.transform);foreach(Transform child in root.transform)child.position-=new Vector3(bounds.center.x,0,bounds.center.z);
            root.isStatic=true;return root;
        }
    }
}
