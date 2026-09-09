using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class M03RadarPingTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests=new M03RadarPingTests();
            tests.PingUsesDetectorJobAndFiltersFutureInfantryAirAndNeutralTargets();
            tests.DuplicateCooldownPauseAndExhaustionDoNotSpend();
            tests.DestroyedAirOnlyAndStaleAttemptSensorsDoNotSpend();
            tests.SensorDeathAfterReservationDoesNotSpend();
            AISquadValidationTests.RunFocusedValidation();
            AICombatOrderValidationTests.RunFocusedValidation();
            new VehicleVisualAdornmentsSystemTests().VehicleDestroyedVisualSystemSpawnsDestroyedVisualAndCleansRuntimeAdornments();
            Debug.Log("[M03RadarPingValidation] result=Passed pingTests=4 aiTests=4 linkedWreckRegression=1");
            ValidationExit.Passed();
        }
        catch(Exception exception) { Debug.LogException(exception); Debug.LogError("[M03RadarPingValidation] result=Failed"); ValidationExit.Failed(); }
    }

    [Test]
    public void PingUsesDetectorJobAndFiltersFutureInfantryAirAndNeutralTargets()
    {
        using var f=new Fixture();
        Entity target=f.Unit(2,new int2(75,50),true);
        f.Em.AddComponentData(target,new UnitTarget {Cell=new int2(100,50)}); // Moving away: passive scan does not disclose it.
        Entity future=f.Unit(2,new int2(70,50),true); f.Em.AddComponent<CampaignMissionCombatSuppressedTag>(future);
        Entity infantry=f.Unit(2,new int2(70,50),false);
        Entity neutral=f.Unit(0,new int2(70,50),true);
        Entity air=f.Unit(2,new int2(70,50),true); f.Em.AddComponent<UnitAirMovement>(air);
        f.AddHostile(target); f.AddHostile(future); f.AddHostile(infantry); f.AddHostile(air);
        f.Scan.Update(f.World.Unmanaged);
        Assert.IsFalse(f.Em.HasComponent<ScanIntelLastSeen>(target));
        f.Request(1); f.Update();
        var ping=f.Ping;
        Assert.AreEqual(RadarPingResultKind.Accepted,ping.Result);
        Assert.AreEqual(1,ping.Charges); Assert.AreEqual(70000,ping.ReadyAtMilliseconds);
        Assert.AreEqual(1,ping.LastContactCount);
        Assert.IsTrue(f.Em.HasComponent<ScanIntelLastSeen>(target));
        foreach(var excluded in new[]{future,infantry,neutral,air}) Assert.IsFalse(f.Em.HasComponent<ScanIntelLastSeen>(excluded));
        var observations=f.Em.GetBuffer<ThreatWarningObservation>(f.Root);
        Assert.AreEqual(1,observations.Length); Assert.AreEqual(ThreatWarningSourceKind.RadarPing,observations[0].Source);
        Assert.AreEqual(1,observations[0].KnownVehicleCount);
    }

    [Test]
    public void DuplicateCooldownPauseAndExhaustionDoNotSpend()
    {
        using var f=new Fixture();
        f.Request(1); f.Request(1); f.Update();
        Assert.AreEqual(1,f.Ping.Charges); Assert.AreEqual(0,f.Ping.LastContactCount,"An empty valid scan is truthful and consumes one use.");
        f.Request(2); f.Update(); Assert.AreEqual(RadarPingResultKind.Cooldown,f.Ping.Result); Assert.AreEqual(1,f.Ping.Charges);
        f.SetElapsed(70000); RuntimeGameplayStateTestHelper.SetSimulationActive(f.Em,false);
        f.Request(3); f.Update(); Assert.AreEqual(RadarPingResultKind.Paused,f.Ping.Result); Assert.AreEqual(1,f.Ping.Charges);
        RuntimeGameplayStateTestHelper.SetSimulationActive(f.Em,true);
        f.Request(4); f.Update(); Assert.AreEqual(0,f.Ping.Charges);
        f.SetElapsed(140000); f.Request(5); f.Update(); Assert.AreEqual(RadarPingResultKind.NoCharges,f.Ping.Result);
    }

    [Test]
    public void DestroyedAirOnlyAndStaleAttemptSensorsDoNotSpend()
    {
        using var f=new Fixture();
        f.Em.SetComponentData(f.Sensor,new ThreatDetector {Kind=(byte)ThreatDetectionKind.Air,RadiusCells=40});
        f.Request(1); f.Update(); Assert.AreEqual(RadarPingResultKind.NoSensor,f.Ping.Result); Assert.AreEqual(2,f.Ping.Charges);
        f.Em.SetComponentData(f.Sensor,new ThreatDetector {Kind=(byte)ThreatDetectionKind.Ground,RadiusCells=40});
        f.Em.SetComponentData(f.Sensor,new UnitHealth {Current=0,Max=100});
        f.Request(2); f.Update(); Assert.AreEqual(RadarPingResultKind.NoSensor,f.Ping.Result); Assert.AreEqual(2,f.Ping.Charges);
        f.Em.GetBuffer<RadarPingRequest>(f.Root).Add(new RadarPingRequest {SessionToken="old-attempt",AttemptOrdinal=99,SourceVersion=1,RequestId=999});
        f.Update(); Assert.AreEqual(2,f.Ping.Charges); Assert.AreEqual(2,f.Ping.LastRequestId);
    }

    [Test]
    public void SensorDeathAfterReservationDoesNotSpend()
    {
        using var f=new Fixture(); f.Request(1); f.RequestSystem.Update(f.World.Unmanaged);
        Assert.AreEqual(RadarPingResultKind.Pending,f.Ping.Result);
        f.Em.SetComponentData(f.Sensor,new UnitHealth {Current=0,Max=100});
        f.Scan.Update(f.World.Unmanaged);
        Assert.AreEqual(RadarPingResultKind.NoSensor,f.Ping.Result); Assert.AreEqual(2,f.Ping.Charges);
        Assert.AreEqual(0,f.Ping.PendingRequestId);
    }

    private sealed class Fixture:IDisposable
    {
        public readonly World World=new("M03 Radar Ping test");
        public EntityManager Em=>World.EntityManager;
        public readonly Entity Root,Sensor;
        public readonly SystemHandle RequestSystem,Scan;
        public RadarPingState Ping=>Em.GetComponentData<RadarPingState>(Root);
        public Fixture()
        {
            Root=Em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent),
                typeof(CampaignMissionAttemptFactsComponent),typeof(CampaignMissionDefenseStateComponent),typeof(RadarPingState),typeof(ThreatWarningLedgerState));
            Em.SetComponentData(Root,new CampaignMissionRuntimeComponent {Version=1,SourceVersion=1,SessionToken="attempt",AttemptOrdinal=1,Phase=MissionPhaseKind.Engage});
            Em.SetComponentData(Root,new RadarPingState {SourceVersion=1,SessionToken="attempt",AttemptOrdinal=1,Charges=2,CooldownMilliseconds=60000});
            Em.SetComponentData(Root,new ThreatWarningLedgerState {SourceVersion=1,SessionToken="attempt",AttemptOrdinal=1});
            Em.AddBuffer<RadarPingRequest>(Root); Em.AddBuffer<ThreatWarningObservation>(Root); Em.AddBuffer<ThreatWarningRecord>(Root);
            Em.AddBuffer<CampaignMissionConvoyElementState>(Root).Add(default);
            Em.AddBuffer<CampaignMissionDefenseMember>(Root);
            Sensor=Unit(1,new int2(50,50),true);
            Em.AddComponentData(Sensor,new ThreatDetector {Kind=(byte)ThreatDetectionKind.Ground,RadiusCells=40});
            Em.GetBuffer<CampaignMissionDefenseMember>(Root).Add(new CampaignMissionDefenseMember {Entity=Sensor,ElementIndex=-1,FactionId=1,IsSensor=1});
            SetElapsed(10000); RuntimeGameplayStateTestHelper.SetPlayRequested(Em,true);
            RequestSystem=World.CreateSystem<RadarPingRequestSystem>(); Scan=World.CreateSystem<ThreatDetectionWarningSystem>();
        }
        public Entity Unit(byte faction,int2 cell,bool vehicle)
        {
            Entity e=Em.CreateEntity(typeof(Faction),typeof(UnitGrid),typeof(UnitHealth),typeof(UnitMovementBehavior),typeof(LocalTransform));
            Em.SetComponentData(e,new Faction {Id=faction}); Em.SetComponentData(e,new UnitGrid {Cell=cell});
            Em.SetComponentData(e,new UnitHealth {Current=100,Max=100});
            Em.SetComponentData(e,new UnitMovementBehavior {UsesVehicleMotion=vehicle ? (byte)1 : (byte)0});
            Em.SetComponentData(e,LocalTransform.FromPosition(new float3(cell.x,0,cell.y))); return e;
        }
        public void AddHostile(Entity e)=>Em.GetBuffer<CampaignMissionDefenseMember>(Root).Add(new CampaignMissionDefenseMember {Entity=e,ElementIndex=0,FactionId=2});
        public void Request(uint id)=>Em.GetBuffer<RadarPingRequest>(Root).Add(new RadarPingRequest {SourceVersion=1,SessionToken="attempt",AttemptOrdinal=1,RequestId=id});
        public void SetElapsed(int elapsed)=>Em.SetComponentData(Root,new CampaignMissionAttemptFactsComponent {ElapsedMilliseconds=elapsed});
        public void Update() {RequestSystem.Update(World.Unmanaged);Scan.Update(World.Unmanaged);}
        public void Dispose() {World.Dispose();RuntimeGameplayStateTestHelper.SetPlayRequested(false);RuntimeGameplayStateTestHelper.SetSimulationActive(false);}
    }
}
