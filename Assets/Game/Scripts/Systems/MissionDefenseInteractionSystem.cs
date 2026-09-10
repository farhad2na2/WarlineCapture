using Unity.Burst;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ThreatWarningResolveSystem))]
    [BurstCompile]
    public partial struct MissionDefenseInteractionSystem : ISystem
    {
        private EntityQuery cameraQuery,snapshotQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            cameraQuery=state.GetEntityQuery(ComponentType.ReadWrite<RuntimeCameraFocusRequestComponent>());
            snapshotQuery=state.GetEntityQuery(ComponentType.ReadOnly<RuntimeCameraSnapshotComponent>());
            state.RequireForUpdate<MissionDefenseInteractionRequest>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root)) return;
            EntityManager em = state.EntityManager;
            if (!em.HasBuffer<MissionDefenseInteractionRequest>(root)) return;
            var requests = em.GetBuffer<MissionDefenseInteractionRequest>(root);
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var defense = em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
            bool active = runtime.SessionToken.Equals(defense.SessionToken) && runtime.AttemptOrdinal == defense.AttemptOrdinal &&
                          runtime.SourceVersion == defense.SourceVersion && runtime.Phase == MissionPhaseKind.Engage &&
                          runtime.Outcome == MissionOutcomeKind.None &&
                          SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) && gameplay.SimulationActive != 0;
            if (!active) { requests.Clear(); return; }
            bool smooth=!SystemAPI.TryGetSingleton(out CampaignMissionCameraTourState tour) || tour.ReducedMotion==0;
            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                if (!request.SessionToken.Equals(runtime.SessionToken) || request.AttemptOrdinal != runtime.AttemptOrdinal ||
                    request.SourceVersion != runtime.SourceVersion) continue;
                if(request.Kind==MissionDefenseInteractionKind.ReturnCamera)
                {
                    if(defense.FocusReturnAvailable==0 || cameraQuery.CalculateEntityCount()!=1) continue;
                    em.SetComponentData(cameraQuery.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent
                    {Requested=1,Smooth=smooth ? (byte)1 : (byte)0,SmoothTimeSeconds=.8f,World=defense.FocusReturnPosition,
                        UseExplicitPerspective=1,Perspective=defense.FocusReturnPerspective});
                    defense.FocusReturnAvailable=0;
                }
                else if (request.Kind is MissionDefenseInteractionKind.ReadWarning or MissionDefenseInteractionKind.FocusWarning)
                {
                    var records = em.GetBuffer<ThreatWarningRecord>(root);
                    for (int r = 0; r < records.Length; r++)
                    {
                        var warning = records[r];
                        if (warning.ElementIndex != request.WarningElementIndex || warning.Resolved != 0) continue;
                        if (warning.ReadByPlayer == 0)
                        {
                            warning.ReadByPlayer = 1; records[r] = warning;
                            var ledger = em.GetComponentData<ThreatWarningLedgerState>(root);
                            ledger.Version++; em.SetComponentData(root, ledger);
                        }
                        defense.AcknowledgedGuidanceMask |= 1u;
                        if (request.Kind != MissionDefenseInteractionKind.FocusWarning || warning.HasFocus == 0 ||
                            cameraQuery.CalculateEntityCount()!=1) break;
                        Entity camera=cameraQuery.GetSingletonEntity();
                        if(defense.FocusReturnAvailable==0 && snapshotQuery.CalculateEntityCount()==1 &&
                            CampaignMissionPatrolOrderSystem.TryCaptureTourStart(snapshotQuery.GetSingleton<RuntimeCameraSnapshotComponent>(),
                                defense.InnerCoreCenter.y,out var previousFocus,out var previousPerspective))
                        {
                            defense.FocusReturnPosition=previousFocus; defense.FocusReturnPerspective=previousPerspective;
                            defense.FocusReturnAvailable=1;
                        }
                        em.SetComponentData(camera, new RuntimeCameraFocusRequestComponent
                        { Requested = 1, Smooth = smooth ? (byte)1 : (byte)0, SmoothTimeSeconds = 0.8f, World = warning.FocusPosition });
                        defense.PlayerRequestedFocus = 1;
                        defense.AcknowledgedGuidanceMask |= 2u;
                        break;
                    }
                }
                else
                {
                    var guidance = em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                    int step = (int)guidance.Prompt - 12;
                    if (guidance.Active == 0 || guidance.GuidanceId != request.GuidanceId || step < 1 || step > 12) continue;
                    bool explain = step is 3 or 10 or 11 or 12;
                    bool optional = step is 4 or 7 or 8 or 9;
                    if ((request.Kind == MissionDefenseInteractionKind.ContinueExplanation && explain) ||
                        (request.Kind == MissionDefenseInteractionKind.SkipOptional && optional))
                        defense.AcknowledgedGuidanceMask |= 1u << (step - 1);
                }
            }
            requests.Clear(); em.SetComponentData(root, defense);
        }
    }
}
