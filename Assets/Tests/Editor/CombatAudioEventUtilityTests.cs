using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class CombatAudioEventUtilityTests
{
    private World _world;
    private EntityManager _entityManager;

    [SetUp]
    public void SetUp()
    {
        _world = new World("CombatAudioEventUtilityTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void EmitStandardWeaponFire_InfantryEnqueuesSpatialRifle()
    {
        Entity infantry = _entityManager.CreateEntity();
        float3 position = new(2f, 0f, 4f);

        Assert.IsTrue(CombatAudioEventUtility.EmitStandardWeaponFire(_entityManager, infantry, position, 3f));

        AudioPlaybackRequestElement request = LastRequest();
        AssertRequest(request, AudioEventIds.GameplayWeaponRifleFire, AudioEventIds.GameplayWeaponRifleFireHash, AudioPlaybackPriority.Medium, infantry, position);
    }

    [Test]
    public void EmitStandardWeaponFire_VehicleEnqueuesSpatialVehicleCannon()
    {
        Entity vehicle = _entityManager.CreateEntity(typeof(UnitVehicleMovement));
        float3 position = new(6f, 0f, 8f);

        Assert.IsTrue(CombatAudioEventUtility.EmitStandardWeaponFire(_entityManager, vehicle, position, 4f));

        AudioPlaybackRequestElement request = LastRequest();
        AssertRequest(request, AudioEventIds.GameplayWeaponVehicleCannonFire, AudioEventIds.GameplayWeaponVehicleCannonFireHash, AudioPlaybackPriority.High, vehicle, position);
    }

    [Test]
    public void EmitStandardWeaponFire_AircraftEnqueuesFlybyAndMissileLaunch()
    {
        Entity aircraft = _entityManager.CreateEntity(typeof(UnitAirComponent));
        float3 position = new(10f, 24f, 12f);

        Assert.IsTrue(CombatAudioEventUtility.EmitStandardWeaponFire(_entityManager, aircraft, position, 5f));

        DynamicBuffer<AudioPlaybackRequestElement> requests = Requests();
        Assert.AreEqual(2, requests.Length);
        AssertRequest(requests[0], AudioEventIds.GameplayUnitAircraftFlyby, AudioEventIds.GameplayUnitAircraftFlybyHash, AudioPlaybackPriority.High, aircraft, position);
        AssertRequest(requests[1], AudioEventIds.GameplayWeaponMissileLaunch, AudioEventIds.GameplayWeaponMissileLaunchHash, AudioPlaybackPriority.High, aircraft, position);
    }

    [Test]
    public void EmitAirMissileLaunch_EnqueuesFlybyAndAirMissileLaunch()
    {
        Entity aircraft = _entityManager.CreateEntity(typeof(UnitAirComponent));
        float3 position = new(11f, 28f, 13f);

        Assert.IsTrue(CombatAudioEventUtility.EmitAirMissileLaunch(_entityManager, aircraft, position, 6f));

        DynamicBuffer<AudioPlaybackRequestElement> requests = Requests();
        Assert.AreEqual(2, requests.Length);
        AssertRequest(requests[0], AudioEventIds.GameplayUnitAircraftFlyby, AudioEventIds.GameplayUnitAircraftFlybyHash, AudioPlaybackPriority.High, aircraft, position);
        AssertRequest(requests[1], AudioEventIds.GameplayWeaponAirMissileLaunch, AudioEventIds.GameplayWeaponAirMissileLaunchHash, AudioPlaybackPriority.High, aircraft, position);
    }

    [Test]
    public void EmitImpactAndExplosionEvents_EnqueueExpectedSpatialEvents()
    {
        Entity source = _entityManager.CreateEntity();
        float3 position = new(14f, 0f, 15f);

        Assert.IsTrue(CombatAudioEventUtility.EmitBulletImpact(_entityManager, source, position, 7f));
        Assert.IsTrue(CombatAudioEventUtility.EmitSmallExplosion(_entityManager, source, position, 7.2f));
        Assert.IsTrue(CombatAudioEventUtility.EmitLargeExplosion(_entityManager, source, position, 7.4f));
        Assert.IsTrue(CombatAudioEventUtility.EmitVehicleDestroyed(_entityManager, source, position, 7.6f));

        DynamicBuffer<AudioPlaybackRequestElement> requests = Requests();
        Assert.AreEqual(5, requests.Length);
        AssertRequest(requests[0], AudioEventIds.GameplayImpactBullet, AudioEventIds.GameplayImpactBulletHash, AudioPlaybackPriority.Medium, source, position);
        AssertRequest(requests[1], AudioEventIds.GameplayExplosionSmall, AudioEventIds.GameplayExplosionSmallHash, AudioPlaybackPriority.High, source, position);
        AssertRequest(requests[2], AudioEventIds.GameplayExplosionLarge, AudioEventIds.GameplayExplosionLargeHash, AudioPlaybackPriority.Critical, source, position);
        AssertRequest(requests[3], AudioEventIds.GameplayUnitVehicleDestroyed, AudioEventIds.GameplayUnitVehicleDestroyedHash, AudioPlaybackPriority.High, source, position);
        AssertRequest(requests[4], AudioEventIds.GameplayExplosionLarge, AudioEventIds.GameplayExplosionLargeHash, AudioPlaybackPriority.Critical, source, position);
    }

    private DynamicBuffer<AudioPlaybackRequestElement> Requests()
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        return _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
    }

    private AudioPlaybackRequestElement LastRequest()
    {
        DynamicBuffer<AudioPlaybackRequestElement> requests = Requests();
        Assert.Greater(requests.Length, 0);
        return requests[requests.Length - 1];
    }

    private static void AssertRequest(
        AudioPlaybackRequestElement request,
        string eventId,
        uint eventHash,
        AudioPlaybackPriority priority,
        Entity source,
        float3 position)
    {
        Assert.AreEqual(eventId, request.EventId.ToString());
        Assert.AreEqual(eventHash, request.EventHash);
        Assert.AreEqual(new FixedString32Bytes("SFX"), request.BusId);
        Assert.AreEqual(priority, request.Priority);
        Assert.AreEqual(source, request.SourceEntity);
        Assert.AreEqual(1, request.Spatial);
        Assert.AreEqual(1, request.HasWorldPosition);
        Assert.AreEqual(position, request.WorldPosition);
    }
}
