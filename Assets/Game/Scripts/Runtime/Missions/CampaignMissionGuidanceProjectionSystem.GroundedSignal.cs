using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionGuidanceProjectionSystem
    {
        private static readonly FixedString64Bytes GroundedSignalGuidanceMissionId = "saga.ch04.m04.grounded_signal";
        private static readonly FixedString64Bytes GroundedSignalTerminalRole = "role.protected.terminal";
        private static readonly FixedString64Bytes GroundedSignalTitlePrefix = "mission.grounded_signal.tutorial.";
        private static readonly FixedString128Bytes GroundedSignalBodyPrefix = "mission.grounded_signal.tutorial.";
        private static readonly FixedString64Bytes GroundedSignalRelayApproachTitle = "mission.grounded_signal.tutorial.2.approach.title";
        private static readonly FixedString128Bytes GroundedSignalRelayApproachBody = "mission.grounded_signal.tutorial.2.approach.body";
        private static readonly FixedString64Bytes GroundedSignalCarrierApproachTitle = "mission.grounded_signal.tutorial.2.apc_approach.title";
        private static readonly FixedString128Bytes GroundedSignalCarrierApproachBody = "mission.grounded_signal.tutorial.2.apc_approach.body";
        private static readonly FixedString64Bytes GroundedSignalCarrierAttackTitle = "mission.grounded_signal.tutorial.2.apc.title";
        private static readonly FixedString128Bytes GroundedSignalCarrierAttackBody = "mission.grounded_signal.tutorial.2.apc.body";
        private bool TryUpdateGroundedSignalGuidance(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts, in AssistantSettingsComponent settings, in CampaignMissionGuidanceProjectionComponent current)
        {
            if (!runtime.MissionId.Equals(GroundedSignalGuidanceMissionId)) return false;
            var em = system.EntityManager;
            if (runtime.Phase != MissionPhaseKind.Engage || runtime.Outcome != MissionOutcomeKind.None ||
                !em.HasComponent<CampaignMissionGroundedSignalState>(root) || !em.HasComponent<CampaignMissionExtractionState>(root))
            { ClearDefenseGuidance(em, root, in current); return true; }
            var mission = em.GetComponentData<CampaignMissionGroundedSignalState>(root);
            var extraction = em.GetComponentData<CampaignMissionExtractionState>(root);
            if (!CampaignMissionGroundedSignalRuleUtility.Matches(in mission, in runtime))
            { ClearDefenseGuidance(em, root, in current); return true; }
            int step = mission.Inserted == 0 ? 1 : mission.RelayDisabled == 0 ? 2 : mission.HardwareRecovered == 0 ? 3 :
                mission.SpecialistsAboardCarrier < 2 ? 4 : 5;
            Entity actor = step == 1 ? extraction.Aircraft : step == 5 ? extraction.Carrier : Entity.Null;
            var roster = em.GetBuffer<CampaignMissionExtractionMember>(root, true);
            for (int i = 0; i < roster.Length && actor == Entity.Null; i++)
            {
                Entity entity = roster[i].Entity;
                if (roster[i].Dead != 0 || !em.Exists(entity)) continue;
                if (step == 2 && roster[i].Kind == 0 && em.HasComponent<CampaignMissionUnitRoleComponent>(entity) &&
                    !em.GetComponentData<CampaignMissionUnitRoleComponent>(entity).MissionRoleId.Equals(GroundedSignalTerminalRole)) actor = entity;
                if (step is 3 or 4 && roster[i].Kind == 1) actor = entity;
            }
            if (step == 2 && actor == Entity.Null && em.Exists(extraction.Carrier)) actor = extraction.Carrier;
            bool relayCarrier = step == 2 && actor == extraction.Carrier;
            float3 position = step == 1 ? mission.ApronCenter : step == 3 ? mission.HardwareCenter : extraction.LandingCenter;
            Entity target = step == 2 ? mission.Relay : step == 4 ? extraction.Carrier : step == 1 ? extraction.Aircraft : Entity.Null;
            if (target != Entity.Null && em.Exists(target) && em.HasComponent<LocalTransform>(target)) position = em.GetComponentData<LocalTransform>(target).Position;
            bool relayApproach = false;
            if (step == 2 && em.Exists(actor) && em.HasComponent<LocalTransform>(actor))
            {
                float3 actorPosition = em.GetComponentData<LocalTransform>(actor).Position;
                // Normal Move uses navigation to cross the open service gate. Combat chase cannot route across the yard fence.
                float3 approach = new float3(position.x, position.y, position.z - 18f);
                float range = em.HasComponent<UnitAttack>(actor) ? math.max(0f, em.GetComponentData<UnitAttack>(actor).Range) : 0f;
                float2 relayDelta = actorPosition.xz - position.xz;
                float2 approachDelta = actorPosition.xz - approach.xz;
                relayApproach = math.lengthsq(relayDelta) > (range + 5f) * (range + 5f) && math.lengthsq(approachDelta) > 25f;
                if (relayApproach) { position = approach; target = Entity.Null; }
            }
            FixedString64Bytes title = GroundedSignalTitlePrefix; title.Append(step); title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body = GroundedSignalBodyPrefix; body.Append(step); body.Append(ExtractionBodySuffix);
            if (relayApproach)
            {
                title = GroundedSignalRelayApproachTitle;
                body = GroundedSignalRelayApproachBody;
            }
            if (relayCarrier)
            {
                if (relayApproach)
                {
                    title = GroundedSignalCarrierApproachTitle;
                    body = GroundedSignalCarrierApproachBody;
                }
                else
                {
                    title = GroundedSignalCarrierAttackTitle;
                    body = GroundedSignalCarrierAttackBody;
                }
            }
            var next = new CampaignMissionGuidanceProjectionComponent {
                GuidanceId = 66000 + step, Version = Next(current.Version), MissionSourceVersion = runtime.Version,
                Prompt = (CampaignMissionGuidancePromptKind)(86 + step), GuidanceMode = NarrativeGuidanceMode.Full, Active = 1,
                RecommendationKind = step == 2 && !relayApproach ? AssistantRecommendationKind.Attack : step == 1 || step == 4 ? AssistantRecommendationKind.Select : AssistantRecommendationKind.Move,
                TargetKind = AssistantTargetKind.WorldPosition, SourceEntity = actor, TargetEntity = target,
                WorldPosition = position, HasWorldPosition = 1, Title = title, Body = body, ActionLabel = RadarAct,
                CanShow = 1, CanExecute = 0, Priority = AssistantMessagePriority.High,
                SubtitlesEnabled = settings.SubtitlesEnabled, LargeTextEnabled = settings.LargeTextEnabled, HighContrastEnabled = settings.HighContrastEnabled };
            // Acknowledgements only dismiss guidance; progress comes from authoritative mission actions.
            em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Clear();
            if (!ProjectionEquals(in current, in next) || !current.Body.Equals(next.Body)) em.SetComponentData(root, next);
            return true;
        }
    }
}
