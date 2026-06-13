using System;
using UnityEngine;

public interface IMatchHudSelectionPanelView
{
    void BindActions(Action returnRequested, Action destroyRequested, Action boardRequested);

    void BindTransportPassengerActions(
        Action passengerChipRequested,
        Action passengerDrawerCloseRequested,
        Action passengerExitAllRequested,
        Action<UiEntityHandle> passengerExitRequested);

    void HideSelection();

    void SetSelectionVisible(bool visible);

    void SetSelectionVisible(bool visible, Sprite portraitSprite);

    void SetBoardActionSelected(bool selected);

    Sprite ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind kind);

    void Apply(MatchHudSelectionPanelModel model);

    void ApplyTransportPassengers(MatchHudTransportPassengersModel model);
}
