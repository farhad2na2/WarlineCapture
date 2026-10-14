using System;
using System.Collections.Generic;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BuildDrawerCatalogScrollSurfaceTests
{
    sealed class Fixture : IDisposable
    {
        public readonly GameObject Root;
        public readonly EventSystem Events;
        public readonly ScrollRect Scroll;
        public readonly RectTransform Viewport, Content, Card;
        public readonly Image Scrim;
        public readonly Camera Camera;
        readonly RenderTexture Target;
        public Fixture()
        {
            Root = new GameObject("OwnedCatalogScrollTest", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            // executeMethod is synchronous EditMode: render an owned Canvas instead of
            // assuming overlay graphics acquired native depth during an Editor frame.
            Camera = new GameObject("OwnedCatalogCamera", typeof(Camera)).GetComponent<Camera>();
            Camera.transform.position = new Vector3(0,0,-10); Camera.orthographic = true; Camera.orthographicSize = 400;
            Camera.nearClipPlane = .1f; Camera.farClipPlane = 20; Camera.cullingMask = 1 << 31; Camera.enabled = false;
            Target = new RenderTexture(1024,1024,16); Target.Create(); Camera.targetTexture = Target;
            var canvas = Root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = Camera; canvas.sortingOrder = 32767;
            ((RectTransform)Root.transform).sizeDelta = new Vector2(800,800); Root.layer = 31;
            Events = new GameObject("OwnedCatalogEventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            var scrim = Rect("BattlefieldScrim", Root.transform, new Vector2(600,600), Vector2.zero); Scrim = scrim.gameObject.AddComponent<Image>(); Scrim.color = Color.clear;
            var scroll = Rect("CatalogScroll", Root.transform, new Vector2(300,300), Vector2.zero); Scroll = scroll.gameObject.AddComponent<ScrollRect>();
            Viewport = Rect("CatalogViewport", scroll, new Vector2(300,300), Vector2.zero); Viewport.gameObject.AddComponent<RectMask2D>();
            Content = Rect("Content", Viewport, new Vector2(300,600), Vector2.zero); Content.anchorMin=Content.anchorMax=new Vector2(.5f,1); Content.pivot=new Vector2(.5f,1);
            Card = Rect("Card", Content, new Vector2(260,80), new Vector2(0,-40)); Card.anchorMin=Card.anchorMax=new Vector2(.5f,1); Card.gameObject.AddComponent<Image>(); Card.gameObject.AddComponent<Button>();
            Scroll.viewport=Viewport; Scroll.content=Content; Scroll.horizontal=false; Scroll.vertical=true; Scroll.inertia=false; Scroll.movementType=ScrollRect.MovementType.Clamped;
            Refresh();
        }
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=31;r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        public void Refresh(){Canvas.ForceUpdateCanvases();Camera.Render();}
        public Vector2 Point(RectTransform rect, Vector2 local) => RectTransformUtility.WorldToScreenPoint(Camera,rect.TransformPoint(local));
        public GameObject Hit(Vector2 point)
        {
            Refresh();
            var hits=new List<RaycastResult>();Root.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(Events){position=point},hits);
            // The test qualifies this owned surface, independent of unrelated scene UI.
            hits.RemoveAll(hit => hit.gameObject == null || !hit.gameObject.transform.IsChildOf(Root.transform));
            Assert.That(hits.Count,Is.GreaterThan(0),"Owned rendered GraphicRaycaster produced no hit: point="+point+" cameraRect="+Camera.pixelRect+" canvasRect="+Root.GetComponent<Canvas>().pixelRect+" scrimDepth="+Scrim.depth+" scrimCull="+Scrim.canvasRenderer.cull);
            LastHit=hits[0];return LastHit.gameObject;
        }
        public RaycastResult LastHit;
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Root);UnityEngine.Object.DestroyImmediate(Events.gameObject);
            Camera.targetTexture=null;Target.Release();UnityEngine.Object.DestroyImmediate(Target);UnityEngine.Object.DestroyImmediate(Camera.gameObject);
        }
    }
    [Test] public void GapRaycastAndNativeDragReachScrollRectAfterRepair()
    {
        using var f=new Fixture();var start=f.Point(f.Viewport,new Vector2(0,-40));
        Assert.AreSame(f.Scrim.gameObject,f.Hit(start),"Control must reproduce empty-gap scrim interception");
        BuildDrawerCatalogPresentationSystemHelper.EnsureCatalogScrollSurface(f.Scroll);Canvas.ForceUpdateCanvases();
        var hit=f.Hit(start);Assert.AreSame(f.Viewport.gameObject,hit);
        Assert.AreSame(f.Scroll,hit.GetComponentInParent<ScrollRect>());
        float before=f.Content.anchoredPosition.y;
        var data=new PointerEventData(f.Events){position=start,pressPosition=start,button=PointerEventData.InputButton.Left,pointerPressRaycast=f.LastHit};
        ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.initializePotentialDrag);ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.beginDragHandler);
        data.position=start+Vector2.up*60;ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.dragHandler);ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.endDragHandler);
        Assert.Greater(f.Content.anchoredPosition.y,before+30,"Real ScrollRect drag must change content, not merely record a gesture");
    }
    [Test] public void CardStillWinsAndOutsideViewportStillHitsScrim()
    {
        using var f=new Fixture();BuildDrawerCatalogPresentationSystemHelper.EnsureCatalogScrollSurface(f.Scroll);Canvas.ForceUpdateCanvases();
        Assert.AreSame(f.Card.gameObject,f.Hit(f.Point(f.Card,Vector2.zero)));
        Assert.AreSame(f.Scrim.gameObject,f.Hit(f.Point(f.Viewport,new Vector2(200,0))));
        Assert.NotNull(f.Viewport.GetComponent<RectMask2D>());Assert.IsTrue(f.Scrim.raycastTarget);
    }
    [Test] public void SurfaceIsTransparentIdempotentAndPreservesExistingGraphics()
    {
        using var f=new Fixture();BuildDrawerCatalogPresentationSystemHelper.EnsureCatalogScrollSurface(f.Scroll);
        var image=f.Viewport.GetComponent<Image>();Assert.AreEqual(0,image.color.a);Assert.IsTrue(image.raycastTarget);
        BuildDrawerCatalogPresentationSystemHelper.EnsureCatalogScrollSurface(f.Scroll);Assert.AreEqual(1,f.Viewport.GetComponents<Graphic>().Length);
        image.color=Color.red;image.raycastTarget=false;BuildDrawerCatalogPresentationSystemHelper.EnsureCatalogScrollSurface(f.Scroll);
        Assert.AreEqual(Color.red,image.color);Assert.IsFalse(image.raycastTarget);
    }
    [Test] public void ExistingCatalogPrefabReceivesSurfaceWithoutChangingScrollOrMask()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab");Assert.NotNull(prefab);
        var clone=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var view=clone.GetComponent<BuildDrawerView>();Assert.NotNull(view);
            var scroll=view.ItemContentRoot.GetComponentInParent<ScrollRect>();Assert.NotNull(scroll);var content=scroll.content;var mask=scroll.viewport.GetComponent<RectMask2D>();Assert.NotNull(mask);
            BuildDrawerCatalogPresentationSystemHelper.EnsureCatalogScrollSurface(scroll);
            Assert.IsTrue(scroll.viewport.GetComponent<Graphic>().raycastTarget);Assert.AreSame(content,scroll.content);Assert.AreSame(mask,scroll.viewport.GetComponent<RectMask2D>());
            Assert.IsTrue(view.DrawerRoot.GetComponent<Image>().raycastTarget);
        }
        finally {UnityEngine.Object.DestroyImmediate(clone);}
    }
    public static void RunFocusedValidation()
    {
        var t=new BuildDrawerCatalogScrollSurfaceTests();t.GapRaycastAndNativeDragReachScrollRectAfterRepair();t.CardStillWinsAndOutsideViewportStillHitsScrim();t.SurfaceIsTransparentIdempotentAndPreservesExistingGraphics();t.ExistingCatalogPrefabReceivesSurfaceWithoutChangingScrollOrMask();
        Debug.Log("[BuildDrawerCatalogScrollSurfaceTests] result=Passed tests=4 scope=IsolatedNativeUiNotNormalMission");
    }
}
