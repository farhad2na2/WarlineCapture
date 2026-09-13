namespace Game.UI.Contracts
{
    public interface IUiMissionBreachGateway
    {
        bool TryContinueBreachPlan();
        bool TrySelectBreachActor();
        bool TryReadBreachInputMode(out int mode);
        bool IsBreachGuideContext();
    }
}
