using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string CrossMissionKey = "Warline.M03.Probe.CrossMission";
        private static readonly string[] crossMissions = {
            "saga.ch01.m01.first_contact", "saga.ch01.m02.establish_base", "saga.ch01.m03.radar_warning" };
        private static bool CrossMissionActive => SessionState.GetBool(CrossMissionKey, false);
        private static int crossMission, crossStep;
        private static double crossDeadline;
        private static readonly List<Entity> crossOldMembers = new();
        public static void RunCrossMissionIsolation() => RunChecked(() =>
        {
            M03RadarWarningConfigBuilder.Build(); M03RadarWarningUiBuilder.Build();
            crossMission = crossStep = 0; crossOldMembers.Clear();
            crossDeadline = EditorApplication.timeSinceStartup + 180;
            SessionState.SetBool(CrossMissionKey, true); Run();
        });
        private static bool AdvanceCrossMissionIsolation(EntityManager em, Entity root)
        {
            if (!CrossMissionActive) return false;
            if (EditorApplication.timeSinceStartup > crossDeadline)
                throw new TimeoutException($"Cross-mission entry={crossMission} step={crossStep}");
            if (!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.IsTransitionRunning) return true;
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            switch (crossStep)
            {
                case 0:
                    if(shell.CurrentMode!=UiShellMode.MainMenu || shell.Phase!=UiShellTransitionPhase.MenuReady) return true;
                    if (crossMission < 3)
                    {
                        var store = em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store;
                        store.EnsureAvailable(crossMissions[crossMission]);
                        if (!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign) || !campaign.IsValid) return true;
                        if (campaign.SelectedMission.MissionId != crossMissions[crossMission])
                        {UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select, crossMissions[crossMission]); return true;}
                        if (!UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy, crossMissions[crossMission])) return true;
                        CrossNext();
                    }
                    else
                    {
                        UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute, UIRoute.QuickCustomSetup, true);
                        crossStep = 5; crossDeadline = EditorApplication.timeSinceStartup + 90;
                    }
                    break;
                case 1:
                    SkipNarrative();
                    if (shell.ActiveRoute != UIRoute.Match) return true;
                    if (UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) && restrictions.CinematicInteractionLocked) return true;
                    using (var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
                        if (gameplay.CalculateEntityCount()!=1 || gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive==0) return true;
                    if (crossMission < 3)
                    {
                        if (runtime.MissionId.ToString()!=crossMissions[crossMission] || facts.CommandSquadSpawned==0 || runtime.Phase==MissionPhaseKind.InteractiveBrief) return true;
                        using var map = em.CreateEntityQuery(typeof(ActiveOperationMapComponent));
                        if (map.CalculateEntityCount()!=1 || !map.GetSingleton<ActiveOperationMapComponent>().MissionId.Equals(runtime.MissionId))
                            throw new InvalidOperationException("Cross-mission entry retained another mission's map identity.");
                    }
                    if (crossMission==2)
                    {
                        if (runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<1000) return true;
                        ValidateFreshLifecycleAttempt(em,root,in runtime,in facts);
                        using var dormant=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<CampaignMissionDormantMapUnitTag>()},Options=EntityQueryOptions.IncludeDisabledEntities});
                        if(dormant.CalculateEntityCount()!=22) throw new InvalidOperationException("M3 did not scope its 22 shared-map vehicles.");
                        foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true)) crossOldMembers.Add(member.Entity);
                    }
                    else
                    {
                        AssertNoM3State(em,root);
                        if(crossMission==3)
                        {
                            using var map=em.CreateEntityQuery(typeof(ActiveOperationMapComponent));
                            if(map.CalculateEntityCount()==1 && map.GetSingleton<ActiveOperationMapComponent>().MissionId.ToString()==crossMissions[2])
                                throw new InvalidOperationException("Skirmish retained the M3 operation-map identity.");
                        }
                    }
                    Debug.Log("[M03CrossMissionProbe] entered="+(crossMission<3 ? crossMissions[crossMission] : "Skirmish")+" correct identity, live simulation, scoped M3 state");
                    UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.Pause); CrossNext(); break;
                case 2:
                    var pause=UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>();
                    if(pause==null || !pause.ExitButton.interactable) return true;
                    ClickCommand(pause.ExitButton); CrossNext(); break;
                case 3:
                    // ActiveRoute changes when opaque loading starts. Wait for the
                    // actual returned menu before sending another visible action.
                    if(shell.ActiveRoute!=UIRoute.MainMenu || shell.CurrentMode!=UiShellMode.MainMenu ||
                        shell.Phase!=UiShellTransitionPhase.MenuReady) return true;
                    AssertNoM3State(em,root);
                    foreach(var member in crossOldMembers)
                        if(em.Exists(member)) throw new InvalidOperationException("M3 unit survived exit into the next mode.");
                    crossOldMembers.Clear();
                    if(Time.timeScale==0 || UiShellRuntimeGateway.IsMissionFieldGuidePresenting())
                        throw new InvalidOperationException("Cross-mission exit retained a paused clock/guide.");
                    if(++crossMission==4)
                    {Complete(true,"live M1 -> menu -> M2 -> menu -> M3 -> menu -> Skirmish -> menu; exact map identity, active simulation, M3 state and map dormancy isolated, M3 members removed"); return true;}
                    crossStep=0; crossDeadline=EditorApplication.timeSinceStartup+180; break;
                case 5:
                    var quick=UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>();
                    if(quick==null) return true;
                    var launch=(Button)typeof(QuickCustomScreenView).GetField("launchButton",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(quick);
                    ClickCommand(launch); crossStep=1; crossDeadline=EditorApplication.timeSinceStartup+180; break;
            }
            return true;
        }
        private static void CrossNext() {crossStep++; crossDeadline=EditorApplication.timeSinceStartup+180;}
        private static void AssertNoM3State(EntityManager em,Entity root)
        {
            if(UiShellRuntimeGateway.TryReadMissionDefense(out _) || UiShellRuntimeGateway.TryReadMissionCameraTour())
                throw new InvalidOperationException("M3 defense/camera UI escaped its attempt.");
            using var dormant=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<CampaignMissionDormantMapUnitTag>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            if(dormant.CalculateEntityCount()!=0) throw new InvalidOperationException("M3 map dormancy survived outside M3.");
            if(em.HasBuffer<CampaignMissionDefenseMember>(root) && em.GetBuffer<CampaignMissionDefenseMember>(root).Length!=0)
                throw new InvalidOperationException("M3 finite membership survived outside M3.");
        }
    }
}
