namespace Game.Operations.Contracts
{
    public readonly struct UiOperationsReadModel
    {
        public UiOperationsReadModel(
            int revision,
            int day,
            int actionPoints,
            OperationsRunPhaseKind phase,
            string selectedDistrictId,
            bool endDayAvailable,
            bool deployAvailable)
        {
            Revision = revision;
            Day = day;
            ActionPoints = actionPoints;
            Phase = phase;
            SelectedDistrictId = selectedDistrictId ?? string.Empty;
            EndDayAvailable = endDayAvailable;
            DeployAvailable = deployAvailable;
        }

        public int Revision { get; }
        public int Day { get; }
        public int ActionPoints { get; }
        public OperationsRunPhaseKind Phase { get; }
        public string SelectedDistrictId { get; }
        public bool EndDayAvailable { get; }
        public bool DeployAvailable { get; }
    }

    /// <summary>
    /// Operations-owned gateway contract. ARCHITECTURE places the shipping copy in
    /// Game.UI.Contracts after Game PM adds an assembly reference. P0 does not edit
    /// that shared asmdef.
    /// </summary>
    public interface IUiOperationsGateway
    {
        bool TryReadOperations(out UiOperationsReadModel model);
        OperationsCommandResult RequestDeploy(OperationsCommand command);
        OperationsCommandResult RequestAbstractAction(OperationsCommand command);
        OperationsCommandResult RequestEndDay(OperationsCommand command);
        OperationsCommandResult RequestConclude(OperationsCommand command);
        OperationsCommandResult RequestWithdraw(OperationsCommand command);
        OperationsCommandResult RequestPractice(OperationsCommand command);
        OperationsCommandResult RequestResume(OperationsCommand command);
    }
}
