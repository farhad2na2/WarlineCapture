using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
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
            map.texture = painted != null ? painted : SkirmishLibraryPreviewAssets.Placeholder();
            map.color = TheatrePaintDim();
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
            bool reframed = viewport.X > 0.5f || viewport.W < plateWidth - 0.5f;
            RawImage theatrePaint = null;
            if (painted != null)
            {
                var raw = new GameObject("TheaterPaint", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                raw.transform.SetParent(fit, false);
                if (reframed)
                    PlaceTopLeft(raw.rectTransform, viewport.X, viewport.Y, viewport.W, viewport.H);
                else
                    Stretch(raw.rectTransform);
                raw.texture = painted;
                raw.color = TheatrePaintDim();
                raw.raycastTarget = false;
                raw.rectTransform.localScale = Vector3.one;
                if (reframed)
                    ApplyPlainTheatreCover(raw, viewport.W, viewport.H);
                else
                    ApplyTheatreCover(raw, plateWidth, plateHeight);
                theatrePaint = raw;
                var shade = new GameObject("TheatreShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                shade.transform.SetParent(fit, false);
                Stretch(shade.rectTransform);
                shade.sprite = ZoneVignetteSprite();
                shade.color = new Color(0.02f, 0.03f, 0.04f, 0.35f);
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

            PlaceZoneSlots(slots, safe, plateWidth, plateHeight, fa);
            bool wideFrame = plateHeight > 2f && plateWidth / plateHeight >= 2.4f;
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
                button.onClick.AddListener(() => SelectTheaterRegion(captured));
                _regionButtons.Add(button);
            }
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
            return new Color(1f, 0.96f, 0.90f, 1f);
        }

        private static Texture2D LoadTheatreBase()
        {
            return SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.TheatreBaseKey);
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
            Color fill = color;
            fill.a = 0.40f;
            root.GetComponent<V3PolygonGraphic>().Configure(local, fill);
            PlaceDistrictHalo(root, local, color);
            Color outer = color;
            outer.a = 0.96f;
            Color glow = Color.Lerp(color, Color.white, 0.18f);
            glow.a = 1f;
            Color ink = Color.Lerp(color, Color.white, selected ? 0.74f : 0.62f);
            ink.a = 1f;
            // Vivid core inside the 3–4.5 capture band. The bright edge is the outer glow.
            const float stroke = 4f;
            DrawHexEdges(root, local, outer, 26f, ZoneStrokeSprite());
            DrawHexEdges(root, local, glow, 14f, ZoneStrokeSprite());
            DrawHexEdges(root, local, ink, stroke, ZoneSolidSprite());
            DrawHexLattice(root, local, color);
            for (int i = 0; i < local.Length; i++)
            {
                var cap = new GameObject("HexCap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                cap.transform.SetParent(root, false);
                cap.sprite = ZoneCapSprite();
                cap.color = ink;
                cap.raycastTarget = false;
                PlaceHexPoint(cap.rectTransform, local[i], stroke + 3f);
            }
        }

        private static void PlaceDistrictHalo(RectTransform root, Vector2[] localPoints, Color color)
        {
            const float pad = 22f;
            Sprite halo = BuildDistrictHalo(localPoints, pad);
            if (halo == null || root == null)
                return;
            BoundsOf(localPoints, 0f, out float minX, out float minY, out float maxX, out float maxY);
            var image = new GameObject("ZoneHalo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(root, false);
            image.sprite = halo;
            image.type = Image.Type.Simple;
            Color glow = Color.Lerp(color, Color.white, 0.35f);
            glow.a = 1f;
            image.color = glow;
            image.raycastTarget = false;
            PlaceTopLeft(image.rectTransform, minX - pad, minY - pad, (maxX - minX) + pad * 2f, (maxY - minY) + pad * 2f);
            image.rectTransform.localScale = Vector3.one;
        }

        private static Sprite BuildDistrictHalo(Vector2[] localPoints, float pad)
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
            // Edge band only. A filled blur stacks on the 0.23 tint and hides the painting.
            const float reach = 18f;
            float originX = minX - pad;
            float originY = minY - pad;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int target = (height - 1 - y) * width;
                for (int x = 0; x < width; x++)
                {
                    var point = new Vector2(originX + (x + 0.5f) / scale, originY + (y + 0.5f) / scale);
                    float dist = DistanceToPolygonEdge(point, localPoints);
                    float band = 1f - Mathf.Clamp01(dist / reach);
                    band = band * band * (3f - 2f * band);
                    byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(band * 255f), 0, 255);
                    pixels[target + x] = new Color32(255, 255, 255, alpha);
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
            Color ink = Color.Lerp(color, Color.white, 0.45f);
            ink.a = 0.42f;
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
            plate.color = new Color(0.03f, 0.05f, 0.07f, 0.92f);
            plate.raycastTarget = false;
            PlaceTopLeft(plate.rectTransform, slot.Cue.x, slot.Cue.y, slot.Cue.width, slot.Cue.height);
            plate.rectTransform.localScale = Vector3.one;
            var cue = CreateLabel(parent, "ZoomCue", 20f);
            PlaceTopLeft(cue.rectTransform, slot.Cue.x, slot.Cue.y, slot.Cue.width, slot.Cue.height);
            cue.alignment = TextAlignmentOptions.Center;
            cue.isRightToLeftText = fa;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.fontStyle = FontStyles.Bold;
            cue.color = new Color(1f, 0.97f, 0.9f, 1f);
            SetCopy(cue, fa ? "لمس برای بزرگنمایی" : "Tap to zoom", 22f, 32f);
            cue.alignment = TextAlignmentOptions.Center;
            cue.isRightToLeftText = fa;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.maxVisibleLines = 1;
            cue.fontStyle = FontStyles.Bold;
            cue.color = new Color(1f, 0.97f, 0.9f, 1f);
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

            float cardH = slot.Card.height;
            float nameH = Mathf.Min(TheatreSeatGeometry.MaxNameBoxHeight(cardH, wideFrame), cardH >= 190f ? 64f : 48f);
            card.sprite = ZoneSolidSprite();
            card.type = Image.Type.Simple;
            card.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
            var name = CreateLabel(card.transform, "ZoneName", 18f);
            name.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
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
            medallion.color = slot.Zone;
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
            track.sprite = ZoneRingSprite();
            track.type = Image.Type.Simple;
            track.color = pending
                ? new Color(slot.Zone.r, slot.Zone.g, slot.Zone.b, 0.55f)
                : new Color(1f, 1f, 1f, 0.55f);
            track.raycastTarget = false;
            var fill = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(track.transform, false);
            Stretch(fill.rectTransform);
            fill.sprite = ZoneRingSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = ratio;
            fill.color = BrightRingFill(slot.Zone);
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
            preview.color = Color.white;
            preview.uvRect = new Rect(0f, 0f, 1f, 1f);
            Texture2D plate = PaintedPlate(mapId);
            if (plate != null)
            {
                preview.texture = plate;
                CoverCrop(preview, ZoneThumbSize, ZoneThumbSize);
                return;
            }

            Texture2D painted = LoadTheatreBase();
            preview.texture = painted;
            if (painted == null)
                return;

            int index = 0;
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                if (!string.Equals(SkirmishBattleLibraryArt.MapIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                index = i;
                break;
            }

            Rect region = SkirmishBattleLibraryArt.TheaterRegions[index];
            Vector2 centre = SkirmishBattleLibraryArt.TheaterLabelCentres[index];
            float side = Mathf.Min(region.width, region.height) * 0.92f;
            float x = Mathf.Clamp(centre.x - side * 0.5f, region.x, Mathf.Max(region.x, region.xMax - side));
            float y = Mathf.Clamp(centre.y - side * 0.5f, region.y, Mathf.Max(region.y, region.yMax - side));
            preview.uvRect = new Rect(x, 1f - (y + side), side, side);
        }

        private void SelectTheaterRegion(string mapId)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.SelectMapCollection(mapId);
            _libraryViewModel.BackToTheater();
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
            // The frame pass runs after the seat stack. Refit from the card's final rect so a
            // layout group, an early size, or a parent scale cannot leave a ~38px tile.
            RefitTheatreCardArt();
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

        /// <summary>
        /// The packer already left the cue clear of the leader. Fit the label inside that rect.
        /// A one-line cue that already fits is left alone, so the wide plates keep their label.
        /// </summary>
        private static void FitCueInsidePackedRect(TMP_Text cue)
        {
            if (cue == null || cue.rectTransform == null)
                return;
            RectTransform rect = cue.rectTransform;
            float boxW = rect.rect.width > 1f ? rect.rect.width : rect.sizeDelta.x;
            float boxH = rect.rect.height > 1f ? rect.rect.height : rect.sizeDelta.y;
            if (boxW < 8f || boxH < 8f)
                return;
            string source = FlattenCopy(cue.text);
            if (source.Length == 0)
                return;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.maxVisibleLines = 1;
            FitCopyInside(cue, 11f);
            cue.ForceMeshUpdate();
            float line = MeasuredNameWidth(cue);
            float height = cue.preferredHeight;
            if (line > 0.5f && line <= boxW + 2f && height <= boxH + 2f && !cue.isTextTruncated && !cue.isTextOverflowing)
                return;
            CueLabelSplits(cue, source, cue.fontSize, boxW, boxH);
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
        /// Keeps a zoom pill that already misses every district, or moves it to a plate corner
        /// or edge margin that does. Zone cards are not moved.
        /// </summary>
        private static bool SeatZoomCueClearOfHexes(TMP_Text cue, bool fa, string selectedId)
        {
            if (cue == null || cue.rectTransform == null || cue.rectTransform.parent is not RectTransform parent)
                return false;
            float frameW = parent.rect.width > 2f ? parent.rect.width : parent.sizeDelta.x;
            float frameH = parent.rect.height > 2f ? parent.rect.height : parent.sizeDelta.y;
            if (frameW < 32f || frameH < 32f)
                return false;
            Rect current = new Rect(
                cue.rectTransform.anchoredPosition.x,
                -cue.rectTransform.anchoredPosition.y,
                cue.rectTransform.sizeDelta.x,
                cue.rectTransform.sizeDelta.y);
            Rect detail = SeatingDetailRect(frameW, frameH, fa);
            if (current.width >= 96f && current.height >= 28f && MarginSpotClear(parent, current, frameW, frameH, detail))
            {
                SyncCuePlate(parent, current);
                FitCueInsidePackedRect(cue);
                return true;
            }

            float[] heights = { 44f, 32f };
            float widest = Mathf.Max(current.width, 220f);
            for (int pass = 0; pass < heights.Length; pass++)
            {
                float height = heights[pass];
                for (float width = widest; width >= 120f - 0.5f; width -= 20f)
                {
                    if (!TryMarginSpot(parent, frameW, frameH, width, height, detail, selectedId, out Rect spot))
                        continue;
                    PlaceTopLeft(cue.rectTransform, spot.x, spot.y, spot.width, spot.height);
                    SyncCuePlate(parent, spot);
                    FitCueInsidePackedRect(cue);
                    return true;
                }
            }

            return false;
        }

        private static void SyncCuePlate(RectTransform parent, Rect spot)
        {
            if (parent != null && parent.Find("CuePlate") is RectTransform plate)
                PlaceTopLeft(plate, spot.x, spot.y, spot.width, spot.height);
        }

        private static bool TryMarginSpot(
            RectTransform parent,
            float frameW,
            float frameH,
            float width,
            float height,
            Rect detail,
            string selectedId,
            out Rect spot)
        {
            spot = default;
            if (width < 8f || height < 8f || width > frameW - 24f || height > frameH - 24f)
                return false;
            Vector2 pin = SelectedPinCentre(parent, selectedId);
            const float edge = 12f;
            var corners = new[]
            {
                new Rect(edge, edge, width, height),
                new Rect(frameW - edge - width, edge, width, height),
                new Rect(edge, frameH - edge - height, width, height),
                new Rect(frameW - edge - width, frameH - edge - height, width, height)
            };
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < corners.Length; i++)
                TakeMarginSpot(parent, corners[i], frameW, frameH, detail, pin, ref spot, ref best, ref found);
            for (float x = edge; x + width <= frameW - edge + 0.1f; x += 8f)
            {
                TakeMarginSpot(parent, new Rect(x, edge, width, height), frameW, frameH, detail, pin, ref spot, ref best, ref found);
                TakeMarginSpot(parent, new Rect(x, frameH - edge - height, width, height), frameW, frameH, detail, pin, ref spot, ref best, ref found);
            }

            for (float y = edge; y + height <= frameH - edge + 0.1f; y += 8f)
            {
                TakeMarginSpot(parent, new Rect(edge, y, width, height), frameW, frameH, detail, pin, ref spot, ref best, ref found);
                TakeMarginSpot(parent, new Rect(frameW - edge - width, y, width, height), frameW, frameH, detail, pin, ref spot, ref best, ref found);
            }

            return found;
        }

        private static void TakeMarginSpot(
            RectTransform parent,
            Rect candidate,
            float frameW,
            float frameH,
            Rect detail,
            Vector2 pin,
            ref Rect spot,
            ref float best,
            ref bool found)
        {
            if (!MarginSpotClear(parent, candidate, frameW, frameH, detail))
                return;
            float reach = Vector2.Distance(candidate.center, pin);
            if (found && reach >= best)
                return;
            spot = candidate;
            best = reach;
            found = true;
        }

        private static bool MarginSpotClear(RectTransform parent, Rect spot, float frameW, float frameH, Rect detail)
        {
            const float edge = 12f;
            if (spot.xMin < edge || spot.yMin < edge || spot.xMax > frameW - edge || spot.yMax > frameH - edge)
                return false;
            if (!CueMissesDetail(spot, detail))
                return false;
            if (PillHitsLiveHex(parent, InflateRect(spot, 16f)))
                return false;
            var cards = new List<Rect>(8);
            var pins = new List<Rect>(8);
            CollectOverlayRects(parent, "ZoneCard_", cards);
            CollectOverlayRects(parent, "ZonePin_", pins);
            return CueClears(spot, cards, 4f) && CueClears(spot, pins, 4f);
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
                RaiseTheatreReadability(_libraryRegions);
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
                LogTheatreSeats(cue.transform.parent, fa);
                return;
            }

            cue.alignment = TextAlignmentOptions.Center;
            cue.isRightToLeftText = fa;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.enableAutoSizing = true;
            cue.fontSizeMin = 22f;
            cue.fontSizeMax = 32f;
            cue.maxVisibleLines = 1;
            cue.ForceMeshUpdate();
            Vector2 preferred = cue.GetPreferredValues();
            const float padX = 28f;
            const float padY = 14f;
            float width = Mathf.Max(220f, Mathf.Ceil(preferred.x + padX));
            float height = Mathf.Max(44f, Mathf.Ceil(preferred.y + padY));
            RectTransform rect = cue.rectTransform;
            RectTransform parent = rect.parent as RectTransform;
            string selectedId = _libraryViewModel != null ? _libraryViewModel.Theater.SelectedMapId : "DB";
            if (string.IsNullOrEmpty(selectedId))
                selectedId = "DB";
            RectTransform pin = parent != null ? parent.Find("ZonePin_" + selectedId) as RectTransform : null;

            float frameW = parent != null ? parent.rect.width : 0f;
            float frameH = parent != null ? parent.rect.height : 0f;
            if (frameW < 2f && parent != null)
                frameW = parent.sizeDelta.x;
            if (frameH < 2f && parent != null)
                frameH = parent.sizeDelta.y;
            const float inset = 12f;
            const float pinGap = 8f;
            float left = rect.anchoredPosition.x;
            float top = -rect.anchoredPosition.y;
            if (pin != null)
                PlaceCueClearOfPin(pin, width, height, frameW, frameH, inset, pinGap, ref left, ref top);
            if (frameW > width + inset * 2f)
                left = Mathf.Clamp(left, inset, frameW - inset - width);
            if (frameH > height + inset * 2f)
                top = Mathf.Clamp(top, inset, frameH - inset - height);
            PlaceTopLeft(rect, left, top, width, height);
            if (parent == null)
            {
                RaiseTheatreReadability(_libraryRegions);
                LogTheatreSeats(_libraryRegions, fa);
                return;
            }
            Rect cueRect = new Rect(left, top, width, height);
            for (int pass = 0; pass < 8; pass++)
            {
                bool hit = false;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child == null)
                        continue;
                    bool card = child.name.StartsWith("ZoneCard_", StringComparison.Ordinal);
                    bool pinHit = child.name.StartsWith("ZonePin_", StringComparison.Ordinal);
                    if (!card && !pinHit)
                        continue;
                    var block = child as RectTransform;
                    if (block == null)
                        continue;
                    Rect blockRect = new Rect(block.anchoredPosition.x, -block.anchoredPosition.y, block.sizeDelta.x, block.sizeDelta.y);
                    if (!Overlaps(cueRect, blockRect, 0f))
                        continue;
                    hit = true;
                    if (pinHit)
                    {
                        float below = blockRect.yMax + pinGap;
                        float beside = blockRect.xMax + pinGap;
                        if (below + cueRect.height <= frameH - inset)
                            cueRect.y = below;
                        else
                            cueRect.x = beside;
                    }
                    else
                    {
                        cueRect.y = cueRect.center.y <= blockRect.center.y
                            ? blockRect.yMin - cueRect.height - pinGap
                            : blockRect.yMax + pinGap;
                    }
                }

                if (frameW > cueRect.width + inset * 2f)
                    cueRect.x = Mathf.Clamp(cueRect.x, inset, frameW - inset - cueRect.width);
                if (frameH > cueRect.height + inset * 2f)
                    cueRect.y = Mathf.Clamp(cueRect.y, inset, frameH - inset - cueRect.height);
                if (!hit)
                    break;
            }

            ClearCueFromPins(parent, frameW, frameH, inset, pinGap, ref cueRect);
            ShiftCueOffCards(parent, frameW, frameH, inset, ref cueRect);
            ClearCueFromPins(parent, frameW, frameH, inset, pinGap, ref cueRect);
            PlaceTopLeft(rect, cueRect.x, cueRect.y, cueRect.width, cueRect.height);
            cue.ForceMeshUpdate();
            float natural = Mathf.Max(cue.preferredWidth, cue.GetPreferredValues().x);
            if (natural + padX > cueRect.width + 1f)
            {
                float fitted = Mathf.Ceil(natural + padX);
                float center = cueRect.x + cueRect.width * 0.5f;
                cueRect.width = fitted;
                cueRect.x = center - fitted * 0.5f;
                if (frameW > cueRect.width + inset * 2f)
                    cueRect.x = Mathf.Clamp(cueRect.x, inset, frameW - inset - cueRect.width);
                ClearCueFromPins(parent, frameW, frameH, inset, pinGap, ref cueRect);
                ShiftCueOffCards(parent, frameW, frameH, inset, ref cueRect);
                ClearCueFromPins(parent, frameW, frameH, inset, pinGap, ref cueRect);
                PlaceTopLeft(rect, cueRect.x, cueRect.y, cueRect.width, cueRect.height);
                cue.ForceMeshUpdate();
            }

            Rect detail = SeatingDetailRect(frameW, frameH, fa);
            float worldScaleX = Mathf.Max(0.05f, Mathf.Abs(parent.lossyScale.x));
            float worldScaleY = Mathf.Max(0.05f, Mathf.Abs(parent.lossyScale.y));
            const float nearLimit = 160f;
            Rect fullCue = cueRect;
            bool cueSeated = SeatCueNearPin(parent, pin, selectedId, frameW, frameH, inset, detail, nearLimit, worldScaleX, worldScaleY, ref cueRect);
            if (!cueSeated)
            {
                float naturalWidth = fullCue.width;
                for (float candidateWidth = naturalWidth - 20f; candidateWidth >= 96f; candidateWidth -= 12f)
                {
                    cueRect.width = candidateWidth;
                    if (!SeatCueNearPin(parent, pin, selectedId, frameW, frameH, inset, detail, nearLimit, worldScaleX, worldScaleY, ref cueRect))
                        continue;
                    cueSeated = true;
                    break;
                }
            }

            if (!cueSeated)
            {
                cueRect.width = 96f;
                cueSeated = SeatCueNearPin(parent, pin, selectedId, frameW, frameH, inset, detail, nearLimit, worldScaleX, worldScaleY, ref cueRect);
            }

            if (!cueSeated)
                cueRect = fullCue;
            if (!MarginSpotClear(parent, cueRect, frameW, frameH, detail))
            {
                float[] heights = { cueRect.height >= 32f ? cueRect.height : 44f, 32f };
                for (int pass = 0; pass < heights.Length; pass++)
                {
                    float height = heights[pass];
                    bool placed = false;
                    for (float marginWidth = Mathf.Max(cueRect.width, 160f); marginWidth >= 120f - 0.5f; marginWidth -= 20f)
                    {
                        if (!TryMarginSpot(parent, frameW, frameH, marginWidth, height, detail, selectedId, out Rect margin))
                            continue;
                        cueRect = margin;
                        placed = true;
                        break;
                    }

                    if (placed)
                        break;
                }
            }

            PlaceTopLeft(rect, cueRect.x, cueRect.y, cueRect.width, cueRect.height);

            Transform plate = parent.Find("CuePlate");
            if (plate is RectTransform plateRect)
                PlaceTopLeft(plateRect, cueRect.x, cueRect.y, cueRect.width, cueRect.height);
            RaiseTheatreReadability(parent);
            LogTheatreSeats(parent, fa);
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
                {
                    slot.Card = new Rect(seat.Card.X, seat.Card.Y, seat.Card.W, seat.Card.H);
                    slot.Cue = new Rect(layout.Cue.X, layout.Cue.Y, layout.Cue.W, layout.Cue.H);
                }

                slots[i] = slot;
            }

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
                    RaiseTheatreLine(text, 16f, 28f);
            }

        }

        private const float ZoneNameFontCeiling = 32f;
        private const float ZoneNameFontFloor = 16f;
        /// <summary>Drawn name size. The packer still stops at NameFontCeiling so the seats do not move.</summary>
        private const float DrawnNameCeiling = 32f;
        /// <summary>About 80% of the approved-1 cap height. One line at this size is the aim.</summary>
        private const float DrawnNameTarget = 26f;

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
            // One size for the plate: the largest that fits inside every card, using tracking
            // and a second line when one line will not.
            float shared = DrawnNameCeiling;
            bool any = false;
            for (int i = 0; i < names.Count && i < cards.Count; i++)
            {
                float used = FitOnePortraitName(names[i], cards[i], boxes, i, detail, cue, hint, plateHeight, wide);
                if (used < 1f)
                    continue;
                any = true;
                if (used < shared)
                    shared = used;
            }

            if (!any)
                return;
            for (int i = 0; i < names.Count && i < cards.Count; i++)
            {
                if (Mathf.Abs(names[i].fontSize - shared) > 0.4f)
                    TryCommitPortraitName(names[i], cards[i], shared, wide);
                RecordDrawnZoneName(names[i]);
            }
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
            bool wide)
        {
            if (name == null || card == null)
                return 0f;
            float cardW = card.sizeDelta.x;
            float boxWidth = PortraitNameWidth(cardW);
            string source = FlattenCopy(name.text);
            if (name.rectTransform != null)
                name.rectTransform.sizeDelta = new Vector2(boxWidth, Mathf.Max(24f, name.rectTransform.sizeDelta.y));
            float fontFloor = cardW <= TheatreSeatGeometry.PortraitMinWidth + 8f
                ? TheatreSeatGeometry.PortraitCompactFontFloor
                : TheatreSeatGeometry.NameFontFloor;
            if (fontFloor > TheatreSeatGeometry.PortraitCompactFontFloor
                && boxWidth <= 114f
                && ContainsArabicLetter(source))
                fontFloor = TheatreSeatGeometry.PortraitCompactFontFloor;
            // Noto's Arabic line box is taller than the Latin one. Step under the compact floor
            // only until two whole words fit inside the v47 name budget.
            if (ContainsArabicLetter(source))
                fontFloor = Mathf.Min(fontFloor, 13f);
            for (float size = DrawnNameCeiling; size >= fontFloor - 0.01f; size -= 1f)
            {
                if (TryCommitPortraitName(name, card, size, wide))
                    return size;
            }

            TightenPortraitName(name, card, source, wide);
            return name.fontSize;
        }

        /// <summary>
        /// Puts every zone title on this plate at <paramref name="size"/>. Returns false when the
        /// words or the name box will not hold that size.
        /// </summary>
        private static bool TryCommitPortraitName(TMP_Text name, RectTransform card, float size, bool wide)
        {
            if (name == null || card == null || size < 1f)
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
            bool latin = !ContainsArabicLetter(source);
            bool oneLine = FitsNameLine(name, source, size, boxWidth, latin ? -12f : 0f);
            if (name.rectTransform != null)
                name.rectTransform.sizeDelta = new Vector2(boxWidth, Mathf.Max(24f, name.rectTransform.sizeDelta.y));
            if (!oneLine)
            {
                name.characterSpacing = 0f;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.maxVisibleLines = 2;
                if (!WrapZoneName(name, size))
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
            if (TheatreSeatGeometry.MaxNameBoxHeight(cardH, wide) + 1f < needed)
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

        private static void TightenPortraitName(TMP_Text name, RectTransform card, string source, bool wide)
        {
            if (name == null || card == null)
                return;
            float size = card.sizeDelta.x <= TheatreSeatGeometry.PortraitMinWidth + 8f
                ? TheatreSeatGeometry.PortraitCompactFontFloor
                : TheatreSeatGeometry.NameFontFloor;
            float boxWidth = PortraitNameWidth(card.sizeDelta.x);
            bool arabic = ContainsArabicLetter(source);
            if (size > TheatreSeatGeometry.PortraitCompactFontFloor && boxWidth <= 114f && arabic)
                size = TheatreSeatGeometry.PortraitCompactFontFloor;
            float stop = arabic ? 13f : size;
            var binding = name.GetComponent<V3LocalizedTextBindingView>();
            for (float candidate = Mathf.Max(size, DrawnNameTarget); candidate >= stop - 0.01f; candidate -= 1f)
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
                if (!FitsNameLine(name, source, candidate, boxWidth, arabic ? 0f : -12f))
                    continue;
                float needed = PortraitNameBox(name, source, candidate, card.sizeDelta.y);
                if (TheatreSeatGeometry.MaxNameBoxHeight(card.sizeDelta.y, wide) + 1f < needed)
                    continue;
                ApplyPortraitNameLayout(card, name, needed, card.sizeDelta.y, wide, true);
                name.lineSpacing = 0f;
                name.ForceMeshUpdate();
                if (PortraitNameFitsRect(name, source))
                    return;
            }

            float wrapStart = arabic ? 13f : DrawnNameTarget;
            float wrapStop = arabic ? 13f : size;
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
                if (!WrapZoneName(name, wrapped))
                    continue;
                float wrappedHeight = PortraitNameBox(name, source, wrapped, card.sizeDelta.y);
                if (TheatreSeatGeometry.MaxNameBoxHeight(card.sizeDelta.y, wide) + 1f < wrappedHeight)
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
        /// Full-width portrait at about 68% of the card, name overlaid on its top so two lines
        /// stay legal, progress ring on the portrait's bottom corner, status line across the bottom.
        /// The seat rect is left alone.
        /// </summary>
        private static void LayoutZoneCardStack(RectTransform card, TMP_Text name, float nameH, float cardH, bool wide)
        {
            if (card == null)
                return;
            float cardW = card.sizeDelta.x;
            const float pad = 2f;
            const float gap = 2f;
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
            float innerW = Mathf.Max(48f, cardW - pad * 2f);
            DominantCardBands(cardH, wide, out float nameRoom, out float portraitH, out float statusH);
            float maxName = TheatreSeatGeometry.MaxNameBoxHeight(cardH, wide);
            if (nameH > maxName)
                nameH = maxName;
            if (nameH < 1f)
                nameH = 1f;
            if (nameH > nameRoom)
                nameH = nameRoom;
            float stackRoom = cardH - TheatreSeatGeometry.NameStackChrome(cardH);
            if (nameH + statusH > stackRoom + 0.5f)
                statusH = Mathf.Max(22f, stackRoom - nameH);

            if (name != null && name.rectTransform != null)
                PlaceTopLeft(name.rectTransform, nameX, pad, nameW, nameH);

            float statusY = cardH - pad - statusH;
            float portraitTop = statusY - gap - portraitH;
            if (portraitTop < pad)
                portraitTop = pad;
            if (portraitTop + portraitH > cardH)
                portraitH = Mathf.Max(8f, cardH - portraitTop);
            float portraitW = innerW;

            RectTransform thumb = card.Find("ThumbFrame") as RectTransform;
            if (thumb != null)
            {
                PlaceTopLeft(thumb, pad, portraitTop, portraitW, portraitH);
                thumb.localScale = Vector3.one;
                KeepAuthoredSize(thumb);
                if (thumb.Find("Thumbnail") is RectTransform preview)
                {
                    Stretch(preview);
                    preview.localScale = Vector3.one;
                    KeepAuthoredSize(preview);
                    RawImage picture = preview.GetComponent<RawImage>();
                    if (picture != null && picture.texture != null)
                        CoverCrop(picture, portraitW, portraitH);
                }
            }

            RectTransform ring = card.Find("ProgressRing") as RectTransform;
            RectTransform disc = null;
            if (ring != null)
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                float ringSide = Mathf.Clamp(Mathf.Min(portraitW, portraitH) * 0.46f, 52f, 76f);
                if (ringSide > portraitH - 4f)
                    ringSide = Mathf.Max(28f, portraitH - 4f);
                float ringX = fa ? pad + 4f : pad + portraitW - 4f - ringSide;
                float ringBottom = Mathf.Min(portraitTop + portraitH, statusY) - 4f;
                float ringY = ringBottom - ringSide;
                if (ringY < portraitTop)
                    ringY = portraitTop;
                disc = EnsureRingDisc(card);
                PlaceTopLeft(disc, ringX, ringY, ringSide, ringSide);
                disc.localScale = Vector3.one;
                KeepAuthoredSize(disc);
                PlaceTopLeft(ring, ringX, ringY, ringSide, ringSide);
                ring.localScale = Vector3.one;
                KeepAuthoredSize(ring);
            }

            RectTransform shade = null;
            if (name != null)
            {
                shade = EnsureNameShade(card);
                PlaceTopLeft(shade, nameX, pad, nameW, nameH);
                shade.localScale = Vector3.one;
                KeepAuthoredSize(shade);
            }

            Transform countNode = card.Find("ZoneCount");
            if (countNode == null && ring != null)
                countNode = ring.Find("ZoneCount");
            if (countNode != null && countNode.parent != card)
                countNode.SetParent(card, false);
            if (countNode is RectTransform countRect)
                PlaceTopLeft(countRect, pad, statusY, innerW, Mathf.Max(1f, statusH));
            if (thumb != null)
                thumb.SetAsFirstSibling();
            if (disc != null)
                disc.SetSiblingIndex(thumb != null ? thumb.GetSiblingIndex() + 1 : 0);
            if (ring != null)
                ring.SetSiblingIndex(disc != null ? disc.GetSiblingIndex() + 1 : (thumb != null ? thumb.GetSiblingIndex() + 1 : 0));
            if (shade != null)
                shade.SetSiblingIndex(ring != null ? ring.GetSiblingIndex() + 1 : (disc != null ? disc.GetSiblingIndex() + 1 : 0));
            if (name != null)
                name.transform.SetAsLastSibling();
            if (countNode != null)
                countNode.SetAsLastSibling();
            if (countNode != null && countNode.TryGetComponent<TMP_Text>(out var count))
                FitCopyInside(count, 11f);
        }

        /// <summary>
        /// Portrait is 68% of the card (kept inside 65–70%). The name overlays that portrait,
        /// so its room is the full two-line budget. The status row is tall enough for "به زودی".
        /// </summary>
        private static void DominantCardBands(float cardH, bool wide, out float nameRoom, out float portraitH, out float statusH)
        {
            statusH = cardH >= 190f ? 40f : 34f;
            float cap = TheatreSeatGeometry.MaxNameBoxHeight(cardH, wide);
            nameRoom = cap;
            if (nameRoom < 18f)
                nameRoom = 18f;
            float low = cardH * 0.65f;
            float high = cardH * 0.70f;
            portraitH = Mathf.Round(cardH * 0.68f);
            if (portraitH < low)
                portraitH = Mathf.Ceil(low);
            if (portraitH > high)
                portraitH = Mathf.Floor(high);
            if (portraitH < 8f)
                portraitH = 8f;
        }

        /// <summary>Dark bar behind the overlaid title so a two-line name still reads on the photo.</summary>
        private static RectTransform EnsureNameShade(RectTransform card)
        {
            RectTransform shade = card.Find("NameShade") as RectTransform;
            if (shade != null)
                return shade;
            var image = new GameObject("NameShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(card, false);
            image.sprite = ZoneSolidSprite();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.color = new Color(0.03f, 0.04f, 0.05f, 0.72f);
            return image.rectTransform;
        }

        /// <summary>Dark circle drawn behind the progress ring so the arc reads on the photo.</summary>
        private static RectTransform EnsureRingDisc(RectTransform card)
        {
            RectTransform disc = card.Find("RingDisc") as RectTransform;
            if (disc != null)
                return disc;
            var image = new GameObject("RingDisc", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(card, false);
            image.sprite = ZoneDiscSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(0.05f, 0.04f, 0.03f, 0.92f);
            return image.rectTransform;
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
                float cardW = card.sizeDelta.x;
                float cardH = card.sizeDelta.y;
                float liveW = card.rect.width;
                float liveH = card.rect.height;
                if (liveW > 2f && Mathf.Abs(liveW - cardW) > 8f)
                    cardW = liveW;
                if (liveH > 2f && Mathf.Abs(liveH - cardH) > 8f)
                    cardH = liveH;
                if (Mathf.Abs(cardW - card.sizeDelta.x) > 0.5f || Mathf.Abs(cardH - card.sizeDelta.y) > 0.5f)
                {
                    PlaceTopLeft(card, card.anchoredPosition.x, -card.anchoredPosition.y, cardW, cardH);
                    card.localScale = Vector3.one;
                }

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
                return;
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
                width += size * (line[i] <= 127 ? 0.7f : 0.95f);
            return width;
        }

        private static void GrowZoneNameBox(TMP_Text name)
        {
            RectTransform rect = name.rectTransform;
            if (rect == null)
                return;
            float limit = ZoneNameHeightLimit(rect);
            float needed = Mathf.Ceil(name.preferredHeight + 2f);
            float height = Mathf.Min(limit, Mathf.Max(rect.sizeDelta.y, needed));
            if (height > rect.sizeDelta.y + 0.5f)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            NudgeRingBelowName(rect);
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
            DominantCardBands(cardH, wide, out float nameRoom, out _, out _);
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

        private static Color RegionZoneColor(string mapId)
        {
            switch ((mapId ?? string.Empty).ToUpperInvariant())
            {
                case "CC": return new Color(0.82f, 0.32f, 0.28f, 1f);
                case "MP": return new Color(0.92f, 0.46f, 0.12f, 1f);
                case "IB": return new Color(0.78f, 0.56f, 0.16f, 1f);
                case "AP": return new Color(0.14f, 0.55f, 0.50f, 1f);
                default: return new Color(0.88f, 0.62f, 0.18f, 1f);
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
                float dy = Mathf.Abs(y - (height - 1) * 0.5f) / (height * 0.5f);
                float alpha = Mathf.Clamp01(1f - dy);
                alpha = alpha * alpha * (3f - 2f * alpha);
                byte a = (byte)Mathf.RoundToInt(alpha * 255f);
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
}
