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
        private static readonly FixedString64Bytes TrustApproachSuffix = ".approach";
        private static readonly FixedString64Bytes TrustGuidanceMissionId = CampaignMissionSequence.TrustUnderFire;
        private bool TryUpdateTrustUnderFireGuidance(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime, in AssistantSettingsComponent settings, in CampaignMissionGuidanceProjectionComponent current)
        {
            if (!runtime.MissionId.Equals(TrustGuidanceMissionId)) return false;
            var em = system.EntityManager;
            if (runtime.Phase != MissionPhaseKind.Engage || runtime.Outcome != MissionOutcomeKind.None || !em.HasComponent<CampaignMissionTrustUnderFireState>(root)) { ClearDefenseGuidance(em, root, in current); return true; }
            var m = em.GetComponentData<CampaignMissionTrustUnderFireState>(root);
            if (!CampaignMissionTrustUnderFireRuleUtility.Matches(in m, in runtime)) { ClearDefenseGuidance(em, root, in current); return true; }
            int stage = CampaignMissionTrustUnderFireRuleUtility.Stage(in m);
            Entity actor = stage == 2 ? m.NorthConvoy : stage == 4 ? m.SouthConvoy : stage >= 7 ? m.Engineer : CampaignMissionRuntimeSystem.ResolveTrustEscort(em, root, stage == 3 ? TrustUnderFireMemberKind.SouthEscort : TrustUnderFireMemberKind.NorthEscort);
            Entity hostile = Entity.Null;
            float3 destination = stage == 2 ? (m.NorthCrossed == 0 ? CampaignMissionTrustUnderFireRuleUtility.PassageWaypoint(m.NorthPassagePhase, m.NorthCrossing) : m.NorthArrival) : stage == 4 ? (m.SouthCrossed == 0 ? CampaignMissionTrustUnderFireRuleUtility.PassageWaypoint(m.SouthPassagePhase, m.SouthCrossing) : m.SouthArrival) : stage == 5 ? m.RelayApproach : m.RelayGate;
            var kind = stage == 8 ? AssistantRecommendationKind.Explain : AssistantRecommendationKind.Move;
            if (stage is 1 or 3 or 6)
            {
                var threat = stage == 1 ? TrustUnderFireMemberKind.NorthHostile : stage == 3 ? TrustUnderFireMemberKind.SouthHostile : TrustUnderFireMemberKind.RelayHostile;
                foreach (var member in em.GetBuffer<CampaignMissionTrustUnderFireMember>(root, true))
                    if (member.Kind == threat && member.Dead == 0 && em.Exists(member.Entity) && em.HasComponent<LocalTransform>(member.Entity) && !em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity)) { hostile = member.Entity; break; }
                if (hostile == Entity.Null) kind = AssistantRecommendationKind.Explain;
                else if (em.Exists(actor) && em.HasComponent<LocalTransform>(actor) && em.HasComponent<UnitAttack>(actor))
                {
                    var target = em.GetComponentData<LocalTransform>(hostile).Position; var p = em.GetComponentData<LocalTransform>(actor).Position;
                    float range = math.max(3, em.GetComponentData<UnitAttack>(actor).Range - 8);
                    float2 direction = math.normalizesafe(p.xz - target.xz, new float2(-1, 0));
                    destination = new float3(target.x + direction.x * range, p.y, target.z + direction.y * range);
                    if (math.distancesq(p.xz, target.xz) <= math.square(range + 3)) { destination = target; kind = AssistantRecommendationKind.Attack; }
                }
            }
            if (actor == Entity.Null || !em.Exists(actor) || !em.HasComponent<LocalTransform>(actor)) { ClearDefenseGuidance(em, root, in current); return true; }
            FixedString64Bytes title = "mission.trust_under_fire.tutorial."; title.Append(stage); title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body = "mission.trust_under_fire.tutorial."; body.Append(stage); body.Append(ExtractionBodySuffix);
            if (stage is 1 or 3 or 6 && kind == AssistantRecommendationKind.Move) body.Append(TrustApproachSuffix);
            var next = new CampaignMissionGuidanceProjectionComponent { GuidanceId = 69000 + stage, Version = Next(current.Version), MissionSourceVersion = runtime.Version,
                Prompt = (CampaignMissionGuidancePromptKind)(108 + stage), GuidanceMode = NarrativeGuidanceMode.Full, Active = 1, RecommendationKind = kind,
                TargetKind = AssistantTargetKind.WorldPosition, SourceEntity = actor, TargetEntity = hostile, WorldPosition = destination, HasWorldPosition = 1,
                Title = title, Body = body, ActionLabel = RadarAct, CanShow = 1, CanExecute = 0, Priority = AssistantMessagePriority.High,
                SubtitlesEnabled = settings.SubtitlesEnabled, LargeTextEnabled = settings.LargeTextEnabled, HighContrastEnabled = settings.HighContrastEnabled };
            em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Clear();
            if (!ProjectionEquals(in current, in next) || !current.Body.Equals(next.Body)) em.SetComponentData(root, next);
            return true;
        }
    }
}
