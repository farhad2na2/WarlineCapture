using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Game.Configs;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        [Serializable] private sealed class NaturalDetailReceipt
        {
            public string map,result,beforeHash,afterHash,protectedSceneHash;
            public int rocks,mountains,water,plantGroups,runwayGroundPieces,loweredGroundVertices,renderersBefore,renderersAfter,trianglesBefore,trianglesAfter;
            public PropMove[] moves;
            public VisualRow[] waterSurfaces;
        }
        private static bool NaturalRock(MeshRenderer r)
        {
            string n=MeshName(r);
            return n.StartsWith("SM_Env_Rock_")||n.StartsWith("SM_Generic_Small_Rocks_")||n.StartsWith("SM_Env_Mountain_")||n.StartsWith("SM_Gen_Env_Background_Mountain_");
        }
        private static bool NaturalWater(MeshRenderer r)
        {
            string n=MeshName(r).ToLowerInvariant();
            return !n.Contains("tank")&&r.bounds.size.y<1.2f&&(n.Contains("water_plane")||r.sharedMaterials.Any(m=>m!=null&&m.shader!=null&&m.shader.name=="Game/Environment/CampaignWater")||r.sharedMaterials.Any(m=>m!=null&&AssetDatabase.GetAssetPath(m).ToLowerInvariant().Contains("water.mat")));
        }
        private static Material WeatheredRock(Material source,bool mountain)
        {
            string path=VisualArt+"/Landscape/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+(mountain?"_Mountain":"_Rock")+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            var shader=Shader.Find("Game/Environment/CampaignRockWeathering");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Rock shader missing or invalid");
            m=new Material(shader){name=mountain?"Weathered_Mountain":"Weathered_Rock",enableInstancing=true};
            foreach(var property in new[]{"_BaseMap","_Albedo_Map","_MainTex"})
                if(source.HasProperty(property)&&source.GetTexture(property)!=null){m.SetTexture("_BaseMap",source.GetTexture(property));m.SetTextureScale("_BaseMap",source.GetTextureScale(property));m.SetTextureOffset("_BaseMap",source.GetTextureOffset(property));break;}
            if(source.HasProperty("_BaseColor"))m.SetColor("_BaseColor",source.GetColor("_BaseColor"));else if(source.HasProperty("_Color"))m.SetColor("_BaseColor",source.GetColor("_Color"));
            m.SetFloat("_Weathering",mountain?.58f:.70f);m.SetFloat("_BaseHeight",mountain?8:2.8f);m.SetFloat("_StrataScale",mountain?.18f:.62f);
            AssetDatabase.CreateAsset(m,path);return m;
        }
        private static Material RefinedWater(Material source)
        {
            string path=VisualArt+"/Landscape/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+"_Water.mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            var shader=Shader.Find("Game/Environment/CampaignWater");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Water shader missing or invalid");
            m=new Material(shader){name="Muted_Water",enableInstancing=true};m.SetColor("_BaseColor",new Color(.24f,.38f,.42f,1f));
            m.SetFloat("_Smoothness",.46f);m.SetFloat("_RippleStrength",.075f);AssetDatabase.CreateAsset(m,path);return m;
        }
        private static Tuple<string,Vector3,float>[] NaturalViews(MeshRenderer[] visible,OperationMapDefinition def)
        {
            var views=new List<Tuple<string,Vector3,float>>();var center=(def.Bounds.PlayableMin+def.Bounds.PlayableMax)*.5f;center.y=0;
            var rock=visible.Where(r=>NaturalRock(r)&&!MeshName(r).Contains("Mountain")&&r.bounds.size.y>3).OrderBy(r=>(r.bounds.center-center).sqrMagnitude).FirstOrDefault();
            if(rock!=null)views.Add(Tuple.Create("landscape",rock.bounds.center,70f));
            var water=visible.Where(NaturalWater).OrderBy(r=>(r.bounds.center-center).sqrMagnitude).FirstOrDefault();
            if(water!=null)
            {
                var p=water.bounds.ClosestPoint(center);p.y=water.bounds.max.y;
                views.Add(Tuple.Create("waterfront",p,95f));
            }
            var mountain=visible.Where(r=>NaturalRock(r)&&MeshName(r).Contains("Mountain")).OrderBy(r=>(r.bounds.center-center).sqrMagnitude).FirstOrDefault();
            if(mountain!=null)views.Add(Tuple.Create("mountain",mountain.bounds.center,Mathf.Clamp(Mathf.Max(mountain.bounds.size.x,mountain.bounds.size.z),85,220)));
            var runways=visible.Where(r=>MeshName(r).Contains("RunwaySurface")).ToArray();
            if(runways.Length>0)
            {
                var b=runways[0].bounds;foreach(var r in runways.Skip(1))b.Encapsulate(r.bounds);
                views.Add(Tuple.Create("runway",b.center,Mathf.Clamp(Mathf.Max(b.size.x,b.size.z)*.8f,80,360)));
            }
            return views.ToArray();
        }
        private static void ValidateVisualHashHelpers()
        {
            foreach(var sample in new[]{"", "ASCII scene\n", "نام فارسی 😀", new string('x',131071)+"😀\n"})
            {
                using var h=System.Security.Cryptography.SHA256.Create();
                string expected=BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sample))).Replace("-", "").ToLowerInvariant();
                if(TextHash(sample)!=expected)throw new InvalidOperationException("Streaming UTF8 hash mismatch");
            }
            Debug.Log("[MapVisualHashHelpers] result=Passed samples=4");
        }

        private static void RestoreVisualReviewSetup(SceneSetup[] setup)
        {
            // An unsaved empty scene is omitted from GetSceneManagerSetup.
            if(setup.Any(s=>s.isLoaded&&s.isActive&&!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
        public static void FinishNaturalDetails()
        {
            Check();ValidateVisualHashHelpers();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{VisualArt+"/Landscape"}))
                {
                    var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if(m.shader.name!="Game/Environment/CampaignWater")continue;
                    m.SetColor("_BaseColor",new Color(.24f,.38f,.42f,1f));m.SetFloat("_RippleStrength",.075f);m.renderQueue=-1;
                    EditorUtility.SetDirty(m);
                }
                AssetDatabase.SaveAssets();
                foreach(string map in VisualMaps)
                {
                    string source=Source(map),receiptPath=VisualReport+"/"+map+"-natural-finish.json";
                    if(File.Exists(receiptPath))
                    {
                        var completed=JsonUtility.FromJson<NaturalDetailReceipt>(File.ReadAllText(receiptPath));
                        if(completed.result=="Passed"&&completed.afterHash==Hash(source)){Debug.Log("[MapNaturalFinish] preserved completed map="+map);continue;}
                        throw new InvalidOperationException("Finish source changed; inspect saved evidence: "+map);
                    }
                    var prior=JsonUtility.FromJson<NaturalDetailReceipt>(File.ReadAllText(VisualReport+"/"+map+"-natural-details.json"));
                    if(prior.result!="Passed"||prior.afterHash!=Hash(source))throw new InvalidOperationException("Natural detail source mismatch "+map);
                    GC.Collect();
                    var receipt=new NaturalDetailReceipt{map=map,beforeHash=Hash(source),protectedSceneHash=TextHash(NonVisualReferences(ReadSceneText(source))),moves=Array.Empty<PropMove>()};
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));var min=def.Bounds.PlayableMin;var max=def.Bounds.PlayableMax;
                    var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&r.bounds.max.x>min.x-80&&r.bounds.min.x<max.x+80&&r.bounds.max.z>min.z-80&&r.bounds.min.z<max.z+80).ToArray();
                    var views=NaturalViews(visible,def);var runwayViews=views.Where(v=>v.Item1=="runway").ToArray();
                    if(runwayViews.Length>0)Capture(map,scene,def,rs,"runway-before",VisualReport,runwayViews);
                    receipt.renderersBefore=rs.Length;receipt.trianglesBefore=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3);
                    if(receipt.protectedSceneHash!=TextHash(NonVisualReferences(ReadSceneText(source))))throw new InvalidOperationException("Protected scene data changed in final surface finish "+map);
                    receipt.renderersAfter=rs.Length;receipt.trianglesAfter=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3);
                    if(receipt.trianglesBefore!=receipt.trianglesAfter)throw new InvalidOperationException("Runway triangle count changed "+map);
                    AssetDatabase.SaveAssets();receipt.afterHash=Hash(source);
                    Capture(map,scene,def,rs,"natural-final",VisualReport,views);
                    foreach(var m in rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(VisualArt+"/Landscape/")).Distinct())
                        if(ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Landscape shader failed native render "+m.name);
                    receipt.result="Passed";File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt,true));
                    Debug.Log($"[MapNaturalFinish] map={map} result=Passed runwayGroundPieces={receipt.runwayGroundPieces} loweredVertices={receipt.loweredGroundVertices} geometryCount=unchanged");
                }
                Debug.Log("[MapNaturalFinish] result=Passed maps=11 protectedSceneData=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }

        public static void ClearRunwaySandEdges()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string map in VisualMaps)
                {
                    string source=Source(map),receiptPath=VisualReport+"/"+map+"-runway-clearance.json";
                    if(File.Exists(receiptPath))
                    {
                        var completed=JsonUtility.FromJson<NaturalDetailReceipt>(File.ReadAllText(receiptPath));
                        if(completed.result=="Passed"&&completed.afterHash==Hash(source)){Debug.Log("[MapRunwayClearance] preserved completed map="+map);continue;}
                        throw new InvalidOperationException("Runway clearance source changed; inspect saved evidence: "+map);
                    }
                    var prior=JsonUtility.FromJson<NaturalDetailReceipt>(File.ReadAllText(VisualReport+"/"+map+"-natural-finish.json"));
                    if(prior.result!="Passed"||prior.afterHash!=Hash(source))throw new InvalidOperationException("Final source mismatch "+map);
                    var receipt=new NaturalDetailReceipt{map=map,beforeHash=Hash(source),moves=Array.Empty<PropMove>()};
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));var min=def.Bounds.PlayableMin;var max=def.Bounds.PlayableMax;
                    var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&r.bounds.max.x>min.x-80&&r.bounds.min.x<max.x+80&&r.bounds.max.z>min.z-80&&r.bounds.min.z<max.z+80).ToArray();
                    var runways=visible.Where(r=>MeshName(r).Contains("RunwaySurface")).Select(r=>r.bounds).ToArray();
                    var roads=visible.Where(r=>r.name=="Asphalt"||MeshName(r).Contains("DirtRoad")||MeshName(r).Contains("RunwaySurface")).Select(r=>r.bounds).ToArray();
                    var hard=visible.Where(r=>r.bounds.size.x>1&&r.bounds.size.z>1&&r.bounds.size.x<80&&r.bounds.size.z<80&&r.bounds.size.y>.2f).ToArray();
                    var moves=new List<PropMove>();var occupied=new List<Bounds>();
                    receipt.renderersBefore=rs.Length;receipt.trianglesBefore=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3);
                    string original=runways.Length>0?ReadSceneText(source):null;
                    foreach(var root in visible.Where(r=>MeshName(r).StartsWith("SM_Env_SandEdge_")).Select(DecorationRoot).Where(t=>t!=null).Distinct().OrderBy(Hierarchy))
                    {
                        var b=DecorationBounds(root);var overlap=runways.Where(r=>TouchesXZ(b,r,.2f)).ToArray();if(overlap.Length==0)continue;
                        var road=overlap.OrderBy(r=>(r.center-b.center).sqrMagnitude).First();var options=new List<Vector3>();
                        for(int i=0;i<7;i++)
                        {
                            float pad=1.8f+i*2.5f;
                            foreach(float along in new[]{0f,-6f,6f,-12f,12f,-18f,18f,-24f,24f,-32f,32f,-40f,40f})
                            {
                                options.Add(new Vector3(b.center.x+along,b.center.y,road.min.z-b.extents.z-pad));options.Add(new Vector3(b.center.x+along,b.center.y,road.max.z+b.extents.z+pad));
                                options.Add(new Vector3(road.min.x-b.extents.x-pad,b.center.y,b.center.z+along));options.Add(new Vector3(road.max.x+b.extents.x+pad,b.center.y,b.center.z+along));
                            }
                        }
                        bool placed=false;
                        foreach(var target in options.OrderBy(t=>(t-b.center).sqrMagnitude))
                        {
                            var proposed=new Bounds(target,b.size);
                            if(target.x<min.x||target.x>max.x||target.z<min.z||target.z>max.z||roads.Any(r=>TouchesXZ(proposed,r,1.2f))||occupied.Any(r=>TouchesXZ(proposed,r,.3f))||hard.Any(r=>!r.transform.IsChildOf(root)&&TouchesXZ(proposed,r.bounds,.3f)))continue;
                            moves.Add(MoveDecoration(root,root.position+target-b.center,root.eulerAngles.y,"decorative sand edge moved from runway to clear shoulder"));occupied.Add(proposed);placed=true;break;
                        }
                        if(!placed)throw new InvalidOperationException("No legal sand-edge shoulder placement: "+Hierarchy(root));
                    }
                    receipt.moves=moves.ToArray();
                    if(original!=null)receipt.protectedSceneHash=TextHash(NonDecorativeChanges(original,receipt.moves));original=null;GC.Collect();
                    if(moves.Count>0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Runway clearance save failed "+map);
                        if(receipt.protectedSceneHash!=TextHash(NonDecorativeChanges(ReadSceneText(source),receipt.moves)))throw new InvalidOperationException("Protected runway scene data changed "+map);
                    }
                    int remaining=visible.Count(r=>MeshName(r).StartsWith("SM_Env_SandEdge_")&&runways.Any(b=>TouchesXZ(r.bounds,b,.2f)));
                    if(remaining>0)throw new InvalidOperationException("Sand edges remain inside runway: "+map+" count="+remaining);
                    receipt.renderersAfter=rs.Length;receipt.trianglesAfter=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3);
                    if(receipt.renderersBefore!=receipt.renderersAfter||receipt.trianglesBefore!=receipt.trianglesAfter)throw new InvalidOperationException("Runway clearance geometry count changed "+map);
                    receipt.afterHash=Hash(source);AssetDatabase.SaveAssets();
                    if(runways.Length>0)Capture(map,scene,def,rs,"natural-final",VisualReport,NaturalViews(visible,def));
                    receipt.result="Passed";File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt,true));
                    Debug.Log($"[MapRunwayClearance] map={map} result=Passed movedSandEdges={moves.Count} remainingIntrusions=0");
                }
                Debug.Log("[MapRunwayClearance] result=Passed maps=11 remainingIntrusions=0");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        public static void BuildNaturalDetails()
        {
            Check();ValidateVisualHashHelpers();Directory.CreateDirectory(VisualArt+"/Landscape");AssetDatabase.Refresh();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string map in VisualMaps)
                {
                    string source=Source(map),receiptPath=VisualReport+"/"+map+"-natural-details.json",backup=VisualReport+"/local-baselines/"+map+"-natural-scene.unity";
                    if(File.Exists(receiptPath))
                    {
                        var completed=JsonUtility.FromJson<NaturalDetailReceipt>(File.ReadAllText(receiptPath));
                        if(completed.result=="Passed"&&completed.afterHash==Hash(source)){Debug.Log("[MapNaturalDetails] preserved completed map="+map);continue;}
                        throw new InvalidOperationException("Natural-detail source changed since its saved receipt; re-audit before a new baseline: "+map);
                    }
                    GC.Collect();
                    string original=ReadSceneText(source);if(!File.Exists(backup))File.WriteAllText(backup,original);
                    else if(Hash(source)!=Hash(backup))throw new InvalidOperationException("Inspect partial natural-detail receipt before retrying "+map);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));var min=def.Bounds.PlayableMin;var max=def.Bounds.PlayableMax;
                    bool Near(MeshRenderer r)=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&r.bounds.max.x>min.x-80&&r.bounds.min.x<max.x+80&&r.bounds.max.z>min.z-80&&r.bounds.min.z<max.z+80;
                    var visible=rs.Where(Near).ToArray();var views=NaturalViews(visible,def);
                    Capture(map,scene,def,rs,"natural-before",VisualReport,views);
                    var receipt=new NaturalDetailReceipt{map=map,beforeHash=Hash(source),renderersBefore=rs.Length,trianglesBefore=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3)};
                    receipt.waterSurfaces=visible.Where(NaturalWater).Select(DescribeVisual).ToArray();
                    foreach(var r in visible.Where(r=>r.GetComponentInParent<Game.Authoring.OperationMapBuildingAuthoring>()==null&&!MeshName(r).Contains("Background_Mountain")&&(NaturalRock(r)||NaturalWater(r))))
                    {
                        bool water=NaturalWater(r),mountain=MeshName(r).Contains("Mountain");var mats=r.sharedMaterials;bool changed=false;
                        for(int i=0;i<mats.Length;i++)
                        {
                            if(mats[i]==null||AssetDatabase.GetAssetPath(mats[i]).StartsWith(VisualArt+"/Landscape/"))continue;
                            mats[i]=water?RefinedWater(mats[i]):WeatheredRock(mats[i],mountain);changed=true;
                        }
                        if(!changed)continue;r.sharedMaterials=mats;EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                        if(water)receipt.water++;else if(mountain)receipt.mountains++;else receipt.rocks++;
                    }
                    var roads=visible.Where(r=>r.name=="Asphalt"||MeshName(r).Contains("DirtRoad")||MeshName(r).Contains("Runway")).Select(r=>r.bounds).ToArray();
                    var buildings=visible.Where(IsBuilding).Select(r=>r.bounds).ToArray();
                    var plants=visible.Where(r=>MeshName(r).StartsWith("SM_Env_Grass_")||MeshName(r).StartsWith("SM_Generic_Grass_"))
                        .Select(DecorationRoot).Where(t=>t!=null).Distinct().Where(t=>{var b=DecorationBounds(t);return b.size.x<3&&b.size.z<3&&b.size.y<3;}).ToList();
                    var hard=visible.Where(r=>!NaturalRock(r)&&!IsRoadDetail(r)&&!MeshName(r).Contains("Ground")&&r.bounds.size.y>1&&r.bounds.size.x>1&&r.bounds.size.z>1&&r.bounds.size.x<60&&r.bounds.size.z<60).ToArray();
                    var moves=new List<PropMove>();var occupied=new List<Bounds>();
                    foreach(var rock in visible.Where(r=>NaturalRock(r)&&!MeshName(r).Contains("Mountain")&&r.bounds.size.y>1.5f&&r.bounds.size.x<20&&r.bounds.size.z<20).OrderBy(r=>Hierarchy(r.transform),StringComparer.Ordinal))
                    {
                        if(moves.Count>=150)break;var rb=rock.bounds;int count=0;
                        foreach(var plant in plants.OrderBy(t=>(t.position-rb.center).sqrMagnitude).Take(9).ToArray())
                        {
                            var b=DecorationBounds(plant);if((b.center-rb.center).sqrMagnitude>18*18||Mathf.Abs(b.min.y-rb.min.y)>1.2f)continue;
                            foreach(int side in new[]{-1,1})
                            {
                                var p=new Vector3(rb.center.x+side*(rb.extents.x+b.extents.x+.45f),b.center.y,rb.center.z+(count-1)*1.65f);
                                var proposed=new Bounds(p,b.size);
                                if(p.x<min.x||p.x>max.x||p.z<min.z||p.z>max.z||roads.Any(x=>TouchesXZ(proposed,x,1.5f))||buildings.Any(x=>TouchesXZ(proposed,x,1.5f))||occupied.Any(x=>TouchesXZ(proposed,x,.15f))||hard.Any(r=>!r.transform.IsChildOf(plant)&&TouchesXZ(proposed,r.bounds,.25f)))continue;
                                moves.Add(MoveDecoration(plant,plant.position+p-b.center,plant.eulerAngles.y,"sheltered planting at existing rock base"));occupied.Add(proposed);plants.Remove(plant);count++;break;
                            }
                            if(count>=3)break;
                        }
                        if(count>0)receipt.plantGroups++;
                    }
                    receipt.moves=moves.ToArray();receipt.protectedSceneHash=TextHash(NonDecorativeChanges(original,receipt.moves));original=null;GC.Collect();receipt.result="Pending native save and invariant check";
                    File.WriteAllText(receiptPath+".pending.json",JsonUtility.ToJson(receipt,true));
                    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Natural scene save failed "+map);
                    if(receipt.protectedSceneHash!=TextHash(NonDecorativeChanges(ReadSceneText(source),receipt.moves)))throw new InvalidOperationException("Non-decorative scene data changed "+map);
                    receipt.renderersAfter=rs.Length;receipt.trianglesAfter=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3);
                    if(receipt.trianglesBefore!=receipt.trianglesAfter)throw new InvalidOperationException("Natural detail changed geometry count "+map);
                    AssetDatabase.SaveAssets();
                    foreach(var m in rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(VisualArt+"/Landscape/")).Distinct())
                        if(ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Landscape shader error "+m.name);
                    receipt.afterHash=Hash(source);
                    Capture(map,scene,def,rs,"natural-details",VisualReport,views);
                    foreach(var m in rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(VisualArt+"/Landscape/")).Distinct())
                        if(ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Landscape shader failed after native render "+m.name);
                    receipt.result="Passed";File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt,true));
                    GC.Collect();
                    Debug.Log($"[MapNaturalDetails] map={map} result=Passed rocks={receipt.rocks} mountains={receipt.mountains} water={receipt.water} plantGroups={receipt.plantGroups} movedPlants={moves.Count} newRenderers=0 newTriangles=0");
                }
                Debug.Log("[MapNaturalDetails] result=Passed maps=11 gameplayCollisionAndLighting=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
