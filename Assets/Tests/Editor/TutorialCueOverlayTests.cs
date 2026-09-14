using System.Reflection;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public sealed class TutorialCueOverlayTests
{
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
            Assert.That(label.transform.parent.gameObject.activeSelf,Is.False,"Do not overlay a duplicate banner on ARIA's text or buttons.");
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
