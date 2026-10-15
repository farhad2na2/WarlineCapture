using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Game.Tactical.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public static class SkirmishEnemyCapabilityService
    {
        public const byte FeatureVersion=1;
        public sealed class OwnActor
        {
            public Entity Entity;
            public FixedString64Bytes StableId;
            public uint GroupId,ReservationId;
            public SkirmishRoleKind Role;
            public float3 Position;
            public bool Air,Carrier;
        }
        public static bool EnsureSession(EntityManager em,Entity session)
        {
            if(!em.Exists(session)||!em.HasComponent<SkirmishEnemyPlannerV1Component>(session))return false;
            bool state=em.HasComponent<SkirmishEnemyCapabilityStateV1Component>(session);
            bool operations=em.HasBuffer<SkirmishEnemyCapabilityOperationV1>(session),plans=em.HasBuffer<SkirmishEnemyTransportPlanV1>(session);
            if(state||operations||plans)return state&&operations&&plans&&SkirmishEnemyCapabilityCheckpoint.ValidState(em.GetComponentData<SkirmishEnemyCapabilityStateV1Component>(session));
            em.AddComponentData(session,new SkirmishEnemyCapabilityStateV1Component{Version=FeatureVersion,NextOperationId=1});
            em.AddBuffer<SkirmishEnemyCapabilityOperationV1>(session);em.AddBuffer<SkirmishEnemyTransportPlanV1>(session);return true;
        }
        public static List<OwnActor> ReadOwnActors(EntityManager em,Entity session)
        {
            var result=new List<OwnActor>();
            if(!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return result;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(SkirmishUnitRoleComponent),typeof(LocalTransform));
            using var entities=q.ToEntityArray(Allocator.Temp);
            foreach(var actor in entities)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(!own.SessionId.Equals(attempt)||!SkirmishActorCommandAuthority.IsLivingOwnedActor(em,actor,2))continue;
                var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role;
                bool air=em.HasComponent<UnitAirComponent>(actor);
                bool carrier=SkirmishEnemyCapabilityCheckpoint.IsCarrierRole(role)&&((em.HasComponent<UnitTransportCapacity>(actor)&&em.GetComponentData<UnitTransportCapacity>(actor).SoldierCapacity>0)||
                    (em.HasComponent<UnitTransportCargoCapacity>(actor)&&em.GetComponentData<UnitTransportCargoCapacity>(actor).VehicleCapacity>0));
                if(!air&&!carrier&&role!=SkirmishRoleKind.Radar)continue;
                // Aircraft and recon are bound only to a real paid, delivered receipt.
                uint group=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId;
                if((air||role==SkirmishRoleKind.Radar)&&!PaidReceipt(em,session,own.ReservationId,role,group))
                {
                    // A real authored initial helicopter has no purchase receipt.
                    // Bind its exact immutable force/member identity instead; never
                    // reinterpret an unpaid produced aircraft as a starting actor.
                    var setup=em.HasComponent<SkirmishResolvedSetupRecord>(session)?em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup:null;
                    if(own.ReservationId!=0||!air||!carrier||own.IsStructure!=0||own.CapacityReleased!=0||
                        !em.HasComponent<UnitSourcePrefabKey>(actor)||
                        SpawnableKeyText.Normalize(em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString())!=SpawnableKeyText.Normalize(SkirmishRoleCatalogConfig.RuntimePrefabKey(role))||
                        !SkirmishGenericGroundEnemyRollout.IsAuthoredStartingTransport(setup,own.StableObjectId.ToString(),role,own.FactionId)&&
                        !SkirmishEnemyObjectivePolicyScope.IsAuthoredStartingTransport(setup,own.StableObjectId.ToString(),role,own.FactionId))continue;
                }
                result.Add(new OwnActor{Entity=actor,StableId=own.StableObjectId,ReservationId=own.ReservationId,
                    GroupId=group,Role=role,
                    Position=em.GetComponentData<LocalTransform>(actor).Position,Air=air,Carrier=carrier});
            }
            result.Sort((a,b)=>string.CompareOrdinal(a.StableId.ToString(),b.StableId.ToString()));return result;
        }
        internal static List<OwnActor> ReadUnreservedOwnActors(EntityManager em,Entity session,ISet<uint> currentDecisionRepairGroups)
        {
            var actors=ReadOwnActors(em,session);var reserved=SkirmishEnemyVehicleRepairPolicy.ReservedGroups(em,session);
            if(currentDecisionRepairGroups!=null)reserved.UnionWith(currentDecisionRepairGroups);
            actors.RemoveAll(actor=>reserved.Contains(actor.GroupId));return actors;
        }
        public static Dictionary<SkirmishRoleKind,int> ReadLivingRoleCounts(EntityManager em,Entity session)
        {
            var result=new Dictionary<SkirmishRoleKind,int>();
            if(!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return result;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var q=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),ComponentType.ReadOnly<SkirmishUnitRoleComponent>(),ComponentType.ReadOnly<SkirmishArmyGroupMembershipComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var entities=q.ToEntityArray(Allocator.Temp);
            foreach(var actor in entities)
            {
                if(!SkirmishActorCommandAuthority.IsLivingOwnedActor(em,actor,2,true)||!em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).SessionId.Equals(attempt))continue;
                var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role;
                result.TryGetValue(role,out int count);result[role]=count+1;
            }
            return result;
        }
        public static void RemoveReservedGroundGroups(EntityManager em,Entity session,SkirmishEnemyPerceptionBuilder.Snapshot facts)
        {

            var reserved=ReservedGroups(em,session);
            for(int i=facts.Groups.Count-1;i>=0;i--)if(reserved.Contains(facts.Groups[i].Id))
            {facts.CombatSupply-=facts.Groups[i].Supply;facts.Groups.RemoveAt(i);}
        }
        public static HashSet<uint> ReservedGroups(EntityManager em,Entity session)
        {
            var result=SkirmishEnemyVehicleRepairPolicy.ReservedGroups(em,session);
            if(!em.HasBuffer<SkirmishEnemyCapabilityOperationV1>(session))return result;
            var ops=em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(session,true);
            var phases=new Dictionary<uint,SkirmishEnemyCapabilityPhase>();
            foreach(var op in ops)
            {
                phases[op.OperationId]=op.Phase;
                if(op.Kind==SkirmishEnemyCapabilityKind.Transport&&op.Phase!=SkirmishEnemyCapabilityPhase.Cooldown)result.Add(op.GroupId);
            }
            if(em.HasBuffer<SkirmishEnemyTransportPlanV1>(session))foreach(var plan in em.GetBuffer<SkirmishEnemyTransportPlanV1>(session,true))
                if(phases.TryGetValue(plan.OperationId,out var phase)&&phase!=SkirmishEnemyCapabilityPhase.Cooldown)result.Add(plan.PassengerGroupId);
            return result;
        }
        // Caller invokes at the existing commander cadence. It remains the ONLY
        // unit purchase arbiter; this returns a suggestion and never pays/queues.
        public static SkirmishRoleKind Tick(EntityManager em,Entity session,SkirmishResolvedSetup setup,
            SkirmishArmyProfileConfig army,SkirmishEnemyPerceptionBuilder.Snapshot facts,
            SkirmishEnemyGroupAllocation.Package package,bool defending,float now,ref SkirmishEnemyPlannerV1Component planner,out int dispatched,int commandBudget=-1,float3? publicObjectiveDestination=null,ISet<uint> repairDecisionReserved=null)
        {
            dispatched=0;
            if(publicObjectiveDestination.HasValue && (!math.all(math.isfinite(publicObjectiveDestination.Value)) ||
                !SkirmishEnemyObjectivePolicyScope.Supports(setup)))return SkirmishRoleKind.None;
            if(setup!=null && setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault && !publicObjectiveDestination.HasValue)return SkirmishRoleKind.None;
            if(em.WorldUnmanaged.Time.DeltaTime<=0||setup.EnemyPolicy==null||!SkirmishEnemyPlannerService.TryReadActiveClock(em,session,out float actualNow)||
                actualNow!=now||!EnsureSession(em,session))return SkirmishRoleKind.None;
            bool openingDone=now>=planner.OpeningUntil;
            var actors=ReadUnreservedOwnActors(em,session,repairDecisionReserved);
            var copied=new List<SkirmishEnemyCapabilityOperationV1>();
            foreach(var op in em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(session,true))copied.Add(op);
            foreach(var op in copied)if(!actors.Exists(a=>a.StableId.Equals(op.SourceStableId))||
                (op.Kind==SkirmishEnemyCapabilityKind.Transport&&op.Phase==SkirmishEnemyCapabilityPhase.Cooldown&&now>=op.NextSurveyAt))Remove(em,session,op.OperationId);
            foreach(var actor in actors)
            {
                if(actor.GroupId==0||Read(em,session,actor.StableId,out _)||GroupAlreadyBound(em,session,actor.GroupId))continue;
                if(actor.Carrier&&actor.Role!=SkirmishRoleKind.Drone&&actor.Role!=SkirmishRoleKind.Radar)
                {
                    if(!openingDone||defending||package.Scout==null||!package.Viable)continue;
                    if(!actor.Air&&(!package.Escort.Exists(g=>g.Id==actor.GroupId)||package.Escort.Count<2))continue;
                    if(!TryPlanTransport(em,session,actor,package.Scout,setup,facts,now,publicObjectiveDestination))continue;
                }
                else Add(em,session,actor,Kind(actor),Survey(setup,facts,publicObjectiveDestination),now);
            }
            int dispatches=0;
            foreach(var actor in actors)
            {
                if(dispatches>=(commandBudget<0?setup.EnemyPolicy.MaxDispatchPerDecision:math.min(setup.EnemyPolicy.MaxDispatchPerDecision,math.max(0,commandBudget))))break;
                if(!Read(em,session,actor.StableId,out var op)||now<op.RetryAt||now<op.NextSurveyAt||(op.Phase==SkirmishEnemyCapabilityPhase.Failed&&op.Kind!=SkirmishEnemyCapabilityKind.Transport))continue;
                if(!openingDone&&op.Kind==SkirmishEnemyCapabilityKind.Transport&&
                    (op.Phase==SkirmishEnemyCapabilityPhase.Ready||op.Phase==SkirmishEnemyCapabilityPhase.Boarding||op.Phase==SkirmishEnemyCapabilityPhase.Transit))
                    AbortTransport(em,session,actor,ref op,now);
                bool issued=op.Kind==SkirmishEnemyCapabilityKind.Transport
                    ?StepTransport(em,session,actor,ref op,now,setup,facts)
                    :StepReconOrAir(em,session,actor,ref op,now,setup,facts,defending,openingDone,now>=planner.NextScoutAt,publicObjectiveDestination);
                if(issued)dispatches++;
                if(issued&&op.CommandAccepted!=0&&(op.Kind==SkirmishEnemyCapabilityKind.Recon||op.Kind==SkirmishEnemyCapabilityKind.Radar)&&
                    (op.Phase==SkirmishEnemyCapabilityPhase.Scanning||op.Phase==SkirmishEnemyCapabilityPhase.Investigating))
                    planner.NextScoutAt=now+SkirmishEnemyPlannerService.RandomRange(ref planner.RandomState,setup.EnemyPolicy.ScoutMinSeconds,setup.EnemyPolicy.ScoutMaxSeconds);
                Store(em,session,op);
            }
            dispatched=dispatches;
            return ChooseDesiredRole(actors,facts,package,defending,setup.DeadlineSeconds-now,
                role=>SkirmishEnemyPaidProductionAdapter.CanDispatchNow(em,session,role));
        }
        public static SkirmishRoleKind ProgressionGoal(List<OwnActor> actors,SkirmishEnemyPerceptionBuilder.Snapshot facts,
            SkirmishEnemyGroupAllocation.Package package,bool defending,float remaining,SkirmishArmyProfileConfig army)
            =>ChooseDesiredRole(actors,facts,package,defending,remaining,role=>army!=null&&army.Allows(SkirmishRoleIds.ToId(role)));
        public static SkirmishRoleKind ChooseDesiredRole(List<OwnActor> actors,SkirmishEnemyPerceptionBuilder.Snapshot facts,
            SkirmishEnemyGroupAllocation.Package package,bool defending,float remaining,Func<SkirmishRoleKind,bool> feasible)
        {
            if(defending||!package.Viable||package.Reserve.Count==0||remaining<180)return SkirmishRoleKind.None;
            bool Has(SkirmishRoleKind role)=>actors.Exists(a=>a.Role==role);
            var choices=new List<SkirmishRoleKind>();
            if(facts.Visible.Exists(c=>c.Category==SkirmishPopulationCategory.Air)&&!Has(SkirmishRoleKind.Fighter))choices.Add(SkirmishRoleKind.Fighter);
            if(!Has(SkirmishRoleKind.Drone)&&!Has(SkirmishRoleKind.Radar)){choices.Add(SkirmishRoleKind.Drone);choices.Add(SkirmishRoleKind.Radar);}
            if(facts.Visible.Exists(c=>c.Structure)&&!Has(SkirmishRoleKind.Strike))choices.Add(SkirmishRoleKind.Strike);
            if(facts.Visible.Exists(c=>c.Category==SkirmishPopulationCategory.Ground)&&!Has(SkirmishRoleKind.AttackHeli)&&!Has(SkirmishRoleKind.AttackHeliLight))
            {choices.Add(SkirmishRoleKind.AttackHeli);choices.Add(SkirmishRoleKind.AttackHeliLight);}
            if(package.Scout!=null&&!actors.Exists(a=>a.Carrier&&a.Role!=SkirmishRoleKind.Radar&&a.Role!=SkirmishRoleKind.Drone)){choices.Add(SkirmishRoleKind.TransportHeli);choices.Add(SkirmishRoleKind.TransportPlane);}
            foreach(var role in choices)if(feasible(role))return role;
            return SkirmishRoleKind.None;
        }
        static bool StepReconOrAir(EntityManager em,Entity session,OwnActor actor,ref SkirmishEnemyCapabilityOperationV1 op,
            float now,SkirmishResolvedSetup setup,SkirmishEnemyPerceptionBuilder.Snapshot facts,bool defending,bool openingDone,bool scoutDue,float3? publicObjectiveDestination)
        {
            var air=actor.Air?em.GetComponentData<UnitAirComponent>(actor.Entity):default;
            op.LastObservedAirborne=air.Airborne;
            if(op.Phase==SkirmishEnemyCapabilityPhase.Returning)
            {
                if(actor.Air?(air.Airborne==0&&air.TakeoffRolling==0&&air.LandingRolling==0&&air.ReturningHome==0):
                    !em.HasComponent<UnitPathRequest>(actor.Entity)&&!em.HasComponent<UnitPathFollow>(actor.Entity)&&!em.HasComponent<UnitTarget>(actor.Entity)&&math.distancesq(actor.Position.xz,new float2(op.DestinationX,op.DestinationZ))<=36)
                {op.Phase=SkirmishEnemyCapabilityPhase.Cooldown;op.NextSurveyAt=now;op.CommandAccepted=0;}
                return false;
            }
            if(op.Phase==SkirmishEnemyCapabilityPhase.Cooldown){op.Phase=SkirmishEnemyCapabilityPhase.Ready;op.StartedAt=now;op.Retries=0;}
            if(actor.Air&&(air.ReturningHome!=0||em.GetComponentData<UnitHealth>(actor.Entity).Current*100<em.GetComponentData<UnitHealth>(actor.Entity).Max*35))
                return Return(em,session,actor,ref op,now);
            if(op.Kind==SkirmishEnemyCapabilityKind.CombatAir)
            {
                if(!TryTarget(em,actor,facts,defending?facts.Home:(float3?)null,out var target,openingDone))
                    return op.Phase==SkirmishEnemyCapabilityPhase.Engaging?Return(em,session,actor,ref op,now):false;
                if(op.Phase==SkirmishEnemyCapabilityPhase.Engaging&&op.TargetStableId.Equals(target.StableId))
                    return now-op.StartedAt>=90?Return(em,session,actor,ref op,now):false;
                if(actor.Air&&(air.HomeInitialized==0||air.ReturningHome!=0||air.LandingRolling!=0))return false;
                bool accepted=SkirmishArmyCommandService.TryIssueGroupOrder(em,session,actor.GroupId,2,SkirmishGroupOrderKind.Attack,target.Entity,target.Position,out var decision);
                if(accepted){op.Phase=SkirmishEnemyCapabilityPhase.Engaging;op.TargetStableId=target.StableId;op.StartedAt=now;Accepted(ref op,now);}
                else Reject(ref op,(ushort)decision.Reason,now);
                return true;
            }
            if(op.Kind==SkirmishEnemyCapabilityKind.Radar)
            {
                if(op.Phase==SkirmishEnemyCapabilityPhase.Investigating)
                {if(now-op.StartedAt>=setup.EnemyPolicy.ScoutMinSeconds)return Return(em,session,actor,ref op,now);return false;}
                if(!openingDone||!scoutDue)return false;
                var survey=Survey(setup,facts,publicObjectiveDestination);op.DestinationX=survey.x;op.DestinationZ=survey.z;
                return Move(em,session,actor,ref op,now,SkirmishEnemyCapabilityPhase.Investigating);
            }
            if(op.Phase==SkirmishEnemyCapabilityPhase.Scanning)
            {
                // Never reissue a Scan just because transient request/results were
                // not serialized. The actual native source order is the witness.
                if(em.HasComponent<UnitScanOrder>(actor.Entity))
                {if(em.GetComponentData<UnitScanOrder>(actor.Entity).RequestId!=op.ScanRequestId)op.Phase=SkirmishEnemyCapabilityPhase.Failed;return false;}
                return Return(em,session,actor,ref op,now);
            }
            if(!openingDone||!scoutDue)return false;
            if(actor.Air&&(air.HomeInitialized==0||air.Airborne!=0||air.TakeoffRolling!=0||air.LandingRolling!=0))return false;
            var point=Survey(setup,facts,publicObjectiveDestination);op.DestinationX=point.x;op.DestinationZ=point.z;
            op.PublicAnchorId=new FixedString64Bytes(facts.LastSeen.Count>0?"frozen.contact.investigation":"player.staging");
            bool scan=SkirmishEnemyCapabilityCommands.TryScan(em,session,actor.Entity,point,out int request,out var reason);
            if(scan){op.Phase=SkirmishEnemyCapabilityPhase.Scanning;op.ScanRequestId=request;op.StartedAt=now;Accepted(ref op,now);}
            else Reject(ref op,(ushort)reason,now);
            return true;
        }
        internal static bool StepTransport(EntityManager em,Entity session,OwnActor carrier,ref SkirmishEnemyCapabilityOperationV1 op,float now,SkirmishResolvedSetup setup,SkirmishEnemyPerceptionBuilder.Snapshot facts)
        {
            var plans=new List<SkirmishEnemyTransportPlanV1>();
            foreach(var plan in em.GetBuffer<SkirmishEnemyTransportPlanV1>(session,true))if(plan.OperationId==op.OperationId)plans.Add(plan);
            if(op.Phase==SkirmishEnemyCapabilityPhase.Failed)
            {op.Phase=SkirmishEnemyCapabilityPhase.Aborting;op.Retries=0;op.CommandAccepted=0;}
            if(plans.Count==0&&op.Phase!=SkirmishEnemyCapabilityPhase.Returning){op.Phase=SkirmishEnemyCapabilityPhase.Cooldown;op.NextSurveyAt=now+60;return false;}
            if(op.Phase==SkirmishEnemyCapabilityPhase.Ready)
            {
                // Clear the prior ground Scout order once before staged boarding;
                // never repeatedly Hold/reset a group while cargo is in transit.
                bool accepted=SkirmishArmyCommandService.TryIssueGroupOrder(em,session,plans[0].PassengerGroupId,2,SkirmishGroupOrderKind.Hold,Entity.Null,default,out var decision);
                if(accepted){op.Phase=SkirmishEnemyCapabilityPhase.Boarding;op.StartedAt=now;Accepted(ref op,now);}
                else Reject(ref op,(ushort)decision.Reason,now);
                return true;
            }
            if(op.Phase==SkirmishEnemyCapabilityPhase.Aborting)
            {
                bool pending=false;
                foreach(var plan in plans)
                {var passenger=Find(em,session,plan.PassengerStableId);if(passenger!=Entity.Null&&em.HasComponent<UnitTransportBoardingTarget>(passenger)&&
                    em.GetComponentData<UnitTransportBoardingTarget>(passenger).Transport==carrier.Entity)pending=true;}
                if(!pending||op.CommandAccepted!=0)return Return(em,session,carrier,ref op,now);
                uint group=plans[0].PassengerGroupId;
                bool accepted=SkirmishArmyCommandService.TryIssueGroupOrder(em,session,group,2,SkirmishGroupOrderKind.Hold,Entity.Null,default,out var decision);
                if(accepted)Accepted(ref op,now);else Reject(ref op,(ushort)decision.Reason,now);
                return true;
            }
            if(op.Phase==SkirmishEnemyCapabilityPhase.Boarding)
            {
                if(now-op.StartedAt>90)return AbortTransport(em,session,carrier,ref op,now);
                bool all=true;int living=0;var commands=new TransportBoardingCommandSystem();
                foreach(var plan in plans)
                {
                    Entity passenger=Find(em,session,plan.PassengerStableId);
                    if(passenger==Entity.Null||!SkirmishActorCommandAuthority.IsLivingOwnedActor(em,passenger,2,true))continue; // Actual casualty.
                    if(!em.HasComponent<SkirmishArmyGroupMembershipComponent>(passenger)||em.GetComponentData<SkirmishArmyGroupMembershipComponent>(passenger).GroupId!=plan.PassengerGroupId)return AbortTransport(em,session,carrier,ref op,now);
                    living++;
                    if(em.HasComponent<UnitTransportPassenger>(passenger))
                    {if(em.GetComponentData<UnitTransportPassenger>(passenger).Transport!=carrier.Entity)return AbortTransport(em,session,carrier,ref op,now);continue;}
                    all=false;
                    if(em.HasComponent<UnitTransportBoardingTarget>(passenger)&&em.GetComponentData<UnitTransportBoardingTarget>(passenger).Transport==carrier.Entity)continue;
                    var result=commands.TryIssueOwnedBoardOrderToPassenger(em,session,2,carrier.Entity,passenger,new UnitTransportAirPickupSystem(),new UnitMoveOrderSystem());
                    if(result.Accepted)Accepted(ref op,now);else Reject(ref op,(ushort)result.ReasonCode,now);
                    return true;
                }
                if(!all)return false;
                if(living==0)return AbortTransport(em,session,carrier,ref op,now);
                return Move(em,session,carrier,ref op,now,SkirmishEnemyCapabilityPhase.Transit);
            }
            if(op.Phase==SkirmishEnemyCapabilityPhase.Transit)
            {
                if(now-op.StartedAt>180)return AbortTransport(em,session,carrier,ref op,now);
                if(math.distancesq(carrier.Position.xz,new float2(op.DestinationX,op.DestinationZ))>12*12)return false;
                if(!SafeDestination(facts,new float3(op.DestinationX,0,op.DestinationZ)))return AbortTransport(em,session,carrier,ref op,now);
                using var grid=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent));
                bool accepted;TacticalCommandReasonCode reason;
                if(carrier.Role==SkirmishRoleKind.TransportPlane)
                {
                    if(grid.CalculateEntityCount()!=1)return false;
                    var cell=GridUtils.WorldToCell(grid.GetSingleton<GridConfig>(),new float3(op.DestinationX,0,op.DestinationZ));
                    accepted=TransportBoardingCommandSystem.TryIssueOwnedDeploy(em,session,2,carrier.Entity,new UnitTransportCapacitySystem(),new UnitMoveOrderSystem(),grid,cell,out reason);
                }
                else accepted=TransportBoardingCommandSystem.TryIssueOwnedDisembark(em,session,2,carrier.Entity,new UnitTransportCapacitySystem(),new UnitMoveOrderSystem(),grid,out reason);
                if(accepted){op.Phase=SkirmishEnemyCapabilityPhase.Unloading;op.StartedAt=now;Accepted(ref op,now);}else Reject(ref op,(ushort)reason,now);
                return true;
            }
            if(op.Phase==SkirmishEnemyCapabilityPhase.Unloading)
            {
                if(PhysicallyReleased(em,session,plans)){ClearPlan(em,session,op.OperationId);return Return(em,session,carrier,ref op,now);}
                if(now-op.StartedAt>90)return AbortTransport(em,session,carrier,ref op,now);
                return false;
            }
            if(op.Phase==SkirmishEnemyCapabilityPhase.Returning)
            {
                bool home=carrier.Air?Landed(em,carrier.Entity):!em.HasComponent<UnitPathRequest>(carrier.Entity)&&!em.HasComponent<UnitPathFollow>(carrier.Entity)&&!em.HasComponent<UnitTarget>(carrier.Entity)&&math.distancesq(carrier.Position.xz,new float2(op.DestinationX,op.DestinationZ))<=36;
                if(!home)return now-op.LastIssuedAt>=180?Return(em,session,carrier,ref op,now):false;
                if(em.HasBuffer<UnitTransportPassengerElement>(carrier.Entity)&&em.GetBuffer<UnitTransportPassengerElement>(carrier.Entity,true).Length>0)
                {
                    using var grid=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent));
                    bool accepted=TransportBoardingCommandSystem.TryIssueOwnedDisembark(em,session,2,carrier.Entity,new UnitTransportCapacitySystem(),new UnitMoveOrderSystem(),grid,out var reason);
                    if(accepted)Accepted(ref op,now);else Reject(ref op,(ushort)reason,now);return true;
                }
                if(!PhysicallyReleased(em,session,plans))return false;
                ClearPlan(em,session,op.OperationId);op.Phase=SkirmishEnemyCapabilityPhase.Cooldown;op.NextSurveyAt=now+60;return false;
            }
            return false;
        }
        static bool TryPlanTransport(EntityManager em,Entity session,OwnActor carrier,SkirmishEnemyPerceptionBuilder.Group group,
            SkirmishResolvedSetup setup,SkirmishEnemyPerceptionBuilder.Snapshot facts,float now,float3? publicObjectiveDestination)
        {
            if(ReservedGroups(em,session).Contains(group.Id))return false;
            if(group.Role!=SkirmishRoleKind.Rifle||!SkirmishEnemyGroupAllocation.Healthy(group)||group.Id==carrier.GroupId)return false;
            if(!em.HasComponent<UnitTransportCapacity>(carrier.Entity)||!em.HasBuffer<UnitTransportPassengerElement>(carrier.Entity)||em.GetBuffer<UnitTransportPassengerElement>(carrier.Entity,true).Length!=0)return false;
            var passengers=new List<Entity>();
            using var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent));
            using var entities=q.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)if(em.GetComponentData<SkirmishArmyGroupMembershipComponent>(entity).GroupId==group.Id&&
                SkirmishActorCommandAuthority.IsActiveOwnedActor(em,session,2,entity))passengers.Add(entity);
            if(passengers.Count!=group.Members||passengers.Count==0||passengers.Count>em.GetComponentData<UnitTransportCapacity>(carrier.Entity).SoldierCapacity)return false;
            var destination=publicObjectiveDestination??new float3(setup.PlayerStagingWorldX,0,setup.PlayerStagingWorldZ);
            if(!SafeDestination(facts,destination))return false;
            var op=Add(em,session,carrier,SkirmishEnemyCapabilityKind.Transport,destination,now);
            Store(em,session,op);
            foreach(var passenger in passengers)em.GetBuffer<SkirmishEnemyTransportPlanV1>(session).Add(new SkirmishEnemyTransportPlanV1{
                OperationId=op.OperationId,PassengerGroupId=group.Id,PassengerStableId=em.GetComponentData<SkirmishAttemptOwnedComponent>(passenger).StableObjectId});
            return true;
        }
        static bool AbortTransport(EntityManager em,Entity session,OwnActor carrier,ref SkirmishEnemyCapabilityOperationV1 op,float now)
        {
            // Cancellation and Return are separate cadence actions. A whole-group
            // native Hold cancels pending board orders but excludes Disabled cargo.
            op.Phase=SkirmishEnemyCapabilityPhase.Aborting;op.CommandAccepted=0;op.Retries=0;op.StartedAt=now;
            return false;
        }
        static bool PhysicallyReleased(EntityManager em,Entity session,List<SkirmishEnemyTransportPlanV1> plans)
        {
            foreach(var plan in plans){var passenger=Find(em,session,plan.PassengerStableId);if(passenger!=Entity.Null&&
                (em.HasComponent<UnitTransportPassenger>(passenger)||em.HasComponent<Disabled>(passenger)||em.HasComponent<UnitTransportBoardingTarget>(passenger)||em.HasComponent<UnitTransportParachuteDropComponent>(passenger)||em.HasComponent<UnitTransportRopeDropComponent>(passenger)||
                 em.HasComponent<UnitTransportCargoDropComponent>(passenger)||em.HasComponent<UnitTransportAirdropSettleComponent>(passenger)))return false;}return true;
        }
        static bool Move(EntityManager em,Entity session,OwnActor actor,ref SkirmishEnemyCapabilityOperationV1 op,float now,SkirmishEnemyCapabilityPhase phase)
        {
            bool accepted=SkirmishArmyCommandService.TryIssueGroupOrder(em,session,actor.GroupId,2,SkirmishGroupOrderKind.Move,Entity.Null,new float3(op.DestinationX,0,op.DestinationZ),out var decision);
            if(accepted){op.Phase=phase;op.StartedAt=now;Accepted(ref op,now);}else Reject(ref op,(ushort)decision.Reason,now);return true;
        }
        static bool Return(EntityManager em,Entity session,OwnActor actor,ref SkirmishEnemyCapabilityOperationV1 op,float now)
        {
            bool accepted=UnitReturnToBaseCommandService.TryIssueOwned(em,session,2,actor.Entity);
            if(accepted)
            {
                // Observe the actual accepted native home goal. A cancelled/failed
                // ground path away from home is not a completed Return.
                float3 home=default;bool known=false;
                if(actor.Air){var air=em.GetComponentData<UnitAirComponent>(actor.Entity);home=air.HomePosition;known=air.HomeInitialized!=0;}
                else if(em.HasComponent<UnitTarget>(actor.Entity))
                {using var grid=em.CreateEntityQuery(typeof(GridConfig));if(grid.CalculateEntityCount()==1){home=GridUtils.CellToWorldCenter(grid.GetSingleton<GridConfig>(),em.GetComponentData<UnitTarget>(actor.Entity).Cell);known=true;}}
                if(known){op.DestinationX=home.x;op.DestinationZ=home.z;op.PublicAnchorId=new FixedString64Bytes("native.return.home");
                    op.Phase=SkirmishEnemyCapabilityPhase.Returning;op.StartedAt=now;Accepted(ref op,now);}
                else Reject(ref op,(ushort)TacticalCommandReasonCode.CommandUnavailable,now);
            }
            else Reject(ref op,(ushort)TacticalCommandReasonCode.CommandUnavailable,now);return true;
        }
        static bool TryTarget(EntityManager em,OwnActor source,SkirmishEnemyPerceptionBuilder.Snapshot facts,float3? near,out SkirmishEnemyPerceptionBuilder.Contact chosen,bool openingDone)
        {
            chosen=default;if(!em.HasComponent<SkirmishRoleOverlayComponent>(source.Entity)||!em.HasComponent<SkirmishAttemptOwnedComponent>(source.Entity))return false;
            var attempt=em.GetComponentData<SkirmishAttemptOwnedComponent>(source.Entity).SessionId;
            var domain=em.GetComponentData<SkirmishRoleOverlayComponent>(source.Entity).TargetDomains;float best=float.MaxValue;bool found=false;
            foreach(var contact in facts.Visible)
            {
                if(!em.Exists(contact.Entity)||!em.HasComponent<SkirmishAttemptOwnedComponent>(contact.Entity))continue;
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(contact.Entity);
                if(own.FactionId==2||!own.SessionId.Equals(attempt)||!own.StableObjectId.Equals(contact.StableId))continue;
                if(!SkirmishExpandedEngagementService.DomainAllows(domain,contact.Structure,contact.Category)||!SkirmishFogService.IsVisibleToFaction(em,contact.Entity,2))continue;
                if(!em.HasComponent<UnitHealth>(contact.Entity)||em.GetComponentData<UnitHealth>(contact.Entity).Current<=0)continue;
                if(!AllowsProactiveOrLocalDefense(openingDone,true,facts.Home,contact.Position))continue;
                if(near.HasValue&&math.distancesq(contact.Position.xz,near.Value.xz)>60*60)continue;
                float d=math.distancesq(contact.Position.xz,source.Position.xz);if(d>=best)continue;best=d;chosen=contact;found=true;
            }
            return found;
        }
        public static bool AllowsProactiveOrLocalDefense(bool openingDone,bool visible,float3 home,float3 target)
            =>visible&&(openingDone||math.distancesq(home.xz,target.xz)<=60*60);
        public static bool SafeDestination(SkirmishEnemyPerceptionBuilder.Snapshot facts,float3 destination)
        {foreach(var contact in facts.Visible)if(math.distancesq(contact.Position.xz,destination.xz)<30*30)return false;return true;}
        static float3 Survey(SkirmishResolvedSetup setup,SkirmishEnemyPerceptionBuilder.Snapshot facts,float3? publicObjectiveDestination=null)
            =>publicObjectiveDestination??(facts.LastSeen.Count>0?facts.LastSeen[0]:new float3(setup.PlayerStagingWorldX,0,setup.PlayerStagingWorldZ));
        static SkirmishEnemyCapabilityKind Kind(OwnActor actor)=>actor.Role==SkirmishRoleKind.Drone?SkirmishEnemyCapabilityKind.Recon:
            actor.Role==SkirmishRoleKind.Radar?SkirmishEnemyCapabilityKind.Radar:SkirmishEnemyCapabilityKind.CombatAir;
        static bool PaidReceipt(EntityManager em,Entity session,uint id,SkirmishRoleKind role,uint group)
        {
            if(id==0||!em.HasBuffer<SkirmishProductionReservation>(session))return false;
            foreach(var r in em.GetBuffer<SkirmishProductionReservation>(session,true))if(r.ReservationId==id)
                return r.FactionId==2&&r.Role==role&&r.ProductionGroupId==group&&r.DeliveredMembers>0&&r.Phase==SkirmishReservationPhase.Live;return false;
        }
        static FixedString64Bytes Producer(EntityManager em,Entity session,uint receiptId)
        {
            if(receiptId==0)return default;
            if(em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session))
            {var intent=em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);if(intent.ReservationId==receiptId&&!intent.ProducerStableObjectId.IsEmpty)return intent.ProducerStableObjectId;}
            int runtimeId=0;foreach(var receipt in em.GetBuffer<SkirmishProductionReservation>(session,true))if(receipt.ReservationId==receiptId)runtimeId=receipt.ProducerRuntimeId;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            FixedString64Bytes result=default;
            using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(RuntimeBuildingCombatInfo));using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities){var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);if(own.FactionId!=2||!own.SessionId.Equals(attempt)||em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId!=runtimeId)continue;if(!result.IsEmpty)return default;result=own.StableObjectId;}return result;
        }
        static bool Landed(EntityManager em,Entity actor)
        {var air=em.GetComponentData<UnitAirComponent>(actor);return air.Airborne==0&&air.ReturningHome==0&&air.TakeoffRolling==0&&air.LandingRolling==0;}
        internal static Entity Find(EntityManager em,Entity session,FixedString64Bytes stable)
        {
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;Entity result=Entity.Null;
            using var q=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var entities=q.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities){var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);if(own.FactionId!=2||!own.SessionId.Equals(attempt)||!own.StableObjectId.Equals(stable))continue;if(result!=Entity.Null)return Entity.Null;result=entity;}return result;
        }
        static SkirmishEnemyCapabilityOperationV1 Add(EntityManager em,Entity session,OwnActor actor,SkirmishEnemyCapabilityKind kind,float3 destination,float now)
        {
            var state=em.GetComponentData<SkirmishEnemyCapabilityStateV1Component>(session);
            if(state.NextOperationId==0||state.NextOperationId==uint.MaxValue)throw new InvalidOperationException("Enemy capability identity exhausted.");
            var op=new SkirmishEnemyCapabilityOperationV1{OperationId=state.NextOperationId++,GroupId=actor.GroupId,ReservationId=actor.ReservationId,SourceStableId=actor.StableId,
                ProducerStableId=Producer(em,session,actor.ReservationId),Role=actor.Role,Kind=kind,Phase=SkirmishEnemyCapabilityPhase.Ready,PublicAnchorId=new FixedString64Bytes("player.staging"),DestinationX=destination.x,DestinationZ=destination.z,StartedAt=now};
            em.SetComponentData(session,state);em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(session).Add(op);return op;
        }
        static bool GroupAlreadyBound(EntityManager em,Entity session,uint group)
        {foreach(var op in em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(session,true))if(op.GroupId==group)return true;return false;}
        static bool Read(EntityManager em,Entity session,FixedString64Bytes source,out SkirmishEnemyCapabilityOperationV1 result)
        {foreach(var op in em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(session,true))if(op.SourceStableId.Equals(source)){result=op;return true;}result=default;return false;}
        static void Store(EntityManager em,Entity session,SkirmishEnemyCapabilityOperationV1 op)
        {var buffer=em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(session);for(int i=0;i<buffer.Length;i++)if(buffer[i].OperationId==op.OperationId){buffer[i]=op;return;}}
        static void ClearPlan(EntityManager em,Entity session,uint operation)
        {var buffer=em.GetBuffer<SkirmishEnemyTransportPlanV1>(session);for(int i=buffer.Length-1;i>=0;i--)if(buffer[i].OperationId==operation)buffer.RemoveAt(i);}
        static void Remove(EntityManager em,Entity session,uint operation)
        {ClearPlan(em,session,operation);var buffer=em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(session);for(int i=buffer.Length-1;i>=0;i--)if(buffer[i].OperationId==operation)buffer.RemoveAt(i);}
        static void Accepted(ref SkirmishEnemyCapabilityOperationV1 op,float now){op.CommandAccepted=1;op.LastIssuedAt=now;op.Retries=0;op.LastReason=0;op.RetryAt=now;}
        internal static void Reject(ref SkirmishEnemyCapabilityOperationV1 op,ushort reason,float now)
        {
            op.CommandAccepted=0;op.Retries=(byte)(op.LastReason==reason?math.min(3,op.Retries+1):1);
            op.LastReason=reason;op.LastIssuedAt=now;op.RetryAt=now+5*op.Retries;
            if(op.Retries<3)return;
            if(op.Kind==SkirmishEnemyCapabilityKind.Transport)
            {
                // A rejection never releases still boarding/loaded cargo. Cleanup
                // keeps a bounded retry cadence, rather than a permanent Failed lock.
                if(op.Phase!=SkirmishEnemyCapabilityPhase.Returning)op.Phase=SkirmishEnemyCapabilityPhase.Aborting;
                op.Retries=0;op.RetryAt=now+45;
            }
            else op.Phase=SkirmishEnemyCapabilityPhase.Failed;
        }
    }
}
