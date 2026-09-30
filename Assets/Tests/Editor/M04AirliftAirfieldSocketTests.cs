using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Rendering;
using NUnit.Framework;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public sealed class M04AirliftAirfieldSocketTests
{
    [Test]
    public void ReplacesOnlyDeclaredAircraftAndRestoresOnMissionChange()
    {
        using var world=new World("Airlift socket isolation");var em=world.EntityManager;
        var mission=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));
        em.SetComponentData(mission,new CampaignMissionRuntimeComponent {MissionId="saga.ch01.m04.airlift",Phase=MissionPhaseKind.Engage});
        var socket=em.CreateEntity(typeof(OperationMapMissionPresentationSocket));em.SetComponentData(socket,new OperationMapMissionPresentationSocket {MissionId="saga.ch01.m04.airlift"});
        var aircraft=em.CreateEntity(typeof(MaterialMeshInfo));em.AddBuffer<Child>(socket).Add(new Child {Value=aircraft});
        var neighboringAircraft=em.CreateEntity(typeof(MaterialMeshInfo));
        var system=world.GetOrCreateSystem<CampaignMissionMapPresentationSocketSystem>();system.Update(world.Unmanaged);
        Assert.IsTrue(em.HasComponent<DisableRendering>(aircraft));Assert.IsFalse(em.HasComponent<DisableRendering>(neighboringAircraft));
        em.SetComponentData(mission,new CampaignMissionRuntimeComponent {MissionId="saga.ch04.m01.air_corridor",Phase=MissionPhaseKind.Engage});system.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<DisableRendering>(aircraft));Assert.IsFalse(em.HasComponent<OperationMapMissionPresentationHiddenTag>(aircraft));
        Assert.IsTrue(em.Exists(socket));Assert.IsTrue(em.Exists(neighboringAircraft));
    }
    [Test]
    public void PreservesVisibilityAlreadyOwnedByAnotherSystem()
    {
        using var world=new World("Airlift prior visibility");var em=world.EntityManager;
        var mission=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));em.SetComponentData(mission,new CampaignMissionRuntimeComponent {MissionId="saga.ch01.m04.airlift",Phase=MissionPhaseKind.Engage});
        var socket=em.CreateEntity(typeof(OperationMapMissionPresentationSocket));em.SetComponentData(socket,new OperationMapMissionPresentationSocket {MissionId="saga.ch01.m04.airlift"});
        var renderer=em.CreateEntity(typeof(MaterialMeshInfo),typeof(DisableRendering));em.AddBuffer<Child>(socket).Add(new Child {Value=renderer});
        var system=world.GetOrCreateSystem<CampaignMissionMapPresentationSocketSystem>();system.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<OperationMapMissionPresentationHiddenTag>(renderer));
        em.SetComponentData(mission,new CampaignMissionRuntimeComponent {Phase=MissionPhaseKind.None});system.Update(world.Unmanaged);
        Assert.IsTrue(em.HasComponent<DisableRendering>(renderer));
    }
    public static void RunFocusedValidation()
    {
        try
        {
            var rules=new M04AirliftRuleTests();rules.FullManifestAndGroundLegAreRequired();rules.FailureWinsOverSimultaneousDeparture();rules.OpeningMustFinishBeforeMissionStarts();rules.VictoryWaitsForFinale();rules.EscortLossIsABonusCriterion();
            var sockets=new M04AirliftAirfieldSocketTests();sockets.ReplacesOnlyDeclaredAircraftAndRestoresOnMissionChange();sockets.PreservesVisibilityAlreadyOwnedByAnotherSystem();
            MatchHudAssistantUiSystemHelperTests.RunShowMeValidation();
            Debug.Log("[AirliftAirfieldFocused] result=Passed ruleCases=15 socketCases=2 neighboringScenery=Preserved priorVisibility=Preserved");ValidationExit.Passed();
        }
        catch(Exception e){Debug.LogException(e);ValidationExit.Failed();}
    }
}
