namespace Game.UI.Contracts
{
    public enum UiSkirmishAction : byte { FocusPlayer, FocusEnemy, Surrender, Replay, AdjustSetup, MainMenu, Restart }
    public struct UiSkirmishModel
    {
        public bool Finished, Paused;
        public string Infantry;
        public string PlayerBase, EnemyBase, Clock, Objective, ResultTitle, ResultDetail, Statistics;
    }
    public interface IUiSkirmishGateway
    {
        bool TryReadSkirmish(out UiSkirmishModel model);
        bool TryRequestSkirmish(UiSkirmishAction action);
    }
}
