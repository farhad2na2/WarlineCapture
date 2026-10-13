#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    public sealed class MatchHudSquadNativeScrollTests
    {
        [UnityTest] public IEnumerator EnglishRosterUsesPhysicalSwipesAndCardToggles() => Run("en");
        [UnityTest] public IEnumerator PersianRosterUsesPhysicalSwipesAndCardToggles() => Run("fa-IR");

        // Rendered UI input fixture only: no mission state, gameplay outcome or
        // troop order is injected, and this is not a mission-readiness witness.
        private static IEnumerator Run(string locale)
        {
            string oldLocale = GameLocalization.CurrentLocaleCode;
            var selected = new HashSet<uint> { 7 };
            int cardCallbacks = 0, swipes = 0;
            GameObject canvasObject = null, inputObject = null, cameraObject = null;
            AriaTouchInputUiSystemHelper touch = null;
            try
            {
                GameLocalization.SetLocale(locale, false);
                if (EventSystem.current == null)
                {
                    inputObject = new GameObject("SquadScrollInputFixture", typeof(EventSystem));
                    var module = inputObject.AddComponent<InputSystemUIInputModule>();
                    module.AssignDefaultActions();
                }
                canvasObject = new GameObject("SquadScrollCanvasFixture", typeof(RectTransform),
                    typeof(Canvas), typeof(GraphicRaycaster));
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.sortingOrder = 31000;
                if (locale == "fa-IR")
                {
                    cameraObject = new GameObject("SquadScrollCameraFixture", typeof(Camera));
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cameraObject.GetComponent<Camera>(); canvas.planeDistance = 10;
                }
                else canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
                Assert.IsNotNull(prefab);
                var hud = UnityEngine.Object.Instantiate(prefab, canvasObject.transform, false);
                var root = (RectTransform)hud.transform;
                root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
                root.offsetMin = root.offsetMax = Vector2.zero;
                // Freeze unrelated HUD presenters in this isolated UI fixture. Button,
                // ScrollRect and pointer-relay components still receive normal input.
                foreach (var view in hud.GetComponentsInChildren<MonoBehaviour>(true))
                    if (view != null && view.GetType().Namespace == "Game.UI.Runtime" &&
                        view.GetType().Name.EndsWith("View", StringComparison.Ordinal)) view.enabled = false;
                var tray = hud.GetComponentInChildren<MatchHudSquadTrayView>(true);
                Assert.IsNotNull(tray);
                var rect = (RectTransform)tray.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(620, 210);
                tray.ConfigureExpandedPagination(_ => { }, null);
                tray.ConfigureExpandedGroupSelection(group =>
                {
                    cardCallbacks++;
                    if (!selected.Add(group)) selected.Remove(group);
                });
                tray.ConfigureExpandedRoster(destination =>
                {
                    destination.Clear();
                    for (uint id = 1; id <= 18; id++)
                        destination.Add(new UiMatchHudSquadTrayCardModel(true, "SQUAD " + id,
                            "4", 1, id, 4, (int)id, selected.Contains(id)));
                }, _ => null);
                var refresh = typeof(MatchHudSquadTrayView).GetMethod("RefreshScrollingRoster",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(refresh);
                void Refresh() { refresh.Invoke(tray, new object[] { true }); Canvas.ForceUpdateCanvases(); }
                Refresh(); yield return null; Refresh();
                touch = new AriaTouchInputUiSystemHelper();
                Assert.IsTrue(touch.Start(), "Native input must start without physical intervention.");
                // A slightly clipped fifth card needs a short preparation swipe,
                // not a jump into another logical roster window.
                var clipped = MatchHudSquadTouchNavigation.GroupTarget(tray, 5);
                Assert.IsTrue(clipped.Available && clipped.Drag,
                    "The fifth card must be partly clipped in this rendered fixture.");
                int callbacksBeforeClip = cardCallbacks;
                yield return Gesture(clipped); Refresh(); swipes++;
                Assert.AreEqual(callbacksBeforeClip, cardCallbacks,
                    "A short visibility swipe must not toggle the card under its contact.");
                var fifth = MatchHudSquadTouchNavigation.GroupTarget(tray, 5);
                Assert.IsTrue(fifth.Available && !fifth.Drag,
                    "The short swipe must present the intended fifth card fully.");
                Assert.AreEqual(clipped.ScrollTargetId, fifth.Id,
                    "Scrolling must preserve the intended group identity.");
                Assert.AreEqual(0, tray.LogicalPageIndex,
                    "Revealing a slightly clipped card must not skip its original window.");
                yield return Gesture(fifth); Refresh();
                Assert.IsTrue(selected.Contains(5), "The revealed card tap must select group five.");
                fifth = MatchHudSquadTouchNavigation.GroupTarget(tray, 5);
                Assert.IsTrue(fifth.Available && !fifth.Drag);
                yield return Gesture(fifth); Refresh();
                Assert.IsFalse(selected.Contains(5), "The same card must deselect group five.");
                Assert.AreEqual(callbacksBeforeClip + 2, cardCallbacks);

                var first = MatchHudSquadTouchNavigation.GroupTarget(tray, 18);
                Assert.IsTrue(first.Available && first.Drag, "Distant squad must require a real swipe.");
                Assert.AreEqual(locale != "en", first.DragEnd.x > first.Position.x,
                    "Swipe direction must follow the rendered RTL/LTR roster.");

                AriaTouchTarget observedTarget = default;
                bool tapped = false;
                for (int attempt = 0; attempt < 30 && !tapped; attempt++)
                {
                    Refresh();
                    yield return ObserveReachable(() => MatchHudSquadTouchNavigation.GroupTarget(tray, 18), 18);
                    var target = observedTarget;
                    int before = cardCallbacks;
                    yield return Gesture(target);
                    Refresh();
                    if (target.Drag)
                    { swipes++; Assert.AreEqual(before, cardCallbacks, "Swiping must not select a squad."); }
                    else tapped = true;
                }
                Assert.IsTrue(tapped && selected.Contains(18), "Actual card tap must select the intended squad.");
                Assert.Greater(swipes, 3, "Long roster must exercise more than the old three-attempt limit.");

                // Deselect every current group through its own card, including the
                // first selection now off-screen. No Clear or paging button is used.
                for (int attempt = 0; attempt < 60 && selected.Count > 0; attempt++)
                {
                    Refresh();
                    yield return ObserveReachable(() => MatchHudSquadTouchNavigation.SelectedCardTarget(tray), 0);
                    var target = observedTarget;
                    int before = cardCallbacks;
                    yield return Gesture(target);
                    Refresh();
                    if (target.Drag)
                    { swipes++; Assert.AreEqual(before, cardCallbacks, "Reset navigation must not toggle a hidden card."); }
                }
                Assert.IsEmpty(selected, "All deselections must be achieved by normal card taps.");
                var page = MatchHudSquadTouchNavigation.PageTarget(tray, false);
                Assert.IsTrue(page.Available && page.Drag, "Previous logical window must be reached with a swipe.");
                int callbacksBeforePage = cardCallbacks;
                yield return Gesture(page); Refresh();
                Assert.AreEqual(callbacksBeforePage, cardCallbacks);
                Assert.AreEqual(touch.DispatchedGestures, touch.CompletedGestures);
                Assert.AreEqual(0u, touch.UnexpectedSamples);
                Assert.AreEqual(0u, touch.PhysicalInterventions);
                Debug.Log("[SquadScrollNativeInput] result=Passed locale=" + locale +
                    " swipes=" + swipes + " pairedContacts=" + touch.CompletedGestures +
                    " cardToggles=" + cardCallbacks + " hiddenPaging=0 directScrollWrites=0 missionCertified=0");

                IEnumerator ObserveReachable(Func<AriaTouchTarget> read, uint groupId)
                {
                    // Graphic masks/raycast presentation update on normal UI frames.
                    // Observe a reachable control before dispatching the next contact.
                    float deadline = Time.realtimeSinceStartup + 2;
                    observedTarget = read();
                    while (!observedTarget.Available && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                        Refresh(); observedTarget = read();
                    }
                    Assert.IsTrue(observedTarget.Available,
                        "Only reachable presented geometry may be used. " + DescribeTarget(tray, groupId));
                }

                IEnumerator Gesture(AriaTouchTarget target)
                {
                    Assert.IsTrue(touch.TryGesture(target.Position, target.Drag ? target.DragEnd : target.Position,
                        .12f, target.Drag ? .4f : 0f, Time.unscaledTime));
                    float deadline = Time.realtimeSinceStartup + 8;
                    while (touch.IsBusy)
                    {
                        Assert.Less(Time.realtimeSinceStartup, deadline, "Native contact completion timed out.");
                        touch.Tick(Time.unscaledTime);
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(.2f);
                }
            }
            finally
            {
                touch?.Dispose();
                if (canvasObject != null) UnityEngine.Object.Destroy(canvasObject);
                if (inputObject != null) UnityEngine.Object.Destroy(inputObject);
                if (cameraObject != null) UnityEngine.Object.Destroy(cameraObject);
                GameLocalization.SetLocale(oldLocale, false);
            }
        }
        private static string DescribeTarget(MatchHudSquadTrayView tray, uint groupId)
        {
            Button card; Vector2 from, to;
            bool exists = groupId == 0
                ? tray.TryGetFirstSelectedExpandedCardTarget(out groupId, out card, out from, out to)
                : tray.TryGetExpandedCardTarget(groupId, out card, out from, out to);
            var viewport = tray.ExpandedScrollViewport;
            var scroll = viewport != null ? viewport.GetComponentInParent<ScrollRect>() : null;
            var canvas = viewport != null ? viewport.GetComponentInParent<Canvas>()?.rootCanvas : null;
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var point = card != null
                ? RectTransformUtility.WorldToScreenPoint(camera,
                    ((RectTransform)card.transform).TransformPoint(((RectTransform)card.transform).rect.center)) : from;
            string HitsAt(Vector2 at)
            {
                var hits = new List<RaycastResult>();
                if (EventSystem.current != null)
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = at }, hits);
                var names = new List<string>();
                for (int i = 0; i < Mathf.Min(3, hits.Count); i++)
                    names.Add(hits[i].gameObject.name + "/button=" +
                        hits[i].gameObject.GetComponentInParent<Button>()?.name + "/scroll=" +
                        hits[i].gameObject.GetComponentInParent<ScrollRect>()?.name);
                return names.Count == 0 ? "none" : string.Join(",", names);
            }
            string cardBounds = "none";
            if (card != null && viewport != null)
            {
                var rect = (RectTransform)card.transform;
                cardBounds = viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.min)) + "/" +
                    viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.max));
            }
            return "group=" + groupId + " exists=" + exists + " fullCard=" + (card != null) +
                " active=" + (card != null && card.IsActive()) +
                " interactable=" + (card != null && card.IsInteractable()) + " page=" + tray.LogicalPageIndex +
                " distance=" + tray.ExpandedCardScrollDistance(groupId) + " scroll=" +
                (scroll != null ? scroll.horizontalNormalizedPosition.ToString() : "none") +
                " offset=" + (scroll != null ? scroll.content.anchoredPosition.ToString() : "none") +
                " viewport=" + (viewport != null ? viewport.rect.ToString() : "none") +
                " cardBounds=" + cardBounds + " canvas=" + canvas?.name + "/" + canvas?.renderMode +
                " camera=" + camera?.name + " screen=" + Screen.width + "x" + Screen.height +
                " safeArea=" + Screen.safeArea + " center=" + point + "/safe=" + Screen.safeArea.Contains(point) +
                "/hits=" + HitsAt(point) + " from=" + from + "/safe=" + Screen.safeArea.Contains(from) +
                "/hits=" + HitsAt(from) + " to=" + to + "/safe=" + Screen.safeArea.Contains(to) +
                "/hits=" + HitsAt(to);
        }
    }
}
#endif
