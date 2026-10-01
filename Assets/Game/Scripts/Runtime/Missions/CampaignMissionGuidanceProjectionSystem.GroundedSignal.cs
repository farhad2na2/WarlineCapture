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
        private bool TryUpdateGroundedSignalGuidance(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts, in AssistantSettingsComponent settings, in CampaignMissionGuidanceProjectionComponent current)
        {
            if (!runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.GroundedSignal))) return false;
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
                    !em.GetComponentData<CampaignMissionUnitRoleComponent>(entity).MissionRoleId.Equals(new FixedString64Bytes("role.protected.terminal"))) actor = entity;
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
            FixedString64Bytes title = "mission.grounded_signal.tutorial."; title.Append(step); title.Append(new FixedString32Bytes(".title"));
            FixedString128Bytes body = "mission.grounded_signal.tutorial."; body.Append(step); body.Append(new FixedString32Bytes(".body"));
            if (relayApproach)
            {
                title = "mission.grounded_signal.tutorial.2.approach.title";
                body = "mission.grounded_signal.tutorial.2.approach.body";
            }
            if (relayCarrier)
            {
                title = relayApproach ? "mission.grounded_signal.tutorial.2.apc_approach.title" : "mission.grounded_signal.tutorial.2.apc.title";
                body = relayApproach ? "mission.grounded_signal.tutorial.2.apc_approach.body" : "mission.grounded_signal.tutorial.2.apc.body";
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
