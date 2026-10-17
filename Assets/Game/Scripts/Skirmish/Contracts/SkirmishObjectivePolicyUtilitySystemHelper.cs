using System;

namespace Game.Skirmish.Contracts
{
    // Facts are supplied by each side's independently audited legal observer.
    // This score neither discovers targets nor issues orders or UI input.
    public struct SkirmishObjectivePolicyCandidate
    {
        public bool Legal, Affordable;
        public bool ObjectiveGain, PreventTerminalLoss, PreserveMandatorySubject, ObservedCounterAdvantage;
        public float TravelSeconds;
        public int KnownExposurePenalty;
    }

    public static class SkirmishObjectivePolicyUtilitySystemHelper
    {
        public const int Version=1, AssignmentSwitchMargin=15;
        public static bool TryScore(in SkirmishObjectivePolicyCandidate candidate,out int score)
        {
            score=0;
            if(!candidate.Legal||!candidate.Affordable||float.IsNaN(candidate.TravelSeconds)||
                float.IsInfinity(candidate.TravelSeconds)||candidate.TravelSeconds<0||
                candidate.KnownExposurePenalty<0||candidate.KnownExposurePenalty>30)return false;
            int travel=(int)Math.Ceiling(Math.Min(30d,candidate.TravelSeconds));
            score=(candidate.ObjectiveGain?40:0)+(candidate.PreventTerminalLoss?100:0)+
                (candidate.PreserveMandatorySubject?80:0)+(candidate.ObservedCounterAdvantage?20:0)-
                travel-candidate.KnownExposurePenalty;
            return true;
        }

        // A critical event removes hysteresis, not legality or score comparison.
        // Ties retain the current assignment. Invalid current assignments can be
        // replaced only by a candidate that the caller has already scored legally.
        public static bool ShouldReplaceAssignment(int currentScore,int alternativeScore,
            bool currentStillLegal,bool terminalRiskChanged)
        {
            if(!currentStillLegal)return true;
            return alternativeScore>currentScore&&
                (terminalRiskChanged||(long)alternativeScore-currentScore>=AssignmentSwitchMargin);
        }
    }
}
