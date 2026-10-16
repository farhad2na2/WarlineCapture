using System;
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
    public sealed class SkirmishEnemyPerceptionTests
    {
        [Test]
        public void HiddenHostileMutationsCannotChangeEnemyDecisions()
        {
            using var world = new World(nameof(HiddenHostileMutationsCannotChangeEnemyDecisions));
            var em = world.EntityManager;
            var session = em.CreateEntity();
            em.AddComponentData(session,new SkirmishExpandedSessionComponent { SessionId = new FixedString64Bytes("perception-test") });
            var setup = new SkirmishResolvedSetup { EnemyBaseWorldX = 50, EnemyBaseWorldZ = 50 };
            var hidden = em.CreateEntity();
            em.AddComponentData(hidden,new SkirmishAttemptOwnedComponent {
                SessionId = new FixedString64Bytes("perception-test"), FactionId = 1, StableObjectId = new FixedString64Bytes("hostile") });
            em.AddComponentData(hidden,LocalTransform.FromPosition(60,0,60));
            em.AddComponentData(hidden,new UnitHealth { Current = 100, Max = 100 });
            em.AddComponentData(hidden,new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Rifle, Category = SkirmishPopulationCategory.Infantry });
            var knowledge = em.AddBuffer<SkirmishContactKnowledgeElement>(hidden);
            knowledge.Add(new SkirmishContactKnowledgeElement { ObserverFactionId = 2, Sight = SkirmishContactSight.Unknown });
            var before = SkirmishEnemyPerceptionBuilder.Build(em,session,setup);
            em.SetComponentData(hidden,LocalTransform.FromPosition(500,0,900));
            em.SetComponentData(hidden,new UnitHealth { Current = 1, Max = 999 });
            em.SetComponentData(hidden,new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Tank, Category = SkirmishPopulationCategory.Ground });
            var after = SkirmishEnemyPerceptionBuilder.Build(em,session,setup);
            Assert.AreEqual(before.EventHash,after.EventHash);
            Assert.AreEqual(0,after.Visible.Count);
            Assert.AreEqual(0,after.LocalThreatSupply);
            var observed = em.GetBuffer<SkirmishContactKnowledgeElement>(hidden);
            observed[0] = new SkirmishContactKnowledgeElement { ObserverFactionId = 2, Sight = SkirmishContactSight.Visible };
            var visible = SkirmishEnemyPerceptionBuilder.Build(em,session,setup);
            Assert.AreEqual(1,visible.Visible.Count);
            Assert.AreEqual(SkirmishRoleKind.Tank,visible.Visible[0].Role);
            Assert.AreEqual(new float3(500,0,900),visible.Visible[0].Position);
            Assert.AreNotEqual(before.EventHash,visible.EventHash);
        }
        [Test]
        public void LastSeenRemainsFrozenAfterHiddenActorMovesOrDies()
        {
            using var world = new World(nameof(LastSeenRemainsFrozenAfterHiddenActorMovesOrDies));
            var em = world.EntityManager;
            var session = em.CreateEntity();
            em.AddComponentData(session,new SkirmishExpandedSessionComponent { SessionId = new FixedString64Bytes("frozen-test") });
            em.AddComponentData(session,new SkirmishContactLedgerStateComponent { SessionId = new FixedString64Bytes("frozen-test") });
            var ledger = em.AddBuffer<SkirmishContactLedgerElement>(session);
            ledger.Add(new SkirmishContactLedgerElement {
                ContactStableObjectId = new FixedString64Bytes("gone"), ObserverFactionId = 2,
                Sight = SkirmishContactSight.LastSeen, LastSeenPosition = new float3(4,0,9) });
            var facts = SkirmishEnemyPerceptionBuilder.Build(em,session,new SkirmishResolvedSetup());
            Assert.AreEqual(0,facts.Visible.Count);
            Assert.AreEqual(1,facts.LastSeen.Count);
            Assert.AreEqual(new float3(4,0,9),facts.LastSeen[0]);
            em.SetComponentData(session,new SkirmishContactLedgerStateComponent { SessionId = new FixedString64Bytes("old-attempt") });
            Assert.AreEqual(0,SkirmishEnemyPerceptionBuilder.Build(em,session,new SkirmishResolvedSetup()).LastSeen.Count);
        }
        [Test]
        public void ReserveAndAssaultDoNotSplitOrDuplicateGroups()
        {
            var facts = new SkirmishEnemyPerceptionBuilder.Snapshot();
            for(uint id=1;id<=6;id++) facts.Groups.Add(new SkirmishEnemyPerceptionBuilder.Group {
                Id=id, Members=4, Supply=4, Health=400, MaximumHealth=400,
                Role=id==1?SkirmishRoleKind.Rocketeer:SkirmishRoleKind.Rifle,
                Domains=id==1?SkirmishTargetDomain.Structure:SkirmishTargetDomain.Infantry });
            facts.CombatSupply=24;
            var package=SkirmishEnemyGroupAllocation.Allocate(facts,25);
            Assert.IsTrue(package.Viable);
            Assert.AreEqual(8,package.ReserveSupply);
            var seen=new System.Collections.Generic.HashSet<uint>();
            foreach(var group in package.Reserve) Assert.IsTrue(seen.Add(group.Id));
            foreach(var group in package.Assault) Assert.IsTrue(seen.Add(group.Id));
            foreach(var group in package.Escort) Assert.IsTrue(seen.Add(group.Id));
            if(package.Scout!=null) Assert.IsTrue(seen.Add(package.Scout.Id));
            Assert.AreEqual(6,seen.Count);
        }
        [Test]
        public void LoneStructureSquadRegroupsInsteadOfUnsupportedAssault()
        {
            var facts = new SkirmishEnemyPerceptionBuilder.Snapshot { CombatSupply=4 };
            facts.Groups.Add(new SkirmishEnemyPerceptionBuilder.Group {
                Id=1,Members=4,Supply=4,Health=400,MaximumHealth=400,Domains=SkirmishTargetDomain.Structure });
            var package=SkirmishEnemyGroupAllocation.Allocate(facts,25);
            Assert.IsFalse(package.Viable); Assert.AreEqual(1,package.Reserve.Count);
            Assert.AreEqual(0,package.Assault.Count);
        }
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishEnemyPerceptionTests();
            tests.HiddenHostileMutationsCannotChangeEnemyDecisions();
            tests.LastSeenRemainsFrozenAfterHiddenActorMovesOrDies();
            tests.ReserveAndAssaultDoNotSplitOrDuplicateGroups();
            tests.LoneStructureSquadRegroupsInsteadOfUnsupportedAssault();
            Debug.Log("[SkirmishEnemyPerceptionTests] result=Passed cases=4");
        }
    }
}
