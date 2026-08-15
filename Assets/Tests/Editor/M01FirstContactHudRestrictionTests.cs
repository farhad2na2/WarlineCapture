using System;
using System.Reflection;
using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public sealed class M01FirstContactHudRestrictionTests
{
    private const string Marker = "[M01FirstContactHudRestrictionValidation] result=Passed tests=3";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(CanonicalMissionRestrictionsProjectReadOnly, ref passed);
            Run(RightRailHidesOnlyBuildAndSupport, ref passed);
            Run(ResourceHeaderHidesEconomyButPreservesCivilianRisk, ref passed);
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[M01FirstContactHudRestrictionValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    [Test]
    public static void CanonicalMissionRestrictionsProjectReadOnly()
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = CreateMissionWorld(out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Assert.That(UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions), Is.True);
            Assert.That(restrictions.MissionId, Is.EqualTo("saga.ch01.m01.first_contact"));
            Assert.That(restrictions.BuildingDisabled, Is.True);
            Assert.That(restrictions.ProductionDisabled, Is.True);
            Assert.That(restrictions.EconomyDisabled, Is.True);
            Assert.That(restrictions.TransportDisabled, Is.True);
            Assert.That(restrictions.AirDisabled, Is.True);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            blob.Dispose();
        }
    }

    [Test]
    public static void RightRailHidesOnlyBuildAndSupport()
    {
        GameObject root = new("RightRail", typeof(RectTransform), typeof(MatchHudRightQuickRailView));
        try
        {
            Button build = CreateButton(root.transform, "BuildCommand");
            Button support = CreateButton(root.transform, "SupportCommand");
            Button zoomIn = CreateButton(root.transform, "ZoomInButton");
            Button zoomOut = CreateButton(root.transform, "ZoomOutButton");
            typeof(MatchHudRightQuickRailView)
                .GetField("buildButton", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(root.GetComponent<MatchHudRightQuickRailView>(), build);

            MatchHudRightQuickRailView view = root.GetComponent<MatchHudRightQuickRailView>();
            view.ApplyMissionRestrictionVisibility(buildDisabled: true, supportDisabled: true);
            Assert.That(build.gameObject.activeSelf, Is.False);
            Assert.That(support.gameObject.activeSelf, Is.False);
            Assert.That(zoomIn.gameObject.activeSelf, Is.True);
            Assert.That(zoomOut.gameObject.activeSelf, Is.True);
            view.ApplyMissionRestrictionVisibility(buildDisabled: false, supportDisabled: false);
            Assert.That(build.gameObject.activeSelf, Is.True, "Skirmish/default presentation must remain unchanged.");
            Assert.That(support.gameObject.activeSelf, Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public static void ResourceHeaderHidesEconomyButPreservesCivilianRisk()
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = CreateMissionWorld(out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        GameObject root = new("ResourceHeader");
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            GameObject materials = CreateSlot(root.transform, "MaterialsSlot", out TMP_Text materialsLabel, out TMP_Text materialsValue);
            GameObject oil = CreateSlot(root.transform, "OilSlot", out TMP_Text oilLabel, out TMP_Text oilValue);
            GameObject fuel = CreateSlot(root.transform, "FuelSlot", out TMP_Text fuelLabel, out TMP_Text fuelValue);
            GameObject civilian = CreateSlot(root.transform, "CivilianRiskSlot", out TMP_Text civilianLabel, out TMP_Text civilianValue);

            var presentation = new MatchHudResourceHeaderPresentation();
            presentation.Bind(oil, materialsLabel, materialsValue, oilLabel, oilValue,
                fuelLabel, fuelValue, civilianLabel, civilianValue, 0f);
            Assert.That(materials.activeSelf, Is.False);
            Assert.That(oil.activeSelf, Is.False);
            Assert.That(fuel.activeSelf, Is.False);
            Assert.That(civilian.activeSelf, Is.True);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            blob.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static World CreateMissionWorld(out BlobAssetReference<CampaignMissionCatalogBlob> blob)
    {
        World world = new("M01 HUD restriction tests");
        Entity root = world.EntityManager.CreateEntity(typeof(CampaignMissionRootComponent));
        world.EntityManager.AddComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
            Phase = MissionPhaseKind.FindSquad,
            Version = 1,
            SourceVersion = 1
        });

        using BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
        missions[0].MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact");
        missions[0].BuildingDisabled = 1;
        missions[0].ProductionDisabled = 1;
        missions[0].EconomyDisabled = 1;
        missions[0].TransportDisabled = 1;
        missions[0].AirDisabled = 1;
        blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        world.EntityManager.AddComponentData(root, new CampaignMissionCatalogComponent
        {
            Blob = blob,
            SourceVersion = 1,
            OwnsBlob = 0
        });
        return world;
    }

    private static Button CreateButton(Transform parent, string name)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        return buttonObject.GetComponent<Button>();
    }

    private static GameObject CreateSlot(
        Transform parent, string name, out TMP_Text label, out TMP_Text value)
    {
        GameObject slot = new(name, typeof(RectTransform));
        slot.transform.SetParent(parent, false);
        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(slot.transform, false);
        GameObject valueObject = new("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueObject.transform.SetParent(slot.transform, false);
        label = labelObject.GetComponent<TMP_Text>();
        value = valueObject.GetComponent<TMP_Text>();
        return slot;
    }

    private static void Run(Action test, ref int passed)
    {
        test();
        passed++;
    }
}
