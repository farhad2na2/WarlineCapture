using System;
using Game.Skirmish.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Battle card in two variants driven by <see cref="SkirmishBattleCardState"/>:
    /// the map pin (header bar, big art, four-medal row, locked state) and the grid cell
    /// (art-first, one big medal for the highest earned tier, NEW ribbon).
    /// </summary>
    public static class SkirmishBattleCardWidget
    {
        public static readonly Vector2 PinReferenceSize = new Vector2(292f, 208f);
        public const float PinHeaderHeight = 40f;
        public const float PinMedalBand = 42f;

        public const string HeaderName = "Header";
        public const string ArtName = "Preview";
        public const string PendingName = "PreviewPending";
        public const string EmptyCellName = "EmptyCell";
        public const string LockName = "Locked";
        public const string BigMedalName = "BigMedal";
        public const string NewRibbonName = "NewRibbon";
        public const string IdName = "Id";

        public static bool IsLocked(SkirmishBattleCardState card) => card.Access != SkirmishCardAccess.Playable;

        /// <summary>A published, playable card that has never been won shows the NEW ribbon.</summary>
        public static bool IsNew(SkirmishBattleCardState card) =>
            !string.IsNullOrEmpty(card.ScenarioId) && card.Access == SkirmishCardAccess.Playable && card.Medals == SkirmishMedalMask.None;

        public static Vector2 PinSize(SkirmishWidgetContext context)
        {
            float narrow = context.Aspect == SkirmishWidgetAspect.Narrow ? 0.82f : 1f;
            return PinReferenceSize * context.Scale * narrow;
        }

        /// <summary>Header copy: "S001  GROUND · FIELD" (army · start), localized.</summary>
        public static string HeaderText(SkirmishBattleCardState card, SkirmishWidgetContext context)
        {
            SkirmishBattleLibraryArt.ProfileCopy(card.ArmyProfile, context.RightToLeft, out string armyFallback);
            SkirmishBattleLibraryArt.ProfileCopy(card.StartProfile, context.RightToLeft, out string startFallback);
            // Short uppercase profile codes (GROUND · FIELD) match the mockup header; loc keys only fill gaps.
            string army = !string.IsNullOrEmpty(armyFallback) ? armyFallback : context.Text(card.ArmyLocKey, card.ArmyProfile);
            string start = !string.IsNullOrEmpty(startFallback) ? startFallback : context.Text(card.StartLocKey, card.StartProfile);
            string profile = string.IsNullOrEmpty(start) ? army : army + " · " + start;
            return profile;
        }

        public static RectTransform BuildMapPin(Transform parent, SkirmishBattleCardState card, SkirmishWidgetContext context,
            Action<string> onSelect = null)
        {
            Vector2 size = PinSize(context);
            RectTransform root = CardRoot(parent, card, size, context, onSelect);
            bool locked = IsLocked(card);
            float header = context.Px(PinHeaderHeight);
            float band = context.Px(PinMedalBand);

            Image frame = root.GetComponent<Image>();
            frame.color = SkirmishWidgetFactory.Panel;

            // Header bar: id (accent) then army · start; mirrored in RTL.
            Image bar = SkirmishWidgetFactory.Image(HeaderName, root, SkirmishWidgetFactory.HeaderBar);
            SkirmishWidgetFactory.TopLeft(bar.rectTransform, 0f, 0f, size.x, header);
            float idWidth = context.Px(76f);
            float pad = context.Px(8f);
            TextMeshProUGUI id = SkirmishWidgetFactory.Label(IdName, bar.rectTransform,
                card.ScenarioId,
                context.Px(24f), SkirmishWidgetFactory.TextAccent, context, TextAlignmentOptions.MidlineLeft);
            SkirmishWidgetFactory.TopLeft(id.rectTransform, SkirmishWidgetFactory.MirrorX(pad, idWidth, size.x, context.RightToLeft), 0f, idWidth, header);
            id.isRightToLeftText = false; // scenario ids are latin codes
            TextMeshProUGUI profile = SkirmishWidgetFactory.Label("Profile", bar.rectTransform, HeaderText(card, context),
                context.Px(26f), SkirmishWidgetFactory.TextPrimary, context, TextAlignmentOptions.MidlineLeft);
            float profileWidth = size.x - idWidth - pad * 3f;
            SkirmishWidgetFactory.TopLeft(profile.rectTransform,
                SkirmishWidgetFactory.MirrorX(pad * 2f + idWidth, profileWidth, size.x, context.RightToLeft), 0f, profileWidth, header);
            if (context.RightToLeft)
            {
                // TMP ellipsis on shaped RTL text can blank the whole line; clip with the bar instead.
                profile.overflowMode = TextOverflowModes.Overflow;
                if (bar.GetComponent<RectMask2D>() == null)
                    bar.gameObject.AddComponent<RectMask2D>();
            }

            // Big art between header and medal band.
            RawImage art = SkirmishWidgetFactory.Raw(ArtName, root, context.LoadPreview(card.PreviewSpriteKey), Color.white);
            float inset = context.Px(4f);
            SkirmishWidgetFactory.TopLeft(art.rectTransform, inset, header + inset, size.x - inset * 2f, size.y - header - band - inset);
            if (art.texture == null)
                art.color = SkirmishWidgetFactory.EmptyCell;
            else
                FitCover(art, size.x - inset * 2f, size.y - header - band - inset);

            // Medal row centred in the bottom band.
            RectTransform bandRect = SkirmishWidgetFactory.Rect("MedalBand", root);
            SkirmishWidgetFactory.TopLeft(bandRect, 0f, size.y - band, size.x, band);
            SkirmishMedalRowWidget.Build(bandRect, locked ? SkirmishMedalMask.None : card.Medals, context, 26f, 12f);

            if (locked && card.Access == SkirmishCardAccess.Planned)
            {
                // approved-2: planned pins keep their art (slightly muted) with a small Coming soon pill, not a dark shade.
                if (art.texture != null)
                    art.color = new Color(0.8f, 0.82f, 0.84f, 1f);
                AddPlannedPill(root, context, header, size.y - header - band, size.x);
            }
            else if (locked)
            {
                AddLock(root, card, context, header, size);
            }

            if (card.IsSelected)
                SkirmishGlowFrameWidget.Apply(root, context);
            return root;
        }

        public static Vector2 GridCellSize(float gridWidth, float gridHeight, int columns, int rows, float gap)
        {
            columns = Mathf.Max(1, columns);
            rows = Mathf.Max(1, rows);
            return new Vector2((gridWidth - gap * (columns - 1)) / columns, (gridHeight - gap * (rows - 1)) / rows);
        }

        public static RectTransform BuildGridCell(Transform parent, SkirmishBattleCardState card, Vector2 size,
            SkirmishWidgetContext context, Action<string> onSelect = null, bool pending = false, string pendingText = null)
        {
            RectTransform root = CardRoot(parent, card, size, context, onSelect);
            Image frame = root.GetComponent<Image>();
            bool empty = string.IsNullOrEmpty(card.ScenarioId);
            bool locked = IsLocked(card);
            frame.color = SkirmishWidgetFactory.EmptyCell;

            // Art is full-bleed. A scenario without its own preview (pending) shows a crop of the battlefield plate at
            // full strength, per the contract; only cells with no scenario at all are dimmed and tagged.
            Texture texture = context.LoadPreview(card.PreviewSpriteKey);
            bool dim = empty;
            if (texture != null)
            {
                RawImage art = SkirmishWidgetFactory.Raw(ArtName, root, texture, dim ? new Color(0.42f, 0.42f, 0.42f, 1f) : Color.white);
                float inset = Mathf.Max(1f, context.Px(2f));
                SkirmishWidgetFactory.Stretch(art.rectTransform, inset, inset, inset, inset);
                FitCover(art, size.x - inset * 2f, size.y - inset * 2f);
                if (pending)
                    art.uvRect = PlateCrop(art.uvRect, card.ScenarioId);
            }

            // One big medal in the bottom trailing corner (right in LTR, left in RTL).
            float medal = Mathf.Min(context.Px(30f), size.y * 0.36f);
            float pad = context.Px(6f);
            SkirmishDifficultyId? tier = SkirmishMedalRowWidget.HighestEarned(card.Medals);
            Image big = SkirmishWidgetFactory.Image(BigMedalName, root,
                tier.HasValue ? Color.white : new Color(0.85f, 0.87f, 0.88f, 0.85f),
                context.LoadSprite(tier.HasValue
                    ? SkirmishBattleLibraryArt.Medal(true, tier.Value)
                    : SkirmishBattleLibraryArt.Medal(false, SkirmishDifficultyId.Recruit)));
            big.preserveAspect = true;
            SkirmishWidgetFactory.TopLeft(big.rectTransform,
                SkirmishWidgetFactory.MirrorX(size.x - pad - medal, medal, size.x, context.RightToLeft), size.y - pad - medal, medal, medal);
            if (big.sprite == null)
                big.color = tier.HasValue ? SkirmishMedalRowWidget.TierColor(tier.Value) : new Color(1f, 1f, 1f, 0.3f);

            if (texture != null && dim)
                AddPendingTag(root, size, context, pendingText);
            if (!empty && IsNew(card))
                AddNewRibbon(root, size, context);
            if (!empty && locked)
                AddLockBand(root, card, context, size);
            if (card.IsSelected)
                SkirmishGlowFrameWidget.Apply(root, context, null, 3f, 7f);
            return root;
        }

        private static RectTransform CardRoot(Transform parent, SkirmishBattleCardState card, Vector2 size,
            SkirmishWidgetContext context, Action<string> onSelect)
        {
            string name = string.IsNullOrEmpty(card.ScenarioId)
                ? EmptyCellName
                : SkirmishLibraryControlNames.BattleCard(card.ScenarioId);
            RectTransform root = SkirmishWidgetFactory.Rect(name, parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = size;
            Image background = root.gameObject.AddComponent<Image>();
            background.raycastTarget = true;
            if (!string.IsNullOrEmpty(card.ScenarioId))
            {
                Button button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                string id = card.ScenarioId;
                bool locked = IsLocked(card);
                button.onClick.AddListener(() =>
                {
                    context.Play(locked ? UIAudioEventKind.CardLocked : UIAudioEventKind.CardSelect);
                    onSelect?.Invoke(id);
                });
            }
            return root;
        }

        /// <summary>Picks a stable, distinct window of the plate for a scenario so neighbouring cells do not repeat.</summary>
        public static Rect PlateCrop(Rect cover, string scenarioId)
        {
            int hash = 17;
            if (!string.IsNullOrEmpty(scenarioId))
            {
                for (int i = 0; i < scenarioId.Length; i++)
                    hash = hash * 31 + scenarioId[i];
            }

            // Pick one of 12 distinct windows (4 x 3) of the plate, mirrored on alternate hashes, so neighbours never repeat.
            uint mixed = (uint)hash * 2654435761u;
            int cell = (int)(mixed % 12u);
            bool flip = ((mixed >> 8) & 1u) == 1u;
            float zoom = 0.34f;
            float w = cover.width * zoom;
            float h = cover.height * zoom;
            float fx = (cell % 4) / 3f;
            float fy = (cell / 4) / 2f;
            float x = cover.x + (cover.width - w) * fx;
            float y = cover.y + (cover.height - h) * fy;
            return flip ? new Rect(x + w, y, -w, h) : new Rect(x, y, w, h);
        }

        /// <summary>Grid lock: a bottom band with the reason wrapped to at most two lines, leaving the art visible.</summary>
        private static void AddLockBand(RectTransform root, SkirmishBattleCardState card, SkirmishWidgetContext context, Vector2 size)
        {
            float band = Mathf.Clamp(size.y * 0.42f, context.Px(34f), context.Px(56f));
            Image shade = SkirmishWidgetFactory.Image(LockName, root, new Color(0.02f, 0.03f, 0.04f, 0.72f));
            SkirmishWidgetFactory.TopLeft(shade.rectTransform, 0f, size.y - band, size.x, band);
            string fallback = card.Access == SkirmishCardAccess.Planned ? SkirmishBattleLibraryArt.ComingSoon(context.RightToLeft) : "LOCKED";
            string copy = card.Access == SkirmishCardAccess.Planned ? fallback : context.Text(card.LockedReasonLocKey, fallback);
            TextMeshProUGUI reason = SkirmishWidgetFactory.Label("Reason", shade.rectTransform, copy, context.Px(15f),
                SkirmishWidgetFactory.TextMuted, context, TextAlignmentOptions.Center);
            float medalRoom = Mathf.Min(context.Px(30f), size.y * 0.36f) + context.Px(8f);
            SkirmishWidgetFactory.Stretch(reason.rectTransform, context.RightToLeft ? medalRoom : context.Px(4f),
                context.RightToLeft ? context.Px(4f) : medalRoom, 0f, 0f);
            reason.textWrappingMode = TextWrappingModes.Normal;
            reason.overflowMode = TextOverflowModes.Ellipsis;
            reason.maxVisibleLines = 2;
        }

        private static void AddPlannedPill(RectTransform root, SkirmishWidgetContext context, float top, float artHeight, float width)
        {
            float pillWidth = Mathf.Min(width * 0.62f, context.Px(170f));
            float pillHeight = Mathf.Min(artHeight * 0.36f, context.Px(36f));
            Image pill = SkirmishWidgetFactory.Image(LockName, root, new Color(0.03f, 0.04f, 0.05f, 0.78f));
            SkirmishWidgetFactory.TopLeft(pill.rectTransform, (width - pillWidth) * 0.5f, top + (artHeight - pillHeight) * 0.5f, pillWidth, pillHeight);
            var edge = pill.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.93f, 0.78f, 0.38f, 0.85f);
            edge.effectDistance = new Vector2(1f, -1f);
            TextMeshProUGUI reason = SkirmishWidgetFactory.Label("Reason", pill.rectTransform, SkirmishBattleLibraryArt.ComingSoon(context.RightToLeft),
                context.Px(18f), SkirmishWidgetFactory.TextPrimary, context, TextAlignmentOptions.Center);
            SkirmishWidgetFactory.Stretch(reason.rectTransform, context.Px(6f), context.Px(6f), 0f, 0f);
        }

        private static void AddLock(RectTransform root, SkirmishBattleCardState card, SkirmishWidgetContext context, float top, Vector2 size)
        {
            Image shade = SkirmishWidgetFactory.Image(LockName, root, SkirmishWidgetFactory.LockShade);
            SkirmishWidgetFactory.TopLeft(shade.rectTransform, 0f, top, size.x, size.y - top);
            string fallback = card.Access == SkirmishCardAccess.Planned ? SkirmishBattleLibraryArt.ComingSoon(context.RightToLeft) : "LOCKED";
            // Planned scenarios read "Coming soon" (shared copy), not a long loc reason.
            TextMeshProUGUI reason = SkirmishWidgetFactory.Label("Reason", shade.rectTransform,
                card.Access == SkirmishCardAccess.Planned ? fallback : context.Text(card.LockedReasonLocKey, fallback), context.Px(15f), SkirmishWidgetFactory.TextMuted, context,
                TextAlignmentOptions.Center);
            SkirmishWidgetFactory.Stretch(reason.rectTransform, context.Px(6f), context.Px(6f), 0f, 0f);
            reason.textWrappingMode = TextWrappingModes.Normal;
            reason.maxVisibleLines = 2;
        }

        private static void AddPendingTag(RectTransform root, Vector2 size, SkirmishWidgetContext context, string text)
        {
            float h = Mathf.Min(context.Px(32f), size.y * 0.34f);
            Image tag = SkirmishWidgetFactory.Image(PendingName, root, new Color(0f, 0f, 0f, 0.55f));
            SkirmishWidgetFactory.TopLeft(tag.rectTransform, 0f, size.y * 0.5f - h * 0.5f, size.x, h);
            string copy = string.IsNullOrEmpty(text) ? (context.RightToLeft ? "پیش‌نمایش در راه" : "Preview pending") : text;
            TextMeshProUGUI label = SkirmishWidgetFactory.Label("Label", tag.rectTransform, copy, h * 0.75f,
                SkirmishWidgetFactory.TextMuted, context, TextAlignmentOptions.Center, false);
            SkirmishWidgetFactory.Stretch(label.rectTransform, context.Px(4f), context.Px(4f), 0f, 0f);
        }

        private static void AddNewRibbon(RectTransform root, Vector2 size, SkirmishWidgetContext context)
        {
            float w = Mathf.Min(context.Px(70f), size.x * 0.45f);
            float h = Mathf.Min(context.Px(30f), size.y * 0.28f);
            Image ribbon = SkirmishWidgetFactory.Image(NewRibbonName, root, SkirmishWidgetFactory.NewRibbon);
            SkirmishWidgetFactory.TopLeft(ribbon.rectTransform, SkirmishWidgetFactory.MirrorX(size.x - w, w, size.x, context.RightToLeft), 0f, w, h);
            TextMeshProUGUI label = SkirmishWidgetFactory.Label("Label", ribbon.rectTransform, context.RightToLeft ? "جدید" : "NEW",
                h * 0.72f, Color.white, context, TextAlignmentOptions.Center);
            SkirmishWidgetFactory.Stretch(label.rectTransform);
        }

        /// <summary>Crops the texture's uvRect so it covers the target without stretching.</summary>
        public static void FitCover(RawImage image, float width, float height)
        {
            Texture texture = image.texture;
            if (texture == null || width <= 0f || height <= 0f || texture.height == 0)
                return;
            float target = width / height;
            float source = texture.width / (float)texture.height;
            if (source > target)
            {
                float w = target / source;
                image.uvRect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else
            {
                float h = source / target;
                image.uvRect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
        }
    }
}
