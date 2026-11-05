using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Skirmish.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private RectTransform _objectiveColumn;
        private RectTransform _progressTrack;
        private Image _progressFill;
        private RectTransform _overallStrip;
        private Image _overallFill;
        private RectTransform _statRow;
        private RawImage _briefingPreview;
        private RectTransform _medalGrid;
        private Image _crestImage;
        private bool _libraryLayoutHooked;
        private bool _suppressSavedMatch;

        /// <summary>
        /// Visible tier counts for every library level. The default reads the view-model
        /// projection of <c>SkirmishMedalLedger</c>: <see cref="SkirmishTheaterState.OverallMedalTotals"/>,
        /// filled by <c>CountTier</c> from <c>SkirmishMedalLedger.TotalsByDifficulty</c>.
        /// Replace the delegate only to point at a different projection of that same ledger.
        /// </summary>
        public static Func<SkirmishTheaterState, SkirmishMedalTotals> ReadMedalCounts = ReadViewModelMedalProjection;

        /// <summary>The view-model projection of the medal ledger. This is the strip's default source.</summary>
        public static SkirmishMedalTotals ReadViewModelMedalProjection(SkirmishTheaterState theater) =>
            theater.OverallMedalTotals;

        /// <summary>
        /// The capture save. One row per catalog id and difficulty, the same shape as <c>skirmishMedals</c>.
        /// S001 and S002 are Desert Base, S025 is City Crossroads, S073 is Industrial Basin.
        /// </summary>
        public static readonly (string CatalogId, SkirmishDifficultyId Difficulty)[] CaptureMedalGrants =
        {
            ("S001", SkirmishDifficultyId.Recruit),
            ("S001", SkirmishDifficultyId.Regular),
            ("S002", SkirmishDifficultyId.Recruit),
            ("S025", SkirmishDifficultyId.Regular),
            ("S073", SkirmishDifficultyId.Recruit)
        };

        public void ApplyLibraryCaptureDemo(Action<SkirmishDifficultyId> setLaunchDifficulty = null)
        {
            _suppressSavedMatch = true;
            EnsureLibraryCards();
            var medals = new CaptureMedalReader(_catalogEntries);
            for (int i = 0; i < CaptureMedalGrants.Length; i++)
                medals.Grant(CaptureMedalGrants[i].CatalogId, CaptureMedalGrants[i].Difficulty);
            var model = new SkirmishLibraryViewModel(
                _catalogEntries,
                medals,
                _ => ContentAccessState.Allowed,
                showPlanned: true,
                prefs: new CaptureLibraryPrefs(),
                setLaunchDifficulty: setLaunchDifficulty);
            UseLibraryViewModel(model);
        }

        private sealed class CaptureMedalReader : ISkirmishMedalReader
        {
            private readonly IReadOnlyList<SkirmishBattleCatalogEntry> _catalog;
            private readonly HashSet<string> _earned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public CaptureMedalReader(IReadOnlyList<SkirmishBattleCatalogEntry> catalog) =>
                _catalog = catalog ?? Array.Empty<SkirmishBattleCatalogEntry>();

            public void Grant(string catalogId, SkirmishDifficultyId difficulty) =>
                _earned.Add(Key(catalogId, difficulty));

            public bool Has(string catalogId, SkirmishDifficultyId difficulty) =>
                _earned.Contains(Key(catalogId, difficulty));

            public int CountTier(SkirmishDifficultyId difficulty)
            {
                string tier = ((byte)difficulty).ToString();
                int count = 0;
                foreach (string key in _earned)
                {
                    int bar = key.LastIndexOf('|');
                    if (bar >= 0 && bar < key.Length - 1 && key.Substring(bar + 1) == tier)
                        count++;
                }

                return count;
            }

            public int CountFor(string mapId) => CountFor(mapId, null);

            public int CountFor(string mapId, string objectiveId)
            {
                if (string.IsNullOrEmpty(mapId) || _catalog == null)
                    return 0;
                int count = 0;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < _catalog.Count; i++)
                {
                    SkirmishBattleCatalogEntry entry = _catalog[i];
                    if (!string.Equals(entry.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!string.IsNullOrEmpty(objectiveId) &&
                        !string.Equals(entry.ObjectiveId, objectiveId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!seen.Add(entry.ScenarioId))
                        continue;
                    if (Has(entry.ScenarioId, SkirmishDifficultyId.Recruit) ||
                        Has(entry.ScenarioId, SkirmishDifficultyId.Regular) ||
                        Has(entry.ScenarioId, SkirmishDifficultyId.Veteran) ||
                        Has(entry.ScenarioId, SkirmishDifficultyId.Commander))
                        count++;
                }

                return count;
            }

            public void Refresh()
            {
            }

            private static string Key(string catalogId, SkirmishDifficultyId difficulty) =>
                (catalogId ?? string.Empty) + "|" + (byte)difficulty;
        }

        private sealed class CaptureLibraryPrefs : ISkirmishLibraryPrefs
        {
            private readonly System.Collections.Generic.Dictionary<string, string> _values =
                new System.Collections.Generic.Dictionary<string, string>();

            public string Get(string key, string fallback) =>
                _values.TryGetValue(key ?? string.Empty, out string value) ? value : fallback ?? string.Empty;

            public void Set(string key, string value) => _values[key ?? string.Empty] = value ?? string.Empty;

            public void Save()
            {
            }
        }

        private void EnsureLibraryPass(Transform composition, Transform preview, Transform rules)
        {
            EnsureLibraryBackdrop();
            if (composition != null && !_libraryLayoutHooked)
            {
                MainMenuV3SectionLayoutView layout = composition.GetComponent<MainMenuV3SectionLayoutView>();
                if (layout != null)
                {
                    layout.LayoutApplied += OnLibraryLayoutApplied;
                    _libraryLayoutHooked = true;
                }
            }

            Transform plate = preview != null ? preview.Find("MapPreviewClip") : null;
            if (plate is RectTransform plateRect)
            {
                _objectiveColumn = FindOrCreate(plate, "ObjectiveColumn");
                MaskRect(plateRect);
            }

            if (preview is RectTransform previewRect)
                MaskRect(previewRect);

            if (_libraryGrid != null)
                MaskRect(_libraryGrid);
            if (rules is RectTransform rulesRect)
                MaskRect(rulesRect);

            if (rules != null)
            {
                _briefingPreview = AdoptBriefingWidget(rules, "BriefingPreview").GetComponent<RawImage>();
                if (_briefingPreview == null)
                    _briefingPreview = AdoptBriefingWidget(rules, "BriefingPreview").gameObject.AddComponent<RawImage>();

                _progressTrack = AdoptBriefingWidget(rules, "BattlesWonTrack");
                if (_progressTrack.Find("Fill") == null)
                {
                    var bar = new GameObject("Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    bar.transform.SetParent(_progressTrack, false);
                    PlaceTopLeft(bar.rectTransform, 0f, 30f, 540f, 16f);
                    bar.color = new Color(0.08f, 0.12f, 0.14f, 1f);
                    bar.raycastTarget = false;
                    var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    fill.transform.SetParent(bar.transform, false);
                    fill.color = new Color(0.91f, 0.72f, 0.22f, 1f);
                    fill.raycastTarget = false;
                    _progressFill = fill;
                }
                else
                    _progressFill = _progressTrack.Find("Bar/Fill")?.GetComponent<Image>() ?? _progressTrack.Find("Fill")?.GetComponent<Image>();

                if (_progressTrack.Find("Caption") == null)
                {
                    var caption = CreateBoundLabel(_progressTrack, "Caption", "ui.skirmish.library.zone_progress", "ZONE PROGRESS");
                    PlaceTopLeft(caption.rectTransform, 0f, 0f, 340f, 28f);
                    caption.fontSize = 18f;
                    caption.alignment = TextAlignmentOptions.MidlineLeft;
                }

                if (_progressTrack.Find("Count") == null)
                {
                    var count = CreateBoundLabel(_progressTrack, "Count", "ui.skirmish.library_battles_count", "0/0");
                    PlaceTopLeft(count.rectTransform, 360f, 0f, 160f, 28f);
                    count.alignment = TextAlignmentOptions.MidlineRight;
                    count.fontSize = 18f;
                    count.isRightToLeftText = false;
                }

                _statRow = AdoptBriefingWidget(rules, "BriefingStats");
                EnsureStatTile(_statRow, "Time", 0f, "TIME");
                EnsureStatTile(_statRow, "Forces", 132f, "FORCES");
                EnsureStatTile(_statRow, "Size", 264f, "SIZE");
                EnsureStatTile(_statRow, "Credits", 396f, "CREDITS");
                HideLegacyStat(_statRow, "Army");
                HideLegacyStat(_statRow, "Start");
                HideLegacyStat(_statRow, "Objective");
                _medalGrid = FindOrCreate(rules, "MedalGrid");
            }

            EnsureOverallProgressStrip(preview);

            EnsureOrnateCorners(preview as RectTransform);
            EnsureOrnateCorners(rules as RectTransform);

            Transform title = composition != null ? composition.Find("ScreenTitlePanel") : null;
            if (title != null && title.Find("Crest") == null)
            {
                _crestImage = new GameObject("Crest", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                _crestImage.transform.SetParent(title, false);
                PlaceTopLeft(_crestImage.rectTransform, 8f, 4f, 48f, 48f);
                ShowIcon(_crestImage, SkirmishBattleLibraryArt.Crest, Color.white);
            }
            else if (title != null)
                _crestImage = title.Find("Crest")?.GetComponent<Image>();
        }

        private bool _refittingSaga;
        private bool _refittingTheatre;
        private Vector4 _sagaTheatreLayoutStamp;
        private bool _sagaTheatreLayoutRtl;
        private string _sagaTheatreLayoutSelection;

        private void OnLibraryLayoutApplied()
        {
            if (approvedSkirmishAtlas != null) return;
            if (_libraryPresenting || _refittingTheatre)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
            PlaceLibraryChrome();
            StyleCommandButtons(battlefield);
            PlaceLibraryFooter();
            ComposeBriefingColumn(battlefield);
            ApplyLibraryDepth();
            PresentZoomHint(fa, battlefield);
            EnforceLibraryFontFloor();
            FinishLibraryText();
            RemoveBlankPanelImages();
            PlaceLibraryHeader();
            FitBriefingPicture();
            ClampStrayMedalBars();
            LockMedalArtwork();
            PresentTheatreSummary();
            ApplyBriefingColumnState();
            PresentZoomHint(fa, battlefield);
            RaiseLaunchTransition();
            if (!_refittingGrid &&
                battlefield &&
                LibraryLayout == SkirmishBattlefieldLayout.Grid &&
                _libraryViewModel != null &&
                _libraryGrid != null &&
                _libraryGrid.gameObject.activeInHierarchy)
            {
                _refittingGrid = true;
                try
                {
                    PresentGrid(_libraryViewModel.Battlefield, fa);
                    FinishLibraryText();
                    foreach (LibraryFixedTextSize held in GetComponentsInChildren<LibraryFixedTextSize>(true)) held.Apply(); // P1: fixed-size labels opt out of the refit text pass
                }
                finally
                {
                    _refittingGrid = false;
                }
            }

            if (!battlefield && _libraryViewModel!=null && _libraryRegions!=null && _libraryRegions.gameObject.activeInHierarchy)
            {
                var stamp=new Vector4(_libraryRegions.rect.width,_libraryRegions.rect.height,_libraryRegions.lossyScale.x,_libraryRegions.lossyScale.y);
                if((stamp-_sagaTheatreLayoutStamp).sqrMagnitude>.001f)
                {
                    _refittingTheatre=true;
                    try { PresentRegions(_libraryViewModel.Theater,fa);FinishLibraryText(); }
                    finally { _refittingTheatre=false; }
                }
                FitZoomCue();
            }
            if (battlefield && !_refittingSaga && _libraryViewModel != null && _libraryPins != null)
            {
                _refittingSaga = true;
                try { PresentSaga(_libraryViewModel.Battlefield, fa); }
                finally { _refittingSaga = false; }
            }
            ApplySagaStyle();
        }


        private void EnsureOverallProgressStrip(Transform preview)
        {
            if (preview == null)
                return;
            _overallStrip = FindOrCreate(preview, "OverallProgressStrip");
            if (_overallStrip.Find("Caption") != null)
            {
                _overallFill = _overallStrip.Find("Bar/Fill")?.GetComponent<Image>();
                return;
            }

            TintPanel(_overallStrip.gameObject);
            V3GradientGraphic plate = _overallStrip.GetComponent<V3GradientGraphic>();
            if (plate != null)
                plate.Configure(
                    new Color(0.05f, 0.08f, 0.09f, 0.96f),
                    new Color(0.02f, 0.04f, 0.05f, 0.96f),
                    new Color(0.93f, 0.78f, 0.38f, 1f),
                    3f);

            var caption = CreateBoundLabel(_overallStrip, "Caption", "ui.skirmish.library.battles_won", "Battles won");
            PlaceTopLeft(caption.rectTransform, 10f, 8f, 210f, 28f);
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Overflow;

            var count = CreateLabel(_overallStrip, "Count", 18f);
            PlaceTopLeft(count.rectTransform, 220f, 8f, 110f, 28f);
            count.alignment = TextAlignmentOptions.MidlineLeft;
            count.textWrappingMode = TextWrappingModes.NoWrap;
            count.isRightToLeftText = false;

            var bar = new GameObject("Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            bar.transform.SetParent(_overallStrip, false);
            PlaceTopLeft(bar.rectTransform, 10f, 42f, 320f, 14f);
            bar.color = new Color(0.08f, 0.12f, 0.14f, 1f);
            bar.raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(bar.transform, false);
            fill.color = new Color(0.91f, 0.72f, 0.22f, 1f);
            fill.raycastTarget = false;
            _overallFill = fill;

            var difficulties = new[]
            {
                SkirmishDifficultyId.Recruit,
                SkirmishDifficultyId.Regular,
                SkirmishDifficultyId.Veteran,
                SkirmishDifficultyId.Commander
            };
            for (int i = 0; i < difficulties.Length; i++)
            {
                var slot = new GameObject("Medal" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                slot.SetParent(_overallStrip, false);
                PlaceTopLeft(slot, 348f + i * 104f, 10f, 100f, 48f);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(slot, false);
                PlaceTopLeft(icon.rectTransform, 0f, 8f, 32f, 32f);
                ShowIcon(icon, SkirmishBattleLibraryArt.Medal(true, difficulties[i]), Color.white);
                var tier = CreateLabel(slot, "Count", 18f);
                PlaceTopLeft(tier.rectTransform, 36f, 2f, 60f, 24f);
                tier.alignment = TextAlignmentOptions.MidlineLeft;
                tier.textWrappingMode = TextWrappingModes.NoWrap;
                tier.isRightToLeftText = false;
                var tierCaption = CreateLabel(slot, "Caption", 12f);
                PlaceTopLeft(tierCaption.rectTransform, 36f, 26f, 62f, 20f);
                tierCaption.alignment = TextAlignmentOptions.MidlineLeft;
                tierCaption.overflowMode = TextOverflowModes.Ellipsis;
                tierCaption.isRightToLeftText = false;
            }
        }

        private static SkirmishMedalTotals MedalCounts(SkirmishTheaterState theater)
        {
            if (ReadMedalCounts != null)
                return ReadMedalCounts(theater);
            return ReadViewModelMedalProjection(theater);
        }

        private void PublishMedalCounts(SkirmishTheaterState theater)
        {
            // Theatre tier counts belong on Programmer 1's TheatreSummary, not this strip.
            if (LibraryLevel == SkirmishLibraryLevel.Theater)
                return;
            SkirmishMedalTotals medals = MedalCounts(theater);
            string[] text =
            {
                medals.Recruit.ToString(),
                medals.Regular.ToString(),
                medals.Veteran.ToString(),
                medals.Commander.ToString()
            };
            if (_medalRow != null && _medalRow.gameObject.activeInHierarchy)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    TMP_Text count = _medalRow.Find("Slot" + i + "/Count")?.GetComponent<TMP_Text>();
                    SetCopy(count, text[i], 14f, 18f);
                }
            }
        }

        private void PresentProgress(SkirmishTheaterState theater, bool fa)
        {
            if (_overallStrip != null)
                _overallStrip.gameObject.SetActive(false);
        }

        private void LayoutTheatreMedals(bool fa)
        {
            if (_overallStrip == null)
                return;
            for (int i = 0; i < 4; i++)
            {
                if (_overallStrip.Find("Medal" + i) is not RectTransform slot)
                    continue;
                PlaceStripMedal(slot, i, fa);
            }
        }

        private float TheatreStripWidth()
        {
            if (_overallStrip == null)
                return 900f;
            if (_overallStrip.parent is RectTransform parent)
            {
                float parentWidth = PointSpan(parent);
                if (parentWidth >= 200f)
                    return parentWidth - 16f;
            }

            float stripWidth = PointSpan(_overallStrip);
            return stripWidth >= 200f ? stripWidth : 900f;
        }

        private void PlaceStripMedal(RectTransform slot, int index, bool fa)
        {
            if (slot == null)
                return;
            float stripWidth = TheatreStripWidth();
            TheatreSeatGeometry.StripMedal(stripWidth + 16f, fa, index, out TheatreSeatGeometry.Box seat, out TheatreSeatGeometry.Box label, out TheatreSeatGeometry.Box count);
            PlaceTopLeft(slot, seat.X, seat.Y, seat.W, seat.H);
            LayoutElement element = slot.GetComponent<LayoutElement>();
            if (element != null)
                element.ignoreLayout = false;
            DropExtraMedalImages(slot);
            const float icon = 22f;
            float iconX = fa ? seat.W - 4f - icon : 4f;
            RectTransform iconRect = slot.Find("Icon") as RectTransform;
            PlaceTopLeft(iconRect, iconX, 8f, icon, icon);
            RectTransform caption = slot.Find("Caption") as RectTransform;
            RectTransform countRect = slot.Find("Count") as RectTransform;
            PlaceTopLeft(caption, label.X - seat.X, label.Y - seat.Y, label.W, label.H);
            PlaceTopLeft(countRect, count.X - seat.X, count.Y - seat.Y, count.W, count.H);
            TMP_Text captionText = caption != null ? caption.GetComponent<TMP_Text>() : null;
            if (captionText != null)
            {
                captionText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                captionText.isRightToLeftText = fa;
                captionText.overflowMode = TextOverflowModes.Overflow;
                captionText.textWrappingMode = TextWrappingModes.NoWrap;
                captionText.enableAutoSizing = false;
                float captionSize = TheatreSeatGeometry.StripCaptionSize(label.W, captionText.text);
                captionText.fontSize = captionSize;
                captionText.fontSizeMin = captionSize;
                captionText.fontSizeMax = captionSize;
                captionText.maxVisibleLines = 1;
            }

            TMP_Text countText = countRect != null ? countRect.GetComponent<TMP_Text>() : null;
            if (countText != null)
            {
                countText.isRightToLeftText = false;
                countText.alignment = TextAlignmentOptions.Center;
                countText.overflowMode = TextOverflowModes.Overflow;
                countText.textWrappingMode = TextWrappingModes.NoWrap;
                countText.fontSize = 14f;
            }

            BindMedalIcon(slot.Find("Icon")?.GetComponent<Image>(), index);
        }


        private void EnsureOrnateCorners(RectTransform panel)
        {
            if (panel == null || panel.Find("OrnamentCorner0") != null)
                return;
            Sprite corner = LoadLibraryIcon(SkirmishBattleLibraryArt.Corner);
            if (corner == null)
                return;
            for (int i = 0; i < 4; i++)
            {
                var image = new GameObject("OrnamentCorner" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(panel, false);
                image.sprite = corner;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = new Color(0.93f, 0.78f, 0.38f, 1f);
                PlaceOrnament(image.rectTransform, i);
            }
        }

        private static void ClampOrnamentCorners(RectTransform panel)
        {
            if (panel == null)
                return;
            for (int i = 0; i < 4; i++)
            {
                if (panel.Find("OrnamentCorner" + i) is RectTransform rect)
                    PlaceOrnament(rect, i);
            }
        }

        private static void PlaceOrnament(RectTransform rect, int index)
        {
            if (rect == null)
                return;
            const float size = 22f;
            const float margin = 18f;
            float center = size * 0.5f + margin;
            Vector2 anchor;
            Vector2 position;
            float rotation;
            switch (index)
            {
                case 0:
                    anchor = new Vector2(0f, 1f);
                    position = new Vector2(center, -center);
                    rotation = 0f;
                    break;
                case 1:
                    anchor = new Vector2(1f, 1f);
                    position = new Vector2(-center, -center);
                    rotation = -90f;
                    break;
                case 2:
                    anchor = new Vector2(1f, 0f);
                    position = new Vector2(-center, center);
                    rotation = 180f;
                    break;
                default:
                    anchor = new Vector2(0f, 0f);
                    position = new Vector2(center, center);
                    rotation = 90f;
                    break;
            }

            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = position;
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);
            rect.localScale = Vector3.one;
        }


        private static void MaskRect(RectTransform rect)
        {
            if (rect != null && rect.GetComponent<RectMask2D>() == null)
                rect.gameObject.AddComponent<RectMask2D>();
        }

        private static string MixedText(string value, bool fa)
        {
            // Bidi isolates are missing from the Farsi TMP font, so mixed lines stay unmarked.
            return fa ? value ?? string.Empty : value ?? string.Empty;
        }

        private void EnsureLibraryBackdrop()
        {
            if (transform.Find("LibraryBackdrop") != null)
                return;
            var image = new GameObject("LibraryBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(transform, false);
            image.transform.SetAsFirstSibling();
            Stretch(image.rectTransform);
            image.texture = Resources.Load<Texture2D>(SkirmishBattleLibraryArt.Backdrop);
            image.color = image.texture != null ? Color.white : new Color(0.07f, 0.09f, 0.08f, 1f);
            image.raycastTarget = false;
        }
    }
}
