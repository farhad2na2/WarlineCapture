using System;
using UnityEngine;

namespace Game.UI.Contracts
{
    public interface IMatchHudSelectionPanelView
    {
        void BindActions(Action returnRequested, Action destroyRequested, Action boardRequested);

        void BindCameraAction(Action cameraRequested);

        void BindTransportPassengerActions(
            Action passengerChipRequested,
            Action passengerDrawerCloseRequested,
            Action passengerExitAllRequested,
            Action<UiEntityHandle> passengerExitRequested);

        void BindMaterialFabricationProductionAction(Action<bool> productionEnabledRequested);

        void HideSelection();

        void SetSelectionVisible(bool visible);

        void SetSelectionVisible(bool visible, Sprite portraitSprite);

        void SetBoardActionSelected(bool selected);

        void SetCameraActionSelected(bool selected);

        void SetCameraActionEnabled(bool enabled);

        Sprite ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind kind);

        void Apply(MatchHudSelectionPanelModel model);

        void ApplyTransportPassengers(MatchHudTransportPassengersModel model);
    }
}
