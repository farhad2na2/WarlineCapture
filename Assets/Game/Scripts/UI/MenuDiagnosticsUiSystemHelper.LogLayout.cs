using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed partial class MenuDiagnosticsUiSystemHelper
    {
        private static void ResizeRuntimeLogContent(MenuDiagnosticsView view)
        {
            if (view.LogText == null || view.LogScrollRect == null)
                return;

            RectTransform content = view.LogScrollRect.content;
            RectTransform viewport = view.LogScrollRect.viewport != null
                ? view.LogScrollRect.viewport
                : view.LogScrollRect.GetComponent<RectTransform>();
            if (content == null || viewport == null)
                return;

            RectTransform textRect = view.LogText.rectTransform;
            float textWidth = Mathf.Max(1f, textRect.rect.width);
            Vector2 preferred = view.LogText.GetPreferredValues(view.LogText.text, textWidth, 0f);
            float contentHeight = Mathf.Max(viewport.rect.height, preferred.y + 32f);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, preferred.y));
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private static void ScrollRuntimeLogToBottom(MenuDiagnosticsView view)
        {
            if (view.LogScrollRect == null)
                return;

            view.LogScrollRect.StopMovement();
            view.LogScrollRect.verticalNormalizedPosition = 0f;
        }

    }
}
