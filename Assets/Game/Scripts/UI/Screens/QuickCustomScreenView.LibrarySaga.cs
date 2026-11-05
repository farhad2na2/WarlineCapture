using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private ScrollRect _sagaScroll;
        private string _sagaFocusMap;
        private string _sagaFocusObjective;
        private Vector2 _sagaPan = new(0f, 1f);
        private void PresentSaga(SkirmishBattlefieldState field, bool fa)
        {
            if (_libraryPins == null)
                return;
            if (_sagaScroll != null)
                _sagaPan = _sagaScroll.normalizedPosition;
            ClearChildren(_libraryPins);
            Canvas.ForceUpdateCanvases();
            var viewport = SkirmishWidgetFactory.Rect("SagaViewport", _libraryPins);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            _sagaScroll = viewport.gameObject.AddComponent<ScrollRect>();
            _sagaScroll.viewport = viewport;
            _sagaScroll.movementType = ScrollRect.MovementType.Clamped;
            _sagaScroll.scrollSensitivity = 36f;
            float width = Mathf.Max(SkirmishSagaPath.MinimumWidth, _libraryPins.rect.width);
            float height = Mathf.Max(SkirmishSagaPath.MinimumHeight, _libraryPins.rect.height);
            var content = SkirmishWidgetFactory.Rect("SagaRoad", viewport);
            PlaceTopLeft(content, 0, 0, width, height);
            _sagaScroll.content = content;
            _sagaScroll.horizontal = width > _libraryPins.rect.width + 1;
            _sagaScroll.vertical = height > _libraryPins.rect.height + 1;
            var plate = SkirmishWidgetFactory.Raw("CampaignPlate", content, SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.ResolvePlateKey(field.MapId)), Color.white);
            Stretch(plate.rectTransform);
            if (plate.texture != null)
            {
                float source = plate.texture.width / (float)plate.texture.height, aspect = width / height;
                plate.uvRect = aspect < source ? new Rect((1 - aspect / source) * .5f, 0, aspect / source, 1) : new Rect(0, (1 - source / aspect) * .5f, 1, source / aspect);
            }

            var wash = SkirmishWidgetFactory.Image("PlateShade", content, new Color(.05f, .025f, .01f, .18f));
            Stretch(wash.rectTransform);
            IReadOnlyList<Vector2> stops = SkirmishSagaPath.ForMap(field.MapId);
            var visible = new List<Vector2>();
            if (field.GridCards != null)
                for (int i = 0; i < field.GridCards.Length && i < stops.Count; i++)
                    if (!IsEmptyCard(field.GridCards[i]))
                        visible.Add(stops[i]);
            var road = SkirmishWidgetFactory.Rect("RoadOverlay", content).gameObject.AddComponent<SkirmishSagaRoadGraphic>();
            Stretch(road.rectTransform);
            road.Configure(visible);
            if (field.GridCards != null)
                for (int i = 0; i < field.GridCards.Length && i < stops.Count; i++)
                {
                    var card = field.GridCards[i];
                    if (IsEmptyCard(card))
                        continue;
                    if (i % 6 == 0)
                        BuildSagaChapter(content, field, i / 6, stops[i], width, height, fa);
                    BuildSagaStop(content, card, i, stops[i], width, height, fa);
                }

            Canvas.ForceUpdateCanvases();
            bool chapterChanged = _sagaFocusMap != field.MapId || _sagaFocusObjective != field.SelectedObjectiveId;
            _sagaScroll.normalizedPosition = _sagaPan;
            if (chapterChanged)
            {
                int chapter = Array.IndexOf(SkirmishBattleLibraryArt.ObjectiveIds, field.SelectedObjectiveId);
                int index = Mathf.Clamp(chapter * 6, 0, stops.Count - 1);
                Vector2 point = stops[index];
                float travelX = Mathf.Max(0, width - viewport.rect.width), travelY = Mathf.Max(0, height - viewport.rect.height);
                float x = Mathf.Clamp(point.x * width - viewport.rect.width * .5f, 0, travelX);
                float y = Mathf.Clamp(point.y * height - viewport.rect.height * .32f, 0, travelY);
                _sagaScroll.normalizedPosition = new Vector2(travelX > 1 ? x / travelX : 0, travelY > 1 ? 1 - y / travelY : 1);
            }

            _sagaFocusMap = field.MapId;
            _sagaFocusObjective = field.SelectedObjectiveId;
        }

        private void BuildSagaStop(RectTransform parent, SkirmishBattleCardState card, int index, Vector2 point, float width, float height, bool fa)
        {
            const float size = SkirmishSagaPath.TouchDiameter;
            Vector2 center = new(point.x * width, point.y * height);
            var root = SkirmishWidgetFactory.Rect(SkirmishLibraryControlNames.BattleCard(card.ScenarioId), parent);
            PlaceTopLeft(root, center.x - size * .5f, center.y - size * .5f, size, size);
            bool owned = card.Access == SkirmishCardAccess.Playable;
            Color brass = owned ? Color.white : new Color(.58f, .72f, .84f, .9f);
            if (card.IsSelected)
            {
                var glow = SagaIcon(root, "SelectedHalo", SkirmishBattleLibraryArt.Medal(false, SkirmishDifficultyId.Recruit), new Color(1, .68f, .09f, 1));
                PlaceTopLeft(glow.rectTransform, -17, -17, size + 34, size + 34);
                var shadow = glow.gameObject.AddComponent<Outline>();
                shadow.effectColor = new Color(1, .64f, .04f, .55f);
                shadow.effectDistance = new Vector2(6, -6);
            }

            var shade = SagaDisc(root, "Shadow", size, new Color(.025f, .012f, .005f, .75f));
            PlaceTopLeft(shade.rectTransform, 3, 8, size, size);
            var rim = SagaIcon(root, "Medallion", "skirmish.shared.medallion", brass);
            Stretch(rim.rectTransform);
            rim.raycastTarget = true; // The full medallion is the native mobile touch target.
            var face = SagaDisc(root, "NumberFace", size * .68f, owned ? new Color(.18f, .105f, .035f, 1) : new Color(.075f, .10f, .12f, 1));
            PlaceTopLeft(face.rectTransform, size * .16f, size * .16f, size * .68f, size * .68f);
            var number = CreateLabel(root, "StopNumber", 32);
            Stretch(number.rectTransform, 10, 10, 12, 10);
            number.text = fa ? SkirmishWidgetContext.ToPersianDigits((index + 1).ToString()) : (index + 1).ToString();
            number.font = UiShellRuntimeGateway.Localization.CurrentFontAsset as TMP_FontAsset ?? number.font;
            number.enableAutoSizing = false;
            number.fontSize = 32;
            number.isRightToLeftText = false;
            number.alignment = TextAlignmentOptions.Center;
            number.fontStyle = FontStyles.Bold;
            number.color = Color.white;
            number.raycastTarget = false;
            for (int tier = 0; tier < 4; tier++)
            {
                bool earned = ((int)card.Medals & (1 << tier)) != 0;
                if (!earned)
                    continue;
                var ribbon = SkirmishWidgetFactory.Rect("EarnedRibbon_" + tier, root).gameObject.AddComponent<V3PolygonGraphic>();
                PlaceTopLeft(ribbon.rectTransform, 16 + tier * 12, 68, 10, 40);
                ribbon.ConfigureResponsive(new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 40), new Vector2(5, 34), new Vector2(0, 40) }, SagaTierColor(tier), new Color(.9f, .72f, .3f), 1, new Vector2(10, 40));
                ribbon.transform.SetAsFirstSibling();
            }

            if (card.Access == SkirmishCardAccess.NotOwned)
            {
                var tag = SkirmishWidgetFactory.Image("StoreTag", root, new Color(.04f, .06f, .07f, .98f));
                PlaceTopLeft(tag.rectTransform, 44, 57, 30, 27);
                var locked = SagaIcon(tag.transform, "Lock", SkirmishBattleLibraryArt.ResourceRoot + "icon_lock", new Color(1, .8f, .34f));
                Stretch(locked.rectTransform, 4, 4, 3, 3);
            }

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = rim;
            var colors = button.colors;
            colors.highlightedColor = new Color(1, 1, .85f);
            colors.pressedColor = new Color(.75f, .65f, .45f);
            button.colors = colors;
            string id = card.ScenarioId;
            button.onClick.AddListener(() => OnLibraryCard(id));
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;
        }

        private void BuildSagaChapter(RectTransform parent, SkirmishBattlefieldState field, int chapter, Vector2 point, float width, float height, bool fa)
        {
            string id = SkirmishBattleLibraryArt.ObjectiveIds[chapter];
            bool left = chapter % 2 == 0;
            float x = left ? 4 : width - 130;
            float y = Mathf.Max(6, point.y * height - 104);
            var flag = SkirmishWidgetFactory.Rect("ChapterFlag_" + id, parent);
            PlaceTopLeft(flag, x, y, 126, 96);
            var cloth = flag.gameObject.AddComponent<V3PolygonGraphic>();
            cloth.ConfigureResponsive(new[] { new Vector2(3, 6), new Vector2(123, 6), new Vector2(123, 76), new Vector2(63, 96), new Vector2(3, 76) }, chapter < 2 ? new Color(.23f, .065f, .025f, .98f) : new Color(.035f, .13f, .21f, .98f), new Color(.23f, .29f, .31f), 1.5f, new Vector2(126, 96));
            var pole = SkirmishWidgetFactory.Image("FlagPole", flag, new Color(.23f, .29f, .31f));
            PlaceTopLeft(pole.rectTransform, 0, 0, 126, 5);
            var label = CreateLabel(flag, "ChapterTitle", 21);
            PlaceTopLeft(label.rectTransform, 8, 13, 110, 54);
            SkirmishBattleLibraryArt.ObjectiveCopy(id, fa, out string title, out _);
            if (!fa)
                title = id switch
                {
                    "BA" => "BASE\nASSAULT",
                    "FC" => "FRONTLINE\nCONTROL",
                    "BT" => "BREAK\nTHROUGH",
                    _ => "CONVOY\nESCORT"
                };
            SetCopy(label, title, fa ? 22 : 20, fa ? 22 : 20);
            label.GetComponent<V3LocalizedTextBindingView>()?.ConfigureFontBounds(fa ? 22 : 20, fa ? 22 : 20);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            var icon = SagaIcon(flag, "Objective", SkirmishBattleLibraryArt.ObjectiveIcon(id), new Color(.97f, .78f, .35f));
            PlaceTopLeft(icon.rectTransform, 52, 67, 22, 22);
        }

        private Image SagaIcon(Transform parent, string name, string key, Color tint)
        {
            var icon = SkirmishWidgetFactory.Image(name, parent, tint);
            if (key.StartsWith("skirmish.shared.", StringComparison.Ordinal))
            {
                var registry = SkirmishLibrarySpriteRegistry.Load();
                if (registry != null && registry.TryGetSprite(key, out var sprite))
                    icon.sprite = sprite;
                icon.preserveAspect = true;
            }
            else
                ShowIcon(icon, key, tint);
            return icon;
        }

        private static V3PolygonGraphic SagaDisc(Transform parent, string name, float size, Color tint)
        {
            var disc = SkirmishWidgetFactory.Rect(name, parent).gameObject.AddComponent<V3PolygonGraphic>();
            var circle = new Vector2[48];
            for (int i = 0; i < circle.Length; i++)
            {
                float a = i * Mathf.PI * 2 / circle.Length;
                circle[i] = new Vector2(.5f + Mathf.Cos(a) * .5f, .5f + Mathf.Sin(a) * .5f) * size;
            }

            disc.ConfigureResponsive(circle, tint, Color.clear, 0, new Vector2(size, size));
            return disc;
        }

        private static Color SagaTierColor(int tier) => tier switch
        {
            0 => new Color(.76f, .35f, .12f),
            1 => new Color(.7f, .78f, .82f),
            2 => new Color(.98f, .7f, .17f),
            _ => new Color(.8f, .13f, .065f)};
    }
}
