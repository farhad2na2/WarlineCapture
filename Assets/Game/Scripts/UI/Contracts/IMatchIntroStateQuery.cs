namespace Game.UI.Contracts
{
    public interface IMatchIntroStateQuery
    {
        bool IsGameplayInputLocked();

        bool IsIntroComplete();
    }
}
