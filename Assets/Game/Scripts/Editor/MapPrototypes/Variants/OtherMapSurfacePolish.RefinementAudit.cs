using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        private const string RefinementReport = "Design/AgentReports/MapVisualRefinement20261010";
        private const string RefinementArt = VisualArt + "/Refinement";
        [Serializable] private sealed class RefinementAudit
        {
            public string map, source, sourceHash;
            public Vector3 min, max;
            public VisualRow[] waters, vehicles, roofs, unsupported;
            public string[] lighting;
        }
        private static MeshRenderer[] SceneRenderers(Scene s) => s.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
        private static bool Within(MeshRenderer r, OperationMapDefinition def, float margin=0) =>
            r.bounds.max.x > def.Bounds.PlayableMin.x-margin && r.bounds.min.x < def.Bounds.PlayableMax.x+margin &&
            r.bounds.max.z > def.Bounds.PlayableMin.z-margin && r.bounds.min.z < def.Bounds.PlayableMax.z+margin;
        private static bool RoofProp(MeshRenderer r)
        {
            var n=MeshName(r).ToLowerInvariant();
            return n.Contains("watertank") || n.Contains("satellite") || n.Contains("aircon") || n.Contains("washing") ||
                n.Contains("cloth") || n.Contains("carpet") || n.Contains("awning") || n.Contains("ladder");
        }
        private static bool VehicleScenery(MeshRenderer r)
        {
            var n=MeshName(r).ToLowerInvariant();
            return (n.Contains("vehicle")||n.Contains("car_")||n.Contains("truck")||n.Contains("wreck")) &&
                !n.Contains("aircraft") && r.GetComponentInParent<Game.Authoring.OperationMapBuildingAuthoring>()==null;
        }
        private static bool Supported(Bounds b, MeshRenderer[] supports, Transform self)
        {
            return supports.Any(r=>!r.transform.IsChildOf(self) && r.bounds.max.y >= b.min.y-1.3f && r.bounds.min.y < b.min.y &&
                r.bounds.min.x < b.center.x+.2f && r.bounds.max.x > b.center.x-.2f &&
                r.bounds.min.z < b.center.z+.2f && r.bounds.max.z > b.center.z-.2f);
        }
        private static string[] InspectLighting(Scene scene)
        {
            var output=new List<string>();
            foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
            {
                if(c==null)continue;
                if(c is Light || c.GetType().Name=="Volume" || c.GetType().Name.Contains("LensFlare"))
                {
                    output.Add(Hierarchy(c.transform)+" | "+c.GetType().FullName+" | "+EditorJsonUtility.ToJson(c));
                    var profile=new SerializedObject(c).FindProperty("sharedProfile")?.objectReferenceValue;
                    if(profile!=null)foreach(var part in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(profile)))
                        output.Add(AssetDatabase.GetAssetPath(profile)+" | "+part.GetType().FullName+" | "+EditorJsonUtility.ToJson(part));
                }
            }
            output.Add("RenderSettings | "+EditorJsonUtility.ToJson(Unsupported.GetRenderSettings()));
            return output.ToArray();
        }
        public static void AuditRefinement()
        {
            Check(); Directory.CreateDirectory(RefinementReport);var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in VisualMaps)
                {
                    var source=Source(map); var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var rs=SceneRenderers(scene);var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,30)).ToArray();
                    var supports=visible.Where(r=>IsBuilding(r)||MeshName(r).ToLowerInvariant().Contains("platform")||MeshName(r).ToLowerInvariant().Contains("roof")||MeshName(r).Contains("Ground")).ToArray();
                    var detached=visible.Where(r=>RoofProp(r)&&DecorationRoot(r)!=null&&r.bounds.min.y>4 && !Supported(r.bounds,supports,DecorationRoot(r))).ToArray();
                    var audit=new RefinementAudit {map=map,source=source,sourceHash=Hash(source),min=def.Bounds.PlayableMin,max=def.Bounds.PlayableMax,
                        waters=visible.Where(NaturalWater).Select(DescribeVisual).ToArray(),vehicles=visible.Where(VehicleScenery).Select(DescribeVisual).ToArray(),
                        roofs=visible.Where(r=>MeshName(r).ToLowerInvariant().Contains("roof")||IsBuilding(r)).Select(DescribeVisual).ToArray(),unsupported=detached.Select(DescribeVisual).ToArray()};
                    var binding=EditorSceneManager.OpenScene(Binding(map),OpenSceneMode.Additive);SceneManager.SetActiveScene(binding);audit.lighting=InspectLighting(binding);
                    File.WriteAllText(RefinementReport+"/"+map+"-audit.json",JsonUtility.ToJson(audit,true));
                    var views=new List<Tuple<string,Vector3,float>>(NaturalViews(visible,def));
                    if(detached.Length>0)views.Add(Tuple.Create("detached",detached[0].bounds.center,58f));
                    Capture(map,scene,def,rs,"before",RefinementReport,views.ToArray());
                    if(audit.sourceHash!=Hash(source))throw new InvalidOperationException("Audit mutated scene "+map);
                    Debug.Log($"[MapRefinementAudit] map={map} result=Passed unsupportedCandidates={detached.Length} water={audit.waters.Length} vehicles={audit.vehicles.Length}");
                    GC.Collect();
                }
                var match=EditorSceneManager.OpenScene("Assets/Game/Scenes/Match.unity",OpenSceneMode.Single);
                File.WriteAllLines(RefinementReport+"/match-lighting.txt",InspectLighting(match));
                Debug.Log("[MapRefinementAudit] result=Passed maps=11");
            }
            finally { RestoreVisualReviewSetup(setup); }
        }
    }
}
