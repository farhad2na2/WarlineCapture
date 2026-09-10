using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudTransportPassengerDrawerView
    {
        private void ApplyCapacitySlots(int occupiedCount, int capacity)
        {
            if (capacitySlotsRoot == null)
                return;

            int visibleCapacity = Mathf.Clamp(capacity, 0, capacitySlotsRoot.childCount);
            int visibleOccupied = Mathf.Clamp(occupiedCount, 0, visibleCapacity);
            for (int i = 0; i < capacitySlotsRoot.childCount; i++)
            {
                Transform slot = capacitySlotsRoot.GetChild(i);
                bool visible = i < visibleCapacity;
                if (slot.gameObject.activeSelf != visible)
                    slot.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                V3GradientGraphic gradient = slot.GetComponentInChildren<V3GradientGraphic>(true);
                bool occupied = i < visibleOccupied;
                gradient?.Configure(
                    occupied ? new Color32(102, 207, 57, 255) : new Color32(20, 32, 34, 255),
                    occupied ? new Color32(42, 130, 20, 255) : new Color32(5, 12, 14, 255),
                    occupied ? new Color32(129, 232, 73, 255) : new Color32(72, 94, 99, 255),
                    2f);
            }
        }

        private void ApplySelectionLayout(bool compact)
        {
            CacheSelectionLayout();
            if (!_selectionLayoutCached)
                return;

            if (!compact)
            {
                _selectionPanel.sizeDelta = _widePanelSize;
                _title.sizeDelta = _wideTitleSize;
                _title.anchoredPosition = _wideTitlePosition;
                _subtitle.sizeDelta = _wideSubtitleSize;
                _subtitle.anchoredPosition = _wideSubtitlePosition;
                _portraitFrame.sizeDelta = _widePortraitSize;
                _healthPanel.sizeDelta = _wideHealthSize;
                _orderLabel.sizeDelta = _wideOrderSize;
                _playerControl.sizeDelta = _widePlayerControlSize;
                _commandButtons.sizeDelta = _wideCommandButtonsSize;
                _commandButtons.anchoredPosition = _wideCommandButtonsPosition;
                _passengerChip.sizeDelta = _widePassengerChipSize;
                _passengerChip.anchoredPosition = _widePassengerChipPosition;
                SetHealthLayout(_wideHealthFrameSize, _wideHealthFillSize, _wideHealthTextPosition, _wideHealthTextSize);
                GridLayoutGroup wideGrid = _commandButtons.GetComponent<GridLayoutGroup>();
                if (wideGrid != null)
                {
                    wideGrid.cellSize = _wideCommandCellSize;
                    wideGrid.constraint = _wideCommandConstraint;
                    wideGrid.constraintCount = _wideCommandConstraintCount;
                }
                TMP_Text wideTitle = _title.GetComponent<TMP_Text>();
                if (wideTitle != null)
                    wideTitle.fontSize = _wideTitleFontSize;
                _playerControl.gameObject.SetActive(_widePlayerControlActive);
                _returnButton.gameObject.SetActive(_wideReturnActive);
                _destroyButton.gameObject.SetActive(_wideDestroyActive);
                _cameraButton.gameObject.SetActive(_wideCameraActive);
                ApplyCompactCommandVisual(_boardButton, false);
                ApplyCompactCommandVisual(ropeDropButton != null ? ropeDropButton.transform as RectTransform : null, false);
                ApplyCompactPassengerChip(false);
                if (ropeDropButton != null)
                    ropeDropButton.gameObject.SetActive(_wideRopeDropActive);
                return;
            }

            _selectionPanel.sizeDelta = new Vector2(306f, _widePanelSize.y);
            _title.sizeDelta = new Vector2(272f, _wideTitleSize.y);
            _title.anchoredPosition = new Vector2(17f, _wideTitlePosition.y);
            _subtitle.sizeDelta = new Vector2(272f, _wideSubtitleSize.y);
            _subtitle.anchoredPosition = new Vector2(17f, _wideSubtitlePosition.y);
            _portraitFrame.sizeDelta = new Vector2(278f, _widePortraitSize.y);
            _healthPanel.sizeDelta = new Vector2(272f, _wideHealthSize.y);
            _orderLabel.sizeDelta = new Vector2(272f, _wideOrderSize.y);
            _playerControl.sizeDelta = new Vector2(272f, _widePlayerControlSize.y);
            _playerControl.gameObject.SetActive(false);
            _passengerChip.anchoredPosition = new Vector2(_widePassengerChipPosition.x, -337f);
            _passengerChip.sizeDelta = new Vector2(272f, 78f);
            _commandButtons.anchoredPosition = new Vector2(_wideCommandButtonsPosition.x, -427f);
            _commandButtons.sizeDelta = new Vector2(272f, 179f);
            SetHealthLayout(new Vector2(183f, 18f), new Vector2(170f, 10f), new Vector2(194f, 0f), new Vector2(60f, 31f));
            GridLayoutGroup compactGrid = _commandButtons.GetComponent<GridLayoutGroup>();
            if (compactGrid != null)
            {
                compactGrid.cellSize = new Vector2(272f, 84f);
                compactGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                compactGrid.constraintCount = 1;
            }
            _returnButton.gameObject.SetActive(false);
            _destroyButton.gameObject.SetActive(false);
            _cameraButton.gameObject.SetActive(false);
            if (ropeDropButton != null)
                ropeDropButton.gameObject.SetActive(true);
            ApplyCompactCommandVisual(_boardButton, true);
            ApplyCompactCommandVisual(ropeDropButton != null ? ropeDropButton.transform as RectTransform : null, true);
            ApplyCompactPassengerChip(true);
            TMP_Text titleText = _title.GetComponent<TMP_Text>();
            if (titleText != null)
                titleText.fontSize = 18.5f;
        }

        private void CacheSelectionLayout()
        {
            if (_selectionLayoutCached)
                return;

            _selectionFrame = transform.parent as RectTransform;
            _selectionPanel = _selectionFrame != null ? _selectionFrame.parent as RectTransform : null;
            if (_selectionFrame == null || _selectionPanel == null)
                return;

            _title = FindDirectChild(_selectionFrame, "Title");
            _subtitle = FindDirectChild(_selectionFrame, "Subtitle");
            _portraitFrame = FindDirectChild(_selectionFrame, "PortraitFrame");
            _healthPanel = FindDirectChild(_selectionFrame, "HealthPanel");
            _orderLabel = FindDirectChild(_selectionFrame, "OrderLabel");
            _playerControl = FindDirectChild(_selectionFrame, "V3PlayerControl");
            _commandButtons = FindDirectChild(_selectionFrame, "CommandButtons");
            _passengerChip = FindDirectChild(_selectionFrame, "PassengerChip");
            _returnButton = FindDirectChild(_commandButtons, "ReturnButton");
            _destroyButton = FindDirectChild(_commandButtons, "DestroyButton");
            _boardButton = FindDirectChild(_commandButtons, "BoardButton");
            _cameraButton = FindDirectChild(_commandButtons, "CameraButton");
            if (_title == null || _subtitle == null || _portraitFrame == null || _healthPanel == null ||
                _orderLabel == null || _playerControl == null || _commandButtons == null || _passengerChip == null ||
                _returnButton == null || _destroyButton == null || _boardButton == null || _cameraButton == null)
            {
                return;
            }

            RectTransform healthFrame = FindDirectChild(_healthPanel, "HealthFrame");
            RectTransform healthFill = FindDirectChild(_healthPanel, "HealthFill");
            RectTransform healthText = FindDirectChild(_healthPanel, "HealthText");
            if (healthFrame == null || healthFill == null || healthText == null)
                return;

            _widePanelSize = _selectionPanel.sizeDelta;
            _wideTitleSize = _title.sizeDelta;
            _wideTitlePosition = _title.anchoredPosition;
            _wideSubtitleSize = _subtitle.sizeDelta;
            _wideSubtitlePosition = _subtitle.anchoredPosition;
            _widePortraitSize = _portraitFrame.sizeDelta;
            _wideHealthSize = _healthPanel.sizeDelta;
            _wideOrderSize = _orderLabel.sizeDelta;
            _widePlayerControlSize = _playerControl.sizeDelta;
            _wideCommandButtonsSize = _commandButtons.sizeDelta;
            _wideCommandButtonsPosition = _commandButtons.anchoredPosition;
            _widePassengerChipSize = _passengerChip.sizeDelta;
            _widePassengerChipPosition = _passengerChip.anchoredPosition;
            _wideHealthFrameSize = healthFrame.sizeDelta;
            _wideHealthFillSize = healthFill.sizeDelta;
            _wideHealthTextPosition = healthText.anchoredPosition;
            _wideHealthTextSize = healthText.sizeDelta;
            GridLayoutGroup grid = _commandButtons.GetComponent<GridLayoutGroup>();
            _wideCommandCellSize = grid != null ? grid.cellSize : Vector2.zero;
            _wideCommandConstraint = grid != null ? grid.constraint : GridLayoutGroup.Constraint.Flexible;
            _wideCommandConstraintCount = grid != null ? grid.constraintCount : 2;
            TMP_Text titleText = _title.GetComponent<TMP_Text>();
            _wideTitleFontSize = titleText != null ? titleText.fontSize : 29f;
            _widePlayerControlActive = _playerControl.gameObject.activeSelf;
            _wideReturnActive = _returnButton.gameObject.activeSelf;
            _wideDestroyActive = _destroyButton.gameObject.activeSelf;
            _wideCameraActive = _cameraButton.gameObject.activeSelf;
            _wideRopeDropActive = ropeDropButton != null && ropeDropButton.gameObject.activeSelf;
            _selectionLayoutCached = true;
        }

        private void SetHealthLayout(
            Vector2 frameSize,
            Vector2 fillSize,
            Vector2 textPosition,
            Vector2 textSize)
        {
            RectTransform healthFrame = FindDirectChild(_healthPanel, "HealthFrame");
            RectTransform healthFill = FindDirectChild(_healthPanel, "HealthFill");
            RectTransform healthText = FindDirectChild(_healthPanel, "HealthText");
            healthFrame.sizeDelta = frameSize;
            healthFill.sizeDelta = fillSize;
            healthText.anchoredPosition = textPosition;
            healthText.sizeDelta = textSize;
        }

    }
}
