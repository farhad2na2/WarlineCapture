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

        public void ApplyLibraryCaptureDemo(Action<SkirmishDifficultyId> setLaunchDifficulty = null)
        {
            _suppressSavedMatch = true;
            EnsureLibraryCards();
            var medals = new CaptureMedalReader(_catalogEntries);
            medals.Grant("S001", SkirmishDifficultyId.Recruit);
            medals.Grant("S001", SkirmishDifficultyId.Regular);
            medals.Grant("S002", SkirmishDifficultyId.Recruit);
            medals.Grant("S025", SkirmishDifficultyId.Regular);
            medals.Grant("S073", SkirmishDifficultyId.Recruit);
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
                _briefingPreview = FindOrCreate(rules, "BriefingPreview").GetComponent<RawImage>();
                if (_briefingPreview == null)
                {
                    _briefingPreview = FindOrCreate(rules, "BriefingPreview").gameObject.AddComponent<RawImage>();
                }

                _progressTrack = FindOrCreate(rules, "BattlesWonTrack");
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
                    var caption = CreateBoundLabel(_progressTrack, "Caption", "ui.skirmish.library_battles_won", "BATTLES WON");
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

                _statRow = FindOrCreate(rules, "BriefingStats");
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

        private void OnLibraryLayoutApplied()
        {
            if (_libraryPresenting)
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
            RaiseLaunchTransition();
        }

        private void EnsureStatTile(RectTransform row, string name, float x, string captionText)
        {
            if (row.Find(name) != null)
                return;
            var tile = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            tile.SetParent(row, false);
            PlaceTopLeft(tile, x, 0f, 124f, 72f);
            TintPanel(tile.gameObject);
            var caption = CreateBoundLabel(tile, "Caption", "ui.skirmish.library_stat_" + name.ToLowerInvariant(), captionText);
            PlaceTopLeft(caption.rectTransform, 6f, 4f, 112f, 22f);
            caption.fontSize = 14f;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            var value = CreateBoundLabel(tile, "Value", "ui.skirmish.library_stat_" + name.ToLowerInvariant() + "_value", "—");
            PlaceTopLeft(value.rectTransform, 6f, 28f, 112f, 36f);
            value.fontSize = 18f;
            value.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void HideLegacyStat(RectTransform row, string name)
        {
            Transform tile = row != null ? row.Find(name) : null;
            if (tile != null)
                tile.gameObject.SetActive(false);
        }

        private void PresentObjectiveColumn(SkirmishBattlefieldState field, bool fa)
        {
            if (_objectiveColumn == null)
                return;
            ClearChildren(_objectiveColumn);
            PlaceTopLeft(_objectiveColumn, 8f, 8f, 116f, 520f);
            if (field.Tabs == null)
                return;
            for (int i = 0; i < field.Tabs.Length; i++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[i];
                var button = CreateChromeButton(_objectiveColumn, SkirmishLibraryControlNames.ObjectiveTab(objective.ObjectiveId));
                PlaceTopLeft(button.GetComponent<RectTransform>(), 0f, i * 108f, 112f, 100f);
                bool selected = string.Equals(objective.ObjectiveId, field.SelectedObjectiveId, StringComparison.OrdinalIgnoreCase);
                TintButton(button, selected, true);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(button.transform, false);
                PlaceTopLeft(icon.rectTransform, 22f, 6f, 40f, 40f);
                ShowIcon(icon, SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId), Color.white);
                SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out _, out string shortLabel);
                var label = CreateLabel(button.transform, "Label", 14f);
                var count = CreateLabel(button.transform, "Count", 14f);
                const float countWidth = 40f;
                if (fa)
                {
                    PlaceTopLeft(count.rectTransform, 4f, 56f, countWidth, 28f);
                    PlaceTopLeft(label.rectTransform, 46f, 50f, 62f, 42f);
                    label.alignment = TextAlignmentOptions.MidlineRight;
                }
                else
                {
                    PlaceTopLeft(label.rectTransform, 4f, 50f, 62f, 42f);
                    PlaceTopLeft(count.rectTransform, 68f, 56f, countWidth, 28f);
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                }

                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                SetCopy(label, shortLabel, 12f, 16f);
                count.alignment = TextAlignmentOptions.Center;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                count.overflowMode = TextOverflowModes.Overflow;
                SetCopy(count, objective.WonCount + "/" + objective.PublishedCount, 13f, 16f);
                count.isRightToLeftText = false;
                string captured = objective.ObjectiveId;
                button.onClick.AddListener(() => SelectLibraryObjective(captured));
            }
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

        private void PresentProgress(SkirmishTheaterState theater, bool fa)
        {
            if (_overallStrip == null)
                return;
            bool show = LibraryLevel != SkirmishLibraryLevel.Battlefield;
            _overallStrip.gameObject.SetActive(show);
            if (!show)
                return;

            int won = theater.OverallWonCount;
            int published = theater.OverallPublishedCount;
            float ratio = published > 0 ? Mathf.Clamp01(won / (float)published) : 0f;
            if (_overallFill != null)
            {
                RectTransform fill = _overallFill.rectTransform;
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(ratio, 1f);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;
                fill.pivot = new Vector2(0f, 0.5f);
            }

            float stripWidth = TheatreStripWidth();
            TMP_Text battles = _overallStrip.Find("Caption")?.GetComponent<TMP_Text>();
            SetCopy(battles, LibraryText("ui.skirmish.library.battles_won", fa), 14f, 20f);
            TMP_Text count = _overallStrip.Find("Count")?.GetComponent<TMP_Text>();
            SetCopy(count, won + " / " + published, 15f, 22f);
            const float captionWidth = 168f;
            const float countWidth = 96f;
            const float pairGap = 6f;
            float pairX = fa ? stripWidth - 8f - captionWidth - pairGap - countWidth : 8f;
            if (battles != null)
            {
                battles.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                battles.isRightToLeftText = fa;
                battles.overflowMode = TextOverflowModes.Overflow;
                battles.textWrappingMode = TextWrappingModes.NoWrap;
                PlaceTopLeft(battles.rectTransform, fa ? pairX + countWidth + pairGap : pairX, 4f, captionWidth, 32f);
            }

            if (count != null)
            {
                count.isRightToLeftText = false;
                count.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                PlaceTopLeft(count.rectTransform, fa ? pairX : pairX + captionWidth + pairGap, 6f, countWidth, 28f);
            }

            if (_overallFill != null && _overallFill.transform.parent is RectTransform bar)
            {
                PlaceTopLeft(bar, pairX, 46f, captionWidth + pairGap + countWidth, 22f);
                Image track = bar.GetComponent<Image>();
                if (track != null)
                    track.color = new Color(0.05f, 0.07f, 0.08f, 1f);
                Outline rim = bar.GetComponent<Outline>() ?? bar.gameObject.AddComponent<Outline>();
                rim.effectColor = new Color(0.93f, 0.78f, 0.38f, 1f);
                rim.effectDistance = new Vector2(1.5f, -1.5f);
            }

            SkirmishMedalTotals medals = theater.OverallMedalTotals;
            int[] tiers = { medals.Recruit, medals.Regular, medals.Veteran, medals.Commander };
            var tierNames = new[]
            {
                SkirmishDifficultyId.Recruit,
                SkirmishDifficultyId.Regular,
                SkirmishDifficultyId.Veteran,
                SkirmishDifficultyId.Commander
            };
            for (int i = 0; i < tiers.Length; i++)
            {
                Transform slot = _overallStrip.Find("Medal" + i);
                if (slot == null)
                    continue;
                TMP_Text tier = slot.Find("Count")?.GetComponent<TMP_Text>();
                SetCopy(tier, tiers[i].ToString(), 13f, 18f);
                if (tier != null)
                {
                    tier.isRightToLeftText = false;
                    tier.alignment = TextAlignmentOptions.MidlineLeft;
                    tier.overflowMode = TextOverflowModes.Overflow;
                }

                SkirmishBattleLibraryArt.DifficultyCopy(tierNames[i], fa, out string tierLabel);
                TMP_Text caption = slot.Find("Caption")?.GetComponent<TMP_Text>();
                if (caption == null)
                    caption = CreateLabel(slot, "Caption", 12f);
                SetCopy(caption, tierLabel, 11f, 15f);
                caption.isRightToLeftText = fa;
                caption.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                caption.overflowMode = TextOverflowModes.Overflow;
                caption.textWrappingMode = TextWrappingModes.NoWrap;
            }

            LayoutTheatreMedals(fa);
        }

        private void LayoutTheatreMedals(bool fa)
        {
            if (_overallStrip == null)
                return;
            float stripWidth = TheatreStripWidth();
            const float captionWidth = 168f;
            const float summaryCountWidth = 96f;
            const float pairGap = 6f;
            float reserved = 8f + captionWidth + pairGap + summaryCountWidth + 16f;
            float slotWidth = Mathf.Clamp((stripWidth - reserved) * 0.25f, 132f, 168f);
            float run = slotWidth * 4f;
            float origin = fa ? stripWidth - reserved - run : reserved;
            for (int i = 0; i < 4; i++)
            {
                if (_overallStrip.Find("Medal" + i) is not RectTransform slot)
                    continue;
                int visual = fa ? 3 - i : i;
                PlaceStripMedal(slot, i, fa, origin + visual * slotWidth, slotWidth);
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

        private void PlaceStripMedal(RectTransform slot, int index, bool fa, float origin, float slotWidth)
        {
            if (slot == null)
                return;
            float inner = Mathf.Max(96f, slotWidth - 8f);
            PlaceTopLeft(slot, origin, 4f, inner, 44f);
            LayoutElement element = slot.GetComponent<LayoutElement>();
            if (element != null)
                element.ignoreLayout = false;
            DropExtraMedalImages(slot);
            LayoutMedalEntry(slot, fa, 24f, 32f);
            BindMedalIcon(slot.Find("Icon")?.GetComponent<Image>(), index);
        }

        private void PresentStatTiles(SkirmishBriefingState briefing, bool hasCard, SkirmishBattleCardState card, bool fa)
        {
            if (_statRow == null)
                return;
            HideLegacyStat(_statRow, "Forces");
            HideLegacyStat(_statRow, "Credits");
            HideLegacyStat(_statRow, "SourcedFacts");
            HideLegacyStat(_statRow, "FactsFrame");
            EnsureStatTile(_statRow, "Time", 0f, fa ? "زمان" : "TIME");
            EnsureStatTile(_statRow, "Size", 0f, fa ? "اندازه" : "SIZE");
            EnsureStatTile(_statRow, "Objective", 0f, fa ? "هدف" : "OBJECTIVE");
            EnsureStatTile(_statRow, "Army", 0f, fa ? "ارتش" : "ARMY");
            EnsureStatTile(_statRow, "Start", 0f, fa ? "آغاز" : "START");

            PresentFlaggedTile("Time", briefing.ShowTime, fa ? "زمان" : "TIME", FormatClock(briefing.DeadlineSeconds), true);
            PresentFlaggedTile("Size", briefing.ShowSize, fa ? "اندازه" : "SIZE", LibraryText(briefing.SizeLocKey, fa), false);

            string army = hasCard ? card.ArmyProfile : string.Empty;
            string start = hasCard ? card.StartProfile : string.Empty;
            string objectiveId = hasCard ? card.ObjectiveId : string.Empty;
            if (TryFindEntry(briefing.ScenarioId, out SkirmishBattleCatalogEntry entry))
            {
                if (string.IsNullOrEmpty(army))
                    army = entry.ArmyProfile;
                if (string.IsNullOrEmpty(start))
                    start = entry.StartProfile;
                if (string.IsNullOrEmpty(objectiveId))
                    objectiveId = entry.ObjectiveId;
            }

            string objective = string.Empty;
            if (!string.IsNullOrEmpty(objectiveId))
                SkirmishBattleLibraryArt.ObjectiveCopy(objectiveId, fa, out objective, out _);
            SkirmishBattleLibraryArt.ProfileCopy(army, fa, out string armyLabel);
            SkirmishBattleLibraryArt.ProfileCopy(start, fa, out string startLabel);
            BindStatTile(_statRow.Find("Objective"), fa ? "هدف" : "OBJECTIVE", objective, false);
            BindStatTile(_statRow.Find("Army"), fa ? "ارتش" : "ARMY", armyLabel, false);
            BindStatTile(_statRow.Find("Start"), fa ? "آغاز" : "START", startLabel, false);
            LayoutVisibleStatTiles();
        }

        private void PresentFlaggedTile(string name, bool show, string caption, string value, bool latinValue)
        {
            Transform tile = _statRow != null ? _statRow.Find(name) : null;
            if (tile == null)
                return;
            tile.gameObject.SetActive(show);
            if (!show)
                return;
            SetTile(tile, caption, value ?? string.Empty);
            TMP_Text valueText = tile.Find("Value")?.GetComponent<TMP_Text>();
            if (valueText != null && latinValue)
                valueText.isRightToLeftText = false;
        }

        private void BindStatTile(Transform tile, string caption, string value, bool latinValue)
        {
            if (tile == null)
                return;
            if (string.IsNullOrEmpty(value))
            {
                tile.gameObject.SetActive(false);
                return;
            }

            tile.gameObject.SetActive(true);
            SetTile(tile, caption, value);
            TMP_Text valueText = tile.Find("Value")?.GetComponent<TMP_Text>();
            if (valueText != null && latinValue)
                valueText.isRightToLeftText = false;
        }

        private void LayoutVisibleStatTiles()
        {
            if (_statRow == null)
                return;
            string[] order = { "Time", "Size", "Objective", "Army", "Start" };
            int count = 0;
            for (int i = 0; i < order.Length; i++)
            {
                Transform tile = _statRow.Find(order[i]);
                if (tile != null && tile.gameObject.activeSelf)
                    count++;
            }

            if (count == 0)
                return;
            float width = _statRow.rect.width > 200f ? _statRow.rect.width : _statRow.sizeDelta.x;
            if (width < 200f)
                width = 518f;
            const float gap = 8f;
            float tileWidth = (width - gap * (count - 1)) / count;
            float x = 0f;
            for (int i = 0; i < order.Length; i++)
            {
                Transform tile = _statRow.Find(order[i]);
                if (tile == null || !tile.gameObject.activeSelf)
                    continue;
                PlaceTruthfulTile(tile, x, tileWidth);
                x += tileWidth + gap;
            }
        }

        private static string FormatClock(int seconds)
        {
            int minutes = seconds / 60;
            int remainder = seconds % 60;
            if (remainder < 0)
                remainder = -remainder;
            return minutes.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ":"
                + remainder.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void PlaceTruthfulTile(Transform tile, float x, float width)
        {
            if (tile == null)
                return;
            tile.gameObject.SetActive(true);
            if (tile is RectTransform rect)
                PlaceTopLeft(rect, x, 0f, width, 84f);
            float inner = Mathf.Max(20f, width - 8f);
            TMP_Text captionText = tile.Find("Caption")?.GetComponent<TMP_Text>();
            if (captionText != null)
            {
                PlaceTopLeft(captionText.rectTransform, 4f, 2f, inner, 26f);
                captionText.overflowMode = TextOverflowModes.Overflow;
                captionText.textWrappingMode = TextWrappingModes.NoWrap;
                captionText.margin = new Vector4(2f, 0f, 2f, 0f);
                captionText.fontSizeMin = 12f;
                captionText.fontSizeMax = 16f;
            }

            TMP_Text valueText = tile.Find("Value")?.GetComponent<TMP_Text>();
            if (valueText != null)
            {
                PlaceTopLeft(valueText.rectTransform, 4f, 28f, inner, 48f);
                valueText.textWrappingMode = TextWrappingModes.Normal;
                valueText.overflowMode = TextOverflowModes.Overflow;
                valueText.margin = new Vector4(2f, 0f, 2f, 0f);
                valueText.enableAutoSizing = true;
                valueText.fontSizeMin = 13f;
                valueText.fontSizeMax = 20f;
            }

            RectMask2D mask = tile.GetComponent<RectMask2D>();
            if (mask != null)
                mask.enabled = false;
        }

        private static int CountMedals(SkirmishMedalMask mask)
        {
            int count = 0;
            if ((mask & SkirmishMedalMask.Recruit) != 0) count++;
            if ((mask & SkirmishMedalMask.Regular) != 0) count++;
            if ((mask & SkirmishMedalMask.Veteran) != 0) count++;
            if ((mask & SkirmishMedalMask.Commander) != 0) count++;
            return count;
        }

        private void SetTile(Transform tile, string caption, string value)
        {
            if (tile == null)
                return;
            TMP_Text captionText = tile.Find("Caption")?.GetComponent<TMP_Text>();
            TMP_Text valueText = tile.Find("Value")?.GetComponent<TMP_Text>();
            SetCopy(captionText, caption, LibraryFontFloor, 24f);
            SetCopy(valueText, value, LibraryFontFloor, 28f);
            if (captionText != null)
            {
                captionText.textWrappingMode = TextWrappingModes.NoWrap;
                captionText.overflowMode = TextOverflowModes.Overflow;
            }

            if (valueText != null)
            {
                valueText.textWrappingMode = TextWrappingModes.NoWrap;
                valueText.overflowMode = TextOverflowModes.Overflow;
                float width = tile is RectTransform row && row.sizeDelta.x > 20f ? row.sizeDelta.x - 12f : 80f;
                PlaceTopLeft(valueText.rectTransform, 6f, 26f, width, 34f);
            }
        }

        private void PresentBriefingPreview(SkirmishBriefingState briefing, bool hasPin, SkirmishTheaterPinState pin, bool battlefield)
        {
            if (_briefingPreview == null)
                return;
            string key = battlefield ? ScenarioPreviewKey(BriefingPreviewKey(briefing)) : null;
            if (battlefield && string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(briefing.ScenarioId))
                key = "skirmish.preview." + briefing.ScenarioId;
            Texture2D texture = string.IsNullOrEmpty(key) ? null : SkirmishLibraryPreviewAssets.Load(key);
            if (texture == null && battlefield && !string.IsNullOrEmpty(key))
                texture = SkirmishLibraryPreviewAssets.Placeholder();
            bool show = battlefield && texture != null;
            _briefingPreview.texture = null;
            _briefingPreview.enabled = false;
            _briefingPreview.color = new Color(0.05f, 0.07f, 0.08f, 1f);
            _briefingPreview.gameObject.SetActive(show);
            LayoutElement element = _briefingPreview.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.ignoreLayout = !show;
                element.minHeight = show ? 180f : 0f;
                element.preferredHeight = show ? 200f : 0f;
                element.flexibleHeight = 0f;
            }

            RawImage picture = _briefingPreview.transform.Find("Picture")?.GetComponent<RawImage>();
            if (picture == null)
            {
                picture = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                picture.transform.SetParent(_briefingPreview.transform, false);
            }

            picture.texture = show ? texture : null;
            picture.enabled = show;
            picture.color = Color.white;
            picture.uvRect = new Rect(0f, 0f, 1f, 1f);
            picture.raycastTarget = false;
        }

        private void PresentTheaterDetail(bool fa)
        {
            if (_medalGrid != null)
            {
                ClearChildren(_medalGrid);
                _medalGrid.gameObject.SetActive(false);
            }
        }

        private void StyleCommandButtons(bool battlefield)
        {
            TintCommand(transform.Find("SkirmishSetupComposition/BackButton")?.GetComponent<Button>()
                ?? FindDeep(transform, "BackButton")?.GetComponent<Button>(),
                new Color(0.16f, 0.48f, 0.78f, 1f), new Color(0.06f, 0.22f, 0.42f, 1f));
            if (_savedMatchAvailable)
                return;
            TintCommand(resetButton, new Color(0.16f, 0.4f, 0.62f, 1f), new Color(0.06f, 0.18f, 0.32f, 1f));
            TintCommand(_libraryFavouritesButton, new Color(0.42f, 0.28f, 0.1f, 1f), new Color(0.2f, 0.12f, 0.04f, 1f));
            TintCommand(_libraryAdvancedButton, new Color(0.22f, 0.32f, 0.36f, 1f), new Color(0.08f, 0.14f, 0.16f, 1f));
            TintCommand(randomizeSeedButton, new Color(0.14f, 0.46f, 0.7f, 1f), new Color(0.05f, 0.2f, 0.36f, 1f));
            TintCommand(launchButton, new Color(0.24f, 0.58f, 0.18f, 1f), new Color(0.08f, 0.28f, 0.08f, 1f));

            EnlargeButtonLabel(resetButton);
            EnlargeButtonLabel(_libraryFavouritesButton);
            EnlargeButtonLabel(_libraryAdvancedButton);
            EnlargeButtonLabel(randomizeSeedButton);
            EnlargeButtonLabel(launchButton);
        }

        private void EnlargeButtonLabel(Button button)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null)
                return;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 26f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
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

        private static void TintCommand(Button button, Color top, Color bottom)
        {
            V3GradientGraphic gradient = button != null ? button.targetGraphic as V3GradientGraphic : null;
            if (gradient == null && button != null)
                gradient = button.GetComponent<V3GradientGraphic>();
            if (gradient == null)
                return;
            gradient.Configure(top, bottom, new Color(0.93f, 0.78f, 0.38f, 1f), 3f);
            SkirmishLibraryButtonStateView state = button.GetComponent<SkirmishLibraryButtonStateView>();
            if (state == null)
                state = button.gameObject.AddComponent<SkirmishLibraryButtonStateView>();
            state.Bind(gradient);
            state.ApplyPalette(top, bottom, new Color(0.93f, 0.78f, 0.38f, 1f), button.interactable, false);
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
