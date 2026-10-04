#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Authoring;
using Game.Composition;
using Game.Components;
using Game.Rendering;
using Game.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
namespace Game.Editor
{
    public static class EnemyBarrackPrefabBuilder
    {
        const string Barrack="Assets/Game/Prefabs/Buildings/Building_Barrack.prefab";
        const string Folder="Assets/Game/Prefabs/Buildings/Visuals/BarrackMasonry";
        const string Enemy=Folder+"/Barrack_Masonry_Enemy_C.prefab";
        const string Ruins=Folder+"/Barrack_Masonry_Enemy_C_Ruins.prefab";
        const string Evidence="Design/AgentReports/BarrackReplacementReview/EnemyC";
        static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        public static void BuildAndValidate()
        {
            Require(!EditorApplication.isPlaying,"Preserve running matches; author in Edit mode.");Directory.CreateDirectory(Evidence);
            var active=SceneManager.GetActiveScene();bool dirty=active.isDirty;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Barrack);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);
            var config=AssetDatabase.LoadMainAssetAtPath("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset");string settings=EditorJsonUtility.ToJson(config);
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(Enemy))Require(AssetDatabase.CopyAsset(Folder+"/Barrack_Masonry_Intact.prefab",Enemy),"Copy enemy visual failed");
            var root=PrefabUtility.LoadPrefabContents(Enemy);
            try
            {
                root.name="Barrack_Masonry_Enemy_C";var old=root.transform.Find("B - masonry barrack");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var oldC=root.transform.Find("C - enemy masonry barrack");if(oldC)UnityEngine.Object.DestroyImmediate(oldC.gameObject);
                var c=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonBattleRoyale/Prefabs/Buildings/SM_Bld_House_01.prefab"),root.scene);
                c.name="C - enemy masonry barrack";c.transform.SetParent(root.transform,false);
                var renderers=c.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
                c.transform.localPosition+=new Vector3(-6.7f,0,0)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                foreach(var collider in c.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                PrefabUtility.SaveAsPrefabAsset(root,Enemy);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(Ruins))Require(AssetDatabase.CopyAsset(Folder+"/Barrack_Masonry_Ruins.prefab",Ruins),"Copy enemy ruins failed");
            root=PrefabUtility.LoadPrefabContents(Ruins);
            try
            {
                root.name="Barrack_Masonry_Enemy_C_Ruins";
                var west=root.transform.Find("West wall remnant");west.localPosition=new Vector3(-11.7f,.7f,-.5f);west.localScale=new Vector3(.5f,1.4f,6);
                var rear=root.transform.Find("Rear wall remnant");rear.localPosition=new Vector3(-8.3f,1.05f,4.7f);rear.localScale=new Vector3(6.5f,2.1f,.5f);
                var front=root.transform.Find("Front wall remnant");front.localPosition=new Vector3(-6.7f,.45f,-4.5f);front.localScale=new Vector3(5.5f,.9f,.55f);
                PrefabUtility.SaveAsPrefabAsset(root,Ruins);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            root=PrefabUtility.LoadPrefabContents(Barrack);
            try
            {
                var model=root.transform.Find("Model");Require(model!=null,"Missing Model wrapper");var player=model.Find("Masonry compound");Require(player!=null,"Missing approved B compound");
                var prior=model.Find("Enemy C compound");if(prior)UnityEngine.Object.DestroyImmediate(prior.gameObject);
                var enemy=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Enemy),root.scene);enemy.name="Enemy C compound";enemy.transform.SetParent(model,false);
                var variants=model.GetComponent<BuildingFactionVisualVariants>()??model.gameObject.AddComponent<BuildingFactionVisualVariants>();
                variants.ConfigureForEditor(player.gameObject,enemy,AssetDatabase.LoadAssetAtPath<GameObject>(Ruins));
                PrefabUtility.SaveAsPrefabAsset(root,Barrack);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();source=AssetDatabase.LoadAssetAtPath<GameObject>(Barrack);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string afterGuid,out long afterId);
            Require(guid==afterGuid&&id==afterId,"Barrack identity changed");Require(settings==EditorJsonUtility.ToJson(config),"Gameplay config changed");
            Validate();Require(active==SceneManager.GetActiveScene()&&dirty==active.isDirty,"Active scene changed");
            Debug.Log("[EnemyBarrackInstall] result=Passed enemy=C player=B identity=Preserved settings=Preserved activeScene=Preserved");
        }
        public static void Validate()
        {
            Require(!EditorApplication.isPlaying,"Use isolated Edit-mode runtime visual validation.");Directory.CreateDirectory(Evidence);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Barrack);var defHelper=new BuildingDefinitionPrefabSystemHelper();defHelper.ConfigureAuthoringMetadataResolvers(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,null);
            var definition=defHelper.CreateDefinition(prefab,"Barracks","",1200,null,null,null,null);var playerRuins=definition.DestroyedVisualPrefab;
            Require(definition.FootprintCells==new Vector2Int(28,15),"Footprint differs");Require(definition.MaxHealth==1200&&definition.CreditsCost==40000&&definition.MaterialsCost==90&&definition.ProductionDurationSeconds==30&&definition.ProductionSlots[0].Quantity==4,"Gameplay differs");
            foreach(var exit in definition.ProductionSpawnLocalPositions)Require(Mathf.Abs(exit.x)>14||Mathf.Abs(exit.z)>7.5f,"Production exit inside footprint");
            foreach(var path in new[]{Enemy,Ruins})
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);Require(BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(asset,out var b),"No bounds");Require(b.min.x>=-14&&b.max.x<=14&&b.min.z>=-7.5f&&b.max.z<=7.5f,"Enemy outside footprint");
                Require(asset.GetComponentsInChildren<Collider>(true).Length==0,"Extra colliders");foreach(var renderer in asset.GetComponentsInChildren<Renderer>(true))foreach(var mat in renderer.sharedMaterials)Require(mat!=null&&mat.shader!=null&&!ShaderUtil.ShaderHasError(mat.shader),"Broken material");
            }
            var scene=EditorSceneManager.NewPreviewScene();var presenter=new BuildingPlacementVisualPresentationSystemHelper();GameObject parent=null;
            using(var world=new World("Enemy Barrack visual validation"))
            try
            {
                parent=new GameObject("Ownership proof");SceneManager.MoveGameObjectToScene(parent,scene);
                var visualSystem=world.GetOrCreateSystemManaged<BuildingVisualSystem>();var factions=world.GetOrCreateSystemManaged<BuildingFactionVisualSystem>();var block=new MaterialPropertyBlock();
                var initializer=new BuildingRuntimeVisualPresentationSystemHelper();var initContext=new BuildingRuntimeVisualPresentationSystemHelper.Context(new Dictionary<int,RuntimeBuildingEntity>(),visualSystem,factions,null,null,block,.12f);
                var owner=new BuildingRuntimeOwnershipCompositionSystemHelper();var ownerContext=new BuildingRuntimeOwnershipCompositionSystemHelper.Context(null,null,block,factions,.12f);
                var instance=presenter.CreateBuildingVisualInstance(definition,parent.transform);var building=new RuntimeBuildingEntity{Instance=instance,Definition=definition,HasOwnerFaction=true,OwnerFactionId=FactionIdentity.EnemyFactionId};
                initializer.InitializeBuildingVisuals(initContext,building);var variants=building.FactionVisualVariants;Require(variants!=null,"Missing selector");AssertVariant(building,true);Capture(scene,instance,Evidence+"/Enemy-Barrack-C.png");
                owner.SetRuntimeBuildingOwnerFaction(ownerContext,building,FactionIdentity.PlayerFactionId);AssertVariant(building,false);Require(building.OwnerDestroyedVisualPrefab==null,"Captured enemy ruin override remains");Capture(scene,instance,Evidence+"/Player-Barrack-B.png");
                owner.SetRuntimeBuildingOwnerFaction(ownerContext,building,FactionIdentity.NeutralFactionId);AssertVariant(building,false);
                owner.SetRuntimeBuildingOwnerFaction(ownerContext,building,null);AssertVariant(building,false);
                owner.SetRuntimeBuildingOwnerFaction(ownerContext,building,(byte)3);AssertVariant(building,true);
                Require(definition.DestroyedVisualPrefab==playerRuins,"Shared definition mutated by ownership");
                var destruction=new BuildingDestroyedVisualPresentationSystemHelper();var context=new BuildingDestroyedVisualPresentationSystemHelper.Context(visualSystem,o=>UnityEngine.Object.DestroyImmediate(o));building.IsDestroyed=true;
                destruction.BeginDestroyedVisual(context,building);Require(building.DestroyedVisualInstance!=null&&building.DestroyedVisualInstance.name!=null,"No enemy ruins");Require(building.AliveVisualRoots.All(t=>!t.gameObject.activeSelf),"Destroyed art still alive");
                Require(building.OwnerDestroyedVisualPrefab==AssetDatabase.LoadAssetAtPath<GameObject>(Ruins),"Wrong enemy destruction prefab");Capture(scene,building.DestroyedVisualInstance,Evidence+"/Enemy-Barrack-C-Destroyed.png");
                owner.SetRuntimeBuildingOwnerFaction(ownerContext,building,FactionIdentity.PlayerFactionId);Require(building.AliveVisualRoots.All(t=>!t.gameObject.activeSelf),"Ownership resurrects destroyed art");
                destruction.CleanupDestroyedVisual(context,building);Require(building.DestroyedVisualInstance==null,"Enemy destruction cleanup failed");
                // A fresh player shares the definition but never inherits the enemy visual/ruins.
                var playerInstance=presenter.CreateBuildingVisualInstance(definition,parent.transform);var player=new RuntimeBuildingEntity{Instance=playerInstance,Definition=definition,HasOwnerFaction=true,OwnerFactionId=FactionIdentity.PlayerFactionId};initializer.InitializeBuildingVisuals(initContext,player);AssertVariant(player,false);Require(player.OwnerDestroyedVisualPrefab==null,"Enemy state leaked into fresh player");
                player.IsDestroyed=true;destruction.BeginDestroyedVisual(context,player);Require(player.DestroyedVisualInstance!=null,"Player destruction missing");destruction.CleanupDestroyedVisual(context,player);
                // Pool a surviving enemy and reassign it to the player.
                var pooledInstance=presenter.CreateBuildingVisualInstance(definition,parent.transform);var pooledEnemy=new RuntimeBuildingEntity{Instance=pooledInstance,Definition=definition,HasOwnerFaction=true,OwnerFactionId=2};initializer.InitializeBuildingVisuals(initContext,pooledEnemy);AssertVariant(pooledEnemy,true);presenter.ReleaseBuildingVisualInstance(pooledInstance);
                var reused=presenter.CreateBuildingVisualInstance(definition,parent.transform);Require(reused==pooledInstance,"Pool path not exercised");var reusedPlayer=new RuntimeBuildingEntity{Instance=reused,Definition=definition,HasOwnerFaction=true,OwnerFactionId=1};initializer.InitializeBuildingVisuals(initContext,reusedPlayer);AssertVariant(reusedPlayer,false);
                Debug.Log("[EnemyBarrackNative] result=Passed player=B enemy=C hostileFaction3=C neutral=B capture=B poolReuse=B sharedDefinition=Unchanged destruction=PerOwner cleanup=Passed noResurrection=Passed footprint=28x15 fullMission=NotClaimed deviceAcceptance=Pending");
            }
            finally{presenter.Dispose();if(parent)UnityEngine.Object.DestroyImmediate(parent);EditorSceneManager.ClosePreviewScene(scene);}
        }
        public static void ValidatePlayerRebuild()
        {
            BarrackMasonryPrefabBuilder.BuildAndValidate();
            Validate();
            Debug.Log("[EnemyBarrackRebuild] result=Passed playerRebuild=EnemyVariantPreserved");
        }
        static void AssertVariant(RuntimeBuildingEntity building,bool enemy)
        {
            var variants=building.FactionVisualVariants;Require(variants.PlayerVisualRoot.activeSelf!=enemy&&variants.EnemyVisualRoot.activeSelf==enemy,"Wrong ownership visual");Require(building.Instance.GetComponentsInChildren<Renderer>().Length==18,"Multiple variants render together");
        }
        static void Capture(Scene scene,GameObject model,string path)
        {
            var cameraGo=new GameObject("Variant proof camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);var c=cameraGo.AddComponent<Camera>();c.scene=scene;c.orthographic=true;c.aspect=1.6f;c.orthographicSize=16;c.nearClipPlane=.01f;c.farClipPlane=300;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.25f,.23f,.2f);BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(model,out var b);var targetCenter=model.transform.TransformPoint(b.center);c.transform.position=targetCenter+new Vector3(1,.85f,-1).normalized*100;c.transform.LookAt(targetCenter);
            var sunGo=new GameObject("Variant proof sun");SceneManager.MoveGameObjectToScene(sunGo,scene);var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.6f;sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.None;
            var rt=new RenderTexture(1600,1000,24){antiAliasing=4};Texture2D image=null;var old=RenderTexture.active;
            try{rt.Create();c.targetTexture=rt;c.Render();RenderTexture.active=rt;image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=old;if(image)UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(sunGo);}
        }
    }
}
#endif
