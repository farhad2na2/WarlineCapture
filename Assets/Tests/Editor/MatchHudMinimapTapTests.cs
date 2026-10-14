using System;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Tests.Editor
{
    public sealed class MatchHudMinimapTapTests
    {
        [Test]
        public void StationaryTouchOnPaddedViewportEdgeStillFocusesMap() => WithMap((view, rect) =>
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(60, 0, 0)));
            int focus = 0;
            view.FocusRequested += _ => focus++;
            Assert.IsTrue(view.TryBeginViewportDrag(point, null), "Keep the generous touch drag hit area.");
            Assert.IsFalse(view.DragViewport(point, null), "A stationary touch is not a drag.");
            Assert.IsFalse(view.DragViewport(point + Vector2.right, null), "One pixel of jitter must not consume a tap.");
            view.EndViewportDrag();
            view.OnPointerClick(new PointerEventData(EventSystem.current) { position = point });
            Assert.AreEqual(1, focus, "A map tap outside the visible viewport must focus the camera.");
            Assert.IsFalse(view.HasManualViewportOverride);
        });

        [Test]
        public void RealViewportDragFocusesOnceAndSuppressesFollowupClick() => WithMap((view, rect) =>
        {
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(Vector3.zero));
            Vector2 end = start + Vector2.right * 30;
            int focus = 0;
            view.FocusRequested += _ => focus++;
            Assert.IsTrue(view.TryBeginViewportDrag(start, null));
            Assert.IsTrue(view.DragViewport(end, null));
            view.EndViewportDrag();
            view.OnPointerClick(new PointerEventData(EventSystem.current) { position = end });
            Assert.AreEqual(1, focus);
            Assert.IsTrue(view.HasManualViewportOverride);
        });

        [Test]
        public void StationaryTapInsideViewportPreservesCurrentCamera() => WithMap((view, rect) =>
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(Vector3.zero));
            int focus = 0;
            view.FocusRequested += _ => focus++;
            Assert.IsTrue(view.TryBeginViewportDrag(point, null));
            Assert.IsFalse(view.DragViewport(point, null));
            view.EndViewportDrag();
            view.OnPointerClick(new PointerEventData(EventSystem.current) { position = point });
            Assert.AreEqual(0, focus);
        });

        [TestCase(.04f, .5f)]
        [TestCase(.96f, .5f)]
        [TestCase(.5f, .04f)]
        [TestCase(.5f, .96f)]
        public void EdgeDragFocusesTargetAndKeepsLogicalFootprint(float x, float y) => WithMap((view, rect) =>
        {
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(Vector3.zero));
            Vector2 goal = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(
                rect.rect.xMin + x * rect.rect.width, rect.rect.yMin + y * rect.rect.height, 0)));
            Vector2 focused = default;
            view.FocusRequested += point => focused = point;
            Assert.IsTrue(view.TryBeginViewportDrag(start, null));
            Assert.IsTrue(view.DragViewport(goal, null));
            Assert.That(focused.x, Is.EqualTo(x).Within(.001f));
            Assert.That(focused.y, Is.EqualTo(y).Within(.001f));
            Assert.That(view.ManualViewportNormalizedRect.width, Is.EqualTo(.25f).Within(.001f));
            Assert.That(view.ManualViewportNormalizedRect.height, Is.EqualTo(.25f).Within(.001f));
            Assert.That(Mathf.Min(view.ViewportRect.rect.width, view.ViewportRect.rect.height), Is.EqualTo(66f).Within(.1f));
            // A second sample in the same drag must retain its grab offset and
            // footprint even though the visible outline was clipped by the first.
            Vector2 next = Vector2.Lerp(start, goal, .9f);
            Assert.IsTrue(view.DragViewport(next, null));
            Assert.That(focused.x, Is.EqualTo(Mathf.Lerp(.5f, x, .9f)).Within(.001f));
            Assert.That(focused.y, Is.EqualTo(Mathf.Lerp(.5f, y, .9f)).Within(.001f));
            view.EndViewportDrag();
        });

        [TestCase(400f, 300f, 1f)]
        [TestCase(300f, 400f, 1f)]
        [TestCase(400f, 300f, 2f)]
        [TestCase(300f, 400f, .5f)]
        public void FullMapSpriteKeepsAllEdgesInsideClipAndFocusable(float width, float height, float aspect) => WithMap((view, rect) =>
        {
            var clip = new GameObject("MapClip", typeof(RectTransform), typeof(RectMask2D));
            clip.transform.SetParent(rect.parent, false);
            var clipRect = clip.GetComponent<RectTransform>();
            clipRect.sizeDelta = new Vector2(width, height);
            rect.SetParent(clipRect, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            var texture = new Texture2D(Mathf.RoundToInt(64f * aspect), 64);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f);
            try
            {
                view.SetMapSprite(sprite);
                LayoutRebuilder.ForceRebuildLayoutImmediate(clipRect);
                Canvas.ForceUpdateCanvases();
                foreach (var normalized in new[] { new Vector2(.04f, .5f), new Vector2(.96f, .5f),
                    new Vector2(.5f, .04f), new Vector2(.5f, .96f) })
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(
                        rect.rect.xMin + normalized.x * rect.rect.width,
                        rect.rect.yMin + normalized.y * rect.rect.height, 0)));
                    Assert.IsTrue(RectTransformUtility.RectangleContainsScreenPoint(clipRect, point, null),
                        "The entire tactical map must remain inside its clip, including legal edge sites.");
                    Vector2 focus = new(-1, -1);
                    Action<Vector2> observer = value => focus = value;
                    view.FocusRequested += observer;
                    view.OnPointerClick(new PointerEventData(EventSystem.current) { position = point });
                    view.FocusRequested -= observer;
                    Assert.That(focus.x, Is.EqualTo(normalized.x).Within(.001f));
                    Assert.That(focus.y, Is.EqualTo(normalized.y).Within(.001f));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture); }
        });

        [Test]
        public void CompactMapRetainsItsExistingAspectPolicy() => WithMap((view, rect) =>
        {
            view.SetProjectionMode(false);
            var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            var texture = new Texture2D(64, 64);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * .5f);
            try
            {
                view.SetMapSprite(sprite);
                Assert.That(fitter.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
            }
            finally { UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture); }
        });

        [Test]
        public void PresentedCommandPointsMatchLaidOutMapEdgesAndCenter() => WithMap((view, rect) =>
        {
            SetPresentedGrid(view, new MatchHudMinimapProjectionGrid(Vector3.zero, 100, 100));
            foreach (var world in new[] { Vector3.zero, new Vector3(50, 0, 50), new Vector3(100, 0, 100) })
            {
                Assert.IsTrue(view.TryGetPresentedMapPoint(world, null, out var point));
                Vector2 expected = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(
                    rect.rect.xMin + world.x / 100 * rect.rect.width, rect.rect.yMin + world.z / 100 * rect.rect.height, 0)));
                Assert.That(Vector2.Distance(point, expected), Is.LessThan(.01f));
            }
        });

        [TestCase(0f, 400f)]
        [TestCase(400f, 0f)]
        [TestCase(-400f, 400f)]
        public void UnlaidOutMapCannotAdvertisePresentedCommandPoint(float width, float height) => WithMap((view, rect) =>
        {
            SetPresentedGrid(view, new MatchHudMinimapProjectionGrid(Vector3.zero, 100, 100));
            rect.sizeDelta = new Vector2(width, height);
            Assert.IsFalse(view.TryGetPresentedMapPoint(new Vector3(50, 0, 50), null, out var point));
            Assert.That(point, Is.EqualTo(Vector2.zero));
        });

        [Test]
        public void HiddenOrDisabledMapCannotAdvertisePresentedCommandPoint() => WithMap((view, rect) =>
        {
            SetPresentedGrid(view, new MatchHudMinimapProjectionGrid(Vector3.zero, 100, 100));
            view.enabled = false;
            Assert.IsFalse(view.TryGetPresentedMapPoint(Vector3.zero, null, out _));
            view.enabled = true;
            rect.gameObject.SetActive(false);
            Assert.IsFalse(view.TryGetPresentedMapPoint(Vector3.zero, null, out _));
        });

        [Test]
        public void CollapsedRenderedMapCannotAdvertiseItsCenter() => WithMap((view, rect) =>
        {
            SetPresentedGrid(view, new MatchHudMinimapProjectionGrid(Vector3.zero, 100, 100));
            rect.localScale = new Vector3(0, 1, 1);
            Assert.That(rect.rect.width, Is.GreaterThan(0), "The local rect alone is insufficient.");
            Assert.IsFalse(view.TryGetPresentedMapPoint(new Vector3(50, 0, 50), null, out var point));
            Assert.That(point, Is.EqualTo(Vector2.zero));
        });

        [Test]
        public void NonfiniteProjectionCannotAdvertisePresentedCommandPoint() => WithMap((view, rect) =>
        {
            SetPresentedGrid(view, new MatchHudMinimapProjectionGrid(Vector3.zero, 100, 100));
            Assert.IsFalse(view.TryGetPresentedMapPoint(new Vector3(float.NaN, 0, 50), null, out _));
            Assert.IsFalse(view.TryGetPresentedMapPoint(new Vector3(float.PositiveInfinity, 0, 50), null, out _));
            SetPresentedGrid(view, new MatchHudMinimapProjectionGrid(Vector3.zero, float.PositiveInfinity, 100));
            Assert.IsFalse(view.TryGetPresentedMapPoint(Vector3.zero, null, out _));
        });

        private static void SetPresentedGrid(MatchHudMinimapView view, MatchHudMinimapProjectionGrid grid)
            => typeof(MatchHudMinimapView).GetField("PresentedProjection", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(view, grid);

        private static void WithMap(Action<MatchHudMinimapView, RectTransform> test)
        {
            var root = new GameObject("MinimapTapTest", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            try
            {
                var map = new GameObject("Map", typeof(RectTransform), typeof(Image));
                map.transform.SetParent(root.transform, false);
                var rect = map.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 400);
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(rect, false);
                var viewportRect = viewport.GetComponent<RectTransform>();
                viewportRect.sizeDelta = new Vector2(100, 100);
                var view = map.AddComponent<MatchHudMinimapView>();
                view.Configure(map.GetComponent<Image>(), rect, viewportRect, null, null, rect);
                view.ApplyInteractionOptions(true, true, true, true, false, false);
                Canvas.ForceUpdateCanvases();
                test(view, rect);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static void RunFocusedValidation()
        {
            var suite = new MatchHudMinimapTapTests();
            suite.StationaryTouchOnPaddedViewportEdgeStillFocusesMap();
            suite.RealViewportDragFocusesOnceAndSuppressesFollowupClick();
            suite.StationaryTapInsideViewportPreservesCurrentCamera();
            suite.EdgeDragFocusesTargetAndKeepsLogicalFootprint(.04f, .5f);
            suite.EdgeDragFocusesTargetAndKeepsLogicalFootprint(.96f, .5f);
            suite.EdgeDragFocusesTargetAndKeepsLogicalFootprint(.5f, .04f);
            suite.EdgeDragFocusesTargetAndKeepsLogicalFootprint(.5f, .96f);
            suite.FullMapSpriteKeepsAllEdgesInsideClipAndFocusable(400f, 300f, 1f);
            suite.FullMapSpriteKeepsAllEdgesInsideClipAndFocusable(300f, 400f, 1f);
            suite.FullMapSpriteKeepsAllEdgesInsideClipAndFocusable(400f, 300f, 2f);
            suite.FullMapSpriteKeepsAllEdgesInsideClipAndFocusable(300f, 400f, .5f);
            suite.CompactMapRetainsItsExistingAspectPolicy();
            suite.PresentedCommandPointsMatchLaidOutMapEdgesAndCenter();
            suite.UnlaidOutMapCannotAdvertisePresentedCommandPoint(0, 400);
            suite.UnlaidOutMapCannotAdvertisePresentedCommandPoint(400, 0);
            suite.UnlaidOutMapCannotAdvertisePresentedCommandPoint(-400, 400);
            suite.HiddenOrDisabledMapCannotAdvertisePresentedCommandPoint();
            suite.CollapsedRenderedMapCannotAdvertiseItsCenter();
            suite.NonfiniteProjectionCannotAdvertisePresentedCommandPoint();
            Debug.Log("[MatchHudMinimapTapTests] result=Passed tests=19");
        }
    }
}
