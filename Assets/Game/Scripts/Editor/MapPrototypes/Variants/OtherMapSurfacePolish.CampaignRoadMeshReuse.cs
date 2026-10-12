using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        public static void ReuseIdenticalCampaignRoadMeshes()
        {
            Check();
            var setup=EditorSceneManager.GetSceneManagerSetup();
            var paths=Directory.GetFiles(FixArt+"/RoadEntries","*.asset").OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            var canonical=new Dictionary<string,string>();
            var replacements=new Dictionary<string,string>();
            using(var sha=SHA256.Create())
                foreach(string path in paths)
                {
                    string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
                    if(canonical.TryGetValue(hash,out var first))replacements[path]=first;
                    else canonical.Add(hash,path);
                }
            int total=0;
            var report=new List<string>{"inputMeshAssets="+paths.Length,"uniqueExactPayloads="+canonical.Count};
            try
            {
                foreach(string map in VisualMaps)
                {
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);
                    var protection=new RefinementProtection(scene);int changed=0;
                    foreach(var r in SceneRenderers(scene))
                    {
                        var filter=r.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                        string original=AssetDatabase.GetAssetPath(filter.sharedMesh);
                        if(!replacements.TryGetValue(original,out var replacement))continue;
                        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(replacement);
                        if(mesh==null)throw new Exception("Missing canonical road mesh: "+replacement);
                        filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(filter);changed++;
                    }
                    string hash=protection.Validate(scene);
                    if(changed>0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Road mesh reuse changed protected data");
                    }
                    total+=changed;report.Add(map+"="+changed+" protectedHash="+hash);
                }
                report.Add("result=Passed");
                report.Add("reusedReferences="+total);
                report.Add("geometry=byte-identical; all original generated mesh assets retained");
                File.WriteAllLines(FixReport+"/road-mesh-reuse.txt",report);
                CampaignRenderReconciliation20261011.Inspect();
                Debug.Log("[CampaignRoadMeshReuse] result=Passed references="+total+" uniqueMeshes="+canonical.Count);
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
