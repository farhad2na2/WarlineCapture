using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    /// <summary>Developmental Watch probe. Scripted preparation; NOT public-entry acceptance evidence.</summary>
    [InitializeOnLoad]
    public static partial class CH02M01GridlockWatchProbe
    {
        private const string Active="Warline.Gridlock.WatchProbe";
        private const string Output="/private/tmp/warline-gridlock";
        private static bool prepared,deployed,finished,watchStarted;
        private static AriaTouchInputUiSystemHelper preparationTouch;
        private static double started,lastLog,lastClick;
        private static float3 departure;
        private static bool observedDeparture;
        private static float traveled;
        private static int step;
        static CH02M01GridlockWatchProbe(){if(SessionState.GetBool(Active,false))EditorApplication.update+=Tick;}
        public static void Run()
        {
            SessionState.SetBool(ManualModeKey,false);
            Begin();
        }
        private static void Begin()
        {
            Directory.CreateDirectory(Output);SessionState.SetBool(Active,true);prepared=deployed=finished=watchStarted=false;observedDeparture=false;traveled=0;step=0;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);started=EditorApplication.timeSinceStartup;
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(finished || !EditorApplication.isPlaying) return;
            try
            {
                if(started==0)started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>(ManualMode?780:420))throw new TimeoutException("Gridlock input probe timeout");
                var world=World.DefaultGameObjectInjectionWorld;if(world==null || !world.IsCreated)return;var em=world.EntityManager;
                using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;
                var root=roots.GetSingletonEntity();
                if(!prepared)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    var store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"probe-profile-"+Guid.NewGuid().ToString("N")))));
                    store.EnsureAvailable(CampaignMissionSequence.Gridlock);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                    GameLocalization.SetLocale("en",false);prepared=true;return;
                }
                if(!deployed)
                {
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign)||!campaign.IsValid)return;
                    if(campaign.SelectedMission.MissionId!=CampaignMissionSequence.Gridlock){UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,CampaignMissionSequence.Gridlock);return;}
                    deployed=UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy,CampaignMissionSequence.Gridlock);return;
                }
                if (!watchStarted) SkipStoryForMechanicsProbe();
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                var g=em.HasComponent<CampaignMissionGridlockState>(root)?em.GetComponentData<CampaignMissionGridlockState>(root):default;
                if(EditorApplication.timeSinceStartup-lastLog>5)
                {
                    lastLog=EditorApplication.timeSinceStartup;
                    var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                    var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                    step=guidance.GuidanceId-75000;
                    using var observations=em.CreateEntityQuery(typeof(AriaPlayObservationComponent));
                    if(observations.CalculateEntityCount()==1)
                    {
                        var observation=observations.GetSingleton<AriaPlayObservationComponent>();
                        Debug.Log($"[GridlockWatch] observation={observation.Kind} drag={observation.Drag} from={observation.Position} to={observation.DragEnd} target={observation.TargetId}");
                    }
                    Debug.Log($"[GridlockWatch] watch={UiShellRuntimeGateway.ReadAriaPlay().Phase} actions={UiShellRuntimeGateway.ReadAriaPlay().Actions} step={step} phase={runtime.Phase} ready={g.Ready} failure={g.Failure} clock={g.ElapsedMilliseconds} rifles={g.LivingRifles} workers={g.LivingWorkers} fadi={g.LivingFadi} hostiles={facts.HostileDefeatedCount}/{facts.HostileTotalCount} connected={g.RouteConnected} arrived={g.VehicleArrived} hold={g.HoldMilliseconds} travel={traveled:F1}");
                }
                if(g.Failure!=GridlockFailure.None)
                {
                    Debug.Log($"[GridlockWatch] failureDetail ready={g.Ready} preparation={g.PreparationMilliseconds} clock={g.ElapsedMilliseconds}");
                    foreach(var member in em.GetBuffer<CampaignMissionGridlockMember>(root,true))
                        Debug.Log($"[GridlockWatch] member={member.Kind} exists={em.Exists(member.Entity)} health={Hp(em,member.Entity)} initialized={member.HealthInitialized} dead={member.Dead}");
                    foreach(var siteState in em.GetBuffer<CampaignMissionGridlockWorkSite>(root,true))
                        Debug.Log($"[GridlockWatch] site={siteState.Center} request={siteState.SpawnRequestId} building={siteState.BuildingRuntimeId} initialized={siteState.Initialized} work={siteState.WorkMilliseconds} clear={siteState.ClearRequested} complete={siteState.Complete}");
                    using var boundaries=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingRuntimeSpawnRequest));
                    if(boundaries.CalculateEntityCount()==1)
                        foreach(var spawn in em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaries.GetSingletonEntity(),true))
                            Debug.Log($"[GridlockWatch] spawn={spawn.BuildingId} status={spawn.Status} resultCode={spawn.ResultCode} origin={spawn.PreferredOrigin} actualFootprint={spawn.ActualFootprint}");
                    throw new InvalidOperationException("Gridlock failure "+g.Failure);
                }
                if(em.Exists(g.Vehicle) && em.HasComponent<LocalTransform>(g.Vehicle))
                {
                    var position=em.GetComponentData<LocalTransform>(g.Vehicle).Position;
                    if(!observedDeparture){departure=position;observedDeparture=true;}traveled=math.max(traveled,math.distance(position,departure));
                }
                var watch = UiShellRuntimeGateway.ReadAriaPlay();
                if (watch.Active && !watchStarted)
                {
                    watchStarted = true;
                    preparationTouch?.Dispose(); preparationTouch = null;
                    Debug.Log("[GridlockWatch] started=real-confirm-touch observer=read-only");
                }
                if (watchStarted && !watch.Active && runtime.Outcome == MissionOutcomeKind.None)
                {
                    using var sessions=em.CreateEntityQuery(typeof(AriaPlaySessionComponent));
                    byte reason=sessions.CalculateEntityCount()==1?sessions.GetSingleton<AriaPlaySessionComponent>().StopReason:(byte)255;
                    foreach(var member in em.GetBuffer<CampaignMissionGridlockMember>(root,true))
                        if(member.Kind<=GridlockMemberKind.ReliefVehicle && em.Exists(member.Entity))
                            Debug.Log($"[GridlockWatch] stoppedMember={member.Kind} hp={Hp(em,member.Entity)} selected={em.HasComponent<SelectedUnitTag>(member.Entity)} position={(em.HasComponent<LocalTransform>(member.Entity)?em.GetComponentData<LocalTransform>(member.Entity).Position:default)}");
                    throw new InvalidOperationException($"Watch stopped before mission outcome: {watch.Phase}, reason={reason}, focused={Application.isFocused}");
                }
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    if((!watchStarted && !ManualMode) || traveled<120 || g.Delivered==0 || g.HoldMilliseconds<20000)throw new InvalidOperationException("Victory without normal input and actual traversal");
                    ScreenCapture.CaptureScreenshot(Output+"/developmental-watch.png");
                    Complete(true,"actualTravel="+traveled.ToString("F1")+(ManualMode?" scope=manual-touch-feasibility publicEntry=NotRun aria=NotRun":" scope=developmental-watch publicEntry=NotRun manual=NotRun"));return;
                }
                if(ManualMode){TickManual(runtime.Phase,g.Ready);return;}
                if (watchStarted) return; // No commands, camera moves or input after real Start.
                if (runtime.Phase != MissionPhaseKind.Engage || g.Ready == 0) return;
                if (preparationTouch == null)
                {
                    var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
                    gameView.Focus();
                    preparationTouch = new AriaTouchInputUiSystemHelper();
                    if (!preparationTouch.Start()) throw new InvalidOperationException("Cannot start preparation touch");
                }
                preparationTouch.Tick(Time.unscaledTime);
                if (preparationTouch.IsBusy || EditorApplication.timeSinceStartup-lastClick < 1.2) return;
                var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
                Button next=null;
                foreach(var button in buttons)
                    if(button.name=="ConfirmWatchAria" && button.IsActive() && button.IsInteractable()) {next=button;break;}
                if(next==null)
                    foreach(var button in buttons)
                        if(button.name=="WatchAriaPlay" && button.IsActive() && button.IsInteractable()) {next=button;break;}
                if(next==null)return;
                var rect=(RectTransform)next.transform;
                var canvas=next.GetComponentInParent<Canvas>();
                var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
                if(preparationTouch.TryGesture(point,point,.15f,0,Time.unscaledTime))
                {lastClick=EditorApplication.timeSinceStartup;Debug.Log("[GridlockWatch] preparationTouch="+next.name+" position="+point);}

            }
            catch(Exception e){Debug.LogException(e);Complete(false,e.Message);}
        }
        private static int Hp(EntityManager em,Entity entity)=>em.Exists(entity)&&em.HasComponent<UnitHealth>(entity)?em.GetComponentData<UnitHealth>(entity).Current:0;
        private static void SkipStoryForMechanicsProbe()
        {
            var view=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
            if(view==null || !Visible(view,"rootGroup") || EditorApplication.timeSinceStartup-lastClick<.7)return;
            var confirm=view.SkipConfirmationView;bool confirming=confirm!=null&&Visible(confirm,"group");object owner=confirming?(object)confirm:view.PlaybackControlsView;
            var button=owner?.GetType().GetField(confirming?"confirmButton":"skipButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner)as Button;
            if(button==null||!button.isActiveAndEnabled||!button.interactable)return;button.onClick.Invoke();lastClick=EditorApplication.timeSinceStartup;
        }
        private static bool Visible(object owner,string field){var group=owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner)as CanvasGroup;return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static void Complete(bool pass,string detail)
        {
            if(finished)return;preparationTouch?.Dispose();preparationTouch=null;finished=true;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;
            ScreenCapture.CaptureScreenshot(Output+"/developmental-watch-last.png");
            Debug.Log("[GridlockWatch] result="+(pass?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(pass);
        }
    }
}
