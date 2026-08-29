#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Catalog.Contracts;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class M02EstablishBaseDoItTests
{
    [Test]
    public void EveryActionableM02StepHasAnExactDoItRoute()
    {
        Assert.That(MatchHudAssistantUiSystemHelper.IsM02DoItStep(
            Panel(2, AssistantRecommendationKind.Build, AssistantTargetKind.UiSurface)), Is.True);
        Assert.That(MatchHudAssistantUiSystemHelper.IsM02DoItStep(
            Panel(3, AssistantRecommendationKind.Select, AssistantTargetKind.UiSurface)), Is.True);
        Assert.That(MatchHudAssistantUiSystemHelper.IsM02DoItStep(
            Panel(4, AssistantRecommendationKind.Build, AssistantTargetKind.WorldPosition)), Is.True);
        Assert.That(MatchHudAssistantUiSystemHelper.IsM02DoItStep(
            Panel(5, AssistantRecommendationKind.Explain, AssistantTargetKind.UiSurface)), Is.True);
        Assert.That(MatchHudAssistantUiSystemHelper.IsM02DoItStep(
            Panel(6, AssistantRecommendationKind.Produce, AssistantTargetKind.UiSurface)), Is.True);
        Assert.That(MatchHudAssistantUiSystemHelper.IsM02DoItStep(
            Panel(7, AssistantRecommendationKind.DefensiveAlert, AssistantTargetKind.Objective)), Is.False);
        Assert.That(MatchHudAssistantUiSystemHelper.IsM02DoItStep(
            Panel(3, AssistantRecommendationKind.Select, AssistantTargetKind.UiSurface, 5)), Is.False);
    }

    [TestCase(4, UiCampaignGuidanceTargetKind.BuildButton)]
    [TestCase(1, UiCampaignGuidanceTargetKind.BarracksCatalogItem)]
    [TestCase(9, UiCampaignGuidanceTargetKind.ResourceStrip)]
    [TestCase(5, UiCampaignGuidanceTargetKind.RifleProduction)]
    public void EverySuccessfulM02UiActionMapsToAuthoritativeAcknowledgement(
        byte recommendationKind,
        UiCampaignGuidanceTargetKind expected)
    {
        Assert.That(
            MatchHudAssistantUiSystemHelper.ResolveM02AcknowledgementTarget(recommendationKind),
            Is.EqualTo(expected));
    }

    [Test]
    public void BarracksDoItReopensTheDrawerAndSelectsTheRenderedItem()
    {
        GameObject buildObject = new("Build", typeof(RectTransform), typeof(Image), typeof(Button));
        GameObject drawerObject = new("Build Drawer", typeof(RectTransform));
        GameObject itemObject = new("Barracks Item", typeof(RectTransform), typeof(Image), typeof(Button));
        GameObject barracksPrefab = new("Building_Barrack");
        itemObject.transform.SetParent(drawerObject.transform, false);
        BuildDrawerView drawer = drawerObject.AddComponent<BuildDrawerView>();
        BuildDrawerItemView item = itemObject.AddComponent<BuildDrawerItemView>();
        Button buildButton = buildObject.GetComponent<Button>();
        Button barracksButton = itemObject.GetComponent<Button>();
        SetField(item, "selectionButton", barracksButton);
        SetField(drawer, "itemTemplate", item);
        BuildDrawerCatalogRuntimeView catalog = drawerObject.AddComponent<BuildDrawerCatalogRuntimeView>();
        catalog.ConfigureForTests(drawer, null, null);
        SetField(catalog, "_activeCategory", BuildDrawerCategory.Buildings);
        GetField<List<BuildDrawerCatalogItem>>(catalog, "_items").Add(
            new BuildDrawerCatalogItem(
                BuildDrawerCategory.Buildings,
                barracksPrefab,
                "Barracks",
                "BUILDINGS",
                "Forward post barracks",
                90,
                0,
                30f,
                new Vector2Int(20, 10),
                null,
                null,
                null));

        AssistantHighlightPresentationSystemHelper helper = new();
        try
        {
            int buildClicks = 0;
            int barracksClicks = 0;
            buildButton.onClick.AddListener(() => buildClicks++);
            barracksButton.onClick.AddListener(() => barracksClicks++);
            helper.Bind(null);
            helper.BindBuildButton(buildButton);
            helper.BindBuildDrawer(drawer);
            helper.BeginPendingShowMe(
                (byte)AssistantRecommendationKind.Select,
                (byte)AssistantTargetKind.UiSurface);
            drawerObject.SetActive(false);

            Assert.That(helper.TryExecuteUiSurface(
                (byte)AssistantRecommendationKind.Select,
                (byte)AssistantTargetKind.UiSurface), Is.False);
            Assert.That(buildClicks, Is.EqualTo(1));
            Assert.That(barracksClicks, Is.Zero);

            drawerObject.SetActive(true);
            Assert.That(helper.TryExecuteUiSurface(
                (byte)AssistantRecommendationKind.Select,
                (byte)AssistantTargetKind.UiSurface), Is.True);
            Assert.That(buildClicks, Is.EqualTo(1));
            Assert.That(barracksClicks, Is.EqualTo(1));

            drawerObject.SetActive(false);
            Assert.That(helper.TryExecuteUiSurface(
                (byte)AssistantRecommendationKind.Select,
                (byte)AssistantTargetKind.UiSurface), Is.False);
            Assert.That(buildClicks, Is.EqualTo(2));
        }
        finally
        {
            helper.Unbind();
            UnityEngine.Object.DestroyImmediate(barracksPrefab);
            UnityEngine.Object.DestroyImmediate(drawerObject);
            UnityEngine.Object.DestroyImmediate(buildObject);
        }
    }

    [Test]
    public void OneDoItRequestRetriesUntilTheBarracksControlIsReady()
    {
        MatchHudAssistantUiSystemHelper helper = new();
        SetField(helper, "_lastPanelModel",
            Panel(3, AssistantRecommendationKind.Select, AssistantTargetKind.UiSurface));
        Invoke(helper, "ExecuteRecommendation");
        Assert.That(GetField<byte>(helper, "_pendingM02DoItStep"), Is.EqualTo(3));

        GameObject drawerObject = new("Deferred Build Drawer", typeof(RectTransform));
        GameObject itemObject = new("Deferred Barracks", typeof(RectTransform), typeof(Image), typeof(Button));
        GameObject barracksPrefab = new("Building_Barrack");
        itemObject.transform.SetParent(drawerObject.transform, false);
        BuildDrawerView drawer = drawerObject.AddComponent<BuildDrawerView>();
        BuildDrawerItemView item = itemObject.AddComponent<BuildDrawerItemView>();
        Button barracksButton = itemObject.GetComponent<Button>();
        SetField(item, "selectionButton", barracksButton);
        SetField(drawer, "itemTemplate", item);
        BuildDrawerCatalogRuntimeView catalog = drawerObject.AddComponent<BuildDrawerCatalogRuntimeView>();
        catalog.ConfigureForTests(drawer, null, null);
        SetField(catalog, "_activeCategory", BuildDrawerCategory.Buildings);
        GetField<List<BuildDrawerCatalogItem>>(catalog, "_items").Add(
            new BuildDrawerCatalogItem(
                BuildDrawerCategory.Buildings,
                barracksPrefab,
                "Barracks",
                "BUILDINGS",
                string.Empty,
                90,
                0,
                30f,
                new Vector2Int(20, 10),
                null,
                null,
                null));
        int clicks = 0;
        barracksButton.onClick.AddListener(() => clicks++);

        try
        {
            helper.BindBuildDrawer(drawer);
            helper.TickHighlight(Time.unscaledTime);
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(GetField<byte>(helper, "_pendingM02DoItStep"), Is.Zero);
        }
        finally
        {
            helper.Unbind();
            UnityEngine.Object.DestroyImmediate(barracksPrefab);
            UnityEngine.Object.DestroyImmediate(drawerObject);
        }
    }

    [Test]
    public void LateAssistantBindRestoresTheAlreadyOpenBuildDrawer()
    {
        GameObject drawerObject = new("Live Build Drawer", typeof(RectTransform));
        BuildDrawerView drawer = drawerObject.AddComponent<BuildDrawerView>();
        MainMenuPlayUI owner = new();

        try
        {
            owner.BindBuildDrawer(drawer);

            // Production can install/open the drawer before the ARIA HUD finishes binding.
            // The assistant bind internally resets its presentation helper.
            owner.BindMatchHudAssistant(null, null, null);

            MatchHudAssistantUiSystemHelper assistant =
                GetField<MatchHudAssistantUiSystemHelper>(owner, "_matchHudAssistantUiSystem");
            AssistantHighlightPresentationSystemHelper highlight =
                GetField<AssistantHighlightPresentationSystemHelper>(
                    assistant,
                    "_highlightPresentationSystem");

            Assert.That(
                GetField<BuildDrawerView>(highlight, "_buildDrawerView"),
                Is.SameAs(drawer),
                "A late ARIA bind must retain the open drawer used by the second M2 DO IT action.");
        }
        finally
        {
            owner.Dispose();
            UnityEngine.Object.DestroyImmediate(drawerObject);
        }
    }

    private static UiAssistantPanelModel Panel(
        byte step,
        AssistantRecommendationKind recommendation,
        AssistantTargetKind target,
        byte stepCount = 9) =>
        new(
            1,
            false,
            0,
            UiAssistantGoalRowModel.Empty,
            UiAssistantGoalRowModel.Empty,
            UiAssistantGoalRowModel.Empty,
            UiAssistantMessageRowModel.Empty,
            UiAssistantMessageRowModel.Empty,
            UiAssistantMessageRowModel.Empty,
            UiAssistantMessageRowModel.Empty,
            UiAssistantMessageRowModel.Empty,
            UiAssistantTargetLockModel.Empty,
            UiAssistantNarrationModel.Empty,
            true,
            "M2",
            "M2",
            "HIGH",
            "DO IT",
            true,
            true,
            false,
            false,
            string.Empty,
            string.Empty,
            recommendationKind: (byte)recommendation,
            recommendationTargetKind: (byte)target,
            tutorialStep: step,
            tutorialStepCount: stepCount);

    private static void Invoke(object target, string methodName) =>
        target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);

    private static T GetField<T>(object target, string fieldName) =>
        (T)target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

    private static void SetField(object target, string fieldName, object value) =>
        target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
}
#endif
