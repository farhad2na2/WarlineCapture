using System;
using System.Text;
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
        private RectTransform _statRow;
        private RawImage _briefingPreview;
        private RectTransform _medalGrid;
        private Image _crestImage;
        private bool _libraryLayoutHooked;

        public void ApplyLibraryCaptureDemo()
        {
            EnsureLibraryViewModel();
            if (_libraryViewModel is SkirmishLibraryStubViewModel stub)
                stub.ApplyCaptureDemo();
        }

        private void EnsureLibraryPass(Transform composition, Transform preview, Transform rules)
        {
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
                EnsureStatTile(_statRow, "Army", 0f);
                EnsureStatTile(_statRow, "Start", 180f);
                EnsureStatTile(_statRow, "Objective", 360f);
                _medalGrid = FindOrCreate(rules, "MedalGrid");
            }

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
        }

        private void EnsureStatTile(RectTransform row, string name, float x)
        {
            if (row.Find(name) != null)
                return;
            var tile = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            tile.SetParent(row, false);
            PlaceTopLeft(tile, x, 0f, 168f, 72f);
            TintPanel(tile.gameObject);
            var caption = CreateBoundLabel(tile, "Caption", "ui.skirmish.library_stat_" + name.ToLowerInvariant(), name.ToUpperInvariant());
            PlaceTopLeft(caption.rectTransform, 8f, 4f, 152f, 24f);
            caption.fontSize = 14f;
            var value = CreateBoundLabel(tile, "Value", "ui.skirmish.library_stat_value", "—");
            PlaceTopLeft(value.rectTransform, 8f, 30f, 152f, 36f);
            value.fontSize = 18f;
        }

        private void PresentObjectiveColumn(SkirmishBattlefieldState field, bool fa)
        {
            if (_objectiveColumn == null)
                return;
            ClearChildren(_objectiveColumn);
            PlaceTopLeft(_objectiveColumn, 8f, 8f, 88f, 420f);
            if (field?.Tabs == null)
                return;
            for (int i = 0; i < field.Tabs.Count; i++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[i];
                var button = CreateChromeButton(_objectiveColumn, "ObjectiveTab_" + objective.ObjectiveId);
                PlaceTopLeft(button.GetComponent<RectTransform>(), 0f, i * 100f, 84f, 92f);
                bool selected = string.Equals(objective.ObjectiveId, field.SelectedObjectiveId, StringComparison.OrdinalIgnoreCase);
                TintButton(button, selected, true);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(button.transform, false);
                PlaceTopLeft(icon.rectTransform, 22f, 6f, 40f, 40f);
                icon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var label = CreateLabel(button.transform, "Label", 15f);
                PlaceTopLeft(label.rectTransform, 4f, 48f, 76f, 40f);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out _, out string shortLabel);
                SetCopy(label, shortLabel, 14f, 18f);
                string captured = objective.ObjectiveId;
                button.onClick.AddListener(() => SelectLibraryObjective(captured));
            }
        }

        private void PresentProgress(SkirmishTheaterPinState pin, bool fa)
        {
            if (_progressTrack == null)
                return;
            int won = pin != null ? pin.WonCount : 0;
            int published = pin != null ? pin.PublishedCount : 0;
            float ratio = published > 0 ? Mathf.Clamp01(won / (float)published) : 0f;
            if (_progressFill != null)
            {
                var fill = _progressFill.rectTransform;
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(Mathf.Max(0.04f, ratio), 1f);
                fill.offsetMin = new Vector2(2f, 2f);
                fill.offsetMax = new Vector2(-2f, -2f);
            }

            SetCopy(_progressTrack.Find("Caption")?.GetComponent<TMP_Text>(), fa ? "نبردهای برده" : "BATTLES WON", 16f, 22f);
            SetCopy(_progressTrack.Find("Count")?.GetComponent<TMP_Text>(), won + "/" + published, 16f, 22f);
        }

        private void PresentStatTiles(SkirmishBriefingState briefing, SkirmishBattleCardState card, bool fa)
        {
            if (_statRow == null)
                return;
            string army = briefing != null ? LibraryText(briefing.ArmyLocKey, fa) : string.Empty;
            string start = briefing != null ? LibraryText(briefing.StartLocKey, fa) : string.Empty;
            string objective = card != null ? LibraryText(card.SubtitleLocKey, fa) : string.Empty;
            SetTile(_statRow.Find("Army"), fa ? "ارتش" : "ARMY", army);
            SetTile(_statRow.Find("Start"), fa ? "شروع" : "START", start);
            SetTile(_statRow.Find("Objective"), fa ? "هدف" : "OBJECTIVE", objective);
        }

        private void SetTile(Transform tile, string caption, string value)
        {
            if (tile == null)
                return;
            SetCopy(tile.Find("Caption")?.GetComponent<TMP_Text>(), caption, 13f, 16f);
            SetCopy(tile.Find("Value")?.GetComponent<TMP_Text>(), string.IsNullOrEmpty(value) ? "—" : value, 15f, 20f);
        }

        private void PresentBriefingPreview(SkirmishBriefingState briefing, SkirmishTheaterPinState pin, bool battlefield)
        {
            if (_briefingPreview == null)
                return;
            string key = battlefield && briefing != null
                ? briefing.ScenarioId
                : null;
            string resource = !string.IsNullOrEmpty(key)
                ? SkirmishBattleLibraryArt.PreviewKey(key, pin != null ? pin.MapId : LibraryMapId)
                : pin != null && !string.IsNullOrEmpty(pin.PlatePreviewKey)
                    ? pin.PlatePreviewKey
                    : SkirmishBattleLibraryArt.TheaterPlate;
            Texture2D texture = Resources.Load<Texture2D>(resource);
            _briefingPreview.texture = texture;
            _briefingPreview.color = texture != null ? Color.white : new Color(0.08f, 0.12f, 0.14f, 1f);
            _briefingPreview.raycastTarget = false;
        }

        private void PresentMedalGrid(SkirmishBattlefieldState field)
        {
            if (_medalGrid == null)
                return;
            ClearChildren(_medalGrid);
            if (field?.MapCards == null)
                return;
            var difficulties = new[]
            {
                SkirmishDifficultyId.Recruit,
                SkirmishDifficultyId.Regular,
                SkirmishDifficultyId.Veteran,
                SkirmishDifficultyId.Commander
            };
            int shown = 0;
            for (int i = 0; i < field.MapCards.Count && shown < 6; i++)
            {
                SkirmishBattleCardState card = field.MapCards[i];
                if (card == null)
                    continue;
                for (int medal = 0; medal < difficulties.Length; medal++)
                {
                    var image = new GameObject("GridMedal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    image.transform.SetParent(_medalGrid, false);
                    PlaceTopLeft(image.rectTransform, medal * 28f, shown * 26f, 24f, 24f);
                    image.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(MedalEarned(card.Medals, difficulties[medal]), difficulties[medal]));
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                }

                var id = CreateLabel(_medalGrid, "Id", 14f);
                PlaceTopLeft(id.rectTransform, 116f, shown * 26f, 120f, 24f);
                id.alignment = TextAlignmentOptions.MidlineLeft;
                id.isRightToLeftText = false;
                SetCopy(id, card.ScenarioId, 13f, 16f);
                shown++;
            }
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
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
        }

        private static void TintCommand(Button button, Color top, Color bottom)
        {
            V3GradientGraphic gradient = button != null ? button.targetGraphic as V3GradientGraphic : null;
            if (gradient == null && button != null)
                gradient = button.GetComponent<V3GradientGraphic>();
            if (gradient == null)
                return;
            gradient.Configure(top, bottom, new Color(0.93f, 0.78f, 0.38f, 1f), 3f);
        }

        private static void MaskRect(RectTransform rect)
        {
            if (rect != null && rect.GetComponent<RectMask2D>() == null)
                rect.gameObject.AddComponent<RectMask2D>();
        }

        private static string MixedText(string value, bool fa)
        {
            if (!fa || string.IsNullOrEmpty(value))
                return value ?? string.Empty;
            var builder = new StringBuilder(value.Length + 8);
            int index = 0;
            while (index < value.Length)
            {
                bool latin = IsLatinRun(value[index]);
                int end = index + 1;
                while (end < value.Length && IsLatinRun(value[end]) == latin)
                    end++;
                if (latin)
                {
                    builder.Append('\u2066');
                    builder.Append(value, index, end - index);
                    builder.Append('\u2069');
                }
                else
                    builder.Append(value, index, end - index);
                index = end;
            }

            return builder.ToString();
        }

        private static bool IsLatinRun(char value)
        {
            return (value >= 'A' && value <= 'Z')
                || (value >= 'a' && value <= 'z')
                || (value >= '0' && value <= '9')
                || value == '/' || value == '-' || value == '+' || value == '.' || value == '#';
        }
    }
}
