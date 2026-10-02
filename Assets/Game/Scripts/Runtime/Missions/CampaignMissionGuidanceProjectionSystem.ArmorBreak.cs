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
        private static readonly FixedString64Bytes ArmorBreakGuidanceMissionId = "saga.ch04.m05.armor_break";
        private static readonly FixedString64Bytes ArmorBreakTitlePrefix = "mission.armor_break.tutorial.";
        private static readonly FixedString128Bytes ArmorBreakBodyPrefix = "mission.armor_break.tutorial.";
        private static readonly FixedString64Bytes ArmorBreakAircraftApproachTitle = "mission.armor_break.tutorial.4.approach.title";
        private static readonly FixedString128Bytes ArmorBreakAircraftAssaultBody = "mission.armor_break.tutorial.6.aircraft.body";
        private static readonly FixedString128Bytes ArmorBreakAircraftApproachBody = "mission.armor_break.tutorial.4.approach.body";
        private bool TryUpdateArmorBreakGuidance(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime,
            in AssistantSettingsComponent settings, in CampaignMissionGuidanceProjectionComponent current)
        {
            if (!runtime.MissionId.Equals(ArmorBreakGuidanceMissionId)) return false;
            var em = system.EntityManager;
            if (runtime.Phase != MissionPhaseKind.Engage || runtime.Outcome != MissionOutcomeKind.None || !em.HasComponent<CampaignMissionArmorBreakState>(root))
            { ClearDefenseGuidance(em, root, in current); return true; }
            var mission = em.GetComponentData<CampaignMissionArmorBreakState>(root);
            if (!CampaignMissionArmorBreakRuleUtility.Matches(in mission, in runtime)) { ClearDefenseGuidance(em, root, in current); return true; }
            int step = CampaignMissionArmorBreakRuleUtility.Stage(in mission);
            Entity actor = step is 1 or 2 ? mission.AirDefense : step == 3 ? mission.Launcher : step == 4 ? mission.Aircraft : Entity.Null;
            Entity hostile = step == 3 ? mission.Battery : Entity.Null;
            if (step == 6) actor = ResolveArmorBreakAssaultActor(em, root, in mission);
            bool aircraftAssault = step == 6 && actor == mission.Aircraft;
            foreach (var member in em.GetBuffer<CampaignMissionArmorBreakMember>(root, true))
            {
                if (member.Dead != 0 || !em.Exists(member.Entity)) continue;
                if (actor == Entity.Null && (step == 5 && member.Kind == ArmorBreakMemberKind.Armor || step == 7 && member.Kind == ArmorBreakMemberKind.Infantry)) actor = member.Entity;
                if (hostile == Entity.Null && step == 4 && member.Kind == ArmorBreakMemberKind.HostileArmor) hostile = member.Entity;
                if (hostile == Entity.Null && step == 6 && member.Kind is ArmorBreakMemberKind.HostileCommand or ArmorBreakMemberKind.HostileArmor) hostile = member.Entity;
            }
            if (actor == Entity.Null || !em.Exists(actor) || !em.HasComponent<LocalTransform>(actor)) { ClearDefenseGuidance(em, root, in current); return true; }
            float3 destination = step is 1 or 2 ? mission.CoverageCenter : step == 5 ? mission.AssaultApproach : mission.AuthorityCenter;
            bool approach = false;
            if (hostile != Entity.Null && em.Exists(hostile) && em.HasComponent<LocalTransform>(hostile))
            {
                destination = em.GetComponentData<LocalTransform>(hostile).Position;
                if (step == 4 || aircraftAssault)
                {
                    float range = em.HasComponent<UnitAttack>(actor) ? math.max(1, em.GetComponentData<UnitAttack>(actor).Range) : 35f;
                    float standoff = math.max(1, range - 10f);
                    float3 aircraftPosition = em.GetComponentData<LocalTransform>(actor).Position;
                    float firingThreshold = math.max(1, range - 5f);
                    approach = math.distancesq(aircraftPosition.xz, destination.xz) > firingThreshold * firingThreshold;
                    if (approach)
                    {
                        float2 direction = math.normalizesafe(aircraftPosition.xz - destination.xz, new float2(-1, 0));
                        destination.xz += direction * standoff;
                        hostile = Entity.Null;
                    }
                }
            }
            FixedString64Bytes title = ArmorBreakTitlePrefix; title.Append(step); title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body = ArmorBreakBodyPrefix; body.Append(step); body.Append(ExtractionBodySuffix);
            if (aircraftAssault) body = ArmorBreakAircraftAssaultBody;
            if (approach) { title = ArmorBreakAircraftApproachTitle; body = ArmorBreakAircraftApproachBody; }
            var next = new CampaignMissionGuidanceProjectionComponent {
                GuidanceId = 67000 + step, Version = Next(current.Version), MissionSourceVersion = runtime.Version,
                Prompt = (CampaignMissionGuidancePromptKind)(91 + step), GuidanceMode = NarrativeGuidanceMode.Full, Active = 1,
                RecommendationKind = step == 2 ? AssistantRecommendationKind.Explain : step == 3 || step is 4 or 6 && !approach ? AssistantRecommendationKind.Attack : AssistantRecommendationKind.Move,
                TargetKind = AssistantTargetKind.WorldPosition, SourceEntity = actor, TargetEntity = hostile,
                WorldPosition = destination, HasWorldPosition = 1, Title = title, Body = body, ActionLabel = RadarAct,
                CanShow = 1, CanExecute = 0, Priority = AssistantMessagePriority.High,
                SubtitlesEnabled = settings.SubtitlesEnabled, LargeTextEnabled = settings.LargeTextEnabled, HighContrastEnabled = settings.HighContrastEnabled };
            em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Clear();
            if (!ProjectionEquals(in current, in next) || !current.Body.Equals(next.Body)) em.SetComponentData(root, next);
            return true;
        }
        public static bool IsArmorBreakArmedActor(EntityManager em, Entity actor) =>
            em.Exists(actor) && em.HasComponent<UnitHealth>(actor) && em.GetComponentData<UnitHealth>(actor).Current > 0 &&
            em.HasComponent<LocalTransform>(actor) && em.HasComponent<UnitCombat>(actor) &&
            em.GetComponentData<UnitCombat>(actor).CanAttack != 0 && em.HasComponent<UnitAttack>(actor) &&
            em.GetComponentData<UnitAttack>(actor).Damage > 0 && em.GetComponentData<UnitAttack>(actor).Range > 0;

        internal static Entity ResolveArmorBreakAssaultActor(EntityManager em, Entity root, in CampaignMissionArmorBreakState mission)
        {
            foreach (var member in em.GetBuffer<CampaignMissionArmorBreakMember>(root, true))
                if (member.Kind == ArmorBreakMemberKind.Armor && member.Dead == 0 && IsArmorBreakArmedActor(em, member.Entity))
                    return member.Entity;
            if (mission.AircraftUsed != 0 && mission.ArmoredThreatsCleared != 0 && IsArmorBreakArmedActor(em, mission.Aircraft))
                foreach (var member in em.GetBuffer<CampaignMissionArmorBreakMember>(root, true))
                    if (member.Kind == ArmorBreakMemberKind.Aircraft && member.Entity == mission.Aircraft && member.Dead == 0)
                        return mission.Aircraft;
            return Entity.Null;
        }
    }
}
