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
            bool popup=SystemAPI.TryGetSingleton(out UiShellActivePopupComponent active) && active.Visible!=0 &&
                       active.PopupKind is UiShellPopupKind.Pause or UiShellPopupKind.Settings or UiShellPopupKind.MissionFieldGuide;
            bool same=held && session.Equals(runtime.SessionToken) && attempt==runtime.AttemptOrdinal;
            if(held && (!defense || !popup || !same))
            {
                if(Time.timeScale==0) Time.timeScale=previousTimeScale;
                if(same && defense) { gameplay.SimulationActive=previousSimulationActive; em.SetComponentData(gameplayEntity,gameplay); }
                held=false;
            }
            if(!defense || !popup) return;
            if(!held)
            { previousTimeScale=Time.timeScale; previousSimulationActive=gameplay.SimulationActive; session=runtime.SessionToken; attempt=runtime.AttemptOrdinal; held=true; }
            Time.timeScale=0;
            if(gameplay.SimulationActive!=0) { gameplay.SimulationActive=0; em.SetComponentData(gameplayEntity,gameplay); }
        }
    }
}
