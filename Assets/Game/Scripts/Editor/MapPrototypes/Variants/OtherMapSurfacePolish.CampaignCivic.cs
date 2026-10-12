using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static readonly Rect CivicRect=Rect.MinMaxRect(1752,735,1830,765);
        static bool CivicContains(Vector3 p)=>CivicRect.Contains(new Vector2(p.x,p.z));
        static Mesh RemoveCivicPaint(MeshRenderer r)
        {
            var source=r.GetComponent<MeshFilter>().sharedMesh;
            var mesh=UnityEngine.Object.Instantiate(source);mesh.name=source.name;
            var points=source.vertices;
            for(int s=0;s<source.subMeshCount;s++)
            {
                var original=source.GetTriangles(s);var triangles=new List<int>();
                for(int i=0;i<original.Length;i+=3)
                {
                    Vector3 center=r.transform.TransformPoint((points[original[i]]+points[original[i+1]]+points[original[i+2]])/3);
                    if(CivicContains(center))continue;
                    triangles.Add(original[i]);triangles.Add(original[i+1]);triangles.Add(original[i+2]);
                }
                mesh.SetTriangles(triangles,s);
            }
            mesh.RecalculateBounds();return SaveVisualMesh(FixArt+"/Civic/"+source.name+".asset",mesh);
        }
        static float CivicSupport(MeshRenderer[] rs,Vector3 position)
        {
            float top=.035f;
            foreach(var r in rs)
            {
                var b=r.bounds;if(b.max.y>1.5f||b.size.y>2||position.x<b.min.x||position.x>b.max.x||position.z<b.min.z||position.z>b.max.z)continue;
                var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
                try{if(collider.Raycast(new Ray(new Vector3(position.x,3,position.z),Vector3.down),out var hit,4))top=Mathf.Max(top,hit.point.y+.025f);}
                finally{UnityEngine.Object.DestroyImmediate(collider);}
            }
            return top;
        }
        public static void BuildCampaignCivicForecourt()
        {
            Check();Directory.CreateDirectory(FixReport+"/civic");Directory.CreateDirectory(FixArt+"/Civic");AssetDatabase.Refresh();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(Source("DenseCity"),OpenSceneMode.Single);var rs=SceneRenderers(scene);
                var ids=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).Select(i=>i.StableId).OrderBy(x=>x).ToArray();
                var owners=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).Select(o=>o.StableId).OrderBy(x=>x).ToArray();
                var materials=new Dictionary<Material,Material>();int changed=0;
                foreach(var r in rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)))
                {
                    var b=r.bounds;if(b.min.x>CivicRect.xMax||b.max.x<CivicRect.xMin||b.min.z>CivicRect.yMax||b.max.z<CivicRect.yMin||b.min.y>1)continue;
                    bool surface=r.sharedMaterials.Any(m=>m!=null&&m.shader.name=="Game/Environment/GroundMacroVariation");
                    if(surface)
                    {
                        r.sharedMaterials=r.sharedMaterials.Select(m=>
                        {
                            if(m==null||m.shader.name!="Game/Environment/GroundMacroVariation")return m;
                            if(materials.TryGetValue(m,out var saved))return saved;
                            if(AssetDatabase.GetAssetPath(m).StartsWith(FixArt+"/Civic/"))return m;
                            string path=FixArt+"/Civic/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m))+".mat";
                            saved=AssetDatabase.LoadAssetAtPath<Material>(path);if(saved==null){saved=new Material(m);AssetDatabase.CreateAsset(saved,path);}
                            saved.SetVector("_CivicSurfaceRect",new Vector4(CivicRect.xMin,CivicRect.yMin,CivicRect.xMax,CivicRect.yMax));saved.SetFloat("_CivicSurfaceStrength",1);Save(saved);materials[m]=saved;return saved;
                        }).ToArray();EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);changed++;
                    }
                    if(new[]{"DenseCity_Lanes","DenseCity_Cracks","DenseCity_SandSpill"}.Contains(MeshName(r)))
                    {var mf=r.GetComponent<MeshFilter>();mf.sharedMesh=RemoveCivicPaint(r);EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);}
                }
                // Dress actual market carts. Keep the entrance axis and vehicle route free.
                var carts=rs.Where(r=>MeshName(r)=="SM_Prop_Cart_Stall_01"&&r.bounds.center.x>1720&&r.bounds.center.x<1850&&r.bounds.center.z>710&&r.bounds.center.z<744)
                    .Where(r=>r.GetComponentInParent<DenseCityPresentationIdentityAuthoring>()!=null).OrderBy(r=>r.bounds.center.x).ToArray();
                int added=0;
                foreach(var cart in carts)
                {
                    var owner=cart.GetComponentInParent<DenseCityPresentationIdentityAuthoring>();
                    if(owner.transform.Find("CampaignMarketStock20261011")!=null)continue;
                    var stock=new GameObject("CampaignMarketStock20261011");stock.transform.SetParent(owner.transform,false);
                    var center=cart.bounds.center;var depth=cart.bounds.extents.z;
                    for(int i=0;i<3;i++)
                    {
                        string name=i==0?"SM_Prop_Crate_Wood_01":i==1?"SM_Prop_Basket_03":"SM_Prop_Pot_01";
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Props/"+name+".prefab");if(prefab==null)throw new InvalidOperationException("Missing market prop "+name);
                        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,stock.transform);g.name="MarketStock_"+i;
                        var p=new Vector3(center.x+(i-1)*.75f,0,center.z-depth-.65f);p.y=CivicSupport(rs,p);g.transform.position=p;g.transform.rotation=Quaternion.Euler(0,i*31,0);g.transform.localScale=Vector3.one*.8f;
                        foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                        foreach(var t in g.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
                        added++;
                    }
                }
                var afterIds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).Select(i=>i.StableId).OrderBy(x=>x).ToArray();
                var afterOwners=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).Select(o=>o.StableId).OrderBy(x=>x).ToArray();
                if(!ids.SequenceEqual(afterIds)||!owners.SequenceEqual(afterOwners))throw new InvalidOperationException("Civic dressing changed canonical owners");
                EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Civic scene save failed");
                File.WriteAllText(FixReport+"/civic/result.txt",$"Ground material renderers={changed}\nMarket stock props={added}\nCanonical identities and gameplay building owners unchanged. Forecourt is a flush stone shared crossing; original road and navigation footprint retained.\n");
                Debug.Log($"[CampaignCivicForecourt] result=Passed surfaces={changed} marketProps={added}");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        public static void BuildCampaignPriorityReview()
        {
            BuildCampaignRunwayEnds();
            CaptureCampaignFixDetails();
            BuildCampaignCurbTransitions();
            CampaignRenderReconciliation20261011.Inspect();
            Debug.Log("[CampaignPriorityReview] result=Passed");
        }
        public static void CaptureCampaignFixDetails()
        {
            Check();Directory.CreateDirectory(FixReport+"/detail-captures");var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in new[]{"DenseCity","CityEdgeAirfield","CH04M04GroundedSignal"})
                {
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);var rs=SceneRenderers(scene);var views=new List<Tuple<string,Vector3,float>>();int n=0;
                    foreach(var r in rs.Where(RunwayRenderer))
                    {
                        var b=r.GetComponent<MeshFilter>().sharedMesh.bounds;
                        foreach(bool left in new[]{true,false})if(OuterRunwayEnd(r,rs.Where(RunwayRenderer).ToArray(),left))views.Add(Tuple.Create("runway-end-"+n++,r.transform.TransformPoint(new Vector3(left?b.min.x:b.max.x,b.center.y,b.center.z)),115f));
                    }
                    if(map=="DenseCity")views.Add(Tuple.Create("civic-forecourt",new Vector3(1789,.5f,747),110f));
                    Capture(map,scene,AssetDatabase.LoadAssetAtPath<Game.Configs.OperationMapDefinition>(Definition(map)),rs,"after",FixReport+"/detail-captures",views.ToArray());
                }
                Debug.Log("[CampaignFixDetails] result=Passed maps=3");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
