using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class SkirmishPrototypeEditorProbe
    {
        private const string Key="Warline.SkirmishProbe";
        private static int stage;
        private static double next,deadline;
        static SkirmishPrototypeEditorProbe()
        {
            if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;
            if(SessionState.GetBool(Key+".RestorePending",false))EditorApplication.update+=RestoreWhenIdle;
        }
        [Serializable] private sealed class EditorSnapshot
        {
            public UnityEditor.SceneManagement.SceneSetup[] scenes;
            public string profile;
            public Vector2 gameViewSize;
        }
        public static void LaunchInIdleEditor(string isolatedProfileRoot = null)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Editor is not idle.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scene work before QA.");
            if(!string.IsNullOrEmpty(SessionState.GetString(Key+".EditorSnapshot","")))throw new InvalidOperationException("A reversible QA session already exists.");
            var snapshot=new EditorSnapshot{
                scenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup(),
                profile=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"),
                gameViewSize=Handles.GetMainGameViewSize()};
            SessionState.SetString(Key+".EditorSnapshot",JsonUtility.ToJson(snapshot));
            SessionState.SetBool(Key+".SetupOnly",false);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            Launch(isolatedProfileRoot);
        }
        public static void PrepareSetupInIdleEditor()
        {
            LaunchInIdleEditor();
            SessionState.SetBool(Key+".SetupOnly",true);
        }
        public static void RestoreIdleEditor()
        {
            SessionState.SetBool(Key+".RestorePending",true);
            EditorApplication.update-=RestoreWhenIdle;EditorApplication.update+=RestoreWhenIdle;
            if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();
        }
        private static void RestoreWhenIdle()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            var json=SessionState.GetString(Key+".EditorSnapshot","");
            if(!string.IsNullOrEmpty(json))
            {
                var snapshot=JsonUtility.FromJson<EditorSnapshot>(json);
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",snapshot.profile);
                UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(snapshot.scenes);
                if(snapshot.gameViewSize.x>0&&snapshot.gameViewSize.y>0)
                    MainMenuV3PrefabBuilder.SetGameViewResolution((int)snapshot.gameViewSize.x,(int)snapshot.gameViewSize.y);
                SessionState.EraseString(Key+".EditorSnapshot");
            }
            SessionState.SetBool(Key+".RestorePending",false);EditorApplication.update-=RestoreWhenIdle;
            Debug.Log("[SkirmishPrototypeProbe] Restored Editor scene, save root and resolution.");
        }
        public static void Launch(string isolatedProfileRoot = null)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit play mode before launch.");
            System.Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",isolatedProfileRoot ?? "/private/tmp/skirmish-prototype-profile");
            SessionState.SetBool(Key,true); stage=0;next=0;deadline=0;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            double now=EditorApplication.timeSinceStartup;
            if(deadline==0)deadline=now+150;
            if(now>deadline){Finish("Timed out at stage "+stage);return;}
            if(now<next)return;
            try
            {
                if(stage==0)
                {
                    GameLocalization.SetLocale("en",false);
                    if(!UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,UIRoute.QuickCustomSetup,false))return;
                    stage=1;next=now+2;return;
                }
                if(stage==1)
                {
                    var setup=UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>();if(setup==null)return;
                    Directory.CreateDirectory("/private/tmp/skirmish-prototype-evidence");
                    ScreenCapture.CaptureScreenshot("/private/tmp/skirmish-prototype-evidence/setup-en.png");
                    stage=2;next=now+1;return;
                }
                if(stage==2)
                {
                    if(SessionState.GetBool(Key+".SetupOnly",false)){Finish("Setup ready for UI validation");return;}
                    UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>().LaunchMatch();stage=3;next=now+5;return;
                }
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;
                using var query=world.EntityManager.CreateEntityQuery(typeof(SkirmishMatchState));
                if(query.CalculateEntityCount()!=1)return;
                var state=query.GetSingleton<SkirmishMatchState>();
                if(state.Phase!=SkirmishPhase.Playing)return;
                if(!UiShellRuntimeGateway.TryReadShellState(out var shell)||shell.CurrentMode!=UiShellMode.MatchHud||shell.IsTransitionRunning)return;
                if(stage==3){stage=4;next=now+2;return;}
                ScreenCapture.CaptureScreenshot("/private/tmp/skirmish-prototype-evidence/opening-en.png");
                Finish("Playing; seed="+state.Seed+" player="+state.PlayerMainBase+" enemy="+state.EnemyMainBase);
            }
            catch(Exception e){Finish(e.ToString());}
        }
        private static void Finish(string message)
        {SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log("[SkirmishPrototypeProbe] "+message);}
    }
}
