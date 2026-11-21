using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Card = Game.UI.Runtime.SkirmishObjectiveCardLayout;

namespace Game.UI.Runtime
{
    /// <summary>
    /// The compact objective card (W40, Boards 2 and 4; owner approval 2026-11-20). One charcoal card
    /// with a warm keyline under the resource bar: purpose and next action first, then the timer, then
    /// the status row of existing focus controls and the score. Ownership reads as friendly green with a
    /// filled shield, enemy red-orange with a diamond, neutral silver with a hollow ring. Presentation
    /// only: the controls keep their GameObjects, handlers and raycasts, and nothing here issues orders.
    /// </summary>
    public sealed partial class SkirmishMatchView
    {
        private RectTransform objectiveHeader, objectiveStatus, objectiveClockPanel, resourceStrip;
        private HudOwnershipGlyphGraphic objectiveGlyph;
        private TMP_Text objectivePurpose, objectiveNextAction, objectiveScore;
        private string objectivePurposeShown, objectiveNextShown;
        private int objectiveCardSignature, resourceStripSearchFrame = -1000;
        private float objectiveTimerWidth = Card.TimerMinWidth;
        private readonly Vector3[] resourceCorners = new Vector3[4];

        /// <summary>True while the card's next-action line shows the mission's first order (S026).</summary>
        public bool ObjectiveCardShowsFirstOrder { get; private set; }

        private RectTransform BuildObjectiveCard()
        {
            var card = Panel("BaseObjectives", transform);
            card.anchorMin = card.anchorMax = new Vector2(Card.CentreX, 1f);
            card.pivot = new Vector2(.5f, 1f);
            card.sizeDelta = new Vector2(Card.MinWidth, Card.Height);
            card.anchoredPosition = new Vector2(0f, -Card.DefaultTop);
            card.GetComponent<V3GradientGraphic>().Configure(Card.CardTop, Card.CardBottom, Card.Keyline, Card.KeylineWidth);

            objectiveHeader = Panel("ObjectiveHeader", card, false);
            TopBand(objectiveHeader, Card.Padding, Card.HeaderHeight);
            var glyph = Panel("ObjectiveGlyph", objectiveHeader, false);
            glyph.sizeDelta = Vector2.one * Card.GlyphSize;
            objectiveGlyph = glyph.gameObject.AddComponent<HudOwnershipGlyphGraphic>();
            objectiveGlyph.Present(HudOwnershipGlyph.Objective, Card.ObjectiveAmber);
            objectivePurpose = HeaderLabel("ObjectivePurpose", Card.PurposeSize, Card.PurposeSize * .6f, false, Color.white);
            objectiveNextAction = HeaderLabel("ObjectiveNextAction", Card.NextActionMaxSize, Card.NextActionMinSize, true, Card.DetailText);
            CreateClock(objectiveHeader);
            objectiveClockPanel = (RectTransform)clock.transform.parent;

            objectiveStatus = Panel("ObjectiveStatus", card, false);
            TopBand(objectiveStatus, Card.Padding + Card.HeaderHeight + Card.RowGap, Card.ControlHeight);
            var row = objectiveStatus.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = Card.ChipGap; row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            PlaceObjectiveHeader(UiShellRuntimeGateway.Localization.IsRightToLeft, objectiveTimerWidth);
            return card;
        }

        private TMP_Text HeaderLabel(string name, float max, float min, bool wrap, Color colour)
        {
            var label = Label(objectiveHeader, "", max);
            label.gameObject.name = name;
            label.color = colour;
            label.enableAutoSizing = true; label.fontSizeMax = max; label.fontSizeMin = min;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static void TopBand(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1f);
            rect.offsetMin = new Vector2(Card.Padding, -(top + height));
            rect.offsetMax = new Vector2(-Card.Padding, -top);
        }

        // Runs after the base, goal and guidance presenters have set this frame's copy.
        private void PresentObjectiveCard(in UiSkirmishModel model)
        {
            if (hud == null || !hud.activeSelf) return;
            var text = UiShellRuntimeGateway.Localization;
            bool rtl = text.IsRightToLeft;
            var goal = objectiveModel != null && objectiveModel.Available ? objectiveModel.Goal : null;
            string purpose = Card.Purpose(goal != null ? goal.ObjectiveKind : SkirmishObjectiveKind.BaseAssault, rtl);
            if (purpose != objectivePurposeShown) { objectivePurposeShown = purpose; UiLocalizedText.Set(objectivePurpose, purpose); }
            // S026 (W40): the opening first order is the card's next action, not a second centre bar.
            ObjectiveCardShowsFirstOrder = !string.IsNullOrEmpty(model.FirstOrderText);
            string next = ObjectiveCardShowsFirstOrder ? model.FirstOrderText : ObjectiveSentence(goal, model.Objective, rtl);
            if (next != objectiveNextShown) { objectiveNextShown = next; UiLocalizedText.Set(objectiveNextAction, next); }
            BoundFont(objectivePurpose, Card.PurposeSize * .6f, Card.PurposeSize);
            BoundFont(objectiveNextAction, Card.NextActionMinSize, Card.NextActionMaxSize);

            if (objectiveModel == null)
            {
                PresentChip(PlayerFocusButton, playerHealth, HudOwnership.Friendly, null, -1f, HudOwnership.Neutral);
                PresentChip(EnemyFocusButton, enemyHealth, HudOwnership.Enemy, null, -1f, HudOwnership.Neutral);
            }
            else
            {
                PresentGoalChip(PlayerFocusButton, playerHealth, firstPublicGoal, goal, rtl);
                PresentGoalChip(EnemyFocusButton, enemyHealth, secondPublicGoal, goal, rtl);
                if (thirdObjective != null) PresentGoalChip(thirdObjective.GetComponentInParent<Button>(), thirdObjectiveProgress, thirdPublicGoal, goal, rtl);
            }
            FitObjectiveCard(rtl);
        }

        // Frontline Control, Breakthrough and Convoy Escort use their family's objective copy: the
        // session objective line is the Base Assault sentence for those catalog entries (live S031).
        private static string ObjectiveSentence(SkirmishPublicGoalSnapshot goal, string sessionObjective, bool rtl)
        {
            string key = goal != null ? Card.ObjectiveCopyKey(goal.ObjectiveKind, goal.Perspective) : null;
            if (key != null)
                foreach (var row in Game.Configs.SkirmishObjectiveCopyTable.Rows)
                    if (row.Key == key) return UiShellRuntimeGateway.Localization.Get(key, rtl ? row.Farsi : row.English);
            return sessionObjective ?? string.Empty;
        }

        private void PresentGoalChip(Button button, TMP_Text detail, string publicId, SkirmishPublicGoalSnapshot goal, bool rtl)
        {
            if (button == null) return;
            if (goal != null && !string.IsNullOrEmpty(publicId))
                for (int i = 0; i < goal.NodeCount; i++)
                {
                    var node = goal.Node(i);
                    if (node.PublicId != publicId || node.Kind != SkirmishPublicGoalNodeKind.CaptureZone) continue;
                    // Ownership has its own line; contested state and capture progress stay on the title line.
                    PresentChip(button, detail, Card.Resolve(node.OwnerFaction, goal.ObserverFaction), ZoneStatusTag(node, rtl),
                        node.CaptureProgressKnown ? node.CaptureFraction : -1f, Card.Resolve(node.CapturingFaction, goal.ObserverFaction));
                    return;
                }
            PresentChip(button, detail, null, null, -1f, HudOwnership.Neutral);
        }

        private void PresentChip(Button button, TMP_Text detail, HudOwnership? owner, string tag, float capture, HudOwnership capturer)
        {
            if (button == null || detail == null) return;
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            var chip = button.transform;

            var glyph = ChipChild<HudOwnershipGlyphGraphic>(chip, "OwnershipGlyph", owner.HasValue);
            if (glyph != null)
            {
                glyph.gameObject.SetActive(owner.HasValue);
                if (owner.HasValue)
                {
                    var rect = glyph.rectTransform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(rtl ? 1f : 0f, .25f);
                    rect.sizeDelta = Vector2.one * Card.OwnershipGlyphSize;
                    rect.anchoredPosition = new Vector2(rtl ? -Card.ChipInset : Card.ChipInset, 0f);
                    glyph.Present(Card.Glyph(owner.Value), Card.Colour(owner.Value));
                }
            }

            float lead = Card.ChipInset + (owner.HasValue ? Card.OwnershipGlyphSize + Card.ChipGlyphGap : 0f);
            var line = detail.rectTransform;
            line.anchorMin = Vector2.zero; line.anchorMax = new Vector2(1f, .5f);
            line.offsetMin = new Vector2(rtl ? Card.ChipInset : lead, 3f);
            line.offsetMax = new Vector2(rtl ? -lead : -Card.ChipInset, 0f);
            detail.alignment = rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            detail.textWrappingMode = TextWrappingModes.NoWrap;
            detail.overflowMode = TextOverflowModes.Overflow;
            detail.color = Card.DetailText;
            BoundFont(detail, Card.DetailMinSize, Card.DetailMaxSize);

            bool tagged = !string.IsNullOrEmpty(tag);
            var status = ChipLabel(chip, "ChipStatus", tagged);
            float tagWidth = 0f;
            if (status != null)
            {
                status.gameObject.SetActive(tagged);
                if (tagged)
                {
                    if (StatusSource(status) != tag) UiLocalizedText.Set(status, tag);
                    var rect = status.rectTransform;
                    rect.anchorMin = new Vector2(0f, .5f); rect.anchorMax = Vector2.one;
                    rect.offsetMin = new Vector2(Card.ChipInset, 0f); rect.offsetMax = new Vector2(-Card.ChipInset, -2f);
                    status.alignment = rtl ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
                    status.textWrappingMode = TextWrappingModes.NoWrap;
                    status.color = Card.StatusTag;
                    BoundFont(status, Card.TagSize, Card.TagSize);
                    tagWidth = Preferred(status, Card.TagSize);
                }
            }
            HudIconButtonView.SetTitleTrailingInset(button, tagWidth > 0f ? tagWidth + Card.TagGap : 0f);

            var bar = ChipChild<Image>(chip, "CaptureProgress", capture >= 0f);
            if (bar != null)
            {
                bar.gameObject.SetActive(capture >= 0f);
                if (capture >= 0f)
                {
                    float fraction = Mathf.Clamp01(capture);
                    var rect = bar.rectTransform;
                    rect.anchorMin = new Vector2(rtl ? 1f - fraction : 0f, 0f); rect.anchorMax = new Vector2(rtl ? 1f : fraction, 0f);
                    rect.pivot = new Vector2(rtl ? 1f : 0f, 0f);
                    rect.offsetMin = new Vector2(rtl ? 0f : Card.KeylineWidth, Card.KeylineWidth);
                    rect.offsetMax = new Vector2(rtl ? -Card.KeylineWidth : 0f, Card.KeylineWidth + Card.CaptureBarHeight);
                    bar.color = Card.Colour(capturer);
                }
            }
        }

        private static T ChipChild<T>(Transform chip, string name, bool create) where T : Graphic
        {
            var existing = chip.Find(name);
            if (existing != null) return existing.GetComponent<T>();
            if (!create) return null;
            var rect = Panel(name, chip, false);
            var graphic = rect.gameObject.AddComponent<T>();
            graphic.raycastTarget = false;
            return graphic;
        }

        private static TMP_Text ChipLabel(Transform chip, string name, bool create)
        {
            var existing = chip.Find(name);
            if (existing != null) return existing.GetComponent<TMP_Text>();
            if (!create) return null;
            var label = Label(chip, "", Card.TagSize);
            label.gameObject.name = name;
            return label;
        }

        private static string StatusSource(TMP_Text label) =>
            label is RTLTMPro.RTLTextMeshPro localized ? localized.OriginalText : label.text;

        private static void BoundFont(TMP_Text label, float min, float max)
        {
            if (label == null) return;
            label.enableAutoSizing = true;
            label.fontSizeMin = min; label.fontSizeMax = max;
            label.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(min, max);
        }

        // Width of the label's current text at a given point size (TMP measures at fontSizeMax when auto-sizing).
        private static float Preferred(TMP_Text label, float size)
        {
            if (label == null || !label.gameObject.activeInHierarchy || string.IsNullOrEmpty(label.text)) return 0f;
            float basis = label.enableAutoSizing ? label.fontSizeMax : label.fontSize;
            return label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity).x * size / Mathf.Max(1f, basis);
        }

        private TMP_Text ChipTitle(Transform chip) =>
            chip == player.transform.parent ? player : chip == enemy.transform.parent ? enemy :
            thirdObjective != null && chip == thirdObjective.transform.parent ? thirdObjective : null;

        private TMP_Text ChipDetail(Transform chip) =>
            chip == player.transform.parent ? playerHealth : chip == enemy.transform.parent ? enemyHealth :
            thirdObjective != null && chip == thirdObjective.transform.parent ? thirdObjectiveProgress : null;

        // Sizes the controls to their text, then the card to its rows, only when the copy changes.
        private void FitObjectiveCard(bool rtl)
        {
            float canvasWidth = ((RectTransform)transform).rect.width;
            int signature = 17;
            unchecked
            {
                signature = signature * 31 + (rtl ? 1 : 0);
                signature = signature * 31 + Mathf.RoundToInt(canvasWidth);
                signature = signature * 31 + (objectivePurpose.text?.GetHashCode() ?? 0);
                signature = signature * 31 + (objectiveNextAction.text?.GetHashCode() ?? 0);
                signature = signature * 31 + (clockHeading.text?.GetHashCode() ?? 0);
                signature = signature * 31 + (clock.text?.Length ?? 0);
                foreach (Transform child in objectiveStatus)
                {
                    signature = signature * 31 + (child.gameObject.activeSelf ? 1 : 2);
                    foreach (var label in child.GetComponentsInChildren<TMP_Text>(false))
                        signature = signature * 31 + (label.text?.GetHashCode() ?? 0);
                }
            }
            if (signature == objectiveCardSignature) return;
            objectiveCardSignature = signature;

            objectiveTimerWidth = Card.TimerWidth(Preferred(clockHeading, Card.CaptionSize), Preferred(clock, Card.TimerSize));
            PlaceObjectiveHeader(rtl, objectiveTimerWidth);

            float status = 0f; int count = 0;
            foreach (Transform child in objectiveStatus)
            {
                if (!child.gameObject.activeSelf) continue;
                var element = child.GetComponent<LayoutElement>();
                if (element == null) continue;
                float width;
                if (child.GetComponent<Button>() != null)
                {
                    var tag = child.Find("ChipStatus");
                    var tagLabel = tag != null && tag.gameObject.activeSelf ? tag.GetComponent<TMP_Text>() : null;
                    var glyph = child.Find("OwnershipGlyph");
                    width = Card.ChipWidth(Preferred(ChipTitle(child), Card.TitleMaxSize), Preferred(ChipDetail(child), Card.DetailMaxSize),
                        glyph != null && glyph.gameObject.activeSelf, Preferred(tagLabel, Card.TagSize));
                }
                else width = Card.ScoreWidth(Preferred(objectiveScore, Card.ScoreSize));
                element.preferredWidth = width;
                element.minWidth = -1f;
                element.flexibleWidth = 1f;
                status += width; count++;
            }
            status += Mathf.Max(0, count - 1) * Card.ChipGap;
            float header = Card.HeaderWidth(Preferred(objectivePurpose, Card.PurposeSize), Preferred(objectiveNextAction, Card.NextActionMaxSize), objectiveTimerWidth, !rtl);
            var rect = (RectTransform)hud.transform;
            rect.sizeDelta = new Vector2(Card.CardWidth(status, header, canvasWidth), Card.Height);
        }

        private void PlaceObjectiveHeader(bool rtl, float timer)
        {
            var glyph = objectiveGlyph.rectTransform;
            glyph.anchorMin = glyph.anchorMax = glyph.pivot = new Vector2(rtl ? 1f : 0f, .5f);
            glyph.anchoredPosition = Vector2.zero;
            objectiveClockPanel.anchorMin = new Vector2(rtl ? 0f : 1f, 0f);
            objectiveClockPanel.anchorMax = new Vector2(rtl ? 0f : 1f, 1f);
            objectiveClockPanel.pivot = new Vector2(rtl ? 0f : 1f, .5f);
            objectiveClockPanel.sizeDelta = new Vector2(timer, 0f);
            objectiveClockPanel.anchoredPosition = Vector2.zero;
            float lead = Card.GlyphSize + Card.GlyphGap, trail = timer + Card.GlyphGap;
            var purpose = objectivePurpose.rectTransform;
            purpose.anchorMin = new Vector2(0f, Card.PurposeSplit); purpose.anchorMax = Vector2.one;
            purpose.offsetMin = new Vector2(rtl ? trail : lead, 0f); purpose.offsetMax = new Vector2(rtl ? -lead : -trail, 0f);
            objectivePurpose.alignment = rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            var next = objectiveNextAction.rectTransform;
            next.anchorMin = Vector2.zero; next.anchorMax = new Vector2(1f, Card.PurposeSplit);
            next.offsetMin = new Vector2(rtl ? trail : lead, 0f); next.offsetMax = new Vector2(rtl ? -lead : -trail, 0f);
            objectiveNextAction.alignment = rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.TopLeft;
            // English wraps onto two lines; Persian stays on one line (its line box is about twice its size).
            objectiveNextAction.textWrappingMode = rtl ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
        }

        /// <summary>Canvas units from the top to the resource bar's bottom edge, or 0 when it is not shown.</summary>
        private float ResourceBarBottom()
        {
            if (resourceStrip == null || !resourceStrip.gameObject.activeInHierarchy)
            {
                resourceStrip = null;
                if (Time.frameCount - resourceStripSearchFrame < 30) return 0f;
                resourceStripSearchFrame = Time.frameCount;
                var found = GameObject.Find("ResourceStrip");
                resourceStrip = found != null ? found.transform as RectTransform : null;
                if (resourceStrip == null) return 0f;
            }
            var canvas = resourceStrip.GetComponentInParent<Canvas>();
            if (canvas == null) return 0f;
            var root = canvas.rootCanvas;
            resourceStrip.GetWorldCorners(resourceCorners);
            var screen = RectTransformUtility.WorldToScreenPoint(root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera, resourceCorners[0]);
            var own = (RectTransform)transform;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(own, screen, null, out var local) ? own.rect.yMax - local.y : 0f;
        }
    }
}
