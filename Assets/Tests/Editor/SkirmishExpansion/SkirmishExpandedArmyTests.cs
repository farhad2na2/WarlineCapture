using System;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
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
                em, session, 1, 1, 2, out SkirmishArmyGroupRecord tankSlot));
            Assert.AreEqual(SkirmishRoleKind.Tank, tankSlot.Role);
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectPageSlot(
                em, session, 1, 2, false, out SkirmishCommandDecision pageTank));
            Assert.AreEqual(tankSlot.GroupId, pageTank.GroupId);
            Assert.AreEqual(1, SkirmishArmySelectionService.SelectedLivingCount(em, session));

            uint enemyRifle = FirstFactionGroup(em, session, 2, SkirmishRoleKind.Rifle).GroupId;
            Assert.IsFalse(SkirmishArmySelectionService.TrySelectGroup(
                em, session, enemyRifle, false, out SkirmishCommandDecision enemy));
            Assert.AreEqual(SkirmishReasonCode.InvalidSelection, enemy.Reason);
        }

        private sealed class PresentedTray : IMatchHudSquadTrayView
        {
            public int Rejections;
            public void Bind(Action<MatchHudSquadTraySlot> action) { }
            public void ClearActiveSlot() { }
            public bool ContainsScreenPoint(Vector2 point) => false;
            public void FlashDisabled(MatchHudSquadTraySlot slot) => Rejections++;
            public void SetSelectedSlot(MatchHudSquadTraySlot slot) { }
            public bool TryGetPortraitSprite(MatchHudSquadTraySlot slot, out Sprite sprite) { sprite = null; return false; }
            public void Unbind() { }
        }

        [Test]
        public void PresentedTrayClickUsesPageGroupWithoutLegacyReselection()
        {
            using var world = new World(nameof(PresentedTrayClickUsesPageGroupWithoutLegacyReselection));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            bool GetManager(out EntityManager manager) { manager = em; return true; }
            var context = new MatchHudSquadTraySelectionUiSystemHelper.Context(null, GetManager,
                null, null, null, null, null, null, null, null);
            var tray = new PresentedTray();
            var selection = new MatchHudSquadTraySelectionUiSystemHelper();
            selection.SelectSlot(context, tray, MatchHudSquadTraySlot.Soldiers);
            Assert.AreEqual(4, SkirmishArmySelectionService.SelectedLivingCount(em, session));
            selection.SelectSlot(context, tray, MatchHudSquadTraySlot.Transport); // Fifth real group
            Assert.AreEqual(0, em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex);
            Assert.AreEqual(8, SkirmishArmySelectionService.SelectedLivingCount(em, session));
            int selectedBeforePaging = SkirmishArmySelectionService.SelectedLivingCount(em, session);
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryChangePage(em, session, 1));
            Assert.AreEqual(selectedBeforePaging, SkirmishArmySelectionService.SelectedLivingCount(em, session));
            Assert.IsFalse(SkirmishExpandedPresentedOrders.TryChangePage(em, session, 1));
            selection.SelectSlot(context, tray, MatchHudSquadTraySlot.AttackHelicopter); // Tank on page two
            Assert.Greater(SkirmishArmySelectionService.SelectedLivingCount(em, session), selectedBeforePaging);
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryGetPresentedMember(em, session, 2, out var representative));
            Assert.AreEqual(SkirmishRoleKind.Tank, em.GetComponentData<SkirmishUnitRoleComponent>(representative).Role);
            Assert.IsTrue(em.HasComponent<SelectedUnitTag>(representative));
            Assert.AreEqual(0, tray.Rejections);
        }

        [Test]
        public void AcceptedArmyOrdersRequireInputEvidence()
        {
            using var world = new World(nameof(AcceptedArmyOrdersRequireInputEvidence));
            var em = world.EntityManager;
            CompileAndSpawn(em, out var session, out _);
            Assert.IsTrue(SkirmishArmySelectionService.TrySelectGroup(em, session,
                FirstPlayerGroup(em, session, SkirmishRoleKind.Tank).GroupId, false, out _));
            Entity target = FirstFactionUnit(em, 2, SkirmishRoleKind.Tank);
            try
            {
                AriaCommandEvidence.Begin();
                Assert.IsTrue(SkirmishArmyCommandService.TryAttack(em, session, target, out _));
                Assert.Greater(AriaCommandEvidence.AcceptedCommands, 0);
                Assert.AreEqual(AriaCommandEvidence.AcceptedCommands, AriaCommandEvidence.Violations);
                int unproven = AriaCommandEvidence.Violations;
                AriaCommandEvidence.ObserveRelease(10, Vector2.zero);
                using (AriaCommandEvidence.Enter(AriaCommandEvidence.ClaimRelease(10)))
                    Assert.IsTrue(SkirmishArmyCommandService.TryAttack(em, session, target, out _));
                Assert.AreEqual(unproven, AriaCommandEvidence.Violations);
                Assert.Greater(AriaCommandEvidence.AcceptedCommands, unproven);
            }
            finally { AriaCommandEvidence.End(); }
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
            Assert.AreEqual(5, SkirmishArmyDrawerProjection.Project(em, session));
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

        [Test]
        public void PresentedHealthTracksDamageAndExcludesOtherAttempts()
        {
            using var world = new World(nameof(PresentedHealthTracksDamageAndExcludesOtherAttempts));
            var em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryGetPresentedMember(em, session, 0, out var member));
            var health = em.GetComponentData<UnitHealth>(member);
            health.Current = health.Max / 2;
            em.SetComponentData(member, health);
            var foreign = em.Instantiate(member);
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(foreign);
            owned.SessionId = new FixedString64Bytes("different-attempt");
            em.SetComponentData(foreign, owned);
            health.Current = 1;
            em.SetComponentData(foreign, health);
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryReadPage(em, session,
                out _, out _, out _, out _, out _, out _, out _, out _, out var first, out _, out _, out _, out _));
            Assert.AreEqual(4, first.Alive);
            Assert.That(first.Health01, Is.EqualTo(0.875f).Within(0.001f));
        }

        [Test]
        public void FiveSlotPageSelectsFifthGroupAndPagingPreservesSelectionAndOrders()
        {
            using var world = new World(nameof(FiveSlotPageSelectsFifthGroupAndPagingPreservesSelectionAndOrders));
            var em = world.EntityManager;
            CompileAndSpawn(em, out Entity session, out _);
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryReadPage(em, session,
                out int page, out bool previous, out bool next, out int pageCount,
                out int total, out int selected, out int offPage, out uint revision,
                out var a, out var b, out var c, out var d, out var fifth));
            Assert.AreEqual(0, page); Assert.IsFalse(previous); Assert.IsTrue(next);
            Assert.AreEqual(2, pageCount); Assert.AreEqual(8, total); Assert.AreEqual(0, selected);
            var ids = new[] { a.GroupId, b.GroupId, c.GroupId, d.GroupId, fifth.GroupId };
            Assert.AreEqual(5, ids.Length);
            for (int i = 0; i < ids.Length; i++) Assert.AreNotEqual(0u, ids[i]);
            Assert.AreEqual(5, System.Linq.Enumerable.Distinct(ids).Count());
            uint fifthId = fifth.GroupId;
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryPresentedSlot(em, session, 4));
            Assert.AreEqual(0, em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex);
            var records = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            bool foundSelected = false;
            for (int i = 0; i < records.Length; i++) if (records[i].GroupId == fifthId) foundSelected = records[i].Selected != 0;
            Assert.IsTrue(foundSelected, "slot 4 selects its stable GroupId");
            Assert.IsTrue(SkirmishArmyGroupSystem.TryGet(em, session, fifthId, out var fifthBefore));
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryChangePage(em, session, 1));
            Assert.AreEqual(1, em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex);
            Assert.AreEqual(1, em.GetComponentData<SkirmishArmySelectionComponent>(session).SelectedGroupCount);
            Assert.IsTrue(SkirmishArmyGroupSystem.TryGet(em, session, fifthId, out var fifthAfterPaging));
            Assert.AreEqual(fifthBefore.LastOrder, fifthAfterPaging.LastOrder);
            Assert.IsFalse(SkirmishExpandedPresentedOrders.TryChangePage(em, session, 1), "last page does not wrap");
            Assert.IsFalse(SkirmishExpandedPresentedOrders.TryChangePage(em, session, 0));
            Assert.IsFalse(SkirmishExpandedPresentedOrders.TryChangePage(em, session, 2));
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryChangePage(em, session, -1));
            Assert.IsTrue(SkirmishExpandedPresentedOrders.TryClearSelection(em, session));
            Assert.AreEqual(0, em.GetComponentData<SkirmishArmySelectionComponent>(session).SelectedGroupCount);
            Assert.AreEqual(0, em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex);
            Assert.IsTrue(SkirmishArmyGroupSystem.TryGet(em, session, fifthId, out var fifthAfterClear));
            Assert.AreEqual(fifthBefore.LastOrder, fifthAfterClear.LastOrder);
        }

        [Test]
        public void DeadGroupsDoNotHideSurvivorsOrLeaveAnInvalidPage()
        {
            using var world = new World(nameof(DeadGroupsDoNotHideSurvivorsOrLeaveAnInvalidPage));
            var em = world.EntityManager;
            var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishArmySelectionComponent));
            em.SetComponentData(session, new SkirmishArmySelectionComponent { PageSize = 4, PageIndex = 1 });
            var groups = em.AddBuffer<SkirmishArmyGroupRecord>(session);
            for (uint i = 1; i <= 5; i++)
                groups.Add(new SkirmishArmyGroupRecord { GroupId = i, FactionId = 1,
                    Role = SkirmishRoleKind.Rifle, AliveCount = i == 1 ? 0 : 1 });
            Assert.AreEqual(4, SkirmishArmyDrawerProjection.Project(em, session));
            Assert.AreEqual(0, em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex);
            var slots = em.GetBuffer<SkirmishArmyDrawerSlot>(session);
            for (int slot = 0; slot < 5; slot++)
            {
                if (slot < 4)
                {
                    Assert.IsTrue(SkirmishArmyGroupSystem.TryGetPagedGroup(em, session, 1, 0, slot, out var group));
                    Assert.AreEqual((uint)(slot + 2), group.GroupId);
                    Assert.AreEqual(group.GroupId, slots[slot].GroupId);
                }
                else Assert.IsFalse(SkirmishArmyGroupSystem.TryGetPagedGroup(em, session, 1, 0, slot, out _));
            }
        }

        public static void RunFocusedValidation()
        {
            try
            {
                var suite = new SkirmishExpandedArmyTests();
                suite.S002StartingForcesFormStablePlayerGroups();
                suite.LegalSelectionAndPagingUseSharedSelectionTags();
                suite.PresentedTrayClickUsesPageGroupWithoutLegacyReselection();
                suite.SharedFogGatesAttackEligibility();
                suite.GroundMoveExcludesAirAndHoldStampsGroup();
                suite.ProducedTankJoinsNewGroupAndDeathKeepsIdentity();
                suite.StandardGroundUnitsAdvanceWorldTransformOnMove();
                suite.ArmyDrawerProjectsCurrentPlayerPage();
                suite.PresentedHealthTracksDamageAndExcludesOtherAttempts();
                suite.DeadGroupsDoNotHideSurvivorsOrLeaveAnInvalidPage();
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
