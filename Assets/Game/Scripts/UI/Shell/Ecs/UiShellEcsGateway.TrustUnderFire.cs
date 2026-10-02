using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiTrustUnderFireGateway, IUiTrustUnderFireResultGateway
    {
        private static bool TryTrustUnderFire(out EntityManager em, out Entity root, out CampaignMissionTrustUnderFireState mission)
        {
            mission = default;
            if (!TryGetMissionRoot(out em, out root) || !em.HasComponent<CampaignMissionTrustUnderFireState>(root)) return false;
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root); mission = em.GetComponentData<CampaignMissionTrustUnderFireState>(root);
            return runtime.MissionId.Equals(CampaignMissionSequence.TrustUnderFire) && runtime.Phase == MissionPhaseKind.Engage && runtime.Outcome == MissionOutcomeKind.None && CampaignMissionTrustUnderFireRuleUtility.Matches(in mission, in runtime);
        }
        public bool TryReadTrustUnderFire(out UiTrustUnderFireModel model)
        {
            model = default; if (!TryTrustUnderFire(out var em, out var root, out var m)) return false;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            model = new UiTrustUnderFireModel(CampaignMissionTrustUnderFireRuleUtility.Stage(in m), m.NorthHoldMilliseconds / 1000, m.SouthHoldMilliseconds / 1000,
                m.VerificationMilliseconds / 1000, math.max(0, (CampaignMissionTrustUnderFireRuleUtility.DeadlineMilliseconds - m.ElapsedMilliseconds + 999) / 1000), facts.HostileDefeatedCount,
                m.NorthArrived != 0, m.SouthArrived != 0, m.RelayVerified != 0, m.NorthCrossed != 0, m.SouthCrossed != 0,
                m.NorthCrossing, m.SouthCrossing, m.NorthArrival, m.SouthArrival, m.RelayGate, m.RelayApproach); return true;
        }
        public bool TryReadTrustUnderFireResult(out UiTrustUnderFireResultModel model)
        {
            model = default; if (!TryGetMissionRoot(out var em, out var root)) return false;
            var runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (!runtime.MissionId.Equals(CampaignMissionSequence.TrustUnderFire) || runtime.Outcome == MissionOutcomeKind.None) return false;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            model = new UiTrustUnderFireResultModel(facts.TrustNorthArrived != 0, facts.TrustSouthArrived != 0, facts.TrustRelayVerified != 0, facts.CivilianLossCount, facts.HostileDefeatedCount, facts.TrustFailure.ToString()); return true;
        }
        private static bool ResolveTrustUnderFireTarget(EntityManager em, Entity root, in CampaignMissionGuidanceProjectionComponent g, out UiMissionTutorialTarget target)
        {
            target = default; if (!em.HasComponent<CampaignMissionTrustUnderFireState>(root) || !em.Exists(g.SourceEntity) || !em.HasComponent<LocalTransform>(g.SourceEntity)) return false;
            var m = em.GetComponentData<CampaignMissionTrustUnderFireState>(root); int stage = g.GuidanceId - 69000;
            float3 selection = em.GetComponentData<LocalTransform>(g.SourceEntity).Position, min = selection, max = selection;
            bool selected = em.HasComponent<SelectedUnitTag>(g.SourceEntity), moving = IsTutorialActorMoving(em, g.SourceEntity); int required = 1;
            if (stage is 1 or 3 or 5 or 6)
            {
                var kind = TrustUnderFireMemberKind.NorthEscort;
                foreach (var member in em.GetBuffer<CampaignMissionTrustUnderFireMember>(root, true)) if (member.Entity == g.SourceEntity) { kind = member.Kind; break; }
                selection = default; min = new float3(float.MaxValue); max = new float3(float.MinValue); selected = true; moving = false; required = 0;
                foreach (var member in em.GetBuffer<CampaignMissionTrustUnderFireMember>(root, true))
                { if (member.Kind != kind || member.Dead != 0 || !em.Exists(member.Entity) || !em.HasComponent<LocalTransform>(member.Entity)) continue;
                  var position = em.GetComponentData<LocalTransform>(member.Entity).Position; selection += position; min = math.min(min, position); max = math.max(max, position); required++;
                  selected &= em.HasComponent<SelectedUnitTag>(member.Entity); moving |= IsTutorialActorMoving(em, member.Entity); }
                if (required == 0) return false; selection /= required;
            }
            bool watching = stage == 8 || g.RecommendationKind == AssistantRecommendationKind.Explain ||
                stage == 2 && m.NorthHoldMilliseconds > 0 || stage == 4 && m.SouthHoldMilliseconds > 0 || stage == 7 && m.VerificationMilliseconds > 0;
            var action = watching ? UiTutorialBattleAction.Watch : g.RecommendationKind == AssistantRecommendationKind.Attack ? UiTutorialBattleAction.Attack : UiTutorialBattleAction.Move;
            target = new UiMissionTutorialTarget(selection, g.WorldPosition, !watching && !selected, moving, required, action,
                executingAttack: action == UiTutorialBattleAction.Attack && IsTutorialAttackInProgress(em, g.SourceEntity, g.TargetEntity), areaRadius: stage == 7 ? 6 : 12,
                dragSelection: required > 1, selectionMin: min - new float3(3, 0, 3), selectionMax: max + new float3(3, 3, 3)); return true;
        }
    }
}
