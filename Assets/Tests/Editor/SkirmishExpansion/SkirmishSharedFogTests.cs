using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishSharedFogTests
    {
        public static void RunFocusedValidation()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Fog focused suite requires an idle Editor.");
            var suite = new SkirmishSharedFogTests();
            suite.SharedIntelBuilderDefaultsToActualFog();
            suite.UnarmedSharedDroneCanScanWithoutGainingAttackPermission();
            suite.DisabledHostilePassengerAgesWithoutLeakingCargoPosition();
            suite.NativeDetectorDiagonalUsesMatchingDomainChebyshevRadius(1);
            suite.NativeDetectorDiagonalUsesMatchingDomainChebyshevRadius(2);
            suite.FactionKnowledgeIsIndependentAndDoesNotOverwriteCombatPolicy();
            suite.LastSeenFreezesPositionThroughPauseHiddenMovementAndDeathThenExpires();
            suite.NativeScanPublishesOnlyMatchingAttemptAndSustainsRealActiveSource(1);
            suite.NativeScanPublishesOnlyMatchingAttemptAndSustainsRealActiveSource(2);
            suite.ActiveScanDoesNotInventKnowledgeAndStaleOrPausedSourcesCannotReveal();
            suite.ExplicitDebugVisionIsSymmetricAndCampaignRemainsUnowned();
            suite.OptionalCheckpointKnowledgeRestoresBothObserversAndRebasesAgeAndSource();
            for (int mode = 0; mode < 6; mode++) suite.CheckpointClearsExpiredObserverHandleWithoutDiscardingObservedState(mode);
            suite.OlderOptionalBufferMigrationPreservesVisibleSampleAndDoesNotInventLastSeenPose();
            Debug.Log("[SkirmishSharedFogTests] result=Passed cases=19 scope=IsolatedActualSystemNotNormalMission");
        }

        private const string Attempt = "fog.actual-system.fixture";
        [Test]
        public void SharedIntelBuilderDefaultsToActualFog()
        {
            var config = ScriptableObject.CreateInstance<SkirmishIntelConfig>();
            try
            {
                config.ConfigureSharedFog();
                Assert.IsTrue(config.SharedFog);
                Assert.IsFalse(config.DevelopmentFullVision);
                Assert.AreEqual(20, config.LastSeenExpireSeconds);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public void FactionKnowledgeIsIndependentAndDoesNotOverwriteCombatPolicy()
        {
            using var world = new World(nameof(FactionKnowledgeIsIndependentAndDoesNotOverwriteCombatPolicy));
            var em = world.EntityManager;
            Entity session = Session(em);
            Entity player = Actor(em, "player", 1, 0, 5);
            Entity enemy = Actor(em, "enemy", 2, 4, 0);
            em.AddComponentData(enemy, new CombatTargetPolicy { Visible = 1 });
            Step(world, 1, .1f);
            Assert.IsTrue(SkirmishFogService.IsVisibleToFaction(em, enemy, 1));
            Assert.IsTrue(SkirmishFogService.IsVisibleToFaction(em, enemy, 2));
            Assert.IsFalse(SkirmishFogService.IsVisibleToFaction(em, player, 2), "Enemy must not inherit player's own-faction sight.");
            Move(em, enemy, 100);
            Step(world, 2, .1f);
            Assert.IsFalse(SkirmishFogService.IsVisible(em, enemy));
            Assert.AreEqual(SkirmishContactSight.LastSeen, Knowledge(em, enemy, 1).Sight);
            Assert.AreEqual(1, em.GetComponentData<CombatTargetPolicy>(enemy).Visible, "Domain policy is not observer fog.");
            Assert.AreEqual(2, em.GetBuffer<SkirmishContactKnowledgeElement>(enemy).Length);
            SkirmishFogService.Project(em, session);
            SkirmishFogService.Project(em, session);
            Assert.AreEqual(SkirmishContactSight.LastSeen, Knowledge(em, enemy, 1).Sight, "Adopting another actor cannot decay prior intelligence.");
        }

        [Test]
        public void LastSeenFreezesPositionThroughPauseHiddenMovementAndDeathThenExpires()
        {
            using var world = new World(nameof(LastSeenFreezesPositionThroughPauseHiddenMovementAndDeathThenExpires));
            var em = world.EntityManager;
            Entity session = Session(em);
            Actor(em, "player", 1, 0, 5);
            Entity enemy = Actor(em, "enemy", 2, 4, 0);
            Step(world, 1, .1f);
            var seen = Knowledge(em, enemy, 1);
            Move(em, enemy, 100);
            Step(world, 2, .1f);
            var stale = Knowledge(em, enemy, 1);
            Assert.AreEqual(seen.LastSeenPosition, stale.LastSeenPosition);
            Assert.AreEqual(100, stale.LastSeenHealth);
            em.SetComponentData(enemy, new UnitHealth { Current = 0, Max = 100 });
            Step(world, 2, 0);
            AssertKnowledgeEqual(stale, Knowledge(em, enemy, 1));
            var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            clock.Paused = 1; em.SetComponentData(session, clock);
            Step(world, 2, .1f);
            AssertKnowledgeEqual(stale, Knowledge(em, enemy, 1));
            clock.Paused = 0; em.SetComponentData(session, clock);
            Step(world, 10, .1f);
            Assert.AreEqual(SkirmishContactSight.LastSeen, Knowledge(em, enemy, 1).Sight);
            Step(world, 21, .1f);
            Assert.AreEqual(SkirmishContactSight.Unknown, Knowledge(em, enemy, 1).Sight);
            Assert.AreEqual(seen.LastSeenPosition, Knowledge(em, enemy, 1).LastSeenPosition);
        }

        [TestCase((byte)1)] [TestCase((byte)2)]
        public void NativeScanPublishesOnlyMatchingAttemptAndSustainsRealActiveSource(byte faction)
        {
            using var world = new World(nameof(NativeScanPublishesOnlyMatchingAttemptAndSustainsRealActiveSource));
            var em = world.EntityManager;
            Session(em);
            byte hostile = faction == 1 ? (byte)2 : (byte)1;
            Entity source = Actor(em, "scanner", faction, 0, 0);
            BindUnarmedDrone(em, source);
            Assert.IsTrue(SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em, source));
            Assert.AreEqual(0, em.GetComponentData<UnitCombat>(source).CanAttack);
            Entity target = Actor(em, "target", hostile, 20, 0);
            Entity unrelated = Actor(em, "old-target", hostile, 20, 0, "old-attempt");
            Entity campaign = em.CreateEntity(typeof(Faction), typeof(UnitGrid), typeof(UnitHealth), typeof(LocalTransform));
            em.SetComponentData(campaign, new Faction { Id = hostile });
            em.SetComponentData(campaign, new UnitGrid { Cell = new int2(20, 0) });
            em.SetComponentData(campaign, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(campaign, LocalTransform.FromPosition(new float3(20, 0, 0)));
            Step(world, 1, .1f);
            Assert.AreEqual(SkirmishContactSight.Unknown, Knowledge(em, target, faction).Sight);
            const int request = 901, frame = 42;
            // Native Scan queue and actual system; no sight/feed/result injection.
            ScanIntelCommandSystem.EnqueueScan(em, request, frame, new int2(20, 0), new float3(20, 0, 0), source, true, false, 4);
            world.GetOrCreateSystem<ScanIntelCommandSystem>().Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            using (var resultsQuery = em.CreateEntityQuery(typeof(ScanIntelCommandQueueComponent), typeof(ScanIntelCommandResultElement)))
            {
                Assert.AreEqual(1, resultsQuery.CalculateEntityCount());
                var results = em.GetBuffer<ScanIntelCommandResultElement>(resultsQuery.GetSingletonEntity(), true);
                Assert.AreEqual(1, results.Length);
                var result = results[0];
                Debug.Log("[SkirmishSharedFogTests.ScanActual] observer="+faction+" accepted="+result.Accepted+" reason="+result.ReasonCode+" source="+result.SourceEntity+" hasSource="+result.HasSourceEntity+" revealed="+result.RevealedCount+" radius="+result.RadiusCells);
                Assert.AreEqual(1, result.Accepted, "Native source/order validation must actually succeed.");
                Assert.AreEqual(source, result.SourceEntity);
                Assert.AreEqual(1, result.HasSourceEntity);
                Assert.AreEqual(1, result.RevealedCount);
            }
            var observed = Knowledge(em, target, faction);
            Assert.AreEqual(SkirmishContactSight.Visible, observed.Sight);
            Assert.AreEqual((byte)2, observed.ObservationKind);
            Assert.AreEqual(source, observed.ObservationSource);
            Assert.AreEqual(request, observed.ScanRequestId);
            Assert.AreEqual(frame, observed.ObservationFrame);
            Assert.IsTrue(em.HasComponent<ScanIntelLastSeen>(target));
            Assert.IsFalse(em.HasComponent<ScanIntelLastSeen>(unrelated));
            Assert.IsFalse(em.HasComponent<ScanIntelLastSeen>(campaign));
            using (var feedQuery = em.CreateEntityQuery(typeof(ScanIntelFeedQueueTag)))
            {
                var feed = em.GetBuffer<ScanIntelFeedEntry>(feedQuery.GetSingletonEntity());
                Assert.AreEqual(1, feed.Length);
                Assert.AreEqual(1, feed[0].RevealedCount);
                Assert.AreEqual(source, feed[0].SourceEntity);
            }
            // Real order lifecycle data is explicit isolated input here. Its active
            // area cannot reveal an unobserved contact before a native emission.
            Move(em, source, 20);
            em.AddComponentData(source, new UnitScanOrder { SourceEntity = source, RequestId = request,
                StartedFrame = frame, CenterCell = new int2(20, 0), RadiusCells = 4,
                HasStarted = 1, StartedTimeSeconds = 1, DurationSeconds = 8 });
            Step(world, 2, .1f);
            Assert.AreEqual(SkirmishContactSight.Visible, Knowledge(em, target, faction).Sight);
            Assert.AreEqual((byte)2, Knowledge(em, target, faction).ObservationKind);
            em.RemoveComponent<UnitScanOrder>(source);
            Step(world, 3, .1f);
            Assert.AreEqual(SkirmishContactSight.LastSeen, Knowledge(em, target, faction).Sight);
            Step(world, 22, .1f);
            Assert.AreEqual(SkirmishContactSight.Unknown, Knowledge(em, target, faction).Sight);
            if (faction == 2)
            {
                em.AddComponent<SelectedUnitTag>(source);
                using var grids = em.CreateEntityQuery(typeof(GridConfig));
                bool Clicked(Vector2 point, EntityManager manager, out int2 cell, out Vector3 position)
                { cell = new int2(20, 0); position = new Vector3(20, 0, 0); return true; }
                var playerResult = new ScanIntelCommandSystem().TryIssueScan(em, Vector2.zero, 902, 43, grids, Clicked);
                Assert.IsFalse(playerResult.HasSourceEntity, "Player selection cannot issue orders using an enemy-owned API source.");
                Assert.IsFalse(em.HasComponent<UnitScanOrder>(source));
            }
        }

        [Test]
        public void ActiveScanDoesNotInventKnowledgeAndStaleOrPausedSourcesCannotReveal()
        {
            using var world = new World(nameof(ActiveScanDoesNotInventKnowledgeAndStaleOrPausedSourcesCannotReveal));
            var em = world.EntityManager;
            Entity session = Session(em);
            Entity source = Actor(em, "source", 1, 20, 0);
            Entity stale = Actor(em, "stale", 1, 20, 0, "old-attempt");
            Entity target = Actor(em, "target", 2, 20, 0);
            em.AddComponentData(source, new UnitScanOrder { SourceEntity = source, RequestId = 1,
                CenterCell = new int2(20, 0), RadiusCells = 4, HasStarted = 1, DurationSeconds = 8 });
            Step(world, 1, .1f);
            Assert.AreEqual(SkirmishContactSight.Unknown, Knowledge(em, target, 1).Sight);
            Assert.IsFalse(SkirmishFogService.ObserveFromScan(em, target, stale, 1, 1));
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(source);
            owned.CapacityReleased = 1; em.SetComponentData(source, owned);
            Assert.IsFalse(SkirmishFogService.ObserveFromScan(em, target, source, 1, 1));
            owned.CapacityReleased = 0; em.SetComponentData(source, owned);
            world.SetTime(new TimeData(1, 0));
            Assert.IsFalse(SkirmishFogService.ObserveFromScan(em, target, source, 1, 1));
            world.SetTime(new TimeData(1, .1f));
            var clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            clock.Paused = 1; em.SetComponentData(session, clock);
            Assert.IsFalse(SkirmishFogService.ObserveFromScan(em, target, source, 1, 1));
            Assert.AreEqual(SkirmishContactSight.Unknown, Knowledge(em, target, 1).Sight);
        }

        [Test]
        public void ExplicitDebugVisionIsSymmetricAndCampaignRemainsUnowned()
        {
            using var world = new World(nameof(ExplicitDebugVisionIsSymmetricAndCampaignRemainsUnowned));
            var em = world.EntityManager;
            Entity session = Session(em);
            Entity player = Actor(em, "player", 1, 0, 0), enemy = Actor(em, "enemy", 2, 100, 0);
            Entity campaign = em.CreateEntity(typeof(LocalTransform));
            var fog = em.GetComponentData<SkirmishFogStateComponent>(session);
            fog.DevelopmentFullVision = 1; em.SetComponentData(session, fog);
            Step(world, 1, .1f);
            Assert.IsTrue(SkirmishFogService.IsVisibleToFaction(em, player, 2));
            Assert.IsTrue(SkirmishFogService.IsVisibleToFaction(em, enemy, 1));
            Assert.IsFalse(em.HasBuffer<SkirmishContactKnowledgeElement>(campaign));
            Assert.IsFalse(em.HasComponent<SkirmishContactSightComponent>(campaign));
        }

        [Test]
        public void OptionalCheckpointKnowledgeRestoresBothObserversAndRebasesAgeAndSource()
        {
            using var sourceWorld = new World("fog-source");
            var em = sourceWorld.EntityManager;
            Session(em);
            Entity source = Actor(em, "observer", 1, 0, 5), target = Actor(em, "target", 2, 4, 0);
            Step(sourceWorld, 1, .1f);
            ScanIntelCommandSystem.EnqueueScan(em, 707, 31, new int2(4, 0), new float3(4, 0, 0), source, true, false, 4);
            sourceWorld.GetOrCreateSystem<ScanIntelCommandSystem>().Update(sourceWorld.Unmanaged);
            em.CompleteAllTrackedJobs();
            Move(em, target, 100);
            Step(sourceWorld, 2, .1f);
            var original = Knowledge(em, target, 1);
            string Id(Entity entity) => entity == source ? "observer" : entity == target ? "target" : null;
            var parts = SkirmishNativeCheckpointParts.Capture(em, target, Id);
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(parts, new HashSet<string> { "observer", "target" }));
            var rebased = SkirmishNativeCheckpointParts.RebaseClocks(parts, 100, 0, 10);
            using var destination = new World("fog-destination");
            var fresh = destination.EntityManager;
            Entity newSource = fresh.CreateEntity(), newTarget = fresh.CreateEntity();
            SkirmishNativeCheckpointParts.Apply(fresh, newTarget, rebased,
                id => id == "observer" ? newSource : id == "target" ? newTarget : Entity.Null);
            var restored = Knowledge(fresh, newTarget, 1);
            Assert.AreEqual(original.Sight, restored.Sight);
            Assert.AreEqual(original.LastSeenPosition, restored.LastSeenPosition);
            Assert.AreEqual(original.LastSeenCell, restored.LastSeenCell);
            Assert.AreEqual(original.LastSeenHealth, restored.LastSeenHealth);
            Assert.AreEqual(original.LastSeenMax, restored.LastSeenMax);
            Assert.AreEqual(original.ObservationSourceStableObjectId, restored.ObservationSourceStableObjectId);
            Assert.AreEqual(original.LastObservedTimeSeconds + 100, restored.LastObservedTimeSeconds);
            Assert.AreEqual(newSource, restored.ObservationSource);
            Assert.AreEqual(original.ScanRequestId, restored.ScanRequestId);
            Assert.AreEqual(original.ObservationFrame + 10, restored.ObservationFrame);
            Assert.AreEqual((byte)2, restored.ObservationKind);
            Assert.AreEqual(2, fresh.GetBuffer<SkirmishContactKnowledgeElement>(newTarget).Length);
            // Missing optional buffer in older files remains a valid codec set.
            var oldParts = Array.FindAll(parts, p => p.Type != typeof(SkirmishContactKnowledgeElement).FullName);
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(oldParts, new HashSet<string> { "observer", "target" }));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void CheckpointClearsExpiredObserverHandleWithoutDiscardingObservedState(int invalidation)
        {
            using var world = new World(nameof(CheckpointClearsExpiredObserverHandleWithoutDiscardingObservedState));
            var em = world.EntityManager;
            Session(em);
            Entity observer = Actor(em, "observer", 1, 0, 5), target = Actor(em, "target", 2, 4, 0);
            Step(world, 1, .1f);
            var observed = Knowledge(em, target, 1);
            if (invalidation == 0) em.DestroyEntity(observer);
            else if (invalidation == 1)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(observer);
                owned.StableObjectId = new FixedString64Bytes("recycled-actor"); em.SetComponentData(observer, owned);
            }
            else if (invalidation == 2)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(observer);
                owned.CapacityReleased = 1; em.SetComponentData(observer, owned);
            }
            else if (invalidation == 4) em.SetComponentData(observer, new UnitHealth { Current = 0, Max = 100 });
            else if (invalidation == 5) em.AddComponent<Disabled>(observer);
            var parts = SkirmishNativeCheckpointParts.Capture(em, target,
                entity => entity == target ? "target" : invalidation != 3 && entity == observer ? "observer" : null);
            Assert.IsTrue(SkirmishNativeCheckpointParts.Validate(parts, new HashSet<string> { "target" }));
            Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadBuffer<SkirmishContactKnowledgeElement>(parts, out var values));
            Assert.AreEqual(Entity.Null, values[0].ObservationSource);
            Assert.AreEqual(observed.ObservationSourceStableObjectId, values[0].ObservationSourceStableObjectId);
            Assert.AreEqual(observed.LastSeenPosition, values[0].LastSeenPosition);
            Assert.AreEqual(observed.LastSeenHealth, values[0].LastSeenHealth);
        }

        [Test]
        public void OlderOptionalBufferMigrationPreservesVisibleSampleAndDoesNotInventLastSeenPose()
        {
            using var world = new World(nameof(OlderOptionalBufferMigrationPreservesVisibleSampleAndDoesNotInventLastSeenPose));
            var em = world.EntityManager;
            Entity session = Session(em);
            Entity visible = Actor(em, "old-visible", 2, 71, 0), stale = Actor(em, "old-lastseen", 2, 97, 0);
            em.AddComponentData(visible, new SkirmishContactSightComponent { Sight = SkirmishContactSight.Visible });
            em.AddComponentData(stale, new SkirmishContactSightComponent { Sight = SkirmishContactSight.LastSeen });
            world.SetTime(new TimeData(4, 0));
            SkirmishFogService.Project(em, session);
            Assert.AreEqual(new float3(71, 0, 0), Knowledge(em, visible, 1).LastSeenPosition);
            Assert.AreEqual(100, Knowledge(em, visible, 1).LastSeenHealth);
            Assert.AreEqual(SkirmishContactSight.Unknown, Knowledge(em, stale, 1).Sight);
            Assert.AreEqual(float3.zero, Knowledge(em, stale, 1).LastSeenPosition);
            var before = Knowledge(em, visible, 1);
            SkirmishFogService.Project(em, session);
            AssertKnowledgeEqual(before, Knowledge(em, visible, 1));
        }

        [Test]
        public void UnarmedSharedDroneCanScanWithoutGainingAttackPermission()
        {
            using var world = new World(nameof(UnarmedSharedDroneCanScanWithoutGainingAttackPermission));
            var em = world.EntityManager;
            Session(em);
            Entity drone = Actor(em, "drone", 1, 0, 0);
            BindUnarmedDrone(em, drone);
            Assert.AreEqual(0, em.GetComponentData<UnitCombat>(drone).CanAttack);
            Assert.AreEqual(CombatTargetDomain.None, em.GetComponentData<CombatTargetPolicy>(drone).AllowedTargets);
            Assert.IsTrue(SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em, drone));
            em.RemoveComponent<SkirmishSharedActorTag>(drone);
            Assert.IsFalse(SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em, drone), "Typed role alone cannot alter legacy capability.");
            em.AddComponent<SkirmishSharedActorTag>(drone);
            em.SetComponentData(drone, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.TransportPlane });
            Assert.IsFalse(SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em, drone), "Other unarmed shared roles retain policy.");
            em.RemoveComponent<SkirmishUnitRoleComponent>(drone);
            Assert.IsFalse(SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em, drone));
            em.AddComponentData(drone, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Drone });
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(drone);
            owned.CapacityReleased = 1; em.SetComponentData(drone, owned);
            Assert.IsFalse(SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em, drone));
            Entity legacy = Actor(em, "legacy-armed", 1, 0, 0);
            Assert.IsTrue(SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em, legacy));
            em.SetComponentData(legacy, new UnitCombat { CanAttack = 0 });
            Assert.IsFalse(SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em, legacy));
        }

        [TestCase((byte)1)] [TestCase((byte)2)]
        public void NativeDetectorDiagonalUsesMatchingDomainChebyshevRadius(byte kind)
        {
            using var world = new World(nameof(NativeDetectorDiagonalUsesMatchingDomainChebyshevRadius));
            var em = world.EntityManager;
            Session(em);
            Entity source = Actor(em, "detector", 1, 0, 0), target = Actor(em, "target", 2, 5, 0);
            em.AddComponentData(source, new ThreatDetector { Kind = kind, RadiusCells = 5 });
            em.SetComponentData(target, new UnitGrid { Cell = new int2(5, 5) });
            em.SetComponentData(target, LocalTransform.FromPosition(new float3(5, 0, 5)));
            if (kind == 2) em.AddComponent<UnitAirMovement>(target);
            else em.AddComponentData(target, new UnitMovementBehavior { UsesVehicleMotion = 1 });
            Entity infantry = Actor(em, "infantry-control", 2, 5, 0), structure = Actor(em, "structure-control", 2, 5, 0);
            em.SetComponentData(infantry, new UnitGrid { Cell = new int2(5, 5) });
            em.SetComponentData(structure, new UnitGrid { Cell = new int2(5, 5) });
            em.AddComponent<RuntimeBuildingCombatInfo>(structure);
            Step(world, 1, .1f);
            Assert.AreEqual(SkirmishContactSight.Unknown, Knowledge(em, infantry, 1).Sight, "Native detector does not acquire infantry.");
            Assert.AreEqual(SkirmishContactSight.Unknown, Knowledge(em, structure, 1).Sight, "Native detector does not acquire structures.");
            Assert.IsTrue(SkirmishFogService.IsVisible(em, target), "Native detector diagonal inside grid radius must reveal.");
            em.SetComponentData(source, new ThreatDetector { Kind = kind == 1 ? (byte)2 : (byte)1, RadiusCells = 5 });
            Step(world, 2, .1f);
            Assert.AreEqual(SkirmishContactSight.LastSeen, Knowledge(em, target, 1).Sight, "Opposite-domain detector cannot observe contact.");
        }

        private static void BindUnarmedDrone(EntityManager em, Entity source)
        {
            em.AddComponent<SkirmishSharedActorTag>(source);
            em.AddComponentData(source, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Drone, Category = SkirmishPopulationCategory.Air });
            SkirmishSharedCombatBinding.Bind(em, source, new SkirmishRoleOverlay { RoleKind = SkirmishRoleKind.Drone,
                RoleId = SkirmishRoleIds.Drone, Damage = 0, RangeWorld = 0, TargetDomains = SkirmishTargetDomain.None });
        }

        [Test]
        public void DisabledHostilePassengerAgesWithoutLeakingCargoPosition()
        {
            using var world = new World(nameof(DisabledHostilePassengerAgesWithoutLeakingCargoPosition));
            var em = world.EntityManager;
            Session(em);
            Actor(em, "player", 1, 0, 5);
            Entity enemy = Actor(em, "passenger", 2, 4, 0);
            Step(world, 1, .1f);
            var before = Knowledge(em, enemy, 1);
            em.AddComponent<Disabled>(enemy);
            em.AddComponent<UnitTransportPassenger>(enemy);
            // Disabled cargo remains physically in range here. It cannot confer
            // fresh opposing knowledge or disclose its hidden transport position.
            Move(em, enemy, 3);
            Step(world, 2, .1f);
            Assert.AreEqual(SkirmishContactSight.LastSeen, Knowledge(em, enemy, 1).Sight);
            Assert.AreEqual(before.LastSeenPosition, Knowledge(em, enemy, 1).LastSeenPosition);
            Step(world, 21, .1f);
            Assert.AreEqual(SkirmishContactSight.Unknown, Knowledge(em, enemy, 1).Sight);
        }

        private static Entity Session(EntityManager em)
        {
            Entity grid = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(grid, new GridConfig { Width = 200, Height = 100, CellSize = 1 });
            Entity session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishFogStateComponent), typeof(SkirmishObjectiveClockComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = new FixedString64Bytes(Attempt), Phase = SkirmishSessionPhase.Playing });
            em.SetComponentData(session, new SkirmishFogStateComponent { SharedFog = 1, LastSeenExpireSeconds = 20 });
            return session;
        }
        private static Entity Actor(EntityManager em, string id, byte faction, float x, int radius, string attempt = Attempt)
        {
            Entity entity = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(Faction), typeof(UnitGrid),
                typeof(LocalTransform), typeof(UnitHealth), typeof(UnitCombat), typeof(UnitMove));
            em.SetComponentData(entity, new SkirmishAttemptOwnedComponent { SessionId = new FixedString64Bytes(attempt),
                StableObjectId = new FixedString64Bytes(id), FactionId = faction });
            em.SetComponentData(entity, new Faction { Id = faction });
            em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(entity, new UnitCombat { AggroRangeCells = radius, CanAttack = 1 });
            em.SetComponentData(entity, new UnitMove { WalkSpeed = 6, RoadSpeedMultiplier = 1, ArriveDistance = .1f });
            Move(em, entity, x);
            return entity;
        }
        private static void Move(EntityManager em, Entity entity, float x)
        {
            em.SetComponentData(entity, LocalTransform.FromPosition(new float3(x, 0, 0)));
            em.SetComponentData(entity, new UnitGrid { Cell = new int2((int)x, 0) });
        }
        private static void Step(World world, double time, float delta)
        {
            world.SetTime(new TimeData(time, delta));
            world.GetOrCreateSystem<SkirmishSharedFogUpdateSystem>().Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();
        }
        private static SkirmishContactKnowledgeElement Knowledge(EntityManager em, Entity entity, byte faction)
        {
            Assert.IsTrue(SkirmishFogService.TryGetKnowledge(em, entity, faction, out var knowledge));
            return knowledge;
        }
        private static void AssertKnowledgeEqual(SkirmishContactKnowledgeElement expected, SkirmishContactKnowledgeElement actual)
        {
            Assert.AreEqual(expected.ObserverFactionId, actual.ObserverFactionId);
            Assert.AreEqual(expected.Sight, actual.Sight);
            Assert.AreEqual(expected.LastSeenCell, actual.LastSeenCell);
            Assert.AreEqual(expected.LastSeenPosition, actual.LastSeenPosition);
            Assert.AreEqual(expected.LastSeenHealth, actual.LastSeenHealth);
            Assert.AreEqual(expected.LastSeenMax, actual.LastSeenMax);
            Assert.AreEqual(expected.ObservationSourceStableObjectId, actual.ObservationSourceStableObjectId);
            Assert.AreEqual(expected.LastObservedTimeSeconds, actual.LastObservedTimeSeconds);
            Assert.AreEqual(expected.ObservationSource, actual.ObservationSource);
            Assert.AreEqual(expected.ScanRequestId, actual.ScanRequestId);
            Assert.AreEqual(expected.ObservationFrame, actual.ObservationFrame);
            Assert.AreEqual(expected.ObservationKind, actual.ObservationKind);
        }
    }
}
