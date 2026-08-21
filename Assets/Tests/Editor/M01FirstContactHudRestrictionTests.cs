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
    private const string Marker = "[M01FirstContactHudRestrictionValidation] result=Passed tests=7";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(CanonicalMissionRestrictionsProjectReadOnly, ref passed);
            Run(CinematicInteractionLockTracksOpeningReturn, ref passed);
            Run(RightRailDisablesAndGraysBuildAndSupport, ref passed);
            Run(ResourceHeaderDisablesAndGraysEconomyButPreservesCivilianRisk, ref passed);
            Run(SquadTrayDisablesAndGraysNonAuthoredCategoriesButPreservesDefaults, ref passed);
            Run(SquadGuidancePointsAtExactSoldierButtonWithoutBlockingInput, ref passed);
            Run(AssistantPreviewPreservesRtsFraming, ref passed);
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
            Assert.That(restrictions.CinematicInteractionLocked, Is.False);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            blob.Dispose();
        }
    }

    [Test]
    public static void CinematicInteractionLockTracksOpeningReturn()
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = CreateMissionWorld(out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Entity root = world.EntityManager.CreateEntityQuery(typeof(CampaignMissionRootComponent)).GetSingletonEntity();
            CampaignMissionRuntimeComponent runtime = world.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            runtime.SessionToken = new FixedString64Bytes("m01.hud.test");
            world.EntityManager.SetComponentData(root, runtime);
            world.EntityManager.AddComponentData(root, new CampaignMissionOpeningPresentationComponent
            {
                SessionToken = runtime.SessionToken,
                Stage = 5
            });

            Assert.That(UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var duringOpening), Is.True);
            Assert.That(duringOpening.CinematicInteractionLocked, Is.True);
            CampaignMissionOpeningPresentationComponent opening =
                world.EntityManager.GetComponentData<CampaignMissionOpeningPresentationComponent>(root);
            opening.Stage = 6;
            world.EntityManager.SetComponentData(root, opening);
            Assert.That(UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var afterReturn), Is.True);
            Assert.That(afterReturn.CinematicInteractionLocked, Is.False);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            blob.Dispose();
        }
    }

    [Test]
    public static void RightRailDisablesAndGraysBuildAndSupport()
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
            Assert.That(build.gameObject.activeSelf, Is.True);
            Assert.That(support.gameObject.activeSelf, Is.True);
            Assert.That(build.interactable, Is.False);
            Assert.That(support.interactable, Is.False);
            Assert.That(build.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(support.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(build.colors.disabledColor.a, Is.EqualTo(build.colors.normalColor.a));
            Assert.That(support.colors.disabledColor.a, Is.EqualTo(support.colors.normalColor.a));
            Assert.That(build.GetComponent<Image>().material.shader.name,
                Is.EqualTo("Warline/UI/Disabled Grayscale"));
            Assert.That(support.GetComponent<Image>().material.shader.name,
                Is.EqualTo("Warline/UI/Disabled Grayscale"));
            Assert.That(zoomIn.gameObject.activeSelf, Is.True);
            Assert.That(zoomOut.gameObject.activeSelf, Is.True);
            view.ApplyMissionRestrictionVisibility(buildDisabled: false, supportDisabled: false);
            Assert.That(build.gameObject.activeSelf, Is.True, "Skirmish/default presentation must remain unchanged.");
            Assert.That(support.gameObject.activeSelf, Is.True);
            Assert.That(build.interactable, Is.True);
            Assert.That(support.interactable, Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public static void ResourceHeaderDisablesAndGraysEconomyButPreservesCivilianRisk()
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
            Assert.That(materials.activeSelf, Is.True);
            Assert.That(oil.activeSelf, Is.True);
            Assert.That(fuel.activeSelf, Is.True);
            Assert.That(civilian.activeSelf, Is.True);
            Assert.That(root.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(materials.GetComponent<Image>().material.shader.name,
                Is.EqualTo("Warline/UI/Disabled Grayscale"));
            Assert.That(oil.GetComponent<Image>().material.shader.name,
                Is.EqualTo("Warline/UI/Disabled Grayscale"));
            Assert.That(fuel.GetComponent<Image>().material.shader.name,
                Is.EqualTo("Warline/UI/Disabled Grayscale"));
            Assert.That(civilian.GetComponent<Image>().material.shader.name,
                Is.Not.EqualTo("Warline/UI/Disabled Grayscale"));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            blob.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public static void SquadTrayDisablesAndGraysNonAuthoredCategoriesButPreservesDefaults()
    {
        GameObject root = new("SquadTray", typeof(RectTransform));
        root.SetActive(false);
        try
        {
            MatchHudSquadTrayView view = root.AddComponent<MatchHudSquadTrayView>();
            var cards = new MatchHudSquadTrayView.Card[5];
            for (int i = 0; i < cards.Length; i++)
            {
                Button button = CreateButton(root.transform, $"Card{i}");
                cards[i] = new MatchHudSquadTrayView.Card
                {
                    Button = button,
                    FrameImage = button.GetComponent<Image>()
                };
            }

            typeof(MatchHudSquadTrayView)
                .GetField("cards", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(view, cards);
            root.SetActive(true);

            view.ApplyMissionRestrictionVisibility(
                combatVehiclesDisabled: true,
                airDisabled: true,
                transportDisabled: true);
            Assert.That(cards[0].Button.gameObject.activeSelf, Is.True, "The authored soldier command squad must remain available.");
            for (int i = 1; i < cards.Length; i++)
            {
                Assert.That(cards[i].Button.gameObject.activeSelf, Is.True, $"Restricted card {i} must remain visible.");
                Assert.That(cards[i].Button.interactable, Is.False, $"Restricted card {i} must be disabled.");
                Assert.That(cards[i].FrameImage.color.a, Is.EqualTo(1f), $"Restricted card {i} must preserve alpha.");
                Assert.That(cards[i].FrameImage.material.shader.name,
                    Is.EqualTo("Warline/UI/Disabled Grayscale"),
                    $"Restricted card {i} must use the disabled material.");
            }

            view.ApplyMissionRestrictionVisibility(
                combatVehiclesDisabled: false,
                airDisabled: false,
                transportDisabled: false);
            for (int i = 0; i < cards.Length; i++)
            {
                Assert.That(cards[i].Button.gameObject.activeSelf, Is.True, "Skirmish/default presentation must remain unchanged.");
                Assert.That(cards[i].Button.interactable, Is.True);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public static void AssistantPreviewPreservesRtsFraming()
    {
        using World world = new("M01 assistant camera preview test");
        Entity camera = world.EntityManager.CreateEntity(typeof(RtsCameraRequestQueueComponent));
        world.EntityManager.AddBuffer<RtsCameraRequestElement>(camera);

        Type previewUtility = typeof(UiShellEcsGateway).Assembly.GetType(
            "Game.UI.Shell.Ecs.AssistantPreviewTargetUtility",
            throwOnError: true);
        MethodInfo queueCameraPreview = previewUtility.GetMethod(
            "QueueCameraPreview",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(queueCameraPreview, Is.Not.Null);
        queueCameraPreview.Invoke(
            null,
            new object[]
            {
                world.EntityManager,
                camera,
                new Unity.Mathematics.float3(10f, 0f, 20f)
            });

        DynamicBuffer<RtsCameraRequestElement> requests =
            world.EntityManager.GetBuffer<RtsCameraRequestElement>(camera);
        Assert.That(requests.Length, Is.EqualTo(4));
        Assert.That(requests[0].Kind, Is.EqualTo(RtsCameraRequestKind.MoveGroundCenterTo));
        Assert.That(requests[1].Kind, Is.EqualTo(RtsCameraRequestKind.ClearSmoothFocusTarget));
        Assert.That(requests[2].Kind, Is.EqualTo(RtsCameraRequestKind.ClearSmoothPerspectiveTarget));
        Assert.That(requests[3].Kind, Is.EqualTo(RtsCameraRequestKind.ClearDragging));
        for (int index = 0; index < requests.Length; index++)
        {
            Assert.That(requests[index].Kind,
                Is.Not.EqualTo(RtsCameraRequestKind.ApplyPerspectiveModeInstant),
                "Show Me must not drop into the high-detail tactical zoom.");
        }
    }

    [Test]
    public static void SquadGuidancePointsAtExactSoldierButtonWithoutBlockingInput()
    {
        GameObject root = new("SquadTrayGuidance", typeof(RectTransform));
        root.SetActive(false);
        try
        {
            MatchHudSquadTrayView view = root.AddComponent<MatchHudSquadTrayView>();
            var cards = new MatchHudSquadTrayView.Card[5];
            for (int index = 0; index < cards.Length; index++)
            {
                Button button = CreateButton(root.transform, $"Card{index}");
                cards[index] = new MatchHudSquadTrayView.Card
                {
                    Button = button,
                    FrameImage = button.GetComponent<Image>()
                };
            }
            typeof(MatchHudSquadTrayView)
                .GetField("cards", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(view, cards);
            root.SetActive(true);

            view.ApplyAssistantGuidance(new UiAssistantHighlightModel(
                1, true, 7, 25010, 0, (byte)AssistantTargetKind.Squad, 0f, 0f, 0f, 1f));
            Transform cue = cards[0].Button.transform.Find("AriaButtonGuidance");
            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.gameObject.activeSelf, Is.True);
            Assert.That(cue.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Assert.That(cue.GetComponentInChildren<TMP_Text>().text, Does.Contain("TAP RIFLE SQUAD"));
        }
        finally
        {
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
        GameObject slot = new(name, typeof(RectTransform), typeof(Image));
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
