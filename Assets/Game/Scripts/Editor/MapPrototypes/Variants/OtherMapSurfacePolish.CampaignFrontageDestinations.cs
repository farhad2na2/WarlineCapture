using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static GameObject CreateFrontageDestination(string name,string kind)
        {
            var root=new GameObject(name);
            Bounds Add(string asset,float x,float z,float size,float y=0,float yaw=0)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Props/"+asset+".prefab");if(prefab==null)throw new Exception("Missing frontage asset "+asset);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
                foreach(var c in go.GetComponentsInChildren<Component>(true))if(c!=null&&!(c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup))UnityEngine.Object.DestroyImmediate(c);
                go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one;var b=DecorationBounds(go.transform);
                go.transform.localScale*=Mathf.Min(1,size/Mathf.Max(b.size.x,b.size.z));b=DecorationBounds(go.transform);go.transform.position+=new Vector3(x-b.center.x,y-b.min.y,z-b.center.z);
                foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;return DecorationBounds(go.transform);
            }
            if(kind=="domestic")
            {
                Add("SM_Prop_Table_Outdoor_01",0,0,.8f);Add("SM_Prop_Chair_Plastic_01",-.8f,0,.65f,0,90);Add("SM_Prop_Chair_Plastic_01",.8f,0,.65f,0,-90);
                Add("SM_Prop_Pot_01",1.35f,0,.35f);
            }
            else
            {
                var table=Add("SM_Prop_Table_Outdoor_01",-.45f,0,1.5f);
                Add("SM_Prop_Basket_03",-.45f,0,.55f,table.max.y+.015f);Add("Rugs/SM_Prop_Rug_Rolls_01",1,0,.65f,0,90);
                Add("SM_Prop_Pot_01",.65f,.55f,.35f);
            }
            var b=DecorationBounds(root.transform);foreach(Transform t in root.transform)t.position-=new Vector3(b.center.x,0,b.center.z);root.isStatic=true;return root;
        }
    }
}
