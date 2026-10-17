using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using A=Game.Runtime.SkirmishEnemyObjectiveAllocationUtilitySystemHelper;

namespace Game.Runtime
{
    public static class SkirmishEnemyObjectiveCommanderService
    {
        public static SkirmishStrategyScore Evaluate(EntityManager em,Entity session,SkirmishResolvedSetup setup,SkirmishArmyProfileConfig army)
        {
            if(!SkirmishEnemyObjectivePolicyScope.Supports(setup)||setup.EnemyPolicy==null||em.WorldUnmanaged.Time.DeltaTime<=0||
                !SkirmishEnemyPlannerService.TryReadActiveClock(em,session,out float now))return default;
            // Build exact legal public perspective before any planner/economic mutation.
            var facts=SkirmishEnemyPerceptionBuilder.Build(em,session,setup);
            if(!SkirmishEnemyObjectivePublicPerspective.TryBuild(em,session,setup,facts,out var goal,out _)||
                !TryOwnGroups(em,session,facts,out var groups)||!TryPatrols(setup,out var patrols)||
                !A.TryAllocate(goal,groups,patrols,setup.EnemyRallyPadX,setup.EnemyRallyPadZ,out var allocation,out _,ReadPreviousPublicAssignments(em,session)))return default;
            if(!SkirmishEnemyPlannerService.EnsureSession(em,session,setup))return default;
            SkirmishEnemyEconomicCoordinator.Reconcile(em,session);
            var policy=setup.EnemyPolicy;var state=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
            if(state.NextActionKey>uint.MaxValue-(uint)policy.MaxDispatchPerDecision)return default;
            if(!SkirmishEnemyDecisionRules.ObserveDue(now,state.NextObservationAt,state.ResponsePending!=0,state.ResponseDueAt))return Project(state);
            state.NextObservationAt=now+policy.ObservationCadenceSeconds;
            uint eventHash=facts.EventHash;
            for(int i=0;i<goal.NodeCount;i++)
            {
                var node=goal.Node(i);eventHash=unchecked((eventHash^(uint)node.OwnerFaction)*16777619);
                eventHash=unchecked((eventHash^(uint)(node.CaptureFraction*100))*16777619);
                eventHash=unchecked((eventHash^(node.Contested?1u:0u))*16777619);
            }
            bool changed=state.LastObservationHash!=0&&state.LastObservationHash!=eventHash;
            if(changed&&state.ResponsePending==0)
            {state.ResponsePending=1;state.ResponseDueAt=now+SkirmishEnemyPlannerService.RandomRange(ref state.RandomState,policy.ResponseMinSeconds,policy.ResponseMaxSeconds);}
            state.LastObservationHash=eventHash;
            if(!SkirmishEnemyDecisionRules.Due(now,state.NextDecisionAt,state.ResponsePending!=0,state.ResponseDueAt))
            {em.SetComponentData(session,state);return Project(state);}
            state.ResponsePending=0;state.NextDecisionAt=now+policy.StrategicCadenceSeconds;
            state.LastDecisionAt=now;state.DecisionCount++;state.CurrentPlan=SkirmishEnemyPlanKind.Hold;state.CurrentScore=0;
            if(state.PlanRevision==uint.MaxValue)return default;state.PlanRevision++;
            SkirmishEnemyCapabilityService.RemoveReservedGroundGroups(em,session,facts);
            int repairDispatched=SkirmishEnemyVehicleRepairPolicy.Step(em,session,facts,math.max(1,policy.MaxDispatchPerDecision-1),out var repairReserved);
            SkirmishEnemyVehicleRepairPolicy.RemoveReserved(facts,repairReserved);
            var reserved=SkirmishEnemyCapabilityService.ReservedGroups(em,session);
            var package=CapabilityPackage(facts);
            var ownLiving=SkirmishEnemyCapabilityService.ReadLivingRoleCounts(em,session);
            var wanted=WantedRole(setup,army,facts,ownLiving);
            var capabilityGoal=SkirmishEnemyCapabilityService.ProgressionGoal(SkirmishEnemyCapabilityService.ReadOwnActors(em,session),facts,package,false,setup.DeadlineSeconds-now,army);
            var economy=SkirmishEnemyEconomicCoordinator.Tick(em,session,wanted,false,capabilityGoal,ref state,capabilityGoal).Economy;
            if(economy.SuggestedCombatRole!=SkirmishRoleKind.None)wanted=economy.SuggestedCombatRole;
            if(economy.PermitCombatPurchase&&wanted!=SkirmishRoleKind.None)
            {
                var intent=em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);
                if(SkirmishEnemyCommanderService.MayStartProduction(intent,wanted)&&SkirmishEnemyPaidProductionAdapter.CanDispatchNow(em,session,wanted))
                    SkirmishEnemyPaidProductionAdapter.TryRequest(em,session,new FixedString64Bytes("enemy.purchase."+intent.NextRequestId),wanted,state.PlanRevision,army,now);
            }
            float3? publicSurvey=null;
            foreach(var assignment in allocation)if(assignment.Task!=A.Duty.Reserve){publicSurvey=new float3(assignment.X,0,assignment.Z);break;}
            if(!publicSurvey.HasValue)
                for(int i=0;i<goal.NodeCount;i++)
                {
                    var node=goal.Node(i);
                    if(node.Provenance==SkirmishPublicGoalProvenance.PublicObjective&&node.PositionKnown)
                    {publicSurvey=new float3(node.WorldX,0,node.WorldZ);break;}
                }
            if(!publicSurvey.HasValue){em.SetComponentData(session,state);return Project(state);}
            // Same native capability owner, but explicitly supplied public objective
            // terrain replaces BA's player-staging destination for this call only.
            SkirmishEnemyCapabilityService.Tick(em,session,setup,army,facts,package,false,now,ref state,out int dispatched,
                math.max(0,policy.MaxDispatchPerDecision-1-repairDispatched),publicSurvey,repairReserved);
            dispatched+=repairDispatched;
            reserved=SkirmishEnemyCapabilityService.ReservedGroups(em,session);
            foreach(var desired in allocation)
            {
                if(dispatched>=policy.MaxDispatchPerDecision||reserved.Contains(desired.GroupId))continue;
                var group=facts.Groups.Find(g=>g.Id==desired.GroupId);if(group==null)continue;
                var previous=ReadAssignment(em,session,group.Id);
                if(now<previous.RetryAfter)continue;
                var destination=new float3(desired.X,0,desired.Z);Entity target=Entity.Null;FixedString64Bytes stable=default;
                var order=SkirmishGroupOrderKind.Move;
                if(now<state.OpeningUntil)
                {
                    destination=new float3(setup.EnemyRallyPadX,0,setup.EnemyRallyPadZ);
                    if(SkirmishEnemyCommanderService.TryVisibleTarget(facts,group,true,out var intrusion))
                    {target=intrusion.Entity;stable=intrusion.StableId;destination=intrusion.Position;order=SkirmishGroupOrderKind.Attack;}
                    else if(math.distancesq(group.Position.xz,destination.xz)<=1f)order=SkirmishGroupOrderKind.Hold;
                }
                else if(desired.Task==A.Duty.Intercept)
                {
                    // Resolve only this decision's legal contacts. Never resolve a
                    // hidden original subject by stable ID or collector membership.
                    foreach(var contact in facts.Visible)
                        if(SkirmishEnemyObjectivePublicPerspective.PublicContactId(contact.StableId.ToString())==desired.PublicGoalId&&
                            SkirmishFogService.IsVisibleToFaction(em,contact.Entity,2)&&
                            SkirmishExpandedEngagementService.DomainAllows(group.Domains,contact.Structure,contact.Category))
                        {target=contact.Entity;stable=contact.StableId;destination=contact.Position;order=SkirmishGroupOrderKind.Attack;break;}
                    if(target==Entity.Null)continue;
                }
                else if(math.distancesq(group.Position.xz,destination.xz)<=1f)order=SkirmishGroupOrderKind.Hold;
                // Pure allocation applies contact score hysteresis. Required
                // whole-squad coverage changes invalidate the prior assignment;
                // the shared native Issue owner suppresses duplicate orders.
                var duty=desired.Task==A.Duty.Reserve?SkirmishEnemyDuty.HomeReserve:
                    desired.Task==A.Duty.Intercept?SkirmishEnemyDuty.MainAssault:SkirmishEnemyDuty.Escort;
                if(SkirmishEnemyCommanderService.TryIssueObjectiveGroupOrder(em,session,group,duty,order,target,stable,destination,now,ref state,ref previous,policy))dispatched++;
                StoreAssignment(em,session,previous);state.CurrentScore=math.max(state.CurrentScore,desired.Score);
            }
            em.SetComponentData(session,state);return Project(state);
        }

        public static SkirmishRoleKind WantedRole(SkirmishResolvedSetup setup,SkirmishArmyProfileConfig army,
            SkirmishEnemyPerceptionBuilder.Snapshot facts,IReadOnlyDictionary<SkirmishRoleKind,int> own)
        {
            if(army==null||setup?.Forces==null)return SkirmishRoleKind.None;
            if(setup.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl&&facts.Groups.FindAll(g=>g.Category==SkirmishPopulationCategory.Infantry&&SkirmishEnemyGroupAllocation.Healthy(g)).Count<2)return SkirmishRoleKind.Rifle;
            if(army.Allows(SkirmishRoleIds.AntiAir)&&facts.Visible.Exists(c=>c.Category==SkirmishPopulationCategory.Air)&&(!own.TryGetValue(SkirmishRoleKind.AntiAir,out int aa)||aa==0))return SkirmishRoleKind.AntiAir;
            if(facts.Visible.Exists(c=>c.Category==SkirmishPopulationCategory.Ground)&&(!own.TryGetValue(SkirmishRoleKind.Rocketeer,out int rocketeers)||rocketeers<4))return SkirmishRoleKind.Rocketeer;
            foreach(var force in setup.Forces)
            {
                if(force.FactionId!=2||!army.Allows(SkirmishRoleIds.ToId(force.RoleKind))||
                    !SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays,force.RoleKind,out var overlay)||overlay.Damage<=0||
                    SkirmishRoleIds.Category(force.RoleKind)==SkirmishPopulationCategory.Air)continue;
                int count=own.TryGetValue(force.RoleKind,out int living)?living:0;
                if(count<force.Quantity)return force.RoleKind;
            }
            return SkirmishRoleKind.None;
        }
        private static bool TryOwnGroups(EntityManager em,Entity session,SkirmishEnemyPerceptionBuilder.Snapshot facts,out A.Group[] result)
        {
            var groups=new List<A.Group>();var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitMove));
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var group in facts.Groups)
            {
                float speed=float.MaxValue;int members=0;
                foreach(var actor in entities)
                {
                    var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if(own.SessionId!=attempt||em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId!=group.Id||
                        !SkirmishActorCommandAuthority.IsLivingOwnedActor(em,actor,2))continue;
                    speed=math.min(speed,em.GetComponentData<UnitMove>(actor).Speed);members++;
                }
                if(members!=group.Members||!float.IsFinite(speed)||speed<=0)return FailGroups(out result);
                groups.Add(new A.Group{Id=group.Id,Category=group.Category,Supply=group.Supply,Members=members,
                    X=group.Position.x,Z=group.Position.z,Speed=speed,Commandable=true,Healthy=SkirmishEnemyGroupAllocation.Healthy(group)});
            }
            result=groups.ToArray();return true;
        }
        private static bool FailGroups(out A.Group[] groups){groups=null;return false;}
        private static bool TryPatrols(SkirmishResolvedSetup setup,out A.PublicPatrolPoint[] points)
        {
            points=null;if(setup.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort)return true;
            var found=new List<A.PublicPatrolPoint>();
            foreach(var route in setup.EnemyPolicy.Routes)
            {
                if(!route.AllowsVehicles||route.Waypoints.Count<3)continue;
                var point=route.Waypoints[route.Waypoints.Count/2];
                if(found.Exists(p=>p.X==point.X&&p.Z==point.Z))continue;
                found.Add(new A.PublicPatrolPoint{PublicRouteId=route.RouteId,X=point.X,Z=point.Z});if(found.Count==2)break;
            }
            points=found.ToArray();return found.Count==2;
        }
        private static SkirmishEnemyGroupAllocation.Package CapabilityPackage(SkirmishEnemyPerceptionBuilder.Snapshot facts)
        {
            var result=new SkirmishEnemyGroupAllocation.Package();
            foreach(var group in facts.Groups)
            {
                if(!SkirmishEnemyGroupAllocation.Healthy(group)){result.Reserve.Add(group);continue;}
                if(result.Assault.Count==0){result.Assault.Add(group);continue;}
                if(result.Escort.Count==0){result.Escort.Add(group);continue;}
                if(result.Reserve.Count==0){result.Reserve.Add(group);continue;}
                if(result.Scout==null&&group.Role==SkirmishRoleKind.Rifle){result.Scout=group;continue;}
                result.Escort.Add(group);
            }
            return result;
        }
        private static SkirmishEnemyGroupAssignmentV1 ReadAssignment(EntityManager em,Entity session,uint group)
        {foreach(var row in em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session))if(row.GroupId==group)return row;return default;}
        private static A.Assignment[] ReadPreviousPublicAssignments(EntityManager em,Entity session)
        {
            if(!em.HasBuffer<SkirmishEnemyGroupAssignmentV1>(session))return Array.Empty<A.Assignment>();
            var result=new List<A.Assignment>();
            foreach(var row in em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session))
                if(row.Order==SkirmishGroupOrderKind.Attack&&!row.TargetStableId.IsEmpty)
                    result.Add(new A.Assignment{GroupId=row.GroupId,Task=A.Duty.Intercept,PublicGoalId=SkirmishEnemyObjectivePublicPerspective.PublicContactId(row.TargetStableId.ToString())});
            return result.ToArray();
        }
        private static void StoreAssignment(EntityManager em,Entity session,SkirmishEnemyGroupAssignmentV1 value)
        {
            var rows=em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session);
            for(int i=0;i<rows.Length;i++)if(rows[i].GroupId==value.GroupId){rows[i]=value;return;}rows.Add(value);
        }
        private static SkirmishStrategyScore Project(SkirmishEnemyPlannerV1Component state)=>new SkirmishStrategyScore
        {Priority=SkirmishStrategyPriority.Hold,Total=state.CurrentScore,Field="enemy.objective.public.v1"};
    }
}
