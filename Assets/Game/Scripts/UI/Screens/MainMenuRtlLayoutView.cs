using System;
using Game.Configs;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    [Serializable]
    public struct MainMenuRtlTarget
    {
        public RectTransform Target;
        public bool Mirror;
        public bool FlipGraphic;
        public float RightToLeftWidth;
        [HideInInspector] public Vector2 AnchorMin, AnchorMax, Pivot, Position, Size;
        [HideInInspector] public Vector3 Scale;

        public MainMenuRtlTarget(RectTransform target, bool mirror, bool flipGraphic = false, float rightToLeftWidth = 0f)
        {
            Target = target; Mirror = mirror; FlipGraphic = flipGraphic; RightToLeftWidth = rightToLeftWidth;
            AnchorMin = target.anchorMin; AnchorMax = target.anchorMax; Pivot = target.pivot;
            Position = target.anchoredPosition; Size = target.sizeDelta; Scale = target.localScale;
        }
    }

    // Localized texts already mirror their alignment; MirrorAlignment is for unbound copy such as numbers.
    [Serializable]
    public struct MainMenuRtlText
    {
        public TMP_Text Text;
        public float RightToLeftLineSpacing;
        public bool MirrorAlignment;
        [HideInInspector] public float LineSpacing;
        [HideInInspector] public HorizontalAlignmentOptions Alignment;

        public MainMenuRtlText(TMP_Text text, float? rightToLeftLineSpacing = null, bool mirrorAlignment = false)
        {
            Text = text; LineSpacing = text.lineSpacing; Alignment = text.horizontalAlignment;
            RightToLeftLineSpacing = rightToLeftLineSpacing ?? text.lineSpacing; MirrorAlignment = mirrorAlignment;
        }
    }

    // Mirrors panel internals that the section frame does not drive. Frame targets must not be listed.
    public sealed class MainMenuRtlLayoutView : MonoBehaviour
    {
        [SerializeField] private MainMenuRtlTarget[] targets = Array.Empty<MainMenuRtlTarget>();
        [SerializeField] private MainMenuRtlText[] texts = Array.Empty<MainMenuRtlText>();
        private bool? rightToLeft;

        public void Configure(MainMenuRtlTarget[] rules, MainMenuRtlText[] textRules = null)
        {
            targets = rules ?? Array.Empty<MainMenuRtlTarget>();
            texts = textRules ?? Array.Empty<MainMenuRtlText>();
            rightToLeft = null;
        }

        private void OnEnable() => rightToLeft = null;

        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            bool rtl = GameLocalization.CurrentLocaleCode == GameLocalization.PersianLocaleCode;
            if (rightToLeft == rtl) return;
            rightToLeft = rtl;
            foreach (var rule in targets)
                if (rule.Target != null) Apply(rule, rtl);
            foreach (var rule in texts)
            {
                if (rule.Text == null) continue;
                rule.Text.lineSpacing = rtl ? rule.RightToLeftLineSpacing : rule.LineSpacing;
                if (rule.MirrorAlignment)
                    rule.Text.horizontalAlignment = !rtl ? rule.Alignment : rule.Alignment switch
                    {
                        HorizontalAlignmentOptions.Left => HorizontalAlignmentOptions.Right,
                        HorizontalAlignmentOptions.Right => HorizontalAlignmentOptions.Left,
                        _ => rule.Alignment
                    };
            }
        }

        private static void Apply(MainMenuRtlTarget rule, bool rtl)
        {
            RectTransform rect = rule.Target;
            Vector2 anchorMin = rule.AnchorMin, anchorMax = rule.AnchorMax, pivot = rule.Pivot;
            Vector2 position = rule.Position, size = rule.Size;
            Vector3 scale = rule.Scale;
            if (rtl && rule.RightToLeftWidth > 0f && Mathf.Approximately(anchorMin.x, anchorMax.x))
            {
                position.x += pivot.x * (rule.RightToLeftWidth - size.x);
                size.x = rule.RightToLeftWidth;
            }
            if (rtl && rule.Mirror)
            {
                anchorMin.x = 1f - rule.AnchorMax.x;
                anchorMax.x = 1f - rule.AnchorMin.x;
                pivot.x = 1f - pivot.x;
                position.x = -position.x;
            }
            if (rtl && rule.FlipGraphic)
            {
                // Scale flips around the pivot; centre it so the graphic keeps its rect.
                position.x += (.5f - pivot.x) * rect.rect.width;
                pivot.x = .5f;
                scale.x = -Mathf.Abs(scale.x);
            }
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot;
            rect.sizeDelta = size; rect.anchoredPosition = position; rect.localScale = scale;
        }
    }
}
