namespace Game.UI.Contracts
{
    public enum UiMissionDefenseAction : byte { ReadWarning = 1, FocusWarning = 2, ContinueExplanation = 3, SkipOptional = 4, RadarPing = 5, OpenWarning = 6, CloseWarning = 7, OpenGuide = 8, CloseGuide = 9, SkipCameraTour = 10, ReduceCameraMotion = 11, ReturnCamera = 12 }

    public readonly struct UiMissionDefenseModel
    {
        public readonly bool IsActive, HasWarning, CanFocus, CanPing;
        public readonly bool CanReturnCamera, WarningNeedsAttention;
        public readonly string WarningText, PingText;
        public readonly int Charges, CooldownSeconds, WarningElementIndex, GuidanceId;
        public readonly uint Version;
        public UiMissionDefenseModel(bool active, bool warning, bool focus, bool ping, string warningText, string pingText,
            int charges, int cooldown, int element, int guidanceId, uint version,bool canReturnCamera=false,bool warningNeedsAttention=false)
        { IsActive=active; HasWarning=warning; CanFocus=focus; CanPing=ping; WarningText=warningText; PingText=pingText;
          Charges=charges; CooldownSeconds=cooldown; WarningElementIndex=element; GuidanceId=guidanceId; Version=version; CanReturnCamera=canReturnCamera; WarningNeedsAttention=warningNeedsAttention; }
    }

    public interface IUiMissionDefenseGateway
    {
        bool TryReadMissionDefense(out UiMissionDefenseModel model);
        bool TryReadMissionCameraTour();
        bool TryReadMissionRadioArchive();
        bool IsMissionFieldGuidePresenting();
        bool TryRequestMissionDefenseAction(UiMissionDefenseAction action, int warningElementIndex, int guidanceId);
    }
}
