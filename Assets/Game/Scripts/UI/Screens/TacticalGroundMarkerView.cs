using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    // Presentation only: no collider, raycaster or selectable control.
    public sealed class TacticalGroundMarkerView : MonoBehaviour
    {
        public enum Symbol { People, Departure, Clock, Alert, Check }
        private readonly LineRenderer[] ticks = new LineRenderer[8];
        private Material material;
        private Canvas markerCanvas;
        private RectTransform caption;
        private Image stem;
        private TextMeshProUGUI label;
        private Image border;
        private RectTransform icon;
        private Vector3 center;
        private Color tint;
        private Symbol symbol;
        private bool corners;

        public void Configure(Vector3 position, float radius, Color color, string text, Symbol glyph, bool cornerMarks = false)
        {
            Ensure(); center = position; tint = color; corners = cornerMarks;
            for (int i = 0; i < ticks.Length; i++)
            {
                float angle = i * Mathf.PI / 4;
                Vector3 radial = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 tangent = new(-radial.z, 0, radial.x);
                var point = center + Vector3.up * .18f + radial * radius;
                float length = Mathf.Clamp(radius * .09f, .4f, 1.4f);
                ticks[i].SetPosition(0, point - tangent * length * .5f);
                ticks[i].SetPosition(1, point + tangent * length * .5f);
                ticks[i].startColor = ticks[i].endColor = color;
                ticks[i].widthMultiplier = .12f;
                ticks[i].enabled = !corners;
            }
            if (corners)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = (i < 2 ? 1 : -1) * .7071068f, z = (i % 2 == 0 ? 1 : -1) * .7071068f;
                    Vector3 p = center + new Vector3(x * radius, .18f, z * radius);
                    ticks[i * 2].enabled = ticks[i * 2 + 1].enabled = true;
                    ticks[i * 2].SetPosition(0, p); ticks[i * 2].SetPosition(1, p - Vector3.right * x * radius * .32f);
                    ticks[i * 2 + 1].SetPosition(0, p); ticks[i * 2 + 1].SetPosition(1, p - Vector3.forward * z * radius * .32f);
                }
            }
            border.color = color; stem.color=color;
            UiLocalizedText.Set(label, text);
            if (symbol != glyph || icon.childCount == 0) { symbol = glyph; DrawIcon(); }
            foreach (var image in icon.GetComponentsInChildren<Image>()) image.color = color;
        }

        private void Ensure()
        {
            if (material != null) return;
            material = new Material(Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default"))
                { name = "Tactical marker matte", hideFlags = HideFlags.HideAndDontSave, renderQueue = 3100 };
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_Cull", (int)CullMode.Off); material.SetInt("_ZWrite", 0); material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            for (int i = 0; i < ticks.Length; i++)
            {
                var go = new GameObject("Perimeter tick " + i); go.transform.SetParent(transform, false);
                var line = ticks[i] = go.AddComponent<LineRenderer>(); line.sharedMaterial = material;
                line.useWorldSpace = true; line.positionCount = 2; line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false; line.numCapVertices = 0;
            }
            var tag = new GameObject("Camera facing caption", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            tag.transform.SetParent(transform, false); markerCanvas = tag.GetComponent<Canvas>(); markerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            markerCanvas.sortingOrder=1000;
            var group = tag.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            caption=new GameObject("Compact marker label",typeof(RectTransform)).GetComponent<RectTransform>();
            caption.SetParent(tag.transform,false);caption.sizeDelta=new Vector2(164,32);
            tag=caption.gameObject;
            border = Solid(tag.transform, "Keyline", Vector2.zero, new Vector2(164, 32), tint);
            Solid(tag.transform, "Charcoal", Vector2.zero, new Vector2(162, 30), new Color32(12, 26, 32, 245));
            stem=Solid(tag.transform, "Stem", new Vector2(0, -27), new Vector2(1, 22), new Color32(255, 191, 48, 220));
            icon = new GameObject("Pictogram", typeof(RectTransform)).GetComponent<RectTransform>(); icon.SetParent(tag.transform, false);
            icon.anchoredPosition = new Vector2(-65, 0); icon.sizeDelta = new Vector2(22, 22);
            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(tag.transform, false);
            label = text.GetComponent<TextMeshProUGUI>(); label.rectTransform.sizeDelta = new Vector2(126, 28); label.rectTransform.anchoredPosition = new Vector2(13, 0);
            label.fontSize = 16; label.fontStyle = FontStyles.Bold; label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = 16;
            label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.textWrappingMode = TextWrappingModes.NoWrap; label.raycastTarget = false;
            text.AddComponent<V3LocalizedTextBindingView>();
        }

        private static Image Solid(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            image.rectTransform.sizeDelta = size; image.rectTransform.anchoredPosition = position; return image;
        }
        private void Stroke(Vector2 a, Vector2 b, float width = 2)
        {
            var image = Solid(icon, "Icon stroke", (a + b) * .5f, new Vector2(Vector2.Distance(a, b), width), tint);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        }
        private void DrawIcon()
        {
            foreach (Transform child in icon) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            switch (symbol)
            {
                case Symbol.People:
                    for (int i = -1; i <= 1; i++) { Solid(icon, "Head", new Vector2(i * 7, 6), new Vector2(4, 4), tint); Stroke(new Vector2(i * 7, 1), new Vector2(i * 7, -8), 4); } break;
                case Symbol.Departure:
                    Stroke(new Vector2(0, -9), new Vector2(0, 9), 3); Stroke(new Vector2(-9, -1), new Vector2(0, 4)); Stroke(new Vector2(0, 4), new Vector2(9, -1)); Stroke(new Vector2(-4, -8), new Vector2(4, -8)); break;
                case Symbol.Check: Stroke(new Vector2(-8, 0), new Vector2(-2, -6), 3); Stroke(new Vector2(-2, -6), new Vector2(9, 7), 3); break;
                case Symbol.Alert: Stroke(new Vector2(0, 9), new Vector2(-10, -8)); Stroke(new Vector2(-10, -8), new Vector2(10, -8)); Stroke(new Vector2(10, -8), new Vector2(0, 9)); Stroke(new Vector2(0, 4), new Vector2(0, -2)); Solid(icon, "Dot", new Vector2(0, -5), new Vector2(2, 2), tint); break;
                default:
                    for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6, b = (i + 1) * Mathf.PI / 6; Stroke(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 9, new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 9); }
                    Stroke(Vector2.zero, new Vector2(0, 6)); Stroke(Vector2.zero, new Vector2(5, 0)); break;
            }
        }
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null || caption == null) return;
            var point = camera.WorldToScreenPoint(center);
            float scale=Mathf.Clamp(Screen.height/1080f,.75f,1.5f);
            Rect battlefield=Rect.MinMaxRect(Screen.width*.24f,Screen.height*.28f,Screen.width*.72f,Screen.height*.73f);
            Vector2 anchor=new(
                Mathf.Clamp(point.x,battlefield.xMin+82*scale,battlefield.xMax-82*scale),
                Mathf.Clamp(point.y+52*scale,battlefield.yMin+16*scale,battlefield.yMax-16*scale));
            bool visible=point.z>0 && battlefield.Contains(new Vector2(point.x,point.y));
            caption.gameObject.SetActive(visible);
            if(!visible)return;
            caption.position=new Vector3(anchor.x,anchor.y,0);
            caption.localScale=Vector3.one*scale;
            Vector2 start=new(0,-17),end=new((point.x-anchor.x)/scale,(point.y-anchor.y)/scale+5);
            stem.rectTransform.anchoredPosition=(start+end)*.5f;
            stem.rectTransform.sizeDelta=new Vector2(Vector2.Distance(start,end),1);
            stem.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg);
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
