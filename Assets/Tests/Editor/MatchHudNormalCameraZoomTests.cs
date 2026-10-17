using System.Reflection;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Tests.Editor
{
    public sealed class MatchHudNormalCameraZoomTests
    {
        public static void RunFocusedValidation()
        {
            var tests=new MatchHudNormalCameraZoomTests();
            tests.CompactControlsUseDiscreteCameraCallbacksAndEndstopState();
            tests.RebindingAndUnbindingNeverDuplicateOrRetainOldCallbacks();
            tests.FullMapProjectionNeverInvokesCameraZoom();
            Debug.Log("[MatchHudNormalCameraZoom] result=Passed controls=3");
        }
        sealed class Fixture : System.IDisposable
        {
            public readonly GameObject Root=new GameObject("CompactCameraZoomControl",typeof(RectTransform));
            public readonly MatchHudMinimapView View;
            public readonly Button In,Out;
            public Fixture()
            {
                Root.SetActive(false);View=Root.AddComponent<MatchHudMinimapView>();
                In=Button("ZoomIn");Out=Button("ZoomOut");
                typeof(MatchHudMinimapView).GetField("zoomInButton",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(View,In);
                typeof(MatchHudMinimapView).GetField("zoomOutButton",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(View,Out);
                Root.SetActive(true);View.ApplyInteractionOptions(false,false,false,false,false,true);
            }
            Button Button(string name){var obj=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));obj.transform.SetParent(Root.transform,false);return obj.GetComponent<Button>();}
            public static void Click(Button button)=>button.OnPointerClick(new PointerEventData(null){button=PointerEventData.InputButton.Left});
            public void Dispose()=>Object.DestroyImmediate(Root);
        }
        [Test]
        public void CompactControlsUseDiscreteCameraCallbacksAndEndstopState()
        {
            using var f=new Fixture();int inside=0,outside=0;bool canIn=true,canOut=false;
            f.View.BindNormalCameraZoom(()=>inside++,()=>outside++,()=>new MatchHudZoomControlState(canIn,canOut));
            Assert.That(f.View.HasNormalCameraZoomControls,Is.True);Assert.That(f.In.gameObject.activeInHierarchy,Is.True);
            Assert.That(f.Out.interactable,Is.False);Fixture.Click(f.In);Fixture.Click(f.Out);
            Assert.That(inside,Is.EqualTo(1));Assert.That(outside,Is.Zero);
            canIn=false;canOut=true;f.View.RefreshNormalCameraZoomControls();Fixture.Click(f.In);Fixture.Click(f.Out);
            Assert.That(inside,Is.EqualTo(1));Assert.That(outside,Is.EqualTo(1));
        }
        [Test]
        public void RebindingAndUnbindingNeverDuplicateOrRetainOldCallbacks()
        {
            using var f=new Fixture();int old=0,current=0;
            f.View.BindNormalCameraZoom(()=>old++,()=>old++,()=>new MatchHudZoomControlState(true,true));
            f.View.BindNormalCameraZoom(()=>current++,()=>current++,()=>new MatchHudZoomControlState(true,true));
            Fixture.Click(f.In);Assert.That(old,Is.Zero);Assert.That(current,Is.EqualTo(1));
            f.View.UnbindNormalCameraZoom();Assert.That(f.View.HasNormalCameraZoomControls,Is.False);
            Assert.That(f.In.gameObject.activeSelf,Is.False);Fixture.Click(f.In);Assert.That(current,Is.EqualTo(1));
        }
        [Test]
        public void FullMapProjectionNeverInvokesCameraZoom()
        {
            using var f=new Fixture();int camera=0,map=0;
            f.View.BindNormalCameraZoom(()=>camera++,()=>camera++,()=>new MatchHudZoomControlState(true,true));
            f.View.ZoomHeldChanged+=(direction,held)=>{if(held)map++;};
            f.View.ApplyInteractionOptions(true,true,true,true,true,false);
            Assert.That(f.View.HasNormalCameraZoomControls,Is.False);f.View.NotifyZoomHeld(1,true);Fixture.Click(f.In);
            Assert.That(map,Is.EqualTo(1));Assert.That(camera,Is.Zero);
        }
    }
}
