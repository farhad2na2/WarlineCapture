using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        private static RefinementAudit ReadRefinementAudit(string map)
        {
            string path=RefinementReport+"/"+map+"-audit.json";
            if(File.Exists(path))return JsonUtility.FromJson<RefinementAudit>(File.ReadAllText(path));
            using(var file=File.OpenRead(path+".gz"))
            using(var gzip=new GZipStream(file,CompressionMode.Decompress))
            using(var reader=new StreamReader(gzip))return JsonUtility.FromJson<RefinementAudit>(reader.ReadToEnd());
        }
        private static string WaterMeshKey(Vector3[] vertices,int[] triangles,Color[] colors)
        {
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            return TextHash(string.Join("|",vertices.Select(p=>p.ToString("F4",culture)))+"/"+string.Join(",",triangles)+"/"+string.Join("|",colors.Select(c=>c.ToString("F3",culture)))).Substring(0,20);
        }
        public static void ConsolidateRefinementWater()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int replaced=0;
            try
            {
                foreach(var map in VisualMaps)
                {
                    string path=RefinementReport+"/"+map+"-result.json",source=Source(map);
                    var receipt=JsonUtility.FromJson<RefinementReceipt>(File.ReadAllText(path));
                    if(receipt.result!="Passed"||receipt.afterHash!=Hash(source))throw new InvalidOperationException("Water consolidation requires exact saved source "+map);
                    if(receipt.waterEdges==0)continue;
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    string original=TextHash(NonVisualReferences(ReadSceneText(source)));int count=0;
                    foreach(var r in SceneRenderers(scene).Where(NaturalWater))
                    {
                        var filter=r.GetComponent<MeshFilter>();var old=filter.sharedMesh;
                        if(!AssetDatabase.GetAssetPath(old).StartsWith(RefinementArt+"/WaterMeshes/"))continue;
                        var vertices=old.vertices.Select(v=>new Vector3(Mathf.Round(v.x*10000)/10000,Mathf.Round(v.y*10000)/10000,Mathf.Round(v.z*10000)/10000)).ToArray();
                        string key=WaterMeshKey(vertices,old.triangles,old.colors),meshPath=RefinementArt+"/WaterMeshes/Bank_"+key+".asset";
                        var shared=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(shared==null)
                        {
                            shared=UnityEngine.Object.Instantiate(old);shared.name="CampaignWaterBank_"+key;shared.vertices=vertices;shared.RecalculateNormals();shared.RecalculateBounds();AssetDatabase.CreateAsset(shared,meshPath);
                        }
                        if(filter.sharedMesh==shared)continue;
                        filter.sharedMesh=shared;EditorUtility.SetDirty(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(filter);count++;
                    }
                    if(count>0)
                    {
                        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Consolidation save failed "+map);
                        if(original!=TextHash(NonVisualReferences(ReadSceneText(source))))throw new InvalidOperationException("Non-mesh data changed "+map);
                        receipt.afterHash=Hash(source);File.WriteAllText(path,JsonUtility.ToJson(receipt,true));
                    }
                    replaced+=count;Debug.Log("[MapRefinementWaterReuse] map="+map+" sharedMeshReplacements="+count);
                }
                // Only task-created obsolete meshes are eligible; verify references against all saved sources.
                var dependencies=VisualMaps.SelectMany(map=>AssetDatabase.GetDependencies(Source(map),true)).ToHashSet();
                foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{RefinementArt+"/WaterMeshes"}))
                {
                    string path=AssetDatabase.GUIDToAssetPath(guid);
                    if(Path.GetFileName(path).StartsWith("Bank_")||dependencies.Contains(path))continue;
                    if(!VisualMaps.Any(map=>Path.GetFileName(path).StartsWith(map+"_")))throw new InvalidOperationException("Unexpected mesh in task directory "+path);
                    if(!AssetDatabase.DeleteAsset(path))throw new InvalidOperationException("Cannot remove unreferenced task mesh "+path);
                }
                AssetDatabase.SaveAssets();Debug.Log("[MapRefinementWaterReuse] result=Passed replacements="+replaced+" protectedSceneData=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
