using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class TutorialTapPointerView : MonoBehaviour
    {
        private RectTransform arrow, caption, target;
        private Canvas canvas;
        private Transform obstacleTextRoot;
        private readonly List<TextMeshProUGUI> obstacleTexts = new();
        private readonly List<MatchHudMinimapView> obstacleMinimaps = new();
        private readonly Vector3[] corners = new Vector3[4];
        private readonly List<Rect> obstacles = new();
        private Rect arrowScreen, captionScreen, lastTarget, lastSafe;
        private Vector2 captionSize;
        private TutorialPointerSide side;
        private float refreshAt;
        private bool visible, placed;

        internal static void BindCaption(RectTransform target, RectTransform caption, Transform obstacleTextRoot)
        {
            var view = target.GetComponent<TutorialTapPointerView>() ?? target.gameObject.AddComponent<TutorialTapPointerView>();
            view.caption = caption;
            view.obstacleTextRoot = obstacleTextRoot;
            view.refreshAt = -1;
        }

        internal static void Present(RectTransform target, float elapsed, bool visible)
        {
            var view = target.GetComponent<TutorialTapPointerView>();
            if (view == null && visible) view = target.gameObject.AddComponent<TutorialTapPointerView>();
            if (view == null) return;
            view.visible = visible;
            view.Draw();
        }

        internal void SetCaptionSize(Vector2 size)
        {
            if (captionSize == size) return;
            captionSize = size;
            if (caption != null) caption.sizeDelta = size;
            refreshAt = -1;
        }

        private void OnDisable() { visible = false; placed = false; if (arrow != null) arrow.gameObject.SetActive(false); }

        private void Draw()
        {
            if (!visible || !gameObject.activeInHierarchy) { if (arrow != null) arrow.gameObject.SetActive(false); return; }
            target ??= transform as RectTransform;
            canvas ??= GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null) return;
            if (arrow == null)
            {
                var go = new GameObject("AttentionTapPointer", typeof(RectTransform), typeof(TutorialChevronGraphic));
                go.layer = gameObject.layer; arrow = (RectTransform)go.transform;
                arrow.SetParent(transform, false); arrow.anchorMin = arrow.anchorMax = arrow.pivot = Vector2.one * .5f;
                go.GetComponent<TutorialChevronGraphic>().raycastTarget = false;

            }
            Rect screen = ScreenRect(target);
            Rect safe = Screen.safeArea;
            float margin = Mathf.Max(6, Screen.height * .008f);
            safe = Rect.MinMaxRect(safe.xMin + margin, safe.yMin + margin, safe.xMax - margin, safe.yMax - margin);
            if (screen != lastTarget || safe != lastSafe || Time.unscaledTime >= refreshAt || !placed)
            {
                lastTarget = screen; lastSafe = safe; refreshAt = Time.unscaledTime + .25f;
                obstacles.Clear();
                // Exclude the target itself and all of its decoration. Other labels and
                // controls are obstacles, including ARIA copy and adjacent popup tabs.
                foreach (var control in Selectable.allSelectablesArray)
                    if (control.IsActive() && Visible(control.transform) && !control.transform.IsChildOf(transform) &&
                        !transform.IsChildOf(control.transform))
                    {
                        Rect bounds = ScreenRect((RectTransform)control.transform);
                        // The cue lives on an overlay canvas. Its real button ancestors
                        // (such as the ARIA rail) are containers, not adjacent controls.
                        if (bounds.Contains(screen.min) && bounds.Contains(screen.max)) continue;
                        obstacles.Add(bounds);
                    }
                obstacleTexts.Clear();
                var textRoot = obstacleTextRoot != null ? obstacleTextRoot : canvas.transform;
                textRoot.GetComponentsInChildren(false, obstacleTexts);
                foreach (var text in obstacleTexts)
                    if (Visible(text.transform) && !text.transform.IsChildOf(transform) && !string.IsNullOrWhiteSpace(text.text) && text.color.a > .01f)
                        obstacles.Add(ScreenRect(text.rectTransform));
                // Minimap input uses pointer interfaces rather than a Selectable button.
                obstacleMinimaps.Clear();
                textRoot.GetComponentsInChildren(false, obstacleMinimaps);
                foreach (var map in obstacleMinimaps)
                    if (map.MapRect != null && Visible(map.transform)) obstacles.Add(ScreenRect(map.MapRect));
                Vector2 labelSize = caption != null ? ScreenRect(caption).size : Vector2.zero;
                labelSize.x = Mathf.Min(labelSize.x, safe.width);
                float size = Mathf.Clamp(Screen.height * .045f, 28, 54);
                float travel = Mathf.Clamp(Screen.height * .014f, 8, 18);
                placed = TutorialTapPointerLayout.TryPlace(screen, safe, labelSize, size, margin, travel,
                    obstacles, out arrowScreen, out captionScreen, out side);
                if (!placed && caption != null)
                {
                    // Tight layouts retain a safe caption and pulse frame without an arrow.
                    bool labelFits = TutorialTapPointerLayout.TryPlace(screen, safe, labelSize, 0, margin, 0,
                        obstacles, out _, out captionScreen, out side);
                    caption.gameObject.SetActive(labelFits);
                    if (labelFits) Place(caption, captionScreen.center, captionScreen.size);
                }
            }
            arrow.gameObject.SetActive(placed);
            if (!placed) return;
            float bounce = SettingsService.LoadReducedMotionPreference() ? 0 :
                Mathf.Clamp(Screen.height * .014f, 8, 18) * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / 1.2f);
            var approach = (arrowScreen.center - screen.center).normalized;
            Place(arrow, arrowScreen.center + approach * bounce, arrowScreen.size);
            arrow.localRotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(
                Vector2.down, -approach));
            if (caption != null) { caption.gameObject.SetActive(true); Place(caption, captionScreen.center, captionScreen.size); }
        }

        private static bool Visible(Transform value)
        {
            for (var node = value; node != null; node = node.parent)
                foreach (var group in node.GetComponents<CanvasGroup>())
                {
                    if (group.alpha < .01f) return false;
                    if (group.ignoreParentGroups) return true;
                }
            return true;
        }

        private Rect ScreenRect(RectTransform rect)
        {
            var owner = rect.GetComponentInParent<Canvas>()?.rootCanvas;
            var camera = owner != null && owner.renderMode != RenderMode.ScreenSpaceOverlay ? owner.worldCamera : null;
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < 4; i++) { var p = RectTransformUtility.WorldToScreenPoint(camera, corners[i]); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void Place(RectTransform rect, Vector2 center, Vector2 size)
        {
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(target, center, camera, out var local);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(target, center + size, camera, out var end);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = local - target.rect.center;
            rect.sizeDelta = end - local;
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialChevronGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect;
            Arrow(vh,r,1f,new Color(.025f,.035f,.04f));
            Arrow(vh,r,.82f,new Color(1,.79f,.015f));
            Segment(vh,r.center+new Vector2(-r.width*.26f,-r.height*.04f),
                r.center+new Vector2(0,-r.height*.32f),r.width*.04f,new Color(1,.96f,.5f));
        }
        private static void Arrow(VertexHelper vh,Rect r,float scale,Color color)
        {
            Vector2 P(float x,float y) => r.center+new Vector2(x*r.width,y*r.height)*scale;
            int i=vh.currentVertCount;
            vh.AddVert(P(0,-.48f),color,Vector2.zero); vh.AddVert(P(-.46f,0),color,Vector2.zero);
            vh.AddVert(P(.46f,0),color,Vector2.zero); vh.AddTriangle(i,i+1,i+2);
            i=vh.currentVertCount;
            vh.AddVert(P(-.18f,-.06f),color,Vector2.zero); vh.AddVert(P(-.18f,.45f),color,Vector2.zero);
            vh.AddVert(P(.18f,.45f),color,Vector2.zero); vh.AddVert(P(.18f,-.06f),color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
        private static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x) * width / 2;
            int i = vh.currentVertCount;
            vh.AddVert(a - n, color, Vector2.zero); vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero); vh.AddVert(b - n, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
