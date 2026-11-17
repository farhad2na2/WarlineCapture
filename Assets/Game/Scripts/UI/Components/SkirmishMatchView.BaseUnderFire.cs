using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Base-under-fire cue (owner designer playtest 2026-11-17, S002). Additive and
    /// non-blocking: a compact alert row under the base strip, a direction arrow toward
    /// the attacker when the player can see it (otherwise toward the base), and a pulsing
    /// outline on the nearest own squad card. The alert tap only frames the base
    /// (camera); the defender tap only pages the tray. Neither selects nor orders troops.
    /// </summary>
    public sealed partial class SkirmishMatchView : ISkirmishBaseAlertPresentation
    {
        private SkirmishBaseFireTracker baseFire;
        private RectTransform baseFireRow, baseFireArrow;
        private Button baseFireFocus, baseFireDefender;
        private HudDirectionArrowGraphic baseFireArrowGraphic;
        private MatchHudSquadTrayView baseFireTray;
        private string baseFireDefenderShown, baseFireFocusShown;
        private uint baseFireDefenderGroup;
        private static readonly Color BaseFireRed = new(.91f, .23f, .12f, 1f);
        private static readonly Color BaseFireAmber = new(1f, .65f, .1f, 1f);
        private RectTransform baseFirePing;
        private V3GradientGraphic baseFirePingGraphic;
        private MatchHudMinimapView baseFireMinimap;

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
                if (model.Finished || model.StartupFailed) baseFire.Reset();
                SetBaseFireVisible(false);
                return;
            }
            float now = Time.time; // scaled: pausing freezes the quiet timer as it freezes damage.
            bool firstHit = baseFire.Observe(model.PlayerHealth, now);
            bool active = baseFire.UnderFire || model.BaseIntruderInside || model.PlayerBaseRecentlyHit && model.PlayerHealth > 0;
            if (!active || model.PlayerHealth <= 0)
            {
                SetBaseFireVisible(false);
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
            PositionBaseFireRow();

            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            bool hit = baseFire.UnderFire || model.PlayerBaseRecentlyHit;
            string focusSignature = (model.BaseAttackerVisible ? "seen" : "base") + hit + fa;
            if (baseFireFocusShown != focusSignature)
            {
                baseFireFocusShown = focusSignature;
                string en = (hit ? "BASE UNDER FIRE · " : "ENEMY AT BASE · ") + (model.BaseAttackerVisible ? "ATTACKER IN SIGHT" : "SOURCE UNKNOWN");
                string persian = (hit ? "پایگاه زیر آتش · " : "دشمن در پایگاه · ") + (model.BaseAttackerVisible ? "مهاجم در دید" : "منبع نامعلوم");
                HudIconButtonView.Apply(baseFireFocus, HudButtonIcon.Alert, HudButtonRole.Alert, en, persian);
            }

            baseFireDefenderGroup = model.BaseDefenderGroupId;
            bool haveDefender = baseFireDefenderGroup != 0 && !string.IsNullOrEmpty(model.BaseDefenderLabel);
            baseFireDefender.gameObject.SetActive(haveDefender);
            if (haveDefender && baseFireDefenderShown != model.BaseDefenderLabel + fa + model.BaseDefenderIsHomeGuard)
            {
                baseFireDefenderShown = model.BaseDefenderLabel + fa + model.BaseDefenderIsHomeGuard;
                HudIconButtonView.Apply(baseFireDefender, HudButtonIcon.Team, HudButtonRole.Navigation,
                    (model.BaseDefenderIsHomeGuard ? "HOME GUARD · " : "NEAREST · ") + model.BaseDefenderLabel,
                    (model.BaseDefenderIsHomeGuard ? "نگهبان پایگاه · " : "نزدیک‌ترین · ") + model.BaseDefenderLabel);
            }
            if (baseFireTray == null) baseFireTray = FindAnyObjectByType<MatchHudSquadTrayView>();
            if (baseFireTray != null) baseFireTray.SetDefenderHighlight(haveDefender ? baseFireDefenderGroup : 0);

            PresentBaseFireArrow(model);
            PresentBaseFirePing(model);
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

        private void SetBaseFireVisible(bool visible)
        {
            if (baseFireRow != null && baseFireRow.gameObject.activeSelf != visible) baseFireRow.gameObject.SetActive(visible);
            if (baseFireArrow != null && !visible && baseFireArrow.gameObject.activeSelf) baseFireArrow.gameObject.SetActive(false);
            if (baseFirePing != null && !visible && baseFirePing.gameObject.activeSelf) baseFirePing.gameObject.SetActive(false);
            if (!visible && baseFireTray != null) baseFireTray.SetDefenderHighlight(0);
        }

        private void BuildBaseUnderFire()
        {
            baseFireRow = Panel("BaseUnderFire", transform, false);
            baseFireRow.anchorMin = baseFireRow.anchorMax = new Vector2(.5f, 1f);
            baseFireRow.pivot = new Vector2(.5f, 1f);
            baseFireRow.sizeDelta = new Vector2(820, 62);
            var row = baseFireRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8; row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = false;
            var focusLabel = Button(baseFireRow, "", () => Send(UiSkirmishAction.FocusPlayer));
            baseFireFocus = focusLabel.GetComponentInParent<Button>();
            focusLabel.transform.parent.GetComponent<LayoutElement>().preferredWidth = 470;
            var defenderLabel = Button(baseFireRow, "", RevealBaseDefender);
            baseFireDefender = defenderLabel.GetComponentInParent<Button>();
            defenderLabel.transform.parent.GetComponent<LayoutElement>().preferredWidth = 330;

            baseFireArrow = Panel("BaseUnderFireDirection", transform, false);
            baseFireArrow.sizeDelta = new Vector2(64, 52);
            baseFireArrow.pivot = new Vector2(.5f, .5f);
            baseFireArrowGraphic = baseFireArrow.gameObject.AddComponent<HudDirectionArrowGraphic>();
            baseFireArrowGraphic.color = BaseFireRed;
        }

        // Player-requested only. Pages the tray to the card; never selects the squad.
        private void RevealBaseDefender()
        {
            if (baseFireTray == null) baseFireTray = FindAnyObjectByType<MatchHudSquadTrayView>();
            if (baseFireTray != null) baseFireTray.TryRevealGroup(baseFireDefenderGroup);
        }

        private void PositionBaseFireRow()
        {
            var strip = (RectTransform)hud.transform;
            // The strip is top-anchored; its bottom edge sits at offsetMin.y below the top.
            baseFireRow.anchoredPosition = new Vector2(0, strip.offsetMin.y - 10 - GuidanceStackOffset);
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
