using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        /// <summary>Plate-space reserve rect this seating pass inset for. Capture compares it with TheatreDetailRect.</summary>
        public Rect SeatingReserveRect { get; private set; }

        private void PresentTheatrePlate(RawImage map, float plateWidth, float plateHeight)
        {
            Texture2D painted = LoadTheatreBase();
            LogTheatreBase(painted);
            map.texture = painted != null ? painted : SkirmishLibraryPreviewAssets.Placeholder();
            map.color = TheatrePaintDim();
            BrightenTheatrePlate(map);
            Stretch(map.rectTransform);
            map.rectTransform.localScale = Vector3.one;
            ApplyTheatreCover(map, plateWidth, plateHeight);
        }

        private void PresentRegions(SkirmishTheaterState theater, bool fa)
        {
            if (_libraryRegions == null)
                return;
            if (!TheatreSeatGeometry.SeatingReady)
            {
                TheatreSeatGeometry.NoteDetailReserve(false);
                SeatingReserveRect = default;
                return;
            }
            ClearChildren(_libraryRegions);
            _regionButtons.Clear();
            RawImage map = transform.Find("SkirmishSetupComposition/OperationPreview/MapPreviewClip/MapPreview")
                ?.GetComponent<RawImage>();
            bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
            if (battlefield || theater.Pins == null)
            {
                TheatreSeatGeometry.NoteDetailReserve(false);
                SeatingReserveRect = default;
                if (map != null)
                    map.enabled = true;
                HideTheaterLabels(map);
                return;
            }

            Canvas.ForceUpdateCanvases();
            RectTransform plate = map != null ? map.rectTransform.parent as RectTransform : null;
            if (!TryFrameSize(plate, out float plateWidth, out float plateHeight))
            {
                plateWidth = 1024f;
                plateHeight = 540f;
            }

            Texture2D painted = LoadTheatreBase();
            LogTheatreBase(painted);
            var fit = new GameObject("TheaterFit", typeof(RectTransform)).GetComponent<RectTransform>();
            fit.SetParent(_libraryRegions, false);
            Stretch(fit);
            fit.localScale = Vector3.one;
            Canvas.ForceUpdateCanvases();
            if (TryFrameSize(fit, out float fittedWidth, out float fittedHeight))
            {
                plateWidth = fittedWidth;
                plateHeight = fittedHeight;
            }

            CoverWindow(
                plateWidth,
                plateHeight,
                TextureAspect(painted),
                TheatreFocalX,
                TheatreFocalY,
                out float cropU,
                out float cropV,
                out float cropW,
                out float cropH);

            NoteHostCanvas(plate);
            string selectedMap = !string.IsNullOrEmpty(theater.SelectedMapId)
                ? theater.SelectedMapId
                : "DB";
            bool reserve = TheatreSeatGeometry.DetailReserveOn(
                plateWidth, plateHeight, fa, plateWidth >= 600f, TheatreSeatGeometry.ShotIsFourByThree(), selectedMap);
            TheatreSeatGeometry.NoteDetailReserve(reserve);
            TheatreSeatGeometry.ShotReserve = reserve;
            TheatreSeatGeometry.Box viewport = TheatreSeatGeometry.OpenViewport(plateWidth, plateHeight, fa, reserve);
            TheatreSeatGeometry.Box reserveRect = TheatreSeatGeometry.DetailRect(plateWidth, plateHeight, fa, reserve);
            RawImage theatrePaint = null;
            if (painted != null)
            {
                var raw = new GameObject("TheaterPaint", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                raw.transform.SetParent(fit, false);
                Stretch(raw.rectTransform);
                raw.texture = painted;
                raw.color = TheatrePaintDim();
                BrightenTheatrePlate(raw);
                raw.raycastTarget = false;
                raw.rectTransform.localScale = Vector3.one;
                ApplyTheatreCover(raw, plateWidth, plateHeight);
                theatrePaint = raw;
                var wash = new GameObject("TheatreWash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                wash.transform.SetParent(fit, false);
                Stretch(wash.rectTransform);
                wash.sprite = ZoneSolidSprite();
                wash.color = new Color(0.04f, 0.05f, 0.06f, 0f);
                wash.enabled = false;
                wash.raycastTarget = false;
                wash.rectTransform.localScale = Vector3.one;
                var shade = new GameObject("TheatreShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                shade.transform.SetParent(fit, false);
                Stretch(shade.rectTransform);
                shade.sprite = ZoneVignetteSprite();
                shade.color = new Color(0.02f, 0.03f, 0.04f, 0f);
                shade.enabled = false;
                shade.raycastTarget = false;
                shade.rectTransform.localScale = Vector3.one;
            }

            LogTheatreCover(plate, plateWidth, plateHeight, viewport, reserveRect, theatrePaint, fa);

            if (map != null)
                map.enabled = painted == null;

            Rect safe = Rect.MinMaxRect(
                TheatreSafeLeft,
                TheatreSafeTop,
                Mathf.Max(TheatreSafeLeft + ZoneCardWidth, plateWidth - TheatreSafeRight),
                Mathf.Max(TheatreSafeTop + ZoneCardHeight, plateHeight - TheatreSafeBottom));
            Rect detail = SeatingDetailRect(plateWidth, plateHeight, fa);
            if (reserve
                && TryCopyTheatreDetailRect(out Rect placed)
                && placed.width > 1f
                && placed.width < plateWidth * 0.5f
                && placed.height > plateHeight * 0.45f)
                detail = placed;
            SeatingReserveRect = detail;
            safe = ShrinkSeatingForDetail(safe, plateWidth, detail);
            var slots = new List<TheatreZoneSlot>(SkirmishBattleLibraryArt.MapIds.Length);
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                string mapId = SkirmishBattleLibraryArt.MapIds[i];
                if (!TryRegion(mapId, out _, out Vector2 centre))
                    continue;
                bool selected = string.Equals(mapId, theater.SelectedMapId, StringComparison.OrdinalIgnoreCase);
                int won = 0;
                int playable = 0;
                if (theater.Pins != null)
                {
                    for (int pinIndex = 0; pinIndex < theater.Pins.Length; pinIndex++)
                    {
                        if (!string.Equals(theater.Pins[pinIndex].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                            continue;
                        won = theater.Pins[pinIndex].WonCount;
                        playable = theater.Pins[pinIndex].PublishedCount;
                        break;
                    }
                }

                // Screen-space hex, so the 60° sides survive the cover crop. The painting polygons were chamfered boxes.
                Vector2[] polygon = DistrictHex(mapId, plateWidth, plateHeight);
                if (polygon.Length < 6 || PolygonArea(polygon) < 1200f)
                    polygon = VisibleDistrictHex(MapPaintingPoint(centre, cropU, cropV, cropW, cropH, plateWidth, plateHeight), plateWidth, plateHeight);
                Vector2 cardAnchor = i < SkirmishBattleLibraryArt.TheaterCardAnchors.Length
                    ? SkirmishBattleLibraryArt.TheaterCardAnchors[i]
                    : centre;
                slots.Add(new TheatreZoneSlot
                {
                    MapId = mapId,
                    Zone = RegionZoneColor(mapId),
                    Selected = selected,
                    Won = won,
                    Playable = playable,
                    Anchor = MapPaintingPoint(centre, cropU, cropV, cropW, cropH, plateWidth, plateHeight),
                    CardAnchor = MapPaintingPoint(cardAnchor, cropU, cropV, cropW, cropH, plateWidth, plateHeight),
                    Hex = polygon
                });
            }

            if (fa
                && (string.IsNullOrEmpty(TheatreSeatGeometry.ShotLocale)
                    || !TheatreSeatGeometry.ShotLocale.StartsWith("fa", StringComparison.OrdinalIgnoreCase)))
                TheatreSeatGeometry.ShotLocale = "fa";
            PlaceZoneSlots(slots, safe, plateWidth, plateHeight, fa);
            // The row slides clear of the side column. Moving the paint rect with it
            // left a black band on the uncovered side. Keep the rect on the plate and
            // shift the crop so the districts stay on the same pixels.
            if (theatrePaint != null && Mathf.Abs(TheatreSeatGeometry.ReserveSlideX) > 0.5f)
            {
                Rect uv = theatrePaint.uvRect;
                float du = TheatreSeatGeometry.ReserveSlideX / Mathf.Max(1f, plateWidth) * uv.width;
                float u = uv.x - du;
                float w = uv.width;
                if (u < 0f)
                {
                    w += u;
                    u = 0f;
                }

                if (w < 0.02f)
                    w = 0.02f;
                if (u + w > 1f)
                    w = Mathf.Max(0.02f, 1f - u);
                theatrePaint.uvRect = new Rect(u, uv.y, w, uv.height);
            }
            bool wideFrame = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
            // Shade the sand outside the regions. The bright plate stays under the fills.
            PlaceTerrainShade(fit, slots, plateWidth, plateHeight);
            for (int i = 0; i < slots.Count; i++)
                DrawZoneOutline(fit, "ZoneOutline_" + slots[i].MapId, slots[i].Hex, slots[i].Zone, slots[i].Selected);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Card.width < 1f)
                    continue;
                PlaceZonePin(fit, slots[i], fa);
                PlaceZoneCard(fit, slots[i], fa, wideFrame);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Card.width < 1f)
                    continue;
                DrawZoneLeader(fit, slots[i]);
            }

            for (int i = 0; i < slots.Count; i++)
                LogTerritory(slots[i]);

            var hits = new GameObject("TheaterHits", typeof(RectTransform)).GetComponent<RectTransform>();
            hits.SetParent(fit, false);
            Stretch(hits);
            for (int i = 0; i < slots.Count; i++)
            {
                TheatreZoneSlot slot = slots[i];
                BoundsOf(slot.Hex, 0f, out float minX, out float minY, out float maxX, out float maxY);
                var button = CreateHitButton(hits, SkirmishLibraryControlNames.TheaterPin(slot.MapId));
                PlaceTopLeft(button.GetComponent<RectTransform>(), minX, minY, Mathf.Max(48f, maxX - minX), Mathf.Max(48f, maxY - minY));
                if (button.targetGraphic is Image hit)
                    hit.color = new Color(0f, 0f, 0f, 0f);
                string captured = slot.MapId;
                TheatreDistrictPress press = button.gameObject.AddComponent<TheatreDistrictPress>();
                press.Bind(fit, captured);
                button.onClick.AddListener(() => SelectTheaterRegion(captured));
                _regionButtons.Add(button);
            }
        }

        /// <summary>
        /// Darkens the sand outside the districts. The hole follows each coast so the
        /// fill and its rim stay on the bright plate. This is not the old full-board veil.
        /// </summary>
        private static void PlaceTerrainShade(RectTransform parent, List<TheatreZoneSlot> slots, float plateWidth, float plateHeight)
        {
            if (parent == null || slots == null || slots.Count == 0 || plateWidth < 32f || plateHeight < 32f)
                return;
            const int width = 180;
            int height = Mathf.Max(48, Mathf.RoundToInt(width * plateHeight / plateWidth));
            var pixels = new Color32[width * height];
            var shapes = new Vector2[slots.Count][];
            for (int i = 0; i < slots.Count; i++)
                shapes[i] = slots[i].Hex;
            for (int y = 0; y < height; y++)
            {
                float plateY = plateHeight - (y + 0.5f) / height * plateHeight;
                for (int x = 0; x < width; x++)
                {
                    float plateX = (x + 0.5f) / width * plateWidth;
                    var point = new Vector2(plateX, plateY);
                    bool inside = false;
                    float outside = float.MaxValue;
                    for (int s = 0; s < shapes.Length; s++)
                    {
                        Vector2[] shape = shapes[s];
                        if (shape == null || shape.Length < 3)
                            continue;
                        if (PointInsidePolygon(point, shape))
                        {
                            inside = true;
                            break;
                        }

                        outside = Mathf.Min(outside, DistanceToPolygonEdge(point, shape));
                    }

                    if (inside || outside > 100000f)
                    {
                        pixels[y * width + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    // The glow stays on the bright plate. Past that rim the ground is a
                    // dark, low-saturation olive so the districts read as the board.
                    const float rimClear = 18f;
                    float t = Mathf.Clamp01((outside - rimClear) / 22f);
                    t = t * t * (3f - 2f * t);
                    float edgeX = Mathf.Min(plateX, plateWidth - plateX) / Mathf.Max(1f, plateWidth);
                    float edgeY = Mathf.Min(plateY, plateHeight - plateY) / Mathf.Max(1f, plateHeight);
                    float edge = Mathf.Clamp01(1f - Mathf.Min(edgeX, edgeY) * 6f);
                    float alpha = Mathf.Lerp(0.72f, 0.88f, Mathf.Max(t, edge * 0.85f));
                    int hash = (x * 374761393 + y * 668265263) & 255;
                    int grit = (hash % 17) - 8;
                    byte shadeAlpha = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                    pixels[y * width + x] = new Color32(
                        (byte)Mathf.Clamp(26 + grit, 0, 255),
                        (byte)Mathf.Clamp(30 + grit / 2, 0, 255),
                        (byte)Mathf.Clamp(24 + grit / 3, 0, 255),
                        shadeAlpha);
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            var shade = new GameObject("TerrainShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            shade.transform.SetParent(parent, false);
            shade.sprite = sprite;
            shade.type = Image.Type.Simple;
            shade.color = Color.white;
            shade.raycastTarget = false;
            Stretch(shade.rectTransform);
            shade.rectTransform.localScale = Vector3.one;
        }

        private static void HideTheaterLabels(RawImage map)
        {
            Transform clip = map != null ? map.transform.parent : null;
            SetActive(clip != null ? clip.Find("TheaterLabels") : null, false);
            Transform preview = clip != null ? clip.parent : null;
            SetActive(preview != null ? preview.Find("TheaterLabels") : null, false);
        }

        private static Color TheatrePaintDim()
        {
            // Full plate. Territory fills carry the district tint; a multiply here washed the painting out.
            return Color.white;
        }

        /// <summary>
        /// Lifts the theatre painting toward the approved warm desert. The name strips
        /// stay on their own dark shades, so this does not touch the 4.5 name contrast.
        /// </summary>
        private static void BrightenTheatrePlate(RawImage plate)
        {
            if (plate == null)
                return;
            Shader shader = Shader.Find("Game/UI/TheatrePlate");
            if (shader == null)
                return;
            var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            mat.SetFloat("_Gain", 1.12f);
            mat.SetFloat("_Sat", 1.26f);
            mat.SetFloat("_Lift", 0.025f);
            plate.material = mat;
        }

        /// <summary>
        /// Saturated multiply factor. Light enough that the painting's local contrast stays above the
        /// capture variance floor, dark enough that the hex is not a pale wash.
        /// </summary>
        private static Color DistrictMultiplyTint(Color zone)
        {
            float peak = Mathf.Max(zone.r, Mathf.Max(zone.g, zone.b));
            Color hue = peak > 0.01f ? zone / peak : zone;
            Color tint = Color.Lerp(hue, new Color(0.72f, 0.68f, 0.62f, 1f), 0.34f);
            float lum = tint.r * 0.299f + tint.g * 0.587f + tint.b * 0.114f;
            if (lum < 0.62f && lum > 0.01f)
                tint *= 0.62f / lum;
            tint.r = Mathf.Clamp01(tint.r);
            tint.g = Mathf.Clamp01(tint.g);
            tint.b = Mathf.Clamp01(tint.b);
            tint.a = 1f;
            return tint;
        }

        /// <summary>The district hue at full strength, with a small hot lift. Not a white-gold mix.</summary>
        private static Color DistrictNeon(Color zone, float whiteLift)
        {
            float peak = Mathf.Max(zone.r, Mathf.Max(zone.g, zone.b));
            Color hue = peak > 0.01f ? zone / peak : zone;
            Color neon = Color.Lerp(hue, Color.white, Mathf.Clamp01(whiteLift));
            neon.a = 1f;
            return neon;
        }

        private static void ApplyDistrictMultiply(GameObject host)
        {
            Graphic polygon = host != null ? host.GetComponent<Graphic>() : null;
            if (polygon == null)
                return;
            Shader shader = Shader.Find("Game/UI/DistrictMultiply");
            if (shader == null)
            {
                Debug.LogWarning("District multiply shader is missing. Theatre tints will not blend.");
                return;
            }

            Material mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            mat.SetTexture("_MainTex", Texture2D.whiteTexture);
            polygon.material = mat;
        }

        private static Texture2D LoadTheatreBase()
        {
            return SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.TheatreBaseKey);
        }

        /// <summary>Confirms the painted theatre plate is the registered base, not the hex board.</summary>
        private static void LogTheatreBase(Texture2D painted)
        {
            Debug.Log("[TheatreBase] key=" + SkirmishLibraryPreviewAssets.TheatreBaseKey
                + " drawn=" + (painted != null ? "1" : "0")
                + " name=" + (painted != null ? painted.name : "none"));
        }

        private static float TextureAspect(Texture texture)
        {
            if (texture != null && texture.width > 0 && texture.height > 0)
                return texture.width / (float)texture.height;
            return 16f / 9f;
        }

        private static void CoverWindow(
            float frameWidth,
            float frameHeight,
            float sourceAspect,
            float focalX,
            float focalY,
            out float u,
            out float v,
            out float width,
            out float height)
        {
            if (sourceAspect < 0.01f)
                sourceAspect = 16f / 9f;
            float coverHeight = TheatreSeatGeometry.CoverFrameHeight(frameWidth, frameHeight);
            float frameAspect = frameWidth > 2f && coverHeight > 2f ? frameWidth / coverHeight : sourceAspect;
            if (frameAspect >= sourceAspect)
            {
                width = 1f;
                height = sourceAspect / frameAspect;
                u = 0f;
                v = Mathf.Clamp(focalY - height * 0.5f, 0f, Mathf.Max(0f, 1f - height));
            }
            else
            {
                height = 1f;
                width = frameAspect / sourceAspect;
                u = Mathf.Clamp(focalX - width * 0.5f, 0f, Mathf.Max(0f, 1f - width));
                v = 0f;
            }
        }

        private static void ApplyTheatreCover(RawImage image, float frameWidth, float frameHeight)
        {
            if (image == null)
                return;
            if (frameWidth < 2f || frameHeight < 2f)
            {
                image.uvRect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            CoverWindow(
                frameWidth,
                frameHeight,
                TextureAspect(image.texture),
                TheatreFocalX,
                TheatreFocalY,
                out float u,
                out float v,
                out float width,
                out float height);
            image.uvRect = new Rect(u, 1f - (v + height), width, height);
        }

        private static void ApplyPlainTheatreCover(RawImage image, float frameWidth, float frameHeight)
        {
            if (image == null)
                return;
            if (frameWidth < 2f || frameHeight < 2f)
            {
                image.uvRect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            TheatreSeatGeometry.CoverCrop(frameWidth, frameHeight, true, out float u, out float v, out float width, out float height);
            image.uvRect = new Rect(u, 1f - (v + height), width, height);
        }

        private static Vector2 MapPaintingPoint(
            Vector2 normalized,
            float cropU,
            float cropV,
            float cropW,
            float cropH,
            float frameWidth,
            float frameHeight)
        {
            float coverHeight = TheatreSeatGeometry.CoverFrameHeight(frameWidth, frameHeight);
            float y = (normalized.y - cropV) / Mathf.Max(0.001f, cropH) * coverHeight;
            if (coverHeight > 2f && frameHeight > coverHeight + 0.5f)
                y *= frameHeight / coverHeight;
            return new Vector2(
                (normalized.x - cropU) / Mathf.Max(0.001f, cropW) * frameWidth,
                y);
        }

        private static Vector2[] ClipPolygonToFrame(Vector2[] points, float frameWidth, float frameHeight)
        {
            const float inset = 20f;
            if (points == null || points.Length < 3 || frameWidth < 2f || frameHeight < 2f)
                return points ?? Array.Empty<Vector2>();
            float right = Mathf.Max(inset + 8f, frameWidth - inset);
            float bottom = Mathf.Max(inset + 8f, frameHeight - inset);
            Vector2[] output = points;
            output = ClipHalfPlane(output, point => point.x >= inset, (a, b) => CrossVertical(a, b, inset));
            output = ClipHalfPlane(output, point => point.x <= right, (a, b) => CrossVertical(a, b, right));
            output = ClipHalfPlane(output, point => point.y >= inset, (a, b) => CrossHorizontal(a, b, inset));
            output = ClipHalfPlane(output, point => point.y <= bottom, (a, b) => CrossHorizontal(a, b, bottom));
            return output;
        }

        private static Vector2[] ClipHalfPlane(
            Vector2[] input,
            Func<Vector2, bool> inside,
            Func<Vector2, Vector2, Vector2> intersect)
        {
            if (input == null || input.Length == 0)
                return Array.Empty<Vector2>();
            var output = new List<Vector2>(input.Length + 2);
            Vector2 previous = input[input.Length - 1];
            bool previousInside = inside(previous);
            for (int i = 0; i < input.Length; i++)
            {
                Vector2 current = input[i];
                bool currentInside = inside(current);
                if (currentInside)
                {
                    if (!previousInside)
                        output.Add(intersect(previous, current));
                    output.Add(current);
                }
                else if (previousInside)
                    output.Add(intersect(previous, current));
                previous = current;
                previousInside = currentInside;
            }

            return output.ToArray();
        }

        private static Vector2 CrossVertical(Vector2 a, Vector2 b, float x)
        {
            float span = b.x - a.x;
            float t = Mathf.Abs(span) < 0.001f ? 0f : (x - a.x) / span;
            return new Vector2(x, a.y + (b.y - a.y) * t);
        }

        private static Vector2 CrossHorizontal(Vector2 a, Vector2 b, float y)
        {
            float span = b.y - a.y;
            float t = Mathf.Abs(span) < 0.001f ? 0f : (y - a.y) / span;
            return new Vector2(a.x + (b.x - a.x) * t, y);
        }

        private struct DistrictHexSpec
        {
            public float ReferenceWidth;
            public float CenterX;
            public float CenterY;
            public float HalfWidth;
            public float HalfHeight;
        }

        /// <summary>
        /// Flat-top hex in the map clip (y down). Half-height sets the 60° sides, so the outline
        /// stays a hexagon rather than a chamfered box. Values are the v35 capture specs.
        /// The detail panel is an overlay and does not reshape these outlines.
        /// </summary>
        private static Vector2[] DistrictHex(string mapId, float frameWidth, float frameHeight)
        {
            TheatreSeatGeometry.Vec2[] hex = TheatreSeatGeometry.DistrictHex(mapId, frameWidth, frameHeight);
            if (hex == null || hex.Length < 3)
                return Array.Empty<Vector2>();
            var points = new Vector2[hex.Length];
            for (int i = 0; i < hex.Length; i++)
                points[i] = new Vector2(hex[i].X, hex[i].Y);
            return points;
        }

        private static DistrictHexSpec LandscapeDistrictHex(string mapId)
        {
            switch (mapId)
            {
                case "CC": return HexSpec(1294f, 990f, 140f, 230f, 115f);
                case "MP": return HexSpec(1294f, 300f, 390f, 200f, 115f);
                case "IB": return HexSpec(1294f, 1020f, 385f, 190f, 110f);
                case "AP": return HexSpec(1294f, 665f, 466f, 156f, 78f);
                default: return HexSpec(1294f, 330f, 145f, 250f, 120f);
            }
        }

        private static DistrictHexSpec WideDistrictHex(string mapId)
        {
            switch (mapId)
            {
                case "CC": return HexSpec(1774f, 1340f, 146f, 250f, 110f);
                case "MP": return HexSpec(1774f, 420f, 400f, 210f, 110f);
                case "IB": return HexSpec(1774f, 1320f, 400f, 210f, 110f);
                case "AP": return HexSpec(1774f, 900f, 482f, 180f, 62f);
                default: return HexSpec(1774f, 460f, 146f, 250f, 110f);
            }
        }

        private static DistrictHexSpec SquareDistrictHex(string mapId)
        {
            switch (mapId)
            {
                case "CC": return HexSpec(1039.6f, 800f, 130f, 200f, 105f);
                case "MP": return HexSpec(1039.6f, 200f, 390f, 170f, 100f);
                case "IB": return HexSpec(1039.6f, 840f, 390f, 165f, 100f);
                case "AP": return HexSpec(1039.6f, 520f, 430f, 155f, 100f);
                default: return HexSpec(1039.6f, 300f, 140f, 270f, 115f);
            }
        }

        private static DistrictHexSpec HexSpec(float referenceWidth, float centerX, float centerY, float halfWidth, float halfHeight)
        {
            return new DistrictHexSpec
            {
                ReferenceWidth = referenceWidth,
                CenterX = centerX,
                CenterY = centerY,
                HalfWidth = halfWidth,
                HalfHeight = halfHeight
            };
        }

        private static Vector2[] FlatHex(float centerX, float centerY, float halfWidth, float halfHeight)
        {
            float dx = halfHeight / 1.7320508f;
            return new[]
            {
                new Vector2(centerX - halfWidth + dx, centerY - halfHeight),
                new Vector2(centerX + halfWidth - dx, centerY - halfHeight),
                new Vector2(centerX + halfWidth, centerY),
                new Vector2(centerX + halfWidth - dx, centerY + halfHeight),
                new Vector2(centerX - halfWidth + dx, centerY + halfHeight),
                new Vector2(centerX - halfWidth, centerY)
            };
        }

        private static void SeatCardsInsideDistricts(List<TheatreZoneSlot> slots, Rect safe)
        {
            if (slots == null)
                return;
            float frameWidth = safe.xMax + TheatreSafeRight;
            float frameHeight = safe.yMax + TheatreSafeBottom;
            bool square = frameHeight > 2f && frameWidth / frameHeight < 1.9f;
            var placed = new bool[slots.Count];
            int[] order = { 3, 2, 1, 0, 4 };
            for (int n = 0; n < order.Length; n++)
            {
                int index = order[n];
                if (index < 0 || index >= slots.Count || slots[index].Hex == null)
                    continue;
                TheatreZoneSlot slot = slots[index];
                Rect best = slot.Card;
                float bestScore = float.MaxValue;
                bool found = false;
                float yMax = safe.yMax - slot.Card.height;
                float xMax = safe.xMax - slot.Card.width;
                for (float y = safe.yMin; y <= yMax + 0.1f; y += 4f)
                {
                    for (float x = safe.xMin; x <= xMax + 0.1f; x += 4f)
                    {
                        var cand = new Rect(x, y, slot.Card.width, slot.Card.height);
                        Vector2 centre = cand.center;
                        if (!PointInsidePolygon(centre, slot.Hex) || DistanceToPolygonEdge(centre, slot.Hex) < 4f)
                            continue;
                        if (!CardClearsPins(cand, slots) || !CardClearsSeated(cand, slots, placed))
                            continue;
                        float score = CardSeatScore(slot.MapId, centre, slot.Pin.center, square);
                        if (score >= bestScore)
                            continue;
                        bestScore = score;
                        best = cand;
                        found = true;
                    }
                }

                if (!found)
                    continue;
                slot.Card = best;
                slots[index] = slot;
                placed[index] = true;
            }
        }

        private static float CardSeatScore(string mapId, Vector2 centre, Vector2 pin, bool square)
        {
            if (mapId == "DB")
                return Mathf.Abs(centre.y - (pin.y + 145f)) + Mathf.Abs(centre.x - (pin.x + 140f));
            // Airfield Plains stays high so the lower interior of its hex remains visible.
            if (mapId == "AP")
                return centre.y + Mathf.Abs(centre.x - pin.x) * 0.3f;
            // The square frame stacks Airfield Plains above the side districts, so those cards sit low.
            // Wider frames have a side corridor, so the side cards sit in the body of the hex.
            if (square)
            {
                if (mapId == "MP")
                    return centre.x * 0.2f + (2000f - centre.y);
                if (mapId == "IB")
                    return (2000f - centre.x) * 0.2f + (2000f - centre.y);
            }
            else
            {
                float dy = centre.y - (pin.y + 100f);
                if (mapId == "MP")
                    return dy * dy * 0.01f + Mathf.Abs(centre.x - pin.x) * 0.25f;
                if (mapId == "IB")
                    return dy * dy * 0.01f + Mathf.Abs(centre.x - (pin.x + 80f)) * 0.25f;
            }

            return Mathf.Abs(centre.x - pin.x) + Mathf.Abs(centre.y - pin.y);
        }

        private static bool CardClearsPins(Rect card, List<TheatreZoneSlot> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!CueClears(card, slots[i].Pin, ZoneCardGap))
                    return false;
            }

            return true;
        }

        private static bool CardClearsSeated(Rect card, List<TheatreZoneSlot> slots, bool[] placed)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (placed == null || i >= placed.Length || !placed[i])
                    continue;
                if (CardClearance(card, slots[i].Card) + 0.01f < ZoneSeparateGap)
                    return false;
            }

            return true;
        }

        private static bool CueClears(Rect cue, Rect blocker, float margin)
        {
            float dx = AxisGap(cue.xMin, cue.xMax, blocker.xMin, blocker.xMax);
            float dy = AxisGap(cue.yMin, cue.yMax, blocker.yMin, blocker.yMax);
            return dx >= margin || dy >= margin;
        }

        private static void SeatZoneCards(List<TheatreZoneSlot> slots, Rect safe)
        {
            if (slots == null)
                return;
            int[] order = { 3, 4, 2, 0, 1 };
            var seated = new bool[slots.Count];
            for (int n = 0; n < order.Length; n++)
            {
                int index = order[n];
                if (index < 0 || index >= slots.Count)
                    continue;
                TheatreZoneSlot slot = slots[index];
                Vector2 target = slot.Card.center;
                if (ZoneCardLegal(slots, index, slot.Card, seated))
                {
                    seated[index] = true;
                    continue;
                }

                Rect best = slot.Card;
                float bestDist = float.MaxValue;
                bool found = false;
                float yMax = safe.yMax - slot.Card.height;
                float xMax = safe.xMax - slot.Card.width;
                for (float y = safe.yMin; y <= yMax + 0.1f; y += 8f)
                {
                    for (float x = safe.xMin; x <= xMax + 0.1f; x += 8f)
                    {
                        var cand = new Rect(x, y, slot.Card.width, slot.Card.height);
                        if (!ZoneCardLegal(slots, index, cand, seated))
                            continue;
                        float dx = cand.center.x - target.x;
                        float dy = cand.center.y - target.y;
                        float dist = dx * dx + dy * dy;
                        if (dist >= bestDist)
                            continue;
                        bestDist = dist;
                        best = cand;
                        found = true;
                    }
                }

                if (!found)
                    continue;
                slot.Card = best;
                slots[index] = slot;
                seated[index] = true;
            }
        }

        private static bool ZoneCardLegal(List<TheatreZoneSlot> slots, int index, Rect card, bool[] seated)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (CardsTooClose(card, slots[i].Pin))
                    return false;
                if (seated != null && i < seated.Length && seated[i] && CardClearance(card, slots[i].Card) + 0.01f < ZoneSeparateGap)
                    return false;
            }

            return index >= 0;
        }

        private static int HexHitCount(List<TheatreZoneSlot> slots)
        {
            if (slots == null)
                return 0;
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (RectHitsAnyHex(slots[i].Card, slots))
                    count++;
            }

            return count;
        }

        private static void NudgeCardsOffOwnEdges(List<TheatreZoneSlot> slots, Rect safe)
        {
            if (slots == null)
                return;
            const float margin = 4f;
            for (int i = 0; i < slots.Count; i++)
            {
                TheatreZoneSlot slot = slots[i];
                if (slot.Hex == null || !EdgeCrossesCard(slot.Hex, slot.Card))
                    continue;
                Rect current = slot.Card;
                Rect best = current;
                float bestDist = float.MaxValue;
                bool found = false;
                float xMax = safe.xMax - current.width;
                float yMax = safe.yMax - current.height;
                for (float y = safe.yMin; y <= yMax + 0.1f; y += 8f)
                {
                    for (float x = safe.xMin; x <= xMax + 0.1f; x += 8f)
                    {
                        var cand = new Rect(x, y, current.width, current.height);
                        if (EdgeCrossesCard(slot.Hex, cand) || CardBoundaryGap(slot.Hex, cand) < margin)
                            continue;
                        bool blocked = false;
                        for (int j = 0; j < slots.Count; j++)
                        {
                            if (j != i && CardClearance(cand, slots[j].Card) + 0.01f < ZoneSeparateGap)
                            {
                                blocked = true;
                                break;
                            }
                        }

                        if (blocked)
                            continue;
                        float dx = cand.center.x - current.center.x;
                        float dy = cand.center.y - current.center.y;
                        float dist = dx * dx + dy * dy;
                        if (!CardInsidePolygon(cand, slot.Hex))
                            dist += 10000000f;
                        if (dist >= bestDist)
                            continue;
                        bestDist = dist;
                        best = cand;
                        found = true;
                    }
                }

                if (!found)
                    continue;
                slot.Card = best;
                slots[i] = slot;
            }
        }

        private static bool EdgeCrossesCard(Vector2[] poly, Rect card)
        {
            if (poly == null)
                return false;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Length];
                if (SegmentHitsRect(a, b, card))
                    return true;
            }

            return false;
        }

        private static bool SegmentHitsRect(Vector2 a, Vector2 b, Rect rect)
        {
            if (PointInRect(a, rect) || PointInRect(b, rect))
                return true;
            Vector2 c0 = new Vector2(rect.xMin, rect.yMin);
            Vector2 c1 = new Vector2(rect.xMax, rect.yMin);
            Vector2 c2 = new Vector2(rect.xMax, rect.yMax);
            Vector2 c3 = new Vector2(rect.xMin, rect.yMax);
            return SegmentsCross(a, b, c0, c1) || SegmentsCross(a, b, c1, c2)
                || SegmentsCross(a, b, c2, c3) || SegmentsCross(a, b, c3, c0);
        }

        private static bool PointInRect(Vector2 point, Rect rect)
        {
            return point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax;
        }

        private static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float ab = Orient(a, b, c);
            float ab2 = Orient(a, b, d);
            float cd = Orient(c, d, a);
            float cd2 = Orient(c, d, b);
            return ab * ab2 < 0f && cd * cd2 < 0f;
        }

        private static float Orient(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        private static float CardBoundaryGap(Vector2[] poly, Rect card)
        {
            if (EdgeCrossesCard(poly, card))
                return 0f;
            float best = float.MaxValue;
            Vector2[] corners =
            {
                new Vector2(card.xMin, card.yMin),
                new Vector2(card.xMax, card.yMin),
                new Vector2(card.xMax, card.yMax),
                new Vector2(card.xMin, card.yMax)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % corners.Length];
                for (int step = 0; step <= 4; step++)
                {
                    float t = step / 4f;
                    var point = new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
                    best = Mathf.Min(best, DistanceToPolygonEdge(point, poly));
                }
            }

            return best;
        }

        private static bool CardInsidePolygon(Rect card, Vector2[] poly)
        {
            Vector2[] corners =
            {
                new Vector2(card.xMin, card.yMin),
                new Vector2(card.xMax, card.yMin),
                new Vector2(card.xMax, card.yMax),
                new Vector2(card.xMin, card.yMax)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                if (!PointInsidePolygon(corners[i], poly))
                    return false;
            }

            return !EdgeCrossesCard(poly, card);
        }

        private static bool PointInsidePolygon(Vector2 point, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[j];
                if ((a.y > point.y) == (b.y > point.y))
                    continue;
                float x = (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (point.x < x)
                    inside = !inside;
            }

            return inside;
        }

        private static bool RectHitsAnyHex(Rect rect, List<TheatreZoneSlot> slots)
        {
            Rect shrunk = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            if (shrunk.width <= 0f || shrunk.height <= 0f)
                return true;
            for (int i = 0; i < slots.Count; i++)
            {
                if (RectHitsPolygon(shrunk, slots[i].Hex))
                    return true;
            }

            return false;
        }

        private static bool RectHitsPolygon(Rect rect, Vector2[] poly)
        {
            if (poly == null || poly.Length < 3)
                return false;
            Vector2[] corners =
            {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMin, rect.yMax)
            };
            if (SeparatedOnAxis(corners, poly, new Vector2(1f, 0f)) || SeparatedOnAxis(corners, poly, new Vector2(0f, 1f)))
                return false;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Length];
                Vector2 edge = b - a;
                float length = edge.magnitude;
                if (length < 0.001f)
                    continue;
                var axis = new Vector2(-edge.y / length, edge.x / length);
                if (SeparatedOnAxis(corners, poly, axis))
                    return false;
            }

            return true;
        }

        private static bool SeparatedOnAxis(Vector2[] corners, Vector2[] poly, Vector2 axis)
        {
            float rMin = float.MaxValue;
            float rMax = float.MinValue;
            for (int i = 0; i < corners.Length; i++)
            {
                float dot = Vector2.Dot(corners[i], axis);
                rMin = Mathf.Min(rMin, dot);
                rMax = Mathf.Max(rMax, dot);
            }

            float pMin = float.MaxValue;
            float pMax = float.MinValue;
            for (int i = 0; i < poly.Length; i++)
            {
                float dot = Vector2.Dot(poly[i], axis);
                pMin = Mathf.Min(pMin, dot);
                pMax = Mathf.Max(pMax, dot);
            }

            return rMax <= pMin || pMax <= rMin;
        }

        private static void NoteHostCanvas(RectTransform plate)
        {
            if (TheatreSeatGeometry.ShotCanvasHeight > 2f)
                return;
            Canvas canvas = plate != null ? plate.GetComponentInParent<Canvas>() : null;
            RectTransform root = canvas != null && canvas.rootCanvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;
            if (root == null || root.rect.height < 2f)
                return;
            TheatreSeatGeometry.ShotCanvasWidth = root.rect.width;
            TheatreSeatGeometry.ShotCanvasHeight = root.rect.height;
            if (string.IsNullOrEmpty(TheatreSeatGeometry.ShotRatio)
                || string.Equals(TheatreSeatGeometry.ShotRatio, "unset", StringComparison.Ordinal))
            {
                TheatreSeatGeometry.ShotRatio = TheatreSeatGeometry.CanvasIsFourByThree(root.rect.width, root.rect.height)
                    ? "4:3"
                    : "16:9";
            }
        }

        private static void PlaceZoneSlots(List<TheatreZoneSlot> slots, Rect safe, float plateWidth, float plateHeight, bool fa)
        {
            if (slots == null)
                return;
            TryApplyArrangedSeats(slots, plateWidth, plateHeight, fa);
            if (fa)
                ReserveFarsiCueShelf(slots, plateWidth, plateHeight);
            var seated = new TheatreSeatGeometry.Vec2[slots.Count][];
            for (int i = 0; i < slots.Count; i++)
            {
                Vector2[] hex = slots[i].Hex;
                if (hex == null || hex.Length < 3)
                    continue;
                var shape = new TheatreSeatGeometry.Vec2[hex.Length];
                for (int p = 0; p < hex.Length; p++)
                    shape[p] = new TheatreSeatGeometry.Vec2(hex[p].x, hex[p].y);
                seated[i] = shape;
            }

            Rect detailRect = SeatingDetailRect(plateWidth, plateHeight, fa);
            var detail = new TheatreSeatGeometry.Box(detailRect.x, detailRect.y, detailRect.width, detailRect.height);
            TheatreSeatGeometry.Box hint = TheatreSeatGeometry.ZoomHintRect(plateWidth, plateHeight);
            for (int i = 0; i < slots.Count; i++)
            {
                TheatreZoneSlot slot = slots[i];
                slot.Hex = AsTerritory(slot.Hex, slot.Card, slot.Pin, slot.Cue, slot.MapId, seated, i, fa, plateWidth, plateHeight, detail, hint);
                slots[i] = slot;
            }
        }

        /// <summary>
        /// Farsi at 16:9 was keeping a 160×36 tab, so بزرگنمایی clipped at the floor.
        /// Hang a 160×56 tab fully above the selected card before the coastline is drawn.
        /// When the plate edge is in the way, the card and pin drop together.
        /// </summary>
        private static void ReserveFarsiCueShelf(List<TheatreZoneSlot> slots, float plateWidth, float plateHeight)
        {
            if (slots == null || plateHeight < 32f)
                return;
            int selected = -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].Selected)
                    continue;
                selected = i;
                break;
            }

            if (selected < 0)
                return;
            TheatreZoneSlot slot = slots[selected];
            if (slot.Card.width < 48f || slot.Card.height < 48f)
                return;
            const float height = 56f;
            // بزرگنمایی at 22px prefers about 202px. A 160px shelf clips that line.
            float width = Mathf.Max(206f, slot.Card.width);
            Rect card = slot.Card;
            Rect seated = slot.Cue;
            bool insideMap = seated.xMin >= 12f && seated.yMin >= 12f
                && seated.xMax <= plateWidth - 12f && seated.yMax <= plateHeight - 12f;
            // The arranged pill is already off every hex. Replacing it with a shelf
            // above the card is what put the Farsi tab back on Industrial Basin.
            if (seated.width + 0.5f >= width && seated.height + 0.5f >= height
                && insideMap && !ShelfHitsHex(slots, selected, seated))
                return;
            if (!TryFaShelf(slots, selected, card, seated, plateWidth, plateHeight, width, height, out Rect shelf))
                return;
            for (int i = 0; i < slots.Count; i++)
            {
                TheatreZoneSlot each = slots[i];
                if (each.Cue.width > 1f || i == selected)
                    each.Cue = shelf;
                slots[i] = each;
            }
        }

        /// <summary>
        /// A 206×56 Farsi tab that misses every district stroke. The card stays put
        /// so the portrait keeps the region centre the seater just locked.
        /// </summary>
        private static bool TryFaShelf(
            List<TheatreZoneSlot> slots,
            int selected,
            Rect card,
            Rect seated,
            float plateWidth,
            float plateHeight,
            float width,
            float height,
            out Rect spot)
        {
            spot = default;
            float[] xs =
            {
                seated.width > 8f ? seated.x : card.x,
                card.x,
                card.xMax - width,
                card.xMax + 8f,
                card.xMin - 8f - width,
                card.x + (card.width - width) * 0.5f
            };
            float[] ys =
            {
                seated.height > 8f ? seated.y : card.y - height,
                card.y - height,
                card.yMax + 8f,
                card.y,
                card.y + (card.height - height) * 0.5f
            };
            for (int y = 0; y < ys.Length; y++)
            {
                for (int x = 0; x < xs.Length; x++)
                {
                    float cx = Mathf.Clamp(xs[x], 12f, Mathf.Max(12f, plateWidth - 12f - width));
                    float cy = Mathf.Clamp(ys[y], 12f, Mathf.Max(12f, plateHeight - 12f - height));
                    var trial = new Rect(cx, cy, width, height);
                    if (trial.xMin < 12f || trial.yMin < 12f || trial.xMax > plateWidth - 12f || trial.yMax > plateHeight - 12f)
                        continue;
                    if (ShelfHitsHex(slots, selected, trial))
                        continue;
                    spot = trial;
                    return true;
                }
            }

            return false;
        }

        private static bool ShelfHitsHex(List<TheatreZoneSlot> slots, int selected, Rect spot)
        {
            if (slots == null)
                return false;
            Rect inflated = InflateRect(spot, TheatreSeatGeometry.CueHexStroke);
            for (int i = 0; i < slots.Count; i++)
            {
                Vector2[] shape = slots[i].Hex;
                if (shape == null || shape.Length < 3)
                    continue;
                bool hits = i == selected ? EdgesHitRect(shape, inflated) : RectHitsPolygon(inflated, shape);
                if (hits)
                    return true;
            }

            return false;
        }

        private static bool ShelfClearOfCards(List<TheatreZoneSlot> slots, int selected, Rect card)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (i == selected || slots[i].Card.width < 1f)
                    continue;
                if (CardsTooClose(card, slots[i].Card))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Seated district turned into an irregular territory. The card rect is unchanged.
        /// </summary>
        private static Vector2[] AsTerritory(
            Vector2[] hex,
            Rect card,
            Rect pin,
            Rect cue,
            string mapId,
            TheatreSeatGeometry.Vec2[][] seated,
            int index,
            bool fa,
            float plateWidth,
            float plateHeight,
            TheatreSeatGeometry.Box detail,
            TheatreSeatGeometry.Box hint)
        {
            if (hex == null || hex.Length < 3)
                return hex;
            var shape = new TheatreSeatGeometry.Vec2[hex.Length];
            for (int i = 0; i < hex.Length; i++)
                shape[i] = new TheatreSeatGeometry.Vec2(hex[i].x, hex[i].y);
            TheatreSeatGeometry.Vec2[][] others = null;
            if (seated != null)
            {
                others = new TheatreSeatGeometry.Vec2[seated.Length][];
                for (int i = 0; i < seated.Length; i++)
                {
                    if (i == index)
                        continue;
                    others[i] = seated[i];
                }
            }

            var box = new TheatreSeatGeometry.Box(card.x, card.y, card.width, card.height);
            var pinBox = new TheatreSeatGeometry.Box(pin.x, pin.y, pin.width, pin.height);
            var pill = new TheatreSeatGeometry.Box(cue.x, cue.y, cue.width, cue.height);
            if (fa && pill.W > 1f)
            {
                // First width TryWidenLiveFaCue places. بزرگنمایی at 22px needs this shelf.
                // 76 is two line boxes at the 22px floor with the leading still at 0.
                // The extra width hangs inward so the 26px stroke stays inside the halo.
                const float needW = 220f;
                const float needH = 76f;
                float width = Mathf.Max(pill.W, needW);
                float height = Mathf.Max(pill.H, needH);
                float bottom = pill.Y + pill.H;
                float x = pill.XMax - width;
                if (x > pill.X)
                    x = pill.X;
                float y = bottom - height;
                float limitL = 1f + TheatreSeatGeometry.HaloReach + TheatreSeatGeometry.CueHexStroke + 2f;
                float limitR = plateWidth - limitL;
                if (x + width > limitR)
                    x = limitR - width;
                if (x < limitL)
                    x = limitL;
                if (y < limitL)
                    y = limitL;
                if (x < 12f)
                    x = 12f;
                if (x + width > plateWidth - 12f)
                    x = plateWidth - 12f - width;
                if (y < 12f)
                    y = 12f;
                pill = new TheatreSeatGeometry.Box(x, y, width, height);
            }

            TheatreSeatGeometry.Vec2[] terrain = TheatreSeatGeometry.FinishTerritory(
                shape, box, pinBox, mapId, others, pill, plateWidth, plateHeight, detail, hint);
            if (terrain == null || terrain.Length < 3)
                return hex;
            var points = new Vector2[terrain.Length];
            for (int i = 0; i < terrain.Length; i++)
                points[i] = new Vector2(terrain[i].X, terrain[i].Y);
            return points;
        }

        private static void LogTerritory(TheatreZoneSlot slot)
        {
            if (slot.Hex == null || slot.Hex.Length < 3)
                return;
            var shape = new TheatreSeatGeometry.Vec2[slot.Hex.Length];
            for (int i = 0; i < slot.Hex.Length; i++)
                shape[i] = new TheatreSeatGeometry.Vec2(slot.Hex[i].x, slot.Hex[i].y);
            var card = new TheatreSeatGeometry.Box(slot.Card.x, slot.Card.y, slot.Card.width, slot.Card.height);
            bool inside = slot.Card.width >= 1f && TheatreSeatGeometry.CardInsideTerritory(card, shape);
            Debug.Log("[Territory] map=" + slot.MapId
                + " vertices=" + slot.Hex.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " alpha=" + TheatreSeatGeometry.TerritoryBorderAlpha.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " fill=" + TheatreSeatGeometry.TerritoryFillAlpha.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " feather=" + TheatreSeatGeometry.TerritoryFeather.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                + " inside=" + (inside ? "1" : "0"));
        }

        /// <summary>Width reserved on the trailing edge. Matches the detail panel when the plate is wide enough.</summary>
        private static float SeatingDetailReserve(float plateWidth)
        {
            if (plateWidth < 600f)
                return 0f;
            return Mathf.Clamp(plateWidth * 0.22f, 230f, 290f);
        }

        /// <summary>
        /// Pulls the safe rect off the side occupied by <paramref name="detail"/>.
        /// The side is the rect's, including P1's TheatreDetailRect when that panel has already reported one.
        /// </summary>
        private static Rect ShrinkSeatingForDetail(Rect safe, float plateWidth, Rect detail)
        {
            if (detail.width <= 1f)
                return safe;
            bool onLeft = TheatreSeatGeometry.DetailOccupiesLeft(plateWidth, detail.x, detail.width);
            float column = onLeft
                ? detail.xMax + Mathf.Max(0f, detail.xMin)
                : (plateWidth - detail.xMin) + Mathf.Max(0f, plateWidth - detail.xMax);
            if (column <= 1f)
                return safe;
            float fullRight = Mathf.Max(TheatreSafeLeft + ZoneCardWidth, plateWidth - TheatreSafeRight);
            if (onLeft)
            {
                if (safe.xMin > TheatreSafeLeft + 1f)
                    return safe;
                float xMin = Mathf.Min(safe.xMax - 40f, safe.xMin + column);
                return Rect.MinMaxRect(xMin, safe.yMin, safe.xMax, safe.yMax);
            }

            if (safe.xMax < fullRight - 1f)
                return safe;
            float xMax = Mathf.Max(safe.xMin + 40f, safe.xMax - column);
            return Rect.MinMaxRect(safe.xMin, safe.yMin, xMax, safe.yMax);
        }

        /// <summary>
        /// Reads P1's <c>TheatreDetailRect</c> when that partial is compiled in. Absent on this branch until rebase.
        /// </summary>
        public bool TryCopyTheatreDetailRect(out Rect rect)
        {
            rect = default;
            PropertyInfo property = GetType().GetProperty(
                "TheatreDetailRect",
                BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.PropertyType != typeof(Rect) || property.GetMethod == null)
                return false;
            if (property.GetValue(this) is not Rect placed)
                return false;
            rect = placed;
            return true;
        }

        private static Rect SeatingDetailRect(float plateWidth, float plateHeight, bool fa)
        {
            if (!TheatreSeatGeometry.DetailReserveOn(
                plateWidth, plateHeight, fa, plateWidth >= 600f, TheatreSeatGeometry.ShotIsFourByThree(), TheatreSeatGeometry.ShotSelected))
                return Rect.zero;
            float reserve = SeatingDetailReserve(plateWidth);
            if (reserve <= 0f)
                return Rect.zero;
            const float margin = 10f;
            float width = reserve - margin * 2f;
            float height = Mathf.Max(200f, plateHeight - margin * 2f);
            float x = fa ? margin : plateWidth - reserve + margin;
            return new Rect(x, margin, width, height);
        }

        private const float ZoneLeaderCapReference = 180f;

        /// <summary>
        /// Keeps a v35 320x110 seat when it misses the detail panel and does not straddle.
        /// A card that meets the panel, or that cannot rest inside its hex or clear of every hex,
        /// moves to the nearest legal spot by its own pin. Only that card may shrink toward 280x96.
        /// </summary>
        private static void SeatCardsClearOfDetail(List<TheatreZoneSlot> slots, Rect safe, float plateWidth, float plateHeight, bool fa)
        {
            if (slots == null)
                return;
            Rect detail = SeatingDetailRect(plateWidth, plateHeight, fa);
            float frameWidth = safe.xMax + TheatreSafeRight;
            float frameHeight = safe.yMax + TheatreSafeBottom;
            float cap = ZoneLeaderCap(plateWidth, plateHeight);
            var held = new bool[slots.Count];
            var move = new bool[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                bool hitsDetail = detail.width > 1f && Overlaps(slots[i].Card, detail, 0f);
                bool holds = ZoneCardSeatHolds(slots[i].Card, slots[i].MapId, frameWidth, frameHeight);
                move[i] = hitsDetail || !holds;
                held[i] = !move[i];
            }

            // Smaller districts first, so a wide neighbour does not take the only pocket.
            int[] order = { 4, 3, 2, 1, 0 };
            for (int n = 0; n < order.Length; n++)
            {
                int i = order[n];
                if (i < 0 || i >= slots.Count || !move[i])
                    continue;
                if (!TryReseatCard(slots, i, detail, frameWidth, frameHeight, fa, cap, held, out Rect seated)
                    && !TryReseatCard(slots, i, detail, frameWidth, frameHeight, fa, cap * 1.75f, held, out seated)
                    && !TryReseatCard(slots, i, detail, frameWidth, frameHeight, fa, 4000f, held, out seated))
                    continue;
                TheatreZoneSlot slot = slots[i];
                slot.Card = seated;
                slots[i] = slot;
                held[i] = true;
            }
        }

        private static float ZoneLeaderCap(float plateWidth, float plateHeight)
        {
            float aspect = plateHeight > 2f ? plateWidth / plateHeight : 1.6f;
            float reference = aspect >= 2.4f ? 1774f : aspect >= 1.9f ? 1294f : 1039.6f;
            float scaleX = reference > 2f ? plateWidth / reference : 1f;
            float scaleY = plateHeight > 2f ? plateHeight / 620f : 1f;
            return ZoneLeaderCapReference * Mathf.Max(1f, scaleX, scaleY);
        }

        private static bool TryReseatCard(
            List<TheatreZoneSlot> slots,
            int index,
            Rect detail,
            float frameWidth,
            float frameHeight,
            bool fa,
            float leaderCap,
            bool[] held,
            out Rect seated)
        {
            TheatreZoneSlot slot = slots[index];
            seated = slot.Card;
            bool found = false;
            float bestScore = float.MaxValue;
            Vector2 pin = slot.Pin.center;
            // Full v35 size first. Shrink only when this card has no legal seat at the larger size.
            float[] widths = { 320f, 304f, 288f, 280f };
            float[] heights = { 110f, 104f, 100f, 96f };
            const float edge = 12f;
            for (int size = 0; size < widths.Length; size++)
            {
                float cardWidth = widths[size];
                float cardHeight = heights[size];
                float yMax = frameHeight - edge - cardHeight;
                float xMax = frameWidth - edge - cardWidth;
                for (float y = edge; y <= yMax + 0.1f; y += 8f)
                {
                    for (float x = edge; x <= xMax + 0.1f; x += 8f)
                    {
                        var cand = new Rect(x, y, cardWidth, cardHeight);
                        if (detail.width > 1f && Overlaps(cand, detail, 0f))
                            continue;
                        if (!CardClearsPins(cand, slots) || !CardClearsSeated(cand, slots, held))
                            continue;
                        if (!ZoneCardSeatHolds(cand, slot.MapId, frameWidth, frameHeight))
                            continue;
                        float leader = LeaderLength(pin, cand);
                        if (leader > leaderCap)
                            continue;
                        bool inside = CardInsidePolygon(cand, slot.Hex) && CardBoundaryGap(slot.Hex, cand) + 0.01f >= 8f;
                        // The closed side is the side the detail rect occupies.
                        bool panelOnLeft = detail.width > 1f
                            && TheatreSeatGeometry.DetailOccupiesLeft(frameWidth, detail.x, detail.width);
                        bool closedSide = detail.width > 1f
                            ? (panelOnLeft ? cand.center.x < pin.x - 24f : cand.center.x > pin.x + 24f)
                            : (fa ? cand.center.x < pin.x - 24f : cand.center.x > pin.x + 24f);
                        float score = leader + (inside ? 0f : 36f) + (closedSide ? 48f : 0f);
                        if (score >= bestScore)
                            continue;
                        bestScore = score;
                        seated = cand;
                        found = true;
                    }
                }

                if (found)
                    return true;
            }

            return false;
        }

        private static float LeaderLength(Vector2 pin, Rect card)
        {
            float x = Mathf.Clamp(pin.x, card.xMin, card.xMax);
            float y = Mathf.Clamp(pin.y, card.yMin, card.yMax);
            return Vector2.Distance(pin, new Vector2(x, y));
        }

        private static void NudgeCardsOffPins(List<TheatreZoneSlot> slots, Rect safe)
        {
            if (slots == null)
                return;
            for (int i = 0; i < slots.Count; i++)
            {
                TheatreZoneSlot slot = slots[i];
                if (!CardsTooClose(slot.Card, slot.Pin))
                    continue;
                Rect card = slot.Card;
                Rect pin = slot.Pin;
                bool pushX = AxisGap(card.xMin, card.xMax, pin.xMin, pin.xMax) >=
                    AxisGap(card.yMin, card.yMax, pin.yMin, pin.yMax);
                if (!PushCardFromRect(ref card, pin, safe, pushX) && !PushCardFromRect(ref card, pin, safe, !pushX))
                    continue;
                slot.Card = card;
                slots[i] = slot;
            }
        }

        private static bool PushCardFromRect(ref Rect card, Rect blocker, Rect safe, bool pushX)
        {
            Rect next = card;
            if (pushX)
            {
                float right = blocker.xMax + ZoneCardGap;
                float left = blocker.xMin - ZoneCardGap - card.width;
                next.x = card.center.x >= blocker.center.x ? right : left;
            }
            else
            {
                float below = blocker.yMax + ZoneCardGap;
                float above = blocker.yMin - ZoneCardGap - card.height;
                next.y = card.center.y >= blocker.center.y ? below : above;
            }

            next.x = Mathf.Clamp(next.x, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - next.width));
            next.y = Mathf.Clamp(next.y, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - next.height));
            if (CardsTooClose(next, blocker))
                return false;
            card = next;
            return true;
        }

        private static void SeparateCards(List<TheatreZoneSlot> slots, Rect safe)
        {
            if (slots == null)
                return;
            for (int pass = 0; pass < 48; pass++)
            {
                if (!TryPushWorstPair(slots, safe))
                    return;
            }
        }

        private static bool TryPushWorstPair(List<TheatreZoneSlot> slots, Rect safe)
        {
            int worstI = -1;
            int worstJ = -1;
            float worst = float.MaxValue;
            for (int i = 0; i < slots.Count; i++)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    float gap = CardClearance(slots[i].Card, slots[j].Card);
                    if (gap + 0.01f >= ZoneSeparateGap || gap >= worst)
                        continue;
                    worst = gap;
                    worstI = i;
                    worstJ = j;
                }
            }

            if (worstI < 0)
                return false;

            Rect a = slots[worstI].Card;
            Rect b = slots[worstJ].Card;
            float dx = AxisGap(a.xMin, a.xMax, b.xMin, b.xMax);
            float dy = AxisGap(a.yMin, a.yMax, b.yMin, b.yMax);
            bool primaryX = ChooseSeparationAxis(dx, dy);
            int beforeCount = FailingPairCount(slots);
            float beforeMin = MinimumClearance(slots);
            int beforePins = PinHitCount(slots);
            int beforeHex = HexHitCount(slots);
            if (TryAcceptSeparation(slots, safe, worstI, worstJ, primaryX, beforeCount, beforeMin, beforePins, beforeHex))
                return true;
            return TryAcceptSeparation(slots, safe, worstI, worstJ, !primaryX, beforeCount, beforeMin, beforePins, beforeHex);
        }

        private static bool TryAcceptSeparation(
            List<TheatreZoneSlot> slots,
            Rect safe,
            int i,
            int j,
            bool pushX,
            int beforeCount,
            float beforeMin,
            int beforePins,
            int beforeHex)
        {
            return TryCommit(slots, ApplyApart(slots, safe, i, j, pushX, 0), beforeCount, beforeMin, beforePins, beforeHex)
                || TryCommit(slots, ApplyApart(slots, safe, i, j, pushX, 1), beforeCount, beforeMin, beforePins, beforeHex)
                || TryCommit(slots, ApplyApart(slots, safe, i, j, pushX, 2), beforeCount, beforeMin, beforePins, beforeHex)
                || TryCommit(slots, ApplyCollapse(slots, safe, i, j, pushX), beforeCount, beforeMin, beforePins, beforeHex);
        }

        private static bool TryCommit(
            List<TheatreZoneSlot> slots,
            Rect[] candidate,
            int beforeCount,
            float beforeMin,
            int beforePins,
            int beforeHex)
        {
            if (candidate == null)
                return false;
            var snapshot = new Rect[slots.Count];
            for (int n = 0; n < slots.Count; n++)
                snapshot[n] = slots[n].Card;
            for (int n = 0; n < slots.Count; n++)
            {
                TheatreZoneSlot slot = slots[n];
                slot.Card = candidate[n];
                slots[n] = slot;
            }

            int afterCount = FailingPairCount(slots);
            float afterMin = MinimumClearance(slots);
            bool improved = afterCount < beforeCount || (afterCount == beforeCount && afterMin > beforeMin + 0.05f);
            if (improved && PinHitCount(slots) <= beforePins && HexHitCount(slots) <= beforeHex)
                return true;
            for (int n = 0; n < slots.Count; n++)
            {
                TheatreZoneSlot restore = slots[n];
                restore.Card = snapshot[n];
                slots[n] = restore;
            }

            return false;
        }

        private static Rect[] ApplyApart(List<TheatreZoneSlot> slots, Rect safe, int i, int j, bool pushX, int mode)
        {
            Rect a = slots[i].Card;
            Rect b = slots[j].Card;
            float gap = pushX
                ? AxisGap(a.xMin, a.xMax, b.xMin, b.xMax)
                : AxisGap(a.yMin, a.yMax, b.yMin, b.yMax);
            if (gap >= ZoneSeparateGap - 0.01f)
                return null;
            float deficit = ZoneSeparateGap - gap;
            int lower = pushX
                ? (a.center.x <= b.center.x ? i : j)
                : (a.center.y <= b.center.y ? i : j);
            int upper = lower == i ? j : i;
            Rect low = slots[lower].Card;
            Rect up = slots[upper].Card;
            float roomLow = pushX ? low.x - safe.xMin : low.y - safe.yMin;
            float roomUp = pushX ? safe.xMax - (up.x + up.width) : safe.yMax - (up.y + up.height);
            roomLow = Mathf.Max(0f, roomLow);
            roomUp = Mathf.Max(0f, roomUp);
            float takeLow;
            float takeUp;
            if (mode == 1)
            {
                takeLow = Mathf.Min(roomLow, deficit);
                takeUp = 0f;
            }
            else if (mode == 2)
            {
                takeLow = 0f;
                takeUp = Mathf.Min(roomUp, deficit);
            }
            else
            {
                takeLow = Mathf.Min(roomLow, deficit);
                takeUp = Mathf.Min(roomUp, deficit - takeLow);
            }

            if (takeLow + takeUp + 0.01f < deficit)
                return null;
            if (pushX)
            {
                low.x -= takeLow;
                up.x += takeUp;
            }
            else
            {
                low.y -= takeLow;
                up.y += takeUp;
            }

            return CopyCards(slots, lower, low, upper, up);
        }

        private static Rect[] ApplyCollapse(List<TheatreZoneSlot> slots, Rect safe, int i, int j, bool pushX)
        {
            Rect a = slots[i].Card;
            Rect b = slots[j].Card;
            float dx = AxisGap(a.xMin, a.xMax, b.xMin, b.xMax);
            float dy = AxisGap(a.yMin, a.yMax, b.yMin, b.yMax);
            float gap = pushX ? dx : dy;
            float other = pushX ? dy : dx;
            if (gap < 0f || other < ZoneSeparateGap - 0.01f)
                return null;
            float need = gap + 1f;
            int lower = pushX
                ? (a.center.x <= b.center.x ? i : j)
                : (a.center.y <= b.center.y ? i : j);
            int upper = lower == i ? j : i;
            Rect low = slots[lower].Card;
            Rect up = slots[upper].Card;
            float roomLow = pushX ? safe.xMax - (low.x + low.width) : safe.yMax - (low.y + low.height);
            float roomUp = pushX ? up.x - safe.xMin : up.y - safe.yMin;
            roomLow = Mathf.Max(0f, roomLow);
            roomUp = Mathf.Max(0f, roomUp);
            float takeUp = Mathf.Min(roomUp, need);
            float takeLow = Mathf.Min(roomLow, need - takeUp);
            if (takeLow + takeUp + 0.01f < need)
                return null;
            if (pushX)
            {
                low.x += takeLow;
                up.x -= takeUp;
            }
            else
            {
                low.y += takeLow;
                up.y -= takeUp;
            }

            return CopyCards(slots, lower, low, upper, up);
        }

        private static Rect[] CopyCards(List<TheatreZoneSlot> slots, int lower, Rect low, int upper, Rect up)
        {
            var next = new Rect[slots.Count];
            for (int n = 0; n < slots.Count; n++)
                next[n] = slots[n].Card;
            next[lower] = low;
            next[upper] = up;
            return next;
        }

        private static bool ChooseSeparationAxis(float dx, float dy)
        {
            bool xSeparated = dx >= 0f;
            bool ySeparated = dy >= 0f;
            if (xSeparated && ySeparated)
                return dx <= dy;
            if (xSeparated)
                return true;
            if (ySeparated)
                return false;
            return dx >= dy;
        }

        private static int FailingPairCount(List<TheatreZoneSlot> slots)
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (CardClearance(slots[i].Card, slots[j].Card) + 0.01f < ZoneSeparateGap)
                        count++;
                }
            }

            return count;
        }

        private static float MinimumClearance(List<TheatreZoneSlot> slots)
        {
            float worst = float.MaxValue;
            for (int i = 0; i < slots.Count; i++)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    float gap = CardClearance(slots[i].Card, slots[j].Card);
                    if (gap < worst)
                        worst = gap;
                }
            }

            return worst;
        }

        private static int PinHitCount(List<TheatreZoneSlot> slots)
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (CardsTooClose(slots[i].Card, slots[i].Pin))
                    count++;
            }

            return count;
        }

        private static float CardClearance(Rect a, Rect b)
        {
            float dx = ZoneSeparatingGap(a.xMin, a.xMax, b.xMin, b.xMax);
            float dy = ZoneSeparatingGap(a.yMin, a.yMax, b.yMin, b.yMax);
            if (dx >= 0f && dy >= 0f)
                return Mathf.Min(dx, dy);
            return dx >= 0f ? dx : dy;
        }

        private static float ZoneSeparatingGap(float aMin, float aMax, float bMin, float bMax)
        {
            if (aMax <= bMin)
                return bMin - aMax;
            if (bMax <= aMin)
                return aMin - bMax;
            return -1f;
        }

        private static bool CardsTooClose(Rect a, Rect b)
        {
            float dx = AxisGap(a.xMin, a.xMax, b.xMin, b.xMax);
            float dy = AxisGap(a.yMin, a.yMax, b.yMin, b.yMax);
            return dx < ZoneCardGap && dy < ZoneCardGap;
        }

        private static float AxisGap(float aMin, float aMax, float bMin, float bMax)
        {
            if (aMax <= bMin)
                return bMin - aMax;
            if (bMax <= aMin)
                return aMin - bMax;
            return -Mathf.Min(aMax - bMin, bMax - aMin);
        }

        private static float PolygonArea(Vector2[] points)
        {
            if (points == null || points.Length < 3)
                return 0f;
            float sum = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Length];
                sum += a.x * b.y - b.x * a.y;
            }

            return Mathf.Abs(sum) * 0.5f;
        }

        private static Vector2[] VisibleDistrictHex(Vector2 centre, float frameWidth, float frameHeight)
        {
            const float radius = 78f;
            float cx = Mathf.Clamp(centre.x, ZoneFrameInset + radius, Mathf.Max(ZoneFrameInset + radius, frameWidth - ZoneFrameInset - radius));
            float cy = Mathf.Clamp(centre.y, ZoneFrameInset + radius, Mathf.Max(ZoneFrameInset + radius, frameHeight - ZoneFrameInset - radius));
            var hex = new Vector2[6];
            for (int i = 0; i < hex.Length; i++)
            {
                float angle = (60f * i - 90f) * Mathf.Deg2Rad;
                hex[i] = new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius * 0.82f);
            }

            return hex;
        }

        private static void NudgeCues(List<TheatreZoneSlot> slots, Rect safe)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].Selected)
                    continue;
                for (int pass = 0; pass < 6; pass++)
                {
                    TheatreZoneSlot slot = slots[i];
                    Rect cue = slot.Cue;
                    bool hit = false;
                    for (int j = 0; j < slots.Count; j++)
                    {
                        Rect card = slots[j].Card;
                        if (!Overlaps(cue, card, 2f))
                            continue;
                        hit = true;
                        cue.y = cue.center.y <= card.center.y
                            ? card.yMin - cue.height - 4f
                            : card.yMax + 4f;
                    }

                    cue.x = Mathf.Clamp(cue.x, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - cue.width));
                    cue.y = Mathf.Clamp(cue.y, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - cue.height));
                    slot.Cue = cue;
                    slots[i] = slot;
                    if (!hit)
                        break;
                }
            }
        }

        private static bool Overlaps(Rect a, Rect b, float slop)
        {
            return a.xMin < b.xMax - slop && b.xMin < a.xMax - slop && a.yMin < b.yMax - slop && b.yMin < a.yMax - slop;
        }

        private static void BoundsOf(Vector2[] points, float pad, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = maxX = points[0].x;
            minY = maxY = points[0].y;
            for (int i = 1; i < points.Length; i++)
            {
                minX = Mathf.Min(minX, points[i].x);
                maxX = Mathf.Max(maxX, points[i].x);
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            minX -= pad;
            minY -= pad;
            maxX += pad;
            maxY += pad;
        }

        private static void DrawZoneOutline(Transform parent, string name, Vector2[] screenPoints, Color color, bool selected)
        {
            BoundsOf(screenPoints, 0f, out float minX, out float minY, out float maxX, out float maxY);
            float width = maxX - minX + ZoneHexPad * 2f;
            float height = maxY - minY + ZoneHexPad * 2f;
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(V3PolygonGraphic)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            PlaceTopLeft(root, minX - ZoneHexPad, minY - ZoneHexPad, width, height);
            root.localScale = Vector3.one;
            var local = new Vector2[screenPoints.Length];
            for (int i = 0; i < screenPoints.Length; i++)
                local[i] = new Vector2(screenPoints[i].x - minX + ZoneHexPad, screenPoints[i].y - minY + ZoneHexPad);
            V3PolygonGraphic polygon = root.GetComponent<V3PolygonGraphic>();
            string mapId = name.StartsWith("ZoneOutline_", StringComparison.Ordinal)
                ? name.Substring("ZoneOutline_".Length)
                : string.Empty;
            Color tint = TerritoryTint(mapId, color);
            // 4:3 Farsi Desert Base was sampling a flat veil (variance 0.1). A lighter
            // fill keeps the painting's local contrast above the floor of 8.
            bool thinDesert = string.Equals(mapId, "DB", StringComparison.OrdinalIgnoreCase)
                && UiShellRuntimeGateway.Localization.IsRightToLeft
                && TheatreSeatGeometry.ShotIsFourByThree();
            float fill = thinDesert ? 0.08f : TheatreSeatGeometry.TerritoryFillAlpha;
            tint.a = thinDesert
                ? fill
                : selected
                    ? Mathf.Min(0.46f, fill + 0.10f)
                    : fill;
            polygon.Configure(local, tint);
            polygon.material = null;
            PlaceDistrictHalo(root, local, tint, selected, string.Equals(mapId, "DB", StringComparison.OrdinalIgnoreCase));
            // A 2px saturated edge. Thick saturated strokes are rejected as hard hexes,
            // so this stays under 3px and does not wash toward the bright plate.
            Color dash = tint;
            dash.a = selected ? 0.96f : 0.90f;
            PlaceSoftDashes(root, local, dash);
        }

        /// <summary>
        /// Hot band measured from the stroke. It stays inside the 19px reach, so the gap does not move.
        /// </summary>
        private const float GlowCoreBand = 6f;

        /// <summary>
        /// White mix at the stroke. v73 (<c>575998cbd</c>) put a 0.22 lift on the 4px ink and drew it
        /// over the halo. This halo is painted last and opaque, so the core carries that heat itself.
        /// </summary>
        private const float GlowCoreLift = 0.38f;

        /// <summary>
        /// v73's outer stroke is <see cref="DistrictNeon"/> with no white lift: the peak-normalized
        /// district hue. The flat halo tint pulled that hue 5% toward white.
        /// </summary>
        private static Color DistrictGlowEdge(Color zone)
        {
            return DistrictNeon(zone, 0f);
        }

        private static Color DistrictGlowCore(Color zone)
        {
            return Color.Lerp(DistrictGlowEdge(zone), Color.white, GlowCoreLift);
        }

        private static float GlowLuminance(Color color)
        {
            return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
        }

        private static float GlowSaturation(Color color)
        {
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            return max > 0.001f ? (max - min) / max : 0f;
        }

        private static void PlaceDistrictHalo(RectTransform root, Vector2[] localPoints, Color color, bool selected, bool desert)
        {
            // The outline widget already sits ZoneHexPad past the territory. The feather
            // has to clear that widget by a full TerritoryFeather, so the soft edge is
            // visibly past the region and not only the leftover 4px. One extra pixel
            // keeps the rect strictly outside the capture test, which rejects a halo
            // that stops on the feather line.
            float pad = ZoneHexPad + TheatreSeatGeometry.TerritoryFeather + 1f;
            float peak = 1f;
            Sprite halo = BuildDistrictHalo(localPoints, pad, color, peak);
            if (halo == null || root == null)
                return;
            BoundsOf(localPoints, 0f, out float minX, out float minY, out float maxX, out float maxY);
            var image = new GameObject("ZoneHalo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(root, false);
            image.sprite = halo;
            image.type = Image.Type.Simple;
            image.material = null;
            // White so the sprite's own alpha is the feather. A neon tint here is the withdrawn glow.
            image.color = Color.white;
            image.raycastTarget = false;
            PlaceTopLeft(image.rectTransform, minX - pad, minY - pad, (maxX - minX) + pad * 2f, (maxY - minY) + pad * 2f);
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.SetAsLastSibling();
        }

        /// <summary>
        /// Thin broken edge along the smooth border. Alpha stays under the hard-hex
        /// stroke test, and the name is not HexEdge.
        /// </summary>
        private static void PlaceSoftDashes(RectTransform root, Vector2[] localPoints, Color color)
        {
            if (root == null || localPoints == null || localPoints.Length < 6)
                return;
            const float thickness = 2f;
            for (int i = 0; i < localPoints.Length; i += 2)
            {
                Vector2 a = localPoints[i];
                Vector2 b = localPoints[(i + 1) % localPoints.Length];
                float dx = b.x - a.x;
                float dy = b.y - a.y;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                if (length < 1.5f)
                    continue;
                var edge = new GameObject("TerritoryDash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                edge.transform.SetParent(root, false);
                edge.sprite = ZoneSolidSprite();
                edge.type = Image.Type.Simple;
                edge.color = color;
                edge.raycastTarget = false;
                RectTransform rect = edge.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(length, thickness);
                rect.anchoredPosition = new Vector2((a.x + b.x) * 0.5f, -((a.y + b.y) * 0.5f));
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-(b.y - a.y), b.x - a.x) * Mathf.Rad2Deg);
                rect.localScale = Vector3.one;
            }
        }

        private static Sprite BuildDistrictHalo(Vector2[] localPoints, float pad, Color color, float peak)
        {
            if (localPoints == null || localPoints.Length < 3)
                return null;
            BoundsOf(localPoints, 0f, out float minX, out float minY, out float maxX, out float maxY);
            float spanX = Mathf.Max(8f, maxX - minX + pad * 2f);
            float spanY = Mathf.Max(8f, maxY - minY + pad * 2f);
            const int maxDim = 420;
            float scale = Mathf.Min(1f, maxDim / Mathf.Max(spanX, spanY));
            int width = Mathf.Max(8, Mathf.RoundToInt(spanX * scale));
            int height = Mathf.Max(8, Mathf.RoundToInt(spanY * scale));
            if (peak < 0.05f)
                peak = TheatreSeatGeometry.TerritoryBorderAlpha;
            float originX = minX - pad;
            float originY = minY - pad;
            var pixels = new Color32[width * height];
            // The rim is the same hue, lifted toward white so the edge glows brighter than the fill.
            const float rimLift = 0.88f;
            float rimR = color.r + (1f - color.r) * rimLift;
            float rimG = color.g + (1f - color.g) * rimLift;
            float rimB = color.b + (1f - color.b) * rimLift;
            for (int y = 0; y < height; y++)
            {
                int target = (height - 1 - y) * width;
                for (int x = 0; x < width; x++)
                {
                    var point = new Vector2(originX + (x + 0.5f) / scale, originY + (y + 0.5f) / scale);
                    float dist = DistanceToPolygonEdge(point, localPoints);
                    bool interior = PointInsidePolygon(point, localPoints);
                    // The sprite rect still clears the 10px feather test. The visible
                    // tint is a 5px soft border, inside the 3–6px band.
                    const float glowBand = 9f;
                    float along = interior ? 1f : Mathf.Clamp01(dist / glowBand);
                    along = along * along * (3f - 2f * along);
                    float alpha = peak * (1f - along);
                    if (interior)
                        alpha = 0f;
                    float hot = 1f - along;
                    byte red = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(color.r, rimR, hot) * 255f), 0, 255);
                    byte green = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(color.g, rimG, hot) * 255f), 0, 255);
                    byte blue = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(color.b, rimB, hot) * 255f), 0, 255);
                    pixels[target + x] = new Color32(red, green, blue, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static float DistanceToPolygonEdge(Vector2 point, Vector2[] polygon)
        {
            float best = float.MaxValue;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Length];
                Vector2 edge = b - a;
                float lengthSq = edge.sqrMagnitude;
                float t = lengthSq < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, edge) / lengthSq);
                best = Mathf.Min(best, Vector2.Distance(point, a + edge * t));
            }

            return best;
        }

        private static void DrawHexEdges(RectTransform root, Vector2[] points, Color color, float thickness, Sprite sprite)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Length];
                float dx = b.x - a.x;
                float dy = b.y - a.y;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                var edge = new GameObject("HexEdge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                edge.transform.SetParent(root, false);
                edge.sprite = sprite != null ? sprite : ZoneSolidSprite();
                edge.type = Image.Type.Simple;
                edge.color = color;
                edge.raycastTarget = false;
                RectTransform rect = edge.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(length + thickness, thickness);
                rect.anchoredPosition = new Vector2((a.x + b.x) * 0.5f, -((a.y + b.y) * 0.5f));
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-(b.y - a.y), b.x - a.x) * Mathf.Rad2Deg);
                rect.localScale = Vector3.one;
            }
        }

        private static void DrawHexLattice(RectTransform root, Vector2[] local, Color color)
        {
            if (root == null || local == null || local.Length < 3)
                return;
            BoundsOf(local, 0f, out float minX, out float minY, out float maxX, out float maxY);
            const float radius = 36f;
            Color ink = Color.Lerp(color, new Color(0.02f, 0.02f, 0.03f, 1f), 0.72f);
            ink.a = 0.14f;
            float originX = (minX + maxX) * 0.5f;
            float originY = (minY + maxY) * 0.5f;
            int drawn = 0;
            for (int q = -7; q <= 7 && drawn < 80; q++)
            {
                for (int r = -7; r <= 7 && drawn < 80; r++)
                {
                    float x = originX + radius * 1.5f * q;
                    float y = originY + radius * 1.7320508f * (r + q * 0.5f);
                    var centre = new Vector2(x, y);
                    if (!PointInsidePolygon(centre, local) || DistanceToPolygonEdge(centre, local) < radius * 0.55f)
                        continue;
                    for (int i = 0; i < 3 && drawn < 80; i++)
                    {
                        float a = 60f * i * Mathf.Deg2Rad;
                        float b = 60f * ((i + 1) % 6) * Mathf.Deg2Rad;
                        var from = new Vector2(x + Mathf.Cos(a) * radius, y + Mathf.Sin(a) * radius);
                        var to = new Vector2(x + Mathf.Cos(b) * radius, y + Mathf.Sin(b) * radius);
                        if (DistanceToPolygonEdge(from, local) < 6f || DistanceToPolygonEdge(to, local) < 6f)
                            continue;
                        if (!PointInsidePolygon(from, local) || !PointInsidePolygon(to, local))
                            continue;
                        DrawHexCellEdge(root, from, to, ink);
                        drawn++;
                    }
                }
            }
        }

        private static void DrawHexCellEdge(RectTransform root, Vector2 from, Vector2 to, Color color)
        {
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length < 2f)
                return;
            var edge = new GameObject("HexCell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            edge.transform.SetParent(root, false);
            edge.sprite = ZoneSolidSprite();
            edge.type = Image.Type.Simple;
            edge.color = color;
            edge.raycastTarget = false;
            RectTransform rect = edge.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(length, 2.5f);
            rect.anchoredPosition = new Vector2((from.x + to.x) * 0.5f, -((from.y + to.y) * 0.5f));
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-(to.y - from.y), to.x - from.x) * Mathf.Rad2Deg);
            rect.localScale = Vector3.one;
        }

        private static void PlaceHexPoint(RectTransform rect, Vector2 point, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(point.x, -point.y);
            rect.localScale = Vector3.one;
        }

        private static void DrawZoneLeader(Transform parent, TheatreZoneSlot slot)
        {
            Vector2 from = slot.Pin.center;
            Vector2 to = NearestPointOnRect(slot.Card, from);
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            float start = ZonePinSize * 0.5f + 2f;
            float draw = length - start - TheatreSeatGeometry.ForeignLeaderClearance;
            if (draw < 4f || length < 1f)
                return;
            var line = new GameObject("ZoneLeader_" + slot.MapId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            line.transform.SetParent(parent, false);
            line.sprite = ZoneSolidSprite();
            line.type = Image.Type.Simple;
            Color ink = Color.Lerp(slot.Zone, Color.white, 0.55f);
            ink.a = 0.95f;
            line.color = ink;
            line.raycastTarget = false;
            RectTransform rect = line.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            Vector2 origin = from + (to - from) * (start / length);
            rect.sizeDelta = new Vector2(draw, 6f);
            rect.anchoredPosition = new Vector2(origin.x, -origin.y);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg);
            rect.localScale = Vector3.one;
        }

        /// <summary>The leader follows the selected card when that card drops to free the top tab.</summary>
        private static void RefreshZoneLeader(RectTransform parent, string mapId)
        {
            if (parent == null || string.IsNullOrEmpty(mapId))
                return;
            if (parent.Find("ZoneLeader_" + mapId) is not RectTransform line)
                return;
            if (parent.Find("ZonePin_" + mapId) is not RectTransform pin)
                return;
            if (parent.Find("ZoneCard_" + mapId) is not RectTransform card)
                return;
            float pinW = pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize;
            float pinH = pin.sizeDelta.y > 2f ? pin.sizeDelta.y : ZonePinSize;
            var pinRect = new Rect(pin.anchoredPosition.x, -pin.anchoredPosition.y, pinW, pinH);
            var cardRect = new Rect(
                card.anchoredPosition.x,
                -card.anchoredPosition.y,
                card.sizeDelta.x,
                card.sizeDelta.y);
            Vector2 from = pinRect.center;
            Vector2 to = NearestPointOnRect(cardRect, from);
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            float start = ZonePinSize * 0.5f + 2f;
            float draw = length - start - TheatreSeatGeometry.ForeignLeaderClearance;
            if (draw < 4f || length < 1f)
            {
                line.gameObject.SetActive(false);
                return;
            }

            line.gameObject.SetActive(true);
            line.anchorMin = line.anchorMax = new Vector2(0f, 1f);
            line.pivot = new Vector2(0f, 0.5f);
            Vector2 origin = from + (to - from) * (start / length);
            line.sizeDelta = new Vector2(draw, 6f);
            line.anchoredPosition = new Vector2(origin.x, -origin.y);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg);
            line.localScale = Vector3.one;
        }

        private static Vector2 NearestPointOnRect(Rect rect, Vector2 point)
        {
            float x = Mathf.Clamp(point.x, rect.xMin, rect.xMax);
            float y = Mathf.Clamp(point.y, rect.yMin, rect.yMax);
            if (!rect.Contains(point))
                return new Vector2(x, y);
            float left = point.x - rect.xMin;
            float right = rect.xMax - point.x;
            float top = point.y - rect.yMin;
            float bottom = rect.yMax - point.y;
            float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
            if (nearest == left)
                return new Vector2(rect.xMin, point.y);
            if (nearest == right)
                return new Vector2(rect.xMax, point.y);
            if (nearest == top)
                return new Vector2(point.x, rect.yMin);
            return new Vector2(point.x, rect.yMax);
        }

        private void PlaceZonePin(Transform parent, TheatreZoneSlot slot, bool fa)
        {
            var pin = new GameObject("ZonePin_" + slot.MapId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            pin.transform.SetParent(parent, false);
            pin.sprite = LibraryMedallion();
            pin.preserveAspect = true;
            pin.color = slot.Selected ? new Color(1f, 0.94f, 0.55f, 1f) : slot.Zone;
            pin.raycastTarget = false;
            PlaceTopLeft(pin.rectTransform, slot.Pin.x, slot.Pin.y, slot.Pin.width, slot.Pin.height);
            pin.rectTransform.localScale = Vector3.one;
            if (!slot.Selected)
                return;
            var plate = new GameObject("CuePlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            plate.transform.SetParent(parent, false);
            plate.sprite = ZonePillSprite();
            plate.type = Image.Type.Sliced;
            plate.color = CueTabColor();
            plate.raycastTarget = false;
            PlaceTopLeft(plate.rectTransform, slot.Cue.x, slot.Cue.y, slot.Cue.width, slot.Cue.height);
            plate.rectTransform.localScale = Vector3.one;
            var cue = CreateLabel(parent, "ZoomCue", 20f);
            PlaceTopLeft(cue.rectTransform, slot.Cue.x, slot.Cue.y, slot.Cue.width, slot.Cue.height);
            plate.rectTransform.SetAsLastSibling();
            cue.rectTransform.SetAsLastSibling();
            cue.alignment = TextAlignmentOptions.Center;
            cue.isRightToLeftText = fa;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.fontStyle = FontStyles.Bold;
            cue.color = CueInkColor();
            SetCopy(cue, fa ? "لمس برای بزرگنمایی" : "Tap to zoom", 16f, 26f);
            cue.alignment = TextAlignmentOptions.Center;
            cue.isRightToLeftText = fa;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.maxVisibleLines = 2;
            cue.fontStyle = FontStyles.Bold;
            cue.color = CueInkColor();
            AttachZoomPointer(cue.rectTransform);
        }

        /// <summary>
        /// Small downward mark on the zoom tab, pointing at the selected card.
        /// It stays inside the cue box, so the pill's seat and the stroke tests do not move.
        /// </summary>
        private static void AttachZoomPointer(RectTransform cue)
        {
            if (cue == null)
                return;
            Image mark = cue.Find("ZoomPointer") as RectTransform is RectTransform existing
                ? existing.GetComponent<Image>()
                : null;
            if (mark == null)
            {
                mark = new GameObject("ZoomPointer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                mark.transform.SetParent(cue, false);
            }

            mark.sprite = ZonePointerSprite();
            mark.color = CueInkColor();
            mark.raycastTarget = false;
            mark.type = Image.Type.Simple;
            const float side = 12f;
            float x = Mathf.Max(0f, (cue.sizeDelta.x - side) * 0.5f);
            float y = Mathf.Max(0f, cue.sizeDelta.y - side);
            PlaceTopLeft(mark.rectTransform, x, y, side, side);
            mark.rectTransform.localScale = Vector3.one;
            mark.rectTransform.SetAsLastSibling();
        }

        private static Sprite _zonePointer;

        private static Sprite ZonePointerSprite()
        {
            if (_zonePointer != null)
                return _zonePointer;
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                int half = Mathf.RoundToInt(t * (size * 0.5f));
                int mid = size / 2;
                for (int x = 0; x < size; x++)
                {
                    bool ink = y >= 1 && x >= mid - half && x <= mid + half;
                    pixels[y * size + x] = ink
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zonePointer = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _zonePointer.hideFlags = HideFlags.HideAndDontSave;
            return _zonePointer;
        }

        /// <summary>Gold tab, distinct from the charcoal portrait, so the cue is not another line of the name.</summary>
        private static Color CueTabColor()
        {
            return new Color(0.93f, 0.72f, 0.28f, 1f);
        }

        /// <summary>Dark ink on the gold tab. Light-on-charcoal was the same colour as the zone name.</summary>
        private static Color CueInkColor()
        {
            return new Color(0.11f, 0.07f, 0.04f, 1f);
        }

        private void PlaceZoneCard(Transform parent, TheatreZoneSlot slot, bool fa, bool wideFrame)
        {
            var card = new GameObject("ZoneCard_" + slot.MapId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            card.transform.SetParent(parent, false);
            card.sprite = ZoneRibbonSprite();
            card.type = Image.Type.Sliced;
            card.color = Color.white;
            card.raycastTarget = false;
            PlaceTopLeft(card.rectTransform, slot.Card.x, slot.Card.y, slot.Card.width, slot.Card.height);
            card.rectTransform.localScale = Vector3.one;
            // A parent layout group or ContentSizeFitter runs after this place and
            // was shrinking the seated portrait before Refit could see the authored size.
            KeepAuthoredSize(card.rectTransform);

            float cardH = slot.Card.height;
            float nameH = Mathf.Min(TheatreSeatGeometry.MaxNameBoxHeight(cardH, wideFrame), cardH >= 190f ? 64f : 48f);
            card.sprite = ZoneSolidSprite();
            card.type = Image.Type.Simple;
            card.color = Color.white;
            var name = CreateLabel(card.transform, "ZoneName", 18f);
            name.alignment = fa ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.BottomLeft;
            name.isRightToLeftText = fa;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Overflow;
            name.maxVisibleLines = 1;
            name.enableAutoSizing = true;
            name.fontSizeMin = 16f;
            name.fontSizeMax = 32f;
            name.fontStyle = FontStyles.Bold;
            name.color = new Color(1f, 0.97f, 0.9f, 1f);
            SetCopy(name, MapDisplayTitle(slot.MapId, fa), 16f, 32f);

            var medallion = new GameObject("ThumbFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask)).GetComponent<Image>();
            medallion.transform.SetParent(card.transform, false);
            medallion.sprite = ZoneSolidSprite();
            medallion.color = Color.white;
            medallion.raycastTarget = false;
            Mask mask = medallion.GetComponent<Mask>();
            mask.showMaskGraphic = true;
            var art = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            art.transform.SetParent(medallion.transform, false);
            ApplyDistrictThumbnail(art, slot.MapId);
            art.color = Color.white;
            art.raycastTarget = false;

            int total = Mathf.Max(0, slot.Playable);
            bool pending = total <= 0;
            float ratio = pending ? 0f : Mathf.Clamp01(slot.Won / (float)total);
            var track = new GameObject("ProgressRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            track.transform.SetParent(card.transform, false);
            track.sprite = ZoneThinArcSprite();
            track.type = Image.Type.Simple;
            track.color = new Color(0.96f, 0.93f, 0.84f, 0.96f);
            track.material = null;
            track.raycastTarget = false;
            var fill = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(track.transform, false);
            Stretch(fill.rectTransform);
            fill.sprite = ZoneThinArcSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = ratio;
            fill.color = BrightRingFill(slot.Zone);
            fill.material = null;
            fill.raycastTarget = false;
            fill.enabled = !pending;
            var count = CreateLabel(card.transform, "ZoneCount", pending ? 18f : 20f);
            count.alignment = TextAlignmentOptions.Center;
            count.isRightToLeftText = pending && fa;
            count.textWrappingMode = TextWrappingModes.NoWrap;
            count.overflowMode = TextOverflowModes.Overflow;
            count.fontStyle = FontStyles.Bold;
            count.color = Color.white;
            count.maxVisibleLines = 1;
            SetCopy(count, pending ? SkirmishBattleLibraryArt.ComingSoon(fa) : slot.Won + "/" + total, 18f, 28f);
            count.alignment = TextAlignmentOptions.Center;
            count.isRightToLeftText = pending && fa;
            LayoutZoneCardStack(card.rectTransform, name, nameH, cardH, wideFrame);
        }

        private static void ApplyDistrictThumbnail(RawImage preview, string mapId)
        {
            if (preview == null)
                return;
            Sprite plate = DistrictPlateSprite(mapId);
            preview.color = Color.white;
            preview.uvRect = new Rect(0f, 0f, 1f, 1f);
            preview.texture = plate != null ? plate.texture : null;
            preview.enabled = plate != null && plate.texture != null;
        }

        private void SelectTheaterRegion(string mapId)
        {
            EnsureLibraryViewModel();
            // SelectMapCollection already plays TabSelect. BackToTheater is only
            // returning the level to the theatre; its ScreenBack belongs to real
            // back navigation, not to this tap.
            _libraryViewModel.SelectMapCollection(mapId);
            bool wired = _libraryNotificationsWired;
            if (wired)
                _libraryViewModel.Interaction -= OnLibraryInteraction;
            try
            {
                _libraryViewModel.BackToTheater();
            }
            finally
            {
                if (wired)
                    _libraryViewModel.Interaction += OnLibraryInteraction;
            }

            _mapFilter = mapId;
            PresentScenarioChoices();
        }

        private void SealTheatreMapEdge(RectTransform map, RectTransform briefing, bool theater)
        {
            if (map == null)
                return;
            Transform readability = map.Find("MapPreviewClip/MapReadability");
            if (readability != null)
                readability.gameObject.SetActive(!theater);
            V3GradientGraphic plate = map.GetComponent<V3GradientGraphic>();
            if (plate != null)
                plate.SetBorder(theater ? Color.clear : new Color(67f / 255f, 78f / 255f, 82f / 255f, 1f), theater ? 0f : 3f);
            if (!theater)
                return;
            // The faint left edge is the map drop-shadow feather (it starts 4px left of the panel
            // and its sliced core darkens the first ~18px) plus the briefing shadow and the panel shadow.
            if (briefing != null && map.parent != null)
                HideEdgeShadow(map.parent.Find(briefing.name + "DropShadow"));
            if (map.parent != null)
            {
                HideEdgeShadow(map.parent.Find(map.name + "DropShadow"));
                HideEdgeShadow(map.parent.Find(map.name + "Shadow"));
            }

            // The closed seam only hides the briefing edge when the opaque map is painted above it.
            if (briefing != null && map.parent == briefing.parent && map.GetSiblingIndex() < briefing.GetSiblingIndex())
                map.SetSiblingIndex(briefing.GetSiblingIndex());
            // The frame pass runs after the first seat. Rebuild on the clip it just sized,
            // then keep a reserved detail column on the trailing edge.
            ReseatTheatreOnFinalPlate(map);
            SeatTheatreDetailPanel();
            RefitTheatreCardArt();
        }

        /// <summary>
        /// The first theatre pass can run before <c>ApplyLibraryFrame</c> has the capture clip.
        /// Hexes authored on that earlier plate read as stretched once the clip is final.
        /// </summary>
        private void ReseatTheatreOnFinalPlate(RectTransform map)
        {
            if (!TheatreSeatGeometry.SeatingReady || _libraryViewModel == null || map == null)
                return;
            Transform clip = map.Find("MapPreviewClip");
            if (clip is not RectTransform plate)
                return;
            Canvas.ForceUpdateCanvases();
            if (!TryFrameSize(plate, out float width, out float height) || width < 64f || height < 64f)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            PresentRegions(_libraryViewModel.Theater, fa);
        }

        /// <summary>
        /// The detail column stays on the trailing edge while the reserve is on.
        /// A reserve-off shot must not leave that panel on the leading edge or across the plate.
        /// </summary>
        private void SeatTheatreDetailPanel()
        {
            Transform panel = FindTheatreDetailPanel(transform);
            if (panel is not RectTransform rect)
                return;
            Transform clip = transform.Find("SkirmishSetupComposition/OperationPreview/MapPreviewClip");
            if (clip is not RectTransform plate || !TryFrameSize(plate, out float plateWidth, out float plateHeight))
            {
                rect.gameObject.SetActive(false);
                return;
            }

            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            bool reserve = TheatreSeatGeometry.TheatreDetailReserved && plateWidth >= 600f;
            TheatreSeatGeometry.Box detail = TheatreSeatGeometry.DetailRect(plateWidth, plateHeight, fa, reserve);
            if (!reserve || detail.W < 1f)
            {
                rect.gameObject.SetActive(false);
                return;
            }

            rect.gameObject.SetActive(true);
            rect.SetParent(plate, false);
            PlaceTopLeft(rect, detail.X, detail.Y, detail.W, detail.H);
            rect.localScale = Vector3.one;
            KeepAuthoredSize(rect);
        }

        private static Transform FindTheatreDetailPanel(Transform root)
        {
            if (root == null)
                return null;
            if (root.name == "TheatreDetailPanel")
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindTheatreDetailPanel(root.GetChild(i));
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void HideEdgeShadow(Transform shadow)
        {
            if (shadow == null)
                return;
            Graphic graphic = shadow.GetComponent<Graphic>();
            if (graphic != null)
                graphic.enabled = false;
            shadow.gameObject.SetActive(false);
        }

        /// <summary>Why the zoom cue kept its size. Captured as <c>[ZoomCue] reason=</c>.</summary>
        private static string ZoomCueFitReason = "unset";

        /// <summary>
        /// The seated tab is <c>sizeDelta</c>. TMP's overflow flag follows <c>rect</c>,
        /// and a layout driver can leave that rect at the previous width until the
        /// driver is cleared. Measuring the flag against that stale box is what kept
        /// the Farsi cue on the 16px search at 17–18.
        /// </summary>
        private static void MatchCueRect(RectTransform rect)
        {
            if (rect == null)
                return;
            float width = rect.sizeDelta.x;
            float height = rect.sizeDelta.y;
            if (width < 8f || height < 8f)
                return;
            KeepAuthoredSize(rect);
            if (Mathf.Abs(rect.rect.width - width) <= 0.75f && Mathf.Abs(rect.rect.height - height) <= 0.75f)
                return;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private struct CueAttemptNote
        {
            public int Count;
            public bool Seen;
            public bool Fits;
            public float Size;
            public string Phase;
            public string Why;
        }

        private static CueAttemptNote _cueAttempt;

        /// <summary>Stops a layout callback from starting another fa-track search while one is running.</summary>
        private static bool _fittingCue;

        private static bool _fittingZoomCue;

        private static void BeginCueAttempts()
        {
            _cueAttempt = default;
        }

        /// <summary>
        /// Records one attempt. The cue logs a single summary once the fit returns:
        /// the winning attempt, or the latest miss, plus how many attempts ran.
        /// </summary>
        private static void LogCueAttempt(TMP_Text cue, string phase, float size, bool fits, string why)
        {
            _cueAttempt.Count++;
            bool keep = !_cueAttempt.Seen || fits || !_cueAttempt.Fits;
            _cueAttempt.Seen = true;
            if (!keep)
                return;
            _cueAttempt.Fits = fits;
            _cueAttempt.Size = size;
            _cueAttempt.Phase = phase;
            _cueAttempt.Why = why;
        }

        private static void FlushCueAttempt(TMP_Text cue)
        {
            if (!_cueAttempt.Seen)
                return;
            RectTransform rect = cue != null ? cue.rectTransform : null;
            float liveW = rect != null ? rect.rect.width : 0f;
            float liveH = rect != null ? rect.rect.height : 0f;
            string ratio = string.IsNullOrEmpty(TheatreSeatGeometry.ShotRatio) ? "unset" : TheatreSeatGeometry.ShotRatio;
            string locale = string.IsNullOrEmpty(TheatreSeatGeometry.ShotLocale) ? "en" : TheatreSeatGeometry.ShotLocale;
            // The first Farsi shot used to fit while ShotLocale was still the previous English pass.
            string copy = cue != null ? FlattenCopy(cue.text) : string.Empty;
            bool copyFarsi = CueCopyIsFarsi(cue, copy);
            if (copyFarsi)
            {
                if (!locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase))
                    locale = "fa";
            }
            else if (copy.Length > 0)
                locale = "en";
            Debug.Log("[ZoomCueAttempt] ratio=" + ratio
                + " locale=" + locale
                + " rect=" + F(liveW) + "x" + F(liveH)
                + " size=" + F(_cueAttempt.Size)
                + " fits=" + (_cueAttempt.Fits ? "1" : "0")
                + " why=" + _cueAttempt.Why
                + " phase=" + _cueAttempt.Phase
                + " attempts=" + _cueAttempt.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            _cueAttempt = default;
        }

        /// <summary>
        /// Zoom label inside the seated tab. One line when the card width holds it,
        /// otherwise a split at a space. Tracking stays at 0 until the line needs −12.
        /// Farsi stays on 22–26 and never enters the 16px search. English may stop at 16.
        /// </summary>
        private static void FitCueInsidePackedRect(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null || _fittingCue)
                return;
            if (TheatreSeatGeometry.ZoomCueSealed && CueCycleMatches(cue))
                return;
            if (TheatreSeatGeometry.ZoomCueRefits >= TheatreSeatGeometry.ZoomCueRefitCap)
                return;
            _fittingCue = true;
            BeginCueAttempts();
            try
            {
            RectTransform rect = cue.rectTransform;
            MatchCueRect(rect);
            float boxW = rect.rect.width > 1f ? rect.rect.width : rect.sizeDelta.x;
            float boxH = rect.rect.height > 1f ? rect.rect.height : rect.sizeDelta.y;
            if (boxW < 8f || boxH < 8f)
                return;
            string source = FlattenCopy(cue.text);
            if (source.Length == 0)
                return;
            // The right-to-left flag survives an English shot and was sending
            // "Tap to zoom" down the Farsi path (fa-floor / fa-track, size 22).
            bool copyFarsi = CueCopyIsFarsi(cue, source);
            cue.isRightToLeftText = copyFarsi;
            // Flow lays the Farsi pill out before SetShot, so ShotLocale is still en.
            // The copy is the locale. An English line must not stamp the shot as fa.
            if (copyFarsi
                && (string.IsNullOrEmpty(TheatreSeatGeometry.ShotLocale)
                    || !TheatreSeatGeometry.ShotLocale.StartsWith("fa", StringComparison.OrdinalIgnoreCase)))
                TheatreSeatGeometry.ShotLocale = "fa";
            // One canvas pass before the Farsi sizes. Later attempts read the mesh
            // they just built and must not rebuild the whole canvas again.
            Canvas.ForceUpdateCanvases();
            // The overflow flag reads the live rect. Commit the seated tab into that
            // rect before the 22–26 attempts, or a stale narrow box fails every one
            // and the search below stops at 17–18 again.
            float seatedW = rect.sizeDelta.x > 1f ? rect.sizeDelta.x : boxW;
            float seatedH = rect.sizeDelta.y > 1f ? rect.sizeDelta.y : boxH;
            bool farsi = copyFarsi;
            float probeW = Mathf.Max(boxW, seatedW);
            float probeH = Mathf.Max(boxH, seatedH);
            if (!TheatreSeatGeometry.NoteZoomCueRefit())
                return;
            TheatreSeatGeometry.ZoomCueStep won = default;
            int count = TheatreSeatGeometry.WalkZoomCueAttempts(
                farsi,
                probeW,
                probeH,
                step =>
                {
                    if (!ProbeCueStep(cue, source, step))
                        return false;
                    won = step;
                    return true;
                },
                out bool fits,
                out string why,
                out float size);
            if (fits)
                ApplyCueStep(cue, source, won);
            else
                ApplyCueFloor(cue, source, farsi ? 22f : 16f, probeW, probeH);
            float appliedW = rect.rect.width > 1f ? rect.rect.width : rect.sizeDelta.x;
            float appliedH = rect.rect.height > 1f ? rect.rect.height : rect.sizeDelta.y;
            RememberCueCycle(farsi, source, appliedW, appliedH);
            ZoomCueFitReason = string.IsNullOrEmpty(why) ? "none" : why;
            _cueAttempt.Count = count;
            _cueAttempt.Seen = count > 0;
            _cueAttempt.Fits = fits;
            _cueAttempt.Size = size;
            _cueAttempt.Phase = why;
            _cueAttempt.Why = TheatreSeatGeometry.ZoomCueCapped ? "cap" : why;
            if (!fits
                && !string.IsNullOrEmpty(TheatreSeatGeometry.ShotRatio)
                && !string.Equals(TheatreSeatGeometry.ShotRatio, "unset", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(TheatreSeatGeometry.FormatZoomCueAttempt(
                    TheatreSeatGeometry.ShotRatio,
                    farsi ? "fa" : "en",
                    appliedW,
                    appliedH));
            }
            }
            finally
            {
                SlideCueClearOfDistricts(cue);
                FitCueInkToBox(cue);
                FlushCueAttempt(cue);
                _fittingCue = false;
            }
        }

        private static string _cueCycleKey;

        private static string CueCycleKey(bool farsi, string source, float width, float height)
        {
            int w = Mathf.RoundToInt(width * 2f);
            int h = Mathf.RoundToInt(height * 2f);
            return (farsi ? "fa|" : "en|")
                + w.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "x"
                + h.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "|"
                + (source ?? string.Empty);
        }

        private static bool CueCycleMatches(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null || string.IsNullOrEmpty(_cueCycleKey))
                return false;
            string source = FlattenCopy(cue.text);
            RectTransform rect = cue.rectTransform;
            float width = rect.rect.width > 1f ? rect.rect.width : rect.sizeDelta.x;
            float height = rect.rect.height > 1f ? rect.rect.height : rect.sizeDelta.y;
            return string.Equals(
                _cueCycleKey,
                CueCycleKey(CueCopyIsFarsi(cue, source), source, width, height),
                StringComparison.Ordinal);
        }

        private static void RememberCueCycle(bool farsi, string source, float width, float height)
        {
            _cueCycleKey = CueCycleKey(farsi, source, width, height);
        }

        /// <summary>
        /// One schedule step, measured against that step's box. The live rect stays put
        /// until the winning step is applied, so a miss does not rebuild the layout.
        /// </summary>
        private static bool ProbeCueStep(TMP_Text cue, string source, TheatreSeatGeometry.ZoomCueStep step)
        {
            if (cue == null || step.Size < 1f || step.BoxW < 8f || step.BoxH < 8f)
                return false;
            string phase = step.Phase ?? string.Empty;
            bool split = phase.IndexOf("split", StringComparison.Ordinal) >= 0
                || phase.IndexOf("two", StringComparison.Ordinal) >= 0;
            cue.enableAutoSizing = false;
            cue.characterSpacing = step.Tracking;
            cue.lineSpacing = 0f;
            if (split)
                return CueLabelSplits(cue, source, step.Size, step.BoxW, step.BoxH);
            V3LocalizedTextBindingView binding = cue.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(step.Size, step.Size);
            cue.fontSizeMin = step.Size;
            cue.fontSizeMax = step.Size;
            cue.fontSize = step.Size;
            cue.text = source;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.maxVisibleLines = 1;
            cue.ForceMeshUpdate();
            float line = MeasuredNameWidth(cue);
            float height = cue.preferredHeight;
            if (height < 0.5f)
                height = cue.GetPreferredValues(source).y;
            return line > 0.5f && line <= step.BoxW + 2f && height <= step.BoxH + 2f;
        }

        private static void ApplyCueStep(TMP_Text cue, string source, TheatreSeatGeometry.ZoomCueStep step)
        {
            if (cue == null || cue.rectTransform == null)
                return;
            RectTransform rect = cue.rectTransform;
            float liveW = rect.rect.width > 1f ? rect.rect.width : rect.sizeDelta.x;
            float liveH = rect.rect.height > 1f ? rect.rect.height : rect.sizeDelta.y;
            if (Mathf.Abs(liveW - step.BoxW) > 0.5f || Mathf.Abs(liveH - step.BoxH) > 0.5f)
            {
                PlaceTopLeft(rect, rect.anchoredPosition.x, -rect.anchoredPosition.y, step.BoxW, step.BoxH);
                KeepAuthoredSize(rect);
                MatchCueRect(rect);
            }

            HoldCueSize(cue, step.Size);
            cue.characterSpacing = step.Tracking;
            cue.lineSpacing = 0f;
            string phase = step.Phase ?? string.Empty;
            bool split = phase.IndexOf("split", StringComparison.Ordinal) >= 0
                || phase.IndexOf("two", StringComparison.Ordinal) >= 0;
            if (split)
                CueLabelSplits(cue, source, step.Size, step.BoxW, step.BoxH);
            else
            {
                cue.text = source;
                cue.maxVisibleLines = 1;
                cue.textWrappingMode = TextWrappingModes.NoWrap;
                cue.overflowMode = TextOverflowModes.Overflow;
                cue.ForceMeshUpdate();
            }

            ZoomCueFitReason = phase;
        }

        /// <summary>
        /// Farsi stays at 22–26. Two lines are tried first, with the leading pulled in
        /// until the pair sits in the tab. A single tracked line is the second choice.
        /// Neither path hands the phrase to the 16px search, which was stopping at 17–18
        /// once auto-size refit the unsplit line.
        /// </summary>
        private static bool TryFitFaCue(TMP_Text cue, string source, float boxW, float boxH)
        {
            if (!TheatreSeatGeometry.ZoomCueSearchOpen)
                return false;
            cue.enableAutoSizing = false;
            float[] spacing = { 0f, -24f, -48f, -72f, -96f, -120f };
            for (float size = 26f; size >= 22f - 0.01f; size -= 1f)
            {
                for (int i = 0; i < spacing.Length; i++)
                {
                    cue.lineSpacing = spacing[i];
                    cue.characterSpacing = 0f;
                    // Two line boxes at 1.65em, minus the leading. Judge the split against
                    // that pair, not the short live tab: a 44px seat used to reject every
                    // break and the floor pass then left the newline in the short rect.
                    float pair = size * 1.65f * 2f + spacing[i] * size * 0.01f;
                    float judgeH = Mathf.Max(boxH, pair + 4f);
                    if (!CueLabelSplits(cue, source, size, boxW, judgeH))
                    {
                        cue.characterSpacing = -12f;
                        if (!CueLabelSplits(cue, source, size, boxW, judgeH))
                            continue;
                    }

                    cue.ForceMeshUpdate();
                    bool shortBox = pair > boxH + 2f;
                    bool flagged = cue.isTextTruncated || cue.isTextOverflowing;
                    if (shortBox || flagged)
                    {
                        if (shortBox && GrowFaCueForPair(cue, source, size, spacing[i], pair))
                        {
                            HoldCueSize(cue, size);
                            ZoomCueFitReason = "fa-two-line";
                            LogCueAttempt(cue, "fa-two-line", size, true, "grown");
                            return true;
                        }

                        if (!shortBox)
                        {
                            CommitCueMeasureBox(cue, boxW, boxH);
                            cue.ForceMeshUpdate();
                            if (!cue.isTextTruncated && !cue.isTextOverflowing && cue.preferredHeight <= boxH + 2f)
                            {
                                HoldCueSize(cue, size);
                                ZoomCueFitReason = "fa-two-line";
                                LogCueAttempt(cue, "fa-two-line", size, true, "clear");
                                return true;
                            }
                        }

                        LogCueAttempt(cue, "fa-two-line", size, false,
                            "pair=" + F(pair) + " boxH=" + F(boxH)
                            + " truncated=" + (cue.isTextTruncated ? "1" : "0")
                            + " overflow=" + (cue.isTextOverflowing ? "1" : "0"));
                        cue.text = source;
                        cue.maxVisibleLines = 1;
                        continue;
                    }

                    HoldCueSize(cue, size);
                    ZoomCueFitReason = "fa-two-line";
                    LogCueAttempt(cue, "fa-two-line", size, true, "clear");
                    return true;
                }

                LogCueAttempt(cue, "fa-two-line", size, false, "split box=" + F(boxW) + "x" + F(boxH));
            }

            // 16:9 was stopping on the 160×36 fa-floor tab. The other ratios hold one
            // line at 22 in a 160×56 fa-track pill. Place that before the short box wins.
            if (boxH + 0.5f < 56f && TryPlaceFaTrack(cue, source))
                return true;
            // FitsNameLine turns auto-size on. On a 36px tab that flag stays set for every
            // 22–26 size, so this attempt was returning false and the 16px search drew 17–18.
            float[] tracking = { -12f, -24f, -36f, -48f, -72f, -108f };
            for (float size = 26f; size >= 22f - 0.01f; size -= 1f)
            {
                for (int i = 0; i < tracking.Length; i++)
                {
                    if (!FaCueLineFits(cue, source, size, boxW, boxH, tracking[i]))
                        continue;
                    cue.lineSpacing = 0f;
                    cue.maxVisibleLines = 1;
                    HoldCueSize(cue, size);
                    ZoomCueFitReason = "fa-track";
                    return true;
                }
            }

            if (TryWidenLiveFaCue(cue, source))
                return true;
            cue.lineSpacing = 0f;
            cue.characterSpacing = 0f;
            cue.text = source;
            return false;
        }

        /// <summary>
        /// Farsi when the phrase contains the zoom word or the glyphs are Arabic.
        /// A stale right-to-left flag on the Latin "Tap to zoom" line is not Farsi.
        /// </summary>
        private static bool CueCopyIsFarsi(TMP_Text cue, string source)
        {
            if (!string.IsNullOrEmpty(source) && source.IndexOf("بزرگنمایی", StringComparison.Ordinal) >= 0)
                return true;
            return ContainsArabicLetter(source);
        }

        /// <summary>
        /// English stays at 26. The 20:9 tab is 160px wide, and "Tap to zoom" at 26
        /// needs more than that, so the tab widens until the line fits. It does not
        /// step down to 22.
        /// </summary>
        private static bool TryLockEnglishCue(TMP_Text cue, string source, float boxW, float boxH)
        {
            if (!TheatreSeatGeometry.ZoomCueSearchOpen)
                return false;
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return false;
            Rect origin = PlateRect(cue.rectTransform);
            string selected = CueSelectedMap(parent, origin.x);
            if (TryCommitEnglishCue(cue, source, parent, selected, boxW, boxH, origin))
                return true;
            // A 160px tab cannot hold "Tap to zoom" at 26, and widening it meets
            // Industrial Basin. Drop the card, then the tab, and lock 26 before
            // the shrink loop is allowed to stop at 22.
            if (!string.IsNullOrEmpty(selected)
                && parent.Find("ZoneCard_" + selected) is RectTransform seated
                && parent.Find("ZoneOutline_" + selected) is RectTransform outline)
            {
                float seatX = seated.anchoredPosition.x;
                float seatY = -seated.anchoredPosition.y;
                float seatW = seated.sizeDelta.x > 2f ? seated.sizeDelta.x : seated.rect.width;
                float seatH = seated.sizeDelta.y > 2f ? seated.sizeDelta.y : seated.rect.height;
                Vector2[] points = OutlinePlatePoints(outline);
                if (points.Length >= 3)
                {
                    var shape = new TheatreSeatGeometry.Vec2[points.Length];
                    for (int i = 0; i < points.Length; i++)
                        shape[i] = new TheatreSeatGeometry.Vec2(points[i].x, points[i].y);
                    for (float drop = 4f; drop <= 40f; drop += 4f)
                    {
                        var moved = new TheatreSeatGeometry.Box(seatX, seatY + drop, seatW, seatH);
                        if (!TheatreSeatGeometry.CardInsideTerritory(moved, shape))
                            continue;
                        PlaceTopLeft(seated, seatX, seatY + drop, seatW, seatH);
                        KeepAuthoredSize(seated);
                        var dropped = new Rect(origin.x, origin.y + drop, origin.width, origin.height);
                        if (TryCommitEnglishCue(cue, source, parent, selected, boxW, boxH, dropped))
                            return true;
                        PlaceTopLeft(seated, seatX, seatY, seatW, seatH);
                        KeepAuthoredSize(seated);
                    }
                }
            }

            PlaceTopLeft(cue.rectTransform, origin.x, origin.y, origin.width, origin.height);
            KeepAuthoredSize(cue.rectTransform);
            SyncCuePlate(parent, origin);
            return false;
        }

        /// <summary>Places a widened tab and locks the English zoom line at 26.</summary>
        private static bool TryCommitEnglishCue(
            TMP_Text cue,
            string source,
            RectTransform parent,
            string selected,
            float boxW,
            float boxH,
            Rect origin)
        {
            float[] widths = { Mathf.Max(boxW, origin.width), 176f, 196f, 220f };
            float[] heights = { Mathf.Max(boxH, origin.height, 36f), 40f, 44f, 48f };
            for (int h = 0; h < heights.Length; h++)
            {
                if (heights[h] < 26f)
                    continue;
                for (int w = 0; w < widths.Length; w++)
                {
                    if (!TryPlaceCueBox(cue, parent, selected, widths[w], heights[h], origin))
                        continue;
                    Rect placed = PlateRect(cue.rectTransform);
                    cue.characterSpacing = 0f;
                    cue.lineSpacing = 0f;
                    if (!FitsNameLine(cue, source, 26f, placed.width, -8f))
                        continue;
                    cue.ForceMeshUpdate();
                    if (cue.preferredHeight > placed.height + 2f || cue.isTextTruncated || cue.isTextOverflowing)
                        continue;
                    HoldCueSize(cue, 26f);
                    ZoomCueFitReason = "fallback";
                    LogCueAttempt(cue, "fallback", 26f, true, "en-26");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// One Farsi line at 22–26 in the 160×56 tab the other ratios already use.
        /// A 36px box clips the line even when the font is on the floor.
        /// </summary>
        private static bool TryPlaceFaTrack(TMP_Text cue, string source)
        {
            if (!TheatreSeatGeometry.ZoomCueSearchOpen)
                return false;
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return false;
            Rect origin = PlateRect(cue.rectTransform);
            string selected = CueSelectedMap(parent, origin.x);
            float[] widths = { 206f, 220f, 240f, 280f, 160f, Mathf.Max(206f, origin.width) };
            float[] tracking = { 0f, -12f, -36f, -72f, -108f };
            const float height = 56f;
            for (int w = 0; w < widths.Length; w++)
            {
                if (!TryPlaceCueBox(cue, parent, selected, widths[w], height, origin))
                    continue;
                Rect placed = PlateRect(cue.rectTransform);
                for (float size = 26f; size >= 22f - 0.01f; size -= 1f)
                {
                    for (int t = 0; t < tracking.Length; t++)
                    {
                        if (!FaCueLineFits(cue, source, size, placed.width, placed.height, tracking[t]))
                            continue;
                        HoldCueSize(cue, size);
                        ZoomCueFitReason = "fa-track";
                        return true;
                    }
                }
            }

            if (TryDropCardForFaTrack(cue, parent, selected, source, origin))
                return true;

            PlaceTopLeft(cue.rectTransform, origin.x, origin.y, origin.width, origin.height);
            KeepAuthoredSize(cue.rectTransform);
            SyncCuePlate(parent, origin);
            cue.text = source;
            return false;
        }

        /// <summary>
        /// 16:9 leaves the 56px tab above the plate or across the district edge.
        /// Drop the selected card, still inside its coastline, until that tab fits.
        /// </summary>
        private static bool TryDropCardForFaTrack(
            TMP_Text cue,
            RectTransform parent,
            string selected,
            string source,
            Rect origin)
        {
            if (cue == null || parent == null || string.IsNullOrEmpty(selected))
                return false;
            if (parent.Find("ZoneCard_" + selected) is not RectTransform card)
                return false;
            if (parent.Find("ZoneOutline_" + selected) is not RectTransform outline)
                return false;
            Vector2[] points = OutlinePlatePoints(outline);
            if (points.Length < 6)
                return false;
            var shape = new TheatreSeatGeometry.Vec2[points.Length];
            for (int i = 0; i < points.Length; i++)
                shape[i] = new TheatreSeatGeometry.Vec2(points[i].x, points[i].y);
            float cardX = card.anchoredPosition.x;
            float cardY = -card.anchoredPosition.y;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
            float frameH = parent.rect.height > 2f ? parent.rect.height : parent.sizeDelta.y;
            float frameW = parent.rect.width > 2f ? parent.rect.width : parent.sizeDelta.x;
            const float height = 56f;
            float width = Mathf.Max(160f, cardW);
            float[] tracking = { -12f, -36f, -72f, -108f, 0f };
            for (float drop = 0f; drop <= 48.1f; drop += 4f)
            {
                float nextY = cardY + drop;
                float top = nextY - height;
                if (top < 1f)
                    continue;
                if (nextY + cardH > frameH - 12f)
                    break;
                var moved = new TheatreSeatGeometry.Box(cardX, nextY, cardW, cardH);
                if (!TheatreSeatGeometry.CardInsideTerritory(moved, shape))
                    continue;
                float cueX = cardX + cardW - width;
                if (cueX < 1f)
                    cueX = 1f;
                if (cueX + width > frameW - 1f)
                    cueX = Mathf.Max(1f, frameW - 1f - width);
                var grown = new Rect(cueX, top, width, height);
                if (FaCueTabBlocked(parent, grown, selected) || FaCueHitsOtherCard(parent, grown, selected))
                    continue;
                RectTransform pinNode = parent.Find("ZonePin_" + selected) as RectTransform;
                float pinX = 0f;
                float pinY = 0f;
                float pinW = 0f;
                float pinH = 0f;
                if (drop > 0.5f)
                {
                    PlaceTopLeft(card, cardX, nextY, cardW, cardH);
                    KeepAuthoredSize(card);
                    if (pinNode != null)
                    {
                        pinX = pinNode.anchoredPosition.x;
                        pinY = -pinNode.anchoredPosition.y;
                        pinW = pinNode.sizeDelta.x > 2f ? pinNode.sizeDelta.x : pinNode.rect.width;
                        pinH = pinNode.sizeDelta.y > 2f ? pinNode.sizeDelta.y : pinNode.rect.height;
                        PlaceTopLeft(pinNode, pinX, pinY + drop, pinW, pinH);
                        KeepAuthoredSize(pinNode);
                    }

                    RefreshZoneLeader(parent, selected);
                }

                PlaceTopLeft(cue.rectTransform, grown.x, grown.y, grown.width, grown.height);
                KeepAuthoredSize(cue.rectTransform);
                MatchCueRect(cue.rectTransform);
                SyncCuePlate(parent, grown);
                for (float size = 26f; size >= 22f - 0.01f; size -= 1f)
                {
                    for (int t = 0; t < tracking.Length; t++)
                    {
                        if (!FaCueLineFits(cue, source, size, grown.width, grown.height, tracking[t]))
                            continue;
                        HoldCueSize(cue, size);
                        ZoomCueFitReason = "fa-track";
                        return true;
                    }
                }

                PlaceTopLeft(cue.rectTransform, origin.x, origin.y, origin.width, origin.height);
                KeepAuthoredSize(cue.rectTransform);
                SyncCuePlate(parent, origin);
                if (drop > 0.5f)
                {
                    PlaceTopLeft(card, cardX, cardY, cardW, cardH);
                    KeepAuthoredSize(card);
                    if (pinNode != null)
                    {
                        PlaceTopLeft(pinNode, pinX, pinY, pinW, pinH);
                        KeepAuthoredSize(pinNode);
                    }

                    RefreshZoneLeader(parent, selected);
                }
            }

            return false;
        }

        /// <summary>
        /// Widens the live Farsi tab when a taller one is blocked, so the phrase is
        /// not clipped at 16:9. Size stays in 22–26.
        /// </summary>
        private static bool TryWidenLiveFaCue(TMP_Text cue, string source)
        {
            if (!TheatreSeatGeometry.ZoomCueSearchOpen)
                return false;
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return false;
            Rect origin = PlateRect(cue.rectTransform);
            string selected = CueSelectedMap(parent, origin.x);
            float[] widths = { 160f, 176f, 220f, 240f, 280f, 320f, 360f };
            float[] heights = { 56f, 62f, 76f, 72f, 68f, 64f, 48f, 44f, 40f };
            float[] lead = { 0f, -24f, -48f, -72f, -96f, -120f };
            for (float size = 26f; size >= 22f - 0.01f; size -= 1f)
            {
                for (int h = 0; h < heights.Length; h++)
                {
                    for (int w = 0; w < widths.Length; w++)
                    {
                        if (!TryPlaceCueBox(cue, parent, selected, widths[w], heights[h], origin))
                            continue;
                        Rect placed = PlateRect(cue.rectTransform);
                        for (int leadIndex = 0; leadIndex < lead.Length; leadIndex++)
                        {
                            float pair = size * 1.65f * 2f + lead[leadIndex] * size * 0.01f;
                            if (pair > placed.height + 2f)
                                continue;
                            cue.lineSpacing = lead[leadIndex];
                            cue.characterSpacing = 0f;
                            if (!CueLabelSplits(cue, source, size, placed.width, placed.height))
                                continue;
                            cue.ForceMeshUpdate();
                            float fitted = size * 1.65f * 2f + cue.lineSpacing * size * 0.01f;
                            if (fitted <= placed.height + 2f && !cue.isTextTruncated && !cue.isTextOverflowing)
                            {
                                HoldCueSize(cue, size);
                                ZoomCueFitReason = "fa-two-line";
                                LogCueAttempt(cue, "fa-two-line", size, true, "widened");
                                return true;
                            }
                        }

                        float[] track = { -12f, -36f, -72f, -108f };
                        for (int trackIndex = 0; trackIndex < track.Length; trackIndex++)
                        {
                            if (!FaCueLineFits(cue, source, size, placed.width, placed.height, track[trackIndex]))
                                continue;
                            HoldCueSize(cue, size);
                            ZoomCueFitReason = "fa-track";
                            return true;
                        }
                    }
                }
            }

            PlaceTopLeft(cue.rectTransform, origin.x, origin.y, origin.width, origin.height);
            KeepAuthoredSize(cue.rectTransform);
            SyncCuePlate(parent, origin);
            cue.text = source;
            return false;
        }

        /// <summary>
        /// Puts the tab on the card's top edge at the requested size, shifted to the
        /// side that still clears every district.
        /// </summary>
        private static bool TryPlaceCueBox(
            TMP_Text cue,
            RectTransform parent,
            string selected,
            float width,
            float height,
            Rect origin)
        {
            if (cue == null || parent == null || width < 32f || height < 32f)
                return false;
            if (string.IsNullOrEmpty(selected) || parent.Find("ZoneCard_" + selected) is not RectTransform card)
                return false;
            float cardX = card.anchoredPosition.x;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            if (width + 0.5f < cardW && width + 0.5f < 206f)
                return false;
            float frameW = parent.rect.width > 2f ? parent.rect.width : parent.sizeDelta.x;
            float xLo = cardX + cardW - width;
            float xHi = cardX;
            float bottom = origin.y + origin.height;
            float top = bottom - height;
            if (top < 1f)
                return false;
            float mid = cardX + (cardW - width) * 0.5f;
            float[] xs =
            {
                Mathf.Clamp(origin.x, 1f, Mathf.Max(1f, frameW - width - 1f)),
                mid, xLo, xHi,
                mid - 48f, mid + 48f, mid - 96f, mid + 96f, mid - 144f, mid + 144f
            };
            for (int i = 0; i < xs.Length; i++)
            {
                float x = Mathf.Clamp(xs[i], 1f, Mathf.Max(1f, frameW - width - 1f));
                var grown = new Rect(x, top, width, height);
                if (FaCueTabBlocked(parent, grown, selected) || FaCueHitsOtherCard(parent, grown, selected))
                    continue;
                PlaceTopLeft(cue.rectTransform, grown.x, grown.y, grown.width, grown.height);
                KeepAuthoredSize(cue.rectTransform);
                MatchCueRect(cue.rectTransform);
                SyncCuePlate(parent, grown);
                return true;
            }

            return false;
        }

        /// <summary>
        /// The seated tab can still meet Industrial Basin once it has been widened.
        /// Slide it, then the card, until the 26px stroke is clear.
        /// </summary>
        private static void SlideCueClearOfDistricts(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return;
            Rect spot = PlateRect(cue.rectTransform);
            string selected = CueSelectedMap(parent, spot.x);
            if (string.IsNullOrEmpty(selected) || !FaCueTabBlocked(parent, spot, selected))
                return;
            if (TryPlaceCueBox(cue, parent, selected, spot.width, spot.height, spot))
                return;
            if (parent.Find("ZoneCard_" + selected) is not RectTransform card)
                return;
            float cardX = card.anchoredPosition.x;
            float cardY = -card.anchoredPosition.y;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
            if (parent.Find("ZoneOutline_" + selected) is not RectTransform outline)
                return;
            Vector2[] points = OutlinePlatePoints(outline);
            if (points.Length < 3)
                return;
            var shape = new TheatreSeatGeometry.Vec2[points.Length];
            for (int i = 0; i < points.Length; i++)
                shape[i] = new TheatreSeatGeometry.Vec2(points[i].x, points[i].y);
            for (float drop = 4f; drop <= 40f; drop += 4f)
            {
                var moved = new TheatreSeatGeometry.Box(cardX, cardY + drop, cardW, cardH);
                if (!TheatreSeatGeometry.CardInsideTerritory(moved, shape))
                    continue;
                var trial = new Rect(spot.x, spot.y + drop, spot.width, spot.height);
                if (FaCueTabBlocked(parent, trial, selected))
                    continue;
                PlaceTopLeft(card, cardX, cardY + drop, cardW, cardH);
                KeepAuthoredSize(card);
                PlaceTopLeft(cue.rectTransform, trial.x, trial.y, trial.width, trial.height);
                KeepAuthoredSize(cue.rectTransform);
                SyncCuePlate(parent, trial);
                return;
            }
        }

        /// <summary>
        /// One Farsi line at a locked size. Auto-size stays off: turning it on marked
        /// every 22–26px size as overflowing on a 36px tab, and the 16px search then
        /// drew 17–18. A line that fits the width but not the height grows the tab.
        /// </summary>
        private static bool FaCueLineFits(TMP_Text cue, string source, float size, float boxW, float boxH, float spacing)
        {
            if (cue == null || size < 1f || boxW < 8f || boxH < 8f)
                return false;
            CommitCueMeasureBox(cue, boxW, boxH);
            PaintFaCueLine(cue, source, size, spacing);
            if (FaCueLineClears(cue, size, boxW, boxH))
            {
                LogCueAttempt(cue, "fa-track", size, true, "clear track=" + F(spacing));
                return true;
            }

            // Overflow against a rect that is still shorter than the seated tab is
            // the stale layout box, not the tab capture will measure.
            if (CueRectIsShort(cue.rectTransform, boxW, boxH))
            {
                CommitCueMeasureBox(cue, boxW, boxH);
                PaintFaCueLine(cue, source, size, spacing);
                if (FaCueLineClears(cue, size, boxW, boxH))
                {
                    LogCueAttempt(cue, "fa-track", size, true, "synced track=" + F(spacing));
                    return true;
                }
            }

            float line = MeasuredNameWidth(cue);
            string why = CueLineWhy(cue, size, boxW, boxH) + " track=" + F(spacing) + " preferred=" + F(line);
            if (line <= 0.5f || line > boxW + 2f)
            {
                LogCueAttempt(cue, "fa-track", size, false, why);
                return false;
            }

            if (GrowFaCueForLine(cue, source, size, spacing))
            {
                LogCueAttempt(cue, "fa-track", size, true, "grown track=" + F(spacing));
                return true;
            }

            LogCueAttempt(cue, "fa-track", size, false, why + " grow=0");
            return false;
        }

        /// <summary>
        /// Writes the box the fit is judging into the live rect. TMP decides
        /// truncation from that rect, so a check against sizeDelta alone still
        /// failed while the driver held the old width.
        /// </summary>
        private static void CommitCueMeasureBox(TMP_Text cue, float boxW, float boxH)
        {
            if (cue == null || cue.rectTransform == null)
                return;
            RectTransform rect = cue.rectTransform;
            bool widthOff = Mathf.Abs(rect.sizeDelta.x - boxW) > 0.5f || Mathf.Abs(rect.rect.width - boxW) > 0.75f;
            bool heightOff = Mathf.Abs(rect.sizeDelta.y - boxH) > 0.5f || Mathf.Abs(rect.rect.height - boxH) > 0.75f;
            if (widthOff || heightOff)
                PlaceTopLeft(rect, rect.anchoredPosition.x, -rect.anchoredPosition.y, boxW, boxH);
            MatchCueRect(rect);
        }

        private static bool CueRectIsShort(RectTransform rect, float boxW, float boxH)
        {
            if (rect == null)
                return false;
            return rect.rect.width + 0.75f < boxW || rect.rect.height + 0.75f < boxH;
        }

        private static string CueLineWhy(TMP_Text cue, float size, float boxW, float boxH)
        {
            if (cue == null)
                return "missing";
            float line = MeasuredNameWidth(cue);
            float stacked = cue.preferredHeight;
            if (line > boxW + 2f)
                return "width";
            if (stacked > boxH + 2f)
                return "height";
            if (size * 1.65f > boxH + 2f)
                return "em";
            if (cue.isTextTruncated)
                return "truncated";
            if (cue.isTextOverflowing)
                return "overflow";
            return "clear";
        }

        private static void PaintFaCueLine(TMP_Text cue, string source, float size, float spacing)
        {
            cue.enableAutoSizing = false;
            cue.lineSpacing = 0f;
            cue.characterSpacing = spacing;
            cue.fontSizeMin = size;
            cue.fontSizeMax = size;
            cue.fontSize = size;
            cue.maxVisibleLines = 1;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.text = source;
            cue.ForceMeshUpdate();
        }

        /// <summary>
        /// Noto Arabic's line box is about 1.65em, taller than the preferred height TMP
        /// reports. Both have to sit in the tab, and the mesh must not be truncated.
        /// </summary>
        private static bool FaCueLineClears(TMP_Text cue, float size, float boxW, float boxH)
        {
            float line = MeasuredNameWidth(cue);
            float stacked = cue.preferredHeight;
            bool stale = CueRectIsShort(cue.rectTransform, boxW, boxH);
            if (stale || stacked < 0.5f || line < 0.5f)
            {
                Vector2 againstBox = cue.GetPreferredValues(cue.text, boxW, boxH);
                if (line < 0.5f)
                    line = againstBox.x;
                if (stacked < 0.5f || stale)
                    stacked = againstBox.y > 0.5f ? againstBox.y : stacked;
            }
            bool geometry = line > 0.5f
                && line <= boxW + 2f
                && stacked <= boxH + 2f
                && size * 1.65f <= boxH + 2f;
            if (!geometry)
                return false;
            if (!cue.isTextTruncated && !cue.isTextOverflowing)
                return true;
            // Truncation is raised against the stale rect. The seated tab is the
            // box the capture pass measures once layout has caught up.
            return CueRectIsShort(cue.rectTransform, boxW, boxH);
        }

        /// <summary>
        /// Grows the tab upward, bottom still on the card, until the locked line fits.
        /// 38 crosses the stroke on the 16:9 Desert Base tab; 37 still clears it.
        /// </summary>
        private static bool GrowFaCueForLine(TMP_Text cue, string source, float size, float spacing)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return false;
            Rect spot = PlateRect(cue.rectTransform);
            if (spot.height >= 61.5f || spot.width < 8f)
                return false;
            string selected = CueSelectedMap(parent, spot.x);
            if (string.IsNullOrEmpty(selected))
            {
                LogCueAttempt(cue, "fa-grow", size, false, "no-card x=" + F(spot.x));
                return false;
            }
            float[] heights = { 62f, 60f, 58f, 56f, 52f, 48f, 44f, 40f, 38f, 37f };
            for (int i = 0; i < heights.Length; i++)
            {
                if (heights[i] <= spot.height + 0.5f)
                    continue;
                if (size * 1.65f > heights[i] + 2f)
                    continue;
                float top = spot.y + spot.height - heights[i];
                if (top < 1f)
                    continue;
                var grown = new Rect(spot.x, top, spot.width, heights[i]);
                if (FaCueTabBlocked(parent, grown, selected))
                {
                    LogCueAttempt(cue, "fa-grow", size, false, "blocked tab=" + F(grown.width) + "x" + F(grown.height));
                    continue;
                }
                PlaceTopLeft(cue.rectTransform, grown.x, grown.y, grown.width, grown.height);
                KeepAuthoredSize(cue.rectTransform);
                MatchCueRect(cue.rectTransform);
                SyncCuePlate(parent, grown);
                PaintFaCueLine(cue, source, size, spacing);
                if (FaCueLineClears(cue, size, grown.width, grown.height))
                    return true;
                LogCueAttempt(cue, "fa-grow", size, false,
                    CueLineWhy(cue, size, grown.width, grown.height) + " tab=" + F(grown.width) + "x" + F(grown.height));
                PlaceTopLeft(cue.rectTransform, spot.x, spot.y, spot.width, spot.height);
                KeepAuthoredSize(cue.rectTransform);
                SyncCuePlate(parent, spot);
                PaintFaCueLine(cue, source, size, spacing);
            }

            return false;
        }

        /// <summary>
        /// Grows the tab upward until a two-line Farsi pair at 22–26 sits in it.
        /// The bottom stays on the card. A 22px pair at leading 0 is about 73px,
        /// so 62 is not the last height when the district still clears.
        /// </summary>
        private static bool GrowFaCueForPair(TMP_Text cue, string source, float size, float spacing, float pair)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return false;
            Rect spot = PlateRect(cue.rectTransform);
            if (spot.width < 8f || pair <= spot.height + 2f)
                return false;
            string selected = CueSelectedMap(parent, spot.x);
            if (string.IsNullOrEmpty(selected))
            {
                LogCueAttempt(cue, "fa-grow", size, false, "no-card x=" + F(spot.x));
                return false;
            }

            float[] heights = { 76f, 72f, 68f, 64f, 62f, 60f, 58f, 56f, 52f, 48f, 44f, 40f };
            for (int i = 0; i < heights.Length; i++)
            {
                if (heights[i] + 2f < pair || heights[i] <= spot.height + 0.5f)
                    continue;
                float top = spot.y + spot.height - heights[i];
                if (top < 1f)
                    continue;
                var grown = new Rect(spot.x, top, spot.width, heights[i]);
                if (FaCueTabBlocked(parent, grown, selected) || FaCueHitsOtherCard(parent, grown, selected))
                {
                    LogCueAttempt(cue, "fa-grow", size, false, "blocked tab=" + F(grown.width) + "x" + F(grown.height));
                    continue;
                }

                PlaceTopLeft(cue.rectTransform, grown.x, grown.y, grown.width, grown.height);
                KeepAuthoredSize(cue.rectTransform);
                MatchCueRect(cue.rectTransform);
                SyncCuePlate(parent, grown);
                cue.lineSpacing = spacing;
                if (!CueLabelSplits(cue, source, size, grown.width, grown.height))
                {
                    PlaceTopLeft(cue.rectTransform, spot.x, spot.y, spot.width, spot.height);
                    KeepAuthoredSize(cue.rectTransform);
                    SyncCuePlate(parent, spot);
                    cue.text = source;
                    continue;
                }

                cue.ForceMeshUpdate();
                float fitted = size * 1.65f * 2f + cue.lineSpacing * size * 0.01f;
                if (fitted <= grown.height + 2f && !cue.isTextTruncated && !cue.isTextOverflowing)
                    return true;
                LogCueAttempt(cue, "fa-grow", size, false, "pair=" + F(fitted) + " tab=" + F(grown.width) + "x" + F(grown.height));
                PlaceTopLeft(cue.rectTransform, spot.x, spot.y, spot.width, spot.height);
                KeepAuthoredSize(cue.rectTransform);
                SyncCuePlate(parent, spot);
                cue.text = source;
                cue.maxVisibleLines = 1;
            }

            return false;
        }

        private static string CueSelectedMap(RectTransform parent, float cueX)
        {
            if (parent == null)
                return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || child is not RectTransform zone)
                    continue;
                if (Mathf.Abs(zone.anchoredPosition.x - cueX) > 2f)
                    continue;
                return child.name.Substring("ZoneCard_".Length);
            }

            return null;
        }

        /// <summary>
        /// Capture compares preferred width with the cue rect. Shrink to the cue
        /// floor first (16 English, 22 Farsi). A line that still overflows grows
        /// the box, then the pill is seated inside the card or off every hex.
        /// </summary>
        private static void FitCueInkToBox(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return;
            string source = FlattenCopy(cue.text);
            if (source.Length == 0)
                return;
            bool farsi = CueCopyIsFarsi(cue, source);
            float floor = farsi ? 22f : 16f;
            RectTransform rect = cue.rectTransform;
            float boxW = rect.rect.width > 1f ? rect.rect.width : rect.sizeDelta.x;
            float boxH = rect.rect.height > 1f ? rect.rect.height : rect.sizeDelta.y;
            if (boxW < 8f || boxH < 8f)
                return;
            float size = cue.fontSize > 1f ? cue.fontSize : floor;
            if (size < floor)
                size = floor;
            for (int guard = 0; guard < 12 && size > floor + 0.01f; guard++)
            {
                HoldCueSize(cue, size);
                cue.characterSpacing = 0f;
                cue.text = source.IndexOf('\n') >= 0 ? cue.text : source;
                cue.textWrappingMode = TextWrappingModes.NoWrap;
                cue.overflowMode = TextOverflowModes.Overflow;
                cue.ForceMeshUpdate();
                float ink = MeasuredNameWidth(cue);
                if (ink < 0.5f)
                    ink = cue.preferredWidth;
                if (ink <= boxW + 2f && cue.preferredHeight <= boxH + 2f)
                    break;
                size -= 1f;
            }

            HoldCueSize(cue, Mathf.Max(floor, size));
            cue.characterSpacing = 0f;
            cue.ForceMeshUpdate();
            float line = MeasuredNameWidth(cue);
            if (line < 0.5f)
                line = cue.preferredWidth;
            float needed = line > boxW + 2f ? Mathf.Ceil(line + 4f) : boxW;
            SeatCueInkOffHexes(cue, needed);
        }

        /// <summary>
        /// Moves the fitted pill inside the selected card, or to a spot whose 26px
        /// stroke misses every coast, and keeps it on the map.
        /// </summary>
        private static void SeatCueInkOffHexes(TMP_Text cue, float neededWidth)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return;
            Rect spot = PlateRect(cue.rectTransform);
            float frameWidth = parent.rect.width > 2f ? parent.rect.width : parent.sizeDelta.x;
            float frameHeight = parent.rect.height > 2f ? parent.rect.height : parent.sizeDelta.y;
            if (neededWidth > spot.width)
                spot.width = neededWidth;
            if (frameWidth > 24f && spot.xMax > frameWidth - 12f)
                spot.x = Mathf.Max(12f, frameWidth - 12f - spot.width);
            if (frameHeight > 24f && spot.yMax > frameHeight - 12f)
                spot.y = Mathf.Max(12f, frameHeight - 12f - spot.height);
            string selected = CueSelectedMap(parent, spot.x);
            if (string.IsNullOrEmpty(selected))
            {
                float best = float.MaxValue;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || child is not RectTransform zone)
                        continue;
                    float dx = zone.anchoredPosition.x + zone.sizeDelta.x * 0.5f - (spot.x + spot.width * 0.5f);
                    float dy = -zone.anchoredPosition.y + zone.sizeDelta.y * 0.5f - (spot.y + spot.height * 0.5f);
                    float dist = dx * dx + dy * dy;
                    if (dist >= best)
                        continue;
                    best = dist;
                    selected = child.name.Substring("ZoneCard_".Length);
                }

                if (string.IsNullOrEmpty(selected))
                    return;
            }

            if (parent.Find("ZoneCard_" + selected) is not RectTransform card)
                return;
            float cardX = card.anchoredPosition.x;
            float cardY = -card.anchoredPosition.y;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
            var cardBox = new Rect(cardX, cardY, cardW, cardH);
            bool inside = spot.x >= cardBox.x - 1.5f && spot.y >= cardBox.y - 1.5f
                && spot.xMax <= cardBox.xMax + 1.5f && spot.yMax <= cardBox.yMax + 1.5f;
            bool onMap = spot.x >= 12f && spot.y >= 12f && spot.xMax <= frameWidth - 12f && spot.yMax <= frameHeight - 12f;
            if (inside && onMap)
            {
                CommitCueSpot(cue, parent, spot);
                return;
            }

            if (!FaCueTabBlocked(parent, spot, selected) && onMap)
            {
                CommitCueSpot(cue, parent, spot);
                return;
            }

            float bandW = Mathf.Min(spot.width, Mathf.Max(36f, cardW - 4f));
            float bandH = Mathf.Min(spot.height, Mathf.Max(36f, cardH * 0.42f));
            if (bandH > cardH - 8f)
                bandH = Mathf.Max(36f, cardH - 8f);
            float bandX = cardX + (cardW - bandW) * 0.5f;
            float bandY = cardY + 6f;
            var band = new Rect(bandX, bandY, bandW, bandH);
            bool bandInside = band.x >= cardBox.x - 1.5f && band.y >= cardBox.y - 1.5f
                && band.xMax <= cardBox.xMax + 1.5f && band.yMax <= cardBox.yMax + 1.5f;
            if (bandInside && band.width >= 36f && band.width + 1f >= neededWidth)
            {
                CommitCueSpot(cue, parent, band);
                return;
            }

            if (parent.Find("ZoneOutline_" + selected) is RectTransform outline)
            {
                Vector2[] points = OutlinePlatePoints(outline);
                if (points.Length >= 3)
                {
                    float minY = points[0].y;
                    float maxY = points[0].y;
                    for (int i = 1; i < points.Length; i++)
                    {
                        if (points[i].y < minY)
                            minY = points[i].y;
                        if (points[i].y > maxY)
                            maxY = points[i].y;
                    }

                    float reach = TheatreSeatGeometry.CueHexStroke + 3f;
                    float[] tops = { minY - reach - spot.height, maxY + reach, cardY - spot.height };
                    for (int i = 0; i < tops.Length; i++)
                    {
                        float x = Mathf.Clamp(cardX + (cardW - spot.width) * 0.5f, 12f, Mathf.Max(12f, frameWidth - 12f - spot.width));
                        float y = Mathf.Clamp(tops[i], 12f, Mathf.Max(12f, frameHeight - 12f - spot.height));
                        var trial = new Rect(x, y, spot.width, spot.height);
                        if (FaCueTabBlocked(parent, trial, selected) || FaCueHitsOtherCard(parent, trial, selected))
                            continue;
                        CommitCueSpot(cue, parent, trial);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// The ink width has to land on the ZoomCue rect. Growing only the plate
        /// leaves capture measuring the old 128 or 160 box.
        /// </summary>
        private static void CommitCueSpot(TMP_Text cue, RectTransform parent, Rect spot)
        {
            if (cue == null || cue.rectTransform == null || spot.width < 8f || spot.height < 8f)
                return;
            PlaceTopLeft(cue.rectTransform, spot.x, spot.y, spot.width, spot.height);
            KeepAuthoredSize(cue.rectTransform);
            MatchCueRect(cue.rectTransform);
            SyncCuePlate(parent, spot);
        }

        private static void HoldCueSize(TMP_Text cue, float size)
        {
            V3LocalizedTextBindingView binding = cue.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
            cue.enableAutoSizing = false;
            cue.fontSizeMin = size;
            cue.fontSizeMax = size;
            cue.fontSize = size;
        }

        private static void ApplyCueFloor(TMP_Text cue, string source, float size, float boxW, float boxH)
        {
            V3LocalizedTextBindingView binding = cue.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
            cue.enableAutoSizing = false;
            cue.fontSizeMin = size;
            cue.fontSizeMax = size;
            cue.fontSize = size;
            cue.characterSpacing = -12f;
            cue.lineSpacing = 0f;
            cue.maxVisibleLines = 1;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            // The old call passed the width as the height, so a two-line split was
            // accepted against a 220px "box" and then drawn in a 44px tab.
            float pair = size * 1.65f * 2f;
            if (boxH + 2f >= pair && CueLabelSplits(cue, source, size, boxW, boxH))
                cue.maxVisibleLines = 2;
            else
            {
                cue.text = source;
                cue.maxVisibleLines = 1;
                cue.textWrappingMode = TextWrappingModes.NoWrap;
            }

            cue.ForceMeshUpdate();
        }

        /// <summary>
        /// Shrink a one-line label until its mesh width and height sit inside the rect.
        /// Capture measures the mesh line, which can be wider than preferredWidth.
        /// </summary>
        private static void FitCopyInside(TMP_Text text, float floor)
        {
            if (text == null || text.rectTransform == null)
                return;
            float boxW = text.rectTransform.rect.width > 1f ? text.rectTransform.rect.width : text.rectTransform.sizeDelta.x;
            float boxH = text.rectTransform.rect.height > 1f ? text.rectTransform.rect.height : text.rectTransform.sizeDelta.y;
            if (boxW < 8f || boxH < 8f)
                return;
            string source = FlattenCopy(text.text);
            if (source.Length == 0)
                return;
            float start = text.fontSizeMax > 1f ? text.fontSizeMax : text.fontSize;
            if (start < floor)
                start = floor;
            var binding = text.GetComponent<V3LocalizedTextBindingView>();
            for (float size = start; size >= floor - 0.01f; size -= 1f)
            {
                if (binding != null)
                    binding.ConfigureFontBounds(size, size);
                text.enableAutoSizing = true;
                text.fontSizeMin = size;
                text.fontSizeMax = size;
                text.fontSize = size;
                text.text = source;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.maxVisibleLines = 1;
                text.ForceMeshUpdate();
                float line = MeasuredNameWidth(text);
                float height = text.preferredHeight;
                if (height < 0.5f)
                    height = text.GetPreferredValues(source).y;
                if (line > 0.5f && line <= boxW + 2f && height <= boxH + 2f && !text.isTextTruncated && !text.isTextOverflowing)
                    return;
            }
        }

        private static bool CueLabelSplits(TMP_Text cue, string source, float size, float boxW, float boxH)
        {
            int split = -1;
            float bestGap = float.MaxValue;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != ' ')
                    continue;
                string left = source.Substring(0, i + 1);
                string right = source.Substring(i + 1).TrimStart();
                if (right.Length == 0)
                    continue;
                float leftWidth = ZoneNameLineWidth(cue, left, size);
                float rightWidth = ZoneNameLineWidth(cue, right, size);
                if (leftWidth > boxW + 1.5f || rightWidth > boxW + 1.5f)
                    continue;
                float gap = Mathf.Abs(leftWidth - rightWidth);
                if (gap >= bestGap)
                    continue;
                bestGap = gap;
                split = i;
            }

            if (split < 0)
                return false;
            cue.text = source.Substring(0, split + 1) + "\n" + source.Substring(split + 1).TrimStart();
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.maxVisibleLines = 2;
            cue.fontSizeMin = size;
            cue.fontSizeMax = size;
            cue.fontSize = size;
            cue.ForceMeshUpdate();
            float needed = MeasuredNameHeight(cue);
            float line = MeasuredNameWidth(cue);
            if (needed > boxH + 1.5f || line > boxW + 1.5f)
            {
                cue.text = source;
                cue.maxVisibleLines = 1;
                cue.ForceMeshUpdate();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Grows a short Farsi tab upward, bottom still flush with the card, when the
        /// district stroke still clears. The name under the card does not move.
        /// </summary>
        private static Rect RaiseFaCueTab(RectTransform parent, Rect spot, string selectedId)
        {
            if (parent == null || spot.height >= 61.5f || spot.height < 8f)
                return spot;
            float[] heights = { 62f, 60f, 58f, 56f, 52f, 48f, 44f, 40f, 38f, 37f };
            for (int i = 0; i < heights.Length; i++)
            {
                if (heights[i] <= spot.height + 0.5f)
                    continue;
                float top = spot.y + spot.height - heights[i];
                if (top < 1f)
                    continue;
                var grown = new Rect(spot.x, top, spot.width, heights[i]);
                // The selected district fails on the stroke edge, the same test capture uses.
                // An interior overlap was rejecting a tab the cue rule still allows.
                if (FaCueTabBlocked(parent, grown, selectedId))
                    continue;
                return grown;
            }

            return spot;
        }

        private static bool FaCueTabBlocked(RectTransform parent, Rect spot, string selectedId)
        {
            Rect inflated = InflateRect(spot, TheatreSeatGeometry.CueHexStroke);
            string own = string.IsNullOrEmpty(selectedId) ? null : "ZoneOutline_" + selectedId;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneOutline_", StringComparison.Ordinal) || child is not RectTransform outline)
                    continue;
                if (!outline.gameObject.activeInHierarchy)
                    continue;
                Vector2[] shape = OutlinePlatePoints(outline);
                if (shape.Length < 3)
                    continue;
                bool selected = own != null && string.Equals(child.name, own, StringComparison.Ordinal);
                if (selected ? EdgesHitRect(shape, inflated) : RectHitsPolygon(inflated, shape))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The pill may sit on the selected card. Any other zone card, including its
        /// tab, stays clear. A shared edge is not an overlap.
        /// </summary>
        private static bool FaCueHitsOtherCard(RectTransform parent, Rect spot, string selectedId)
        {
            if (parent == null)
                return false;
            string own = string.IsNullOrEmpty(selectedId) ? null : "ZoneCard_" + selectedId;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || child is not RectTransform card)
                    continue;
                if (!card.gameObject.activeInHierarchy)
                    continue;
                if (own != null && string.Equals(child.name, own, StringComparison.Ordinal))
                    continue;
                float cardX = card.anchoredPosition.x;
                float cardY = -card.anchoredPosition.y;
                float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
                float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
                var box = new Rect(cardX, cardY, cardW, cardH);
                if (Overlaps(spot, box, 1f))
                    return true;
            }

            return false;
        }

        private static bool EdgesHitRect(Vector2[] shape, Rect rect)
        {
            for (int i = 0; i < shape.Length; i++)
            {
                if (SegmentHitsRect(shape[i], shape[(i + 1) % shape.Length], rect))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Puts the zoom cue on the selected card. The same attachment the seat harness uses,
        /// so a full-width strip or a tab hung off the card stays inside the near-pin rule.
        /// </summary>
        private static bool SeatZoomCueClearOfHexes(TMP_Text cue, bool fa, string selectedId)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return false;
            if (string.IsNullOrEmpty(selectedId))
                selectedId = "DB";
            float frameW = parent.rect.width > 2f ? parent.rect.width : parent.sizeDelta.x;
            float frameH = parent.rect.height > 2f ? parent.rect.height : parent.sizeDelta.y;
            if (frameW < 32f || frameH < 32f)
                return false;
            string[] ids = TheatreSeatGeometry.MapIds;
            var cards = new TheatreSeatGeometry.Box[ids.Length];
            var pins = new TheatreSeatGeometry.Box[ids.Length];
            var hexes = new TheatreSeatGeometry.Vec2[ids.Length][];
            int selected = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], selectedId, StringComparison.OrdinalIgnoreCase))
                    selected = i;
                if (parent.Find("ZoneCard_" + ids[i]) is RectTransform cardNode)
                {
                    cards[i] = new TheatreSeatGeometry.Box(
                        cardNode.anchoredPosition.x,
                        -cardNode.anchoredPosition.y,
                        cardNode.sizeDelta.x,
                        cardNode.sizeDelta.y);
                }

                if (parent.Find("ZonePin_" + ids[i]) is RectTransform pinNode)
                {
                    float pinW = pinNode.sizeDelta.x > 2f ? pinNode.sizeDelta.x : ZonePinSize;
                    float pinH = pinNode.sizeDelta.y > 2f ? pinNode.sizeDelta.y : ZonePinSize;
                    pins[i] = new TheatreSeatGeometry.Box(
                        pinNode.anchoredPosition.x,
                        -pinNode.anchoredPosition.y,
                        pinW,
                        pinH);
                }

                if (parent.Find("ZoneOutline_" + ids[i]) is RectTransform outline)
                {
                    Vector2[] points = OutlinePlatePoints(outline);
                    var shape = new TheatreSeatGeometry.Vec2[points.Length];
                    for (int p = 0; p < points.Length; p++)
                        shape[p] = new TheatreSeatGeometry.Vec2(points[p].x, points[p].y);
                    hexes[i] = shape;
                }
            }

            Rect detailRect = SeatingDetailRect(frameW, frameH, fa);
            var detail = new TheatreSeatGeometry.Box(detailRect.x, detailRect.y, detailRect.width, detailRect.height);
            Rect liveCue = PlateRect(cue.rectTransform);
            var liveBox = new TheatreSeatGeometry.Box(liveCue.x, liveCue.y, liveCue.width, liveCue.height);
            if (TheatreSeatGeometry.ParkedCueHolds(
                liveBox, cards[selected], hexes, pins, cards, selected, detail, frameW, frameH, fa))
            {
                FitCueInsidePackedRect(cue);
                LockCueFont(cue);
                SyncCuePlate(parent, PlateRect(cue.rectTransform));
                LogTheatreCue(cue);
                return true;
            }

            var cardSnapshot = new TheatreSeatGeometry.Box[cards.Length];
            for (int i = 0; i < cards.Length; i++)
                cardSnapshot[i] = cards[i];
            float cardXBefore = cards[selected].X;
            float cardYBefore = cards[selected].Y;
            float cardHBefore = cards[selected].H;
            bool attached = fa
                ? TheatreSeatGeometry.TryAttachFarsiCue(frameW, frameH, pins, cards, hexes, detail, selected, out TheatreSeatGeometry.Box seated)
                    || TheatreSeatGeometry.TryAttachCueToCard(frameW, frameH, pins, cards, hexes, detail, selected, out seated)
                : TheatreSeatGeometry.TryAttachCueToCard(frameW, frameH, pins, cards, hexes, detail, selected, out seated);
            if (attached)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (hexes[i] == null || cards[i].W < 1f)
                        continue;
                    if (TheatreSeatGeometry.CardInsideTerritory(cards[i], hexes[i]))
                        continue;
                    cards[i] = cardSnapshot[i];
                }

                if (!TheatreSeatGeometry.ParkedCueHolds(
                    seated, cards[selected], hexes, pins, cards, selected, detail, frameW, frameH, fa)
                    && TheatreSeatGeometry.ParkedCueHolds(
                        liveBox, cards[selected], hexes, pins, cards, selected, detail, frameW, frameH, fa))
                {
                    seated = liveBox;
                    for (int i = 0; i < cards.Length; i++)
                        cards[i] = cardSnapshot[i];
                }
            }

            if (!attached)
                return false;
            if ((Mathf.Abs(cards[selected].X - cardXBefore) > 0.5f
                    || Mathf.Abs(cards[selected].Y - cardYBefore) > 0.5f
                    || Mathf.Abs(cards[selected].H - cardHBefore) > 0.5f)
                && parent.Find("ZoneCard_" + ids[selected]) is RectTransform movedCard)
            {
                PlaceTopLeft(movedCard, cards[selected].X, cards[selected].Y, cards[selected].W, cards[selected].H);
                KeepAuthoredSize(movedCard);
                RefreshZoneLeader(parent, ids[selected]);
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (i == selected || parent.Find("ZoneCard_" + ids[i]) is not RectTransform other)
                    continue;
                if (Mathf.Abs(other.anchoredPosition.x - cards[i].X) <= 0.5f
                    && Mathf.Abs(-other.anchoredPosition.y - cards[i].Y) <= 0.5f)
                    continue;
                PlaceTopLeft(other, cards[i].X, cards[i].Y, cards[i].W, cards[i].H);
                KeepAuthoredSize(other);
                RefreshZoneLeader(parent, ids[i]);
            }

            var spot = new Rect(seated.X, seated.Y, seated.W, seated.H);
            PlaceTopLeft(cue.rectTransform, spot.x, spot.y, spot.width, spot.height);
            KeepAuthoredSize(cue.rectTransform);
            SyncCuePlate(parent, spot);
            if (parent.Find("CuePlate") is RectTransform plate)
                KeepAuthoredSize(plate);
            FitCueInsidePackedRect(cue);
            LockCueFont(cue);
            Rect lived = PlateRect(cue.rectTransform);
            float spotBottom = spot.y + spot.height;
            float livedBottom = lived.y + lived.height;
            bool faTrack = string.Equals(ZoomCueFitReason, "fa-track", StringComparison.Ordinal)
                && lived.height + 0.5f >= 56f
                && lived.width + 0.5f >= 160f;
            bool flush = Mathf.Abs(livedBottom - spotBottom) <= 1.5f
                && lived.x <= spot.x + 1.5f
                && lived.xMax >= spot.xMax - 1.5f
                && lived.height <= 78f
                && lived.height + 0.5f >= Mathf.Min(spot.height, 36f)
                && lived.width + 0.5f >= spot.width
                && lived.width <= 360.5f;
            Rect kept = faTrack || flush ? lived : spot;
            PlaceTopLeft(cue.rectTransform, kept.x, kept.y, kept.width, kept.height);
            SyncCuePlate(parent, kept);
            LogTheatreCue(cue);
            return true;
        }

        /// <summary>
        /// The seat search missed. Still paint a full-width strip on the bottom of the portrait,
        /// above the count row, instead of the old squeezed pill.
        /// </summary>
        private static bool PlaceBottomCueStrip(TMP_Text cue, string selectedId)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return false;
            if (string.IsNullOrEmpty(selectedId))
                selectedId = "DB";
            if (parent.Find("ZoneCard_" + selectedId) is not RectTransform card)
                return false;
            float cardX = card.anchoredPosition.x;
            float cardY = -card.anchoredPosition.y;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
            if (cardW < 48f || cardH < 48f)
                return false;
            float statusH = cardH >= 190f ? 40f : 34f;
            float stripH = 44f;
            float room = cardH - statusH - 8f;
            if (stripH > room)
                stripH = room;
            if (stripH > 52f)
                stripH = 52f;
            if (stripH < 36f)
                stripH = 36f;
            float stripW = Mathf.Max(32f, cardW - 4f);
            float stripY = cardY + cardH - statusH - 2f - stripH;
            if (stripY < cardY + 2f)
                stripY = cardY + 2f;
            var spot = new Rect(cardX + 2f, stripY, stripW, stripH);
            PlaceTopLeft(cue.rectTransform, spot.x, spot.y, spot.width, spot.height);
            SyncCuePlate(parent, spot);
            return true;
        }

        /// <summary>
        /// Keeps the fitted 16–26px cue size when the localization binding refreshes.
        /// The section frame scales the whole library (about 0.86 on a 4:3 canvas),
        /// so the local size is raised until the drawn pixels are at least 16.
        /// Autosize stays off: a later LibraryPass or fitter was settling the Farsi
        /// cue at a 12px floor.
        /// </summary>
        private static void LockCueFont(TMP_Text cue)
        {
            if (cue == null)
                return;
            // A snap back to the short seat used to keep the two-line newline.
            // Fit again before the lock, and do not leave the second line clipped.
            if (cue.text != null && cue.text.IndexOf('\n') >= 0 && cue.rectTransform != null)
            {
                cue.ForceMeshUpdate();
                float boxW = cue.rectTransform.rect.width > 1f ? cue.rectTransform.rect.width : cue.rectTransform.sizeDelta.x;
                float boxH = cue.rectTransform.rect.height > 1f ? cue.rectTransform.rect.height : cue.rectTransform.sizeDelta.y;
                bool clipped = cue.isTextTruncated || cue.isTextOverflowing || cue.preferredHeight > boxH + 1.5f;
                if (clipped && CueCopyIsFarsi(cue, FlattenCopy(cue.text)))
                    FitCueInsidePackedRect(cue);
            }

            float size = CueLocalSize(cue);
            string drawn = cue.text;
            int lines = cue.maxVisibleLines;
            float spacing = cue.characterSpacing;
            float leading = cue.lineSpacing;
            V3LocalizedTextBindingView binding = cue.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
            if (cue.rectTransform != null)
                KeepAuthoredSize(cue.rectTransform);
            cue.enableAutoSizing = false;
            cue.fontSizeMin = size;
            cue.fontSizeMax = size;
            cue.fontSize = size;
            cue.color = CueInkColor();
            cue.text = drawn;
            cue.maxVisibleLines = lines > 0 ? lines : 2;
            cue.characterSpacing = spacing;
            cue.lineSpacing = leading;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.alignment = TextAlignmentOptions.Center;
            cue.ForceMeshUpdate();
        }

        /// <summary>Local font size whose drawn height is at least 16px and at most 26px.</summary>
        private static float CueLocalSize(TMP_Text cue)
        {
            float size = cue != null ? cue.fontSize : 16f;
            if (size < 16f)
                size = 16f;
            if (size > 26f)
                size = 26f;
            float scale = 1f;
            if (cue != null && cue.rectTransform != null)
                scale = cue.rectTransform.lossyScale.y;
            if (scale < 0.05f || scale > 4f)
                scale = 1f;
            if (scale < 0.98f)
            {
                float needed = 16f / scale;
                if (needed > size)
                    size = needed;
            }

            if (size < 16f)
                size = 16f;
            if (size > 26f)
                size = 26f;
            return size;
        }

        /// <summary>
        /// Last word after LibraryPass, a layout group, or the localization fitter.
        /// Capture reads the cue inside the layout pass; this runs again at the end of the frame.
        /// </summary>
        public static void HoldZoomCuePaint(TMP_Text cue)
        {
            if (cue == null || cue.name != "ZoomCue")
                return;
            LockCueFont(cue);
            FitCueInkToBox(cue);
            LiftCueClearOfTitle(cue);
            AttachZoomPointer(cue.rectTransform);
            if (cue.transform.parent is not RectTransform parent)
                return;
            float cueX = cue.rectTransform.anchoredPosition.x;
            string selected = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || child is not RectTransform zone)
                    continue;
                if (Mathf.Abs(zone.anchoredPosition.x - cueX) > 2f)
                    continue;
                selected = child.name.Substring("ZoneCard_".Length);
                break;
            }

            if (!string.IsNullOrEmpty(selected))
                DropNameBelowCue(cue, selected);
        }

        private static void LogTheatreCue(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null)
                return;
            RectTransform rect = cue.rectTransform;
            float x = rect.anchoredPosition.x;
            float y = -rect.anchoredPosition.y;
            float w = rect.sizeDelta.x;
            float h = rect.sizeDelta.y;
            Debug.Log("[TheatreCue] x=" + F(x)
                + " y=" + F(y)
                + " w=" + F(w)
                + " h=" + F(h)
                + " font=" + F(cue.fontSize));
        }

        /// <summary>
        /// Keep 6px between the cue and the name ink on every side. The cue stays at the
        /// card's x and width; only the name moves when the ink is still inside that margin.
        /// </summary>
        private static void DropNameBelowCue(TMP_Text cue, string selectedId)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return;
            if (string.IsNullOrEmpty(selectedId))
                return;
            if (parent.Find("ZoneCard_" + selectedId) is not RectTransform card)
                return;
            if (card.Find("ZoneName") is not RectTransform name)
                return;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
            float nameX = name.anchoredPosition.x;
            float nameY = -name.anchoredPosition.y;
            float nameW = name.sizeDelta.x;
            float nameH = name.sizeDelta.y;
            float cardY = -card.anchoredPosition.y;
            Rect cueBox = PlateRect(cue.rectTransform);
            TMP_Text nameText = name.GetComponent<TMP_Text>();
            string title = nameText != null ? nameText.text : string.Empty;
            bool wide = parent.rect.height > 2f && parent.rect.width / parent.rect.height >= 2.4f;
            float nameClearance = TheatreSeatGeometry.NameClearanceBelowCue(title, cardW, cardH, wide);
            float needed = TheatreSeatGeometry.WrappedFloorNameHeight(title, cardW);
            float nameCap = TheatreSeatGeometry.MaxNameBoxHeight(cardH, wide);
            if (nameH + 0.5f < needed && needed <= nameCap + 1f)
            {
                nameH = Mathf.Min(nameCap, Mathf.Max(nameH, needed));
                PlaceTopLeft(name, nameX, nameY, nameW, nameH);
            }
            float statusH = cardH >= 190f ? 40f : 34f;
            float limit = cardH - 2f - statusH - 4f - nameH;
            float nameGap = (cardY + nameY) - cueBox.yMax;
            if (nameGap < nameClearance)
            {
                float next = nameY + (nameClearance - nameGap);
                if (next > limit)
                    next = limit;
                if (next > nameY + 0.5f)
                {
                    PlaceTopLeft(name, nameX, next, nameW, nameH);
                    nameY = next;
                }
            }

            if (!TryPlateInk(nameText, card, out Rect ink))
                return;
            Rect cueRect = PlateRect(cue.rectTransform);
            // Capture still rejects a gap under 6px. The layout leaves a wider band
            // so the zoom label does not read as a second line of the name.
            if (AxisGap(cueRect.xMin, cueRect.xMax, ink.xMin, ink.xMax) < nameClearance)
            {
                float cueMidY = cueRect.y + cueRect.height * 0.5f;
                float inkMidY = ink.y + ink.height * 0.5f;
                if (cueMidY <= inkMidY)
                {
                    float push = cueRect.yMax + nameClearance - ink.yMin;
                    if (push > 0.5f)
                    {
                        float next = Mathf.Min(limit, nameY + push);
                        if (next > nameY + 0.5f)
                        {
                            PlaceTopLeft(name, nameX, next, nameW, nameH);
                            nameY = next;
                        }
                    }
                }
            }

            if (!TryPlateInk(nameText, card, out ink))
                return;
            cueRect = PlateRect(cue.rectTransform);
            if (AxisGap(cueRect.yMin, cueRect.yMax, ink.yMin, ink.yMax) < nameClearance
                && AxisGap(cueRect.xMin, cueRect.xMax, ink.xMin, ink.xMax) < nameClearance)
            {
                float cueMidX = cueRect.x + cueRect.width * 0.5f;
                float inkMidX = ink.x + ink.width * 0.5f;
                float push = cueMidX >= inkMidX
                    ? ink.xMax - (cueRect.xMin - nameClearance)
                    : (cueRect.xMax + nameClearance) - ink.xMin;
                if (push > 0.5f)
                {
                    float nextX = cueMidX >= inkMidX ? nameX - push : nameX + push;
                    float minX = 1f;
                    float maxX = Mathf.Max(minX, cardW - nameW - 1f);
                    if (nextX < minX)
                        nextX = minX;
                    if (nextX > maxX)
                        nextX = maxX;
                    if (Mathf.Abs(nextX - nameX) > 0.5f)
                    {
                        PlaceTopLeft(name, nextX, nameY, nameW, nameH);
                        nameX = nextX;
                    }
                }
            }

            PlaceNameBand(card, nameX, nameY, nameW, nameH, cardW, cardH);
        }

        private static bool ForeignHexHits(RectTransform parent, Rect spot, string selectedId)
        {
            if (parent == null)
                return false;
            Rect grown = InflateRect(spot, TheatreSeatGeometry.CueHexStroke);
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneOutline_", StringComparison.Ordinal) || child is not RectTransform outline)
                    continue;
                if (!outline.gameObject.activeInHierarchy)
                    continue;
                if (child.name.EndsWith("_" + selectedId, StringComparison.Ordinal))
                    continue;
                Vector2[] shape = OutlinePlatePoints(outline);
                if (shape.Length >= 3 && RectHitsPolygon(grown, shape))
                    return true;
            }

            return false;
        }

        private static Rect PlateRect(RectTransform rect)
        {
            float y = -rect.anchoredPosition.y;
            return new Rect(rect.anchoredPosition.x, y, rect.sizeDelta.x, rect.sizeDelta.y);
        }

        /// <summary>Glyph bounds in the card's parent, y down. Falls back to the name rect.</summary>
        private static bool TryPlateInk(TMP_Text name, RectTransform card, out Rect ink)
        {
            ink = default;
            if (name == null || card == null || name.rectTransform == null)
                return false;
            float cardX = card.anchoredPosition.x;
            float cardY = -card.anchoredPosition.y;
            float nameX = name.rectTransform.anchoredPosition.x;
            float nameY = -name.rectTransform.anchoredPosition.y;
            float nameW = name.rectTransform.sizeDelta.x;
            float nameH = name.rectTransform.sizeDelta.y;
            name.ForceMeshUpdate();
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            bool any = false;
            TMP_TextInfo info = name.textInfo;
            if (info != null && info.characterCount > 0)
            {
                int count = info.characterCount;
                for (int i = 0; i < count; i++)
                {
                    TMP_CharacterInfo glyph = info.characterInfo[i];
                    if (!glyph.isVisible)
                        continue;
                    any = true;
                    IncludeInkCorner(glyph.bottomLeft, nameX, nameY, ref minX, ref minY, ref maxX, ref maxY);
                    IncludeInkCorner(glyph.topLeft, nameX, nameY, ref minX, ref minY, ref maxX, ref maxY);
                    IncludeInkCorner(glyph.topRight, nameX, nameY, ref minX, ref minY, ref maxX, ref maxY);
                    IncludeInkCorner(glyph.bottomRight, nameX, nameY, ref minX, ref minY, ref maxX, ref maxY);
                }
            }

            if (!any || maxX <= minX || maxY <= minY)
            {
                ink = new Rect(cardX + nameX, cardY + nameY, nameW, nameH);
                return ink.width > 1f && ink.height > 1f;
            }

            ink = Rect.MinMaxRect(cardX + minX, cardY + minY, cardX + maxX, cardY + maxY);
            return ink.width > 0.5f && ink.height > 0.5f;
        }

        private static void IncludeInkCorner(Vector3 local, float nameX, float nameY, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            float x = nameX + local.x;
            float y = nameY - local.y;
            if (x < minX)
                minX = x;
            if (x > maxX)
                maxX = x;
            if (y < minY)
                minY = y;
            if (y > maxY)
                maxY = y;
        }

        private static void SyncCuePlate(RectTransform parent, Rect spot)
        {
            if (parent != null && parent.Find("CuePlate") is RectTransform plate)
            {
                PlaceTopLeft(plate, spot.x, spot.y, spot.width, spot.height);
                if (plate.TryGetComponent<Image>(out Image image))
                    image.color = CueTabColor();
            }
        }

        private static bool MarginSpotClear(RectTransform parent, Rect spot, float frameW, float frameH, Rect detail)
        {
            const float edge = 12f;
            if (spot.xMin < edge || spot.yMin < edge || spot.xMax > frameW - edge || spot.yMax > frameH - edge)
                return false;
            if (!CueMissesDetail(spot, detail))
                return false;
            if (PillHitsLiveHex(parent, InflateRect(spot, 13f)))
                return false;
            if (PillCrossesLeader(parent, spot))
                return false;
            var cards = new List<Rect>(8);
            var pins = new List<Rect>(8);
            CollectOverlayRects(parent, "ZoneCard_", cards);
            CollectOverlayRects(parent, "ZonePin_", pins);
            return CueClears(spot, cards, 2f) && CueClears(spot, pins, 1f);
        }

        private static bool PillCrossesLeader(RectTransform parent, Rect spot)
        {
            if (parent == null)
                return false;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZonePin_", StringComparison.Ordinal) || child is not RectTransform pin)
                    continue;
                string mapId = child.name.Substring("ZonePin_".Length);
                if (parent.Find("ZoneCard_" + mapId) is not RectTransform card)
                    continue;
                float pinW = pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize;
                float pinH = pin.sizeDelta.y > 2f ? pin.sizeDelta.y : ZonePinSize;
                Vector2 from = new Vector2(pin.anchoredPosition.x + pinW * 0.5f, -pin.anchoredPosition.y + pinH * 0.5f);
                var cardRect = new Rect(card.anchoredPosition.x, -card.anchoredPosition.y, card.sizeDelta.x, card.sizeDelta.y);
                Vector2 to = new Vector2(
                    Mathf.Clamp(from.x, cardRect.xMin, cardRect.xMax),
                    Mathf.Clamp(from.y, cardRect.yMin, cardRect.yMax));
                if (SegmentHitsRect(from, to, spot))
                    return true;
            }

            return false;
        }

        private static bool PillHitsLiveHex(RectTransform parent, Rect spot)
        {
            if (parent == null || spot.width < 1f || spot.height < 1f)
                return false;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneOutline_", StringComparison.Ordinal) || child is not RectTransform outline)
                    continue;
                if (!outline.gameObject.activeInHierarchy)
                    continue;
                Vector2[] shape = OutlinePlatePoints(outline);
                if (shape.Length >= 3 && RectHitsPolygon(spot, shape))
                    return true;
            }

            return false;
        }

        private static Vector2[] OutlinePlatePoints(RectTransform outline)
        {
            V3PolygonGraphic graphic = outline != null ? outline.GetComponent<V3PolygonGraphic>() : null;
            int count = graphic != null ? graphic.PointCount : 0;
            if (count < 3)
                return Array.Empty<Vector2>();
            float originX = outline.anchoredPosition.x;
            float originY = -outline.anchoredPosition.y;
            var shape = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Vector2 local = graphic.LocalPoint(i);
                shape[i] = new Vector2(originX + local.x, originY - local.y);
            }

            return shape;
        }

        private static Rect InflateRect(Rect rect, float margin)
        {
            return new Rect(rect.x - margin, rect.y - margin, rect.width + margin * 2f, rect.height + margin * 2f);
        }

        private void FitZoomCue()
        {
            if (_fittingZoomCue)
                return;
            _fittingZoomCue = true;
            try
            {
                FitZoomCueOnce();
            }
            finally
            {
                _fittingZoomCue = false;
            }
        }

        private void FitZoomCueOnce()
        {
            FitBattleCardSubtitles();
            if (_libraryRegions == null || !_libraryRegions.gameObject.activeInHierarchy)
                return;
            TMP_Text cue = null;
            TMP_Text[] texts = _libraryRegions.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "ZoomCue" && texts[i].gameObject.activeInHierarchy)
                {
                    cue = texts[i];
                    break;
                }
            }

            if (cue == null)
            {
                Debug.LogError("[ZoomCue] missing ratio=" + TheatreSeatGeometry.ShotRatio
                    + " locale=" + TheatreSeatGeometry.ShotLocale);
                RaiseTheatreReadability(_libraryRegions);
                ReseatFramedCards(_libraryRegions);
                LogTheatreSeats(_libraryRegions, UiShellRuntimeGateway.Localization.IsRightToLeft);
                return;
            }
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            string heldSelection = _libraryViewModel != null ? _libraryViewModel.Theater.SelectedMapId : "DB";
            if (string.IsNullOrEmpty(heldSelection))
                heldSelection = "DB";
            if (SeatZoomCueClearOfHexes(cue, fa, heldSelection))
            {
                RaiseTheatreReadability(_libraryRegions);
                FitCueInsidePackedRect(cue);
                InkZoneNames(_libraryRegions);
                FinishCuePaint(cue, heldSelection);
                ReseatFramedCards(_libraryRegions);
                LogTheatreCue(cue);
                LogTheatreSeats(cue.transform.parent, fa);
                return;
            }

            RaiseTheatreReadability(_libraryRegions);
            FinishCuePaint(cue, heldSelection);
            ReseatFramedCards(_libraryRegions);
            LogTheatreCue(cue);
            LogTheatreSeats(cue.transform.parent, fa);
        }

        /// <summary>
        /// English ink may drop to 16px. Farsi stays at least 22px and the box grows
        /// to the line. Capture rejects a font under 14 and a line past the box.
        /// </summary>
        private static void FitCueInk(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null)
                return;
            bool fa = CueCopyIsFarsi(cue, FlattenCopy(cue.text))
                || UiShellRuntimeGateway.Localization.IsRightToLeft;
            float floor = fa ? 22f : 16f;
            cue.ForceMeshUpdate();
            float width = cue.rectTransform.rect.width > 1f ? cue.rectTransform.rect.width : cue.rectTransform.sizeDelta.x;
            float ink = MeasuredNameWidth(cue);
            if (!fa)
            {
                while (ink > width + 2f && cue.fontSize > floor + 0.1f)
                {
                    float next = Mathf.Max(floor, cue.fontSize - 1f);
                    cue.fontSizeMin = next;
                    cue.fontSizeMax = next;
                    cue.fontSize = next;
                    cue.ForceMeshUpdate();
                    ink = MeasuredNameWidth(cue);
                    if (next <= floor + 0.1f)
                        break;
                }
            }

            if (ink <= width + 2f)
                return;
            float need = ink + 6f;
            if (cue.rectTransform.parent is not RectTransform parent)
                return;
            float frameW = parent.rect.width > 2f ? parent.rect.width : parent.sizeDelta.x;
            float x = cue.rectTransform.anchoredPosition.x;
            float maxW = frameW - 12f - Mathf.Max(0f, x);
            if (need > maxW)
                need = maxW;
            if (need < width + 1f)
                return;
            var size = cue.rectTransform.sizeDelta;
            cue.rectTransform.sizeDelta = new Vector2(need, size.y);
            KeepAuthoredSize(cue.rectTransform);
            SyncCuePlate(parent, new Rect(x, -cue.rectTransform.anchoredPosition.y, need, size.y));
        }

        /// <summary>
        /// Locks the cue after every layout pass and writes the sizes the capture camera draws.
        /// </summary>
        private static void FinishCuePaint(TMP_Text cue, string selectedId)
        {
            if (cue == null)
                return;
            if (cue.GetComponent<TheatreZoomCueLock>() == null)
                cue.gameObject.AddComponent<TheatreZoomCueLock>();
            LockCueFont(cue);
            FitCueInkToBox(cue);
            LiftCueClearOfTitle(cue);
            if (!string.IsNullOrEmpty(selectedId))
                DropNameBelowCue(cue, selectedId);
            AttachZoomPointer(cue.rectTransform);
            LogRenderedMeasures(cue);
        }

        /// <summary>
        /// Drawn pixels, not the seat harness. <c>fontSize</c> is the on-screen cue size.
        /// Card <c>w h</c> are the on-screen portrait. <c>DistrictGap</c> is the dark strip
        /// between outer halos. Capture outlines sit at lossyScale 1, so the logged gap
        /// is the fill edge minus two <see cref="TheatreSeatGeometry.DistrictGlowReach"/> halos.
        /// </summary>
        private static void LogRenderedMeasures(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null)
                return;
            float cueScale = cue.rectTransform.lossyScale.y;
            if (cueScale < 0.001f)
                cueScale = 1f;
            cue.ForceMeshUpdate();
            if (CueCopyIsFarsi(cue, FlattenCopy(cue.text))
                && (string.IsNullOrEmpty(TheatreSeatGeometry.ShotLocale)
                    || !TheatreSeatGeometry.ShotLocale.StartsWith("fa", StringComparison.OrdinalIgnoreCase)))
                TheatreSeatGeometry.ShotLocale = "fa";
            int cueLines = cue.textInfo != null ? cue.textInfo.lineCount : 0;
            float cueBoxW = cue.rectTransform.rect.width > 1f ? cue.rectTransform.rect.width : cue.rectTransform.sizeDelta.x;
            float cueBoxH = cue.rectTransform.rect.height > 1f ? cue.rectTransform.rect.height : cue.rectTransform.sizeDelta.y;
            Debug.Log("[ZoomCue] fontSize=" + F(cue.fontSize * cueScale)
                + " local=" + F(cue.fontSize)
                + " scale=" + F(cueScale)
                + " lines=" + cueLines.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " box=" + F(cueBoxW) + "x" + F(cueBoxH)
                + " reason=" + ZoomCueFitReason
                + ShotTag());
            if (cue.transform.parent is not RectTransform parent)
                return;
            var outlines = new List<RectTransform>(8);
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                    continue;
                if (child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) && child is RectTransform zone)
                {
                    float scaleX = zone.lossyScale.x;
                    float scaleY = zone.lossyScale.y;
                    if (scaleX < 0.001f)
                        scaleX = 1f;
                    if (scaleY < 0.001f)
                        scaleY = 1f;
                    string mapId = child.name.Substring("ZoneCard_".Length);
                    float drawnW = (zone.rect.width > 1f ? zone.rect.width : zone.sizeDelta.x) * scaleX;
                    float drawnH = (zone.rect.height > 1f ? zone.rect.height : zone.sizeDelta.y) * scaleY;
                    Debug.Log("[ZoneCard] " + mapId + ShotTag() + " " + F(drawnW) + " " + F(drawnH)
                        + " local=" + F(zone.sizeDelta.x) + "x" + F(zone.sizeDelta.y));
                }

                if (child.name.StartsWith("ZoneOutline_", StringComparison.Ordinal) && child is RectTransform outline)
                    outlines.Add(outline);
            }

            for (int i = 0; i < outlines.Count; i++)
            {
                for (int j = i + 1; j < outlines.Count; j++)
                {
                    Vector2[] left = OutlinePlatePoints(outlines[i]);
                    Vector2[] right = OutlinePlatePoints(outlines[j]);
                    float localGap = OutlineEdgeGap(left, right) - TheatreSeatGeometry.DistrictGlowReach * 2f;
                    float scale = outlines[i].lossyScale.x;
                    if (scale < 0.001f)
                        scale = 1f;
                    string pair = OutlineMapId(outlines[i]) + "-" + OutlineMapId(outlines[j]);
                    Debug.Log("[DistrictGap] " + pair + ShotTag() + " " + F(localGap * scale));
                }
            }

            for (int i = 0; i < outlines.Count; i++)
                LogDistrictGlow(outlines[i]);
        }

        /// <summary>
        /// What the camera composites for the soft halo. Reach is
        /// <see cref="TheatreSeatGeometry.DistrictGlowReach"/> times the outline scale.
        /// Alpha is the image tint. The material is the shader actually on the halo,
        /// not the multiply shader on the fill. <c>coreAlpha</c> is the sprite alpha on
        /// the stroke, <c>brightness</c> is that core's luminance, and <c>saturation</c>
        /// is the district hue painted out to the reach.
        /// </summary>
        private static void LogDistrictGlow(RectTransform outline)
        {
            if (outline == null)
                return;
            float localReach = TheatreSeatGeometry.TerritoryFeather;
            Transform haloNode = outline.Find("ZoneHalo");
            Image halo = haloNode != null ? haloNode.GetComponent<Image>() : null;
            float scale = outline.lossyScale.x;
            if (scale < 0.001f)
                scale = 1f;
            float alpha = halo != null ? halo.color.a : 0f;
            string shader = "none";
            if (halo != null && halo.material != null && halo.material.shader != null)
                shader = halo.material.shader.name;
            float coreAlpha = 0f;
            float brightness = 0f;
            float saturation = 0f;
            if (halo != null)
            {
                Color zone = TerritoryTint(OutlineMapId(outline), RegionZoneColor(OutlineMapId(outline)));
                coreAlpha = TheatreSeatGeometry.TerritoryBorderAlpha;
                brightness = GlowLuminance(zone);
                saturation = GlowSaturation(zone);
            }

            Debug.Log("[DistrictGlow] " + OutlineMapId(outline) + ShotTag()
                + " reach=" + F(localReach * scale)
                + " alpha=" + F(alpha)
                + " material=" + shader
                + " coreAlpha=" + coreAlpha.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " fill=" + TheatreSeatGeometry.TerritoryFillAlpha.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " brightness=" + brightness.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " saturation=" + saturation.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        }

        private static string ShotTag()
        {
            string ratio = string.IsNullOrEmpty(TheatreSeatGeometry.ShotRatio) ? "unset" : TheatreSeatGeometry.ShotRatio;
            string locale = string.IsNullOrEmpty(TheatreSeatGeometry.ShotLocale) ? "en" : TheatreSeatGeometry.ShotLocale;
            return " ratio=" + ratio + " locale=" + locale;
        }

        private static string OutlineMapId(RectTransform outline)
        {
            const string prefix = "ZoneOutline_";
            if (outline == null || !outline.name.StartsWith(prefix, StringComparison.Ordinal))
                return "";
            return outline.name.Substring(prefix.Length);
        }

        private static float OutlineEdgeGap(Vector2[] left, Vector2[] right)
        {
            if (left == null || right == null || left.Length < 3 || right.Length < 3)
                return 0f;
            float best = float.MaxValue;
            for (int i = 0; i < left.Length; i++)
                best = Mathf.Min(best, DistanceToPolygonEdge(left[i], right));
            for (int i = 0; i < right.Length; i++)
                best = Mathf.Min(best, DistanceToPolygonEdge(right[i], left));
            return best;
        }

        private static bool TryApplyArrangedSeats(List<TheatreZoneSlot> slots, float plateWidth, float plateHeight, bool fa)
        {
            string selected = "DB";
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Selected && !string.IsNullOrEmpty(slots[i].MapId))
                    selected = slots[i].MapId;
            }

            bool reserve = TheatreSeatGeometry.DetailReserveOn(
                plateWidth, plateHeight, fa, plateWidth >= 600f, TheatreSeatGeometry.ShotIsFourByThree(), selected);
            TheatreSeatGeometry.Layout layout = TheatreSeatGeometry.Arrange(
                plateWidth, plateHeight, fa, reserve, selected);
            TheatreSeatGeometry.NoteSeating(plateWidth, plateHeight, fa, reserve, selected, layout);
            if (layout.Seats == null)
                return false;
            bool cardsOk = layout.CardsLegal && layout.CueNear && layout.HexAspectLegal;
            for (int i = 0; i < slots.Count; i++)
            {
                TheatreZoneSlot slot = slots[i];
                int index = -1;
                for (int seatIndex = 0; seatIndex < layout.Seats.Length; seatIndex++)
                {
                    if (!string.Equals(layout.Seats[seatIndex].MapId, slot.MapId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    index = seatIndex;
                    break;
                }

                if (index < 0)
                    return false;
                TheatreSeatGeometry.Seat seat = layout.Seats[index];
                slot.Pin = new Rect(seat.Pin.X, seat.Pin.Y, seat.Pin.W, seat.Pin.H);
                if (seat.Hex != null && seat.Hex.Length >= 3)
                {
                    var hex = new Vector2[seat.Hex.Length];
                    for (int point = 0; point < seat.Hex.Length; point++)
                        hex[point] = new Vector2(seat.Hex[point].X, seat.Hex[point].Y);
                    slot.Hex = hex;
                }

                if (cardsOk && seat.Card.W >= 1f)
                    slot.Card = new Rect(seat.Card.X, seat.Card.Y, seat.Card.W, seat.Card.H);
                if (slot.Selected && layout.Cue.W >= 8f && layout.Cue.H >= 8f)
                    slot.Cue = new Rect(layout.Cue.X, layout.Cue.Y, layout.Cue.W, layout.Cue.H);
                else if (cardsOk && seat.Card.W >= 1f)
                    slot.Cue = new Rect(layout.Cue.X, layout.Cue.Y, layout.Cue.W, layout.Cue.H);

                slots[i] = slot;
            }

            float coverage = TheatreSeatGeometry.DrawnTerritoryCoverage(layout, plateWidth, plateHeight, fa, reserve);
            Debug.Log(TheatreSeatGeometry.FormatTerritoryCoverage(
                TheatreSeatGeometry.ShotRatio,
                fa ? "fa" : TheatreSeatGeometry.ShotLocale,
                coverage,
                reserve));
            return cardsOk;
        }

        private static void LogTheatreSeats(Transform parent, bool fa)
        {
            if (parent == null)
                return;
            string locale = fa ? "fa" : "en";
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal))
                    continue;
                if (child is not RectTransform rect)
                    continue;
                string mapId = child.name.Substring("ZoneCard_".Length);
                var box = new TheatreSeatGeometry.Box(
                    rect.anchoredPosition.x,
                    -rect.anchoredPosition.y,
                    rect.sizeDelta.x,
                    rect.sizeDelta.y);
                Debug.Log(TheatreSeatGeometry.FormatSeat(locale, mapId, box, true));
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || child.name != "ZoomCue" || child is not RectTransform cueRect)
                    continue;
                Debug.Log("[TheatreSeat] locale=" + locale
                    + " cue x=" + cueRect.anchoredPosition.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                    + " y=" + (-cueRect.anchoredPosition.y).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                    + " w=" + cueRect.sizeDelta.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                    + " h=" + cueRect.sizeDelta.y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// One line per theatre shot: plate, open viewport, detail reserve, and the painting rect.
        /// <c>band=reserve</c> means the painting covers the viewport and the unpainted column is the
        /// detail reserve (#66 does not draw P1's panel). <c>band=gap</c> means the painting is inside
        /// the viewport. <c>gapB</c> is the empty strip under the painting inside the plate.
        /// </summary>
        private static void LogTheatreCover(
            RectTransform clip,
            float plateWidth,
            float plateHeight,
            TheatreSeatGeometry.Box viewport,
            TheatreSeatGeometry.Box reserve,
            RawImage paint,
            bool fa)
        {
            float paintX = 0f;
            float paintY = 0f;
            float paintW = 0f;
            float paintH = 0f;
            bool hasPaint = paint != null && paint.rectTransform != null;
            if (hasPaint)
            {
                RectTransform rect = paint.rectTransform;
                bool stretched = rect.anchorMin.x <= 0.01f && rect.anchorMax.x >= 0.99f
                    && rect.anchorMin.y <= 0.01f && rect.anchorMax.y >= 0.99f;
                if (stretched && rect.sizeDelta.x < 2f && rect.sizeDelta.y < 2f)
                {
                    paintW = plateWidth;
                    paintH = plateHeight;
                }
                else
                {
                    paintX = rect.anchoredPosition.x;
                    paintY = -rect.anchoredPosition.y;
                    paintW = rect.sizeDelta.x;
                    paintH = rect.sizeDelta.y;
                }
            }

            float gapL = paintX;
            float gapT = paintY;
            float gapR = plateWidth - (paintX + paintW);
            float gapB = plateHeight - (paintY + paintH);
            bool covers = hasPaint
                && paintX <= viewport.X + 1.5f
                && paintY <= viewport.Y + 1.5f
                && paintX + paintW >= viewport.XMax - 1.5f
                && paintY + paintH >= viewport.YMax - 1.5f;
            string band = !hasPaint ? "no-paint" : !covers ? "gap" : reserve.W > 1f ? "reserve" : "full";
            string clipInset = "none";
            if (clip != null)
            {
                clipInset = F(clip.offsetMin.x) + "," + F(clip.offsetMin.y)
                    + "," + F(clip.offsetMax.x) + "," + F(clip.offsetMax.y);
            }

            Debug.Log("[TheatreCover] ratio=" + TheatreSeatGeometry.ShotRatio
                + " notch=" + TheatreSeatGeometry.ShotNotch
                + " locale=" + TheatreSeatGeometry.ShotLocale
                + " fa=" + (fa ? "1" : "0")
                + " reserveOn=" + (TheatreSeatGeometry.ShotReserve ? "1" : "0")
                + " plate=0,0," + F(plateWidth) + "," + F(plateHeight)
                + " viewport=" + F(viewport.X) + "," + F(viewport.Y) + "," + F(viewport.W) + "," + F(viewport.H)
                + " reserve=" + F(reserve.X) + "," + F(reserve.Y) + "," + F(reserve.W) + "," + F(reserve.H)
                + " paint=" + (hasPaint
                    ? F(paintX) + "," + F(paintY) + "," + F(paintW) + "," + F(paintH)
                    : "none")
                + " gapL=" + F(gapL)
                + " gapR=" + F(gapR)
                + " gapT=" + F(gapT)
                + " gapB=" + F(gapB)
                + " coversViewport=" + (covers ? "1" : "0")
                + " band=" + band
                + " clipOffset=" + clipInset);
        }

        private static string F(float value)
        {
            return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void RaiseTheatreReadability(Transform regions)
        {
            if (regions == null)
                return;
            FitZoneNames(regions);
            TMP_Text[] texts = regions.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.gameObject.activeInHierarchy)
                    continue;
                if (text.name == "ZoneName")
                {
                    if (!ZoneCardIsCompact(text))
                        RaiseTheatreLine(text, 18f, 32f);
                    continue;
                }
                if (text.name == "ZoneCount")
                    RaiseTheatreLine(text, 18f, 28f);
                else if (text.name == "ZoomCue")
                    continue;
            }

            ReseatFramedCards(regions);
        }

        private const float ZoneNameFontCeiling = 32f;
        private const float ZoneNameFontFloor = 16f;
        /// <summary>Drawn name size. The packer still stops at NameFontCeiling so the seats do not move.</summary>
        private const float DrawnNameCeiling = 32f;
        /// <summary>About 80% of the approved-1 cap height. One line at this size is the aim.</summary>
        private const float DrawnNameTarget = 26f;
        /// <summary>Latin titles stay at least this large. Arabic keeps its own floor and must not pull Latin down.</summary>
        private const float LatinNameFloor = 20f;

        /// <summary>
        /// Titles on a full 320x110 card stay on the v35 18–26 line. A card that had to shrink
        /// toward 280x96 fits its own title down from that ceiling, and may break after a space.
        /// </summary>
        private static void FitPortraitNameBoxes(Transform regions)
        {
            if (regions == null)
                return;
            TMP_Text[] texts = regions.GetComponentsInChildren<TMP_Text>(true);
            var names = new List<TMP_Text>(8);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text != null && text.name == "ZoneName" && text.gameObject.activeInHierarchy && ZoneCardIsCompact(text))
                    names.Add(text);
            }

            if (names.Count == 0)
                return;
            RectTransform plate = names[0].rectTransform.parent != null
                ? names[0].rectTransform.parent.parent as RectTransform
                : null;
            if (plate == null || !TryFrameSize(plate, out float plateWidth, out float plateHeight))
                return;
            bool fa = names[0].isRightToLeftText;
            // Locale owns the branch. A Farsi plate can still be holding the English source
            // when this pass runs, and a Latin plate must not inherit the Arabic floor.
            bool plateFarsi = UiShellRuntimeGateway.Localization.IsRightToLeft || fa;
            bool wide = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
            var cards = new List<RectTransform>(names.Count);
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i].rectTransform.parent is RectTransform card)
                    cards.Add(card);
            }

            var boxes = new TheatreSeatGeometry.Box[cards.Count];
            for (int i = 0; i < cards.Count; i++)
                boxes[i] = PlateBoxOf(cards[i]);
            Rect detailRect = SeatingDetailRect(plateWidth, plateHeight, fa);
            var detail = new TheatreSeatGeometry.Box(detailRect.x, detailRect.y, detailRect.width, detailRect.height);
            TheatreSeatGeometry.Box cue = default;
            if (plate.Find("ZoomCue") is RectTransform cueRect && cueRect.gameObject.activeInHierarchy)
                cue = PlateBoxOf(cueRect);
            TheatreSeatGeometry.Box hint = TheatreSeatGeometry.ZoomHintRect(plateWidth, plateHeight);
            // Latin and Farsi each take the largest size that fits every card of that branch.
            // Farsi keeps the 3e2c86f45 Arabic floor. That floor must not become the Latin size.
            float latinShared = DrawnNameCeiling;
            float arabicShared = DrawnNameCeiling;
            bool anyLatin = false;
            bool anyArabic = false;
            var arabic = new bool[names.Count];
            for (int i = 0; i < names.Count && i < cards.Count; i++)
            {
                arabic[i] = plateFarsi || ZoneNameIsFarsi(names[i]);
                float used = FitOnePortraitName(names[i], cards[i], boxes, i, detail, cue, hint, plateHeight, wide, arabic[i]);
                if (used < 1f)
                    continue;
                if (arabic[i])
                {
                    anyArabic = true;
                    if (used < arabicShared)
                        arabicShared = used;
                }
                else
                {
                    anyLatin = true;
                    if (used < latinShared)
                        latinShared = used;
                }
            }

            if (!anyLatin && !anyArabic)
                return;
            for (int i = 0; i < names.Count && i < cards.Count; i++)
            {
                bool script = arabic[i];
                if (script && !anyArabic)
                    continue;
                if (!script && !anyLatin)
                    continue;
                float shared = script ? arabicShared : latinShared;
                if (!script && shared < LatinNameFloor - 0.1f)
                    shared = LatinNameFloor;
                if (Mathf.Abs(names[i].fontSize - shared) > 0.4f)
                    TryCommitPortraitName(names[i], cards[i], shared, wide, script);
                LockDrawnZoneName(names[i], shared, script);
                RecordDrawnZoneName(names[i]);
                if (names[i].rectTransform != null && cards[i] != null)
                {
                    float cardW = cards[i].sizeDelta.x > 2f ? cards[i].sizeDelta.x : cards[i].rect.width;
                    float cardH = cards[i].sizeDelta.y > 2f ? cards[i].sizeDelta.y : cards[i].rect.height;
                    ClampNameInsideCard(names[i].rectTransform, cardW, cardH);
                }
            }
        }

        /// <summary>
        /// Farsi when the locale is right-to-left, the label is marked right-to-left, or the
        /// glyphs are already Arabic. Latin titles on an English plate stay on the other side.
        /// </summary>
        private static bool ZoneNameIsFarsi(TMP_Text name)
        {
            if (name == null)
                return false;
            if (name.isRightToLeftText)
                return true;
            return ContainsArabicLetter(FlattenCopy(name.text));
        }

        private static TheatreSeatGeometry.Box PlateBoxOf(RectTransform rect)
        {
            if (rect == null)
                return default;
            return new TheatreSeatGeometry.Box(rect.anchoredPosition.x, -rect.anchoredPosition.y, rect.sizeDelta.x, rect.sizeDelta.y);
        }

        private static float FitOnePortraitName(
            TMP_Text name,
            RectTransform card,
            TheatreSeatGeometry.Box[] boxes,
            int index,
            TheatreSeatGeometry.Box detail,
            TheatreSeatGeometry.Box cue,
            TheatreSeatGeometry.Box hint,
            float plateHeight,
            bool wide,
            bool farsi)
        {
            if (name == null || card == null)
                return 0f;
            float cardW = card.sizeDelta.x;
            float boxWidth = PortraitNameWidth(cardW);
            string source = FlattenCopy(name.text);
            if (name.rectTransform != null)
                name.rectTransform.sizeDelta = new Vector2(boxWidth, Mathf.Max(24f, name.rectTransform.sizeDelta.y));
            float fontFloor;
            if (farsi)
            {
                // Same floor 3e2c86f45 used for Arabic, applied from the locale so a still-English
                // source string cannot take the Latin 20px floor.
                fontFloor = cardW <= TheatreSeatGeometry.PortraitMinWidth + 8f
                    ? TheatreSeatGeometry.PortraitCompactFontFloor
                    : TheatreSeatGeometry.NameFontFloor;
                if (fontFloor > TheatreSeatGeometry.PortraitCompactFontFloor && boxWidth <= 114f)
                    fontFloor = TheatreSeatGeometry.PortraitCompactFontFloor;
            }
            else
                fontFloor = LatinNameFloor;
            for (float size = DrawnNameCeiling; size >= fontFloor - 0.01f; size -= 1f)
            {
                if (TryCommitPortraitName(name, card, size, wide, farsi))
                    return size;
            }

            TightenPortraitName(name, card, source, wide, farsi);
            return name.fontSize;
        }

        /// <summary>
        /// Puts every zone title on this plate at <paramref name="size"/>. Returns false when the
        /// words or the name box will not hold that size.
        /// </summary>
        private static bool TryCommitPortraitName(TMP_Text name, RectTransform card, float size, bool wide, bool farsi)
        {
            if (name == null || card == null || size < 1f)
                return false;
            if (!farsi && size < LatinNameFloor - 0.1f)
                return false;
            float cardH = card.sizeDelta.y;
            float boxWidth = PortraitNameWidth(card.sizeDelta.x);
            string source = FlattenCopy(name.text);
            if (name.rectTransform != null)
                name.rectTransform.sizeDelta = new Vector2(boxWidth, Mathf.Max(24f, name.rectTransform.sizeDelta.y));
            var binding = name.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
            name.enableAutoSizing = true;
            name.fontSizeMin = size;
            name.fontSizeMax = size;
            name.fontSize = size;
            name.fontStyle = FontStyles.Bold;
            name.text = source;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Overflow;
            name.maxVisibleLines = 1;
            name.characterSpacing = 0f;
            bool oneLine = FitsNameLine(name, source, size, boxWidth, 0f);
            if (name.rectTransform != null)
                name.rectTransform.sizeDelta = new Vector2(boxWidth, Mathf.Max(24f, name.rectTransform.sizeDelta.y));
            if (!oneLine)
            {
                name.characterSpacing = 0f;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.maxVisibleLines = 2;
                bool wrapped = WrapZoneName(name, size);
                if (!wrapped)
                    return false;
                name.fontSizeMin = size;
                name.fontSizeMax = size;
                name.fontSize = size;
                name.maxVisibleLines = 2;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.ForceMeshUpdate();
            }

            float needed = PortraitNameBox(name, source, size, cardH);
            if (needed < 0.5f)
                return false;
            float cap = farsi ? TheatreSeatGeometry.MaxNameBoxHeight(cardH, wide) : LatinNameCap(cardH);
            if (cap + 1f < needed)
                return false;

            ApplyPortraitNameLayout(card, name, needed, cardH, wide, oneLine);
            name.lineSpacing = 0f;
            name.ForceMeshUpdate();
            return PortraitNameFitsRect(name, source);
        }

        /// <summary>
        /// One line at <paramref name="size"/>. Tracking may tighten down to <paramref name="minSpacing"/>
        /// so a two-word title stays on one line instead of wrapping.
        /// </summary>
        private static bool FitsNameLine(TMP_Text name, string source, float size, float boxWidth, float minSpacing)
        {
            if (name == null || size < 1f || boxWidth < 8f)
                return false;
            var binding = name.GetComponent<V3LocalizedTextBindingView>();
            if (minSpacing > 0f)
                minSpacing = 0f;
            for (float spacing = 0f; spacing >= minSpacing - 0.01f; spacing -= 1f)
            {
                if (binding != null)
                    binding.ConfigureFontBounds(size, size);
                name.enableAutoSizing = true;
                name.fontSizeMin = size;
                name.fontSizeMax = size;
                name.fontSize = size;
                name.characterSpacing = spacing;
                name.text = source;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Overflow;
                name.maxVisibleLines = 1;
                name.ForceMeshUpdate();
                float line = MeasuredNameWidth(name);
                if (line > 0.5f && line <= boxWidth + 2f && !name.isTextTruncated && !name.isTextOverflowing)
                    return true;
            }

            name.characterSpacing = 0f;
            return false;
        }

        private static void TightenPortraitName(TMP_Text name, RectTransform card, string source, bool wide, bool farsi)
        {
            if (name == null || card == null)
                return;
            float size;
            float stop;
            if (farsi)
            {
                size = card.sizeDelta.x <= TheatreSeatGeometry.PortraitMinWidth + 8f
                    ? TheatreSeatGeometry.PortraitCompactFontFloor
                    : TheatreSeatGeometry.NameFontFloor;
                float boxProbe = PortraitNameWidth(card.sizeDelta.x);
                if (size > TheatreSeatGeometry.PortraitCompactFontFloor && boxProbe <= 114f)
                    size = TheatreSeatGeometry.PortraitCompactFontFloor;
                stop = size;
            }
            else
            {
                size = LatinNameFloor;
                stop = LatinNameFloor;
            }

            float boxWidth = PortraitNameWidth(card.sizeDelta.x);
            float cap = farsi ? TheatreSeatGeometry.MaxNameBoxHeight(card.sizeDelta.y, wide) : LatinNameCap(card.sizeDelta.y);
            var binding = name.GetComponent<V3LocalizedTextBindingView>();
            for (float candidate = Mathf.Max(size, farsi ? DrawnNameTarget : LatinNameFloor); candidate >= stop - 0.01f; candidate -= 1f)
            {
                if (binding != null)
                    binding.ConfigureFontBounds(candidate, candidate);
                name.fontSizeMin = candidate;
                name.fontSizeMax = candidate;
                name.fontSize = candidate;
                name.text = source;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Overflow;
                name.maxVisibleLines = 1;
                name.lineSpacing = 0f;
                name.characterSpacing = 0f;
                if (name.rectTransform != null)
                    name.rectTransform.sizeDelta = new Vector2(boxWidth, Mathf.Max(24f, name.rectTransform.sizeDelta.y));
                if (!FitsNameLine(name, source, candidate, boxWidth, 0f))
                    continue;
                float needed = PortraitNameBox(name, source, candidate, card.sizeDelta.y);
                if (cap + 1f < needed)
                    continue;
                ApplyPortraitNameLayout(card, name, needed, card.sizeDelta.y, wide, true);
                name.lineSpacing = 0f;
                name.ForceMeshUpdate();
                if (PortraitNameFitsRect(name, source))
                    return;
            }

            float wrapStart = farsi ? size : DrawnNameTarget;
            float wrapStop = size;
            for (float wrapped = wrapStart; wrapped >= wrapStop - 0.01f; wrapped -= 1f)
            {
                if (binding != null)
                    binding.ConfigureFontBounds(wrapped, wrapped);
                name.fontSizeMin = wrapped;
                name.fontSizeMax = wrapped;
                name.fontSize = wrapped;
                name.text = source;
                name.maxVisibleLines = 2;
                if (name.rectTransform != null)
                    name.rectTransform.sizeDelta = new Vector2(boxWidth, Mathf.Max(24f, name.rectTransform.sizeDelta.y));
                name.characterSpacing = 0f;
                bool wrappedOk = WrapZoneName(name, wrapped);
                if (!wrappedOk)
                    continue;

                float wrappedHeight = PortraitNameBox(name, source, wrapped, card.sizeDelta.y);
                float floorHeight = TheatreSeatGeometry.WrappedFloorNameHeight(source, card.sizeDelta.x);
                if (cap + 1f < wrappedHeight && floorHeight <= cap + 1f)
                    wrappedHeight = floorHeight;
                if (cap + 1f < wrappedHeight)
                    continue;
                ApplyPortraitNameLayout(card, name, wrappedHeight, card.sizeDelta.y, wide, false);
                name.lineSpacing = 0f;
                name.ForceMeshUpdate();
                if (PortraitNameFitsRect(name, source))
                    return;
            }
        }

        /// <summary>
        /// Height the name rect has to be. On a short portrait, Noto Arabic's line box is about
        /// 1.65em, taller than the preferred height TMP reports, so a wrapped Farsi title is not
        /// given a rect the status row will cover. Taller cards keep the measured preferred height.
        /// </summary>
        private static float PortraitNameBox(TMP_Text name, string source, float size, float cardHeight)
        {
            float needed = Mathf.Ceil((name != null ? name.preferredHeight : 0f) + 2f);
            if (cardHeight >= 190f || !ContainsArabicLetter(source))
                return needed;
            int lines = 1;
            if (name != null && name.textInfo != null && name.textInfo.lineCount > 0)
                lines = name.textInfo.lineCount;
            float arabic = Mathf.Ceil(lines * size * 1.65f + 2f);
            return arabic > needed ? arabic : needed;
        }

        private static bool PortraitNameFitsRect(TMP_Text name, string source)
        {
            if (name == null || name.rectTransform == null)
                return false;
            if (name.isTextTruncated || name.isTextOverflowing)
                return false;
            float width = name.rectTransform.rect.width > 1f
                ? name.rectTransform.rect.width
                : name.rectTransform.sizeDelta.x;
            if (MeasuredNameWidth(name) > width + 2f)
                return false;
            if (name.preferredHeight > name.rectTransform.rect.height + 1.5f)
                return false;
            return PortraitNameCovers(name, source);
        }

        private static bool PortraitNameCovers(TMP_Text text, string source)
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

        private static void ApplyPortraitNameLayout(RectTransform card, TMP_Text name, float nameH, float cardH, bool wide, bool oneLine)
        {
            if (card == null || name == null)
                return;
            float cardW = card.sizeDelta.x;
            float cardX = card.anchoredPosition.x;
            float cardY = -card.anchoredPosition.y;
            PlaceTopLeft(card, cardX, cardY, cardW, cardH);
            if (oneLine)
                nameH = Mathf.Max(nameH, cardH >= 190f ? 28f : 24f);
            LayoutZoneCardStack(card, name, nameH, cardH, wide);
        }

        /// <summary>
        /// Title width. Two pixels of rim on each side; the rest of the card is the name,
        /// so a long word wraps onto a second line instead of shrinking the shared size.
        /// </summary>
        private static float PortraitNameWidth(float cardWidth)
        {
            return Mathf.Max(8f, cardWidth - 4f);
        }

        /// <summary>
        /// Full-bleed plate. The name and the count share one bottom strip of at most 22%
        /// of the card. The seat rect is left alone.
        /// </summary>
        private static void LayoutZoneCardStack(RectTransform card, TMP_Text name, float nameH, float cardH, bool wide)
        {
            if (card == null)
                return;
            float cardW = card.sizeDelta.x;
            const float pad = 2f;
            float nameW = PortraitNameWidth(cardW);
            if (name != null && name.rectTransform != null)
            {
                float authored = name.rectTransform.sizeDelta.x;
                if (authored > 8f)
                    nameW = Mathf.Min(nameW, authored);
            }

            if (nameW > cardW - pad)
                nameW = Mathf.Max(8f, cardW - pad);
            float nameX = Mathf.Max(0f, (cardW - nameW) * 0.5f);
            if (nameX + nameW > cardW)
                nameW = Mathf.Max(8f, cardW - nameX);
            bool farsi = ZoneNameIsFarsi(name) || UiShellRuntimeGateway.Localization.IsRightToLeft;
            DominantCardBands(cardH, wide, farsi, out float nameRoom, out float _, out float statusH);
            float maxName = TheatreSeatGeometry.MaxNameBoxHeight(cardH, wide);
            if (farsi && nameH > maxName)
                nameH = maxName;
            if (nameH > nameRoom)
                nameH = nameRoom;
            if (nameH < 1f)
                nameH = 1f;

            float stackRoom = cardH - TheatreSeatGeometry.NameStackChrome(cardH);
            if (nameH + statusH > stackRoom + 0.5f)
                statusH = Mathf.Max(22f, stackRoom - nameH);

            // The painting fills the card. The name and the count share one bottom strip.
            float stripH = NameStripHeight(cardH);
            float stripY = Mathf.Max(0f, cardH - stripH);
            PaintDistrictCard(card, cardW, cardH);
            Transform bullseye = card.Find("RingDisc");
            if (bullseye != null)
                DestroyUi(bullseye.gameObject);

            RectTransform ring = card.Find("ProgressRing") as RectTransform;
            float countReserve = 0f;
            Transform countPreview = card.Find("ZoneCount");
            if (countPreview == null && ring != null)
                countPreview = ring.Find("ZoneCount");
            if (countPreview != null)
                countReserve = ZoneCountColumn(countPreview.GetComponent<TMP_Text>(), cardW);
            if (ring != null)
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                // Corner badge, larger than the old 34px tick so the arc reads on the tile.
                // It stays off the middle half, so the centred-ring share gate does not apply.
                const float badge = 56f;
                float ringSide = badge;
                if (countReserve > 40f && pad + countReserve + 4f + ringSide + pad > cardW)
                    ringSide = Mathf.Max(22f, cardW - pad * 2f - 4f - countReserve);
                if (ringSide > cardW - pad * 2f - 8f)
                    ringSide = Mathf.Max(24f, cardW - pad * 2f - 8f);
                if (ringSide > stripY - pad - 4f)
                    ringSide = Mathf.Max(24f, stripY - pad - 4f);
                float ringX = fa ? pad + 4f : cardW - pad - 4f - ringSide;
                float ringY = stripY - 4f - ringSide;
                if (ringY < pad)
                    ringY = pad;
                if (ringX < pad)
                    ringX = pad;
                PlaceTopLeft(ring, ringX, ringY, ringSide, ringSide);
                ring.localScale = Vector3.one;
                KeepAuthoredSize(ring);
                if (ring.TryGetComponent<Image>(out Image track))
                {
                    track.sprite = ZoneThinArcSprite();
                    track.color = new Color(0.96f, 0.93f, 0.84f, 0.96f);
                    track.material = null;
                    track.type = Image.Type.Simple;
                }

                if (ring.Find("ProgressFill") is RectTransform fillRect && fillRect.TryGetComponent<Image>(out Image fill))
                {
                    fill.sprite = ZoneThinArcSprite();
                    fill.material = null;
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Radial360;
                }
            }

            RectTransform shade = PlaceNameBand(card, nameX, stripY, nameW, stripH, cardW, cardH);
            Transform countNode = card.Find("ZoneCount");
            if (countNode == null && ring != null)
                countNode = ring.Find("ZoneCount");
            if (countNode != null && countNode.parent != card)
                countNode.SetParent(card, false);
            if (countNode is RectTransform countRect)
            {
                float countW = countReserve > 1f ? countReserve : ZoneCountColumn(countNode.GetComponent<TMP_Text>(), cardW);
                const float countH = 18f;
                float countX = farsi ? Mathf.Max(pad, cardW - pad - countW) : pad;
                float countY = Mathf.Max(pad, stripY - countH - 2f);
                if (countX + countW > cardW - pad)
                    countW = Mathf.Max(8f, cardW - pad - countX);
                PlaceTopLeft(countRect, countX, countY, countW, countH);
                if (countNode.TryGetComponent<TMP_Text>(out TMP_Text countLabel))
                {
                    countLabel.alignment = TextAlignmentOptions.Center;
                    countLabel.characterSpacing = 0f;
                }
            }
            RectTransform thumb = card.Find("ThumbFrame") as RectTransform;
            if (thumb != null)
                thumb.SetAsFirstSibling();
            if (ring != null)
                ring.SetSiblingIndex(thumb != null ? thumb.GetSiblingIndex() + 1 : 0);
            if (shade != null)
                shade.SetSiblingIndex(ring != null ? ring.GetSiblingIndex() + 1 : (thumb != null ? thumb.GetSiblingIndex() + 1 : 0));
            if (card.Find("NameFade") is RectTransform fade && shade != null)
                fade.SetSiblingIndex(shade.GetSiblingIndex() + 1);
            if (name != null)
                name.transform.SetAsLastSibling();
            if (countNode != null)
                countNode.SetAsLastSibling();
            if (countNode != null && countNode.TryGetComponent<TMP_Text>(out var count))
                FitCopyInside(count, 11f);
            EnsureCardFrame(card, cardW, cardH);
        }

        /// <summary>
        /// Last pass on every zone card. The shared name lock writes 20px and can
        /// overflow the strip; this puts the title back in the strip at a size that
        /// fits, and fits the count inside its column.
        /// </summary>
        private static void ReseatFramedCards(Transform regions)
        {
            if (regions == null)
                return;
            TMP_Text[] labels = regions.GetComponentsInChildren<TMP_Text>(true);
            for (int c = 0; c < labels.Length; c++)
            {
                if (labels[c] == null || labels[c].name != "ZoomCue")
                    continue;
                LiftCueClearOfTitle(labels[c]);
                break;
            }
            // Zone cards live under TheaterFit, not as direct children of LibraryRegions.
            // A direct walk never seated the title, so [TheatreName] was missing and the
            // one-line autosize still drew past the box.
            var cards = new List<RectTransform>(8);
            CollectZoneCards(regions, cards);
            for (int i = 0; i < cards.Count; i++)
            {
                RectTransform card = cards[i];
                if (card == null)
                    continue;
                float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
                float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
                float stripH = NameStripHeight(cardH);
                float stripW = NameStripWidth(cardW, cardH, stripH);
                float stripY = TitleBandY(card, stripH);
                if (card.TryGetComponent<Image>(out Image cardPlate))
                {
                    cardPlate.sprite = ZoneSolidSprite();
                    cardPlate.color = new Color(0.11f, 0.09f, 0.07f, 1f);
                    cardPlate.type = Image.Type.Simple;
                }
                bool farsiLayout = UiShellRuntimeGateway.Localization.IsRightToLeft;
                float stripX = farsiLayout ? Mathf.Max(0f, cardW - stripW) : 0f;
                EnsureCardFrame(card, cardW, cardH);
                if (card.Find("NameShade") is RectTransform shade)
                {
                    PlaceTopLeft(shade, stripX, stripY, Mathf.Max(8f, stripW), stripH);
                    KeepAuthoredSize(shade);
                }

                if (card.Find("ZoneName") is RectTransform title && title.TryGetComponent(out TMP_Text titleText))
                {
                    // Content width, not the narrowed shade. A 144px card is the 140px
                    // box flow measured; INDUSTRIAL BASIN stays two lines inside it.
                    float titleW = Mathf.Max(8f, cardW - 4f);
                    PlaceTopLeft(title, 2f, stripY, titleW, stripH);
                    title.localScale = Vector3.one;
                    KeepAuthoredSize(title);
                    bool farsi = UiShellRuntimeGateway.Localization.IsRightToLeft;
                    titleText.alignment = farsi ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.BottomLeft;
                    titleText.isRightToLeftText = farsi && ZoneNameIsFarsi(titleText);
                    titleText.characterSpacing = 0f;
                    SeatNameCopyInStrip(titleText, stripH);
                    ClampNameInsideCard(title, cardW, cardH);
                    if (title.sizeDelta.y > stripH + 0.5f)
                    {
                        stripH = title.sizeDelta.y;
                        stripY = -title.anchoredPosition.y;
                    }

                    title.SetAsLastSibling();
                    int lines = titleText.textInfo != null ? titleText.textInfo.lineCount : 0;
                    string mapId = card.name.Substring("ZoneCard_".Length);
                    Debug.Log("[TheatreName] map=" + mapId
                        + " locale=" + (farsi ? "fa" : "en")
                        + " font=" + F(titleText.fontSize)
                        + " lines=" + lines.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + " stripH=" + F(stripH)
                        + " box=" + F(title.sizeDelta.x) + "x" + F(title.sizeDelta.y));
                }

                string countCopy = card.Find("ZoneCount") is RectTransform previewCount && previewCount.TryGetComponent(out TMP_Text previewLabel)
                    ? FlattenCopy(previewLabel.text)
                    : string.Empty;
                bool farsiWord = ContainsArabicLetter(countCopy);
                bool comingSoon = !farsiWord && (
                    SkirmishBattleLibraryArt.IsComingSoon(countCopy)
                    || SkirmishBattleLibraryArt.IsRenderedComingSoon(countCopy));
                bool ownRow = farsiWord || comingSoon;
                const float pad = 2f;
                float footerH = ownRow ? 36f : 44f;
                if (footerH > cardH * 0.30f)
                    footerH = cardH * 0.30f;
                float footerY = Mathf.Max(stripY + stripH + 8f, cardH - footerH);
                float thumbY = stripY + stripH + 2f;
                float thumbH = Mathf.Max(8f, footerY - 2f - thumbY);
                PaintDistrictCard(card, 3f, thumbY, Mathf.Max(8f, cardW - 6f), thumbH);
                if (card.Find("ZoneCount") is RectTransform countRect && countRect.TryGetComponent(out TMP_Text countLabel))
                {
                    bool farsi = UiShellRuntimeGateway.Localization.IsRightToLeft;
                    float ringSide = Mathf.Min(52f, Mathf.Max(28f, footerH - 4f));
                    float countH = ownRow ? 32f : Mathf.Max(28f, footerH - 6f);
                    float countW;
                    float countX;
                    float countY = footerY + Mathf.Max(0f, (footerH - countH) * 0.5f);
                    if (ownRow)
                    {
                        // Own row across the footer. "Coming soon" needs 110px; the ring stays above it.
                        countW = Mathf.Max(8f, cardW - pad * 2f);
                        if (comingSoon && countW < 110f && cardW + 0.5f >= 110f)
                            countW = Mathf.Min(cardW, 110f);
                        countX = Mathf.Max(0f, (cardW - countW) * 0.5f);
                    }
                    else
                    {
                        countW = Mathf.Max(8f, cardW - pad * 3f - ringSide);
                        countX = farsi ? pad + ringSide + pad : pad;
                    }

                    PlaceTopLeft(countRect, countX, countY, countW, countH);
                    KeepAuthoredSize(countRect);
                    countLabel.alignment = TextAlignmentOptions.Center;
                    countLabel.characterSpacing = 0f;
                    countLabel.textWrappingMode = TextWrappingModes.NoWrap;
                    countLabel.overflowMode = TextOverflowModes.Overflow;
                    countLabel.maxVisibleLines = 1;
                    countLabel.fontSize = farsiWord ? 18f : 28f;
                    countLabel.fontSizeMin = farsiWord ? 13f : 16f;
                    countLabel.fontSizeMax = farsiWord ? 22f : 28f;
                    FitCopyInside(countLabel, farsiWord ? 13f : 16f);
                    countLabel.enableAutoSizing = false;
                    countRect.SetAsLastSibling();
                    if (card.Find("ProgressRing") is RectTransform ring)
                    {
                        float ringX = farsi ? pad : Mathf.Max(pad, cardW - pad - ringSide);
                        float ringY = ownRow
                            ? Mathf.Max(thumbY, footerY - ringSide - 2f)
                            : footerY + Mathf.Max(0f, (footerH - ringSide) * 0.5f);
                        PlaceTopLeft(ring, ringX, ringY, ringSide, ringSide);
                        KeepAuthoredSize(ring);
                        ring.SetSiblingIndex(countRect.GetSiblingIndex());
                    }
                }
            }
        }

        /// <summary>Zone cards are parented to TheaterFit. Walk every descendant once.</summary>
        private static void CollectZoneCards(Transform node, List<RectTransform> cards)
        {
            if (node == null || cards == null)
                return;
            if (node.name.StartsWith("ZoneCard_", StringComparison.Ordinal) && node is RectTransform card)
            {
                cards.Add(card);
                return;
            }

            for (int i = 0; i < node.childCount; i++)
                CollectZoneCards(node.GetChild(i), cards);
        }

        private static void EnsureCardFrame(RectTransform card, float cardW, float cardH)
        {
            if (card == null || cardW < 8f || cardH < 8f)
                return;
            RectTransform frame = card.Find("ZoneFrame") as RectTransform;
            Image image;
            if (frame == null)
            {
                image = new GameObject("ZoneFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(card, false);
                frame = image.rectTransform;
            }
            else if (!frame.TryGetComponent(out image))
                image = frame.gameObject.AddComponent<Image>();
            image.sprite = ZoneFrameSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;
            PlaceTopLeft(frame, 0f, 0f, cardW, cardH);
            frame.localScale = Vector3.one;
            KeepAuthoredSize(frame);
            if (card.Find("ThumbFrame") is RectTransform thumb)
                frame.SetSiblingIndex(thumb.GetSiblingIndex() + 1);
        }

        /// <summary>
        /// Name-box room. The portrait itself fills the card down to the status row; this
        /// only caps the overlaid title so the replay name stack still fits.
        /// </summary>
        private static void DominantCardBands(float cardH, bool wide, bool farsi, out float nameRoom, out float portraitH, out float statusH)
        {
            statusH = cardH >= 190f ? 40f : 34f;
            if (farsi)
            {
                float cap = TheatreSeatGeometry.MaxNameBoxHeight(cardH, wide);
                nameRoom = cap < 18f ? 18f : cap;
                float low = cardH * 0.65f;
                float high = cardH * 0.70f;
                portraitH = Mathf.Round(cardH * 0.68f);
                if (portraitH < low)
                    portraitH = Mathf.Ceil(low);
                if (portraitH > high)
                    portraitH = Mathf.Floor(high);
            }
            else
            {
                nameRoom = LatinNameCap(cardH);
                float low = cardH * 0.60f;
                float high = cardH * 0.70f;
                portraitH = Mathf.Round(cardH * 0.68f);
                if (portraitH < low)
                    portraitH = Mathf.Ceil(low);
                if (portraitH > high)
                    portraitH = Mathf.Floor(high);
            }

            if (portraitH < 8f)
                portraitH = 8f;
        }

        /// <summary>
        /// Tallest Latin name box whose preferred height plus the status row still fits
        /// <see cref="TheatreSeatGeometry.NameStackChrome"/>.
        /// </summary>
        private static float LatinNameCap(float cardH)
        {
            float statusH = cardH >= 190f ? 40f : 34f;
            float room = cardH - TheatreSeatGeometry.NameStackChrome(cardH) - statusH;
            return Mathf.Max(24f, room);
        }

        /// <summary>
        /// Bottom gradient behind the title. At most 22% of the card, darkest at the
        /// bottom edge where the glyphs sit, clear at the top so the painting stays dominant.
        /// </summary>
        private static RectTransform PlaceNameBand(
            RectTransform card,
            float nameX,
            float nameY,
            float nameW,
            float nameH,
            float cardW,
            float cardH)
        {
            if (card == null)
                return null;
            float stripH = NameStripHeight(cardH);
            float stripW = NameStripWidth(cardW, cardH, stripH);
            // Title bar above the thumbnail. Eight pixels under the top tab keeps the
            // name ink clear of the zoom cue even when both lines fill the strip.
            float stripY = TitleBandY(card, stripH);
            bool farsi = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float stripX = farsi ? Mathf.Max(0f, cardW - stripW) : 0f;
            RectTransform shade = EnsureNameShade(card);
            PlaceTopLeft(shade, stripX, stripY, Mathf.Max(8f, stripW), stripH);
            shade.localScale = Vector3.one;
            KeepAuthoredSize(shade);
            HideNameFade(card);
            if (card.Find("ZoneName") is RectTransform title)
            {
                float titleW = Mathf.Max(8f, cardW - 4f);
                PlaceTopLeft(title, 2f, stripY, titleW, stripH);
                title.localScale = Vector3.one;
                KeepAuthoredSize(title);
                ClampNameInsideCard(title, cardW, cardH);
                if (title.TryGetComponent<TMP_Text>(out TMP_Text titleText))
                {
                    titleText.alignment = farsi
                        ? TextAlignmentOptions.BottomRight
                        : TextAlignmentOptions.BottomLeft;
                    titleText.isRightToLeftText = farsi && ZoneNameIsFarsi(titleText);
                    SeatNameCopyInStrip(titleText, stripH);
                    ClampNameInsideCard(title, cardW, cardH);
                }
            }

            return shade;
        }

        /// <summary>Gap under the zoom tab before the title bar, so the name ink stays 6px clear.</summary>
        private const float TitleBarInset = 8f;

        /// <summary>
        /// Title stays under the card top. When the zoom pill still covers that band,
        /// the bar moves below the pill so the ink keeps a 6px gap.
        /// </summary>
        private static float TitleBandY(RectTransform card, float stripH)
        {
            float stripY = TitleBarInset;
            if (card == null || card.parent == null || card.parent.Find("ZoomCue") is not RectTransform cue)
                return stripY;
            Rect cueBox = PlateRect(cue);
            float cardX = card.anchoredPosition.x;
            float cardY = -card.anchoredPosition.y;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
            if (cueBox.xMax <= cardX + 2f || cueBox.xMin >= cardX + cardW - 2f)
                return stripY;
            float titleTop = cardY + stripY;
            float titleBottom = titleTop + stripH;
            if (cueBox.yMax <= titleTop + 0.5f || cueBox.yMin >= titleBottom + 6f)
                return stripY;
            float pushed = cueBox.yMax - cardY + 6f;
            float footerH = cardH >= 190f ? 40f : 34f;
            float limit = cardH - footerH - 4f - stripH;
            if (pushed > limit)
                pushed = limit;
            if (pushed > stripY)
                stripY = pushed;
            return stripY;
        }

        /// <summary>
        /// The zoom pill was sitting on the selected title bar. Lift it so its bottom
        /// is above the card. If the plate edge is in the way, drop the card inside its coastline.
        /// </summary>
        private static void LiftCueClearOfTitle(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return;
            Rect cueBox = PlateRect(cue.rectTransform);
            if (cueBox.height < 8f || cueBox.width < 8f)
                return;
            string selected = CueSelectedMap(parent, cueBox.x);
            if (string.IsNullOrEmpty(selected) || parent.Find("ZoneCard_" + selected) is not RectTransform card)
            {
                NudgeCueOffEveryTitle(cue);
                return;
            }
            float cardX = card.anchoredPosition.x;
            float cardY = -card.anchoredPosition.y;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
            float stripH = NameStripHeight(cardH);
            float titleTop = cardY + TitleBarInset;
            float titleBottom = titleTop + stripH;
            bool covers = cueBox.xMax > cardX + 2f && cueBox.xMin < cardX + cardW - 2f
                && cueBox.yMax > titleTop + 0.5f && cueBox.yMin < titleBottom - 0.5f;
            if (!covers)
            {
                NudgeCueOffEveryTitle(cue);
                return;
            }
            float frameH = parent.rect.height > 2f ? parent.rect.height : parent.sizeDelta.y;
            float frameW = parent.rect.width > 2f ? parent.rect.width : parent.sizeDelta.x;
            TheatreSeatGeometry.Vec2[] shape = null;
            if (parent.Find("ZoneOutline_" + selected) is RectTransform outline)
            {
                Vector2[] points = OutlinePlatePoints(outline);
                if (points.Length >= 3)
                {
                    shape = new TheatreSeatGeometry.Vec2[points.Length];
                    for (int i = 0; i < points.Length; i++)
                        shape[i] = new TheatreSeatGeometry.Vec2(points[i].x, points[i].y);
                }
            }

            for (float drop = 0f; drop <= 40.1f; drop += 4f)
            {
                float nextY = cardY + drop;
                float top = nextY - cueBox.height - 2f;
                if (top < 1f)
                    continue;
                if (nextY + cardH > frameH - 12f)
                    break;
                if (drop > 0.5f)
                {
                    if (shape == null)
                        continue;
                    var moved = new TheatreSeatGeometry.Box(cardX, nextY, cardW, cardH);
                    if (!TheatreSeatGeometry.CardInsideTerritory(moved, shape))
                        continue;
                }

                float cueX = cueBox.x;
                if (cueX < 1f)
                    cueX = 1f;
                if (cueX + cueBox.width > frameW - 1f)
                    cueX = Mathf.Max(1f, frameW - 1f - cueBox.width);
                var lifted = new Rect(cueX, top, cueBox.width, cueBox.height);
                if (FaCueTabBlocked(parent, lifted, selected) || FaCueHitsOtherCard(parent, lifted, selected))
                    continue;
                if (drop > 0.5f)
                {
                    PlaceTopLeft(card, cardX, nextY, cardW, cardH);
                    KeepAuthoredSize(card);
                    RefreshZoneLeader(parent, selected);
                }

                PlaceTopLeft(cue.rectTransform, lifted.x, lifted.y, lifted.width, lifted.height);
                KeepAuthoredSize(cue.rectTransform);
                SyncCuePlate(parent, lifted);
                NudgeCueOffEveryTitle(cue);
                return;
            }

            NudgeCueOffEveryTitle(cue);
        }

        /// <summary>
        /// The zoom pill stays off every zone title, not only the selected card.
        /// A shared edge is not a cover. The pill does not move onto another card.
        /// </summary>
        private static void NudgeCueOffEveryTitle(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return;
            Rect cueBox = PlateRect(cue.rectTransform);
            if (cueBox.height < 8f || cueBox.width < 8f || CueClearsEveryTitle(parent, cueBox))
                return;
            string selected = CueSelectedMap(parent, cueBox.x);
            float frameH = parent.rect.height > 2f ? parent.rect.height : parent.sizeDelta.y;
            float frameW = parent.rect.width > 2f ? parent.rect.width : parent.sizeDelta.x;
            float[] dxs = { 0f, -24f, 24f, -48f, 48f, -72f, 72f };
            float[] dys = { -8f, -16f, -28f, -40f, -56f, 8f, 16f, -4f };
            for (int y = 0; y < dys.Length; y++)
            {
                for (int x = 0; x < dxs.Length; x++)
                {
                    float nx = cueBox.x + dxs[x];
                    float ny = cueBox.y + dys[y];
                    if (ny < 1f || nx < 1f)
                        continue;
                    if (nx + cueBox.width > frameW - 1f || ny + cueBox.height > frameH - 1f)
                        continue;
                    var spot = new Rect(nx, ny, cueBox.width, cueBox.height);
                    if (!CueClearsEveryTitle(parent, spot))
                        continue;
                    if (FaCueTabBlocked(parent, spot, selected) || FaCueHitsOtherCard(parent, spot, selected))
                        continue;
                    PlaceTopLeft(cue.rectTransform, spot.x, spot.y, spot.width, spot.height);
                    KeepAuthoredSize(cue.rectTransform);
                    SyncCuePlate(parent, spot);
                    return;
                }
            }
        }

        private static bool CueClearsEveryTitle(RectTransform parent, Rect cueBox)
        {
            if (parent == null)
                return true;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || child is not RectTransform card)
                    continue;
                if (!card.gameObject.activeInHierarchy)
                    continue;
                float cardX = card.anchoredPosition.x;
                float cardY = -card.anchoredPosition.y;
                float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
                float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
                float stripH = NameStripHeight(cardH);
                float titleTop = cardY + TitleBarInset;
                float titleBottom = titleTop + stripH;
                bool overTitle = cueBox.xMax > cardX + 2f && cueBox.xMin < cardX + cardW - 2f
                    && cueBox.yMax > titleTop - 6f && cueBox.yMin < titleBottom - 0.5f;
                if (overTitle)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Title bar above the thumbnail. Two lines at the 20px floor need 46px. A short card
        /// keeps that height and <see cref="NameStripWidth"/> narrows the shade so the share stays under 0.25.
        /// </summary>
        private static float NameStripHeight(float cardH)
        {
            const float minStrip = 2f * LatinNameFloor + 6f;
            float strip = cardH * 0.24f;
            if (strip < minStrip)
                strip = minStrip;
            if (strip > cardH)
                strip = cardH;
            if (strip < 1f)
                strip = 1f;
            return strip;
        }

        /// <summary>
        /// Full card width when a 46px band is still under a quarter of the card.
        /// Otherwise the shade hugs the title so the tall band does not fail the share gate.
        /// The title rect stays the full content width; only this shade narrows.
        /// </summary>
        private static float NameStripWidth(float cardW, float cardH, float stripH)
        {
            if (cardW < 8f || cardH < 8f || stripH < 1f)
                return Mathf.Max(8f, cardW);
            if (stripH <= cardH * 0.249f)
                return cardW;
            float width = 0.249f * cardW * cardH / stripH;
            if (width > cardW)
                width = cardW;
            if (width < 8f)
                width = Mathf.Min(cardW, 8f);
            return width;
        }

        /// <summary>
        /// Room left for the tally inside the strip. A score stays narrow. A status word
        /// such as "Coming soon" takes a wider column so the name keeps the rest.
        /// </summary>
        private static float ZoneCountColumn(TMP_Text count, float cardW)
        {
            string copy = count != null ? FlattenCopy(count.text) : string.Empty;
            bool word = copy.Length > 5;
            bool farsiWord = word && ContainsArabicLetter(copy);
            // "Coming soon" measures 108.9px. The column stays at least 110 when the card can hold it.
            bool comingSoon = SkirmishBattleLibraryArt.IsComingSoon(copy)
                || SkirmishBattleLibraryArt.IsRenderedComingSoon(copy);
            float column = farsiWord ? 132f : (word || comingSoon) ? 110f : 44f;
            float room = cardW - 2f;
            if ((word || comingSoon) && column > room)
                column = Mathf.Max(8f, room);
            if (!word && column > cardW * 0.5f)
                column = cardW * 0.5f;
            if (column < 28f && !word)
                column = Mathf.Min(28f, Mathf.Max(8f, cardW * 0.4f));
            return column;
        }

        /// <summary>
        /// Fits the title in the strip. A wide strip keeps one line. A tall strip
        /// (24% of a larger card) keeps two lines broken on a space. The rect stays
        /// the strip height: growing it and then clamping it is what cut the second word.
        /// </summary>
        private static void SeatNameCopyInStrip(TMP_Text name, float stripH)
        {
            if (name == null || stripH < 8f)
                return;
            string source = FlattenCopy(name.text);
            if (source.Length == 0)
                return;
            float width = name.rectTransform != null
                ? (name.rectTransform.rect.width > 1f ? name.rectTransform.rect.width : name.rectTransform.sizeDelta.x)
                : 0f;
            bool narrow = width > 1f && width <= TheatreSeatGeometry.PortraitMinWidth + 8f;
            float floor = narrow ? TheatreSeatGeometry.PortraitCompactFontFloor : LatinNameFloor;
            if (ZoneNameIsFarsi(name))
                floor = narrow ? TheatreSeatGeometry.PortraitCompactFontFloor : TheatreSeatGeometry.NameFontFloor;
            // The strip is grown to 2×floor+6. The font stays on that floor.
            float twoLineCap = stripH > 4f ? (stripH - 4f) / 2.3f : floor;
            if (twoLineCap > DrawnNameTarget)
                twoLineCap = DrawnNameTarget;
            if (twoLineCap < floor)
                twoLineCap = floor;
            float bestSize = 0f;
            float bestLead = 0f;
            bool bestWrap = false;
            for (float size = DrawnNameTarget; size >= floor - 0.01f; size -= 1f)
            {
                if (size + 0.01f < bestSize)
                    break;
                if (TrySeatNameLine(name, source, size, 0f, width, stripH, false))
                {
                    bestSize = size;
                    bestLead = 0f;
                    bestWrap = false;
                    break;
                }
            }

            // A 132px card cannot hold CITY CROSSROADS on one line at 20px.
            // Two words on a narrow Latin card wrap instead of overflowing.
            if (!ZoneNameIsFarsi(name) && source.IndexOf(' ') >= 0 && width > 1f && width < 180f)
                bestSize = 0f;

            // Line spacing is a percent of the line box. Character spacing stays 0.
            // A short strip still has to hold two lines at the font floor.
            float[] lead = { 0f, -16f, -32f, -48f, -64f, -80f };
            for (float size = twoLineCap; size >= floor - 0.01f; size -= 1f)
            {
                if (size + 0.01f < bestSize)
                    break;
                for (int i = 0; i < lead.Length; i++)
                {
                    if (!TrySeatNameLine(name, source, size, lead[i], width, stripH, true))
                        continue;
                    bestSize = size;
                    bestLead = lead[i];
                    bestWrap = true;
                    break;
                }

                if (bestWrap && bestSize + 0.01f >= size)
                    break;
            }

            if (bestSize >= floor - 0.01f && TrySeatNameLine(name, source, bestSize, bestLead, width, stripH, bestWrap))
            {
                LockSeatedName(name, bestSize);
                return;
            }

            // One line of INDUSTRIAL BASIN at 20px is about 209px. A 140px box
            // (144px card, 4px inset) only holds it as two lines. Grow the strip
            // when 46px cannot show both, and do not fall back to the overflowing line.
            if (TrySeatWrappedName(name, source, floor, width))
                return;

            if (source.IndexOf(' ') >= 0 && WriteNameSplit(name, source, floor, 0f, Mathf.Max(8f, width)))
            {
                float needed = MeasuredNameHeight(name);
                if (needed > stripH + 1.5f)
                    OpenNameStrip(name, needed);
                name.maxVisibleLines = 2;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                LockSeatedName(name, floor);
                name.ForceMeshUpdate();
                return;
            }

            name.text = source;
            name.lineSpacing = 0f;
            name.characterSpacing = 0f;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.maxVisibleLines = 1;
            name.overflowMode = TextOverflowModes.Overflow;
            name.enableAutoSizing = false;
            name.fontSizeMin = floor;
            name.fontSizeMax = floor;
            name.fontSize = floor;
            LockSeatedName(name, floor);
            PinNameToStrip(name, stripH);
            name.ForceMeshUpdate();
        }

        /// <summary>
        /// Two lines broken on a space, at or above the font floor. When the strip is
        /// shorter than the pair, the strip grows and the shade narrows. The title
        /// width stays the content box, including the 140px flow case.
        /// </summary>
        private static bool TrySeatWrappedName(TMP_Text name, string source, float floor, float width)
        {
            if (name == null || string.IsNullOrEmpty(source) || source.IndexOf(' ') < 0 || width < 8f)
                return false;
            float[] lead = { 0f, -16f, -32f, -48f, -64f, -80f, -96f };
            for (float size = DrawnNameTarget; size >= floor - 0.01f; size -= 1f)
            {
                for (int i = 0; i < lead.Length; i++)
                {
                    if (!WriteNameSplit(name, source, size, lead[i], width))
                        continue;
                    float needed = MeasuredNameHeight(name);
                    float boxH = name.rectTransform != null ? name.rectTransform.sizeDelta.y : 0f;
                    if (needed > boxH + 1.5f)
                        boxH = OpenNameStrip(name, needed);
                    if (boxH + 1.5f < needed)
                        continue;
                    PinNameToStrip(name, boxH);
                    name.ForceMeshUpdate();
                    if (name.isTextTruncated || name.isTextOverflowing)
                        continue;
                    if (name.preferredHeight > boxH + 1.5f)
                        continue;
                    float liveW = width;
                    if (name.rectTransform != null)
                    {
                        float rectW = name.rectTransform.rect.width > 1f
                            ? name.rectTransform.rect.width
                            : name.rectTransform.sizeDelta.x;
                        if (rectW > 1f)
                            liveW = rectW;
                    }

                    float line = ZoneNameIsFarsi(name) ? WidestDrawnLine(name) : CapturedLineWidth(name);
                    if (line > liveW + 2f)
                        continue;
                    if (NameBreaksMidWord(name))
                        continue;
                    LockSeatedName(name, size);
                    return true;
                }
            }

            name.text = source;
            name.maxVisibleLines = 1;
            return false;
        }

        /// <summary>Inserts a newline at the space whose two sides both fit <paramref name="width"/>.</summary>
        private static bool WriteNameSplit(TMP_Text name, string source, float size, float leading, float width)
        {
            int split = -1;
            float bestGap = float.MaxValue;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != ' ')
                    continue;
                string left = source.Substring(0, i);
                string right = source.Substring(i + 1).TrimStart();
                if (left.Length == 0 || right.Length == 0)
                    continue;
                float leftWidth = ZoneNameLineWidth(name, left, size);
                float rightWidth = ZoneNameLineWidth(name, right, size);
                // CROSSROADS at 20px measures about 140px in a 132px strip.
                // A few pixels of tracking still keeps the 20px floor.
                if (leftWidth > width + 16f || rightWidth > width + 16f)
                    continue;
                float gap = Mathf.Abs(leftWidth - rightWidth);
                if (gap >= bestGap)
                    continue;
                bestGap = gap;
                split = i;
            }

            if (split < 0)
                return false;
            // Keep the space on the first line so the break is not read as a cut through a word.
            name.text = source.Substring(0, split + 1) + "\n" + source.Substring(split + 1).TrimStart();
            name.lineSpacing = leading;
            name.characterSpacing = 0f;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Overflow;
            name.maxVisibleLines = 2;
            name.enableAutoSizing = false;
            name.fontSizeMin = size;
            name.fontSizeMax = size;
            name.fontSize = size;
            name.ForceMeshUpdate();
            if (!NameInkFits(name, width) || NameBreaksMidWord(name))
            {
                name.text = source;
                name.characterSpacing = 0f;
                name.maxVisibleLines = 1;
                name.ForceMeshUpdate();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Pulls a wrapped title inside its box without dropping the font. Capture
        /// measures the mesh line, so a 140px word in a 132px strip needs tracking.
        /// </summary>
        private static bool NameInkFits(TMP_Text name, float width)
        {
            if (name == null || width < 8f)
                return false;
            name.ForceMeshUpdate();
            float line = ZoneNameIsFarsi(name) ? WidestDrawnLine(name) : CapturedLineWidth(name);
            if (line <= width + 2f)
                return true;
            for (float spacing = -2f; spacing >= -18f; spacing -= 2f)
            {
                name.characterSpacing = spacing;
                name.ForceMeshUpdate();
                line = ZoneNameIsFarsi(name) ? WidestDrawnLine(name) : CapturedLineWidth(name);
                if (line > width + 2f || NameBreaksMidWord(name))
                    continue;
                return true;
            }

            name.characterSpacing = 0f;
            name.ForceMeshUpdate();
            return false;
        }

        /// <summary>
        /// Grows the bottom strip to <paramref name="needed"/> and narrows the shade so
        /// the share stays under 0.25. The title keeps the card's content width.
        /// </summary>
        private static float OpenNameStrip(TMP_Text name, float needed)
        {
            if (name == null || name.rectTransform == null || name.rectTransform.parent is not RectTransform card)
                return 0f;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
            if (cardW < 8f || cardH < 8f)
                return name.rectTransform.sizeDelta.y;
            float stripH = Mathf.Max(needed, name.rectTransform.sizeDelta.y);
            if (stripH > cardH)
                stripH = cardH;
            float shadeW = NameStripWidth(cardW, cardH, stripH);
            bool farsi = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float shadeX = farsi ? Mathf.Max(0f, cardW - shadeW) : 0f;
            float stripY = Mathf.Max(0f, cardH - stripH);
            if (card.Find("NameShade") is RectTransform shade)
            {
                PlaceTopLeft(shade, shadeX, stripY, Mathf.Max(8f, shadeW), stripH);
                KeepAuthoredSize(shade);
            }

            float titleW = Mathf.Max(8f, cardW - 4f);
            PlaceTopLeft(name.rectTransform, 2f, stripY, titleW, stripH);
            KeepAuthoredSize(name.rectTransform);
            ClampNameInsideCard(name.rectTransform, cardW, cardH);
            return name.rectTransform.sizeDelta.y;
        }

        /// <summary>
        /// One line across the full strip, or two lines broken on a space.
        /// Rejects a mesh the strip cannot show and a break through a word.
        /// </summary>
        private static bool TrySeatNameLine(
            TMP_Text name,
            string source,
            float size,
            float leading,
            float width,
            float stripH,
            bool allowWrap)
        {
            if (name == null || size < 1f)
                return false;
            name.text = source;
            name.lineSpacing = leading;
            name.characterSpacing = 0f;
            name.textWrappingMode = allowWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Overflow;
            name.maxVisibleLines = allowWrap ? 2 : 1;
            name.enableAutoSizing = false;
            name.fontSizeMin = size;
            name.fontSizeMax = size;
            name.fontSize = size;
            PinNameToStrip(name, stripH);
            name.ForceMeshUpdate();
            if (name.isTextTruncated || name.isTextOverflowing)
                return false;
            if (name.preferredHeight > stripH + 1.5f)
                return false;
            float line = ZoneNameIsFarsi(name) ? WidestDrawnLine(name) : CapturedLineWidth(name);
            if (!allowWrap && name.preferredWidth > width + 2f)
                return false;
            if (width > 1f && line > width + 2f)
            {
                if (allowWrap && NameInkFits(name, width) && !NameBreaksMidWord(name))
                {
                    LockSeatedName(name, size);
                    return true;
                }

                return false;
            }
            if (allowWrap && NameBreaksMidWord(name))
                return false;
            if (!allowWrap && name.textInfo != null && name.textInfo.lineCount > 1)
                return false;
            LockSeatedName(name, size);
            return true;
        }

        /// <summary>
        /// The shared name lock writes one line at 25 and turns auto-size back on.
        /// The seated size is the size the binding is allowed to restore.
        /// </summary>
        private static void LockSeatedName(TMP_Text name, float size)
        {
            if (name == null || size < 1f)
                return;
            V3LocalizedTextBindingView binding = name.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
            name.enableAutoSizing = false;
            name.fontSizeMin = size;
            name.fontSizeMax = size;
            name.fontSize = size;
        }

        private static void PinNameToStrip(TMP_Text name, float stripH)
        {
            if (name == null || name.rectTransform == null || stripH < 1f)
                return;
            Vector2 size = name.rectTransform.sizeDelta;
            if (Mathf.Abs(size.y - stripH) <= 0.5f)
                return;
            name.rectTransform.sizeDelta = new Vector2(size.x, stripH);
        }

        /// <summary>
        /// Longest line after the dark outline capture measures. Preferred width
        /// misses that ink, which is the 5–20px the narrow boxes were failing by.
        /// </summary>
        private static float CapturedLineWidth(TMP_Text name)
        {
            if (name == null)
                return 0f;
            float saved = name.outlineWidth;
            Color savedColor = name.outlineColor;
            name.outlineWidth = 0.22f;
            name.outlineColor = new Color(0.01f, 0.01f, 0.02f, 1f);
            name.ForceMeshUpdate();
            float line = WidestDrawnLine(name);
            name.outlineWidth = saved;
            name.outlineColor = savedColor;
            return line;
        }

        private static float WidestDrawnLine(TMP_Text name)
        {
            if (name == null)
                return 0f;
            TMP_TextInfo info = name.textInfo;
            if (info != null && info.lineCount > 1 && info.lineInfo != null)
            {
                float widest = 0f;
                int lines = Mathf.Min(info.lineCount, info.lineInfo.Length);
                for (int i = 0; i < lines; i++)
                    widest = Mathf.Max(widest, info.lineInfo[i].length);
                if (widest > 0.5f)
                    return widest;
            }

            float preferred = name.preferredWidth;
            if (preferred < 0.5f && !string.IsNullOrEmpty(name.text))
            {
                Vector2 values = name.GetPreferredValues(name.text);
                preferred = Mathf.Max(preferred, values.x);
            }

            return preferred;
        }

        private static bool NameBreaksMidWord(TMP_Text name)
        {
            if (name == null || name.textInfo == null || name.textInfo.lineCount <= 1 || name.textInfo.lineInfo == null)
                return false;
            string source = name.text ?? string.Empty;
            TMP_TextInfo info = name.textInfo;
            int lines = Mathf.Min(info.lineCount, info.lineInfo.Length);
            for (int line = 0; line < lines - 1; line++)
            {
                TMP_LineInfo row = info.lineInfo[line];
                TMP_LineInfo next = info.lineInfo[line + 1];
                if (row.characterCount <= 0 || next.characterCount <= 0)
                    continue;
                int last = row.lastCharacterIndex;
                int firstNext = next.firstCharacterIndex;
                if (last < 0 || firstNext < 0 || last >= info.characterCount || firstNext >= info.characterCount)
                    continue;
                int leftIndex = info.characterInfo[last].index;
                int rightIndex = info.characterInfo[firstNext].index;
                if (leftIndex < 0 || rightIndex <= leftIndex || leftIndex >= source.Length || rightIndex >= source.Length)
                    continue;
                if (IsNameWordChar(source[leftIndex]) && IsNameWordChar(source[rightIndex]))
                    return true;
            }

            return false;
        }

        private static bool IsNameWordChar(char value)
        {
            if (char.IsWhiteSpace(value) || value == '-' || value == '/' || value == '·')
                return false;
            return char.IsLetterOrDigit(value);
        }

        /// <summary>The title rect stays inside the card. A later fitter must not push it past the edge.</summary>
        private static void ClampNameInsideCard(RectTransform title, float cardW, float cardH)
        {
            if (title == null)
                return;
            float x = title.anchoredPosition.x;
            float y = -title.anchoredPosition.y;
            float w = title.sizeDelta.x;
            float h = title.sizeDelta.y;
            if (x < 1f)
                x = 1f;
            if (y < 1f)
                y = 1f;
            if (w > cardW - x - 1f)
                w = Mathf.Max(8f, cardW - x - 1f);
            if (h > cardH - y - 1f)
                h = Mathf.Max(8f, cardH - y - 1f);
            PlaceTopLeft(title, x, y, w, h);
        }

        /// <summary>Solid title bar above the thumbnail. The name is no longer a caption on the painting.</summary>
        private static RectTransform EnsureNameShade(RectTransform card)
        {
            RectTransform shade = card.Find("NameShade") as RectTransform;
            Image image;
            if (shade == null)
            {
                image = new GameObject("NameShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(card, false);
                shade = image.rectTransform;
            }
            else if (!shade.TryGetComponent(out image))
                image = shade.gameObject.AddComponent<Image>();
            image.sprite = ZoneSolidSprite();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.color = new Color(0.14f, 0.11f, 0.08f, 0.96f);
            shade.gameObject.SetActive(true);
            return shade;
        }

        /// <summary>The old fade sat under a top band and added a second dark slab. It stays hidden.</summary>
        private static void HideNameFade(RectTransform card)
        {
            if (card == null || card.Find("NameFade") is not RectTransform fade)
                return;
            fade.gameObject.SetActive(false);
            if (fade.TryGetComponent<Image>(out Image image))
                image.color = new Color(0f, 0f, 0f, 0f);
            PlaceTopLeft(fade, 0f, 0f, 0f, 0f);
        }

        /// <summary>Fade under the solid title band. It starts below the ink.</summary>
        private static RectTransform EnsureNameFade(RectTransform card)
        {
            RectTransform fade = card.Find("NameFade") as RectTransform;
            Image image;
            if (fade == null)
            {
                image = new GameObject("NameFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(card, false);
                fade = image.rectTransform;
            }
            else if (!fade.TryGetComponent(out image))
                image = fade.gameObject.AddComponent<Image>();
            image.sprite = NameVeilSprite();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.color = Color.white;
            return fade;
        }

        /// <summary>Full-brightness zone hue, pulled a little toward white so a half arc stays obvious.</summary>
        private static Color BrightRingFill(Color zone)
        {
            float peak = Mathf.Max(zone.r, Mathf.Max(zone.g, zone.b));
            Color hue = peak > 0.01f ? zone / peak : Color.white;
            Color bright = Color.Lerp(hue, Color.white, 0.28f);
            bright.a = 1f;
            return bright;
        }

        /// <summary>
        /// A parent layout group must not rewrite the portrait after the seat is applied.
        /// </summary>
        private static void KeepAuthoredSize(RectTransform rect)
        {
            if (rect == null)
                return;
            LayoutElement element = rect.GetComponent<LayoutElement>();
            if (element == null)
                element = rect.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            if (rect.TryGetComponent<ContentSizeFitter>(out ContentSizeFitter fitter))
                fitter.enabled = false;
            if (rect.TryGetComponent<AspectRatioFitter>(out AspectRatioFitter aspect))
                aspect.enabled = false;
        }

        /// <summary>
        /// A layout group can shrink the live rect after the seat. Put the authored size back
        /// only as far as the card still sits inside its hex.
        /// </summary>
        private static void KeepRestoredCardInsideHex(RectTransform card)
        {
            if (card == null || card.parent is not RectTransform parent)
                return;
            string mapId = card.name.StartsWith("ZoneCard_", StringComparison.Ordinal)
                ? card.name.Substring("ZoneCard_".Length)
                : string.Empty;
            string[] ids = TheatreSeatGeometry.MapIds;
            var hexes = new TheatreSeatGeometry.Vec2[ids.Length][];
            int own = 0;
            bool any = false;
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], mapId, StringComparison.OrdinalIgnoreCase))
                    own = i;
                if (parent.Find("ZoneOutline_" + ids[i]) is not RectTransform outline)
                    continue;
                Vector2[] points = OutlinePlatePoints(outline);
                if (points.Length < 3)
                    continue;
                var shape = new TheatreSeatGeometry.Vec2[points.Length];
                for (int p = 0; p < points.Length; p++)
                    shape[p] = new TheatreSeatGeometry.Vec2(points[p].x, points[p].y);
                hexes[i] = shape;
                any = true;
            }

            if (!any || hexes[own] == null)
                return;
            float x = card.anchoredPosition.x;
            float y = -card.anchoredPosition.y;
            float authoredW = card.sizeDelta.x;
            float authoredH = card.sizeDelta.y;
            float liveW = card.rect.width > 2f ? card.rect.width : authoredW;
            float liveH = card.rect.height > 2f ? card.rect.height : authoredH;
            bool shrunk = (liveW > 2f && liveW < authoredW - 8f) || (liveH > 2f && liveH < authoredH - 8f);
            var authored = new TheatreSeatGeometry.Box(x, y, authoredW, authoredH);
            if (!shrunk && TheatreSeatGeometry.SeatHolds(authored, hexes[own], hexes))
                return;
            var live = new TheatreSeatGeometry.Box(x, y, shrunk ? liveW : authoredW, shrunk ? liveH : authoredH);
            TheatreSeatGeometry.Box fit = TheatreSeatGeometry.RestoreAuthoredCard(
                live, authoredW, authoredH, hexes[own], hexes);
            PlaceTopLeft(card, fit.X, fit.Y, fit.W, fit.H);
            card.localScale = Vector3.one;
            KeepAuthoredSize(card);
        }

        /// <summary>
        /// Last theatre pass, after the frame has the final plate. Lays the full-width portrait
        /// from the card's live size and logs the rect the capture camera will draw.
        /// </summary>
        private void RefitTheatreCardArt()
        {
            if (_libraryRegions == null)
                return;
            TMP_Text[] texts = _libraryRegions.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text name = texts[i];
                if (name == null || name.name != "ZoneName" || !name.gameObject.activeInHierarchy)
                    continue;
                RectTransform card = name.rectTransform != null ? name.rectTransform.parent as RectTransform : null;
                if (card == null || !card.name.StartsWith("ZoneCard_", StringComparison.Ordinal))
                    continue;
                KeepAuthoredSize(card);
                KeepRestoredCardInsideHex(card);

                float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
                float nameH = name.rectTransform.rect.height > 1f
                    ? name.rectTransform.rect.height
                    : name.rectTransform.sizeDelta.y;
                RectTransform plate = card.parent as RectTransform;
                bool wide = false;
                if (plate != null && TryFrameSize(plate, out float plateWidth, out float plateHeight))
                    wide = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
                LayoutZoneCardStack(card, name, nameH, cardH, wide);
                LogTheatreCardArt(card);
            }

            FitZoneNames(_libraryRegions);
            TMP_Text zoom = null;
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "ZoomCue" && texts[i].gameObject.activeInHierarchy)
                {
                    zoom = texts[i];
                    break;
                }
            }

            string selected = _libraryViewModel != null ? _libraryViewModel.Theater.SelectedMapId : "DB";
            if (string.IsNullOrEmpty(selected))
                selected = "DB";
            if (zoom != null)
                FitZoomCue();
        }

        /// <summary>One line per zone card: authored size, live rect, lossy scale, and world-corner span.</summary>
        private static void LogTheatreCardArt(RectTransform card)
        {
            if (card == null)
                return;
            string mapId = card.name.StartsWith("ZoneCard_", StringComparison.Ordinal)
                ? card.name.Substring("ZoneCard_".Length)
                : card.name;
            RectTransform thumb = card.Find("ThumbFrame") as RectTransform;
            RectTransform ring = card.Find("ProgressRing") as RectTransform;
            Debug.Log("[TheatreCardArt] map=" + mapId
                + " card=" + F(card.sizeDelta.x) + "x" + F(card.sizeDelta.y)
                + " cardRect=" + F(card.rect.width) + "x" + F(card.rect.height)
                + " cardScale=" + F(card.lossyScale.x) + "," + F(card.lossyScale.y)
                + " cardWorld=" + F(WorldSpan(card))
                + " thumb=" + SizeText(thumb)
                + " thumbRect=" + RectText(thumb)
                + " thumbScale=" + ScaleText(thumb)
                + " thumbWorld=" + F(WorldSpan(thumb))
                + " ring=" + SizeText(ring)
                + " ringRect=" + RectText(ring)
                + " ringScale=" + ScaleText(ring)
                + " ringWorld=" + F(WorldSpan(ring)));
            Image art = card.Find("ThumbFrame/CardArt")?.GetComponent<Image>();
            string spriteName = art != null && art.sprite != null ? art.sprite.name : "none";
            RawImage plate = card.Find("ThumbFrame/Thumbnail")?.GetComponent<RawImage>();
            Color tint = Color.white;
            float alpha = 1f;
            if (plate != null && plate.enabled)
            {
                tint = plate.color;
                alpha = plate.color.a;
            }
            else if (art != null)
            {
                tint = art.color;
                alpha = art.color.a;
            }

            float share = 0f;
            if (ring != null && card.sizeDelta.x > 1f && card.sizeDelta.y > 1f)
                share = ring.sizeDelta.x * ring.sizeDelta.y / (card.sizeDelta.x * card.sizeDelta.y);
            float nameShare = NameStripShare(card);
            float contrast = PlateLuminanceContrast(mapId);
            Debug.Log("[CardArt] map=" + mapId
                + " drawn=" + spriteName
                + " alpha=" + alpha.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " tint=" + tint.r.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + "," + tint.g.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + "," + tint.b.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " progress=" + share.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " nameShare=" + nameShare.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " contrast=" + contrast.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Area of the name strip over the card. An inactive fade does not count.
        /// A full-width strip at 22% of the height reports 0.22.
        /// </summary>
        public static float NameStripShare(RectTransform card)
        {
            if (card == null)
                return 0f;
            float cardArea = card.sizeDelta.x * card.sizeDelta.y;
            if (cardArea < 1f)
                return 0f;
            bool any = false;
            float minX = 0f;
            float minY = 0f;
            float maxX = 0f;
            float maxY = 0f;
            AccumulateStrip(card, "NameShade", ref any, ref minX, ref minY, ref maxX, ref maxY);
            AccumulateStrip(card, "NameFade", ref any, ref minX, ref minY, ref maxX, ref maxY);
            if (!any)
                return 0f;
            return Mathf.Max(0f, maxX - minX) * Mathf.Max(0f, maxY - minY) / cardArea;
        }

        private static void AccumulateStrip(
            RectTransform card,
            string childName,
            ref bool any,
            ref float minX,
            ref float minY,
            ref float maxX,
            ref float maxY)
        {
            if (card.Find(childName) is not RectTransform strip || !strip.gameObject.activeInHierarchy)
                return;
            if (strip.sizeDelta.x < 1f || strip.sizeDelta.y < 1f)
                return;
            float x = strip.anchoredPosition.x;
            float y = -strip.anchoredPosition.y;
            if (!any)
            {
                minX = x;
                minY = y;
                maxX = x + strip.sizeDelta.x;
                maxY = y + strip.sizeDelta.y;
                any = true;
                return;
            }

            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x + strip.sizeDelta.x);
            maxY = Mathf.Max(maxY, y + strip.sizeDelta.y);
        }

        private static string SizeText(RectTransform rect)
        {
            if (rect == null)
                return "none";
            return F(rect.sizeDelta.x) + "x" + F(rect.sizeDelta.y);
        }

        private static string RectText(RectTransform rect)
        {
            if (rect == null)
                return "none";
            return F(rect.rect.width) + "x" + F(rect.rect.height);
        }

        private static string ScaleText(RectTransform rect)
        {
            if (rect == null)
                return "none";
            return F(rect.lossyScale.x) + "," + F(rect.lossyScale.y);
        }

        private static float WorldSpan(RectTransform rect)
        {
            if (rect == null)
                return 0f;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Vector3.Distance(corners[0], corners[3]);
        }

        private static void FitZoneNames(Transform regions)
        {
            var names = new List<TMP_Text>(8);
            TMP_Text[] texts = regions.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text != null && text.name == "ZoneName" && text.gameObject.activeInHierarchy && ZoneCardIsCompact(text))
                    names.Add(text);
            }

            if (names.Count == 0)
            {
                ReseatFramedCards(regions);
                return;
            }

            // Framed cards take their size from SeatNameCopyInStrip. SetZoneNameFont's
            // 25px one-line lock was what capture measured past the box.
            if (names[0].transform.parent is RectTransform framed
                && framed.name.StartsWith("ZoneCard_", StringComparison.Ordinal))
            {
                ReseatFramedCards(regions);
                return;
            }

            float chosen = ZoneNameFontFloor;
            bool fitted = false;
            for (float candidate = ZoneNameFontCeiling; candidate >= ZoneNameFontFloor - 0.01f; candidate -= 1f)
            {
                if (!ZoneNamesFit(names, candidate))
                    continue;
                chosen = candidate;
                fitted = true;
                break;
            }

            ApplyZoneNameSize(names, chosen, wrapOverflow: !fitted);
            FitPortraitNameBoxes(regions);
            ReseatFramedCards(regions);
        }

        private static bool ZoneCardIsCompact(TMP_Text name)
        {
            RectTransform card = name != null ? name.rectTransform.parent as RectTransform : null;
            if (card == null)
                return false;
            return card.sizeDelta.x < 312f || card.sizeDelta.y < 106f;
        }

        private static bool ZoneNamesFit(List<TMP_Text> names, float size)
        {
            SetZoneNameFont(names, size);
            for (int i = 0; i < names.Count; i++)
            {
                TMP_Text name = names[i];
                if (name == null)
                    continue;
                float boxWidth = ZoneNameBoxWidth(name);
                if (boxWidth < 8f || name.preferredWidth > boxWidth + 1.5f)
                    return false;
                if (name.preferredHeight > ZoneNameHeightLimit(name.rectTransform) - 1f)
                    return false;
            }

            return true;
        }

        private static void ApplyZoneNameSize(List<TMP_Text> names, float size, bool wrapOverflow)
        {
            SetZoneNameFont(names, size);
            for (int i = 0; i < names.Count; i++)
            {
                TMP_Text name = names[i];
                if (name == null)
                    continue;
                float boxWidth = ZoneNameBoxWidth(name);
                if (wrapOverflow && boxWidth >= 8f && name.preferredWidth > boxWidth + 1.5f && !WrapZoneName(name, size))
                {
                    name.textWrappingMode = TextWrappingModes.NoWrap;
                    name.maxVisibleLines = 1;
                    name.ForceMeshUpdate();
                }
                GrowZoneNameBox(name);
            }
        }

        private static void SetZoneNameFont(List<TMP_Text> names, float size)
        {
            for (int i = 0; i < names.Count; i++)
            {
                TMP_Text name = names[i];
                if (name == null)
                    continue;
                if (name.transform.parent is RectTransform framedCard
                    && framedCard.name.StartsWith("ZoneCard_", StringComparison.Ordinal))
                    continue;
                var binding = name.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(size, size);
                name.enableAutoSizing = true;
                name.fontSizeMin = size;
                name.fontSizeMax = size;
                name.fontSize = size;
                name.fontStyle = FontStyles.Bold;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Overflow;
                name.maxVisibleLines = 1;
                name.ForceMeshUpdate();
            }
        }

        private static bool WrapZoneName(TMP_Text name, float size)
        {
            string source = FlattenCopy(name.text);
            float boxWidth = ZoneNameBoxWidth(name);
            int split = -1;
            float bestGap = float.MaxValue;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != ' ')
                    continue;
                string left = source.Substring(0, i + 1);
                string right = source.Substring(i + 1).TrimStart();
                if (right.Length == 0)
                    continue;
                float leftWidth = ZoneNameLineWidth(name, left, size);
                float rightWidth = ZoneNameLineWidth(name, right, size);
                if (leftWidth > boxWidth + 1.5f || rightWidth > boxWidth + 1.5f)
                    continue;
                float gap = Mathf.Abs(leftWidth - rightWidth);
                if (gap >= bestGap)
                    continue;
                bestGap = gap;
                split = i;
            }

            if (split < 0)
            {
                name.text = source;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Overflow;
                name.maxVisibleLines = 1;
                name.fontSizeMin = size;
                name.fontSizeMax = size;
                name.fontSize = size;
                name.ForceMeshUpdate();
                return false;
            }

            string first = source.Substring(0, split).TrimEnd();
            string second = source.Substring(split + 1).TrimStart();
            name.text = first + "\n" + second;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Overflow;
            name.maxVisibleLines = 2;
            name.fontSizeMin = size;
            name.fontSizeMax = size;
            name.fontSize = size;
            name.ForceMeshUpdate();
            RectTransform rect = name.rectTransform;
            float needed = MeasuredNameHeight(name);
            float limit = ZoneNameHeightLimit(rect);
            float height = Mathf.Min(limit, Mathf.Max(rect != null ? rect.sizeDelta.y : needed, needed));
            if (rect != null && height > rect.sizeDelta.y + 0.5f)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            float line = MeasuredNameWidth(name);
            if (line > boxWidth + 1.5f)
            {
                name.text = source;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.maxVisibleLines = 1;
                name.ForceMeshUpdate();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Writes the fitted size into the localization binding so a later refresh cannot
        /// put the Latin 20px floor on a Farsi title, or the 13px library relax on a Latin one.
        /// </summary>
        private static void LockDrawnZoneName(TMP_Text name, float size, bool farsi)
        {
            if (name == null)
                return;
            if (!farsi && size < LatinNameFloor - 0.1f)
                size = LatinNameFloor;
            string drawn = name.text;
            int lines = name.maxVisibleLines;
            float spacing = name.characterSpacing;
            V3LocalizedTextBindingView binding = name.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.SetFontBounds(size, size);
            name.enableAutoSizing = true;
            name.fontSizeMin = size;
            name.fontSizeMax = size;
            name.fontSize = size;
            name.fontStyle = FontStyles.Bold;
            name.text = drawn;
            name.maxVisibleLines = lines > 0 ? lines : 1;
            name.characterSpacing = spacing;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Overflow;
            InkZoneName(name);
            name.ForceMeshUpdate();
        }

        private static void InkZoneNames(Transform regions)
        {
            if (regions == null)
                return;
            TMP_Text[] texts = regions.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text name = texts[i];
                if (name != null && name.name == "ZoneName" && name.gameObject.activeInHierarchy)
                    InkZoneName(name);
            }
        }

        /// <summary>How far the first glyph sits below the label's top edge, in y-down pixels.</summary>
        private static float NameInkTopInset(TMP_Text name)
        {
            if (name == null)
                return 0f;
            name.ForceMeshUpdate();
            if (name.textInfo == null || name.textInfo.characterCount <= 0)
                return 0f;
            float top = float.NegativeInfinity;
            int count = name.textInfo.characterCount;
            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo info = name.textInfo.characterInfo[i];
                if (!info.isVisible)
                    continue;
                if (info.topLeft.y > top)
                    top = info.topLeft.y;
            }

            if (top == float.NegativeInfinity)
                return 0f;
            float inset = -top;
            return inset < 0f ? 0f : inset;
        }

        /// <summary>White title, dark outline and drop shadow, so the glyphs read on a pale photo.</summary>
        private static void InkZoneName(TMP_Text name)
        {
            if (name == null)
                return;
            name.color = Color.white;
            name.outlineWidth = 0.22f;
            name.outlineColor = new Color(0.01f, 0.01f, 0.02f, 1f);
            Shadow shadow = name.GetComponent<Shadow>();
            if (shadow == null)
                shadow = name.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(0f, -1.6f);
            shadow.useGraphicAlpha = true;
        }

        private static void RecordDrawnZoneName(TMP_Text name)
        {
            if (name == null || name.rectTransform == null)
                return;
            RectTransform card = name.rectTransform.parent as RectTransform;
            if (card == null || !card.name.StartsWith("ZoneCard_", StringComparison.Ordinal))
                return;
            string mapId = card.name.Substring("ZoneCard_".Length);
            float boxWidth = name.rectTransform.rect.width > 1f
                ? name.rectTransform.rect.width
                : name.rectTransform.sizeDelta.x;
            float boxHeight = name.rectTransform.rect.height > 1f
                ? name.rectTransform.rect.height
                : name.rectTransform.sizeDelta.y;
            float statusHeight = 0f;
            if (card.Find("ZoneCount") is RectTransform countRow)
                statusHeight = countRow.rect.height > 1f ? countRow.rect.height : countRow.sizeDelta.y;
            else if (card.Find("ProgressRing") is RectTransform ring)
                statusHeight = ring.rect.height > 1f ? ring.rect.height : ring.sizeDelta.y;
            float cardHeight = card.rect.height > 1f ? card.rect.height : card.sizeDelta.y;
            float measuredHeight = name.preferredHeight;
            if (cardHeight < 190f
                && ContainsArabicLetter(name.text)
                && name.textInfo != null
                && name.textInfo.lineCount > 1)
            {
                float lineBox = name.textInfo.lineCount * name.fontSize * 1.65f;
                if (lineBox > measuredHeight)
                    measuredHeight = lineBox;
            }

            TheatreSeatGeometry.NoteDrawnName(
                mapId,
                name.fontSize,
                boxWidth,
                boxHeight,
                MeasuredNameWidth(name),
                measuredHeight,
                statusHeight,
                cardHeight);
            bool rtl = name.isRightToLeftText;
            bool arabic = ContainsArabicLetter(name.text);
            bool farsi = rtl || arabic || UiShellRuntimeGateway.Localization.IsRightToLeft;
            Debug.Log("[TheatreName] map=" + mapId
                + " locale=" + (farsi ? "fa" : "en")
                + " script=" + (arabic ? "ar" : "lat")
                + " rtl=" + (rtl ? "1" : "0")
                + " font=" + F(name.fontSize)
                + " boxH=" + F(boxHeight)
                + " preferredH=" + F(measuredHeight)
                + " statusH=" + F(statusHeight)
                + " cardH=" + F(cardHeight));
        }

        /// <summary>
        /// One line of source text. A wrapped title is stored as "DESERT \nBASE"; turning the
        /// newline into a space without collapsing the leftover space drew "DESERT  BASE" and
        /// then refused to break on the word.
        /// </summary>
        private static string FlattenCopy(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var builder = new System.Text.StringBuilder(value.Length);
            bool pendingSpace = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\u00A0' || c == '\u202F' || char.IsWhiteSpace(c))
                {
                    if (builder.Length > 0)
                        pendingSpace = true;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static bool ContainsArabicLetter(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] > 127)
                    return true;
            }

            return false;
        }

        private static float MeasuredNameWidth(TMP_Text name)
        {
            if (name == null)
                return 0f;
            name.ForceMeshUpdate();
            float measured = name.preferredWidth;
            if (measured < 0.5f)
            {
                Vector2 values = name.GetPreferredValues(name.text);
                measured = Mathf.Max(values.x, values.y);
            }

            TMP_TextInfo info = name.textInfo;
            if (info != null && info.lineCount > 1 && info.lineInfo != null)
            {
                float widest = 0f;
                int lines = Mathf.Min(info.lineCount, info.lineInfo.Length);
                for (int i = 0; i < lines; i++)
                    widest = Mathf.Max(widest, info.lineInfo[i].length);
                if (widest > 0.5f)
                    return widest;
            }

            if (info != null && info.lineCount > 0 && info.lineInfo != null)
            {
                float widest = 0f;
                int lines = Mathf.Min(info.lineCount, info.lineInfo.Length);
                for (int i = 0; i < lines; i++)
                    widest = Mathf.Max(widest, info.lineInfo[i].length);
                if (widest > measured)
                    measured = widest;
            }

            return measured;
        }

        private static float MeasuredNameHeight(TMP_Text name)
        {
            if (name == null)
                return 0f;
            name.ForceMeshUpdate();
            float measured = name.preferredHeight;
            if (measured < 0.5f)
            {
                Vector2 values = name.GetPreferredValues(name.text);
                measured = Mathf.Max(measured, values.y);
            }

            return Mathf.Ceil(measured + 2f);
        }

        private static float ZoneNameBoxWidth(TMP_Text name)
        {
            if (name == null || name.rectTransform == null)
                return 0f;
            return Mathf.Max(name.rectTransform.rect.width, name.rectTransform.sizeDelta.x);
        }

        private static float ZoneNameLineWidth(TMP_Text name, string line, float size)
        {
            string previous = name.text;
            name.text = line;
            name.fontSizeMin = size;
            name.fontSizeMax = size;
            name.fontSize = size;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.maxVisibleLines = 1;
            name.ForceMeshUpdate();
            float measured = name.preferredWidth;
            if (measured < 0.5f && !string.IsNullOrEmpty(line))
            {
                Vector2 values = name.GetPreferredValues(line);
                measured = values.x;
            }

            if (measured < 0.5f)
                measured = EstimateLineWidth(line, size);
            name.text = previous;
            return measured;
        }

        private static float EstimateLineWidth(string line, float size)
        {
            if (string.IsNullOrEmpty(line))
                return 0f;
            float width = 0f;
            for (int i = 0; i < line.Length; i++)
                width += size * (line[i] <= 127 ? 0.66f : 0.95f);
            return width;
        }

        private static void GrowZoneNameBox(TMP_Text name)
        {
            RectTransform rect = name != null ? name.rectTransform : null;
            if (rect == null || rect.parent is not RectTransform card)
                return;
            float cardW = card.sizeDelta.x > 2f ? card.sizeDelta.x : card.rect.width;
            float cardH = card.sizeDelta.y > 2f ? card.sizeDelta.y : card.rect.height;
            ClampNameInsideCard(rect, cardW, cardH);
        }

        private static float ZoneNameHeightLimit(RectTransform name)
        {
            if (name == null)
                return 0f;
            RectTransform card = name.parent as RectTransform;
            if (card == null)
                return name.sizeDelta.y;
            float cardH = card.sizeDelta.y > 1f ? card.sizeDelta.y : card.rect.height;
            RectTransform plate = card.parent as RectTransform;
            bool wide = false;
            if (plate != null && TryFrameSize(plate, out float plateWidth, out float plateHeight))
                wide = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
            bool farsi = UiShellRuntimeGateway.Localization.IsRightToLeft;
            DominantCardBands(cardH, wide, farsi, out float nameRoom, out _, out _);
            return Mathf.Max(18f, nameRoom);
        }

        private static void NudgeRingBelowName(RectTransform name)
        {
            RectTransform card = name.parent as RectTransform;
            if (card == null)
                return;
            RectTransform ring = card.Find("ProgressRing") as RectTransform;
            if (ring == null || ring.sizeDelta.x > ring.sizeDelta.y + 8f)
                return;
            if (Mathf.Abs(ring.sizeDelta.x - ring.sizeDelta.y) <= 8f
                && card.Find("ThumbFrame") is RectTransform thumb
                && Mathf.Abs(thumb.sizeDelta.x - thumb.sizeDelta.y) <= 8f)
                return;
            float nameBottom = -name.anchoredPosition.y + name.sizeDelta.y;
            float ringTop = -ring.anchoredPosition.y;
            if (ringTop >= nameBottom + 2f)
                return;
            float nextTop = nameBottom + 2f;
            float room = card.sizeDelta.y - 4f - nextTop;
            float side = ring.sizeDelta.x;
            if (room + 0.5f < side)
            {
                if (room < 22f)
                    return;
                side = room;
            }

            float right = ring.anchoredPosition.x + ring.sizeDelta.x;
            ring.anchoredPosition = new Vector2(right - side, -nextTop);
            ring.sizeDelta = new Vector2(side, side);
        }

        private static void RaiseTheatreLine(TMP_Text text, float min, float max)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = Mathf.Max(min, max);
            if (text.fontSize < min)
                text.fontSize = min;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.maxVisibleLines = 1;
            text.ForceMeshUpdate();
            float width = 0f;
            float boxHeight = 0f;
            if (text.rectTransform != null)
            {
                width = text.rectTransform.rect.width > 1f ? text.rectTransform.rect.width : text.rectTransform.sizeDelta.x;
                boxHeight = text.rectTransform.rect.height > 1f ? text.rectTransform.rect.height : text.rectTransform.sizeDelta.y;
            }

            if (width < 8f)
                return;
            FitCopyInside(text, Mathf.Min(min, 11f));
            if (text.fontSizeMax < min)
                return;
            for (int step = 0; step < 4 && text.fontSize < max - 0.5f; step++)
            {
                float trial = Mathf.Min(max, text.fontSize + 1f);
                float kept = text.fontSize;
                text.fontSizeMin = trial;
                text.fontSizeMax = trial;
                text.fontSize = trial;
                text.ForceMeshUpdate();
                float line = MeasuredNameWidth(text);
                bool wideLine = line > width + 2f;
                bool tallLine = boxHeight > 8f && text.preferredHeight > boxHeight + 2f;
                if (wideLine || tallLine || text.isTextTruncated || text.isTextOverflowing)
                {
                    text.fontSizeMin = kept;
                    text.fontSizeMax = kept;
                    text.fontSize = kept;
                    text.ForceMeshUpdate();
                    return;
                }
            }
        }

        private static void ShiftCueOffCards(RectTransform parent, float frameW, float frameH, float inset, ref Rect cue)
        {
            if (parent == null)
                return;
            for (int pass = 0; pass < 4; pass++)
            {
                bool hit = false;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || child is not RectTransform card)
                        continue;
                    var cardRect = new Rect(card.anchoredPosition.x, -card.anchoredPosition.y, card.sizeDelta.x, card.sizeDelta.y);
                    if (!Overlaps(cue, cardRect, 2f))
                        continue;
                    hit = true;
                    float toRight = cardRect.xMax + 8f;
                    float toLeft = cardRect.xMin - 8f - cue.width;
                    bool rightFits = frameW <= cue.width + inset * 2f || toRight + cue.width <= frameW - inset;
                    bool leftFits = frameW <= cue.width + inset * 2f || toLeft >= inset;
                    if (rightFits && (!leftFits || cue.center.x >= cardRect.center.x))
                        cue.x = toRight;
                    else if (leftFits)
                        cue.x = toLeft;
                }

                cue = ClampCue(cue, frameW, frameH, inset);
                if (!hit)
                    break;
            }
        }

        private static void ClearCueFromPins(
            RectTransform parent,
            float frameW,
            float frameH,
            float inset,
            float gap,
            ref Rect cue)
        {
            if (parent == null)
                return;
            for (int pass = 0; pass < 6; pass++)
            {
                bool hit = false;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child == null || !child.name.StartsWith("ZonePin_", StringComparison.Ordinal) || child is not RectTransform pin)
                        continue;
                    float pinW = pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize;
                    float pinH = pin.sizeDelta.y > 2f ? pin.sizeDelta.y : ZonePinSize;
                    var pinRect = new Rect(pin.anchoredPosition.x, -pin.anchoredPosition.y, pinW, pinH);
                    if (!Overlaps(cue, pinRect, 0f))
                        continue;
                    hit = true;
                    Rect below = cue;
                    below.y = pinRect.yMax + gap;
                    below.x = pinRect.center.x - cue.width * 0.5f;
                    Rect right = cue;
                    right.x = pinRect.xMax + gap;
                    right.y = pinRect.center.y - cue.height * 0.5f;
                    Rect left = cue;
                    left.x = pinRect.xMin - gap - cue.width;
                    left.y = right.y;
                    if (CueMissesPins(parent, ClampCue(below, frameW, frameH, inset)))
                        cue = ClampCue(below, frameW, frameH, inset);
                    else if (CueMissesPins(parent, ClampCue(right, frameW, frameH, inset)))
                        cue = ClampCue(right, frameW, frameH, inset);
                    else
                        cue = ClampCue(left, frameW, frameH, inset);
                }

                if (!hit)
                    break;
            }
        }

        private static bool CueMissesPins(RectTransform parent, Rect cue)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZonePin_", StringComparison.Ordinal) || child is not RectTransform pin)
                    continue;
                float pinW = pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize;
                float pinH = pin.sizeDelta.y > 2f ? pin.sizeDelta.y : ZonePinSize;
                var pinRect = new Rect(pin.anchoredPosition.x, -pin.anchoredPosition.y, pinW, pinH);
                if (Overlaps(cue, pinRect, 0f))
                    return false;
            }

            return true;
        }

        private static Rect ClampCue(Rect cue, float frameW, float frameH, float inset)
        {
            if (frameW > cue.width + inset * 2f)
                cue.x = Mathf.Clamp(cue.x, inset, frameW - inset - cue.width);
            if (frameH > cue.height + inset * 2f)
                cue.y = Mathf.Clamp(cue.y, inset, frameH - inset - cue.height);
            return cue;
        }

        private static bool SeatCueNearPin(
            RectTransform parent,
            RectTransform pin,
            string selectedId,
            float frameW,
            float frameH,
            float inset,
            Rect detail,
            float nearLimit,
            float worldScaleX,
            float worldScaleY,
            ref Rect cue)
        {
            if (parent == null || pin == null)
                return false;
            const float margin = 12f;
            var cards = new List<Rect>(8);
            var pins = new List<Rect>(8);
            CollectOverlayRects(parent, "ZoneCard_", cards);
            CollectOverlayRects(parent, "ZonePin_", pins);
            float pinLeft = pin.anchoredPosition.x;
            float pinTop = -pin.anchoredPosition.y;
            float pinW = pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize;
            float pinH = pin.sizeDelta.y > 2f ? pin.sizeDelta.y : ZonePinSize;
            float pinX = pinLeft + pinW * 0.5f;
            float pinY = pinTop + pinH * 0.5f;
            return TryCueNear(parent, nearLimit, worldScaleX, worldScaleY, selectedId, pinX, pinY, pinLeft, pinTop, pinW, pinH, frameW, frameH, inset, margin, detail, cue, cards, pins, ref cue);
        }

        private static bool TryCueNear(
            RectTransform parent,
            float nearLimit,
            float worldScaleX,
            float worldScaleY,
            string selectedId,
            float pinX,
            float pinY,
            float pinLeft,
            float pinTop,
            float pinW,
            float pinH,
            float frameW,
            float frameH,
            float inset,
            float margin,
            Rect detail,
            Rect cue,
            List<Rect> cards,
            List<Rect> pins,
            ref Rect placed)
        {
            var pinCentre = new Vector2(pinX, pinY);
            for (int dist = 0; dist <= 180; dist += 4)
            {
                for (int slide = 0; slide <= 180; slide += 4)
                {
                    float[] slides = slide == 0 ? new[] { 0f } : new[] { -slide, (float)slide };
                    for (int s = 0; s < slides.Length; s++)
                    {
                        float offset = slides[s];
                        Rect[] spots =
                        {
                            new Rect(pinX - cue.width * 0.5f + offset, pinTop + pinH + margin + dist, cue.width, cue.height),
                            new Rect(pinX - cue.width * 0.5f + offset, pinTop - margin - cue.height - dist, cue.width, cue.height),
                            new Rect(pinLeft + pinW + margin + dist, pinY - cue.height * 0.5f + offset, cue.width, cue.height),
                            new Rect(pinLeft - margin - cue.width - dist, pinY - cue.height * 0.5f + offset, cue.width, cue.height)
                        };
                        for (int i = 0; i < spots.Length; i++)
                        {
                            Rect spot = spots[i];
                            Vector2 centre = spot.center;
                            if (CueWorldReach(centre, pinCentre, worldScaleX, worldScaleY) > nearLimit)
                                continue;
                            if (!CueReadsAsSelected(parent, centre, pinCentre, selectedId, frameW, frameH))
                                continue;
                            if (PillHitsZoneHex(spot, frameW, frameH))
                                continue;
                            if (TryAcceptCue(spot, frameW, frameH, inset, margin, detail, cards, pins, ref placed))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private static float CueWorldReach(Vector2 centre, Vector2 pin, float worldScaleX, float worldScaleY)
        {
            float dx = (centre.x - pin.x) * worldScaleX;
            float dy = (centre.y - pin.y) * worldScaleY;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// The packed pill is only kept when it misses every district hex, including the selected one.
        /// </summary>
        private bool PackedCueClearsHexes(RectTransform cue)
        {
            if (cue == null || cue.parent is not RectTransform parent)
                return true;
            float frameW = parent.rect.width > 2f ? parent.rect.width : parent.sizeDelta.x;
            float frameH = parent.rect.height > 2f ? parent.rect.height : parent.sizeDelta.y;
            var spot = new Rect(cue.anchoredPosition.x, -cue.anchoredPosition.y, cue.sizeDelta.x, cue.sizeDelta.y);
            return !PillHitsZoneHex(spot, frameW, frameH);
        }

        private static bool PillHitsZoneHex(Rect spot, float frameW, float frameH)
        {
            if (frameW < 2f || frameH < 2f || spot.width < 1f || spot.height < 1f)
                return false;
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            for (int i = 0; i < mapIds.Length; i++)
            {
                Vector2[] hex = DistrictHex(mapIds[i], frameW, frameH);
                if (hex != null && hex.Length >= 3 && RectHitsPolygon(spot, hex))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Moves the zoom pill off every district hex. A packed seat can sit in the middle of the
        /// selected Industrial Basin while its centre still reads as that pin.
        /// </summary>
        private static void SlideCueBesideForeignHex(
            ref Rect cue,
            string selectedId,
            float frameW,
            float frameH,
            float inset,
            Rect detail,
            RectTransform parent)
        {
            if (!PillHitsZoneHex(cue, frameW, frameH))
                return;
            var cards = new List<Rect>(8);
            CollectOverlayRects(parent, "ZoneCard_", cards);
            Vector2 pin = SelectedPinCentre(parent, selectedId);
            Rect origin = cue;
            float best = float.MaxValue;
            Rect chosen = cue;
            bool found = false;
            const float nearLimit = 160f;
            for (float width = origin.width; width >= 96f - 0.5f; width -= 8f)
            {
            for (int step = 8; step <= 320; step += 8)
            {
                float[] dx = { step, -step, 0f, 0f, step, -step, step, -step };
                float[] dy = { 0f, 0f, step, -step, step, step, -step, -step };
                for (int i = 0; i < dx.Length; i++)
                {
                    float slide = (origin.width - width) * 0.5f;
                    var spot = new Rect(origin.x + dx[i] + slide, origin.y + dy[i], width, origin.height);
                    if (spot.xMin < inset || spot.yMin < inset || spot.xMax > frameW - inset || spot.yMax > frameH - inset)
                        continue;
                    if (Vector2.Distance(spot.center, pin) > nearLimit)
                        continue;
                    if (PillHitsZoneHex(spot, frameW, frameH))
                        continue;
                    if (!CueMissesDetail(spot, detail) || !CueClears(spot, cards, 8f))
                        continue;
                    float reach = dx[i] * dx[i] + dy[i] * dy[i];
                    if (reach >= best)
                        continue;
                    best = reach;
                    chosen = spot;
                    found = true;
                }

                if (found && best <= step * step)
                    break;
            }

            if (found)
                break;
            }

            if (found)
                cue = chosen;
        }

        private static Vector2 SelectedPinCentre(RectTransform parent, string selectedId)
        {
            if (parent != null && parent.Find("ZonePin_" + selectedId) is RectTransform pin)
            {
                float pinW = pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize;
                float pinH = pin.sizeDelta.y > 2f ? pin.sizeDelta.y : ZonePinSize;
                return new Vector2(pin.anchoredPosition.x + pinW * 0.5f, -pin.anchoredPosition.y + pinH * 0.5f);
            }

            return Vector2.zero;
        }

        private static bool CueReadsAsSelected(RectTransform parent, Vector2 centre, Vector2 pin, string selectedId, float frameW, float frameH)
        {
            float own = Vector2.Distance(centre, pin);
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            for (int i = 0; i < mapIds.Length; i++)
            {
                if (string.Equals(mapIds[i], selectedId, StringComparison.OrdinalIgnoreCase))
                    continue;
                Vector2[] other = DistrictHex(mapIds[i], frameW, frameH);
                if (other != null && other.Length >= 3 && PointInsidePolygon(centre, other))
                    return false;
            }

            if (parent == null)
                return true;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZonePin_", StringComparison.Ordinal) || child is not RectTransform otherPin)
                    continue;
                if (string.Equals(child.name, "ZonePin_" + selectedId, StringComparison.OrdinalIgnoreCase))
                    continue;
                float otherW = otherPin.sizeDelta.x > 2f ? otherPin.sizeDelta.x : ZonePinSize;
                float otherH = otherPin.sizeDelta.y > 2f ? otherPin.sizeDelta.y : ZonePinSize;
                var otherCentre = new Vector2(otherPin.anchoredPosition.x + otherW * 0.5f, -otherPin.anchoredPosition.y + otherH * 0.5f);
                if (Vector2.Distance(centre, otherCentre) + 0.5f < own)
                    return false;
            }

            return true;
        }

        private static void NudgeCardsForCue(
            RectTransform parent,
            RectTransform pin,
            float frameW,
            float frameH,
            Rect cue,
            Rect detail)
        {
            if (parent == null || pin == null)
                return;
            float pinTop = -pin.anchoredPosition.y;
            float pinH = pin.sizeDelta.y > 2f ? pin.sizeDelta.y : ZonePinSize;
            float pinLeft = pin.anchoredPosition.x;
            float pinW = pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize;
            float pinX = pinLeft + pinW * 0.5f;
            float pinY = pinTop + pinH * 0.5f;
            Rect[] probes =
            {
                new Rect(pinX - cue.width * 0.5f, pinTop + pinH + 12f, cue.width, cue.height),
                new Rect(pinX - cue.width * 0.5f, pinTop - 12f - cue.height, cue.width, cue.height),
                new Rect(pinLeft + pinW + 12f, pinY - cue.height * 0.5f, cue.width, cue.height),
                new Rect(pinLeft - 12f - cue.width, pinY - cue.height * 0.5f, cue.width, cue.height)
            };
            for (int p = 0; p < probes.Length; p++)
            {
                Rect probe = ClampCue(probes[p], frameW, frameH, 12f);
                NudgeCardsOffProbe(parent, frameW, frameH, probe, detail);
            }
        }

        private static void NudgeCardsOffProbe(RectTransform parent, float frameW, float frameH, Rect probe, Rect detail)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || child is not RectTransform card)
                    continue;
                var cardRect = new Rect(card.anchoredPosition.x, -card.anchoredPosition.y, card.sizeDelta.x, card.sizeDelta.y);
                if (CueClears(cardRect, probe, 12f))
                    continue;
                string mapId = child.name.Substring("ZoneCard_".Length);
                bool pushX = AxisGap(cardRect.xMin, cardRect.xMax, probe.xMin, probe.xMax) >=
                    AxisGap(cardRect.yMin, cardRect.yMax, probe.yMin, probe.yMax);
                bool cleared = false;
                for (int axis = 0; axis < 2 && !cleared; axis++)
                {
                    bool horizontal = axis == 0 ? pushX : !pushX;
                    for (int step = 1; step <= 16; step++)
                    {
                        Rect next = cardRect;
                        float delta = step * 8f;
                        if (horizontal)
                            next.x += next.center.x >= probe.center.x ? delta : -delta;
                        else
                            next.y += next.center.y >= probe.center.y ? delta : -delta;
                        if (next.xMin < 12f || next.yMin < 12f || next.xMax > frameW - 12f || next.yMax > frameH - 12f)
                            break;
                        if (!CueMissesDetail(next, detail) || !ZoneCardSeatHolds(next, mapId, frameW, frameH))
                            continue;
                        if (!CueClears(next, probe, 12f))
                            continue;
                        bool blocked = false;
                        for (int j = 0; j < parent.childCount; j++)
                        {
                            if (j == i)
                                continue;
                            Transform other = parent.GetChild(j);
                            if (other == null || !other.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || other is not RectTransform otherRect)
                                continue;
                            var otherCard = new Rect(otherRect.anchoredPosition.x, -otherRect.anchoredPosition.y, otherRect.sizeDelta.x, otherRect.sizeDelta.y);
                            if (CardClearance(next, otherCard) + 0.01f < ZoneSeparateGap)
                            {
                                blocked = true;
                                break;
                            }
                        }

                        if (blocked || !CardClearsLivePins(parent, next))
                            continue;
                        PlaceTopLeft(card, next.x, next.y, next.width, next.height);
                        cleared = true;
                        break;
                    }
                }
            }
        }

        private static bool CardClearsLivePins(RectTransform parent, Rect card)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZonePin_", StringComparison.Ordinal) || child is not RectTransform pin)
                    continue;
                float pinW = pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize;
                float pinH = pin.sizeDelta.y > 2f ? pin.sizeDelta.y : ZonePinSize;
                var pinRect = new Rect(pin.anchoredPosition.x, -pin.anchoredPosition.y, pinW, pinH);
                if (!CueClears(card, pinRect, ZoneCardGap))
                    return false;
            }

            return true;
        }

        private static bool TryAcceptCue(
            Rect candidate,
            float frameW,
            float frameH,
            float inset,
            float margin,
            Rect detail,
            List<Rect> cards,
            List<Rect> pins,
            ref Rect placed)
        {
            if (candidate.x < inset - 0.5f || candidate.y < inset - 0.5f)
                return false;
            if (candidate.xMax > frameW - inset + 0.5f || candidate.yMax > frameH - inset + 0.5f)
                return false;
            if (!CueMissesDetail(candidate, detail))
                return false;
            if (!CueClears(candidate, cards, margin) || !CueClears(candidate, pins, margin))
                return false;
            placed = candidate;
            return true;
        }

        private static bool CueMissesDetail(Rect spot, Rect detail)
        {
            if (detail.width < 1f || detail.height < 1f)
                return true;
            return !Overlaps(spot, detail, -1f);
        }

        private static bool ZoneCardSeatHolds(Rect card, string mapId, float frameWidth, float frameHeight)
        {
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            var hexes = new TheatreSeatGeometry.Vec2[mapIds.Length][];
            int ownIndex = 0;
            for (int i = 0; i < mapIds.Length; i++)
            {
                hexes[i] = TheatreSeatGeometry.DistrictHex(mapIds[i], frameWidth, frameHeight);
                if (string.Equals(mapIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                    ownIndex = i;
            }

            var box = new TheatreSeatGeometry.Box(card.x, card.y, card.width, card.height);
            return TheatreSeatGeometry.SeatHolds(box, hexes[ownIndex], hexes);
        }

        private static bool CueClears(Rect cue, List<Rect> blockers, float margin)
        {
            for (int i = 0; i < blockers.Count; i++)
            {
                Rect block = blockers[i];
                float dx = AxisGap(cue.xMin, cue.xMax, block.xMin, block.xMax);
                float dy = AxisGap(cue.yMin, cue.yMax, block.yMin, block.yMax);
                if (dx < margin && dy < margin)
                    return false;
            }

            return true;
        }

        private static void CollectOverlayRects(RectTransform parent, string prefix, List<Rect> rects)
        {
            if (rects == null)
                return;
            rects.Clear();
            if (parent == null)
                return;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith(prefix, StringComparison.Ordinal) || child is not RectTransform block)
                    continue;
                float width = block.sizeDelta.x > 2f ? block.sizeDelta.x : ZonePinSize;
                float height = block.sizeDelta.y > 2f ? block.sizeDelta.y : ZonePinSize;
                rects.Add(new Rect(block.anchoredPosition.x, -block.anchoredPosition.y, width, height));
            }
        }

        private static void PlaceCueClearOfPin(
            RectTransform pin,
            float width,
            float height,
            float frameW,
            float frameH,
            float inset,
            float gap,
            ref float left,
            ref float top)
        {
            float pinLeft = pin.anchoredPosition.x;
            float pinTop = -pin.anchoredPosition.y;
            float pinW = pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize;
            float pinH = pin.sizeDelta.y > 2f ? pin.sizeDelta.y : ZonePinSize;
            left = pinLeft + pinW * 0.5f - width * 0.5f;
            top = pinTop + pinH + gap;
            bool belowFits = frameH < height + inset * 2f || top + height <= frameH - inset;
            if (!belowFits)
            {
                top = pinTop + pinH * 0.5f - height * 0.5f;
                left = pinLeft + pinW + gap;
                if (frameW > width + inset * 2f && left + width > frameW - inset)
                    left = pinLeft - gap - width;
            }
        }

        private static bool TryRegion(string mapId, out Rect region, out Vector2 centre)
        {
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                if (!string.Equals(SkirmishBattleLibraryArt.MapIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                region = SkirmishBattleLibraryArt.TheaterRegions[i];
                centre = SkirmishBattleLibraryArt.TheaterLabelCentres[i];
                return true;
            }

            region = default;
            centre = new Vector2(0.5f, 0.5f);
            return false;
        }

        /// <summary>
        /// District tint sampled from approved-1. Saturation stays in the capture band.
        /// Alpha is applied by the caller so the fill stays a veil, not a neon plate.
        /// </summary>
        private static Color TerritoryTint(string mapId, Color fallback)
        {
            switch ((mapId ?? string.Empty).ToUpperInvariant())
            {
                case "CC": return new Color(0.94f, 0.42f, 0.08f, 1f);
                case "MP": return new Color(0.62f, 0.34f, 0.86f, 1f);
                case "IB": return new Color(0.24f, 0.56f, 0.92f, 1f);
                case "AP": return new Color(0.20f, 0.74f, 0.26f, 1f);
                case "DB": return new Color(1.00f, 0.40f, 0.02f, 1f);
                default:
                    return fallback.a > 0f ? fallback : new Color(0.62f, 0.50f, 0.22f, 1f);
            }
        }

        private static Color RegionZoneColor(string mapId)
        {
            switch ((mapId ?? string.Empty).ToUpperInvariant())
            {
                case "CC": return new Color(0.94f, 0.42f, 0.08f, 1f);
                case "MP": return new Color(0.62f, 0.34f, 0.86f, 1f);
                case "IB": return new Color(0.24f, 0.56f, 0.92f, 1f);
                case "AP": return new Color(0.20f, 0.74f, 0.26f, 1f);
                default: return new Color(1.00f, 0.40f, 0.02f, 1f);
            }
        }

        private static Sprite ZoneMedallionSprite()
        {
            if (_zoneMedallion != null)
                return _zoneMedallion;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var fill = new Color32(18, 22, 26, 255);
            var gold = new Color32(236, 196, 96, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    if (distance > 1f)
                        continue;
                    pixels[y * size + x] = distance > 0.82f ? gold : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneMedallion = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _zoneMedallion.hideFlags = HideFlags.HideAndDontSave;
            return _zoneMedallion;
        }

        private static Sprite LibraryMedallion()
        {
            if (_libraryMedallion != null)
                return _libraryMedallion;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    if (distance > 1f)
                        continue;
                    bool ring = distance > 0.72f;
                    bool core = distance < 0.34f;
                    pixels[y * size + x] = ring
                        ? new Color32(255, 236, 196, 255)
                        : core
                            ? new Color32(255, 255, 255, 255)
                            : new Color32(255, 214, 120, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _libraryMedallion = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _libraryMedallion.hideFlags = HideFlags.HideAndDontSave;
            return _libraryMedallion;
        }

        private static Sprite ZoneGlowSprite()
        {
            if (_zoneGlow != null)
                return _zoneGlow;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    if (distance > 1f)
                        continue;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.72f) / 0.22f);
                    float halo = Mathf.Clamp01(1f - distance) * 0.35f;
                    byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(ring, halo) * 255f), 0, 255);
                    pixels[y * size + x] = new Color32(255, 236, 180, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneGlow = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _zoneGlow.hideFlags = HideFlags.HideAndDontSave;
            return _zoneGlow;
        }

        private static Sprite ZoneRibbonSprite()
        {
            if (_zoneRibbon != null)
                return _zoneRibbon;
            const int width = 256;
            const int height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            var fill = new Color32(6, 10, 12, 242);
            var gold = new Color32(236, 196, 96, 255);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int inset = 0;
                    if (x < 28)
                        inset = 28 - x;
                    else if (x >= width - 28)
                        inset = x - (width - 29);
                    int top = 2 + inset / 4;
                    int bottom = height - 3 - inset / 4;
                    if (y < top || y > bottom)
                        continue;
                    bool rim = y <= top + 5 || y >= bottom - 5 || x <= 5 || x >= width - 6;
                    pixels[y * width + x] = rim ? gold : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneRibbon = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(36f, 12f, 36f, 12f),
                false);
            _zoneRibbon.hideFlags = HideFlags.HideAndDontSave;
            return _zoneRibbon;
        }

        private static Sprite ZoneSolidSprite()
        {
            if (_zoneSolid != null)
                return _zoneSolid;
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneSolid = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            _zoneSolid.hideFlags = HideFlags.HideAndDontSave;
            return _zoneSolid;
        }

        private static Sprite ZoneStrokeSprite()
        {
            if (_zoneStroke != null)
                return _zoneStroke;
            const int width = 64;
            const int height = 16;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                byte a = 255;
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = new Color32(255, 255, 255, a);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneStroke = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(8f, 4f, 8f, 4f),
                false);
            _zoneStroke.hideFlags = HideFlags.HideAndDontSave;
            return _zoneStroke;
        }

        private static Sprite ZoneCapSprite()
        {
            if (_zoneCap != null)
                return _zoneCap;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneCap = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _zoneCap.hideFlags = HideFlags.HideAndDontSave;
            return _zoneCap;
        }

        private static Sprite ZoneRingSprite()
        {
            if (_zoneRing != null)
                return _zoneRing;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    const float inner = 0.52f;
                    const float outer = 0.96f;
                    float ring = 0f;
                    if (distance >= inner && distance <= outer)
                        ring = 1f;
                    else if (distance < inner)
                        ring = Mathf.Clamp01(1f - (inner - distance) / 0.06f);
                    else
                        ring = Mathf.Clamp01(1f - (distance - outer) / 0.04f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(ring * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneRing = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
            _zoneRing.hideFlags = HideFlags.HideAndDontSave;
            return _zoneRing;
        }

        /// <summary>
        /// Thin stroke for the corner progress badge. The old ring filled from 52% of
        /// the radius out, so a large badge read as a bullseye on the painting.
        /// </summary>
        private static Sprite ZoneThinArcSprite()
        {
            if (_zoneThinArc != null)
                return _zoneThinArc;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    const float inner = 0.62f;
                    const float outer = 0.96f;
                    float ring = 0f;
                    if (distance >= inner && distance <= outer)
                        ring = 1f;
                    else if (distance < inner)
                        ring = Mathf.Clamp01(1f - (inner - distance) / 0.05f);
                    else
                        ring = Mathf.Clamp01(1f - (distance - outer) / 0.04f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(ring * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneThinArc = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
            _zoneThinArc.hideFlags = HideFlags.HideAndDontSave;
            return _zoneThinArc;
        }

        private static Sprite ZoneDiscSprite()
        {
            if (_zoneDisc != null)
                return _zoneDisc;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    float alpha = distance <= 0.94f ? 1f : Mathf.Clamp01((1.02f - distance) / 0.08f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneDisc = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
            _zoneDisc.hideFlags = HideFlags.HideAndDontSave;
            return _zoneDisc;
        }

        private static Sprite _zonePill;
        private static Sprite _zoneVignette;
        private static Sprite _nameVeil;
        private static Sprite _zoneFrame;
        private static Sprite _zoneThinArc;

        private static Sprite ZoneFrameSprite()
        {
            if (_zoneFrame != null)
                return _zoneFrame;
            const int size = 16;
            const int rim = 3;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var ink = new Color32(28, 22, 16, 235);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < rim || y < rim || x >= size - rim || y >= size - rim;
                    pixels[y * size + x] = edge ? ink : new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneFrame = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(rim, rim, rim, rim),
                false);
            _zoneFrame.hideFlags = HideFlags.HideAndDontSave;
            return _zoneFrame;
        }

        private static Sprite ZonePillSprite()
        {
            if (_zonePill != null)
                return _zonePill;
            const int width = 64;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = x < height * 0.5f ? (height * 0.5f - x) : (x >= width - height * 0.5f ? x - (width - height * 0.5f - 1f) : 0f);
                    float dy = y - (height - 1) * 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / (height * 0.5f);
                    if (distance > 1f)
                        continue;
                    pixels[y * width + x] = new Color32(255, 255, 255, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zonePill = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(16f, 16f, 16f, 16f),
                false);
            _zonePill.hideFlags = HideFlags.HideAndDontSave;
            return _zonePill;
        }

        private static Sprite NameVeilSprite()
        {
            if (_nameVeil != null)
                return _nameVeil;
            const int width = 8;
            const int height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                // The strip is only the bottom 22%. Keep that whole band opaque so the
                // glyphs sit on ink, not on the painting. A fade that started at 72%
                // left the contrast at 2.71 and 3.74.
                byte a = 250;
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = new Color32(3, 4, 6, a);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _nameVeil = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            _nameVeil.hideFlags = HideFlags.HideAndDontSave;
            return _nameVeil;
        }

        private static Sprite ZoneVignetteSprite()
        {
            if (_zoneVignette != null)
                return _zoneVignette;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center.x) / center.x;
                    float dy = (y - center.y) / center.y;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = Mathf.Clamp01((distance - 0.55f) / 0.70f);
                    edge = edge * edge;
                    byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(0f, 0.45f, edge) * 255f), 0, 255);
                    pixels[y * size + x] = new Color32(0, 0, 0, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneVignette = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _zoneVignette.hideFlags = HideFlags.HideAndDontSave;
            return _zoneVignette;
        }
    }

    /// <summary>
    /// Pressed state for a transparent territory hit. The hit image stays clear so it
    /// does not cover the painting or add a rect the overlap checks would see.
    /// The existing district fill brightens while the pointer is down, at the same alpha.
    /// </summary>
    sealed class TheatreDistrictPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private V3PolygonGraphic _fill;
        private Color _rest;
        private bool _held;

        public void Bind(Transform plate, string mapId)
        {
            Transform outline = plate != null ? plate.Find("ZoneOutline_" + mapId) : null;
            _fill = outline != null ? outline.GetComponent<V3PolygonGraphic>() : null;
            if (_fill != null)
                _rest = _fill.color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_fill == null)
                return;
            _rest = _fill.color;
            _held = true;
            Color pressed = _rest;
            pressed.r = Mathf.Min(1f, _rest.r + 0.22f);
            pressed.g = Mathf.Min(1f, _rest.g + 0.22f);
            pressed.b = Mathf.Min(1f, _rest.b + 0.22f);
            pressed.a = _rest.a;
            _fill.color = pressed;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        private void OnDisable() => Release();

        private void Release()
        {
            if (!_held || _fill == null)
                return;
            _fill.color = _rest;
            _held = false;
        }
    }
}
