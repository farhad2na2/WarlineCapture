using UnityEngine;
namespace Game.UI.Contracts
{
    public readonly struct UiCommandNodeModel
    {
        public readonly int Stage, HoldSeconds, RemainingSeconds, HostilesDefeated, IsolationStep;
        public readonly bool ClinicIsolated, UtilityIsolated, NodeDisabled, Breached, QassemDefeated, AuditPreserved, ReleaseOrdered, AuditReleased, SpecialistsSafe, CoverReady;
        public readonly Vector3 IsolationClinic, IsolationUtility, BreachGate, CoreAudit, AuditRelease, SafeReceiving, CoverGate;
        public UiCommandNodeModel(int stage, int hold, int remaining, int hostiles, int isolation, bool clinic, bool utility, bool node, bool breached, bool qassem, bool audit, bool ordered, bool released, bool safe, Vector3 clinicGate, Vector3 utilityGate, Vector3 breach, Vector3 core, Vector3 release, Vector3 receiving, bool coverReady = false, Vector3 coverGate = default)
        { Stage = stage; HoldSeconds = hold; RemainingSeconds = remaining; HostilesDefeated = hostiles; IsolationStep = isolation; ClinicIsolated = clinic; UtilityIsolated = utility; NodeDisabled = node; Breached = breached; QassemDefeated = qassem; AuditPreserved = audit; ReleaseOrdered = ordered; AuditReleased = released; SpecialistsSafe = safe; IsolationClinic = clinicGate; IsolationUtility = utilityGate; BreachGate = breach; CoreAudit = core; AuditRelease = release; SafeReceiving = receiving; CoverReady = coverReady; CoverGate = coverGate; }
    }
    public readonly struct UiCommandNodeResultModel
    {
        public readonly bool NetworkSeparated, AuditReleased, SpecialistsSafe;
        public UiCommandNodeResultModel(bool network, bool audit, bool safe) { NetworkSeparated = network; AuditReleased = audit; SpecialistsSafe = safe; }
    }
    public interface IUiCommandNodeGateway { bool TryReadCommandNode(out UiCommandNodeModel model); }
    public interface IUiCommandNodeResultGateway { bool TryReadCommandNodeResult(out UiCommandNodeResultModel model); }
    public static class UiCommandNodeProgress
    {
        public static int WatchGoal(int stage, int defeated, int isolation, bool releaseOrdered, bool coverReady = false) => 7200 + (stage switch
        { 1 => System.Math.Clamp(defeated, 0, 4), 2 => 5 + System.Math.Clamp(isolation, 0, 2), 3 => 8, 4 => coverReady ? 10 : 9, 5 => 11 + System.Math.Clamp(defeated - 5, 0, 5), 6 => 17, 7 => releaseOrdered ? 19 : 18, 8 => 20, _ => 21 });
    }
}
