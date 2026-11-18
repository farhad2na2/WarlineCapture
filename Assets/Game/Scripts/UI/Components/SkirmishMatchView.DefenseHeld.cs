using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Guidance after a successful defense (designer retest 2026-11-18, S002): once a base alert
    /// clears with the main base standing, a short choice prompt in the guidance-row style offers the
    /// existing enemy-base action ("Keep a guard at your base. Scout the reported enemy base."), and
    /// the enemy-base readout pulses so it reads as tappable. The tap frames the same reported
    /// position the readout already exposes; nothing new is revealed, no squad is selected or ordered.
    /// </summary>
    public sealed partial class SkirmishMatchView
    {
        private SkirmishDefenseHeldPrompt defenseHeld;
        private RectTransform defenseHeldRow, enemyReadoutPulse;
        private Button defenseHeldButton;
        private TMP_Text defenseHeldHeading, defenseHeldDetail;
        private LayoutElement defenseHeldLayout;
        private V3GradientGraphic enemyReadoutPulseGraphic;
        private string defenseHeldShown;
        private int defenseHeldMeasureFrames;

        public bool DefenseHeldPromptPresented => defenseHeldRow != null && defenseHeldRow.gameObject.activeSelf;
        public Button DefenseHeldPromptButton => DefenseHeldPromptPresented ? defenseHeldButton : null;

        private void RefreshDefenseHeld(in UiSkirmishModel model, bool live)
        {
            if (!live)
            {
                if (model.Finished || model.StartupFailed) defenseHeld.Reset();
                SetDefenseHeldVisible(false);
                return;
            }
            // Base Assault HUD only, while the enemy base is out of sight (its reported or last-seen
            // position is what the readout frames). The fog-respecting readout decides this.
            bool eligible = objectiveModel == null && model.EnemyBaseKnowledgeKnown &&
                            model.EnemyBaseKnowledge != SkirmishEnemyBaseMarkerState.ReadoutVisible;
            bool show = defenseHeld.Observe(BaseUnderFirePresented, model.PlayerHealth > 0, eligible, Time.time);
            SetDefenseHeldVisible(show);
            if (!show) return;
            if (defenseHeldRow == null) BuildDefenseHeld();
            defenseHeldRow.anchoredPosition = new Vector2(0, -GuidanceRowDown());
            var text = UiShellRuntimeGateway.Localization;
            if (defenseHeldShown != text.CurrentLocaleCode)
            {
                defenseHeldShown = text.CurrentLocaleCode;
                string heading = text.Get("ui.skirmish.defense_held.title", "BASE HELD");
                HudIconButtonView.Apply(defenseHeldButton, HudButtonIcon.Guide, HudButtonRole.Information, heading, heading, defenseHeldHeading, true);
                UiLocalizedText.Set(defenseHeldDetail, text.Get("ui.skirmish.defense_held.choice",
                    "Keep a guard at your base. Scout the reported enemy base."));
                defenseHeldMeasureFrames = 3;
            }
            AlignBaseFireDetail(defenseHeldDetail);
            if (defenseHeldMeasureFrames > 0)
            {
                defenseHeldMeasureFrames--;
                float width = Mathf.Max(Preferred(defenseHeldHeading), Preferred(defenseHeldDetail));
                float canvasWidth = ((RectTransform)transform).rect.width;
                float button = Mathf.Min(SkirmishBaseAlertRowLayout.ButtonWidth(width), SkirmishBaseAlertRowLayout.MaxRowWidth(canvasWidth));
                defenseHeldLayout.preferredWidth = button;
                defenseHeldRow.sizeDelta = new Vector2(button, SkirmishBaseAlertRowLayout.RowHeight);
            }
            PulseEnemyReadout();
        }

        private void SetDefenseHeldVisible(bool visible)
        {
            if (defenseHeldRow != null && defenseHeldRow.gameObject.activeSelf != visible) defenseHeldRow.gameObject.SetActive(visible);
            if (enemyReadoutPulse != null && !visible && enemyReadoutPulse.gameObject.activeSelf) enemyReadoutPulse.gameObject.SetActive(false);
        }

        private void BuildDefenseHeld()
        {
            defenseHeldRow = Panel("DefenseHeldPrompt", transform, false);
            defenseHeldRow.anchorMin = defenseHeldRow.anchorMax = new Vector2(.5f, 1f);
            defenseHeldRow.pivot = new Vector2(.5f, 1f);
            defenseHeldRow.sizeDelta = new Vector2(820, SkirmishBaseAlertRowLayout.RowHeight);
            var row = defenseHeldRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = false;
            defenseHeldHeading = Button(defenseHeldRow, "", ScoutReportedEnemyBase);
            defenseHeldButton = defenseHeldHeading.GetComponentInParent<Button>();
            defenseHeldLayout = defenseHeldHeading.transform.parent.GetComponent<LayoutElement>();
            defenseHeldLayout.preferredHeight = SkirmishBaseAlertRowLayout.RowHeight;
            defenseHeldDetail = HealthLabel(defenseHeldHeading);
        }

        // The same action as tapping the enemy-base readout: frame its reported (or last-seen) position.
        private void ScoutReportedEnemyBase()
        {
            defenseHeld.Dismiss();
            SetDefenseHeldVisible(false);
            FocusPresentedObjectiveOrBase(false);
        }

        // A slow outline pulse on the enemy-base readout: it is a control, not a status label.
        private void PulseEnemyReadout()
        {
            var target = EnemyFocusButton;
            if (target == null) return;
            if (enemyReadoutPulse == null || enemyReadoutPulse.parent != target.transform)
            {
                if (enemyReadoutPulse == null)
                {
                    enemyReadoutPulse = Panel("EnemyBaseReadoutPulse", target.transform, false);
                    enemyReadoutPulseGraphic = enemyReadoutPulse.gameObject.AddComponent<V3GradientGraphic>();
                    enemyReadoutPulseGraphic.Configure(Color.clear, Color.clear, BaseFireAmber, 4f);
                    enemyReadoutPulseGraphic.raycastTarget = false;
                }
                enemyReadoutPulse.SetParent(target.transform, false);
                enemyReadoutPulse.anchorMin = Vector2.zero; enemyReadoutPulse.anchorMax = Vector2.one;
                enemyReadoutPulse.offsetMin = new Vector2(-3f, -3f); enemyReadoutPulse.offsetMax = new Vector2(3f, 3f);
            }
            enemyReadoutPulse.SetAsLastSibling();
            enemyReadoutPulse.gameObject.SetActive(true);
            enemyReadoutPulseGraphic.color = new Color(1f, 1f, 1f, .45f + .55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
        }
    }
}
