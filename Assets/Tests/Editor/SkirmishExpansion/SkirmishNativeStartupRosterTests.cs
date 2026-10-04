using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishNativeStartupRosterTests
    {
        public static void RunFocusedValidation()
        {
            using var world = new World("NativeStartupRosterValidation");
            var em = world.EntityManager;
            var attempt = new FixedString64Bytes("roster-validation");
            var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = attempt });
            var setup = new SkirmishResolvedSetup
            {
                Forces = new[] { new SkirmishResolvedForceEntry
                { RoleId = "role.rifle", RoleKind = SkirmishRoleKind.Rifle, FactionId = 1, Quantity = 2 } }
            };
            Entity Actor(string id, uint reservation = 0)
            {
                var actor = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent), typeof(SkirmishVisualSpawnedComponent));
                em.SetComponentData(actor, new SkirmishAttemptOwnedComponent
                { SessionId = attempt, StableObjectId = new FixedString64Bytes(id), FactionId = 1, ReservationId = reservation });
                em.SetComponentData(actor, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Rifle });
                em.SetComponentData(actor, new SkirmishVisualSpawnedComponent { Spawned = 1 });
                return actor;
            }
            var first = Actor("role.rifle.1.0.0");
            Actor("role.rifle.1.0.1");
            Assert.IsTrue(SkirmishS002AriaRunHarness.NativeStartupRosterMatches(em, session, setup, out int initial, out int produced));
            Assert.AreEqual(2, initial); Assert.AreEqual(0, produced);
            var delivery = new SkirmishProductionReservation
            { ReservationId = 1, FactionId = 1, Role = SkirmishRoleKind.Rifle, MemberCount = 4, DeliveredMembers = 1,
                MaterialsPaid = 120, ProducerRuntimeId = 10 };
            var ledger = em.AddBuffer<SkirmishProductionReservation>(session); ledger.Add(delivery);
            var reinforcement = Actor("production.1.0", 1);
            Assert.IsTrue(SkirmishS002AriaRunHarness.NativeStartupRosterMatches(em, session, setup, out initial, out produced));
            Assert.AreEqual(2, initial); Assert.AreEqual(1, produced);
            ledger = em.GetBuffer<SkirmishProductionReservation>(session); ledger.Clear();
            Assert.IsFalse(SkirmishS002AriaRunHarness.NativeStartupRosterMatches(em, session, setup, out _, out _), "Unrecorded extra actor must fail.");
            delivery.MaterialsPaid = 0; ledger.Add(delivery);
            Assert.IsFalse(SkirmishS002AriaRunHarness.NativeStartupRosterMatches(em, session, setup, out _, out _), "Unpaid extra actor must fail.");
            delivery.MaterialsPaid = 120; ledger[0] = delivery;
            em.SetComponentData(reinforcement, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Tank });
            Assert.IsFalse(SkirmishS002AriaRunHarness.NativeStartupRosterMatches(em, session, setup, out _, out _), "Wrong delivered role must fail.");
            em.SetComponentData(reinforcement, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Rifle });
            em.DestroyEntity(first);
            Assert.IsFalse(SkirmishS002AriaRunHarness.NativeStartupRosterMatches(em, session, setup, out _, out _), "A reinforcement must not replace a missing starting actor.");
            Debug.Log("[SkirmishNativeStartupRosterTests] result=Passed cases=6");
        }
    }
}
