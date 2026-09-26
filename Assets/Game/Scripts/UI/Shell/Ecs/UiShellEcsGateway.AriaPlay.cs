using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiAriaPlayGateway
    {
        AriaPlayCapability IUiAriaPlayGateway.ReadAriaPlayCapability() =>
            TryGetBoundary(out var em, out _) ? ReadAriaCapability(em) : AriaPlayCapability.None;
        private static AriaPlayCapability ReadAriaCapability(EntityManager em)
        {
            using (var controls = em.CreateEntityQuery(ComponentType.ReadOnly<FactionControlEntry>()))
            {
                using var roots = controls.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var root in roots)
                    foreach (var control in em.GetBuffer<FactionControlEntry>(root, true))
                        if (control.IsPlayerFaction != 0 && control.AIControlled != 0) return AriaPlayCapability.None;
            }
            if (TrySkirmish(out _, out _, out var skirmish))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Remove this development gate only after the coverage matrix passes.
                return skirmish.Phase == SkirmishPhase.Playing ? AriaPlayCapability.BaseAssault : AriaPlayCapability.None;
#else
                return AriaPlayCapability.None;
#endif
            }
            using (var operations = em.CreateEntityQuery(ComponentType.ReadOnly<OperationsReconMissionComponent>()))
                if (operations.CalculateEntityCount() == 1)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    return operations.GetSingleton<OperationsReconMissionComponent>().Phase == OperationsReconPhase.Playing
                        ? AriaPlayCapability.ReconOperation : AriaPlayCapability.None;
#else
                    return AriaPlayCapability.None;
#endif
                }
            using var missions = em.CreateEntityQuery(ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
            if (missions.CalculateEntityCount() != 1) return AriaPlayCapability.None;
            var mission = missions.GetSingleton<CampaignMissionRuntimeComponent>();
            if (mission.Outcome != Game.Missions.Contracts.MissionOutcomeKind.None) return AriaPlayCapability.None;
            string id = mission.MissionId.ToString();
            if(id==Game.Missions.Contracts.CampaignMissionSequence.SupplyLine ||
                id==Game.Missions.Contracts.CampaignMissionSequence.MarketLifeline ||
                id==Game.Missions.Contracts.CampaignMissionSequence.PowerRelay)
                return AriaPlayCapability.GuidedCampaign;
            return id is "saga.ch01.m01.first_contact" or "saga.ch01.m02.establish_base" or
                "saga.ch01.m03.radar_warning" or "saga.ch01.m04.airlift" or "saga.ch01.m05.breach_assault" or
                "saga.ch02.m01.gridlock"
                ? AriaPlayCapability.GuidedCampaign : AriaPlayCapability.None;
        }
        void IUiAriaPlayGateway.PublishAriaObservation(AriaPlayObservation value)
        {
            if (!TryGetBoundary(out var em, out var boundary)) return;
            EnsureAriaPlay(em, boundary);
            em.SetComponentData(boundary, new AriaPlayObservationComponent
            {
                Kind = value.Kind, TargetId = value.TargetId, GoalId = value.GoalId,
                Position = value.Position, Frame = value.Frame, Time = value.Time,Drag=value.Drag?(byte)1:(byte)0,DragEnd=value.DragEnd
            });
        }
        void IUiAriaPlayGateway.PublishAriaSkirmishObservation(AriaSkirmishObservation value)
        {
            if (!TryGetBoundary(out var em, out var boundary)) return;
            EnsureAriaPlay(em, boundary);
            if (!em.HasComponent<AriaSkirmishObservationComponent>(boundary)) em.AddComponent<AriaSkirmishObservationComponent>(boundary);
            if (!em.HasComponent<AriaSkirmishPlanComponent>(boundary)) em.AddComponent<AriaSkirmishPlanComponent>(boundary);
            em.SetComponentData(boundary, new AriaSkirmishObservationComponent { Value = value });
        }
        AriaSkirmishIntent IUiAriaPlayGateway.ReadAriaSkirmishIntent() =>
            TryGetBoundary(out var em, out var boundary) && em.HasComponent<AriaSkirmishPlanComponent>(boundary)
                ? em.GetComponentData<AriaSkirmishPlanComponent>(boundary).Intent : default;
        bool IUiAriaPlayGateway.TryStartAriaPlay()
        {
            if (!TryGetBoundary(out var em, out var boundary) || !em.HasComponent<AriaPlayObservationComponent>(boundary)) return false;
            if (ReadAriaCapability(em) == AriaPlayCapability.None) return false;
            var observation = em.GetComponentData<AriaPlayObservationComponent>(boundary);
            if (observation.Kind is AriaPlayObservationKind.Unavailable or AriaPlayObservationKind.Finished) return false;
            if (em.HasComponent<AssistantStateComponent>(boundary) &&
                em.GetComponentData<AssistantStateComponent>(boundary).ControlState != AssistantControlState.Player) return false;
            if (em.HasBuffer<AssistantCommandIntentRequestElement>(boundary) && em.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Length != 0) return false;
            var session = em.GetComponentData<AriaPlaySessionComponent>(boundary);
            if (session.Phase is not AriaPlayPhase.Manual and not AriaPlayPhase.Blocked) return false;
            if (em.HasComponent<AriaSkirmishPlanComponent>(boundary)) em.SetComponentData(boundary, default(AriaSkirmishPlanComponent));
            em.SetComponentData(boundary, new AriaPlaySessionComponent
            { Phase = AriaPlayPhase.Starting, LastProgressAt = observation.Time, DueAt = observation.Time + .3f });
            return true;
        }
        void IUiAriaPlayGateway.StopAriaPlay()
        {
            World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<AriaPlayInputSystem>()?.Cancel();
            if (TryGetBoundary(out var em, out var boundary))
            {
                if (em.HasComponent<AriaPlaySessionComponent>(boundary))
                    em.SetComponentData(boundary, default(AriaPlaySessionComponent));
                if (em.HasComponent<AriaSkirmishPlanComponent>(boundary))
                    em.SetComponentData(boundary, default(AriaSkirmishPlanComponent));
            }
        }
        AriaPlayModel IUiAriaPlayGateway.ReadAriaPlay()
        {
            if (!TryGetBoundary(out var em, out var boundary) || !em.HasComponent<AriaPlaySessionComponent>(boundary)) return default;
            var value = em.GetComponentData<AriaPlaySessionComponent>(boundary);
            return new AriaPlayModel(value.Phase, value.Contact, value.Pressed != 0, value.Actions,
                value.Target, value.DueAt);
        }
        private static void EnsureAriaPlay(EntityManager em, Entity boundary)
        {
            if (!em.HasComponent<AriaPlayObservationComponent>(boundary)) em.AddComponent<AriaPlayObservationComponent>(boundary);
            if (!em.HasComponent<AriaPlaySessionComponent>(boundary)) em.AddComponent<AriaPlaySessionComponent>(boundary);
        }
    }
}
