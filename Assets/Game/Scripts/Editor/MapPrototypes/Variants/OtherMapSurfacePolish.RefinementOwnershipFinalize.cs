using System;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        // Reconcile only the exact identities transferred to intact building owners by
        // the saved refinement journal. Runtime readiness remains an exact count check.
        public static void RepairRefinementReadiness()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int repaired=0;
            const string report="Design/AgentReports/CampaignVisualAudit20261011/fixes/readiness";
            Directory.CreateDirectory(report);
            try
            {
                foreach(var map in VisualMaps)
                {
                    string path=RefinementReport+"/"+map+"-result.json";
                    if(!File.Exists(path))continue;
                    var receipt=JsonUtility.FromJson<RefinementReceipt>(File.ReadAllText(path));
                    int removed=receipt.independentAttachmentIdentitiesRemoved;
                    if(removed==0)continue;
                    string source=Source(map);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    var ids=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).ToArray();
                    var roots=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true)).ToArray();
                    if(roots.Length!=3||ids.Select(i=>i.StableId).Distinct().Count()!=ids.Length||ids.Any(i=>!i.TryValidate(out _)))throw new InvalidOperationException("Invalid source identities "+map);
                    int render=ids.Count(i=>i.Role==OperationMapEntityPresentationRole.RenderOnly);
                    int buildings=ids.Count(i=>i.Role==OperationMapEntityPresentationRole.GameplayBuildings);
                    if(roots.All(r=>r.ExpectedGeneratedIdentityCount==ids.Length&&r.ExpectedRenderOnlyCount==render))continue;
                    if(receipt.afterHash!=Hash(source))throw new InvalidOperationException("Refinement source no longer matches journal "+map);
                    var transferred=receipt.attachments.SelectMany(a=>a.presentationIdentities).Distinct().ToArray();
                    if(transferred.Length!=removed||transferred.Any(id=>ids.Any(i=>i.StableId==id)))throw new InvalidOperationException("Removed identities not explained by attachments "+map);
                    foreach(var entry in receipt.attachments)
                    {
                        if(!GlobalObjectId.TryParse(entry.prop,out var objectId))throw new InvalidOperationException("Invalid journal object");
                        var t=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(objectId) as Transform;
                        var attachment=t==null?null:t.GetComponent<OperationMapBuildingAttachmentAuthoring>();
                        if(attachment==null||!attachment.TryValidate(out _)||attachment.BuildingOwner.StableId!=entry.buildingStableId||!attachment.BuildingOwner.TryValidate(out _))throw new InvalidOperationException("Lost attachment owner "+map);
                    }
                    foreach(var root in roots)
                    {
                        if(root.ExpectedGeneratedIdentityCount!=ids.Length+removed||root.ExpectedRenderOnlyCount!=render+removed||root.ExpectedGameplayBuildingCount!=buildings)throw new InvalidOperationException("Count delta not explained by journal "+map);
                        var so=new SerializedObject(root);
                        so.FindProperty("expectedGeneratedIdentityCount").intValue=ids.Length;
                        so.FindProperty("expectedRenderOnlyCount").intValue=render;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(root);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Readiness save failed");
                    File.WriteAllText(report+"/"+map+".txt",$"source={source}\nbefore={receipt.afterHash}\nafter={Hash(source)}\ntransferredIdentities={removed}\nexpectedGenerated={ids.Length}\nexpectedRenderOnly={render}\nexpectedBuildings={buildings}\nAll removed identities verified against live intact building attachments. Runtime predicate unchanged.\n");
                    repaired++;Debug.Log($"[RefinementReadinessRepair] map={map} generated={ids.Length} renderOnly={render} transferred={removed}");
                }
                Debug.Log($"[RefinementReadinessRepair] result=Passed repaired={repaired}");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }

        public static void FinalizeRefinementOwnership()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int attachments=0,removed=0;
            try
            {
                foreach(var map in VisualMaps)
                {
                    string source=Source(map),path=RefinementReport+"/"+map+"-result.json";
                    var receipt=JsonUtility.FromJson<RefinementReceipt>(File.ReadAllText(path));
                    if(receipt.result!="Passed"||receipt.afterHash!=Hash(source))throw new InvalidOperationException("Ownership refinement requires exact saved source "+map);
                    if(receipt.attachments==null||receipt.attachments.Length==0)continue;
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var protection=new RefinementProtection(scene);int mapRemoved=0;
                    foreach(var entry in receipt.attachments)
                    {
                        if(!GlobalObjectId.TryParse(entry.prop,out var id))throw new InvalidOperationException("Invalid attachment source identity");
                        var root=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Transform;
                        if(root==null)throw new InvalidOperationException("Cannot resolve saved attachment "+entry.prop);
                        var attachment=root.GetComponent<OperationMapBuildingAttachmentAuthoring>();
                        if(attachment==null||attachment.BuildingOwner.StableId!=entry.buildingStableId||!attachment.TryValidate(out _))throw new InvalidOperationException("Attachment owner mismatch");
                        foreach(var identity in root.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true))
                        {
                            if(!entry.presentationIdentities.Contains(identity.StableId))throw new InvalidOperationException("Unexpected independent attachment identity");
                            protection.RemovedIdentities.Add(identity.GetEntityId().ToString());protection.AttachmentObjects.Add(identity.gameObject.GetEntityId().ToString());
                            UnityEngine.Object.DestroyImmediate(identity);mapRemoved++;
                        }
                        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                        {
                            if(renderer.GetComponentInParent<DenseCityPresentationIdentityAuthoring>(true)!=attachment.BuildingOwner.IntactVisualRoot.GetComponentInParent<DenseCityPresentationIdentityAuthoring>(true))throw new InvalidOperationException("Renderer has a separate streamed visual owner: "+Hierarchy(renderer.transform)+" building="+Hierarchy(attachment.BuildingOwner.transform)+" identities="+string.Join(";",renderer.GetComponentsInParent<DenseCityPresentationIdentityAuthoring>(true).Select(i=>Hierarchy(i.transform)+":"+i.StableId)));
                        }
                        if(!attachment.BuildingOwner.TryValidate(out string error))throw new InvalidOperationException(error);
                        attachments++;
                    }
                    if(mapRemoved>0)
                    {
                        var generated=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).ToArray();
                        var accepted=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true)).ToArray();
                        int renderOnly=generated.Count(i=>i.Role==OperationMapEntityPresentationRole.RenderOnly)+accepted.Count(i=>i.Role==OperationMapEntityPresentationRole.RenderOnly);
                        foreach(var root in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true)))
                        {
                            if(root.ExpectedGeneratedIdentityCount!=generated.Length+mapRemoved||root.ExpectedRenderOnlyCount!=renderOnly+mapRemoved)
                                throw new InvalidOperationException("Ownership transfer did not reconcile with prior source readiness: "+map);
                            protection.TransferRenderOnlyIdentities(root,mapRemoved);
                        }
                    }
                    string protectedHash=protection.Validate(scene);
                    if(mapRemoved>0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Ownership save failed");
                        if(protectedHash!=protection.Validate(scene))throw new InvalidOperationException("Ownership save changed protected data");
                        receipt.afterHash=Hash(source);receipt.canonicalOwnershipHash=protectedHash;receipt.independentAttachmentIdentitiesRemoved+=mapRemoved;
                        File.WriteAllText(path,JsonUtility.ToJson(receipt,true));
                    }
                    removed+=mapRemoved;Debug.Log("[MapRefinementOwnership] map="+map+" attachments="+receipt.attachments.Length+" independentIdentitiesRemoved="+mapRemoved);
                }
                Debug.Log("[MapRefinementOwnership] result=Passed attachments="+attachments+" independentIdentitiesRemoved="+removed+" protectedSceneData=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
