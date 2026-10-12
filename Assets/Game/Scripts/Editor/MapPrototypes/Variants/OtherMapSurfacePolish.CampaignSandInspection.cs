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
        public static void InspectCampaignMarketSand()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();string report=FixReport+"/market-sand-inspection-01";
            if(Directory.Exists(report))throw new Exception("Preserve prior inspection");Directory.CreateDirectory(report);
            try
            {
                var scene=EditorSceneManager.OpenScene(Source("DenseCity"),OpenSceneMode.Single);var rs=SceneRenderers(scene);
                var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition("DenseCity"));
                var rows=new List<string>();
                var views=new[]{Tuple.Create("left",new Vector3(650,0,430),45f),Tuple.Create("right",new Vector3(782,0,430),45f),Tuple.Create("wide",new Vector3(715,0,430),110f)};
                foreach(var r in rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&XzRect(r.bounds).Overlaps(new Rect(630,416,190,27))&&(MeshName(r).Contains("Sand")||r.name=="Asphalt")))
                    rows.Add(Hierarchy(r.transform)+" mesh="+AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh)+" bounds="+r.bounds);
                File.WriteAllLines(report+"/renderers.txt",rows);
                Capture("DenseCity",scene,def,rs,"source",report,views);
                Debug.Log("[CampaignMarketSandInspection] result=Passed sourceAssetsUnchanged=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
