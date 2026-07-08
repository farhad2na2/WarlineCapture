using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class AlertObjectiveAudioFeedbackTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new AlertObjectiveAudioFeedbackTests();
            tests.ThreatWarningAudio_ResolvesMinorAndCriticalAlerts();
            passed++;
            tests.TryEmitThreatWarningAudio_EnqueuesAlertsBusRequest();
            passed++;
            tests.ThreatDetectionWarningSystem_NewCloseAirThreatEnqueuesCriticalAlertAudio();
            passed++;

            Debug.Log($"[AlertObjectiveAudioFeedbackValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AlertObjectiveAudioFeedbackValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ThreatWarningAudio_ResolvesMinorAndCriticalAlerts()
    {
        AssertThreatAudio(
            ThreatWarningType.Ground,
            etaSeconds: 12f,
            threatCount: 1,
            AudioEventIds.AlertThreatMinor,
            AudioEventIds.AlertThreatMinorHash,
            AudioPlaybackPriority.High,
            expectedCooldownSeconds: 3f);

        AssertThreatAudio(
            ThreatWarningType.Air,
            etaSeconds: 0f,
            threatCount: 1,
            AudioEventIds.AlertThreatCritical,
            AudioEventIds.AlertThreatCriticalHash,
            AudioPlaybackPriority.Critical,
            expectedCooldownSeconds: 4f);

        AssertThreatAudio(
            ThreatWarningType.Ground,
            etaSeconds: 10f,
            threatCount: 2,
            AudioEventIds.AlertThreatCritical,
            AudioEventIds.AlertThreatCriticalHash,
            AudioPlaybackPriority.Critical,
            expectedCooldownSeconds: 4f);
    }

    [Test]
    public void TryEmitThreatWarningAudio_EnqueuesAlertsBusRequest()
    {
        using World world = new("AlertAudioFeedbackEmitTests");

        Assert.IsTrue(ThreatDetectionWarningSystem.TryEmitThreatWarningAudio(
            world.EntityManager,
            ThreatWarningType.Ground,
            etaSeconds: 5f,
            threatCount: 1,
            requestedAt: 1.25f));

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(world.EntityManager);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.AlertThreatMinor, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.AlertThreatMinorHash, requests[0].EventHash);
        Assert.AreEqual("Alerts", requests[0].BusId.ToString());
        Assert.AreEqual(AudioPlaybackPriority.High, requests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, requests[0].Status);
        Assert.That(requests[0].CooldownSeconds, Is.EqualTo(3f).Within(0.001f));
        Assert.That(requests[0].RequestedAt, Is.EqualTo(1.25f).Within(0.001f));
    }

    [Test]
    public void ThreatDetectionWarningSystem_NewCloseAirThreatEnqueuesCriticalAlertAudio()
    {
        using World world = new("AlertAudioFeedbackThreatDetectionTests");
        EntityManager em = world.EntityManager;

        CreateUnit(em, FactionIdentity.PlayerFactionId, new int2(20, 20), air: false, health: 100);
        CreateUnit(em, FactionIdentity.EnemyFactionId, new int2(30, 20), air: true, health: 100);

        SystemHandle system = world.CreateSystem<ThreatDetectionWarningSystem>();
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            ThreatWarningRuntimeState.Reset();

            world.SetTime(new TimeData(0.1d, 0.1f));
            system.Update(world.Unmanaged);

            Assert.IsTrue(ThreatWarningRuntimeState.HasPendingWarning);
            Assert.AreEqual(ThreatWarningType.Air, ThreatWarningRuntimeState.PendingType);

            DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(em);
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(AudioEventIds.AlertThreatCritical, requests[0].EventId.ToString());
            Assert.AreEqual(AudioEventIds.AlertThreatCriticalHash, requests[0].EventHash);
            Assert.AreEqual("Alerts", requests[0].BusId.ToString());
            Assert.AreEqual(AudioPlaybackPriority.Critical, requests[0].Priority);
            Assert.That(requests[0].CooldownSeconds, Is.EqualTo(4f).Within(0.001f));
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            ThreatWarningRuntimeState.Reset();
        }
    }

    private static void AssertThreatAudio(
        ThreatWarningType warningType,
        float etaSeconds,
        int threatCount,
        string expectedEventId,
        uint expectedEventHash,
        AudioPlaybackPriority expectedPriority,
        float expectedCooldownSeconds)
    {
        Assert.IsTrue(ThreatDetectionWarningSystem.TryResolveThreatWarningAudioEvent(
            warningType,
            etaSeconds,
            threatCount,
            out string eventId,
            out uint eventHash,
            out AudioPlaybackPriority priority,
            out float cooldownSeconds));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
        Assert.AreEqual(expectedPriority, priority);
        Assert.That(cooldownSeconds, Is.EqualTo(expectedCooldownSeconds).Within(0.001f));
    }

    private static Entity CreateUnit(EntityManager em, byte factionId, int2 cell, bool air, int health)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new Faction { Id = factionId });
        em.AddComponentData(entity, new UnitGrid { Cell = cell });
        em.AddComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.AddComponentData(entity, new UnitMovementBehavior
        {
            AllowIdleWander = 0,
            UsesVehicleMotion = 0
        });
        if (air)
        {
            em.AddComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 6f,
                RunwayTaxiSpeed = 5f
            });
        }

        return entity;
    }

    private static DynamicBuffer<AudioPlaybackRequestElement> GetAudioRequests(EntityManager em)
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        return em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
    }
}
