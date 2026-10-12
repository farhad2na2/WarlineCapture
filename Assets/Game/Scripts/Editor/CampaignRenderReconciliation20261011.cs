using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Game.Editor
{
    public static class CampaignRenderReconciliation20261011
    {
        public static void Inspect()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,OpenSceneMode.Single);
                var branches=new SortedDictionary<string,int>();var addedByBranch=new SortedDictionary<string,int>();int additions=0,destinations=0;
                var baseline=new Dictionary<string,int>{
                    ["AuthoredOperationMapEntityPresentation/GameplayBuildings"]=1584,
                    ["AuthoredOperationMapEntityPresentation/GameplayVehicles"]=340,
                    ["AuthoredOperationMapEntityPresentation/RenderOnly"]=10843,
                    ["CityCrossroads_NorthCourtyard"]=23,
                    ["Generated_GiantDenseMiddleEasternCity_EntityPresentation/GameplayBuildings"]=12781,
                    ["Generated_GiantDenseMiddleEasternCity_EntityPresentation/RenderOnly"]=98597};
                foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy))
                {
                    string path=r.name;for(var t=r.transform.parent;t!=null;t=t.parent)path=t.name+"/"+path;
                    var segments=path.Split('/');string branch=segments[0]+(segments[0]=="CityCrossroads_NorthCourtyard"?"":"/"+segments[1]);
                    int count=r.sharedMaterials.Length;branches.TryGetValue(branch,out int old);branches[branch]=old+count;
                    bool destination=path.Contains("/CampaignDestinationV2_20261011_")||path.Contains("/CampaignDestination20261011_");
                    if(path.Contains("/CampaignMarketStock20261011/")||destination)
                    {
                        if(r.GetComponentInParent<DenseCityPresentationIdentityAuthoring>()==null&&r.GetComponentInParent<OperationMapEntityPresentationIdentityAuthoring>()==null)throw new Exception("New scenery lacks canonical owner: "+path);
                        if(destination)destinations+=count;else additions+=count;
                        addedByBranch.TryGetValue(branch,out int before);addedByBranch[branch]=before+count;
                    }
                }
                foreach(var pair in branches)
                {
                    addedByBranch.TryGetValue(pair.Key,out int added);
                    if(!baseline.TryGetValue(pair.Key,out int old)||pair.Value!=old+added)throw new Exception("Unexplained source render-row change: "+pair.Key);
                }
                if(branches.Count!=baseline.Count)throw new Exception("Missing source branch");
                using var sha=System.Security.Cryptography.SHA256.Create();using var stream=File.OpenRead(scene.path);
                var lines=new List<string>{"sourceSha256="+BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant(),"marketStockMaterialRows="+additions,"destinationMaterialRows="+destinations};
                lines.AddRange(branches.Select(p=>p.Key+"="+p.Value));
                lines.AddRange(addedByBranch.Select(p=>"addition:"+p.Key+"="+p.Value));
                const string pathOut="Design/AgentReports/CampaignVisualAudit20261011/fixes/render-reconciliation.txt";
                File.WriteAllLines(pathOut,lines);Debug.Log("[CampaignRenderReconciliation] result=Passed rows="+branches.Values.Sum()+" marketStockRows="+additions+" destinationRows="+destinations);
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
