using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        /// <summary>Shared Campaign chrome: dark steel frames, blue selection and colored command faces.</summary>
        public void ApplySagaPrefabStyle() => ApplySagaStyle();
        private void ApplySagaStyle()
        {
            Color steel = new(.23f, .29f, .31f, 1), top = new(.055f, .085f, .095f, 1), bottom = new(.015f, .035f, .043f, 1);
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Caption" && child.parent != null && child.parent.parent == _medalRow)
                    child.gameObject.SetActive(LibraryLevel != SkirmishLibraryLevel.Battlefield);
                if (child.name == "MapPreview" && child.TryGetComponent<RawImage>(out var oldPlate))
                    oldPlate.enabled = false;
                if (child.name == "ChapterTitle" && child.TryGetComponent<TMP_Text>(out var chapterTitle))
                {
                    float size = UiShellRuntimeGateway.Localization.IsRightToLeft ? 22 : 19;
                    chapterTitle.enableAutoSizing = false;
                    chapterTitle.fontSize = chapterTitle.fontSizeMin = chapterTitle.fontSizeMax = size;
                    chapterTitle.textWrappingMode = TextWrappingModes.NoWrap;
                    chapterTitle.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(size, size);
                }

                if (child.name == "Rule" && child.parent != null && child.parent.name == TheatreDetailPanelName && child.TryGetComponent<Image>(out var divider))
                    divider.color = steel;
                if (child.name.StartsWith("OrnamentCorner", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
                if (child.parent != null && child.parent.name == TheatreDetailPanelName && child.name == "Value")
                    child.gameObject.SetActive(false);
                // Keep every rectangular frame consistent, including nested prefab chrome.
                if (child.TryGetComponent<V3GradientGraphic>(out var rectangular))
                    rectangular.SetBorder(steel, 1.5f);
                if (child.TryGetComponent<Outline>(out var outline))
                {
                    outline.effectColor = steel;
                    outline.effectDistance = new Vector2(1f, -1f);
                }

                if (child.name is "SearchBar" or "LibrarySearch" or "SearchField" && child.TryGetComponent<Image>(out var searchEdge))
                    searchEdge.color = steel;
                bool panel = child.name is "BaseAssaultRules" or "TheatreSummary" or "ScreenTitlePanel" or "RecommendedNext";
                if (panel && child.TryGetComponent<V3GradientGraphic>(out var frame))
                    frame.Configure(top, bottom, steel, 1.5f);
                if (child.name == TheatreDetailPanelName && child.TryGetComponent<Image>(out var detail))
                {
                    detail.color = bottom;
                    if (detail.TryGetComponent<Outline>(out var edge))
                    {
                        edge.effectColor = steel;
                        edge.effectDistance = new Vector2(1.5f, -1.5f);
                    }
                }
            }

            foreach (string name in new[]
            {
                "LibraryBackdrop",
                "LibraryDepthPlate"
            }

            )
            {
                var backdrop = transform.Find(name)?.GetComponent<RawImage>();
                if (backdrop != null)
                    backdrop.color = new Color(.045f, .065f, .075f, 1);
            }

            Transform composition = transform.Find("SkirmishSetupComposition");
            if (composition == null)
                return;
            TMP_Text title = composition.Find("ScreenTitlePanel/ScreenTitle")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.fontStyle = FontStyles.Normal;
                title.fontSizeMin = 24;
                title.fontSizeMax = 38;
                title.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(24, 38);
            }

            if (_objectiveColumn != null)
            {
                foreach (Button button in _objectiveColumn.GetComponentsInChildren<Button>(true))
                {
                    bool selected = button.name == SkirmishLibraryControlNames.ObjectiveTab(_libraryViewModel?.Battlefield.SelectedObjectiveId);
                    var face = button.GetComponent<V3GradientGraphic>();
                    if (face != null)
                        face.Configure(selected ? new Color(.07f, .18f, .23f) : top, bottom, selected ? new Color(.22f, .48f, .58f) : steel, selected ? 2 : 1.5f);
                    if (UiShellRuntimeGateway.Localization.IsRightToLeft)
                        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
                        {
                            text.fontSizeMin = text.fontSizeMax = text.fontSize = 22;
                            text.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(22, 22);
                        }
                }
            }

            if (_objectiveColumn != null && !UiShellRuntimeGateway.Localization.IsRightToLeft)
            {
                var breakthrough = _objectiveColumn.Find(SkirmishLibraryControlNames.ObjectiveTab("BT"))?.Find("Label")?.GetComponent<TMP_Text>();
                if (breakthrough != null)
                {
                    breakthrough.textWrappingMode = TextWrappingModes.NoWrap;
                    breakthrough.enableAutoSizing = true;
                    breakthrough.fontSizeMin = 18;
                    breakthrough.fontSizeMax = 22;
                    breakthrough.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(18, 22);
                }
            }

            FitSagaMissionSizeLabels();
            foreach (Button button in new[]
            {
                resetButton,
                randomizeSeedButton,
                launchButton,
                _libraryAdvancedButton
            }

            )
            {
                if (button == null)
                    continue;
                bool primary = button == _footerPrimary;
                bool navy = button == _libraryAdvancedButton || button == launchButton && !primary;
                Color high = primary ? new Color(.18f, .40f, .055f) : navy ? new Color(.025f, .12f, .28f) : new Color(.025f, .26f, .43f);
                Color low = primary ? new Color(.035f, .15f, .035f) : navy ? new Color(.008f, .035f, .10f) : new Color(.008f, .065f, .14f);
                Color border = primary ? new Color(.28f, .55f, .1f) : new Color(.06f, .34f, .53f);
                TintCommand(button, high, low);
                var gradient = button.targetGraphic as V3GradientGraphic;
                if (gradient != null)
                    gradient.Configure(high, low, border, 2);
                var bevel = button.transform.Find("FooterBevel")?.GetComponent<Image>();
                if (bevel != null)
                {
                    bevel.rectTransform.sizeDelta = new Vector2(0, 2);
                    bevel.color = border;
                }

                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.fontStyle = FontStyles.Normal;
                    label.fontSizeMax = primary ? 38 : 30;
                }
            }
        }

        private float MeasureSagaBriefingDescription(float columnWidth)
        {
            if (scenarioDescription == null) return 112f;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float size = fa ? 22f : 20f;
            scenarioDescription.enableAutoSizing = false;
            scenarioDescription.fontSize = scenarioDescription.fontSizeMin = scenarioDescription.fontSizeMax = size;
            scenarioDescription.maxVisibleLines = int.MaxValue;
            scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
            scenarioDescription.overflowMode = TextOverflowModes.Overflow;
            scenarioDescription.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(size, size);
            scenarioDescription.ForceMeshUpdate();
            float height = scenarioDescription.GetPreferredValues(scenarioDescription.text, Mathf.Max(160f,columnWidth - 64f), float.PositiveInfinity).y;
            height = Mathf.Max(height, scenarioDescription.renderedHeight);
            return Mathf.Clamp(Mathf.Ceil(height + 24f), 112f, 420f);
        }

        private void FitSagaMissionSizeLabels()
        {
            if (missionSizeSegmented == null) return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            foreach (var button in missionSizeSegmented.SegmentButtons ?? Array.Empty<Button>())
                if (button != null && button.targetGraphic is V3GradientGraphic face)
                    face.SetBorder(new Color(.23f, .29f, .31f, 1f), 1.5f);
            foreach (var label in missionSizeSegmented.GetComponentsInChildren<TMP_Text>(true))
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = fa ? 22 : 18;
                label.fontSizeMax = fa ? 22 : 20;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(label.fontSizeMin, label.fontSizeMax);
            }
        }

        private static Color SagaMedalTint(string key)
        {
            if (key.EndsWith("medal_regular", StringComparison.Ordinal))
                return new Color(.65f, .82f, 1f, 1);
            if (key.EndsWith("medal_recruit", StringComparison.Ordinal))
                return new Color(.9f, .62f, .37f, 1);
            if (key.EndsWith("medal_commander", StringComparison.Ordinal))
                return new Color(1f, .34f, .24f, 1);
            return Color.white;
        }
    }
}
