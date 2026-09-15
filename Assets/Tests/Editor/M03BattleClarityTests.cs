using System;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class M03BattleClarityTests
{
    [Test]
    public void BattleChangesFromSelectionThroughMoveAndHoldToExplicitWaiting()
    {
        using var f=new Fixture();
        Assert.AreEqual("mission.m03.clarity.select",f.Read().Body.ToString());
        f.Em.AddComponent<SelectedUnitTag>(f.Rifle);
        Assert.AreEqual(AssistantRecommendationKind.Move,f.Read().RecommendationKind);
        Assert.AreEqual("mission.m03.clarity.move",f.Read().Body.ToString());
        f.Em.AddComponent<UnitPathRequest>(f.Rifle);
        Assert.AreEqual("mission.m03.clarity.moving",f.Read().Body.ToString());
        f.Em.RemoveComponent<UnitPathRequest>(f.Rifle);
        f.Em.SetComponentData(f.Rifle,LocalTransform.FromPosition(float3.zero));
        Assert.AreEqual(AssistantRecommendationKind.DefensiveAlert,f.Read().RecommendationKind);
        f.Em.AddComponent<HoldPositionOrderTag>(f.Rifle);
        var ready=f.Read();
        Assert.AreEqual("mission.m03.clarity.wait",ready.Body.ToString());
        Assert.AreEqual(0,ready.CanExecute,"Waiting must never require a Continue click.");
        f.Em.RemoveComponent<HoldPositionOrderTag>(f.Rifle);
        Assert.AreEqual("mission.m03.clarity.hold",f.Read().Body.ToString(),"Wrong Stop action must recover to Hold.");
    }
    [Test]
    public void OnlyLiveConfirmedContactCanBecomeBattleTarget()
    {
        using var f=new Fixture();
        f.Em.AddComponent<SelectedUnitTag>(f.Rifle);f.Em.AddComponent<HoldPositionOrderTag>(f.Rifle);
        f.Em.SetComponentData(f.Rifle,LocalTransform.FromPosition(float3.zero));
        var enemy=f.Em.CreateEntity(typeof(UnitHealth),typeof(LocalTransform));
        f.Em.SetComponentData(enemy,new UnitHealth {Current=100});
        f.Em.SetComponentData(enemy,LocalTransform.FromPosition(new float3(12,0,0)));
        var warning=new ThreatWarningRecord {ObservedTarget=enemy,Source=ThreatWarningSourceKind.ScoutReport};
        var warnings=f.Em.GetBuffer<ThreatWarningRecord>(f.Root);warnings.Add(warning);
        Assert.AreEqual(Entity.Null,f.Read().TargetEntity,"Scout report cannot reveal an unconfirmed actor.");
        warning.Source=ThreatWarningSourceKind.VisualContact;warnings[0]=warning;
        Assert.AreEqual(enemy,f.Read().TargetEntity);
        Assert.AreEqual(float3.zero,f.Read().WorldPosition,"Confirmed contacts must not move the defended position.");
        Assert.AreEqual("mission.m03.clarity.engage",f.Read().Body.ToString());
        warning.Stale=1;warnings[0]=warning;
        Assert.AreEqual(Entity.Null,f.Read().TargetEntity);
        warning.Stale=0;warnings[0]=warning;
        f.Em.SetComponentData(enemy,new UnitHealth {Current=0});
        Assert.AreEqual(Entity.Null,f.Read().TargetEntity);
    }
    [Test]
    public void RadarSelectionCannotReplaceRifleGuidanceAndAllNewCopyIsBilingual()
    {
        using var f=new Fixture();
        var sensor=f.Em.CreateEntity(typeof(LocalTransform),typeof(UnitHealth),typeof(SelectedUnitTag));
        f.Em.SetComponentData(sensor,new UnitHealth {Current=100});
        f.Em.GetBuffer<CampaignMissionDefenseMember>(f.Root).Add(new CampaignMissionDefenseMember {Entity=sensor,FactionId=1,IsSensor=1});
        Assert.AreEqual(f.Rifle,f.Read().SourceEntity);
        foreach(var entry in M03RadarWarningUiCopyCatalog.Entries)
            if(entry.Key.StartsWith("mission.m03.clarity.") || entry.Key.Contains("recharging_label"))
            {Assert.IsNotEmpty(entry.English);Assert.IsNotEmpty(entry.Persian);Assert.AreNotEqual(entry.English,entry.Persian);}
    }
    public static void Run()
    {
        try
        {
            var tests=new M03BattleClarityTests();
            tests.BattleChangesFromSelectionThroughMoveAndHoldToExplicitWaiting();
            tests.OnlyLiveConfirmedContactCanBecomeBattleTarget();
            tests.RadarSelectionCannotReplaceRifleGuidanceAndAllNewCopyIsBilingual();
            using(ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();SelectionUiReadModelLookupTests.RunFocusedValidation();
                if(ValidationExit.LastExitCode!=0) throw new Exception("Selection read model regression failed.");
                ValidationExit.ClearLastExitCode();MissionReadinessArchitectureValidation.Run();
                if(ValidationExit.LastExitCode!=0) throw new Exception("Architecture regression failed.");
            }
            Debug.Log("[M03BattleClarityTests] result=Passed tests=3 plus architecture");ValidationExit.Passed();
        }
        catch(Exception error) {Debug.LogException(error);Debug.LogError("[M03BattleClarityTests] result=Failed");ValidationExit.Failed();}
    }
    private sealed class Fixture : IDisposable
    {
        public readonly World World=new("Battle clarity");
        public EntityManager Em=>World.EntityManager;
        public readonly Entity Root,Rifle;
        public Fixture()
        {
            Root=Em.CreateEntity();Em.AddBuffer<CampaignMissionDefenseMember>(Root);Em.AddBuffer<ThreatWarningRecord>(Root);
            Rifle=Em.CreateEntity(typeof(LocalTransform),typeof(UnitHealth),typeof(UnitCombat),typeof(UnitAttack));
            Em.SetComponentData(Rifle,LocalTransform.FromPosition(new float3(200,0,0)));
            Em.SetComponentData(Rifle,new UnitHealth {Current=100});Em.SetComponentData(Rifle,new UnitCombat {CanAttack=1});
            Em.SetComponentData(Rifle,new UnitAttack {Range=40});
            Em.GetBuffer<CampaignMissionDefenseMember>(Root).Add(new CampaignMissionDefenseMember {Entity=Rifle,FactionId=1});
        }
        public CampaignMissionGuidanceProjectionComponent Read()
        {
            object[] args={Em,Root,9,float3.zero,new CampaignMissionGuidanceProjectionComponent {SourceEntity=Rifle}};
            typeof(CampaignMissionGuidanceProjectionSystem).GetMethod("ApplyDefenseBattleGuidance",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
            return (CampaignMissionGuidanceProjectionComponent)args[4];
        }
        public void Dispose()=>World.Dispose();
    }
}
