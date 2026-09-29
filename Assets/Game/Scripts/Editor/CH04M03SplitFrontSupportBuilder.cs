using System;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class CH04M03SplitFrontSupportBuilder
    {
        public const string ContextPath="Assets/Game/Configs/Support/CH04M03_SplitFront_SupportContext.asset";
        public static void Build()
        {
            var config=AssetDatabase.LoadAssetAtPath<SupportMissionContextConfig>(ContextPath);
            if(config==null){config=ScriptableObject.CreateInstance<SupportMissionContextConfig>();AssetDatabase.CreateAsset(config,ContextPath);}
            config.MissionId=CampaignMissionSequence.SplitFront;config.OperationMapId=CH04M03SplitFrontConfigBuilder.MapId;
            config.MissionSourceVersion=1;config.Revision=1;config.GroundMin=new Vector2(590,410);config.GroundMax=new Vector2(845,490);
            config.RouteAuthored=false;config.PopulationCeiling=0;
            config.Regions=new[]{
                new SupportAuthoredGroundRegion{Min=config.GroundMin,Max=config.GroundMax,Visible=true},
                new SupportAuthoredGroundRegion{Min=new Vector2(689,450),Max=new Vector2(725,486),Visible=true,Protected=true}};
            if(!config.TryValidate(out var error))throw new InvalidOperationException(error);EditorUtility.SetDirty(config);
            var catalog=AssetDatabase.LoadAssetAtPath<SupportAbilityCatalogConfig>(SupportAbilityCatalogBuilder.CatalogPath);
            for(int i=0;i<catalog.Abilities.Length;i++)if(catalog.Abilities[i].Kind==SupportAbilityKind.Smoke)
            {var smoke=catalog.Abilities[i];smoke.ProductionReady=true;catalog.Abilities[i]=smoke;}
            EditorUtility.SetDirty(catalog);
            var match=EditorSceneManager.OpenScene("Assets/Game/Scenes/Match.unity",OpenSceneMode.Additive);
            try
            {
                var sub=Array.Find(match.GetRootGameObjects(),x=>x.GetComponent<Unity.Scenes.SubScene>()!=null)?.GetComponent<Unity.Scenes.SubScene>();
                if(sub==null)throw new InvalidOperationException("Match runtime SubScene missing");
                string path=AssetDatabase.GetAssetPath(sub.SceneAsset);
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    SupportMissionContextAuthoring authoring=null;
                    foreach(var root in scene.GetRootGameObjects())foreach(var candidate in root.GetComponentsInChildren<SupportMissionContextAuthoring>(true))
                        if(candidate.Config!=null&&candidate.Config.MissionId==config.MissionId)authoring=candidate;
                    if(authoring==null){var root=new GameObject("Split Front Support Context");SceneManager.MoveGameObjectToScene(root,scene);authoring=root.AddComponent<SupportMissionContextAuthoring>();}
                    authoring.Config=config;EditorUtility.SetDirty(authoring);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                finally{EditorSceneManager.CloseScene(scene,true);}
            }
            finally{EditorSceneManager.CloseScene(match,true);}
            Debug.Log("[SplitFrontSupportContext] result=Passed scope=mission,map,version protected=civilians route=none optional=Smoke");
        }
    }
}
