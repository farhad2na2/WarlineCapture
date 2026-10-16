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
            string id = string.IsNullOrEmpty(mapId) ? "DB" : mapId;
            Texture2D plate = SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.ResolvePlateKey(id));
            if (plate != null)
                return plate;
            return SkirmishLibraryPreviewAssets.Load("skirmish.plate." + id);
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
                FillPinPortrait(card);
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

        private static void TintNamedImage(Transform card, string childName, Color color)
        {
            Transform node = card.Find(childName);
            if (node != null && node.TryGetComponent<Image>(out Image image))
                image.color = color;
        }
    }
}
