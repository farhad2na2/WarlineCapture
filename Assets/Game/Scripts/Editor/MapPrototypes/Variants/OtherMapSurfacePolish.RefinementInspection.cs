using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        [Serializable] private sealed class RefinementGeometryProbe { public string map;public VisualRow[] rows; }
        public static void InspectRemainingRefinementGeometry()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in new[]{"CH05M01CitywideAlert","CH05M03NetworkCollapse","CH05M04LastCorridor","CH05M05CommandNode"})
                {
                    string source=Source(map),hash=Hash(source);var audit=ReadRefinementAudit(map);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    var center=audit.unsupported[0].center;
                    var rows=SceneRenderers(scene).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Mathf.Abs(r.bounds.center.x-center.x)<65&&Mathf.Abs(r.bounds.center.z-center.z)<65&&r.bounds.min.y>.5f).Select(DescribeVisual).ToArray();
                    File.WriteAllText(RefinementReport+"/"+map+"-geometry-probe.json",JsonUtility.ToJson(new RefinementGeometryProbe{map=map,rows=rows},true));
                    if(hash!=Hash(source))throw new InvalidOperationException("Inspection changed source");
                }
                Debug.Log("[MapRefinementGeometryInspection] result=Passed maps=4 sourceUnchanged=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        public static void InspectCommandNodeServiceGround()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();string source=Source("CH05M05CommandNode"),hash=Hash(source);
            try
            {
                var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                var def=AssetDatabase.LoadAssetAtPath<Game.Configs.OperationMapDefinition>(Definition("CH05M05CommandNode"));
                var visible=SceneRenderers(scene).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,30)).ToArray();
                var grounds=visible.Where(r=>r.bounds.size.x>100||r.bounds.size.z>100||Hierarchy(r.transform).Contains("/Ground/")).ToArray();
                var obstacles=visible.Where(r=>!RefinementBuilding(r)&&r.bounds.size.y>.1f&&r.bounds.size.x<100&&r.bounds.size.z<100).ToArray();
                var lines=new System.Collections.Generic.List<string>();
                foreach(float x in new[]{1100f,1112f,1124f,1136f})foreach(float z in new[]{481f,489f,497f,505f})
                {
                    var p=new Vector3(x,0,z);var b=new Bounds(p,new Vector3(3,3,3));
                    string blocked=string.Join(";",obstacles.Where(r=>TouchesXZ(b,r.bounds,.6f)).Take(5).Select(r=>Hierarchy(r.transform)+" "+r.bounds));
                    string ground=string.Join(";",grounds.Where(r=>!NaturalWater(r)&&TouchesXZ(b,r.bounds)).Select(r=>r.name+" support="+RoofSupport(r,p,b.size,out float y)+" y="+y+" bounds="+r.bounds));
                    lines.Add(p+" blocked="+blocked+" ground="+ground);
                }
                File.WriteAllLines(RefinementReport+"/command-node-service-ground-inspection.txt",lines);
                if(hash!=Hash(source))throw new InvalidOperationException("Inspection changed source");
                Debug.Log("[MapRefinementGroundInspection] result=Passed sourceUnchanged=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        public static void RepairRefinementMobileTier()
        {
            Check();var assets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if(assets.Length!=1)throw new InvalidOperationException("Cannot resolve native QualitySettings asset");
            var so=new SerializedObject(assets[0]);var tiers=so.FindProperty("m_QualitySettings");int updated=0;
            for(int i=0;i<tiers.arraySize;i++)
            {
                var tier=tiers.GetArrayElementAtIndex(i);
                if(tier.FindPropertyRelative("name").stringValue!="Mobile")continue;
                tier.FindPropertyRelative("antiAliasing").intValue=0;updated++;
            }
            if(updated!=1)throw new InvalidOperationException("Expected one Mobile tier");
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(assets[0]);AssetDatabase.SaveAssets();
            Debug.Log("[MapRefinementMobileTier] result=Passed mobileBuiltinMsaa=0 highUrpMsaa=unchanged");
        }
    }
}
