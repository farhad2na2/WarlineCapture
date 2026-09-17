using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionGuidanceProjectionSystem
    {
        internal const uint RetiredDefenseStopLessonMask=1u<<6;
        // These optional tools remain available, but must not interrupt a ready defense.
        internal const uint OptionalDefenseToolsMask=(1u<<7)|(1u<<8);
        // The battle has no Continue click: each cue follows a real defensive order or contact.
        private static void ApplyDefenseBattleGuidance(EntityManager em, Entity root, int step,
            float3 line, ref CampaignMissionGuidanceProjectionComponent next)
        {
            if(step==0) next.Body="mission.m03.clarity.brief";
            if(step==2) next.Body="mission.m03.clarity.prepare";
            if(step==7) next.Body="mission.m03.clarity.radar";
            if(step<9) return;
            Entity actor=next.SourceEntity, contact=Entity.Null;
            float nearest=float.MaxValue;
            // Prefer a rifle defending the road over a newly produced squad at the barracks.
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
            {
                var unit=member.Entity;
                if(member.FactionId!=1 || member.IsSensor!=0 || !IsLiveDefenseActor(em,unit) ||
                    !em.HasComponent<UnitCombat>(unit) || em.GetComponentData<UnitCombat>(unit).CanAttack==0) continue;
                float distance=math.distancesq(em.GetComponentData<LocalTransform>(unit).Position.xz,line.xz);
                if(em.HasComponent<SelectedUnitTag>(unit)) distance-=100000f;
                if(distance<nearest) {nearest=distance;actor=unit;}
            }
            if(!IsLiveDefenseActor(em,actor)) return;
            foreach(var warning in em.GetBuffer<ThreatWarningRecord>(root,true))
                if(warning.Resolved==0 && warning.Source!=ThreatWarningSourceKind.ScoutReport &&
                    warning.Stale==0 && IsLiveDefenseActor(em,warning.ObservedTarget))
                {contact=warning.ObservedTarget;break;}
            bool moving=em.HasComponent<UnitPathRequest>(actor) || em.HasComponent<UnitPathFollow>(actor);
            float range=em.HasComponent<UnitAttack>(actor) ? math.max(10,em.GetComponentData<UnitAttack>(actor).Range-10) : 20;
            bool atLine=math.distance(em.GetComponentData<LocalTransform>(actor).Position.xz,line.xz)<=range;
            bool holding=em.HasComponent<HoldPositionOrderTag>(actor);
            next.SourceEntity=actor; next.TargetEntity=contact; next.WorldPosition=line;
            next.CanExecute=0; next.ActionLabel=WaitAction;
            next.Title=step==9 ? "mission.m03.clarity.vanguard" : "mission.m03.clarity.main";
            next.RecommendationKind=!atLine || moving ? AssistantRecommendationKind.Move :
                !holding ? AssistantRecommendationKind.DefensiveAlert : AssistantRecommendationKind.CameraFocus;
            next.Body=ResolveDefenseBattleBody(em.HasComponent<SelectedUnitTag>(actor),moving,atLine,holding,contact!=Entity.Null);
            // A confirmed contact is intelligence, not an order to leave the defensive line.
        }

        internal static Unity.Collections.FixedString128Bytes ResolveDefenseBattleBody(bool selected,
            bool moving,bool atLine,bool holding,bool contact)
        {
            if(!selected) return "mission.m03.clarity.select";
            if(moving) return "mission.m03.clarity.moving";
            if(!atLine) return "mission.m03.clarity.move";
            if(!holding) return "mission.m03.clarity.hold";
            return contact ? "mission.m03.clarity.engage" : "mission.m03.clarity.wait";
        }

        private static bool IsLiveDefenseActor(EntityManager em,Entity unit) => em.Exists(unit) &&
            em.HasComponent<LocalTransform>(unit) && em.HasComponent<UnitHealth>(unit) &&
            em.GetComponentData<UnitHealth>(unit).Current>0;
    }
}
