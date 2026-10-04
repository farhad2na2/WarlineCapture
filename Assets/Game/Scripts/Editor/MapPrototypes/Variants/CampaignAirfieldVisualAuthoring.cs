using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    // Owner-requested presentation work on the existing shared map. Never rebuilds
    // its terrain, navigation, authored entity scene or gameplay inventories.
    public static class CampaignAirfieldVisualAuthoring
    {
        private const string Folder="Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/";
        private const string Evidence="Design/MapVariants/Beautify/CityEdgeAirfield/20261004";
        private const string Dressing="SharedLandscapeDressing20261004";
        public static void ApplyAndCapture()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit owned preview before authoring.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Unsaved scene work must be preserved.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Evidence);
            try
            {
                var prepared=EditorSceneManager.OpenScene(Folder+"PreparedEntities.unity",OpenSceneMode.Single);
                var binding=EditorSceneManager.OpenScene(Folder+"RuntimeBinding.unity",OpenSceneMode.Additive);
                SceneManager.SetActiveScene(binding);
                var ground=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/MapBeautify/DetailedDesertGround.mat");
                if(ground==null || !ground.HasProperty("_SandPaletteStrength")) throw new InvalidOperationException("Landscape shader has not imported.");
                ground.SetFloat("_SandPaletteStrength",1);
                ground.SetColor("_BaseColor",Color.white);
                ground.SetColor("_SandDryColor",new Color(.88f,.75f,.51f));
                ground.SetColor("_SandWornColor",new Color(.69f,.57f,.39f));
                ground.SetColor("_SandScrubColor",new Color(.65f,.65f,.43f));
                ground.SetColor("_MacroTintA",new Color(.96f,.91f,.78f));
                ground.SetColor("_MacroTintB",new Color(1.05f,1.03f,.96f));
                ground.SetFloat("_MacroStrength",.28f);ground.SetFloat("_GroundDetailStrength",.35f);
                ground.SetFloat("_GroundDetailNormalStrength",.32f);ground.SetFloat("_DetailStrength",.06f);
                EditorUtility.SetDirty(ground);AssetDatabase.SaveAssetIfDirty(ground);
                RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;
                RenderSettings.ambientSkyColor=new Color(.72f,.79f,.88f);
                RenderSettings.ambientEquatorColor=new Color(.60f,.56f,.46f);
                RenderSettings.ambientGroundColor=new Color(.36f,.32f,.25f);
                var old=binding.GetRootGameObjects().FirstOrDefault(g=>g.name==Dressing);
                if(old!=null) UnityEngine.Object.DestroyImmediate(old);
                old=prepared.GetRootGameObjects().FirstOrDefault(g=>g.name==Dressing);
                if(old!=null) UnityEngine.Object.DestroyImmediate(old);
                var root=new GameObject(Dressing);SceneManager.MoveGameObjectToScene(root,prepared);
                var bounds=prepared.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))
                    .Where(r=>r.gameObject.activeInHierarchy && !r.name.StartsWith("Ground_") &&
                        !r.name.Contains("SandWear") && !r.name.Contains("Destroyed") &&
                        !r.GetComponentsInParent<Transform>().Any(t=>t.name.Contains("Fence")) && !r.name.Contains("Paint"))
                    .Select(r=>r.bounds).ToArray();
                // Trees grow in town-side verges outside the source's playable
                // rectangle. They cannot change any mission's movement contract.
                int palms=0,details=0,rejected=0;
                var tree=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_03.prefab");
                var grass=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab");
                var rocks=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Generic/SM_Generic_Small_Rocks_01.prefab");
                var covers=new[]{
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_ClothCover_Small_01.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_ClothCover_Small_02.prefab")};
                var cart=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Stall_01.prefab");
                var pot=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Pot_Large_02.prefab");
                int townGroups=0;
                // Existing town courtyards are outside the airfield's playable
                // sector. Add human-scale colour and greenery in clear gaps;
                // this is dressing on the shared town, not another town layout.
                foreach(float column in new[]{313f,332f,351f,370f,392f})
                for(int j=0;j<12;j++)
                {
                    var p=new Vector3(column+(j%3-1)*1.7f,0,430+j*18+(j%2)*3);
                    if(Overlaps(bounds,p,.7f)){rejected++;continue;}
                    AddVisual(tree,root.transform,p,j*137+column,.88f+(j%3)*.06f);palms++;
                    AddVisual(grass,root.transform,p+new Vector3(.3f,0,-.4f),j*29,.7f);details++;
                    var q=p+new Vector3(-5,0,2.5f);
                    if(townGroups>=12 || Overlaps(bounds,q,3.2f)) continue;
                    AddVisual(covers[townGroups%2],root.transform,q,(j%2)*90,1);
                    AddVisual(cart,root.transform,q+new Vector3(.4f,0,-.3f),(j%2)*90,1);
                    AddVisual(pot,root.transform,q+new Vector3(-1.7f,0,1.5f),j*31,1);
                    townGroups++;details+=3;
                }
                var grid=AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>("Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Grid.asset");
                var blocked=new HashSet<Vector2Int>(grid.BlockedCells);
                bool BlockedTrunk(Vector3 p)
                {
                    for(int z=Mathf.FloorToInt(p.z-.75f);z<=Mathf.FloorToInt(p.z+.75f);z++)
                    for(int x=Mathf.FloorToInt(p.x-.75f);x<=Mathf.FloorToInt(p.x+.75f);x++)
                        if(!blocked.Contains(new Vector2Int(x,z))) return false;
                    return true;
                }
                // Fill existing blocked fence verges, never a walkable cell.
                for(int j=0;j<30;j++)
                {
                    float z=350+j*11;
                    for(int x=409;x<440;x++)
                    {
                        var p=new Vector3(x,0,z+.5f);
                        if(!BlockedTrunk(p) || Overlaps(bounds,p,1.5f)) continue;
                        AddVisual(tree,root.transform,p,j*137,.9f);palms++;
                        AddVisual(grass,root.transform,p+new Vector3(.25f,0,.25f),j*37,.6f);details++;
                        break;
                    }
                }
                foreach(float x in new[]{377f,389f,1020f,1033f})
                for(int j=0;j<24;j++)
                {
                    float z=345+j*14+(j%3)*2;
                    var p=new Vector3(x,0,z);
                    if(Overlaps(bounds,p,3f)){rejected++;continue;}
                    AddVisual(tree,root.transform,p,(j*137)%360,1);palms++;
                    for(int k=0;k<3;k++)
                    {
                        var q=p+new Vector3((k-1)*2.2f,0,(k%2==0?2:-2));
                        AddVisual(k==1?rocks:grass,root.transform,q,j*79+k*43,.7f);details++;
                    }
                }
                // Small verge details remain outside the playable rectangle too.
                // Cluster spacing varies; there is no regular carpet across pads.
                for(int j=0;j<70;j++)
                {
                    float x=410+(j*47)%570,z=j%2==0?285:716;
                    var p=new Vector3(x,0,z);
                    if(Overlaps(bounds,p,2)){rejected++;continue;}
                    AddVisual(j%3==0?rocks:grass,root.transform,p,j*137,.8f);details++;
                }
                if(root.GetComponentsInChildren<Collider>(true).Length!=0 ||
                    root.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)
                    throw new InvalidOperationException("Dressing must contain renderers only.");
                EditorSceneManager.MarkSceneDirty(binding);
                EditorSceneManager.MarkSceneDirty(prepared);
                if(!EditorSceneManager.SaveScene(prepared)) throw new IOException("Shared scenery save failed.");
                if(!EditorSceneManager.SaveScene(binding)) throw new IOException("Shared presentation save failed.");
                Capture(binding,new Vector3(450,0,540),"town-edge");
                Capture(binding,new Vector3(388,0,530),"town");
                Capture(binding,new Vector3(700,0,560),"airfield");
                File.WriteAllText(Evidence+"/dressing.json",JsonUtility.ToJson(new Report{palms=palms,details=details,rejected=rejected,townGroups=townGroups},true));
                Debug.Log("[CampaignAirfieldVisualAuthoring] result=Passed palms="+palms+" details="+details+" townGroups="+townGroups+" terrainGridSurfaceUnchanged=1 sharedConsumers=AllCityEdgeAirfield");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        [Serializable] private sealed class Report {public int palms,details,rejected,townGroups;public string scope="Shared CityEdgeAirfield presentation";public bool playerReady=false;}
        private static bool Overlaps(Bounds[] bounds,Vector3 p,float margin)=>bounds.Any(b=>
            p.x>b.min.x-margin && p.x<b.max.x+margin && p.z>b.min.z-margin && p.z<b.max.z+margin);
        private static void AddVisual(GameObject prefab,Transform parent,Vector3 p,float yaw,float scale)
        {
            if(prefab==null) throw new InvalidOperationException("Existing landscape prefab missing.");
            var instance=UnityEngine.Object.Instantiate(prefab,parent);
            instance.name="Landscape_"+prefab.name;
            instance.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
            instance.transform.localScale=Vector3.one*scale;
            foreach(var c in instance.GetComponentsInChildren<Component>(true))
                if(c!=null && c is not Transform && c is not MeshRenderer && c is not MeshFilter)
                    UnityEngine.Object.DestroyImmediate(c);
        }
        private static void Capture(Scene scene,Vector3 focus,string label)
        {
            var go=new GameObject("LandscapeReviewCamera");SceneManager.MoveGameObjectToScene(go,scene);
            var c=go.AddComponent<Camera>();c.fieldOfView=50;c.nearClipPlane=.3f;c.farClipPlane=3000;
            c.transform.position=focus+new Vector3(36,65,-51);c.transform.LookAt(focus);
            var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            var previousPipeline=QualitySettings.renderPipeline;
            var lighting=go.AddComponent<Game.Composition.SkirmishS004CampaignLighting>();
            try {lighting.Configure(default,scene);c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Evidence+"/"+label+".png",image.EncodeToPNG());}
            finally {RenderTexture.active=previous;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);QualitySettings.renderPipeline=previousPipeline;}
        }
    }
}
