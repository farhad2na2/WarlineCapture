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
        [SerializeField] private RectTransform passengerChipIcon;
        public bool IsPassengerDrawerOpen => _passengerDrawerOpen;
        public void ToggleTransportPassengerDrawer()
        {
            _passengerDrawerOpen = !_passengerDrawerOpen;
        }

        public void CloseTransportPassengerDrawer()
        {
            _passengerDrawerOpen = false;
        }

        public void ApplyTransportPassengers(MatchHudTransportPassengersModel model)
        {
            bool isPassengerModel = model.StorageKind == MatchHudStorageChipKind.Passengers;
            if (!model.Visible || (isPassengerModel && model.Transport.IsNull))
            {
                _passengerDrawerOpen = false;
                _passengerDrawerTransport = UiEntityHandle.Null;
                ApplyPassengerChip(MatchHudTransportPassengersModel.Hidden);
                passengerDrawer?.Apply(MatchHudTransportPassengersModel.Hidden);
                return;
            }

            if (!isPassengerModel)
            {
                _passengerDrawerOpen = false;
                _passengerDrawerTransport = UiEntityHandle.Null;
                ApplyPassengerChip(model);
                passengerDrawer?.Apply(MatchHudTransportPassengersModel.Hidden);
                return;
            }

            if (_passengerDrawerTransport != model.Transport)
            {
                _passengerDrawerTransport = model.Transport;
                _passengerDrawerOpen = false;
            }

            ApplyPassengerChip(model);
            passengerDrawer?.Apply(new MatchHudTransportPassengersModel(
                true,
                _passengerDrawerOpen,
                model.Transport,
                model.PassengerCount,
                model.Capacity,
                model.ExitAllEnabled,
                model.Passengers ?? _emptyPassengers,
                model.SoldierPassengerCount,
                model.SoldierCapacity,
                model.VehiclePassengerCount,
                model.VehicleCapacity,
                model.StorageKind,
                model.OilCurrent,
                model.OilCapacity,
                model.FuelCurrent,
                model.FuelCapacity,
                model.StatusText));
        }

        private void HandlePassengerChip()
        {
            if (_storageChipKind == MatchHudStorageChipKind.MaterialFabrication)
            {
                bool productionEnabled = !_materialFabricationProductionEnabled;
                _materialFabricationProductionEnabled = productionEnabled;
                _materialFabricationProductionRequested?.Invoke(productionEnabled);
                return;
            }

            ToggleTransportPassengerDrawer();
            _passengerChipRequested?.Invoke();
        }

        private void HandlePassengerDrawerClose()
        {
            CloseTransportPassengerDrawer();
            _passengerDrawerCloseRequested?.Invoke();
        }

        private void HandlePassengerExitAll()
        {
            _passengerExitAllRequested?.Invoke();
        }

        private void HandlePassengerExit(UiEntityHandle passenger)
        {
            _passengerExitRequested?.Invoke(passenger);
        }

        private void ApplyPassengerChip(MatchHudTransportPassengersModel model)
        {
            bool visible = model.Visible;
            _storageChipKind = model.StorageKind;
            _materialFabricationProductionEnabled = model.ProductionEnabled;
            ApplyPassengerChipLayout(model.StorageKind == MatchHudStorageChipKind.MaterialFabrication);
            if (passengerChipRoot != null && passengerChipRoot.activeSelf != visible)
                passengerChipRoot.SetActive(visible);

            if (passengerChipButton != null)
            {
                bool interactable = visible &&
                                    (model.StorageKind == MatchHudStorageChipKind.Passengers ||
                                     model.StorageKind == MatchHudStorageChipKind.MaterialFabrication);
                if (passengerChipButton.interactable != interactable)
                    passengerChipButton.interactable = interactable;
            }

            if (passengerChipLabel != null)
            {
                string text = visible ? ResolveStorageChipLabel(model) : string.Empty;
                SetRawText(passengerChipLabel, text);
            }
        }

        private static string ResolveStorageChipLabel(MatchHudTransportPassengersModel model)
        {
            return model.StorageKind switch
            {
                MatchHudStorageChipKind.OilBarrels =>
                    AppendStatus(
                        $"OIL BARRELS {Mathf.Max(0, model.OilCurrent)}/{Mathf.Max(0, model.OilCapacity)}",
                        model.StatusText),
                MatchHudStorageChipKind.FuelBarrels =>
                    AppendStatus(
                        $"FUEL {Mathf.Max(0, model.FuelCurrent)}/{Mathf.Max(0, model.FuelCapacity)}",
                        model.StatusText),
                MatchHudStorageChipKind.OilAndFuel =>
                    AppendStatus(
                        $"OIL {Mathf.Max(0, model.OilCurrent)}/{Mathf.Max(0, model.OilCapacity)} | FUEL {Mathf.Max(0, model.FuelCurrent)}/{Mathf.Max(0, model.FuelCapacity)}",
                        model.StatusText),
                MatchHudStorageChipKind.ResourceCargo =>
                    ResolveResourceCargoChipLabel(model),
                MatchHudStorageChipKind.MaterialFabrication =>
                    ResolveMaterialFabricationChipLabel(model),
                _ =>
                    $"PASSENGERS {Mathf.Max(0, model.PassengerCount)}/{Mathf.Max(0, model.Capacity)}"
            };
        }

        private static string AppendStatus(string label, string statusText)
        {
            return string.IsNullOrEmpty(statusText)
                ? label
                : $"{label} | {statusText}";
        }

        private static string ResolveResourceCargoChipLabel(MatchHudTransportPassengersModel model)
        {
            int capacity = Mathf.Max(0, model.Capacity);
            int oil = Mathf.Max(0, model.OilCurrent);
            int fuel = Mathf.Max(0, model.FuelCurrent);
            string label;
            if (oil > 0 && fuel > 0)
                label = $"OIL {oil}/{capacity} | FUEL {fuel}/{capacity}";
            else if (fuel > 0)
                label = $"FUEL {fuel}/{capacity}";
            else if (oil > 0)
                label = $"OIL {oil}/{capacity}";
            else
                label = $"CARGO 0/{capacity}";
            return AppendStatus(label, model.StatusText);
        }

        private static string ResolveMaterialFabricationChipLabel(MatchHudTransportPassengersModel model)
        {
            int progressPercent = Mathf.RoundToInt(Mathf.Clamp01(model.CycleProgress01) * 100f);
            int cycleSeconds = Mathf.Max(0, Mathf.RoundToInt(model.CycleDurationSeconds));
            string label =
                $"OIL {Mathf.Max(0, model.OilCurrent)}/{Mathf.Max(0, model.OilCapacity)} | " +
                $"MATERIALS {Mathf.Max(0, model.MaterialsCurrent)}/{Mathf.Max(0, model.MaterialsCapacity)}\n" +
                $"{Mathf.Max(0, model.OilConsumedPerCycle)} OIL > {Mathf.Max(0, model.MaterialsOutputPerCycle)} MATERIALS / {cycleSeconds}s\n" +
                $"{progressPercent}%";
            return AppendStatus(label, model.StatusText);
        }

        private void CachePassengerChipLayout()
        {
            if (_passengerChipLayoutCached)
                return;

            _passengerChipRect = passengerChipRoot != null
                ? passengerChipRoot.transform as RectTransform
                : null;
            if (_passengerChipRect != null)
            {
                _passengerChipDefaultSize = _passengerChipRect.sizeDelta;
                _passengerChipDefaultPosition = _passengerChipRect.anchoredPosition;
                _passengerChipIconRect = passengerChipIcon;
                _passengerChipIconDefaultActive = _passengerChipIconRect != null && _passengerChipIconRect.gameObject.activeSelf;
            }
            _commandButtonsRoot = returnAction != null && returnAction.transform.parent != null
                ? returnAction.transform.parent.gameObject
                : null;
            _commandButtonsDefaultActive = _commandButtonsRoot != null && _commandButtonsRoot.activeSelf;
            if (passengerChipLabel != null)
            {
                _passengerChipLabelRect = passengerChipLabel.rectTransform;
                _passengerChipLabelDefaultAnchorMin = _passengerChipLabelRect.anchorMin;
                _passengerChipLabelDefaultAnchorMax = _passengerChipLabelRect.anchorMax;
                _passengerChipLabelDefaultPivot = _passengerChipLabelRect.pivot;
                _passengerChipLabelDefaultPosition = _passengerChipLabelRect.anchoredPosition;
                _passengerChipLabelDefaultSize = _passengerChipLabelRect.sizeDelta;
                _passengerChipDefaultAutoSizing = passengerChipLabel.enableAutoSizing;
                _passengerChipDefaultFontSizeMin = passengerChipLabel.fontSizeMin;
                _passengerChipDefaultFontSizeMax = passengerChipLabel.fontSizeMax;
                _passengerChipDefaultWrappingMode = passengerChipLabel.textWrappingMode;
                _passengerChipDefaultOverflowMode = passengerChipLabel.overflowMode;
                _passengerChipDefaultMargin = passengerChipLabel.margin;
            }

            _passengerChipLayoutCached = true;
        }

        private void ApplyPassengerChipLayout(bool materialFabrication)
        {
            CachePassengerChipLayout();
            if (_passengerChipRect != null)
            {
                Vector2 size = _passengerChipDefaultSize;
                if (materialFabrication)
                {
                    float width = MaterialFabricationChipSize.x;
                    if (_passengerChipRect.parent is RectTransform parent)
                    {
                        float horizontalInset = Mathf.Max(0f, Mathf.Abs(_passengerChipRect.anchoredPosition.x));
                        width = Mathf.Min(width, Mathf.Max(0f, parent.rect.width - horizontalInset * 2f));
                    }
                    size = new Vector2(width, MaterialFabricationChipSize.y);
                }
                if (_passengerChipRect.sizeDelta != size)
                    _passengerChipRect.sizeDelta = size;
                Vector2 position = materialFabrication
                    ? new Vector2(_passengerChipDefaultPosition.x, -548f)
                    : _passengerChipDefaultPosition;
                if (_passengerChipRect.anchoredPosition != position)
                    _passengerChipRect.anchoredPosition = position;
            }

            if (_commandButtonsRoot != null && _commandButtonsRoot.activeSelf != (!materialFabrication && _commandButtonsDefaultActive))
                _commandButtonsRoot.SetActive(!materialFabrication && _commandButtonsDefaultActive);
            if (_passengerChipIconRect != null)
            {
                bool iconActive = !materialFabrication && _passengerChipIconDefaultActive;
                if (_passengerChipIconRect.gameObject.activeSelf != iconActive)
                    _passengerChipIconRect.gameObject.SetActive(iconActive);
            }

            if (passengerChipLabel == null)
                return;

            if (_passengerChipLabelRect != null)
            {
                if (materialFabrication)
                {
                    _passengerChipLabelRect.anchorMin = Vector2.zero;
                    _passengerChipLabelRect.anchorMax = Vector2.one;
                    _passengerChipLabelRect.pivot = new Vector2(.5f, .5f);
                    _passengerChipLabelRect.anchoredPosition = Vector2.zero;
                    _passengerChipLabelRect.sizeDelta = Vector2.zero;
                }
                else
                {
                    _passengerChipLabelRect.anchorMin = _passengerChipLabelDefaultAnchorMin;
                    _passengerChipLabelRect.anchorMax = _passengerChipLabelDefaultAnchorMax;
                    _passengerChipLabelRect.pivot = _passengerChipLabelDefaultPivot;
                    _passengerChipLabelRect.anchoredPosition = _passengerChipLabelDefaultPosition;
                    _passengerChipLabelRect.sizeDelta = _passengerChipLabelDefaultSize;
                }
            }

            passengerChipLabel.enableAutoSizing = materialFabrication || _passengerChipDefaultAutoSizing;
            passengerChipLabel.fontSizeMin = materialFabrication ? 20f : _passengerChipDefaultFontSizeMin;
            passengerChipLabel.fontSizeMax = materialFabrication ? 32f : _passengerChipDefaultFontSizeMax;
            passengerChipLabel.textWrappingMode = materialFabrication
                ? TextWrappingModes.Normal
                : _passengerChipDefaultWrappingMode;
            passengerChipLabel.overflowMode = materialFabrication
                ? TextOverflowModes.Overflow
                : _passengerChipDefaultOverflowMode;
            passengerChipLabel.margin = materialFabrication
                ? new Vector4(18f, 8f, 18f, 8f)
                : _passengerChipDefaultMargin;
        }

    }
}
