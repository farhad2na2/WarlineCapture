using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Game.Editor
{
    // Editor-only review entry. All gameplay uses the ordinary Campaign screens.
    [InitializeOnLoad]
    public static class CampaignMapMissionReview
    {
        private const string Active="Warline.MapMissionReview.Active";
        private static bool seeded;
        private static string reviewPath;
        private static double readyAt;
        static CampaignMapMissionReview(){if(SessionState.GetBool(Active,false))EditorApplication.update+=Seed;}
        [MenuItem("Game/Campaign/Prepared Maps/Open Seven Mission Review")]
        public static void OpenEnglishReview()
        {
            SessionState.SetBool(Active,true);seeded=false;readyAt=0;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            EditorApplication.update-=Seed;EditorApplication.update+=Seed;EditorApplication.EnterPlaymode();
        }
        private static void Seed()
        {
            if(!EditorApplication.isPlaying)return;
            var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;
            var em=world.EntityManager;using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionProgressStoreReferenceComponent));
            if(roots.CalculateEntityCount()!=1)return;
            if(!seeded)
            {
            string path=Path.GetFullPath("Design/AgentReports/MapVariantMissionRework/HumanReview/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(path);
            var store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(path)));
            for(int i=0;i<18;i++)store.EnsureAvailable(CampaignMissionSequence.IdAt(i));
            em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(roots.GetSingletonEntity()).Store=store;
            GameLocalization.SetLocale("en",false);reviewPath=path;seeded=true;
            }
            if(UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>()==null)
            {UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,UIRoute.Campaign,true);return;}
            if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign))return;
            const uint reviewMask=(1u<<3)|(1u<<6)|(1u<<8)|(1u<<9)|(1u<<15)|(1u<<16)|(1u<<17);
            if((campaign.AvailableMissionMask&reviewMask)!=reviewMask)return;
            if(campaign.SelectedMission.MissionId!=CampaignMissionSequence.IdAt(3))
            {UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,CampaignMissionSequence.IdAt(3));return;}
            if(readyAt==0){EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();readyAt=EditorApplication.timeSinceStartup+1;return;}
            if(EditorApplication.timeSinceStartup<readyAt)return;
            UnityEngine.ScreenCapture.CaptureScreenshot(Path.Combine(reviewPath,"campaign-seven-mission-review.png"));
            SessionState.SetBool(Active,false);EditorApplication.update-=Seed;
            UnityEngine.Debug.Log("[CampaignMapMissionReview] ready=True missions=7 selected=Airlift automation=Stopped isolatedProfile="+reviewPath+" playerAcceptance=Pending deviceAcceptance=Pending");
        }
    }
}
