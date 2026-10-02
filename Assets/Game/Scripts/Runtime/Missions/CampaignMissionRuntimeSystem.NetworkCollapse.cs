using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes NetworkId=CampaignMissionSequence.NetworkCollapse;
        private bool TryAdvanceNetworkCollapse(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!runtime.MissionId.Equals(NetworkId))return false;
            var em=system.EntityManager;var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            if(runtime.Outcome!=MissionOutcomeKind.None)return true;
            if(runtime.Phase==MissionPhaseKind.Preparing){if(TryTransition(in runtime,MissionPhaseKind.InteractiveBrief,MissionOutcomeKind.None,MissionReturnDestinationKind.None,out var next))em.SetComponentData(root,next);return true;}
            if(runtime.Phase==MissionPhaseKind.InteractiveBrief&&facts.InteractiveBriefCompleted!=0){if(TryTransition(in runtime,MissionPhaseKind.FindSquad,MissionOutcomeKind.None,MissionReturnDestinationKind.None,out var next))em.SetComponentData(root,next);return true;}
            if(facts.CommandSquadSpawned==0||!SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata)||!metadata.Blob.IsCreated)return true;
            if(!em.HasComponent<CampaignMissionNetworkCollapseState>(root)||em.GetComponentData<CampaignMissionNetworkCollapseState>(root).Initialized==0)InitializeNetworkCollapse(em,root,in runtime,ref metadata.Blob.Value);
            var m=em.GetComponentData<CampaignMissionNetworkCollapseState>(root);if(!CampaignMissionNetworkCollapseRuleUtility.Matches(in m,in runtime))return true;
            ProjectNetworkBuildings(em,root,ref metadata.Blob.Value,ref m);
            var scope=new SupportFuelScopeComponent { Storage=m.Reserve,Required=1 };if(em.HasComponent<SupportFuelScopeComponent>(root))em.SetComponentData(root,scope);else em.AddComponentData(root,scope);
            bool opening=em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)&&em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).SessionToken.Equals(runtime.SessionToken)&&em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage>=6;
            bool active=runtime.Phase==MissionPhaseKind.Engage&&opening&&m.Ready!=0;
            ApplyNetworkActivationPolicy(em,root,active);
            int delta=active?(int)math.round(SystemAPI.Time.DeltaTime*1000):0;if(active)m.ElapsedMilliseconds=SaturatingAddMilliseconds(m.ElapsedMilliseconds,SystemAPI.Time.DeltaTime);
            var members=em.GetBuffer<CampaignMissionNetworkCollapseMember>(root);int initialized=0,hostiles=0,defeated=0,staff=0,staffDead=0,escorts=0,losses=0;int3 guards=default;int3 nodes=default;
            using var commands=new EntityCommandBuffer(Allocator.Temp);
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];bool exists=em.Exists(member.Entity)&&em.HasComponent<UnitHealth>(member.Entity);
                if(!exists&&member.Dead==0){MarkNetworkIntegrity(ref m,"Original actor disappeared: "+member.Kind);continue;}
                var hp=exists?em.GetComponentData<UnitHealth>(member.Entity):default;
                if(hp.Max>0)member.HealthInitialized=1;if(member.HealthInitialized!=0&&hp.Current<=0)member.Dead=1;initialized+=member.HealthInitialized;members[i]=member;
                bool node=(byte)member.Kind>=(byte)NetworkCollapseMemberKind.NodeOne&&(byte)member.Kind<=(byte)NetworkCollapseMemberKind.NodeThree;
                bool guard=(byte)member.Kind>=(byte)NetworkCollapseMemberKind.GuardOne&&(byte)member.Kind<=(byte)NetworkCollapseMemberKind.GuardThree;
                if(node||guard){hostiles++;defeated+=member.Dead;}
                if(guard&&member.Dead==0)guards[(int)member.Kind-(int)NetworkCollapseMemberKind.GuardOne]++;
                if(node)
                {
                    int n=(int)member.Kind-(int)NetworkCollapseMemberKind.NodeOne;bool verified=n==0?m.NodeOneVerified!=0:n==1?m.NodeTwoVerified!=0:m.NodeThreeVerified!=0;
                    if(member.Dead!=0){nodes[n]=1;if(!verified)m.Failure=NetworkCollapseFailure.UnverifiedNodeDestroyed;}
                    else if(exists)
                    {
                        bool suppressed=!active||!verified;
                        if(suppressed&&!em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity))commands.AddComponent<CampaignMissionCombatSuppressedTag>(member.Entity);
                        if(!suppressed&&em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity))commands.RemoveComponent<CampaignMissionCombatSuppressedTag>(member.Entity);
                        if(em.HasComponent<SupportTargetEligibilityComponent>(member.Entity)){var eligibility=em.GetComponentData<SupportTargetEligibilityComponent>(member.Entity);eligibility.CurrentlyVisible=eligibility.HostileConfirmed=(byte)(!suppressed?1:0);em.SetComponentData(member.Entity,eligibility);}
                    }
                }
                if(member.Kind==NetworkCollapseMemberKind.CivicStaff){staff++;staffDead+=member.Dead;}
                if(member.Kind==NetworkCollapseMemberKind.Escort){losses+=member.Dead;if(member.Dead==0)escorts++;}
                if(member.Dead!=0&&member.Kind==NetworkCollapseMemberKind.Engineer)m.Failure=NetworkCollapseFailure.EngineerLost;
                if(member.Dead!=0&&member.Kind==NetworkCollapseMemberKind.Carrier)m.Failure=NetworkCollapseFailure.CarrierLost;
            }
            int rosterLength=members.Length;commands.Playback(em);
            if(rosterLength!=21||hostiles!=9||staff!=4)MarkNetworkIntegrity(ref m,"Wrong closed original roster");
            if(staffDead>0)m.Failure=NetworkCollapseFailure.StaffLost;
            if(initialized==21&&escorts==0&&defeated<hostiles)m.Failure=NetworkCollapseFailure.EscortLost;
            if(m.ElapsedMilliseconds>=CampaignMissionNetworkCollapseRuleUtility.DeadlineMilliseconds)m.Failure=NetworkCollapseFailure.Deadline;
            m.NodeOneDisabled=(byte)nodes.x;m.NodeTwoDisabled=(byte)nodes.y;m.NodeThreeDisabled=(byte)nodes.z;m.MilitaryCleared=(byte)(hostiles==9&&defeated==9?1:0);
            int stage=CampaignMissionNetworkCollapseRuleUtility.Stage(in m);
            if(stage is 1 or 3 or 5)
            {
                int n=(stage-1)/2;float3 point=n==0?m.ReconOne:n==1?m.ReconTwo:m.ReconThree;
                bool qualified=active&&CampaignMissionNetworkCollapseRuleUtility.CanVerify(n,in m,guards[n])&&CitywideStoppedAt(em,m.Engineer,point,6)&&NetworkLive(em,m.Audit);
                m.VerificationMilliseconds=CampaignMissionNetworkCollapseRuleUtility.AdvanceHold(m.VerificationMilliseconds,delta,qualified);
                if(m.VerificationMilliseconds>=6000){if(n==0)m.NodeOneVerified=1;if(n==1)m.NodeTwoVerified=1;if(n==2)m.NodeThreeVerified=1;m.VerificationMilliseconds=0;}
            }
            else m.VerificationMilliseconds=0;
            bool recovery=active&&m.NodeOneVerified!=0&&m.NodeTwoVerified!=0&&m.NodeThreeVerified!=0&&m.MilitaryCleared!=0&&CitywideStoppedAt(em,m.Engineer,m.AuditGate,6)&&NetworkLive(em,m.Audit);
            if(m.AuditRecovered==0){m.RecoveryMilliseconds=CampaignMissionNetworkCollapseRuleUtility.AdvanceHold(m.RecoveryMilliseconds,delta,recovery);if(m.RecoveryMilliseconds>=6000)m.AuditRecovered=1;}
            m.EngineerAboard=(byte)(NetworkEngineerAboard(em,in m)?1:0);
            bool extract=active&&m.AuditRecovered!=0&&m.EngineerAboard!=0&&NetworkLive(em,m.Carrier)&&CitywideStoppedAt(em,m.Carrier,m.Extraction,8)&&m.MilitaryCleared!=0&&m.Failure==NetworkCollapseFailure.None;
            m.ExtractionMilliseconds=CampaignMissionNetworkCollapseRuleUtility.AdvanceHold(m.ExtractionMilliseconds,delta,extract);m.Extracted=(byte)(m.ExtractionMilliseconds>=6000?1:0);
            if(m.NodeOneDisabled!=0)m.CommsReady=1;
            if(initialized==21&&m.CivicOne!=Entity.Null&&m.CivicTwo!=Entity.Null&&m.Audit!=Entity.Null&&m.Reserve!=Entity.Null)m.Ready=1;
            facts.ElapsedMilliseconds=m.ElapsedMilliseconds;facts.HostileTotalCount=hostiles;facts.HostileDefeatedCount=defeated;facts.CivilianTotalCount=staff;facts.CivilianLossCount=staffDead;facts.SquadLossCount=losses;facts.CommandSquadAlive=(byte)(NetworkLive(em,m.Engineer)?1:0);
            facts.NetworkNodesVerified=(byte)(m.NodeOneVerified+m.NodeTwoVerified+m.NodeThreeVerified);facts.NetworkNodesDisabled=(byte)(m.NodeOneDisabled+m.NodeTwoDisabled+m.NodeThreeDisabled);facts.NetworkAuditRecovered=m.AuditRecovered;facts.NetworkExtracted=m.Extracted;facts.NetworkEngineerAboard=m.EngineerAboard;facts.NetworkCommsReady=m.CommsReady;facts.NetworkFailure=m.Failure;
            if(m.Failure==NetworkCollapseFailure.Integrity)facts.HostileRosterIntegrityFault=1;
            em.SetComponentData(root,m);em.SetComponentData(root,facts);
            if(CampaignMissionNetworkCollapseRuleUtility.TryAdvance(in runtime,in facts,in m,opening,out var changed))em.SetComponentData(root,changed);
            return true;
        }
        internal static Entity ResolveNetworkEscort(EntityManager em,Entity root)
        {
            Entity best=Entity.Null;float range=0;foreach(var member in em.GetBuffer<CampaignMissionNetworkCollapseMember>(root,true))
            {if(member.Kind!=NetworkCollapseMemberKind.Escort||member.Dead!=0||!NetworkLive(em,member.Entity)||!em.HasComponent<UnitCombat>(member.Entity)||!em.HasComponent<UnitAttack>(member.Entity))continue;var weapon=em.GetComponentData<UnitAttack>(member.Entity);if(em.GetComponentData<UnitCombat>(member.Entity).CanAttack!=0&&weapon.Damage>0&&weapon.Range>range){range=weapon.Range;best=member.Entity;}}return best;
        }
        internal static bool NetworkEngineerAboard(EntityManager em,in CampaignMissionNetworkCollapseState m)
        {
            if(!NetworkLive(em,m.Engineer)||!NetworkLive(em,m.Carrier)||!em.HasComponent<UnitTransportPassenger>(m.Engineer)||em.GetComponentData<UnitTransportPassenger>(m.Engineer).Transport!=m.Carrier||!em.HasBuffer<UnitTransportPassengerElement>(m.Carrier))return false;
            foreach(var passenger in em.GetBuffer<UnitTransportPassengerElement>(m.Carrier,true))if(passenger.Passenger==m.Engineer)return true;return false;
        }
        internal static bool NetworkLive(EntityManager em,Entity e)=>em.Exists(e)&&em.HasComponent<UnitHealth>(e)&&em.GetComponentData<UnitHealth>(e).Max>0&&em.GetComponentData<UnitHealth>(e).Current>0;
        private static void MarkNetworkIntegrity(ref CampaignMissionNetworkCollapseState m,string detail){if(m.Failure!=NetworkCollapseFailure.Integrity)UnityEngine.Debug.LogError("[NetworkIntegrity] "+detail);m.Failure=NetworkCollapseFailure.Integrity;}
        internal static void InitializeNetworkCollapse(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,ref OperationMapBlob map)
        {
            var m=new CampaignMissionNetworkCollapseState {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion,Initialized=1};
            string[] suffix={"recon_1","recon_2","recon_3","approach_1","approach_2","approach_3","audit_gate","extraction"};
            for(int i=0;i<suffix.Length;i++){if(!CampaignMissionSpawnSystem.TryFindAnchor(ref map,new FixedString64Bytes("anchor.ch05.m03."+suffix[i]),out var a))MarkNetworkIntegrity(ref m,"Missing anchor "+suffix[i]);if(i==0)m.ReconOne=a.Position;if(i==1)m.ReconTwo=a.Position;if(i==2)m.ReconThree=a.Position;if(i==3)m.ApproachOne=a.Position;if(i==4)m.ApproachTwo=a.Position;if(i==5)m.ApproachThree=a.Position;if(i==6)m.AuditGate=a.Position;if(i==7)m.Extraction=a.Position;}
            if(!em.HasBuffer<CampaignMissionNetworkCollapseMember>(root))em.AddBuffer<CampaignMissionNetworkCollapseMember>(root);var roster=em.GetBuffer<CampaignMissionNetworkCollapseMember>(root);roster.Clear();
            using var q=em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent),typeof(UnitHealth),typeof(LocalTransform));using var entities=q.ToEntityArray(Allocator.Temp);using var commands=new EntityCommandBuffer(Allocator.Temp);
            foreach(var e in entities)
            {
                var role=em.GetComponentData<CampaignMissionUnitRoleComponent>(e);if(!role.SessionToken.Equals(runtime.SessionToken))continue;
                var kind=role.MissionRoleId.ToString() switch {"role.friendly.escort"=>NetworkCollapseMemberKind.Escort,"role.friendly.engineer"=>NetworkCollapseMemberKind.Engineer,"role.friendly.extraction_apc"=>NetworkCollapseMemberKind.Carrier,"role.hostile.node.1"=>NetworkCollapseMemberKind.NodeOne,"role.hostile.node.2"=>NetworkCollapseMemberKind.NodeTwo,"role.hostile.node.3"=>NetworkCollapseMemberKind.NodeThree,"role.hostile.guard.1"=>NetworkCollapseMemberKind.GuardOne,"role.hostile.guard.2"=>NetworkCollapseMemberKind.GuardTwo,"role.hostile.guard.3"=>NetworkCollapseMemberKind.GuardThree,"role.civilian.protected"=>NetworkCollapseMemberKind.CivicStaff,_=>(NetworkCollapseMemberKind)255};
                if((byte)kind==255){MarkNetworkIntegrity(ref m,"Unknown role "+role.MissionRoleId);continue;}roster.Add(new CampaignMissionNetworkCollapseMember {Entity=e,Kind=kind});
                if(kind==NetworkCollapseMemberKind.Engineer)m.Engineer=e;if(kind==NetworkCollapseMemberKind.Carrier)m.Carrier=e;
                bool node=(byte)kind>=(byte)NetworkCollapseMemberKind.NodeOne&&(byte)kind<=(byte)NetworkCollapseMemberKind.NodeThree;
                if(kind==NetworkCollapseMemberKind.Engineer||kind==NetworkCollapseMemberKind.CivicStaff||node)QualifyCitywideEngineer(em,e);
                if(kind==NetworkCollapseMemberKind.CivicStaff||(byte)kind>=(byte)NetworkCollapseMemberKind.NodeOne&&(byte)kind<=(byte)NetworkCollapseMemberKind.GuardThree){if(!em.HasComponent<CampaignMissionStationaryUnitTag>(e))commands.AddComponent<CampaignMissionStationaryUnitTag>(e);}
                if(node&&!em.HasComponent<CampaignMissionCombatSuppressedTag>(e))commands.AddComponent<CampaignMissionCombatSuppressedTag>(e);
                if(em.HasComponent<UnitMovementBehavior>(e)){var movement=em.GetComponentData<UnitMovementBehavior>(e);movement.AllowIdleWander=0;em.SetComponentData(e,movement);}
                if((byte)kind>=(byte)NetworkCollapseMemberKind.NodeOne&&(byte)kind<=(byte)NetworkCollapseMemberKind.GuardThree&&em.HasComponent<UnitFuelConsumption>(e)){var fuel=em.GetComponentData<UnitFuelConsumption>(e);fuel.Enabled=0;em.SetComponentData(e,fuel);}
            }
            int count=roster.Length;commands.Playback(em);if(count!=21||m.Engineer==Entity.Null||m.Carrier==Entity.Null)MarkNetworkIntegrity(ref m,"Original roster incomplete");
            if(!em.HasBuffer<CampaignMissionNetworkBuildingRequest>(root))em.AddBuffer<CampaignMissionNetworkBuildingRequest>(root);var bindings=em.GetBuffer<CampaignMissionNetworkBuildingRequest>(root);bindings.Clear();for(int i=0;i<4;i++)bindings.Add(default);
            if(em.HasComponent<CampaignMissionNetworkCollapseState>(root))em.SetComponentData(root,m);else em.AddComponentData(root,m);
        }
        private static void ApplyNetworkActivationPolicy(EntityManager em,Entity root,bool active)
        {
            foreach(var member in em.GetBuffer<CampaignMissionNetworkCollapseMember>(root,true))
            {if(member.Dead!=0||!em.Exists(member.Entity)||!em.HasComponent<ArmorBreakUnitActivationState>(member.Entity))continue;var original=em.GetComponentData<ArmorBreakUnitActivationState>(member.Entity);
             if(em.HasComponent<UnitFuelConsumption>(member.Entity)){var fuel=em.GetComponentData<UnitFuelConsumption>(member.Entity);byte enabled=active?original.OriginalFuelEnabled:(byte)0;if(fuel.Enabled!=enabled){fuel.Enabled=enabled;em.SetComponentData(member.Entity,fuel);if(enabled!=0&&em.HasComponent<UnitFuelConsumptionState>(member.Entity))em.SetComponentData(member.Entity,default(UnitFuelConsumptionState));}}
             if(em.HasComponent<UnitCombat>(member.Entity)){var combat=em.GetComponentData<UnitCombat>(member.Entity);combat.AutoEngage=active?original.OriginalAutoEngage:(byte)0;em.SetComponentData(member.Entity,combat);}}
        }
    }
}
