using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        string publicRepairScope;
        uint publicRepairEpoch;
        // Only the current normal selected-vehicle readout is observed. This
        // adapter cannot find a service pad, drive there, heal, spend, or cancel.
        void ObserveFieldRepair(ref AriaSkirmishObservation view)
        {
            if(watchSkirmish==null||!watchSkirmish.TryReadPublicObjectiveForAria(Time.frameCount,out var frame)||
                !frame.Presented||frame.Goal==null||frame.Goal.Outcome!=SkirmishOutcomeKind.None||
                !view.PublicObjective.Supported||!view.PublicObjective.Presented||watchSelection==null||
                !watchSelection.TryReadPresentedFieldRepair(Time.frameCount,out var shown,out var button)||
                !SkirmishPresentedRepairPolicy.ShouldObserve(frame.Goal,frame.Groups,shown))return;
            string scope=shown.AttemptId+"/"+shown.ActorPublicId;
            if(publicRepairScope!=scope){publicRepairScope=scope;publicRepairEpoch++;if(publicRepairEpoch==0)publicRepairEpoch=1;}
            ulong key=PublicObjectiveKey("repair/"+scope);
            var target=ObserveScrollableButton(button,unchecked((int)(key^(key>>32))));
            bool active=shown.Working||shown.RequestPending||shown.TerminalFeedback;
            var result=new AriaObjectiveTouchObservation{Applicable=true,Supported=true,Presented=true,Paused=frame.Paused||shown.Paused,
                GroupId=shown.GroupId,NavigationGroupId=shown.GroupId,PresentationEpoch=frame.PresentationEpoch,RosterRevision=publicRepairEpoch,
                PublicKey=key,Action=AriaObjectiveAction.Repair,Intent=AriaSkirmishIntent.RepairVehicle,ExclusiveGroupSelected=true,
                CameraSettled=true,GoalInView=true,GroundFitsPublicGoal=true,RepairWorkActive=active,
                Repair=target.Drag?default:target,OpenRepair=target.Drag?target:default,
                ProgressKnown=true,PublicProgressRevision=unchecked((uint)shown.RemainingTicks^(uint)(shown.RemainingTicks>>32)^((uint)shown.Phase<<24)^(uint)shown.Health),
                AcceptedOrderRevision=shown.IssuedRequestId};
            // Both the exact native state and the actual completed UI contact
            // must join the normal issued request. Acceptance is not healing.
            if(button!=null&&SkirmishPresentedRepairPolicy.HasAcceptedContact(shown,button.GetEntityId().GetHashCode()))
            {result.AcceptedGroupId=shown.GroupId;result.AcceptedPublicKey=key;result.AcceptedAction=AriaObjectiveAction.Repair;
                result.AcceptedContactTargetId=shown.CompletedContactTargetId;result.AcceptedFrame=shown.CompletedContactFrame;}
            view.PublicObjective=result;
        }
    }

    public static class SkirmishPresentedRepairPolicy
    {
        public static bool HasAcceptedContact(UiVehicleRepairModel shown,int targetId)=>shown!=null&&targetId!=0&&shown.StateMatchesIssued&&
            !shown.IssuedCancel&&shown.IssuedScreenKnown&&shown.CompletedContactTargetId==targetId&&
            shown.CompletedContactFrame>=shown.IssuedFrame&&Vector2.Distance(shown.CompletedContactPosition,shown.IssuedScreen)<=32;
        public static bool SameQuote(UiVehicleRepairModel shown,UiVehicleRepairModel actual)=>shown!=null&&actual!=null&&
            shown.AttemptId==actual.AttemptId&&shown.ActorPublicId==actual.ActorPublicId&&shown.GroupId==actual.GroupId&&
            shown.Health==actual.Health&&shown.MaximumHealth==actual.MaximumHealth&&shown.Materials==actual.Materials&&
            shown.DurationTicks==actual.DurationTicks&&shown.FractionNumerator==actual.FractionNumerator&&shown.FractionDenominator==actual.FractionDenominator;
        public static bool ShouldObserve(SkirmishPublicGoalSnapshot goal,SkirmishPublicPlayerGroup[] groups,UiVehicleRepairModel shown)
        {
            if(goal==null||goal.Outcome!=SkirmishOutcomeKind.None||goal.Perspective==SkirmishPublicGoalPerspective.BaseAssault||
                shown==null||!shown.Visible||!shown.SingleOwnVehicle||shown.GroupId==0||string.IsNullOrEmpty(shown.ActorPublicId))return false;
            SkirmishPublicPlayerGroup group=null;
            foreach(var row in groups??System.Array.Empty<SkirmishPublicPlayerGroup>())if(row.GroupId==shown.GroupId)
            {if(group!=null)return false;group=row;}
            if(group==null||!group.Own||!group.Living)return false;
            if(shown.Working||shown.RequestPending||shown.TerminalFeedback)return true;
            // Preserve accepted travel/defense. Auto-repair is a bounded service
            // action for an already selected damaged mandatory convoy at a safe
            // own pad, never a hidden logistics route or another mission policy.
            if(goal.Perspective!=SkirmishPublicGoalPerspective.Escort||group.AcceptedOrderActive||!shown.Eligible||
                shown.MaximumHealth<=0||shown.Health<=0||(long)shown.Health*4>(long)shown.MaximumHealth*3)return false;
            bool mandatory=false;
            for(int i=0;i<goal.NodeCount;i++)
            {var node=goal.Node(i);if(node.PublicId==shown.ActorPublicId&&node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&
                node.RevealedSubjectKind==SkirmishPublicSubjectKind.ObjectiveConvoyTruck&&node.SubjectDisposition==SkirmishPublicSubjectDisposition.Live&&
                node.Provenance==SkirmishPublicGoalProvenance.OwnRoster)mandatory=true;}
            return mandatory&&SkirmishObjectivePolicyUtilitySystemHelper.TryScore(new SkirmishObjectivePolicyCandidate
                {Legal=true,Affordable=true,PreserveMandatorySubject=true},out _);
        }
    }
}
