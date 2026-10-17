using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    // Pure legal-observation allocation. It has no Entity, world query, source
    // lookup, clock mutation, purchase API or gameplay actuator.
    public static class SkirmishEnemyObjectiveAllocationUtilitySystemHelper
    {
        public enum Duty : byte { None, Capture, Screen, DefendCorridor, Patrol, Intercept, Reserve }
        public struct Group
        {
            public uint Id;
            public SkirmishPopulationCategory Category;
            public int Supply,Members;
            public float X,Z,Speed;
            public bool Commandable,Healthy;
        }
        public struct PublicPatrolPoint
        {
            public string PublicRouteId;
            public float X,Z;
        }
        public struct Assignment
        {
            public uint GroupId;
            public string PublicGoalId;
            public Duty Task;
            public float X,Z;
            public int Score;
        }

        public static bool TryAllocate(SkirmishPublicGoalSnapshot goal,IReadOnlyList<Group> ownGroups,
            IReadOnlyList<PublicPatrolPoint> authoredPatrols,float homeX,float homeZ,
            out Assignment[] assignments,out string error,IReadOnlyList<Assignment> previousAssignments=null)
        {
            assignments=null;error="Independent active faction-2 goal and own commandable groups required.";
            if(goal==null||goal.ObserverFaction!=2||goal.Outcome!=SkirmishOutcomeKind.None||ownGroups==null||ownGroups.Count>1024||
                !Finite(homeX)||!Finite(homeZ)||goal.ObjectiveKind==SkirmishObjectiveKind.BaseAssault)return false;
            var groups=new List<Group>();var ids=new HashSet<uint>();
            foreach(var group in ownGroups)
            {
                if(group.Id==0||!ids.Add(group.Id)||group.Supply<0||group.Members<0||!Finite(group.X)||!Finite(group.Z)||!Finite(group.Speed)||group.Speed<=0)return false;
                if(group.Commandable&&group.Members>0&&(group.Category==SkirmishPopulationCategory.Infantry||group.Category==SkirmishPopulationCategory.Ground))groups.Add(group);
            }
            groups.Sort((a,b)=>a.Id.CompareTo(b.Id));
            var points=new List<SkirmishPublicGoalNode>();
            var contacts=new List<SkirmishPublicGoalNode>();
            for(int i=0;i<goal.NodeCount;i++)
            {
                var node=goal.Node(i);
                if(!node.PositionKnown)continue;
                if(node.Kind==SkirmishPublicGoalNodeKind.ObservedOpposingSubject)
                {
                    if(node.CurrentlyVisible&&node.Provenance==SkirmishPublicGoalProvenance.LegallyObservedContact&&
                        node.LastObservedTicks==goal.ElapsedTicks&&node.SubjectDisposition==SkirmishPublicSubjectDisposition.Live)contacts.Add(node);
                    continue;
                }
                if(goal.Perspective==SkirmishPublicGoalPerspective.Capture&&node.Kind==SkirmishPublicGoalNodeKind.CaptureZone||
                    goal.Perspective==SkirmishPublicGoalPerspective.DefendExits&&node.Kind==SkirmishPublicGoalNodeKind.ExitCorridor)points.Add(node);
            }
            points.Sort((a,b)=>string.CompareOrdinal(a.PublicId,b.PublicId));
            if(goal.Perspective==SkirmishPublicGoalPerspective.Capture)
            {
                if(points.Count!=3)return false;
                points.Sort((a,b)=>
                {
                    // Retain a sustainable existing majority before spreading to
                    // the third zone. Neutralization is not counted as ownership.
                    int owner=(b.OwnerKnown&&b.OwnerFaction==2).CompareTo(a.OwnerKnown&&a.OwnerFaction==2);
                    if(owner!=0)return owner;
                    int progress=(b.CaptureProgressKnown&&b.CapturingFaction==2?b.CaptureFraction:0).CompareTo(a.CaptureProgressKnown&&a.CapturingFaction==2?a.CaptureFraction:0);
                    return progress!=0?progress:string.CompareOrdinal(a.PublicId,b.PublicId);
                });
                points.RemoveAt(2);
            }
            else if(goal.Perspective==SkirmishPublicGoalPerspective.DefendExits)
            {if(points.Count!=2)return false;}
            else if(goal.Perspective==SkirmishPublicGoalPerspective.Interdict)
            {
                if(authoredPatrols==null||authoredPatrols.Count!=2)return false;
                var routes=new HashSet<string>(StringComparer.Ordinal);
                foreach(var point in authoredPatrols)
                {
                    if(string.IsNullOrWhiteSpace(point.PublicRouteId)||!routes.Add(point.PublicRouteId)||!Finite(point.X)||!Finite(point.Z))return false;
                    points.Add(new SkirmishPublicGoalNode{PublicId=point.PublicRouteId,PositionKnown=true,WorldX=point.X,WorldZ=point.Z,Provenance=SkirmishPublicGoalProvenance.PublicObjective});
                }
                if(points[0].WorldX==points[1].WorldX&&points[0].WorldZ==points[1].WorldZ)return false;
            }
            else return false;

            var output=new List<Assignment>();var load=new int[2];int total=0;
            foreach(var group in groups)if(group.Healthy)
            {if((long)total+group.Supply>int.MaxValue)return false;total+=group.Supply;}
            // Whole-squad 40/40/20 initial BT/CE coverage. FC uses its retained
            // two points, and never reserves its sole/second capture squad.
            int reserveTarget=goal.Perspective==SkirmishPublicGoalPerspective.Capture?0:total/5;
            int reserveSupply=0,assigned=0;
            groups.Sort((a,b)=>
            {
                // Infantry must establish the two captures before vehicles screen.
                int infantry=(b.Category==SkirmishPopulationCategory.Infantry).CompareTo(a.Category==SkirmishPopulationCategory.Infantry);
                return infantry!=0?infantry:a.Id.CompareTo(b.Id);
            });
            foreach(var group in groups)
            {
                if(!group.Healthy)
                {output.Add(At(group,"own.rally",Duty.Reserve,homeX,homeZ,0));continue;}
                bool reserve=goal.Perspective!=SkirmishPublicGoalPerspective.Capture&&assigned>=2&&reserveSupply<reserveTarget;
                if(reserve)
                {
                    reserveSupply=checked(reserveSupply+group.Supply);
                    if(TryNearestContact(goal,group,contacts,previousAssignments,out var intercept))output.Add(intercept);
                    else output.Add(At(group,"own.rally",Duty.Reserve,homeX,homeZ,0));
                    continue;
                }
                int slot=load[0]<=load[1]?0:1;
                if(load[0]==load[1]&&Distance(group,points[1].WorldX,points[1].WorldZ)<Distance(group,points[0].WorldX,points[0].WorldZ))slot=1;
                var point=points[slot];
                bool infantry=group.Category==SkirmishPopulationCategory.Infantry;
                // Vehicles can screen an FC point but cannot capture it. They
                // never replace the first required infantry assignment to a zone.
                var task=goal.Perspective==SkirmishPublicGoalPerspective.Capture?(infantry?Duty.Capture:Duty.Screen):
                    goal.Perspective==SkirmishPublicGoalPerspective.DefendExits?Duty.DefendCorridor:Duty.Patrol;
                var candidate=new SkirmishObjectivePolicyCandidate{Legal=true,Affordable=true,
                    ObjectiveGain=goal.Perspective!=SkirmishPublicGoalPerspective.Capture||infantry,
                    PreventTerminalLoss=goal.Perspective==SkirmishPublicGoalPerspective.Capture&&goal.Score.TicketsKnown&&goal.Score.OwnTickets<goal.Score.OpposingTickets,
                    TravelSeconds=Distance(group,point.WorldX,point.WorldZ)/group.Speed};
                if(!SkirmishObjectivePolicyUtilitySystemHelper.TryScore(candidate,out int score))return false;
                output.Add(At(group,point.PublicId,task,point.WorldX,point.WorldZ,score));
                load[slot]=checked(load[slot]+Math.Max(1,group.Supply));assigned++;
            }
            assignments=output.ToArray();error=null;return true;
        }

        private static bool TryNearestContact(SkirmishPublicGoalSnapshot goal,Group group,List<SkirmishPublicGoalNode> contacts,IReadOnlyList<Assignment> previous,out Assignment chosen)
        {
            chosen=default;bool found=false;int best=int.MinValue;
            string currentId=null;if(previous!=null)foreach(var assignment in previous)
                if(assignment.GroupId==group.Id&&assignment.Task==Duty.Intercept){currentId=assignment.PublicGoalId;break;}
            bool currentLegal=false;Assignment current=default;
            foreach(var contact in contacts)
            {
                var candidate=new SkirmishObjectivePolicyCandidate{Legal=true,Affordable=true,ObjectiveGain=true,
                    PreventTerminalLoss=contact.RevealedSubjectKind!=SkirmishPublicSubjectKind.None,
                    TravelSeconds=Distance(group,contact.WorldX,contact.WorldZ)/group.Speed};
                if(!SkirmishObjectivePolicyUtilitySystemHelper.TryScore(candidate,out int score))continue;
                if(contact.PublicId==currentId){currentLegal=true;current=At(group,contact.PublicId,Duty.Intercept,contact.WorldX,contact.WorldZ,score);}
                if(!found||score>best||score==best&&string.CompareOrdinal(contact.PublicId,chosen.PublicGoalId)<0)
                {best=score;chosen=At(group,contact.PublicId,Duty.Intercept,contact.WorldX,contact.WorldZ,score);found=true;}
            }
            if(found&&currentLegal&&!SkirmishObjectivePolicyUtilitySystemHelper.ShouldReplaceAssignment(current.Score,best,true,false))chosen=current;
            return found;
        }
        private static Assignment At(Group group,string id,Duty duty,float x,float z,int score)=>new Assignment{GroupId=group.Id,PublicGoalId=id,Task=duty,X=x,Z=z,Score=score};
        private static float Distance(Group group,float x,float z)=>(float)Math.Sqrt((double)(group.X-x)*(group.X-x)+(double)(group.Z-z)*(group.Z-z));
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
