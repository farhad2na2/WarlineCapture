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
        private static readonly FixedString64Bytes CorridorGuidanceId = CampaignMissionSequence.LastCorridor, CorridorApproachSuffix = ".approach", CorridorEntrySuffix = ".route.entry", CorridorMiddleSuffix = ".route.mid", CorridorExitSuffix = ".route.exit";
        private bool TryUpdateLastCorridorGuidance(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime, in AssistantSettingsComponent settings, in CampaignMissionGuidanceProjectionComponent current)
        {
            if (!runtime.MissionId.Equals(CorridorGuidanceId)) return false; var em = system.EntityManager;
            if (runtime.Phase != MissionPhaseKind.Engage || runtime.Outcome != MissionOutcomeKind.None || !em.HasComponent<CampaignMissionLastCorridorState>(root)) { ClearDefenseGuidance(em, root, in current); return true; }
            var m = em.GetComponentData<CampaignMissionLastCorridorState>(root); if (!CampaignMissionLastCorridorRuleUtility.Matches(in m, in runtime)) { ClearDefenseGuidance(em, root, in current); return true; }
            int stage = CampaignMissionLastCorridorRuleUtility.Stage(in m); Entity actor = m.Engineer, target = Entity.Null; float3 position = m.RepairGate; var kind = AssistantRecommendationKind.Move;
            if (stage == 1)
            {
                actor = CampaignMissionRuntimeSystem.ResolveCorridorEscort(em, root); target = CampaignMissionRuntimeSystem.ResolveCorridorHostile(em, root);
                if (target != Entity.Null && em.Exists(target) && em.HasComponent<LocalTransform>(target) && em.Exists(actor) && em.HasComponent<LocalTransform>(actor) && em.HasComponent<UnitAttack>(actor))
                {
                    var goal = em.GetComponentData<LocalTransform>(target).Position; var p = em.GetComponentData<LocalTransform>(actor).Position; float range = math.max(3, em.GetComponentData<UnitAttack>(actor).Range - 8);
                    float2 direction = math.normalizesafe(p.xz - goal.xz, new float2(-1, 0)); position = new float3(goal.x + direction.x * range, p.y, goal.z + direction.y * range);
                    if (math.distancesq(p.xz, goal.xz) <= math.square(range + 3)) { position = goal; kind = AssistantRecommendationKind.Attack; }
                }
            }
            else if (stage == 2) { position = m.RepairGate; if (m.RepairMilliseconds > 0) kind = AssistantRecommendationKind.Explain; }
            else if (stage == 3) { actor = m.MedicineCarrier; position = CampaignMissionRuntimeSystem.CorridorRouteTarget(in m, CorridorCargoKind.Medicine); if (m.MedicineMilliseconds > 0) kind = AssistantRecommendationKind.Explain; }
            else if (stage == 4) { actor = m.FuelCarrier; position = CampaignMissionRuntimeSystem.CorridorRouteTarget(in m, CorridorCargoKind.Fuel); if (m.FuelMilliseconds > 0) kind = AssistantRecommendationKind.Explain; }
            else if (stage == 5)
            {
                actor = Entity.Null; foreach (var member in em.GetBuffer<CampaignMissionLastCorridorMember>(root, true)) if (member.Kind == LastCorridorMemberKind.Reinforcement && member.Dead == 0 && em.Exists(member.Entity)) { actor = member.Entity; break; }
                position = m.ReinforcementGate; if (m.ReinforcementMilliseconds > 0) kind = AssistantRecommendationKind.Explain;
            }
            else if (stage == 6) position = m.EngineerPickup;
            else if (stage == 7) { target = m.KeyCarrier; position = m.EngineerPickup; kind = AssistantRecommendationKind.Logistics; }
            else { actor = m.KeyCarrier; position = CampaignMissionRuntimeSystem.CorridorRouteTarget(in m, CorridorCargoKind.Keys); if (m.KeyMilliseconds > 0) kind = AssistantRecommendationKind.Explain; }
            if (actor == Entity.Null || !em.Exists(actor) || !em.HasComponent<LocalTransform>(actor)) { ClearDefenseGuidance(em, root, in current); return true; }
            FixedString64Bytes title = "mission.last_corridor.tutorial."; title.Append(stage); title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body = "mission.last_corridor.tutorial."; body.Append(stage); body.Append(ExtractionBodySuffix);
            if (stage == 1 && kind == AssistantRecommendationKind.Move) body.Append(CorridorApproachSuffix);
            if (stage is 3 or 4 or 8)
            {
                int step = stage == 3 ? m.MedicineRouteStep : stage == 4 ? m.FuelRouteStep : m.KeyRouteStep;
                if (step < 3) body.Append(step == 0 ? CorridorEntrySuffix : step == 1 ? CorridorMiddleSuffix : CorridorExitSuffix);
            }
            var next = new CampaignMissionGuidanceProjectionComponent { GuidanceId = 71000 + stage, Version = Next(current.Version), MissionSourceVersion = runtime.Version, Prompt = (CampaignMissionGuidancePromptKind)(124 + stage), GuidanceMode = NarrativeGuidanceMode.Full, Active = 1, RecommendationKind = kind, TargetKind = AssistantTargetKind.WorldPosition, SourceEntity = actor, TargetEntity = target, WorldPosition = position, HasWorldPosition = 1, Title = title, Body = body, ActionLabel = RadarAct, CanShow = 1, CanExecute = 0, Priority = AssistantMessagePriority.High, SubtitlesEnabled = settings.SubtitlesEnabled, LargeTextEnabled = settings.LargeTextEnabled, HighContrastEnabled = settings.HighContrastEnabled };
            em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Clear(); if (!ProjectionEquals(in current, in next) || !current.Body.Equals(next.Body)) em.SetComponentData(root, next); return true;
        }
    }
}
