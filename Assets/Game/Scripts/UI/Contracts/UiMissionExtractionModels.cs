namespace Game.UI.Contracts
{
    public enum UiMissionExtractionAction : byte { ShowLesson, ContinuePlan, FocusTeam, FocusLanding, FocusDeparture }
    public readonly struct UiMissionExtractionModel
    {
        public readonly int Aboard, Delivered, Required, CarrierLeg, SecureSeconds, RemainingSeconds, Lesson;
        public readonly bool Contested, Cleared;
        public UiMissionExtractionModel(int aboard,int delivered,int required,int carrierLeg,int secure,int remaining,int lesson,bool contested,bool cleared)
        {Aboard=aboard;Delivered=delivered;Required=required;CarrierLeg=carrierLeg;SecureSeconds=secure;RemainingSeconds=remaining;Lesson=lesson;Contested=contested;Cleared=cleared;}
    }
    public interface IUiMissionExtractionGateway
    {
        bool TryReadMissionExtraction(out UiMissionExtractionModel model);
        bool TryRequestExtractionAction(UiMissionExtractionAction action);
        bool IsExtractionGuideContext();
    }
}
