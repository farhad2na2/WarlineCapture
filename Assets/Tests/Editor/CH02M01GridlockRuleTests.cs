using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;
using Unity.Entities;

public sealed class CH02M01GridlockRuleTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var t=new CH02M01GridlockRuleTests();
            t.InterruptedWorkResumesWithoutClearingEarly(); t.PauseFreezesHoldAndDepartureResetsIt();
            t.LossPrecedesArrivalAndOneWorkerIsRecoverable(); t.MissingInitializationCannotWin();
            t.AllMandatoryLossesFail(); t.ConnectivityAndArrivalAreIndependent(); t.ReserveEntersAfterWarningWithoutChangingHealth(); t.CrewSelectionUsesMissionIdentity(); t.ChapterOpeningPersistsWithoutGrantingRewards();
            Debug.Log("[GridlockRules] result=Passed cases=9"); ValidationExit.Passed();
        }
        catch(Exception e) {Debug.LogException(e);ValidationExit.Failed();}
    }
    private static CampaignMissionGridlockState Arrived() => new() {
        Ready=1,LivingFadi=1,LivingWorkers=2,LivingRifles=8,LivingVehicle=1,
        RouteConnected=1,VehicleArrived=1,HoldMilliseconds=20000,ElapsedMilliseconds=200000 };
    [Test] public void InterruptedWorkResumesWithoutClearingEarly()
    {
        int work=0;
        Assert.AreEqual(GridlockWorkStatus.Working,CampaignMissionGridlockRuleUtility.AdvanceWork(ref work,25000,10000,true,true,true,false,false,false));
        Assert.AreEqual(10000,work);
        Assert.AreEqual(GridlockWorkStatus.ThreatNearby,CampaignMissionGridlockRuleUtility.AdvanceWork(ref work,25000,20000,true,true,true,true,false,false));
        Assert.AreEqual(10000,work);
        Assert.AreEqual(GridlockWorkStatus.CrewMissing,CampaignMissionGridlockRuleUtility.AdvanceWork(ref work,25000,20000,true,false,true,false,false,false));
        Assert.AreEqual(10000,work);
        Assert.AreEqual(GridlockWorkStatus.Clearing,CampaignMissionGridlockRuleUtility.AdvanceWork(ref work,25000,20000,true,true,true,false,false,false));
        Assert.AreEqual(25000,work);
        Assert.AreEqual(GridlockWorkStatus.Clearing,CampaignMissionGridlockRuleUtility.AdvanceWork(ref work,25000,1,true,true,true,false,true,false));
        Assert.AreEqual(GridlockWorkStatus.Complete,CampaignMissionGridlockRuleUtility.AdvanceWork(ref work,25000,1,true,true,true,false,true,true));
    }
    [Test] public void PauseFreezesHoldAndDepartureResetsIt()
    {
        int hold=10000;
        CampaignMissionGridlockRuleUtility.AdvanceHold(ref hold,20000,60000,false,true,true,true,false);Assert.AreEqual(10000,hold);
        CampaignMissionGridlockRuleUtility.AdvanceHold(ref hold,20000,1000,true,true,true,false,false);Assert.AreEqual(0,hold);
        CampaignMissionGridlockRuleUtility.AdvanceHold(ref hold,20000,19000,true,true,true,true,false);Assert.AreEqual(19000,hold);
        CampaignMissionGridlockRuleUtility.AdvanceHold(ref hold,20000,1000,true,true,true,true,true);Assert.AreEqual(0,hold);
    }
    [Test] public void LossPrecedesArrivalAndOneWorkerIsRecoverable()
    {
        var s=Arrived();s.LivingWorkers=1;Assert.IsTrue(CampaignMissionGridlockRuleUtility.IsVictory(s,true,20000,720000));
        s.ElapsedMilliseconds=720000;Assert.AreEqual(GridlockFailure.Deadline,CampaignMissionGridlockRuleUtility.Failure(s,720000));
        Assert.IsFalse(CampaignMissionGridlockRuleUtility.IsVictory(s,true,20000,720000));
    }
    [Test] public void MissingInitializationCannotWin()
    {
        var s=Arrived();s.Ready=0;Assert.IsFalse(CampaignMissionGridlockRuleUtility.IsVictory(s,true,20000,720000));
        s.Ready=1;s.Failure=GridlockFailure.Integrity;Assert.IsFalse(CampaignMissionGridlockRuleUtility.IsVictory(s,true,20000,720000));
    }
    [Test] public void AllMandatoryLossesFail()
    {
        for(int i=0;i<4;i++)
        {
            var s=Arrived();if(i==0)s.LivingFadi=0;else if(i==1)s.LivingWorkers=0;else if(i==2)s.LivingRifles=0;else s.LivingVehicle=0;
            Assert.AreNotEqual(GridlockFailure.None,CampaignMissionGridlockRuleUtility.Failure(s,720000));
            Assert.IsFalse(CampaignMissionGridlockRuleUtility.IsVictory(s,true,20000,720000));
        }
    }
    [Test] public void ConnectivityAndArrivalAreIndependent()
    {
        var s=Arrived();s.RouteConnected=0;Assert.IsFalse(CampaignMissionGridlockRuleUtility.IsVictory(s,true,20000,720000));
        s=Arrived();s.VehicleArrived=0;Assert.IsFalse(CampaignMissionGridlockRuleUtility.IsVictory(s,true,20000,720000));
        s=Arrived();Assert.IsFalse(CampaignMissionGridlockRuleUtility.IsVictory(s,false,20000,720000));
    }

    [Test] public void ChapterOpeningPersistsWithoutGrantingRewards()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GridlockOpening-"+Guid.NewGuid().ToString("N"));
        try
        {
            var service = new SaveService(new JsonSaveRepository(path));
            var store = new CampaignMissionProgressStore(service);
            const string mission = "saga.ch02.m01.gridlock";
            var before = service.LoadProfile();
            Assert.IsFalse(store.HasSeenChapterOpening(mission));
            store.MarkChapterOpeningSeen(mission);
            store.MarkChapterOpeningSeen(mission);
            var reopened = new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(path)));
            Assert.IsTrue(reopened.HasSeenChapterOpening(mission));
            Assert.IsTrue(reopened.ReadAll()[0].chapterOpeningSeen);
            Assert.IsFalse(reopened.ReadAll()[0].firstClearCompleted);
            Assert.AreEqual(before.credits, service.LoadProfile().credits);
            Assert.AreEqual(before.commanderXp, service.LoadProfile().commanderXp);
            reopened.EnsureAvailable(mission);
            Assert.IsTrue(reopened.HasSeenChapterOpening(mission));
        }
        finally { if (System.IO.Directory.Exists(path)) System.IO.Directory.Delete(path, true); }
    }

    [Test] public void CrewSelectionUsesMissionIdentity()
    {
        using var world = new World("Gridlock crew presentation");
        var em = world.EntityManager;
        var fadi = em.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
        var worker = em.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
        var rifle = em.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
        em.SetComponentData(fadi, new CampaignMissionUnitRoleComponent { MissionRoleId = "role.gridlock.fadi" });
        em.SetComponentData(worker, new CampaignMissionUnitRoleComponent { MissionRoleId = "role.gridlock.worker" });
        em.SetComponentData(rifle, new CampaignMissionUnitRoleComponent { MissionRoleId = "role.friendly.rifle" });
        Assert.IsTrue(SelectionUiReadModelLookup.IsGridlockFadi(em, fadi));
        Assert.IsTrue(SelectionUiReadModelLookup.IsGridlockCrew(em, worker));
        Assert.IsFalse(SelectionUiReadModelLookup.IsGridlockCrew(em, rifle));
        Assert.AreNotEqual(SelectionUiReadModelLookup.ResolveGroupTitle(3,3,0,0,0,0,0),
            SelectionUiReadModelLookup.ResolveGroupTitle(3,3,0,0,0,0,0,3));
        Assert.AreNotEqual(SelectionUiReadModelLookup.ResolveGroupSubtitle(3,3,0,0,0,0,0),
            SelectionUiReadModelLookup.ResolveGroupSubtitle(3,3,0,0,0,0,0,3));
    }

    [Test] public void ReserveEntersAfterWarningWithoutChangingHealth()
    {
        using var world=new World("Gridlock reserve validation");var em=world.EntityManager;
        var root=em.CreateEntity();var reserve=em.CreateEntity(typeof(UnitHealth));var visual=em.CreateEntity();
        em.SetComponentData(reserve,new UnitHealth {Current=70,Max=70});
        var group=em.AddBuffer<LinkedEntityGroup>(reserve);group.Add(new LinkedEntityGroup {Value=reserve});group.Add(new LinkedEntityGroup {Value=visual});
        em.AddBuffer<CampaignMissionGridlockMember>(root).Add(new CampaignMissionGridlockMember
            {Entity=reserve,Kind=GridlockMemberKind.Counterattack,HealthInitialized=1});
        var state=new CampaignMissionGridlockState();
        CampaignMissionRuntimeSystem.UpdateGridlockReserve(em,root,state);
        Assert.IsFalse(em.HasComponent<Disabled>(reserve),"Initialization must finish before staging the reserve.");
        state.Ready=1;CampaignMissionRuntimeSystem.UpdateGridlockReserve(em,root,state);
        Assert.IsTrue(em.HasComponent<Disabled>(reserve));Assert.IsTrue(em.HasComponent<Disabled>(visual));
        state.CounterattackWarned=1;state.CounterattackReleaseAtMilliseconds=15000;state.ElapsedMilliseconds=14999;
        CampaignMissionRuntimeSystem.UpdateGridlockReserve(em,root,state);Assert.IsTrue(em.HasComponent<Disabled>(reserve));
        state.ElapsedMilliseconds=15000;CampaignMissionRuntimeSystem.UpdateGridlockReserve(em,root,state);
        Assert.IsFalse(em.HasComponent<Disabled>(reserve));Assert.IsFalse(em.HasComponent<Disabled>(visual));
        Assert.AreEqual(70,em.GetComponentData<UnitHealth>(reserve).Current,"Release must not replace or heal the finite reserve.");
    }
}
