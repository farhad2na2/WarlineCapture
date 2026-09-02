using Game.UI.Contracts;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MatchHudTransportPassengerDrawerTests
{
    private const string MatchHudPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private GameObject _instance;

    public static void RunBatchValidation()
    {
        try
        {
            RunTest(test => test.MatchHudPrefabSerializesTransportPassengerDrawerReferences());
            RunTest(test => test.TransportPassengerModelShowsChipAndDrawerRows());
            RunTest(test => test.MaterialFabricationModelShowsCompleteChipAndResetsHiddenState());
            RunTest(test => test.DisembarkPassengerRequestStoresTransportAndPassenger());
            Debug.Log("[MatchHudTransportPassengerDrawerValidation] result=Passed tests=4");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchHudTransportPassengerDrawerValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (_instance != null)
            Object.DestroyImmediate(_instance);
    }

    [Test]
    public void MatchHudPrefabSerializesTransportPassengerDrawerReferences()
    {
        MatchHudSelectionPanelView view = InstantiateSelectionPanelView();
        SerializedObject serialized = new(view);

        Assert.NotNull(GetReference<GameObject>(serialized, "passengerChipRoot"));
        Assert.NotNull(GetReference<Button>(serialized, "passengerChipButton"));
        Assert.NotNull(GetReference<TMP_Text>(serialized, "passengerChipLabel"));
        Assert.NotNull(GetReference<MatchHudTransportPassengerDrawerView>(serialized, "passengerDrawer"));

        MatchHudTransportPassengerDrawerView drawer = GetReference<MatchHudTransportPassengerDrawerView>(serialized, "passengerDrawer");
        SerializedObject drawerSerialized = new(drawer);
        Assert.NotNull(GetReference<GameObject>(drawerSerialized, "drawerRoot"));
        Assert.NotNull(GetReference<TMP_Text>(drawerSerialized, "headerText"));
        Assert.NotNull(GetReference<GameObject>(drawerSerialized, "emptyStateRoot"));
        Assert.NotNull(GetReference<RectTransform>(drawerSerialized, "contentRoot"));
        Assert.NotNull(GetReference<MatchHudTransportPassengerItemView>(drawerSerialized, "itemTemplate"));
        Assert.NotNull(GetReference<Button>(drawerSerialized, "exitAllButton"));
        Assert.NotNull(GetReference<Button>(drawerSerialized, "closeButton"));
    }

    [Test]
    public void TransportPassengerModelShowsChipAndDrawerRows()
    {
        MatchHudSelectionPanelView view = InstantiateSelectionPanelView();
        SerializedObject serialized = new(view);
        GameObject chip = GetReference<GameObject>(serialized, "passengerChipRoot");
        TMP_Text chipLabel = GetReference<TMP_Text>(serialized, "passengerChipLabel");
        MatchHudTransportPassengerDrawerView drawer = GetReference<MatchHudTransportPassengerDrawerView>(serialized, "passengerDrawer");
        GameObject drawerRoot = GetReference<GameObject>(new SerializedObject(drawer), "drawerRoot");
        Transform threatJumpPanel = FindNamedChild(_instance.transform, "ThreatJumpPanel");
        Assert.NotNull(threatJumpPanel);
        bool threatWasActive = threatJumpPanel.gameObject.activeSelf;
        SerializedObject drawerSerialized = new(drawer);
        RectTransform contentRoot = GetReference<RectTransform>(drawerSerialized, "contentRoot");
        RectTransform capacitySlotsRoot = GetReference<RectTransform>(drawerSerialized, "capacitySlotsRoot");
        Button ropeDropButton = GetReference<Button>(drawerSerialized, "ropeDropButton");
        Sprite riflemanCardSprite = CreateTestSprite();
        UiEntityHandle exitedPassenger = UiEntityHandle.Null;
        int exitAllRequests = 0;
        view.BindTransportPassengerActions(
            () => { },
            () => { },
            () => exitAllRequests++,
            passenger => exitedPassenger = passenger);

        var passengers = new List<MatchHudSelectionPanelPassengerItemModel>
        {
            new(new UiEntityHandle(12, 1), "Rifleman", "SOLDIER", "Health: 80/100", 0.8f, riflemanCardSprite, true),
            new(new UiEntityHandle(13, 1), "Medic", "SOLDIER", "Health: 60/100", 0.6f, null, true)
        };

        view.ApplyTransportPassengers(new MatchHudTransportPassengersModel(
            true,
            false,
            new UiEntityHandle(99, 1),
            2,
            8,
            true,
            passengers));

        Assert.IsTrue(chip.activeSelf);
        Assert.AreEqual("PASSENGERS 2/8", chipLabel.text);
        Assert.IsFalse(drawerRoot.activeSelf);

        view.ToggleTransportPassengerDrawer();
        view.ApplyTransportPassengers(new MatchHudTransportPassengersModel(
            true,
            false,
            new UiEntityHandle(99, 1),
            2,
            8,
            true,
            passengers));

        Assert.IsTrue(drawerRoot.activeSelf);
        Assert.IsFalse(threatJumpPanel.gameObject.activeSelf,
            "The open passenger drawer must suppress the overlapping hostile-alert header.");
        Assert.GreaterOrEqual(CountActivePassengerRows(contentRoot), 2);
        Assert.AreSame(riflemanCardSprite, ResolveFirstActivePassengerPortrait(contentRoot));
        Assert.NotNull(capacitySlotsRoot);
        Assert.AreEqual(8, CountActiveChildren(capacitySlotsRoot));
        Assert.NotNull(ropeDropButton);
        Assert.IsTrue(ropeDropButton.gameObject.activeSelf);
        ropeDropButton.onClick.Invoke();
        Assert.AreEqual(1, exitAllRequests);

        MatchHudTransportPassengerItemView firstItem = ResolveFirstActivePassengerItem(contentRoot);
        Assert.NotNull(firstItem);
        SerializedObject itemSerialized = new(firstItem);
        Assert.AreEqual("Rifleman", GetReference<TMP_Text>(itemSerialized, "nameText").text);
        Assert.AreEqual("SOLDIER", GetReference<TMP_Text>(itemSerialized, "roleText").text);
        Assert.AreEqual("Health: 80/100", GetReference<TMP_Text>(itemSerialized, "healthText").text);
        Assert.AreEqual(0.8f, GetReference<Image>(itemSerialized, "healthFillImage").fillAmount, 0.001f);
        Button exitButton = GetReference<Button>(itemSerialized, "exitButton");
        Assert.IsTrue(exitButton.interactable);
        exitButton.onClick.Invoke();
        Assert.AreEqual(passengers[0].Passenger, exitedPassenger);

        view.CloseTransportPassengerDrawer();
        view.ApplyTransportPassengers(new MatchHudTransportPassengersModel(
            true,
            false,
            new UiEntityHandle(99, 1),
            2,
            8,
            true,
            passengers));
        Assert.AreEqual(threatWasActive, threatJumpPanel.gameObject.activeSelf,
            "Closing the passenger drawer must restore the header's exact prior visibility.");
    }

    [Test]
    public void MaterialFabricationModelShowsCompleteChipAndResetsHiddenState()
    {
        MatchHudSelectionPanelView view = InstantiateSelectionPanelView();
        SerializedObject serialized = new(view);
        GameObject chip = GetReference<GameObject>(serialized, "passengerChipRoot");
        Button chipButton = GetReference<Button>(serialized, "passengerChipButton");
        TMP_Text chipLabel = GetReference<TMP_Text>(serialized, "passengerChipLabel");
        MatchHudTransportPassengerDrawerView drawer = GetReference<MatchHudTransportPassengerDrawerView>(serialized, "passengerDrawer");
        GameObject drawerRoot = GetReference<GameObject>(new SerializedObject(drawer), "drawerRoot");
        bool? requestedProductionEnabled = null;
        Vector2 defaultChipSize = ((RectTransform)chip.transform).sizeDelta;
        view.BindMaterialFabricationProductionAction(enabled => requestedProductionEnabled = enabled);

        view.ApplyTransportPassengers(new MatchHudTransportPassengersModel(
            true,
            false,
            UiEntityHandle.Null,
            32,
            80,
            false,
            null,
            storageKind: MatchHudStorageChipKind.MaterialFabrication,
            oilCurrent: 18,
            oilCapacity: 60,
            statusText: "FABRICATING MATERIALS - OUTPUT ACTIVE",
            materialsCurrent: 32,
            materialsCapacity: 80,
            oilConsumedPerCycle: 3.5f,
            materialsOutputPerCycle: 7,
            cycleDurationSeconds: 12f,
            cycleProgress01: 0.45f,
            productionEnabled: true));

        Assert.IsTrue(chip.activeSelf);
        Assert.IsTrue(chipButton.interactable);
        Assert.IsFalse(drawerRoot.activeSelf);
        RectTransform chipRect = chip.transform as RectTransform;
        Assert.NotNull(chipRect);
        float expectedFabricationWidth = Mathf.Min(
            610f,
            ((RectTransform)chipRect.parent).rect.width - Mathf.Abs(chipRect.anchoredPosition.x) * 2f);
        Assert.AreEqual(new Vector2(expectedFabricationWidth, 156f), chipRect.sizeDelta);
        Assert.GreaterOrEqual(chipRect.rect.height, 44f, "Fabrication control must retain a mobile touch target.");
        Assert.IsTrue(chipLabel.enableAutoSizing);
        Assert.AreEqual(TextWrappingModes.Normal, chipLabel.textWrappingMode);
        Assert.AreEqual(20f, chipLabel.fontSizeMin);
        Assert.LessOrEqual(chipRect.rect.width, ((RectTransform)chipRect.parent).rect.width);
        StringAssert.Contains("OIL 18/60", chipLabel.text);
        StringAssert.Contains("3.5 OIL > 7 MATERIALS / 12s", chipLabel.text);
        StringAssert.Contains("45%", chipLabel.text);
        StringAssert.Contains("MATERIALS 32/80", chipLabel.text);
        StringAssert.Contains("FABRICATING", chipLabel.text);
        Assert.AreEqual(2, CountCharacter(chipLabel.text, '\n'));
        Canvas.ForceUpdateCanvases();
        chipLabel.ForceMeshUpdate();
        Assert.IsFalse(chipLabel.isTextOverflowing);
        AssertChipInsideSelectionFrameAtResolution(_instance, chipRect, 1920f, 1080f);
        AssertChipInsideSelectionFrameAtResolution(_instance, chipRect, 2400f, 1080f);

        chipButton.onClick.Invoke();
        Assert.AreEqual(false, requestedProductionEnabled);
        Assert.IsFalse(drawerRoot.activeSelf);
        chipButton.onClick.Invoke();
        Assert.AreEqual(true, requestedProductionEnabled);
        Assert.IsFalse(drawerRoot.activeSelf);

        view.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);

        Assert.IsFalse(chip.activeSelf);
        Assert.IsFalse(drawerRoot.activeSelf);
        Assert.AreEqual(string.Empty, chipLabel.text);
        Assert.AreEqual(defaultChipSize, chipRect.sizeDelta);
        Assert.IsFalse(chipLabel.enableAutoSizing);
    }

    [Test]
    public void DisembarkPassengerRequestStoresTransportAndPassenger()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("DisembarkPassengerRequestStoresTransportAndPassenger");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            var inputSystem = new RtsSelectionInputCompositionSystemHelper(Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager);
            Entity transport = new() { Index = 22, Version = 1 };
            Entity passenger = new() { Index = 23, Version = 1 };

            Assert.IsTrue(inputSystem.QueueDisembarkTransportPassengerCommandRequest(transport, passenger, frame: 44));
            Assert.IsTrue(inputSystem.HasPendingTransportCommandRequests());
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out _));
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(RtsSelectionCommandIntentKind.DisembarkTransportPassenger, requests[0].Kind);
            Assert.AreEqual(transport, requests[0].TargetEntity);
            Assert.AreEqual(passenger, requests[0].SecondaryTargetEntity);
            Assert.AreEqual(1, requests[0].HasTargetEntity);
            Assert.AreEqual(1, requests[0].HasSecondaryTargetEntity);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    private MatchHudSelectionPanelView InstantiateSelectionPanelView()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudPrefabPath);
        Assert.NotNull(prefab, $"Missing Match HUD prefab at {MatchHudPrefabPath}.");
        _instance = Object.Instantiate(prefab);
        MatchHudSelectionPanelView view = _instance.GetComponentInChildren<MatchHudSelectionPanelView>(true);
        Assert.NotNull(view);
        return view;
    }

    private static Transform FindNamedChild(Transform root, string targetName)
    {
        if (root == null)
            return null;
        if (root.name == targetName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedChild(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }
        return null;
    }

    private static int CountCharacter(string value, char character)
    {
        int count = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == character)
                count++;
        }

        return count;
    }

    private static void AssertChipInsideSelectionFrameAtResolution(
        GameObject prefabRoot,
        RectTransform chipRect,
        float width,
        float height)
    {
        RectTransform rootRect = prefabRoot.transform as RectTransform;
        Assert.NotNull(rootRect);
        rootRect.sizeDelta = new Vector2(width, height);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

        RectTransform frameRect = chipRect.parent as RectTransform;
        Assert.NotNull(frameRect);
        var corners = new Vector3[4];
        chipRect.GetWorldCorners(corners);
        Rect frame = frameRect.rect;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 localCorner = frameRect.InverseTransformPoint(corners[i]);
            Assert.IsTrue(
                frame.Contains(localCorner),
                $"Fabrication chip corner {i} must stay inside the selection frame at {width}x{height}.");
        }
    }

    private static int CountActivePassengerRows(RectTransform contentRoot)
    {
        int count = 0;
        for (int i = 0; i < contentRoot.childCount; i++)
        {
            MatchHudTransportPassengerItemView item = contentRoot.GetChild(i).GetComponent<MatchHudTransportPassengerItemView>();
            if (item != null && item.gameObject.activeSelf)
                count++;
        }

        return count;
    }

    private static Sprite ResolveFirstActivePassengerPortrait(RectTransform contentRoot)
    {
        MatchHudTransportPassengerItemView item = ResolveFirstActivePassengerItem(contentRoot);
        if (item == null)
            return null;

        Image portraitImage = GetReference<Image>(new SerializedObject(item), "portraitImage");
        return portraitImage != null ? portraitImage.sprite : null;
    }

    private static MatchHudTransportPassengerItemView ResolveFirstActivePassengerItem(RectTransform contentRoot)
    {
        for (int i = 0; i < contentRoot.childCount; i++)
        {
            MatchHudTransportPassengerItemView item = contentRoot.GetChild(i).GetComponent<MatchHudTransportPassengerItemView>();
            if (item != null && item.gameObject.activeSelf)
                return item;
        }

        return null;
    }

    private static int CountActiveChildren(RectTransform root)
    {
        int count = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i).gameObject.activeSelf)
                count++;
        }
        return count;
    }

    private static Sprite CreateTestSprite()
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void RunTest(Action<MatchHudTransportPassengerDrawerTests> test)
    {
        var fixture = new MatchHudTransportPassengerDrawerTests();
        try
        {
            test(fixture);
        }
        finally
        {
            fixture.TearDown();
        }
    }

    private static T GetReference<T>(SerializedObject serialized, string propertyName) where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        Assert.NotNull(property, $"Missing serialized property {propertyName}.");
        return property.objectReferenceValue as T;
    }
}
#endif
