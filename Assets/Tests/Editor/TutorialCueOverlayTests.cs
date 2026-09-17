using System.Reflection;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public sealed class TutorialCueOverlayTests
{
    public static void ValidateWedgeFraming()
    {
        new TutorialCueOverlayTests().WedgeGuideFramesOnlyTheVisibleSector();
        new TutorialCueOverlayTests().MinimapLeavesRoomForBottomCornerArrow();
        new TutorialCueOverlayTests().WaitAreaDoesNotResembleAClickTargetAndRestoresTargetStyle();
        Debug.Log("[TutorialWedgeFraming] result=Passed four sectors, hit geometry and crowded minimap corner");
    }

    [Test]
    public void WaitAreaDoesNotResembleAClickTargetAndRestoresTargetStyle()
    {
        var helper = new AssistantHighlightPresentationSystemHelper();
        try
        {
            helper.ShowTutorialArea(Vector3.zero, 3, defensive: true);
            var root = (GameObject)typeof(AssistantHighlightPresentationSystemHelper)
                .GetField("_worldRingRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(helper);
            Assert.IsTrue(root.activeSelf);
            Assert.IsTrue(root.GetComponent<LineRenderer>().enabled);
            foreach (var line in root.GetComponentsInChildren<LineRenderer>())
                if (line.gameObject != root) Assert.IsFalse(line.enabled, "Waiting must not display click crosshairs or brackets.");
            helper.ShowTutorialWorld(Vector3.right);
            foreach (var line in root.GetComponentsInChildren<LineRenderer>())
                Assert.IsTrue(line.enabled, "The next real world action must restore its target markings.");
        }
        finally { helper.Unbind(); }
    }

    [Test]
    public void MinimapLeavesRoomForBottomCornerArrow()
    {
        // M3's 1920x1080 footer and minimap leave a free approach just left
        // of the map. Reserve the rotated arrow's whole bounce envelope.
        var target = new Rect(1720, 15, 185, 190);
        var safe = new Rect(8, 8, 1904, 1064);
        var obstacles = new[] { new Rect(1535,208,366,240), new Rect(0,0,1720,200), new Rect(1445,630,460,440) };
        Assert.IsTrue(TutorialTapPointerLayout.TryPlace(target, safe, Vector2.zero, 48, 8, 15,
            obstacles, out var arrow, out _, out _));
        var envelope = new Rect(arrow.center - Vector2.one * (48 * .707107f + 15),
            Vector2.one * (48 * .707107f + 15) * 2);
        Assert.IsTrue(safe.Contains(envelope.min) && safe.Contains(envelope.max));
        foreach (var obstacle in obstacles) Assert.IsFalse(envelope.Overlaps(obstacle));
    }

    [Test]
    public void NarrowFooterKeepsArrowWhenMapAndCancellationBannerBlockDifferentAxes()
    {
        var target = new Rect(1142, 4, 132, 134);
        var safe = new Rect(6, 6, 1268, 708);
        var obstacles = new[] {
            new Rect(0, 0, 1140, 132), new Rect(1025, 136, 243, 167),
            new Rect(978, 312, 284, 398), new Rect(550, 141, 408, 37)
        };
        const float size = 32.4f, travel = 10.08f;
        Assert.IsTrue(TutorialTapPointerLayout.TryPlace(target, safe, Vector2.zero, size, 6, travel,
            obstacles, out var arrow, out _, out _));
        float radius = size * .707107f + travel;
        var envelope = new Rect(arrow.center - Vector2.one * radius, Vector2.one * radius * 2);
        Assert.IsTrue(safe.Contains(envelope.min) && safe.Contains(envelope.max));
        foreach (var obstacle in obstacles) Assert.IsFalse(envelope.Overlaps(obstacle));
    }

    [Test]
    public void WedgeGuideFramesOnlyTheVisibleSector()
    {
        var root = new GameObject("Wedge framing", typeof(RectTransform), typeof(V3RadialWedgeGraphic));
        try
        {
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(600,600);
            var wedge = root.GetComponent<V3RadialWedgeGraphic>();
            var corners = new Vector3[4];
            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                wedge.Configure(quadrant * 90 + 1, 88, .36f, 1f, Color.blue, Color.blue, Color.white, 3, true);
                wedge.GetGuidanceWorldCorners(corners);
                var size = corners[2] - corners[0];
                Assert.Less(size.x, 310, "Frame must not enclose the entire 600-pixel wheel.");
                Assert.Less(size.y, 310);
                Vector3 center = (corners[0] + corners[2]) * .5f;
                Assert.Greater(center.magnitude, 200, "Guide must leave the portrait hole.");
                Assert.IsTrue(wedge.Raycast(RectTransformUtility.WorldToScreenPoint(null, center), null),
                    "Frame center must identify a point on this clickable sector.");
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void NextClickFrameIsAbovePopupAndNeverBlocksItsButton()
    {
        var root=new GameObject("HUD",typeof(RectTransform),typeof(Canvas));
        var popup=new GameObject("Popup",typeof(RectTransform),typeof(Canvas)); popup.transform.SetParent(root.transform,false);
        var pulse=new GameObject("Pulse",typeof(RectTransform),typeof(Image));pulse.transform.SetParent(root.transform,false);
        var button=new GameObject("Next",typeof(RectTransform),typeof(Image),typeof(Button));button.transform.SetParent(popup.transform,false);
        var helper=new AssistantHighlightPresentationSystemHelper();
        try
        {
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            popup.GetComponent<Canvas>().overrideSorting=true;popup.GetComponent<Canvas>().sortingOrder=200;
            helper.Bind(pulse.GetComponent<Image>());
            var target=button.GetComponent<Button>(); int clicks=0; target.onClick.AddListener(()=>clicks++);
            helper.ShowTutorialControl(target,"tutorial.next.place");
            var field=typeof(AssistantHighlightPresentationSystemHelper).GetField("_screenTargetIndicator",BindingFlags.Instance|BindingFlags.NonPublic);
            var cue=(RectTransform)field.GetValue(helper);
            Assert.That(cue.gameObject.activeSelf,Is.True);
            Assert.That(cue.GetComponent<Canvas>().sortingOrder,Is.GreaterThan(200));
            Assert.That(cue.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
            foreach(var graphic in cue.GetComponentsInChildren<Graphic>()) Assert.That(graphic.raycastTarget,Is.False);
            target.onClick.Invoke(); Assert.That(clicks,Is.EqualTo(1));
            var aria=new GameObject("ARIA",typeof(RectTransform),typeof(AriaTutorialBriefingView));
            aria.transform.SetParent(root.transform,false);
            button.transform.SetParent(aria.transform,false);
            helper.ShowTutorialControl(target,"tutorial.next.place");
            var label=(TMPro.TMP_Text)typeof(AssistantHighlightPresentationSystemHelper)
                .GetField("_screenTargetLabel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(helper);
            Assert.That(cue.gameObject.activeSelf,Is.True,"Keep ARIA's next-click outline.");
            Assert.That(label.transform.parent.gameObject.activeSelf,Is.True,"ARIA must retain the localized next-click caption.");
            // Plan B places the caption beside the arrow on whichever side has room;
            // it is intentionally independent of a narrow button's width.
            Assert.That(((RectTransform)label.transform.parent).rect.width,Is.GreaterThan(0));
            var buttonCorners = new Vector3[4]; var cueCorners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(buttonCorners); cue.GetWorldCorners(cueCorners);
            for (int i = 0; i < 4; i++)
                Assert.Less(Vector3.Distance(buttonCorners[i], cueCorners[i]), 18, "Outline must follow the button, with only the intentional outer padding.");
            button.transform.SetParent(popup.transform,false);
            helper.ShowTutorialControl(target,"tutorial.next.place");
            Assert.That(label.transform.parent.gameObject.activeSelf,Is.True,"Restore captions for battlefield/build controls.");
            target.interactable=false;helper.ShowTutorialControl(target,"tutorial.next.place");
            Assert.That(cue.gameObject.activeSelf,Is.False,"Never direct the player to an unavailable control.");
            helper.ShowTutorialWorld(new Vector3(120,0,30));
            Assert.That(cue.gameObject.activeSelf,Is.False,"World destination uses the world marker, not a stale UI rectangle.");
            helper.ClearDirectTutorialCue();
        }
        finally {helper.Unbind();UnityEngine.Object.DestroyImmediate(root);}
    }
}
