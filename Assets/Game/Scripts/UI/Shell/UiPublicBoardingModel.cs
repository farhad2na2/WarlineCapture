using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed class UiPublicBoardingModel
    {
        public bool Visible,Eligible,Accepted,Presented,AllMounted,AlreadyMounted,CarrierSelected,MovePresented,MoveAccepted,MoveActive;
        public string Attempt,CarrierId,Status;
        public UiEntityHandle Carrier;
        public ulong World;
        public uint SetupHash,GroupId,CarrierGroupId,MoveRevision;
        public int Count,Pending,Mounted,Seats,RequestId,Frame,CompletedTarget,CompletedFrame,MoveFrame;
        public Vector2 Screen,CompletedScreen,MoveScreen;
        public Vector3 CarrierWorld,MoveWorld;
        public Vector2[] CarrierFootprint,MoveGoals;
    }
    public interface IUiPublicBoardingGateway {bool TryReadPublicBoarding(out UiPublicBoardingModel model);}
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadPublicBoarding(out UiPublicBoardingModel model)
        {model=null;return current is IUiPublicBoardingGateway gateway&&gateway.TryReadPublicBoarding(out model);}
    }
}
