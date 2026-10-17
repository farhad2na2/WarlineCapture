using System;

namespace Game.UI.Runtime
{
    public enum UiVehicleRepairPhase : byte { None,Working,Completed,Cancelled,Destroyed,AccountingFailure }
    public sealed class UiVehicleRepairModel
    {
        public bool Applicable,Visible,SingleOwnVehicle,Eligible,Working,RequestPending,Paused,TerminalFeedback;
        public string AttemptId,ActorPublicId,Status,ActionLabel;
        public uint GroupId,StateRequestId,IssuedRequestId;
        public int Health,MaximumHealth,Materials,PaidMaterials,RefundedMaterials;
        public long FractionNumerator,FractionDenominator,DurationTicks,RemainingTicks;
        public UiVehicleRepairPhase Phase;
        public int IssuedFrame,CompletedContactTargetId,CompletedContactFrame;
        public UnityEngine.Vector2 IssuedScreen,CompletedContactPosition;
        public bool IssuedScreenKnown,IssuedCancel,StateMatchesIssued,RequestRejected;
    }
    public interface IUiVehicleRepairGateway
    {
        bool TryReadVehicleRepair(out UiVehicleRepairModel model);
        bool TryRequestQuotedVehicleRepair(UiVehicleRepairModel presented,bool cancel);
    }
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadVehicleRepair(out UiVehicleRepairModel model)
        {model=null;return current is IUiVehicleRepairGateway gateway&&gateway.TryReadVehicleRepair(out model);}
        public static bool TryRequestQuotedVehicleRepair(UiVehicleRepairModel presented,bool cancel)=>
            current is IUiVehicleRepairGateway gateway&&gateway.TryRequestQuotedVehicleRepair(presented,cancel);
    }
}
