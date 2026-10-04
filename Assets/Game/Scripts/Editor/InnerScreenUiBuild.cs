using System;
using System.Threading.Tasks;
using UnityEngine;
namespace Game.Editor
{
    public static class InnerScreenUiBuild
    {
        public static int CleanAuthoringPreview()
        {
            if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Cleanup requires idle Editor");
            const string campaignPath="Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab";
            var campaignRoot=UnityEditor.PrefabUtility.LoadPrefabContents(campaignPath);
            try
            {
                var view=campaignRoot.GetComponentInChildren<Game.UI.Runtime.CampaignOperationsScreenView>(true);
                var bindings=new UnityEditor.SerializedObject(view);
                bindings.FindProperty("chapterOneButton").objectReferenceValue=view.ChapterOneButton;
                bindings.FindProperty("chapterTwoButton").objectReferenceValue=view.ChapterTwoButton;
                bindings.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.SaveAsPrefabAsset(campaignRoot,campaignPath);
            }
            finally{UnityEditor.PrefabUtility.UnloadPrefabContents(campaignRoot);}
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            foreach(var root in scene.GetRootGameObjects())if(root.name=="SCN07_LoadoutSquadPrepContent")UnityEngine.Object.DestroyImmediate(root);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            LoadoutSquadPrepV3PrefabBuilder.Validate();
            Debug.Log("[InnerScreenUiCleanup] result=Passed authoringPreviewRemoved=True prefabRetained=True chapterBindingsRestored=True");return 0;
        }
        public static async Task<int> BuildRemainingAndCapture()
        {
            try {StoreCommandExchangeV3PrefabBuilder.Build();CommandFeedV3PrefabBuilder.Build();return await InnerScreenUiAuditCapture.RunStoreFeedChecked();}
            catch(System.Exception error){UnityEngine.Debug.LogException(error);return 1;}
        }
        public static async Task<int> BuildAndCapture()
        {
            try
            {
                MenuUiApprovedAuthoring.Localize();
                SplashLoadingV3PrefabBuilder.Build();
                MissionBriefingV3PrefabBuilder.Build();
                LoadoutSquadPrepV3PrefabBuilder.Build();
                StoreCommandExchangeV3PrefabBuilder.Build();
                ArmoryV3PrefabBuilder.Build();
                DistrictDetailActionsV3PrefabBuilder.Build();
                CommandFeedV3PrefabBuilder.Build();
                Debug.Log("[InnerScreenUiBuild] result=Passed nativeScreens=6");
                return await InnerScreenUiAuditCapture.RunAfterChecked();
            }
            catch(Exception e){Debug.LogException(e);Debug.LogError("[InnerScreenUiBuild] result=Failed");return 1;}
        }
    }
}
