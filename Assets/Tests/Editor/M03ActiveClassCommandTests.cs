using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class M03ActiveClassCommandTests
{
    [Test] public void GroupHoldAndStopValidateEveryActorAndRepeatedHoldPersists()
    {
        using var world=new World("M3 grouped immediate commands");
        var em=world.EntityManager;
        var runtime=em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtime,new RuntimeGameplayStateComponent {PlayRequested=1,SimulationActive=1,SelectionModeActive=1});
        var queue=em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(queue);
        Entity rifle=Actor(em,1,100),sensor=Actor(em,1,100),enemy=Actor(em,2,100),civilian=Actor(em,0,100),dead=Actor(em,1,0),passenger=Actor(em,1,100);
        em.AddComponent<UnitTransportPassenger>(passenger);
        em.SetComponentData(rifle,new UnitCombat {CanAttack=1});
        foreach(var kind in new[]{RtsSelectionCommandIntentKind.HoldPosition,RtsSelectionCommandIntentKind.HoldPosition,RtsSelectionCommandIntentKind.Stop})
        {
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(queue).Add(new RtsSelectionCommandIntentRequestElement {Kind=kind});
            Assert.IsTrue(RtsSelectionImmediateSelectedUnitCommandSystem.ProcessPendingRequests(em,Entity.Null,out var processed,out bool accepted,out _,out int count));
            Assert.IsTrue(accepted); Assert.AreEqual(kind,processed); Assert.AreEqual(2,count);
            foreach(var actor in new[]{rifle,sensor})
            {
                Assert.IsFalse(em.HasComponent<UnitPathFollow>(actor));
                Assert.AreEqual(kind==RtsSelectionCommandIntentKind.HoldPosition,em.HasComponent<HoldPositionOrderTag>(actor));
            }
            Assert.AreEqual(kind==RtsSelectionCommandIntentKind.HoldPosition ? 1 : 0,em.GetComponentData<UnitCombat>(rifle).AutoEngage);
            Assert.AreEqual(0,em.GetComponentData<UnitCombat>(sensor).CanAttack);
            foreach(var actor in new[]{enemy,civilian,dead,passenger})
            {Assert.IsTrue(em.HasComponent<UnitPathFollow>(actor)); Assert.IsFalse(em.HasComponent<HoldPositionOrderTag>(actor));}
        }
    }
    private static Entity Actor(EntityManager em,byte faction,int health)
    {
        var actor=em.CreateEntity(typeof(SelectedUnitTag),typeof(Faction),typeof(UnitHealth),typeof(UnitGrid),typeof(UnitMove),typeof(UnitCombat),typeof(UnitPathFollow));
        em.SetComponentData(actor,new Faction {Id=faction}); em.SetComponentData(actor,new UnitHealth {Current=health,Max=100});
        return actor;
    }
    public static void RunFocusedValidation()
    {
        try {new M03ActiveClassCommandTests().GroupHoldAndStopValidateEveryActorAndRepeatedHoldPersists();
            Debug.Log("[M03ActiveClassCommands] result=Passed groupHoldStop=true repeatedHold=true foreignDeadPassengerRejected=true"); ValidationExit.Passed();}
        catch(Exception error) {Debug.LogException(error); Debug.LogError("[M03ActiveClassCommands] result=Failed"); ValidationExit.Failed();}
    }
}
