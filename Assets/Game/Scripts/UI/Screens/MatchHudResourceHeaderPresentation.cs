using TMPro;
using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed class MatchHudResourceHeaderPresentation
    {
        private const float RefreshIntervalSeconds = 0.2f;

        private GameObject _oilSlotRoot;
        private TMP_Text _oilSlotLabel;
        private TMP_Text _oilSlotValue;
        private TMP_Text _fuelSlotLabel;
        private TMP_Text _fuelSlotValue;
        private string _lastOilText;
        private string _lastFuelText;
        private int _lastOilValue;
        private int _lastFuelValue;
        private bool _lastOilWasNumeric;
        private bool _lastFuelWasNumeric;
        private bool _lastShowOil;
        private bool _oilVisibilityApplied;
        private bool _labelsApplied;
        private float _nextRefreshTime;

        public void Bind(
            GameObject oilSlotRoot,
            TMP_Text oilSlotLabel,
            TMP_Text oilSlotValue,
            TMP_Text fuelSlotLabel,
            TMP_Text fuelSlotValue,
            float now)
        {
            Clear();
            _oilSlotRoot = oilSlotRoot;
            _oilSlotLabel = oilSlotLabel;
            _oilSlotValue = oilSlotValue;
            _fuelSlotLabel = fuelSlotLabel;
            _fuelSlotValue = fuelSlotValue;
            RefreshNow();
            _nextRefreshTime = now + RefreshIntervalSeconds;
        }

        public void Clear()
        {
            _oilSlotRoot = null;
            _oilSlotLabel = null;
            _oilSlotValue = null;
            _fuelSlotLabel = null;
            _fuelSlotValue = null;
            _lastOilText = null;
            _lastFuelText = null;
            _lastOilValue = 0;
            _lastFuelValue = 0;
            _lastOilWasNumeric = false;
            _lastFuelWasNumeric = false;
            _lastShowOil = false;
            _oilVisibilityApplied = false;
            _labelsApplied = false;
            _nextRefreshTime = 0f;
        }

        public void RefreshIfDue(float now)
        {
            if (_labelsApplied && now < _nextRefreshTime)
                return;

            _nextRefreshTime = now + RefreshIntervalSeconds;
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (_oilSlotValue == null && _fuelSlotValue == null)
                return;

            if (UiShellRuntimeGateway.TryReadMatchHudResourceValues(
                    out UiMatchHudResourceValuesModel values) &&
                values.IsValid &&
                !values.RequiresTextFallback)
            {
                ApplyVisibility(values.ShowOil);
                ApplyLabels();
                ApplyNumericValues(values);
                return;
            }

            if (!UiShellRuntimeGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header))
                return;

            ApplyVisibility(header.ShowOil);
            ApplyLabels();
            ApplyFallbackText(header);
        }

        private void ApplyNumericValues(in UiMatchHudResourceValuesModel values)
        {
            if (values.ShowOil &&
                _oilSlotValue != null &&
                (!_lastOilWasNumeric || _lastOilValue != values.Oil))
            {
                SetCompactText(_oilSlotValue, values.Oil);
                _lastOilValue = values.Oil;
                _lastOilText = null;
                _lastOilWasNumeric = true;
            }

            if (_fuelSlotValue != null &&
                (!_lastFuelWasNumeric || _lastFuelValue != values.Fuel))
            {
                SetCompactText(_fuelSlotValue, values.Fuel);
                _lastFuelValue = values.Fuel;
                _lastFuelText = null;
                _lastFuelWasNumeric = true;
            }
        }

        private void ApplyFallbackText(in UiMatchHudHeaderModel header)
        {
            if (header.ShowOil)
            {
                string oilText = string.IsNullOrWhiteSpace(header.OilText) ? "0" : header.OilText;
                if (_oilSlotValue != null && (_lastOilWasNumeric || _lastOilText != oilText))
                {
                    _oilSlotValue.text = oilText;
                    _lastOilText = oilText;
                    _lastOilWasNumeric = false;
                }
            }

            string fuelText = string.IsNullOrWhiteSpace(header.FuelText) ? "0" : header.FuelText;
            if (_fuelSlotValue != null && (_lastFuelWasNumeric || _lastFuelText != fuelText))
            {
                _fuelSlotValue.text = fuelText;
                _lastFuelText = fuelText;
                _lastFuelWasNumeric = false;
            }
        }

        private void ApplyVisibility(bool showOil)
        {
            if (_oilSlotRoot == null || (_oilVisibilityApplied && _lastShowOil == showOil))
                return;

            _oilSlotRoot.SetActive(showOil);
            _lastShowOil = showOil;
            _oilVisibilityApplied = true;
        }

        private void ApplyLabels()
        {
            if (_labelsApplied)
                return;

            if (_oilSlotLabel != null && _oilSlotLabel.text != "Oil")
                _oilSlotLabel.text = "Oil";
            if (_fuelSlotLabel != null && _fuelSlotLabel.text != "Fuel")
                _fuelSlotLabel.text = "Fuel";
            _labelsApplied = true;
        }

        private static void SetCompactText(TMP_Text target, int value)
        {
            int safeValue = Mathf.Max(0, value);
            if (safeValue >= 1000000)
                target.SetText("{0:0.#}M", safeValue / 1000000f);
            else if (safeValue >= 10000)
                target.SetText("{0:0.#}K", safeValue / 1000f);
            else
                target.SetText("{0}", safeValue);
        }
    }
}
