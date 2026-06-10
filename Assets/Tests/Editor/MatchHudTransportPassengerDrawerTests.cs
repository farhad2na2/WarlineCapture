#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudTransportPassengerDrawerTests
{
    private const string MatchHudPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private GameObject _instance;

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
        RectTransform contentRoot = GetReference<RectTransform>(new SerializedObject(drawer), "contentRoot");
        Sprite riflemanCardSprite = CreateTestSprite();

        var passengers = new List<MatchHudSelectionPanelView.PassengerItemModel>
        {
            new(new Entity { Index = 12, Version = 1 }, "Rifleman", "SOLDIER", "Health: 80/100", 0.8f, riflemanCardSprite, true),
            new(new Entity { Index = 13, Version = 1 }, "Medic", "SOLDIER", "Health: 60/100", 0.6f, null, true)
        };

        view.ApplyTransportPassengers(new MatchHudSelectionPanelView.TransportPassengersModel(
            true,
            false,
            new Entity { Index = 99, Version = 1 },
            2,
            8,
            true,
            passengers));

        Assert.IsTrue(chip.activeSelf);
        Assert.AreEqual("PASSENGERS 2/8", chipLabel.text);
        Assert.IsFalse(drawerRoot.activeSelf);

        view.ToggleTransportPassengerDrawer();
        view.ApplyTransportPassengers(new MatchHudSelectionPanelView.TransportPassengersModel(
            true,
            false,
            new Entity { Index = 99, Version = 1 },
            2,
            8,
            true,
            passengers));

        Assert.IsTrue(drawerRoot.activeSelf);
        Assert.GreaterOrEqual(CountActivePassengerRows(contentRoot), 2);
        Assert.AreSame(riflemanCardSprite, ResolveFirstActivePassengerPortrait(contentRoot));
    }

    [Test]
    public void DisembarkPassengerRequestStoresTransportAndPassenger()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("DisembarkPassengerRequestStoresTransportAndPassenger");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            var inputSystem = new RtsSelectionInputSystem();
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
        for (int i = 0; i < contentRoot.childCount; i++)
        {
            MatchHudTransportPassengerItemView item = contentRoot.GetChild(i).GetComponent<MatchHudTransportPassengerItemView>();
            if (item == null || !item.gameObject.activeSelf)
                continue;

            Image portraitImage = GetReference<Image>(new SerializedObject(item), "portraitImage");
            return portraitImage != null ? portraitImage.sprite : null;
        }

        return null;
    }

    private static Sprite CreateTestSprite()
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
    }

    private static T GetReference<T>(SerializedObject serialized, string propertyName) where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        Assert.NotNull(property, $"Missing serialized property {propertyName}.");
        return property.objectReferenceValue as T;
    }
}
#endif
