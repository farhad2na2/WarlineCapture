using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class CampaignOpeningVisualFramingTests
{
    static void Unit(EntityManager em,string role,string group,float3 position)
    {
        var entity=em.CreateEntity(typeof(CampaignMissionUnitRoleComponent),typeof(LocalTransform));
        em.SetComponentData(entity,new CampaignMissionUnitRoleComponent{MissionRoleId=role,UnitGroupId=group});
        em.SetComponentData(entity,LocalTransform.FromPosition(position));
    }
    static void Visible(in CampaignMissionOpeningPresentationComponent opening,float3 point)
    {
        float height=opening.CommandCameraPerspective.x-point.y;
        float pitch=math.radians(opening.CommandCameraPerspective.y),sine=math.sin(pitch),cosine=math.cos(pitch);
        float tangent=math.tan(math.radians(opening.CommandCameraPerspective.w*.5f));
        float aspect=math.clamp((float)Screen.width/math.max(1,Screen.height),1.5f,2.4f);
        float3 camera=opening.CommandCameraFocus+new float3(0,height,-height*cosine/sine);
        float3 delta=point-camera;float depth=-delta.y*sine+delta.z*cosine;
        float2 uv=new float2(.5f+delta.x/(2*depth*tangent*aspect),.5f+(delta.y*cosine+delta.z*sine)/(2*depth*tangent));
        Assert.That(depth,Is.GreaterThan(0));Assert.That(uv.x,Is.InRange(.12f,.69f));Assert.That(uv.y,Is.InRange(.28f,.81f));
    }
    [Test] public void SupplyFramesFirstSquadWithoutChangingGameplayFocus()
    {
        using var world=new World(nameof(SupplyFramesFirstSquadWithoutChangingGameplayFocus));
        Unit(world.EntityManager,"role.friendly.command_squad","group.ch02.m02.squad_a",new float3(585,0,505));
        Unit(world.EntityManager,"role.friendly.command_squad","group.ch02.m02.squad_b",new float3(905,0,540));
        using var builder=new BlobBuilder(Allocator.Temp);ref var map=ref builder.ConstructRoot<OperationMapBlob>();builder.Allocate(ref map.Anchors,0);
        var runtime=new CampaignMissionRuntimeComponent{MissionId="saga.ch02.m02.supply_line"};
        var opening=new CampaignMissionOpeningPresentationComponent{FriendlyFocus=new float3(123,0,456)};
        using var blob=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Temp);
        CampaignMissionSpawnSystem.FrameMissionCommandGroup(world.EntityManager,in runtime,ref blob.Value,ref opening);
        Visible(in opening,new float3(585,0,505));Assert.That(opening.CommandCameraPerspective.x,Is.LessThan(100));
        Assert.AreEqual(new float3(123,0,456),opening.FriendlyFocus,"Presentation must not change troop/order focus.");
    }
    [Test] public void PowerFramesRiflesAndEngineersInsteadOfRemoteConvoy()
    {
        using var world=new World(nameof(PowerFramesRiflesAndEngineersInsteadOfRemoteConvoy));
        Unit(world.EntityManager,"role.power.rifle","squad",new float3(808,0,505));
        Unit(world.EntityManager,"role.power.rifle","squad",new float3(847,0,505));
        Unit(world.EntityManager,"role.power.engineer","engineers",new float3(814,0,503));
        Unit(world.EntityManager,"role.power.family_convoy","families",new float3(555,0,505));
        using var builder=new BlobBuilder(Allocator.Temp);ref var map=ref builder.ConstructRoot<OperationMapBlob>();builder.Allocate(ref map.Anchors,0);
        var runtime=new CampaignMissionRuntimeComponent{MissionId="saga.ch02.m04.power_relay"};var opening=default(CampaignMissionOpeningPresentationComponent);
        using var blob=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Temp);
        CampaignMissionSpawnSystem.FrameMissionCommandGroup(world.EntityManager,in runtime,ref blob.Value,ref opening);
        Visible(in opening,new float3(808,0,505));Visible(in opening,new float3(847,0,505));Visible(in opening,new float3(814,0,503));
    }
    [Test] public void EstablishBaseShowsWholeAuthoredConstructionArea()
    {
        using var world=new World(nameof(EstablishBaseShowsWholeAuthoredConstructionArea));
        Unit(world.EntityManager,"role.friendly.command_squad","squad",new float3(920,0,425));
        using var builder=new BlobBuilder(Allocator.Temp);ref var map=ref builder.ConstructRoot<OperationMapBlob>();var anchors=builder.Allocate(ref map.Anchors,1);
        anchors[0]=new OperationMapAnchorBlob{Id="anchor.ch01.m02.build_lot",Position=new float3(1020,0,324),Radius=20};
        var runtime=new CampaignMissionRuntimeComponent{MissionId="saga.ch01.m02.establish_base"};var opening=default(CampaignMissionOpeningPresentationComponent);
        using var blob=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Temp);
        CampaignMissionSpawnSystem.FrameMissionCommandGroup(world.EntityManager,in runtime,ref blob.Value,ref opening);
        foreach(float x in new[]{1000f,1040f})foreach(float z in new[]{304f,344f})Visible(in opening,new float3(x,0,z));
    }
    public static void RunFocusedValidation()
    {
        try
        {
            var tests=new CampaignOpeningVisualFramingTests();tests.SupplyFramesFirstSquadWithoutChangingGameplayFocus();
            tests.PowerFramesRiflesAndEngineersInsteadOfRemoteConvoy();tests.EstablishBaseShowsWholeAuthoredConstructionArea();
            Debug.Log("[CampaignOpeningVisualFraming] result=Passed tests=3 gameplayFocus=preserved");ValidationExit.Passed();
        }
        catch(Exception e){Debug.LogException(e);Debug.LogError("[CampaignOpeningVisualFraming] result=Failed");ValidationExit.Failed();}
    }
}
