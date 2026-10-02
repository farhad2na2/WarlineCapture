using UnityEngine;
namespace Game.UI.Contracts
{
    public readonly struct UiTrustUnderFireModel
    {
        public readonly int Stage, NorthHoldSeconds, SouthHoldSeconds, VerificationSeconds, RemainingSeconds, HostilesDefeated;
        public readonly bool NorthArrived, SouthArrived, RelayVerified, NorthCrossed, SouthCrossed;
        public readonly Vector3 NorthCrossing, SouthCrossing, NorthArrival, SouthArrival, RelayGate, RelayApproach;
        public UiTrustUnderFireModel(int stage, int northSeconds, int southSeconds, int verificationSeconds, int remaining, int cleared,
            bool northArrived, bool southArrived, bool relayVerified, bool northCrossed, bool southCrossed,
            Vector3 northCrossing, Vector3 southCrossing, Vector3 northArrival, Vector3 southArrival, Vector3 relayGate, Vector3 relayApproach)
        {
            Stage=stage; NorthHoldSeconds=northSeconds; SouthHoldSeconds=southSeconds; VerificationSeconds=verificationSeconds;
            RemainingSeconds=remaining; HostilesDefeated=cleared; NorthArrived=northArrived; SouthArrived=southArrived;
            RelayVerified=relayVerified; NorthCrossed=northCrossed; SouthCrossed=southCrossed;
            NorthCrossing=northCrossing; SouthCrossing=southCrossing; NorthArrival=northArrival; SouthArrival=southArrival;
            RelayGate=relayGate; RelayApproach=relayApproach;
        }
    }
    public readonly struct UiTrustUnderFireResultModel
    {
        public readonly bool NorthArrived, SouthArrived, RelayVerified;
        public readonly int CivilianLosses, HostilesDefeated;
        public readonly string Failure;
        public UiTrustUnderFireResultModel(bool northArrived, bool southArrived, bool relayVerified, int civilianLosses, int hostilesDefeated, string failure)
        { NorthArrived=northArrived; SouthArrived=southArrived; RelayVerified=relayVerified; CivilianLosses=civilianLosses; HostilesDefeated=hostilesDefeated; Failure=failure ?? string.Empty; }
    }
    public interface IUiTrustUnderFireResultGateway { bool TryReadTrustUnderFireResult(out UiTrustUnderFireResultModel model); }
    public interface IUiTrustUnderFireGateway { bool TryReadTrustUnderFire(out UiTrustUnderFireModel model); }
}
