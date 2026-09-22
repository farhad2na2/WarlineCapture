using System;
using Game.Components;
using Game.Composition;
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
    public sealed class SkirmishExpandedArmyTests
    {
        [Test]
        public void S002StartingForcesFormStablePlayerGroups()
        {
            using var world = new World(nameof(S002StartingForcesFormStablePlayerGroups));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out SkirmishResolvedSetup setup);
            Assert.IsTrue(setup.SharedFog);
            Assert.IsTrue(setup.DevelopmentFullVision);
            Assert.AreEqual(8, SkirmishArmyGroupSystem.CountFactionGroups(em, session, 1));
            Assert.AreEqual(3, CountPlayerGroups(em, session, SkirmishRoleKind.Rifle));
            Assert.AreEqual(1, CountPlayerGroups(em, session, SkirmishRoleKind.Gunner));
            Assert.AreEqual(1, CountPlayerGroups(em, session, SkirmishRoleKind.Rocketeer));
            Assert.AreEqual(1, CountPlayerGroups(em, session, SkirmishRoleKind.Car));
            Assert.AreEqual(1, CountPlayerGroups(em, session, SkirmishRoleKind.ApcArmored));
            Assert.AreEqual(1, CountPlayerGroups(em, session, SkirmishRoleKind.Tank));
            Assert.AreEqual(4, FirstPlayerGroup(em, session, SkirmishRoleKind.Rifle).AliveCount);
            Assert.AreEqual(1, FirstPlayerGroup(em, session, SkirmishRoleKind.Tank).AliveCount);
        }

        [Test]
        public void LegalSelectionAndPagingUseSharedSelectionTags()
        {
            using var world = new World(nameof(LegalSelectionAndPagingUseSharedSelectionTags));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            uint rifle = FirstPlayerGroup(em, session, SkirmishRoleKind.Rifle).GroupId;
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(
                em, session, rifle, false, out SkirmishCommandDecision first));
            Assert.AreEqual(4, first.IssuedCount);
            Assert.AreEqual(4, SkirmishArmySelectionService.SelectedLivingCount(em, session));
            Assert.AreEqual(0, CountSelectedFaction(em, 2));

            uint gunner = FirstPlayerGroup(em, session, SkirmishRoleKind.Gunner).GroupId;
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(
                em, session, gunner, true, out SkirmishCommandDecision added));
            Assert.AreEqual(4, added.IssuedCount);
            Assert.AreEqual(8, SkirmishArmySelectionService.SelectedLivingCount(em, session));
            Assert.AreEqual(2, em.GetComponentData<SkirmishArmySelectionComponent>(session).SelectedGroupCount);

            Assert.IsTrue(SkirmishArmySelectionService.TrySelectPageSlot(
                em, session, 0, 0, false, out SkirmishCommandDecision pageZero));
            Assert.AreEqual(rifle, pageZero.GroupId);
            Assert.IsTrue(SkirmishArmyGroupSystem.TryGetPagedGroup(
                em, session, 1, 1, 3, out SkirmishArmyGroupRecord tankSlot));
            Assert.AreEqual(SkirmishRoleKind.Tank, tankSlot.Role);
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectPageSlot(
                em, session, 1, 3, false, out SkirmishCommandDecision pageTank));
            Assert.AreEqual(tankSlot.GroupId, pageTank.GroupId);
            Assert.AreEqual(1, SkirmishArmySelectionService.SelectedLivingCount(em, session));

            uint enemyRifle = FirstFactionGroup(em, session, 2, SkirmishRoleKind.Rifle).GroupId;
            Assert.IsFalse(SkirmishArmySelectionService.TrySelectGroup(
                em, session, enemyRifle, false, out SkirmishCommandDecision enemy));
            Assert.AreEqual(SkirmishReasonCode.InvalidSelection, enemy.Reason);
        }

        [Test]
        public void SharedFogGatesAttackEligibility()
        {
            using var world = new World(nameof(SharedFogGatesAttackEligibility));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(
                em, session, FirstPlayerGroup(em, session, SkirmishRoleKind.Tank).GroupId, false, out _));
            Entity enemyTank = FirstFactionUnit(em, 2, SkirmishRoleKind.Tank);
            Assert.AreNotEqual(Entity.Null, enemyTank);
            Assert.AreEqual(SkirmishContactSight.Visible, em.GetComponentData<SkirmishContactSightComponent>(enemyTank).Sight);
            Assert.IsTrue(SkirmishArmyCommandService.TryAttack(
                em, session, enemyTank, out SkirmishCommandDecision seen));
            Assert.AreEqual(SkirmishGroupOrderKind.Attack, seen.Order);

            var fog = em.GetComponentData<SkirmishFogStateComponent>(session);
            fog.DevelopmentFullVision = 0;
            em.SetComponentData(session, fog);
            SkirmishFogService.Project(em, session);
            Assert.AreEqual(SkirmishContactSight.LastSeen, em.GetComponentData<SkirmishContactSightComponent>(enemyTank).Sight);
            Assert.IsFalse(SkirmishArmyCommandService.TryAttack(
                em, session, enemyTank, out SkirmishCommandDecision hidden));
            Assert.AreEqual(SkirmishReasonCode.HiddenContact, hidden.Reason);

            SkirmishFogService.Reveal(em, enemyTank);
            Assert.IsTrue(SkirmishArmyCommandService.TryAttack(em, session, enemyTank, out _));
        }

        [Test]
        public void GroundMoveExcludesAirAndHoldStampsGroup()
        {
            using var world = new World(nameof(GroundMoveExcludesAirAndHoldStampsGroup));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            uint rifle = FirstPlayerGroup(em, session, SkirmishRoleKind.Rifle).GroupId;
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, rifle, false, out _));
            Entity first = FirstSelected(em);
            var membership = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(first);
            membership.Domain = SkirmishPopulationCategory.Air;
            em.SetComponentData(first, membership);
            Assert.IsTrue(SkirmishArmyCommandService.TryMove(
                em, session, out SkirmishCommandDecision move));
            Assert.AreEqual(3, move.IssuedCount);
            Assert.AreEqual(1, move.ExcludedCount);
            Assert.IsTrue(SkirmishArmyCommandService.TryHold(em, session, out SkirmishCommandDecision hold));
            Assert.AreEqual(SkirmishGroupOrderKind.Hold, hold.Order);
            Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(FirstSelected(em)));
            Assert.AreEqual(SkirmishGroupOrderKind.Hold, FirstPlayerGroup(em, session, SkirmishRoleKind.Rifle).LastOrder);
        }

        [Test]
        public void ProducedTankJoinsNewGroupAndDeathKeepsIdentity()
        {
            using var world = new World(nameof(ProducedTankJoinsNewGroupAndDeathKeepsIdentity));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            int before = SkirmishArmyGroupSystem.CountFactionGroups(em, session, 1);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            Assert.IsTrue(SkirmishProductionService.TryProduce(
                em, session, SkirmishRoleIds.Tank, 1, authored.ArmyGround, out SkirmishProductionDecision tank));
            Assert.AreEqual(before + 1, SkirmishArmyGroupSystem.CountFactionGroups(em, session, 1));
            uint producedGroup = 0;
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId == 1 && buffer[i].Role == SkirmishRoleKind.Tank)
                    producedGroup = buffer[i].GroupId;
            }

            Assert.AreNotEqual(0u, producedGroup);
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, producedGroup, false, out _));

            uint startingRifle = FirstPlayerGroup(em, session, SkirmishRoleKind.Rifle).GroupId;
            Entity member = FirstGroupMember(em, startingRifle);
            var health = em.GetComponentData<UnitHealth>(member);
            health.Current = 0;
            em.SetComponentData(member, health);
            Assert.IsTrue(SkirmishProductionService.TryReleaseDeath(em, session, member));
            Assert.IsTrue(SkirmishArmyGroupSystem.TryGet(em, session, startingRifle, out SkirmishArmyGroupRecord wounded));
            Assert.AreEqual(4, wounded.MemberCount);
            Assert.AreEqual(3, wounded.AliveCount);
            Assert.AreEqual(startingRifle, wounded.GroupId);
        }

        [Test]
        public void StandardGroundUnitsAdvanceWorldTransformOnMove()
        {
            using var world = new World(nameof(StandardGroundUnitsAdvanceWorldTransformOnMove));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            uint tankGroup = FirstPlayerGroup(em, session, SkirmishRoleKind.Tank).GroupId;
            uint apcGroup = FirstPlayerGroup(em, session, SkirmishRoleKind.ApcArmored).GroupId;
            uint rifleGroup = FirstPlayerGroup(em, session, SkirmishRoleKind.Rifle).GroupId;
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, tankGroup, false, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, apcGroup, true, out _));
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, rifleGroup, true, out _));

            Entity tank = FirstGroupMember(em, tankGroup);
            Entity apc = FirstGroupMember(em, apcGroup);
            Entity rifle = FirstGroupMember(em, rifleGroup);
            Place(em, tank, new float3(0f, 0f, 0f));
            Place(em, apc, new float3(0f, 0f, 2f));
            Place(em, rifle, new float3(0f, 0f, 4f));
            var destination = new float3(20f, 0f, 0f);
            Assert.IsTrue(SkirmishArmyCommandService.TryMove(em, session, destination, out SkirmishCommandDecision move));
            Assert.GreaterOrEqual(move.IssuedCount, 3);
            Assert.AreEqual(1, em.GetComponentData<SkirmishMoveIntentComponent>(tank).Active);

            Assert.Greater(SkirmishWorldMovementService.Step(em, session, 1f, false), 0);
            float tankX = em.GetComponentData<LocalTransform>(tank).Position.x;
            float apcX = em.GetComponentData<LocalTransform>(apc).Position.x;
            float rifleX = em.GetComponentData<LocalTransform>(rifle).Position.x;
            Assert.Greater(tankX, 0f);
            Assert.Greater(apcX, 0f);
            Assert.Greater(rifleX, 0f);
            Assert.Less(tankX, destination.x);
            Assert.Greater(tankX, rifleX);

            float pausedX = tankX;
            Assert.AreEqual(0, SkirmishWorldMovementService.Step(em, session, 1f, true));
            Assert.AreEqual(pausedX, em.GetComponentData<LocalTransform>(tank).Position.x);

            Assert.IsTrue(SkirmishArmyCommandService.TryHold(em, session, out _));
            Assert.AreEqual(0, em.GetComponentData<SkirmishMoveIntentComponent>(tank).Active);
            Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(tank));
        }

        [Test]
        public void ArmyDrawerProjectsCurrentPlayerPage()
        {
            using var world = new World(nameof(ArmyDrawerProjectsCurrentPlayerPage));
            EntityManager em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            uint rifle = FirstPlayerGroup(em, session, SkirmishRoleKind.Rifle).GroupId;
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session, rifle, false, out _));
            Assert.AreEqual(4, SkirmishArmyDrawerProjection.Project(em, session));
            DynamicBuffer<SkirmishArmyDrawerSlot> slots = em.GetBuffer<SkirmishArmyDrawerSlot>(session);
            bool found = false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].GroupId != rifle)
                    continue;
                Assert.AreEqual(1, slots[i].Selected);
                Assert.AreEqual(4, slots[i].AliveCount);
                found = true;
            }

            Assert.IsTrue(found);
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedArmyTests();
                suite.S002StartingForcesFormStablePlayerGroups();
                suite.LegalSelectionAndPagingUseSharedSelectionTags();
                suite.SharedFogGatesAttackEligibility();
                suite.GroundMoveExcludesAirAndHoldStampsGroup();
                suite.ProducedTankJoinsNewGroupAndDeathKeepsIdentity();
                suite.StandardGroundUnitsAdvanceWorldTransformOnMove();
                suite.ArmyDrawerProjectsCurrentPlayerPage();
                Debug.Log("[SkirmishExpandedArmyTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishExpandedArmyTests] result=Failed\n" + exception);
                throw;
            }
        }

        private static void CompileAndSpawn(
            EntityManager em,
            out Entity session,
            out SkirmishResolvedSetup setup)
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = authored.DefinitionS002.RequiredFeatureIds };
            Assert.IsTrue(SkirmishExpandedLaunchResolver.TryCompileAndQueue(
                em,
                "S002",
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                104731,
                authored,
                matrix,
                manifest,
                out setup,
                out _,
                out var reasons),
                reasons.Count == 0 ? "compile failed" : reasons[0].ToString());
            session = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)).GetSingletonEntity();
            Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(
                em, session, setup, out SkirmishReasonCode reason, out byte visualPending), reason.ToString());
            Assert.AreEqual(0, visualPending);
            using var owned = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(
                em,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                setup);
            SkirmishArmyGroupSystem.RefreshAlive(
                em,
                session,
                owned,
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId);
        }

        private static int CountPlayerGroups(EntityManager em, Entity session, SkirmishRoleKind role)
        {
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            int total = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId == 1 && buffer[i].Role == role)
                    total++;
            }

            return total;
        }

        private static SkirmishArmyGroupRecord FirstPlayerGroup(
            EntityManager em,
            Entity session,
            SkirmishRoleKind role) =>
            FirstFactionGroup(em, session, 1, role);

        private static SkirmishArmyGroupRecord FirstFactionGroup(
            EntityManager em,
            Entity session,
            byte faction,
            SkirmishRoleKind role)
        {
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId == faction && buffer[i].Role == role)
                    return buffer[i];
            }

            return default;
        }

        private static Entity FirstFactionUnit(EntityManager em, byte faction, SkirmishRoleKind role)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId == faction &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(entities[i]).Role == role)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static Entity FirstGroupMember(EntityManager em, uint groupId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishArmyGroupMembershipComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishArmyGroupMembershipComponent>(entities[i]).GroupId == groupId)
                    return entities[i];
            }

            return Entity.Null;
        }

        private static void Place(EntityManager em, Entity unit, float3 position)
        {
            var transform = LocalTransform.FromPosition(position);
            if (em.HasComponent<LocalTransform>(unit))
                em.SetComponentData(unit, transform);
            else
                em.AddComponentData(unit, transform);
        }

        private static Entity FirstSelected(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(SelectedUnitTag));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            return entities.Length == 0 ? Entity.Null : entities[0];
        }

        private static int CountSelectedFaction(EntityManager em, byte faction)
        {
            using var query = em.CreateEntityQuery(typeof(SelectedUnitTag), typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int total = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId == faction)
                    total++;
            }

            return total;
        }
    }
}
