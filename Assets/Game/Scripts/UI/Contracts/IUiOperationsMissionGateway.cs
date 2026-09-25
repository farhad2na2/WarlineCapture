namespace Game.UI.Contracts
{
    public enum UiOperationsMissionAction : byte
    {
        Deploy, FocusSite, ScanSite, FocusEvidence, RecoverEvidence, FocusExit, Conclude, Withdraw, Return, PromptWithdraw,
        AdvanceSite, AdvanceEvidence, AdvanceExit, RestartAttempt, WithdrawInterrupted, ResumeAttempt, SaveAndExit,
        StartIntroduction, SkipIntroduction, FocusObjective, FocusSquad, ScanNearby
    }

    public struct UiOperationsMissionModel
    {
        public bool InterruptedAttempt, CanResume, InMission, Finished, CanDeploy, CanConclude, Saved, EvidenceAvailable, CanRecover, CanRecoverHere, EvidenceCarried;
        public string Title, Description, Status, Clock, Objective, Result, EvidenceStatus;
        public string[] SiteStatus;
        public bool[] SiteCompleted, CanScanSite;
        public float[] SiteProgress;
        public float EvidenceProgress;
        public bool Introduction, Touring, Resumed, Paused, ViewingObjective;
        public int IntroductionStage, NextSite, CompletedScans, InfantryAtExit;
        public string Guidance, Progress;
        public UnityEngine.Vector3 ObjectivePosition;
    }

    public struct UiOperationsDashboardModel
    {
        public bool HasRun;
        public int Credits, Command, Day, ActionPoints;
        public int[] Readiness;
        public string[] Warnings;
    }

    public interface IUiOperationsMissionGateway
    {
        bool TryReadOperationsMission(out UiOperationsMissionModel model);
        bool TryRequestOperationsMission(UiOperationsMissionAction action, int siteIndex);
    }
}
