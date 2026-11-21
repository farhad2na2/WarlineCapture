using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>
    /// W38 (gap table step 2): the pure Operations objective-graph steps that OperationsReconObjectiveSystem used to do
    /// inline. The native system gathers facts from the shared combat world (alive, dismounted, reachable, hostiles near)
    /// and passes them in as booleans; these steps own the node state transitions: SCAN, INTERACT (evidence recovery),
    /// evidence carry and EXTRACT/terminal resolution. Arithmetic and ordering are exactly the O001 rules, so O001 facts,
    /// checkpoints and settlement are unchanged. O002 nodes (ESCORT/HOLD/PROTECT/CLEAR) add steps here.
    /// </summary>
    public static class OperationsObjectiveGraphReducer
    {
        // O001 (RECON) terminal rules: fewer than two surviving infantry is a defeat; two scans and two at the exit allow
        // a partial; victory needs every scan, two infantry and the evidence carrier at a safe exit.
        public const int MinimumSurvivors = 2, PartialScans = 2, ExtractInfantry = 2;

        /// <summary>SCAN: advance an active scan channel. Returns true when the site completes on this tick.</summary>
        public static bool StepScan(ref OperationsReconSiteElement site, bool actorCanContinue, float scanSeconds, float delta)
        {
            if (site.Completed != 0 || site.Actor == Entity.Null) return false;
            if (!actorCanContinue) { site.Actor = Entity.Null; site.ChannelSeconds = 0f; return false; }
            site.ChannelSeconds = math.min(scanSeconds, site.ChannelSeconds + delta);
            if (site.ChannelSeconds < scanSeconds) return false;
            site.Completed = 1;
            site.Actor = Entity.Null;
            return true;
        }

        /// <summary>INTERACT: advance the evidence recovery channel. Returns true when the evidence is recovered.</summary>
        public static bool StepInteract(ref OperationsReconEvidenceComponent evidence, bool actorCanContinue, float seconds, float delta)
        {
            if (evidence.Actor == Entity.Null) return false;
            if (!actorCanContinue) { evidence.Actor = Entity.Null; evidence.ChannelSeconds = 0f; return false; }
            evidence.ChannelSeconds = math.min(seconds, evidence.ChannelSeconds + delta);
            if (evidence.ChannelSeconds < seconds) return false;
            evidence.Carrier = evidence.Actor;
            evidence.Actor = Entity.Null;
            evidence.Recovered = 1;
            return true;
        }

        /// <summary>
        /// Evidence carry: the evidence follows its living carrier; if the carrier dies it stays at the last known world
        /// position and can be recovered again.
        /// </summary>
        public static void CarryEvidence(ref OperationsReconEvidenceComponent evidence, bool carrierHasPosition, float3 carrierPosition, bool carrierAlive)
        {
            if (evidence.Carrier == Entity.Null) return;
            if (carrierHasPosition) evidence.Position = carrierPosition;
            if (!carrierAlive)
            {
                evidence.Carrier = Entity.Null;
                evidence.Actor = Entity.Null;
                evidence.ChannelSeconds = 0f;
            }
        }

        /// <summary>EXTRACT and terminal resolution, in the O001 priority order. None while the mission continues.</summary>
        public static OperationsReconOutcome Resolve(int survivingInfantry, int completedScans, int requiredScans, int infantryAtExit,
            bool evidenceAtExit, float elapsedSeconds, float deadlineSeconds, bool withdraw, bool conclude, out bool partialAvailable)
        {
            partialAvailable = completedScans >= PartialScans && infantryAtExit >= ExtractInfantry;
            if (survivingInfantry < MinimumSurvivors) return OperationsReconOutcome.Defeat;
            if (completedScans == requiredScans && infantryAtExit >= ExtractInfantry && evidenceAtExit) return OperationsReconOutcome.Victory;
            if (elapsedSeconds >= deadlineSeconds) return partialAvailable ? OperationsReconOutcome.Partial : OperationsReconOutcome.Defeat;
            if (withdraw) return OperationsReconOutcome.Withdraw;
            if (conclude && partialAvailable) return OperationsReconOutcome.Partial;
            return OperationsReconOutcome.None;
        }
    }
}
