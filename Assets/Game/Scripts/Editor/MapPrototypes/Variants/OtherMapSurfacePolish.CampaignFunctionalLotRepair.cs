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
        public static void RepairSupplyDestinationLots()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            string report=FixReport+"/functional-lot-clearance-01";
            if(Directory.Exists(report))throw new Exception("Preserve existing lot-repair evidence; use a new run.");
            Directory.CreateDirectory(report);
            try
            {
                const string map="RefineryDistrict";
                var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);
                var rs=SceneRenderers(scene);var protection=new RefinementProtection(scene);
                var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                var journal=new List<string>();var views=new List<Tuple<string,Vector3,float>>();
                int moved=RepairCampaignDestinationRoadClearance(map,rs,def,protection,journal,views);
                string hash=protection.Validate(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Protected data changed during lot clearance");
                Capture(map,scene,def,rs,"after",report,views.ToArray());
                File.WriteAllLines(report+"/RefineryDistrict.txt",journal.Concat(new[]{"protectedHash="+hash}));
                Debug.Log("[CampaignFunctionalLots] result=Passed moved="+moved+" requiredOrigins=unchanged runtimeVerification=pending");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
