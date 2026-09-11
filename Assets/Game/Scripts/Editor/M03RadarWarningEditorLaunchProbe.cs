using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string ActiveKey = "Warline.M03.LaunchProbe.Active";
        private static string Output => SessionState.GetString("Warline.M03.ReadinessOutput", "/private/tmp/warline-m03-editor-probe");
        private static double started, lastLog, lastClick;
        private static bool prepared, deployed, capturedBrief, capturedHud, finished;
        private static int hudFrame;
        private static string runtimeFailure;
        private static World probeRootWorld;
        private static EntityQuery probeRoots;
        private static void RunChecked(Action launch)
        {
            try {launch();}
            catch(Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[M03RadarWarningEditorLaunchProbe] result=Failed startup: "+exception.Message);
                EditorApplication.Exit(1);
            }
        }
        static M03RadarWarningEditorLaunchProbe()
        {
            if(SessionState.GetBool(ActiveKey,false)) { EditorApplication.update += Tick; Application.logMessageReceived += ObserveError; }
        }
        public static void Run()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Launch probe needs its own wrapper Editor in Edit Mode.");
            Directory.CreateDirectory(Output);
            SessionState.SetBool(ActiveKey,true);
            prepared=deployed=capturedBrief=capturedHud=finished=false; hudFrame=0; runtimeFailure=null;
            ResetCombatDiagnostics();
            started=EditorApplication.timeSinceStartup;
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            // Font/preview refreshes must not hot-rebake the map during an attempt.
            // Asset import and compilation run normally before this Play Mode probe.
            AssetDatabase.DisallowAutoRefresh();
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Application.logMessageReceived -= ObserveError; Application.logMessageReceived += ObserveError;
            EditorApplication.EnterPlaymode();
        }
        public static void BuildDataAndRun()
        {
            M03RadarWarningConfigBuilder.Build();
            Run();
        }
        private static void Tick()
        {
            if(finished) return;
            try
            {
                if(!EditorApplication.isPlaying) return;
                if(runtimeFailure!=null) { Complete(false,runtimeFailure); return; }
                if(started==0) started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>ProbeTimeoutSeconds) throw new TimeoutException("M03 Editor probe exceeded its deadline.");
                World world=World.DefaultGameObjectInjectionWorld;
                if(world==null || !world.IsCreated) return;
                EntityManager em=world.EntityManager;
                if(probeRootWorld!=world)
                {
                    if(probeRootWorld!=null && probeRootWorld.IsCreated) probeRoots.Dispose();
                    probeRootWorld=world;
                    probeRoots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));
                }
                if(probeRoots.CalculateEntityCount()!=1) return;
                Entity root=probeRoots.GetSingletonEntity();
                if(!prepared)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root)) return;
                    string savePath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"warline-m03-launch-probe",Guid.NewGuid().ToString("N"));
                    probeSavePath=savePath;
                    var store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(savePath)));
                    store.EnsureAvailable(M03RadarWarningConfigBuilder.MissionId);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                    PrepareEveryEntryStore(store);
                    prepared=true; return;
                }
                if(AdvanceCrossMissionIsolation(em,root)) return;
                if(!deployed)
                {
                    if(PrepareEveryEntryCampaign()) return;
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel campaign) || !campaign.IsValid) return;
                    if(campaign.SelectedMission.MissionId!=M03RadarWarningConfigBuilder.MissionId)
                    { UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,M03RadarWarningConfigBuilder.MissionId); return; }
                    deployed=UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy,M03RadarWarningConfigBuilder.MissionId);
                    return;
                }
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                MissionMotionEditorAudit.Sample(em,root,in runtime,in facts);
                if(ConvoyProbeActive && !PerformanceActive) LogCombatDamage(em);
                if(!PerformanceActive && EditorApplication.timeSinceStartup-lastLog>5)
                {
                    lastLog=EditorApplication.timeSinceStartup;
                    var defense=em.HasComponent<CampaignMissionDefenseStateComponent>(root) ? em.GetComponentData<CampaignMissionDefenseStateComponent>(root) : default;
                    var opening=em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) ? em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root) : default;
                    string state=$"mission={runtime.MissionId} phase={runtime.Phase} elapsed={facts.ElapsedMilliseconds} spawned={facts.CommandSquadSpawned} brief={facts.InteractiveBriefCompleted} post={facts.ForwardPostBound} hostiles={facts.HostileTotalCount} integrity={facts.HostileRosterIntegrityFault} producer={defense.InitialProducerReady} request={defense.InitialProducerRequestId} opening={opening.Stage}/{opening.ElapsedMilliseconds}";
                    File.AppendAllText(Output+"/state.txt",state+"\n"); Debug.Log("[M03LaunchProbe] "+state);
                    LogDefenseMembers(em,root);
                }
                if(runtime.Outcome!=MissionOutcomeKind.None && !ConvoyProbeActive) throw new InvalidOperationException("Mission settled during opening: "+runtime.Outcome);
                if(!SessionState.GetBool(ComicKey,false) && (!PerformanceActive || runtime.Phase<MissionPhaseKind.Engage) && (!SessionState.GetBool(ResultKey,false) || runtime.Phase<MissionPhaseKind.SecureCorridor)) SkipNarrative();
                if(AdvanceEveryEntryValidation(em,root,in runtime,in facts)) return;
                if(AdvanceHudRoadValidation(em,root,in runtime,in facts)) return;
                if(AdvanceComicValidation(em)) return;
                if(AdvanceSaveRecoveryValidation(em,root,in runtime,in facts)) return;
                if(AdvanceGuidanceJourney(em,root,in runtime,in facts)) return;
                if(AdvanceResultValidation(em,root,in runtime,in facts)) return;
                if(AdvanceCameraValidation(em,root,in runtime,in facts)) return;
                if(AdvanceUiValidation(em,root,in runtime,in facts)) return;
                if(AdvanceCommandValidation(em,root,in runtime,in facts)) return;
                if(AdvanceLifecycleValidation(em,root,in runtime,in facts)) return;
                if(AdvancePerformanceValidation(em,root,in runtime,in facts)) return;
                if(AdvanceAllocationValidation(em,root,in runtime,in facts)) return;
                if(AdvanceRoadBarrierValidation(em,root,in runtime,in facts)) return;
                if(ConvoyProbeActive && AdvanceConvoyProbe(em,root,in runtime,in facts)) return;
                if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<5000) return;
                if(facts.HostileRosterIntegrityFault!=0 || facts.ForwardPostBound==0 || facts.HostileTotalCount!=7 ||
                    !em.HasBuffer<CampaignMissionDefenseMember>(root) || em.GetBuffer<CampaignMissionDefenseMember>(root).Length!=20)
                    throw new InvalidOperationException("M3 launch roster/post mismatch.");
                if(!capturedHud)
                { ValidateStartingBudget(em); ScreenCapture.CaptureScreenshot(Output+"/m03_ready_hud.png"); capturedHud=true; hudFrame=Time.frameCount; return; }
                if(Time.frameCount-hudFrame<4) return;
                Complete(true,"canonical mission; 20 members; physical post bound; completed Barracks; camera returned; simulation active");
            }
            catch(Exception exception) { Debug.LogException(exception); Complete(false,exception.Message); }
        }
        private static void SkipNarrative()
        {
            NarrativeSequenceView narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
            if(narrative==null || !Visible(narrative,"rootGroup")) return;
            if(!capturedBrief) { ScreenCapture.CaptureScreenshot(Output+"/m03_brief.png"); capturedBrief=true; lastClick=EditorApplication.timeSinceStartup; return; }
            if(EditorApplication.timeSinceStartup-lastClick<1) return;
            NarrativeSkipConfirmationView confirmation=narrative.SkipConfirmationView;
            bool confirming=confirmation!=null && Visible(confirmation,"group");
            object owner=confirming ? (object)confirmation : narrative.PlaybackControlsView;
            if(owner==null) return;
            Button button=owner.GetType().GetField(confirming ? "confirmButton" : "skipButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as Button;
            if(button==null || !button.isActiveAndEnabled || !button.interactable) return;
            button.onClick.Invoke(); lastClick=EditorApplication.timeSinceStartup;
            Debug.Log("[M03LaunchProbe] clicked "+button.name);
        }
        private static bool Visible(object owner,string field)
        {
            CanvasGroup group=owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as CanvasGroup;
            return group!=null && group.alpha>0.9f && group.gameObject.activeInHierarchy;
        }
        private static void Complete(bool passed,string detail)
        {
            SessionState.SetBool(HudRoadKey,false);
            SessionState.SetBool(CrossMissionKey,false);
            StopComicValidation();
            StopGuidanceJourney();
            StopSaveRecoveryValidation();
            StopAllocationAttribution();
            ReleaseRoadMouse(); SessionState.SetBool(RoadBarrierKey,false);
            StopPerformanceRecorders(); SessionState.SetBool(PerformanceKey,false);
            SessionState.SetBool(ComicKey,false);
            SessionState.SetBool(CommandsKey,false);
            if(probeRootWorld!=null && probeRootWorld.IsCreated) probeRoots.Dispose(); probeRootWorld=null;
            Time.timeScale=1f;
            RestoreEveryEntrySettings();
            RestoreUiSettings();
            if(SessionState.GetBool(UiKey,false) && originalLocale!=null) Game.Configs.GameLocalization.SetLocale(originalLocale,false);
            SessionState.SetBool(UiKey,false);
            SessionState.SetBool(InspectKey, false);
            SessionState.SetBool(ConvoyKey, false);
            SessionState.SetBool(RifleKey, false);
            SessionState.SetBool(ConstructionKey,false);
            SessionState.SetBool(CameraKey,false);
            SessionState.SetBool(ResultKey,false);
            SessionState.SetBool(LifecycleKey,false);
            SessionState.SetBool(RecoveryKey,false);
            finished=true; SessionState.SetBool(ActiveKey,false); EditorApplication.update-=Tick;
            Application.logMessageReceived-=ObserveError;
            string marker=$"[M03RadarWarningEditorLaunchProbe] result={(passed ? "Passed" : "Failed")} {detail}";
            File.AppendAllText(Output+"/state.txt",marker+"\n"); Debug.Log(marker);
            MissionEditorValidationExit.Complete(passed);
        }
        private static void ObserveError(string message,string stack,LogType type)
        {
            if(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,type))
            {Debug.LogWarning("[M03LaunchProbe] Editor cloud token exchange failed; original exception retained in log, separate from local mission QA.");return;}
            if(type==LogType.Exception || type==LogType.Assert) runtimeFailure=message;
        }
    }
}
