using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSelectionPanelView
    {
        internal Button ResolvePassengerTutorialButton() => _passengerDrawerOpen
            ? passengerDrawer?.ExitAllButton : passengerChipButton;

        public bool TryOpenPassengerPanel()
        {
            if(passengerChipButton==null || !passengerChipButton.isActiveAndEnabled || !passengerChipButton.interactable) return false;
            passengerChipButton.onClick.Invoke(); return true;
        }

        private void SetHealthFill(float health01)
        {
            if (healthFillImage == null)
                return;

            if (healthFillImage.type != Image.Type.Filled)
                healthFillImage.type = Image.Type.Filled;
            if (healthFillImage.fillMethod != Image.FillMethod.Horizontal)
                healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            if (healthFillImage.fillOrigin != 0)
                healthFillImage.fillOrigin = 0;
            float fillAmount = Mathf.Clamp01(health01);
            // Unity renders a sprite-less Image as a full quad even in Filled mode.
            // Scale that authored solid-color bar from its left edge as health changes.
            healthFillImage.rectTransform.localScale = new Vector3(healthFillImage.sprite==null ? fillAmount : 1f,1f,1f);
            if (!Mathf.Approximately(healthFillImage.fillAmount, fillAmount))
                healthFillImage.fillAmount = fillAmount;
        }
    }
}
