using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class OperationsMissionScreenView
    {
        private readonly System.Collections.Generic.Dictionary<TMP_Text,float> authoredLabelSizes = new();
        private bool typographyRtl;

        private void RefreshLocaleTypography()
        {
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (rtl == typographyRtl) return;
            typographyRtl = rtl;
            foreach (var entry in authoredLabelSizes)
            {
                entry.Key.margin = rtl ? new Vector4(0,-12,0,-12) : Vector4.zero;
                entry.Key.lineSpacing = rtl ? -12 : 0;
                entry.Key.GetComponent<V3LocalizedTextBindingView>().SetFontBounds(rtl ? entry.Value*.8f : entry.Value,entry.Value);
            }
        }
        private static RectTransform Node(string name,Transform parent)
        { var root = new GameObject(name,typeof(RectTransform)); root.transform.SetParent(parent,false); return (RectTransform)root.transform; }
        private static RectTransform Surface(string name,Transform parent,Color? fill=null)
        {
            var rect = Node(name,parent); var graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(fill ?? new Color(.09f,.13f,.15f,1f),fill ?? new Color(.025f,.05f,.06f,1f),new Color(.43f,.5f,.52f),fill.HasValue ? 0 : 2); return rect;
        }
        private TMP_Text Label(Transform parent,string value,float size,float height,TextAlignmentOptions alignment=TextAlignmentOptions.MidlineLeft)
        {
            var rect = Node("Label",parent); var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font ?? TMP_Settings.defaultFontAsset; label.fontSize = size; label.color = Color.white;
            label.alignment = alignment; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
            // The Arabic font has a taller metric box than its visible glyphs.
            // Give that box room without shrinking mission copy to half size.
            if (UiShellRuntimeGateway.Localization.IsRightToLeft)
            { label.margin = new Vector4(0,-12,0,-12); label.lineSpacing = -12; }
            Height(rect,height); Set(label,value);
            authoredLabelSizes[label] = size;
            if (UiShellRuntimeGateway.Localization.IsRightToLeft)
                label.GetComponent<V3LocalizedTextBindingView>().SetFontBounds(size*.8f,size);
            return label;
        }
        private Button Button(Transform parent,string label,Action action,Color accent)
        {
            var rect = Surface(label,parent); Height(rect,hud ? 76 : 54);
            Color upper = accent*.65f, lower = accent*.25f; upper.a=lower.a=1;
            rect.GetComponent<V3GradientGraphic>().Configure(upper,lower,accent,3);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<V3GradientGraphic>(); button.onClick.AddListener(() => action());
            var text = Label(rect,label,hud ? 28 : 20,0,TextAlignmentOptions.Center); Stretch(text.rectTransform,14); return button;
        }
        private void Portrait(Transform parent,float width,float height)
        {
            var rect = Node("AriaPortrait",parent); Width(rect,width); Height(rect,height);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = portrait; image.preserveAspect = true; image.raycastTarget = false;
            if (portrait == null) image.color = Color.clear;
        }
        private static void Rule(Transform parent) { Height(Surface("Divider",parent,new Color(.4f,.49f,.51f)),2); }
        private static RectTransform Row(Transform parent,float height)
        {
            var rect=Node("Row",parent); Height(rect,height); var layout=rect.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing=18;
            layout.childControlWidth=layout.childControlHeight=true; layout.childForceExpandWidth=layout.childForceExpandHeight=false; return rect;
        }
        private static void Vertical(RectTransform rect,int spacing,int padding)
        {
            var layout=rect.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing=spacing; layout.padding=new RectOffset(padding,padding,padding,padding);
            layout.childControlHeight=layout.childControlWidth=true; layout.childForceExpandHeight=false; layout.childForceExpandWidth=true;
        }
        private RectTransform Modal(string name,out GameObject root)
        {
            var shade=Surface(name,hud ? safeRoot : GetComponentInParent<Canvas>().rootCanvas.transform,new Color(0,0,0,.75f)); Stretch(shade);
            root=shade.gameObject; var card=Surface("Card",shade); Center(card,900,540); Vertical(card,20,36); return card;
        }
        private static void Height(RectTransform rect,float height)
        { var e=rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>(); e.minHeight=e.preferredHeight=height; e.flexibleHeight=0; }
        private static void Width(RectTransform rect,float width)
        { var e=rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>(); e.minWidth=e.preferredWidth=width; e.flexibleWidth=0; }
        private static void Stretch(RectTransform rect,float inset=0)
        { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.one*inset; rect.offsetMax=-Vector2.one*inset; }
        private static void Center(RectTransform rect,float width,float height)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.sizeDelta=new Vector2(width,height); }
        private static void TopRight(RectTransform rect,float width,float height,float right,float top)
        { rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one; rect.sizeDelta=new Vector2(width,height); rect.anchoredPosition=new Vector2(-right,-top); }
    }
}
