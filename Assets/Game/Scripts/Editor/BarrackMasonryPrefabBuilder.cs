#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Authoring;
using Game.Composition;
using Game.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Game.Editor
{
    /// <summary>Installs the owner-approved Demo 2 compound without replacing gameplay identity.</summary>
    public static class BarrackMasonryPrefabBuilder
    {
        const string Barrack = "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab";
        const string Candidate = "Assets/Game/Prefabs/Buildings/Candidates/BarrackB/Building_Barrack_B_Masonry_Visual_Preview.prefab";
        const string Folder = "Assets/Game/Prefabs/Buildings/Visuals/BarrackMasonry";
        const string Intact = Folder + "/Barrack_Masonry_Intact.prefab";
        const string Ruins = Folder + "/Barrack_Masonry_Ruins.prefab";
        const string Config = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset";
        const string Evidence = "Design/AgentReports/BarrackReplacementReview/Implementation";
        static string Settings(UnityEngine.Object target) => Regex.Replace(EditorJsonUtility.ToJson(target), "\"destroyedVisualPrefab\":\\{[^}]*\\}", "\"destroyedVisualPrefab\":{}");
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void EnsureFolder(string path) { if(AssetDatabase.IsValidFolder(path))return; var parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path)); }
        public static void BuildAndValidate()
        {
            Require(!EditorApplication.isPlaying,"Author in Edit mode; preserve running matches.");
            Directory.CreateDirectory(Evidence);EnsureFolder(Folder);
            var active=SceneManager.GetActiveScene();bool dirty=active.isDirty;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Barrack);
            Require(source!=null,"Missing Barrack");
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long fileId);
            var config=AssetDatabase.LoadMainAssetAtPath(Config);string configBefore=Settings(config);
            string genericRuins="Assets/Game/Prefabs/Buildings/Destroyed/Building_Barrack_Destroyed.prefab";
            string genericHash=AssetDatabase.GetAssetDependencyHash(genericRuins).ToString();
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(Intact))Require(AssetDatabase.CopyAsset(Candidate,Intact),"Copy approved candidate failed");
            var intactRoot=PrefabUtility.LoadPrefabContents(Intact);
            try
            {
                intactRoot.name="Barrack_Masonry_Intact";
                foreach(var renderer in intactRoot.GetComponentsInChildren<Renderer>(true))
                {
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)
                    {
                        var path=AssetDatabase.GetAssetPath(materials[i]);
                        if(!path.Contains("/Candidates/BarrackB/"))continue;
                        var destination=Folder+"/"+Path.GetFileName(path);
                        if(!AssetDatabase.LoadAssetAtPath<Material>(destination))Require(AssetDatabase.CopyAsset(path,destination),"Material copy failed");
                        materials[i]=AssetDatabase.LoadAssetAtPath<Material>(destination);
                        materials[i].enableInstancing=true;EditorUtility.SetDirty(materials[i]);
                    }
                    renderer.sharedMaterials=materials;
                }
                PrefabUtility.SaveAsPrefabAsset(intactRoot,Intact);
            }
            finally {PrefabUtility.UnloadPrefabContents(intactRoot);}
            BuildRuins();
            var so=new SerializedObject(config);so.FindProperty("destroyedVisualPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Ruins);so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);
            Require(configBefore==Settings(config),"Non-visual gameplay config changed");
            var root=PrefabUtility.LoadPrefabContents(Barrack);
            try
            {
                var authoring=root.GetComponent<BuildingDefinitionAuthoring>();authoring.ApplyConfigIfAvailable();string settings=Settings(authoring);
                var model=root.transform.Find("Model");Require(model!=null,"Missing existing Model root");
                // Keep an installed enemy variant when rebuilding the approved player art.
                var variants=model.GetComponent<Game.Rendering.BuildingFactionVisualVariants>();
                var enemyRoot=variants!=null?variants.EnemyVisualRoot:null;
                var enemyRuins=variants!=null?variants.EnemyDestroyedVisualPrefab:null;
                foreach(var child in model.Cast<Transform>().ToArray())
                    if(enemyRoot==null||child.gameObject!=enemyRoot)UnityEngine.Object.DestroyImmediate(child.gameObject);
                foreach(var component in model.GetComponents<Component>())
                    if(!(component is Transform)&&component!=variants)UnityEngine.Object.DestroyImmediate(component);
                model.localPosition=Vector3.zero;model.localRotation=Quaternion.identity;model.localScale=Vector3.one;
                var compound=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Intact),root.scene);
                compound.name="Masonry compound";compound.transform.SetParent(model,false);
                if(variants!=null)variants.ConfigureForEditor(compound,enemyRoot,enemyRuins);
                Require(settings==Settings(authoring),"Barrack gameplay authoring changed");
                PrefabUtility.SaveAsPrefabAsset(root,Barrack);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            var installed=AssetDatabase.LoadAssetAtPath<GameObject>(Barrack);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(installed,out string afterGuid,out long afterId);
            Require(guid==afterGuid&&fileId==afterId,"Barrack asset identity changed");
            Require(genericHash==AssetDatabase.GetAssetDependencyHash(genericRuins).ToString(),"Shared generic destruction changed");
            ValidateRuntimeAndCapture();
            Require(active==SceneManager.GetActiveScene()&&dirty==active.isDirty,"Active scene changed");
            File.WriteAllText(Evidence+"/identity.txt",$"prefab={Barrack}\nguid={guid}\nfileId={fileId}\nconfigSettings=Preserved\ngenericCityRuins=Preserved\n");
            Debug.Log("[BarrackMasonryReplacement] result=Passed assetIdentity=Preserved footprint=28x15 gameplay=Preserved genericCityRuins=Preserved runtimeCreateDestroyCleanup=Passed productionExit=OutsideFootprint activeScene=Preserved");
        }
        static Material RuinMaterial()
        {
            var path=Folder+"/Barrack_Masonry_Charred.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",new Color(.29f,.22f,.19f));mat.SetFloat("_Smoothness",.08f);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
        }
        static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(cube,parent.gameObject.scene);cube.name=name;cube.transform.SetParent(parent,false);cube.transform.localPosition=position;cube.transform.localScale=scale;cube.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());return cube;
        }
        static void BuildRuins()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("Barrack_Masonry_Ruins");SceneManager.MoveGameObjectToScene(root,scene);
                var concrete=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/BarrackB_Concrete.mat");var charred=RuinMaterial();
                Cube(root.transform,"Damaged foundation",new Vector3(0,-.13f,0),new Vector3(27.6f,.26f,14.6f),concrete);
                Cube(root.transform,"West wall remnant",new Vector3(-12.6f,.7f,-1),new Vector3(.5f,1.4f,7),charred);
                Cube(root.transform,"Rear wall remnant",new Vector3(-9,1.05f,5.5f),new Vector3(7,2.1f,.5f),charred);
                Cube(root.transform,"Front wall remnant",new Vector3(-7,.45f,-5.3f),new Vector3(6,.9f,.55f),charred);
                Cube(root.transform,"Office wall remnant",new Vector3(9,.55f,5.4f),new Vector3(4,1.1f,.4f),charred);
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Rubble_Pile_01.prefab");Require(source!=null,"Missing masonry rubble source");
                var positions=new[]{new Vector3(-9,0,1.5f),new Vector3(-4,0,-1.8f),new Vector3(-8,0,-3),new Vector3(9,0,4)};
                for(int i=0;i<positions.Length;i++)
                {
                    var pile=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);pile.name="Masonry rubble "+i;pile.transform.SetParent(root.transform,false);pile.transform.localRotation=Quaternion.Euler(0,i*71,0);
                    Require(BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(pile,out var b),"Missing rubble bounds");
                    float span=Mathf.Max(b.size.x,b.size.z);pile.transform.localScale*=Mathf.Min(i==3?4:6,span)/span;
                    Require(BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(pile,out b),"Missing scaled rubble bounds");pile.transform.localPosition+=positions[i]-new Vector3(b.center.x,b.min.y,b.center.z);
                    foreach(var collider in pile.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                }
                PrefabUtility.SaveAsPrefabAsset(root,Ruins);
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
        public static void ValidateRuntimeAndCapture()
        {
            Require(!EditorApplication.isPlaying,"Validation uses isolated preview scene in Edit mode.");Directory.CreateDirectory(Evidence);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Barrack);
            var helper=new BuildingDefinitionPrefabSystemHelper();helper.ConfigureAuthoringMetadataResolvers(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,null);
            var definition=helper.CreateDefinition(prefab,"Barracks","",1200,null,null,null,null);
            Require(definition.FootprintCells==new Vector2Int(28,15),"Runtime footprint changed: "+definition.FootprintCells);
            Require(definition.CreditsCost==40000&&definition.MaterialsCost==90&&definition.MaxHealth==1200&&Mathf.Approximately(definition.ProductionDurationSeconds,30),"Runtime gameplay values changed");
            Require(definition.ProductionSlots!=null&&definition.ProductionSlots.Count==1&&definition.ProductionSlots[0].Quantity==4&&definition.ProductionSlots[0].SpawnUnitPrefab!=null,"Production recipe changed");
            Require(definition.ProductionSpawnLocalPositions!=null&&definition.ProductionSpawnLocalPositions.Length>0,"Missing production exit");
            foreach(var exit in definition.ProductionSpawnLocalPositions)Require(Mathf.Abs(exit.x)>14||Mathf.Abs(exit.z)>7.5f,"Production exit inside occupancy");
            foreach(var asset in new[]{prefab,definition.DestroyedVisualPrefab})
            {
                Require(BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(asset,out var b),"Missing bounds");
                Require(b.min.x>=-14.01f&&b.max.x<=14.01f&&b.min.z>=-7.51f&&b.max.z<=7.51f,"Visual exceeds footprint: "+b);
                Require(asset.GetComponentsInChildren<Collider>(true).Length==0,"Visual has duplicate physical blockers");
                foreach(var mesh in asset.GetComponentsInChildren<MeshFilter>(true))Require(mesh.sharedMesh!=null,"Missing mesh");
                foreach(var renderer in asset.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)Require(material!=null&&material.shader!=null&&!ShaderUtil.ShaderHasError(material.shader),"Missing or broken material");
            }
            var scene=EditorSceneManager.NewPreviewScene();var presenter=new BuildingPlacementVisualPresentationSystemHelper();GameObject container=null;
            using(var world=new World("Barrack masonry visual validation"))
            try
            {
                container=new GameObject("Barrack runtime proof");SceneManager.MoveGameObjectToScene(container,scene);
                var instance=presenter.CreateBuildingVisualInstance(definition,container.transform);var visualRoot=instance.transform.GetChild(0);
                var building=new RuntimeBuildingEntity{Instance=instance,Definition=definition,AliveVisualRoots=Enumerable.Range(0,visualRoot.childCount).Select(i=>visualRoot.GetChild(i)).ToArray()};
                Require(instance.GetComponentsInChildren<Renderer>().Length==18,"Approved compound renderer count differs");
                Capture(scene,instance,Evidence+"/Barrack-Masonry-Installed.png");
                instance.transform.rotation=Quaternion.Euler(0,90,0);Require(BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(instance,out var rotated)&&rotated.size.x<=28.01f&&rotated.size.z<=15.01f,"Rotation changes local footprint");instance.transform.rotation=Quaternion.identity;
                var visuals=world.GetOrCreateSystemManaged<BuildingVisualSystem>();var destruction=new BuildingDestroyedVisualPresentationSystemHelper();var context=new BuildingDestroyedVisualPresentationSystemHelper.Context(visuals,o=>UnityEngine.Object.DestroyImmediate(o));
                destruction.BeginDestroyedVisual(context,building);Require(building.DestroyedVisualInstance!=null&&building.AliveVisualRoots.All(t=>!t.gameObject.activeSelf),"Intact visuals survived destruction");
                var destroyed=building.DestroyedVisualInstance;Capture(scene,destroyed,Evidence+"/Barrack-Masonry-Destroyed.png");
                destruction.CleanupDestroyedVisual(context,building);Require(building.DestroyedVisualInstance==null&&destroyed==null,"Destroyed cleanup failed");
                Debug.Log("[BarrackMasonryRuntime] result=Passed create=Native destruction=Native cleanup=Native production=4Soldiers exits=Outside28x15 footprint=28x15 deviceAcceptance=Pending fullMission=NotClaimed");
            }
            finally {presenter.Dispose();if(container)UnityEngine.Object.DestroyImmediate(container);EditorSceneManager.ClosePreviewScene(scene);}
        }
        static void Capture(Scene scene,GameObject model,string path)
        {
            var cameraGo=new GameObject("Barrack proof camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.aspect=1.6f;camera.orthographicSize=16;camera.nearClipPlane=.01f;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.23f,.2f);
            Require(BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(model,out var b),"Capture has no bounds");camera.transform.position=model.transform.TransformPoint(b.center)+new Vector3(1,.85f,-1).normalized*100;camera.transform.LookAt(model.transform.TransformPoint(b.center));
            var lightGo=new GameObject("Barrack proof sun");SceneManager.MoveGameObjectToScene(lightGo,scene);var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.transform.rotation=Quaternion.Euler(48,-35,0);light.shadows=LightShadows.None;
            var target=new RenderTexture(1600,1000,24){antiAliasing=4};Texture2D image=null;var old=RenderTexture.active;
            try {target.Create();camera.targetTexture=target;camera.Render();RenderTexture.active=target;image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally {camera.targetTexture=null;RenderTexture.active=old;if(image)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);}
        }
    }
}
#endif
