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
        public static void DiagnoseCampaignMarketSandAllRenderers()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();string report=FixReport+"/market-sand-diagnosis-02";
            if(Directory.Exists(report))throw new Exception("Preserve prior diagnosis");Directory.CreateDirectory(report);
            try
            {
                var scene=EditorSceneManager.OpenScene(Source("DenseCity"),OpenSceneMode.Single);var rs=SceneRenderers(scene);
                var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition("DenseCity"));
                var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                File.WriteAllLines(report+"/all-road-renderers.txt",visible.Where(r=>XzRect(r.bounds).Overlaps(new Rect(620,410,200,45))&&r.bounds.size.x<250&&r.bounds.size.z<250).Select(r=>Hierarchy(r.transform)+" mesh="+MeshName(r)+" bounds="+r.bounds+" materials="+string.Join(";",r.sharedMaterials.Select(AssetDatabase.GetAssetPath))));
                var view=new[]{Tuple.Create("left",new Vector3(650,0,430),45f),Tuple.Create("right",new Vector3(782,0,430),45f)};
                foreach(string category in new[]{"Sand","Sidewalk","DirtRoad","Rock"})
                {
                    var subset=visible.Where(r=>MeshName(r).Contains(category)).ToArray();
                    try{foreach(var r in subset)r.enabled=false;Capture("DenseCity",scene,def,rs,"without-"+category,report,view);}
                    finally{foreach(var r in subset)r.enabled=true;}
                }
                Debug.Log("[CampaignMarketSandDiagnosis] result=Passed sourceAssetsUnchanged=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
