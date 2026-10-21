using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private void PresentSagaTheatreRoute(RectTransform host, List<TheatreZoneSlot> slots, float width, float height)
        {
            var points = new List<Vector2>();
            foreach (string id in SkirmishBattleLibraryArt.MapIds)
            {
                for (int i = 0; i < slots.Count; i++)
                    if (slots[i].MapId == id)
                    {
                        Vector2 anchor = new Vector2(slots[i].Card.center.x, slots[i].Card.yMax + 12f);
                        points.Add(new Vector2(anchor.x / width, anchor.y / height));
                        break;
                    }
            }

            var route = SkirmishWidgetFactory.Rect("TheatreSagaRoute", host).gameObject.AddComponent<SkirmishSagaRoadGraphic>();
            Stretch(route.rectTransform);
            route.Configure(points, true, 5f);
        }

        private void BuildSagaTheatrePanel(SkirmishTheaterPinState zone, RectTransform panel)
        {
            if (panel == null || _libraryViewModel == null)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (!IsTheatreBottomSheet(panel))
            {
                PlaceTheatreLowerBlock(panel);
                LayoutTheatreObjectives(panel, fa);
            }

            foreach (Transform child in panel)
                if (child.name.StartsWith("ZoneMedal_", StringComparison.Ordinal) || child.name == "Value")
                    child.gameObject.SetActive(false);
            if (!TryTheatrePanelArea(panel, out Rect area, out Transform blurb))
                return;
            if (blurb != null)
                blurb.gameObject.SetActive(false);
            // Reserve four readable trails from the measured footer and localized
            // header, shrinking the preview when an ultrawide/notched panel needs it.
            if (area.height < 320f && panel.Find("Preview")is RectTransform preview)
            {
                float reduction = Mathf.Min(320f - area.height, Mathf.Max(0f, preview.sizeDelta.y - 96f));
                preview.sizeDelta = new Vector2(preview.sizeDelta.x, preview.sizeDelta.y - reduction);
                KeepAuthoredSize(preview);
                foreach (string name in new[]
                {
                    "Stats",
                    "Rule"
                }

                )
                    if (panel.Find(name)is RectTransform headerPart)
                    {
                        headerPart.anchoredPosition += new Vector2(0, reduction);
                        KeepAuthoredSize(headerPart);
                    }

                if (!TryTheatrePanelArea(panel, out area, out _))
                    return;
            }

            var field = _libraryViewModel.Battlefield;
            if (field.MapId != zone.MapId || field.GridCards == null)
                return;
            var holder = SkirmishWidgetFactory.Rect("TheatreMedalTrails", panel);
            PlaceTopLeft(holder, area.x, area.y, area.width, area.height);
            float rowHeight = Mathf.Min(86, area.height / 4);
            for (int chapter = 0; chapter < 4; chapter++)
            {
                string objective = SkirmishBattleLibraryArt.ObjectiveIds[chapter];
                var row = SkirmishWidgetFactory.Rect("MedalTrail_" + objective, holder);
                PlaceTopLeft(row, 0, chapter * rowHeight, area.width, rowHeight);
                var icon = SagaIcon(row, "Objective", SkirmishBattleLibraryArt.ObjectiveIcon(objective), new Color(.95f, .75f, .32f));
                PlaceTopLeft(icon.rectTransform, fa ? area.width - 24 : 0, 8, 22, 22);
                var title = CreateLabel(row, "ObjectiveTitle", 18);
                PlaceTopLeft(title.rectTransform, fa ? 0 : 28, 0, area.width - 28, 44);
                SkirmishBattleLibraryArt.ObjectiveCopy(objective, fa, out string copy, out _);
                if (!fa && objective == "BT") copy = copy.Replace("BREAKTHROUGH", "BREAK\nTHROUGH");
                SetCopy(title, copy, 16, 20);
                title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                title.textWrappingMode = TextWrappingModes.Normal;
                float diameter = Mathf.Min(30, Mathf.Min((area.width - 12) / 6f - 4, rowHeight - 54));
                float pitch = (area.width - 12 - diameter) / 5;
                var points = new List<Vector2>();
                for (int column = 0; column < 6; column++)
                {
                    int index = chapter * 6 + column;
                    var card = field.GridCards[index];
                    if (IsEmptyCard(card))
                        continue;
                    int visual = fa ? 5 - column : column;
                    float x = 6 + visual * pitch, y = 48 + (column % 2 == 0 ? 0 : 3);
                    points.Add(new Vector2((x + diameter / 2) / area.width, (y + diameter / 2 - 48) / (rowHeight - 48)));
                    var medal = SagaIcon(row, "TrailMedal_" + card.ScenarioId, HighestMedalIcon(card.Medals), Color.white);
                    PlaceTopLeft(medal.rectTransform, x, y, diameter, diameter);
                }

                var trail = SkirmishWidgetFactory.Rect("Trail", row).gameObject.AddComponent<SkirmishSagaRoadGraphic>();
                PlaceTopLeft(trail.rectTransform, 0, 48, area.width, rowHeight - 48);
                trail.Configure(points, true, 2f);
                trail.transform.SetAsFirstSibling();
                var hit = row.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                hit.raycastTarget = true;
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                string id = objective;
                button.onClick.AddListener(() =>
                {
                    _libraryViewModel.ZoomIn();
                    SelectLibraryObjective(id);
                });
            }
        }
    }
}
