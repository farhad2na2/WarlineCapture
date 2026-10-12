using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    // Source-only fallback for maps that fail their native runtime readiness gate.
    // Never bypasses that gate or represents these views as a playable mission.
    public static class CampaignSourceVisualAudit20261011
    {
        const string Root="Design/AgentReports/CampaignVisualAudit20261011";
        static string Hash(string path){using var h=SHA256.Create();using var f=File.OpenRead(path);return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
        [Serializable] public class Receipt { public string missionId,kind,source,sourceHashBefore,sourceHashAfter,graphics;public string[] files;public string scope="Authored source preview; runtime load failed; no deployed units or HUD; overview suppresses distance fog/post processing for inspection; detail views retain authored lighting"; }
        public static void CaptureRunwayEnds()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor must be idle");
            var setup=EditorSceneManager.GetSceneManagerSetup();int total=0,maps=0;
            try
            {
                var rows=JsonUtility.FromJson<CampaignVisualAudit20261011.Inventory>(File.ReadAllText(Root+"/inventory.json")).missions;
                foreach(var row in rows.Where(x=>x.index==0||x.index==3||x.index==18))
                {
                    bool dense=row.index==0;
                    string source=dense?"Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity":row.sourceScene.Replace("RuntimeBinding.unity","PreparedEntities.unity");
                    string output=Root+"/source-runway-previews/"+row.missionId+"/run-01";
                    if(Directory.Exists(output))throw new Exception("Evidence already exists "+output);
                    var receipt=new Receipt{missionId=row.missionId,kind="source-runway-preview",source=source,sourceHashBefore=Hash(source),graphics=SystemInfo.graphicsDeviceType.ToString(),scope="Authored runway-end inspection; no runtime actors/HUD; overview suppresses fog/post processing, detail uses authored lighting"};
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(row.mapPath);
                    var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var views=new System.Collections.Generic.List<Tuple<string,Vector3,float>>();int number=0;
                    foreach(var r in rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy))
                    {
                        var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;
                        if(mesh==null||!(mesh.name=="RunwaySurface"||mesh.name=="Campaign_RunwaySurface"||mesh.name=="SM_Env_Runway_01"))continue;
                        number++;var b=mesh.bounds;
                        views.Add(Tuple.Create($"runway-{number:00}-end-a",r.transform.TransformPoint(new Vector3(b.min.x,b.center.y,b.center.z)),115f));
                        views.Add(Tuple.Create($"runway-{number:00}-end-b",r.transform.TransformPoint(new Vector3(b.max.x,b.center.y,b.center.z)),115f));
                    }
                    if(number==0){Debug.Log("[CampaignRunwayAudit] source="+source+" meshes=0");continue;}
                    Directory.CreateDirectory(output);
                    string map=dense?"DenseCity":Path.GetFileName(Path.GetDirectoryName(source));
                    typeof(Game.Editor.MapVariants.OtherMapSurfacePolish).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{map,scene,def,rs,"source-runway",output,views.ToArray()});
                    receipt.sourceHashAfter=Hash(source);if(receipt.sourceHashBefore!=receipt.sourceHashAfter)throw new Exception("Authored source changed");
                    receipt.files=Directory.GetFiles(output,"*.png").Select(Path.GetFileName).ToArray();File.WriteAllText(output+"/receipt.json",JsonUtility.ToJson(receipt,true));
                    File.WriteAllText(output+"/targets.txt",string.Join("\n",views.Select(v=>$"{v.Item1} focus={v.Item2} distance={v.Item3}")));
                    total+=number;maps++;Debug.Log($"[CampaignRunwayAudit] source={source} runways={number} endpoints={views.Count}");
                }
                if(maps<2)throw new Exception("Expected at least CityEdgeAirfield and GroundedSignal runway sources");
                Debug.Log($"[CampaignRunwayAudit] result=Passed maps={maps} runways={total} endpoints={total*2}");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void CaptureBlockedSources()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Editor must be idle");
            var setup=EditorSceneManager.GetSceneManagerSetup();int count=0;
            try
            {
                var rows=JsonUtility.FromJson<CampaignVisualAudit20261011.Inventory>(File.ReadAllText(Root+"/inventory.json")).missions;
                foreach(var row in rows.Where(x=>x.index>=20))
                {
                    string captures=Root+"/captures/"+row.missionId;
                    if(Directory.Exists(captures)&&Directory.GetFiles(captures,"receipt.json",SearchOption.AllDirectories).Any(p=>JsonUtility.FromJson<CampaignVisualAudit20261011.Receipt>(File.ReadAllText(p)).result=="Passed"))continue;
                    string source=row.sourceScene.Replace("RuntimeBinding.unity","PreparedEntities.unity");
                    string output=Root+"/source-previews/"+row.missionId+"/run-01";
                    if(Directory.Exists(output))throw new Exception("Evidence already exists "+output);
                    Directory.CreateDirectory(output);
                    var receipt=new Receipt{missionId=row.missionId,kind="source-preview",source=source,sourceHashBefore=Hash(source),graphics=SystemInfo.graphicsDeviceType.ToString()};
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(row.mapPath);
                    var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    string[] suffixes=row.index==20?new[]{"clinic","utility","reinforcement_perimeter"}:row.index==22?new[]{"node_1","node_2","node_3","audit_gate","extraction"}:row.index==23?new[]{"receiving","repair","receiving_gate"}:new[]{"clinic","utility","audit","breach","core_audit"};
                    var views=def.Anchors.ToArray().Where(a=>suffixes.Any(s=>a.AnchorId.EndsWith("."+s))).Select(a=>Tuple.Create("story-"+a.AnchorId.Split('.').Last(),a.Position,125f)).ToArray();
                    string map=Path.GetFileName(Path.GetDirectoryName(source));
                    typeof(Game.Editor.MapVariants.OtherMapSurfacePolish).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{map,scene,def,rs,"source-preview",output,views});
                    receipt.sourceHashAfter=Hash(source);if(receipt.sourceHashBefore!=receipt.sourceHashAfter)throw new Exception("Authored source changed");
                    receipt.files=Directory.GetFiles(output,"*.png").Select(Path.GetFileName).ToArray();
                    File.WriteAllText(output+"/receipt.json",JsonUtility.ToJson(receipt,true));
                    count++;Debug.Log("[CampaignSourceVisualAudit] mission="+row.missionId+" result=Passed images="+receipt.files.Length+" runtimeAccepted=false");
                }
                Debug.Log("[CampaignSourceVisualAudit] result=Passed missions="+count);
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
