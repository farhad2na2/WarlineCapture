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
        private static bool WallAttachedAtCurrent(Bounds bounds,MeshRenderer[] structures,Transform self)
        {
            foreach(var renderer in structures.Where(r=>!r.transform.IsChildOf(self)&&Vector3.Distance(r.bounds.ClosestPoint(bounds.center),bounds.center)<Mathf.Max(bounds.extents.x,bounds.extents.z)+.6f))
            {
                var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                var collider=renderer.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();
                try
                {
                    foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
                    {
                        float extent=direction.x!=0?bounds.extents.x:bounds.extents.z;
                        if(collider.Raycast(new Ray(bounds.center,direction),out var hit,extent+.6f)&&Mathf.Abs(hit.normal.y)<.5f)return true;
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(collider);}
            }
            return false;
        }
        private static bool TryGroundDecoration(Transform root,Bounds bounds,MeshRenderer[] grounds,out Vector3 position)
        {
            position=root.position;float best=float.NegativeInfinity;
            foreach(var renderer in grounds.Where(r=>!r.transform.IsChildOf(root)&&!NaturalWater(r)&&TouchesXZ(r.bounds,bounds)))
            {
                var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                var collider=renderer.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();
                try
                {
                    if(collider.Raycast(new Ray(new Vector3(bounds.center.x,bounds.min.y+.1f,bounds.center.z),Vector3.down),out var hit,4)&&hit.normal.y>.7f)best=Mathf.Max(best,hit.point.y);
                }
                finally{UnityEngine.Object.DestroyImmediate(collider);}
            }
            if(float.IsNegativeInfinity(best)||bounds.min.y-best<.3f)return false;
            position+=Vector3.up*(best-bounds.min.y+.015f);return true;
        }
        public static void RestoreSupportReviewFalsePositives()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int restored=0;
            try
            {
                foreach(var entry in new[]{("CityEdgeAirfield",90),("RefineryDistrict",18),("AshLinePort",6)})
                {
                    string source=Source(entry.Item1),path=RefinementReport+"/"+entry.Item1+"-result.json";
                    var receipt=JsonUtility.FromJson<RefinementReceipt>(File.ReadAllText(path));
                    if(receipt.afterHash!=Hash(source))throw new InvalidOperationException("Support rollback source mismatch");
                    var moves=receipt.moves.Skip(receipt.moves.Length-entry.Item2).ToArray();
                    if(moves.Any(m=>m.reason!="detached roof decoration placed on a ray-verified supported roof")||(receipt.attachments?.Length??0)>0)throw new InvalidOperationException("Unexpected support rollback scope");
                    File.WriteAllText(RefinementReport+"/validation/"+entry.Item1+"-support-01-rejected.json",JsonUtility.ToJson(receipt,true));
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var protection=new RefinementProtection(scene);
                    var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>moves.Any(m=>t.name==m.path.Substring(m.path.LastIndexOf('/')+1))).ToArray();
                    foreach(var move in moves)
                    {
                        var root=transforms.Single(t=>GlobalObjectId.GetGlobalObjectIdSlow(t).targetObjectId==move.transformId&&Hierarchy(t)==move.path&&
                            (PrefabUtility.GetPrefabInstanceHandle(t)==null?0:GlobalObjectId.GetGlobalObjectIdSlow(PrefabUtility.GetPrefabInstanceHandle(t)).targetObjectId)==move.instanceId);
                        if((root.position-move.after).sqrMagnitude>.0001f)throw new InvalidOperationException("Rollback pose mismatch");
                        root.position=move.before;protection.Moved.Add(root.GetEntityId().ToString());EditorUtility.SetDirty(root);PrefabUtility.RecordPrefabInstancePropertyModifications(root);
                    }
                    string invariant=protection.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);
                    if(!EditorSceneManager.SaveScene(scene)||invariant!=protection.Validate(scene))throw new InvalidOperationException("Rollback save failed invariants");
                    receipt.moves=receipt.moves.Take(receipt.moves.Length-entry.Item2).ToArray();receipt.supportedProps-=entry.Item2;receipt.supportVerificationHash=null;receipt.afterHash=Hash(source);
                    File.WriteAllText(path,JsonUtility.ToJson(receipt,true));restored+=entry.Item2;
                }
                Debug.Log("[MapRefinementSupportRollback] result=Passed restored="+restored+" protectedSceneData=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        public static void RepairRemainingRefinementSupport()=>RunRefinementSupport(VisualMaps.ToArray());
        public static void RepairCommandNodePlatformSupports()=>RunRefinementSupport(new[]{"CH05M05CommandNode"});
        private static void RunRefinementSupport(string[] maps)
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int repaired=0;
            try
            {
                foreach(var map in maps)
                {
                    string path=RefinementReport+"/"+map+"-result.json",source=Source(map);
                    var receipt=JsonUtility.FromJson<RefinementReceipt>(File.ReadAllText(path));
                    if(receipt.result!="Passed"||receipt.afterHash!=Hash(source))throw new InvalidOperationException("Support repair requires exact saved source "+map);
                    string backup=RefinementReport+"/local-baselines/"+map+"-support-review.unity";if(!File.Exists(backup)){File.Copy(source,backup);File.Copy(path,backup+".receipt.json");}
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var protection=new RefinementProtection(scene);
                    var rs=SceneRenderers(scene);var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,30)).ToArray();
                    var moves=new List<PropMove>();int count=receipt.supportedProps;
                    Debug.Log("[MapRefinementSupport] checking="+map);
                    // Repairs can remove a false support beneath another prop. Require a stable second pass.
                    bool stable=false;
                    for(int pass=0;pass<4;pass++)
                    {
                        int before=moves.Count;FixUnsupported(receipt,visible,moves,protection);
                        if(moves.Count==before){stable=true;break;}
                    }
                    if(!stable)throw new InvalidOperationException("Decoration support did not stabilize "+map);
                    string protectedHash=protection.Validate(scene);
                    if(moves.Count>0)
                    {
                        receipt.moves=receipt.moves.Concat(moves).ToArray();receipt.attachments=(receipt.attachments??Array.Empty<VisualAttachment>()).Concat(protection.Attachments).ToArray();
                        receipt.independentAttachmentIdentitiesRemoved+=protection.RemovedIdentities.Count;
                        receipt.supportVerificationHash=protectedHash;
                        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Support save failed");
                        if(protection.Validate(scene)!=protectedHash)throw new InvalidOperationException("Support save changed protected data");
                        receipt.afterHash=Hash(source);File.WriteAllText(path,JsonUtility.ToJson(receipt,true));
                    }
                    repaired+=receipt.supportedProps-count;Debug.Log("[MapRefinementSupport] map="+map+" additionalRepairs="+(receipt.supportedProps-count));
                }
                Debug.Log("[MapRefinementSupport] result=Passed maps="+maps.Length+" additionalRepairs="+repaired+" protectedSceneData=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
