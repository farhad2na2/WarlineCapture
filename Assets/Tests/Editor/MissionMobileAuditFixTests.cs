using System;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using Game.UI.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionMobileAuditFixTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var test=new MissionMobileAuditFixTests();
            test.SolidHealthBarShowsHalfAndZeroHealth();
            test.DeadUnitCannotRemainFocused();
            test.PassengerDrawerOpensWithoutAnEcsDataChange();
            test.RescuePassengerIdentityAndHealthAreReadable();
            test.DefenseReinforcementDoesNotRequireAProductionObjective();
            Debug.Log("[MissionMobileAuditFixTests] result=Passed tests=5");ValidationExit.Passed();
        }
        catch(Exception error){Debug.LogException(error);ValidationExit.Failed();}
    }
    [Test] public void SolidHealthBarShowsHalfAndZeroHealth()
    {
        var root=new GameObject("Health regression",typeof(RectTransform));
        try
        {
            var view=root.AddComponent<MatchHudSelectionPanelView>();
            var child=new GameObject("HealthFill",typeof(RectTransform),typeof(Image));child.transform.SetParent(root.transform,false);
            var image=child.GetComponent<Image>();image.sprite=null;image.rectTransform.pivot=new Vector2(0,.5f);
            typeof(MatchHudSelectionPanelView).GetField("healthFillImage",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,image);
            var bind=typeof(MatchHudSelectionPanelView).GetMethod("SetHealthFill",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(float health in new[]{1f,.5f,0f,1f})
            {
                bind.Invoke(view,new object[]{health});
                Assert.That(image.rectTransform.localScale.x,Is.EqualTo(health).Within(.001f));
                Assert.That(image.fillAmount,Is.EqualTo(health).Within(.001f));
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void DefenseReinforcementDoesNotRequireAProductionObjective()
    {
        var previous=World.DefaultGameObjectInjectionWorld;
        using var world=new World("M3 optional reinforcement");var em=world.EntityManager;
        var mission=UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.MissionDefinitionConfig>(Game.Editor.M03RadarWarningConfigBuilder.MissionPath);
        var scenario=UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.ScenarioSetupConfig>(Game.Editor.M03RadarWarningConfigBuilder.ScenarioPath);
        var maps=UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.OperationMapCatalogConfig>(Game.Editor.M02EstablishBaseConfigBuilder.OperationMapCatalogPath);
        Assert.IsTrue(Game.Composition.CampaignMissionCatalogProjection.TryProject(em,mission,scenario,maps,1,out var root,out string error),error);
        try
        {
            em.SetComponentData(root,new CampaignMissionRuntimeComponent{MissionId=Game.Editor.M03RadarWarningConfigBuilder.MissionId,Version=1,SourceVersion=1,Phase=Game.Missions.Contracts.MissionPhaseKind.Engage});
            if(!em.HasComponent<CampaignMissionDefenseStateComponent>(root))em.AddComponent<CampaignMissionDefenseStateComponent>(root);
            World.DefaultGameObjectInjectionWorld=world;Game.UI.Shell.Ecs.UiShellEcsGateway.RegisterAsRuntimeGateway();
            foreach(byte ready in new byte[]{0,1})
            {
                em.SetComponentData(root,new CampaignMissionDefenseStateComponent{InitialProducerReady=ready});
                Assert.IsTrue(UiShellRuntimeGateway.TryReadMissionBuildCatalog(out var catalog));
                Assert.AreEqual(scenario.MissionRuntime.RequiredUnitConfigId,catalog.RequiredUnitConfigId);
                Assert.AreEqual(ready!=0,catalog.CanRequestRequiredUnit);
            }
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld=previous;
            var catalog=em.GetComponentData<CampaignMissionCatalogComponent>(root);
            CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);em.SetComponentData(root,catalog);
        }
    }
    [Test] public void DeadUnitCannotRemainFocused()
    {
        using var world=new World("Dead selection");var em=world.EntityManager;
        var unit=em.CreateEntity(typeof(UnitHealth),typeof(Faction));em.SetComponentData(unit,new UnitHealth{Current=10,Max=100});
        var lookup=new SelectionUiReadModelLookup();Assert.IsTrue(lookup.HasFocusedUnit(em,unit));
        em.SetComponentData(unit,new UnitHealth{Current=0,Max=100});Assert.IsFalse(lookup.HasFocusedUnit(em,unit));
    }
    [Test] public void PassengerDrawerOpensWithoutAnEcsDataChange()
    {
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
        var root=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var view=root.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            var drawer=(MatchHudTransportPassengerDrawerView)typeof(MatchHudSelectionPanelView).GetField("passengerDrawer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
            var visible=(GameObject)typeof(MatchHudTransportPassengerDrawerView).GetField("drawerRoot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(drawer);
            view.ApplyTransportPassengers(new Game.UI.Contracts.MatchHudTransportPassengersModel(true,false,
                new Game.UI.Contracts.UiEntityHandle(1,1),0,10,false,Array.Empty<Game.UI.Contracts.MatchHudSelectionPanelPassengerItemModel>()));
            Assert.IsFalse(visible.activeSelf);
            view.ToggleTransportPassengerDrawer();
            typeof(MatchHudTransportPassengerDrawerView).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(drawer,null);
            Assert.IsTrue(view.IsPassengerDrawerOpen);Assert.IsTrue(visible.activeSelf,"Opening is UI state; it cannot wait for health or passenger counts to change.");
            view.CloseTransportPassengerDrawer();Assert.IsFalse(visible.activeSelf);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void RescuePassengerIdentityAndHealthAreReadable()
    {
        using var world=new World("Rescue identity");var em=world.EntityManager;
        var entity=em.CreateEntity(typeof(CampaignMissionUnitRoleComponent),typeof(UnitDisplayInfo));
        em.SetComponentData(entity,new CampaignMissionUnitRoleComponent{MissionRoleId="role.friendly.specialist"});
        em.SetComponentData(entity,new UnitDisplayInfo{Name="Generic civilian"});
        var lookup=new SelectionUiReadModelLookup();
        Assert.AreEqual(Game.Configs.GameText.Get("mission.m04.specialist.name","Specialist"),lookup.ResolveFocusedUnitName(em,entity));
        var format=typeof(SelectionHudFeedbackUiSystemHelper).GetMethod("BuildHealthModelFromValues",BindingFlags.Static|BindingFlags.NonPublic);
        object[] values={50,50,null,0f};format.Invoke(null,values);
        Assert.AreEqual("50/50",values[2],"The compact passenger health row must not clip a translated prefix.");
    }
}
