using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed class UiAircraftServiceModel
    {
        public bool CanScan,ScanCompleted;
        public uint ScanRevision;
        public int ScanRequestId,ScanFrame;
        public Vector2 ScanScreen;
        public Vector3 ScanCenter;
        public AriaPublicAirRole Role;
        public bool OffensiveEligible,PressureBusy,ScanInProgress;
        public uint PressureRevision;
        public int PressureFrame;
        public Vector2 PressureScreen;
        public Vector3 PressureGoal;
        public bool Visible,Airborne,Returning,Rolling,HomeKnown,AtHome,Paused,HasReceipt;
        public string Attempt,StableId,Status;
        public UiEntityHandle Actor;
        public ulong World;
        public uint SetupHash,GroupId,ReturnRevision;
        public int PooledFactionFuel,RequestId,RequestFrame,CompletedTarget,CompletedFrame;
        public Vector2 RequestScreen,CompletedScreen;
    }
    public interface IUiAircraftServiceGateway
    {bool TryReadSelectedAircraftService(out UiAircraftServiceModel model);}
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadSelectedAircraftService(out UiAircraftServiceModel model)
        {model=null;return current is IUiAircraftServiceGateway gateway&&gateway.TryReadSelectedAircraftService(out model);}
    }
}
