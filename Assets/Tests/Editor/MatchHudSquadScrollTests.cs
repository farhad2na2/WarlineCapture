using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Editor;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MatchHudSquadScrollTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private static void Invoke(object instance, string method, params object[] args) =>
        instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, args);

    private sealed class Fixture : IDisposable
    {
        public GameObject Root, Events;
        public MatchHudSquadTrayView View;
        public readonly List<UiMatchHudSquadTrayCardModel> Models = new();
        public Fixture(int count)
        {
            Root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            View = Root.GetComponentInChildren<MatchHudSquadTrayView>(true);
            View.ConfigureExpandedRoster(list => { list.Clear(); list.AddRange(Models); }, _ =>
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card1_RifleSquad.png"));
            SetCount(count);
            Events = new GameObject("SquadScrollTestEvents", typeof(EventSystem));
        }
        public void SetCount(int count)
        {
            Models.Clear();
            for (int i = 0; i < count; i++) Models.Add(new UiMatchHudSquadTrayCardModel(true, "RIFLE " + (i+1), "4", 1, (uint)(i+1), 4, i+1, false));
            Refresh();
        }
        public void Refresh()
        {
            typeof(MatchHudSquadTrayView).GetField("_nextRosterRefresh", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(View, 0f);
            Invoke(View, "RefreshScrollingRoster", true);
            Canvas.ForceUpdateCanvases();
        }
        public Button Card(int ordinal) => View.GetComponent<ScrollRect>().content.GetChild(ordinal-1).GetComponent<Button>();
        public PointerEventData Pointer() => new(Events.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Left };
        public void Dispose() { Object.DestroyImmediate(Root); Object.DestroyImmediate(Events); }
    }

    [TestCase(1)] [TestCase(2)] [TestCase(5)] [TestCase(8)] [TestCase(21)]
    public void CardWidthAndCommandRailStayFixed(int count)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var authoredRail = prefab.transform.Find("V3Composition/FooterContent/CommandRail") as RectTransform;
        using var fixture = new Fixture(count);
        var rail = fixture.Root.transform.Find("V3Composition/FooterContent/CommandRail") as RectTransform;
        Assert.AreEqual(authoredRail.anchoredPosition, rail.anchoredPosition);
        Assert.AreEqual(authoredRail.sizeDelta, rail.sizeDelta);
        for (int i = 1; i <= count; i++) Assert.AreEqual(135, ((RectTransform)fixture.Card(i).transform).rect.width, .1);
        Assert.AreEqual(count, fixture.View.GetComponent<ScrollRect>().content.Cast<Transform>().Count(t => t.gameObject.activeSelf));
        Assert.IsNull(fixture.Root.transform.Find("V3Composition/FooterContent/SquadPagination"));
        Assert.IsNull(new SerializedObject(fixture.View).FindProperty("clearSelectionButton").objectReferenceValue);
        Assert.AreEqual(count > 5, fixture.View.NextPageButton.gameObject.activeSelf);
    }

    [Test]
    public void SwipingDoesNotTapAndPointerIdentityCannotChangeMidPress()
    {
        using var fixture = new Fixture(8);
        uint selected = 0;
        fixture.View.ConfigureExpandedGroupSelection(id => selected = id);
        var card = fixture.Card(1);
        var relay = card.GetComponent<MatchHudSquadCardPressRelay>();
        var pointer = fixture.Pointer();
        relay.OnPointerDown(pointer);
        relay.OnBeginDrag(pointer);
        card.onClick.Invoke();
        Assert.AreEqual(0, selected, "A swipe must never select the card it starts on.");
        relay.OnPointerDown(pointer); card.onClick.Invoke();
        Assert.AreEqual(1, selected, "A normal card press must dispatch its stable ID.");
        selected = 0; relay.OnPointerDown(pointer);
        fixture.Models.RemoveAt(0); fixture.Refresh(); card.onClick.Invoke();
        Assert.AreEqual(0, selected, "A death/reorder between down and click must not select a different group.");
    }

    [Test]
    public void OverflowTargetsRequireSwipeBeforeTapAndClampAfterRosterShrinks()
    {
        using var fixture = new Fixture(8);
        Assert.IsTrue(fixture.View.TryGetExpandedCardTarget(8, out var visible, out var from, out var to));
        Assert.IsNull(visible); Assert.Greater(from.x, to.x);
        Assert.Greater(fixture.View.ExpandedCardScrollDistance(8), 0);
        var scroll = fixture.View.GetComponent<ScrollRect>();
        scroll.horizontalNormalizedPosition = 1;
        Assert.IsTrue(fixture.View.TryGetExpandedCardTarget(8, out visible, out _, out _));
        Assert.NotNull(visible);
        fixture.SetCount(2);
        Assert.IsFalse(fixture.View.NextPageButton.gameObject.activeSelf);
        Assert.IsFalse(fixture.View.PreviousPageButton.gameObject.activeSelf);
        Assert.AreEqual(0, scroll.content.anchoredPosition.x, .1);
        Assert.AreEqual(135, ((RectTransform)fixture.Card(1).transform).rect.width, .1);
    }

    [Test]
    public void PausedRosterCardsCannotSelect()
    {
        using var fixture = new Fixture(8);
        uint selected = 0; fixture.View.ConfigureExpandedGroupSelection(id => selected = id);
        typeof(MatchHudSquadTrayView).GetField("_nextRosterRefresh", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fixture.View, 0f);
        Invoke(fixture.View, "RefreshScrollingRoster", false);
        var card = fixture.Card(1); card.GetComponent<MatchHudSquadCardPressRelay>().OnPointerDown(fixture.Pointer()); card.onClick.Invoke();
        Assert.AreEqual(0, selected); Assert.IsFalse(card.interactable);
    }

    [Test]
    public void CardToggleDeselectsOnlyThatGroupAndContinuousProjectionRetainsLogicalPage()
    {
        using var world = new World("SquadScrollToggle");
        var em = world.EntityManager;
        var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishArmySelectionComponent));
        em.SetComponentData(session, new SkirmishArmySelectionComponent { PageSize = 5 });
        em.AddBuffer<SkirmishArmyGroupRecord>(session);
        for (uint id = 1; id <= 8; id++)
        {
            em.GetBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord
                { GroupId = id, FactionId = 1, Role = SkirmishRoleKind.Rifle, AliveCount = 1 });
            var unit = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishArmyGroupMembershipComponent), typeof(UnitHealth));
            em.SetComponentData(unit, new SkirmishAttemptOwnedComponent { FactionId = 1 });
            em.SetComponentData(unit, new SkirmishArmyGroupMembershipComponent { GroupId = id });
            em.SetComponentData(unit, new UnitHealth { Current = 50, Max = 100 });
        }
        Assert.IsTrue(SkirmishExpandedPresentedOrders.TryTogglePresentedGroup(em, session, 1));
        Assert.IsTrue(SkirmishExpandedPresentedOrders.TryTogglePresentedGroup(em, session, 8));
        Assert.AreEqual(2, SkirmishArmySelectionService.SelectedLivingCount(em, session));
        Assert.IsTrue(SkirmishExpandedPresentedOrders.TryTogglePresentedGroup(em, session, 1));
        Assert.AreEqual(1, SkirmishArmySelectionService.SelectedLivingCount(em, session));
        Assert.IsTrue(SkirmishArmyGroupSystem.TryGet(em, session, 8, out var eighth));
        Assert.AreEqual(1, eighth.Selected);
        var roster = new List<SkirmishArmyDrawerSlot>();
        Assert.AreEqual(5, SkirmishArmyDrawerProjection.Project(em, session, roster));
        Assert.AreEqual(8, roster.Count);
        Assert.AreEqual(8, roster[7].GroupId);
        Assert.AreEqual(.5f, roster[7].Health01);
        Assert.AreEqual(0, em.GetComponentData<SkirmishArmySelectionComponent>(session).PageIndex);
        Assert.AreEqual(5, em.GetBuffer<SkirmishArmyDrawerSlot>(session).Length);
    }

    [Test]
    public void RuntimeBindPreservesExpandedStableIdentityRelay()
    {
        var previous = World.DefaultGameObjectInjectionWorld;
        using var world = new World("SquadScrollRealBind");
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            Game.UI.Shell.Ecs.UiShellEcsGateway.RegisterAsRuntimeGateway();
            var em = world.EntityManager;
            var session = em.CreateEntity(typeof(SkirmishMatchState), typeof(SkirmishExpandedSessionComponent), typeof(SkirmishArmySelectionComponent));
            em.SetComponentData(session, new SkirmishMatchState { Phase = SkirmishPhase.Playing });
            em.SetComponentData(session, new SkirmishArmySelectionComponent { PageSize = 5 });
            em.AddBuffer<SkirmishArmyGroupRecord>(session);
            Assert.IsTrue(UiShellRuntimeGateway.TryReadSkirmish(out var model) && model.Expanded,
                "Fixture must exercise the actual expanded-session bind branch.");
            using var fixture = new Fixture(8);
            uint selected = 0; int legacy = 0;
            fixture.View.ConfigureExpandedGroupSelection(id => selected = id);
            fixture.View.Bind(_ => legacy++);
            var card = fixture.Card(1);
            card.GetComponent<MatchHudSquadCardPressRelay>().OnPointerDown(fixture.Pointer());
            card.onClick.Invoke();
            Assert.AreEqual(1, selected, "Real Bind must preserve the stable group-ID pointer relay.");
            Assert.AreEqual(0, legacy, "Expanded cards must not fall through to category listeners.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            Game.UI.Shell.Ecs.UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    public static void RunCombinedValidation()
    {
        try
        {
            RunFocusedValidation();
            Game.Editor.SquadScrollCompatibilityValidation.RunFocusedValidation();
            Debug.Log("[SquadScrollCombined] result=Passed");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogError("[SquadScrollCombined] result=Failed " + error);
            EditorApplication.Exit(2);
        }
    }

    public static void RunFocusedValidation()
    {
        try
        {
            Game.Configs.GameLocalization.SetLocale("en", false);
            MatchHudV3PrefabBuilder.BuildSquadScroll();
            var suite = new MatchHudSquadScrollTests();
            foreach (int count in new[] { 1, 2, 5, 8, 21 }) suite.CardWidthAndCommandRailStayFixed(count);
            suite.SwipingDoesNotTapAndPointerIdentityCannotChangeMidPress();
            suite.OverflowTargetsRequireSwipeBeforeTapAndClampAfterRosterShrinks();
            suite.PausedRosterCardsCannotSelect();
            suite.CardToggleDeselectsOnlyThatGroupAndContinuousProjectionRetainsLogicalPage();
            suite.RuntimeBindPreservesExpandedStableIdentityRelay();
            var existing = new MatchHudSquadTrayQuickSelectTests();
            existing.RtlPaginationKeepsBothArrowHitAreasInsideTheNavigationRow();
            existing.MatchHudSquadTrayCardsAreButtons();
            existing.MatchHudSquadTrayCardsUseDedicatedPortraitSprites();
            existing.MatchHudSquadTrayViewHasSerializedReferences();
            existing.MatchHudSquadTrayCardClick_EmitsPrimaryClickAudio();
            Game.Tests.Editor.SkirmishExpandedArmyTests.RunFocusedValidation();
            MatchHudV3PrefabBuilder.CaptureSquadScrollReview();
            Debug.Log("[SquadScrollValidation] result=Passed layoutCases=5 inputCases=3 groupToggleAndProjection=true existingPortraitAndSelectionChecks=true");
        }
        catch (Exception error) { Debug.LogException(error); throw; }
    }
}
