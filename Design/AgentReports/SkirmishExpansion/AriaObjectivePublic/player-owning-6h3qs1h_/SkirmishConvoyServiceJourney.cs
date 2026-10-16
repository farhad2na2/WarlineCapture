using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public enum ConvoyServiceStage:byte { None,Travel,Hold,Repair,Resume,EmergencyHold,Handback }
    public sealed class SkirmishConvoyServiceJourney
    {
        public ConvoyServiceStage Stage;
        public string Attempt,OriginalId,ServiceId,Failure;
        public uint GroupId,Epoch=1,ProgressRevision,LastRejectedRequest,LastRepairRequest,LastRepairFailureRequest,HoldRevisionAtStart;
        public int Failures,DestinationIndex,HealthAtStart;
        public float BestDistance=float.PositiveInfinity,LastProgress;
        readonly Dictionary<string,int> servicedHealth=new(StringComparer.Ordinal);
        public void Reset(){Stage=ConvoyServiceStage.None;Attempt=OriginalId=ServiceId=null;GroupId=ProgressRevision=LastRejectedRequest=LastRepairRequest=LastRepairFailureRequest=HoldRevisionAtStart=0;Failures=DestinationIndex=HealthAtStart=0;BestDistance=float.PositiveInfinity;LastProgress=0;Failure=null;servicedHealth.Clear();Epoch++;}
        void Retry(float now,string reason)
        {Failures++;Failure=reason;Epoch++;DestinationIndex=Failures%3;BestDistance=float.PositiveInfinity;LastProgress=now;Stage=Failures>=3?ConvoyServiceStage.Handback:ConvoyServiceStage.Travel;}
        // This state machine receives the exact rendered own UI snapshot. It
        // has no EntityManager, pathfinder, collector, command or repair service.
        public ConvoyServiceStage Tick(UiSkirmishPublicObjectiveModel model,UiVehicleRepairModel selected,bool acceptedServiceHold,bool acceptedRepairContact)
        {
            if(model==null||model.Goal==null||model.Goal.Perspective!=SkirmishPublicGoalPerspective.Escort||model.Goal.Outcome!=SkirmishOutcomeKind.None)
            {Reset();return Stage;}
            if(Attempt!=model.AttemptId){Reset();Attempt=model.AttemptId;}
            if(model.Paused)return Stage;
            var service=model.Service;
            if(Stage==ConvoyServiceStage.Handback)return Stage;
            if(Stage==ConvoyServiceStage.None)
            {
                // A critically damaged original must not keep travelling merely
                // because a legal paid repair is unavailable. Stop through the
                // ordinary visible Hold control, then explain the missing gate.
                foreach(var group in model.OwnGroups)
                {
                    if(!group.Policy.Own||!group.Policy.Living||!group.Policy.CanHold||group.Policy.DesignatedSubjects.Length!=1)continue;
                    string id=group.Policy.DesignatedSubjects[0];
                    for(int i=0;i<model.Goal.NodeCount;i++)
                    {
                        var node=model.Goal.Node(i);if(node.PublicId!=id||node.Kind!=SkirmishPublicGoalNodeKind.OwnDesignatedSubject||
                            node.RevealedSubjectKind!=SkirmishPublicSubjectKind.ObjectiveConvoyTruck||node.SubjectDisposition!=SkirmishPublicSubjectDisposition.Live||
                            node.Provenance!=SkirmishPublicGoalProvenance.OwnRoster||!node.HealthKnown||node.Health<=0||node.MaximumHealth<=0||(long)node.Health*100>(long)node.MaximumHealth*35)continue;
                        var quote=service==null?null:Array.Find(service.Trucks,truck=>truck.OriginalPublicId==id);
                        if(service!=null&&service.Available&&quote!=null&&quote.Affordable)continue;
                        OriginalId=id;GroupId=group.Policy.GroupId;Stage=ConvoyServiceStage.EmergencyHold;HoldRevisionAtStart=model.Order.Revision;Epoch++;
                        Failure=service==null||!service.Available?"The critically damaged truck needs an available own service pad.":"The critically damaged truck needs more shown Materials for repair.";
                        return Stage;
                    }
                }
                if(service==null||!service.Available||service.Destinations.Length!=3||service.EligibilityRadiusMetres!=12)return Stage;
                UiPublicConvoyRepairTruck best=null;int bestScore=int.MinValue;
                foreach(var truck in service.Trucks)
                {
                    var group=Array.Find(model.OwnGroups,row=>row.Policy.GroupId==truck.GroupId);
                    if(group==null||group.AliveCount!=1||!group.Policy.Own||!group.Policy.Living||!group.Policy.CanMove||
                        group.Policy.DesignatedSubjects.Length!=1||group.Policy.DesignatedSubjects[0]!=truck.OriginalPublicId||!OwnOriginal(model.Goal,truck)||
                        !truck.Affordable||truck.Health<=0||truck.MaximumHealth<=0||(long)truck.Health*4>(long)truck.MaximumHealth*3||
                        servicedHealth.TryGetValue(truck.OriginalPublicId,out int prior)&&truck.Health>=prior)continue;
                    bool critical=(long)truck.Health*100<=(long)truck.MaximumHealth*35;
                    // A critically damaged mandatory truck can redirect travel.
                    // Otherwise preserve useful already accepted work.
                    if(group.Policy.AcceptedOrderActive&&!critical)continue;
                    float travel=Vector2.Distance(new Vector2(group.Policy.WorldX,group.Policy.WorldZ),new Vector2(service.World.x,service.World.z))/8;
                    var candidate=new SkirmishObjectivePolicyCandidate{Legal=true,Affordable=truck.Affordable,PreserveMandatorySubject=true,
                        PreventTerminalLoss=critical,TravelSeconds=travel};
                    if(!SkirmishObjectivePolicyUtilitySystemHelper.TryScore(candidate,out int score)||score<=bestScore)continue;
                    best=truck;bestScore=score;
                }
                if(best==null)return Stage;
                GroupId=best.GroupId;OriginalId=best.OriginalPublicId;HealthAtStart=best.Health;ServiceId=service.PublicId;
                Stage=ConvoyServiceStage.Travel;Failures=0;DestinationIndex=0;BestDistance=float.PositiveInfinity;LastProgress=model.SimulationSeconds;Epoch++;
            }
            var own=Array.Find(model.OwnGroups,row=>row.Policy.GroupId==GroupId&&Array.IndexOf(row.Policy.DesignatedSubjects,OriginalId)>=0);
            if(own==null){Stage=ConvoyServiceStage.Resume;return Stage;}
            if(Stage==ConvoyServiceStage.EmergencyHold)
            {if(acceptedServiceHold&&NewHold(model)&&own.HoldActive){Stage=ConvoyServiceStage.Handback;Epoch++;}return Stage;}
            if(service==null||!service.Available||service.PublicId!=ServiceId)
            {Failure="The own service pad is unavailable.";Stage=ConvoyServiceStage.Handback;return Stage;}
            float distance=Vector2.Distance(new Vector2(own.Policy.WorldX,own.Policy.WorldZ),new Vector2(service.World.x,service.World.z));
            if(!float.IsFinite(distance)||own.OwnMemberPositions.Length!=1){Failure="The original truck position could not be verified.";Stage=ConvoyServiceStage.Handback;return Stage;}
            if(BestDistance-distance>=Mathf.Max(model.GroundCellSize,1)){BestDistance=distance;LastProgress=model.SimulationSeconds;ProgressRevision++;}
            if(model.Order.Present&&!model.Order.Accepted&&model.Order.GroupId==GroupId&&model.Order.RequestId>LastRejectedRequest&&
                model.Order.Action==AriaObjectiveAction.Move&&service.Destinations[DestinationIndex].PresentedGroundId!=0&&
                model.CompletedContactTargetId==service.Destinations[DestinationIndex].PresentedGroundId&&Math.Abs(model.CompletedContactFrame-model.Order.Frame)<=2&&
                Vector2.Distance(model.Order.Screen,model.CompletedContactPosition)<=2)
            {LastRejectedRequest=(uint)model.Order.RequestId;Retry(model.SimulationSeconds,"The truck could not reach the service approach.");return Stage;}
            if(Stage==ConvoyServiceStage.Travel)
            {
                if(distance<=service.EligibilityRadiusMetres){Stage=ConvoyServiceStage.Hold;HoldRevisionAtStart=model.Order.Revision;Epoch++;LastProgress=model.SimulationSeconds;}
                else if(model.SimulationSeconds-LastProgress>20)Retry(model.SimulationSeconds,"The truck made no progress toward the service pad.");
            }
            bool exact=selected!=null&&selected.SingleOwnVehicle&&selected.ActorPublicId==OriginalId&&selected.AttemptId==Attempt&&selected.GroupId==GroupId;
            if(Stage==ConvoyServiceStage.Hold&&acceptedServiceHold&&NewHold(model)&&own.HoldActive&&exact)
            {Stage=ConvoyServiceStage.Repair;Epoch++;LastProgress=model.SimulationSeconds;}
            if(Stage==ConvoyServiceStage.Hold&&model.SimulationSeconds-LastProgress>20)Retry(model.SimulationSeconds,"The truck could not stop safely at the service pad.");
            if(Stage==ConvoyServiceStage.Repair&&exact)
            {
                if(selected.Working||selected.RequestPending){LastProgress=model.SimulationSeconds;LastRepairRequest=selected.IssuedRequestId;}
                else if(acceptedRepairContact&&selected.StateMatchesIssued&&selected.IssuedRequestId!=0&&selected.Phase==UiVehicleRepairPhase.Completed&&selected.Health>HealthAtStart)
                {
                    var next=Array.Find(service.Trucks,truck=>truck.OriginalPublicId==OriginalId);
                    if((long)selected.Health*4<=(long)selected.MaximumHealth*3&&next!=null&&next.Affordable)
                    {HealthAtStart=selected.Health;Stage=ConvoyServiceStage.Hold;HoldRevisionAtStart=model.Order.Revision;LastProgress=model.SimulationSeconds;Epoch++;}
                    else if((long)selected.Health*100<=(long)selected.MaximumHealth*35)
                    {Failure="The truck is critically damaged and the shown Materials cannot fund another repair.";Stage=ConvoyServiceStage.Handback;}
                    else{servicedHealth[OriginalId]=selected.Health;Stage=ConvoyServiceStage.Resume;Epoch++;}
                }
                else if(selected.Phase==UiVehicleRepairPhase.AccountingFailure)
                {Failure="The repair refund could not be verified. Please check the truck and Materials.";Stage=ConvoyServiceStage.Handback;}
                else if(selected.RequestRejected&&selected.IssuedRequestId!=LastRepairFailureRequest||selected.Phase==UiVehicleRepairPhase.Cancelled&&selected.StateRequestId!=0&&selected.StateRequestId!=LastRepairFailureRequest)
                {LastRepairFailureRequest=selected.RequestRejected?selected.IssuedRequestId:selected.StateRequestId;Retry(model.SimulationSeconds,"Repair could not continue safely.");}
                else if(model.SimulationSeconds-LastProgress>25)Retry(model.SimulationSeconds,"Repair could not start at the shown price and service conditions.");
            }
            if(Stage==ConvoyServiceStage.Repair&&!exact&&model.SimulationSeconds-LastProgress>25)
                Retry(model.SimulationSeconds,"The original truck selection and repair feedback could not be verified.");
            return Stage;
        }
        public void Resume(){Stage=ConvoyServiceStage.None;OriginalId=null;GroupId=0;Failure=null;Epoch++;}
        bool NewHold(UiSkirmishPublicObjectiveModel model)=>model.Order.Present&&model.Order.Accepted&&model.Order.Action==AriaObjectiveAction.Hold&&
            model.Order.GroupId==GroupId&&model.Order.Revision>HoldRevisionAtStart;
        static bool OwnOriginal(SkirmishPublicGoalSnapshot goal,UiPublicConvoyRepairTruck truck)
        {
            for(int i=0;i<goal.NodeCount;i++)
            {var node=goal.Node(i);if(node.PublicId==truck.OriginalPublicId&&node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&
                node.Provenance==SkirmishPublicGoalProvenance.OwnRoster&&node.RevealedSubjectKind==SkirmishPublicSubjectKind.ObjectiveConvoyTruck&&
                node.SubjectDisposition==SkirmishPublicSubjectDisposition.Live&&node.HealthKnown&&node.Health==truck.Health&&node.MaximumHealth==truck.MaximumHealth)return true;}
            return false;
        }
    }
}
