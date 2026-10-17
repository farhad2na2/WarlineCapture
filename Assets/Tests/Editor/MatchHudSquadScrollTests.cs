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

    // Native UI controls: normal InputSystem touch, never direct scroll/click dispatch.
    private static Vector2 GapScreenPoint(Fixture fixture)
    {
        var a = (RectTransform)fixture.Card(4).transform;
        var b = (RectTransform)fixture.Card(5).transform;
        return RectTransformUtility.WorldToScreenPoint(null,
            (a.TransformPoint(a.rect.center) + b.TransformPoint(b.rect.center)) * .5f);
    }

    private static GameObject TopGapHit(EventSystem events, Vector2 point)
    {
        var hits = new List<RaycastResult>();
        events.RaycastAll(new PointerEventData(events) { position = point }, hits);
        return hits.Count > 0 ? hits[0].gameObject : null;
    }

    private static System.Collections.IEnumerator PumpNormalGesture(
        AriaTouchInputUiSystemHelper hand, Vector2 from, Vector2 to)
    {
        uint completed = hand.CompletedGestures;
        Assert.IsTrue(hand.TryGesture(from, to, .15f, from.Equals(to) ? 0 : .4f, Time.unscaledTime));
        for (int frame = 0; frame < 240 && hand.IsBusy; frame++)
        {
            hand.Tick(Time.unscaledTime);
            yield return null; // Actual InputSystem, EventSystem and ScrollRect player-loop frames.
        }
        yield return null; yield return null; // Let release/click and LateUpdate finish naturally.
        Assert.IsFalse(hand.IsBusy, "Bounded actual touch gesture must complete.");
        Assert.AreEqual(completed + 1, hand.CompletedGestures);
        Assert.AreEqual(0, hand.UnexpectedSamples);
        Assert.AreEqual(0, hand.PhysicalInterventions);
    }

    private System.Collections.IEnumerator NativeViewportGapDragReachesAndTapsFifthSlot(string locale)
    {
        string previousLocale = Game.Configs.GameLocalization.CurrentLocaleCode;
        var previousEvents = EventSystem.current;
        var previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("SquadGapRealInput");
        UnityEngine.InputSystem.DefaultInputActions ownedActions = null;
        var actionReferences = new List<UnityEngine.InputSystem.InputActionReference>();
        var canvasObject = new GameObject("SquadGapNativeCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        try
        {
            Game.Configs.GameLocalization.SetLocale(locale, false);
            World.DefaultGameObjectInjectionWorld = world;
            Game.UI.Shell.Ecs.UiShellEcsGateway.RegisterAsRuntimeGateway();
            var em = world.EntityManager;
            var session = em.CreateEntity(typeof(SkirmishMatchState), typeof(SkirmishExpandedSessionComponent), typeof(SkirmishArmySelectionComponent));
            em.SetComponentData(session, new SkirmishMatchState { Phase = SkirmishPhase.Playing });
            em.SetComponentData(session, new SkirmishArmySelectionComponent { PageSize = 5 });
            em.AddBuffer<SkirmishArmyGroupRecord>(session);
            for (uint id = 1; id <= 10; id++)
            {
                em.GetBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord { GroupId = id, FactionId = 1, Role = SkirmishRoleKind.Rifle, AliveCount = 1 });
                var unit = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishArmyGroupMembershipComponent), typeof(UnitHealth));
                em.SetComponentData(unit, new SkirmishAttemptOwnedComponent { FactionId = 1 });
                em.SetComponentData(unit, new SkirmishArmyGroupMembershipComponent { GroupId = id });
                em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
            }
            Assert.IsTrue(UiShellRuntimeGateway.TryReadSkirmish(out var context) && context.Expanded);
            using var fixture = new Fixture(10);
            fixture.Root.transform.SetParent(canvasObject.transform, false);
            fixture.View.ConfigureExpandedPagination(delta =>
                Assert.IsTrue(UiShellRuntimeGateway.TryChangeExpandedSquadPage(delta)), null);
            fixture.Refresh();
            var events = fixture.Events.GetComponent<EventSystem>();
            fixture.Events.SetActive(false);
            var module = fixture.Events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            ownedActions = new UnityEngine.InputSystem.DefaultInputActions();
            Func<UnityEngine.InputSystem.InputAction, UnityEngine.InputSystem.InputActionReference> reference = action =>
            {
                var value = UnityEngine.InputSystem.InputActionReference.Create(action);
                actionReferences.Add(value); return value;
            };
            module.actionsAsset = ownedActions.asset;
            module.cancel = reference(ownedActions.UI.Cancel); module.submit = reference(ownedActions.UI.Submit);
            module.move = reference(ownedActions.UI.Navigate); module.leftClick = reference(ownedActions.UI.Click);
            module.rightClick = reference(ownedActions.UI.RightClick); module.middleClick = reference(ownedActions.UI.MiddleClick);
            module.point = reference(ownedActions.UI.Point); module.scrollWheel = reference(ownedActions.UI.ScrollWheel);
            module.trackedDeviceOrientation = reference(ownedActions.UI.TrackedDeviceOrientation);
            module.trackedDevicePosition = reference(ownedActions.UI.TrackedDevicePosition);
            fixture.Events.SetActive(true);
            EventSystem.current = events;
            yield return null; yield return null;
            Assert.AreSame(events, EventSystem.current, "Fixture must own normal EventSystem routing.");
            Assert.AreSame(module, events.currentInputModule, "Real frame loop must activate the fixture input module.");
            var scroll = fixture.View.GetComponent<ScrollRect>();
            var viewport = scroll.viewport;
            var inputSurface = viewport.Find("SquadScrollInputSurface");
            Assert.NotNull(inputSurface, "Actual runtime viewport requires a gap hit surface.");
            var surface = inputSurface.GetComponent<Image>();
            uint selected = 0;
            fixture.View.ConfigureExpandedGroupSelection(group =>
            {
                Assert.IsTrue(UiShellRuntimeGateway.TrySelectExpandedGroup(group));
                selected = group;
            });
            Canvas.ForceUpdateCanvases();

            Vector2 gap = GapScreenPoint(fixture);
            Assert.IsTrue(Screen.safeArea.Contains(gap));
            Assert.IsTrue(RectTransformUtility.RectangleContainsScreenPoint(viewport, gap, null));
            var gapHit = TopGapHit(events, gap);
            Assert.AreEqual(surface.gameObject, gapHit, "Real card gap must hit only its invisible viewport surface.");
            Assert.AreEqual(scroll, gapHit.GetComponentInParent<ScrollRect>());
            // Same native gap with surface disabled is the original missing-raycast regression.
            surface.raycastTarget = false;
            Assert.IsNull(TopGapHit(events, gap), "Actual prefab gap must reproduce the old empty raycast.");
            surface.raycastTarget = true;

            var card = fixture.Card(1);
            var cardRect = (RectTransform)card.transform;
            var cardPoint = RectTransformUtility.WorldToScreenPoint(null, cardRect.TransformPoint(cardRect.rect.center));
            Assert.AreEqual(card, TopGapHit(events, cardPoint).GetComponentInParent<Button>(),
                "The new surface must not steal a presented card's tap priority.");

            using var hand = new AriaTouchInputUiSystemHelper();
            Assert.IsTrue(hand.Start());
            bool rtl = Game.UI.Runtime.UiShellRuntimeGateway.Localization.IsRightToLeft;
            Assert.AreEqual(locale == "fa-IR", rtl, "Both actual RTL and LTR input geometry must be exercised.");
            var left = RectTransformUtility.WorldToScreenPoint(null, viewport.TransformPoint(new Vector3(viewport.rect.xMin, viewport.rect.center.y)));
            var right = RectTransformUtility.WorldToScreenPoint(null, viewport.TransformPoint(new Vector3(viewport.rect.xMax, viewport.rect.center.y)));
            var destination = gap + Vector2.right * (right.x - left.x) * (rtl ? .56f : -.56f);
            Assert.IsTrue(Screen.safeArea.Contains(destination));
            Assert.IsTrue(RectTransformUtility.RectangleContainsScreenPoint(viewport, destination, null));
            var before = scroll.content.anchoredPosition;
            var gesture = PumpNormalGesture(hand, gap, destination);
            while (gesture.MoveNext()) yield return null;
            Assert.Greater(Vector2.Distance(before, scroll.content.anchoredPosition), 5,
                "A normal touch starting in empty space must actually move the roster.");
            Assert.AreEqual(0, selected, "Dragging a gap must not select a group.");
            Assert.AreEqual(1, fixture.View.LogicalPageIndex);
            Assert.AreEqual(10u, fixture.View.ExpandedSlotGroupId(4));
            var target = MatchHudSquadTouchNavigation.SlotTarget(fixture.View, 4);
            Assert.IsTrue(target.Available, "Last-page fifth card must expose its actual swipe or already-presented tap.");
            for (int align = 0; align < 8 && target.Available && target.Drag; align++)
            {
                gesture = PumpNormalGesture(hand, target.Position, target.DragEnd);
                while (gesture.MoveNext()) yield return null;
                Assert.AreEqual(0, selected);
                target = MatchHudSquadTouchNavigation.SlotTarget(fixture.View, 4);
            }
            Assert.IsTrue(target.Available && !target.Drag, "Physical alignment must expose the actual fifth-card tap.");
            gesture = PumpNormalGesture(hand, target.Position, target.Position);
            while (gesture.MoveNext()) yield return null;
            Assert.AreEqual(10u, selected, "Only an ordinary touch on the now-presented original group may select it.");
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            if (previousEvents != null) EventSystem.current = previousEvents;
            foreach (var reference in actionReferences) Object.DestroyImmediate(reference);
            ownedActions?.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
            Game.UI.Shell.Ecs.UiShellEcsGateway.RegisterAsRuntimeGateway();
            Game.Configs.GameLocalization.SetLocale(previousLocale, false);
        }
    }

    private static System.Collections.IEnumerator gapNativeCases;
    private static int gapNativeFrame=-1;
    private const string GapNativeKey = "Warline.SquadGapNative.20261016";

    private static void BeginGapNativeValidation(bool normalJourney)
    {
        string token = Environment.GetEnvironmentVariable("WARLINE_SQUAD_GAP_NATIVE_RUN");
        if (string.IsNullOrEmpty(token) || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Fresh explicit native UI validation required.");
        var scenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
        SessionState.SetInt(GapNativeKey+".sceneCount",scenes.Length);
        for(int i=0;i<scenes.Length;i++)
        {
            SessionState.SetString(GapNativeKey+".scenePath"+i,scenes[i].path);
            SessionState.SetBool(GapNativeKey+".sceneLoaded"+i,scenes[i].isLoaded);
            SessionState.SetBool(GapNativeKey+".sceneActive"+i,scenes[i].isActive);
        }
        var startScene=UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
        SessionState.SetString(GapNativeKey+".startScene", startScene==null ? "" : AssetDatabase.GetAssetPath(startScene));
        SessionState.SetString(GapNativeKey+".token", token);
        SessionState.SetBool(GapNativeKey, true);
        SessionState.SetBool(GapNativeKey+".normalJourney", normalJourney);
        SessionState.SetInt(GapNativeKey+".stage", 0);
        SessionState.SetInt(GapNativeKey+".exit", 1);
        SessionState.SetFloat(GapNativeKey+".deadline", (float)EditorApplication.timeSinceStartup+180);
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=null;
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        // A live fixture camera keeps the normal camera-less-preview redirect
        // from loading the full Menu scene during this isolated UI control.
        new GameObject("SquadGapTestStartupCamera",typeof(Camera));
        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        SessionState.SetInt(GapNativeKey+".size",(int)gameView.GetType().GetProperty("selectedSizeIndex",flags).GetValue(gameView));
        SessionState.SetBool(GapNativeKey+".gizmos",(bool)gameView.GetType().GetProperty("drawGizmos",flags).GetValue(gameView));
        typeof(Game.Editor.MainMenuV3PrefabBuilder).GetMethod("SetGameViewResolution",BindingFlags.Static|BindingFlags.NonPublic)
            .Invoke(null,new object[]{2400,1080});
        gameView.Show();gameView.Focus();
        ResumeGapNativeValidation();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void ResumeGapNativeValidation()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;
        string token=Environment.GetEnvironmentVariable("WARLINE_SQUAD_GAP_NATIVE_RUN");
        if (!SessionState.GetBool(GapNativeKey,false) || string.IsNullOrEmpty(token) ||
            token!=SessionState.GetString(GapNativeKey+".token","")) return;
        EditorApplication.update-=TickGapNativeValidation;
        EditorApplication.update+=TickGapNativeValidation;
    }

    private static void TickGapNativeValidation()
    {
        int stage=SessionState.GetInt(GapNativeKey+".stage",0);
        if (stage < 2 && EditorApplication.timeSinceStartup > SessionState.GetFloat(GapNativeKey+".deadline",0))
        {
            (gapNativeCases as IDisposable)?.Dispose(); gapNativeCases=null;
            SessionState.SetInt(GapNativeKey+".stage",2); SessionState.SetInt(GapNativeKey+".exit",1);
            Debug.LogError("[SquadGapTouchValidation] result=Failed reason=realFrameDeadline");
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
            return;
        }
        if (stage==0 && EditorApplication.isPlaying)
        {
            if (Time.frameCount<2 || Screen.width!=2400 || Screen.height!=1080)
            {
                if (EditorApplication.timeSinceStartup<SessionState.GetFloat(GapNativeKey+".deadline",0)) return;
                SessionState.SetInt(GapNativeKey+".stage",2);
                SessionState.SetInt(GapNativeKey+".exit",1);
                Debug.LogError("Native GameView never produced a screen surface.");
                EditorApplication.ExitPlaymode();
                return;
            }
            gapNativeCases=RunGapNativeCases();
            gapNativeFrame=-1;
            SessionState.SetInt(GapNativeKey+".stage",1);
            return;
        }
        if(stage==1 && EditorApplication.isPlaying)
        {
            if(gapNativeFrame==Time.frameCount)return;
            gapNativeFrame=Time.frameCount;
            try
            {
                if(gapNativeCases.MoveNext())return;
                SessionState.SetInt(GapNativeKey+".exit",0);
            }
            catch(Exception error)
            {
                SessionState.SetInt(GapNativeKey+".exit",1);
                Debug.LogException(error);
            }
            (gapNativeCases as IDisposable)?.Dispose();
            gapNativeCases=null;
            SessionState.SetInt(GapNativeKey+".stage",2);
            EditorApplication.ExitPlaymode();
            return;
        }
        if (stage!=2 || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update-=TickGapNativeValidation;
        SessionState.SetBool(GapNativeKey,false);
        int exit=SessionState.GetInt(GapNativeKey+".exit",1);
        bool restored=false;
        try
        {
            int count=SessionState.GetInt(GapNativeKey+".sceneCount",0);
            var scenes=new UnityEditor.SceneManagement.SceneSetup[count];
            for(int i=0;i<count;i++)
                scenes[i]=new UnityEditor.SceneManagement.SceneSetup
                {path=SessionState.GetString(GapNativeKey+".scenePath"+i,""),
                 isLoaded=SessionState.GetBool(GapNativeKey+".sceneLoaded"+i,false),
                 isActive=SessionState.GetBool(GapNativeKey+".sceneActive"+i,false)};
            if(scenes.Length>0) UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(scenes);
            string start=SessionState.GetString(GapNativeKey+".startScene","");
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=
                string.IsNullOrEmpty(start) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(start);
            var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            gameView.GetType().GetProperty("selectedSizeIndex",flags).SetValue(gameView,SessionState.GetInt(GapNativeKey+".size",0));
            gameView.GetType().GetProperty("drawGizmos",flags).SetValue(gameView,SessionState.GetBool(GapNativeKey+".gizmos",false));
            gameView.Repaint();
            var actual=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
            restored=actual.Length==scenes.Length && scenes.Select((scene,i) =>
                scene.path==actual[i].path && scene.isLoaded==actual[i].isLoaded && scene.isActive==actual[i].isActive).All(value=>value);
            var actualStart=UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
            restored &= (actualStart==null ? "" : AssetDatabase.GetAssetPath(actualStart))==start;
            restored &= (int)gameView.GetType().GetProperty("selectedSizeIndex",flags).GetValue(gameView)==SessionState.GetInt(GapNativeKey+".size",0);
            restored &= (bool)gameView.GetType().GetProperty("drawGizmos",flags).GetValue(gameView)==SessionState.GetBool(GapNativeKey+".gizmos",false);
            if(!restored)throw new InvalidOperationException("Exact native fixture scene/start-scene/GameView restoration required");
        }
        catch(Exception error) { exit=1;Debug.LogException(error); }
        Debug.Log("[SquadGapNativeExit] result="+(exit==0 ? "Passed" : "Failed")+" restored="+(restored ? "1" : "0"));
        if(exit!=0 || !restored) { EditorApplication.Exit(exit!=0 ? exit : 1); return; }
        Debug.Log("[SquadGapTouchValidation] result=Passed locales=2 normalPointerRouting=1 missionReady=0");
        if(!SessionState.GetBool(GapNativeKey+".normalJourney",false)) { EditorApplication.Exit(0); return; }
        try
        {
            Game.Tests.Editor.SkirmishS005OpeningViewPolicyTests.RunFocusedValidation();
            Game.Editor.SkirmishS002AriaRunHarness.RunS005EditorAriaFaSeed3MainMenu();
        }
        catch(Exception error) { Debug.LogException(error);EditorApplication.Exit(2); }
    }

    private static System.Collections.IEnumerator RunGapNativeCases()
    {
        foreach(string locale in new[] { "en", "fa-IR" })
        {
            var steps=new MatchHudSquadScrollTests().NativeViewportGapDragReachesAndTapsFifthSlot(locale);
            try { while(steps.MoveNext())yield return null; }
            finally { (steps as IDisposable)?.Dispose(); }
        }
    }

    public static void RunGapInputFocusedValidation() => BeginGapNativeValidation(false);

    // Global namespace, Game.Tests.Editor owns tests -> Editor dependency direction.
    public static void RunGapInputCameraAndM5()
    {
        if (Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC") == "1")
            throw new InvalidOperationException("Full normal M5 must not enable diagnostic180 termination.");
        BeginGapNativeValidation(true);
    }

}
