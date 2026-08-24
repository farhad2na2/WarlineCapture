using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class AriaCommandAssistantPopupView
    {
        private void CacheAccessibilityDefaults()
        {
            _accessibilityTexts = GetComponentsInChildren<TMP_Text>(true);
            int count = _accessibilityTexts.Length;
            _normalFontSizes = new float[count];
            _normalFontSizeMin = new float[count];
            _normalFontSizeMax = new float[count];
            _normalTextColors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                TMP_Text text = _accessibilityTexts[i];
                _normalFontSizes[i] = text.fontSize;
                _normalFontSizeMin[i] = text.fontSizeMin;
                _normalFontSizeMax[i] = text.fontSizeMax;
                _normalTextColors[i] = text.color;
            }
        }

        private static Color ResolveHighContrastColor(Color source)
        {
            Color.RGBToHSV(source, out float hue, out float saturation, out float value);
            Color result = Color.HSVToRGB(hue, Mathf.Min(saturation, 0.85f), Mathf.Max(value, 0.92f));
            result.a = 1f;
            return result;
        }

        private GoalRowBinding BindGoalRow(int index)
        {
            string prefix = "Goal" + index;
            return new GoalRowBinding
            {
                Root = FindObject("GoalRow" + index),
                Icon = FindObject(prefix + "Icon"),
                StateChip = FindObject(prefix + "StateChip"),
                PriorityRail = FindObject(prefix + "PriorityRail"),
                Title = FindComponent<TMP_Text>(prefix + "Title"),
                Body = FindComponent<TMP_Text>(prefix + "Body"),
                State = FindComponent<TMP_Text>(prefix + "StateText")
            };
        }

        private MessageRowBinding BindMessageRow(string prefix, int index)
        {
            string rowPrefix = prefix + index;
            return new MessageRowBinding
            {
                Root = FindObject(prefix + "Row" + index),
                Icon = FindObject(rowPrefix + "Icon"),
                PriorityChip = FindObject(rowPrefix + "PriorityChip"),
                PriorityRail = FindObject(rowPrefix + "PriorityRail"),
                Body = FindComponent<TMP_Text>(rowPrefix + "Body"),
                Detail = FindComponent<TMP_Text>(rowPrefix + "Detail"),
                Priority = FindComponent<TMP_Text>(rowPrefix + "PriorityText")
            };
        }

        private GameObject FindObject(string objectName)
        {
            Transform child = FindNamedTransform(transform, objectName);
            return child != null ? child.gameObject : null;
        }

        private T FindComponent<T>(string objectName) where T : Component
        {
            Transform child = FindNamedTransform(transform, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindNamedTransform(Transform root, string objectName)
        {
            if (root == null)
                return null;
            if (root.name == objectName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindNamedTransform(root.GetChild(i), objectName);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static bool RowsBound(GoalRowBinding[] rows)
        {
            if (rows == null)
                return false;

            for (int i = 0; i < rows.Length; i++)
            {
                GoalRowBinding row = rows[i];
                if (row == null || row.Root == null || row.Title == null || row.Body == null || row.State == null)
                    return false;
            }

            return true;
        }

        private static bool RowsBound(MessageRowBinding[] rows)
        {
            if (rows == null)
                return false;

            for (int i = 0; i < rows.Length; i++)
            {
                MessageRowBinding row = rows[i];
                if (row == null || row.Root == null || row.Body == null || row.Detail == null || row.Priority == null)
                    return false;
            }

            return true;
        }

        private static bool TryGetRow(GoalRowBinding[] rows, int index, out GoalRowBinding row)
        {
            row = rows != null && index >= 0 && index < rows.Length ? rows[index] : null;
            return row != null;
        }

        private static bool TryGetRow(MessageRowBinding[] rows, int index, out MessageRowBinding row)
        {
            row = rows != null && index >= 0 && index < rows.Length ? rows[index] : null;
            return row != null;
        }

        private static void HideRows(GoalRowBinding[] rows)
        {
            if (rows == null)
                return;
            for (int i = 0; i < rows.Length; i++)
                SetActive(rows[i]?.Root, false);
        }

        private static void HideRows(MessageRowBinding[] rows)
        {
            if (rows == null)
                return;
            for (int i = 0; i < rows.Length; i++)
                SetActive(rows[i]?.Root, false);
        }

        private static string GoalStateText(byte state, bool isPrimary)
        {
            string stateText = state switch
            {
                1 => "COMPLETE",
                2 => "WARNING",
                3 => "BLOCKED",
                4 => "FAILED",
                _ => "ACTIVE"
            };
            return isPrimary ? "PRIMARY / " + stateText : stateText;
        }

        private static string PriorityText(byte priority)
        {
            return priority switch
            {
                3 => "CRITICAL",
                2 => "HIGH",
                1 => "NORMAL",
                _ => "LOW"
            };
        }

        private static string AgeStateText(byte ageState)
        {
            return ageState switch
            {
                1 => "NEW",
                2 => "ACTIVE",
                3 => "EXPIRING",
                _ => string.Empty
            };
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
                return;

            string resolved = value ?? string.Empty;
            if (target.text != resolved)
                target.text = resolved;
            SetActive(target.gameObject, resolved.Length > 0);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private void RequestClose()
        {
            _closeRequested?.Invoke();
        }

        private void RequestShowRecommendation()
        {
            _showRecommendationRequested?.Invoke();
        }

        private void RequestExecuteRecommendation()
        {
            _executeRecommendationRequested?.Invoke();
        }

        private void RequestStop()
        {
            _stopRequested?.Invoke();
        }
    }
}
