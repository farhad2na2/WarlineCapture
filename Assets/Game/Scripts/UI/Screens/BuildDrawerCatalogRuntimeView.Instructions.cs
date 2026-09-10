using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerCatalogRuntimeView
    {
        private void ApplyInstructionForCurrentSelection()
        {
            if (view == null)
                return;

            if (!_hasSelectedItem || _selectedItem.Prefab == null)
            {
                ApplyInstruction(BuildDrawerCatalogPresentationSystemHelper.FormatEmptyCategoryInstruction(_gameTextResolver, _activeCategory), BuildDrawerInstructionSeverity.Warning);
                return;
            }

            if (_uiCommandSystem == null)
            {
                ApplyInstruction(BuildDrawerCatalogPresentationSystemHelper.FormatReadyInstruction(_gameTextResolver, _selectedItem), BuildDrawerInstructionSeverity.Ready);
                return;
            }

            BuildingUiCommandFailure failure = _uiCommandSystem.GetCampRequestFailure(
                _selectedItem.Prefab,
                _selectedItem.MaterialsCost,
                out string requiredBuildingDisplayName);
            if (failure == BuildingUiCommandFailure.None)
            {
                if (_selectedItem.Category == BuildDrawerCategory.Buildings && _uiCommandSystem.HasPendingBuildingPlacement)
                {
                    string status = _uiCommandSystem.PlacementStatusText;
                    bool canConfirm = _uiCommandSystem.CanConfirmBuildingPlacement;
                    ApplyInstruction(
                        canConfirm
                            ? _gameTextResolver.Format("build.drawer.instruction.place_pending_confirm", "Place {0}: drag to position, then confirm.", _selectedItem.DisplayName)
                            : _gameTextResolver.Format("build.drawer.instruction.cannot_place_here", "Cannot place here: {0}.", BuildDrawerCatalogPresentationSystemHelper.FormatPlacementStatus(_gameTextResolver, status)),
                        canConfirm ? BuildDrawerInstructionSeverity.Ready : BuildDrawerInstructionSeverity.Error);
                    return;
                }

                ApplyInstruction(BuildDrawerCatalogPresentationSystemHelper.FormatReadyInstruction(_gameTextResolver, _selectedItem), BuildDrawerInstructionSeverity.Ready);
                return;
            }

            ApplyInstruction(
                BuildDrawerCatalogPresentationSystemHelper.FormatInstructionFailureMessage(
                    _gameTextResolver,
                    failure,
                    requiredBuildingDisplayName,
                    _selectedItem,
                    _hasSelectedItem,
                    MaxQueuedUnitProductions),
                BuildDrawerInstructionSeverity.Error);
        }

        private int MaxQueuedUnitProductions =>
            Mathf.Max(0, _uiCommandSystem != null ? _uiCommandSystem.MaxQueuedUnitProductions : 25);

        private BuildingUiCommandFailure GetCampRequestFailure(
            BuildDrawerCatalogItem item,
            out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return _uiCommandSystem != null
                ? _uiCommandSystem.GetCampRequestFailure(item.Prefab, item.MaterialsCost, out requiredBuildingDisplayName)
                // Catalog metadata can arrive one presentation update before the
                // live command adapter. Keep valid catalog cards selectable during
                // that short binding window; OnPrimaryActionClicked already rejects
                // with the explicit "still connecting" result until the adapter is ready.
                : BuildingUiCommandFailure.None;
        }

        private void ApplyInstruction(string text, BuildDrawerInstructionSeverity severity) => view?.ApplyInstruction(text, severity);

    }
}
