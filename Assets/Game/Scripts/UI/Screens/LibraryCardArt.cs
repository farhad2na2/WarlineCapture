using System;
using Game.Skirmish.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        /// <summary>
        /// Design's painted battlefield plate. The files under SkirmishLibrary/Plates match
        /// the approved plates; this is the lookup Map, Grid and the theatre portraits share.
        /// </summary>
        private static Texture2D PaintedPlate(string mapId)
        {
            Sprite sprite = DistrictPlateSprite(mapId);
            if (sprite != null && sprite.texture != null)
                return sprite.texture;
            string id = string.IsNullOrEmpty(mapId) ? "DB" : mapId;
            Texture2D plate = SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.ResolvePlateKey(id));
            if (plate != null)
                return plate;
            return SkirmishLibraryPreviewAssets.Load("skirmish.plate." + id);
        }

        /// <summary>
        /// The painted district plate from the library art folder. A generated disc,
        /// a bullseye, or the placeholder texture is not a plate.
        /// </summary>
        private static Sprite DistrictPlateSprite(string mapId)
        {
            string id = string.IsNullOrEmpty(mapId) ? "DB" : mapId;
            SkirmishLibrarySpriteRegistry registry = SkirmishLibrarySpriteRegistry.Load();
            if (registry == null)
                return null;
            string[] keys =
            {
                SkirmishLibraryPreviewAssets.ResolvePlateKey(id),
                SkirmishLibraryPreviewAssets.PlatePath(id),
                "skirmish.plate." + id
            };
            for (int i = 0; i < keys.Length; i++)
            {
                if (!registry.TryGetSprite(keys[i], out Sprite sprite) || !IsDistrictPlateSprite(sprite, id))
                    continue;
                return sprite;
            }

            return null;
        }

        private static bool IsDistrictPlateSprite(Sprite sprite, string mapId)
        {
            if (sprite == null || string.IsNullOrEmpty(mapId))
                return false;
            string name = sprite.name ?? string.Empty;
            if (name.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (name.IndexOf("bullseye", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (name.IndexOf("disc", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return name.IndexOf("skirmish.plate." + mapId, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Draws the district plate across the whole card. The progress arc stays; the
        /// filled disc that read as an eye does not.
        /// </summary>
        private static void PaintDistrictCard(RectTransform card, float cardW, float cardH)
        {
            if (card == null)
                return;
            string mapId = card.name.StartsWith("ZoneCard_", StringComparison.Ordinal)
                ? card.name.Substring("ZoneCard_".Length)
                : string.Empty;
            Sprite plate = DistrictPlateSprite(mapId);
            RectTransform frame = card.Find("ThumbFrame") as RectTransform;
            if (frame == null)
                return;
            PlaceTopLeft(frame, 0f, 0f, cardW, cardH);
            frame.localScale = Vector3.one;
            KeepAuthoredSize(frame);
            if (frame.TryGetComponent<Image>(out Image frameImage))
            {
                frameImage.sprite = ZoneSolidSprite();
                frameImage.color = Color.white;
                frameImage.type = Image.Type.Simple;
            }

            Image art = EnsureDistrictCardArt(frame);
            art.sprite = plate;
            art.color = Color.white;
            art.type = Image.Type.Simple;
            art.preserveAspect = false;
            art.raycastTarget = false;
            art.enabled = plate != null;
            PlaceTopLeft(art.rectTransform, 0f, 0f, cardW, cardH);
            art.rectTransform.localScale = Vector3.one;
            KeepAuthoredSize(art.rectTransform);

            if (frame.Find("Thumbnail") is RectTransform preview)
            {
                PlaceTopLeft(preview, 0f, 0f, cardW, cardH);
                preview.localScale = Vector3.one;
                KeepAuthoredSize(preview);
                RawImage picture = preview.GetComponent<RawImage>();
                if (picture != null)
                {
                    picture.texture = plate != null ? plate.texture : null;
                    picture.color = Color.white;
                    picture.raycastTarget = false;
                    picture.enabled = plate != null && plate.texture != null;
                    if (picture.enabled)
                        CoverCrop(picture, cardW, cardH);
                }

                preview.SetAsLastSibling();
            }

            Transform disc = card.Find("RingDisc");
            if (disc != null)
                DestroyUi(disc.gameObject);
        }

        private static Image EnsureDistrictCardArt(RectTransform frame)
        {
            RectTransform node = frame.Find("CardArt") as RectTransform;
            Image image;
            if (node == null)
            {
                image = new GameObject("CardArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(frame, false);
            }
            else if (!node.TryGetComponent(out image))
                image = node.gameObject.AddComponent<Image>();
            return image;
        }

        /// <summary>
        /// Paints the battlefield plate across the whole clip and lays a vignette on it.
        /// Grid darkens the same plate so the cards sit on the painting.
        /// </summary>
        private void ApplyMapPlate(RawImage map, SkirmishBattlefieldState field, float plateWidth, float plateHeight)
        {
            if (map == null)
                return;
            string mapId = field.MapId;
            if (string.IsNullOrEmpty(mapId))
                mapId = LibraryMapId;
            bool grid = LibraryLayout == SkirmishBattlefieldLayout.Grid;
            Texture2D plate = PaintedPlate(mapId);
            Stretch(map.rectTransform);
            map.texture = plate != null ? plate : SkirmishLibraryPreviewAssets.Placeholder();
            map.color = grid ? new Color(0.42f, 0.38f, 0.34f, 1f) : Color.white;
            map.raycastTarget = false;
            map.uvRect = new Rect(0f, 0f, 1f, 1f);
            CoverCrop(map, plateWidth, plateHeight);
            EnsurePlateVignette(map.rectTransform, grid);
        }

        /// <summary>
        /// Grid keeps the painted plate full-bleed and lets it show through the panel.
        /// </summary>
        private void ApplyGridPlate(SkirmishBattlefieldState field)
        {
            RawImage map = transform.Find("SkirmishSetupComposition/OperationPreview/MapPreviewClip/MapPreview")
                ?.GetComponent<RawImage>();
            float plateWidth = 1064f;
            float plateHeight = 559f;
            RectTransform clip = map != null ? map.rectTransform.parent as RectTransform : null;
            if (clip != null && TryFrameSize(clip, out float width, out float height))
            {
                plateWidth = width;
                plateHeight = height;
            }

            ApplyMapPlate(map, field, plateWidth, plateHeight);
            if (_libraryGrid == null)
                return;
            V3GradientGraphic fill = _libraryGrid.GetComponent<V3GradientGraphic>();
            if (fill == null)
                return;
            fill.Configure(
                new Color(0.02f, 0.03f, 0.04f, 0.22f),
                new Color(0.02f, 0.03f, 0.04f, 0.38f),
                new Color(0f, 0f, 0f, 0f),
                0f);
        }

        private static void EnsurePlateVignette(RectTransform plate, bool grid)
        {
            if (plate == null)
                return;
            Transform node = plate.Find("PlateVignette");
            Image shade = null;
            if (node != null)
                node.TryGetComponent(out shade);
            if (shade == null)
            {
                shade = new GameObject("PlateVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                shade.transform.SetParent(plate, false);
                shade.sprite = ZoneVignetteSprite();
                shade.type = Image.Type.Simple;
                shade.raycastTarget = false;
                Stretch(shade.rectTransform);
            }

            shade.transform.SetAsLastSibling();
            shade.color = new Color(0.02f, 0.03f, 0.04f, grid ? 0.92f : 0.62f);
        }

        /// <summary>
        /// Approved map and grid card: dark frame, gold edge, photo across the top, warm title.
        /// Preview rects on grid cards stay where the layout put them.
        /// </summary>
        private void StyleBattleCard(Transform card, bool selected, bool playable, bool pin)
        {
            if (card == null)
                return;
            SkirmishLibraryButtonStateView state = card.GetComponent<SkirmishLibraryButtonStateView>();
            if (state != null)
            {
                Color border = selected
                    ? new Color(1f, 0.86f, 0.42f, 1f)
                    : playable
                        ? new Color(0.78f, 0.62f, 0.28f, 1f)
                        : new Color(0.38f, 0.34f, 0.28f, 1f);
                state.ApplyPalette(
                    new Color(0.05f, 0.06f, 0.07f, 0.98f),
                    new Color(0.02f, 0.025f, 0.03f, 0.98f),
                    border,
                    true,
                    selected);
            }
            else if (card.TryGetComponent<Image>(out Image plate))
                plate.color = new Color(0.04f, 0.05f, 0.06f, 0.94f);

            if (pin)
            {
                FillPinPortrait(card);
                StyleMapPinCard(card, selected, playable);
                return;
            }
            TintNamedImage(card, "FooterScrim", new Color(0.02f, 0.03f, 0.04f, 0.90f));
            TintNamedImage(card, "ScenarioBadge", new Color(0.02f, 0.03f, 0.04f, 0.84f));
            TintNamedImage(card, "MedalBacking", new Color(0.02f, 0.02f, 0.03f, 0.55f));
            if (card.Find("Title") is Transform titleNode && titleNode.TryGetComponent<TMP_Text>(out TMP_Text title))
            {
                title.fontStyle = FontStyles.Bold;
                title.color = new Color(0.98f, 0.96f, 0.90f, 1f);
            }

            if (card.Find("Subtitle") is Transform subtitleNode && subtitleNode.TryGetComponent<TMP_Text>(out TMP_Text subtitle))
                subtitle.color = new Color(0.78f, 0.74f, 0.64f, 1f);
        }

        /// <summary>
        /// v35 approved-2 map card: opaque dark frame, the scenario art at full contrast (no tint, no alpha),
        /// a solid title bar with a gold rule, a backed row of medal circles, and a heavy gold frame when selected.
        /// Art is the existing scenario preview / zone plate only (Assets/Game/Art/SkirmishLibrary); nothing new is drawn.
        /// </summary>
        private static void StyleMapPinCard(Transform card, bool selected, bool playable)
        {
            if (card.TryGetComponent<Image>(out Image plate) && card.GetComponent<SkirmishLibraryButtonStateView>() == null)
                plate.color = new Color(0.025f, 0.03f, 0.035f, 1f);
            if (card.Find("Preview") is Transform previewNode && previewNode.TryGetComponent<RawImage>(out RawImage art) && art.texture != null)
                art.color = Color.white;
            TintNamedImage(card, "FooterScrim", new Color(0.015f, 0.02f, 0.025f, 1f));
            TintNamedImage(card, "ScenarioBadge", new Color(0.015f, 0.02f, 0.025f, 0.96f));
            TintNamedImage(card, "MedalBacking", new Color(0f, 0f, 0f, 0.85f));
            if (card.Find("FooterScrim") is RectTransform scrim && scrim.Find("TitleRule") == null)
            {
                var rule = new GameObject("TitleRule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                rule.transform.SetParent(scrim, false);
                rule.raycastTarget = false;
                rule.color = playable ? new Color(0.86f, 0.66f, 0.24f, 1f) : new Color(0.45f, 0.40f, 0.32f, 1f);
                RectTransform r = rule.rectTransform;
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.sizeDelta = new Vector2(0f, 2f);
                r.anchoredPosition = Vector2.zero;
            }

            if (card.Find("Title") is Transform titleNode && titleNode.TryGetComponent<TMP_Text>(out TMP_Text title))
            {
                title.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                title.color = new Color(1f, 0.97f, 0.90f, 1f);
            }

            if (card.Find("Subtitle") is Transform subtitleNode && subtitleNode.TryGetComponent<TMP_Text>(out TMP_Text subtitle))
                subtitle.color = new Color(0.86f, 0.80f, 0.66f, 1f);

            SkirmishDifficultyId[] tiers =
            {
                SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander
            };
            for (int i = 0; i < tiers.Length; i++)
            {
                // v36 approved-2: every circle carries its tier colour (SkirmishMedalRowWidget.TierColor) over the real medal
                // sprite; earned stays full white (true sprite colours), unearned is the tier fill at reduced brightness.
                if (card.Find("Medal" + i) is Transform medal && medal.TryGetComponent<Image>(out Image medalImage) &&
                    medalImage.color.r < 0.99f)
                {
                    Color tier = SkirmishMedalRowWidget.TierColor(tiers[i]);
                    medalImage.color = new Color(tier.r * 0.8f, tier.g * 0.8f, tier.b * 0.8f, 1f);
                }
            }

            Outline edge = card.GetComponent<Outline>();
            if (edge == null)
                edge = card.gameObject.AddComponent<Outline>();
            edge.useGraphicAlpha = false;
            edge.effectColor = selected
                ? new Color(1f, 0.80f, 0.25f, 1f)
                : playable ? new Color(0.62f, 0.48f, 0.22f, 0.9f) : new Color(0.32f, 0.29f, 0.24f, 0.9f);
            edge.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(1.5f, -1.5f);
        }

        /// <summary>
        /// v35 unit art slot, one per force type, loaded from Resources/SkirmishLibrary/UnitArt/{Ground,AirMobile,Combined}
        /// (Design PR #67 painted card art). Drop-in swap: replace the PNG under that folder (same name, keep the meta),
        /// or change UnitArtKey below. Force type is SkirmishBattleCardState.ArmyProfile (G/A/C).
        /// </summary>
        public const string UnitArtRoot = "SkirmishLibrary/UnitArt/";

        public static string UnitArtKey(string armyProfile)
        {
            switch (armyProfile)
            {
                case "G": return "Ground";
                case "A": return "AirMobile";
                case "C": return "Combined";
                default: return null;
            }
        }

        /// <summary>
        /// Map card main image: UnitArt/{Ground,AirMobile,Combined} (Design PR #67 painted 1152x720) at full contrast,
        /// cover-fitted so the aspect is preserved (no stretching). Returns false when the file is missing so the caller
        /// falls back to the scenario preview.
        /// </summary>
        private static bool TryShowUnitArt(RawImage preview, string armyProfile, float x, float y, float frameWidth, float frameHeight)
        {
            string key = UnitArtKey(armyProfile);
            if (preview == null || key == null)
                return false;
            Sprite sprite = LoadLibraryIcon(UnitArtRoot + key);
            Texture2D texture = sprite != null ? sprite.texture : Resources.Load<Texture2D>(UnitArtRoot + key);
            if (texture == null)
                return false;
            preview.raycastTarget = false;
            preview.texture = texture;
            preview.color = Color.white;
            preview.enabled = true;
            PlaceTopLeft(preview.rectTransform, x, y, frameWidth, frameHeight);
            CoverCrop(preview, frameWidth, frameHeight);
            return true;
        }

        /// <summary>Retired in v35 (map cards now use the unit art as the main image); kept for a possible corner-badge variant.</summary>
        private static void AddUnitArtBadge(Transform card, string armyProfile, float width, bool fa)
        {
            string key = UnitArtKey(armyProfile);
            if (card == null || key == null || card.Find("UnitArt") != null)
                return;
            Sprite own = LoadLibraryIcon(UnitArtRoot + key);
            Sprite[] icons = own != null
                ? new[] { own }
                : key == "Combined"
                    ? new[] { LoadLibraryIcon(UnitArtRoot + "Ground"), LoadLibraryIcon(UnitArtRoot + "AirMobile") }
                    : System.Array.Empty<Sprite>();
            icons = System.Array.FindAll(icons, icon => icon != null);
            if (icons.Length == 0)
                return;
            const float size = 34f;
            const float gap = 2f;
            const float pad = 4f;
            float run = icons.Length * size + (icons.Length - 1) * gap + pad * 2f;
            var slot = new GameObject("UnitArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            slot.transform.SetParent(card, false);
            slot.raycastTarget = false;
            slot.color = new Color(0.015f, 0.02f, 0.025f, 0.9f);
            // Opposite corner from the scenario id badge (top-left in EN, top-right in FA).
            PlaceTopLeft(slot.rectTransform, fa ? 4f : width - 4f - run, 4f, run, size + pad * 2f);
            var edge = slot.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.86f, 0.66f, 0.24f, 0.9f);
            edge.effectDistance = new Vector2(1f, -1f);
            for (int i = 0; i < icons.Length; i++)
            {
                var icon = new GameObject("UnitArt" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(slot.transform, false);
                icon.sprite = icons[i];
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = Color.white;
                PlaceTopLeft(icon.rectTransform, pad + i * (size + gap), pad, size, size);
            }
        }

        private static void FillPinPortrait(Transform card)
        {
            Transform previewNode = card.Find("Preview");
            if (previewNode == null || !previewNode.TryGetComponent<RawImage>(out RawImage preview))
                return;
            if (preview.texture == null)
                return;
            var rect = card as RectTransform;
            if (rect == null)
                return;
            float scrimHeight = 0f;
            Transform scrim = card.Find("FooterScrim");
            if (scrim is RectTransform scrimRect)
                scrimHeight = scrimRect.sizeDelta.y;
            float previewHeight = Mathf.Max(48f, rect.sizeDelta.y - scrimHeight - 4f);
            PlaceCoverPreview(preview, rect.sizeDelta.x, previewHeight, preview.texture as Texture2D, false);
        }

        /// <summary>
        /// Scenario art when the preview file exists. S005 and S006 have no plate of their own,
        /// so the card covers with the zone painting instead of an empty black frame.
        /// </summary>
        private static void ShowPinPreview(
            RawImage preview,
            string previewKey,
            string mapId,
            float x,
            float y,
            float frameWidth,
            float frameHeight)
        {
            Texture2D scenario = LoadScenarioPreview(previewKey);
            if (scenario != null)
            {
                PlaceFittedPreview(preview, previewKey, x, y, frameWidth, frameHeight);
                return;
            }

            if (preview == null)
                return;
            Texture2D plate = PaintedPlate(mapId);
            preview.raycastTarget = false;
            preview.texture = plate;
            preview.color = Color.white;
            preview.enabled = plate != null;
            if (plate == null)
                return;
            PlaceTopLeft(preview.rectTransform, x, y, frameWidth, frameHeight);
            CoverCrop(preview, frameWidth, frameHeight);
        }

        private static void TintNamedImage(Transform card, string childName, Color color)
        {
            Transform node = card.Find(childName);
            if (node != null && node.TryGetComponent<Image>(out Image image))
                image.color = color;
        }

        /// <summary>
        /// The difficulty medal itself. Earned stays full colour from the ledger mask.
        /// Unearned keeps that same icon and dims it. The empty outline is not a slot.
        /// </summary>
        private static void PaintDifficultyMedal(Image image, SkirmishDifficultyId difficulty, bool earned)
        {
            if (image == null)
                return;
            string path = SkirmishBattleLibraryArt.Medal(true, difficulty);
            Sprite sprite = LoadLibraryIcon(path);
            if (sprite == null)
                sprite = Resources.Load<Sprite>(path);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.useSpriteMesh = false;
            image.raycastTarget = false;
            image.color = earned ? Color.white : new Color(0.46f, 0.46f, 0.46f, 0.9f);
            bool shown = sprite != null;
            image.enabled = shown;
            if (image.gameObject.activeSelf != shown)
                image.gameObject.SetActive(shown);
        }

        /// <summary>
        /// Pin subtitles are forced onto one 22px line after the card is built, which clips
        /// "GROUND · ESTABLISHED". Shrink onto one line, or wrap onto two, so the full type shows.
        /// </summary>
        private void FitBattleCardSubtitles()
        {
            FitCardSubtitles(_libraryPins);
            FitCardSubtitles(_libraryGrid);
        }

        private static void FitCardSubtitles(Transform root)
        {
            if (root == null || !root.gameObject.activeInHierarchy)
                return;
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text subtitle = texts[i];
                if (subtitle == null || subtitle.name != "Subtitle" || subtitle.rectTransform == null)
                    continue;
                Transform parent = subtitle.transform.parent;
                if (parent == null)
                    continue;
                bool card = parent.name == "EmptyCell" || parent.name.StartsWith("BattleCard_", StringComparison.Ordinal);
                if (!card || string.IsNullOrWhiteSpace(subtitle.text))
                    continue;
                FitOneCardSubtitle(subtitle);
            }
        }

        private static void FitOneCardSubtitle(TMP_Text subtitle)
        {
            float boxW = subtitle.rectTransform.rect.width > 2f
                ? subtitle.rectTransform.rect.width
                : subtitle.rectTransform.sizeDelta.x;
            float boxH = subtitle.rectTransform.rect.height > 2f
                ? subtitle.rectTransform.rect.height
                : subtitle.rectTransform.sizeDelta.y;
            if (boxW < 8f)
                return;
            string source = subtitle.text;
            V3LocalizedTextBindingView binding = subtitle.GetComponent<V3LocalizedTextBindingView>();
            subtitle.enableAutoSizing = true;
            subtitle.overflowMode = TextOverflowModes.Overflow;
            for (float size = Mathf.Clamp(boxH * 0.6f, 20f, 26f); size >= 11f - 0.01f; size -= 1f) // v36: larger cards start larger
            {
                if (!SubtitleFits(subtitle, source, size, boxW, boxH, false))
                    continue;
                CommitCardSubtitle(subtitle, binding, size, false);
                return;
            }

            for (float size = 18f; size >= 10f - 0.01f; size -= 1f)
            {
                if (!SubtitleFits(subtitle, source, size, boxW, boxH, true))
                    continue;
                CommitCardSubtitle(subtitle, binding, size, true);
                return;
            }

            CommitCardSubtitle(subtitle, binding, 10f, true);
        }

        private static bool SubtitleFits(TMP_Text subtitle, string source, float size, float boxW, float boxH, bool wrap)
        {
            subtitle.text = source;
            subtitle.fontSizeMin = size;
            subtitle.fontSizeMax = size;
            subtitle.fontSize = size;
            subtitle.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            subtitle.maxVisibleLines = wrap ? 2 : 1;
            subtitle.overflowMode = TextOverflowModes.Overflow;
            subtitle.ForceMeshUpdate();
            if (subtitle.isTextTruncated)
                return false;
            if (!wrap)
                return subtitle.preferredWidth <= boxW + 1.5f && subtitle.preferredHeight <= boxH + 2f;
            if (subtitle.preferredHeight > boxH + 2f)
                return false;
            return SubtitleShowsSource(subtitle, source);
        }

        private static void CommitCardSubtitle(TMP_Text subtitle, V3LocalizedTextBindingView binding, float size, bool wrap)
        {
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = size;
            subtitle.fontSizeMax = size;
            subtitle.fontSize = size;
            subtitle.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            subtitle.overflowMode = TextOverflowModes.Overflow;
            subtitle.maxVisibleLines = wrap ? 2 : 1;
            subtitle.ForceMeshUpdate();
        }

        private static bool SubtitleShowsSource(TMP_Text text, string source)
        {
            if (text == null)
                return false;
            int expected = 0;
            string flat = source ?? string.Empty;
            for (int i = 0; i < flat.Length; i++)
            {
                if (char.IsLetterOrDigit(flat[i]))
                    expected++;
            }

            TMP_TextInfo info = text.textInfo;
            if (info == null || info.characterInfo == null)
                return expected == 0;
            int visible = 0;
            int count = Mathf.Min(info.characterCount, info.characterInfo.Length);
            for (int i = 0; i < count; i++)
            {
                if (!info.characterInfo[i].isVisible)
                    continue;
                if (char.IsLetterOrDigit(info.characterInfo[i].character))
                    visible++;
            }

            return visible >= expected;
        }
    }
}
