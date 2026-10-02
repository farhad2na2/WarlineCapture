using UnityEngine;
namespace Game.UI.Contracts
{
    public readonly struct UiArmorBreakModel
    {
        public readonly int Stage, RecoverySeconds, RemainingSeconds, ArmoredDefendersCleared;
        public readonly float UsableFuel;
        public readonly bool CommandDisabled, AuthorityRecovered, ReliefLost;
        public readonly Vector3 Coverage, Authority, Relief;
        public UiArmorBreakModel(int stage, int recoverySeconds, int remainingSeconds, float usableFuel,
            bool commandDisabled, bool authorityRecovered, bool reliefLost, Vector3 coverage, Vector3 authority, Vector3 relief, int armoredDefendersCleared=0)
        { Stage=stage; RecoverySeconds=recoverySeconds; RemainingSeconds=remainingSeconds; UsableFuel=usableFuel; CommandDisabled=commandDisabled; AuthorityRecovered=authorityRecovered; ReliefLost=reliefLost; Coverage=coverage; Authority=authority; Relief=relief; ArmoredDefendersCleared=armoredDefendersCleared; }
    }
    public interface IUiArmorBreakGateway { bool TryReadArmorBreak(out UiArmorBreakModel model); }
}
