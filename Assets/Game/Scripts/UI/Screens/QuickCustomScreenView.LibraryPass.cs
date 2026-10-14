using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Skirmish.Contracts;
using Game.UI.Skirmish;
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
                _crestImage.preserveAspect = true;
                _crestImage.raycastTarget = false;
                _crestImage.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Crest);
            }
            else if (title != null)
                _crestImage = title.Find("Crest")?.GetComponent<Image>();
        }

        private void OnLibraryLayoutApplied()
        {
            if (_libraryPresenting)
                return;
            PlaceLibraryChrome();
            StyleCommandButtons(LibraryLevel == SkirmishLibraryLevel.Battlefield);
            PlaceLibraryFooter();
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
            PlaceTopLeft(_objectiveColumn, 8f, 8f, 88f, 420f);
            if (field.Tabs == null)
                return;
            for (int i = 0; i < field.Tabs.Length; i++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[i];
                var button = CreateChromeButton(_objectiveColumn, SkirmishLibraryControlNames.ObjectiveTab(objective.ObjectiveId));
                PlaceTopLeft(button.GetComponent<RectTransform>(), 0f, i * 100f, 84f, 92f);
                bool selected = string.Equals(objective.ObjectiveId, field.SelectedObjectiveId, StringComparison.OrdinalIgnoreCase);
                TintButton(button, selected, true);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(button.transform, false);
                PlaceTopLeft(icon.rectTransform, 22f, 6f, 40f, 40f);
                icon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out _, out string shortLabel);
                var label = CreateLabel(button.transform, "Label", 16f);
                PlaceTopLeft(label.rectTransform, 2f, 46f, 80f, 24f);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                SetCopy(label, shortLabel, 15f, 20f);
                var count = CreateLabel(button.transform, "Count", 14f);
                PlaceTopLeft(count.rectTransform, 2f, 68f, 80f, 20f);
                count.alignment = TextAlignmentOptions.Center;
                count.isRightToLeftText = false;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                SetCopy(count, objective.WonCount + "/" + objective.PublishedCount, 13f, 16f);
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
            caption.overflowMode = TextOverflowModes.Ellipsis;

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
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(true, difficulties[i]));
                var tier = CreateLabel(slot, "Count", 18f);
                PlaceTopLeft(tier.rectTransform, 36f, 8f, 60f, 32f);
                tier.alignment = TextAlignmentOptions.MidlineLeft;
                tier.textWrappingMode = TextWrappingModes.NoWrap;
                tier.isRightToLeftText = false;
            }
        }

        private void PresentProgress(SkirmishTheaterState theater, bool fa)
        {
            if (_progressTrack != null)
                _progressTrack.gameObject.SetActive(false);
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

            SetCopy(
                _overallStrip.Find("Caption")?.GetComponent<TMP_Text>(),
                LibraryText("ui.skirmish.library.battles_won", fa),
                14f,
                20f);
            TMP_Text count = _overallStrip.Find("Count")?.GetComponent<TMP_Text>();
            SetCopy(count, won + " / " + published, 15f, 22f);
            if (count != null)
                count.isRightToLeftText = false;

            SkirmishMedalTotals medals = theater.OverallMedalTotals;
            int[] tiers = { medals.Recruit, medals.Regular, medals.Veteran, medals.Commander };
            for (int i = 0; i < tiers.Length; i++)
            {
                TMP_Text tier = _overallStrip.Find("Medal" + i + "/Count")?.GetComponent<TMP_Text>();
                SetCopy(tier, tiers[i].ToString(), 14f, 20f);
                if (tier != null)
                    tier.isRightToLeftText = false;
            }
        }

        private void PresentStatTiles(SkirmishBriefingState briefing, bool hasCard, SkirmishBattleCardState card, bool fa)
        {
            if (_statRow == null)
                return;
            Transform credits = _statRow.Find("Credits");
            if (credits != null)
                credits.gameObject.SetActive(false);
            string army = hasCard ? card.ArmyProfile : string.Empty;
            string start = hasCard ? card.StartProfile : string.Empty;
            if (TryFindEntry(briefing.ScenarioId, out SkirmishBattleCatalogEntry entry))
            {
                if (string.IsNullOrEmpty(army))
                    army = entry.ArmyProfile;
                if (string.IsNullOrEmpty(start))
                    start = entry.StartProfile;
            }
            SkirmishBattleLibraryArt.ProfileCopy(army, fa, out string forces);
            if (string.IsNullOrEmpty(forces))
                forces = string.IsNullOrEmpty(army) ? (fa ? "زمینی" : "GROUND") : army;
            int seconds = Mathf.Max(0, Mathf.RoundToInt(SkirmishPresetConfig.MatchDurationSeconds));
            string time = (seconds / 60).ToString("00", CultureInfo.InvariantCulture) + ":"
                + (seconds % 60).ToString("00", CultureInfo.InvariantCulture);
            SetTile(_statRow.Find("Time"), fa ? "زمان" : "TIME", time);
            SetTile(_statRow.Find("Forces"), fa ? "نیرو" : "FORCES", forces);
            SetTile(_statRow.Find("Size"), fa ? "اندازه" : "SIZE", StartStructureCount(start).ToString(CultureInfo.InvariantCulture));
            TMP_Text timeValue = _statRow.Find("Time/Value")?.GetComponent<TMP_Text>();
            if (timeValue != null)
                timeValue.isRightToLeftText = false;
        }

        private static int StartStructureCount(string start)
        {
            var package = ScriptableObject.CreateInstance<SkirmishStartPackageConfig>();
            try
            {
                if (string.Equals(start, "E", StringComparison.OrdinalIgnoreCase))
                    package.ConfigureEstablished();
                else
                    package.ConfigureField();
                return Mathf.Max(1, package.StandardStructureCount);
            }
            finally
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(package);
                else
                    UnityEngine.Object.DestroyImmediate(package);
            }
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
            SetCopy(tile.Find("Caption")?.GetComponent<TMP_Text>(), caption, 13f, 16f);
            SetCopy(tile.Find("Value")?.GetComponent<TMP_Text>(), string.IsNullOrEmpty(value) ? "0" : value, 15f, 20f);
        }

        private void PresentBriefingPreview(SkirmishBriefingState briefing, bool hasPin, SkirmishTheaterPinState pin, bool battlefield)
        {
            if (_briefingPreview == null)
                return;
            string key = battlefield ? briefing.ScenarioId : null;
            string resource = !string.IsNullOrEmpty(key)
                ? SkirmishBattleLibraryArt.PreviewKey(key, hasPin ? pin.MapId : LibraryMapId)
                : hasPin && !string.IsNullOrEmpty(pin.PlatePreviewKey)
                    ? LibraryArtKey(pin.PlatePreviewKey, pin.MapId)
                    : SkirmishBattleLibraryArt.TheaterPlate;
            Texture2D texture = Resources.Load<Texture2D>(resource);
            _briefingPreview.texture = texture;
            _briefingPreview.color = texture != null ? Color.white : new Color(0.08f, 0.12f, 0.14f, 1f);
            _briefingPreview.raycastTarget = false;
        }

        private void PresentTheaterDetail(bool fa)
        {
            if (_medalGrid == null)
                return;
            ClearChildren(_medalGrid);
            for (int i = 0; i < SkirmishBattleLibraryArt.ObjectiveIds.Length; i++)
            {
                string objectiveId = SkirmishBattleLibraryArt.ObjectiveIds[i];
                var icon = new GameObject("ObjectiveIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(_medalGrid, false);
                PlaceTopLeft(icon.rectTransform, i * 128f, 0f, 36f, 36f);
                icon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ObjectiveIcon(objectiveId));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var label = CreateLabel(_medalGrid, "ObjectiveLabel", 15f);
                PlaceTopLeft(label.rectTransform, i * 128f + 40f, 4f, 84f, 32f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                SkirmishBattleLibraryArt.ObjectiveCopy(objectiveId, fa, out _, out string shortLabel);
                SetCopy(label, shortLabel, 14f, 16f);
            }

            var hint = CreateLabel(_medalGrid, "ZoomHint", 15f);
            PlaceTopLeft(hint.rectTransform, 0f, 44f, 500f, 28f);
            hint.alignment = TextAlignmentOptions.MidlineLeft;
            hint.textWrappingMode = TextWrappingModes.NoWrap;
            SetCopy(hint, fa ? "برای بزرگنمایی لمس یا جمع کن" : "Tap or pinch to zoom", 14f, 16f);
        }

        private void StyleCommandButtons(bool battlefield)
        {
            TintCommand(transform.Find("SkirmishSetupComposition/BackButton")?.GetComponent<Button>()
                ?? FindDeep(transform, "BackButton")?.GetComponent<Button>(),
                new Color(0.16f, 0.48f, 0.78f, 1f), new Color(0.06f, 0.22f, 0.42f, 1f));
            if (_savedMatchAvailable)
                return;
            if (battlefield)
            {
                TintCommand(resetButton, new Color(0.72f, 0.46f, 0.12f, 1f), new Color(0.36f, 0.2f, 0.04f, 1f));
                TintCommand(randomizeSeedButton, new Color(0.14f, 0.46f, 0.7f, 1f), new Color(0.05f, 0.2f, 0.36f, 1f));
                TintCommand(launchButton, new Color(0.24f, 0.58f, 0.18f, 1f), new Color(0.08f, 0.28f, 0.08f, 1f));
            }
            else
            {
                TintCommand(resetButton, new Color(0.75f, 0.5f, 0.12f, 1f), new Color(0.38f, 0.22f, 0.04f, 1f));
                TintCommand(randomizeSeedButton, new Color(0.12f, 0.58f, 0.66f, 1f), new Color(0.04f, 0.24f, 0.3f, 1f));
                TintCommand(launchButton, new Color(0.24f, 0.58f, 0.18f, 1f), new Color(0.08f, 0.28f, 0.08f, 1f));
            }

            EnlargeButtonLabel(resetButton);
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
            Sprite corner = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Corner);
            if (corner == null)
                return;
            float[] rotation = { 0f, -90f, 180f, 90f };
            Vector2[] anchor = { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) };
            Vector2[] pivot = { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                var image = new GameObject("OrnamentCorner" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(panel, false);
                image.sprite = corner;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = new Color(0.93f, 0.78f, 0.38f, 1f);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = anchor[i];
                rect.pivot = pivot[i];
                rect.sizeDelta = new Vector2(36f, 36f);
                rect.anchoredPosition = Vector2.zero;
                rect.localEulerAngles = new Vector3(0f, 0f, rotation[i]);
            }
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
