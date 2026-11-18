using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Base-under-fire cue (owner designer playtest 2026-11-17, S002; retest 2026-11-18). Additive
    /// and non-blocking: a compact alert row under the base strip, a direction arrow toward the
    /// attacker when the player can see it (otherwise toward the base), a pulsing outline on the
    /// nearest own squad card and a brief impact/shot cue on each hit. The alert tap only frames the
    /// base (camera); the defender tap shows the highlighted card (pages the tray and flashes the
    /// outline). Neither selects, deselects nor orders troops.
    /// </summary>
    public sealed partial class SkirmishMatchView : ISkirmishBaseAlertPresentation
    {
        private SkirmishBaseFireTracker baseFire;
        private RectTransform baseFireRow, baseFireArrow;
        private Button baseFireFocus, baseFireDefender;
        private TMP_Text baseFireFocusHeading, baseFireFocusDetail, baseFireDefenderHeading, baseFireDefenderDetail;
        private LayoutElement baseFireFocusLayout, baseFireDefenderLayout;
        private HudDirectionArrowGraphic baseFireArrowGraphic;
        private MatchHudSquadTrayView baseFireTray;
        private string baseFireDefenderShown, baseFireFocusShown;
        private int baseFireMeasureFrames;
        private float baseFireRowHeld = float.NaN, baseFireStripHeld = float.NaN;
        private uint baseFireDefenderGroup;
        private static readonly Color BaseFireRed = new(.91f, .23f, .12f, 1f);
        private static readonly Color BaseFireAmber = new(1f, .65f, .1f, 1f);
        private RectTransform baseFirePing;
        private V3GradientGraphic baseFirePingGraphic;
        private MatchHudMinimapView baseFireMinimap;

        // One readable hit: impact ring at the base hit point, shot line from a visible attacker.
        private SkirmishBaseHitCue baseHitCue;
        private int baseHitCueEvent;
        private Vector3 baseHitCueWorld, baseHitCueSource;
        private bool baseHitCueHasSource;
        private RectTransform baseHitRing, baseHitShot;
        private V3GradientGraphic baseHitRingGraphic;
        private Image baseHitShotImage;

        // ISkirmishBaseAlertPresentation: exactly what the player is shown (ARIA reads only this).
        private int baseAlertSequence, baseAlertSeenRevision = -1;
        private byte baseAlertReason;
        private bool baseAlertAttackerShown;
        private Vector3 baseAlertAttackerWorld;
        public bool BaseAlertPresented => BaseUnderFirePresented;
        public byte BaseAlertReason => BaseUnderFirePresented ? baseAlertReason : (byte)0;
        public int BaseAlertSequence => baseAlertSequence;
        public bool BaseAlertAttackerShown => BaseUnderFirePresented && baseAlertAttackerShown;
        public Vector3 BaseAlertAttackerWorld => baseAlertAttackerWorld;
        public bool BaseAlertDefenderKnown => BaseUnderFirePresented && baseFireDefender != null && baseFireDefender.gameObject.activeSelf && baseFireDefenderGroup != 0;
        public int BaseAlertDefenderSlot => BaseAlertDefenderKnown && baseFireTray != null ? baseFireTray.VisibleSlotOf(baseFireDefenderGroup) : -1;
        public Button BaseAlertDefenderButton => BaseAlertDefenderKnown ? baseFireDefender : null;

        public bool BaseUnderFirePresented => baseFireRow != null && baseFireRow.gameObject.activeSelf;
        public Button BaseUnderFireFocusButton => baseFireFocus;
        public Button BaseUnderFireDefenderButton => baseFireDefender;

        private void RefreshBaseUnderFire(in UiSkirmishModel model)
        {
            bool live = hud != null && hud.activeSelf && !model.Finished && !model.StartupFailed && model.Expanded;
            if (!live)
            {
                if (model.Finished || model.StartupFailed) { baseFire.Reset(); baseHitCue.Reset(); baseHitCueEvent = 0; }
                SetBaseFireVisible(false);
                HideBaseHitCue();
                RefreshDefenseHeld(model, false);
                return;
            }
            PresentBaseHitCue(model);
            float now = Time.time; // scaled: pausing freezes the quiet timer as it freezes damage.
            bool firstHit = baseFire.Observe(model.PlayerHealth, now);
            bool active = baseFire.UnderFire || model.BaseIntruderInside || model.PlayerBaseRecentlyHit && model.PlayerHealth > 0;
            if (!active || model.PlayerHealth <= 0)
            {
                SetBaseFireVisible(false);
                RefreshDefenseHeld(model, true);
                return;
            }
            if (baseFireRow == null) BuildBaseUnderFire();
            bool onset = !baseFireRow.gameObject.activeSelf;
            // A new alert revision (perimeter entry, then first hit) is a new warning onset even
            // while the row is already up; so is a first hit seen by the HUD's own health tracker.
            bool newRevision = baseAlertSeenRevision >= 0 && model.BaseThreatRevision != baseAlertSeenRevision;
            if (onset || newRevision || firstHit && model.BaseThreatRevision == 0)
            {
                baseAlertSequence++;
                baseAlertReason = model.BaseThreatReason != 0 && (onset || newRevision) ? model.BaseThreatReason : (byte)2;
            }
            baseAlertSeenRevision = model.BaseThreatRevision;
            baseAlertAttackerShown = model.BaseAttackerVisible;
            baseAlertAttackerWorld = model.BaseAttackerVisible ? model.BaseAttackerWorld : Vector3.zero;
            SetBaseFireVisible(true);
            RefreshDefenseHeld(model, true);
            PositionBaseFireRow();

            var text = UiShellRuntimeGateway.Localization;
            bool hit = baseFire.UnderFire || model.PlayerBaseRecentlyHit;
            string focusSignature = (model.BaseAttackerVisible ? "seen" : "base") + hit + text.CurrentLocaleCode;
            if (baseFireFocusShown != focusSignature)
            {
                baseFireFocusShown = focusSignature;
                string heading = hit ? text.Get("ui.skirmish.base_alert.under_fire", "BASE UNDER FIRE")
                    : text.Get("ui.skirmish.base_alert.enemy_at_base", "ENEMY AT BASE");
                HudIconButtonView.Apply(baseFireFocus, HudButtonIcon.Alert, HudButtonRole.Alert, heading, heading, baseFireFocusHeading, true);
                UiLocalizedText.Set(baseFireFocusDetail, model.BaseAttackerVisible
                    ? text.Get("ui.skirmish.base_alert.attacker_in_sight", "ATTACKER IN SIGHT")
                    : text.Get("ui.skirmish.base_alert.source_unknown", "SOURCE UNKNOWN"));
                baseFireMeasureFrames = 3;
            }

            baseFireDefenderGroup = model.BaseDefenderGroupId;
            bool haveDefender = baseFireDefenderGroup != 0 && !string.IsNullOrEmpty(model.BaseDefenderLabel);
            if (baseFireDefender.gameObject.activeSelf != haveDefender) { baseFireDefender.gameObject.SetActive(haveDefender); baseFireMeasureFrames = 3; }
            string defenderSignature = model.BaseDefenderLabel + text.CurrentLocaleCode + model.BaseDefenderIsHomeGuard;
            if (haveDefender && baseFireDefenderShown != defenderSignature)
            {
                baseFireDefenderShown = defenderSignature;
                // The tap shows the card (pages the tray to it and flashes its outline); label it so.
                string heading = model.BaseDefenderIsHomeGuard
                    ? text.Get("ui.skirmish.base_alert.show_home_guard", "SHOW HOME GUARD")
                    : text.Get("ui.skirmish.base_alert.show_nearest", "SHOW NEAREST SQUAD");
                HudIconButtonView.Apply(baseFireDefender, HudButtonIcon.Team, HudButtonRole.Navigation, heading, heading, baseFireDefenderHeading, true);
                UiLocalizedText.Set(baseFireDefenderDetail, model.BaseDefenderLabel);
                baseFireMeasureFrames = 3;
            }
            AlignBaseFireDetail(baseFireFocusDetail);
            AlignBaseFireDetail(baseFireDefenderDetail);
            if (baseFireMeasureFrames > 0) { baseFireMeasureFrames--; FitBaseFireRow(haveDefender); }
            if (baseFireTray == null) baseFireTray = FindAnyObjectByType<MatchHudSquadTrayView>();
            if (baseFireTray != null) baseFireTray.SetDefenderHighlight(haveDefender ? baseFireDefenderGroup : 0);

            PresentBaseFireArrow(model);
            PresentBaseFirePing(model);
        }

        // Size each control to its widest line so neither clips its own text (EN and FA, any width).
        private void FitBaseFireRow(bool haveDefender)
        {
            float focus = Mathf.Max(Preferred(baseFireFocusHeading), Preferred(baseFireFocusDetail));
            float defender = haveDefender ? Mathf.Max(Preferred(baseFireDefenderHeading), Preferred(baseFireDefenderDetail)) : 0f;
            float canvasWidth = ((RectTransform)transform).rect.width;
            var widths = SkirmishBaseAlertRowLayout.Fit(focus, defender, haveDefender, canvasWidth, out _);
            baseFireFocusLayout.preferredWidth = widths.x;
            baseFireDefenderLayout.preferredWidth = widths.y;
            baseFireRow.sizeDelta = new Vector2(SkirmishBaseAlertRowLayout.RowWidth(widths, haveDefender), SkirmishBaseAlertRowLayout.RowHeight);
        }

        private static float Preferred(TMP_Text label) =>
            label == null || string.IsNullOrEmpty(label.text) ? 0f : label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity).x;

        private static void AlignBaseFireDetail(TMP_Text detail)
        {
            if (detail == null) return;
            AlignBaseHealth(detail);
            detail.textWrappingMode = TextWrappingModes.NoWrap;
            detail.overflowMode = TextOverflowModes.Overflow;
            detail.color = new Color(.90f, .93f, .92f);
        }

        // Minimap ping: a pulsing outline on the small map at the base while it is threatened.
        private void PresentBaseFirePing(in UiSkirmishModel model)
        {
            if (!model.PlayerBaseWorldKnown) { if (baseFirePing != null) baseFirePing.gameObject.SetActive(false); return; }
            if (baseFireMinimap == null || !baseFireMinimap.isActiveAndEnabled)
            {
                baseFireMinimap = null;
                foreach (var map in FindObjectsByType<MatchHudMinimapView>(FindObjectsSortMode.None))
                    if (map.isActiveAndEnabled && (fullMap == null || !map.transform.IsChildOf(fullMap.transform))) { baseFireMinimap = map; break; }
            }
            if (baseFireMinimap == null || !baseFireMinimap.TryGetPresentedMapPoint(model.PlayerBaseWorld, null, out var point))
            { if (baseFirePing != null) baseFirePing.gameObject.SetActive(false); return; }
            if (baseFirePing == null)
            {
                baseFirePing = Panel("BaseUnderFireMinimapPing", transform, false);
                baseFirePing.pivot = new Vector2(.5f, .5f);
                baseFirePingGraphic = baseFirePing.gameObject.AddComponent<V3GradientGraphic>();
                baseFirePingGraphic.Configure(Color.clear, Color.clear, BaseFireRed, 3f);
                baseFirePingGraphic.raycastTarget = false;
            }
            float phase = Mathf.Repeat(Time.unscaledTime, 1.2f) / 1.2f;
            baseFirePing.gameObject.SetActive(true);
            baseFirePing.position = point;
            baseFirePing.sizeDelta = Vector2.one * Mathf.Lerp(18f, 54f, phase);
            baseFirePingGraphic.color = new Color(1f, 1f, 1f, 1f - phase);
        }

        // Impact ring at the hit point and, while the attacker is visible, a shot line from it. Reuses
        // the HUD's own outline graphic and alert red; nothing raycasts, so taps reach the ground.
        private void PresentBaseHitCue(in UiSkirmishModel model)
        {
            if (model.BaseHitEventId > 0 && model.BaseHitEventId != baseHitCueEvent)
            {
                baseHitCueEvent = model.BaseHitEventId;
                baseHitCueWorld = model.BaseHitWorld;
                baseHitCueHasSource = model.BaseAttackerVisible; // fog: only a source the player can see
                baseHitCueSource = model.BaseAttackerVisible ? model.BaseAttackerWorld : Vector3.zero;
            }
            float t = baseHitCue.Observe(model.BaseHitEventId, Time.unscaledTime);
            var camera = Camera.main;
            if (t < 0f || camera == null || !TryCanvasPoint(camera, baseHitCueWorld, out var hitPoint)) { HideBaseHitCue(); return; }
            if (baseHitRing == null)
            {
                baseHitRing = Panel("BaseHitImpact", transform, false);
                baseHitRing.SetAsFirstSibling(); // under every HUD control
                baseHitRing.anchorMin = baseHitRing.anchorMax = baseHitRing.pivot = new Vector2(.5f, .5f);
                baseHitRingGraphic = baseHitRing.gameObject.AddComponent<V3GradientGraphic>();
                baseHitRingGraphic.Configure(Color.clear, Color.clear, BaseFireRed, 4f);
                baseHitRingGraphic.raycastTarget = false;
                baseHitShot = Panel("BaseHitShot", transform, false);
                baseHitShot.SetAsFirstSibling();
                baseHitShot.anchorMin = baseHitShot.anchorMax = baseHitShot.pivot = new Vector2(.5f, .5f);
                baseHitShotImage = baseHitShot.gameObject.AddComponent<Image>();
                baseHitShotImage.raycastTarget = false;
            }
            baseHitRing.gameObject.SetActive(true);
            baseHitRing.localPosition = hitPoint;
            baseHitRing.sizeDelta = Vector2.one * Mathf.Lerp(28f, 110f, t);
            baseHitRingGraphic.color = new Color(1f, 1f, 1f, 1f - t);
            Vector2 sourcePoint = default;
            bool shot = baseHitCueHasSource && t < .6f && TryCanvasPoint(camera, baseHitCueSource, out sourcePoint) &&
                        (sourcePoint - hitPoint).sqrMagnitude > 16f;
            baseHitShot.gameObject.SetActive(shot);
            if (!shot) return;
            Vector2 delta = hitPoint - sourcePoint;
            baseHitShot.localPosition = (hitPoint + sourcePoint) * .5f;
            baseHitShot.sizeDelta = new Vector2(delta.magnitude, 5f);
            baseHitShot.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            baseHitShotImage.color = new Color(BaseFireRed.r, BaseFireRed.g, BaseFireRed.b, .9f * (1f - t / .6f));
        }

        private bool TryCanvasPoint(Camera camera, Vector3 world, out Vector2 local)
        {
            local = default;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f || !float.IsFinite(screen.x) || !float.IsFinite(screen.y) || !Screen.safeArea.Contains(screen)) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, null, out local);
        }

        private void HideBaseHitCue()
        {
            if (baseHitRing != null && baseHitRing.gameObject.activeSelf) baseHitRing.gameObject.SetActive(false);
            if (baseHitShot != null && baseHitShot.gameObject.activeSelf) baseHitShot.gameObject.SetActive(false);
        }

        private void SetBaseFireVisible(bool visible)
        {
            if (baseFireRow != null && baseFireRow.gameObject.activeSelf != visible) baseFireRow.gameObject.SetActive(visible);
            if (!visible) baseFireRowHeld = float.NaN;
            if (baseFireArrow != null && !visible && baseFireArrow.gameObject.activeSelf) baseFireArrow.gameObject.SetActive(false);
            if (baseFirePing != null && !visible && baseFirePing.gameObject.activeSelf) baseFirePing.gameObject.SetActive(false);
            if (!visible && baseFireTray != null) baseFireTray.SetDefenderHighlight(0);
        }

        private void BuildBaseUnderFire()
        {
            baseFireRow = Panel("BaseUnderFire", transform, false);
            baseFireRow.anchorMin = baseFireRow.anchorMax = new Vector2(.5f, 1f);
            baseFireRow.pivot = new Vector2(.5f, 1f);
            baseFireRow.sizeDelta = new Vector2(820, SkirmishBaseAlertRowLayout.RowHeight);
            var row = baseFireRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = SkirmishBaseAlertRowLayout.Spacing; row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = false;
            // Two lines per control, as in the base strip: a status heading over a detail line.
            baseFireFocusHeading = Button(baseFireRow, "", () => Send(UiSkirmishAction.FocusPlayer));
            baseFireFocus = baseFireFocusHeading.GetComponentInParent<Button>();
            baseFireFocusLayout = baseFireFocusHeading.transform.parent.GetComponent<LayoutElement>();
            baseFireFocusLayout.preferredHeight = SkirmishBaseAlertRowLayout.RowHeight;
            baseFireFocusDetail = HealthLabel(baseFireFocusHeading);
            baseFireDefenderHeading = Button(baseFireRow, "", RevealBaseDefender);
            baseFireDefender = baseFireDefenderHeading.GetComponentInParent<Button>();
            baseFireDefenderLayout = baseFireDefenderHeading.transform.parent.GetComponent<LayoutElement>();
            baseFireDefenderLayout.preferredHeight = SkirmishBaseAlertRowLayout.RowHeight;
            baseFireDefenderDetail = HealthLabel(baseFireDefenderHeading);

            baseFireArrow = Panel("BaseUnderFireDirection", transform, false);
            baseFireArrow.sizeDelta = new Vector2(64, 52);
            baseFireArrow.pivot = new Vector2(.5f, .5f);
            baseFireArrowGraphic = baseFireArrow.gameObject.AddComponent<HudDirectionArrowGraphic>();
            baseFireArrowGraphic.color = BaseFireRed;
        }

        // Player-requested only. Pages the tray to the card and flashes its outline; never selects
        // or deselects a squad and never touches an order.
        private void RevealBaseDefender()
        {
            if (baseFireTray == null) baseFireTray = FindAnyObjectByType<MatchHudSquadTrayView>();
            if (baseFireTray != null) baseFireTray.TryRevealGroup(baseFireDefenderGroup);
        }

        /// <summary>Distance below the canvas top for rows under the base strip.</summary>
        private float GuidanceRowDown()
        {
            var strip = (RectTransform)hud.transform;
            // The strip is top-anchored; its bottom edge sits at offsetMin.y below the top.
            return -(strip.offsetMin.y - 10 - GuidanceStackOffset);
        }

        private void PositionBaseFireRow()
        {
            // Held for the alert's lifetime: the row may move down if something appears above it,
            // never back up when the top banner expires.
            float down = SkirmishBaseAlertRowLayout.HoldDown(GuidanceRowDown(), ref baseFireRowHeld, true);
            baseFireRow.anchoredPosition = new Vector2(0, -down);
        }

        private void PresentBaseFireArrow(in UiSkirmishModel model)
        {
            var camera = Camera.main;
            bool targetKnown = model.BaseAttackerVisible || model.PlayerBaseWorldKnown;
            if (camera == null || !targetKnown) { baseFireArrow.gameObject.SetActive(false); return; }
            Vector3 world = model.BaseAttackerVisible ? model.BaseAttackerWorld : model.PlayerBaseWorld;
            Vector3 screen = camera.WorldToScreenPoint(world);
            Rect area = Screen.safeArea;
            Vector2 centre = area.center;
            float degrees = SkirmishBaseThreatPolicy.ArrowDegrees(screen, centre);
            Vector2 direction = new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
            Vector2 position;
            if (SkirmishBaseThreatPolicy.OnScreen(screen, area))
                position = (Vector2)screen - direction * 70f; // Sit just short of the target, pointing at it.
            else
            {
                // Clamp to an inset ellipse so the arrow rides the screen edge toward the target.
                float rx = area.width * .5f - 70f, ry = area.height * .5f - 120f;
                float scale = 1f / Mathf.Sqrt(direction.x * direction.x / (rx * rx) + direction.y * direction.y / (ry * ry));
                position = centre + direction * scale;
            }
            baseFireArrow.gameObject.SetActive(true);
            baseFireArrow.position = position;
            baseFireArrow.localRotation = Quaternion.Euler(0, 0, degrees);
            baseFireArrowGraphic.color = model.BaseAttackerVisible ? BaseFireRed : BaseFireAmber;
        }
    }
}
