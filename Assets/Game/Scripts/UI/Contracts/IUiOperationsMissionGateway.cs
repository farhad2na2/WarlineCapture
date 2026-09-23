namespace Game.UI.Contracts
{
    public enum UiOperationsMissionAction : byte
    {
        Deploy, FocusSite, ScanSite, FocusEvidence, RecoverEvidence, FocusExit, Conclude, Withdraw, Return, PromptWithdraw,
        AdvanceSite, AdvanceEvidence, AdvanceExit, RestartAttempt, WithdrawInterrupted, ResumeAttempt, SaveAndExit
    }

    public struct UiOperationsMissionModel
    {
        public bool InterruptedAttempt, CanResume, InMission, Finished, CanDeploy, CanConclude, Saved, EvidenceAvailable, CanRecover;
        public string Title, Description, Status, Clock, Objective, Result, EvidenceStatus;
        public string[] SiteStatus;
    }

    public interface IUiOperationsMissionGateway
    {
        bool TryReadOperationsMission(out UiOperationsMissionModel model);
        bool TryRequestOperationsMission(UiOperationsMissionAction action, int siteIndex);
    }
}
