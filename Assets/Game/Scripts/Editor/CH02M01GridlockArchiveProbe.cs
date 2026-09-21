using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class CH02M01GridlockArchiveProbe
    {
        private const string Active="Warline.Gridlock.ArchiveProbe";
        private static bool prepared,requested,playing,captured,finished;
        private static double started,lastAction,playStarted;
        private static SaveService save;
        private static string beforeProfile;
        private static CampaignMissionRuntimeComponent beforeRuntime;
        static CH02M01GridlockArchiveProbe(){if(SessionState.GetBool(Active,false))Bind();}
        private static void Bind(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Observe;Application.logMessageReceived+=Observe;}
        public static void Run()
        {
            SessionState.SetBool(Active,true);Bind();
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(finished || !EditorApplication.isPlaying)return;
            try
            {
                double now=EditorApplication.timeSinceStartup;if(started==0)started=now;
                if(now-started>240)throw new TimeoutException("Gridlock chapter archive timeout");
                var world=World.DefaultGameObjectInjectionWorld;if(world==null || !world.IsCreated)return;
                var em=world.EntityManager;
                using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));
                using var shells=em.CreateEntityQuery(typeof(UiShellStateComponent));
                if(roots.CalculateEntityCount()!=1 || shells.CalculateEntityCount()!=1)return;
                var root=roots.GetSingletonEntity();var shell=shells.GetSingleton<UiShellStateComponent>();
                if(!prepared)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    save=new SaveService(new JsonSaveRepository(Path.Combine("/private/tmp/warline-gridlock","archive-profile-"+Guid.NewGuid().ToString("N"))));
                    var store=new CampaignMissionProgressStore(save);store.EnsureAvailable("saga.ch02.m01.gridlock");store.MarkChapterOpeningSeen("saga.ch02.m01.gridlock");
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                    GameLocalization.SetLocale("en",false);prepared=true;
                }
                if(!requested)
                {
                    if(now-lastAction<1)return;lastAction=now;
                    if(shell.ActiveRoute!=UIRoute.Campaign || shell.IsTransitionRunning!=0)
                    {UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,UIRoute.Campaign,false);return;}
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var model) || !model.IsValid)return;
                    if(model.SelectedMission.MissionId!="saga.ch02.m01.gridlock")
                    {UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,"saga.ch02.m01.gridlock");return;}
                    var view=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>(FindObjectsInactive.Exclude);
                    if(view?.StoryArchiveButton==null || !view.IsChapterTwo || !view.StoryArchiveButton.IsInteractable())return;
                    beforeProfile=JsonUtility.ToJson(save.LoadProfile());beforeRuntime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                    view.StoryArchiveButton.onClick.Invoke();requested=true;
                    Debug.Log("[GridlockArchive] requested=StoryArchiveButton");return;
                }
                if(!playing)return;
                if(!captured && now-playStarted>3)
                {ScreenCapture.CaptureScreenshot("/private/tmp/warline-gridlock/chapter-archive.png");captured=true;}
                if(!em.HasComponent<GridlockChapterReplayRequest>(root) || em.GetComponentData<GridlockChapterReplayRequest>(root).Pending!=0)return;
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                if(shell.ActiveRoute!=UIRoute.Campaign || runtime.SessionToken!=beforeRuntime.SessionToken ||
                    runtime.AttemptOrdinal!=beforeRuntime.AttemptOrdinal || runtime.Phase!=beforeRuntime.Phase ||
                    beforeProfile!=JsonUtility.ToJson(save.LoadProfile()))
                    throw new InvalidOperationException("Story archive changed mission or profile state");
                Finish(true,"chapter replay completed from Campaign; no launch, progression, or reward mutation");
            }
            catch(Exception error){Debug.LogException(error);Finish(false,error.Message);}
        }
        private static void Observe(string message,string stack,LogType type)
        {
            if(message.StartsWith("[CampaignMissionNarrative] status=started stage=ChapterReplay",StringComparison.Ordinal))
            {playing=true;playStarted=EditorApplication.timeSinceStartup;}
        }
        private static void Finish(bool pass,string detail)
        {
            finished=true;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Observe;
            Debug.Log("[GridlockArchive] result="+(pass?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(pass);
        }
    }
}
