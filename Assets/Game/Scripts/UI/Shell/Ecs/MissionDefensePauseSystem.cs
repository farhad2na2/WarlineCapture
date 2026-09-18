using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MissionDefensePauseSystem : ISystem
    {
        private bool held;
        private float previousTimeScale;
        private byte previousSimulationActive;
        private FixedString64Bytes session;
        private int attempt;
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<RuntimeGameplayStateComponent>();
        public void OnDestroy(ref SystemState state)
        { if(held && Time.timeScale==0) Time.timeScale=previousTimeScale; }
        public void OnUpdate(ref SystemState state)
        {
            EntityManager em=state.EntityManager;
            if(!SystemAPI.TryGetSingletonEntity<RuntimeGameplayStateComponent>(out Entity gameplayEntity)) return;
            var gameplay=em.GetComponentData<RuntimeGameplayStateComponent>(gameplayEntity);
            bool defense=SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent runtime) &&
                         SystemAPI.TryGetSingleton(out CampaignMissionDefenseStateComponent mission) &&
                         mission.SessionToken.Equals(runtime.SessionToken) && mission.AttemptOrdinal==runtime.AttemptOrdinal &&
                         mission.SourceVersion==runtime.SourceVersion && runtime.Phase==MissionPhaseKind.Engage &&
                         runtime.Outcome==MissionOutcomeKind.None && gameplay.PlayRequested!=0;
            bool extraction=SystemAPI.TryGetSingleton(out CampaignMissionExtractionState airlift) &&
                airlift.SessionToken.Equals(runtime.SessionToken) && airlift.AttemptOrdinal==runtime.AttemptOrdinal && airlift.SourceVersion==runtime.SourceVersion &&
                airlift.Initialized!=0 && runtime.Phase==MissionPhaseKind.Engage && runtime.Outcome==MissionOutcomeKind.None && gameplay.PlayRequested!=0;
            defense|=extraction;
            bool hasSkirmish = SystemAPI.TryGetSingleton(out SkirmishMatchState skirmishMatch);
            bool finishedSkirmish = hasSkirmish && skirmishMatch.Phase == SkirmishPhase.Finished;
            bool skirmish = finishedSkirmish || (hasSkirmish &&
                skirmishMatch.Phase == SkirmishPhase.Playing && gameplay.PlayRequested != 0);
            var activeSession = skirmish ? skirmishMatch.SessionId : runtime.SessionToken;
            int activeAttempt = skirmish ? 0 : runtime.AttemptOrdinal;
            bool canPause = defense || skirmish;
            bool popup=SystemAPI.TryGetSingleton(out UiShellActivePopupComponent active) && active.Visible!=0 &&
                       active.PopupKind is UiShellPopupKind.Pause or UiShellPopupKind.Settings or UiShellPopupKind.MissionFieldGuide;
            // The result overlay is not a shell popup. Keep the completed world
            // frozen until session teardown; replay/menu release this ownership.
            popup |= finishedSkirmish;
            bool same=held && session.Equals(activeSession) && attempt==activeAttempt;
            if(held && (!canPause || !popup || !same))
            {
                if(Time.timeScale==0) Time.timeScale=previousTimeScale;
                if(same && canPause) { gameplay.SimulationActive=previousSimulationActive; em.SetComponentData(gameplayEntity,gameplay); }
                held=false;
            }
            if(!canPause || !popup) return;
            if(!held)
            { previousTimeScale=Time.timeScale; previousSimulationActive=gameplay.SimulationActive; session=activeSession; attempt=activeAttempt; held=true; }
            Time.timeScale=0;
            if(gameplay.SimulationActive!=0) { gameplay.SimulationActive=0; em.SetComponentData(gameplayEntity,gameplay); }
        }
    }
}
