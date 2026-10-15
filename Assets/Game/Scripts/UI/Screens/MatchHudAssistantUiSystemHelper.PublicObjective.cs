using System;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private SkirmishPlayerObjectiveChoice publicObjectiveChoice;
        private uint publicObjectiveEpoch, publicObjectiveEvent;
        private float nextPublicObjectiveDecision;
        private static ulong PublicObjectiveKey(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            unchecked { ulong hash=14695981039346656037UL;foreach(char c in value){hash=(hash^(byte)c)*1099511628211UL;hash=(hash^(byte)(c>>8))*1099511628211UL;}return hash==0?1:hash; }
        }
        private void ObservePublicObjective(ref AriaSkirmishObservation view)
        {
            if (watchSkirmish == null) return;
            if (!watchSkirmish.TryReadPublicObjectiveForAria(Time.frameCount, out var frame))
            { if(watchSkirmish.PublicObjectiveAriaExpected)view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true};return; }
            var goal = frame.Goal;
            if (frame.PresentationEpoch != publicObjectiveEpoch) { publicObjectiveChoice=default;publicObjectiveEpoch=frame.PresentationEpoch;nextPublicObjectiveDecision=0; }
            if (goal.Perspective != SkirmishPublicGoalPerspective.Capture && goal.Perspective != SkirmishPublicGoalPerspective.Evacuate && goal.Perspective != SkirmishPublicGoalPerspective.Escort)
            { view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true};return; }
            var result = new AriaObjectiveTouchObservation { Applicable=true, Supported=true, Presented=true,
                Intent=AriaSkirmishIntent.ObserveObjective,
                Finished=goal.Outcome!=SkirmishOutcomeKind.None, CameraSettled=frame.CameraSettled, Paused=frame.Paused,
                RosterRevision=frame.RosterRevision, PresentationEpoch=frame.PresentationEpoch, MoveMode=frame.MoveMode, AcceptedOrderRevision=frame.AcceptedOrderRevision,
                AcceptedGroupId=frame.AcceptedGroup, AcceptedNavigationGroupId=frame.AcceptedNavigationGroupId, AcceptedAction=frame.AcceptedAction, AcceptedContactTargetId=frame.AcceptedContactTargetId, AcceptedFrame=frame.AcceptedFrame,
                PublicProgressRevision=frame.PublicProgressRevision, ProgressKnown=frame.ProgressKnown };
            // Collision check is over exact currently displayed public IDs. A hash
            // never becomes a native target ID and cannot hide an ambiguous join.
            for(int i=0;i<goal.NodeCount;i++)for(int j=i+1;j<goal.NodeCount;j++)
                if(PublicObjectiveKey(goal.Node(i).PublicId)==PublicObjectiveKey(goal.Node(j).PublicId))
                { result.Supported=false;view.PublicObjective=result;return; }
            result.AcceptedPublicKey=PublicObjectiveKey(frame.AcceptedGoalId);
            if ((float.IsNaN(frame.SimulationSeconds)||float.IsInfinity(frame.SimulationSeconds)) || frame.SimulationSeconds<0) { result.Supported=false;view.PublicObjective=result;return; }
            if (!frame.Paused && (!publicObjectiveChoice.Available || frame.SimulationSeconds>=nextPublicObjectiveDecision || publicObjectiveEvent!=frame.SignificantPublicEventRevision))
            {
                publicObjectiveChoice=SkirmishPlayerObjectivePolicyUtilitySystemHelper.Choose(goal,frame.Groups,frame.Approaches,publicObjectiveChoice);
                nextPublicObjectiveDecision=frame.SimulationSeconds+4;publicObjectiveEvent=frame.SignificantPublicEventRevision;
            }
            if (!publicObjectiveChoice.Available) { view.PublicObjective=result;return; }
            AriaPresentedObjectiveTarget? chosen=null;
            foreach(var target in frame.Targets)
                if(target.GroupId==publicObjectiveChoice.GroupId&&target.PublicId==publicObjectiveChoice.GoalId)
                {if(chosen.HasValue){result.Supported=false;view.PublicObjective=result;return;}chosen=target;}
            if(!chosen.HasValue){result.Supported=false;view.PublicObjective=result;return;}
            var shown=chosen.Value;
            result.GroupId=publicObjectiveChoice.GroupId;result.PublicKey=PublicObjectiveKey(publicObjectiveChoice.GoalId);
            result.Reason=(int)publicObjectiveChoice.Reason;
            result.Action=publicObjectiveChoice.Action==SkirmishPublicPlayerAction.Move?AriaObjectiveAction.Move:
                publicObjectiveChoice.Action==SkirmishPublicPlayerAction.Hold?AriaObjectiveAction.Hold:
                publicObjectiveChoice.Action==SkirmishPublicPlayerAction.Unload?AriaObjectiveAction.Unload:AriaObjectiveAction.Inspect;
            result.Intent=result.Action==AriaObjectiveAction.Inspect?AriaSkirmishIntent.InspectObjective:
                result.Action==AriaObjectiveAction.Unload?AriaSkirmishIntent.UnloadOriginals:
                goal.Perspective==SkirmishPublicGoalPerspective.Capture?AriaSkirmishIntent.ControlZone:
                goal.Perspective==SkirmishPublicGoalPerspective.Escort?AriaSkirmishIntent.EscortOriginalTruck:
                goal.Score.ExitOpen?AriaSkirmishIntent.EvacuateOriginals:AriaSkirmishIntent.OpenObjectiveCorridor;
            result.NavigationGroupId=result.Action==AriaObjectiveAction.Unload?shown.NavigationGroupId:result.GroupId;
            result.ExclusiveGroupSelected=frame.ExclusiveSelectedGroup==result.NavigationGroupId;
            result.SelectGroup=MatchHudSquadTouchNavigation.GroupTarget(watchSquads,result.NavigationGroupId);
            result.ClearOtherSelection=view.ExpandedSelectedGroups>0&&!result.ExclusiveGroupSelected
                ? MatchHudSquadTouchNavigation.SelectedCardTarget(watchSquads):default;
            result.Focus=ObserveWatchButton(shown.FocusButton);if(!result.Focus.Available)result.Focus=ObservePublicObjectiveMapFocus(shown.PublicWorld,view);result.Unload=ObserveWatchButton(shown.UnloadButton);result.OpenUnload=ObserveWatchButton(shown.OpenUnloadButton);
            result.Move=ObserveWatchButton(_commandControlsView?.MoveButton);result.Hold=ObserveWatchButton(_commandControlsView?.HoldButton);
            result.GoalInView=shown.InView;result.GroundFitsPublicGoal=shown.GroundFitsPublicGoal;
            result.Ground=shown.Ground;
            if(result.Ground.Available&&!WatchTargetIsReachable(result.Ground.Position,result.Ground.Id,true))result.Ground=default;
            view.PublicObjective=result;
        }
    }
}
