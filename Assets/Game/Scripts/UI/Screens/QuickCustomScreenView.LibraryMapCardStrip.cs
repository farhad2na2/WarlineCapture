using Game.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// v50 (P1, Game PM item 1): approved-2 map card title strip above the art, e.g. "S001  GROUND · FIELD".
    /// P2 calls it once per pin card from CreateBattleCard (LibraryMap.cs), straight after StyleBattleCard:
    ///     if (pin) StyleMapCardTitleStrip(button.transform, fa);
    /// Data: the scenario id the card already shows (ScenarioBadge/ScenarioId, card.ScenarioId) and that battle's catalog
    /// entry (SkirmishBattleCatalogEntry.ArmyProfile / StartProfile -> SkirmishBattleLibraryArt.ProfileCopy). The art
    /// (Preview, cover-fitted by FillPinPortrait) is moved down under the strip and re-covered, so the strip never sits
    /// on the picture; the old top-left id badge is hidden because the id now leads the strip.
    /// </summary>
    public sealed partial class QuickCustomScreenView
    {
        internal const string MapCardStripName = "TitleStrip";
        /// <summary>(size, tracking) tried in order: 22 -> 16 px at 0, then 16 at -4 / -8, then 14 at 0 / -4 / -8.</summary>
        private static readonly Vector2[] MapCardStripSteps =
        {
            new Vector2(22f, 0f), new Vector2(21f, 0f), new Vector2(20f, 0f), new Vector2(19f, 0f), new Vector2(18f, 0f),
            new Vector2(17f, 0f), new Vector2(16f, 0f), new Vector2(16f, -4f), new Vector2(16f, -8f),
            new Vector2(15f, -4f), new Vector2(14f, 0f), new Vector2(14f, -4f), new Vector2(14f, -8f),
        };
        private const float MapCardStripPad = 8f;
        private static readonly Color MapCardStripIdColor = new Color(0.40f, 0.80f, 0.92f, 1f);

        private void StyleMapCardTitleStrip(Transform card, bool fa)
        {
            RectTransform rect = card as RectTransform;
            if (rect == null)
                return;
            float width = rect.sizeDelta.x;
            float height = rect.sizeDelta.y;
            TMP_Text badgeId = card.Find("ScenarioBadge/ScenarioId")?.GetComponent<TMP_Text>();
            string scenarioId = badgeId != null ? (badgeId.text ?? string.Empty).Trim() : string.Empty;
            string text = string.Empty;
            if (!string.IsNullOrEmpty(scenarioId) && TryFindEntry(scenarioId, out SkirmishBattleCatalogEntry entry))
            {
                SkirmishBattleLibraryArt.ProfileCopy(entry.ArmyProfile, fa, out string army);
                SkirmishBattleLibraryArt.ProfileCopy(entry.StartProfile, fa, out string start);
                text = string.IsNullOrEmpty(start) ? army : string.IsNullOrEmpty(army) ? start : army + " · " + start;
            }

            if (string.IsNullOrEmpty(scenarioId) || width < 60f || height < 60f)
            {
                Debug.Log("[MapCardStrip] locale=" + (fa ? "fa-IR" : "en") + " ratio=" + TheatreRatioLabel() + " battle=" +
                    (string.IsNullOrEmpty(scenarioId) ? "-" : scenarioId) + " text=- fits=0 overlapArt=0 reason=" + (string.IsNullOrEmpty(scenarioId) ? "noId" : "small"));
                return;
            }

            float stripH = Mathf.Round(Mathf.Clamp(height * 0.14f, 30f, 40f));
            Image strip = card.Find(MapCardStripName)?.GetComponent<Image>();
            if (strip == null)
            {
                strip = new GameObject(MapCardStripName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                strip.transform.SetParent(card, false);
                strip.raycastTarget = false;
                CreateLabel(strip.transform, "StripId", LibraryFontFloor);
                CreateLabel(strip.transform, "StripText", LibraryFontFloor);
            }

            strip.color = new Color(0.015f, 0.02f, 0.025f, 1f);
            PlaceTopLeft(strip.rectTransform, 0f, 0f, width, stripH);
            strip.transform.SetAsLastSibling();

            // The art moves under the strip, keeping its bottom edge (FillPinPortrait: card - FooterScrim - 4).
            RawImage art = card.Find("Preview")?.GetComponent<RawImage>();
            float artTop = 0f;
            if (art != null)
            {
                RectTransform artRect = art.rectTransform;
                float artBottom = -artRect.anchoredPosition.y + artRect.sizeDelta.y;
                float artX = artRect.anchoredPosition.x;
                float artW = artRect.sizeDelta.x;
                float artH = Mathf.Max(24f, artBottom - stripH);
                if (art.texture is Texture2D cover && art.enabled)
                    PlaceCoverPreview(art, artW, artH, cover, art.color.r < 0.99f);
                else
                    PlaceTopLeft(artRect, artX, 0f, artW, artH);
                artRect.anchoredPosition = new Vector2(artX, -stripH);
                artTop = stripH;
            }

            SetActive(card.Find("ScenarioBadge"), false);
            if (card.Find("Recommended") is RectTransform recommended && -recommended.anchoredPosition.y < stripH)
                recommended.anchoredPosition = new Vector2(recommended.anchoredPosition.x, -(stripH + 2f));

            TMP_Text id = strip.transform.Find("StripId")?.GetComponent<TMP_Text>();
            TMP_Text body = strip.transform.Find("StripText")?.GetComponent<TMP_Text>();
            float inner = width - 2f * MapCardStripPad;
            float size = LibraryFontFloor;
            float idW = 0f;
            float bodyW = 0f;
            bool fits = false;
            if (id != null)
            {
                SetCopy(id, scenarioId, size, size);
                id.isRightToLeftText = false;
                id.fontStyle = FontStyles.Bold;
                id.color = MapCardStripIdColor;
            }

            if (body != null)
            {
                SetCopy(body, text, size, size);
                body.isRightToLeftText = fa;
                body.fontStyle = fa ? FontStyles.Bold : FontStyles.Bold | FontStyles.UpperCase;
                body.color = new Color(0.98f, 0.96f, 0.90f, 1f);
            }

            // Largest size from the library floor down to 16 px at which id + gap + text fit the strip on one line; then
            // (v50b, v50 4:3 "COMBINED · ESTABLISHED" needed 273/227 at 16 px) tighter tracking, then 14 px (the grid
            // header floor) with tracking down to -8.
            float track = 0f;
            for (int step = 0; step < MapCardStripSteps.Length && !fits; step++)
            {
                float s = MapCardStripSteps[step].x;
                float t = MapCardStripSteps[step].y;
                if (body != null)
                    body.characterSpacing = t;
                if (id != null)
                    id.characterSpacing = t;
                idW = id != null ? StripWidthAt(id, s) : 0f;
                bodyW = body != null && text.Length > 0 ? StripWidthAt(body, s) : 0f;
                size = s;
                track = t;
                fits = idW + MapCardStripPad + bodyW <= inner && s * (fa ? 1.65f : 1.2f) <= stripH + 6f;
            }

            float lineH = stripH - 4f;
            float idX = fa ? width - MapCardStripPad - idW : MapCardStripPad;
            float bodyX = fa ? MapCardStripPad : MapCardStripPad + idW + MapCardStripPad;
            float bodyBox = Mathf.Max(1f, inner - idW - MapCardStripPad);
            HoldStripText(id, size, idX, lineH, idW + 2f, fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft);
            HoldStripText(body, size, bodyX, lineH, bodyBox, fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft);
            if (body != null)
                body.gameObject.SetActive(text.Length > 0);

            bool overlapArt = art != null && art.enabled && artTop < stripH - 0.5f;
            Debug.Log("[MapCardStrip] locale=" + (fa ? "fa-IR" : "en") + " ratio=" + TheatreRatioLabel() + " battle=" + scenarioId +
                " text=" + scenarioId + (text.Length > 0 ? " " + text : string.Empty) + " fits=" + (fits ? 1 : 0) +
                " overlapArt=" + (overlapArt ? 1 : 0) + " size=" + size.ToString("0.0") + " track=" + track.ToString("0") + " strip=" + width.ToString("0") + "x" + stripH.ToString("0") +
                " need=" + (idW + MapCardStripPad + bodyW).ToString("0") + "/" + inner.ToString("0"));
        }

        /// <summary>
        /// v50b: the map cards are built before EnforceLibraryFontFloor, which lifts every strip label to 22 px (v50: S002's
        /// id ran into its text at 4:3 / 16:9). LibraryHeaderBreadcrumb.Apply, which runs from PlaceLibraryHeader after that
        /// pass, re-applies each strip label's held size so it matches the positions the strip was laid out for.
        /// </summary>
        private void ReassertMapCardStrips(Transform composition)
        {
            if (composition == null || LibraryLevel != SkirmishLibraryLevel.Battlefield)
                return;
            LibraryFixedTextSize[] held = composition.GetComponentsInChildren<LibraryFixedTextSize>(false);
            for (int i = 0; i < held.Length; i++)
            {
                Transform parent = held[i].transform.parent;
                if (parent != null && parent.name == MapCardStripName)
                    held[i].Apply();
            }
        }

        private static float StripWidthAt(TMP_Text text, float size)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
            return text.GetPreferredValues(text.text, float.PositiveInfinity, float.PositiveInfinity).x;
        }

        private static void HoldStripText(TMP_Text text, float size, float x, float lineH, float width, TextAlignmentOptions alignment)
        {
            if (text == null)
                return;
            PlaceTopLeft(text.rectTransform, x, 2f, width, lineH);
            text.alignment = alignment;
            text.overflowMode = TextOverflowModes.Overflow;
            LibraryFixedTextSize held = text.GetComponent<LibraryFixedTextSize>() ?? text.gameObject.AddComponent<LibraryFixedTextSize>();
            held.Size = size;
            held.Apply();
        }
    }
}
