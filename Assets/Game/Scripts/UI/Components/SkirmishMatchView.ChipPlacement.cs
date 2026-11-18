using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Places world-anchored mission chips clear of the HUD (S026 fa-IR/EN capture,
    /// 2026-11-18: the RENDEZVOUS chip sat on the settings and pause buttons). The HUD is
    /// read from the live layout: every active raycast-target graphic on a screen-space
    /// canvas that is docked to a screen edge (top bar, resources, objective strip, side
    /// panels, minimap, card tray, command bar). A chip whose anchor is under the HUD is
    /// clamped into the open play area and shows a direction arrow toward its anchor.
    /// Presentation only; nothing here raycasts, selects or orders.
    /// </summary>
    public sealed partial class SkirmishMatchView
    {
        private const float ChipCueMargin = 44f; // Canvas units; the cue arrow fits inside it.
        private const int HudOccluderRefreshFrames = 6, HudCanvasRescanFrames = 60;
        private readonly List<Rect> hudOccluders = new();
        private readonly List<Canvas> hudCanvases = new();
        private readonly Vector3[] occluderCorners = new Vector3[4];
        private int hudOccluderFrame = -1000, hudCanvasFrame = -1000;
        private bool safePlayAreaKnown;
        private Rect safePlayArea;
        private Vector2 safePlayAreaScreen;

        /// <summary>The open play area chips are kept inside (screen pixels), for evidence.</summary>
        public bool TryReadSafePlayArea(out Rect area) { bool known = RefreshSafePlayArea(); area = safePlayArea; return known; }

        private bool RefreshSafePlayArea()
        {
            var screenSize = new Vector2(Screen.width, Screen.height);
            if (Time.frameCount - hudOccluderFrame < HudOccluderRefreshFrames && screenSize == safePlayAreaScreen)
                return safePlayAreaKnown;
            hudOccluderFrame = Time.frameCount;
            safePlayAreaScreen = screenSize;
            if (Time.frameCount - hudCanvasFrame >= HudCanvasRescanFrames)
            {
                hudCanvasFrame = Time.frameCount;
                hudCanvases.Clear();
                foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (canvas.rootCanvas.renderMode != RenderMode.WorldSpace) hudCanvases.Add(canvas);
            }
            hudOccluders.Clear();
            foreach (var canvas in hudCanvases)
            {
                if (canvas == null || !canvas.isActiveAndEnabled) continue;
                var root = canvas.rootCanvas;
                Camera eventCamera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
                var graphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
                for (int i = 0; i < graphics.Count; i++)
                {
                    var graphic = graphics[i];
                    if (graphic == null || !graphic.isActiveAndEnabled || !graphic.raycastTarget) continue;
                    if (IsWorldAnchoredChip(graphic.transform)) continue;
                    graphic.rectTransform.GetWorldCorners(occluderCorners);
                    Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
                    for (int c = 0; c < 4; c++)
                    {
                        Vector2 point = RectTransformUtility.WorldToScreenPoint(eventCamera, occluderCorners[c]);
                        min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                    }
                    hudOccluders.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
                }
            }
            var screen = Screen.safeArea;
            safePlayAreaKnown = SkirmishHudChipPlacement.TrySafePlayArea(screen, hudOccluders, screen.center, out safePlayArea);
            return safePlayAreaKnown;
        }

        private bool IsWorldAnchoredChip(Transform candidate)
        {
            if (rendezvousMarker != null && candidate.IsChildOf(rendezvousMarker)) return true;
            if (baseMarker != null && candidate.IsChildOf(baseMarker)) return true;
            foreach (var label in objectiveMarkers.Values)
                if (label != null && candidate.IsChildOf(label.transform)) return true;
            return false;
        }

        /// <summary>
        /// Puts a stemmed marker's foot on <paramref name="anchor"/> when it fits in the play
        /// area; otherwise clamps it there, hides the stem and points the cue at the anchor.
        /// Returns false (marker hidden) when no play area can hold it.
        /// </summary>
        private bool PlaceWorldChip(RectTransform marker, GameObject stem, ref HudDirectionArrowGraphic cue, Color tone, Vector2 anchor)
        {
            float scale = ((RectTransform)transform).lossyScale.x;
            Vector2 size = marker.rect.size * scale;
            float margin = ChipCueMargin * scale;
            if (!RefreshSafePlayArea() ||
                !SkirmishHudChipPlacement.TryPlace(anchor, size, marker.pivot, safePlayArea, margin, out var position, out bool clamped))
            {
                marker.gameObject.SetActive(false);
                if (cue != null) cue.gameObject.SetActive(false);
                return false;
            }
            marker.gameObject.SetActive(true);
            marker.position = position;
            if (stem != null && stem.activeSelf == clamped) stem.SetActive(!clamped);
            if (!clamped) { if (cue != null) cue.gameObject.SetActive(false); return true; }
            if (cue == null)
            {
                var rect = Panel(marker.name + "Direction", transform, false);
                rect.sizeDelta = new Vector2(36, 30);
                rect.pivot = new Vector2(.5f, .5f);
                cue = rect.gameObject.AddComponent<HudDirectionArrowGraphic>();
            }
            var chip = SkirmishHudChipPlacement.ChipRect(position, size, marker.pivot);
            var point = SkirmishHudChipPlacement.CuePoint(chip, anchor, margin, out float degrees);
            cue.color = tone;
            cue.gameObject.SetActive(true);
            cue.rectTransform.position = point;
            cue.rectTransform.localRotation = Quaternion.Euler(0, 0, degrees);
            return true;
        }

        /// <summary>
        /// Labels without a stem or cue: shown only while their anchor is in the open play
        /// area (so the text never names a spot under the HUD), nudged inside it if needed.
        /// </summary>
        private bool PlaceWorldLabel(RectTransform label, Vector2 anchor)
        {
            if (!RefreshSafePlayArea() || !safePlayArea.Contains(anchor)) return false;
            float scale = ((RectTransform)transform).lossyScale.x;
            if (!SkirmishHudChipPlacement.TryPlace(anchor, label.rect.size * scale, label.pivot, safePlayArea, 0, out var position, out _))
                return false;
            label.position = position;
            return true;
        }

        private static void HideCue(HudDirectionArrowGraphic cue)
        {
            if (cue != null && cue.gameObject.activeSelf) cue.gameObject.SetActive(false);
        }
    }
}
