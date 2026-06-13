public interface ISelectionUiCommand
{
    void CaptureUiClickSequence();
    bool RequestDeselectAll();
    bool RequestEnterSelectionMode();
    bool RequestExitSelectionMode();
    bool RequestMoveCommandMode();
    bool RequestAttackCommandMode();
    bool RequestScanCommandMode();
    bool RequestHoldPosition();
    bool RequestStop();
    bool RequestBoardAllSelectedTransport();
    bool RequestCancelActiveCommandMode();
}
