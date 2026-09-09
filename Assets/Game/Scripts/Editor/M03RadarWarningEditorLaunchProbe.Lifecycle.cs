using System;
using System.Collections.Generic;
using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string LifecycleKey="Warline.M03.Probe.Lifecycle";
        private static int lifecycleStep,lifecycleCycle;
        private static double lifecycleDeadline;
        private static CampaignMissionRuntimeComponent lifecycleAttempt;
        private static readonly List<Entity> lifecycleMembers=new(20);
        private static MissionFieldGuideView lifecycleGuide;
        private static int lifecycleProducerRuntimeId;
        private static bool lifecyclePaidConstruction,lifecycleConstructionStarted;
        private static readonly List<int> lifecyclePaidBuildings=new();
        private static readonly List<Entity> lifecycleProducedUnits=new();

        public static void RunLifecycleValidation()=>StartLifecycleValidation(false);
        public static void RunPaidLifecycleValidation()=>StartLifecycleValidation(true);
        private static void StartLifecycleValidation(bool paidConstruction)=>RunChecked(()=>
        {
            M03RadarWarningConfigBuilder.Build(); M03RadarWarningUiBuilder.Build();
            lifecycleStep=lifecycleCycle=0; lifecycleMembers.Clear(); lifecycleGuide=null;
            lifecyclePaidConstruction=paidConstruction; lifecycleConstructionStarted=false;
            lifecyclePaidBuildings.Clear(); lifecycleProducedUnits.Clear();
            lifecycleDeadline=EditorApplication.timeSinceStartup+120;
            SessionState.SetBool(LifecycleKey,true); Run();
        });

        private static bool AdvanceLifecycleValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(LifecycleKey,false)) return false;
            if(EditorApplication.timeSinceStartup>lifecycleDeadline)
                throw new TimeoutException($"Lifecycle cycle {lifecycleCycle+1}, step {lifecycleStep} did not complete.");
            if(runtime.Phase==MissionPhaseKind.FindSquad)
                UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.SkipCameraTour);
            switch(lifecycleStep)
            {
                case 0:
                    if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<1000) return true;
                    if(lifecyclePaidConstruction && lifecycleCycle==0)
                    {
                        if(!lifecycleConstructionStarted)
                        {
                            ValidateFreshLifecycleAttempt(em,root,in runtime,in facts);
                            lifecycleConstructionStarted=true; constructionStep=constructionAt=nextConstructionLog=0;
                            constructionVerified=false; originalBuildings.Clear(); commandedReinforcements.Clear();
                            SessionState.SetBool(ConstructionKey,true);
                        }
                        PlayConstructionDefense(em,root,in runtime,in facts);
                        if(!constructionVerified) return true;
                        SessionState.SetBool(ConstructionKey,false);
                        var built=UnityEngine.Object.FindAnyObjectByType<Game.Composition.MatchSceneView>().MatchBootstrap.BuildingUiQueryContext.RuntimeBuildings;
                        foreach(var pair in built) if(!originalBuildings.Contains(pair.Key)) lifecyclePaidBuildings.Add(pair.Key);
                        lifecycleProducedUnits.AddRange(commandedReinforcements);
                        if(lifecyclePaidBuildings.Count!=2 || lifecycleProducedUnits.Count!=4)
                            throw new InvalidOperationException("Paid cleanup fixture requires exactly two actual buildings and four produced rifles.");
                    }
                    else ValidateFreshLifecycleAttempt(em,root,in runtime,in facts);
                    lifecycleAttempt=runtime; lifecycleMembers.Clear();
                    lifecycleProducerRuntimeId=ReadInitialProducerRuntimeId(em,root);
                    Debug.Log($"[M03LifecycleProbe] attempt={runtime.AttemptOrdinal} buildingBaseline={em.GetComponentData<CampaignMissionDefenseStateComponent>(root).RuntimeBuildingBaselineId} producer={lifecycleProducerRuntimeId}");
                    foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true)) lifecycleMembers.Add(member.Entity);
                    if(!UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide))
                        throw new InvalidOperationException("Lifecycle guide entry was rejected.");
                    NextLifecycle(); break;
                case 1:
                    lifecycleGuide=UnityEngine.Object.FindAnyObjectByType<MissionFieldGuideView>();
                    if(lifecycleGuide==null || Time.timeScale!=0) return true;
                    lifecycleGuide.ShowClasses(); NextLifecycle(); break;
                case 2:
                    if(lifecycleGuide.ClassLoadPending) return true;
                    if(lifecycleGuide.ClassLoadFailed || lifecycleGuide.ResidentClassCount!=1 || lifecycleGuide.VisibleClassCount!=57)
                        throw new InvalidOperationException("Lifecycle class reference failed to load exactly one class.");
                    UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide); NextLifecycle(); break;
                case 3:
                    if(UnityEngine.Object.FindAnyObjectByType<MissionFieldGuideView>()!=null || Time.timeScale==0) return true;
                    if(!ReferenceEquals(lifecycleGuide,null) && lifecycleGuide.ResidentClassCount!=0)
                        throw new InvalidOperationException("Closed guide retained its class asset handle.");
                    if(!UiShellRuntimeGateway.TryRestartCurrentMission() || UiShellRuntimeGateway.TryRestartCurrentMission())
                        throw new InvalidOperationException("Lifecycle restart acceptance/duplicate rejection failed.");
                    capturedBrief=false; NextLifecycle(); break;
                case 4:
                    if(runtime.AttemptOrdinal==lifecycleAttempt.AttemptOrdinal || runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<1000) return true;
                    if(runtime.AttemptOrdinal!=lifecycleAttempt.AttemptOrdinal+1 || !runtime.SessionToken.Equals(lifecycleAttempt.SessionToken))
                        throw new InvalidOperationException("Restart did not preserve session and increment the attempt once.");
                    foreach(Entity member in lifecycleMembers)
                        if(em.Exists(member)) throw new InvalidOperationException("Restart retained a previous mission unit: "+member);
                    var match=UnityEngine.Object.FindAnyObjectByType<Game.Composition.MatchSceneView>();
                    if(match==null) throw new InvalidOperationException("Restart lost the live match owner.");
                    if(match.MatchBootstrap.BuildingUiQueryContext.RuntimeBuildings.ContainsKey(lifecycleProducerRuntimeId))
                        throw new InvalidOperationException("Restart retained the previous attempt's Barracks: "+lifecycleProducerRuntimeId);
                    foreach(int id in lifecyclePaidBuildings)
                        if(match.MatchBootstrap.BuildingUiQueryContext.RuntimeBuildings.ContainsKey(id))
                            throw new InvalidOperationException("Restart retained a paid building: "+id);
                    foreach(Entity unit in lifecycleProducedUnits)
                        if(em.Exists(unit)) throw new InvalidOperationException("Restart retained a produced rifle: "+unit);
                    if(lifecyclePaidBuildings.Count>0)
                        Debug.Log("[M03LifecycleProbe] paid cleanup=Passed tower=removed barrier=removed producedRifles=4 removed");
                    lifecyclePaidBuildings.Clear(); lifecycleProducedUnits.Clear();
                    ValidateFreshLifecycleAttempt(em,root,in runtime,in facts);
                    lifecycleMembers.Clear();
                    foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true)) lifecycleMembers.Add(member.Entity);
                    UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.Pause); NextLifecycle(); break;
                case 5:
                    var pause=UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>();
                    if(pause==null || !pause.ExitButton.interactable) return true;
                    pause.ExitButton.onClick.Invoke(); NextLifecycle(); break;
                case 6:
                    if(!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.ActiveRoute!=UIRoute.MainMenu || shell.IsTransitionRunning ||
                        shell.CurrentMode!=UiShellMode.MainMenu || shell.Phase!=UiShellTransitionPhase.MenuReady) return true;
                    AssertNoM3State(em,root);
                    foreach(Entity member in lifecycleMembers)
                        if(em.Exists(member)) throw new InvalidOperationException("Exit retained a restarted M3 member: "+member);
                    if(UiShellRuntimeGateway.TryReadMissionDefense(out _) || UnityEngine.Object.FindAnyObjectByType<MissionFieldGuideView>()!=null ||
                        UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>()!=null || Time.timeScale==0)
                        throw new InvalidOperationException("Menu retained M3 UI or a paused global clock.");
                    lifecycleCycle++;
                    Debug.Log($"[M03LifecycleProbe] cycle={lifecycleCycle}/10 guide=57/1 restart=clean menu=clean");
                    if(lifecycleCycle==10)
                    {Complete(true,"10 real menu -> M3 -> class guide -> restart -> Pause Exit -> menu cycles; duplicate restarts blocked; old units removed; fresh budget/Ping; guide handles and global clock restored"); return true;}
                    deployed=capturedBrief=capturedHud=false; lifecycleStep=0; lifecycleDeadline=EditorApplication.timeSinceStartup+120;
                    break;
            }
            return true;
        }
        private static void NextLifecycle() {lifecycleStep++; lifecycleDeadline=EditorApplication.timeSinceStartup+90;}
        private static int ReadInitialProducerRuntimeId(EntityManager em,Entity root)
        {
            int request=em.GetComponentData<CampaignMissionDefenseStateComponent>(root).InitialProducerRequestId;
            using var q=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingRuntimeSpawnRequest));
            var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(q.GetSingletonEntity(),true);
            foreach(var item in requests) if(item.RequestId==request && item.Status==BuildingRuntimeSpawnRequest.Succeeded && item.BuildingRuntimeId>0) return item.BuildingRuntimeId;
            throw new InvalidOperationException("The current M3 Barracks has no successful runtime request.");
        }
        private static void ValidateFreshLifecycleAttempt(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            var ping=em.GetComponentData<RadarPingState>(root);
            if(runtime.Outcome!=MissionOutcomeKind.None || facts.HostileDefeatedCount!=0 || facts.CoreBreached!=0 || facts.HostileRosterIntegrityFault!=0 ||
                em.GetBuffer<CampaignMissionDefenseMember>(root).Length!=20 || ping.Charges!=2 || ping.PendingRequestId!=0 ||
                !ping.SessionToken.Equals(runtime.SessionToken) || ping.AttemptOrdinal!=runtime.AttemptOrdinal || ping.SourceVersion!=runtime.SourceVersion)
                throw new InvalidOperationException("Lifecycle attempt retained stale roster, outcome or Ping state.");
            ValidateStartingBudget(em);
        }
    }
}
