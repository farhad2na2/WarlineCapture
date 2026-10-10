using System;
using System.IO;
using System.Linq;
using Game.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
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
