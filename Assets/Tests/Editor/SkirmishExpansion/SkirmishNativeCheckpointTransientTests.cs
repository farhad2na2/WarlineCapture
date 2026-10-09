using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeCheckpointTransientTests
    {
        [TestCase(false)] [TestCase(true)]
        public void FlightRestoresAcrossWorldsUsingFreshBakedSlot(bool air)
        {
            using var source = new World("missile-source");
            var em = source.EntityManager;
            Entity launcher = em.CreateEntity(), target = em.CreateEntity(), parent = em.CreateEntity();
            Entity visual = em.CreateEntity(typeof(LocalTransform));
            em.SetComponentData(visual, LocalTransform.FromPosition(new float3(31, 40, 49)));
            Entity projectile = air ? visual : em.CreateEntity(typeof(LocalTransform));
            if (air)
            {
                em.AddBuffer<AirMissileLauncherMissileVisualComponent>(launcher).Add(new AirMissileLauncherMissileVisualComponent { Missile = visual, SlotIndex = 2 });
                em.AddComponentData(visual, new AirMissileFlyingVisualComponent { Launcher = launcher, OriginalParent = parent, SlotIndex = 2 });
                em.AddComponentData(projectile, new AirMissileProjectileComponent { Source = launcher, Target = target, ElapsedSeconds = 2.5f, LifetimeSeconds = 12, Damage = 50 });
            }
            else
            {
                em.AddBuffer<GroundMissileLauncherRocketVisualComponent>(launcher).Add(new GroundMissileLauncherRocketVisualComponent { Rocket = visual, SlotIndex = 2 });
                em.AddComponentData(visual, new GroundMissileFlyingRocketVisualComponent { Launcher = launcher, OriginalParent = parent, SlotIndex = 2, ElapsedSeconds = 2.5f, DurationSeconds = 12 });
                em.AddComponentData(projectile, new GroundMissileProjectileComponent { Source = launcher, TargetEntity = target, ElapsedSeconds = 2.5f, DurationSeconds = 12, Damage = 50 });
            }
            em.AddComponentData(projectile, new MissileInterceptionTargetComponent { Source = launcher, FactionId = 1 });
            em.AddComponentData(projectile, new MissileInterceptedComponent { Interceptor = target, VisualSeparation = 3 });
            var identities = new Dictionary<Entity, string> { [launcher] = "launcher", [target] = "target" };
            var ids = new HashSet<string> { "launcher", "target" };
            SkirmishNativeCheckpointTransientState.Capture(em, new[] { launcher, target }, identities, ids, out var visuals, out var states);
            var wire = JsonUtility.FromJson<Wire>(JsonUtility.ToJson(new Wire { Visuals = visuals, States = states }));
            Assert.AreEqual(1, wire.Visuals.Length);
            Assert.AreEqual(air ? 0 : 1, wire.States.Length);
            var published = new HashSet<string> { "launcher", "target" };
            Assert.IsTrue(SkirmishNativeCheckpointTransientState.PublishAliases(wire.Visuals, wire.States, published,
                new Dictionary<string, byte> { ["launcher"] = 0, ["target"] = 0 }));
            using var destination = new World("missile-destination");
            var fresh = destination.EntityManager;
            fresh.CreateEntity();
            Entity newLauncher = fresh.CreateEntity(), newTarget = fresh.CreateEntity(), newParent = fresh.CreateEntity();
            Entity newVisual = fresh.CreateEntity(typeof(LocalTransform), typeof(Parent));
            fresh.SetComponentData(newVisual, LocalTransform.Identity);
            fresh.SetComponentData(newVisual, new Parent { Value = newParent });
            if (air) fresh.AddBuffer<AirMissileLauncherMissileVisualComponent>(newLauncher).Add(new AirMissileLauncherMissileVisualComponent { Missile = newVisual, SlotIndex = 2 });
            else fresh.AddBuffer<GroundMissileLauncherRocketVisualComponent>(newLauncher).Add(new GroundMissileLauncherRocketVisualComponent { Rocket = newVisual, SlotIndex = 2 });
            var actors = new Dictionary<string, Entity> { ["launcher"] = newLauncher, ["target"] = newTarget };
            var bindings = SkirmishNativeCheckpointTransientState.Rebuild(fresh, "saved-session", wire.Visuals, wire.States, actors);
            Assert.AreEqual(newParent, fresh.GetComponentData<Parent>(newVisual).Value, "Preflight must not detach the visual.");
            var prepared = new SkirmishNativeCheckpointRecoveryPreparation.Prepared { WorldSequence = fresh.WorldUnmanaged.SequenceNumber, Transients = bindings };
            Entity session = fresh.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            prepared.Session = session;
            fresh.SetComponentData(session, new SkirmishExpandedSessionComponent { Phase = Game.Skirmish.Contracts.SkirmishSessionPhase.Spawning });
            prepared.Identities.Add("$session", session);
            foreach (var actor in actors) prepared.Identities.Add(actor.Key, actor.Value);
            foreach (var alias in bindings.Identities) prepared.Identities.Add(alias.Key, alias.Value);
            foreach (var saved in wire.Visuals) prepared.Parts.Add(bindings.Identities[saved.Id], saved.Parts);
            foreach (var saved in wire.States) prepared.Parts.Add(bindings.Identities[saved.Id], saved.Parts);
            prepared.Parts.Add(session, SkirmishNativeCheckpointParts.Capture(fresh, session, _ => null));
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryCommit.TryCommit(fresh, prepared, out var failure), failure);
            Assert.IsFalse(fresh.HasComponent<Parent>(newVisual));
            Assert.AreEqual(new float3(31, 40, 49), fresh.GetComponentData<LocalTransform>(newVisual).Position);
            Entity newProjectile = air ? newVisual : bindings.Identities[wire.States[0].Id];
            Assert.AreEqual(newLauncher, fresh.GetComponentData<MissileInterceptionTargetComponent>(newProjectile).Source);
            Assert.AreEqual(newTarget, fresh.GetComponentData<MissileInterceptedComponent>(newProjectile).Interceptor);
            if (air)
            {
                var shot = fresh.GetComponentData<AirMissileProjectileComponent>(newProjectile);
                Assert.AreEqual(2.5f, shot.ElapsedSeconds); Assert.AreEqual(newTarget, shot.Target);
                Assert.AreEqual(newParent, fresh.GetComponentData<AirMissileFlyingVisualComponent>(newVisual).OriginalParent);
            }
            else
            {
                var shot = fresh.GetComponentData<GroundMissileProjectileComponent>(newProjectile);
                Assert.AreEqual(2.5f, shot.ElapsedSeconds); Assert.AreEqual(newTarget, shot.TargetEntity);
                Assert.AreEqual(newParent, fresh.GetComponentData<GroundMissileFlyingRocketVisualComponent>(newVisual).OriginalParent);
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MalformedVisualCannotInjectActorStateOrRedirectItsLauncher(int corruption)
        {
            using var world = new World("malformed-missile-visual");
            var em = world.EntityManager;
            Entity launcher = em.CreateEntity(), target = em.CreateEntity();
            Entity visual = em.CreateEntity(typeof(LocalTransform));
            em.SetComponentData(visual, LocalTransform.Identity);
            em.AddBuffer<GroundMissileLauncherRocketVisualComponent>(launcher).Add(
                new GroundMissileLauncherRocketVisualComponent { Rocket = visual, SlotIndex = 0 });
            em.AddComponentData(visual, new GroundMissileFlyingRocketVisualComponent
                { Launcher = launcher, SlotIndex = corruption == 2 ? 99 : 0 });
            if (corruption == 0) em.AddComponentData(visual, new UnitHealth { Current = 900, Max = 900 });
            var identities = new Dictionary<Entity, string> { [launcher] = "launcher", [target] = "target" };
            SkirmishNativeCheckpointTransientState.Capture(em, new[] { launcher, target }, identities,
                new HashSet<string> { "launcher", "target" }, out var visuals, out var transients);
            if (corruption == 1)
                foreach (var part in visuals[0].Parts)
                    if (part.Type == typeof(GroundMissileFlyingRocketVisualComponent).FullName)
                        part.References[0].ObjectId = "target";
            Assert.IsFalse(SkirmishNativeCheckpointTransientState.PublishAliases(visuals, transients,
                new HashSet<string> { "launcher", "target" }, new Dictionary<string, byte> { ["launcher"] = 0, ["target"] = 0 }));
        }

        [Test]
        public void MissingSlotRejectsBeforeCreatingOrChangingHierarchy()
        {
            using var world = new World("missing-missile-slot"); var em = world.EntityManager;
            var launcher = em.CreateEntity(); int count = em.UniversalQuery.CalculateEntityCount();
            Assert.Throws<ArgumentException>(() => SkirmishNativeCheckpointTransientState.Rebuild(em, "session",
                new[] { new SkirmishNativeCheckpointTransientState.Visual { Id = "visual", LauncherId = "launcher", Kind = 1, Slot = 1, ParentId = "parent" } },
                Array.Empty<SkirmishNativeCheckpointTransientState.Transient>(), new Dictionary<string, Entity> { ["launcher"] = launcher }));
            Assert.AreEqual(count, em.UniversalQuery.CalculateEntityCount());
        }

        [Test]
        public void ActiveUnsupportedSupportRejectsCapture()
        {
            using var world = new World("support-capture-guard"); var em = world.EntityManager;
            em.CreateEntity(typeof(SupportFlightComponent));
            Assert.Throws<InvalidOperationException>(() => SkirmishNativeCheckpointTransientState.Capture(em, Array.Empty<Entity>(),
                new Dictionary<Entity, string>(), new HashSet<string>(), out _, out _));
        }

        [Test]
        public void SessionTeardownRemovesOnlyItsReconstructedTransient()
        {
            using var world = new World("transient-teardown"); var em = world.EntityManager;
            Entity mine = em.CreateEntity(typeof(SkirmishNativeTransientOwner)), other = em.CreateEntity(typeof(SkirmishNativeTransientOwner));
            em.SetComponentData(mine, new SkirmishNativeTransientOwner { SessionId = new FixedString64Bytes("mine") });
            em.SetComponentData(other, new SkirmishNativeTransientOwner { SessionId = new FixedString64Bytes("other") });
            SkirmishNativeCheckpointTransientState.DestroySession(em, new FixedString64Bytes("mine"));
            Assert.IsFalse(em.Exists(mine)); Assert.IsTrue(em.Exists(other));
        }

        [TestCase(UnitTransportPassengerKind.Soldier)] [TestCase(UnitTransportPassengerKind.Vehicle)]
        public void DropVisualRestoresFromFreshRegistryAndRebasesCleanup(byte kind)
        {
            using var source = new World("drop-source"); var em = source.EntityManager;
            Entity passenger = em.CreateEntity(), visual = em.CreateEntity(typeof(LocalTransform));
            em.SetComponentData(visual, LocalTransform.FromPosition(new float3(21, 30, 40)));
            em.AddComponentData(visual, new UnitTransportDropVisualSource { CarrierSourceKey = new FixedString64Bytes("carrier"), PassengerKind = kind });
            em.AddComponentData(visual, new UnitTransportAirdropVisualCleanup { DestroyAt = 125 });
            if (kind == UnitTransportPassengerKind.Vehicle) em.AddComponentData(passenger, new UnitTransportCargoDropComponent { VisualEntity = visual, StartedAt = 100, DurationSeconds = 10 });
            else em.AddComponentData(passenger, new UnitTransportParachuteDropComponent { VisualEntity = visual, StartedAt = 100, DurationSeconds = 10 });
            var identities = new Dictionary<Entity, string> { [passenger] = "passenger" };
            SkirmishNativeCheckpointTransientState.Capture(em, new[] { passenger }, identities, new HashSet<string> { "passenger" }, out var visuals, out var states, out var drops);
            Assert.AreEqual(1, drops.Length);
            var passengerParts = SkirmishNativeCheckpointParts.Capture(em, passenger, e => identities.TryGetValue(e, out string id) ? id : null);
            using var destination = new World("drop-destination"); var fresh = destination.EntityManager;
            fresh.CreateEntity(); Entity newPassenger = fresh.CreateEntity();
            Entity prefab = fresh.CreateEntity(typeof(Prefab), typeof(LocalTransform)); fresh.SetComponentData(prefab, LocalTransform.Identity);
            Entity registry = fresh.CreateEntity(); fresh.AddBuffer<UnitTransportAirdropVisualPrefabRegistryEntry>(registry).Add(new UnitTransportAirdropVisualPrefabRegistryEntry {
                SourceKey = new FixedString64Bytes("carrier"), SoldierParachuteVisualPrefab = prefab, VehicleEmergencyDropVisualPrefab = prefab });
            var actors = new Dictionary<string, Entity> { ["passenger"] = newPassenger };
            var bindings = SkirmishNativeCheckpointTransientState.Rebuild(fresh, "saved", visuals, states, actors, drops);
            Entity Resolve(string id) => id == "passenger" ? newPassenger : bindings.Identities[id];
            Entity newVisual = Resolve(drops[0].Id);
            SkirmishNativeCheckpointParts.Apply(fresh, newVisual, SkirmishNativeCheckpointParts.RebaseClocks(drops[0].Parts, -90, 0, 0), Resolve);
            SkirmishNativeCheckpointParts.Apply(fresh, newPassenger, SkirmishNativeCheckpointParts.RebaseClocks(passengerParts, -90, 0, 0), Resolve);
            Assert.AreEqual(35, fresh.GetComponentData<UnitTransportAirdropVisualCleanup>(newVisual).DestroyAt);
            Assert.AreEqual(new float3(21, 30, 40), fresh.GetComponentData<LocalTransform>(newVisual).Position);
            Assert.IsFalse(fresh.HasComponent<Prefab>(newVisual));
            if (kind == UnitTransportPassengerKind.Vehicle) Assert.AreEqual(newVisual, fresh.GetComponentData<UnitTransportCargoDropComponent>(newPassenger).VisualEntity);
            else Assert.AreEqual(newVisual, fresh.GetComponentData<UnitTransportParachuteDropComponent>(newPassenger).VisualEntity);
        }

        [Test]
        public void MissingDropPrefabRollsBackPreviouslyCreatedProjectile()
        {
            using var world = new World("drop-rollback"); var em = world.EntityManager;
            int count = em.UniversalQuery.CalculateEntityCount();
            Assert.Throws<ArgumentException>(() => SkirmishNativeCheckpointTransientState.Rebuild(em, "saved",
                Array.Empty<SkirmishNativeCheckpointTransientState.Visual>(),
                new[] { new SkirmishNativeCheckpointTransientState.Transient { Id = "missile-state/0", Kind = 1 } },
                new Dictionary<string, Entity>(), new[] { new SkirmishNativeCheckpointTransientState.DropVisual { Id = "drop-visual/0", CarrierSourceKey = "missing", PassengerKind = UnitTransportPassengerKind.Soldier } }));
            Assert.AreEqual(count, em.UniversalQuery.CalculateEntityCount());
        }

        [Test]
        public void ActiveSupportRootRejectsCaptureWithoutEffects()
        {
            using var world = new World("support-root-guard"); var em = world.EntityManager;
            Entity root = em.CreateEntity(typeof(SupportSessionComponent));
            em.SetComponentData(root, new SupportSessionComponent { Active = 1 });
            Assert.Throws<InvalidOperationException>(() => SkirmishNativeCheckpointTransientState.Capture(em, Array.Empty<Entity>(),
                new Dictionary<Entity, string>(), new HashSet<string>(), out _, out _));
        }

        [Test]
        public void CarriedPassengerKeepsHiddenFreshHierarchyUntilDisembark()
        {
            using var world = new World("carried-passenger-restore"); var em = world.EntityManager;
            Entity session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { Phase = Game.Skirmish.Contracts.SkirmishSessionPhase.Spawning });
            Entity carrier = em.CreateEntity(), passenger = em.CreateEntity(typeof(LocalTransform)), child = em.CreateEntity(typeof(LocalTransform));
            em.SetComponentData(passenger, LocalTransform.FromScale(0.75f)); em.SetComponentData(child, LocalTransform.FromScale(2));
            em.AddBuffer<Child>(passenger).Add(new Child { Value = child });
            var prepared = new SkirmishNativeCheckpointRecoveryPreparation.Prepared { WorldSequence = em.WorldUnmanaged.SequenceNumber, Session = session };
            prepared.Identities.Add("$session", session); prepared.Identities.Add("carrier", carrier); prepared.Identities.Add("passenger", passenger);
            prepared.Parts.Add(session, SkirmishNativeCheckpointParts.Capture(em, session, _ => null));
            // Capture the state that the save would contain while boarded.
            em.AddComponentData(passenger, new UnitTransportPassenger { Transport = carrier }); em.AddComponent<Disabled>(passenger);
            var transform = em.GetComponentData<LocalTransform>(passenger); transform.Scale = 0; em.SetComponentData(passenger, transform);
            prepared.Parts.Add(passenger, SkirmishNativeCheckpointParts.Capture(em, passenger, e => e == carrier ? "carrier" : null));
            prepared.HiddenPassengers.Add(passenger, 0.75f);
            em.RemoveComponent<UnitTransportPassenger>(passenger); em.RemoveComponent<Disabled>(passenger);
            transform.Scale = 0.75f; em.SetComponentData(passenger, transform);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryCommit.TryCommit(em, prepared, out var failure), failure);
            Assert.AreEqual(0, em.GetComponentData<LocalTransform>(passenger).Scale);
            Assert.AreEqual(0, em.GetComponentData<LocalTransform>(child).Scale);
            Assert.IsTrue(em.HasComponent<Disabled>(child));
            UnitTransportVisualUtility.SetPassengerVisible(em, passenger, true);
            em.RemoveComponent<Disabled>(passenger);
            Assert.AreEqual(0.75f, em.GetComponentData<LocalTransform>(passenger).Scale);
            Assert.AreEqual(2, em.GetComponentData<LocalTransform>(child).Scale);
            Assert.IsFalse(em.HasComponent<Disabled>(child));
        }

        [Serializable] private sealed class Wire
        {
            public SkirmishNativeCheckpointTransientState.Visual[] Visuals;
            public SkirmishNativeCheckpointTransientState.Transient[] States;
        }
        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointTransientTests();
            suite.FlightRestoresAcrossWorldsUsingFreshBakedSlot(false);
            suite.FlightRestoresAcrossWorldsUsingFreshBakedSlot(true);
            suite.MalformedVisualCannotInjectActorStateOrRedirectItsLauncher(0);
            suite.MalformedVisualCannotInjectActorStateOrRedirectItsLauncher(1);
            suite.MalformedVisualCannotInjectActorStateOrRedirectItsLauncher(2);
            suite.MissingSlotRejectsBeforeCreatingOrChangingHierarchy();
            suite.ActiveUnsupportedSupportRejectsCapture();
            suite.SessionTeardownRemovesOnlyItsReconstructedTransient();
            suite.DropVisualRestoresFromFreshRegistryAndRebasesCleanup(UnitTransportPassengerKind.Soldier);
            suite.DropVisualRestoresFromFreshRegistryAndRebasesCleanup(UnitTransportPassengerKind.Vehicle);
            suite.MissingDropPrefabRollsBackPreviouslyCreatedProjectile();
            suite.ActiveSupportRootRejectsCaptureWithoutEffects();
            suite.CarriedPassengerKeepsHiddenFreshHierarchyUntilDisembark();
            Debug.Log("[SkirmishNativeCheckpointTransientTests] result=Passed tests=13 nativePlayerRecovery=0");
        }
    }
}
