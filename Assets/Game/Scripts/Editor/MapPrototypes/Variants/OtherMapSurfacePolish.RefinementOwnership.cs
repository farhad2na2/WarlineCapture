using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Authoring;
using Game.Configs;
using Game.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        [Serializable] private sealed class VisualAttachment
        {
            public string prop,beforeParent,afterParent,buildingStableId;
            public string[] presentationIdentities;
        }
        private sealed class RefinementProtection
        {
            private struct Pose { public Vector3 position,scale;public Quaternion rotation;public string parent; }
            private readonly Dictionary<string,string> components=new Dictionary<string,string>();
            public readonly HashSet<string> RemovedIdentities=new HashSet<string>();
            public readonly Dictionary<string,string> AddedAttachments=new Dictionary<string,string>();
            public readonly HashSet<string> AttachmentObjects=new HashSet<string>();
            private readonly Dictionary<string,string> objects=new Dictionary<string,string>();
            private readonly Dictionary<string,Pose> poses=new Dictionary<string,Pose>();
            public readonly HashSet<string> Moved=new HashSet<string>(),Reparented=new HashSet<string>();
            public readonly List<VisualAttachment> Attachments=new List<VisualAttachment>();
            private static string ComponentJson(Component c)
            {
                string json=EditorJsonUtility.ToJson(c);
                if(c is MeshRenderer)json=Regex.Replace(json,"\"m_Materials\":\\[.*?\\]","\"m_Materials\":[]");
                if(c is MeshFilter)json=Regex.Replace(json,"\"m_Mesh\":\\{.*?\\}","\"m_Mesh\":{}");
                return json;
            }
            private static Transform[] Transforms(Scene scene)=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            public RefinementProtection(Scene scene)
            {
                foreach(var t in Transforms(scene))
                {
                    string id=t.GetEntityId().ToString();poses[id]=new Pose{position=t.localPosition,rotation=t.localRotation,scale=t.localScale,parent=t.parent==null?string.Empty:t.parent.GetEntityId().ToString()};
                    objects[t.gameObject.GetEntityId().ToString()]=EditorJsonUtility.ToJson(t.gameObject);
                    foreach(var c in t.GetComponents<Component>().Where(c=>c!=null&&!(c is Transform)))components[c.GetEntityId().ToString()]=ComponentJson(c);
                }
            }
            public string Validate(Scene scene)
            {
                var ts=Transforms(scene);if(ts.Length!=poses.Count)throw new InvalidOperationException("Visual repair changed the transform count");
                var canonical=new List<string>();int componentCount=0;
                foreach(var t in ts)
                {
                    string id=t.GetEntityId().ToString();if(!poses.TryGetValue(id,out var old))throw new InvalidOperationException("Unexpected new transform");
                    string parent=t.parent==null?string.Empty:t.parent.GetEntityId().ToString();
                    if(parent!=old.parent&&!Reparented.Contains(id))throw new InvalidOperationException("Unexpected parent change "+Hierarchy(t));
                    if(!Reparented.Contains(id)&&(t.localScale-old.scale).sqrMagnitude>1e-10f)throw new InvalidOperationException("Unexpected scale change "+Hierarchy(t));
                    if(!Moved.Contains(id)&&((t.localPosition-old.position).sqrMagnitude>1e-10f||Quaternion.Angle(t.localRotation,old.rotation)>.001f||(t.localScale-old.scale).sqrMagnitude>1e-10f))throw new InvalidOperationException("Unexpected transform change "+Hierarchy(t));
                    string go=EditorJsonUtility.ToJson(t.gameObject);
                    string beforeGo=objects[t.gameObject.GetEntityId().ToString()];
                    if(AttachmentObjects.Contains(t.gameObject.GetEntityId().ToString()))
                    {
                        go=Regex.Replace(go,"\"m_Component\":\\[.*?\\]","\"m_Component\":[]");
                        beforeGo=Regex.Replace(beforeGo,"\"m_Component\":\\[.*?\\]","\"m_Component\":[]");
                    }
                    if(go!=beforeGo)throw new InvalidOperationException("GameObject metadata changed "+Hierarchy(t));
                    foreach(var c in t.GetComponents<Component>().Where(c=>c!=null&&!(c is Transform)))
                    {
                        componentCount++;string cid=c.GetEntityId().ToString();string after=ComponentJson(c);
                        if(!components.TryGetValue(cid,out string before))
                        {
                            if(!(c is OperationMapBuildingAttachmentAuthoring)||!AddedAttachments.TryGetValue(cid,out before)||before!=after)throw new InvalidOperationException("Unexpected component "+c.GetType().Name);
                        }
                        if(before!=after)throw new InvalidOperationException("Protected component changed "+Hierarchy(t)+" "+c.GetType().Name);
                        canonical.Add(cid+"|"+before);
                    }
                }
                if(componentCount!=components.Count+AddedAttachments.Count-RemovedIdentities.Count)throw new InvalidOperationException("Visual repair changed component count");
                canonical.Sort(StringComparer.Ordinal);return TextHash(string.Join("\n",canonical));
            }
        }
        private static bool FitsRepairedFootprint(OperationMapBuildingAuthoring owner,Bounds proposed)
        {
            if(owner==null)return true;
            if(!OperationMapBuildingAuthoring.TryGetSelectionLocalBounds(owner.transform,owner.IntactVisualRoot,out var bounds))return false;
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                var p=owner.transform.InverseTransformPoint(proposed.center+new Vector3(x*proposed.extents.x,0,z*proposed.extents.z));
                if(p.x<bounds.min.x-.005f||p.x>bounds.max.x+.005f||p.z<bounds.min.z-.005f||p.z>bounds.max.z+.005f)return false;
            }
            return true;
        }
        private static void AttachRepairedDecoration(Transform root,OperationMapBuildingAuthoring owner,RefinementProtection protection)
        {
            if(owner==null)return;
            if(!OperationMapBuildingAuthoring.TryGetSelectionLocalBounds(owner.transform,owner.IntactVisualRoot,out var before))throw new InvalidOperationException("Missing owner visual bounds");
            var attachment=new VisualAttachment{prop=GlobalObjectId.GetGlobalObjectIdSlow(root).ToString(),beforeParent=GlobalObjectId.GetGlobalObjectIdSlow(root.parent).ToString(),afterParent=GlobalObjectId.GetGlobalObjectIdSlow(owner.IntactVisualRoot.transform).ToString(),buildingStableId=owner.StableId};
            var scale=root.lossyScale;root.SetParent(owner.IntactVisualRoot.transform,true);
            if((root.lossyScale-scale).sqrMagnitude>1e-6f)throw new InvalidOperationException("Attachment scale changed");
            protection.Reparented.Add(root.GetEntityId().ToString());
            var ids=new List<string>();
            foreach(var identity in root.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true))
            {
                // One canonical building owner must cover every descendant in the streamed renderer.
                // Retain the old decorative stable ID in the repair journal, not as an independent owner.
                ids.Add(identity.StableId);protection.RemovedIdentities.Add(identity.GetEntityId().ToString());
                protection.AttachmentObjects.Add(identity.gameObject.GetEntityId().ToString());
                UnityEngine.Object.DestroyImmediate(identity);
            }
            attachment.presentationIdentities=ids.ToArray();protection.Attachments.Add(attachment);
            var authoring=root.gameObject.AddComponent<OperationMapBuildingAttachmentAuthoring>();
            authoring.ConfigureForEditor(owner,OperationMapBuildingVisualState.Intact);
            if(!authoring.TryValidate(out string attachmentError))throw new InvalidOperationException(attachmentError);
            EditorUtility.SetDirty(authoring);PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
            protection.AddedAttachments.Add(authoring.GetEntityId().ToString(),EditorJsonUtility.ToJson(authoring));
            protection.AttachmentObjects.Add(root.gameObject.GetEntityId().ToString());
            EditorUtility.SetDirty(root);PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            if(!owner.TryValidate(out string ownershipError))throw new InvalidOperationException("Invalid repaired visual ownership: "+ownershipError);
            OperationMapBuildingAuthoring.TryGetSelectionLocalBounds(owner.transform,owner.IntactVisualRoot,out var after);
            if(Mathf.Abs(before.min.x-after.min.x)>.01f||Mathf.Abs(before.min.z-after.min.z)>.01f||Mathf.Abs(before.max.x-after.max.x)>.01f||Mathf.Abs(before.max.z-after.max.z)>.01f)throw new InvalidOperationException("Roof detail expanded building footprint "+owner.StableId);
            // The same intact visual transform used by the baked destruction presentation owns every repaired part.
            if(!root.IsChildOf(owner.IntactVisualRoot.transform))throw new InvalidOperationException("Repaired detail is outside intact visual state");
        }
    }
}
