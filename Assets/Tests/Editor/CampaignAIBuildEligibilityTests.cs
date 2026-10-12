using NUnit.Framework;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class CampaignAIBuildEligibilityTests
{
    [Test]
    public void CatalogFilterPreservesAuthoredRequestsAndSkipsIllegalTent()
    {
        using var world=new World("Campaign AI eligibility");var em=world.EntityManager;
        using var allowed=new NativeList<FixedString128Bytes>(Allocator.Temp);
        Assert.IsFalse(CampaignAIBuildEligibility.ReadAllowed(em,allowed));
        var root=em.CreateEntity(typeof(CampaignMissionRuntimeComponent));
        var runtime=new CampaignMissionRuntimeComponent{MissionId="test.campaign",Phase=MissionPhaseKind.Engage,SourceVersion=7};em.SetComponentData(root,runtime);
        Assert.IsTrue(CampaignAIBuildEligibility.ReadAllowed(em,allowed));Assert.AreEqual(0,allowed.Length);
        using var builder=new BlobBuilder(Allocator.Temp);ref var catalog=ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        var missions=builder.Allocate(ref catalog.Missions,1);missions[0].MissionId=runtime.MissionId;missions[0].MissionRuntimeEnabled=1;
        missions[0].BuildZone=new CampaignMissionBuildZoneBlob{AnchorId="build",HalfWidthCells=20,HalfHeightCells=20};
        var build=builder.Allocate(ref missions[0].BuildCatalog,2);
        build[0]=new CampaignMissionBuildEntryBlob{BuildingConfigId="Building_Barrack",MaxCount=1};
        build[1]=new CampaignMissionBuildEntryBlob{BuildingConfigId="Tent_Regular",MaxCount=0};
        using var blob=builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        em.AddComponentData(root,new CampaignMissionCatalogComponent{Blob=blob,SourceVersion=6});
        Assert.IsTrue(CampaignAIBuildEligibility.ReadAllowed(em,allowed));Assert.AreEqual(0,allowed.Length,"Stale catalog must not authorize generic AI construction");
        em.SetComponentData(root,new CampaignMissionCatalogComponent{Blob=blob,SourceVersion=7});
        var boundary=em.CreateEntity(typeof(AIBuildPlanEntry),typeof(BuildingConfiguredSpawnableReadModel),typeof(BuildingRuntimeOwnedBuildingSummary),typeof(BuildingRuntimeSpawnRequest));
        var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);requests.Add(new BuildingRuntimeSpawnRequest{BuildingId="required_clinic",AuthoredStartingGrant=1,RequirePreferredOrigin=1,Status=BuildingRuntimeSpawnRequest.Pending});
        Assert.IsTrue(CampaignAIBuildEligibility.ReadAllowed(em,allowed));Assert.AreEqual(1,allowed.Length);Assert.AreEqual("building_barrack",allowed[0].ToString());
        Assert.AreEqual(1,requests.Length);Assert.AreEqual(1,requests[0].AuthoredStartingGrant);Assert.AreEqual(BuildingRuntimeSpawnRequest.Pending,requests[0].Status);
        var entries=em.GetBuffer<AIBuildPlanEntry>(boundary);entries.Add(new AIBuildPlanEntry{BuildingId="Tent_Regular"});entries.Add(new AIBuildPlanEntry{BuildingId="Building_Barrack"});
        var spawnables=em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        spawnables.Add(new BuildingConfiguredSpawnableReadModel{BuildingId="tent_regular",CanRequest=1,MaterialsCost=10});spawnables.Add(new BuildingConfiguredSpawnableReadModel{BuildingId="building_barrack",CanRequest=1,MaterialsCost=30});
        var economy=new FactionEconomy{FactionId=2,Money=100};var materials=new FactionTacticalMaterialsComponent{FactionId=2,Current=100,Capacity=100};var plan=new AIBuildPlan{FactionId=2,BaseCenterCell=new int2(20,30)};
        var decision=AIBuildPlanningPolicySystemHelper.SelectBuildDecision(entries,spawnables,em.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundary),requests,plan,economy,materials,allowed.AsArray(),true);
        Assert.AreEqual(AIBuildPlannerSystem.BuildDecisionResult.Request,decision.Result);Assert.AreEqual("building_barrack",decision.BuildingId.ToString());Assert.AreEqual(1,decision.EntryIndex);Assert.AreEqual(30,decision.MaterialsCost);
        Assert.AreEqual(100,materials.Current);Assert.AreEqual(1,requests.Length,"Eligibility must not enqueue or discard authored grants");
        allowed.Clear();decision=AIBuildPlanningPolicySystemHelper.SelectBuildDecision(entries,spawnables,em.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundary),requests,plan,economy,materials,allowed.AsArray(),true);
        Assert.AreEqual(AIBuildPlannerSystem.BuildDecisionResult.None,decision.Result);
        runtime.Phase=MissionPhaseKind.None;em.SetComponentData(root,runtime);Assert.IsFalse(CampaignAIBuildEligibility.ReadAllowed(em,allowed));
    }
    public static void RunFocusedValidation()
    {
        new CampaignAIBuildEligibilityTests().CatalogFilterPreservesAuthoredRequestsAndSkipsIllegalTent();
        AIBuildPlannerValidationTests.RunFocusedValidation();
        Debug.Log("[CampaignAIBuildEligibility] result=Passed requiredGrants=preserved staleCatalog=rejected illegalTent=skipped genericAIRegression=Passed");
    }
}
