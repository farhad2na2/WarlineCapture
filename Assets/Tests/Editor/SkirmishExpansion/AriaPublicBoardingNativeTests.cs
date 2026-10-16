using System;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Tests.Editor
{
    public sealed class AriaPublicBoardingNativeTests
    {
        // Provider-bound disposable components exercise observation invariants;
        // they are not an instantiated prefab or normal mission/source-fit proof.
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
        public void ActualPublicOriginalReadoutRejectsPartialForeignAndBrokenReciprocalEdges(int mutation)
        {
            SkirmishMissionLogicalBindingSubjectTestFixture.WithActiveBreakthrough((em,session)=>
            {
                var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                em.GetComponentObject<SkirmishNativeObjectiveRecord>(session).Breakthrough.ExitOpen=true;
                using var q=em.CreateEntityQuery(typeof(SkirmishObjectiveRoleComponent),typeof(SkirmishAttemptOwnedComponent));
                using var values=q.ToEntityArray(Allocator.Temp);
                var actors=values.ToArray().Where(a=>em.GetComponentData<SkirmishAttemptOwnedComponent>(a).FactionId==1).OrderBy(a=>em.GetComponentData<SkirmishAttemptOwnedComponent>(a).StableObjectId.ToString(),StringComparer.Ordinal).Take(4).ToArray();
                Assert.AreEqual(4,actors.Length);
                uint group=SkirmishArmyGroupSystem.OpenGroup(em,session,1,SkirmishRoleKind.Rifle,SkirmishPopulationCategory.Infantry);
                foreach(var actor in actors){SkirmishArmyGroupSystem.BindMember(em,session,actor,group);em.AddComponent<SelectedUnitTag>(actor);em.AddComponentData(actor,new UnitGrid());}
                var carrier=em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(Faction),typeof(UnitHealth),typeof(LocalTransform),typeof(UnitGrid),typeof(UnitFootprint),
                    typeof(UnitAirComponent),typeof(UnitAirMovement),typeof(UnitTransportCapacity),typeof(SkirmishUnitRoleComponent),typeof(UnitSourcePrefabKey));
                em.SetComponentData(carrier,new SkirmishAttemptOwnedComponent{SessionId=native.SessionId,FactionId=1,StableObjectId="synthetic.carrier"});
                em.SetComponentData(carrier,new Faction{Id=1});em.SetComponentData(carrier,new UnitHealth{Current=100,Max=100});em.SetComponentData(carrier,LocalTransform.Identity);
                em.SetComponentData(carrier,new UnitFootprint{Size=new int2(1,1)});em.SetComponentData(carrier,new UnitTransportCapacity{SoldierCapacity=4});
                em.SetComponentData(carrier,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.TransportHeli,Category=SkirmishPopulationCategory.Air});
                em.SetComponentData(carrier,new UnitSourcePrefabKey{Value=new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.TransportHeli))});
                uint transportGroup=SkirmishArmyGroupSystem.OpenGroup(em,session,1,SkirmishRoleKind.TransportHeli,SkirmishPopulationCategory.Air);
                SkirmishArmyGroupSystem.BindMember(em,session,carrier,transportGroup);em.AddBuffer<UnitTransportPassengerElement>(carrier);
                if(mutation==1)em.RemoveComponent<SelectedUnitTag>(actors[0]);
                if(mutation==2){var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(carrier);own.SessionId="foreign";em.SetComponentData(carrier,own);}
                Assert.AreEqual(mutation!=1&&mutation!=2,SkirmishPublicBoardingPresentation.TryCandidate(em,session,out var candidate));
                if(mutation==1||mutation==2)return;
                Assert.AreEqual(carrier,candidate.Carrier);Assert.AreEqual(4,candidate.Actors.Length);
                var request=new RtsSelectionCommandIntentRequestElement{Kind=RtsSelectionCommandIntentKind.BoardTransport,RequestId=9,Frame=20,HasScreenPosition=1,ScreenPosition=new float2(200,100)};
                SkirmishPublicBoardingPresentation.CaptureBefore(em,request);Assert.IsTrue(em.HasComponent<SkirmishPublicBoardingPresentation>(session));
                foreach(var actor in actors)em.AddComponentData(actor,new UnitTransportBoardingTarget{Transport=carrier});
                SkirmishPublicBoardingPresentation.ObserveResult(em,new RtsSelectionCommandResultElement{Kind=request.Kind,RequestId=9,Frame=20,Accepted=1});
                var row=em.GetComponentObject<SkirmishPublicBoardingPresentation>(session);Assert.IsTrue(row.Accepted);
                Assert.IsTrue(SkirmishPublicBoardingPresentation.TryReadProgress(em,session,row,out int pending,out int mounted));Assert.AreEqual(4,pending);Assert.AreEqual(0,mounted);
                foreach(var actor in actors)
                {em.RemoveComponent<UnitTransportBoardingTarget>(actor);em.AddComponentData(actor,new UnitTransportPassenger{Transport=carrier});em.AddComponent<Disabled>(actor);em.GetBuffer<UnitTransportPassengerElement>(carrier).Add(new UnitTransportPassengerElement{Passenger=actor});}
                if(mutation==3){var role=em.GetComponentData<SkirmishUnitRoleComponent>(actors[0]);role.Category=SkirmishPopulationCategory.Ground;em.SetComponentData(actors[0],role);}
                if(mutation==4)em.GetBuffer<UnitTransportPassengerElement>(carrier).RemoveAt(0);
                Assert.AreEqual(mutation==0,SkirmishPublicBoardingPresentation.TryReadProgress(em,session,row,out pending,out mounted));
                if(mutation==0)
                {
                    Assert.AreEqual(4,mounted);Assert.AreEqual(0,pending);
                    Assert.IsTrue(SkirmishPublicBoardingPresentation.TryReadExistingMounted(em,session,out var fresh));
                    Assert.IsFalse(fresh.Accepted);Assert.IsFalse(fresh.Presented);Assert.AreEqual(0,fresh.RequestId,"Fresh public roster must not fabricate old contact");
                }
            });
        }
    }
}
