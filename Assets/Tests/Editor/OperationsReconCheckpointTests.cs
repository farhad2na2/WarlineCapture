using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class OperationsReconCheckpointTests
{
    [Test]
    public void RoundTripRemapsCarrierAndChannel_PreservesCasualtyOrdersClocksAndRandomState()
    {
        using var source = new World("checkpoint source");
        var em = source.EntityManager;
        var root = Create(em, false);
        var records = em.GetBuffer<OperationsReconSpawnRecord>(root);
        var carrier = records[0].Unit;
        var casualty = records[1].Unit;
        em.SetComponentData(carrier, new UnitHealth { Current = 63, Max = 100 });
        em.AddComponentData(carrier, new UnitTarget { Cell = new int2(45, 68) });
        em.AddComponentData(carrier, new UnitIdleWanderComponent { RandomState = 0x12345678, RetrySeconds = 3 });
        em.AddComponentData(carrier, new UnitAttackCooldownComponent { CooldownRemaining = .75f });
        em.SetComponentData(root, new OperationsReconEvidenceComponent { Carrier = carrier, Recovered = 1, Position = new float3(3,0,4) });
        var site = em.GetBuffer<OperationsReconSiteElement>(root)[0]; site.Actor = carrier; site.ChannelSeconds = 7.5f;
        var sourceSites = em.GetBuffer<OperationsReconSiteElement>(root);
        sourceSites[0] = site;
        em.DestroyEntity(casualty);
        string json = OperationsReconCheckpointCodec.Encode(OperationsReconCheckpointCodec.Capture(em, root, "content-v1"));
        Assert.That(OperationsReconCheckpointCodec.TryDecode(json, "session.operations.checkpoint", "content-v1", out var image), Is.True);
        using var destination = new World("checkpoint destination");
        var target = destination.EntityManager;
        var restoredRoot = Create(target, true);
        var restoredRecords = target.GetBuffer<OperationsReconSpawnRecord>(restoredRoot);
        var restoredCarrier = restoredRecords[0].Unit;
        var restoredCasualty = restoredRecords[1].Unit;
        OperationsReconCheckpointCodec.Apply(target, restoredRoot, image);
        Assert.That(restoredCarrier, Is.Not.EqualTo(carrier), "Fixture must require remapping entity handles.");
        Assert.That(target.GetComponentData<OperationsReconEvidenceComponent>(restoredRoot).Carrier, Is.EqualTo(restoredCarrier));
        Assert.That(target.GetBuffer<OperationsReconSiteElement>(restoredRoot)[0].Actor, Is.EqualTo(restoredCarrier));
        Assert.That(target.GetBuffer<OperationsReconSiteElement>(restoredRoot)[0].ChannelSeconds, Is.EqualTo(7.5f));
        Assert.That(target.Exists(restoredCasualty), Is.False);
        Assert.That(target.GetComponentData<UnitHealth>(restoredCarrier).Current, Is.EqualTo(63));
        Assert.That(target.GetComponentData<UnitIdleWanderComponent>(restoredCarrier).RandomState, Is.EqualTo(0x12345678));
        Assert.That(target.GetComponentData<UnitAttackCooldownComponent>(restoredCarrier).CooldownRemaining, Is.EqualTo(.75f));
        Assert.That(target.GetComponentData<UnitPathRequest>(restoredCarrier).Goal, Is.EqualTo(new int2(45,68)));
        Assert.That(target.GetComponentData<OperationsReconMissionComponent>(restoredRoot).ElapsedSeconds, Is.EqualTo(18));
    }

    [Test]
    public void MissingLastDestinationActorRejectsBeforeChangingEarlierActors()
    {
        using var source = new World("checkpoint source");
        var sourceRoot = Create(source.EntityManager, false);
        var first = source.EntityManager.GetBuffer<OperationsReconSpawnRecord>(sourceRoot)[0].Unit;
        source.EntityManager.SetComponentData(first, new UnitHealth { Current = 7, Max = 100 });
        var image = OperationsReconCheckpointCodec.Capture(source.EntityManager, sourceRoot, "content-v1");
        using var destination = new World("checkpoint destination");
        var em = destination.EntityManager;
        var root = Create(em, true);
        var records = em.GetBuffer<OperationsReconSpawnRecord>(root);
        var untouched = records[0].Unit;
        em.DestroyEntity(records[35].Unit);
        Assert.Throws<System.InvalidOperationException>(() => OperationsReconCheckpointCodec.Apply(em, root, image));
        Assert.That(em.GetComponentData<UnitHealth>(untouched).Current, Is.EqualTo(100));
    }

    [Test]
    public void ValidEnvelopeWithInvalidReferenceOrNonFiniteClockIsRejected()
    {
        using var world = new World("checkpoint invalid state");
        var root = Create(world.EntityManager, false);
        var image = OperationsReconCheckpointCodec.Capture(world.EntityManager, root, "content-v1");
        image.carrier = 37;
        Assert.That(OperationsReconCheckpointCodec.TryDecode(OperationsReconCheckpointCodec.Encode(image), image.session, image.content, out _), Is.False);
        image.carrier = 0;
        image.worldTime = double.NaN;
        Assert.That(OperationsReconCheckpointCodec.TryDecode(OperationsReconCheckpointCodec.Encode(image), image.session, image.content, out _), Is.False);
    }

    [Test]
    public void CorruptOrIncompatibleImageIsRejected()
    {
        using var world = new World("checkpoint corruption");
        var root = Create(world.EntityManager, false);
        var image = OperationsReconCheckpointCodec.Capture(world.EntityManager, root, "content-v1");
        string json = OperationsReconCheckpointCodec.Encode(image);
        Assert.That(OperationsReconCheckpointCodec.TryDecode(json, "another-session", "content-v1", out _), Is.False);
        Assert.That(OperationsReconCheckpointCodec.TryDecode(json, image.session, "content-v2", out _), Is.False);
        Assert.That(OperationsReconCheckpointCodec.TryDecode(json.Replace("content-v1", "corrupted"), image.session, "content-v1", out _), Is.False);
        image.schema++;
        Assert.That(OperationsReconCheckpointCodec.TryDecode(OperationsReconCheckpointCodec.Encode(image), image.session, "content-v1", out _), Is.False);
    }

    private static Entity Create(EntityManager em, bool pad)
    {
        if (pad) { em.CreateEntity(); em.CreateEntity(); em.CreateEntity(); }
        var root = em.CreateEntity(typeof(OperationsReconMissionComponent), typeof(OperationsReconEvidenceComponent), typeof(OperationsReconWaveComponent));
        em.SetComponentData(root, new OperationsReconMissionComponent { SessionId = "session.operations.checkpoint", Phase = OperationsReconPhase.Playing, ElapsedSeconds = 18, DeadlineSeconds = 720, ScanSeconds = 15, EvidenceSeconds = 15 });
        var sites = em.AddBuffer<OperationsReconSiteElement>(root);
        for (int i = 0; i < 3; i++) sites.Add(new OperationsReconSiteElement { RoleId = new FixedString64Bytes("signal" + i), Radius = 8 });
        em.AddBuffer<OperationsReconActionElement>(root);
        em.AddBuffer<OperationsReconSpawnRecord>(root);
        for (int i = 1; i <= 36; i++)
        {
            var unit = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform), typeof(OperationsReconMemberComponent));
            em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(unit, LocalTransform.FromPosition(new float3(i,0,1)));
            em.SetComponentData(unit, new OperationsReconMemberComponent { Session = root, StableIndex = i });
            em.GetBuffer<OperationsReconSpawnRecord>(root).Add(new OperationsReconSpawnRecord { Unit = unit, StableIndex = i });
        }
        return root;
    }
}
