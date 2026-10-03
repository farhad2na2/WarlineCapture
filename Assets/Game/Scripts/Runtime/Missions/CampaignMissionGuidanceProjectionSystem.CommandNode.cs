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
        private static readonly FixedString64Bytes CommandGuideId = CampaignMissionSequence.CommandNode, CommandApproachSuffix = ".approach", CommandUtilitySuffix = ".utility", CommandCoverSuffix = ".cover";
        private bool TryUpdateCommandNodeGuidance(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime, in AssistantSettingsComponent settings, in CampaignMissionGuidanceProjectionComponent current)
        {
            if (!runtime.MissionId.Equals(CommandGuideId)) return false;
            var em = system.EntityManager;
            if (runtime.Phase != MissionPhaseKind.Engage || runtime.Outcome != MissionOutcomeKind.None || !em.HasComponent<CampaignMissionCommandNodeState>(root)) { ClearDefenseGuidance(em, root, in current); return true; }
            var mission = em.GetComponentData<CampaignMissionCommandNodeState>(root);
            if (!CampaignMissionCommandNodeRuleUtility.Matches(in mission, in runtime)) { ClearDefenseGuidance(em, root, in current); return true; }
            int stage = CampaignMissionCommandNodeRuleUtility.Stage(in mission);
            Entity actor = mission.Engineer, target = Entity.Null;
            float3 position = mission.IsolationClinic;
            var kind = AssistantRecommendationKind.Move;
            if (stage is 1 or 3 or 5)
            {
                actor = CampaignMissionRuntimeSystem.ResolveCommandActor(em, root, CommandNodeMemberKind.Escort);
                target = stage == 1 ? CampaignMissionRuntimeSystem.ResolveCommandActor(em, root, CommandNodeMemberKind.Exterior) : stage == 3 ? mission.Node : CampaignMissionRuntimeSystem.ResolveCommandActor(em, root, CommandNodeMemberKind.CoreGuard);
                if (stage == 5 && target == Entity.Null) target = mission.Qassem;
                if (em.Exists(target) && em.HasComponent<LocalTransform>(target) && em.Exists(actor) && em.HasComponent<LocalTransform>(actor) && em.HasComponent<UnitAttack>(actor))
                {
                    var goal = em.GetComponentData<LocalTransform>(target).Position; var p = em.GetComponentData<LocalTransform>(actor).Position;
                    float range = math.max(3, em.GetComponentData<UnitAttack>(actor).Range - 8);
                    float2 direction = math.normalizesafe(p.xz - goal.xz, new float2(-1, 0));
                    position = new float3(goal.x + direction.x * range, p.y, goal.z + direction.y * range);
                    if (math.distancesq(p.xz, goal.xz) <= math.square(range + 3)) { position = goal; kind = AssistantRecommendationKind.Attack; }
                }
            }
            else if (stage == 2) { position = mission.ClinicIsolated == 0 ? mission.IsolationClinic : mission.IsolationUtility; if (mission.IsolationMilliseconds > 0) kind = AssistantRecommendationKind.Explain; }
            else if (stage == 4)
            {
                if (mission.CoverReady == 0) { actor = CampaignMissionRuntimeSystem.ResolveCommandCoverActor(em, root); position = mission.BreachGate + new float3(0,0,20); }
                else { position = mission.BreachGate; if (mission.BreachMilliseconds > 0) kind = AssistantRecommendationKind.Explain; }
            }
            else if (stage == 6) { actor = CampaignMissionRuntimeSystem.ResolveCommandActor(em, root, CommandNodeMemberKind.Specialist); position = mission.CoreAudit; if (mission.AuditMilliseconds > 0) kind = AssistantRecommendationKind.Explain; }
            else if (stage == 7) { position = mission.AuditRelease; if (mission.ReleaseOrdered != 0 && mission.ReleaseMilliseconds > 0) kind = AssistantRecommendationKind.Explain; }
            else { actor = CampaignMissionRuntimeSystem.ResolveCommandActor(em, root, CommandNodeMemberKind.Specialist); position = mission.SafeReceiving; if (mission.ReceivingMilliseconds > 0) kind = AssistantRecommendationKind.Explain; }
            if (!em.Exists(actor) || !em.HasComponent<LocalTransform>(actor)) { ClearDefenseGuidance(em, root, in current); return true; }
            FixedString64Bytes title = "mission.command_node.tutorial."; title.Append(stage); title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body = "mission.command_node.tutorial."; body.Append(stage); body.Append(ExtractionBodySuffix);
            if (stage is 1 or 3 or 5 && kind == AssistantRecommendationKind.Move) body.Append(CommandApproachSuffix);
            if (stage == 2 && mission.ClinicIsolated != 0) body.Append(CommandUtilitySuffix);
            if (stage == 4 && mission.CoverReady == 0) body.Append(CommandCoverSuffix);
            var next = new CampaignMissionGuidanceProjectionComponent { GuidanceId = 72000 + stage, Version = Next(current.Version), MissionSourceVersion = runtime.Version, Prompt = (CampaignMissionGuidancePromptKind)(132 + stage), GuidanceMode = NarrativeGuidanceMode.Full, Active = 1, RecommendationKind = kind, TargetKind = AssistantTargetKind.WorldPosition, SourceEntity = actor, TargetEntity = target, WorldPosition = position, HasWorldPosition = 1, Title = title, Body = body, ActionLabel = RadarAct, CanShow = 1, CanExecute = 0, Priority = AssistantMessagePriority.High, SubtitlesEnabled = settings.SubtitlesEnabled, LargeTextEnabled = settings.LargeTextEnabled, HighContrastEnabled = settings.HighContrastEnabled };
            em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Clear();
            if (!ProjectionEquals(in current, in next) || !current.Body.Equals(next.Body)) em.SetComponentData(root, next);
            return true;
        }
    }
}
