using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public struct ModeResultContent
    {
        public bool Victory;
        public string Title;
        public string Identity;
        public string Status;
        public string Elapsed;
        public int Stars;
        public string ObjectiveTitle;
        public string Objective1;
        public string Objective2;
        public string Objective3;
        public string Objective1State;
        public string Objective2State;
        public string Objective3State;
        public bool HasObjectiveFacts;
        public byte CompletedObjectives;
        public string Performance1;
        public string Performance1Value;
        public string Performance2;
        public string Performance2Value;
        public string Performance3;
        public string Performance3Value;
        public string Summary;
        public string LeaveLabel;
        public bool ShowAdjust;
        public bool ActionsEnabled;
        public string Signature;
    }

    /// <summary>
    /// The campaign result board, filled with the active mode's own mission facts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CampaignStyleResultCard : MonoBehaviour
    {
        private static readonly Color DarkTop = new Color32(17, 27, 31, 250);
        private static readonly Color DarkBottom = new Color32(2, 8, 10, 252);
        private static readonly Color RowTop = new Color32(17, 29, 33, 248);
        private static readonly Color RowBottom = new Color32(5, 13, 16, 250);
        private static readonly Color Line = new Color32(83, 99, 103, 255);
        private static readonly Color Green = new Color32(112, 205, 48, 255);
        private static readonly Color Gold = new Color32(248, 177, 23, 255);
        private static readonly Color Blue = new Color32(28, 123, 194, 255);
        private static readonly Color Loss = new Color32(232, 58, 31, 255);
        private static readonly Color BodyTextColor = new Color32(245, 246, 238, 255);

        private TMP_FontAsset font;
        private TMP_Text title, identity, status, elapsed, summary, leaveLabel, adjustLabel;
        private TMP_Text[] objectiveStates;
        private TMP_Text[] objectiveLabels;
        private TMP_Text[] performanceLabels;
        private TMP_Text[] performanceValues;
        private TMP_Text missionHeading;
        private RectTransform[] filledStars;
        private V3StarGraphic[] outlinedStars;
        private Button retryButton, replayButton, leaveButton, adjustButton;
        private ResultStarSequence stars;
        private string shownSignature;

        public static byte OperationsStars(bool victory, bool partial) =>
            victory ? (byte)3 : partial ? (byte)2 : (byte)0;

        public static byte SkirmishStars(bool victory, int buildingsLost, int unitsLost)
        {
            if (!victory)
                return 0;
            int earned = 1;
            if (buildingsLost == 0)
                earned++;
            if (unitsLost <= 2)
                earned++;
            return (byte)earned;
        }

        public static CampaignStyleResultCard Mount(Transform parent, TMP_FontAsset fontAsset)
        {
            var root = new GameObject("CampaignStyleResult", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1672f, 941f);
            var card = root.AddComponent<CampaignStyleResultCard>();
            card.font = fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
            card.Build();
            return card;
        }

        public void Bind(Action retry, Action replay, Action leave, Action adjust)
        {
            retryButton.onClick.AddListener(() => retry?.Invoke());
            replayButton.onClick.AddListener(() => replay?.Invoke());
            leaveButton.onClick.AddListener(() => leave?.Invoke());
            adjustButton.onClick.AddListener(() => adjust?.Invoke());
        }

        public void Present(in ModeResultContent content)
        {
            Color accent = content.Victory ? Green : Loss;
            Set(title, content.Title);
            title.color = accent;
            Set(identity, content.Identity);
            Set(status, content.Status);
            status.color = accent;
            Set(elapsed, content.Elapsed);
            elapsed.color = content.Victory ? Blue : accent;
            Set(summary, content.Summary);
            Set(leaveLabel, content.LeaveLabel);
            adjustButton.gameObject.SetActive(content.ShowAdjust);
            retryButton.interactable = content.ActionsEnabled;
            replayButton.interactable = content.ActionsEnabled;
            leaveButton.interactable = content.ActionsEnabled;
            for (int i = 0; i < 3; i++)
            {
                string label = i == 0 ? content.Objective1 : i == 1 ? content.Objective2 : content.Objective3;
                string state = i == 0 ? content.Objective1State : i == 1 ? content.Objective2State : content.Objective3State;
                Set(objectiveLabels[i], label);
                Set(objectiveStates[i], state);
                bool completed = content.HasObjectiveFacts
                    ? (content.CompletedObjectives & (1 << i)) != 0 : IsComplete(state);
                objectiveStates[i].color = completed ? Green : Loss;
            }
            Set(performanceLabels[0], content.Performance1);
            Set(performanceLabels[1], content.Performance2);
            Set(performanceLabels[2], content.Performance3);
            Set(performanceValues[0], content.Performance1Value);
            Set(performanceValues[1], content.Performance2Value);
            Set(performanceValues[2], content.Performance3Value);
            Set(missionHeading, string.IsNullOrEmpty(content.ObjectiveTitle) ? "THIS MISSION" : content.ObjectiveTitle);
            Fit();
            if (shownSignature == content.Signature)
                return;
            shownSignature = content.Signature;
            var outlines = new GameObject[outlinedStars.Length];
            for (int i = 0; i < outlinedStars.Length; i++)
            {
                outlinedStars[i].Configure(content.Victory ? Gold : Loss, true, DarkBottom);
                outlines[i] = outlinedStars[i].gameObject;
            }
            stars.Play(filledStars, Mathf.Clamp(content.Stars, 0, 3), content.Victory, outlines);
        }

        private void Fit()
        {
            if (transform.parent is not RectTransform parent)
                return;
            float width = parent.rect.width;
            float height = parent.rect.height;
            if (width < 8f || height < 8f)
                return;
            float scale = Mathf.Min(1f, (width - 36f) / 1672f, (height - 36f) / 941f);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void Build()
        {
            Panel("Board", transform, Vector2.zero, new Vector2(1672f, 941f), DarkTop, DarkBottom, Line);
            var emblem = Panel("Emblem", transform, new Vector2(15f, -16f), new Vector2(165f, 171f), new Color32(29, 76, 20, 255), new Color32(8, 29, 9, 255), Green);
            var emblemStar = Star(emblem, new Vector2(28f, -24f), new Vector2(109f, 123f), false);
            emblemStar.Configure(Green, false, DarkBottom);
            var titlePanel = Panel("Title", transform, new Vector2(180f, -16f), new Vector2(485f, 171f), DarkTop, DarkBottom, Line);
            title = MakeLabel(titlePanel, new Vector2(28f, -27f), new Vector2(430f, 116f), 68f, Green, TextAlignmentOptions.MidlineLeft);
            var starPanel = Panel("Stars", transform, new Vector2(665f, -16f), new Vector2(333f, 171f), DarkTop, DarkBottom, Line);
            filledStars = new RectTransform[3];
            outlinedStars = new V3StarGraphic[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = Node("Star" + (i + 1), starPanel, new Vector2(18f + i * 99f, -18f), new Vector2(95f, 92f));
                filledStars[i] = Star(slot, new Vector2(10f, -6f), new Vector2(75f, 75f), false).rectTransform;
                filledStars[i].GetComponent<V3StarGraphic>().Configure(Gold, false, DarkBottom);
                outlinedStars[i] = Star(slot, new Vector2(10f, -6f), new Vector2(75f, 75f), true);
                outlinedStars[i].Configure(Loss, true, DarkBottom);
            }
            stars = gameObject.AddComponent<ResultStarSequence>();
            MakeLabel(starPanel, new Vector2(0f, -112f), new Vector2(333f, 45f), 27f, Gold, TextAlignmentOptions.Center);
            var identityPanel = Panel("Identity", transform, new Vector2(998f, -16f), new Vector2(456f, 171f), DarkTop, DarkBottom, Line);
            identity = MakeLabel(identityPanel, new Vector2(28f, -18f), new Vector2(400f, 86f), 30f, BodyTextColor, TextAlignmentOptions.TopLeft);
            status = MakeLabel(identityPanel, new Vector2(28f, -110f), new Vector2(400f, 44f), 26f, Green, TextAlignmentOptions.MidlineLeft);
            var timePanel = Panel("Time", transform, new Vector2(1464f, -16f), new Vector2(193f, 171f), DarkTop, DarkBottom, Line);
            elapsed = MakeLabel(timePanel, new Vector2(15f, -58f), new Vector2(163f, 56f), 32f, Blue, TextAlignmentOptions.Center);

            var objectives = Panel("Objectives", transform, new Vector2(15f, -203f), new Vector2(980f, 430f), DarkTop, DarkBottom, Line);
            Set(MakeLabel(objectives, new Vector2(28f, -16f), new Vector2(400f, 48f), 29f, Green, TextAlignmentOptions.MidlineLeft), "OBJECTIVES");
            objectiveLabels = new TMP_Text[3];
            objectiveStates = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var row = Panel("Objective" + i, objectives, new Vector2(16f, -78f - i * 110f), new Vector2(948f, 96f), RowTop, RowBottom, Color.clear);
                objectiveLabels[i] = MakeLabel(row, new Vector2(24f, -16f), new Vector2(640f, 64f), 26f, BodyTextColor, TextAlignmentOptions.MidlineLeft);
                objectiveStates[i] = MakeLabel(row, new Vector2(680f, -16f), new Vector2(244f, 64f), 24f, Green, TextAlignmentOptions.MidlineRight);
            }

            var performance = Panel("Performance", transform, new Vector2(1011f, -203f), new Vector2(646f, 250f), DarkTop, DarkBottom, Line);
            Set(MakeLabel(performance, new Vector2(24f, -12f), new Vector2(400f, 48f), 27f, Blue, TextAlignmentOptions.MidlineLeft), "PERFORMANCE");
            performanceLabels = new TMP_Text[3];
            performanceValues = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var row = Panel("Stat" + i, performance, new Vector2(16f, -68f - i * 56f), new Vector2(614f, 50f), RowTop, RowBottom, Color.clear);
                performanceLabels[i] = MakeLabel(row, new Vector2(20f, 0f), new Vector2(360f, 50f), 22f, BodyTextColor, TextAlignmentOptions.MidlineLeft);
                performanceValues[i] = MakeLabel(row, new Vector2(390f, 0f), new Vector2(200f, 50f), 22f, Blue, TextAlignmentOptions.MidlineRight);
            }

            var match = Panel("MatchFacts", transform, new Vector2(1011f, -469f), new Vector2(646f, 164f), DarkTop, DarkBottom, Line);
            missionHeading = MakeLabel(match, new Vector2(24f, -48f), new Vector2(598f, 68f), 28f, Blue, TextAlignmentOptions.MidlineLeft);

            var summaryPanel = Panel("Summary", transform, new Vector2(15f, -790f), new Vector2(754f, 134f), DarkTop, DarkBottom, Line);
            summary = MakeLabel(summaryPanel, new Vector2(28f, -22f), new Vector2(698f, 90f), 24f, BodyTextColor, TextAlignmentOptions.MidlineLeft);
            retryButton = ActionButton("Retry", new Vector2(785f, -790f), new Color32(190, 48, 27, 255), new Color32(76, 13, 10, 255), Loss, "RETRY");
            replayButton = ActionButton("Replay", new Vector2(1075f, -790f), new Color32(69, 143, 36, 255), new Color32(17, 63, 19, 255), Green, "REPLAY");
            leaveButton = ActionButton("Leave", new Vector2(1365f, -790f), DarkTop, DarkBottom, Line, "RETURN");
            leaveLabel = leaveButton.GetComponentInChildren<TMP_Text>();
            adjustButton = ActionButton("Adjust", new Vector2(1011f, -649f), DarkTop, DarkBottom, Line, "ADJUST SETUP");
            adjustLabel = adjustButton.GetComponentInChildren<TMP_Text>();
            adjustButton.gameObject.SetActive(false);
        }

        private Button ActionButton(string name, Vector2 position, Color top, Color bottom, Color border, string label)
        {
            var rect = Panel(name, transform, position, new Vector2(280f, 134f), top, bottom, border);
            var graphic = rect.GetComponent<V3GradientGraphic>();
            graphic.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            MakeLabel(rect, new Vector2(12f, -28f), new Vector2(256f, 78f), 32f, BodyTextColor, TextAlignmentOptions.Center);
            Set(rect.GetComponentInChildren<TMP_Text>(), label);
            return button;
        }

        private static bool IsComplete(string state) =>
            !string.IsNullOrEmpty(state) &&
            (state.IndexOf("COMPLETE", StringComparison.OrdinalIgnoreCase) >= 0 ||
             state.IndexOf("HELD", StringComparison.OrdinalIgnoreCase) >= 0);

        private RectTransform Panel(string name, Transform parent, Vector2 topLeft, Vector2 size, Color top, Color bottom, Color border)
        {
            var rect = Node(name, parent, topLeft, size);
            var graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, 3f);
            graphic.raycastTarget = false;
            return rect;
        }

        private static RectTransform Node(string name, Transform parent, Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
            return rect;
        }

        private V3StarGraphic Star(Transform parent, Vector2 topLeft, Vector2 size, bool outlined)
        {
            var rect = Node(outlined ? "Outline" : "Filled", parent, topLeft, size);
            var star = rect.gameObject.AddComponent<V3StarGraphic>();
            star.raycastTarget = false;
            return star;
        }

        private TMP_Text MakeLabel(Transform parent, Vector2 topLeft, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var rect = Node("Label", parent, topLeft, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(16f, fontSize * 0.55f);
            label.fontSizeMax = fontSize;
            return label;
        }

        private static void Set(TMP_Text label, string value) => UiLocalizedText.Set(label, value ?? string.Empty);
    }

    [DisallowMultipleComponent]
    public sealed class ResultStarSequence : MonoBehaviour
    {
        private RectTransform[] stars;
        private GameObject[] outlines;
        private float[] startAt;
        private int earned;
        private bool playing;

        public void Play(RectTransform[] filled, int count, bool victory, GameObject[] outlineRoots = null)
        {
            stars = filled;
            outlines = outlineRoots;
            earned = Mathf.Clamp(count, 0, filled != null ? filled.Length : 0);
            bool instant = !Application.isPlaying || !victory || earned == 0 ||
                           SettingsService.Load().Accessibility.ReducedMotion;
            if (filled != null)
            {
                for (int i = 0; i < filled.Length; i++)
                {
                    if (filled[i] == null)
                        continue;
                    bool show = i < earned;
                    filled[i].gameObject.SetActive(show);
                    filled[i].localScale = instant && show ? Vector3.one : Vector3.zero;
                    SetOutline(i, !show || !instant);
                }
            }
            if (instant)
            {
                playing = false;
                return;
            }
            startAt = new float[earned];
            float now = Time.unscaledTime;
            for (int i = 0; i < earned; i++)
                startAt[i] = now + 0.28f + i * 0.46f;
            playing = true;
        }

        private void SetOutline(int index, bool visible)
        {
            if (outlines == null || index < 0 || index >= outlines.Length || outlines[index] == null)
                return;
            outlines[index].SetActive(visible);
        }

        private void Update()
        {
            if (!playing || stars == null)
                return;
            float now = Time.unscaledTime;
            bool finished = true;
            for (int i = 0; i < earned; i++)
            {
                if (stars[i] == null)
                    continue;
                float age = now - startAt[i];
                if (age < 0f)
                {
                    finished = false;
                    stars[i].localScale = Vector3.zero;
                    SetOutline(i, true);
                    continue;
                }
                SetOutline(i, false);
                float u = Mathf.Clamp01(age / 0.34f);
                if (u < 1f)
                    finished = false;
                float scale = u < 0.62f
                    ? Mathf.Lerp(0.15f, 1.32f, u / 0.62f)
                    : Mathf.Lerp(1.32f, 1f, (u - 0.62f) / 0.38f);
                stars[i].localScale = Vector3.one * scale;
            }
            if (finished)
                playing = false;
        }
    }
}
