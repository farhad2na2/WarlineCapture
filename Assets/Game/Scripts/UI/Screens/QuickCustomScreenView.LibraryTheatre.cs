using System;
using System.Collections.Generic;
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
            ClearChildren(_libraryRegions);
            _regionButtons.Clear();
            RawImage map = transform.Find("SkirmishSetupComposition/OperationPreview/MapPreviewClip/MapPreview")
                ?.GetComponent<RawImage>();
            bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
            if (battlefield || theater.Pins == null)
            {
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

            if (painted != null)
            {
                var raw = new GameObject("TheaterPaint", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                raw.transform.SetParent(fit, false);
                Stretch(raw.rectTransform);
                raw.texture = painted;
                raw.color = TheatrePaintDim();
                raw.raycastTarget = false;
                raw.rectTransform.localScale = Vector3.one;
                ApplyTheatreCover(raw, plateWidth, plateHeight);
                var shade = new GameObject("TheatreShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                shade.transform.SetParent(fit, false);
                Stretch(shade.rectTransform);
                shade.sprite = ZoneVignetteSprite();
                shade.color = new Color(0.02f, 0.03f, 0.04f, 0.35f);
                shade.raycastTarget = false;
                shade.rectTransform.localScale = Vector3.one;
            }

            if (map != null)
                map.enabled = painted == null;

            Rect safe = Rect.MinMaxRect(
                TheatreSafeLeft,
                TheatreSafeTop,
                Mathf.Max(TheatreSafeLeft + ZoneCardWidth, plateWidth - TheatreSafeRight),
                Mathf.Max(TheatreSafeTop + ZoneCardHeight, plateHeight - TheatreSafeBottom));
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
                PlaceZonePin(fit, slots[i], fa);
                PlaceZoneCard(fit, slots[i], fa, wideFrame);
            }

            for (int i = 0; i < slots.Count; i++)
                DrawZoneLeader(fit, slots[i]);

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
            return new Color(0.94f, 0.95f, 0.96f, 1f);
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
            float frameAspect = frameWidth > 2f && frameHeight > 2f ? frameWidth / frameHeight : sourceAspect;
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

        private static Vector2 MapPaintingPoint(
            Vector2 normalized,
            float cropU,
            float cropV,
            float cropW,
            float cropH,
            float frameWidth,
            float frameHeight)
        {
            return new Vector2(
                (normalized.x - cropU) / Mathf.Max(0.001f, cropW) * frameWidth,
                (normalized.y - cropV) / Mathf.Max(0.001f, cropH) * frameHeight);
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
        /// District outline in the map clip (y down). Wide frames stay flat-top hexes.
        /// The square frame, and the landscape districts that have to clear the detail
        /// panel, use a convex plate polygon so the whole card can sit inside.
        /// </summary>
        private static Vector2[] DistrictHex(string mapId, float frameWidth, float frameHeight)
        {
            float aspect = frameHeight > 2f ? frameWidth / frameHeight : 1.6f;
            float sy = frameHeight > 2f ? frameHeight / 620f : 1f;
            if (aspect >= 2.4f)
            {
                DistrictHexSpec spec = WideDistrictHex(mapId);
                float sx = spec.ReferenceWidth > 2f ? frameWidth / spec.ReferenceWidth : 1f;
                return FlatHex(spec.CenterX * sx, spec.CenterY * sy, spec.HalfWidth * sx, spec.HalfHeight * sy);
            }

            if (aspect >= 1.9f && (mapId == "DB" || mapId == "AP"))
            {
                DistrictHexSpec spec = LandscapeDistrictHex(mapId);
                float sx = spec.ReferenceWidth > 2f ? frameWidth / spec.ReferenceWidth : 1f;
                return FlatHex(spec.CenterX * sx, spec.CenterY * sy, spec.HalfWidth * sx, spec.HalfHeight * sy);
            }

            float reference = aspect >= 1.9f ? 1294f : 1039.6f;
            Vector2[] plate = aspect >= 1.9f ? LandscapeOpenPolygon(mapId) : SquareOpenPolygon(mapId);
            return ScalePlatePolygon(plate, reference, frameWidth, frameHeight);
        }

        private static DistrictHexSpec LandscapeDistrictHex(string mapId)
        {
            if (mapId == "AP")
                return HexSpec(1294f, 760f, 468f, 280f, 76f);
            return HexSpec(1294f, 260f, 150f, 200f, 100f);
        }

        private static DistrictHexSpec WideDistrictHex(string mapId)
        {
            switch (mapId)
            {
                case "CC": return HexSpec(1774f, 1420f, 145f, 280f, 109f);
                case "MP": return HexSpec(1774f, 420f, 332f, 240f, 68f);
                case "IB": return HexSpec(1774f, 1500f, 332f, 240f, 68f);
                case "AP": return HexSpec(1774f, 980f, 478f, 330f, 66f);
                default: return HexSpec(1774f, 480f, 145f, 290f, 109f);
            }
        }

        private static Vector2[] LandscapeOpenPolygon(string mapId)
        {
            switch (mapId)
            {
                case "CC":
                    return new[]
                    {
                        new Vector2(800f, 100f), new Vector2(950f, 60f), new Vector2(990f, 60f),
                        new Vector2(1238f, 102f), new Vector2(1238f, 218f), new Vector2(930f, 230f),
                        new Vector2(800f, 230f)
                    };
                case "MP":
                    return new[]
                    {
                        new Vector2(24f, 290f), new Vector2(430f, 290f), new Vector2(430f, 360f),
                        new Vector2(370f, 466f), new Vector2(70f, 466f), new Vector2(24f, 360f)
                    };
                default:
                    return new[]
                    {
                        new Vector2(800f, 288f), new Vector2(976f, 256f), new Vector2(1276f, 256f),
                        new Vector2(1276f, 376f), new Vector2(976f, 376f), new Vector2(800f, 372f)
                    };
            }
        }

        private static Vector2[] SquareOpenPolygon(string mapId)
        {
            switch (mapId)
            {
                case "CC":
                    return new[]
                    {
                        new Vector2(708f, 48f), new Vector2(900f, 36f), new Vector2(1022f, 70f),
                        new Vector2(1022f, 228f), new Vector2(1016f, 230f), new Vector2(718f, 230f),
                        new Vector2(708f, 228f)
                    };
                case "MP":
                    return new[]
                    {
                        new Vector2(200f, 250f), new Vector2(392f, 236f), new Vector2(700f, 236f),
                        new Vector2(700f, 348f), new Vector2(392f, 348f), new Vector2(210f, 318f)
                    };
                case "IB":
                    return new[]
                    {
                        new Vector2(722f, 324f), new Vector2(748f, 248f), new Vector2(1008f, 248f),
                        new Vector2(1018f, 324f), new Vector2(1018f, 436f), new Vector2(722f, 436f)
                    };
                case "AP":
                    return new[]
                    {
                        new Vector2(340f, 460f), new Vector2(352f, 348f), new Vector2(648f, 348f),
                        new Vector2(770f, 500f), new Vector2(784f, 543f), new Vector2(420f, 543f),
                        new Vector2(360f, 500f)
                    };
                default:
                    return new[]
                    {
                        new Vector2(20f, 114f), new Vector2(24f, 48f), new Vector2(200f, 36f),
                        new Vector2(390f, 70f), new Vector2(390f, 228f), new Vector2(318f, 230f),
                        new Vector2(20f, 230f)
                    };
            }
        }

        private static Vector2[] ScalePlatePolygon(Vector2[] plate, float referenceWidth, float frameWidth, float frameHeight)
        {
            if (plate == null || plate.Length == 0)
                return Array.Empty<Vector2>();
            float sx = referenceWidth > 2f ? frameWidth / referenceWidth : 1f;
            float sy = frameHeight > 2f ? frameHeight / 620f : 1f;
            var scaled = new Vector2[plate.Length];
            for (int i = 0; i < plate.Length; i++)
                scaled[i] = new Vector2(plate[i].x * sx, plate[i].y * sy);
            return scaled;
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

        private static void PlaceZoneSlots(List<TheatreZoneSlot> slots, Rect safe, float plateWidth, float plateHeight, bool fa)
        {
            if (slots == null)
                return;
            // Same inset as ReserveTheatreDetail. No-ops when that call already ran, so the
            // detail-panel rebase applies the shrink once.
            safe = ShrinkSeatingForDetail(safe, plateWidth, fa);
            float reserve = SeatingDetailReserve(plateWidth);
            for (int i = 0; i < slots.Count; i++)
            {
                TheatreZoneSlot slot = slots[i];
                float centreX = slot.Anchor.x;
                if (fa && slot.MapId == "DB" && reserve > 0f)
                    centreX = Mathf.Max(centreX, reserve + 56f);
                float pinX = Mathf.Clamp(centreX - ZonePinSize * 0.5f, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - ZonePinSize));
                float pinY = Mathf.Clamp(slot.Anchor.y - ZonePinSize * 0.5f, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - ZonePinSize));
                slot.Pin = new Rect(pinX, pinY, ZonePinSize, ZonePinSize);
                slot.Card = ExplicitZoneCard(slot.MapId, plateWidth, plateHeight, fa);
                float cueX = Mathf.Clamp(slot.Pin.center.x - ZoneCueWidth * 0.5f, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - ZoneCueWidth));
                float cueY = slot.Pin.yMax + ZoneStackGap + 4f;
                if (cueY + ZoneCueHeight > safe.yMax)
                    cueX = slot.Pin.xMax + ZoneStackGap + 4f;
                cueX = Mathf.Clamp(cueX, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - ZoneCueWidth));
                cueY = Mathf.Clamp(cueY, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - ZoneCueHeight));
                slot.Cue = new Rect(cueX, cueY, ZoneCueWidth, ZoneCueHeight);
                slots[i] = slot;
            }

            NudgeCues(slots, safe);
        }

        /// <summary>Width reserved on the trailing edge. Matches the detail panel when the plate is wide enough.</summary>
        private static float SeatingDetailReserve(float plateWidth)
        {
            if (plateWidth < 600f)
                return 0f;
            return Mathf.Clamp(plateWidth * 0.22f, 230f, 290f);
        }

        /// <summary>
        /// Pulls the safe rect off the trailing edge. If the safe rect is already inset, it is returned unchanged.
        /// </summary>
        private static Rect ShrinkSeatingForDetail(Rect safe, float plateWidth, bool fa)
        {
            float reserve = SeatingDetailReserve(plateWidth);
            if (reserve <= 0f)
                return safe;
            float fullRight = Mathf.Max(TheatreSafeLeft + ZoneCardWidth, plateWidth - TheatreSafeRight);
            if (fa)
            {
                if (safe.xMin > TheatreSafeLeft + 1f)
                    return safe;
                float xMin = Mathf.Min(safe.xMax - 40f, safe.xMin + reserve);
                return Rect.MinMaxRect(xMin, safe.yMin, safe.xMax, safe.yMax);
            }

            if (safe.xMax < fullRight - 1f)
                return safe;
            float xMax = Mathf.Max(safe.xMin + 40f, safe.xMax - reserve);
            return Rect.MinMaxRect(safe.xMin, safe.yMin, xMax, safe.yMax);
        }

        private static Rect SeatingDetailRect(float plateWidth, float plateHeight, bool fa)
        {
            float reserve = SeatingDetailReserve(plateWidth);
            if (reserve <= 0f)
                return Rect.zero;
            const float margin = 10f;
            float width = reserve - margin * 2f;
            float height = Mathf.Max(200f, plateHeight - margin * 2f);
            float x = fa ? margin : plateWidth - reserve + margin;
            return new Rect(x, margin, width, height);
        }

        private static Rect ExplicitZoneCard(string mapId, float frameWidth, float frameHeight, bool fa)
        {
            float aspect = frameHeight > 2f ? frameWidth / frameHeight : 1.6f;
            float reference = aspect >= 2.4f ? 1774f : aspect >= 1.9f ? 1294f : 1039.6f;
            float sx = reference > 2f ? frameWidth / reference : 1f;
            float sy = frameHeight > 2f ? frameHeight / 620f : 1f;
            ZoneCardSeat(aspect, fa, mapId, out float x, out float y, out float width, out float height);
            return new Rect(x * sx, y * sy, width * sx, height * sy);
        }

        private static void ZoneCardSeat(float aspect, bool fa, string mapId, out float x, out float y, out float width, out float height)
        {
            if (aspect >= 2.4f)
            {
                width = 320f;
                height = 110f;
                if (fa)
                {
                    switch (mapId)
                    {
                        case "CC": x = 1196f; y = 76f; return;
                        case "MP": x = 666f; y = 268f; return;
                        case "IB": x = 1348f; y = 272f; return;
                        case "AP": x = 762f; y = 420f; return;
                        default: x = 318f; y = 412f; return;
                    }
                }

                switch (mapId)
                {
                    case "CC": x = 820f; y = 28f; return;
                    case "MP": x = 268f; y = 404f; return;
                    case "IB": x = 928f; y = 268f; return;
                    case "AP": x = 762f; y = 420f; return;
                    default: x = 286f; y = 132f; return;
                }
            }

            width = 280f;
            height = 96f;
            if (aspect >= 1.9f)
            {
                if (fa)
                {
                    switch (mapId)
                    {
                        case "CC": x = 948f; y = 112f; return;
                        case "MP": x = 480f; y = 290f; return;
                        case "IB": x = 987f; y = 268f; return;
                        case "AP": x = 530f; y = 402f; return;
                        default: x = 480f; y = 176f; return;
                    }
                }

                switch (mapId)
                {
                    case "CC": x = 500f; y = 40f; return;
                    case "MP": x = 80f; y = 360f; return;
                    case "IB": x = 500f; y = 152f; return;
                    case "AP": x = 530f; y = 402f; return;
                    default: x = 106f; y = 112f; return;
                }
            }

            if (fa)
            {
                switch (mapId)
                {
                    case "CC": x = 728f; y = 124f; return;
                    case "MP": x = 400f; y = 244f; return;
                    case "IB": x = 730f; y = 332f; return;
                    case "AP": x = 360f; y = 356f; return;
                    default: x = 400f; y = 12f; return;
                }
            }

            switch (mapId)
            {
                case "CC": x = 400f; y = 12f; return;
                case "MP": x = 28f; y = 408f; return;
                case "IB": x = 400f; y = 124f; return;
                case "AP": x = 360f; y = 356f; return;
                default: x = 28f; y = 124f; return;
            }
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
            fill.a = 0.23f;
            root.GetComponent<V3PolygonGraphic>().Configure(local, fill);
            PlaceDistrictHalo(root, local, color);
            float stroke = selected ? 4f : 3.5f;
            Color glow = Color.Lerp(color, Color.white, 0.78f);
            glow.a = 0.98f;
            Color ink = selected ? Color.Lerp(color, Color.white, 0.35f) : Color.Lerp(color, Color.white, 0.12f);
            ink.a = 1f;
            DrawHexEdges(root, local, glow, 16f, ZoneStrokeSprite());
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
            Color glow = Color.Lerp(color, Color.white, 0.84f);
            glow.a = 0.92f;
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
            float draw = length - start - 2f;
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
            SetCopy(cue, fa ? "لمس برای بزرگنمایی" : "Tap to zoom", 18f, 24f);
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

            float cardW = slot.Card.width;
            float cardH = slot.Card.height;
            float thumb = wideFrame ? 96f : 92f;
            float nameH = 40f;
            float barH = 14f;
            float barY = 86f;
            if (thumb + 12f > cardH - 4f)
            {
                thumb = Mathf.Max(64f, cardH - 20f);
                nameH = 30f;
                barH = 10f;
                barY = cardH - barH - 4f;
            }

            float thumbX = fa ? cardW - 10f - thumb : 10f;
            float textX = fa ? 12f : 10f + thumb + 12f;
            float textW = Mathf.Max(80f, cardW - thumb - 32f);
            var name = CreateLabel(card.transform, "ZoneName", 22f);
            PlaceTopLeft(name.rectTransform, textX, 6f, textW, nameH);
            name.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            name.isRightToLeftText = fa;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Overflow;
            name.maxVisibleLines = 1;
            name.fontStyle = FontStyles.Bold;
            name.color = new Color(1f, 0.97f, 0.9f, 1f);
            SetCopy(name, MapDisplayTitle(slot.MapId, fa), 18f, 26f);
            name.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            name.isRightToLeftText = fa;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Overflow;
            name.maxVisibleLines = 1;
            name.fontStyle = FontStyles.Bold;
            name.color = new Color(1f, 0.97f, 0.9f, 1f);

            var medallion = new GameObject("ThumbFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask)).GetComponent<Image>();
            medallion.transform.SetParent(card.transform, false);
            medallion.sprite = LibraryMedallion();
            medallion.color = Color.white;
            medallion.raycastTarget = false;
            float frame = thumb + 12f;
            float frameY = Mathf.Max(1f, (cardH - frame) * 0.5f);
            PlaceTopLeft(medallion.rectTransform, thumbX - 6f, frameY, frame, frame);
            Mask mask = medallion.GetComponent<Mask>();
            mask.showMaskGraphic = true;
            var preview = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(medallion.transform, false);
            PlaceTopLeft(preview.rectTransform, 10f, 10f, thumb - 8f, thumb - 8f);
            ApplyDistrictThumbnail(preview, slot.MapId);
            preview.color = Color.white;
            preview.raycastTarget = false;

            float ring = wideFrame ? 64f : 56f;
            float ringY = wideFrame ? 46f : 48f;
            if (ringY + ring > barY - 2f)
            {
                ring = Mathf.Clamp(barY - nameH - 12f, 36f, ring);
                ringY = Mathf.Max(nameH + 2f, barY - ring - 4f);
            }

            float ringX = fa ? textX : textX + textW - ring;
            var track = new GameObject("ProgressRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            track.transform.SetParent(card.transform, false);
            track.sprite = ZoneRingSprite();
            track.color = new Color(slot.Zone.r, slot.Zone.g, slot.Zone.b, 0.9f);
            track.raycastTarget = false;
            PlaceTopLeft(track.rectTransform, ringX, ringY, ring, ring);
            var fill = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(track.transform, false);
            Stretch(fill.rectTransform);
            fill.sprite = ZoneRingSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            int total = Mathf.Max(0, slot.Playable);
            bool pending = total <= 0;
            float ratio = pending ? 0f : Mathf.Clamp01(slot.Won / (float)total);
            fill.fillAmount = ratio;
            fill.color = Color.Lerp(slot.Zone, Color.white, 0.35f);
            fill.raycastTarget = false;
            fill.enabled = !pending;
            track.enabled = !pending;
            float barX = fa ? textX + ring + 8f : textX;
            float barW = Mathf.Max(36f, textW - ring - 8f);
            var bar = new GameObject("ProgressBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            bar.transform.SetParent(card.transform, false);
            bar.sprite = ZoneSolidSprite();
            bar.color = new Color(0.04f, 0.05f, 0.06f, 0.9f);
            bar.raycastTarget = false;
            PlaceTopLeft(bar.rectTransform, barX, barY, barW, barH);
            var barFill = new GameObject("BarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            barFill.transform.SetParent(bar.transform, false);
            Stretch(barFill.rectTransform);
            barFill.sprite = ZoneSolidSprite();
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = fa ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = ratio;
            barFill.color = slot.Zone;
            barFill.raycastTarget = false;
            barFill.enabled = !pending;
            bar.enabled = !pending;
            if (pending)
                PlaceTopLeft(track.rectTransform, textX, 52f, textW, 42f);
            var count = CreateLabel(track.transform, "ZoneCount", pending ? 18f : 22f);
            Stretch(count.rectTransform);
            count.alignment = pending
                ? (fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft)
                : TextAlignmentOptions.Center;
            count.isRightToLeftText = pending && fa;
            count.textWrappingMode = TextWrappingModes.NoWrap;
            count.overflowMode = TextOverflowModes.Overflow;
            count.fontStyle = FontStyles.Bold;
            count.color = Color.white;
            SetCopy(count, pending ? SkirmishBattleLibraryArt.ComingSoon(fa) : slot.Won + "/" + total, pending ? 16f : 18f, pending ? 22f : 26f);
            count.alignment = pending
                ? (fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft)
                : TextAlignmentOptions.Center;
            count.isRightToLeftText = pending && fa;
            count.overflowMode = TextOverflowModes.Overflow;
            count.fontStyle = FontStyles.Bold;
            count.color = Color.white;
        }

        private static void ApplyDistrictThumbnail(RawImage preview, string mapId)
        {
            if (preview == null)
                return;
            preview.color = Color.white;
            preview.uvRect = new Rect(0f, 0f, 1f, 1f);
            Texture2D plate = SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.ResolvePlateKey(mapId));
            if (plate == null)
                plate = SkirmishLibraryPreviewAssets.Load("skirmish.plate." + mapId);
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

        private static void SealTheatreMapEdge(RectTransform map, RectTransform briefing, bool theater)
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

        private void FitZoomCue()
        {
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
                return;
            }
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            cue.alignment = TextAlignmentOptions.Center;
            cue.isRightToLeftText = fa;
            cue.textWrappingMode = TextWrappingModes.NoWrap;
            cue.overflowMode = TextOverflowModes.Overflow;
            cue.enableAutoSizing = true;
            cue.fontSizeMin = 18f;
            cue.fontSizeMax = 24f;
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

            Vector2[] selectedDistrict = DistrictHex(selectedId, frameW, frameH);
            Rect detail = SeatingDetailRect(frameW, frameH, fa);
            bool cueSeated = SeatCueNearPin(parent, pin, selectedDistrict, frameW, frameH, inset, detail, ref cueRect);
            if (!cueSeated)
            {
                float naturalWidth = cueRect.width;
                for (float candidateWidth = naturalWidth - 20f; candidateWidth >= 340f; candidateWidth -= 20f)
                {
                    cueRect.width = candidateWidth;
                    if (!SeatCueNearPin(parent, pin, selectedDistrict, frameW, frameH, inset, detail, ref cueRect))
                        continue;
                    cueSeated = true;
                    break;
                }
            }

            if (!cueSeated)
            {
                NudgeCardsForCue(parent, pin, frameW, frameH, cueRect, detail);
                SeatCueNearPin(parent, pin, selectedDistrict, frameW, frameH, inset, detail, ref cueRect);
            }

            PlaceTopLeft(rect, cueRect.x, cueRect.y, cueRect.width, cueRect.height);

            Transform plate = parent.Find("CuePlate");
            if (plate is RectTransform plateRect)
                PlaceTopLeft(plateRect, cueRect.x, cueRect.y, cueRect.width, cueRect.height);
            RaiseTheatreReadability(parent);
        }

        private static void RaiseTheatreReadability(Transform regions)
        {
            if (regions == null)
                return;
            TMP_Text[] texts = regions.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.gameObject.activeInHierarchy)
                    continue;
                if (text.name == "ZoneName")
                    RaiseTheatreLine(text, 18f, 26f);
                else if (text.name == "ZoneCount")
                {
                    bool pending = SkirmishBattleLibraryArt.IsRenderedComingSoon(text.text);
                    RaiseTheatreLine(text, pending ? 16f : 18f, pending ? 22f : 26f);
                }
                else if (text.name == "ZoomCue")
                    RaiseTheatreLine(text, 18f, 24f);
            }
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
            float width = text.rectTransform != null ? text.rectTransform.rect.width : 0f;
            if (width < 8f)
                return;
            for (int step = 0; step < 10 && text.preferredWidth > width + 1.5f && text.fontSizeMax > text.fontSizeMin + 0.5f; step++)
            {
                text.fontSizeMax = Mathf.Max(text.fontSizeMin, text.fontSizeMax - 1f);
                text.ForceMeshUpdate();
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
            Vector2[] district,
            float frameW,
            float frameH,
            float inset,
            Rect detail,
            ref Rect cue)
        {
            if (parent == null || pin == null)
                return false;
            const float margin = 12f;
            const float near = 160f;
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
            if (TryCueNear(near, true, pinX, pinY, pinLeft, pinTop, pinW, pinH, district, frameW, frameH, inset, margin, detail, cue, cards, pins, ref cue))
                return true;
            return TryCueNear(near, false, pinX, pinY, pinLeft, pinTop, pinW, pinH, district, frameW, frameH, inset, margin, detail, cue, cards, pins, ref cue);
        }

        private static bool TryCueNear(
            float near,
            bool requireNear,
            float pinX,
            float pinY,
            float pinLeft,
            float pinTop,
            float pinW,
            float pinH,
            Vector2[] district,
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
                            float reach = Vector2.Distance(centre, new Vector2(pinX, pinY));
                            bool inside = district != null && PointInsidePolygon(centre, district);
                            if (requireNear && reach > near)
                                continue;
                            if (!requireNear && !inside)
                                continue;
                            if (TryAcceptCue(spot, frameW, frameH, inset, margin, detail, cards, pins, ref placed))
                                return true;
                        }
                    }
                }
            }

            return false;
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
            float pinX = pin.anchoredPosition.x + (pin.sizeDelta.x > 2f ? pin.sizeDelta.x : ZonePinSize) * 0.5f;
            var probe = new Rect(pinX - cue.width * 0.5f, pinTop + pinH + 12f, cue.width, cue.height);
            if (frameW > probe.width + 24f)
                probe.x = Mathf.Clamp(probe.x, 12f, frameW - 12f - probe.width);
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || child is not RectTransform card)
                    continue;
                var cardRect = new Rect(card.anchoredPosition.x, -card.anchoredPosition.y, card.sizeDelta.x, card.sizeDelta.y);
                if (CueClears(cardRect, probe, 12f))
                    continue;
                string mapId = child.name.Substring("ZoneCard_".Length);
                for (int step = 1; step <= 8; step++)
                {
                    var next = new Rect(cardRect.x, cardRect.y + step * 6f, cardRect.width, cardRect.height);
                    if (next.yMax > frameH - TheatreSafeBottom)
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
                    break;
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
            Vector2[] own = DistrictHex(mapId, frameWidth, frameHeight);
            if (CardInsidePolygon(card, own) && CardBoundaryGap(own, card) + 0.01f >= 8f)
                return true;
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            for (int i = 0; i < mapIds.Length; i++)
            {
                if (RectHitsPolygon(card, DistrictHex(mapIds[i], frameWidth, frameHeight)))
                    return false;
            }

            return true;
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
                case "CC": return new Color(0.25f, 0.78f, 0.82f, 1f);
                case "MP": return new Color(0.42f, 0.78f, 0.38f, 1f);
                case "IB": return new Color(0.90f, 0.48f, 0.18f, 1f);
                case "AP": return new Color(0.32f, 0.58f, 0.92f, 1f);
                default: return new Color(0.93f, 0.68f, 0.22f, 1f);
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
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.68f) / 0.28f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(ring * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneRing = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _zoneRing.hideFlags = HideFlags.HideAndDontSave;
            return _zoneRing;
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
