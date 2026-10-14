using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Isolated ledger actors exercise the real shared group-order and planner
    // paths. This is neither native prefab delivery nor normal-match acceptance.
    public sealed class SkirmishEnemyCommanderTests
    {
        static SkirmishResolvedSetup Setup(int seed, SkirmishDifficultyId difficulty)
        {
            var setup = new SkirmishResolvedSetup { CatalogId="policy.fixture",DefinitionId="policy.fixture",
                ContentHash="isolated-orders",Seed=seed,DifficultyId=difficulty,Readiness=SkirmishReadinessStage.Field,
                ObjectiveKind=SkirmishObjectiveKind.BaseAssault,EnemyRallyPadX=50,PlayerBaseWorldX=500 };
            Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup,null,out var policy));
            setup.EnemyPolicy=policy;return setup;
        }
        static Entity Session(EntityManager em, SkirmishResolvedSetup setup)
        {
            var session=em.CreateEntity();
            em.AddComponentData(session,new SkirmishExpandedSessionComponent { SessionId=new FixedString64Bytes("isolated-orders"),Phase=SkirmishSessionPhase.Playing });
            em.AddComponentData(session,new SkirmishObjectiveClockComponent { Playing=1 });
            em.AddComponentData(session,new SkirmishEnemyStrategyComponent());
            em.AddBuffer<SkirmishArmyGroupRecord>(session);
            // Native fresh sessions also own an empty paid receipt ledger before any purchase.
            em.AddBuffer<SkirmishProductionReservation>(session);
            Assert.IsTrue(SkirmishEnemyPlannerService.EnsureSession(em,session,setup));return session;
        }
        static void AddGroup(EntityManager em,Entity session,uint id,SkirmishRoleKind role,float x,bool accepted=true)
        {
            em.GetBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord {GroupId=id,FactionId=2,Role=role,
                Domain=SkirmishPopulationCategory.Infantry,MemberCount=4,AliveCount=accepted?4:0});
            for(int i=0;i<4;i++)
            {
                var actor=em.CreateEntity();
                em.AddComponentData(actor,new SkirmishAttemptOwnedComponent { SessionId=new FixedString64Bytes("isolated-orders"),FactionId=2,
                    StableObjectId=new FixedString64Bytes("g"+id+"."+i) });
                em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent {GroupId=id,Domain=SkirmishPopulationCategory.Infantry});
                em.AddComponentData(actor,new SkirmishUnitRoleComponent {Role=role,Category=SkirmishPopulationCategory.Infantry,SupplyCost=1});
                em.AddComponentData(actor,new SkirmishRoleOverlayComponent { Damage=10,TargetDomains=role==SkirmishRoleKind.Rocketeer?
                    SkirmishTargetDomain.Structure:SkirmishTargetDomain.Infantry });
                em.AddComponentData(actor,new UnitHealth {Current=100,Max=100});
                em.AddComponentData(actor,LocalTransform.FromPosition(x,0,0));
            }
        }
        static void At(EntityManager em,Entity session,SkirmishResolvedSetup setup,float elapsed)
        {
            em.SetComponentData(session,new SkirmishObjectiveClockComponent {Playing=1,ElapsedSeconds=elapsed});
            SkirmishEnemyCommanderService.Evaluate(em,session,setup,null);
        }
        static SkirmishEnemyGroupAssignmentV1 Assignment(EntityManager em,Entity session,uint group)
        {
            foreach(var row in em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session))if(row.GroupId==group)return row;
            throw new InvalidOperationException("Missing actual assignment");
        }
        [Test]
        public void WholeGroupsDispatchBoundedThenAcceptedHoldIsDeduplicatedAcrossConfigsAndSeeds()
        {
            foreach(var difficulty in new[] {SkirmishDifficultyId.Recruit,SkirmishDifficultyId.Commander})
            foreach(int seed in new[] {17,9123})
            {
                using var world=new World(nameof(WholeGroupsDispatchBoundedThenAcceptedHoldIsDeduplicatedAcrossConfigsAndSeeds));
                var em=world.EntityManager;var setup=Setup(seed,difficulty);var session=Session(em,setup);
                AddGroup(em,session,1,SkirmishRoleKind.Rocketeer,50);AddGroup(em,session,2,SkirmishRoleKind.Rifle,50);AddGroup(em,session,3,SkirmishRoleKind.Rifle,50);
                At(em,session,setup,100);Assert.AreEqual(2,em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session).Length);
                Assert.AreEqual(SkirmishGroupOrderKind.Hold,em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
                At(em,session,setup,104);Assert.AreEqual(3,em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session).Length);
                uint action=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session).NextActionKey;
                At(em,session,setup,108);Assert.AreEqual(action,em.GetComponentData<SkirmishEnemyPlannerV1Component>(session).NextActionKey);
                foreach(var row in em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session)) Assert.AreEqual(1,row.OrderAccepted);
                using var held=em.CreateEntityQuery(typeof(HoldPositionOrderTag));Assert.AreEqual(12,held.CalculateEntityCount());
                var before=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
                em.SetComponentData(session,new SkirmishObjectiveClockComponent {Playing=1,Paused=1,ElapsedSeconds=109});
                SkirmishEnemyCommanderService.Evaluate(em,session,setup,null);
                Assert.AreEqual(before.DecisionCount,em.GetComponentData<SkirmishEnemyPlannerV1Component>(session).DecisionCount);
            }
        }
        [Test]
        public void FailedNativeOrdersHaveBoundedRecoveryAndTimeCannotResetThem()
        {
            using var world=new World(nameof(FailedNativeOrdersHaveBoundedRecoveryAndTimeCannotResetThem));var em=world.EntityManager;
            var setup=Setup(813,SkirmishDifficultyId.Regular);var session=Session(em,setup);
            // Deliberately stale record rejects the real shared command API. The
            // living fixture cohort remains observable, so retry cannot disappear.
            AddGroup(em,session,1,SkirmishRoleKind.Rocketeer,0,false);
            for(int i=0;i<20;i++)At(em,session,setup,100+i*4);
            var terminal=Assignment(em,session,1);Assert.Greater(terminal.Retries,setup.EnemyPolicy.MaxIdenticalRejects);
            At(em,session,setup,1000);var later=Assignment(em,session,1);
            Assert.AreEqual(terminal.Retries,later.Retries);Assert.AreEqual(terminal.LastIssuedAt,later.LastIssuedAt);
            using(var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(LocalTransform)))
            using(var actors=q.ToEntityArray(Allocator.Temp))foreach(var actor in actors)em.SetComponentData(actor,LocalTransform.FromPosition(2,0,0));
            At(em,session,setup,1004);Assert.AreEqual(1,Assignment(em,session,1).Retries);
        }
        [Test]
        public void MinimalDefensePreservesOffensiveTargetAndScoutNeedsRealAdditionalGroup()
        {
            var facts=new SkirmishEnemyPerceptionBuilder.Snapshot {Home=float3.zero,CombatSupply=12,LocalThreatSupply=4};
            for(uint id=1;id<=3;id++)facts.Groups.Add(new SkirmishEnemyPerceptionBuilder.Group {Id=id,Role=id==1?SkirmishRoleKind.Rocketeer:SkirmishRoleKind.Rifle,
                Position=new float3(50,0,0),Members=4,Supply=4,Health=400,MaximumHealth=400,Domains=id==1?SkirmishTargetDomain.Structure:SkirmishTargetDomain.Infantry});
            var home=new SkirmishEnemyPerceptionBuilder.Contact {StableId=new FixedString64Bytes("home-threat"),Position=new float3(10,0,0),Category=SkirmishPopulationCategory.Infantry};
            var field=new SkirmishEnemyPerceptionBuilder.Contact {StableId=new FixedString64Bytes("field-target"),Position=new float3(120,0,0),Category=SkirmishPopulationCategory.Infantry};
            facts.Visible.Add(home);facts.Visible.Add(field);var package=SkirmishEnemyGroupAllocation.Allocate(facts,25);
            var defenders=SkirmishEnemyCommanderService.AllocateDefenders(facts,package,true);
            Assert.AreEqual(1,defenders.Count);Assert.IsTrue(defenders.Contains(3));
            Assert.IsTrue(SkirmishEnemyCommanderService.TryVisibleTarget(facts,facts.Groups[1],false,out var chosen,true));
            Assert.AreEqual(field.StableId,chosen.StableId);
            var setup=Setup(87,SkirmishDifficultyId.Veteran);Assert.AreEqual(SkirmishRoleKind.Rifle,SkirmishEnemyCommanderService.WantedRole(facts,package,setup));
            Assert.IsNull(package.Scout);Assert.AreEqual(1,package.Assault.Count);Assert.AreEqual(1,package.Escort.Count);Assert.AreEqual(4,package.ReserveSupply);
            facts.Groups.Add(new SkirmishEnemyPerceptionBuilder.Group {Id=4,Role=SkirmishRoleKind.Rifle,Position=new float3(50,0,0),Members=4,Supply=4,
                Health=400,MaximumHealth=400,Domains=SkirmishTargetDomain.Infantry});facts.CombatSupply=16;
            package=SkirmishEnemyGroupAllocation.Allocate(facts,25);Assert.IsNotNull(package.Scout);
            Assert.AreEqual(SkirmishRoleKind.None,SkirmishEnemyCommanderService.WantedRole(facts,package,setup));
        }
        [Test]
        public void CompletedAuthoredRouteApproachesPublicObjectiveWithoutHiddenTarget()
        {
            var authored=SkirmishExpansionCatalogFactory.CreateInMemory();var layout=authored.DefinitionS005.MapLayout;
            try
            {
                var setup=Setup(101,SkirmishDifficultyId.Commander);
                Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup,layout,out var policy));
                Assert.Greater(policy.Routes.Count,0);var route=policy.Routes[0];var final=route.Waypoints[route.Waypoints.Count-1];
                var facts=new SkirmishEnemyPerceptionBuilder.Snapshot { Home=new float3(route.Waypoints[0].X,0,route.Waypoints[0].Z) };
                var group=new SkirmishEnemyPerceptionBuilder.Group {Id=7,Category=SkirmishPopulationCategory.Infantry,Position=new float3(final.X,0,final.Z)};
                setup.PlayerBaseWorldX=final.X+36;setup.PlayerBaseWorldZ=final.Z;
                var assignment=new SkirmishEnemyGroupAssignmentV1 {RouteId=new FixedString64Bytes(route.RouteId),RouteWaypointIndex=route.Waypoints.Count-1};
                var goal=SkirmishEnemyCommanderService.RouteDestination(policy,facts,group,ref assignment,false,setup);
                Assert.AreEqual(new float3(setup.PlayerBaseWorldX,0,setup.PlayerBaseWorldZ),goal);
                Assert.AreEqual(route.Waypoints.Count,assignment.RouteWaypointIndex);
                group.Position = new float3(final.X+20,0,final.Z);
                Assert.AreEqual(goal,SkirmishEnemyCommanderService.RouteDestination(policy,facts,group,ref assignment,false,setup));
                // Capture/apply the genuine native registered Parts representation,
                // then repeat the downstream hidden-objective route evaluation.
                using var world=new World(nameof(CompletedAuthoredRouteApproachesPublicObjectiveWithoutHiddenTarget));
                var em=world.EntityManager;setup.EnemyPolicy=policy;var session=Session(em,setup);
                em.GetBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord {GroupId=7,FactionId=2});
                assignment.GroupId=7;assignment.ActionKey=1;assignment.Duty=SkirmishEnemyDuty.MainAssault;
                em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session).Add(assignment);
                var parts=SkirmishNativeCheckpointParts.Capture(em,session,e=>e==session?"$session":null);
                var saved=new SkirmishNativeCheckpointSnapshotService.Snapshot {SessionParts=parts,
                    Actors=Array.Empty<SkirmishNativeCheckpointSnapshotService.Actor>(),
                    Banks=Array.Empty<SkirmishNativeCheckpointSnapshotService.Bank>(),
                    MapReferences=Array.Empty<SkirmishNativeCheckpointSnapshotService.MapReference>()};
                Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadBuffer(saved.SessionParts,out SkirmishProductionReservation[] receipts));
                Assert.AreEqual(0,receipts.Length,"No isolated purchase was made");
                Assert.IsTrue(SkirmishEnemyCheckpointPolicy.Validate(saved,setup,false));
                var restored=em.CreateEntity();SkirmishNativeCheckpointParts.Apply(em,restored,parts,id=>id=="$session"?restored:Entity.Null);
                var restoredAssignment=em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(restored)[0];
                Assert.AreEqual(route.Waypoints.Count,restoredAssignment.RouteWaypointIndex);
                Assert.AreEqual(goal,SkirmishEnemyCommanderService.RouteDestination(policy,facts,group,ref restoredAssignment,false,setup));
                var assignmentRows=em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session);
                var invalid=assignment;invalid.RouteWaypointIndex++;assignmentRows[0]=invalid;
                saved.SessionParts=SkirmishNativeCheckpointParts.Capture(em,session,e=>e==session?"$session":null);
                Assert.IsFalse(SkirmishEnemyCheckpointPolicy.Validate(saved,setup,false));
                invalid=assignment;invalid.Duty=SkirmishEnemyDuty.Scout;assignmentRows=em.GetBuffer<SkirmishEnemyGroupAssignmentV1>(session);assignmentRows[0]=invalid;
                saved.SessionParts=SkirmishNativeCheckpointParts.Capture(em,session,e=>e==session?"$session":null);
                Assert.IsFalse(SkirmishEnemyCheckpointPolicy.Validate(saved,setup,false));
                Assert.AreEqual(0,facts.Visible.Count);Assert.AreEqual(0,facts.LastSeen.Count);
            }
            finally {UnityEngine.Object.DestroyImmediate(layout);}
        }
        // Invoke the production executor's exact private boundary without making
        // shipping API public. Native shared group-order acceptance remains real.
        static bool IssueMoveOrStationary(EntityManager em, Entity session, SkirmishEnemyPerceptionBuilder.Group group,
            SkirmishEnemyDuty duty, SkirmishGroupOrderKind order, Entity target, FixedString64Bytes targetId,
            float3 goal, float now, ref SkirmishEnemyPlannerV1Component state,
            ref SkirmishEnemyGroupAssignmentV1 assignment, SkirmishEnemyPolicySnapshot policy)
        {
            var method=typeof(SkirmishEnemyCommanderService).GetMethod("Issue",BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.DeclaredOnly);
            Assert.IsNotNull(method);Assert.AreEqual(12,method.GetParameters().Length);
            object[] args={em,session,group,duty,order,target,targetId,goal,now,state,assignment,policy};
            bool dispatched=(bool)method.Invoke(null,args);
            state=(SkirmishEnemyPlannerV1Component)args[9];assignment=(SkirmishEnemyGroupAssignmentV1)args[10];
            return dispatched;
        }
        static void PositionFixtureCohort(EntityManager em,SkirmishEnemyPerceptionBuilder.Group group,float x,float z=0)
        {
            group.Position=new float3(x,0,z);
            using var query=em.CreateEntityQuery(typeof(SkirmishArmyGroupMembershipComponent),typeof(LocalTransform));
            using var actors=query.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors)if(em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId==group.Id)
                em.SetComponentData(actor,LocalTransform.FromPosition(group.Position));
        }
        [Test]
        public void StableMoveGoalOscillationCannotPostponeBoundedRecovery()
        {
            foreach(var difficulty in new[] {SkirmishDifficultyId.Recruit,SkirmishDifficultyId.Commander})
            foreach(int seed in new[] {17,9123})
            {
                using var world=new World(nameof(StableMoveGoalOscillationCannotPostponeBoundedRecovery));var em=world.EntityManager;
                var setup=Setup(seed,difficulty);var session=Session(em,setup);AddGroup(em,session,1,SkirmishRoleKind.Rifle,140);
                var group=new SkirmishEnemyPerceptionBuilder.Group {Id=1,Position=new float3(140,0,0)};
                var state=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);var assignment=new SkirmishEnemyGroupAssignmentV1();
                var goal=new float3(100,0,0);
                Assert.IsTrue(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,SkirmishGroupOrderKind.Move,
                    Entity.Null,default,goal,0,ref state,ref assignment,setup.EnemyPolicy));Assert.AreEqual(1,assignment.OrderAccepted);
                PositionFixtureCohort(em,group,128);IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,
                    SkirmishGroupOrderKind.Move,Entity.Null,default,goal,4,ref state,ref assignment,setup.EnemyPolicy);
                Assert.AreEqual(4,assignment.LastProgressAt);
                float now=8;
                for(int i=0;i<24&&assignment.Retries<=setup.EnemyPolicy.MaxIdenticalRejects;i++)
                {
                    now=Math.Max(now,assignment.RetryAfter+1);
                    // Like the observed vehicle trace, repeatedly travel >2m but
                    // never improve the best distance to the unchanged Move goal.
                    PositionFixtureCohort(em,group,i%2==0?145:128);
                    IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,SkirmishGroupOrderKind.Move,
                        Entity.Null,default,goal,now,ref state,ref assignment,setup.EnemyPolicy);now+=4;
                }
                Assert.Greater(assignment.Retries,setup.EnemyPolicy.MaxIdenticalRejects);
                Assert.AreEqual(SkirmishEnemyDuty.Recover,assignment.Duty);Assert.AreEqual(4,assignment.LastProgressAt);
                float issued=assignment.LastIssuedAt;
                PositionFixtureCohort(em,group,145);IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Recover,
                    SkirmishGroupOrderKind.Move,Entity.Null,default,new float3(200,0,0),1000,ref state,ref assignment,setup.EnemyPolicy);
                Assert.AreEqual(issued,assignment.LastIssuedAt,"A changed proposed recovery goal cannot erase terminal retries");
                Assert.AreEqual(4,assignment.LastProgressAt);
            }
        }
        [Test]
        public void ForwardMoveProgressAndSemanticGoalReplacementRebaseWithoutDutyReset()
        {
            using var world=new World(nameof(ForwardMoveProgressAndSemanticGoalReplacementRebaseWithoutDutyReset));var em=world.EntityManager;
            var setup=Setup(456,SkirmishDifficultyId.Regular);var session=Session(em,setup);AddGroup(em,session,1,SkirmishRoleKind.Rifle,140);
            var group=new SkirmishEnemyPerceptionBuilder.Group {Id=1,Position=new float3(140,0,0)};
            var state=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);var assignment=new SkirmishEnemyGroupAssignmentV1();
            var goal=new float3(100,0,0);
            IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,SkirmishGroupOrderKind.Move,
                Entity.Null,default,goal,0,ref state,ref assignment,setup.EnemyPolicy);
            assignment.Retries=1;
            for(int step=1;step<=5;step++)
            {
                PositionFixtureCohort(em,group,140-step*4);
                Assert.IsFalse(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,SkirmishGroupOrderKind.Move,
                    Entity.Null,default,goal,step*4,ref state,ref assignment,setup.EnemyPolicy));
                Assert.AreEqual(step*4,assignment.LastProgressAt);Assert.AreEqual(0,assignment.Retries);
            }
            // A brief ordinary detour leaves the old best position intact; real
            // approach before the existing timeout still clears the observation.
            PositionFixtureCohort(em,group,124,2);IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,
                SkirmishGroupOrderKind.Move,Entity.Null,default,goal,24,ref state,ref assignment,setup.EnemyPolicy);
            Assert.AreEqual(20,assignment.LastProgressAt);
            PositionFixtureCohort(em,group,116);IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,
                SkirmishGroupOrderKind.Move,Entity.Null,default,goal,28,ref state,ref assignment,setup.EnemyPolicy);
            Assert.AreEqual(28,assignment.LastProgressAt);
            goal=new float3(200,0,0);
            Assert.IsTrue(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,SkirmishGroupOrderKind.Move,
                Entity.Null,default,goal,40,ref state,ref assignment,setup.EnemyPolicy));
            Assert.AreEqual(40,assignment.LastProgressAt);Assert.AreEqual(116,assignment.LastProgressX);
            PositionFixtureCohort(em,group,120);IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,
                SkirmishGroupOrderKind.Move,Entity.Null,default,goal,44,ref state,ref assignment,setup.EnemyPolicy);
            Assert.AreEqual(44,assignment.LastProgressAt);
            uint action=assignment.ActionKey;
            IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Scout,SkirmishGroupOrderKind.Move,
                Entity.Null,default,goal,56,ref state,ref assignment,setup.EnemyPolicy);
            Assert.AreEqual(44,assignment.LastProgressAt);Assert.AreEqual(1,assignment.Retries);
            Assert.AreEqual(action,assignment.ActionKey,"Duty relabeling cannot postpone an expired unchanged Move goal");
            Assert.IsFalse(SkirmishEnemyDecisionRules.TowardMoveGoalProgressed(120,0,float.NaN,0,200,0,2));
        }
        [Test]
        public void StationaryNativeAcceptedHoldAndVisibleAttackDoNotBecomeMovementFailures()
        {
            using var world=new World(nameof(StationaryNativeAcceptedHoldAndVisibleAttackDoNotBecomeMovementFailures));var em=world.EntityManager;
            var setup=Setup(901,SkirmishDifficultyId.Veteran);var session=Session(em,setup);AddGroup(em,session,1,SkirmishRoleKind.Rocketeer,50);
            var group=new SkirmishEnemyPerceptionBuilder.Group {Id=1,Position=new float3(50,0,0)};
            var state=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);var assignment=new SkirmishEnemyGroupAssignmentV1();
            Assert.IsTrue(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.HomeReserve,SkirmishGroupOrderKind.Hold,
                Entity.Null,default,group.Position,0,ref state,ref assignment,setup.EnemyPolicy));
            Assert.AreEqual(1,assignment.OrderAccepted);
            Assert.IsFalse(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.HomeReserve,SkirmishGroupOrderKind.Hold,
                Entity.Null,default,group.Position,1000,ref state,ref assignment,setup.EnemyPolicy));Assert.AreEqual(0,assignment.Retries);
            var target=em.CreateEntity();var targetId=new FixedString64Bytes("visible.fixture.structure");
            em.AddComponentData(target,new SkirmishAttemptOwnedComponent {SessionId=new FixedString64Bytes("isolated-orders"),
                FactionId=1,IsStructure=1,StableObjectId=targetId});em.AddComponentData(target,new UnitHealth {Current=100,Max=100});
            em.AddComponentData(target,LocalTransform.FromPosition(60,0,0));
            // Shared-fog-disabled isolated order fixture: Project
            // creates real observer knowledge rather than injecting visibility.
            em.AddComponentData(session,new SkirmishFogStateComponent {SharedFog=0});SkirmishFogService.Project(em,session);
            Assert.IsTrue(SkirmishFogService.IsVisibleToFaction(em,target,2));
            Assert.IsTrue(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.MainAssault,SkirmishGroupOrderKind.Attack,
                target,targetId,new float3(60,0,0),1001,ref state,ref assignment,setup.EnemyPolicy));Assert.AreEqual(1,assignment.OrderAccepted);
            uint action=assignment.ActionKey;
            Assert.IsFalse(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.MainAssault,SkirmishGroupOrderKind.Attack,
                target,targetId,new float3(60,0,0),2000,ref state,ref assignment,setup.EnemyPolicy));
            Assert.AreEqual(action,assignment.ActionKey);Assert.AreEqual(0,assignment.Retries);
            Assert.AreEqual(SkirmishGroupOrderKind.Attack,em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
        }
        [Test]
        public void AcceptedRecoveryRetainsRetryBoundUntilActualGoalProgress()
        {
            using var world=new World(nameof(AcceptedRecoveryRetainsRetryBoundUntilActualGoalProgress));var em=world.EntityManager;
            var setup=Setup(634,SkirmishDifficultyId.Regular);var session=Session(em,setup);AddGroup(em,session,1,SkirmishRoleKind.Rifle,140);
            var group=new SkirmishEnemyPerceptionBuilder.Group {Id=1,Position=new float3(140,0,0)};
            var state=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);var assignment=new SkirmishEnemyGroupAssignmentV1();
            var oldGoal=new float3(100,0,0);var recoveryGoal=new float3(200,0,0);
            IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Escort,SkirmishGroupOrderKind.Move,
                Entity.Null,default,oldGoal,0,ref state,ref assignment,setup.EnemyPolicy);
            // Outer Evaluate selects Recover when the existing rejection bound is
            // reached, then dispatches only after RetryAfter. Acceptance by itself
            // must not erase the bound: a blocked path can be accepted normally.
            assignment.Retries=(byte)setup.EnemyPolicy.MaxIdenticalRejects;
            assignment.Duty=SkirmishEnemyDuty.Recover;assignment.OrderAccepted=0;assignment.RetryAfter=12;
            Assert.IsTrue(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Recover,SkirmishGroupOrderKind.Move,
                Entity.Null,default,recoveryGoal,16,ref state,ref assignment,setup.EnemyPolicy));
            Assert.AreEqual(1,assignment.OrderAccepted);Assert.AreEqual(setup.EnemyPolicy.MaxIdenticalRejects,assignment.Retries);
            Assert.AreEqual(16,assignment.LastProgressAt);Assert.AreEqual(12,assignment.RetryAfter);
            PositionFixtureCohort(em,group,145);
            Assert.IsFalse(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Recover,SkirmishGroupOrderKind.Move,
                Entity.Null,default,recoveryGoal,20,ref state,ref assignment,setup.EnemyPolicy));
            Assert.AreEqual(0,assignment.Retries);Assert.AreEqual(20,assignment.LastProgressAt);
            // Contrast the same actually accepted recovery without forward
            // progress: one expired window reaches the persisted terminal bound.
            assignment.Retries=(byte)setup.EnemyPolicy.MaxIdenticalRejects;
            IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Recover,SkirmishGroupOrderKind.Move,
                Entity.Null,default,recoveryGoal,32,ref state,ref assignment,setup.EnemyPolicy);
            Assert.AreEqual(setup.EnemyPolicy.MaxIdenticalRejects+1,assignment.Retries);Assert.AreEqual(0,assignment.OrderAccepted);
            float blockedUntil=assignment.RetryAfter;float issued=assignment.LastIssuedAt;
            IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Recover,SkirmishGroupOrderKind.Move,
                Entity.Null,default,new float3(300,0,0),blockedUntil+1,ref state,ref assignment,setup.EnemyPolicy);
            Assert.AreEqual(issued,assignment.LastIssuedAt);Assert.AreEqual(20,assignment.LastProgressAt);
            PositionFixtureCohort(em,group,149);
            Assert.IsTrue(IssueMoveOrStationary(em,session,group,SkirmishEnemyDuty.Recover,SkirmishGroupOrderKind.Move,
                Entity.Null,default,new float3(300,0,0),blockedUntil+5,ref state,ref assignment,setup.EnemyPolicy));
            Assert.AreEqual(0,assignment.Retries);Assert.AreEqual(1,assignment.OrderAccepted);
            Assert.AreEqual(149,assignment.LastProgressX);Assert.AreEqual(blockedUntil+5,assignment.LastProgressAt);
            Assert.LessOrEqual(assignment.RetryAfter,blockedUntil+5,"Retained backoff is already elapsed at legitimate redispatch");
        }
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishEnemyCommanderTests();
            tests.WholeGroupsDispatchBoundedThenAcceptedHoldIsDeduplicatedAcrossConfigsAndSeeds();
            tests.FailedNativeOrdersHaveBoundedRecoveryAndTimeCannotResetThem();
            tests.MinimalDefensePreservesOffensiveTargetAndScoutNeedsRealAdditionalGroup();
            tests.CompletedAuthoredRouteApproachesPublicObjectiveWithoutHiddenTarget();
            tests.StableMoveGoalOscillationCannotPostponeBoundedRecovery();
            tests.ForwardMoveProgressAndSemanticGoalReplacementRebaseWithoutDutyReset();
            tests.StationaryNativeAcceptedHoldAndVisibleAttackDoNotBecomeMovementFailures();
            tests.AcceptedRecoveryRetainsRetryBoundUntilActualGoalProgress();
            Debug.Log("[SkirmishEnemyCommanderTests] result=Passed cases=8 configSeedPairs=4 scope=isolated-shared-orders");
        }
    }
}
