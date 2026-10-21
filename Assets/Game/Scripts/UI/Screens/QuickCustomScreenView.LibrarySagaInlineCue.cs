using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private static bool _fittingSagaInlineCue;

        private static bool TryFitSagaInlineCue(TMP_Text cue)
        {
            if (_fittingSagaInlineCue || cue == null || cue.rectTransform.parent is not RectTransform parent)
                return false;
            Rect original = PlateRect(cue.rectTransform);
            RectTransform card = null;
            foreach (string id in TheatreSeatGeometry.MapIds)
            {
                if (parent.Find("ZoneCard_" + id) is not RectTransform candidate) continue;
                Rect box = PlateRect(candidate);
                if (box.height >= 176f && box.Contains(original.min) && box.Contains(original.max))
                { card = candidate; break; }
            }
            if (card == null) return false;
            _fittingSagaInlineCue = true;
            try
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                Rect box = PlateRect(card);
                float footer = box.height >= 190f ? 40f : 34f;
                float height = fa ? 64f : 44f;
                var spot = new Rect(box.x + 4f, box.yMax - footer - 6f - height, box.width - 8f, height);
                PlaceTopLeft(cue.rectTransform, spot.x, spot.y, spot.width, spot.height);
                KeepAuthoredSize(cue.rectTransform);
                cue.isRightToLeftText = fa;
                // Keep the authored compound intact; allow its native Arabic line metrics.
                UiLocalizedText.Set(cue, fa ? "بزرگ‌نمایی" : "Tap to zoom");
                cue.enableAutoSizing = false;
                float size = fa ? 22f : spot.width < 140f ? 18f : 20f;
                cue.fontSizeMin = cue.fontSizeMax = cue.fontSize = size;
                cue.textWrappingMode = TextWrappingModes.NoWrap;
                cue.overflowMode = TextOverflowModes.Overflow;
                cue.maxVisibleLines = 2;
                cue.characterSpacing = 0;
                cue.margin = new Vector4(2f, 0f, 2f, 8f);
                cue.lineSpacing = 0;
                foreach (float spacing in new[] { 0f, -4f, -8f, -12f })
                {
                    cue.characterSpacing = fa ? spacing : 0f;
                    cue.ForceMeshUpdate();
                    if (!cue.isTextTruncated && !cue.isTextOverflowing
                        && cue.preferredWidth <= spot.width - 4f + 1f
                        && cue.preferredHeight <= height - 8f + 1f) break;
                }
                SyncCuePlate(parent, spot);
                AttachZoomPointer(cue.rectTransform);
                parent.Find("CuePlate")?.SetAsLastSibling();
                cue.transform.SetAsLastSibling();
                return true;
            }
            finally { _fittingSagaInlineCue = false; }
        }
    }
}
