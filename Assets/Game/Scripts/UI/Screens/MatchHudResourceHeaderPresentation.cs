using TMPro;
using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed class MatchHudResourceHeaderPresentation
    {
        private const float RefreshIntervalSeconds = 0.2f;

        private GameObject _oilSlotRoot;
        private TMP_Text _materialsSlotLabel;
        private TMP_Text _materialsSlotValue;
        private TMP_Text _oilSlotLabel;
        private TMP_Text _oilSlotValue;
        private TMP_Text _fuelSlotLabel;
        private TMP_Text _fuelSlotValue;
        private TMP_Text _civilianRiskSlotLabel;
        private TMP_Text _civilianRiskSlotValue;
        private string _lastMaterialsText;
        private string _lastOilText;
        private string _lastFuelText;
        private string _lastCivilianRiskText;
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
            TMP_Text materialsSlotLabel,
            TMP_Text materialsSlotValue,
            TMP_Text oilSlotLabel,
            TMP_Text oilSlotValue,
            TMP_Text fuelSlotLabel,
            TMP_Text fuelSlotValue,
            TMP_Text civilianRiskSlotLabel,
            TMP_Text civilianRiskSlotValue,
            float now)
        {
            Clear();
            _oilSlotRoot = oilSlotRoot;
            _materialsSlotLabel = materialsSlotLabel;
            _materialsSlotValue = materialsSlotValue;
            _oilSlotLabel = oilSlotLabel;
            _oilSlotValue = oilSlotValue;
            _fuelSlotLabel = fuelSlotLabel;
            _fuelSlotValue = fuelSlotValue;
            _civilianRiskSlotLabel = civilianRiskSlotLabel;
            _civilianRiskSlotValue = civilianRiskSlotValue;
            RefreshNow();
            _nextRefreshTime = now + RefreshIntervalSeconds;
        }

        public void Clear()
        {
            _oilSlotRoot = null;
            _materialsSlotLabel = null;
            _materialsSlotValue = null;
            _oilSlotLabel = null;
            _oilSlotValue = null;
            _fuelSlotLabel = null;
            _fuelSlotValue = null;
            _civilianRiskSlotLabel = null;
            _civilianRiskSlotValue = null;
            _lastMaterialsText = null;
            _lastOilText = null;
            _lastFuelText = null;
            _lastCivilianRiskText = null;
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
            if (_materialsSlotValue == null && _oilSlotValue == null && _fuelSlotValue == null)
                return;

            bool appliedNumericValues = false;
            if (UiShellRuntimeGateway.TryReadMatchHudResourceValues(
                    out UiMatchHudResourceValuesModel values) &&
                values.IsValid &&
                !values.RequiresTextFallback)
            {
                ApplyVisibility(values.ShowOil);
                ApplyLabels();
                ApplyNumericValues(values);
                appliedNumericValues = true;
            }

            if (!UiShellRuntimeGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header))
                return;

            ApplyVisibility(header.ShowOil);
            ApplyLabels();
            ApplyHeaderText(header, appliedNumericValues);
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

        private void ApplyHeaderText(in UiMatchHudHeaderModel header, bool resourceValuesApplied)
        {
            SetTextIfChanged(_materialsSlotValue, header.SupplyText, ref _lastMaterialsText);
            SetTextIfChanged(_civilianRiskSlotValue, header.CivilianRiskText, ref _lastCivilianRiskText);

            if (!resourceValuesApplied && header.ShowOil)
            {
                string oilText = string.IsNullOrWhiteSpace(header.OilText) ? "0" : header.OilText;
                if (_oilSlotValue != null && (_lastOilWasNumeric || _lastOilText != oilText))
                {
                    _oilSlotValue.text = oilText;
                    _lastOilText = oilText;
                    _lastOilWasNumeric = false;
                }
            }

            if (!resourceValuesApplied)
            {
                string fuelText = string.IsNullOrWhiteSpace(header.FuelText) ? "0" : header.FuelText;
                if (_fuelSlotValue != null && (_lastFuelWasNumeric || _lastFuelText != fuelText))
                {
                    _fuelSlotValue.text = fuelText;
                    _lastFuelText = fuelText;
                    _lastFuelWasNumeric = false;
                }
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

            if (_materialsSlotLabel != null && _materialsSlotLabel.text != "Materials")
                _materialsSlotLabel.text = "Materials";
            if (_oilSlotLabel != null && _oilSlotLabel.text != "Oil")
                _oilSlotLabel.text = "Oil";
            if (_fuelSlotLabel != null && _fuelSlotLabel.text != "Fuel")
                _fuelSlotLabel.text = "Fuel";
            if (_civilianRiskSlotLabel != null && _civilianRiskSlotLabel.text != "Civilian Risk")
                _civilianRiskSlotLabel.text = "Civilian Risk";
            _labelsApplied = true;
        }

        private static void SetTextIfChanged(TMP_Text target, string value, ref string previousValue)
        {
            if (target == null)
                return;

            string safeValue = string.IsNullOrWhiteSpace(value) ? "0" : value;
            if (previousValue == safeValue)
                return;

            target.text = safeValue;
            previousValue = safeValue;
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
