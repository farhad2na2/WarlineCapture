using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        private const string VisualReport="Design/AgentReports/MapVisualImprovement20261009";
        private const string VisualArt="Assets/Game/Art/MapBeautify/CampaignVisualDetails";
        private static IEnumerable<string> VisualMaps=>Maps.Concat(new[]{"DenseCity"});
        [Serializable] private sealed class VisualRow
        {
            public string id,name,path,mesh,meshAsset,material,shader,owner;
            public string[] components;
            public Vector3 center,size,position,scale,euler,localMeshSize;
            public int vertices,triangles,colliders;
            public bool active;
        }
        [Serializable] private sealed class VisualAudit
        {
            public string map,source,sourceHash;
            public Vector3 min,max;
            public VisualRow[] roads,buildings,rocks,water,props;
        }
        private static string MeshName(MeshRenderer r)=>r.GetComponent<MeshFilter>()?.sharedMesh?.name??"";
        private static bool Intact(MeshRenderer r)=>!Hierarchy(r.transform).ToLowerInvariant().Contains("destroyed")&&!MeshName(r).ToLowerInvariant().Contains("destroyed");
        private static bool IsBuilding(MeshRenderer r)
        {
            var b=r.bounds;string key=MeshName(r).ToLowerInvariant();
            return Intact(r)&&b.size.y>3&&b.size.x>4&&b.size.z>4&&b.size.x<100&&b.size.z<100&&
                new[]{"house","shop","apartment","office","hangar","barracks","warehouse","factory"}.Any(key.Contains)&&
                !new[]{"door","roof","stairs","cloth"}.Any(key.Contains);
        }
        private static VisualRow DescribeVisual(MeshRenderer r)
        {
            var m=r.GetComponent<MeshFilter>()?.sharedMesh;var t=r.transform;var owner=r.GetComponentInParent<Game.Authoring.OperationMapBuildingAuthoring>();
            return new VisualRow{id=GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(),name=r.name,path=Hierarchy(t),mesh=m?.name,meshAsset=AssetDatabase.GetAssetPath(m),
                material=AssetDatabase.GetAssetPath(r.sharedMaterial),shader=r.sharedMaterial?.shader?.name,owner=owner==null?"":Hierarchy(owner.transform),
                center=r.bounds.center,size=r.bounds.size,position=t.position,scale=t.lossyScale,euler=t.eulerAngles,localMeshSize=m==null?Vector3.zero:m.bounds.size,
                vertices=m==null?0:m.vertexCount,triangles=m==null?0:m.triangles.Length/3,colliders=r.GetComponentsInChildren<Collider>(true).Length,
                active=r.enabled&&r.gameObject.activeInHierarchy,components=r.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray()};
        }
        private static bool IsRoadDetail(MeshRenderer r)
        {
            var name=MeshName(r).ToLowerInvariant();return name.Contains("dirtroad")||name.Contains("runway")||r.name.Contains("SandWear")||
                (name.Contains("ground_hill_flat")&&Mathf.Max(r.bounds.size.x,r.bounds.size.z)>30);
        }
        private static Tuple<string,Vector3,float>[] DetailViews(MeshRenderer[] rs,OperationMapDefinition def)
        {
            var min=def.Bounds.PlayableMin;var max=def.Bounds.PlayableMax;
            bool Near(MeshRenderer r)=>r.bounds.center.x>min.x-80&&r.bounds.center.x<max.x+80&&r.bounds.center.z>min.z-80&&r.bounds.center.z<max.z+80;
            var result=new List<Tuple<string,Vector3,float>>();
            var road=rs.Where(r=>Intact(r)&&IsRoadDetail(r)&&Near(r)).OrderByDescending(r=>r.bounds.size.x*r.bounds.size.z).FirstOrDefault();
            if(road!=null){var p=road.bounds.center;p.y=0;result.Add(Tuple.Create("road",p,Mathf.Clamp(Mathf.Max(road.bounds.size.x,road.bounds.size.z)*.75f,65,170)));}
            var rock=rs.Where(r=>Intact(r)&&Near(r)&&MeshName(r).ToLowerInvariant().Contains("rock")&&r.bounds.size.y>3).OrderByDescending(r=>r.bounds.size.y).FirstOrDefault();
            if(rock!=null)result.Add(Tuple.Create("rock",rock.bounds.center,80f));
            return result.ToArray();
        }
        public static void AuditVisualDetails()
        {
            Check();Directory.CreateDirectory(VisualReport);var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in VisualMaps)
                {
                    string source=Source(map),hash=Hash(source);var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var audit=new VisualAudit{map=map,source=source,sourceHash=hash,min=def.Bounds.PlayableMin,max=def.Bounds.PlayableMax,
                        roads=rs.Where(IsRoadDetail).Select(DescribeVisual).ToArray(),buildings=rs.Where(IsBuilding).Select(DescribeVisual).ToArray(),
                        rocks=rs.Where(r=>new[]{"rock","mountain","cliff"}.Any(k=>MeshName(r).ToLowerInvariant().Contains(k))).Select(DescribeVisual).ToArray(),
                        water=rs.Where(r=>(r.sharedMaterial?.shader?.name??"").ToLowerInvariant().Contains("water")||MeshName(r).ToLowerInvariant().Contains("water")).Select(DescribeVisual).ToArray(),
                        props=rs.Where(r=>Intact(r)&&new[]{"pallet","barrel","crate","container","pole","tree","bush","grass","wreck"}.Any(k=>MeshName(r).ToLowerInvariant().Contains(k))).Select(DescribeVisual).ToArray()};
                    File.WriteAllText(VisualReport+"/"+map+"-visual-audit.json",JsonUtility.ToJson(audit,true));
                    Capture(map,scene,def,rs,"baseline",VisualReport,DetailViews(rs,def));
                    if(hash!=Hash(source))throw new InvalidOperationException("Audit mutated source "+map);
                    Debug.Log($"[MapVisualAudit] map={map} result=Passed roads={audit.roads.Length} buildings={audit.buildings.Length} rocks={audit.rocks.Length} water={audit.water.Length} props={audit.props.Length}");
                }
                Debug.Log("[MapVisualAudit] result=Passed maps=11");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
